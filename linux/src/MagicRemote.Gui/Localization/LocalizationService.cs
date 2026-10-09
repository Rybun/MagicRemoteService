using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace MagicRemote.Gui.Localization;

public sealed class LocalizationService : ILocalizationService {
	private readonly ResourceManager _resourceManager;

	public event PropertyChangedEventHandler? PropertyChanged;

	public LocalizationService() {
		_resourceManager = new ResourceManager("MagicRemote.Gui.Resources.Strings", typeof(LocalizationService).Assembly);
	}

	public string this[string key] => _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

	/// <summary>Not wired to any UI yet (no language picker exists) but functional: switches
	/// CurrentUICulture and notifies bound indexers to refresh.</summary>
	public void SetCulture(CultureInfo culture) {
		CultureInfo.CurrentUICulture = culture;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
	}
}
