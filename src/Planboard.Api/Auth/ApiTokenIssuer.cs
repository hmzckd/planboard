using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Planboard.Api.Data;

namespace Planboard.Api.Auth;

public sealed class ApiTokenIssuer(NpgsqlDataSource dataSource)
{
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("SELECT email_exists($1)");
        command.Parameters.Add(new NpgsqlParameter { Value = email });
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task<string> IssueAsync(PlanboardDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var token = "pb_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        await db.Database.ExecuteSqlAsync(
            $"SELECT issue_api_token({userId}, {ApiTokenHasher.Hash(token)}, {"Invitation token"})",
            cancellationToken);

        return token;
    }
}
