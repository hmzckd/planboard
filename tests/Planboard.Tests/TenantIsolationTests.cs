using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Planboard.Api.Data;

namespace Planboard.Tests;

public class TenantIsolationTests(PlanboardApp app)
{
    [Fact]
    public async Task Projects_returns_only_the_callers_tenant()
    {
        var acme = await app.ProjectsOf(Tokens.AcmeOwner);
        var globex = await app.ProjectsOf(Tokens.GlobexOwner);

        Assert.Contains("Website redesign", acme.Values);
        Assert.DoesNotContain("Warehouse robots", acme.Values);
        Assert.Contains("Warehouse robots", globex.Values);
        Assert.Empty(acme.Keys.Intersect(globex.Keys));
    }

    [Fact]
    public async Task Project_by_id_of_another_tenant_is_null()
    {
        var globexProjectId = (await app.ProjectsOf(Tokens.GlobexOwner)).Keys.First();

        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            "query($id: ID!) { project(id: $id) { name } }",
            new { id = globexProjectId });

        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").GetProperty("project").ValueKind);
    }

    [Fact]
    public async Task Projects_without_a_token_is_not_authenticated()
    {
        var body = await app.CreateClient().PostGraphQLAsync("{ projects { nodes { name } } }");

        Assert.Equal("AUTH_NOT_AUTHENTICATED", body.ErrorCode());
    }

    [Fact]
    public async Task A_context_without_a_tenant_sees_no_rows()
    {
        var factory = app.Factory.Services.GetRequiredService<PooledDbContextFactory<PlanboardDbContext>>();
        await using var db = factory.CreateDbContext();

        Assert.Null(db.TenantId);
        Assert.Equal(0, await db.Projects.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await db.Users.CountAsync(TestContext.Current.CancellationToken));
    }
}
