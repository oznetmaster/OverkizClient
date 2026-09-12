# OverKizApi NUnit tests

The 233 offline tests require no credentials, devices, sockets or real API calls. Configuration tests use isolated temporary files and remove them afterward. Six additional live tests are separately selectable and disabled by default. In the offline API tests, the public client receives a scripted HTTP handler which checks each outgoing request and returns synthetic responses. Each test owns its client and handler; no server definitions are mutated. Culture changes are restored in `finally` blocks. Even assertions swallowed by best-effort library cleanup are retained and checked by the handler.

## Running

Open the repository solution in Visual Studio and run `OverKizApi.Tests` in Test Explorer. From a terminal:

```powershell
dotnet test OverKizApi.Tests/OverKizApi.Tests.csproj -c Release -f net472
dotnet test OverKizApi.Tests/OverKizApi.Tests.csproj -c Release -f net10.0
```

Official NUnit 4.6.1, NUnit3TestAdapter 6.3.0 and NUnit.Analyzers 4.14.0 are used. The project is not packable. Both targets compile with `LangVersion=latest`; net472 includes the necessary compiler-attribute shims.

## Behavior covered

| Fixture | Scope |
| --- | --- |
| AuthenticationTests | Standard/local login, Somfy form exchange and expired-token refresh, CozyTouch Basic/Bearer/JWT sequence, credential failures and Nexity's unsupported contract |
| SetupAndDeviceTests | Setup cache/refresh, encoded device URLs, wrapped state responses, nested places and setup options |
| ExecutionAndConfigurationTests | Command payloads, scenario execution/scheduling/cancellation, history, local-token lifecycle, pairing and developer mode |
| EventAndLifecycleTests | Event wire fields/raw JSON, listener lifecycle, best-effort cleanup and local label-poll throttling |
| RexelTests | Multi-home discovery, scoped headers, single/multiple gateway selection, and separation of gateway header ID from external serial number |
| ModelTests | Device URL parsing, tolerant gateway/enum handling, typed states, invariant numeric conversion and state collection serialization |
| ErrorHandlingTests | Structured error-to-exception mapping and HTTP errors across read/write operations |
| ResponseModelTests | Optional and unknown fields, required IDs/tokens, invalid JSON property types, alternative state wrappers, error fallback and failed-refresh state preservation |
| LiveTestConfigurationTests | Explicit enable/disable precedence, shared credential-file updates and private input-directory selection (offline) |
| LiveLocalApiTests | Six opt-in checks against a real local gateway, described below |
| ClientResourceTests | Owned/external HTTP client lifetime, PATCH payload/cancellation and local endpoint construction |

## Regressions found while establishing the suite

The first 150-case run produced 24 failures on each framework. The corresponding fixes are in the library source:

- Successful standard and CozyTouch logins now recognize a JSON `true` value.
- CozyTouch sends the authorization header on the actual OAuth request.
- Numeric/boolean state accessors accept `JsonElement` values as well as direct CLR values.
- Typed state convenience properties are excluded from JSON output, preventing unrelated accessors from throwing during serialization.
- Event `deviceURL` maps to the public `DeviceUrl` property.
- Unrecognized named enum values map to the actual `Unknown` member, even when it is not zero.
- Missing listener/execution/token response IDs raise the documented `OverkizException` instead of leaking a dictionary-key exception.

These are library behavior fixes, not only a test-framework change. No package version or NuGet release is produced by building or running this suite.

## Response model coverage

The response-model refactor adds 71 cases through the public client, bringing the suite to 225 tests. It covers all six operations returning listener/execution/trigger/token/request identifiers, optional login and OAuth fields, unknown server additions, malformed known fields, optional state wrappers and structured error fallback. A rejected Somfy refresh is also checked to ensure it does not replace the current access token. The tests exercise actual deserialization through the HTTP boundary rather than constructing the internal response classes directly.

## Live local API fixture

The six tests in `LiveLocalApiTests` use category `Live` and run sequentially. Each test has its own client and a configurable HTTP timeout (default 15 seconds per request). They read gateway/device data and exercise registration/fetch/unregistration of their own event listener. They do not create or delete tokens, refresh cloud OAuth tokens, issue device commands, refresh physical states or modify device configuration. If there are no devices, the device-specific test is skipped.

### Shared console credentials

The shared file is `%LOCALAPPDATA%/OverkizClient/LiveTestSettings.json` on Windows. Its shape is illustrated in `LiveTestSettings.example.json`. The console's saved local address becomes `gatewayHost` and its saved local token becomes `token`. The console imports its legacy local entry when first opened in local mode and updates the shared file whenever local credentials are saved. This preserves the existing `enabled` and `timeoutSeconds` values. New files default to `enabled: false`.

Never commit the real file. It normally lives outside the checkout; `LiveTestSettings.json` and `*.local.runsettings` are also excluded in this checkout's `.git/info/exclude`. No real settings are included in the projects, NuGet package or test outputs. Only the placeholder example is tracked. Other checkouts should add those private names to their own `.git/info/exclude` if storing them inside the checkout.

### Local opt-in

Set `enabled: true` in the shared JSON file, then select the Live fixture in Visual Studio or run:

```powershell
dotnet test OverKizApi.Tests/OverKizApi.Tests.csproj -c Release -f net472 --filter "TestCategory=Live"
```

Alternatively, explicitly pass `--settings OverKizApi.Tests/LiveTests.runsettings.example`. That file sets the NUnit parameter `EnableLiveTests=true` for the current run without modifying the saved JSON flag. A parameter value of `false` always disables live tests. Merely selecting a fixture or discovering tests does not bypass the gate. The same commands work with `-f net10.0`.

### Embedded runner inputs

Supply the shared `LiveTestSettings.json` as a private test input. Set `TestDataDirectory` to its containing directory and `EnableLiveTests=true` when the user selects the separate Live suite. The JSON may retain `enabled: false`; the explicit run parameter overrides it in memory. The ordinary suite should exclude category `Live` and pass `EnableLiveTests=false`.

When `TestDataDirectory` is supplied, only that directory's JSON file is considered. An explicitly enabled run with missing credentials fails configuration rather than silently using another machine's credentials. The fixture and shared settings code have no platform-specific SDK references.

### Validation boundary

Offline configuration tests prove that live tests default to disabled, an explicit false wins, run overrides do not mutate the saved flag, and console credential updates preserve local preferences. Actual gateway behavior requires an explicitly enabled live run; a passing offline suite does not establish that result. The local HTTP handler uses the same gateway certificate handling as the console.

## Limits and future additions

The handler bypasses actual HTTP transport, cookie persistence, TLS, service throttling and real account permissions. The suite does not validate every vendor's current service protocol. Nexity's Cognito implementation is absent in the library. Local rename synthesis after its 30-second interval needs a controllable clock or a separate timed integration test; this suite deliberately avoids sleeps and private-field reflection. The unused authentication retry pipeline is not treated as a supported automatic-retry contract.

The live fixture covers the local API only. The interactive console remains available for cloud and other manual checks. Any future device-changing live tests must capture and restore the original device state and require explicit selection.

Copyright © 2026 Neil Colvin. MIT License; see the repository LICENSE. NUnit and its tooling retain their respective upstream licenses. No upstream test sources have been copied into this suite.
