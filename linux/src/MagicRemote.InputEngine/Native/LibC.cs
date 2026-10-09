using System.Runtime.InteropServices;

namespace MagicRemote.InputEngine.Native;

internal static partial class LibC {
	private const string Lib = "libc.so.6";

	public const int O_WRONLY = 0x0001;
	public const int O_NONBLOCK = 0x0800;

	[LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
	public static partial int open(string pathname, int flags);

	[LibraryImport(Lib, SetLastError = true)]
	public static partial int close(int fd);

	[LibraryImport(Lib, SetLastError = true)]
	public static partial nint write(int fd, in InputEvent buf, nint count);

	/// <summary>ioctl variant for commands whose argument is an integer value passed by value
	/// (UI_SET_EVBIT, UI_SET_KEYBIT, UI_SET_RELBIT, UI_SET_ABSBIT).</summary>
	[LibraryImport(Lib, EntryPoint = "ioctl", SetLastError = true)]
	public static partial int ioctl_arg(int fd, nuint request, nint arg);

	/// <summary>ioctl variant for commands with no argument (UI_DEV_CREATE, UI_DEV_DESTROY).</summary>
	[LibraryImport(Lib, EntryPoint = "ioctl", SetLastError = true)]
	public static partial int ioctl_noarg(int fd, nuint request);

	[LibraryImport(Lib, EntryPoint = "ioctl", SetLastError = true)]
	public static partial int ioctl_setup(int fd, nuint request, in UinputSetup arg);

	[LibraryImport(Lib, EntryPoint = "ioctl", SetLastError = true)]
	public static partial int ioctl_abssetup(int fd, nuint request, in UinputAbsSetup arg);
}
