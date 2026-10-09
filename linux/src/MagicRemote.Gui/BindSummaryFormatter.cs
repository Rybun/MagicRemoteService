using MagicRemote.Core;
using MagicRemote.Gui.Localization;
using MagicRemote.InputEngine;

namespace MagicRemote.Gui;

/// <summary>Turns a resolved Bind[] (one remote key's effective binding — a user override, or the
/// DefaultBindTable entry it falls back to) into a short human string for the remote-binds grid and
/// the bind editor, e.g. "Left click", "ControlLeft + C", "Shut down the PC", "Command: notify-send hi".</summary>
internal static class BindSummaryFormatter {
	public static string Format(IReadOnlyList<Bind> binds, ILocalizationService loc) =>
		binds.Count == 0 ? loc["BindUnassignedLabel"] : string.Join(" + ", binds.Select(b => FormatOne(b, loc)));

	private static string FormatOne(Bind bind, ILocalizationService loc) => bind switch {
		Bind.Mouse { Button: MouseButton.Left } => loc["MouseLeftLabel"],
		Bind.Mouse { Button: MouseButton.Right } => loc["MouseRightLabel"],
		Bind.Mouse { Button: MouseButton.Middle } => loc["MouseMiddleLabel"],
		Bind.Mouse mouse => mouse.Button.ToString(),
		Bind.Keyboard keyboard => string.Join(" + ", keyboard.Codes.Select(EvdevKeyNames.ToDisplayName)),
		Bind.Action { Value: BindAction.Shutdown } => loc["ActionShutdownLabel"],
		Bind.Action { Value: BindAction.ToggleKeyboard } => loc["ActionToggleKeyboardLabel"],
		Bind.Action action => action.Value.ToString(),
		Bind.Command command => $"{loc["CommandPrefixLabel"]} {command.Shell}",
		_ => "?"
	};
}
