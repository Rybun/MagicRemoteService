// Best-effort X11 monitor-geometry publisher: runs inside the user's own graphical session (unlike
// the daemon, which runs as a separate system service/user and can't query the display directly),
// polls `xrandr --query`, and writes monitors.json (see MonitorFilePaths) for MonitorGeometryProvider
// to read. Superseded by the Fase 4 Avalonia GUI once it exists (its cross-backend Screens API covers
// Wayland too, which this — being xrandr-based — cannot).
using System.Text.Json;
using MagicRemote.Core.Monitors;

TimeSpan pollInterval = TimeSpan.FromSeconds(5);

string outputPath = MonitorFilePaths.ForCurrentUser();

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, e) => {
	e.Cancel = true;
	cts.Cancel();
};

string? lastWritten = null;
while(!cts.IsCancellationRequested) {
	string? xrandrOutput = await RunXrandrAsync(cts.Token);
	if(xrandrOutput is not null) {
		IReadOnlyList<MonitorLayout> monitors = XrandrOutputParser.Parse(xrandrOutput);
		if(monitors.Count > 0) {
			string json = JsonSerializer.Serialize(monitors);
			if(json != lastWritten) {
				try {
					string tempPath = outputPath + ".tmp";
					await File.WriteAllTextAsync(tempPath, json, cts.Token);
					File.Move(tempPath, outputPath, overwrite: true);
					lastWritten = json;
					Console.WriteLine($"Published {monitors.Count} monitor(s) to {outputPath}");
				} catch(UnauthorizedAccessException) {
					// Same best-effort tolerance as AvaloniaMonitorPublisher: don't crash-loop
					// while this session's group membership hasn't caught up yet.
				}
			}
		}
	}
	try {
		await Task.Delay(pollInterval, cts.Token);
	} catch(OperationCanceledException) {
		break;
	}
}
return 0;

static async Task<string?> RunXrandrAsync(CancellationToken cancellationToken) {
	try {
		System.Diagnostics.ProcessStartInfo startInfo = new("xrandr", "--query") {
			RedirectStandardOutput = true,
			UseShellExecute = false
		};
		using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(startInfo);
		if(process is null) {
			return null;
		}
		string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
		await process.WaitForExitAsync(cancellationToken);
		return process.ExitCode == 0 ? output : null;
	} catch(System.ComponentModel.Win32Exception) {
		return null; // xrandr not installed (e.g. Wayland-only system)
	}
}
