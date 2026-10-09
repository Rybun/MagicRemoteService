using MagicRemote.Core;
using MagicRemote.Ipc;
using Xunit;

namespace MagicRemote.Daemon.Tests;

public class CommandRunnerTests : IAsyncLifetime {
	private readonly string _socketPath = Path.Combine(Path.GetTempPath(), $"magicremote-cmdrunner-test-{Guid.NewGuid():N}.sock");
	private readonly string _markerPath = Path.Combine(Path.GetTempPath(), $"magicremote-cmdrunner-marker-{Guid.NewGuid():N}");
	private ControlSocketServer? _server;

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() {
		if(_server is not null) {
			await _server.DisposeAsync();
		}
		File.Delete(_markerPath);
	}

	private sealed class NoOpLogger : ILogger {
		public void Log(string message) { }
	}

	[Fact]
	public async Task RunAsync_RunsLocally_WhenNoGuiConnected() {
		_server = new ControlSocketServer(_socketPath);
		using CancellationTokenSource cts = new();
		_server.Start(cts.Token);
		CommandRunner runner = new(_server, new NoOpLogger());

		await runner.RunAsync($"echo hi > {_markerPath}", CancellationToken.None);
		await WaitUntilAsync(() => File.Exists(_markerPath));

		Assert.True(File.Exists(_markerPath));
	}

	[Fact]
	public async Task RunAsync_ForwardsToGui_InsteadOfRunningLocally_WhenConnected() {
		_server = new ControlSocketServer(_socketPath);
		using CancellationTokenSource cts = new();
		_server.Start(cts.Token);

		TaskCompletionSource<RunCommandMessage> received = new();
		await using ControlSocketClient client = new(_socketPath);
		client.RunCommandReceived += msg => received.TrySetResult(msg);
		client.StartConnecting(cts.Token);
		await WaitUntilAsync(() => _server.HasConnectedClient);

		CommandRunner runner = new(_server, new NoOpLogger());
		await runner.RunAsync($"echo hi > {_markerPath}", CancellationToken.None);
		RunCommandMessage message = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal($"echo hi > {_markerPath}", message.Shell);
		Assert.False(File.Exists(_markerPath)); // ran nowhere locally — forwarding, not executing
	}

	private static async Task WaitUntilAsync(Func<bool> condition) {
		DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
		while(!condition()) {
			if(DateTime.UtcNow > deadline) {
				throw new TimeoutException("condition was not met in time");
			}
			await Task.Delay(20);
		}
	}
}
