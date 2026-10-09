using System.Globalization;
using MagicRemote.Gui.Localization;
using Xunit;

namespace MagicRemote.Gui.Tests;

public class LocalizationServiceTests {
	[Fact]
	public void Indexer_ReturnsEnglishByDefault() {
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		LocalizationService loc = new();

		Assert.Equal("Save", loc["SaveLabel"]);
	}

	[Fact]
	public void SetCulture_Spanish_SwitchesStrings() {
		LocalizationService loc = new();

		loc.SetCulture(new CultureInfo("es"));

		Assert.Equal("Guardar", loc["SaveLabel"]);
		Assert.Equal("Ajustes globales", loc["GlobalSettingsLabel"]);
	}

	[Fact]
	public void SetCulture_French_SwitchesStrings() {
		LocalizationService loc = new();

		loc.SetCulture(new CultureInfo("fr"));

		Assert.Equal("Enregistrer", loc["SaveLabel"]);
		Assert.Equal("Paramètres globaux", loc["GlobalSettingsLabel"]);
	}

	[Fact]
	public void Indexer_FallsBackToKeyName_ForUnknownKey() {
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		LocalizationService loc = new();

		Assert.Equal("NotARealKey", loc["NotARealKey"]);
	}

	[Fact]
	public void SetCulture_RaisesPropertyChanged_ForItemIndexer() {
		LocalizationService loc = new();
		string? raisedProperty = null;
		loc.PropertyChanged += (_, e) => raisedProperty = e.PropertyName;

		loc.SetCulture(new CultureInfo("fr"));

		Assert.Equal("Item[]", raisedProperty);
	}
}
