using System.Runtime.InteropServices;
using MagicRemote.InputEngine.Native;
using Xunit;

namespace MagicRemote.InputEngine.Tests;

/// <summary>Pins struct sizes against the real kernel ABI (see IoctlCodesTests for the dumping method).</summary>
public class UinputStructsTests {
	[Fact]
	public void StructSizesMatchKernelAbi() {
		Assert.Equal(8, Marshal.SizeOf<InputId>());
		Assert.Equal(92, Marshal.SizeOf<UinputSetup>());
		Assert.Equal(24, Marshal.SizeOf<InputAbsInfo>());
		Assert.Equal(28, Marshal.SizeOf<UinputAbsSetup>());
		Assert.Equal(24, Marshal.SizeOf<InputEvent>());
	}
}
