using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

namespace Planboard.Api.Auth;

public sealed class ApiTokenSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override async ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken)
    {
        if (ApiTokenAuthenticator.TokenFromHeader(AuthorizationValue(connectionInitMessage.Payload)) is not { } token)
        {
            return ConnectionStatus.Reject("Send \"authorization\": \"Bearer <token>\" in the connection_init payload.");
        }

        var authenticator = session.Connection.RequestServices.GetRequiredService<ApiTokenAuthenticator>();
        var principal = await authenticator.AuthenticateAsync(token, cancellationToken);

        if (principal is null)
        {
            return ConnectionStatus.Reject("Invalid API token.");
        }

        session.Connection.HttpContext.User = principal;
        return ConnectionStatus.Accept();
    }

    private static string? AuthorizationValue(JsonElement? payload)
    {
        if (payload is not { ValueKind: JsonValueKind.Object } value)
        {
            return null;
        }

        foreach (var property in value.EnumerateObject())
        {
            if (property.Name.Equals("authorization", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            }
        }

        return null;
    }
}
