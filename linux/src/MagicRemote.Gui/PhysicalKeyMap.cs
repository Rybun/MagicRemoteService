using Avalonia.Input;
using MagicRemote.InputEngine;

namespace MagicRemote.Gui;

/// <summary>
/// Maps Avalonia's cross-platform PhysicalKey (a physical key-position enum, independent of
/// keyboard layout — see Enum.GetNames(typeof(PhysicalKey)) for the authoritative member list this
/// was built from) to the evdev KEY_* code BindCreatorWindow should record for a new keyboard bind.
/// IME-specific keys (Convert, KanaMode, Lang1-5, NonConvert, IntlRo/Yen) and the rare Select key
/// (evdev code &gt;255, outside VirtualKeyboard's enabled range) are intentionally unmapped.
/// </summary>
internal static class PhysicalKeyMap {
	public static ushort? ToEvdevCode(PhysicalKey key) => key switch {
		PhysicalKey.A => EvdevCodes.KEY_A,
		PhysicalKey.B => EvdevCodes.KEY_B,
		PhysicalKey.C => EvdevCodes.KEY_C,
		PhysicalKey.D => EvdevCodes.KEY_D,
		PhysicalKey.E => EvdevCodes.KEY_E,
		PhysicalKey.F => EvdevCodes.KEY_F,
		PhysicalKey.G => EvdevCodes.KEY_G,
		PhysicalKey.H => EvdevCodes.KEY_H,
		PhysicalKey.I => EvdevCodes.KEY_I,
		PhysicalKey.J => EvdevCodes.KEY_J,
		PhysicalKey.K => EvdevCodes.KEY_K,
		PhysicalKey.L => EvdevCodes.KEY_L,
		PhysicalKey.M => EvdevCodes.KEY_M,
		PhysicalKey.N => EvdevCodes.KEY_N,
		PhysicalKey.O => EvdevCodes.KEY_O,
		PhysicalKey.P => EvdevCodes.KEY_P,
		PhysicalKey.Q => EvdevCodes.KEY_Q,
		PhysicalKey.R => EvdevCodes.KEY_R,
		PhysicalKey.S => EvdevCodes.KEY_S,
		PhysicalKey.T => EvdevCodes.KEY_T,
		PhysicalKey.U => EvdevCodes.KEY_U,
		PhysicalKey.V => EvdevCodes.KEY_V,
		PhysicalKey.W => EvdevCodes.KEY_W,
		PhysicalKey.X => EvdevCodes.KEY_X,
		PhysicalKey.Y => EvdevCodes.KEY_Y,
		PhysicalKey.Z => EvdevCodes.KEY_Z,

		PhysicalKey.Digit0 => EvdevCodes.KEY_0,
		PhysicalKey.Digit1 => EvdevCodes.KEY_1,
		PhysicalKey.Digit2 => EvdevCodes.KEY_2,
		PhysicalKey.Digit3 => EvdevCodes.KEY_3,
		PhysicalKey.Digit4 => EvdevCodes.KEY_4,
		PhysicalKey.Digit5 => EvdevCodes.KEY_5,
		PhysicalKey.Digit6 => EvdevCodes.KEY_6,
		PhysicalKey.Digit7 => EvdevCodes.KEY_7,
		PhysicalKey.Digit8 => EvdevCodes.KEY_8,
		PhysicalKey.Digit9 => EvdevCodes.KEY_9,

		PhysicalKey.F1 => EvdevCodes.KEY_F1,
		PhysicalKey.F2 => EvdevCodes.KEY_F2,
		PhysicalKey.F3 => EvdevCodes.KEY_F3,
		PhysicalKey.F4 => EvdevCodes.KEY_F4,
		PhysicalKey.F5 => EvdevCodes.KEY_F5,
		PhysicalKey.F6 => EvdevCodes.KEY_F6,
		PhysicalKey.F7 => EvdevCodes.KEY_F7,
		PhysicalKey.F8 => EvdevCodes.KEY_F8,
		PhysicalKey.F9 => EvdevCodes.KEY_F9,
		PhysicalKey.F10 => EvdevCodes.KEY_F10,
		PhysicalKey.F11 => EvdevCodes.KEY_F11,
		PhysicalKey.F12 => EvdevCodes.KEY_F12,

		PhysicalKey.Backquote => EvdevCodes.KEY_GRAVE,
		PhysicalKey.Backslash => EvdevCodes.KEY_BACKSLASH,
		PhysicalKey.BracketLeft => EvdevCodes.KEY_LEFTBRACE,
		PhysicalKey.BracketRight => EvdevCodes.KEY_RIGHTBRACE,
		PhysicalKey.Comma => EvdevCodes.KEY_COMMA,
		PhysicalKey.Equal => EvdevCodes.KEY_EQUAL,
		PhysicalKey.Minus => EvdevCodes.KEY_MINUS,
		PhysicalKey.Period => EvdevCodes.KEY_DOT,
		PhysicalKey.Quote => EvdevCodes.KEY_APOSTROPHE,
		PhysicalKey.Semicolon => EvdevCodes.KEY_SEMICOLON,
		PhysicalKey.Slash => EvdevCodes.KEY_SLASH,

		PhysicalKey.AltLeft => EvdevCodes.KEY_LEFTALT,
		PhysicalKey.AltRight => EvdevCodes.KEY_RIGHTALT,
		PhysicalKey.Backspace => EvdevCodes.KEY_BACKSPACE,
		PhysicalKey.CapsLock => EvdevCodes.KEY_CAPSLOCK,
		PhysicalKey.ControlLeft => EvdevCodes.KEY_LEFTCTRL,
		PhysicalKey.ControlRight => EvdevCodes.KEY_RIGHTCTRL,
		PhysicalKey.Enter => EvdevCodes.KEY_ENTER,
		PhysicalKey.MetaLeft => EvdevCodes.KEY_LEFTMETA,
		PhysicalKey.MetaRight => EvdevCodes.KEY_RIGHTMETA,
		PhysicalKey.ShiftLeft => EvdevCodes.KEY_LEFTSHIFT,
		PhysicalKey.ShiftRight => EvdevCodes.KEY_RIGHTSHIFT,
		PhysicalKey.Space => EvdevCodes.KEY_SPACE,
		PhysicalKey.Tab => EvdevCodes.KEY_TAB,
		PhysicalKey.Escape => EvdevCodes.KEY_ESC,

		PhysicalKey.Delete => EvdevCodes.KEY_DELETE,
		PhysicalKey.End => EvdevCodes.KEY_END,
		PhysicalKey.Home => EvdevCodes.KEY_HOME,
		PhysicalKey.Insert => EvdevCodes.KEY_INSERT,
		PhysicalKey.PageDown => EvdevCodes.KEY_PAGEDOWN,
		PhysicalKey.PageUp => EvdevCodes.KEY_PAGEUP,
		PhysicalKey.ArrowDown => EvdevCodes.KEY_DOWN,
		PhysicalKey.ArrowLeft => EvdevCodes.KEY_LEFT,
		PhysicalKey.ArrowRight => EvdevCodes.KEY_RIGHT,
		PhysicalKey.ArrowUp => EvdevCodes.KEY_UP,

		PhysicalKey.AudioVolumeDown => EvdevCodes.KEY_VOLUMEDOWN,
		PhysicalKey.AudioVolumeMute => EvdevCodes.KEY_MUTE,
		PhysicalKey.AudioVolumeUp => EvdevCodes.KEY_VOLUMEUP,
		PhysicalKey.MediaPlayPause => EvdevCodes.KEY_PLAYPAUSE,
		PhysicalKey.MediaStop => EvdevCodes.KEY_STOP,
		PhysicalKey.MediaTrackNext => EvdevCodes.KEY_NEXTSONG,
		PhysicalKey.MediaTrackPrevious => EvdevCodes.KEY_PREVIOUSSONG,
		PhysicalKey.Pause => EvdevCodes.KEY_PAUSE,

		_ => null
	};
}
