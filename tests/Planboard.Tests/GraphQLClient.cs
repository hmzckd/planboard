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
        var (body, _) = await client.PostGraphQLWithResponseAsync(query, variables);
        return body;
    }

    public static async Task<(JsonElement Body, HttpResponseMessage Response)> PostGraphQLWithResponseAsync(
        this HttpClient client,
        string query,
        object? variables = null)
    {
        var response = await client.PostAsJsonAsync(
            "/graphql",
            new { query, variables },
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return (body, response);
    }

    public static int SqlCommandCount(this HttpResponseMessage response) =>
        int.Parse(response.Headers.GetValues("X-Sql-Command-Count").Single());

    public static string? ErrorCode(this JsonElement body) =>
        body.TryGetProperty("errors", out var errors)
            ? errors[0].GetProperty("extensions").GetProperty("code").GetString()
            : null;
}
