using System.Text.Json;
using MagicRemote.Core.Monitors;

namespace MagicRemote.Daemon.Monitors;

/// <summary>Reads the monitor layout a graphical session has published for itself. Abstracted so
/// MonitorGeometryProvider's caching/fallback/mapping logic is unit-testable without real files.</summary>
internal interface IMonitorLayoutSource {
	Task<IReadOnlyList<MonitorLayout>?> ReadAsync(uint uid, CancellationToken cancellationToken);
}

/// <summary>
/// Reads the per-uid monitors.json a graphical session has published — see MonitorFilePaths for why
/// this lives under /run/magicremoteservice rather than the publishing user's own $XDG_RUNTIME_DIR.
/// </summary>
internal sealed class RuntimeDirMonitorLayoutSource : IMonitorLayoutSource {
	public async Task<IReadOnlyList<MonitorLayout>?> ReadAsync(uint uid, CancellationToken cancellationToken) {
		string path = MonitorFilePaths.ForUid(uid);
		if(!File.Exists(path)) {
			return null;
		}
		try {
			await using FileStream stream = File.OpenRead(path);
			return await JsonSerializer.DeserializeAsync<List<MonitorLayout>>(stream, cancellationToken: cancellationToken);
		} catch(JsonException) {
			return null; // torn/partial write mid-publish; caller falls back until the next refresh
		}
	}
}
