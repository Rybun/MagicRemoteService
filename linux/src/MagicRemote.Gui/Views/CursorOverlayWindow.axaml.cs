using Avalonia.Controls;

namespace MagicRemote.Gui.Views;

/// <summary>
/// Replaces SetSystemCursor: instead of swapping the OS's own cursor theme (not reliably possible
/// for an unprivileged app across X11+Wayland), this draws our own cursor in a transparent
/// always-on-top window positioned at the reported pointer location.
///
/// Known platform limitations (documented, accepted for the MVP — see linux plan notes):
///  - No true click-through: Avalonia has no cross-platform API for OS-level input-transparent
///    windows, so this overlay can intercept clicks meant for the app underneath. Mitigated by
///    keeping it tiny and IsHitTestVisible=false at the Avalonia level (skips hit-testing inside
///    Avalonia's own visual tree; doesn't affect the WM/compositor's delivery of input to this window).
///  - On Wayland compositors without wlr-layer-shell (GNOME, KDE), this is a regular top-level
///    window subject to the window manager's stacking rules, so it can end up covered by fullscreen
///    apps. X11 (including via XWayland) is reliable since it uses override-redirect semantics.
/// </summary>
public partial class CursorOverlayWindow : Window {
	public CursorOverlayWindow() {
		InitializeComponent();
		IsHitTestVisible = false;
	}

	public void MoveTo(double x, double y) {
		Position = new Avalonia.PixelPoint((int)x - (int)(Width / 2), (int)y - (int)(Height / 2));
	}
}
