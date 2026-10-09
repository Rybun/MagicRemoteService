using MagicRemote.Core.Monitors;
using MagicRemote.Daemon.Monitors;
using MagicRemote.Daemon.Tests.Monitors;
using Xunit;

namespace MagicRemote.Daemon.Tests;

public class MonitorGeometryProviderTests {
	[Fact]
	public async Task MapToVirtualDesktopAsync_UsesFallbackMonitor_WhenNoActiveSession() {
		FakeActiveSessionResolver sessionResolver = new() { Session = null };
		FakeMonitorLayoutSource layoutSource = new();
		MonitorGeometryProvider provider = new(sessionResolver, layoutSource);

		(int x, int y) = await provider.MapToVirtualDesktopAsync(0, null, 960, 540, CancellationToken.None);

		Assert.Equal(32767, x);
		Assert.Equal(32767, y);
	}

	[Fact]
	public async Task MapToVirtualDesktopAsync_UsesFallbackMonitor_WhenLayoutFileMissing() {
		FakeActiveSessionResolver sessionResolver = new();
		FakeMonitorLayoutSource layoutSource = new() { Monitors = null };
		MonitorGeometryProvider provider = new(sessionResolver, layoutSource);

		(int x, int y) = await provider.MapToVirtualDesktopAsync(0, null, 0, 0, CancellationToken.None);

		Assert.Equal(0, x);
		Assert.Equal(0, y);
	}

	[Fact]
	public async Task MapToVirtualDesktopAsync_TargetsSecondaryMonitorToTheRight() {
		FakeActiveSessionResolver sessionResolver = new();
		FakeMonitorLayoutSource layoutSource = new() {
			Monitors = [
				new MonitorLayout(0, "primary", 0, 0, 1920, 1080, true),
				new MonitorLayout(1, "secondary", 1920, 0, 1920, 1080, false),
			]
		};
		MonitorGeometryProvider provider = new(sessionResolver, layoutSource);

		(int x, int y) = await provider.MapToVirtualDesktopAsync(displayIndex: 1, displayName: null, rawX: 0, rawY: 0, CancellationToken.None);

		// Desktop is 3840 wide; the secondary monitor's left edge sits at pixel 1920 -> 1920/3840 = 0.5
		Assert.Equal(32767, x);
		Assert.Equal(0, y);
	}

	[Fact]
	public async Task MapToVirtualDesktopAsync_HandlesMonitorLeftOfOrigin_WithoutWrappingNegative() {
		// A monitor placed to the left of the primary has a negative X in most desktop environments'
		// coordinate systems. This is exactly the case the original Win32 formula got wrong (see class
		// doc comment) since it never subtracted DesktopBounds.X before normalizing.
		FakeActiveSessionResolver sessionResolver = new();
		FakeMonitorLayoutSource layoutSource = new() {
			Monitors = [
				new MonitorLayout(0, "left", -1920, 0, 1920, 1080, false),
				new MonitorLayout(1, "primary", 0, 0, 1920, 1080, true),
			]
		};
		MonitorGeometryProvider provider = new(sessionResolver, layoutSource);

		(int x, int y) = await provider.MapToVirtualDesktopAsync(displayIndex: 0, displayName: null, rawX: 960, rawY: 0, CancellationToken.None);

		Assert.InRange(x, 0, 65535); // must never go negative/wrap
		Assert.Equal(16383, x);
	}

	[Fact]
	public async Task MapToVirtualDesktopAsync_PrefersDisplayName_OverStaleIndex_AfterReconnectReordering() {
		// Simulates the bug this parameter exists to fix: the TV's saved index (1) now belongs to a
		// different physical monitor because outputs re-enumerated after a replug — only its stable
		// Name ("hdmi-tv") still identifies the right one.
		FakeActiveSessionResolver sessionResolver = new();
		FakeMonitorLayoutSource layoutSource = new() {
			Monitors = [
				new MonitorLayout(0, "hdmi-tv", 1920, 0, 1920, 1080, false),
				new MonitorLayout(1, "laptop-panel", 0, 0, 1920, 1080, true),
			]
		};
		MonitorGeometryProvider provider = new(sessionResolver, layoutSource);

		(int x, int y) = await provider.MapToVirtualDesktopAsync(displayIndex: 1, displayName: "hdmi-tv", rawX: 0, rawY: 0, CancellationToken.None);

		// Should land on "hdmi-tv" (now at index 0, left edge 1920 of a 3840-wide desktop), not on
		// "laptop-panel" (index 1, which the stale saved index would have pointed at).
		Assert.Equal(32767, x);
	}

	[Fact]
	public async Task MapToVirtualDesktopAsync_CachesWithinTtl() {
		FakeActiveSessionResolver sessionResolver = new();
		FakeMonitorLayoutSource layoutSource = new() {
			Monitors = [new MonitorLayout(0, "primary", 0, 0, 1920, 1080, true)]
		};
		MonitorGeometryProvider provider = new(sessionResolver, layoutSource, cacheTtl: TimeSpan.FromSeconds(30));

		await provider.MapToVirtualDesktopAsync(0, null, 0, 0, CancellationToken.None);
		await provider.MapToVirtualDesktopAsync(0, null, 100, 100, CancellationToken.None);

		Assert.Equal(1, sessionResolver.CallCount);
		Assert.Equal(1, layoutSource.CallCount);
	}

	[Fact]
	public async Task MapToVirtualDesktopAsync_RefreshesAfterTtlExpires() {
		FakeActiveSessionResolver sessionResolver = new();
		FakeMonitorLayoutSource layoutSource = new() {
			Monitors = [new MonitorLayout(0, "primary", 0, 0, 1920, 1080, true)]
		};
		MonitorGeometryProvider provider = new(sessionResolver, layoutSource, cacheTtl: TimeSpan.FromMilliseconds(10));

		await provider.MapToVirtualDesktopAsync(0, null, 0, 0, CancellationToken.None);
		await Task.Delay(30);
		await provider.MapToVirtualDesktopAsync(0, null, 0, 0, CancellationToken.None);

		Assert.Equal(2, sessionResolver.CallCount);
	}
}
