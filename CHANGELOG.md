# Changelog

Notable changes to OverkizClient are recorded here. Earlier release history is available in [GitHub Releases](https://github.com/oznetmaster/OverkizClient/releases).

This changelog records shipped features, fixes, compatibility and runtime dependency changes. See [development and validation history](DEVELOPMENT-HISTORY.md) for tests, CI, build tooling and work not yet released.

## [2.1.0](https://github.com/oznetmaster/OverkizClient/releases/tag/v2.1.0) - 2026-09-28

- Add opt-in listener registration and bounded recovery through `FetchEvents(bool autoRegister)`, retaining the existing parameterless behavior.
- Fix listener IDs used after token refresh, duplicate registration, stale IDs after failed replacement and cleanup, and overlapping event operations.
- Invalidate cached setup, devices, gateways and listener state when switching Rexel gateways or establishing a new cloud session.
- Add typed state lookup/fallback methods, dictionary/list accessors and supported-alias helpers with most-featured-per-type selection.

Existing public method signatures, target frameworks and runtime dependencies are unchanged. See the [release notes](release-notes/v2.1.0.md).

## [2.0.0](https://github.com/oznetmaster/OverkizClient/releases/tag/v2.0.0) - 2026-09-22

- Return typed events and execution actions; make pairing completion-only and remove the raw event-response API.
- Represent variable JSON values as CLR primitives and collections. Declare serialization contracts on model attributes and exclude computed properties.
- Validate CozyTouch JWT responses and reject incompatible enum token types.
- Remove unused log4net and Polly dependencies; retain no Newtonsoft.Json dependency.
- Update JSON and .NET Framework compatibility packages to stable 10.0.12, replacing preview references.

This is a breaking release. See the [migration guide](MIGRATION-2.0.md) and [release notes](release-notes/v2.0.0.md).

## [1.2.0](https://github.com/oznetmaster/OverkizClient/releases/tag/v1.2.0) - 2026-09-12

### Changed

- Use dedicated internal models for login, Somfy/CozyTouch OAuth, listener registration, execution/scheduling IDs, local-token generation/activation and API errors.

- Model alternative device-state wrappers with optional `states`, `deviceStates` and `values` collections, choosing the first non-null collection in that order. Empty objects and null collections still return no states.

- Preserve public method signatures and existing public domain models. Unknown response fields are ignored; open-ended state values, command parameters and the unstructured pairing payload retain flexible handling.

- Reject blank response identifiers and invalid known-field JSON types instead of accepting arbitrary string conversions. Missing required identifiers raise `OverkizException`; incompatible property types raise `JsonException`. Unparseable API error envelopes fall back to the HTTP status exception.

- Validate required OAuth token fields before storing authentication state or requesting the CozyTouch JWT. Somfy expiry is required; its refresh token is optional. Invalid Somfy refresh responses preserve the existing token state.

- Skip response deserialization for successful operations whose bodies are not consumed, while retaining HTTP error mapping.

### Added

- Documentation of optional response fields, typed transport models and remaining flexible payloads.

## [1.1.5](https://github.com/oznetmaster/OverkizClient/releases/tag/v1.1.5) - 2026-09-11

### Fixed

- Recognize JSON `true` in successful standard and CozyTouch login responses.

- Send the Basic authorization header with the actual CozyTouch OAuth token request.

- Convert JSON-backed numeric and boolean state values through the typed state accessors, using invariant numeric conversion.

- Exclude typed state convenience properties from JSON serialization so incompatible accessors cannot interrupt state collection serialization.

- Map the event payload's `deviceURL` field to `EventObject.DeviceUrl`.

- Map unrecognized enum names to the enum's actual `Unknown` member, including enums whose `Unknown` value is not zero.

- Raise `OverkizException` for missing listener, execution and local-token response IDs instead of leaking `KeyNotFoundException`.

### Documentation

- Clarify that Nexity Cognito authentication remains an unimplemented stub.

### Compatibility

The public API, target frameworks and production dependencies are unchanged.

[Compare with v1.1.4](https://github.com/oznetmaster/OverkizClient/compare/v1.1.4...v1.1.5).