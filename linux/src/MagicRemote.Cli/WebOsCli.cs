using System.Diagnostics;
using System.Text.Json;

namespace MagicRemote.Cli;

/// <summary>
/// Ports WebOSCLI.cs: thin wrappers around the webOS CLI (`ares-*` tools, installed separately by
/// the user — see README) that were invoked via `cmd /c ares-command args` on Windows. Here they're
/// invoked directly, no shell wrapper needed on Linux either.
/// </summary>
public static class WebOsCli {
	public static IReadOnlyList<WebOsCliDeviceInput> InputList() => [
		new WebOsCliDeviceInput { Id = "HDMI_1", Name = "HDMI 1", Source = "ext://hdmi:1", AppIdShort = "hdmi1" },
		new WebOsCliDeviceInput { Id = "HDMI_2", Name = "HDMI 2", Source = "ext://hdmi:2", AppIdShort = "hdmi2" },
		new WebOsCliDeviceInput { Id = "HDMI_3", Name = "HDMI 3", Source = "ext://hdmi:3", AppIdShort = "hdmi3" },
		new WebOsCliDeviceInput { Id = "HDMI_4", Name = "HDMI 4", Source = "ext://hdmi:4", AppIdShort = "hdmi4" },
	];

	public static WebOsCliDevice[] SetupDeviceList() {
		var (fileName, args) = WebOsCliCommandBuilder.SetupDeviceList();
		string output = Exec(fileName, args);
		return JsonSerializer.Deserialize<WebOsCliDevice[]>(output) ?? [];
	}

	public static void SetupDeviceAdd(WebOsCliDevice device, string password) {
		var (fileName, args) = WebOsCliCommandBuilder.SetupDeviceAdd(device, password);
		Exec(fileName, args);
	}

	public static void SetupDeviceModify(string deviceName, WebOsCliDevice device, string password) {
		var (fileName, args) = WebOsCliCommandBuilder.SetupDeviceModify(deviceName, device, password);
		Exec(fileName, args);
	}

	public static void SetupDeviceRemove(string deviceName) {
		var (fileName, args) = WebOsCliCommandBuilder.SetupDeviceRemove(deviceName);
		Exec(fileName, args);
	}

	/// <summary>ares-novacom -k prompts for the SSH key passphrase on its first output line — the
	/// original answered it by writing to stdin right after that line, ported as-is below.</summary>
	public static void NovacomGetKey(string deviceName, string passphrase) {
		var (fileName, args) = WebOsCliCommandBuilder.NovacomGetKey(deviceName);
		Exec(fileName, args, stdinByOutputLine: new Dictionary<int, string> { [1] = passphrase });
	}

	public static void NovacomRun(string deviceName, string command) {
		var (fileName, args) = WebOsCliCommandBuilder.NovacomRun(deviceName, command);
		Exec(fileName, args);
	}

