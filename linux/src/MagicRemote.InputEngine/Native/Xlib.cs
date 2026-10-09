using System.Runtime.InteropServices;

namespace MagicRemote.InputEngine.Native;

/// <summary>
/// Minimal P/Invoke surface for typing arbitrary Unicode via a real X11 connection (works through
/// XWayland too). Types are sized for the Linux x86_64 LP64 ABI: KeySym/XID and XTestFakeKeyEvent's
/// delay parameter are all `unsigned long` in Xlib.h, i.e. 8 bytes — mapped to nuint here, not uint.
/// </summary>
internal static partial class Xlib {
	private const string X11 = "libX11.so.6";
	private const string XTst = "libXtst.so.6";

	[LibraryImport(X11, StringMarshalling = StringMarshalling.Utf8)]
	public static partial nint XOpenDisplay(string? displayName);

	[LibraryImport(X11)]
	public static partial int XCloseDisplay(nint display);

	[LibraryImport(X11)]
	public static partial byte XKeysymToKeycode(nint display, nuint keysym);

	[LibraryImport(X11)]
	public static partial int XDisplayKeycodes(nint display, out int minKeycodesReturn, out int maxKeycodesReturn);

	[LibraryImport(X11)]
	public static unsafe partial nuint* XGetKeyboardMapping(nint display, byte firstKeycode, int keycodeCount, out int keysymsPerKeycodeReturn);

	[LibraryImport(X11)]
	public static unsafe partial void XFree(void* data);

	[LibraryImport(X11)]
	public static unsafe partial void XChangeKeyboardMapping(nint display, int firstKeycode, int keysymsPerKeycode, nuint* keysyms, int numCodes);

	[LibraryImport(X11)]
	public static partial int XFlush(nint display);

	[LibraryImport(X11)]
	public static partial int XSync(nint display, [MarshalAs(UnmanagedType.Bool)] bool discard);

	[LibraryImport(XTst)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool XTestFakeKeyEvent(nint display, uint keycode, [MarshalAs(UnmanagedType.Bool)] bool isPress, nuint delay);
}
