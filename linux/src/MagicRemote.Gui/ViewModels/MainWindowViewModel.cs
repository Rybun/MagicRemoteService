using System.Collections.ObjectModel;
using System.Net;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagicRemote.Cli;
using MagicRemote.Core.Config;
using MagicRemote.Core.Monitors;
using MagicRemote.Gui.Localization;
using MagicRemote.Gui.Services;

namespace MagicRemote.Gui.ViewModels;

/// <summary>One entry in the Display dropdown: either a currently-detected monitor, or a display
/// index some configured TV still references but that isn't connected right now (kept selectable
/// so switching monitor setups doesn't silently reassign or lose a TV's display).</summary>
public sealed record DisplayOption(int Index, string Name, string Label);

/// <summary>Root view model — the Linux equivalent of Setting.cs, minus the Windows-service/firewall/
/// Task-Scheduler install steps (Fase 5) and the TV-pairing wizard (Fase 6, needs WebOSCLI first).</summary>
public partial class MainWindowViewModel : ObservableObject {
	private readonly ConfigService _configService;

	public ILocalizationService Loc { get; }

	[ObservableProperty] private int _port;
	[ObservableProperty] private bool _inactivityEnabled;
	[ObservableProperty] private int _inactivityTimeoutSeconds;
	[ObservableProperty] private bool _videoInputEnabled;
	[ObservableProperty] private int _videoInputTimeoutSeconds;
	[ObservableProperty] private string _iconBorderColor = "#FFFFFF";
	[ObservableProperty] private bool _serviceAutostartEnabled;
	/// <summary>Set while ServiceAutostartEnabled is being assigned from code (initial load, or
	/// reverting after a failed/cancelled pkexec) so OnServiceAutostartEnabledChanged doesn't treat
	/// that assignment as a user request to toggle systemd state.</summary>
	private bool _suppressAutostartToggle;

	partial void OnServiceAutostartEnabledChanged(bool value) {
		if(_suppressAutostartToggle) {
			return;
		}
		_ = ApplyServiceAutostartAsync(value);
	}

	private async Task ApplyServiceAutostartAsync(bool enable) {
		StatusMessage = enable ? Loc["EnablingAutostartLabel"] : Loc["DisablingAutostartLabel"];
		bool succeeded = await DaemonAutostartService.SetEnabledAsync(enable);
		if(succeeded) {
			StatusMessage = enable ? Loc["AutostartEnabledLabel"] : Loc["AutostartDisabledLabel"];
		} else {
			StatusMessage = Loc["AutostartChangeFailedLabel"];
			_suppressAutostartToggle = true;
			ServiceAutostartEnabled = !enable;
			_suppressAutostartToggle = false;
		}
	}

	/// <summary>The color-picker swatch/sliders bind to these (real typed values, not a raw hex
	/// string) — IconBorderColor stays the single source of truth (it's what gets persisted and sent
	/// to IconRecolorer), these are just typed views onto it.</summary>
	public Color IconBorderColorValue {
		get => Color.TryParse(IconBorderColor, out Color parsed) ? parsed : Colors.White;
		set => IconBorderColor = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
	}

	public int IconBorderColorR {
		get => IconBorderColorValue.R;
		set => IconBorderColorValue = new Color(255, (byte)Math.Clamp(value, 0, 255), IconBorderColorValue.G, IconBorderColorValue.B);
	}

	public int IconBorderColorG {
		get => IconBorderColorValue.G;
		set => IconBorderColorValue = new Color(255, IconBorderColorValue.R, (byte)Math.Clamp(value, 0, 255), IconBorderColorValue.B);
	}

	public int IconBorderColorB {
		get => IconBorderColorValue.B;
		set => IconBorderColorValue = new Color(255, IconBorderColorValue.R, IconBorderColorValue.G, (byte)Math.Clamp(value, 0, 255));
	}

