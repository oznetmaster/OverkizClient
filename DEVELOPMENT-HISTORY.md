# Development and validation history

See the [product changelog](CHANGELOG.md) for shipped changes. This document preserves test, CI and build history. Dated development entries describe work at that time, not a published product version or completed acceptance. Version headings identify the release alongside which development work was recorded.

## Where changes belong

- Product changelog and product release notes: shipped behavior, API, compatibility, fixes and runtime dependencies. Mention validation briefly when it helps explain a fix.
- This history: test coverage, CI, build tooling, work on pending versions. Split mixed entries so the product effect remains easy to find.
- Testing and workflow guides: current setup and operating instructions.
- Test-only or documentation-only changes do not require a product release.

<!-- development-history -->

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