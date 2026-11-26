using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Planboard.Api.Data;

namespace Planboard.Tests;

public static class Database
{
    public static async Task<Guid> TenantIdAsync(this PlanboardApp app, string name)
    {
        await using var connection = new NpgsqlConnection(app.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT id FROM tenants WHERE name = $1", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = name });
        return (Guid)(await command.ExecuteScalarAsync())!;
    }

    public static PooledDbContextFactory<PlanboardDbContext> AppContextFactory(
        this PlanboardApp app,
        Action<NpgsqlConnectionStringBuilder>? configure = null)
    {
        var builder = new NpgsqlConnectionStringBuilder(app.AppConnectionString);
        configure?.Invoke(builder);
        var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
        return new PooledDbContextFactory<PlanboardDbContext>(DatabaseSetup.CreateAppOptions(dataSource));
    }
}
