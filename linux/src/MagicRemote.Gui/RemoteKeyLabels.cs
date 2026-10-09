namespace MagicRemote.Gui;

/// <summary>The remote keys shown in the "Remote" tab grid, in display order — one row per
/// DefaultBindTable entry (see MagicRemote.Core.DefaultBindTable), paired with the Strings.resx key
/// for its label. LabelKey (not the literal text) so the grid stays localized.</summary>
internal static class RemoteKeyLabels {
	public static readonly IReadOnlyList<(ushort Code, string LabelKey)> All = [
		(0x000D, "RemoteKeyOk"),
		(0x0001, "RemoteKeyClick"),
		(0x0002, "RemoteKeyRightClick"),
		(0x0025, "RemoteKeyLeft"),
		(0x0026, "RemoteKeyUp"),
		(0x0027, "RemoteKeyRight"),
		(0x0028, "RemoteKeyDown"),
		(0x01CD, "RemoteKeyBack"),
		(0x0008, "RemoteKeyBackspace"),
		(0x0193, "RemoteKeyRed"),
		(0x0194, "RemoteKeyGreen"),
		(0x0195, "RemoteKeyYellow"),
		(0x0196, "RemoteKeyBlue"),
		(0x019F, "RemoteKeyPlay"),
		(0x0013, "RemoteKeyPause"),
		(0x019D, "RemoteKeyStop"),
		(0x019C, "RemoteKeyPrevious"),
		(0x01A1, "RemoteKeyNext"),
		(0x0030, "RemoteKeyDigit0"),
		(0x0031, "RemoteKeyDigit1"),
		(0x0032, "RemoteKeyDigit2"),
		(0x0033, "RemoteKeyDigit3"),
		(0x0034, "RemoteKeyDigit4"),
		(0x0035, "RemoteKeyDigit5"),
		(0x0036, "RemoteKeyDigit6"),
		(0x0037, "RemoteKeyDigit7"),
		(0x0038, "RemoteKeyDigit8"),
		(0x0039, "RemoteKeyDigit9"),
	];
}
