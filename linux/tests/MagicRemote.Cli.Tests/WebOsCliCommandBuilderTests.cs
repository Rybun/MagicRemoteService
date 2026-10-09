using System.Net;
using MagicRemote.Cli;
using Xunit;

namespace MagicRemote.Cli.Tests;

public class WebOsCliCommandBuilderTests {
	[Fact]
	public void SetupDeviceList_BuildsDashF() {
		var (fileName, args) = WebOsCliCommandBuilder.SetupDeviceList();
		Assert.Equal("ares-setup-device", fileName);
		Assert.Equal(["-F"], args);
	}

	[Fact]
	public void SetupDeviceRemove_PassesNameAsSeparateArgument() {
		var (fileName, args) = WebOsCliCommandBuilder.SetupDeviceRemove("Living Room TV");
		Assert.Equal("ares-setup-device", fileName);
		Assert.Equal(["-r", "Living Room TV"], args);
	}

	[Fact]
	public void SetupDeviceAdd_EmbedsDeviceInfoAsJson_WithRealQuotes() {
		WebOsCliDevice device = new() {
			Name = "Salon",
			DeviceInfo = new WebOsCliDeviceInfo { Ip = IPAddress.Parse("192.168.1.50"), Port = 9922, User = "prisoner" },
			DeviceDetail = new WebOsCliDeviceDetail { Description = "desc", PrivateKey = "key.pem", Passphrase = "secret" }
		};

		var (fileName, args) = WebOsCliCommandBuilder.SetupDeviceAdd(device, "pw");

		Assert.Equal("ares-setup-device", fileName);
		Assert.Equal("-a", args[0]);
		Assert.Equal("Salon", args[1]);
		Assert.Equal("-i", args[2]);
		// Real JSON, real double quotes — no cmd.exe-survival quote mangling needed with ArgumentList.
		Assert.Contains("\"host\":\"192.168.1.50\"", args[3]);
		Assert.Contains("\"password\":\"pw\"", args[3]);
	}

	[Fact]
	public void NovacomGetKey_BuildsDashDDashK() {
		var (fileName, args) = WebOsCliCommandBuilder.NovacomGetKey("Salon");
		Assert.Equal("ares-novacom", fileName);
		Assert.Equal(["-d", "Salon", "-k"], args);
	}

	[Fact]
	public void Package_IncludesOutputServiceAndPackage_WhenProvided() {
		var (fileName, args) = WebOsCliCommandBuilder.Package("app", "out", "service", "pkg.ipk");
		Assert.Equal("ares-package", fileName);
		Assert.Equal(["app", "-n", "-o", "out", "service", "pkg.ipk"], args);
	}

	[Fact]
	public void Package_OmitsOptionalArguments_WhenNotProvided() {
		var (fileName, args) = WebOsCliCommandBuilder.Package("app");
		Assert.Equal(["app", "-n"], args);
	}

	[Fact]
	public void Launch_IncludesParameter_WhenProvided() {
		var (fileName, args) = WebOsCliCommandBuilder.Launch("Salon", "com.app.id", "{\"foo\":1}");
		Assert.Equal(["-d", "Salon", "com.app.id", "-p", "{\"foo\":1}"], args);
	}

	[Fact]
	public void InstallRemove_UsesDashDashRemove() {
		var (fileName, args) = WebOsCliCommandBuilder.InstallRemove("Salon", "com.app.id");
		Assert.Equal(["-d", "Salon", "--remove", "com.app.id"], args);
	}
}
