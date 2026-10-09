using MagicRemote.Core;
using MagicRemote.Daemon.Power;
using MagicRemote.Daemon.Tests.Power;
using Xunit;

namespace MagicRemote.Daemon.Tests;

public class PowerServiceTests {
	private static ILogger NullLogger() => new NoOpLogger();

	[Fact]
	public async Task ShutdownNowAsync_CallsPowerManagerOnce() {
		FakePowerManager powerManager = new();
		using PowerService service = new(powerManager, new FakeActivityMonitor(), NullLogger());

		await service.ShutdownNowAsync(CancellationToken.None);

		Assert.Equal(1, powerManager.PowerOffCallCount);
	}

	[Fact]
	public async Task ScheduleShutdown_PowersOffAfterDelayWhenIdle() {
		FakePowerManager powerManager = new();
		FakeActivityMonitor activityMonitor = new() { IsIdle = true };
		using PowerService service = new(powerManager, activityMonitor, NullLogger(), pollInterval: TimeSpan.FromMilliseconds(10));

		service.ScheduleShutdown(TimeSpan.FromMilliseconds(50));
		await powerManager.PoweredOff.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(1, powerManager.PowerOffCallCount);
	}

	[Fact]
	public async Task ScheduleShutdown_AbortsWhenLocalActivityDetected() {
		FakePowerManager powerManager = new();
		FakeActivityMonitor activityMonitor = new() { IsIdle = false }; // someone's using the machine
		using PowerService service = new(powerManager, activityMonitor, NullLogger(), pollInterval: TimeSpan.FromMilliseconds(10));

		service.ScheduleShutdown(TimeSpan.FromSeconds(30));
		await Task.Delay(200); // let a couple of poll cycles run

		Assert.Equal(0, powerManager.PowerOffCallCount);
	}

	[Fact]
	public async Task CancelPendingShutdown_StopsAScheduledCountdown() {
		FakePowerManager powerManager = new();
		FakeActivityMonitor activityMonitor = new() { IsIdle = true };
		using PowerService service = new(powerManager, activityMonitor, NullLogger(), pollInterval: TimeSpan.FromMilliseconds(10));

		service.ScheduleShutdown(TimeSpan.FromSeconds(30));
		service.CancelPendingShutdown();
		await Task.Delay(200);

		Assert.Equal(0, powerManager.PowerOffCallCount);
	}

	[Fact]
	public async Task ShutdownNowAsync_CancelsAnyPendingCountdown() {
		FakePowerManager powerManager = new();
		FakeActivityMonitor activityMonitor = new() { IsIdle = true };
		using PowerService service = new(powerManager, activityMonitor, NullLogger(), pollInterval: TimeSpan.FromMilliseconds(10));

		service.ScheduleShutdown(TimeSpan.FromSeconds(30));
		await service.ShutdownNowAsync(CancellationToken.None);
		await Task.Delay(100); // if the countdown were still alive it'd have fired an extra call by now

		Assert.Equal(1, powerManager.PowerOffCallCount);
	}

	private sealed class NoOpLogger : ILogger {
		public void Log(string message) { }
	}
}
