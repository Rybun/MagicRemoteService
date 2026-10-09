using System.Text.RegularExpressions;

namespace MagicRemote.Core.Monitors;

/// <summary>
/// Parses `xrandr --query` output into MonitorLayout entries. This is the X11 half of Fase 3's
/// monitor-geometry publishing story — it works today, standalone, without needing the Fase 4
/// Avalonia GUI to exist yet. Wayland compositors don't expose xrandr (most don't support XWayland's
/// RandR emulation for multi-monitor queries reliably either), so this is deliberately a stand-in:
/// once the Avalonia GUI ships, its cross-backend Screens API supersedes this for both X11 and Wayland.
/// </summary>
public static partial class XrandrOutputParser {
	[GeneratedRegex(@"^(?<name>\S+) connected (?<primary>primary )?(?<width>\d+)x(?<height>\d+)\+(?<x>-?\d+)\+(?<y>-?\d+)")]
	private static partial Regex ConnectedOutputRegex();

	public static IReadOnlyList<MonitorLayout> Parse(string xrandrQueryOutput) {
		List<MonitorLayout> monitors = [];
		int index = 0;
		foreach(string line in xrandrQueryOutput.Split('\n')) {
			Match match = ConnectedOutputRegex().Match(line);
			if(!match.Success) {
				continue;
			}
			monitors.Add(new MonitorLayout(
				Index: index++,
				Name: match.Groups["name"].Value,
				X: int.Parse(match.Groups["x"].Value),
				Y: int.Parse(match.Groups["y"].Value),
				Width: int.Parse(match.Groups["width"].Value),
				Height: int.Parse(match.Groups["height"].Value),
				Primary: match.Groups["primary"].Success));
		}
		return monitors;
	}
}
