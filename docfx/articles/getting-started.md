# Getting Started

OverkizClient is a .NET library for communicating with Overkiz-compatible smart-home gateways such as Somfy TaHoma, Atlantic Cozytouch, and Hitachi Hi Kumo.

## Installation

Install from NuGet:

```
dotnet add package OverkizClient
```

## Basic Usage

```csharp
using OverKizApi;

// Cloud (OAuth2) connection
await using var client = new OverkizClient(server: OverkizServer.Somfy_Europe);
await client.LoginAsync("user@example.com", "password");

// Discover devices
var devices = await client.GetDevicesAsync();
foreach (var device in devices)
	Console.WriteLine(device.Label);

// Execute a command
await client.ExecuteCommandAsync(device.DeviceURL, "open");
```

## Next Pages

- [Overview](overview.md)
- [Key Types](key-types.md)
- [API Reference](../api/toc.yml)
