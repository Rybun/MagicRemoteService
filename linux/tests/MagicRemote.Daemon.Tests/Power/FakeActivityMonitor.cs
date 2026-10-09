using MagicRemote.Daemon.Power;

namespace MagicRemote.Daemon.Tests.Power;

internal sealed class FakeActivityMonitor : ISessionActivityMonitor {
	public bool IsIdle { get; set; } = true;

	public Task<bool> IsIdleAsync(CancellationToken cancellationToken) => Task.FromResult(IsIdle);
}
