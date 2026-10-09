using MagicRemote.Core;

namespace MagicRemote.Daemon.Power;

/// <summary>
/// Ports Service.cs's shutdown handling: MessageType.Shutdown triggers an immediate poweroff
/// (originally `shutdown /s /t 0`); a countdown variant mirrors `shutdown /s /t 300` + `shutdown /a`,
/// polling ISessionActivityMonitor instead of GetLastInputInfo to auto-abort on local activity.
///
/// Not yet wired up (known gap, see linux/tools plan notes): the original also triggers the 300s
/// countdown automatically when the TV goes quiet, via a ping frame with a custom payload that
/// System.Net.WebSockets has no API to send — that trigger path needs a redesign (likely an explicit
/// protocol message) before ScheduleShutdown can be driven automatically instead of only from
/// MessageType.Shutdown.
/// </summary>
internal sealed class PowerService : IDisposable {
	private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);

	private readonly IPowerManager _powerManager;
	private readonly ISessionActivityMonitor _activityMonitor;
	private readonly ILogger _logger;
	private readonly TimeSpan _pollInterval;
	private CancellationTokenSource? _pendingShutdown;

	public PowerService(IPowerManager powerManager, ISessionActivityMonitor activityMonitor, ILogger logger, TimeSpan? pollInterval = null) {
		_powerManager = powerManager;
		_activityMonitor = activityMonitor;
		_logger = logger;
		_pollInterval = pollInterval ?? DefaultPollInterval;
	}

	public Task ShutdownNowAsync(CancellationToken cancellationToken) {
		CancelPendingShutdown();
		_logger.Log("Shutdown requested: powering off now");
		return _powerManager.PowerOffAsync(cancellationToken);
	}

	/// <summary>Starts (or restarts) a cancelable countdown, aborting early if local activity is
	/// detected. Fire-and-forget by design, matching the original's non-blocking Process.Start.</summary>
	public void ScheduleShutdown(TimeSpan delay) {
		CancelPendingShutdown();
		CancellationTokenSource cts = new();
		_pendingShutdown = cts;
		_logger.Log($"Shutdown scheduled in {delay.TotalSeconds:0}s (cancels on local activity)");
		_ = RunCountdownAsync(delay, cts.Token);
	}

	public void CancelPendingShutdown() {
		if(_pendingShutdown is null) {
			return;
		}
		_pendingShutdown.Cancel();
		_pendingShutdown.Dispose();
		_pendingShutdown = null;
		_logger.Log("Pending shutdown canceled");
	}

	private async Task RunCountdownAsync(TimeSpan delay, CancellationToken cancellationToken) {
		try {
			DateTime deadline = DateTime.UtcNow + delay;
			while(DateTime.UtcNow < deadline) {
				if(!await _activityMonitor.IsIdleAsync(cancellationToken)) {
					_logger.Log("Shutdown countdown aborted: local activity detected");
					return;
				}
				await Task.Delay(_pollInterval, cancellationToken);
			}
			_logger.Log("Shutdown countdown elapsed: powering off");
			await _powerManager.PowerOffAsync(cancellationToken);
		} catch(OperationCanceledException) {
			// canceled via CancelPendingShutdown or shutdown, nothing to do
		}
	}

	public void Dispose() => CancelPendingShutdown();
}
