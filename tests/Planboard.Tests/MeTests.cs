namespace Planboard.Tests;

public class MeTests(PlanboardApp app)
{
    private const string MeQuery = "{ me { email displayName role tenant { name } } }";

    [Theory]
    [InlineData(Tokens.AcmeOwner, "alice@acme.test", "OWNER", "Acme")]
    [InlineData(Tokens.GlobexMember, "heidi@globex.test", "MEMBER", "Globex")]
    [InlineData(Tokens.InitechOwner, "peter@initech.test", "OWNER", "Initech")]
    public async Task Me_returns_the_user_behind_the_token(string token, string email, string role, string tenant)
    {
        var body = await app.CreateClient(token).PostGraphQLAsync(MeQuery);

        var me = body.GetProperty("data").GetProperty("me");
        Assert.Equal(email, me.GetProperty("email").GetString());
        Assert.Equal(role, me.GetProperty("role").GetString());
        Assert.Equal(tenant, me.GetProperty("tenant").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Me_without_a_token_is_not_authenticated()
    {
        var body = await app.CreateClient().PostGraphQLAsync(MeQuery);

        Assert.Equal("AUTH_NOT_AUTHENTICATED", body.ErrorCode());
    }

    [Fact]
    public async Task Me_with_an_unknown_token_is_not_authenticated()
    {
        var body = await app.CreateClient("pb_dev_nobody").PostGraphQLAsync(MeQuery);

        Assert.Equal("AUTH_NOT_AUTHENTICATED", body.ErrorCode());
    }
}
