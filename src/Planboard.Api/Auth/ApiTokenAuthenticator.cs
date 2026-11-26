using System.Security.Claims;
using Npgsql;

namespace Planboard.Api.Auth;

public sealed class ApiTokenAuthenticator(NpgsqlDataSource dataSource)
{
    public const string BearerPrefix = "Bearer ";

    public static string? TokenFromHeader(string? header) =>
        header is not null && header.StartsWith(BearerPrefix, StringComparison.Ordinal)
            ? header[BearerPrefix.Length..].Trim()
            : null;

    public async Task<ClaimsPrincipal?> AuthenticateAsync(string token, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT user_id, tenant_id, role FROM authenticate_api_token($1)");
        command.Parameters.Add(new NpgsqlParameter { Value = ApiTokenHasher.Hash(token) });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var claims = new[]
        {
            new Claim(PlanboardClaims.UserId, reader.GetGuid(0).ToString()),
            new Claim(PlanboardClaims.TenantId, reader.GetGuid(1).ToString()),
            new Claim(PlanboardClaims.Role, reader.GetString(2))
        };

        var identity = new ClaimsIdentity(
            claims, ApiTokenAuthenticationHandler.SchemeName, PlanboardClaims.UserId, PlanboardClaims.Role);
        return new ClaimsPrincipal(identity);
    }
}
