using System.Text;
using System.Text.Json;

namespace Planboard.Tests;

public class NodeTests(PlanboardApp app)
{
    [Fact]
    public async Task Ids_are_global_and_name_their_type()
    {
        var projectId = await app.ProjectIdAsync(Tokens.AcmeOwner, "Mobile app");

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(projectId));

        Assert.StartsWith("Project:", decoded);
    }

    [Fact]
    public async Task Node_returns_an_object_of_the_callers_tenant()
    {
        var projectId = await app.ProjectIdAsync(Tokens.AcmeOwner, "Mobile app");

        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            "query($id: ID!) { node(id: $id) { ... on Project { name } } }",
            new { id = projectId });

        Assert.Equal("Mobile app", body.GetProperty("data").GetProperty("node").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Node_of_another_tenant_is_null()
    {
        var acmeProject = await app.ProjectIdAsync(Tokens.AcmeOwner, "Mobile app");
        var globexProject = await app.ProjectIdAsync(Tokens.GlobexOwner, "Mobile app");

        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            "query($ids: [ID!]!) { nodes(ids: $ids) { id } }",
            new { ids = new[] { acmeProject, globexProject } });

        var nodes = body.GetProperty("data").GetProperty("nodes").EnumerateArray().ToList();
        Assert.Equal(acmeProject, nodes[0].GetProperty("id").GetString());
        Assert.Equal(JsonValueKind.Null, nodes[1].ValueKind);
    }

    [Fact]
    public async Task Tasks_of_another_tenant_are_null_through_node()
    {
        var globexProject = await app.ProjectIdAsync(Tokens.GlobexOwner, "Supplier API");
        var globex = await app.CreateClient(Tokens.GlobexOwner).PostGraphQLAsync(
            "query($id: ID!) { project(id: $id) { tasks(first: 1) { nodes { id } } } }",
            new { id = globexProject });
        var globexTaskId = globex.GetProperty("data").GetProperty("project").GetProperty("tasks")
            .GetProperty("nodes")[0].GetProperty("id").GetString();

        var body = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLAsync(
            "query($id: ID!) { node(id: $id) { id } }",
            new { id = globexTaskId });

        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").GetProperty("node").ValueKind);
    }
}
