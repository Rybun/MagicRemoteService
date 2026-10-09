using System.Net;
using System.Reflection;

namespace MagicRemote.Cli;

public sealed record AppExtractOptions(
	string Version,
	WebOsCliDeviceInput Input,
	IPAddress SendIp,
	ushort SendPort,
	IPAddress Mask,
	string PcMac,
	int LongClickMs,
	bool InputDirect,
	bool Overlay,
	bool Extend,
	/// <summary>User-facing label — independent of Input.Name (which is just "HDMI 1".."HDMI 4", the
	/// fixed physical slot being piggybacked on). Shown as the app tile's description, the EIM video
	/// input entry's label, and (once TvInstaller wires it up) the icon's pigImage.</summary>
	string DisplayName,
	/// <summary>#RRGGBB. Recolors the icon's border/shadow (see IconRecolorer) for both the app tile
	/// and the EIM video-input entry.</summary>
	string IconBorderColor
);

/// <summary>
/// Ports Setting.cs's AppExtract: generates the actual webOS app/service files ares-package will
/// bundle, by substituting the PC's configured values into the TV app's JS/JSON templates. Same
/// string-replace approach as the original (looking for the *default* literal and swapping it),
/// just reading from embedded resources here instead of Windows .resx-generated Properties.Resources
/// fields — the templates themselves (TvTemplates/) are untouched webOS/JS assets.
/// </summary>
public static class AppExtractor {
	private const string ResourceRoot = "MagicRemote.Cli.TvTemplates";

	/// <summary>Reads one of the TV app's own icon assets (icon.png/miniIcon.png/largeIcon.png)
	/// unmodified — lets other surfaces (the GUI's own window/tray icon) reuse the exact same shape,
	/// typically recolored via IconRecolorer, so the PC app and the TV app look like the same product.</summary>
	public static byte[] ReadIconTemplate(string fileName) => ReadBytes($"{ResourceRoot}.MagicRemoteService.{fileName}");

	/// <summary>The suffix appended to "com.cathwyler.magicremoteservice." everywhere this app's own
	/// id is built (main.js, appinfo.json, the Service package, and TvInstaller's own Launch/EIM
	/// calls, which must compute this exact same value to address the app they just installed).
	/// Includes the PC's MAC, not just the HDMI slot: the id is otherwise identical for any PC that
	/// installs to the same slot on the same TV, so two different PCs both piggybacking on HDMI_3
	/// would silently overwrite each other's video-input registration — see TvInstaller.Install's
	/// deleteDevice-then-addDevice, which is keyed by this id. This does NOT apply to the *system*
	/// input ids ("com.webos.app.hdmi3") this app matches against to detect that input being
	/// selected — those name real webOS-side inputs, shared across every app that piggybacks on
	/// them, and must stay exactly what webOS itself uses.</summary>
	public static string ComputeAppIdSuffix(string pcMac, string inputAppIdShort) {
		string sanitizedMac = new string(pcMac.Where(char.IsAsciiHexDigit).ToArray()).ToLowerInvariant();
		return $"{sanitizedMac}.{inputAppIdShort}";
	}

