namespace MagicRemote.Daemon.Power;

/// <summary>Actually powers the machine off. Kept as a seam so PowerService's countdown/cancel logic
/// is unit-testable without ever risking a real shutdown.</summary>
internal interface IPowerManager {
	Task PowerOffAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Shells out to `systemctl poweroff`, the same style the original used for Windows' `shutdown.exe`
/// rather than calling a Win32/D-Bus API directly. systemd is present on effectively every mainstream
/// Linux desktop distro, so this needs no extra dependency (no D-Bus client library) and matches how
/// WebOSCLI.cs already shells out to the `ares-*` tools.
/// </summary>
internal sealed class SystemctlPowerManager : IPowerManager {
	public async Task PowerOffAsync(CancellationToken cancellationToken) {
		using System.Diagnostics.Process process = System.Diagnostics.Process.Start("systemctl", "poweroff")
			?? throw new InvalidOperationException("failed to start systemctl");
		await process.WaitForExitAsync(cancellationToken);
	}
}

/// <summary>Used when the daemon is started with --dry-run, so testing shutdown flows never actually
/// powers off the machine running the tests.</summary>
internal sealed class DryRunPowerManager : IPowerManager {
	private readonly Core.ILogger _logger;

	public DryRunPowerManager(Core.ILogger logger) {
		_logger = logger;
	}

	public Task PowerOffAsync(CancellationToken cancellationToken) {
		_logger.Log("[dry-run] would call `systemctl poweroff` now");
		return Task.CompletedTask;
	}
}
