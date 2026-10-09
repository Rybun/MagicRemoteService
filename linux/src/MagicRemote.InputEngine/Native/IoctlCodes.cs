namespace MagicRemote.InputEngine.Native;

/// <summary>
/// Reimplements the _IO/_IOW ioctl-number-encoding macros from linux/ioctl.h (asm-generic) so the
/// uinput command codes are derived the same way the kernel headers derive them, instead of being
/// pasted as unexplained magic numbers. Values are cross-checked in IoctlCodesTests against a C
/// program compiled with the real &lt;linux/uinput.h&gt; on the target container (see
/// linux/tools/dump-uinput-consts).
/// </summary>
internal static class IoctlCodes {
	private const int NrBits = 8;
	private const int TypeBits = 8;
	private const int SizeBits = 14;

	private const int NrShift = 0;
	private const int TypeShift = NrShift + NrBits;     // 8
	private const int SizeShift = TypeShift + TypeBits; // 16
	private const int DirShift = SizeShift + SizeBits;  // 30

	private const uint DirNone = 0;
	private const uint DirWrite = 1;

	private static uint Encode(uint dir, char type, byte nr, int size) =>
		(dir << DirShift) | ((uint)type << TypeShift) | ((uint)nr << NrShift) | ((uint)size << SizeShift);

	private static uint IO(char type, byte nr) => Encode(DirNone, type, nr, 0);

	private static uint IOW(char type, byte nr, int size) => Encode(DirWrite, type, nr, size);

	private const char UinputIoctlBase = 'U';

	public static readonly uint UI_DEV_CREATE = IO(UinputIoctlBase, 1);
	public static readonly uint UI_DEV_DESTROY = IO(UinputIoctlBase, 2);
	public static readonly uint UI_DEV_SETUP = IOW(UinputIoctlBase, 3, UinputSetup.Size);
	public static readonly uint UI_ABS_SETUP = IOW(UinputIoctlBase, 4, UinputAbsSetup.Size);

	public static readonly uint UI_SET_EVBIT = IOW(UinputIoctlBase, 100, sizeof(int));
	public static readonly uint UI_SET_KEYBIT = IOW(UinputIoctlBase, 101, sizeof(int));
	public static readonly uint UI_SET_RELBIT = IOW(UinputIoctlBase, 102, sizeof(int));
	public static readonly uint UI_SET_ABSBIT = IOW(UinputIoctlBase, 103, sizeof(int));
}