	public static void Extract(string outputDirectory, AppExtractOptions options) {
		string appDir = Path.Combine(outputDirectory, "MagicRemoteService");
		string serviceDir = Path.Combine(outputDirectory, "Service");
		string appIdSuffix = ComputeAppIdSuffix(options.PcMac, options.Input.AppIdShort);

		if(Directory.Exists(outputDirectory)) {
			Directory.Delete(outputDirectory, recursive: true);
		}
		Directory.CreateDirectory(appDir);
		Directory.CreateDirectory(Path.Combine(appDir, "webOSTVjs-1.2.13"));
		Directory.CreateDirectory(Path.Combine(appDir, "resources", "fr"));
		Directory.CreateDirectory(Path.Combine(appDir, "resources", "es"));
		Directory.CreateDirectory(serviceDir);

		string mainJs = ReadText($"{ResourceRoot}.MagicRemoteService.main.js")
			.Replace(@"const bInputDirect = true", "const bInputDirect = " + Bool(options.InputDirect))
			.Replace(@"const bOverlay = true", "const bOverlay = " + Bool(options.Overlay))
			.Replace(@"const uiLongClick = 1500", "const uiLongClick = " + options.LongClickMs)
			.Replace(@"const strInputId = ""HDMI""", $@"const strInputId = ""{options.Input.Id}""")
			.Replace(@"const strInputAppId = ""com.webos.app.hdmi""", $@"const strInputAppId = ""com.webos.app.{options.Input.AppIdShort}""")
			.Replace(@"const strInputName = ""HDMI""", $@"const strInputName = ""{options.Input.Name}""")
			.Replace(@"const strInputSource = ""ext://hdmi""", $@"const strInputSource = ""{options.Input.Source}""")
			.Replace(@"const strIP = ""127.0.0.1""", $@"const strIP = ""{options.SendIp}""")
			.Replace(@"const uiPort = 41230", "const uiPort = " + options.SendPort)
			.Replace(@"const strMask = ""255.255.255.0""", $@"const strMask = ""{options.Mask}""")
			.Replace(@"const strMac = ""AA:AA:AA:AA:AA:AA""", $@"const strMac = ""{options.PcMac}""")
			.Replace(@"const strAppId = ""com.cathwyler.magicremoteservice""", $@"const strAppId = ""com.cathwyler.magicremoteservice.{appIdSuffix}""");
		File.WriteAllText(Path.Combine(appDir, "main.js"), mainJs);

		File.WriteAllText(Path.Combine(appDir, "index.html"), ReadText($"{ResourceRoot}.MagicRemoteService.index.html"));

		string appInfo = ReadText($"{ResourceRoot}.MagicRemoteService.appinfo.json")
			.Replace(@"""id"": ""com.cathwyler.magicremoteservice""", $@"""id"": ""com.cathwyler.magicremoteservice.{appIdSuffix}""")
			.Replace(@"""version"": ""1.0.0""", $@"""version"": ""{options.Version}""")
			.Replace(@"""title"": ""MagicRemoteService""", $@"""title"": ""{options.DisplayName}""")
			.Replace(@"""appDescription"": ""HDMI""", $@"""appDescription"": ""{options.DisplayName}""")
			.Replace(@"""defaultWindowType"": ""floating""", $@"""defaultWindowType"": ""{(options.Overlay ? "floating" : "card")}""")
			.Replace(@"""noSplashOnLaunch"": true", @"""noSplashOnLaunch"": " + Bool(options.Overlay));
		File.WriteAllText(Path.Combine(appDir, "appinfo.json"), appInfo);

		// "strAppTittle" — the typo ("Tittle", not "Title") is the template's own, not ours; it's
		// the literal key name main.js's webOS app reads at runtime for the display name shown in
		// the Home Dashboard tile and the EIM video-input entry, so it has to match exactly.
		string appString = ReadText($"{ResourceRoot}.MagicRemoteService.appstring.json")
			.Replace(@"""strAppTittle"": ""MagicRemoteService""", $@"""strAppTittle"": ""{options.DisplayName}""")
			.Replace(@"""strAppDescription"": ""HDMI""", $@"""strAppDescription"": ""{options.DisplayName}""");
		File.WriteAllText(Path.Combine(appDir, "appstring.json"), appString);

		File.WriteAllBytes(Path.Combine(appDir, "icon.png"), IconRecolorer.RecolorBorder(ReadBytes($"{ResourceRoot}.MagicRemoteService.icon.png"), options.IconBorderColor));
		File.WriteAllBytes(Path.Combine(appDir, "miniIcon.png"), IconRecolorer.RecolorBorder(ReadBytes($"{ResourceRoot}.MagicRemoteService.miniIcon.png"), options.IconBorderColor));
		File.WriteAllBytes(Path.Combine(appDir, "largeIcon.png"), IconRecolorer.RecolorBorder(ReadBytes($"{ResourceRoot}.MagicRemoteService.largeIcon.png"), options.IconBorderColor));
		File.WriteAllBytes(Path.Combine(appDir, "cursor.png"), ReadBytes($"{ResourceRoot}.MagicRemoteService.cursor.png"));
		File.WriteAllBytes(Path.Combine(appDir, "MuseoSans-Medium.ttf"), ReadBytes($"{ResourceRoot}.MagicRemoteService.MuseoSans-Medium.ttf"));

		File.WriteAllText(Path.Combine(appDir, "webOSTVjs-1.2.13", "webOSTV-dev.js"), ReadText($"{ResourceRoot}.MagicRemoteService.webOSTVjs_1._2._13.webOSTV-dev.js"));
		File.WriteAllText(Path.Combine(appDir, "webOSTVjs-1.2.13", "webOSTV.js"), ReadText($"{ResourceRoot}.MagicRemoteService.webOSTVjs_1._2._13.webOSTV.js"));

		File.WriteAllText(Path.Combine(appDir, "resources", "fr", "appinfo.json"), ReadText($"{ResourceRoot}.MagicRemoteService.resources.fr.appinfo.json"));
		File.WriteAllText(Path.Combine(appDir, "resources", "fr", "appstring.json"), ReadText($"{ResourceRoot}.MagicRemoteService.resources.fr.appstring.json"));
		File.WriteAllText(Path.Combine(appDir, "resources", "es", "appinfo.json"), ReadText($"{ResourceRoot}.MagicRemoteService.resources.es.appinfo.json"));
		File.WriteAllText(Path.Combine(appDir, "resources", "es", "appstring.json"), ReadText($"{ResourceRoot}.MagicRemoteService.resources.es.appstring.json"));

		string packageJson = ReadText($"{ResourceRoot}.Service.package.json")
			.Replace(@"""name"": ""com.cathwyler.magicremoteservice.service""", $@"""name"": ""com.cathwyler.magicremoteservice.{appIdSuffix}.service""");
		File.WriteAllText(Path.Combine(serviceDir, "package.json"), packageJson);

		string serviceJs = ReadText($"{ResourceRoot}.Service.service.js")
			.Replace(@"var bExtend = true", "var bExtend = " + Bool(options.Extend))
			.Replace(@"var bOverlay = true", "var bOverlay = " + Bool(options.Overlay))
			.Replace(@"var strInputAppId = ""com.webos.app.hdmi""", $@"var strInputAppId = ""com.webos.app.{options.Input.AppIdShort}""")
			.Replace(@"var strAppId = ""com.cathwyler.magicremoteservice""", $@"var strAppId = ""com.cathwyler.magicremoteservice.{appIdSuffix}""");
		File.WriteAllText(Path.Combine(serviceDir, "service.js"), serviceJs);

		string servicesJson = ReadText($"{ResourceRoot}.Service.services.json")
			.Replace(@"""id"": ""com.cathwyler.magicremoteservice.service""", $@"""id"": ""com.cathwyler.magicremoteservice.{appIdSuffix}.service""")
			.Replace(@"""name"": ""com.cathwyler.magicremoteservice.service""", $@"""name"": ""com.cathwyler.magicremoteservice.{appIdSuffix}.service""");
		File.WriteAllText(Path.Combine(serviceDir, "services.json"), servicesJson);
	}

	private static string Bool(bool value) => value ? "true" : "false";

	private static string ReadText(string logicalResourceName) {
		using Stream stream = OpenResource(logicalResourceName);
		using StreamReader reader = new(stream);
		return reader.ReadToEnd();
	}

	private static byte[] ReadBytes(string logicalResourceName) {
		using Stream stream = OpenResource(logicalResourceName);
		using MemoryStream memory = new();
		stream.CopyTo(memory);
		return memory.ToArray();
	}

	private static Stream OpenResource(string logicalResourceName) {
		Assembly assembly = typeof(AppExtractor).Assembly;
		return assembly.GetManifestResourceStream(logicalResourceName)
			?? throw new InvalidOperationException($"Embedded resource not found: {logicalResourceName}");
	}
}
