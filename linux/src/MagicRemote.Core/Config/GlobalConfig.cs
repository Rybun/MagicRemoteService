namespace MagicRemote.Core.Config;

/// <summary>Root config document — replaces the original's HKLM/HKCU registry tree
/// (Software\MagicRemoteService\...). See linux/tools plan notes for the field-by-field mapping.</summary>
public sealed record GlobalConfig {
	public int Version { get; init; } = 1;
	public int Port { get; init; } = 41230;
	// Matches Setting.cs/Service.cs's literal defaults for a fresh install (registry key absent):
	// Inactivity=1(true)/TimeoutInactivity=7200000ms, VideoInput=1(true)/TimeoutVideoInput=900000ms.
	public TimeoutSetting Inactivity { get; init; } = new(Enabled: true, TimeoutSeconds: 7200);
	public TimeoutSetting VideoInput { get; init; } = new(Enabled: true, TimeoutSeconds: 900);
	/// <summary>#RRGGBB. Recolors the border/shadow of the app icon shipped to every TV (Home
	/// Dashboard tile and, once registered, the video-input entry) — see MagicRemote.Cli's
	/// IconRecolorer. Not present in the original, whose icon border was a fixed baked-in black.</summary>
	public string IconBorderColor { get; init; } = "#FFFFFF";
	public List<TvProfile> Tvs { get; init; } = [];

	public static GlobalConfig Default() => new();
}

public sealed record TimeoutSetting(bool Enabled, int TimeoutSeconds);

/// <summary>
/// Per-TV settings — replaces Software\MagicRemoteService\Device\&lt;name&gt;. Field defaults below
/// match Setting.cs's literals exactly (LongClick=1500, InputDirect=1/true, Overlay=0/false,
/// Extend=0/false). SendIp/SendPort/Mask/PcMac are the PC's own network identity embedded into the
/// TV app package (see AppExtract's strIP/uiPort/strMask/strMac placeholders) so the TV knows where
/// to reach the PC — the original computed SendIp/Mask/PcMac from the PC's first active non-loopback
/// network interface and SendPort from the currently configured global Port, all dynamically, not
/// from fixed literals; NetworkDefaults/TvProfileViewModel reproduce that (see Fase 4 GUI).
/// </summary>
public sealed record TvProfile {
	public required string Id { get; init; }
	/// <summary>Positional index into whatever monitor list was last published (see MonitorLayout) —
	/// kept for the daemon's own runtime pointer-mapping and as a fallback when DisplayName is unset
	/// (old configs), but NOT stable across a monitor unplug/replug in a different order, which is
	/// exactly why DisplayName exists: RefreshAvailableDisplays self-heals this field from DisplayName
	/// every time the monitor list changes, so it stays pointed at the right physical screen even
	/// after the plug order shuffles its index.</summary>
	public int Display { get; init; }
	/// <summary>The monitor's own stable output/connector name (MonitorLayout.Name — e.g. "HDMI-1",
	/// an XRandR/xdg-output identifier), unlike Display's raw positional index. Null for TVs created
	/// before this field existed or that have never had a display explicitly (re-)selected since.</summary>
	public string? DisplayName { get; init; }
	public string SendIp { get; init; } = string.Empty;
	public int SendPort { get; init; } = 41230;
	public string Mask { get; init; } = "255.255.255.0";
	public string PcMac { get; init; } = string.Empty;
	public int LongClickMs { get; init; } = 1500;
	public bool InputDirect { get; init; } = true;
	public bool Overlay { get; init; }
	public bool Extend { get; init; }
	/// <summary>Which of the 4 fixed HDMI slots (WebOsCli.InputList's "HDMI_1".."HDMI_4") this TV's
	/// app package piggybacks on — a technical requirement (the app needs a real, otherwise-unused
	/// physical input to detect activation via), unrelated to Id, which is this TV's free-text display
	/// name (used as the video-input entry's label and the Home Dashboard tile's description).</summary>
	public string InputId { get; init; } = "HDMI_1";
	/// <summary>ares-cli's own device alias (`ares-setup-device` name) this TV was last installed
	/// through — lets "reinstall"/unregister reuse the same paired device without re-asking.</summary>
	public string? AresDeviceName { get; init; }

	/// <summary>Overrides layered on top of DefaultBindTable, keyed by the same webOS keyCode.</summary>
	public Dictionary<ushort, Bind[]> Binds { get; init; } = [];
}
