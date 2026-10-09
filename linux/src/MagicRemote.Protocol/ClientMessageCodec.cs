using System.Buffers.Binary;

namespace MagicRemote.Protocol;

/// <summary>
/// Encodes/decodes the binary application messages exchanged with the (unchanged) WebOS TV app.
/// Wire format is little-endian, matching main.js's DataView.set*(offset, value, true) calls.
/// </summary>
public static class ClientMessageCodec {
	public static bool TryDecode(ReadOnlySpan<byte> data, out ClientMessage? message) {
		message = null;
		if(data.Length == 0) {
			return false;
		}

		switch((ClientMessageType)data[0]) {
			case ClientMessageType.PositionRelative when data.Length >= 5:
				message = new ClientMessage.PositionRelative(
					BinaryPrimitives.ReadInt16LittleEndian(data[1..3]),
					BinaryPrimitives.ReadInt16LittleEndian(data[3..5]));
				return true;

			case ClientMessageType.PositionAbsolute when data.Length >= 5:
				message = new ClientMessage.PositionAbsolute(
					BinaryPrimitives.ReadUInt16LittleEndian(data[1..3]),
					BinaryPrimitives.ReadUInt16LittleEndian(data[3..5]));
				return true;

			case ClientMessageType.Wheel when data.Length >= 3:
				message = new ClientMessage.Wheel(BinaryPrimitives.ReadInt16LittleEndian(data[1..3]));
				return true;

			case ClientMessageType.Visible when data.Length >= 2:
				message = new ClientMessage.Visible(data[1] != 0);
				return true;

			case ClientMessageType.Key when data.Length >= 4:
				message = new ClientMessage.Key(
					BinaryPrimitives.ReadUInt16LittleEndian(data[1..3]),
					(data[3] & 0x01) == 0x01);
				return true;

			case ClientMessageType.Unicode when data.Length >= 3:
				message = new ClientMessage.Unicode((char)BinaryPrimitives.ReadUInt16LittleEndian(data[1..3]));
				return true;

			case ClientMessageType.Shutdown:
				message = new ClientMessage.Shutdown();
				return true;

			default:
				return false;
		}
	}

	public static byte[] Encode(ClientMessage message) => message switch {
		ClientMessage.PositionRelative m => Encode(ClientMessageType.PositionRelative, w => {
			BinaryPrimitives.WriteInt16LittleEndian(w[1..3], m.Dx);
			BinaryPrimitives.WriteInt16LittleEndian(w[3..5], m.Dy);
		}, 5),
		ClientMessage.PositionAbsolute m => Encode(ClientMessageType.PositionAbsolute, w => {
			BinaryPrimitives.WriteUInt16LittleEndian(w[1..3], m.X);
			BinaryPrimitives.WriteUInt16LittleEndian(w[3..5], m.Y);
		}, 5),
		ClientMessage.Wheel m => Encode(ClientMessageType.Wheel, w => {
			BinaryPrimitives.WriteInt16LittleEndian(w[1..3], m.Delta);
		}, 3),
		ClientMessage.Visible m => Encode(ClientMessageType.Visible, w => {
			w[1] = (byte)(m.IsVisible ? 1 : 0);
		}, 2),
		ClientMessage.Key m => Encode(ClientMessageType.Key, w => {
			BinaryPrimitives.WriteUInt16LittleEndian(w[1..3], m.Code);
			w[3] = (byte)(m.Down ? 1 : 0);
		}, 4),
		ClientMessage.Unicode m => Encode(ClientMessageType.Unicode, w => {
			BinaryPrimitives.WriteUInt16LittleEndian(w[1..3], (ushort)m.Character);
		}, 3),
		ClientMessage.Shutdown => [(byte)ClientMessageType.Shutdown],
		_ => throw new ArgumentOutOfRangeException(nameof(message), message, "Unknown message type")
	};

	private delegate void FieldWriter(Span<byte> buffer);

	private static byte[] Encode(ClientMessageType type, FieldWriter writeFields, int length) {
		byte[] buffer = new byte[length];
		buffer[0] = (byte)type;
		writeFields(buffer);
		return buffer;
	}
}

/// <summary>Encodes the single-byte messages the daemon sends back to the TV app.</summary>
public static class ServerMessageCodec {
	public static byte[] Encode(ServerMessageType type) => [(byte)type];
}
