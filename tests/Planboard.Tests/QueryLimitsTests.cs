using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;

namespace Planboard.Tests;

public class QueryLimitsTests(PlanboardApp app)
{
    private const string DeepestRealQuery = """
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
        """;

    [Fact]
    public async Task The_deepest_real_query_fits_both_limits_but_not_the_default_cost_limit()
    {
        var client = app.CreateClient(Tokens.AcmeOwner);
        client.DefaultRequestHeaders.Add("GraphQL-Cost", "report");

        var body = await client.PostGraphQLAsync(DeepestRealQuery);

        body.AssertNoErrors();
        var fieldCost = body.GetProperty("extensions").GetProperty("operationCost").GetProperty("fieldCost").GetDouble();
        Assert.InRange(fieldCost, 1_000, 10_000);
    }

    [Fact]
    public async Task A_query_nine_levels_deep_is_rejected_before_any_sql()
    {
        var (body, response) = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLWithResponseAsync(
            "{ projects { nodes { tasks { nodes { project { tasks { nodes { project { name } } } } } } } } }");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = body.GetProperty("errors")[0].GetProperty("extensions");
        Assert.Equal(9, error.GetProperty("detectedExecutionDepth").GetInt32());
        Assert.Equal(0, response.SqlCommandCount());
    }

    [Fact]
    public async Task A_query_within_page_limits_but_too_expensive_is_rejected_before_any_sql()
    {
        var (body, response) = await app.CreateClient(Tokens.AcmeOwner).PostGraphQLWithResponseAsync(
            "{ projects(first: 50) { nodes { tasks(first: 50) { nodes { comments(first: 50) { nodes { author { displayName } } } } } } } }");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("HC0047", body.ErrorCode());
        Assert.Equal(0, response.SqlCommandCount());
    }

    [Fact]
    public async Task Introspection_in_development_is_not_cut_by_the_depth_limit()
    {
        var body = await app.CreateClient().PostGraphQLAsync(
            "{ __schema { types { name fields { type { ofType { ofType { ofType { ofType { ofType { name } } } } } } } } } }");

        body.AssertNoErrors();
    }

    [Fact]
    public async Task Outside_development_introspection_and_the_ide_are_off()
    {
        await using var production = app.Factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var client = production.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.AcmeOwner);

        var introspection = await client.PostGraphQLAsync("{ __schema { queryType { name } } }");
        var typeName = await client.PostGraphQLAsync("{ __typename ping }");

        var developmentIde = await OpenIdeAsync(app.CreateClient());
        var productionIde = await OpenIdeAsync(client);

        Assert.Equal("HC0046", introspection.ErrorCode());
        typeName.AssertNoErrors();
        Assert.Equal(HttpStatusCode.OK, developmentIde.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, productionIde.StatusCode);
    }

    private static Task<HttpResponseMessage> OpenIdeAsync(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/graphql/");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
