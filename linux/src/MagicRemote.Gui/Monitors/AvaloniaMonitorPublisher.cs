using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using MagicRemote.Core.Monitors;

namespace MagicRemote.Gui.Monitors;

/// <summary>
/// Writes the same monitors.json contract the standalone xrandr-based publisher
/// (MagicRemote.MonitorPublisher, Fase 3) writes — see MonitorFilePaths for the path — but sourced
/// from Avalonia's own cross-backend Screens API instead of shelling out to xrandr. This is the piece
/// that gives Wayland users real per-monitor mapping (xrandr doesn't work there) — whenever this GUI
/// is running, it supersedes the xrandr publisher's output; MonitorGeometryProvider doesn't care which
/// one wrote the file, both use the identical schema.
/// </summary>
internal sealed class AvaloniaMonitorPublisher {
	private readonly Window _window;
	private readonly string _outputPath;
	private string? _lastWritten;

	public AvaloniaMonitorPublisher(Window window) {
		_window = window;
		_outputPath = MonitorFilePaths.ForCurrentUser();

		window.Opened += (_, _) => Publish();
		window.Closed += (_, _) => Unsubscribe();
	}

	private void Unsubscribe() {
		if(_window.Screens is { } screens) {
			screens.Changed -= OnScreensChanged;
		}
	}

	private void OnScreensChanged(object? sender, EventArgs e) => Publish();

	public void Publish() {
		Screens? screens = _window.Screens;
		if(screens is null) {
			return;
		}
		screens.Changed -= OnScreensChanged; // avoid double-subscribing across repeated Publish() calls
		screens.Changed += OnScreensChanged;

		var monitors = screens.All.Select((screen, index) => new MonitorLayout(
			Index: index,
			Name: screen.DisplayName ?? $"screen-{index}",
			X: screen.Bounds.X,
			Y: screen.Bounds.Y,
			Width: screen.Bounds.Width,
			Height: screen.Bounds.Height,
			Primary: screen.IsPrimary)).ToList();

		if(monitors.Count == 0) {
			return;
		}

		string json = JsonSerializer.Serialize(monitors);
		if(json == _lastWritten) {
			return;
		}

		try {
			Directory.CreateDirectory(Path.GetDirectoryName(_outputPath)!);
			string tempPath = _outputPath + ".tmp";
			File.WriteAllText(tempPath, json);
			File.Move(tempPath, _outputPath, overwrite: true);
			_lastWritten = json;
		} catch(UnauthorizedAccessException) {
			// Can't write /run/magicremoteservice yet — e.g. this login session started before
			// install.sh's `usermod -aG magicremote` took effect (group membership only applies to
			// new sessions). Best-effort like the rest of this publisher: the daemon just falls back
			// to single-monitor geometry until the next successful publish (next login, or a
			// Screens.Changed event after group membership catches up).
		}
	}
}
