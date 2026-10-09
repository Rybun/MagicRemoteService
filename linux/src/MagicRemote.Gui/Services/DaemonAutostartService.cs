using System.Diagnostics;

namespace MagicRemote.Gui.Services;

/// <summary>
/// Toggles boot-time autostart for the daemon's systemd **system** unit (magicremoteservice.service)
/// from the GUI. Querying the current state needs no privileges (`systemctl is-enabled`), but
/// changing it does — enable/disable go through pkexec, which pops a graphical polkit dialog on this
/// session's own screen for the user to authorize, same as install.sh itself uses.
/// </summary>
public static class DaemonAutostartService {
	private const string UnitName = "magicremoteservice.service";

	public static async Task<bool> IsEnabledAsync() {
		try {
			int exitCode = await RunAsync("systemctl", $"is-enabled {UnitName}");
			return exitCode == 0;
		} catch(Exception) {
			return false;
		}
	}

	public static async Task<bool> SetEnabledAsync(bool enabled) {
		string action = enabled ? "enable" : "disable";
		try {
			int exitCode = await RunAsync("pkexec", $"systemctl {action} {UnitName}");
			return exitCode == 0;
		} catch(Exception) {
			return false;
		}
	}

	private static async Task<int> RunAsync(string fileName, string arguments) {
		ProcessStartInfo startInfo = new(fileName, arguments) {
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false
		};
		using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {fileName}");
		await process.WaitForExitAsync();
		return process.ExitCode;
	}
}
