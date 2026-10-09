using MagicRemote.Core;
using MagicRemote.Core.Config;
using MagicRemote.Daemon;
using MagicRemote.Daemon.Monitors;
using MagicRemote.Daemon.Power;
using MagicRemote.Daemon.Sessions;
using MagicRemote.InputEngine;
using MagicRemote.Ipc;

bool dryRun = args.Contains("--dry-run");
string[] positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

ILogger logger = new ConsoleLogger();
ConfigService configService = new();
GlobalConfig config = await configService.LoadAsync();
logger.Log($"Config loaded from {configService.Path} (port={config.Port}, tvs={config.Tvs.Count})");

int port = positional.Length > 0 && int.TryParse(positional[0], out int parsedPort) ? parsedPort : config.Port;

IPowerManager powerManager = dryRun ? new DryRunPowerManager(logger) : new SystemctlPowerManager();
if(dryRun) {
	logger.Log("Running with --dry-run: shutdown actions will be logged, not executed");
}
IActiveSessionResolver sessionResolver = new LoginctlActiveSessionResolver();
ISessionActivityMonitor activityMonitor = new LoginctlActivityMonitor(sessionResolver);
using PowerService powerService = new(powerManager, activityMonitor, logger);

MonitorGeometryProvider monitorGeometry = new(sessionResolver, new RuntimeDirMonitorLayoutSource());

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, e) => {
	e.Cancel = true;
	cts.Cancel();
};

ActiveSession? activeSession = await sessionResolver.FindActiveSessionAsync(cts.Token);
string controlSocketPath = activeSession is { } session ? ControlSocketPaths.ForUid(session.Uid) : ControlSocketPaths.ForCurrentUser();
await using ControlSocketServer controlSocket = new(controlSocketPath);
controlSocket.Start(cts.Token);
logger.Log($"Control socket listening at {controlSocketPath}");
CommandRunner commandRunner = new(controlSocket, logger);

BindsEngine bindsEngine = new();
InputClientMessageHandler handler;
try {
	handler = new(bindsEngine, powerService, monitorGeometry, commandRunner, config.Tvs, logger);
} catch(UinputException e) {
	// Without uinput the daemon can't do anything useful. Exit cleanly (systemd retries) instead of
	// crashing with a core dump on every restart, and point at the usual cause.
	logger.Log($"{e.Message}. Check that this user can open /dev/uinput (ls -l /dev/uinput): another udev rule may have overridden its group, see SupplementaryGroups in magicremoteservice.service.");
	return 1;
}
using(handler) {
	await using WebSocketServer server = new(port, handler, logger);
	await server.RunAsync(cts.Token);
}
return 0;
