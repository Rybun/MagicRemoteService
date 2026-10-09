using System.Net;
using System.Text.Json.Serialization;

namespace MagicRemote.Cli;

/// <summary>Ports WebOSCLI.cs's JSON model classes verbatim — pure data/serialization, nothing
/// Windows-specific about any of it.</summary>
public sealed class WebOsCliDeviceInput {
	public required string Id { get; init; }
	public required string Name { get; init; }
	public required string Source { get; init; }
	public required string AppIdShort { get; init; }
}

public sealed class WebOsCliDeviceInfo {
	[JsonConverter(typeof(IPAddressJsonConverter))]
	[JsonPropertyName("ip")]
	public IPAddress? Ip { get; init; }

	[JsonConverter(typeof(UShortJsonConverter))]
	[JsonPropertyName("port")]
	public ushort Port { get; init; }

	[JsonPropertyName("user")]
	public string? User { get; init; }
}

public sealed class WebOsCliDeviceDetail {
	[JsonPropertyName("platform")]
	public string? Platform { get; init; }

	[JsonPropertyName("privatekey")]
	public string? PrivateKey { get; init; }

	[JsonPropertyName("passphrase")]
	public string? Passphrase { get; init; }

	[JsonPropertyName("description")]
	public string? Description { get; init; }
}

public sealed class WebOsCliDevice {
	[JsonPropertyName("profile")]
	public string? Profile { get; init; }

	[JsonPropertyName("name")]
	public string? Name { get; init; }

	[JsonPropertyName("default")]
	public bool Default { get; init; }

	[JsonPropertyName("deviceinfo")]
	public WebOsCliDeviceInfo? DeviceInfo { get; init; }

	[JsonPropertyName("connection")]
	public string[]? Connection { get; init; }

	[JsonPropertyName("details")]
	public WebOsCliDeviceDetail? DeviceDetail { get; init; }
}

internal sealed class WebOsCliDeviceSet {
	[JsonPropertyName("name")]
	public string? Name { get; init; }

	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonConverter(typeof(IPAddressJsonConverter))]
	[JsonPropertyName("host")]
	public IPAddress? Ip { get; init; }

	[JsonConverter(typeof(UShortJsonConverter))]
	[JsonPropertyName("port")]
	public ushort Port { get; init; }

	[JsonPropertyName("username")]
	public string? User { get; init; }

	[JsonPropertyName("password")]
	public string? Password { get; init; }

	[JsonPropertyName("privatekey")]
	public string? PrivateKey { get; init; }

	[JsonPropertyName("passphrase")]
	public string? Passphrase { get; init; }
}

internal sealed class IPAddressJsonConverter : JsonConverter<IPAddress> {
	public override IPAddress Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
		IPAddress.Parse(reader.GetString()!);

	public override void Write(System.Text.Json.Utf8JsonWriter writer, IPAddress value, System.Text.Json.JsonSerializerOptions options) =>
		writer.WriteStringValue(value.ToString());
}

internal sealed class UShortJsonConverter : JsonConverter<ushort> {
	public override ushort Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
		ushort.Parse(reader.GetString()!);

	public override void Write(System.Text.Json.Utf8JsonWriter writer, ushort value, System.Text.Json.JsonSerializerOptions options) =>
		writer.WriteStringValue(value.ToString());
}
