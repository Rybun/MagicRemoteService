using System.Threading;
using MagicRemote.InputEngine.Native;

namespace MagicRemote.InputEngine;

/// <summary>
/// Types an arbitrary Unicode character over a real X11 connection (works through XWayland, which is
/// what makes this viable on a Wayland desktop too). Neither X11 nor uinput/evdev has a "type this
/// Unicode codepoint" primitive the way Win32's SendInput(KEYEVENTF_UNICODE) does, so three strategies
/// are used, in order:
///
///  1. If the character's keysym already exists somewhere on the active keyboard layout (true for
///     every character reachable via base/Shift/AltGr — e.g. ñ or ç on an ES layout), press that
///     *real* keycode with the matching modifiers held. This is indistinguishable from a physical
///     keypress to whatever consumes it, so it reaches native Wayland clients (Konsole, GTK/Qt apps,
///     etc.) — not just XWayland/X11 ones.
///  2. If it's a Latin letter with a diacritic reachable via a dead key on this layout (á é í ó ú ü
///     etc. — not present as a single keysym on an ES layout, which composes them from a dead-acute/
///     dead-diaeresis key followed by the base vowel), press the dead-key keycode then the base-letter
///     keycode, mirroring a real physical dead-key sequence. The compositor's own XKB compose state
///     does the composition, so — like path 1 — this reaches native Wayland clients too.
///  3. Otherwise, fall back to remapping one unused keycode to the target keysym via
///     XChangeKeyboardMapping and injecting a press/release via XTest (the classic `xdotool type`
///     trick). This only reaches XWayland/X11 clients: the remap lives in XWayland's own X11-side
///     keymap replica, which native Wayland clients never see, since they read keys through the
///     compositor's independent Wayland-seat keymap instead. No Wayland-native fix exists without
///     implementing the zwp_virtual_keyboard_v1 protocol, which is out of scope here — documented, not
///     silently missed.
///
/// Confirmed empirically against this project's real test environment (KDE Plasma 6, Wayland session,
/// ES layout): `xdotool key ntilde` (path 1) typed correctly into Konsole; `xdotool type` for the same
/// character forced onto path 3 silently typed nothing.
/// </summary>
public sealed class X11UnicodeTyper : IDisposable {
	private const uint UnicodeKeysymOffset = 0x01000000;
	private const nuint XK_Shift_L = 0xffe1;
	private const nuint XK_ISO_Level3_Shift = 0xfe03;
	private const nuint XK_Mode_switch = 0xff7e;
	private const nuint XK_dead_acute = 0xfe51;
	private const nuint XK_dead_grave = 0xfe50;
	private const nuint XK_dead_circumflex = 0xfe52;
	private const nuint XK_dead_tilde = 0xfe53;
	private const nuint XK_dead_diaeresis = 0xfe57;

	private readonly nint _display;
	private readonly byte _scratchKeycode;
	private readonly int _keysymsPerKeycode;
	private readonly byte _shiftKeycode;
	private readonly byte _level3Keycode;

	/// <summary>True if a real X11 connection could be opened; when false, TypeChar is a no-op.</summary>
	public bool IsAvailable => _display != 0;

	public unsafe X11UnicodeTyper() {
		_display = Xlib.XOpenDisplay(null);
		if(_display == 0) {
			_scratchKeycode = 0;
			return;
		}
		Xlib.XDisplayKeycodes(_display, out _, out int maxKeycode);
		_scratchKeycode = (byte)maxKeycode;

		// XChangeKeyboardMapping's keysyms-per-keycode argument isn't just "how many entries am I
		// writing" — under XKB (the default on every modern desktop) it redefines the width of the
		// server's entire keyboard mapping table. Passing a width that doesn't match the display's
		// real one (7 is typical: base/shift x two XKB-flattened groups, plus a trailing dup) desyncs
		// the table instead of setting just the scratch keycode's symbol, so it must be queried, not
		// assumed.
		nuint* mapping = Xlib.XGetKeyboardMapping(_display, _scratchKeycode, keycodeCount: 1, out _keysymsPerKeycode);
		Xlib.XFree(mapping);
		if(_keysymsPerKeycode < 1) {
			_keysymsPerKeycode = 1;
		}

		_shiftKeycode = Xlib.XKeysymToKeycode(_display, XK_Shift_L);
		_level3Keycode = Xlib.XKeysymToKeycode(_display, XK_ISO_Level3_Shift);
		if(_level3Keycode == 0) {
			_level3Keycode = Xlib.XKeysymToKeycode(_display, XK_Mode_switch);
		}
	}

	public unsafe void TypeChar(char character) {
		if(!IsAvailable) {
			return;
		}
		nuint keysym = character <= 0xFF ? character : UnicodeKeysymOffset | character;

		if(TryPressKeysym(keysym)) {
			return;
		}

		if(DecomposeToDeadKey(character) is (nuint deadKeysym, char baseChar)
			&& TryPressKeysym(deadKeysym) && TryPressKeysym(baseChar)) {
			return;
		}

		Span<nuint> row = stackalloc nuint[_keysymsPerKeycode];
		row[0] = keysym;
		fixed(nuint* rowPtr = row) {
			Xlib.XChangeKeyboardMapping(_display, _scratchKeycode, _keysymsPerKeycode, rowPtr, numCodes: 1);
		}
		Xlib.XSync(_display, discard: false);
		PressAndRelease(_scratchKeycode);
	}

