using MagicRemote.Core.Config;
using MagicRemote.Gui.ViewModels;
using Xunit;

namespace MagicRemote.Gui.Tests;

/// <summary>
/// Exercises exactly the logic bound to MainWindow's Add/Remove/Save buttons and NumericUpDown/
/// CheckBox fields. Verified this way rather than by simulating real clicks: an attempt at
/// automated xdotool-driven UI testing (click "Add", read back a screenshot) turned out to be
/// unreliable in this container-to-host X11 setup — coordinate mapping between screenshots and
/// xdotool's window-relative click coordinates didn't line up, and chasing pixel-perfect coordinates
/// wasn't worth it when the actual button logic is trivially and more robustly covered here.
/// The GUI's ability to render on the real display and stay alive was separately confirmed visually.
/// </summary>
public class MainWindowViewModelTests : IDisposable {
	private readonly string _path = Path.Combine(Path.GetTempPath(), $"magicremote-gui-test-{Guid.NewGuid():N}.json");

	[Fact]
	public async Task LoadAsync_PopulatesFieldsFromConfig() {
		ConfigService configService = new(_path);
		await configService.SaveAsync(GlobalConfig.Default() with {
			Port = 12345,
			Inactivity = new TimeoutSetting(true, 60),
			Tvs = [new TvProfile { Id = "salon", SendIp = "192.168.1.50" }]
		});
		MainWindowViewModel vm = new(configService);

		await vm.LoadAsync();

		Assert.Equal(12345, vm.Port);
		Assert.True(vm.InactivityEnabled);
		Assert.Equal(60, vm.InactivityTimeoutSeconds);
		TvProfileViewModel tv = Assert.Single(vm.Tvs);
		Assert.Equal("salon", tv.Id);
		Assert.Equal("192.168.1.50", tv.SendIp);
	}

	[Fact]
	public void AddTvCommand_AddsAndSelectsANewEntry() {
		MainWindowViewModel vm = new(new ConfigService(_path));

		vm.AddTvCommand.Execute(null);

		Assert.Single(vm.Tvs);
		Assert.Same(vm.Tvs[0], vm.SelectedTv);
	}

	[Fact]
	public void RemoveSelectedTvCommand_RemovesTheSelectedEntry() {
		MainWindowViewModel vm = new(new ConfigService(_path));
		vm.AddTvCommand.Execute(null);

		vm.RemoveSelectedTvCommand.Execute(null);

		Assert.Empty(vm.Tvs);
		Assert.Null(vm.SelectedTv);
	}

	[Fact]
	public void RemoveSelectedTvCommand_DoesNothing_WhenNoneSelected() {
		MainWindowViewModel vm = new(new ConfigService(_path));
		vm.AddTvCommand.Execute(null);
		vm.SelectedTv = null;

		vm.RemoveSelectedTvCommand.Execute(null);

		Assert.Single(vm.Tvs);
	}

	[Fact]
	public async Task SaveCommand_PersistsEditedFieldsAndTvs() {
		MainWindowViewModel vm = new(new ConfigService(_path)) {
			Port = 9999,
			InactivityEnabled = false,
			InactivityTimeoutSeconds = 0
		};
		vm.AddTvCommand.Execute(null);
		vm.SelectedTv!.Id = "cocina";
		vm.SelectedTv.SendIp = "10.0.0.5";

		await vm.SaveCommand.ExecuteAsync(null);

		ConfigService reload = new(_path);
		GlobalConfig config = await reload.LoadAsync();
		Assert.Equal(9999, config.Port);
		Assert.False(config.Inactivity.Enabled);
		TvProfile tv = Assert.Single(config.Tvs);
		Assert.Equal("cocina", tv.Id);
		Assert.Equal("10.0.0.5", tv.SendIp);
	}

	[Fact]
	public async Task SaveCommand_SetsStatusMessage() {
		MainWindowViewModel vm = new(new ConfigService(_path));

		await vm.SaveCommand.ExecuteAsync(null);

		Assert.Contains(_path, vm.StatusMessage);
	}

	public void Dispose() {
		File.Delete(_path);
		File.Delete(_path + ".tmp");
	}
}
