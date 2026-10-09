using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MagicRemote.Gui.Localization;

namespace MagicRemote.Gui.Views;

/// <summary>
/// Live key capture for assigning a custom keyboard bind — the Linux equivalent of BindCreator.cs's
/// WM_KEYDOWN/WM_SYSKEYDOWN capture, but recording an evdev KEY_* code (via PhysicalKeyMap) instead
/// of a Windows virtual-key + scancode pair, since that's what VirtualKeyboard.KeyEvent expects.
/// Any physical key can be captured, including Escape — Cancel is a separate button rather than a
/// reserved key, so Escape itself stays bindable.
/// </summary>
public partial class BindCreatorWindow : Window {
	public ushort? CapturedCode { get; private set; }

	public BindCreatorWindow() : this(new LocalizationService()) { }

	public BindCreatorWindow(ILocalizationService localization) {
		InitializeComponent();
		DataContext = localization;
		Focusable = true;
		KeyDown += OnKeyDown;
	}

	private void OnKeyDown(object? sender, KeyEventArgs e) {
		ushort? code = PhysicalKeyMap.ToEvdevCode(e.PhysicalKey);
		e.Handled = true;
		if(code is null) {
			CapturedTextBlock.Text = $"Unsupported key: {e.PhysicalKey}";
			return;
		}
		CapturedCode = code;
		CapturedTextBlock.Text = $"Captured: {e.PhysicalKey} (evdev 0x{code:x2})";
		Close(code);
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
