using MagicRemote.Ipc;
using Xunit;

namespace MagicRemote.Ipc.Tests;

/// <summary>End-to-end over a real Unix domain socket at a temp path — no mocks needed, this is
/// cheap and deterministic enough to just exercise directly.</summary>
public class ControlSocketTests : IAsyncLifetime {
	private readonly string _socketPath = Path.Combine(Path.GetTempPath(), $"magicremote-ipc-test-{Guid.NewGuid():N}.sock");
	private ControlSocketServer? _server;
	private ControlSocketClient? _client;
	private CancellationTokenSource? _cts;

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() {
		_cts?.Cancel();
		if(_client is not null) {
			await _client.DisposeAsync();
		}
		if(_server is not null) {
			await _server.DisposeAsync();
		}
	}

	[Fact]
	public async Task TryRunCommandAsync_ReturnsFalse_WhenNoClientConnected() {
		_server = new ControlSocketServer(_socketPath);
		_cts = new CancellationTokenSource();
		_server.Start(_cts.Token);

		bool sent = await _server.TryRunCommandAsync("echo hi", CancellationToken.None);

		Assert.False(sent);
		Assert.False(_server.HasConnectedClient);
	}

	[Fact]
	public async Task ClientReceivesRunCommandMessage_SentByServer() {
		_server = new ControlSocketServer(_socketPath);
		_cts = new CancellationTokenSource();
		_server.Start(_cts.Token);

		TaskCompletionSource<RunCommandMessage> received = new();
		_client = new ControlSocketClient(_socketPath);
		_client.RunCommandReceived += msg => received.TrySetResult(msg);
		_client.StartConnecting(_cts.Token);

		await WaitUntilAsync(() => _server.HasConnectedClient);

		bool sent = await _server.TryRunCommandAsync("notify-send hello", CancellationToken.None);
		RunCommandMessage message = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.True(sent);
		Assert.Equal("notify-send hello", message.Shell);
	}

	[Fact]
	public async Task HasConnectedClient_BecomesFalse_AfterClientDisposed() {
		_server = new ControlSocketServer(_socketPath);
		_cts = new CancellationTokenSource();
		_server.Start(_cts.Token);

		_client = new ControlSocketClient(_socketPath);
		_client.StartConnecting(_cts.Token);
		await WaitUntilAsync(() => _server.HasConnectedClient);

		await _client.DisposeAsync();
		_client = null;

		await WaitUntilAsync(() => !_server.HasConnectedClient);
	}

	[Fact]
	public async Task ClientReconnects_AfterServerRestarts() {
		_server = new ControlSocketServer(_socketPath);
		_cts = new CancellationTokenSource();
		_server.Start(_cts.Token);

		_client = new ControlSocketClient(_socketPath);
		_client.StartConnecting(_cts.Token);
		await WaitUntilAsync(() => _server.HasConnectedClient);

		await _server.DisposeAsync();
		_server = new ControlSocketServer(_socketPath);
		_server.Start(_cts.Token);

		// the client's reconnect loop uses a multi-second delay; give it real time to notice and retry
		await WaitUntilAsync(() => _server.HasConnectedClient, timeout: TimeSpan.FromSeconds(10));
	}

	private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null) {
		DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(3));
		while(!condition()) {
			if(DateTime.UtcNow > deadline) {
				throw new TimeoutException("condition was not met in time");
			}
			await Task.Delay(20);
		}
	}
}
