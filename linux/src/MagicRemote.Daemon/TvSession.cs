using System.Net.WebSockets;
using MagicRemote.Core;
using MagicRemote.Protocol;

namespace MagicRemote.Daemon;

/// <summary>
/// One connected TV app. Owns the receive loop and decodes application messages off the wire,
/// dispatching each to the configured <see cref="IClientMessageHandler"/>.
///
/// Transport-level ping/pong keep-alive is handled by the framework via
/// HttpListenerContext.AcceptWebSocketAsync's keepAliveInterval — unlike the original Windows
/// implementation, which hand-rolled RFC6455 framing and piggy-backed an inactivity signal on a
/// custom ping payload. That inactivity mechanic is being redesigned as an explicit application
/// message in Fase 2 (PowerService) rather than ported byte-for-byte; see plan.
/// </summary>
public sealed class TvSession : ITvSession {
	private const int MaxMessageBytes = 64;

	private readonly WebSocket _socket;
	private readonly IClientMessageHandler _handler;
	private readonly ILogger _logger;

	public string RemoteEndpoint { get; }

	public TvSession(WebSocket socket, string remoteEndpoint, IClientMessageHandler handler, ILogger logger) {
		_socket = socket;
		RemoteEndpoint = remoteEndpoint;
		_handler = handler;
		_logger = logger;
	}

	public async Task RunAsync(CancellationToken cancellationToken) {
		byte[] buffer = new byte[MaxMessageBytes];
		try {
			while(_socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested) {
				WebSocketReceiveResult result = await _socket.ReceiveAsync(buffer, cancellationToken);
				switch(result.MessageType) {
					case WebSocketMessageType.Close:
						await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
						_logger.Log($"[{RemoteEndpoint}] disconnected");
						return;

					case WebSocketMessageType.Binary:
						await DispatchAsync(buffer.AsMemory(0, result.Count), cancellationToken);
						break;

					case WebSocketMessageType.Text:
						_logger.Log($"[{RemoteEndpoint}] unexpected text frame ignored");
						break;
				}
			}
		} catch(OperationCanceledException) {
			// shutting down
		} catch(WebSocketException ex) {
			_logger.Log($"[{RemoteEndpoint}] connection error: {ex.Message}");
		}
	}

	private Task DispatchAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) {
		if(!ClientMessageCodec.TryDecode(data.Span, out ClientMessage? message) || message is null) {
			_logger.Log($"[{RemoteEndpoint}] unprocessed binary message [0x{Convert.ToHexString(data.Span)}]");
			return Task.CompletedTask;
		}
		_logger.Log($"[{RemoteEndpoint}] {message}");
		return _handler.HandleAsync(message, this, cancellationToken);
	}

	public async Task SendAsync(ServerMessageType message, CancellationToken cancellationToken) {
		if(_socket.State != WebSocketState.Open) {
			return;
		}
		byte[] payload = ServerMessageCodec.Encode(message);
		await _socket.SendAsync(payload, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);
	}
}
