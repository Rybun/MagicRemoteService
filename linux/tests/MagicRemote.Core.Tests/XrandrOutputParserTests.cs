using MagicRemote.Core.Monitors;
using Xunit;

namespace MagicRemote.Core.Tests;

public class XrandrOutputParserTests {
	// Captured verbatim from `xrandr --query` on a real dual-monitor desktop (primary monitor is the
	// *second* physical output here, which is exactly the kind of layout that exercises the
	// left-of-origin/negative-coordinate math in MonitorGeometryProvider).
	private const string RealDualMonitorOutput = """
		Screen 0: minimum 16 x 16, current 5120 x 1440, maximum 32767 x 32767
		DP-3 connected 2560x1440+0+0 (normal left inverted right x axis y axis) 598mm x 336mm
		   2560x1440    164.90*+
		   1920x1440    164.87
		DP-2 connected primary 2560x1440+2560+0 (normal left inverted right x axis y axis) 598mm x 336mm
		   2560x1440    164.90*+
		HDMI-1 disconnected (normal left inverted right x axis y axis)
		""";

	[Fact]
	public void Parse_ExtractsBothConnectedMonitors() {
		IReadOnlyList<MonitorLayout> monitors = XrandrOutputParser.Parse(RealDualMonitorOutput);

		Assert.Equal(2, monitors.Count);
	}

	[Fact]
	public void Parse_ReadsFirstMonitorGeometry() {
		IReadOnlyList<MonitorLayout> monitors = XrandrOutputParser.Parse(RealDualMonitorOutput);

		MonitorLayout dp3 = monitors[0];
		Assert.Equal("DP-3", dp3.Name);
		Assert.Equal(0, dp3.X);
		Assert.Equal(0, dp3.Y);
		Assert.Equal(2560, dp3.Width);
		Assert.Equal(1440, dp3.Height);
		Assert.False(dp3.Primary);
	}

	[Fact]
	public void Parse_ReadsSecondMonitorGeometryAndPrimaryFlag() {
		IReadOnlyList<MonitorLayout> monitors = XrandrOutputParser.Parse(RealDualMonitorOutput);

		MonitorLayout dp2 = monitors[1];
		Assert.Equal("DP-2", dp2.Name);
		Assert.Equal(2560, dp2.X);
		Assert.Equal(0, dp2.Y);
		Assert.True(dp2.Primary);
	}

	[Fact]
	public void Parse_IgnoresDisconnectedOutputs() {
		IReadOnlyList<MonitorLayout> monitors = XrandrOutputParser.Parse(RealDualMonitorOutput);

		Assert.DoesNotContain(monitors, m => m.Name == "HDMI-1");
	}

	[Fact]
	public void Parse_ReturnsEmpty_ForEmptyInput() {
		Assert.Empty(XrandrOutputParser.Parse(""));
	}

	[Fact]
	public void Parse_HandlesNegativeCoordinates_ForMonitorsLeftOfOrigin() {
		const string output = """
			Screen 0: minimum 16 x 16, current 3840 x 1080, maximum 32767 x 32767
			DP-1 connected primary 1920x1080+0+0 (normal left inverted right x axis y axis) 531mm x 299mm
			DP-2 connected 1920x1080+-1920+0 (normal left inverted right x axis y axis) 531mm x 299mm
			""";

		IReadOnlyList<MonitorLayout> monitors = XrandrOutputParser.Parse(output);

		Assert.Equal(-1920, monitors[1].X);
	}
}
