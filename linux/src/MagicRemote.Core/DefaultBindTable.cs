using MagicRemote.InputEngine;

namespace MagicRemote.Core;

/// <summary>
/// Out-of-the-box remote-key bindings, ported 1:1 from Service.cs's hardcoded dBind entries (see
/// MagicRemoteService/Service.cs around line 158). Keys are the raw webOS/DOM keyCode the TV app
/// sends in a ClientMessage.Key — unchanged from the original since main.js is untouched by this port.
/// User-configurable overrides (Fase 5's BindsEngine) layer on top of this table, they don't replace it.
/// </summary>
public static class DefaultBindTable {
	public static readonly IReadOnlyDictionary<ushort, Bind[]> Entries = new Dictionary<ushort, Bind[]> {
		[0x0001] = [new Bind.Mouse(MouseButton.Left)],
		[0x0002] = [new Bind.Mouse(MouseButton.Right)],
		[0x0008] = [new Bind.Keyboard(EvdevCodes.KEY_BACKSPACE)],
		[0x000D] = [new Bind.Keyboard(EvdevCodes.KEY_ENTER)],
		[0x0021] = [new Bind.Keyboard(EvdevCodes.KEY_LEFTCTRL, EvdevCodes.KEY_C)], // Volume+ -> Ctrl+C
		[0x0022] = [new Bind.Keyboard(EvdevCodes.KEY_LEFTCTRL, EvdevCodes.KEY_V)], // Volume- -> Ctrl+V
		[0x0025] = [new Bind.Keyboard(EvdevCodes.KEY_LEFT)],
		[0x0026] = [new Bind.Keyboard(EvdevCodes.KEY_UP)],
		[0x0027] = [new Bind.Keyboard(EvdevCodes.KEY_RIGHT)],
		[0x0028] = [new Bind.Keyboard(EvdevCodes.KEY_DOWN)],
		[0x0030] = [new Bind.Keyboard(EvdevCodes.KEY_0)],
		[0x0031] = [new Bind.Keyboard(EvdevCodes.KEY_1)],
		[0x0032] = [new Bind.Keyboard(EvdevCodes.KEY_2)],
		[0x0033] = [new Bind.Keyboard(EvdevCodes.KEY_3)],
		[0x0034] = [new Bind.Keyboard(EvdevCodes.KEY_4)],
		[0x0035] = [new Bind.Keyboard(EvdevCodes.KEY_5)],
		[0x0036] = [new Bind.Keyboard(EvdevCodes.KEY_6)],
		[0x0037] = [new Bind.Keyboard(EvdevCodes.KEY_7)],
		[0x0038] = [new Bind.Keyboard(EvdevCodes.KEY_8)],
		[0x0039] = [new Bind.Keyboard(EvdevCodes.KEY_9)],
		[0x0193] = [new Bind.Action(BindAction.Shutdown)],        // red
		[0x0194] = [new Bind.Keyboard(EvdevCodes.KEY_LEFTMETA)],  // green -> "Windows" menu key
		[0x0195] = [new Bind.Mouse(MouseButton.Right)],           // yellow
		[0x0196] = [new Bind.Action(BindAction.ToggleKeyboard)],  // blue
		[0x01CD] = [new Bind.Keyboard(EvdevCodes.KEY_ESC)],       // back/return
		[0x019F] = [new Bind.Keyboard(EvdevCodes.KEY_PLAY)],
		[0x0013] = [new Bind.Keyboard(EvdevCodes.KEY_PAUSE)],
		[0x01A1] = [new Bind.Keyboard(EvdevCodes.KEY_NEXTSONG)],
		[0x019C] = [new Bind.Keyboard(EvdevCodes.KEY_PREVIOUSSONG)],
		[0x019D] = [new Bind.Keyboard(EvdevCodes.KEY_STOP)],
	};
}
