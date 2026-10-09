namespace MagicRemote.Core;

/// <summary>Minimal logging seam so the daemon can write to journald (stdout, under systemd) or the
/// console interchangeably without pulling in a full logging framework this early.</summary>
public interface ILogger {
	void Log(string message);
}

public sealed class ConsoleLogger : ILogger {
	public void Log(string message) => Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}");
}
