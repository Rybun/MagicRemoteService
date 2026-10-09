using CommunityToolkit.Mvvm.ComponentModel;
using MagicRemote.Core;
using MagicRemote.Gui.Localization;

namespace MagicRemote.Gui.ViewModels;

/// <summary>One row in the "Remote" tab grid — one remote key, its current effective bind (a
/// per-TV override if the user set one, else DefaultBindTable's entry), and the human summary shown
/// on its button.</summary>
public sealed partial class RemoteBindRowViewModel : ObservableObject {
	private readonly ILocalizationService _loc;

	public ushort Code { get; }
	public string LabelKey { get; }
	public string Label => _loc[LabelKey];

	/// <summary>Null means "no override — using DefaultBindTable". Non-null (including an empty
	/// array, which BindEditorWindow never actually produces but is handled defensively) means the
	/// user explicitly set this key's bind for this TV.</summary>
	[ObservableProperty] private Bind[]? _overrideBinds;

	public Bind[] EffectiveBinds => OverrideBinds ?? (DefaultBindTable.Entries.TryGetValue(Code, out Bind[]? d) ? d : []);
	public bool IsCustom => OverrideBinds is not null;
	public string Summary => BindSummaryFormatter.Format(EffectiveBinds, _loc) + " " + _loc[IsCustom ? "CustomSuffixLabel" : "DefaultSuffixLabel"];

	public RemoteBindRowViewModel(ushort code, string labelKey, Bind[]? overrideBinds, ILocalizationService loc) {
		Code = code;
		LabelKey = labelKey;
		_overrideBinds = overrideBinds;
		_loc = loc;
	}

	partial void OnOverrideBindsChanged(Bind[]? value) {
		OnPropertyChanged(nameof(EffectiveBinds));
		OnPropertyChanged(nameof(IsCustom));
		OnPropertyChanged(nameof(Summary));
	}
}
