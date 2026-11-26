using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Planboard.Api.Auth;

public sealed class ApiTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiTokenAuthenticator authenticator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiToken";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (ApiTokenAuthenticator.TokenFromHeader(Request.Headers.Authorization.ToString()) is not { } token)
        {
            return AuthenticateResult.NoResult();
        }

        var principal = await authenticator.AuthenticateAsync(token, Context.RequestAborted);

        return principal is null
            ? AuthenticateResult.Fail("Invalid API token.")
            : AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
