using MagicRemote.Core;
using MagicRemote.Core.Config;
using MagicRemote.Daemon.Monitors;
using MagicRemote.Daemon.Power;
using MagicRemote.InputEngine;
using MagicRemote.Protocol;

namespace MagicRemote.Daemon;

/// <summary>
/// Turns decoded protocol messages into real input via MagicRemote.InputEngine, MagicRemote.Daemon.Power
/// and MagicRemote.Daemon.Monitors. PositionAbsolute's target display and Key's bind overrides both
/// need "which TvProfile is this session" — there's still no real per-connection TV identity
/// resolution (the protocol has no TV self-identification message, and TvProfile doesn't record the
/// TV's own IP to match session.RemoteEndpoint against), so both fall back to "the single configured
/// TV" when there's exactly one — the common case, and this project's own real setup. With zero or
/// more than one TvProfile configured, both fall back to their old default (display index 0, no
/// overrides) — documented limitation, not silently wrong data. Unicode injection
/// (VirtualKeyboard.TypeUnicode) uses a real X11 connection (X11UnicodeTyper) when one is reachable —
/// correct for any character/layout — falling back to a US-layout-only heuristic (UnicodeKeyMap) only
/// on a fully headless daemon with no DISPLAY.
/// </summary>
internal sealed class InputClientMessageHandler : IClientMessageHandler, IDisposable {
	private readonly VirtualRelativeMouse _relativeMouse = new();
	private readonly VirtualAbsoluteMouse _absoluteMouse = new();
	private readonly VirtualKeyboard _keyboard = new();
	private readonly BindsEngine _bindsEngine;
	private readonly PowerService _powerService;
	private readonly MonitorGeometryProvider _monitorGeometry;
	private readonly CommandRunner _commandRunner;
	private readonly IReadOnlyList<TvProfile> _tvs;
	private readonly ILogger _logger;

	public InputClientMessageHandler(BindsEngine bindsEngine, PowerService powerService, MonitorGeometryProvider monitorGeometry, CommandRunner commandRunner, IReadOnlyList<TvProfile> tvs, ILogger logger) {
		_bindsEngine = bindsEngine;
		_powerService = powerService;
		_monitorGeometry = monitorGeometry;
		_commandRunner = commandRunner;
		_tvs = tvs;
		_logger = logger;
	}

	private TvProfile? ResolveSingleTv() => _tvs.Count == 1 ? _tvs[0] : null;

	public Task HandleAsync(ClientMessage message, ITvSession session, CancellationToken cancellationToken) {
		switch(message) {
			case ClientMessage.PositionRelative m:
				_relativeMouse.Move(m.Dx, m.Dy);
				break;

			case ClientMessage.PositionAbsolute m: {
				TvProfile? tv = ResolveSingleTv();
				return HandlePositionAbsoluteAsync(m, tv?.Display ?? 0, tv?.DisplayName, cancellationToken);
			}

			case ClientMessage.Wheel m:
				_relativeMouse.Scroll(-m.Delta * 3);
				break;

			case ClientMessage.Visible:
				// Cursor overlay swap is Fase 4 (Avalonia GUI); nothing to do at the input-engine level.
				break;

			case ClientMessage.Key m:
				return HandleKeyAsync(m, session, cancellationToken);

			case ClientMessage.Unicode m:
				_keyboard.TypeUnicode(m.Character);
				break;

			case ClientMessage.Shutdown:
				return _powerService.ShutdownNowAsync(cancellationToken);
		}
		return Task.CompletedTask;
	}

	private async Task HandlePositionAbsoluteAsync(ClientMessage.PositionAbsolute message, int displayIndex, string? displayName, CancellationToken cancellationToken) {
		(int x, int y) = await _monitorGeometry.MapToVirtualDesktopAsync(displayIndex, displayName, message.X, message.Y, cancellationToken);
		_absoluteMouse.MoveTo(x, y);
	}

	private Task HandleKeyAsync(ClientMessage.Key message, ITvSession session, CancellationToken cancellationToken) {
		Bind[]? binds = _bindsEngine.Resolve(message.Code, ResolveSingleTv()?.Binds);
		if(binds is null) {
			_logger.Log($"[{session.RemoteEndpoint}] unbound key 0x{message.Code:x4}");
			return Task.CompletedTask;
		}

		foreach(Bind bind in binds) {
			switch(bind) {
				case Bind.Mouse mouse:
					_relativeMouse.Click(mouse.Button, message.Down);
					break;

				case Bind.Keyboard keyboard:
					if(message.Down) {
						foreach(ushort code in keyboard.Codes) {
							_keyboard.KeyEvent(code, down: true);
						}
					} else {
						for(int i = keyboard.Codes.Length - 1; i >= 0; i--) {
							_keyboard.KeyEvent(keyboard.Codes[i], down: false);
						}
					}
					break;

				case Bind.Action action when message.Down:
					return HandleActionAsync(action.Value, session, cancellationToken);

				case Bind.Command command when message.Down:
					return _commandRunner.RunAsync(command.Shell, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private Task HandleActionAsync(BindAction action, ITvSession session, CancellationToken cancellationToken) => action switch {
		BindAction.ToggleKeyboard => session.SendAsync(ServerMessageType.ToggleKeyboard, cancellationToken),
		BindAction.Shutdown => _powerService.ShutdownNowAsync(cancellationToken),
		_ => Task.CompletedTask
	};

	public void Dispose() {
		_relativeMouse.Dispose();
		_absoluteMouse.Dispose();
		_keyboard.Dispose();
	}
}
