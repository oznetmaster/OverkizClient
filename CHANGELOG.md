# Changelog

Notable changes to OverkizClient are recorded here. Earlier release history is available in [GitHub Releases](https://github.com/oznetmaster/OverkizClient/releases).

## [1.1.5](https://github.com/oznetmaster/OverkizClient/releases/tag/v1.1.5) - 2026-09-11

### Fixed

- Recognize JSON `true` in successful standard and CozyTouch login responses.
- Send the Basic authorization header with the actual CozyTouch OAuth token request.
- Convert JSON-backed numeric and boolean state values through the typed state accessors, using invariant numeric conversion.
- Exclude typed state convenience properties from JSON serialization so incompatible accessors cannot interrupt state collection serialization.
- Map the event payload's `deviceURL` field to `EventObject.DeviceUrl`.
- Map unrecognized enum names to the enum's actual `Unknown` member, including enums whose `Unknown` value is not zero.
- Raise `OverkizException` for missing listener, execution and local-token response IDs instead of leaking `KeyNotFoundException`.

### Added

- An NUnit test project in the Visual Studio solution, with 154 offline cases for .NET Framework 4.7.2 and .NET 10.
- Coverage for authentication, setup/devices, state handling, commands, scenarios, schedules, events, Rexel gateway selection, local tokens, pairing, developer mode, error mapping and HTTP client lifetime.
- CI test runs for both frameworks on pushes and pull requests, plus a test gate before NuGet publication.
- A test guide and versioned release notes.

### Documentation

- Clarify that Nexity Cognito authentication remains an unimplemented stub.
- Describe the distinction between scripted API tests and live service validation. The suite needs no credentials and makes no real network requests.

### Compatibility

The public API, target frameworks and production dependencies are unchanged. NUnit and its tooling are test-only dependencies and are not shipped in the library package. No live service or device validation is claimed for this release.

[Compare with v1.1.4](https://github.com/oznetmaster/OverkizClient/compare/v1.1.4...v1.1.5).
