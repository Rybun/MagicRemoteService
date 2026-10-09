namespace MagicRemote.Ipc;

/// <summary>
/// The one message this control channel carries today: "run this shell command in a real graphical
/// session." Sent daemon -> GUI because Bind.Command binds (e.g. launching an app, showing a
/// notification) need a DISPLAY/WAYLAND_DISPLAY the daemon itself doesn't have — it runs as a
/// separate system user with no graphical session of its own. Fire-and-forget, matching the
/// original's own Process.Start-and-don't-wait behavior for command binds.
/// </summary>
public sealed record RunCommandMessage(string Shell);
