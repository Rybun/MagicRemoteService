using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MagicRemote.Cli;

/// <summary>
/// Recolors the soft black border/shadow baked into the app's teardrop icon (icon.png/miniIcon.png/
/// largeIcon.png — see TvTemplates), without touching the white fill or reshaping anything. The
/// border isn't a separate layer or an SVG stroke; pixel analysis shows it's just every partially
/// transparent pixel rendered as black at that pixel's own alpha (two soft bands around alpha 64 and
/// 111, plus antialiasing in between) — the fully opaque pixels are the white fill, fully transparent
/// ones are background. So "recolor the border" reduces to: for every pixel that isn't fully opaque or
/// fully transparent, replace its RGB with the target color and keep its original alpha untouched.
/// </summary>
public static class IconRecolorer {
	public static byte[] RecolorBorder(byte[] pngBytes, string hexColor) {
		Rgb24 color = ParseHexColor(hexColor);
		using Image<Rgba32> image = Image.Load<Rgba32>(pngBytes);

		image.ProcessPixelRows(accessor => {
			for(int y = 0; y < accessor.Height; y++) {
				Span<Rgba32> row = accessor.GetRowSpan(y);
				for(int x = 0; x < row.Length; x++) {
					ref Rgba32 pixel = ref row[x];
					if(pixel.A is > 0 and < 255) {
						pixel.R = color.R;
						pixel.G = color.G;
						pixel.B = color.B;
					}
				}
			}
		});

		using MemoryStream output = new();
		image.SaveAsPng(output);
		return output.ToArray();
	}

	private static Rgb24 ParseHexColor(string hexColor) {
		string hex = hexColor.TrimStart('#');
		if(hex.Length != 6 || !byte.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out byte r)
			|| !byte.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out byte g)
			|| !byte.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out byte b)) {
			throw new ArgumentException($"Expected a #RRGGBB hex color, got '{hexColor}'", nameof(hexColor));
		}
		return new Rgb24(r, g, b);
	}
}
