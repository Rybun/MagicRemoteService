namespace MagicRemote.Protocol;

/// <summary>A decoded application message received from the TV app over the WebSocket data channel.</summary>
public abstract record ClientMessage {
	public sealed record PositionRelative(short Dx, short Dy) : ClientMessage;

	/// <summary>X/Y are in the TV's fixed 1920x1080 remote-pointer coordinate space, not raw pixels
	/// and not the 0-65535 range used internally by absolute pointer APIs.</summary>
	public sealed record PositionAbsolute(ushort X, ushort Y) : ClientMessage;

	/// <summary>Raw wheel delta as reported by the TV. The x3/sign-flip scale to native wheel units is
	/// an input-engine concern applied at injection time, not part of the wire format.</summary>
	public sealed record Wheel(short Delta) : ClientMessage;

	public sealed record Visible(bool IsVisible) : ClientMessage;

	public sealed record Key(ushort Code, bool Down) : ClientMessage;

	public sealed record Unicode(char Character) : ClientMessage;

	public sealed record Shutdown : ClientMessage;
}