	/// <summary>Registers the installed app as a video input entry (alongside HDMI etc. in the TV's
	/// input switcher), via the same `luna://com.webos.service.eim/addDevice` call Setting.cs's
	/// TVInstall_Click issues over novacom when the "Overlay" mode is off. The original always hardcoded
	/// "label" to the literal "MagicRemoteService" (that's the name it shows in the TV's input list) —
	/// here it's the TV's own display name instead, so it's actually the thing the GUI's "Nombre" field
	/// controls. pigImage should be a path the TV can resolve to an image (e.g. a file already
	/// installed as part of the app package) — pass null to fall back to webOS's own default input
	/// icon, like the original always did.</summary>
	public static void EimAddDevice(string deviceName, string appId, string label, string? pigImage = null) {
		string json = $@"{{""appId"":""{appId}"", ""pigImage"": ""{pigImage}"", ""mvpdIcon"": """", ""type"": ""MVPD_IP"", ""showPopup"": true, ""label"": ""{label}"", ""description"": ""{label}""}}";
		NovacomRun(deviceName, $"luna-send-pub -n 1 'luna://com.webos.service.eim/addDevice' '{json}'");
	}

	/// <summary>Unregisters the video input entry — used both for "Overlay" mode (which never wants
	/// one registered) and to clean up before re-registering with a changed name/icon/id.</summary>
	public static void EimDeleteDevice(string deviceName, string appId) {
		string json = $@"{{""appId"":""{appId}""}}";
		NovacomRun(deviceName, $"luna-send-pub -n 1 'luna://com.webos.service.eim/deleteDevice' '{json}'");
	}

	public static void Package(string application, string? outDirectory = null, string? service = null, string? package = null) {
		var (fileName, args) = WebOsCliCommandBuilder.Package(application, outDirectory, service, package);
		Exec(fileName, args);
	}

	public static void Install(string deviceName, string packageFile) {
		var (fileName, args) = WebOsCliCommandBuilder.Install(deviceName, packageFile);
		Exec(fileName, args);
	}

	public static void InstallRemove(string deviceName, string packageId) {
		var (fileName, args) = WebOsCliCommandBuilder.InstallRemove(deviceName, packageId);
		Exec(fileName, args);
	}

	public static void Launch(string deviceName, string applicationId, string? parameter = null) {
		var (fileName, args) = WebOsCliCommandBuilder.Launch(deviceName, applicationId, parameter);
		Exec(fileName, args);
	}

	public static void InspectApplication(string deviceName, string applicationId) {
		var (fileName, args) = WebOsCliCommandBuilder.InspectApplication(deviceName, applicationId);
		Exec(fileName, args);
	}

	public static void InspectService(string deviceName, string serviceId) {
		var (fileName, args) = WebOsCliCommandBuilder.InspectService(deviceName, serviceId);
		Exec(fileName, args);
	}

	public static string DeviceInfo(string deviceName) {
		var (fileName, args) = WebOsCliCommandBuilder.DeviceInfo(deviceName);
		return Exec(fileName, args);
	}

	/// <summary>
	/// Many real webOS TVs (this project's own included) still run an SSH server old enough (webOS's
	/// starfish/dropbear stack, reporting itself as "OpenSSH_6.1") to only offer the "ssh-rsa"
	/// (SHA-1) host key algorithm — an algorithm distro-level OpenSSL 3 crypto policies (Fedora/RHEL's
	/// DEFAULT included) refuse to verify at all, failing every single ares-* SSH call against the TV
	/// with "Handshake failed: signature verification failed" regardless of passphrase correctness.
	///
	/// Fixed by pointing OPENSSL_CONF at a minimal config that skips the system-wide crypto-policy
	/// .include and just re-enables SHA-1 signature verification (the same effect as the system
	/// `update-crypto-policies --set DEFAULT:SHA1`), but scoped to only these ares-* child processes —
	/// the rest of the system (browsers, TLS, everything else) keeps the stricter distro default.
	/// </summary>
	private static readonly Lazy<string> OpenSslConfigPath = new(() => {
		string path = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"magicremoteservice", "openssl-legacy-sha1.cnf");
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, """
			openssl_conf = default_conf

			[default_conf]
			alg_section = evp_properties

			[evp_properties]
			rh-allow-sha1-signatures = yes

			""");
		return path;
	});

	private static string Exec(string fileName, IReadOnlyList<string> args, IReadOnlyDictionary<int, string>? stdinByOutputLine = null, string? workingDirectory = null) {
		ProcessStartInfo startInfo = new(fileName) {
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};
		startInfo.Environment["OPENSSL_CONF"] = OpenSslConfigPath.Value;
		foreach(string arg in args) {
			startInfo.ArgumentList.Add(arg);
		}
		if(!string.IsNullOrEmpty(workingDirectory)) {
			startInfo.WorkingDirectory = workingDirectory;
		}

		using Process process = new() { StartInfo = startInfo };
		System.Text.StringBuilder output = new();
		System.Text.StringBuilder error = new();
		int outputLine = 0;

		process.ErrorDataReceived += (_, e) => {
			if(e.Data is not null) {
				error.Append(e.Data);
			}
		};
		process.OutputDataReceived += (_, e) => {
			if(e.Data is not null) {
				output.Append(e.Data).Append(Environment.NewLine);
				outputLine++;
				if(stdinByOutputLine is not null && stdinByOutputLine.TryGetValue(outputLine, out string? reply)) {
					process.StandardInput.WriteLine(reply);
				}
			}
		};

		process.Start();
		process.BeginErrorReadLine();
		process.BeginOutputReadLine();
		process.WaitForExit();

		if(process.ExitCode != 0) {
			throw new WebOsCliException(error.ToString());
		}
		return output.ToString();
	}
}
