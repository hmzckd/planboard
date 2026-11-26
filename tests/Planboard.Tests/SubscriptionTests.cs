using System.Text.Json;

namespace Planboard.Tests;

public class SubscriptionTests(PlanboardApp app)
{
    private const string TaskChanged = """
        subscription ($projectId: ID!) {
          taskChanged(projectId: $projectId) {
            kind
            task { title status }
            changedBy { displayName }
          }
        }
        """;

    [Fact]
    public async Task A_viewer_sees_task_changes_live_with_who_made_them()
    {
        var (projectId, taskId) = await CreateProjectWithTaskAsync();
        await using var dave = await GraphQLSubscriptionClient.ConnectAsync(app, Tokens.AcmeViewer);
        Assert.Equal("connection_ack", Type(await dave.ReceiveAsync()));

        await dave.SubscribeAsync("1", TaskChanged, new { projectId });
        var updated = await FirstEventAsync(dave, taskId);

        await ChangeStatusAsync(taskId, "IN_PROGRESS");
        var statusChanged = await NextEventOtherThanAsync(dave, "UPDATED");

        Assert.Equal("UPDATED", updated.GetProperty("kind").GetString());
        Assert.Equal("Ship the board", updated.GetProperty("task").GetProperty("title").GetString());
        Assert.Equal("Alice", updated.GetProperty("changedBy").GetProperty("displayName").GetString());
        Assert.Equal("STATUS_CHANGED", statusChanged.GetProperty("kind").GetString());
        Assert.Equal("IN_PROGRESS", statusChanged.GetProperty("task").GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_project_of_another_tenant_cannot_be_subscribed_to()
    {
        var (acmeProjectId, _) = await CreateProjectWithTaskAsync();
        await using var grace = await GraphQLSubscriptionClient.ConnectAsync(app, Tokens.GlobexOwner);
        Assert.Equal("connection_ack", Type(await grace.ReceiveAsync()));

        await grace.SubscribeAsync("1", TaskChanged, new { projectId = acmeProjectId });
        var message = await grace.ReceiveAsync();

        Assert.Equal("error", Type(message));
        Assert.Equal("NOT_FOUND", message!.Value.GetProperty("payload")[0].GetProperty("extensions").GetProperty("code").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("pb_not_a_real_token")]
    public async Task A_connection_without_a_valid_token_is_closed(string? token)
    {
        await using var connection = await GraphQLSubscriptionClient.ConnectAsync(app, token);

        var message = await connection.ReceiveAsync();

        Assert.Null(message);
        Assert.Equal(4403, (int?)connection.CloseStatus);
    }

    private static string? Type(JsonElement? message) => message?.GetProperty("type").GetString();

    private static JsonElement Event(JsonElement? message)
    {
        Assert.Equal("next", Type(message));
        return message!.Value.GetProperty("payload").AssertNoErrors().GetProperty("data").GetProperty("taskChanged");
    }

    private async Task<JsonElement> FirstEventAsync(GraphQLSubscriptionClient connection, string taskId)
    {
        var next = connection.ReceiveAsync();

        for (var attempt = 0; attempt < 25 && !next.IsCompleted; attempt++)
        {
            await UpdateDescriptionAsync(taskId, $"Edited {attempt}");
            await Task.WhenAny(next, Task.Delay(200, TestContext.Current.CancellationToken));
        }

        return Event(await next);
    }

    private static async Task<JsonElement> NextEventOtherThanAsync(GraphQLSubscriptionClient connection, string kind)
    {
        while (true)
        {
            var message = Event(await connection.ReceiveAsync());

            if (message.GetProperty("kind").GetString() != kind)
            {
                return message;
            }
        }
    }

    private async Task<(string ProjectId, string TaskId)> CreateProjectWithTaskAsync()
    {
        var alice = app.CreateClient(Tokens.AcmeOwner);
        var project = await alice.PostGraphQLAsync(
            "mutation ($name: String!) { createProject(input: { name: $name }) { project { id } } }",
            new { name = $"Live board {Guid.NewGuid():N}" });
        var projectId = project.AssertNoErrors().GetProperty("data").GetProperty("createProject")
            .GetProperty("project").GetProperty("id").GetString()!;

        var task = await alice.PostGraphQLAsync(
            "mutation ($projectId: ID!) { createTask(input: { projectId: $projectId, title: \"Ship the board\" }) { task { id } } }",
            new { projectId });
        var taskId = task.AssertNoErrors().GetProperty("data").GetProperty("createTask")
            .GetProperty("task").GetProperty("id").GetString()!;

        return (projectId, taskId);
    }

    private async Task UpdateDescriptionAsync(string taskId, string description)
    {
        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            "mutation ($id: ID!, $description: String) { updateTask(input: { id: $id, description: $description }) { task { id } } }",
            new { id = taskId, description });
        body.AssertNoErrors();
    }

    private async Task ChangeStatusAsync(string taskId, string status)
    {
        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            "mutation ($id: ID!, $status: TaskStatus!) { changeTaskStatus(input: { id: $id, status: $status }) { task { id } } }",
            new { id = taskId, status });
        body.AssertNoErrors();
    }
}
