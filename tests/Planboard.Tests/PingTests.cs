namespace Planboard.Tests;

public class PingTests(PlanboardApp app)
{
    [Fact]
    public async Task Ping_query_returns_pong()
    {
        var client = app.CreateClient();

        var body = await client.PostGraphQLAsync("{ ping }");

        Assert.Equal("pong", body.GetProperty("data").GetProperty("ping").GetString());
    }
}
