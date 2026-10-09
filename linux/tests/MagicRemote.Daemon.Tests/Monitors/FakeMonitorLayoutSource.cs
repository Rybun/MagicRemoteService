using MagicRemote.Core.Monitors;
using MagicRemote.Daemon.Monitors;

namespace MagicRemote.Daemon.Tests.Monitors;

internal sealed class FakeMonitorLayoutSource : IMonitorLayoutSource {
	public IReadOnlyList<MonitorLayout>? Monitors { get; set; }
	public int CallCount { get; private set; }

	public Task<IReadOnlyList<MonitorLayout>?> ReadAsync(uint uid, CancellationToken cancellationToken) {
		CallCount++;
		return Task.FromResult(Monitors);
	}
}
