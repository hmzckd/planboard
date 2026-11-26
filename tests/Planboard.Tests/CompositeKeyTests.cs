using Microsoft.EntityFrameworkCore;
using Npgsql;
using Planboard.Api.Data;

namespace Planboard.Tests;

public class CompositeKeyTests(PlanboardApp app)
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_task_cannot_be_assigned_to_a_user_of_another_tenant()
    {
        var acme = await app.TenantIdAsync("Acme");
        var globexUser = await GlobexUserIdAsync();
        await using var db = app.AppContextFactory().CreateDbContext();
        db.TenantId = acme;
        var project = await db.Projects.FirstAsync(Cancel);

        db.Tasks.Add(new ProjectTask
        {
            Id = Guid.CreateVersion7(),
            TenantId = acme,
            ProjectId = project.Id,
            Title = "Assigned across tenants",
            AssigneeId = globexUser,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Cancel));
        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgres.SqlState);
    }

    private async Task<Guid> GlobexUserIdAsync()
    {
        await using var connection = new NpgsqlConnection(app.OwnerConnectionString);
        await connection.OpenAsync(Cancel);
        await using var command = new NpgsqlCommand("SELECT id FROM users WHERE email = 'heidi@globex.test'", connection);
        return (Guid)(await command.ExecuteScalarAsync(Cancel))!;
    }
}
