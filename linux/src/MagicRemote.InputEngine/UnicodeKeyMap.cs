namespace MagicRemote.InputEngine;

/// <summary>
/// Maps a typed character to the evdev key (plus whether Shift is needed) that produces it — the
/// replacement for SendInput's KEYEVENTF_UNICODE, which uinput has no equivalent for (it only has
/// physical key codes, not "insert this exact codepoint"). Unlike KEYEVENTF_UNICODE, this is
/// keyboard-layout-dependent: it assumes a US QWERTY layout, since evdev key codes are physical
/// positions and it's the active XKB layout that turns them into characters. Base letters and digits
/// land in the same place on nearly every Latin layout (including es/fr), but punctuation especially
/// does not — typing e.g. '@' or accented letters from the TV's on-screen keyboard may produce the
/// wrong character on a non-US layout. A real IBus CommitText fallback (Fase 2's originally-flagged
/// gap) would fix this properly; this is the pragmatic "make the common case work now" version.
/// </summary>
internal static class UnicodeKeyMap {
	public readonly record struct Mapping(ushort Code, bool Shift);

	public static Mapping? Map(char c) => c switch {
		>= 'a' and <= 'z' => new Mapping(LetterCode(c), Shift: false),
		>= 'A' and <= 'Z' => new Mapping(LetterCode(char.ToLowerInvariant(c)), Shift: true),
		>= '0' and <= '9' => new Mapping(DigitCode(c), Shift: false),
		' ' => new Mapping(EvdevCodes.KEY_SPACE, false),
		'-' => new Mapping(EvdevCodes.KEY_MINUS, false),
		'_' => new Mapping(EvdevCodes.KEY_MINUS, true),
		'=' => new Mapping(EvdevCodes.KEY_EQUAL, false),
		'+' => new Mapping(EvdevCodes.KEY_EQUAL, true),
		',' => new Mapping(EvdevCodes.KEY_COMMA, false),
		'<' => new Mapping(EvdevCodes.KEY_COMMA, true),
		'.' => new Mapping(EvdevCodes.KEY_DOT, false),
		'>' => new Mapping(EvdevCodes.KEY_DOT, true),
		'/' => new Mapping(EvdevCodes.KEY_SLASH, false),
		'?' => new Mapping(EvdevCodes.KEY_SLASH, true),
		';' => new Mapping(EvdevCodes.KEY_SEMICOLON, false),
		':' => new Mapping(EvdevCodes.KEY_SEMICOLON, true),
		'\'' => new Mapping(EvdevCodes.KEY_APOSTROPHE, false),
		'"' => new Mapping(EvdevCodes.KEY_APOSTROPHE, true),
		'`' => new Mapping(EvdevCodes.KEY_GRAVE, false),
		'~' => new Mapping(EvdevCodes.KEY_GRAVE, true),
		'\\' => new Mapping(EvdevCodes.KEY_BACKSLASH, false),
		'|' => new Mapping(EvdevCodes.KEY_BACKSLASH, true),
		'[' => new Mapping(EvdevCodes.KEY_LEFTBRACE, false),
		'{' => new Mapping(EvdevCodes.KEY_LEFTBRACE, true),
		']' => new Mapping(EvdevCodes.KEY_RIGHTBRACE, false),
		'}' => new Mapping(EvdevCodes.KEY_RIGHTBRACE, true),
		'!' => new Mapping(EvdevCodes.KEY_1, true),
		'@' => new Mapping(EvdevCodes.KEY_2, true),
		'#' => new Mapping(EvdevCodes.KEY_3, true),
		'$' => new Mapping(EvdevCodes.KEY_4, true),
		'%' => new Mapping(EvdevCodes.KEY_5, true),
		'^' => new Mapping(EvdevCodes.KEY_6, true),
		'&' => new Mapping(EvdevCodes.KEY_7, true),
		'*' => new Mapping(EvdevCodes.KEY_8, true),
		'(' => new Mapping(EvdevCodes.KEY_9, true),
		')' => new Mapping(EvdevCodes.KEY_0, true),
		_ => null
	};

	private static ushort LetterCode(char lower) => lower switch {
		'a' => EvdevCodes.KEY_A, 'b' => EvdevCodes.KEY_B, 'c' => EvdevCodes.KEY_C, 'd' => EvdevCodes.KEY_D,
		'e' => EvdevCodes.KEY_E, 'f' => EvdevCodes.KEY_F, 'g' => EvdevCodes.KEY_G, 'h' => EvdevCodes.KEY_H,
		'i' => EvdevCodes.KEY_I, 'j' => EvdevCodes.KEY_J, 'k' => EvdevCodes.KEY_K, 'l' => EvdevCodes.KEY_L,
		'm' => EvdevCodes.KEY_M, 'n' => EvdevCodes.KEY_N, 'o' => EvdevCodes.KEY_O, 'p' => EvdevCodes.KEY_P,
		'q' => EvdevCodes.KEY_Q, 'r' => EvdevCodes.KEY_R, 's' => EvdevCodes.KEY_S, 't' => EvdevCodes.KEY_T,
		'u' => EvdevCodes.KEY_U, 'v' => EvdevCodes.KEY_V, 'w' => EvdevCodes.KEY_W, 'x' => EvdevCodes.KEY_X,
		'y' => EvdevCodes.KEY_Y, 'z' => EvdevCodes.KEY_Z,
		_ => throw new ArgumentOutOfRangeException(nameof(lower), lower, null)
	};

	private static ushort DigitCode(char digit) => digit switch {
		'0' => EvdevCodes.KEY_0, '1' => EvdevCodes.KEY_1, '2' => EvdevCodes.KEY_2, '3' => EvdevCodes.KEY_3,
		'4' => EvdevCodes.KEY_4, '5' => EvdevCodes.KEY_5, '6' => EvdevCodes.KEY_6, '7' => EvdevCodes.KEY_7,
		'8' => EvdevCodes.KEY_8, '9' => EvdevCodes.KEY_9,
		_ => throw new ArgumentOutOfRangeException(nameof(digit), digit, null)
	};
}
