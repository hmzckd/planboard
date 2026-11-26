using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Planboard.Api.Auth;

public sealed class ApiTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    NpgsqlDataSource dataSource)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiToken";

    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string header = Request.Headers.Authorization.ToString();

        if (!header.StartsWith(BearerPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header[BearerPrefix.Length..].Trim();

        await using var command = dataSource.CreateCommand(
            "SELECT user_id, tenant_id, role FROM authenticate_api_token($1)");
        command.Parameters.Add(new NpgsqlParameter { Value = ApiTokenHasher.Hash(token) });

        await using var reader = await command.ExecuteReaderAsync(Context.RequestAborted);

        if (!await reader.ReadAsync(Context.RequestAborted))
        {
            return AuthenticateResult.Fail("Invalid API token.");
        }

        var claims = new[]
        {
            new Claim(PlanboardClaims.UserId, reader.GetGuid(0).ToString()),
            new Claim(PlanboardClaims.TenantId, reader.GetGuid(1).ToString()),
            new Claim(PlanboardClaims.Role, reader.GetString(2))
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
