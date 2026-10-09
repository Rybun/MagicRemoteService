using Avalonia;
using System;

namespace MagicRemote.Gui;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) {
        // For autostart: a login-time launch shouldn't pop the settings window up over whatever
        // the user is doing — it should just sit in the tray, same as if they'd opened it and
        // clicked away. Read before Avalonia's own startup so App can act on it as soon as the
        // main window exists (see App.OnFrameworkInitializationCompleted).
        App.StartInBackground = args.Contains("--background") || args.Contains("--tray");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    //
    // UseDBusMenu/UseDBusFilePicker are disabled because Avalonia's D-Bus integration doesn't
    // gracefully handle a session bus that's missing the optional freedesktop services it probes
    // for (global app menu, StatusNotifierWatcher, portal file picker) — a widely reported upstream
    // issue (e.g. AvaloniaUI/Avalonia#17616, #11494) where a ServiceUnknown/disconnect on that probe
    // crashes the whole process instead of falling back quietly. Verified live: without this, the
    // GUI crashed a few seconds after launch with an unhandled TaskCanceledException inside
    // Tmds.DBus.Protocol.DBusConnection.Observer, reproducibly, with zero user interaction.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions { UseDBusMenu = false, UseDBusFilePicker = false })
            .WithInterFont()
            .LogToTrace();
}
