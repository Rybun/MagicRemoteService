using MagicRemote.Core;
using MagicRemote.Gui.Localization;
using MagicRemote.Gui.ViewModels;
using MagicRemote.InputEngine;
using Xunit;

namespace MagicRemote.Gui.Tests;

public class RemoteBindRowViewModelTests {
	private static readonly ILocalizationService Loc = new LocalizationService();

	[Fact]
	public void EffectiveBinds_FallsBackToDefaultBindTable_WhenNoOverride() {
		// 0x0001 is DefaultBindTable's left-click entry.
		RemoteBindRowViewModel row = new(0x0001, "RemoteKeyClick", overrideBinds: null, Loc);

		Assert.False(row.IsCustom);
		Assert.Equal(DefaultBindTable.Entries[0x0001], row.EffectiveBinds);
	}

	[Fact]
	public void EffectiveBinds_UsesOverride_WhenSet() {
		Bind[] custom = [new Bind.Action(BindAction.Shutdown)];
		RemoteBindRowViewModel row = new(0x0001, "RemoteKeyClick", custom, Loc);

		Assert.True(row.IsCustom);
		Assert.Equal(custom, row.EffectiveBinds);
	}

	[Fact]
	public void OverrideBinds_ChangingIt_UpdatesEffectiveBindsAndIsCustom() {
		RemoteBindRowViewModel row = new(0x0001, "RemoteKeyClick", overrideBinds: null, Loc);

		row.OverrideBinds = [new Bind.Mouse(MouseButton.Right)];

		Assert.True(row.IsCustom);
		Assert.Equal(MouseButton.Right, Assert.IsType<Bind.Mouse>(Assert.Single(row.EffectiveBinds)).Button);
	}

	[Fact]
	public void Label_ResolvesFromLabelKey() {
		RemoteBindRowViewModel row = new(0x000D, "RemoteKeyOk", overrideBinds: null, Loc);

		Assert.Equal(Loc["RemoteKeyOk"], row.Label);
	}
}
