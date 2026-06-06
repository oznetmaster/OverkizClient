# OverkizClient

A .NET client library for the **Overkiz** cloud and local REST API, enabling control and monitoring of smart-home gateways and devices from Somfy, Atlantic Cozytouch, Hitachi Hi Kumo, and other Overkiz-compatible ecosystems.

[![NuGet](https://img.shields.io/nuget/v/OverkizClient.svg)](https://www.nuget.org/packages/OverkizClient)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Supported Platforms

| Target Framework | Supported |
|---|---|
| .NET 10 | ✅ |
| .NET Framework 4.7.2 | ✅ |

---

## Supported Gateways / Cloud Servers

| Brand / Server | Auth Method |
|---|---|
| Somfy TaHoma (Europe, America, Oceania) | Somfy OAuth 2.0 |
| Atlantic Cozytouch | CozyTouch JWT |
| Sauter Cozytouch | CozyTouch JWT |
| Thermor Cozytouch | CozyTouch JWT |
| Hitachi Hi Kumo (Asia, Europe, Oceania) | Username / Password |
| Nexity Eugénie | AWS Cognito SRP |
| Flexom by Bouygues | Username / Password |
| Brandt Smart Control | Username / Password |
| Rexel Energeasy Connect | External Bearer Token + Gateway Selection |
| SIMU LiveIn2 | Username / Password |
| Hexaom HexaConnect | Username / Password |
| Ubiwizz by Decelect | Username / Password |
| Somfy Developer Mode (local gateway) | Bearer Token |

Local API (LAN) is supported for Somfy TaHoma and compatible gateways when a developer-mode bearer token is available.

Compatibility note: this .NET library is intended to work across the broader family of Overkiz-compatible gateways, following the design and gateway coverage of the original `python-overkiz-api` project. The current .NET implementation has been validated by the author with a Somfy TaHoma gateway; other Overkiz-compatible gateways and cloud ecosystems are expected to work but have not yet been directly tested here. Recent upstream parity updates include the modern Rexel backend flow, newer Hitachi Hi Kumo `hlrrwifi://` device URL handling, and aligned gateway type/sub-type metadata for newer Energeasy Connect variants.

---

## Installation

```
dotnet add package OverkizClient
```

---

## What's New in 1.1.1.0

- Synced recent upstream `python-overkiz-api` parity updates relevant to this .NET implementation.
- Added aligned gateway `Type` metadata and corrected gateway `SubType` numeric mappings.
- Improved Rexel compatibility by treating gateway `subType: 0` as no specific subtype instead of an unknown subtype.
- Preserved support for the newer Rexel bearer-token-plus-gateway-selection flow and Hitachi Hi Kumo `hlrrwifi://` device URL handling.

---

## Quick Start

### Cloud Connection (Somfy)

```csharp
using OverKizApi;
using OverKizApi.Enums;

await using var client = new OverkizClient(
	username: "your@email.com",
	password: "your-password",
	server: OverkizConst.SupportedServers[Server.SomfyEurope]);

await client.Login();

var devices = await client.GetDevices();
foreach (var device in devices)
	Console.WriteLine($"{device.Label} — {device.DeviceUrl}");
```

### Cloud Connection (Rexel)

Rexel now uses an externally managed bearer token plus explicit gateway selection. Supply the token to the constructor, log in, then discover and select the target gateway before making normal setup/device calls.

```csharp
using OverKizApi;
using OverKizApi.Enums;

await using var client = new OverkizClient(
	username: string.Empty,
	password: string.Empty,
	server: OverkizConst.SupportedServers[Server.Rexel],
	token: "your-rexel-bearer-token");

await client.Login();

var gateways = await client.DiscoverRexelGateways();
client.SelectRexelGateway(gateways[0].GatewayId);

var devices = await client.GetDevices();
```

### Local Connection (LAN)

```csharp
using var httpClient = new HttpClient(OverkizConst.CreateLocalHttpClientHandler());

await using var client = new OverkizClient(
	username: string.Empty,
	password: string.Empty,
	server: OverkizConst.LocalServer("192.168.1.xxx"),
	token: "your-local-bearer-token",
	httpClient: httpClient);

await client.Login();

var devices = await client.GetDevices();
```

### Sending a Command

```csharp
string execId = await client.ExecuteDeviceAction(
	deviceUrl: "io://xxxx-xxxx-xxxx/12345678",
	commands: new[]
	{
		new Command { Name = "open" }
	});
```

### Live Event Streaming

```csharp
await client.RegisterEventListener();

while (true)
{
	var events = await client.FetchEvents();
	foreach (var ev in events)
		Console.WriteLine($"{ev.Name}: {ev.DeviceURL}");

	await Task.Delay(2000);
}

await client.UnregisterEventListener();
```

---

## Test Console

The solution includes `OverKizApi.TestConsole`, an interactive command-line tool for testing API operations — device listing, command execution, live event watching, and Rexel gateway discovery/selection — against both cloud and local connections.

---

## Documentation

Full API documentation is published at **[oznetmaster.github.io/OverkizClient](https://oznetmaster.github.io/OverkizClient/)**.

---

## Acknowledgements

Protocol details and server endpoint information derived from
[python-overkiz-api](https://github.com/iMicknl/python-overkiz-api)
by Mick Vleeshouwer — MIT License.

---

## License

MIT © 2026 Neil Colvin — see [LICENSE](LICENSE).
