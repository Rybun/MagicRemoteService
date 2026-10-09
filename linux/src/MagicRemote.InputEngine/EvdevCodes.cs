namespace MagicRemote.InputEngine;

/// <summary>
/// Subset of linux/input-event-codes.h needed by MagicRemote. Values dumped from a real compile
/// against the kernel headers (see linux/tools/dump-uinput-consts), not hand-transcribed from memory.
/// Public: this is also the vocabulary consumers (default binds, and later the GUI's bind editor)
/// use to describe *which* key a bind should inject.
/// </summary>
public static class EvdevCodes {
	// Event types
	public const ushort EV_SYN = 0x0000;
	public const ushort EV_KEY = 0x0001;
	public const ushort EV_REL = 0x0002;
	public const ushort EV_ABS = 0x0003;

	public const ushort SYN_REPORT = 0x0000;

	// Relative axes
	public const ushort REL_X = 0x0000;
	public const ushort REL_Y = 0x0001;
	public const ushort REL_WHEEL = 0x0008;

	// Absolute axes
	public const ushort ABS_X = 0x0000;
	public const ushort ABS_Y = 0x0001;

	// Mouse buttons
	public const ushort BTN_LEFT = 0x0110;
	public const ushort BTN_RIGHT = 0x0111;
	public const ushort BTN_MIDDLE = 0x0112;

	// Keyboard
	public const ushort KEY_ESC = 0x0001;
	public const ushort KEY_1 = 0x0002;
	public const ushort KEY_2 = 0x0003;
	public const ushort KEY_3 = 0x0004;
	public const ushort KEY_4 = 0x0005;
	public const ushort KEY_5 = 0x0006;
	public const ushort KEY_6 = 0x0007;
	public const ushort KEY_7 = 0x0008;
	public const ushort KEY_8 = 0x0009;
	public const ushort KEY_9 = 0x000a;
	public const ushort KEY_0 = 0x000b;
	public const ushort KEY_MINUS = 0x000c;
	public const ushort KEY_EQUAL = 0x000d;
	public const ushort KEY_BACKSPACE = 0x000e;
	public const ushort KEY_TAB = 0x000f;
	public const ushort KEY_Q = 0x0010;
	public const ushort KEY_W = 0x0011;
	public const ushort KEY_E = 0x0012;
	public const ushort KEY_R = 0x0013;
	public const ushort KEY_T = 0x0014;
	public const ushort KEY_Y = 0x0015;
	public const ushort KEY_U = 0x0016;
	public const ushort KEY_I = 0x0017;
	public const ushort KEY_O = 0x0018;
	public const ushort KEY_P = 0x0019;
	public const ushort KEY_LEFTBRACE = 0x001a;
	public const ushort KEY_RIGHTBRACE = 0x001b;
	public const ushort KEY_ENTER = 0x001c;
	public const ushort KEY_LEFTCTRL = 0x001d;
	public const ushort KEY_A = 0x001e;
	public const ushort KEY_S = 0x001f;
	public const ushort KEY_D = 0x0020;
	public const ushort KEY_F = 0x0021;
	public const ushort KEY_G = 0x0022;
	public const ushort KEY_H = 0x0023;
	public const ushort KEY_J = 0x0024;
	public const ushort KEY_K = 0x0025;
	public const ushort KEY_L = 0x0026;
	public const ushort KEY_SEMICOLON = 0x0027;
	public const ushort KEY_APOSTROPHE = 0x0028;
	public const ushort KEY_GRAVE = 0x0029;
	public const ushort KEY_LEFTSHIFT = 0x002a;
	public const ushort KEY_BACKSLASH = 0x002b;
	public const ushort KEY_Z = 0x002c;
	public const ushort KEY_X = 0x002d;
	public const ushort KEY_C = 0x002e;
	public const ushort KEY_V = 0x002f;
	public const ushort KEY_B = 0x0030;
	public const ushort KEY_N = 0x0031;
	public const ushort KEY_M = 0x0032;
	public const ushort KEY_COMMA = 0x0033;
	public const ushort KEY_DOT = 0x0034;
	public const ushort KEY_SLASH = 0x0035;
	public const ushort KEY_RIGHTSHIFT = 0x0036;
	public const ushort KEY_LEFTALT = 0x0038;
	public const ushort KEY_SPACE = 0x0039;
	public const ushort KEY_CAPSLOCK = 0x003a;
	public const ushort KEY_F1 = 0x003b;
	public const ushort KEY_F2 = 0x003c;
	public const ushort KEY_F3 = 0x003d;
	public const ushort KEY_F4 = 0x003e;
	public const ushort KEY_F5 = 0x003f;
	public const ushort KEY_F6 = 0x0040;
	public const ushort KEY_F7 = 0x0041;
	public const ushort KEY_F8 = 0x0042;
	public const ushort KEY_F9 = 0x0043;
	public const ushort KEY_F10 = 0x0044;
	public const ushort KEY_F11 = 0x0057;
	public const ushort KEY_F12 = 0x0058;
	public const ushort KEY_RIGHTCTRL = 0x0061;
	public const ushort KEY_RIGHTALT = 0x0064;
	public const ushort KEY_HOME = 0x0066;
	public const ushort KEY_UP = 0x0067;
	public const ushort KEY_PAGEUP = 0x0068;
	public const ushort KEY_LEFT = 0x0069;
	public const ushort KEY_RIGHT = 0x006a;
	public const ushort KEY_END = 0x006b;
	public const ushort KEY_DOWN = 0x006c;
	public const ushort KEY_PAGEDOWN = 0x006d;
	public const ushort KEY_INSERT = 0x006e;
	public const ushort KEY_DELETE = 0x006f;
	public const ushort KEY_MUTE = 0x0071;
	public const ushort KEY_VOLUMEDOWN = 0x0072;
	public const ushort KEY_VOLUMEUP = 0x0073;
	public const ushort KEY_LEFTMETA = 0x007d;
	public const ushort KEY_RIGHTMETA = 0x007e;
	public const ushort KEY_STOP = 0x0080;
	public const ushort KEY_PAUSE = 0x0077;
	public const ushort KEY_NEXTSONG = 0x00a3;
	public const ushort KEY_PLAYPAUSE = 0x00a4;
	public const ushort KEY_PREVIOUSSONG = 0x00a5;
	public const ushort KEY_STOPCD = 0x00a6;
	public const ushort KEY_PLAY = 0x00cf;
	public const ushort KEY_PAUSECD = 0x00c9;

	public const ushort KEY_MAX = 0x2ff;
}
