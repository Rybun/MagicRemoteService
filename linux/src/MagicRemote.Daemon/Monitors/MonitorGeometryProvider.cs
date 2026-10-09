using MagicRemote.Core.Monitors;
using MagicRemote.Daemon.Sessions;

namespace MagicRemote.Daemon.Monitors;

/// <summary>
/// Maps the TV's fixed 1920x1080 remote-pointer coordinate space onto the real virtual-desktop pixel
/// range (0-65535) our VirtualAbsoluteMouse device reports, targeting a specific monitor by index.
/// Ports Service.cs's PositionAbsolute formula (scrDisplay.Bounds.X + Bounds.Width*x/1920, then
/// normalized by DesktopBounds) with one deliberate correction: this subtracts the desktop's minimum
/// X/Y before normalizing, so layouts with a monitor to the left of/above the origin (negative
/// coordinates) map correctly — the original only worked by coincidence when DesktopBounds.X was 0.
///
/// Session/monitor lookups are cached briefly (CacheTtl) since this runs per PositionAbsolute
/// message — often many per second during active use — and both the active-session lookup (shells
/// out to loginctl) and the monitor file read are far too slow to redo on every mouse move.
/// </summary>
internal sealed class MonitorGeometryProvider {
	private const int TvSpaceWidth = 1920;
	private const int TvSpaceHeight = 1080;
	private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromSeconds(5);
	private static readonly MonitorLayout FallbackMonitor = new(Index: 0, Name: "fallback", X: 0, Y: 0, Width: 1920, Height: 1080, Primary: true);

	private readonly IActiveSessionResolver _sessionResolver;
	private readonly IMonitorLayoutSource _layoutSource;
	private readonly TimeSpan _cacheTtl;
	private (IReadOnlyList<MonitorLayout> Monitors, DateTime FetchedAtUtc)? _cache;

	public MonitorGeometryProvider(IActiveSessionResolver sessionResolver, IMonitorLayoutSource layoutSource, TimeSpan? cacheTtl = null) {
		_sessionResolver = sessionResolver;
		_layoutSource = layoutSource;
		_cacheTtl = cacheTtl ?? DefaultCacheTtl;
	}

	public async Task<(int X, int Y)> MapToVirtualDesktopAsync(int displayIndex, string? displayName, int rawX, int rawY, CancellationToken cancellationToken) {
		IReadOnlyList<MonitorLayout> monitors = await GetMonitorsAsync(cancellationToken);
		MonitorLayout monitor = FindMonitor(monitors, displayIndex, displayName);
		(int minX, int minY, int width, int height) = ComputeDesktopBounds(monitors);

		int pixelX = monitor.X + (monitor.Width * rawX / TvSpaceWidth);
		int pixelY = monitor.Y + (monitor.Height * rawY / TvSpaceHeight);

		int absX = Math.Clamp((pixelX - minX) * 65535 / width, 0, 65535);
		int absY = Math.Clamp((pixelY - minY) * 65535 / height, 0, 65535);
		return (absX, absY);
	}

	// Same "index isn't stable across a reconnect" reasoning as TvProfile.DisplayName (see its doc
	// comment): prefer the stable output name when the TV has one recorded, since the GUI's own
	// self-heal (MainWindowViewModel.RefreshAvailableDisplays) only runs while the GUI is open — the
	// daemon needs to do its own name-based resolution independently so pointer mapping stays correct
	// even between GUI sessions (e.g. after a reboot with the monitors replugged in a different order
	// before the GUI ever runs again). Falls back to the raw index for TVs with no recorded name yet.
	private static MonitorLayout FindMonitor(IReadOnlyList<MonitorLayout> monitors, int displayIndex, string? displayName) {
		if(!string.IsNullOrEmpty(displayName)) {
			foreach(MonitorLayout monitor in monitors) {
				if(monitor.Name == displayName) {
					return monitor;
				}
			}
		}
		foreach(MonitorLayout monitor in monitors) {
			if(monitor.Index == displayIndex) {
				return monitor;
			}
		}
		return monitors[0];
	}

	private static (int MinX, int MinY, int Width, int Height) ComputeDesktopBounds(IReadOnlyList<MonitorLayout> monitors) {
		int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
		foreach(MonitorLayout monitor in monitors) {
			minX = Math.Min(minX, monitor.X);
			minY = Math.Min(minY, monitor.Y);
			maxX = Math.Max(maxX, monitor.X + monitor.Width);
			maxY = Math.Max(maxY, monitor.Y + monitor.Height);
		}
		return (minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
	}

	private async Task<IReadOnlyList<MonitorLayout>> GetMonitorsAsync(CancellationToken cancellationToken) {
		if(_cache is { } cached && DateTime.UtcNow - cached.FetchedAtUtc < _cacheTtl) {
			return cached.Monitors;
		}
		IReadOnlyList<MonitorLayout> monitors = await FetchMonitorsAsync(cancellationToken);
		_cache = (monitors, DateTime.UtcNow);
		return monitors;
	}

	private async Task<IReadOnlyList<MonitorLayout>> FetchMonitorsAsync(CancellationToken cancellationToken) {
		ActiveSession? session = await _sessionResolver.FindActiveSessionAsync(cancellationToken);
		if(session is null) {
			return [FallbackMonitor];
		}
		IReadOnlyList<MonitorLayout>? monitors = await _layoutSource.ReadAsync(session.Value.Uid, cancellationToken);
		return monitors is { Count: > 0 } ? monitors : [FallbackMonitor];
	}
}
