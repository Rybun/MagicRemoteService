using System.Runtime.InteropServices;
using MagicRemote.InputEngine.Native;

namespace MagicRemote.InputEngine;

/// <summary>
/// Thin, correct wrapper around /dev/uinput: create a virtual input device, declare which event
/// types/codes it supports, bring it up, then emit raw events. This is the Linux equivalent of the
/// original Win32 SendInput — a single kernel-level virtual HID that works identically under X11 and
/// Wayland (and at the login greeter), unlike SendInput it needs no "input desktop" to attach to.
/// </summary>
public sealed class UinputDevice : IDisposable {
	private readonly int _fd;
	private bool _created;
	private bool _disposed;

	private UinputDevice(int fd) {
		_fd = fd;
	}

	public static UinputDevice Open() {
		int fd = LibC.open("/dev/uinput", LibC.O_WRONLY | LibC.O_NONBLOCK);
		if(fd < 0) {
			throw new UinputException("open(/dev/uinput)");
		}
		return new UinputDevice(fd);
	}

	public void EnableEventType(ushort type) => Ioctl(IoctlCodes.UI_SET_EVBIT, type);

	public void EnableKey(ushort code) => Ioctl(IoctlCodes.UI_SET_KEYBIT, code);

	public void EnableRelativeAxis(ushort code) => Ioctl(IoctlCodes.UI_SET_RELBIT, code);

	public void EnableAbsoluteAxis(ushort code, int min, int max, int resolution = 0) {
		Ioctl(IoctlCodes.UI_SET_ABSBIT, code);
		UinputAbsSetup absSetup = new() {
			Code = code,
			AbsInfo = new InputAbsInfo { Minimum = min, Maximum = max, Resolution = resolution }
		};
		if(LibC.ioctl_abssetup(_fd, IoctlCodes.UI_ABS_SETUP, in absSetup) < 0) {
			throw new UinputException("ioctl(UI_ABS_SETUP)");
		}
	}

	/// <summary>Declares the device identity and brings it up. Call after all EnableXxx calls.</summary>
	public unsafe void Create(string name, ushort vendor = 0x0000, ushort product = 0x0000, ushort version = 0x0001) {
		UinputSetup setup = new() {
			Id = new InputId { BusType = 0x06 /* BUS_VIRTUAL */, Vendor = vendor, Product = product, Version = version }
		};
		setup.SetName(name);
		if(LibC.ioctl_setup(_fd, IoctlCodes.UI_DEV_SETUP, in setup) < 0) {
			throw new UinputException("ioctl(UI_DEV_SETUP)");
		}
		if(LibC.ioctl_noarg(_fd, IoctlCodes.UI_DEV_CREATE) < 0) {
			throw new UinputException("ioctl(UI_DEV_CREATE)");
		}
		_created = true;
	}

	public void Emit(ushort type, ushort code, int value) {
		InputEvent ev = new(type, code, value);
		if(LibC.write(_fd, in ev, Marshal.SizeOf<InputEvent>()) < 0) {
			throw new UinputException("write(event)");
		}
	}

	public void Sync() => Emit(EvdevCodes.EV_SYN, EvdevCodes.SYN_REPORT, 0);

	private void Ioctl(uint request, ushort value) {
		if(LibC.ioctl_arg(_fd, request, value) < 0) {
			throw new UinputException($"ioctl(0x{request:x}, {value})");
		}
	}

	public void Dispose() {
		if(_disposed) {
			return;
		}
		_disposed = true;
		if(_created) {
			LibC.ioctl_noarg(_fd, IoctlCodes.UI_DEV_DESTROY);
		}
		LibC.close(_fd);
	}
}

public sealed class UinputException : Exception {
	public UinputException(string operation) : base($"{operation} failed: errno {Marshal.GetLastWin32Error()}") { }
}
