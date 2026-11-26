namespace Planboard.Tests;

public static class Queries
{
    public static async Task<Dictionary<string, string>> ProjectsOf(this PlanboardApp app, string token)
    {
        var body = await app.CreateClient(token).PostGraphQLAsync("{ projects(first: 50) { nodes { id name } } }");

        return body.GetProperty("data").GetProperty("projects").GetProperty("nodes").EnumerateArray()
            .ToDictionary(p => p.GetProperty("id").GetString()!, p => p.GetProperty("name").GetString()!);
    }

    public static async Task<string> ProjectIdAsync(this PlanboardApp app, string token, string name) =>
        (await app.ProjectsOf(token)).Single(p => p.Value == name).Key;
}
