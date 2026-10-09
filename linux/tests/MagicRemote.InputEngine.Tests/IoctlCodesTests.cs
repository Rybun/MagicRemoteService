using MagicRemote.InputEngine.Native;
using Xunit;

namespace MagicRemote.InputEngine.Tests;

/// <summary>
/// Pins IoctlCodes' formula-derived values against the exact hex constants dumped from a real compile
/// against &lt;linux/uinput.h&gt; (see linux/tools/dump-uinput-consts/dump_uinput_consts.c). If this
/// ever fails, the _IOC encoding in IoctlCodes.cs has drifted from the kernel ABI.
/// </summary>
public class IoctlCodesTests {
	[Fact]
	public void MatchesRealKernelHeaderValues() {
		Assert.Equal(0x00005501u, IoctlCodes.UI_DEV_CREATE);
		Assert.Equal(0x00005502u, IoctlCodes.UI_DEV_DESTROY);
		Assert.Equal(0x405c5503u, IoctlCodes.UI_DEV_SETUP);
		Assert.Equal(0x401c5504u, IoctlCodes.UI_ABS_SETUP);
		Assert.Equal(0x40045564u, IoctlCodes.UI_SET_EVBIT);
		Assert.Equal(0x40045565u, IoctlCodes.UI_SET_KEYBIT);
		Assert.Equal(0x40045566u, IoctlCodes.UI_SET_RELBIT);
		Assert.Equal(0x40045567u, IoctlCodes.UI_SET_ABSBIT);
	}
}
