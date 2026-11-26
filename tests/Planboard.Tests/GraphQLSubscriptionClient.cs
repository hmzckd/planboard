using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Planboard.Tests;

public sealed class GraphQLSubscriptionClient(WebSocket socket) : IAsyncDisposable
{
    public WebSocketCloseStatus? CloseStatus => socket.CloseStatus;

    public static async Task<GraphQLSubscriptionClient> ConnectAsync(PlanboardApp app, string? token)
    {
        var client = app.Factory.Server.CreateWebSocketClient();
        client.SubProtocols.Add("graphql-transport-ws");
        var socket = await client.ConnectAsync(new Uri("ws://localhost/graphql"), TestContext.Current.CancellationToken);

        var connection = new GraphQLSubscriptionClient(socket);
        object payload = token is null ? new { } : new { authorization = $"Bearer {token}" };
        await connection.SendAsync(new { type = "connection_init", payload });
        return connection;
    }

    public Task SubscribeAsync(string id, string query, object? variables = null) =>
        SendAsync(new { id, type = "subscribe", payload = new { query, variables } });

    public async Task<JsonElement?> ReceiveAsync()
    {
        while (true)
        {
            var message = await ReceiveFrameAsync();

            if (message is not { } value || value.GetProperty("type").GetString() != "ping")
            {
                return message;
            }

            await SendAsync(new { type = "pong" });
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (socket.State == WebSocketState.Open)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        socket.Dispose();
    }

    private Task SendAsync(object message) =>
        socket.SendAsync(
            JsonSerializer.SerializeToUtf8Bytes(message),
            WebSocketMessageType.Text,
            endOfMessage: true,
            TestContext.Current.CancellationToken);

    private async Task<JsonElement?> ReceiveFrameAsync()
    {
        var buffer = new byte[8192];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, TestContext.Current.CancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            stream.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                return JsonDocument.Parse(Encoding.UTF8.GetString(stream.ToArray())).RootElement.Clone();
            }
        }
    }
}
