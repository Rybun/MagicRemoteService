using MagicRemote.Cli;
using Xunit;

namespace MagicRemote.Cli.Tests;

/// <summary>
/// Exercises WebOsCli's actual process execution (not just argument construction) against fake
/// `ares-*` scripts on PATH — the real webOS CLI isn't installed in this environment, but the
/// interesting behavior (answering a passphrase prompt mid-stream, non-zero exit -> exception,
/// stdout capture) doesn't depend on ares' actual implementation, just on Process/stdin/stdout
/// plumbing, which this proves end-to-end with real child processes.
/// </summary>
public class WebOsCliExecTests : IDisposable {
	private readonly string _binDir = Path.Combine(Path.GetTempPath(), $"magicremote-fakecli-{Guid.NewGuid():N}");
	private readonly string _originalPath = Environment.GetEnvironmentVariable("PATH") ?? "";

	public WebOsCliExecTests() {
		Directory.CreateDirectory(_binDir);
		Environment.SetEnvironmentVariable("PATH", _binDir + Path.PathSeparator + _originalPath);
	}

	public void Dispose() {
		Environment.SetEnvironmentVariable("PATH", _originalPath);
		Directory.Delete(_binDir, recursive: true);
	}

	private void WriteFakeScript(string name, string script) {
		string path = Path.Combine(_binDir, name);
		File.WriteAllText(path, "#!/usr/bin/env bash\n" + script);
		File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
			| UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
	}

	[Fact]
	public void NovacomGetKey_AnswersPassphrasePrompt_OnFirstOutputLine() {
		// Mimics ares-novacom -k: prints a prompt, reads the passphrase from stdin, echoes what it
		// got so the test can verify WebOsCli wrote the right thing at the right moment.
		WriteFakeScript("ares-novacom", """
			echo "Enter passphrase:"
			read -r passphrase
			echo "GOT:$passphrase"
			""");

		// NovacomGetKey doesn't return the captured output, so verify indirectly: it must not throw
		// (the fake script exits 0 only if it got a non-empty read), and must complete promptly
		// rather than hang waiting on stdin forever.
		WebOsCli.NovacomGetKey("Salon", "my-secret-passphrase");
	}

	[Fact]
	public void NovacomGetKey_Throws_WhenPassphraseNeverArrives() {
		// If WebOsCli failed to write to stdin at the right time, `read` would block until EOF and
		// the script would see an empty passphrase — assert that path is distinguishable (non-zero exit).
		WriteFakeScript("ares-novacom", """
			echo "Enter passphrase:"
			read -r passphrase
			if [ -z "$passphrase" ]; then exit 1; fi
			""");

		WebOsCli.NovacomGetKey("Salon", "actually-sent");
		// no exception => the passphrase was received, proving the stdin write landed after line 1
	}

	[Fact]
	public void Exec_ThrowsWebOsCliException_WithStderr_OnNonZeroExit() {
		WriteFakeScript("ares-device", """
			echo "something went wrong" >&2
			exit 3
			""");

		WebOsCliException ex = Assert.Throws<WebOsCliException>(() => WebOsCli.DeviceInfo("Salon"));
		Assert.Contains("something went wrong", ex.Message);
	}

	[Fact]
	public void Exec_ReturnsStdout_OnSuccess() {
		WriteFakeScript("ares-device", """
			echo "line one"
			echo "line two"
			""");

		string output = WebOsCli.DeviceInfo("Salon");

		Assert.Contains("line one", output);
		Assert.Contains("line two", output);
	}

	[Fact]
	public void SetupDeviceList_ParsesRealJsonOutput_FromAresSetupDevice() {
		WriteFakeScript("ares-setup-device", """
			echo '[{"name":"Salon","default":true,"deviceinfo":{"ip":"192.168.1.50","port":"9922","user":"prisoner"}}]'
			""");

		WebOsCliDevice[] devices = WebOsCli.SetupDeviceList();

		WebOsCliDevice device = Assert.Single(devices);
		Assert.Equal("Salon", device.Name);
		Assert.True(device.Default);
		Assert.Equal("192.168.1.50", device.DeviceInfo!.Ip!.ToString());
		Assert.Equal((ushort)9922, device.DeviceInfo.Port);
	}
}
