using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MagicRemote.Core;
using MagicRemote.Gui.Localization;
using MouseButton = MagicRemote.InputEngine.MouseButton;

namespace MagicRemote.Gui.Views;

public enum BindEditorOutcome { Cancelled, Saved, ResetToDefault }

public sealed record BindEditorResult(BindEditorOutcome Outcome, Bind[]? Binds);

/// <summary>
/// Full bind editor for one remote key — the Linux equivalent of BindCreator.cs: four mutually
/// exclusive kinds (mouse/keyboard/action/command), matching the original's radio-button layout.
/// Keyboard capture differs from the original's WM_KEYDOWN/WM_KEYUP chord tracking (which snapshots
/// whatever's *currently held*): here, "start capture" simply appends each new key's evdev code
/// (via PhysicalKeyMap) as it's pressed, ignoring key-up entirely. For the common case (hold Ctrl,
/// press C) this produces the identical chord, since both keydowns still fire while Ctrl is held —
/// and it additionally supports sequential entry without needing simultaneous holds, at the cost of
/// not supporting "release one, the rest stays" semantics the original had. Good enough for the
/// binds this app actually uses (short modifier chords), simpler to implement and to explain.
/// </summary>
public partial class BindEditorWindow : Window {
	private readonly ILocalizationService _loc;
	private readonly List<ushort> _capturedCodes = [];
	private bool _capturing;

	public BindEditorWindow() : this("", [], new LocalizationService()) { }

	public BindEditorWindow(string keyLabel, Bind[] initialBinds, ILocalizationService loc) {
		InitializeComponent();
		_loc = loc;
		DataContext = loc;
		Focusable = true;
		KeyDown += OnKeyDown;

		KeyLabelTextBlock.Text = keyLabel;

		ActionComboBox.Items.Add(new ComboBoxItem { Content = loc["ActionShutdownLabel"], Tag = BindAction.Shutdown });
		ActionComboBox.Items.Add(new ComboBoxItem { Content = loc["ActionToggleKeyboardLabel"], Tag = BindAction.ToggleKeyboard });

		CaptureToggleButton.Content = loc["StartCaptureLabel"];
		CapturedKeysTextBlock.Text = loc["NoKeysYetLabel"];

		Preselect(initialBinds);
	}

	private void Preselect(Bind[] initialBinds) {
		switch(initialBinds.Length > 0 ? initialBinds[0] : null) {
			case Bind.Mouse mouse:
				MouseRadio.IsChecked = true;
				(mouse.Button switch {
					MouseButton.Left => MouseLeftRadio,
					MouseButton.Right => MouseRightRadio,
					MouseButton.Middle => MouseMiddleRadio,
					_ => MouseLeftRadio
				}).IsChecked = true;
				break;

			case Bind.Keyboard keyboard:
				KeyboardRadio.IsChecked = true;
				_capturedCodes.AddRange(keyboard.Codes);
				UpdateCapturedKeysText();
				break;

			case Bind.Action action:
				ActionRadio.IsChecked = true;
				foreach(ComboBoxItem item in ActionComboBox.Items.OfType<ComboBoxItem>()) {
					if(item.Tag is BindAction value && value == action.Value) {
						ActionComboBox.SelectedItem = item;
					}
				}
				break;

			case Bind.Command command:
				CommandRadio.IsChecked = true;
				CommandTextBox.Text = command.Shell;
				break;

			default:
				MouseRadio.IsChecked = true;
				MouseLeftRadio.IsChecked = true;
				break;
		}
	}

	// Switch on the sender that just got checked, not on reading every radio's IsChecked — within
	// one RadioButton group, the newly-checked radio's own Checked handler can run before its
	// siblings' IsChecked has actually flipped to false, so reading e.g. MouseRadio.IsChecked here
	// while handling KeyboardRadio's Checked event can still see it as true and leave both panels
	// visible at once.
	private void OnKindChanged(object? sender, RoutedEventArgs e) {
		MousePanel.IsVisible = ReferenceEquals(sender, MouseRadio);
		KeyboardPanel.IsVisible = ReferenceEquals(sender, KeyboardRadio);
		ActionPanel.IsVisible = ReferenceEquals(sender, ActionRadio);
		CommandPanel.IsVisible = ReferenceEquals(sender, CommandRadio);
		ValidationTextBlock.Text = "";
	}

	private void OnCaptureToggleClick(object? sender, RoutedEventArgs e) {
		_capturing = !_capturing;
		if(_capturing) {
			_capturedCodes.Clear();
			CaptureToggleButton.Content = _loc["StopCaptureLabel"];
			CapturedKeysTextBlock.Text = _loc["CapturingKeysLabel"];
		} else {
			CaptureToggleButton.Content = _loc["StartCaptureLabel"];
			UpdateCapturedKeysText();
		}
	}

	private void OnKeyDown(object? sender, KeyEventArgs e) {
		if(!_capturing) {
			return;
		}
		e.Handled = true;
		if(PhysicalKeyMap.ToEvdevCode(e.PhysicalKey) is ushort code && !_capturedCodes.Contains(code)) {
			_capturedCodes.Add(code);
		}
		UpdateCapturedKeysText();
	}

	private void UpdateCapturedKeysText() {
		CapturedKeysTextBlock.Text = _capturedCodes.Count == 0
			? _loc["NoKeysYetLabel"]
			: string.Join(" + ", _capturedCodes.Select(EvdevKeyNames.ToDisplayName));
	}

	private void OnSaveClick(object? sender, RoutedEventArgs e) {
		Bind? bind = BuildBind();
		if(bind is null) {
			return;
		}
		Close(new BindEditorResult(BindEditorOutcome.Saved, [bind]));
	}

	private Bind? BuildBind() {
		if(MouseRadio.IsChecked == true) {
			MouseButton button = MouseRightRadio.IsChecked == true ? MouseButton.Right
				: MouseMiddleRadio.IsChecked == true ? MouseButton.Middle
				: MouseButton.Left;
			return new Bind.Mouse(button);
		}
		if(KeyboardRadio.IsChecked == true) {
			if(_capturedCodes.Count == 0) {
				ValidationTextBlock.Text = _loc["NoKeysYetLabel"];
				return null;
			}
			return new Bind.Keyboard([.. _capturedCodes]);
		}
		if(ActionRadio.IsChecked == true) {
			if(ActionComboBox.SelectedItem is not ComboBoxItem { Tag: BindAction action }) {
				return null;
			}
			return new Bind.Action(action);
		}
		if(CommandRadio.IsChecked == true) {
			if(string.IsNullOrWhiteSpace(CommandTextBox.Text)) {
				return null;
			}
			return new Bind.Command(CommandTextBox.Text);
		}
		return null;
	}

	private void OnResetClick(object? sender, RoutedEventArgs e) => Close(new BindEditorResult(BindEditorOutcome.ResetToDefault, null));

	private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(new BindEditorResult(BindEditorOutcome.Cancelled, null));
}
