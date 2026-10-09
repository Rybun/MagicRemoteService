using System.Text.Json.Serialization;
using MagicRemote.InputEngine;

namespace MagicRemote.Core;

/// <summary>What happens when a remote key (identified by its webOS keyCode) is pressed or released.
/// Mirrors the original Bind/BindMouse/BindKeyboard/BindAction/BindCommand hierarchy, but keyboard
/// binds now hold evdev KEY_* codes directly (see MagicRemote.InputEngine.EvdevCodes) instead of a
/// Windows virtual-key + scancode pair — there is no VK layer to preserve on Linux.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Mouse), "mouse")]
[JsonDerivedType(typeof(Keyboard), "keyboard")]
[JsonDerivedType(typeof(Action), "action")]
[JsonDerivedType(typeof(Command), "command")]
public abstract record Bind {
	public sealed record Mouse(MouseButton Button) : Bind;

	/// <summary>Codes are evdev KEY_* values. Chords (e.g. Ctrl+C) are expressed as multiple codes
	/// pressed in order on key-down and released in reverse order on key-up.</summary>
	public sealed record Keyboard(params ushort[] Codes) : Bind;

	public sealed record Action(BindAction Value) : Bind;

	public sealed record Command(string Shell) : Bind;
}

public enum BindAction {
	Shutdown,
	ToggleKeyboard
}
