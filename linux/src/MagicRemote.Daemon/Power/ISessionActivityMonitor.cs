using MagicRemote.Daemon.Sessions;

namespace MagicRemote.Daemon.Power;

/// <summary>Whether the machine's owner has touched it recently — the Linux equivalent of Win32's
/// GetLastInputInfo, used to abort a pending inactivity shutdown if someone's actually at the keyboard.</summary>
internal interface ISessionActivityMonitor {
	Task<bool> IsIdleAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Backed by `loginctl` (systemd-logind's CLI, present on every systemd desktop) instead of a raw
/// D-Bus client library: finds the active graphical session and reads its IdleHint, which desktop
/// environments update from their own screensaver/idle-detection logic on both X11 and Wayland.
/// </summary>
internal sealed class LoginctlActivityMonitor : ISessionActivityMonitor {
	private readonly IActiveSessionResolver _sessionResolver;

	public LoginctlActivityMonitor(IActiveSessionResolver sessionResolver) {
		_sessionResolver = sessionResolver;
	}

	public async Task<bool> IsIdleAsync(CancellationToken cancellationToken) {
		ActiveSession? session = await _sessionResolver.FindActiveSessionAsync(cancellationToken);
		if(session is null) {
			return false; // no active graphical session to judge idleness by; don't block on unknown state
		}
		string? idleHint = await ProcessRunner.CaptureStdOutAsync("loginctl", $"show-session {session.Value.SessionId} -p IdleHint --value", cancellationToken);
		return idleHint?.Trim() == "yes";
	}
}
