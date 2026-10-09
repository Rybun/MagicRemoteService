using Avalonia.Input;
using MagicRemote.Gui;
using MagicRemote.InputEngine;
using Xunit;

namespace MagicRemote.Gui.Tests;

public class PhysicalKeyMapTests {
	[Theory]
	[InlineData(PhysicalKey.A, EvdevCodes.KEY_A)]
	[InlineData(PhysicalKey.Z, EvdevCodes.KEY_Z)]
	[InlineData(PhysicalKey.Digit1, EvdevCodes.KEY_1)]
	[InlineData(PhysicalKey.Digit0, EvdevCodes.KEY_0)]
	[InlineData(PhysicalKey.Enter, EvdevCodes.KEY_ENTER)]
	[InlineData(PhysicalKey.Escape, EvdevCodes.KEY_ESC)]
	[InlineData(PhysicalKey.Space, EvdevCodes.KEY_SPACE)]
	[InlineData(PhysicalKey.Backspace, EvdevCodes.KEY_BACKSPACE)]
	[InlineData(PhysicalKey.ArrowLeft, EvdevCodes.KEY_LEFT)]
	[InlineData(PhysicalKey.ArrowUp, EvdevCodes.KEY_UP)]
	[InlineData(PhysicalKey.ControlLeft, EvdevCodes.KEY_LEFTCTRL)]
	[InlineData(PhysicalKey.ShiftRight, EvdevCodes.KEY_RIGHTSHIFT)]
	[InlineData(PhysicalKey.MetaLeft, EvdevCodes.KEY_LEFTMETA)]
	[InlineData(PhysicalKey.F1, EvdevCodes.KEY_F1)]
	[InlineData(PhysicalKey.F12, EvdevCodes.KEY_F12)]
	[InlineData(PhysicalKey.AudioVolumeUp, EvdevCodes.KEY_VOLUMEUP)]
	[InlineData(PhysicalKey.MediaPlayPause, EvdevCodes.KEY_PLAYPAUSE)]
	public void ToEvdevCode_MapsKnownKeys(PhysicalKey key, ushort expected) {
		Assert.Equal(expected, PhysicalKeyMap.ToEvdevCode(key));
	}

	[Theory]
	[InlineData(PhysicalKey.None)]
	[InlineData(PhysicalKey.Convert)]
	[InlineData(PhysicalKey.KanaMode)]
	[InlineData(PhysicalKey.Lang1)]
	[InlineData(PhysicalKey.Select)] // evdev code 0x161 is outside VirtualKeyboard's enabled range
	public void ToEvdevCode_ReturnsNull_ForUnmappedKeys(PhysicalKey key) {
		Assert.Null(PhysicalKeyMap.ToEvdevCode(key));
	}

	[Fact]
	public void ToEvdevCode_NeverMapsToCodeOutsideVirtualKeyboardRange() {
		foreach(PhysicalKey key in Enum.GetValues<PhysicalKey>()) {
			ushort? code = PhysicalKeyMap.ToEvdevCode(key);
			if(code is not null) {
				Assert.InRange(code.Value, (ushort)1, (ushort)255);
			}
		}
	}
}
