using System.Runtime.InteropServices;

namespace MagicRemote.Ipc;

/// <summary>
/// Where the control socket lives. Originally this was $XDG_RUNTIME_DIR/magicremoteservice/control.sock
/// (the active user's own runtime dir) — clean in theory, but broken the moment the daemon actually
/// runs as its own dedicated system user (see packaging/systemd's User=magicremote): that directory
/// is mode 0700, owned exclusively by the interactive user, so the daemon has no permission to create
/// anything there at all. Confirmed live on the real install: UnauthorizedAccessException on every
/// single start, crash-looping under systemd's Restart=on-failure. Every earlier test ran the daemon
/// as the interactive user itself via `dotnet run`, which happened to share the same uid as the
/// runtime dir it wrote to and masked this entirely.
///
/// Fixed by moving to a directory the daemon's own user owns instead: /run/magicremoteservice,
/// created via the unit's RuntimeDirectory= (mode 0755 — traversable by everyone, writable only by
/// the daemon). ControlSocketServer chmods the socket file itself to 0666 after Bind() so the
/// interactive user's GUI can connect despite running as a different uid — same trust boundary as
/// before (this channel is local-machine-only, never exposed over the network, and only ever carries
/// the shell string from a user's own already-configured Bind.Command).
/// </summary>
public static class ControlSocketPaths {
	private const string BaseDirectory = "/run/magicremoteservice";

	public static string ForUid(uint uid) => $"{BaseDirectory}/control-{uid}.sock";

	/// <summary>Used by the GUI (always running as the invoking user) and as a dev/test fallback
	/// when no active session could be resolved.</summary>
	public static string ForCurrentUser() => ForUid(GetUid());

	// DllImport (not the source-generated LibraryImport) specifically so this project doesn't need
	// AllowUnsafeBlocks just for one trivial P/Invoke.
	[DllImport("libc", EntryPoint = "getuid")]
	private static extern uint GetUid();
}
