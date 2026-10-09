using MagicRemote.Protocol;
using Xunit;

namespace MagicRemote.Protocol.Tests;

public class ClientMessageCodecTests {
	[Fact]
	public void PositionRelative_RoundTrips() {
		AssertRoundTrip(new ClientMessage.PositionRelative(-120, 340));
	}

	[Fact]
	public void PositionAbsolute_RoundTrips() {
		AssertRoundTrip(new ClientMessage.PositionAbsolute(960, 540));
	}

	[Fact]
	public void Wheel_RoundTrips() {
		AssertRoundTrip(new ClientMessage.Wheel(-3));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Visible_RoundTrips(bool visible) {
		AssertRoundTrip(new ClientMessage.Visible(visible));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Key_RoundTrips(bool down) {
		AssertRoundTrip(new ClientMessage.Key(0x0193, down));
	}

	[Fact]
	public void Unicode_RoundTrips() {
		AssertRoundTrip(new ClientMessage.Unicode('a'));
	}

	[Fact]
	public void Shutdown_RoundTrips() {
		AssertRoundTrip(new ClientMessage.Shutdown());
	}

	[Fact]
	public void PositionRelative_MatchesKnownWireBytes() {
		// dx = -1 (0xFFFF LE), dy = 2 (0x0002 LE)
		byte[] encoded = ClientMessageCodec.Encode(new ClientMessage.PositionRelative(-1, 2));
		Assert.Equal(new byte[] { 0x00, 0xFF, 0xFF, 0x02, 0x00 }, encoded);
	}

	[Fact]
	public void PositionAbsolute_MatchesKnownWireBytes() {
		// x = 960 (0x03C0 LE), y = 540 (0x021C LE)
		byte[] encoded = ClientMessageCodec.Encode(new ClientMessage.PositionAbsolute(960, 540));
		Assert.Equal(new byte[] { 0x01, 0xC0, 0x03, 0x1C, 0x02 }, encoded);
	}

	[Fact]
	public void TryDecode_EmptyBuffer_Fails() {
		Assert.False(ClientMessageCodec.TryDecode(ReadOnlySpan<byte>.Empty, out ClientMessage? message));
		Assert.Null(message);
	}

	[Fact]
	public void TryDecode_TruncatedBuffer_Fails() {
		// PositionRelative needs 5 bytes, only give 3
		Assert.False(ClientMessageCodec.TryDecode(new byte[] { 0x00, 0x01, 0x02 }, out _));
	}

	[Fact]
	public void TryDecode_UnknownType_Fails() {
		Assert.False(ClientMessageCodec.TryDecode(new byte[] { 0xFF }, out _));
	}

	[Fact]
	public void ServerMessageCodec_EncodesSingleByte() {
		Assert.Equal(new byte[] { 0x01 }, ServerMessageCodec.Encode(ServerMessageType.ConfirmShutdown));
		Assert.Equal(new byte[] { 0x02 }, ServerMessageCodec.Encode(ServerMessageType.ToggleKeyboard));
	}

	private static void AssertRoundTrip(ClientMessage original) {
		byte[] encoded = ClientMessageCodec.Encode(original);
		Assert.True(ClientMessageCodec.TryDecode(encoded, out ClientMessage? decoded));
		Assert.Equal(original, decoded);
	}
}
