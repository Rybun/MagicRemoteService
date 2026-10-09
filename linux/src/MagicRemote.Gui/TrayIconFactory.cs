using System.IO;
using Avalonia.Media.Imaging;
using MagicRemote.Cli;

namespace MagicRemote.Gui;

/// <summary>Builds the window/tray icon from the exact same teardrop asset the TV app ships
/// (MagicRemote.Cli's TvTemplates), recolored to match the user's chosen border color — so the PC
/// app and the TV app are visibly the same product instead of the PC side showing a generic
/// placeholder.</summary>
internal static class TrayIconFactory {
	public static Bitmap CreateIcon(string iconBorderColorHex, int size = 32) {
		string templateFile = size <= 60 ? "miniIcon.png" : "largeIcon.png";
		byte[] source = AppExtractor.ReadIconTemplate(templateFile);
		byte[] recolored = IconRecolorer.RecolorBorder(source, iconBorderColorHex);
		using MemoryStream stream = new(recolored);
		return new Bitmap(stream);
	}
}
