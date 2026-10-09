using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MagicRemote.Core;

/// <summary>
/// Ports Setting.cs's static-constructor computation of ipaSendIPDefaut/ipaMaskDefaut/paMacDefaut:
/// the PC's own IPv4 address/subnet mask/MAC on its first active, non-loopback network interface,
/// used to prefill a newly added TV's SendIp/Mask/PcMac fields.
/// </summary>
public static class NetworkDefaults {
	public sealed record PrimaryInterface(string Ip, string Mask, string Mac);

	public static PrimaryInterface GetPrimaryInterface() {
		foreach(NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()) {
			if(ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) {
				continue;
			}
			UnicastIPAddressInformation? unicast = ni.GetIPProperties().UnicastAddresses
				.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
			if(unicast is null) {
				continue;
			}
			string mac = string.Join(":", ni.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
			string mask = unicast.IPv4Mask?.ToString() ?? "255.255.255.0";
			return new PrimaryInterface(unicast.Address.ToString(), mask, mac);
		}
		return new PrimaryInterface(string.Empty, "255.255.255.0", string.Empty);
	}
}
