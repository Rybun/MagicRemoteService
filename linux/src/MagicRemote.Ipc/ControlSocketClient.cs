using System.Net.Sockets;
using System.Text.Json;

namespace MagicRemote.Ipc;

/// <summary>
/// GUI side of the control channel. Reconnects automatically (the daemon may start before or after
/// the GUI, and the GUI may be closed and reopened independently of the daemon's lifetime), and
/// raises RunCommandReceived on the calling thread's synchronization context isn't guaranteed —
/// subscribers that touch UI must dispatch back to the UI thread themselves.
/// </summary>
public sealed class ControlSocketClient : IAsyncDisposable {
	private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(3);

	private readonly string _socketPath;
	private CancellationTokenSource? _cts;

	public ControlSocketClient(string socketPath) {
		_socketPath = socketPath;
	}

	public event Action<RunCommandMessage>? RunCommandReceived;

	public void StartConnecting(CancellationToken cancellationToken) {
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_ = ConnectLoopAsync(_cts.Token);
	}

	private async Task ConnectLoopAsync(CancellationToken cancellationToken) {
		while(!cancellationToken.IsCancellationRequested) {
			try {
				using Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
				await socket.ConnectAsync(new UnixDomainSocketEndPoint(_socketPath), cancellationToken);
				await ReadLoopAsync(socket, cancellationToken);
			} catch(OperationCanceledException) {
				break;
			} catch {
				// daemon not up yet, or connection dropped; retry below
			}
			try {
				await Task.Delay(ReconnectDelay, cancellationToken);
			} catch(OperationCanceledException) {
				break;
			}
		}
	}

	private async Task ReadLoopAsync(Socket socket, CancellationToken cancellationToken) {
		await using NetworkStream stream = new(socket, ownsSocket: false);
		using StreamReader reader = new(stream);
		while(!cancellationToken.IsCancellationRequested) {
			string? line = await reader.ReadLineAsync(cancellationToken);
			if(line is null) {
				break; // server closed the connection
			}
			RunCommandMessage? message = JsonSerializer.Deserialize<RunCommandMessage>(line);
			if(message is not null) {
				RunCommandReceived?.Invoke(message);
			}
		}
	}

	public ValueTask DisposeAsync() {
		_cts?.Cancel();
		return ValueTask.CompletedTask;
	}
}
