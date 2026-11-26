using System.Text.Json;

namespace Planboard.Tests;

public class NPlusOneTests(PlanboardApp app)
{
    [Fact]
    public async Task Nested_query_runs_one_sql_command_per_parent_row()
    {
        var (body, response) = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLWithResponseAsync(
            "{ projects { name tasks { title assignee { displayName } } } }");

        var projects = body.GetProperty("data").GetProperty("projects").EnumerateArray().ToList();
        var tasks = projects.SelectMany(p => p.GetProperty("tasks").EnumerateArray()).ToList();
        var assignedTasks = tasks.Count(t => t.GetProperty("assignee").ValueKind != JsonValueKind.Null);

        var expected = 1 + projects.Count + assignedTasks;

        Assert.True(tasks.Count >= 700, $"Expected the seeded Acme tasks, got {tasks.Count}.");
        Assert.Equal(expected, response.SqlCommandCount());
    }
}
