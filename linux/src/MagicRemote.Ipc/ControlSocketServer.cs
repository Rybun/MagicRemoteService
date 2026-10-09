using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MagicRemote.Ipc;

/// <summary>
/// Daemon side of the control channel: a Unix domain socket (not TCP — no need to expose this
/// beyond the local machine, and a filesystem path lets us set normal Unix permissions on it) that
/// the GUI connects to. One-way today: the daemon pushes RunCommandMessage to whichever GUI is
/// currently connected (normally exactly one, the active user's session).
/// </summary>
public sealed class ControlSocketServer : IAsyncDisposable {
	private readonly string _socketPath;
	private readonly List<Socket> _clients = [];
	private readonly object _lock = new();
	private Socket? _listener;
	private CancellationTokenSource? _cts;

	public ControlSocketServer(string socketPath) {
		_socketPath = socketPath;
	}

	public bool HasConnectedClient {
		get {
			lock(_lock) {
				return _clients.Count > 0;
			}
		}
	}

	public void Start(CancellationToken cancellationToken) {
		string? directory = Path.GetDirectoryName(_socketPath);
		if(!string.IsNullOrEmpty(directory)) {
			Directory.CreateDirectory(directory);
		}
		File.Delete(_socketPath);

		_listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
		_listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
		_listener.Listen(backlog: 4);

		// The socket inode is created owned by the daemon's own user (see ControlSocketPaths) —
		// without this, the interactive user's GUI (a different uid) can't open it to connect.
		File.SetUnixFileMode(_socketPath,
			UnixFileMode.UserRead | UnixFileMode.UserWrite |
			UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
			UnixFileMode.OtherRead | UnixFileMode.OtherWrite);

		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_ = AcceptLoopAsync(_cts.Token);
	}

	private async Task AcceptLoopAsync(CancellationToken cancellationToken) {
		while(!cancellationToken.IsCancellationRequested) {
			Socket client;
			try {
				client = await _listener!.AcceptAsync(cancellationToken);
			} catch(OperationCanceledException) {
				break;
			} catch(ObjectDisposedException) {
				break;
			}
			lock(_lock) {
				_clients.Add(client);
			}
			_ = MonitorClientAsync(client, cancellationToken);
		}
	}

	private async Task MonitorClientAsync(Socket client, CancellationToken cancellationToken) {
		byte[] buffer = new byte[1];
		try {
			while(!cancellationToken.IsCancellationRequested) {
				int read = await client.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
				if(read == 0) {
					break; // client closed the connection
				}
			}
		} catch {
			// connection dropped; fall through to cleanup
		} finally {
			lock(_lock) {
				_clients.Remove(client);
			}
			client.Dispose();
		}
	}

	/// <summary>Returns true if the message was handed off to at least one connected GUI.</summary>
	public async Task<bool> TryRunCommandAsync(string shell, CancellationToken cancellationToken) {
		List<Socket> snapshot;
		lock(_lock) {
			snapshot = [.. _clients];
		}
		if(snapshot.Count == 0) {
			return false;
		}

		byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new RunCommandMessage(shell)) + "\n");
		bool sentToAny = false;
		foreach(Socket client in snapshot) {
			try {
				await client.SendAsync(payload, SocketFlags.None, cancellationToken);
				sentToAny = true;
			} catch {
				// that client is gone; MonitorClientAsync will clean it up
			}
		}
		return sentToAny;
	}

	public ValueTask DisposeAsync() {
		_cts?.Cancel();
		_listener?.Dispose();
		lock(_lock) {
			foreach(Socket client in _clients) {
				client.Dispose();
			}
			_clients.Clear();
		}
		File.Delete(_socketPath);
		return ValueTask.CompletedTask;
	}
}
