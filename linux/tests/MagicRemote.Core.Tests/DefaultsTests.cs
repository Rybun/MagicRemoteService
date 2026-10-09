using MagicRemote.Core;
using MagicRemote.Core.Config;
using Xunit;

namespace MagicRemote.Core.Tests;

/// <summary>Pins defaults against the original Windows app's literals (Setting.cs/Service.cs),
/// confirmed by reading the source directly — not guessed.</summary>
public class DefaultsTests {
	[Fact]
	public void GlobalConfig_MatchesOriginalDefaults() {
		GlobalConfig config = GlobalConfig.Default();

		Assert.Equal(41230, config.Port);
		Assert.True(config.Inactivity.Enabled);
		Assert.Equal(7200, config.Inactivity.TimeoutSeconds); // 7200000ms in the original
		Assert.True(config.VideoInput.Enabled);
		Assert.Equal(900, config.VideoInput.TimeoutSeconds); // 900000ms in the original
	}

	[Fact]
	public void TvProfile_MatchesOriginalDefaults() {
		TvProfile tv = new() { Id = "test" };

		Assert.Equal(1500, tv.LongClickMs);
		Assert.True(tv.InputDirect);
		Assert.False(tv.Overlay);
		Assert.False(tv.Extend);
	}

	[Fact]
	public void NetworkDefaults_FindsAnActiveInterface() {
		// This runs on a real machine with real interfaces (not mocked) — same approach the original
		// took: compute from System.Net.NetworkInformation at the point of use, not a fixed literal.
		NetworkDefaults.PrimaryInterface primary = NetworkDefaults.GetPrimaryInterface();

		Assert.False(string.IsNullOrEmpty(primary.Ip));
		Assert.False(string.IsNullOrEmpty(primary.Mac));
	}
}
