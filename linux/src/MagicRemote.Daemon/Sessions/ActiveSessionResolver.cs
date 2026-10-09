namespace MagicRemote.Daemon.Sessions;

internal readonly record struct ActiveSession(string SessionId, uint Uid);

/// <summary>Finds the graphical session currently in use — analogous to Windows'
/// WTSGetActiveConsoleSessionId, needed both to judge local-activity idleness (PowerService) and to
/// find where that session's monitor-geometry helper publishes its state (MonitorGeometryProvider).</summary>
internal interface IActiveSessionResolver {
	Task<ActiveSession?> FindActiveSessionAsync(CancellationToken cancellationToken);
}

/// <summary>Backed by `loginctl` — see ISessionActivityMonitor's doc comment for why a CLI tool
/// instead of a raw D-Bus client library.</summary>
internal sealed class LoginctlActiveSessionResolver : IActiveSessionResolver {
	public async Task<ActiveSession?> FindActiveSessionAsync(CancellationToken cancellationToken) {
		string? output = await ProcessRunner.CaptureStdOutAsync("loginctl", "list-sessions --no-legend", cancellationToken);
		if(output is null) {
			return null;
		}
		foreach(string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
			string[] columns = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			if(columns.Length == 0) {
				continue;
			}
			string sessionId = columns[0];
			string? active = await ProcessRunner.CaptureStdOutAsync("loginctl", $"show-session {sessionId} -p Active --value", cancellationToken);
			if(active?.Trim() != "yes") {
				continue;
			}
			string? uidText = await ProcessRunner.CaptureStdOutAsync("loginctl", $"show-session {sessionId} -p User --value", cancellationToken);
			if(uint.TryParse(uidText?.Trim(), out uint uid)) {
				return new ActiveSession(sessionId, uid);
			}
		}
		return null;
	}
}
