using MagicRemote.InputEngine.Native;

namespace MagicRemote.InputEngine;

/// <summary>
/// Virtual keyboard covering the standard KEY_* range (1-255, i.e. everything below the BTN_* range
/// which starts at 0x100) so any bind — default or user-configured — can be injected without needing
/// to know the exact key set up front. Equivalent to SendInput's KEYEVENTF_KEYDOWN/KEYUP.
/// </summary>
public sealed class VirtualKeyboard : IDisposable {
	private const ushort MaxStandardKeyCode = 255;

	private readonly UinputDevice _device;
	private readonly X11UnicodeTyper _unicodeTyper = new();

	public VirtualKeyboard() {
		_device = UinputDevice.Open();
		_device.EnableEventType(EvdevCodes.EV_KEY);
		for(ushort code = 1; code <= MaxStandardKeyCode; code++) {
			_device.EnableKey(code);
		}
		_device.Create("MagicRemoteService Keyboard");
	}

	public void KeyEvent(ushort code, bool down) {
		_device.Emit(EvdevCodes.EV_KEY, code, down ? 1 : 0);
		_device.Sync();
	}

	/// <summary>
	/// Types one character. Prefers X11UnicodeTyper (correct regardless of active keyboard layout,
	/// handles any Unicode codepoint — see its doc comment for how and what it needs). Falls back to
	/// UnicodeKeyMap's US-layout key-press heuristic only when no X server is reachable at all (a
	/// fully headless daemon with no DISPLAY) — degraded (US-layout-only, ASCII-only) but better than
	/// nothing for that case.
	/// </summary>
	public void TypeUnicode(char character) {
		if(_unicodeTyper.IsAvailable) {
			_unicodeTyper.TypeChar(character);
			return;
		}

		UnicodeKeyMap.Mapping? mapping = UnicodeKeyMap.Map(character);
		if(mapping is not { } m) {
			return;
		}
		if(m.Shift) {
			KeyEvent(EvdevCodes.KEY_LEFTSHIFT, down: true);
		}
		KeyEvent(m.Code, down: true);
		KeyEvent(m.Code, down: false);
		if(m.Shift) {
			KeyEvent(EvdevCodes.KEY_LEFTSHIFT, down: false);
		}
	}

	public void Dispose() {
		_device.Dispose();
		_unicodeTyper.Dispose();
	}
}
