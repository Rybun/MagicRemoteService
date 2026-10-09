using MagicRemote.Core;
using MagicRemote.Gui.Localization;
using MagicRemote.InputEngine;
using Xunit;

namespace MagicRemote.Gui.Tests;

public class BindSummaryFormatterTests {
	private static readonly ILocalizationService Loc = new LocalizationService();

	[Fact]
	public void Format_EmptyBinds_ReturnsUnassignedLabel() {
		string summary = BindSummaryFormatter.Format([], Loc);

		Assert.Equal(Loc["BindUnassignedLabel"], summary);
	}

	[Fact]
	public void Format_MouseLeft_ReturnsLocalizedLabel() {
		string summary = BindSummaryFormatter.Format([new Bind.Mouse(MouseButton.Left)], Loc);

		Assert.Equal(Loc["MouseLeftLabel"], summary);
	}

	[Fact]
	public void Format_KeyboardChord_JoinsKeyNamesWithPlus() {
		string summary = BindSummaryFormatter.Format(
			[new Bind.Keyboard(EvdevCodes.KEY_LEFTCTRL, EvdevCodes.KEY_C)], Loc);

		Assert.Contains("+", summary);
		Assert.Contains("ControlLeft", summary);
		Assert.Contains("C", summary);
	}

	[Fact]
	public void Format_Action_ReturnsLocalizedLabel() {
		string summary = BindSummaryFormatter.Format([new Bind.Action(BindAction.Shutdown)], Loc);

		Assert.Equal(Loc["ActionShutdownLabel"], summary);
	}

	[Fact]
	public void Format_Command_IncludesShellText() {
		string summary = BindSummaryFormatter.Format([new Bind.Command("notify-send hi")], Loc);

		Assert.Contains("notify-send hi", summary);
	}
}
