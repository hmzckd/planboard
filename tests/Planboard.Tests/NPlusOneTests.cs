using System.Text.Json;

namespace Planboard.Tests;

public class NPlusOneTests(PlanboardApp app)
{
    [Fact]
    public async Task Tasks_with_assignees_load_in_three_sql_commands()
    {
        var (body, response) = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLWithResponseAsync(
            "{ projects(first: 10) { nodes { name tasks(first: 20) { nodes { title assignee { displayName } } } } } }");

        body.AssertNoErrors();
        var tasks = body.GetProperty("data").GetProperty("projects").GetProperty("nodes").EnumerateArray()
            .SelectMany(p => p.GetProperty("tasks").GetProperty("nodes").EnumerateArray())
            .ToList();

        Assert.True(tasks.Count >= 140, $"Expected the seeded Acme tasks, got {tasks.Count}.");
        Assert.Contains(tasks, t => t.GetProperty("assignee").ValueKind == JsonValueKind.Object);
        Assert.Equal(3, response.SqlCommandCount());
    }

    [Fact]
    public async Task Deep_query_runs_one_sql_command_per_data_loader_batch()
    {
        var (body, response) = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLWithResponseAsync(
            """
            {
              projects(first: 10) {
                nodes {
                  tasks(first: 10) {
                    nodes {
                      title
                      assignee { displayName }
                      labels { name }
                      comments(first: 3) { nodes { body author { displayName } } }
                    }
                  }
                }
              }
            }
            """);

        body.AssertNoErrors();
        Assert.Equal(6, response.SqlCommandCount());
    }

    [Fact]
    public async Task Data_loaders_do_not_cross_tenants()
    {
        var acme = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            """
            {
              projects(first: 10) {
                nodes {
                  tasks(first: 10) {
                    nodes { assignee { email } comments(first: 3) { nodes { author { email } } } }
                  }
                }
              }
            }
            """);

        acme.AssertNoErrors();
        var emails = acme.GetProperty("data").GetProperty("projects").GetProperty("nodes").EnumerateArray()
            .SelectMany(p => p.GetProperty("tasks").GetProperty("nodes").EnumerateArray())
            .SelectMany(t => t.GetProperty("comments").GetProperty("nodes").EnumerateArray()
                .Select(c => c.GetProperty("author"))
                .Append(t.GetProperty("assignee")))
            .Where(u => u.ValueKind == JsonValueKind.Object)
            .Select(u => u.GetProperty("email").GetString()!)
            .Distinct()
            .ToList();

        Assert.NotEmpty(emails);
        Assert.All(emails, email => Assert.EndsWith("@acme.test", email));
    }
}
