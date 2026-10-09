using MagicRemote.Daemon.Sessions;

namespace MagicRemote.Daemon.Tests.Monitors;

internal sealed class FakeActiveSessionResolver : IActiveSessionResolver {
	public ActiveSession? Session { get; set; } = new ActiveSession("1", 1000);
	public int CallCount { get; private set; }

	public Task<ActiveSession?> FindActiveSessionAsync(CancellationToken cancellationToken) {
		CallCount++;
		return Task.FromResult(Session);
	}
}
