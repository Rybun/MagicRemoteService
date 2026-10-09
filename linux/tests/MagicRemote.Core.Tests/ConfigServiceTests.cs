using MagicRemote.Core.Config;
using MagicRemote.InputEngine;
using Xunit;

namespace MagicRemote.Core.Tests;

public class ConfigServiceTests : IDisposable {
	private readonly string _path = Path.Combine(Path.GetTempPath(), $"magicremote-test-{Guid.NewGuid():N}.json");

	[Fact]
	public async Task LoadAsync_ReturnsDefault_WhenFileMissing() {
		ConfigService service = new(_path);

		GlobalConfig config = await service.LoadAsync();

		Assert.Equal(41230, config.Port);
		Assert.Empty(config.Tvs);
	}

	[Fact]
	public async Task SaveThenLoad_RoundTripsScalarFields() {
		ConfigService service = new(_path);
		GlobalConfig original = GlobalConfig.Default() with { Port = 12345 };

		await service.SaveAsync(original);
		GlobalConfig loaded = await service.LoadAsync();

		Assert.Equal(12345, loaded.Port);
	}

	[Fact]
	public async Task SaveThenLoad_RoundTripsTvProfileAndPolymorphicBinds() {
		ConfigService service = new(_path);
		TvProfile tv = new() {
			Id = "salon",
			Display = 1,
			SendIp = "192.168.1.50",
			Binds = new Dictionary<ushort, Bind[]> {
				[0x0030] = [new Bind.Mouse(MouseButton.Middle)],
				[0x0031] = [new Bind.Keyboard(EvdevCodes.KEY_LEFTCTRL, EvdevCodes.KEY_C)],
				[0x0193] = [new Bind.Action(BindAction.Shutdown)],
				[0x0194] = [new Bind.Command("systemctl suspend")],
			}
		};
		GlobalConfig original = GlobalConfig.Default() with { Tvs = [tv] };

		await service.SaveAsync(original);
		GlobalConfig loaded = await service.LoadAsync();

		TvProfile loadedTv = Assert.Single(loaded.Tvs);
		Assert.Equal("salon", loadedTv.Id);
		Assert.Equal("192.168.1.50", loadedTv.SendIp);

		Bind.Mouse mouse = Assert.IsType<Bind.Mouse>(Assert.Single(loadedTv.Binds[0x0030]));
		Assert.Equal(MouseButton.Middle, mouse.Button);

		Bind.Keyboard keyboard = Assert.IsType<Bind.Keyboard>(Assert.Single(loadedTv.Binds[0x0031]));
		Assert.Equal([EvdevCodes.KEY_LEFTCTRL, EvdevCodes.KEY_C], keyboard.Codes);

		Bind.Action action = Assert.IsType<Bind.Action>(Assert.Single(loadedTv.Binds[0x0193]));
		Assert.Equal(BindAction.Shutdown, action.Value);

		Bind.Command command = Assert.IsType<Bind.Command>(Assert.Single(loadedTv.Binds[0x0194]));
		Assert.Equal("systemctl suspend", command.Shell);
	}

	[Fact]
	public async Task SaveAsync_DoesNotLeaveTempFileBehind() {
		ConfigService service = new(_path);

		await service.SaveAsync(GlobalConfig.Default());

		Assert.True(File.Exists(_path));
		Assert.False(File.Exists(_path + ".tmp"));
	}

	public void Dispose() {
		File.Delete(_path);
		File.Delete(_path + ".tmp");
	}
}
