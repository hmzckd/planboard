using System.Text.Json;
using Npgsql;

namespace Planboard.Tests;

public class RoleTests(PlanboardApp app)
{
    private const string CreateTask =
        """
        mutation($projectId: ID!) {
          createTask(input: { projectId: $projectId, title: "Viewer task" }) { task { id } }
        }
        """;

    private const string Invite =
        """
        mutation($email: String!, $role: Role!) {
          inviteMember(input: { email: $email, displayName: "New Person", role: $role }) {
            invitedMember { token user { id email role } }
            errors { __typename ... on Error { message } }
          }
        }
        """;

    [Fact]
    public async Task A_viewer_can_read_but_cannot_write()
    {
        var projectId = await app.ProjectIdAsync(Tokens.AcmeViewer, "Mobile app");

        var body = await app.CreateClient(Tokens.AcmeViewer).PostGraphQLAsync(CreateTask, new { projectId });

        Assert.Equal("AUTH_NOT_AUTHORIZED", body.ErrorCode());
    }

    [Fact]
    public async Task Members_can_see_names_but_only_owners_see_other_emails()
    {
        const string query = "{ members(first: 50) { nodes { displayName role email } } }";

        var asMember = await app.CreateClient(Tokens.AcmeMember).PostGraphQLAsync(query);
        var asOwner = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(query);

        var memberNodes = asMember.GetProperty("data").GetProperty("members").GetProperty("nodes").EnumerateArray().ToList();
        Assert.Contains(memberNodes, n => n.GetProperty("displayName").GetString() == "Alice");
        Assert.Equal("AUTH_NOT_AUTHORIZED", asMember.ErrorCode());
        Assert.Contains(memberNodes, n => n.GetProperty("email").ValueKind == JsonValueKind.Null);
        Assert.Contains(memberNodes, n => n.GetProperty("email").GetString() == "bob@acme.test");

        asOwner.AssertNoErrors();
        var ownerEmails = asOwner.GetProperty("data").GetProperty("members").GetProperty("nodes").EnumerateArray()
            .Select(n => n.GetProperty("email").GetString()).ToList();
        Assert.Contains("carol@acme.test", ownerEmails);
        Assert.All(ownerEmails, email => Assert.EndsWith("@acme.test", email));
    }

    [Fact]
    public async Task Only_owners_can_invite_members()
    {
        var body = await app.CreateClient(Tokens.AcmeMember).PostGraphQLAsync(
            Invite, new { email = $"{Guid.NewGuid():N}@acme.test", role = "MEMBER" });

        Assert.Equal("AUTH_NOT_AUTHORIZED", body.ErrorCode());
    }

    [Fact]
    public async Task An_invited_member_gets_a_working_token_for_the_owners_tenant()
    {
        var email = $"{Guid.NewGuid():N}@acme.test";

        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(Invite, new { email, role = "MEMBER" });

        var token = body.AssertNoErrors().GetProperty("data").GetProperty("inviteMember")
            .GetProperty("invitedMember").GetProperty("token").GetString()!;
        var me = await app.CreateClient(token).PostGraphQLAsync("{ me { email role tenant { name } } }");
        Assert.Equal(email, me.GetProperty("data").GetProperty("me").GetProperty("email").GetString());
        Assert.Equal("Acme", me.GetProperty("data").GetProperty("me").GetProperty("tenant").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Inviting_an_existing_email_returns_a_duplicate_error()
    {
        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            Invite, new { email = "grace@globex.test", role = "MEMBER" });

        var error = body.AssertNoErrors().GetProperty("data").GetProperty("inviteMember").GetProperty("errors")[0];
        Assert.Equal("DuplicateEmailError", error.GetProperty("__typename").GetString());
    }

    [Fact]
    public async Task A_role_change_takes_effect_on_the_next_request()
    {
        var owner = app.CreateClient(Tokens.AcmeOwner);
        var invited = (await owner.PostGraphQLAsync(Invite, new { email = $"{Guid.NewGuid():N}@acme.test", role = "MEMBER" }))
            .AssertNoErrors().GetProperty("data").GetProperty("inviteMember").GetProperty("invitedMember");
        var token = invited.GetProperty("token").GetString()!;
        var userId = invited.GetProperty("user").GetProperty("id").GetString();
        var projectId = await app.ProjectIdAsync(Tokens.AcmeOwner, "Mobile app");

        (await app.CreateClient(token).PostGraphQLAsync(CreateTask, new { projectId })).AssertNoErrors();

        (await owner.PostGraphQLAsync(
            "mutation($id: ID!) { changeMemberRole(input: { userId: $id, role: VIEWER }) { user { role } } }",
            new { id = userId })).AssertNoErrors();

        var after = await app.CreateClient(token).PostGraphQLAsync(CreateTask, new { projectId });
        Assert.Equal("AUTH_NOT_AUTHORIZED", after.ErrorCode());
    }

    [Fact]
    public async Task The_last_owner_cannot_step_down()
    {
        var me = await app.CreateClient(Tokens.InitechOwner).PostGraphQLAsync("{ me { id } }");
        var peter = me.GetProperty("data").GetProperty("me").GetProperty("id").GetString();

        var body = await app.CreateClient(Tokens.InitechOwner).PostGraphQLAsync(
            """
            mutation($id: ID!) {
              changeMemberRole(input: { userId: $id, role: MEMBER }) { user { role } errors { __typename } }
            }
            """,
            new { id = peter });

        Assert.Equal("LastOwnerError", body.AssertNoErrors().GetProperty("data").GetProperty("changeMemberRole")
            .GetProperty("errors")[0].GetProperty("__typename").GetString());
    }

    [Fact]
    public async Task A_token_cannot_be_issued_for_a_user_of_another_tenant()
    {
        var acme = await app.TenantIdAsync("Acme");
        await using var connection = new NpgsqlConnection(app.AppConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using (var setTenant = new NpgsqlCommand("SELECT set_config('app.tenant_id', $1, false)", connection))
        {
            setTenant.Parameters.Add(new NpgsqlParameter { Value = acme.ToString() });
            await setTenant.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await using var lookup = new NpgsqlCommand("SELECT count(*) FROM users", connection);
        Assert.True((long)(await lookup.ExecuteScalarAsync(TestContext.Current.CancellationToken))! > 0);

        await using var forGlobex = new NpgsqlCommand(
            "SELECT issue_api_token($1, '\\x01'::bytea, 'stolen')", connection);
        forGlobex.Parameters.Add(new NpgsqlParameter { Value = await GlobexUserIdAsync() });

        var error = await Assert.ThrowsAsync<PostgresException>(
            () => forGlobex.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    private async Task<Guid> GlobexUserIdAsync()
    {
        await using var connection = new NpgsqlConnection(app.OwnerConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("SELECT id FROM users WHERE email = 'judy@globex.test'", connection);
        return (Guid)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
