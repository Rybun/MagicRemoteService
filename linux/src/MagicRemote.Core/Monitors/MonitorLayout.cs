namespace MagicRemote.Core.Monitors;

/// <summary>
/// One physical monitor's geometry, as published by whatever runs inside the graphical session (the
/// Fase 4 Avalonia GUI, or in the meantime a lightweight xrandr-based publisher — see
/// linux/src/MagicRemote.Daemon/Monitors). JSON contract, written to
/// $XDG_RUNTIME_DIR/magicremoteservice/monitors.json as a JSON array of these.
/// </summary>
public sealed record MonitorLayout(int Index, string Name, int X, int Y, int Width, int Height, bool Primary);
