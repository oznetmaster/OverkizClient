# Development and validation history

See the [product changelog](CHANGELOG.md) for shipped changes. This document preserves test, CI and build history. Dated development entries describe work at that time, not a published product version or completed acceptance. Version headings identify the release alongside which development work was recorded.

## Where changes belong

- Product changelog and product release notes: shipped behavior, API, compatibility, fixes and runtime dependencies. Mention validation briefly when it helps explain a fix.
- This history: test coverage, CI, build tooling, work on pending versions. Split mixed entries so the product effect remains easy to find.
- Testing and workflow guides: current setup and operating instructions.
- Test-only or documentation-only changes do not require a product release.

<!-- development-history -->

## 2.1.2 release preparation - 2026-10-19

The two locally prepared error-classification fixes below are combined in patch 2.1.2. README, changelog and release notes now describe the combined release. AssemblyVersion is updated from the local candidate's 2.1.0 to 2.1.2, matching the existing publication workflow's package/assembly/file version contract. Runtime implementation and dependencies are unchanged from the corrected local candidate. Earlier dated local/unpublished entries remain historical records. Release validation against the official NUnit 5 package exposed a local-candidate assertion mismatch; restoring the awaited Assert.CatchAsync call matches the maintained NUnit 5 test contract. The original failed release-build log is retained.


## Pending quoted local listener-rejection correction - 2026-10-19

A real local gateway returned HTTP 400 with `errorCode: UNSPECIFIED_ERROR` and an `error` string containing `"No registered event listener."`, including the quotation marks and period. The previous exact comparison classified it as a generic error, leaving the stale listener ID unchanged and preventing the existing bounded automatic recovery. A diagnostic registered and unregistered only its own listener; no gateway power cycle or shade command was used. This reproduces a recovery defect, without proving the inaccessible installed driver's exact exception.

The classifier now accepts this exact observed spelling and retains the original message. Five additional offline cases cover message classification, automatic recovery, strict fetch, bounded repeated rejection and near-match rejection. Four focused cases failed before the change; all 331 offline tests pass on each of net472 and net10.0 afterward. The distinct 2.1.2 package is local and unpublished; no hardware validation of the corrected bytes is claimed by these offline results.


## Pending quota-error correction — 2026-10-17

- Map the cloud HTTP 400 message `Too many requests, try again later` to `TooManyRequestsException`, preserving its message regardless of the envelope's `errorCode` (including `QUOTA_EXCEEDED`). Existing HTTP 429 handling is unchanged.
- Three intercepted-HTTP regressions failed before the mapping and pass afterward; all 326 offline tests pass on both net472 and net10.0. No live devices were contacted. Receipts are retained under `artifacts/quota-error-regression`.
- Remove an invalid `await` on the existing NUnit `Assert.CatchAsync` call in the transport-recovery test, which prevented both targets from compiling.
- This is a local, unpublished correction. Upstream reference: [pyoverkiz October 11 message-fallback correction](https://github.com/iMicknl/python-overkiz-api/commit/7cd820a4d7042674c18e1ce5e254bfe6d91d7892).

## [2.1.0](https://github.com/oznetmaster/OverkizClient/releases/tag/v2.1.0) - 2026-09-28

- Add opt-in automatic registration and bounded listener/session recovery through `FetchEvents(bool autoRegister)`, preserving the parameterless explicit-registration contract.
- Complete token refresh before constructing listener requests; serialize listener operations and refresh, avoid duplicate registration during cleanup, and clear rejected or stale listener IDs.
- Invalidate cached setup, devices, gateways and listener state when changing Rexel gateways or establishing a new cloud session.
- Add typed state lookup/fallback helpers, dictionary/list accessors and supported-alias models with stable most-featured-per-type selection.
- Add 51 offline NUnit cases. All 323 pass on both net472 and net10.0 with live tests disabled. The original nine review reproductions also pass on both targets; six had failed before the fixes.
- Experimental multi-site authentication remains outside this change.


## Offline release workflow option - 2026-09-15 (no package release)

- Allow an explicit manual release when local hardware or the self-hosted runner is unavailable, with the reason and exact source recorded in the workflow summary.
- Keep hosted source validation mandatory and preserve all build, test and packaging steps. No runtime, API or package-version changes.

## CI validation - 2026-09-15 (no package release)

- Revalidate the current default-branch source after successful release workflows, including version commits created by GitHub Actions.
- Allow maintainers to configure exact-source, App-specific checks that must pass before publishing through `RELEASE_REQUIRED_CHECKS`; missing, failed or unconfirmed checks block the release.

## [1.2.0](https://github.com/oznetmaster/OverkizClient/releases/tag/v1.2.0) - 2026-09-12

- Six separately selectable local API live tests in category `Live`, disabled unless enabled through private JSON or the explicit NUnit run parameter. The tests reuse an existing token and restore their event-listener resources.

- Shared console/live-test credentials in a private `LiveTestSettings.json`, with legacy console import and per-run enable overrides for embedded runners. Actual credential files are not included in builds or packages.

- Eight offline live-configuration checks, bringing the offline suite to 233 tests, plus six opt-in live cases. CI explicitly excludes live tests.

- 71 response-model regression cases, bringing the offline NUnit suite to 225 tests per target framework.

## [1.1.5](https://github.com/oznetmaster/OverkizClient/releases/tag/v1.1.5) - 2026-09-11

- An NUnit test project in the Visual Studio solution, with 154 offline cases for .NET Framework 4.7.2 and .NET 10.

- Coverage for authentication, setup/devices, state handling, commands, scenarios, schedules, events, Rexel gateway selection, local tokens, pairing, developer mode, error mapping and HTTP client lifetime.

- CI test runs for both frameworks on pushes and pull requests, plus a test gate before NuGet publication.

- A test guide and versioned release notes.

- Describe the distinction between scripted API tests and live service validation. The suite needs no credentials and makes no real network requests.

The public API, target frameworks and production dependencies are unchanged. NUnit and its tooling are test-only dependencies and are not shipped in the library package. No live service or device validation is claimed for this release.
## Local 2.1.1 package preparation - 2026-10-17

Prepared distinct package/file version 2.1.1 for the quota-error mapping; AssemblyVersion remains 2.1.0. This is not a NuGet publication or a claim that hosted release validation ran. Existing source/test changes and prior failed/passed regression originals are preserved.
