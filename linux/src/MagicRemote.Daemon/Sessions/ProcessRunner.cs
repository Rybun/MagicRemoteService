using System.Diagnostics;

namespace MagicRemote.Daemon.Sessions;

/// <summary>Shared "run a CLI tool, capture stdout if it exits 0" helper — used everywhere this
/// daemon shells out to a system tool instead of a native/D-Bus API (loginctl, systemctl, xrandr).</summary>
internal static class ProcessRunner {
	public static async Task<string?> CaptureStdOutAsync(string fileName, string arguments, CancellationToken cancellationToken) {
		ProcessStartInfo startInfo = new(fileName, arguments) {
			RedirectStandardOutput = true,
			UseShellExecute = false
		};
		using Process? process = Process.Start(startInfo);
		if(process is null) {
			return null;
		}
		string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
		await process.WaitForExitAsync(cancellationToken);
		return process.ExitCode == 0 ? output : null;
	}
}
