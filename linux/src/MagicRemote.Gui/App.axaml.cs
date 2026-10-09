using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MagicRemote.Core.Config;
using MagicRemote.Gui.Localization;
using MagicRemote.Gui.Monitors;
using MagicRemote.Gui.Views;
using MagicRemote.Ipc;

namespace MagicRemote.Gui;

public partial class App : Application
{
    private readonly CancellationTokenSource _controlSocketCts = new();
    private bool _reallyQuit;
    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;

    /// <summary>Set by Program.Main from the "--background"/"--tray" command-line flag, before
    /// Avalonia's own startup — read here so a login-time autostart launch goes straight to the
    /// tray instead of popping the settings window up. See packaging/systemd's GUI unit, which
    /// launches with this flag.</summary>
    public static bool StartInBackground { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Closing the main window (the X button) must not end the process — this is a
            // tray-resident app, same as the original's NotifyIcon-based WinForms app. Only the
            // tray menu's "Quit" really exits (see SetupTrayIcon).
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            MainWindow mainWindow = new();
            mainWindow.Closing += (_, e) =>
            {
                // Only swallow a genuine user X-click (CloseReason.WindowClosing) — cancelling
                // unconditionally used to also swallow OSShutdown/ApplicationShutdown, which is the
                // close request the desktop's own session manager sends to every top-level window
                // during logout/shutdown. Refusing that made GNOME/KDE report this app as blocking
                // shutdown, forcing the user to quit it from the tray by hand before the PC could
                // power off. Any reason other than a plain user click is let through for real.
                if (!_reallyQuit && e.CloseReason == WindowCloseReason.WindowClosing)
                {
                    e.Cancel = true;
                    mainWindow.Hide();
                }
            };
            desktop.MainWindow = mainWindow;
            SetupTrayIcon(desktop, mainWindow);
            _ = new AvaloniaMonitorPublisher(mainWindow);
            SetupControlSocketClient(desktop);

            // Avalonia's classic desktop lifetime calls MainWindow.Show() itself right after this
            // method returns, unconditionally — a synchronous Hide() here gets clobbered by that.
            // Posting it instead queues the hide for the next dispatcher iteration, which always
            // runs after that framework-driven Show(), so it wins. There's a single-frame flash in
            // principle; in practice the window doesn't visibly appear at normal launch speed.
            if (StartInBackground)
            {
                Dispatcher.UIThread.Post(() => mainWindow.Hide());
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Known limitation, documented rather than silently missed: GNOME's default Shell has no
    // StatusNotifierItem/AppIndicator host built in (needs a third-party extension), so this tray
    // icon simply won't be visible there. It works out of the box on KDE Plasma, XFCE, Cinnamon, etc.
    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, MainWindow mainWindow)
    {
        _mainWindow = mainWindow;

        // Blocking read is fine here — this runs once, synchronously, before the window is even
        // shown, same moment Setting.cs's Load() would run on the original. Reading the color here
        // (rather than waiting for MainWindowViewModel's own async LoadAsync) means the icon is right
        // from the very first frame instead of flashing a default before swapping.
        string initialColor = new ConfigService().LoadAsync().GetAwaiter().GetResult().IconBorderColor;
        var icon = new WindowIcon(TrayIconFactory.CreateIcon(initialColor));
        mainWindow.Icon = icon;

        ILocalizationService loc = new LocalizationService();

        var openItem = new NativeMenuItem(loc["TrayOpenLabel"]);
        openItem.Click += (_, _) =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        };

        var quitItem = new NativeMenuItem(loc["TrayQuitLabel"]);
        quitItem.Click += (_, _) =>
        {
            _reallyQuit = true;
            desktop.Shutdown();
        };

        var menu = new NativeMenu { openItem, quitItem };

        var trayIcon = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "MagicRemoteService",
            Menu = menu
        };
        trayIcon.Clicked += (_, _) =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        };

        _trayIcon = trayIcon;
        TrayIcon.SetIcons(this, new TrayIcons { trayIcon });
    }

    /// <summary>Regenerates the window/tray icon from the current border color — called live as the
    /// user picks a color in the global settings ColorPicker, so they see the same icon they're about
    /// to install on the TV without needing to restart the app.</summary>
    public void RefreshIcon(string iconBorderColorHex)
    {
        if (_mainWindow is null || _trayIcon is null)
        {
            return;
        }
        var icon = new WindowIcon(TrayIconFactory.CreateIcon(iconBorderColorHex));
        _mainWindow.Icon = icon;
        _trayIcon.Icon = icon;
    }

    // Runs Bind.Command commands the daemon forwards here (it has no graphical session of its own
    // to launch apps / show notifications in — see CommandRunner on the daemon side).
    private void SetupControlSocketClient(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var client = new ControlSocketClient(ControlSocketPaths.ForCurrentUser());
        client.RunCommandReceived += message => Dispatcher.UIThread.Post(() => RunCommand(message.Shell));
        client.StartConnecting(_controlSocketCts.Token);
        desktop.Exit += (_, _) => _controlSocketCts.Cancel();
    }

    private static void RunCommand(string shell)
    {
        try
        {
            ProcessStartInfo startInfo = new("sh") { UseShellExecute = false };
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(shell);
            Process.Start(startInfo);
        }
        catch
        {
            // best effort, matches the original's fire-and-forget Process.Start for command binds
        }
    }
}
