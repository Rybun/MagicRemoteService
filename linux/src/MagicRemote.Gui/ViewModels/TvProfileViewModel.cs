using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MagicRemote.Core;
using MagicRemote.Core.Config;
using MagicRemote.Gui.Localization;

namespace MagicRemote.Gui.ViewModels;

/// <summary>Editable wrapper around a TvProfile — mirrors what Setting.cs/TVAdder.cs's WinForms
/// controls bound to directly; here the binding target is this observable view model instead.</summary>
public partial class TvProfileViewModel : ObservableObject {
	[ObservableProperty] private string _id;
	[ObservableProperty] private int _display;
	[ObservableProperty] private string? _displayName;
	[ObservableProperty] private string _sendIp;
	[ObservableProperty] private int _sendPort;
	[ObservableProperty] private string _mask;
	[ObservableProperty] private string _pcMac;
	[ObservableProperty] private int _longClickMs;
	[ObservableProperty] private bool _inputDirect;
	[ObservableProperty] private bool _overlay;
	[ObservableProperty] private bool _extend;
	[ObservableProperty] private string _inputId;
	[ObservableProperty] private string? _aresDeviceName;

	public TvProfileViewModel() : this(new TvProfile { Id = "new-tv" }, new LocalizationService()) { }

	/// <summary>Ports Setting.cs's behavior when adding a TV: SendIp/Mask/PcMac prefill from the
	/// PC's own active network interface, SendPort prefills from the currently configured global
	/// port — none of these are fixed literals in the original.</summary>
	public static TvProfileViewModel CreateNew(int currentGlobalPort, ILocalizationService loc) {
		NetworkDefaults.PrimaryInterface primary = NetworkDefaults.GetPrimaryInterface();
		return new TvProfileViewModel(new TvProfile {
			Id = "new-tv",
			SendIp = primary.Ip,
			SendPort = currentGlobalPort,
			Mask = primary.Mask,
			PcMac = primary.Mac
		}, loc);
	}

	public TvProfileViewModel(TvProfile profile, ILocalizationService loc) {
		_id = profile.Id;
		_display = profile.Display;
		_displayName = profile.DisplayName;
		_sendIp = profile.SendIp;
		_sendPort = profile.SendPort;
		_mask = profile.Mask;
		_pcMac = profile.PcMac;
		_longClickMs = profile.LongClickMs;
		_inputDirect = profile.InputDirect;
		_overlay = profile.Overlay;
		_extend = profile.Extend;
		_inputId = profile.InputId;
		_aresDeviceName = profile.AresDeviceName;

		RemoteBinds = new ObservableCollection<RemoteBindRowViewModel>(
			RemoteKeyLabels.All.Select(k => new RemoteBindRowViewModel(
				k.Code, k.LabelKey, profile.Binds.GetValueOrDefault(k.Code), loc)));
	}

	/// <summary>One row per remote key (see RemoteKeyLabels) — the "Remote" tab grid binds directly
	/// to this. ToProfile() rebuilds the persisted Binds dictionary from whichever rows have an
	/// explicit override.</summary>
	public ObservableCollection<RemoteBindRowViewModel> RemoteBinds { get; }

	public TvProfile ToProfile() => new() {
		Id = Id,
		Display = Display,
		DisplayName = DisplayName,
		SendIp = SendIp,
		SendPort = SendPort,
		Mask = Mask,
		PcMac = PcMac,
		LongClickMs = LongClickMs,
		InputDirect = InputDirect,
		Overlay = Overlay,
		Extend = Extend,
		InputId = InputId,
		AresDeviceName = AresDeviceName,
		Binds = RemoteBinds.Where(r => r.IsCustom).ToDictionary(r => r.Code, r => r.OverrideBinds!)
	};
}
