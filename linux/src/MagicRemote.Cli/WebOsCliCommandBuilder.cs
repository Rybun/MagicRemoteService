using System.Text.Json;

namespace MagicRemote.Cli;

/// <summary>
/// Pure argument-list construction for each ares-* command, split out from execution so it's
/// unit-testable without needing the webOS CLI actually installed. Ports WebOSCLI.cs's argument
/// building, but passes each argument as a separate ArgumentList entry instead of manually quoting
/// into one joined string for `cmd /c` — Process's ArgumentList handles quoting/escaping correctly
/// on its own, so the original's ".Replace(\"\\\"\", \"'\")" workaround for surviving cmd.exe's
/// re-parsing isn't needed here; the JSON payload can keep its real double quotes.
/// </summary>
internal static class WebOsCliCommandBuilder {
	public static (string FileName, string[] Args) SetupDeviceList() =>
		("ares-setup-device", ["-F"]);

	public static (string FileName, string[] Args) SetupDeviceAdd(WebOsCliDevice device, string password) =>
		("ares-setup-device", ["-a", device.Name!, "-i", ToDeviceSetJson(device, password)]);

	public static (string FileName, string[] Args) SetupDeviceModify(string deviceName, WebOsCliDevice device, string password) =>
		("ares-setup-device", ["-m", deviceName, "-i", ToDeviceSetJson(device, password)]);

	public static (string FileName, string[] Args) SetupDeviceRemove(string deviceName) =>
		("ares-setup-device", ["-r", deviceName]);

	public static (string FileName, string[] Args) NovacomGetKey(string deviceName) =>
		("ares-novacom", ["-d", deviceName, "-k"]);

	public static (string FileName, string[] Args) NovacomRun(string deviceName, string command) =>
		("ares-novacom", ["-d", deviceName, "-r", command]);

	public static (string FileName, string[] Args) Package(string application, string? outDirectory = null, string? service = null, string? package = null) {
		List<string> args = [application, "-n"];
		if(!string.IsNullOrEmpty(outDirectory)) {
			args.AddRange(["-o", outDirectory]);
		}
		if(!string.IsNullOrEmpty(service)) {
			args.Add(service);
		}
		if(!string.IsNullOrEmpty(package)) {
			args.Add(package);
		}
		return ("ares-package", [.. args]);
	}

	public static (string FileName, string[] Args) Install(string deviceName, string packageFile) =>
		("ares-install", ["-d", deviceName, packageFile]);

	public static (string FileName, string[] Args) InstallRemove(string deviceName, string packageId) =>
		("ares-install", ["-d", deviceName, "--remove", packageId]);

	public static (string FileName, string[] Args) Launch(string deviceName, string applicationId, string? parameter = null) {
		List<string> args = ["-d", deviceName, applicationId];
		if(!string.IsNullOrEmpty(parameter)) {
			args.AddRange(["-p", parameter]);
		}
		return ("ares-launch", [.. args]);
	}

	public static (string FileName, string[] Args) InspectApplication(string deviceName, string applicationId) =>
		("ares-inspect", ["-d", deviceName, "-a", applicationId, "-o"]);

	public static (string FileName, string[] Args) InspectService(string deviceName, string serviceId) =>
		("ares-inspect", ["-d", deviceName, "-s", serviceId, "-o"]);

	public static (string FileName, string[] Args) DeviceInfo(string deviceName) =>
		("ares-device", ["-d", deviceName, "-i"]);

	private static string ToDeviceSetJson(WebOsCliDevice device, string password) {
		WebOsCliDeviceSet set = new() {
			Name = device.Name,
			Description = device.DeviceDetail?.Description,
			Ip = device.DeviceInfo?.Ip,
			Port = device.DeviceInfo?.Port ?? 0,
			User = device.DeviceInfo?.User,
			Password = password,
			PrivateKey = device.DeviceDetail?.PrivateKey,
			Passphrase = device.DeviceDetail?.Passphrase
		};
		return JsonSerializer.Serialize(set);
	}
}
