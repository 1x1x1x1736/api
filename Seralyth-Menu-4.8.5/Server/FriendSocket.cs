using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Seralyth.Server;

public sealed class FriendSocket
{
    private const int MaxMessageBytes = 256 * 1024;

    private readonly WebSocket _socket;
    private readonly FriendState _state;
    private readonly ILogger<FriendSocket> _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public string? UserId { get; private set; }

    public FriendSocket(WebSocket socket, FriendState state, ILogger<FriendSocket> logger)
    {
        _socket = socket;
        _state = state;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string json, CancellationToken token)
    {
        await _sendLock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (_socket.State != WebSocketState.Open)
                return false;

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Socket send failed: {Message}", ex.Message);
            return false;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task RunAsync(string userId, CancellationToken token)
    {
        UserId = userId;
        _state.Register(this, userId);
        _logger.LogInformation("Friend socket opened for {UserId}", userId);

        try
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                while (_socket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    using var message = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None)
                                .ConfigureAwait(false);
                            return;
                        }

                        if (message.Length + result.Count > MaxMessageBytes)
                        {
                            _logger.LogWarning("Discarding oversized socket message from {UserId}", userId);
                            return;
                        }

                        message.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    string text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                    await HandleAsync(userId, text, token).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Socket loop ended for {UserId}: {Message}", userId, ex.Message);
        }
        finally
        {
            _state.Unregister(this, userId);
            _logger.LogInformation("Friend socket closed for {UserId}", userId);
        }
    }

    private async Task HandleAsync(string userId, string text, CancellationToken token)
    {
        JsonObject? message;
        try
        {
            message = JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException)
        {
            return;
        }

        if (message is null)
            return;

        string? command = message["command"]?.GetValue<string>();
        string? target = message["target"]?.GetValue<string>();

        if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(target) ||
            string.Equals(command, target, StringComparison.Ordinal))
            return;

        JsonObject outgoing = new()
        {
            ["command"] = command,
            ["from"] = userId
        };

        switch (command)
        {
            case "invite":
                {
                    string room = message["room"]?.GetValue<string>() ?? string.Empty;
                    _state.SetRoom(userId, room);
                    outgoing["to"] = room;
                    break;
                }

            case "reqinvite":
                break;

            case "preferences":
                outgoing["data"] = Copy(message["preferences"]);
                break;

            case "theme":
                outgoing["data"] = Copy(message["theme"]);
                break;

            case "macro":
                outgoing["data"] = Copy(message["macro"]);
                break;

            case "message":
                outgoing["message"] = Copy(message["message"]);
                outgoing["color"] = message["color"]?.GetValue<string>() ?? "FFFFFF";
                break;

            default:
                return;
        }

        bool delivered = await _state.SendToUserAsync(target, outgoing.ToJsonString(), token).ConfigureAwait(false);
        if (!delivered)
            _logger.LogInformation("Relay {Command} from {From} to {Target} failed: target offline", command, userId, target);
    }

    private static JsonNode? Copy(JsonNode? node)
    {
        try
        {
            return node?.DeepClone();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
