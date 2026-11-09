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
using OverKizApi.Enums;

// Obtain username and password from your application's credential storage.
await using var client = new OverkizClient(username, password,
    OverkizConst.SupportedServers[Server.SomfyEurope]);
if (!await client.Login(registerEventListener: false))
    throw new InvalidOperationException("Cloud login was rejected.");

// Discover devices
var devices = await client.GetDevices();
foreach (var device in devices)
	Console.WriteLine(device.Label);

```

For Sauter or Brandt appliances, see [Smart Control authentication](../guides/smart-control.md). For local bearer-token connections and command examples, see the repository README. Command names depend on the discovered device's supported commands; discovery itself does not operate a device.

After login, independent reads can overlap. Authentication, listener changes, setup cache replacement, gateway selection and commands are sequenced. Await client disposal before releasing an application-owned HTTP transport. Returned models are mutable snapshots; coordinate application mutations yourself.

## Next Pages

- [Overview](overview.md)
- [Key Types](key-types.md)
- [API Reference](../api/toc.yml)
