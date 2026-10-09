using MagicRemote.InputEngine.Native;

namespace MagicRemote.InputEngine;

/// <summary>Virtual mouse reporting relative motion — the Linux equivalent of SendInput's
/// MOUSEEVENTF_MOVE, used for PositionRelative and Wheel protocol messages.</summary>
public sealed class VirtualRelativeMouse : IDisposable {
	private readonly UinputDevice _device;

	public VirtualRelativeMouse() {
		_device = UinputDevice.Open();
		_device.EnableEventType(EvdevCodes.EV_REL);
		_device.EnableRelativeAxis(EvdevCodes.REL_X);
		_device.EnableRelativeAxis(EvdevCodes.REL_Y);
		_device.EnableRelativeAxis(EvdevCodes.REL_WHEEL);
		_device.EnableEventType(EvdevCodes.EV_KEY);
		_device.EnableKey(EvdevCodes.BTN_LEFT);
		_device.EnableKey(EvdevCodes.BTN_RIGHT);
		_device.EnableKey(EvdevCodes.BTN_MIDDLE);
		_device.Create("MagicRemoteService Mouse (relative)");
	}

	public void Move(int dx, int dy) {
		if(dx != 0) {
			_device.Emit(EvdevCodes.EV_REL, EvdevCodes.REL_X, dx);
		}
		if(dy != 0) {
			_device.Emit(EvdevCodes.EV_REL, EvdevCodes.REL_Y, dy);
		}
		_device.Sync();
	}

	public void Scroll(int delta) {
		_device.Emit(EvdevCodes.EV_REL, EvdevCodes.REL_WHEEL, delta);
		_device.Sync();
	}

	public void Click(MouseButton button, bool down) {
		_device.Emit(EvdevCodes.EV_KEY, ButtonCode(button), down ? 1 : 0);
		_device.Sync();
	}

	internal static ushort ButtonCode(MouseButton button) => button switch {
		MouseButton.Left => EvdevCodes.BTN_LEFT,
		MouseButton.Right => EvdevCodes.BTN_RIGHT,
		MouseButton.Middle => EvdevCodes.BTN_MIDDLE,
		_ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
	};

	public void Dispose() => _device.Dispose();
}
