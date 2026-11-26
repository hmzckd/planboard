using Microsoft.AspNetCore.Mvc.Testing;

namespace Planboard.Tests;

public class SchemaSnapshotTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Schema_matches_the_committed_schema_file()
    {
        var client = factory.CreateClient();
        var actual = Normalize(await client.GetStringAsync("/graphql?sdl", TestContext.Current.CancellationToken));

        var path = Path.Combine(FindRepositoryRoot(), "schema.graphql");

        if (Environment.GetEnvironmentVariable("UPDATE_SCHEMA") == "1")
        {
            await File.WriteAllTextAsync(path, actual, TestContext.Current.CancellationToken);
        }

        Assert.True(File.Exists(path), "schema.graphql is missing. Run the tests once with UPDATE_SCHEMA=1.");
        var expected = Normalize(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));

        Assert.True(
            expected == actual,
            "The GraphQL schema changed. If the change is intended, run the tests with UPDATE_SCHEMA=1 and commit schema.graphql.");
    }

    private static string Normalize(string schema) =>
        schema.Replace("\r\n", "\n").Trim() + "\n";

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Planboard.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Planboard.slnx not found.");
    }
}
