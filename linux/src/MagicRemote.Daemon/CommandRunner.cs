using System.Diagnostics;
using MagicRemote.Core;
using MagicRemote.Ipc;

namespace MagicRemote.Daemon;

/// <summary>
/// Executes Bind.Command binds: forwards to the connected GUI (which runs inside a real graphical
/// session and can launch apps, show notifications, etc.) when one is connected, otherwise runs the
/// command directly in the daemon itself — still useful for headless system commands like
/// `systemctl suspend` that need no display. Mirrors the original's Process.Start-and-forget for
/// command binds (cmd /c strCommand on Windows).
/// </summary>
internal sealed class CommandRunner {
	private readonly ControlSocketServer _controlSocket;
	private readonly ILogger _logger;

	public CommandRunner(ControlSocketServer controlSocket, ILogger logger) {
		_controlSocket = controlSocket;
		_logger = logger;
	}

	public async Task RunAsync(string shell, CancellationToken cancellationToken) {
		if(await _controlSocket.TryRunCommandAsync(shell, cancellationToken)) {
			_logger.Log($"Forwarded command bind to GUI: {shell}");
			return;
		}

		_logger.Log($"No GUI connected; running command bind directly in daemon: {shell}");
		ProcessStartInfo startInfo = new("sh") { UseShellExecute = false };
		startInfo.ArgumentList.Add("-c");
		startInfo.ArgumentList.Add(shell);
		Process.Start(startInfo);
	}
}