	/// <summary>
	/// Presses whatever real keycode currently carries this keysym on the active layout, holding
	/// Shift/AltGr as needed. False means the keysym isn't reachable this way at all (no such key on
	/// this layout, or it needs AltGr but this keyboard has none) — caller should try another strategy.
	/// </summary>
	private unsafe bool TryPressKeysym(nuint keysym) {
		// Excludes the scratch keycode itself: after TypeChar's remap fallback has run once,
		// XKeysymToKeycode can find whatever was last written there, which is leftover X11-side-only
		// state, not a real key the compositor's actual (Wayland-seat) keymap recognizes.
		byte keycode = Xlib.XKeysymToKeycode(_display, keysym);
		if(keycode == 0 || keycode == _scratchKeycode) {
			return false;
		}

		nuint* row = Xlib.XGetKeyboardMapping(_display, keycode, keycodeCount: 1, out int rowLength);
		int index = -1;
		for(int i = 0; i < rowLength; i++) {
			if(row[i] == keysym) {
				index = i;
				break;
			}
		}
		Xlib.XFree(row);
		if(index < 0) {
			return false;
		}

		bool shift = index is 1 or 3 or 5;
		bool level3 = index is 4 or 5 or 6;
		if(level3 && _level3Keycode == 0) {
			return false;
		}

		// The compositor's modifier state (needed to interpret the *next* keycode correctly) is only
		// guaranteed settled once the modifier-down request has actually reached and been processed by
		// the server — XFlush alone only guarantees it left our client-side buffer. Pacing each stage
		// with XSync (+ the same ~12ms settle xdotool uses between fake events) avoids a race where the
		// main keycode arrives before the compositor has applied the modifier.
		if(shift) {
			Xlib.XTestFakeKeyEvent(_display, _shiftKeycode, isPress: true, delay: 0);
			Xlib.XSync(_display, discard: false);
			Thread.Sleep(12);
		}
		if(level3) {
			Xlib.XTestFakeKeyEvent(_display, _level3Keycode, isPress: true, delay: 0);
			Xlib.XSync(_display, discard: false);
			Thread.Sleep(12);
		}
		PressAndRelease(keycode);
		if(level3) {
			Xlib.XTestFakeKeyEvent(_display, _level3Keycode, isPress: false, delay: 0);
			Xlib.XSync(_display, discard: false);
			Thread.Sleep(12);
		}
		if(shift) {
			Xlib.XTestFakeKeyEvent(_display, _shiftKeycode, isPress: false, delay: 0);
			Xlib.XSync(_display, discard: false);
		}
		return true;
	}

	private bool TryPressKeysym(char baseChar) => TryPressKeysym((nuint)baseChar);

	/// <summary>
	/// Latin letters with a diacritic that a real ES-layout keyboard produces via a dead key (press
	/// dead-acute/dead-diaeresis/etc., release, then press the base vowel) rather than a single key —
	/// á/é/í/ó/ú are the common Spanish case, the rest cover other Western European text.
	/// </summary>
	private static (nuint deadKeysym, char baseChar)? DecomposeToDeadKey(char character) => character switch {
		'á' => (XK_dead_acute, 'a'), 'é' => (XK_dead_acute, 'e'), 'í' => (XK_dead_acute, 'i'), 'ó' => (XK_dead_acute, 'o'), 'ú' => (XK_dead_acute, 'u'),
		'Á' => (XK_dead_acute, 'A'), 'É' => (XK_dead_acute, 'E'), 'Í' => (XK_dead_acute, 'I'), 'Ó' => (XK_dead_acute, 'O'), 'Ú' => (XK_dead_acute, 'U'),
		'à' => (XK_dead_grave, 'a'), 'è' => (XK_dead_grave, 'e'), 'ì' => (XK_dead_grave, 'i'), 'ò' => (XK_dead_grave, 'o'), 'ù' => (XK_dead_grave, 'u'),
		'À' => (XK_dead_grave, 'A'), 'È' => (XK_dead_grave, 'E'), 'Ì' => (XK_dead_grave, 'I'), 'Ò' => (XK_dead_grave, 'O'), 'Ù' => (XK_dead_grave, 'U'),
		'â' => (XK_dead_circumflex, 'a'), 'ê' => (XK_dead_circumflex, 'e'), 'î' => (XK_dead_circumflex, 'i'), 'ô' => (XK_dead_circumflex, 'o'), 'û' => (XK_dead_circumflex, 'u'),
		'Â' => (XK_dead_circumflex, 'A'), 'Ê' => (XK_dead_circumflex, 'E'), 'Î' => (XK_dead_circumflex, 'I'), 'Ô' => (XK_dead_circumflex, 'O'), 'Û' => (XK_dead_circumflex, 'U'),
		'ã' => (XK_dead_tilde, 'a'), 'õ' => (XK_dead_tilde, 'o'),
		'Ã' => (XK_dead_tilde, 'A'), 'Õ' => (XK_dead_tilde, 'O'),
		'ü' => (XK_dead_diaeresis, 'u'), 'ï' => (XK_dead_diaeresis, 'i'), 'ë' => (XK_dead_diaeresis, 'e'),
		'Ü' => (XK_dead_diaeresis, 'U'), 'Ï' => (XK_dead_diaeresis, 'I'), 'Ë' => (XK_dead_diaeresis, 'E'),
		_ => null
	};

	private void PressAndRelease(byte keycode) {
		Xlib.XTestFakeKeyEvent(_display, keycode, isPress: true, delay: 0);
		Xlib.XSync(_display, discard: false);
		Thread.Sleep(12);
		Xlib.XTestFakeKeyEvent(_display, keycode, isPress: false, delay: 0);
		Xlib.XFlush(_display);
	}

	public void Dispose() {
		if(_display != 0) {
			Xlib.XCloseDisplay(_display);
		}
	}
}