	partial void OnIconBorderColorChanged(string value) {
		OnPropertyChanged(nameof(IconBorderColorValue));
		OnPropertyChanged(nameof(IconBorderColorR));
		OnPropertyChanged(nameof(IconBorderColorG));
		OnPropertyChanged(nameof(IconBorderColorB));
		// Live preview: the icon the user is about to install on the TV updates immediately as they
		// pick a color, rather than only after Guardar.
		(Avalonia.Application.Current as MagicRemote.Gui.App)?.RefreshIcon(value);
	}
	[ObservableProperty] private TvProfileViewModel? _selectedTv;
	[ObservableProperty] private string _statusMessage = string.Empty;
	[ObservableProperty] private bool _isInstallingToTv;

	public ObservableCollection<TvProfileViewModel> Tvs { get; } = [];
	public ObservableCollection<DisplayOption> AvailableDisplays { get; } = [];
	/// <summary>ares-cli device aliases (`ares-setup-device` entries) — populated on load, best-effort:
	/// an empty list here just means the webOS CLI isn't installed/reachable or no TV is paired yet,
	/// not a fatal error (same "gracefully degrade" pattern as the rest of the CLI-shelling code).</summary>
	public ObservableCollection<string> AvailablePairedDevices { get; } = [];
	/// <summary>WebOsCli.InputList()'s Ids — the 4 fixed HDMI slots a TV's app package can piggyback
	/// on. Kept as plain strings here (rather than pulling in MagicRemote.Cli's model) since the combo
	/// only ever needs the Id half of WebOsCliDeviceInput.</summary>
	public IReadOnlyList<string> AvailableInputSlots { get; } = WebOsCli.InputList().Select(i => i.Id).ToList();

	/// <summary>webOS app package version — same fixed literal appinfo.json's own template ships as
	/// its default, since nothing in this port varies it per-install (the original didn't either).</summary>
	private const string AppPackageVersion = "1.0.0";

	public MainWindowViewModel() : this(new ConfigService(), new LocalizationService()) { }

	public MainWindowViewModel(ConfigService configService, ILocalizationService? localization = null) {
		_configService = configService;
		Loc = localization ?? new LocalizationService();
	}

	public async Task LoadAsync() {
		GlobalConfig config = await _configService.LoadAsync();
		Port = config.Port;
		InactivityEnabled = config.Inactivity.Enabled;
		InactivityTimeoutSeconds = config.Inactivity.TimeoutSeconds;
		VideoInputEnabled = config.VideoInput.Enabled;
		VideoInputTimeoutSeconds = config.VideoInput.TimeoutSeconds;
		IconBorderColor = config.IconBorderColor;

		_suppressAutostartToggle = true;
		ServiceAutostartEnabled = await DaemonAutostartService.IsEnabledAsync();
		_suppressAutostartToggle = false;

		Tvs.Clear();
		foreach(TvProfile tv in config.Tvs) {
			Tvs.Add(new TvProfileViewModel(tv, Loc));
		}
		StatusMessage = $"Loaded from {_configService.Path}";

		await RefreshPairedDevicesAsync();
	}

	[RelayCommand]
	private async Task RefreshPairedDevicesAsync() {
		try {
			WebOsCliDevice[] devices = await Task.Run(WebOsCli.SetupDeviceList);
			AvailablePairedDevices.Clear();
			foreach(string name in devices.Select(d => d.Name).OfType<string>()) {
				AvailablePairedDevices.Add(name);
			}
		} catch(Exception ex) {
			// Not fatal (no TV paired yet is a normal first-run state) but surfaced rather than left
			// silent — an empty list with no explanation looked identical whether nothing was paired
			// or the webOS CLI (ares-*) just isn't on PATH at all, which cost real debugging time once.
			StatusMessage = $"{Loc["RefreshDevicesFailedLabel"]} {ex.Message}";
		}
	}

