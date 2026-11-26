using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Planboard.Api.Data;

namespace Planboard.Tests;

public class PaginationTests(PlanboardApp app)
{
    private const string TasksPage =
        """
        query($id: ID!, $first: Int!, $after: String) {
          project(id: $id) {
            tasks(first: $first, after: $after) {
              nodes { id title }
              pageInfo { hasNextPage endCursor }
            }
          }
        }
        """;

    [Fact]
    public async Task Walking_all_pages_returns_every_project_once()
    {
        var client = app.CreateClient(Tokens.AcmeOwner);
        var seen = new List<string>();
        string? after = null;
        var hasNextPage = true;

        while (hasNextPage)
        {
            var body = await client.PostGraphQLAsync(
                "query($after: String) { projects(first: 3, after: $after) { nodes { name } pageInfo { hasNextPage endCursor } } }",
                new { after });
            var page = body.GetProperty("data").GetProperty("projects");

            seen.AddRange(page.GetProperty("nodes").EnumerateArray().Select(n => n.GetProperty("name").GetString()!));
            hasNextPage = page.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean();
            after = page.GetProperty("pageInfo").GetProperty("endCursor").GetString();
        }

        Assert.Equal(seen.Count, seen.Distinct().Count());
        Assert.Contains("Website redesign", seen);
        Assert.Contains("Onboarding flow", seen);
    }

    [Fact]
    public async Task Page_size_above_fifty_is_rejected()
    {
        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync("{ projects(first: 51) { nodes { name } } }");

        Assert.Equal("HC0051", body.ErrorCode());
    }

    [Fact]
    public async Task A_row_inserted_before_the_cursor_does_not_shift_the_next_page()
    {
        var client = app.CreateClient(Tokens.AcmeOwner);
        var projectId = await app.ProjectIdAsync(Tokens.AcmeOwner, "Security audit");

        var firstTen = await TasksAsync(client, projectId, 10, null);
        var firstPage = await TasksAsync(client, projectId, 5, null);

        var inserted = await InsertTaskBeforeAllAsync("Security audit");
        try
        {
            var secondPage = await TasksAsync(client, projectId, 5, firstPage.EndCursor);

            Assert.Equal(firstTen.Ids.Skip(5), secondPage.Ids);
        }
        finally
        {
            await DeleteTaskAsync(inserted);
        }
    }

    [Fact]
    public async Task Tasks_can_be_filtered_by_status()
    {
        var projectId = await app.ProjectIdAsync(Tokens.AcmeOwner, "Mobile app");

        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            "query($id: ID!) { project(id: $id) { tasks(first: 50, status: DONE) { nodes { status } } } }",
            new { id = projectId });

        var statuses = body.GetProperty("data").GetProperty("project").GetProperty("tasks").GetProperty("nodes")
            .EnumerateArray().Select(t => t.GetProperty("status").GetString()).ToList();

        Assert.NotEmpty(statuses);
        Assert.All(statuses, status => Assert.Equal("DONE", status));
    }

    private static async Task<(List<string> Ids, string? EndCursor)> TasksAsync(
        HttpClient client, string projectId, int first, string? after)
    {
        var body = await client.PostGraphQLAsync(TasksPage, new { id = projectId, first, after });
        var tasks = body.GetProperty("data").GetProperty("project").GetProperty("tasks");

        return (
            tasks.GetProperty("nodes").EnumerateArray().Select(t => t.GetProperty("id").GetString()!).ToList(),
            tasks.GetProperty("pageInfo").GetProperty("endCursor").GetString());
    }

    private async Task<Guid> InsertTaskBeforeAllAsync(string projectName)
    {
        await using var db = app.AppContextFactory().CreateDbContext();
        db.TenantId = await app.TenantIdAsync("Acme");

        var project = await db.Projects.SingleAsync(p => p.Name == projectName);
        var earliest = await db.Tasks.Where(t => t.ProjectId == project.Id).MinAsync(t => t.CreatedAt);

        var task = new ProjectTask
        {
            Id = Guid.CreateVersion7(),
            TenantId = project.TenantId,
            ProjectId = project.Id,
            Title = "Inserted at the front",
            CreatedAt = earliest.AddDays(-1),
            UpdatedAt = earliest.AddDays(-1)
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }

    private async Task DeleteTaskAsync(Guid id)
    {
        await using var db = app.AppContextFactory().CreateDbContext();
        db.TenantId = await app.TenantIdAsync("Acme");
        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync();
    }
}
