using System.Net;
using MagicRemote.Cli;
using Xunit;

namespace MagicRemote.Cli.Tests;

public class AppExtractorTests : IDisposable {
	private readonly string _outputDir = Path.Combine(Path.GetTempPath(), $"magicremote-appextract-test-{Guid.NewGuid():N}");

	private static readonly AppExtractOptions SampleOptions = new(
		Version: "1.2.3",
		Input: new WebOsCliDeviceInput { Id = "HDMI_2", Name = "HDMI 2", Source = "ext://hdmi:2", AppIdShort = "hdmi2" },
		SendIp: IPAddress.Parse("192.168.1.42"),
		SendPort: 41230,
		Mask: IPAddress.Parse("255.255.255.0"),
		PcMac: "AA:BB:CC:DD:EE:FF",
		LongClickMs: 1500,
		InputDirect: true,
		Overlay: false,
		Extend: false,
		DisplayName: "Living Room",
		IconBorderColor: "#00FF00"
	);

	public void Dispose() {
		if(Directory.Exists(_outputDir)) {
			Directory.Delete(_outputDir, recursive: true);
		}
	}

	[Fact]
	public void Extract_WritesExpectedDirectoryStructure() {
		AppExtractor.Extract(_outputDir, SampleOptions);

		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "main.js")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "index.html")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "appinfo.json")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "appstring.json")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "icon.png")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "miniIcon.png")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "largeIcon.png")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "cursor.png")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "MuseoSans-Medium.ttf")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "webOSTVjs-1.2.13", "webOSTV.js")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "webOSTVjs-1.2.13", "webOSTV-dev.js")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "resources", "es", "appinfo.json")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "MagicRemoteService", "resources", "fr", "appstring.json")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "Service", "package.json")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "Service", "service.js")));
		Assert.True(File.Exists(Path.Combine(_outputDir, "Service", "services.json")));
	}

	[Fact]
	public void Extract_SubstitutesNetworkAndInputValues_IntoMainJs() {
		AppExtractor.Extract(_outputDir, SampleOptions);

		string mainJs = File.ReadAllText(Path.Combine(_outputDir, "MagicRemoteService", "main.js"));

		Assert.Contains(@"const strIP = ""192.168.1.42""", mainJs);
		Assert.Contains(@"const uiPort = 41230", mainJs);
		Assert.Contains(@"const strMask = ""255.255.255.0""", mainJs);
		Assert.Contains(@"const strMac = ""AA:BB:CC:DD:EE:FF""", mainJs);
		Assert.Contains(@"const strInputId = ""HDMI_2""", mainJs);
		Assert.Contains(@"const strInputAppId = ""com.webos.app.hdmi2""", mainJs); // system input id: no MAC, matches webOS's own naming
		Assert.Contains(@"const strAppId = ""com.cathwyler.magicremoteservice.aabbccddeeff.hdmi2""", mainJs);
		Assert.Contains(@"const bInputDirect = true", mainJs);
		Assert.Contains(@"const bOverlay = false", mainJs);
		Assert.DoesNotContain("127.0.0.1", mainJs); // the template default must be fully replaced
	}

	[Fact]
	public void Extract_SubstitutesVersionAndWindowType_IntoAppInfo() {
		AppExtractor.Extract(_outputDir, SampleOptions);

		string appInfo = File.ReadAllText(Path.Combine(_outputDir, "MagicRemoteService", "appinfo.json"));

		Assert.Contains(@"""id"": ""com.cathwyler.magicremoteservice.aabbccddeeff.hdmi2""", appInfo);
		Assert.Contains(@"""version"": ""1.2.3""", appInfo);
		Assert.Contains(@"""title"": ""Living Room""", appInfo);
		Assert.Contains(@"""appDescription"": ""Living Room""", appInfo);
		Assert.Contains(@"""defaultWindowType"": ""card""", appInfo); // Overlay:false in SampleOptions
		Assert.DoesNotContain("MagicRemoteService", appInfo); // the template's own display-name default must be fully replaced
	}

	[Fact]
	public void Extract_SubstitutesDisplayName_IntoAppString() {
		AppExtractor.Extract(_outputDir, SampleOptions);

		string appString = File.ReadAllText(Path.Combine(_outputDir, "MagicRemoteService", "appstring.json"));

		Assert.Contains(@"""strAppTittle"": ""Living Room""", appString);
		Assert.Contains(@"""strAppDescription"": ""Living Room""", appString);
	}

	[Fact]
	public void Extract_SubstitutesAppId_IntoServiceFiles() {
		AppExtractor.Extract(_outputDir, SampleOptions);

		string packageJson = File.ReadAllText(Path.Combine(_outputDir, "Service", "package.json"));
		string serviceJs = File.ReadAllText(Path.Combine(_outputDir, "Service", "service.js"));
		string servicesJson = File.ReadAllText(Path.Combine(_outputDir, "Service", "services.json"));

		Assert.Contains(@"""name"": ""com.cathwyler.magicremoteservice.aabbccddeeff.hdmi2.service""", packageJson);
		Assert.Contains(@"var strAppId = ""com.cathwyler.magicremoteservice.aabbccddeeff.hdmi2""", serviceJs);
		Assert.Contains(@"""id"": ""com.cathwyler.magicremoteservice.aabbccddeeff.hdmi2.service""", servicesJson);
	}

	[Fact]
	public void Extract_CopiesNonIconBinaryAssets_ByteForByte() {
		AppExtractor.Extract(_outputDir, SampleOptions);

		byte[] extracted = File.ReadAllBytes(Path.Combine(_outputDir, "MagicRemoteService", "MuseoSans-Medium.ttf"));
		byte[] original = File.ReadAllBytes(Path.Combine(
			AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "MagicRemote.Cli", "TvTemplates", "MagicRemoteService", "MuseoSans-Medium.ttf"));

		Assert.Equal(original, extracted);
	}

	[Fact]
	public void Extract_RecolorsIconBorder_ToRequestedColor() {
		AppExtractor.Extract(_outputDir, SampleOptions);

		byte[] extracted = File.ReadAllBytes(Path.Combine(_outputDir, "MagicRemoteService", "largeIcon.png"));
		byte[] original = File.ReadAllBytes(Path.Combine(
			AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "MagicRemote.Cli", "TvTemplates", "MagicRemoteService", "largeIcon.png"));

		// The recolored icon is a different image (border pixels changed) but the same size/shape —
		// verified precisely by IconRecolorerTests; here just confirm Extract doesn't fall back to a
		// verbatim byte copy for icon files the way it does for genuinely untouched assets.
		Assert.NotEqual(original, extracted);
	}

	[Theory]
	[InlineData("AA:BB:CC:DD:EE:FF", "hdmi2", "aabbccddeeff.hdmi2")]
	[InlineData("b4:2e:99:3e:50:4a", "hdmi3", "b42e993e504a.hdmi3")]
	public void ComputeAppIdSuffix_SanitizesMac_RegardlessOfCaseOrSeparators(string pcMac, string inputAppIdShort, string expected) {
		Assert.Equal(expected, AppExtractor.ComputeAppIdSuffix(pcMac, inputAppIdShort));
	}

	[Fact]
	public void ComputeAppIdSuffix_DiffersBetweenPcs_ForTheSameHdmiSlot() {
		string suffixA = AppExtractor.ComputeAppIdSuffix("AA:AA:AA:AA:AA:AA", "hdmi3");
		string suffixB = AppExtractor.ComputeAppIdSuffix("BB:BB:BB:BB:BB:BB", "hdmi3");

		Assert.NotEqual(suffixA, suffixB);
	}

	[Fact]
	public void Extract_IsIdempotent_WhenOutputDirectoryAlreadyExists() {
		AppExtractor.Extract(_outputDir, SampleOptions);
		AppExtractor.Extract(_outputDir, SampleOptions with { Version = "9.9.9" });

		string appInfo = File.ReadAllText(Path.Combine(_outputDir, "MagicRemoteService", "appinfo.json"));
		Assert.Contains(@"""version"": ""9.9.9""", appInfo);
	}
}
