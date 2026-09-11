# OverKizApi NUnit tests

The 154 tests run without credentials, device settings, files, sockets or real API calls. The public client receives a scripted HTTP handler which checks each outgoing request and returns synthetic responses. Each test owns its client and handler; no server definitions are mutated. Culture changes are restored in `finally` blocks. Even assertions swallowed by best-effort library cleanup are retained and checked by the handler.

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

## Limits and future additions

The handler bypasses actual HTTP transport, cookie persistence, TLS, service throttling and real account permissions. The suite does not validate every vendor's current service protocol. Nexity's Cognito implementation is absent in the library. Local rename synthesis after its 30-second interval needs a controllable clock or a separate timed integration test; this suite deliberately avoids sleeps and private-field reflection. The unused authentication retry pipeline is not treated as a supported automatic-retry contract.

Keep the interactive console for explicitly requested live checks. Any future live fixture should be separately selectable and use private runtime inputs; no real credentials belong in these tests.

Copyright © 2026 Neil Colvin. MIT License; see the repository LICENSE. NUnit and its tooling retain their respective upstream licenses. No upstream test sources have been copied into this suite.
