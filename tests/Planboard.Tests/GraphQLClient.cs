using System.Net.Http.Json;
using System.Text.Json;

namespace Planboard.Tests;

public static class GraphQLClient
{
    public static async Task<JsonElement> PostGraphQLAsync(
        this HttpClient client,
        string query,
        object? variables = null)
    {
        var response = await client.PostAsJsonAsync(
            "/graphql",
            new { query, variables },
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return body;
    }
}
