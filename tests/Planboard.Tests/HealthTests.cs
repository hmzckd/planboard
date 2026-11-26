using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Planboard.Tests;

public class HealthTests(PlanboardApp app)
{
    [Fact]
    public async Task Health_reports_the_database_as_up()
    {
        var client = app.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("up", body.GetProperty("database").GetString());
    }
}