	/// <summary>Merges the monitors Avalonia currently reports with whatever display each configured
	/// TV still points at, so the dropdown always has an entry for the current selection even if that
	/// monitor got unplugged. Call whenever the window's Screens change (see MainWindow).
	///
	/// A monitor's positional Index is NOT stable across a reconnect — plugging/unplugging outputs in
	/// a different order reshuffles who's "index 0" — so a TV that has a DisplayName recorded (i.e.
	/// its display has been explicitly (re-)selected since this field existed) gets self-healed here:
	/// its Display index is snapped back to wherever that same named output currently lives, so the
	/// TV keeps tracking the right physical screen across unplug/replug and app restarts instead of
	/// silently drifting onto whatever else now happens to sit at the old index. TVs with no
	/// DisplayName yet (pre-existing configs) fall back to the old index-only matching.</summary>
	public void RefreshAvailableDisplays(IReadOnlyList<MonitorLayout> currentMonitors) {
		List<DisplayOption> options = currentMonitors
			.Select(m => new DisplayOption(m.Index, m.Name, $"{m.Index}: {m.Name} ({m.Width}x{m.Height}){(m.Primary ? " [primary]" : "")}"))
			.ToList();

		foreach(TvProfileViewModel tv in Tvs) {
			DisplayOption? matchByName = tv.DisplayName is { Length: > 0 }
				? options.FirstOrDefault(o => o.Name == tv.DisplayName)
				: null;
			if(matchByName is not null) {
				tv.Display = matchByName.Index;
			} else if(options.All(o => o.Index != tv.Display)) {
				string label = tv.DisplayName is { Length: > 0 } name
					? $"{name} (not currently connected)"
					: $"{tv.Display}: (not currently connected)";
				options.Add(new DisplayOption(tv.Display, tv.DisplayName ?? string.Empty, label));
			}
		}

		AvailableDisplays.Clear();
		foreach(DisplayOption option in options.OrderBy(o => o.Index)) {
			AvailableDisplays.Add(option);
		}
	}

	[RelayCommand]
	private void AddTv() {
		TvProfileViewModel tv = TvProfileViewModel.CreateNew(Port, Loc);
		Tvs.Add(tv);
		SelectedTv = tv;
	}

	[RelayCommand]
	private void RemoveSelectedTv() {
		if(SelectedTv is not null) {
			Tvs.Remove(SelectedTv);
			SelectedTv = null;
		}
	}

	[RelayCommand]
	private async Task SaveAsync() {
		GlobalConfig config = new() {
			Port = Port,
			Inactivity = new TimeoutSetting(InactivityEnabled, InactivityTimeoutSeconds),
			VideoInput = new TimeoutSetting(VideoInputEnabled, VideoInputTimeoutSeconds),
			IconBorderColor = IconBorderColor,
			Tvs = Tvs.Select(vm => vm.ToProfile()).ToList()
		};
		await _configService.SaveAsync(config);
		StatusMessage = $"Saved to {_configService.Path} at {DateTime.Now:T}";
	}

	/// <summary>Packages, installs and (unless Overlay is on) registers the selected TV's app as a
	/// video input — the flow Setting.cs's TVInstall_Click drove, assembled here from
	/// MagicRemote.Cli's AppExtractor/WebOsCli/TvInstaller.</summary>
	[RelayCommand]
	private async Task InstallToTvAsync() {
		TvProfileViewModel? tv = SelectedTv;
		if(tv is null || string.IsNullOrEmpty(tv.AresDeviceName)) {
			StatusMessage = Loc["NoPairedDeviceSelectedLabel"];
			return;
		}

		IsInstallingToTv = true;
		StatusMessage = Loc["InstallingToTvLabel"];
		try {
			TvInstallOptions options = new(
				AresDeviceName: tv.AresDeviceName,
				Version: AppPackageVersion,
				DisplayName: tv.Id,
				InputId: tv.InputId,
				SendIp: IPAddress.Parse(tv.SendIp),
				SendPort: (ushort)tv.SendPort,
				Mask: IPAddress.Parse(tv.Mask),
				PcMac: tv.PcMac,
				LongClickMs: tv.LongClickMs,
				InputDirect: tv.InputDirect,
				Overlay: tv.Overlay,
				Extend: tv.Extend,
				IconBorderColor: IconBorderColor);
			string workDirectory = Path.Combine(Path.GetTempPath(), "magicremoteservice-install", tv.Id);

			await Task.Run(() => TvInstaller.Install(options, workDirectory));
			StatusMessage = Loc["InstallToTvSucceededLabel"];
		} catch(Exception ex) {
			StatusMessage = $"{Loc["InstallToTvFailedLabel"]} {ex.Message}";
		} finally {
			IsInstallingToTv = false;
		}
	}
}
