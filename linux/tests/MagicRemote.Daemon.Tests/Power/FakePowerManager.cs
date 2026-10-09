using MagicRemote.Daemon.Power;

namespace MagicRemote.Daemon.Tests.Power;

internal sealed class FakePowerManager : IPowerManager {
	public int PowerOffCallCount { get; private set; }
	public TaskCompletionSource PoweredOff { get; } = new();

	public Task PowerOffAsync(CancellationToken cancellationToken) {
		PowerOffCallCount++;
		PoweredOff.TrySetResult();
		return Task.CompletedTask;
	}
}
