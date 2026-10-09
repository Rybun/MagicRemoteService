using System.Runtime.InteropServices;

namespace MagicRemote.InputEngine.Native;

/// <summary>
/// Binary layouts mirror linux/uinput.h and linux/input.h on a 64-bit kernel exactly (field offsets
/// verified against a real compile — see IoctlCodesTests). Explicit layout is used throughout instead
/// of trusting Sequential's auto-padding to match C, since a silent mismatch here corrupts every
/// ioctl/write call to /dev/uinput.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 8)]
internal struct InputId {
	[FieldOffset(0)] public ushort BusType;
	[FieldOffset(2)] public ushort Vendor;
	[FieldOffset(4)] public ushort Product;
	[FieldOffset(6)] public ushort Version;
}

[StructLayout(LayoutKind.Explicit, Size = Size)]
internal unsafe struct UinputSetup {
	public const int Size = 92;
	public const int MaxNameSize = 80;

	[FieldOffset(0)] public InputId Id;
	[FieldOffset(8)] public fixed byte Name[MaxNameSize];
	[FieldOffset(88)] public uint FfEffectsMax;

	public void SetName(string name) {
		Span<byte> destination = MemoryMarshal.CreateSpan(ref Name[0], MaxNameSize);
		destination.Clear();
		int written = System.Text.Encoding.UTF8.GetBytes(name.AsSpan(0, Math.Min(name.Length, MaxNameSize - 1)), destination);
		destination[written] = 0;
	}
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal struct InputAbsInfo {
	[FieldOffset(0)] public int Value;
	[FieldOffset(4)] public int Minimum;
	[FieldOffset(8)] public int Maximum;
	[FieldOffset(12)] public int Fuzz;
	[FieldOffset(16)] public int Flat;
	[FieldOffset(20)] public int Resolution;
}

[StructLayout(LayoutKind.Explicit, Size = Size)]
internal struct UinputAbsSetup {
	public const int Size = 28;

	[FieldOffset(0)] public ushort Code;
	[FieldOffset(4)] public InputAbsInfo AbsInfo;
}

/// <summary>What gets write()'d to /dev/uinput to emit one event. On a 64-bit kernel, struct timeval's
/// two `long` fields are 8 bytes each, so `time` occupies 16 bytes even though we always zero it
/// (the kernel stamps the real time on submit).</summary>
[StructLayout(LayoutKind.Explicit, Size = 24)]
internal struct InputEvent {
	[FieldOffset(0)] public long TimeSec;
	[FieldOffset(8)] public long TimeUsec;
	[FieldOffset(16)] public ushort Type;
	[FieldOffset(18)] public ushort Code;
	[FieldOffset(20)] public int Value;

	public InputEvent(ushort type, ushort code, int value) {
		TimeSec = 0;
		TimeUsec = 0;
		Type = type;
		Code = code;
		Value = value;
	}
}
