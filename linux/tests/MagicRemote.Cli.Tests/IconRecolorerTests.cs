using MagicRemote.Cli;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MagicRemote.Cli.Tests;

public class IconRecolorerTests {
	private static byte[] LargeIconBytes => File.ReadAllBytes(Path.Combine(
		AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "MagicRemote.Cli", "TvTemplates", "MagicRemoteService", "largeIcon.png"));

	[Fact]
	public void RecolorBorder_ChangesOnlyPartiallyTransparentPixels_PreservingAlpha() {
		byte[] original = LargeIconBytes;
		byte[] recolored = IconRecolorer.RecolorBorder(original, "#00FF00");

		using Image<Rgba32> before = Image.Load<Rgba32>(original);
		using Image<Rgba32> after = Image.Load<Rgba32>(recolored);

		Assert.Equal(before.Width, after.Width);
		Assert.Equal(before.Height, after.Height);

		before.ProcessPixelRows(after, (beforeAccessor, afterAccessor) => {
			for(int y = 0; y < beforeAccessor.Height; y++) {
				Span<Rgba32> beforeRow = beforeAccessor.GetRowSpan(y);
				Span<Rgba32> afterRow = afterAccessor.GetRowSpan(y);
				for(int x = 0; x < beforeRow.Length; x++) {
					Rgba32 b = beforeRow[x];
					Rgba32 a = afterRow[x];

					Assert.Equal(b.A, a.A); // alpha (shape/antialiasing) is never touched

					if(b.A is 0 or 255) {
						Assert.Equal(b, a); // fully transparent background / fully opaque fill: untouched
					} else {
						Assert.Equal(0, a.R);
						Assert.Equal(255, a.G);
						Assert.Equal(0, a.B);
					}
				}
			}
		});
	}

	[Fact]
	public void RecolorBorder_RejectsMalformedHexColor() {
		Assert.Throws<ArgumentException>(() => IconRecolorer.RecolorBorder(LargeIconBytes, "not-a-color"));
	}
}
