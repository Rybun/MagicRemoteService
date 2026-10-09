using System.Text.Json;

namespace MagicRemote.Core.Config;

/// <summary>
/// Loads/saves GlobalConfig as JSON, replacing the original's registry-backed persistence
/// (Setting.cs's PCDataSave/DeviceDataSave). Writes are temp-file-then-rename so a crash mid-write
/// never corrupts the config the daemon reads on next start.
/// </summary>
public sealed class ConfigService {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	/// <summary>The real install runs the daemon as its own dedicated system user (see
	/// packaging/systemd's User=magicremote), which is a *different* user from whoever's logged in
	/// and running the GUI — so "the current process's own $HOME/.config" (this method's original,
	/// only behavior) can't be a single shared answer both sides agree on: confirmed live, the
	/// daemon silently loaded an empty config (tvs=0) because $HOME/.config resolved to nothing
	/// useful for a system account with no home directory, and even a *correct* path into the
	/// interactive user's own home would still fail (that directory is mode 0700, readable by no one
	/// else at all). install.sh creates /var/lib/magicremoteservice (group magicremote, mode 2775)
	/// and adds the GUI's user to that group for exactly this reason — prefer it once it exists.
	/// Falls back to the original per-user path when it doesn't (a fresh checkout, `dotnet run`
	/// straight from source, no install.sh ever run) so day-to-day dev work needs no setup at all.</summary>
	public static string DefaultPath {
		get {
			const string sharedDirectory = "/var/lib/magicremoteservice";
			if(Directory.Exists(sharedDirectory)) {
				return System.IO.Path.Combine(sharedDirectory, "config.json");
			}
			return System.IO.Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"magicremoteservice", "config.json");
		}
	}

	public string Path { get; }

	public ConfigService(string? path = null) {
		Path = path ?? DefaultPath;
	}

	public async Task<GlobalConfig> LoadAsync(CancellationToken cancellationToken = default) {
		if(!File.Exists(Path)) {
			return GlobalConfig.Default();
		}
		// ConfigureAwait(false) everywhere in this class: callers include GUI startup code that blocks
		// synchronously on this (GetAwaiter().GetResult()) before Avalonia's dispatcher is pumping —
		// resuming on the captured UI SynchronizationContext there would deadlock the whole app.
		FileStream stream = File.OpenRead(Path);
		try {
			return await JsonSerializer.DeserializeAsync<GlobalConfig>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
				?? GlobalConfig.Default();
		} finally {
			await stream.DisposeAsync().ConfigureAwait(false);
		}
	}

	public async Task SaveAsync(GlobalConfig config, CancellationToken cancellationToken = default) {
		string? directory = System.IO.Path.GetDirectoryName(Path);
		if(!string.IsNullOrEmpty(directory)) {
			Directory.CreateDirectory(directory);
		}
		string tempPath = Path + ".tmp";
		FileStream stream = File.Create(tempPath);
		try {
			await JsonSerializer.SerializeAsync(stream, config, JsonOptions, cancellationToken).ConfigureAwait(false);
		} finally {
			await stream.DisposeAsync().ConfigureAwait(false);
		}
		File.Move(tempPath, Path, overwrite: true);
	}
}
