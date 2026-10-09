using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MagicRemote.Core.Monitors;
using MagicRemote.Gui.ViewModels;

namespace MagicRemote.Gui.Views;

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();
		DataContext = new MainWindowViewModel();
		Loaded += OnLoaded;
	}

	private async void OnLoaded(object? sender, RoutedEventArgs e) {
		if(DataContext is MainWindowViewModel vm) {
			await vm.LoadAsync();
		}
		RefreshAvailableDisplays();
		if(Screens is { } screens) {
			screens.Changed += (_, _) => RefreshAvailableDisplays();
		}
	}

	// Same Screens -> MonitorLayout mapping as AvaloniaMonitorPublisher, just feeding the ViewModel's
	// Display dropdown instead of writing monitors.json.
	private void RefreshAvailableDisplays() {
		if(DataContext is not MainWindowViewModel vm || Screens is not { } screens) {
			return;
		}
		var monitors = screens.All.Select((screen, index) => new MonitorLayout(
			Index: index,
			Name: screen.DisplayName ?? $"screen-{index}",
			X: screen.Bounds.X,
			Y: screen.Bounds.Y,
			Width: screen.Bounds.Width,
			Height: screen.Bounds.Height,
			Primary: screen.IsPrimary)).ToList();
		vm.RefreshAvailableDisplays(monitors);
	}

	// SelectedValue/SelectedValueBinding only round-trips the raw index (see the XAML comment on the
	// Display ComboBox for why that alone isn't enough) — this captures the option's stable Name
	// alongside it, on every selection change, including the self-heal in
	// MainWindowViewModel.RefreshAvailableDisplays reassigning Display programmatically (harmless
	// here: it re-sets DisplayName to the same value it already matched by).
	private void OnDisplaySelectionChanged(object? sender, SelectionChangedEventArgs e) {
		if(DataContext is MainWindowViewModel { SelectedTv: { } tv } && sender is ComboBox { SelectedItem: DisplayOption option }) {
			tv.DisplayName = option.Name;
		}
	}

	private async void OnCaptureTestKeyClick(object? sender, RoutedEventArgs e) {
		BindCreatorWindow dialog = DataContext is MainWindowViewModel { Loc: var loc } ? new BindCreatorWindow(loc) : new BindCreatorWindow();
		ushort? code = await dialog.ShowDialog<ushort?>(this);
		if(DataContext is MainWindowViewModel vm) {
			vm.StatusMessage = code is null
				? "Key capture canceled"
				: $"Captured evdev key code 0x{code:x2}";
		}
	}

	private async void OnEditBindClick(object? sender, RoutedEventArgs e) {
		if(sender is not Button { Tag: RemoteBindRowViewModel row } || DataContext is not MainWindowViewModel { Loc: var loc }) {
			return;
		}
		BindEditorWindow dialog = new(row.Label, row.EffectiveBinds, loc);
		BindEditorResult? result = await dialog.ShowDialog<BindEditorResult?>(this);
		switch(result?.Outcome) {
			case BindEditorOutcome.Saved:
				row.OverrideBinds = result.Binds;
				break;
			case BindEditorOutcome.ResetToDefault:
				row.OverrideBinds = null;
				break;
		}
	}
}
