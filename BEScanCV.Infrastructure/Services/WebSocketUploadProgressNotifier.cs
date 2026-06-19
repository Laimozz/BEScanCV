using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Infrastructure.Services;

public sealed class WebSocketUploadProgressNotifier(
    ILogger<WebSocketUploadProgressNotifier> logger) : IUploadProgressNotifier
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _connections = new();

    public async Task HandleClientAsync(
        HttpContext httpContext,
        string batchId,
        CancellationToken cancellationToken = default)
    {
        if (!httpContext.WebSockets.IsWebSocketRequest)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid();
        var batchConnections = _connections.GetOrAdd(batchId, _ => new ConcurrentDictionary<Guid, WebSocket>());
        batchConnections[connectionId] = webSocket;

        try
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        finally
        {
            batchConnections.TryRemove(connectionId, out _);
            if (batchConnections.IsEmpty)
            {
                _connections.TryRemove(batchId, out _);
            }

            if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closed",
                    CancellationToken.None);
            }
        }
    }

    public async Task NotifyAsync(
        string batchId,
        object payload,
        CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(batchId, out var connections) || connections.IsEmpty)
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (connectionId, webSocket) in connections)
        {
            if (webSocket.State != WebSocketState.Open)
            {
                connections.TryRemove(connectionId, out _);
                continue;
            }

            try
            {
                await webSocket.SendAsync(
                    segment,
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                connections.TryRemove(connectionId, out _);
                logger.LogWarning(
                    ex,
                    "Removed disconnected upload WebSocket client. BatchId: {BatchId}, ConnectionId: {ConnectionId}",
                    batchId,
                    connectionId);
            }
        }
    }
}
