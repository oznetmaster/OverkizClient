# Changelog

Notable changes to OverkizClient are recorded here. Earlier release history is available in [GitHub Releases](https://github.com/oznetmaster/OverkizClient/releases).

This changelog records shipped features, fixes, compatibility and runtime dependency changes. See [development and validation history](DEVELOPMENT-HISTORY.md) for tests, CI, build tooling and work not yet released.

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