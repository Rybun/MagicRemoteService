using MagicRemote.Core;
using MagicRemote.Core.Config;
using MagicRemote.Gui.Localization;
using MagicRemote.Gui.ViewModels;
using MagicRemote.InputEngine;
using Xunit;

namespace MagicRemote.Gui.Tests;

public class TvProfileViewModelTests {
	private static readonly ILocalizationService Loc = new LocalizationService();

	[Fact]
	public void Constructor_PopulatesOneRemoteBindRow_PerDefaultBindTableEntry() {
		TvProfileViewModel vm = new(new TvProfile { Id = "tv" }, Loc);

		Assert.Equal(RemoteKeyLabels.All.Count, vm.RemoteBinds.Count);
		Assert.All(vm.RemoteBinds, row => Assert.False(row.IsCustom));
	}

	[Fact]
	public void Constructor_MarksOverriddenRows_FromProfileBinds() {
		TvProfile profile = new() {
			Id = "tv",
			Binds = new Dictionary<ushort, Bind[]> { [0x0001] = [new Bind.Action(BindAction.Shutdown)] }
		};

		TvProfileViewModel vm = new(profile, Loc);

		RemoteBindRowViewModel row = Assert.Single(vm.RemoteBinds, r => r.Code == 0x0001);
		Assert.True(row.IsCustom);
		Assert.IsType<Bind.Action>(Assert.Single(row.EffectiveBinds));
	}

	[Fact]
	public void ToProfile_RoundTripsOnlyCustomizedRows() {
		TvProfileViewModel vm = new(new TvProfile { Id = "tv" }, Loc);
		RemoteBindRowViewModel row = vm.RemoteBinds.First(r => r.Code == 0x0002);
		row.OverrideBinds = [new Bind.Mouse(MouseButton.Middle)];

		TvProfile profile = vm.ToProfile();

		Bind[] persisted = Assert.Single(profile.Binds).Value;
		Assert.Equal(0x0002, Assert.Single(profile.Binds).Key);
		Assert.Equal(MouseButton.Middle, Assert.IsType<Bind.Mouse>(Assert.Single(persisted)).Button);
	}
}
