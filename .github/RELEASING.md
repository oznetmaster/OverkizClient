# Concurrent releases and completed validation

## NuGet Trusted Publishing

Product publication uses the pinned `NuGet/login` action to exchange the GitHub OIDC token for a short-lived NuGet credential immediately before upload. It does not read or fall back to the repository's expired `NUGET_API_KEY` secret. Only the publishing job has `id-token: write` permission.

The NuGet policy must name package owner `oznetmaster`, GitHub repository `oznetmaster/OverkizClient`, workflow file `publish-nuget.yml`, no GitHub environment, and the exact package ID `OverkizClient`. Scope it to **Push only new package versions**. Do not grant wildcard-package, new-package or unlisting access. Creating the policy is a separate account authorization; changing the workflow alone does not grant publishing rights.

The built package is retained as the `release-package` workflow artifact before upload. Manual and tag-triggered publication both create the corresponding GitHub release from that same package. Existing tags must refer to the current release source; duplicate NuGet uploads fail rather than masking uncertain publication state.

## Concurrent validation and publication

Independent tasks may initiate releases at the same time. All publication workflows in this repository share `public-release` with `cancel-in-progress: false` and `queue: max`. GitHub retains up to 100 queued requests per group; overflow/cancellation is not a successful release. Different repositories publish independently. Do not invoke another workflow requiring the same publication lock while holding it.

On manual release workflows, desktop and processor evidence can be reused independently. Both options default to false:

- `skip_desktop_checks` requires `desktop_skip_reason`, identifying the tested source, completion time, frameworks, pass counts and report location.
- `skip_hardware_checks` requires `hardware_skip_reason`, identifying processor results, source, completion time, cleanup and reservation release. It also supports an explicitly recorded unavailable-hardware exception.
- `validation_commit` selects recent successful CI for a full ancestor SHA. The shared policy verifies the source relationship, approved documentation/release-automation-only differences, exact check identity and completion within 24 hours. Runtime, tests, projects, dependencies and other build/validation inputs require new evidence.

The pinned shared action validates and records each selection. Manual reasons are operator attestations, not automatic verification of private reports. Tag-triggered releases cannot select manual overrides. Reuse does not establish a new test pass. Selecting one override retains the other configured checks.

Existing source test commands remain available and run by default. Explicit desktop reuse avoids the repeated source suites where the release workflow invokes them. Builds, fresh package consumers, package execution/integrity, metadata, API, license and distribution-version checks remain mandatory where already configured. Discovery may still run to create a current inventory for new-package verification. DevTools and NUnit release builders accept `SkipDesktopTests` only with nonempty single-line `DesktopValidationEvidence`; their new-package checks remain outside that branch.

Use an immutable source commit, isolated checkout and run-scoped artifacts. Recheck version/tag/package ownership under the publication lock; never overwrite a different release's assets or infer success merely from a duplicate package response. A partially published version requires provenance inspection before resuming. Publication serialization does not replace processor reservations.

Processor ownership uses the processor-resident shared lease protocol across cooperating hosts. Availability observation is advisory; atomic acquisition is decisive. Uncertain cleanup retains the lease. A busy runner or processor must not be reported as a test pass. Central hardware routing still needs qualified capability-based workers, shared version allocation and independent processor admission before removing its current global serialization. VM availability alone does not establish runner/tool qualification.

Published changelog headings use inline version links to the verified GitHub release. Keep Unreleased as plain text, preserve entry dates and text, and leave versions without a corresponding GitHub release as plain headings.
