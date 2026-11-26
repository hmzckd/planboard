using Microsoft.AspNetCore.Mvc.Testing;

namespace Planboard.Tests;

public class PingTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Ping_query_returns_pong()
    {
        var client = factory.CreateClient();

        var body = await client.PostGraphQLAsync("{ ping }");

        Assert.Equal("pong", body.GetProperty("data").GetProperty("ping").GetString());
    }
}
