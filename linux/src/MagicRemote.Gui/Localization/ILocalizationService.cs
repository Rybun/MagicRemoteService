using System.ComponentModel;

namespace MagicRemote.Gui.Localization;

/// <summary>
/// Replaces the original's Properties.Resources.* (culture-driven .resx, es/fr alongside the
/// invariant/English default) — same idea, ResourceManager-based, just exposed as an indexer so
/// XAML can bind `{Binding Loc[SaveLabel]}` instead of needing one CLR property per string.
/// Implements INotifyPropertyChanged so a runtime language switch (Fase 7+, not built yet) could
/// refresh every bound label by raising a change on the indexer.
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged {
	string this[string key] { get; }
}
