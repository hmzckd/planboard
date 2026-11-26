using Microsoft.EntityFrameworkCore;
using Npgsql;
using Planboard.Api.Data;

namespace Planboard.Tests;

public class RowLevelSecurityTests(PlanboardApp app)
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Without_the_application_filter_the_database_still_returns_only_the_tenants_rows()
    {
        var acme = await app.TenantIdAsync("Acme");
        await using var db = app.AppContextFactory().CreateDbContext();
        db.TenantId = acme;

        var tenantsSeen = await db.Projects.IgnoreQueryFilters().Select(p => p.TenantId).Distinct().ToListAsync(Cancel);

        Assert.Equal([acme], tenantsSeen);
    }

    [Fact]
    public async Task Without_a_tenant_the_database_returns_no_rows_even_without_the_filter()
    {
        await using var db = app.AppContextFactory().CreateDbContext();

        Assert.Equal(0, await db.Projects.IgnoreQueryFilters().CountAsync(Cancel));
        Assert.Equal(0, await db.Users.IgnoreQueryFilters().CountAsync(Cancel));
        Assert.Equal(0, await db.Tenants.IgnoreQueryFilters().CountAsync(Cancel));
    }

    [Fact]
    public async Task Writing_a_row_for_another_tenant_is_rejected()
    {
        var acme = await app.TenantIdAsync("Acme");
        var globex = await app.TenantIdAsync("Globex");
        await using var db = app.AppContextFactory().CreateDbContext();
        db.TenantId = acme;

        db.Projects.Add(new Project
        {
            Id = Guid.CreateVersion7(),
            TenantId = globex,
            Name = "Planted by Acme",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Cancel));
        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, postgres.SqlState);
    }

    [Fact]
    public async Task A_reused_connection_does_not_keep_the_previous_tenant()
    {
        var acme = await app.TenantIdAsync("Acme");
        var factory = app.AppContextFactory(builder =>
        {
            builder.MaxPoolSize = 1;
            builder.NoResetOnClose = true;
        });

        await using (var acmeDb = factory.CreateDbContext())
        {
            acmeDb.TenantId = acme;
            Assert.NotEqual(0, await acmeDb.Projects.IgnoreQueryFilters().CountAsync(Cancel));
        }

        await using var anonymousDb = factory.CreateDbContext();

        Assert.Equal(0, await anonymousDb.Projects.IgnoreQueryFilters().CountAsync(Cancel));
    }

    [Fact]
    public async Task App_role_cannot_read_api_tokens_directly()
    {
        await using var connection = new NpgsqlConnection(app.AppConnectionString);
        await connection.OpenAsync(Cancel);
        await using var command = new NpgsqlCommand("SELECT count(*) FROM api_tokens", connection);

        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteScalarAsync(Cancel));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }
}
