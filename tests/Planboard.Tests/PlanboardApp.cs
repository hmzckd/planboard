using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Planboard.Tests.PlanboardApp))]

namespace Planboard.Tests;

public sealed class PlanboardApp : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("planboard")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithResourceMapping(
            new FileInfo(Path.Combine(RepositoryRoot.Path, "docker", "postgres", "init.sql")),
            "/docker-entrypoint-initdb.d/")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public WebApplicationFactory<Program> Factory =>
        _factory ?? throw new InvalidOperationException("The app is not started.");

    public string AppConnectionString => ConnectionString("planboard_app", "planboard_app_dev");

    public string OwnerConnectionString => ConnectionString("planboard_owner", "planboard_owner_dev");

    public HttpClient CreateClient() => Factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:App", AppConnectionString);
            builder.UseSetting("ConnectionStrings:Owner", OwnerConnectionString);
        });

        _ = _factory.Server;
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    private string ConnectionString(string username, string password) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = _postgres.Hostname,
            Port = _postgres.GetMappedPublicPort(5432),
            Database = "planboard",
            Username = username,
            Password = password
        }.ConnectionString;
}
