using System.Net;
using System.Net.WebSockets;
using MagicRemote.Core;

namespace MagicRemote.Daemon;

/// <summary>
/// Replaces the original hand-rolled RFC6455 server (raw TCP socket + manual handshake/framing) with
/// HttpListener + System.Net.WebSockets — a fully managed implementation available on Linux, which
/// also gives us WebSocket-level keep-alive for free instead of the original's manual ping timer.
/// </summary>
public sealed class WebSocketServer : IAsyncDisposable {
	private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(30);

	private readonly HttpListener _listener = new();
	private readonly IClientMessageHandler _handler;
	private readonly ILogger _logger;
	private readonly CancellationTokenSource _cts = new();

	public WebSocketServer(int port, IClientMessageHandler handler, ILogger logger) {
		_handler = handler;
		_logger = logger;
		_listener.Prefixes.Add($"http://+:{port}/");
	}

	public async Task RunAsync(CancellationToken cancellationToken) {
		using CancellationTokenRegistration registration = cancellationToken.Register(() => _cts.Cancel());
		_listener.Start();
		_logger.Log($"Listening on {string.Join(", ", _listener.Prefixes)}");

		try {
			while(!_cts.IsCancellationRequested) {
				HttpListenerContext context;
				try {
					context = await _listener.GetContextAsync().WaitAsync(_cts.Token);
				} catch(OperationCanceledException) {
					break;
				}
				_ = AcceptAsync(context);
			}
		} finally {
			_listener.Stop();
		}
	}

	private async Task AcceptAsync(HttpListenerContext context) {
		if(!context.Request.IsWebSocketRequest) {
			context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			context.Response.Close();
			return;
		}

		string remote = context.Request.RemoteEndPoint?.ToString() ?? "unknown";
		try {
			HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(subProtocol: null, KeepAliveInterval);
			_logger.Log($"[{remote}] connected");
			TvSession session = new(wsContext.WebSocket, remote, _handler, _logger);
			await session.RunAsync(_cts.Token);
		} catch(Exception ex) {
			_logger.Log($"[{remote}] handshake failed: {ex.Message}");
		} finally {
			// HttpListener otherwise keeps the underlying TCP connection open for keep-alive reuse even
			// after the WebSocket itself has closed, which stalls clients waiting for the TCP-level FIN.
			context.Response.Close();
		}
	}

	public ValueTask DisposeAsync() {
		_cts.Cancel();
		_listener.Close();
		_cts.Dispose();
		return ValueTask.CompletedTask;
	}
}
