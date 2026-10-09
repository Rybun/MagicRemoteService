using System.Net;

namespace MagicRemote.Cli;

/// <summary>
/// End-to-end "install this TV's app package and (un)register it as a video input" flow — the
/// counterpart to Setting.cs's TVInstall_Click, assembled here from AppExtractor + WebOsCli's already-
/// ported pieces (which, until now, had no caller wiring them together into that same flow).
/// </summary>
public sealed record TvInstallOptions(
	string AresDeviceName,
	string Version,
	string DisplayName,
	/// <summary>One of WebOsCli.InputList()'s Ids ("HDMI_1".."HDMI_4") — which physical input slot
	/// this TV's app package piggybacks on.</summary>
	string InputId,
	IPAddress SendIp,
	ushort SendPort,
	IPAddress Mask,
	string PcMac,
	int LongClickMs,
	bool InputDirect,
	bool Overlay,
	bool Extend,
	string IconBorderColor
);

public static class TvInstaller {
	/// <summary>Extracts, packages and installs the app, then registers (or, in Overlay mode,
	/// unregisters) it as a video input — mirrors Setting.cs's cbOverlay.Checked branch in
	/// TVInstall_Click exactly (addDevice when unchecked, deleteDevice when checked).</summary>
	public static string Install(TvInstallOptions options, string workDirectory) {
		WebOsCliDeviceInput input = WebOsCli.InputList().FirstOrDefault(i => i.Id == options.InputId)
			?? throw new ArgumentException($"Unknown input slot '{options.InputId}' — expected one of {string.Join(", ", WebOsCli.InputList().Select(i => i.Id))}", nameof(options));

		AppExtractOptions extractOptions = new(
			Version: options.Version,
			Input: input,
			SendIp: options.SendIp,
			SendPort: options.SendPort,
			Mask: options.Mask,
			PcMac: options.PcMac,
			LongClickMs: options.LongClickMs,
			InputDirect: options.InputDirect,
			Overlay: options.Overlay,
			Extend: options.Extend,
			DisplayName: options.DisplayName,
			IconBorderColor: options.IconBorderColor);

		if(Directory.Exists(workDirectory)) {
			Directory.Delete(workDirectory, recursive: true);
		}
		AppExtractor.Extract(workDirectory, extractOptions);

		string appId = $"com.cathwyler.magicremoteservice.{AppExtractor.ComputeAppIdSuffix(options.PcMac, input.AppIdShort)}";
		string appDir = Path.Combine(workDirectory, "MagicRemoteService");
		string serviceDir = Path.Combine(workDirectory, "Service");
		string outDir = Path.Combine(workDirectory, "out");
		Directory.CreateDirectory(outDir);

		WebOsCli.Package(appDir, outDir, serviceDir);
		string packageFile = Directory.GetFiles(outDir, "*.ipk").Single();

		WebOsCli.Install(options.AresDeviceName, packageFile);
		WebOsCli.Launch(options.AresDeviceName, appId);

		// addDevice appears to be create-only on real hardware — reinstalling with a changed name/icon
		// while an entry for this appId already exists silently kept the old metadata. deleteDevice
		// first (ignoring failure: there may be nothing registered yet, e.g. first install) makes this
		// idempotent regardless of whether one was already there.
		try {
			WebOsCli.EimDeleteDevice(options.AresDeviceName, appId);
		} catch(WebOsCliException) {
			// nothing registered yet — fine, addDevice below creates it
		}

		if(!options.Overlay) {
			// Best-effort: points at the icon this same package just installed, so the video-input
			// entry uses "la flechita" instead of webOS's generic default (which is what the original
			// always got, since it always passed pigImage: ""). The exact path webOS resolves an
			// installed app's own files under isn't documented publicly and can vary by firmware —
			// verify visually on real hardware; if it doesn't render, EIM just falls back to its
			// default icon (showPopup/label/description still work regardless).
			string pigImage = $"file:///media/cryptofs/apps/usr/palm/applications/{appId}/largeIcon.png";
			WebOsCli.EimAddDevice(options.AresDeviceName, appId, options.DisplayName, pigImage);
		}

		return appId;
	}

	/// <summary>Unregisters the video input entry and removes the installed package — for when a TV
	/// profile is deleted or its input slot changes (each slot+PC combination is its own distinct
	/// appId — see ComputeAppIdSuffix — so pcMac must match whatever Install was called with).</summary>
	public static void Uninstall(string aresDeviceName, string inputId, string pcMac) {
		WebOsCliDeviceInput input = WebOsCli.InputList().FirstOrDefault(i => i.Id == inputId)
			?? throw new ArgumentException($"Unknown input slot '{inputId}'", nameof(inputId));
		string appId = $"com.cathwyler.magicremoteservice.{AppExtractor.ComputeAppIdSuffix(pcMac, input.AppIdShort)}";
		WebOsCli.EimDeleteDevice(aresDeviceName, appId);
		WebOsCli.InstallRemove(aresDeviceName, appId);
	}
}
