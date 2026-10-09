namespace MagicRemote.Protocol;

/// <summary>First byte of a binary WebSocket message sent TV -> PC. Mirrors MagicRemoteService.MessageType.</summary>
public enum ClientMessageType : byte {
	PositionRelative = 0x00,
	PositionAbsolute = 0x01,
	Wheel = 0x02,
	Visible = 0x03,
	Key = 0x04,
	Unicode = 0x05,
	Shutdown = 0x06
}

/// <summary>Single-byte message sent PC -> TV. Values intentionally overlap ClientMessageType's byte space
/// since the two enums are only ever interpreted in their own direction.</summary>
public enum ServerMessageType : byte {
	ConfirmShutdown = 0x01,
	ToggleKeyboard = 0x02
}
