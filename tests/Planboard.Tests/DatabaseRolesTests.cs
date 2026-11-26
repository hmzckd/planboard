using Npgsql;

namespace Planboard.Tests;

public class DatabaseRolesTests(PlanboardApp app)
{
    [Fact]
    public async Task App_role_cannot_change_the_schema()
    {
        await using var connection = new NpgsqlConnection(app.AppConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = new NpgsqlCommand("CREATE TABLE hacked (id int)", connection);
        var error = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task App_role_can_read_and_write_rows()
    {
        await using var connection = new NpgsqlConnection(app.AppConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var tenantId = Guid.CreateVersion7();

        await using var setTenant = new NpgsqlCommand("SELECT set_config('app.tenant_id', $1, false)", connection);
        setTenant.Parameters.Add(new NpgsqlParameter { Value = tenantId.ToString() });
        await setTenant.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        await using var insert = new NpgsqlCommand(
            "INSERT INTO tenants (id, name, created_at) VALUES ($1, 'Roles test', now()) RETURNING name",
            connection);
        insert.Parameters.Add(new NpgsqlParameter { Value = tenantId });
        var name = await insert.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Roles test", name);
    }

    [Fact]
    public async Task Tables_are_owned_by_the_owner_role()
    {
        await using var connection = new NpgsqlConnection(app.AppConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = new NpgsqlCommand(
            "SELECT DISTINCT tableowner FROM pg_tables WHERE schemaname = 'public'",
            connection);
        var owner = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        Assert.Equal("planboard_owner", owner);
    }
}
