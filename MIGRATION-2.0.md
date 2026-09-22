# Migrating to OverkizClient 2.0

Version 2.0 changes the public API to keep JSON transport details inside the client. It continues to target .NET Framework 4.7.2 and .NET 10.

## Events

Replace `FetchEventsRaw()` and tuple deconstruction with `FetchEvents()`:

```csharp
IReadOnlyList<EventObject> events = await client.FetchEvents();
```

The raw response string is no longer exposed. Local label-change detection still runs and returns synthesized typed events. Applications can log relevant fields from these models; the client itself does not write logs.

## Pairing

`OpenLocalPairing(string gatewayId)` now returns `Task` instead of `Task<JsonElement?>`. Await it for successful completion:

```csharp
await client.OpenLocalPairing(gatewayId);
```

Remove code that reads the returned payload. Pairing success is established by the HTTP result, with the usual client exception mapping. The response body has no established portable schema and is no longer part of the public contract.

## Execution actions

`Execution.ActionGroup` is now `IReadOnlyList<OverKizApi.Models.Action>` instead of a list of dictionaries. Access `action.DeviceUrl`, `action.Commands`, `command.Name`, and `command.Parameters` directly. Unknown action fields are ignored, as with other domain models.

## Variable values

State values, event values, command parameters, device data properties and firmware-specific failed-command details remain flexible because their schemas vary by device and command. Deserialized values now use ordinary CLR types:

| JSON value | CLR value |
| --- | --- |
| null | null |
| boolean | bool |
| string | string |
| integer fitting Int64 | long |
| other number fitting Decimal | decimal |
| other supported number | double |
| array | List<object?> |
| object | Dictionary<string, object?> |

Replace casts to `JsonElement` or JSON property lookups with typed state accessors or CLR collection access. `State.ValueAsInt`, `ValueAsFloat`, `ValueAsBool` and `ValueAsStr` remain available. Numeric accessors use invariant conversion. Passing CLR primitives and collections as command parameters continues to work.

## Serialization and validation

Domain and request models declare wire names using System.Text.Json attributes. Public domain models support ordinary `JsonSerializer` calls without borrowing the client's private options. Numeric properties retain support for quoted numeric values. Convenience aliases and computed device properties are excluded from serialization; serialized Device, Gateway, Place and Scenario objects therefore contain fewer fields than before. Enum attributes preserve tolerant unknown-name handling, and numeric enum values remain supported. Boolean, object, array and inappropriate null tokens are rejected instead of becoming enum zero.

CozyTouch's JWT exchange rejects failed HTTP responses, missing tokens and non-string JSON. JSON escapes are decoded by the serializer. A legacy `text/plain` response is accepted only when it has a compact three-part JWT shape. This shape check does not verify JWT signatures; authentication is performed by the service.

## Dependencies

log4net is removed because the client had no logging calls. Polly and Polly.Extensions are also removed: their only use was a private retry-pipeline builder that was never called, so removing it does not change request retry behavior. Newtonsoft.Json is not a dependency. Applications that independently use these packages must declare their own dependencies.

System.Text.Json and System.Net.Http.Json now use stable 10.0.12. On .NET Framework, Microsoft.Bcl.AsyncInterfaces and Microsoft.Bcl.Memory also use stable 10.0.12 instead of 11.0 preview builds. The client uses established asynchronous disposal and indexing/range support; no preview-only API is required. These compatibility references remain conditional on net472.
