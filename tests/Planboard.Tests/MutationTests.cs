using System.Text.Json;

namespace Planboard.Tests;

public class MutationTests(PlanboardApp app)
{
    private const string CreateProject =
        """
        mutation($name: String!) {
          createProject(input: { name: $name, description: "From a test" }) {
            project { id name }
            errors { __typename ... on Error { message } ... on ValidationError { code field } ... on DuplicateNameError { code } }
          }
        }
        """;

    private const string CreateTask =
        """
        mutation($projectId: ID!, $title: String!, $assigneeId: ID) {
          createTask(input: { projectId: $projectId, title: $title, assigneeId: $assigneeId }) {
            task { id title status priority assignee { email } project { name } }
            errors { __typename ... on Error { message } ... on NotFoundError { code } ... on ValidationError { code } }
          }
        }
        """;

    private const string ChangeStatus =
        """
        mutation($id: ID!, $status: TaskStatus!) {
          changeTaskStatus(input: { id: $id, status: $status }) {
            task { status }
            errors { __typename ... on InvalidStatusTransitionError { code message } }
          }
        }
        """;

    [Fact]
    public async Task Create_project_returns_the_project_and_it_shows_up_in_the_list()
    {
        var name = UniqueName("Launch plan");

        var body = await app.CreateClient(Tokens.AcmeMember).PostGraphQLAsync(CreateProject, new { name });

        var payload = Payload(body, "createProject");
        Assert.Equal(name, payload.GetProperty("project").GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("errors").ValueKind);
        Assert.Contains(name, (await app.ProjectsOf(Tokens.AcmeOwner)).Values);
        Assert.DoesNotContain(name, (await app.ProjectsOf(Tokens.GlobexOwner)).Values);
    }

    [Fact]
    public async Task Create_project_with_an_empty_name_returns_a_validation_error()
    {
        var body = await app.CreateClient(Tokens.AcmeMember).PostGraphQLAsync(CreateProject, new { name = "   " });

        var error = FirstError(body, "createProject");
        Assert.Equal("ValidationError", error.GetProperty("__typename").GetString());
        Assert.Equal("VALIDATION", error.GetProperty("code").GetString());
        Assert.Equal("name", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Create_project_with_an_existing_name_returns_a_duplicate_error()
    {
        var body = await app.CreateClient(Tokens.AcmeMember).PostGraphQLAsync(CreateProject, new { name = "Mobile app" });

        var error = FirstError(body, "createProject");
        Assert.Equal("DuplicateNameError", error.GetProperty("__typename").GetString());
    }

    [Fact]
    public async Task The_same_project_name_is_allowed_in_another_tenant()
    {
        var body = await app.CreateClient(Tokens.InitechOwner).PostGraphQLAsync(CreateProject, new { name = "Website redesign" });

        Assert.Equal("Website redesign", Payload(body, "createProject").GetProperty("project").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Create_task_fills_defaults_and_links_project_and_assignee()
    {
        var projectId = await app.ProjectIdAsync(Tokens.AcmeOwner, "Customer portal");
        var bob = await MeIdAsync(Tokens.AcmeMember);

        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            CreateTask, new { projectId, title = "Write the release notes", assigneeId = bob });

        var task = Payload(body, "createTask").GetProperty("task");
        Assert.Equal("TODO", task.GetProperty("status").GetString());
        Assert.Equal("MEDIUM", task.GetProperty("priority").GetString());
        Assert.Equal("bob@acme.test", task.GetProperty("assignee").GetProperty("email").GetString());
        Assert.Equal("Customer portal", task.GetProperty("project").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Create_task_in_another_tenants_project_returns_not_found()
    {
        var globexProject = await app.ProjectIdAsync(Tokens.GlobexOwner, "Supplier API");

        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            CreateTask, new { projectId = globexProject, title = "Sneaky task" });

        var error = FirstError(body, "createTask");
        Assert.Equal("NotFoundError", error.GetProperty("__typename").GetString());
        Assert.Equal("Project was not found.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_task_assigned_to_another_tenants_user_returns_not_found()
    {
        var projectId = await app.ProjectIdAsync(Tokens.AcmeOwner, "Customer portal");
        var heidi = await MeIdAsync(Tokens.GlobexMember);

        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            CreateTask, new { projectId, title = "Cross-tenant assignee", assigneeId = heidi });

        Assert.Equal("User was not found.", FirstError(body, "createTask").GetProperty("message").GetString());
    }

    [Fact]
    public async Task Status_moves_along_allowed_transitions_only()
    {
        var taskId = await NewTaskAsync("Status flow");
        var client = app.CreateClient(Tokens.AcmeOwner);

        var skipToDone = await client.PostGraphQLAsync(ChangeStatus, new { id = taskId, status = "DONE" });
        var error = FirstError(skipToDone, "changeTaskStatus");
        Assert.Equal("INVALID_TRANSITION", error.GetProperty("code").GetString());
        Assert.Equal("A task cannot move from TODO to DONE.", error.GetProperty("message").GetString());

        var start = await client.PostGraphQLAsync(ChangeStatus, new { id = taskId, status = "IN_PROGRESS" });
        Assert.Equal("IN_PROGRESS", Payload(start, "changeTaskStatus").GetProperty("task").GetProperty("status").GetString());

        var finish = await client.PostGraphQLAsync(ChangeStatus, new { id = taskId, status = "DONE" });
        Assert.Equal("DONE", Payload(finish, "changeTaskStatus").GetProperty("task").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Add_comment_uses_the_caller_as_author()
    {
        var taskId = await NewTaskAsync("Commented task");

        var body = await app.CreateClient(Tokens.AcmeMember).PostGraphQLAsync(
            """
            mutation($taskId: ID!) {
              addComment(input: { taskId: $taskId, body: "On it." }) { comment { body author { email } } errors { __typename } }
            }
            """,
            new { taskId });

        var comment = Payload(body, "addComment").GetProperty("comment");
        Assert.Equal("On it.", comment.GetProperty("body").GetString());
        Assert.Equal("bob@acme.test", comment.GetProperty("author").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Mutations_require_a_token()
    {
        var body = await app.CreateClient().PostGraphQLAsync(CreateProject, new { name = UniqueName("Anonymous") });

        Assert.Equal("AUTH_NOT_AUTHENTICATED", body.ErrorCode());
    }

    private async Task<string> NewTaskAsync(string title)
    {
        var projectId = await app.ProjectIdAsync(Tokens.AcmeOwner, "Customer portal");
        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(CreateTask, new { projectId, title });
        return Payload(body, "createTask").GetProperty("task").GetProperty("id").GetString()!;
    }

    private async Task<string> MeIdAsync(string token)
    {
        var body = await app.CreateClient(token).PostGraphQLAsync("{ me { id } }");
        return body.GetProperty("data").GetProperty("me").GetProperty("id").GetString()!;
    }

    private static JsonElement Payload(JsonElement body, string field) =>
        body.AssertNoErrors().GetProperty("data").GetProperty(field);

    private static JsonElement FirstError(JsonElement body, string field) =>
        Payload(body, field).GetProperty("errors")[0];

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..(prefix.Length + 9)];
}
