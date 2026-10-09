using MagicRemote.InputEngine.Native;

namespace MagicRemote.InputEngine;

/// <summary>
/// Virtual pointer reporting absolute position over a fixed 0-65535 axis range — the Linux
/// equivalent of SendInput's MOUSEEVENTF_ABSOLUTE|MOUSEEVENTF_VIRTUALDESK, used for the
/// PositionAbsolute protocol message. The caller (BindsEngine, Fase 2) is responsible for mapping
/// the TV's 1920x1080 coordinate space onto the target monitor and then into this 0-65535 range,
/// same as the original Screen.cs/Service.cs did for Win32.
///
/// Known limitation to validate on real hardware: libinput classifies EV_ABS pointers similarly to
/// graphics tablets. Precisely how that interacts with multi-monitor mapping may need refinement in
/// a later phase once tested against an actual multi-monitor layout.
/// </summary>
public sealed class VirtualAbsoluteMouse : IDisposable {
	public const int AxisMax = 65535;

	private readonly UinputDevice _device;

	public VirtualAbsoluteMouse() {
		_device = UinputDevice.Open();
		_device.EnableEventType(EvdevCodes.EV_ABS);
		_device.EnableAbsoluteAxis(EvdevCodes.ABS_X, 0, AxisMax);
		_device.EnableAbsoluteAxis(EvdevCodes.ABS_Y, 0, AxisMax);
		_device.EnableEventType(EvdevCodes.EV_KEY);
		_device.EnableKey(EvdevCodes.BTN_LEFT);
		_device.EnableKey(EvdevCodes.BTN_RIGHT);
		_device.EnableKey(EvdevCodes.BTN_MIDDLE);
		_device.Create("MagicRemoteService Mouse (absolute)");
	}

	public void MoveTo(int x, int y) {
		_device.Emit(EvdevCodes.EV_ABS, EvdevCodes.ABS_X, Math.Clamp(x, 0, AxisMax));
		_device.Emit(EvdevCodes.EV_ABS, EvdevCodes.ABS_Y, Math.Clamp(y, 0, AxisMax));
		_device.Sync();
	}

	public void Click(MouseButton button, bool down) {
		_device.Emit(EvdevCodes.EV_KEY, VirtualRelativeMouse.ButtonCode(button), down ? 1 : 0);
		_device.Sync();
	}

	public void Dispose() => _device.Dispose();
}
