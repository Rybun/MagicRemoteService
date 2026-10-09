using System.Runtime.InteropServices;

namespace MagicRemote.Core.Monitors;

/// <summary>
/// Where a graphical session's monitors.json lives. Originally $XDG_RUNTIME_DIR/magicremoteservice/monitors.json
/// (the publishing user's own runtime dir) — broken for the same reason the control socket was: that
/// directory is mode 0700, owned exclusively by the interactive user, so MagicRemote.Daemon (running as
/// its own dedicated system user) can't traverse into it to read the file at all, regardless of the
/// file's own permissions. Confirmed live: /run/user/1000/magicremoteservice/monitors.json was itself
/// 0755, but /run/user/1000 above it was 0700 and blocked the daemon outright.
///
/// Fixed the same way as MagicRemote.Ipc.ControlSocketPaths: publish into /run/magicremoteservice
/// instead (created by the daemon unit's RuntimeDirectory=, mode 0775 so members of the magicremote
/// group — which install.sh's --gui-user adds the interactive user to — can create files there too).
/// One file per uid since multiple graphical sessions could in principle publish concurrently.
/// </summary>
public static class MonitorFilePaths {
	private const string BaseDirectory = "/run/magicremoteservice";

	public static string ForUid(uint uid) => $"{BaseDirectory}/monitors-{uid}.json";

	public static string ForCurrentUser() => ForUid(GetUid());

	// DllImport (not the source-generated LibraryImport) specifically so this project doesn't need
	// AllowUnsafeBlocks just for one trivial P/Invoke.
	[DllImport("libc", EntryPoint = "getuid")]
	private static extern uint GetUid();
}
