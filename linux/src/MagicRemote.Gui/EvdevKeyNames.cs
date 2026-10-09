using Avalonia.Input;
using MagicRemote.InputEngine;

namespace MagicRemote.Gui;

/// <summary>Short display name for an evdev KEY_* code, for showing a captured keyboard bind back to
/// the user (e.g. "ControlLeft + C"). Built by inverting PhysicalKeyMap rather than hand-duplicating
/// its switch — same source of truth, so any key PhysicalKeyMap can capture also has a name here.</summary>
internal static class EvdevKeyNames {
	private static readonly Dictionary<ushort, string> Names = BuildNames();

	public static string ToDisplayName(ushort code) => Names.TryGetValue(code, out string? name) ? name : $"0x{code:x2}";

	private static Dictionary<ushort, string> BuildNames() {
		Dictionary<ushort, string> map = [];
		foreach(PhysicalKey key in Enum.GetValues<PhysicalKey>()) {
			if(PhysicalKeyMap.ToEvdevCode(key) is ushort code && !map.ContainsKey(code)) {
				map[code] = key.ToString();
			}
		}
		return map;
	}
}
