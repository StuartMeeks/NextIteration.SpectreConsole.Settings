# Changelog

All notable changes to `NextIteration.SpectreConsole.Settings` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Fixed

- Resolved every open CodeQL code-scanning alert from the `security-and-quality`
  pack with a genuine code change rather than a suppression (no consumer-visible
  behaviour changes):
  - `cs/path-combine`: switched every `Path.Combine` to `Path.Join` (one in
    `ServiceCollectionExtensions`, the rest in test infrastructure). `Path.Join`
    concatenates unconditionally, so it cannot silently discard the base
    directory if a later segment ever looks rooted.
  - `cs/catch-of-all-exceptions`: the two best-effort cleanup catches
    (`AtomicFile.TryDelete`, `SettingsStore.TryBackupCorruptFile`) and the test
    `TempDir.Dispose` now catch only the `IOException`/`UnauthorizedAccessException`
    they expect, so an unexpected exception surfaces instead of being swallowed.
    The three intentionally broad catches — the fire-and-forget persistence
    safety net (`SettingsBase`) and the two `settings` command boundaries — keep
    routing all operational failures to their handler but now let a process-fatal
    `OutOfMemoryException` propagate rather than mislabel it.
  - `cs/linq/missed-where`: `SettingsStore.ResetInstanceToDefaults` filters the
    settable properties with `.Where(...)` instead of an `if` inside the loop.
  - `cs/missed-using-statement`: the debounce task now scopes its
    `CancellationTokenSource` with a `using` block instead of a manual
    `finally`-dispose, preserving the existing (idempotent) disposal semantics.
  - `cs/local-not-disposed`: the test CLI harness now disposes its `TestConsole`.

## [1.0.0] — 2026-08-21

First stable release. This is a milestone, not a rewrite: the public API and the
shipped target frameworks (`net8.0` and `net10.0`) are unchanged from 0.3.0. What
1.0.0 adds is a commitment — from here the public surface follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html) strictly, so a breaking
change to it requires a 2.0.0. The work in this cycle is the test-infrastructure,
repository maintenance and standards alignment below; the only consumer-visible
runtime change is a servicing bump to the `net10.0`
`Microsoft.Extensions.DependencyInjection.Abstractions` floor.

### Changed

- Upgraded the test suite from xUnit.net v2 (`xunit` 2.9.3) to xUnit.net v3
  (`xunit.v3` 4.0.0 — the v3 API line, at package version 4). v3 test projects
  are self-hosting executables built on Microsoft.Testing.Platform rather than
  libraries loaded by VSTest, so the test project is now
  `<OutputType>Exe</OutputType>` and the VSTest-only
  `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` packages are gone. A
  root `global.json` opts `dotnet test` into the Microsoft.Testing.Platform
  runner — the .NET 10 SDK refuses to drive an MTP test project through the
  legacy VSTest path without it.
- The test project now multi-targets `net8.0;net10.0`, mirroring the library.
  Previously it targeted `net10.0` only, so its `ProjectReference` always
  resolved to the library's `net10.0` build and the shipped `net8.0` assembly was
  compile-checked and API-validated but never executed by a test. Both assemblies
  now run the full suite. CI installs the .NET 8 runtime alongside .NET 10 to
  match.
- Reworked the timing-sensitive tests to wait for an observable effect instead of
  sleeping for a fixed interval. Automatic persistence is debounced and
  fire-and-forget, so nothing signals completion and the tests slept 400ms and
  hoped; on a loaded machine the write could overrun that and
  `Automatic_Mutation_PersistsAndRoundTrips` would fail spuriously (reproduced on
  the pre-existing suite, so this predates the xUnit upgrade). Positive
  assertions now poll via a new `Wait` test helper — returning as soon as the
  effect lands, failing with a named expectation after 10s. Negative assertions
  ("no write happened") keep an explicit quiet window, since there is nothing to
  wait for and too short a wait can only mask a bug, never fail a correct build.
- Replaced the coverage collector `coverlet.collector` (a VSTest data collector,
  inert under Microsoft.Testing.Platform) with
  `Microsoft.Testing.Extensions.CodeCoverage`. Collect coverage with
  `dotnet test --coverage`.
- Tests now thread `TestContext.Current.CancellationToken` through every
  cancellable async call (xUnit analyzer rule xUnit1051), so a cancelled or
  timed-out run stops promptly instead of waiting out its I/O and delays.
- Updated NuGet dependencies to their latest stable versions: the test-only
  `Microsoft.Extensions.DependencyInjection` 10.0.11 and the build-time-only
  `Microsoft.SourceLink.GitHub` 10.0.400. The `net10.0`
  `Microsoft.Extensions.DependencyInjection.Abstractions` floor moves to 10.0.11;
  the `net8.0` floor stays at 8.0.2 (its LTS line's latest).
  `Spectre.Console`/`Spectre.Console.Testing` 0.57.2 and `Spectre.Console.Cli`
  0.55.0 are already current.
- Bumped the CI workflow's actions to their current majors (`checkout` v7,
  `setup-dotnet` v6, `upload-artifact` v7, `download-artifact` v8) and dropped
  the now-redundant `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24` override — those majors
  already run on Node 24.
- Removed the `release` job that cut the GitHub release automatically from
  `CHANGELOG.md`. `ci.yml`'s non-comment content is now identical to the canonical
  template apart from the tag glob — the same as every other repo in the estate — and
  `STANDARD.md` §3.0.1 now requires that and checks it. GitHub releases for this package
  are cut by hand, as they already were everywhere else. **No consumer impact:** the job
  only ever ran on a `v*` tag, after the package had already been pushed to nuget.org.
  Publishing is unchanged.
- Adopted the canonical CI shape from
  [NextIteration.Standards](https://github.com/StuartMeeks/NextIteration.Standards)
  (`STANDARD.md` section 3). `build` and `test` are now separate jobs, `test`
  runs a three-platform matrix (Linux, Windows, macOS) rather than Linux alone,
  and an aggregating `ci` gate is the single required status check — so the
  matrix can be reshaped without touching branch protection. Coverage is now
  actually collected in CI (`dotnet test -- --coverage`); the collector was
  referenced but never invoked. Workflows gained `concurrency`,
  `timeout-minutes`, a least-privilege `permissions` block and a NuGet cache.

- `global.json` now pins the SDK (`10.0.100`, `rollForward: latestFeature`) as well
  as the test runner. Without a pin a contributor on an older SDK gets different
  analyzer results from CI, and `TreatWarningsAsErrors` turns that into a build that
  fails for them and passes for everyone else.
- Adopted the canonical `.gitignore` and `.editorconfig`. The `.editorconfig` change
  scopes the private-field naming rule to instance fields — a `const` is a field, so
  the rule previously demanded `_nonceSize` for `private const int NonceSize`.
- Enabled `EnforceCodeStyleInBuild` and adopted the revised canonical
  `.editorconfig` (`STANDARD.md` 1.2.1 and 5.2). The IDE style analyzers now run
  in-build under `TreatWarningsAsErrors`, so the ordained house style — braces
  always, block-scoped namespaces, `var` throughout, explicit accessibility,
  collection expressions, the naming ruleset, and `CS1591` public-API docs — is
  gated by the build rather than merely documented. The `.editorconfig` is now a
  deliberate allow-list of named gates rather than a blanket severity, so a style
  rule a future SDK ships never auto-fails the build. The clause had been blocked
  on exactly that blanket-severity problem. Bringing the code to green under the
  flag converted two collection initialisations in `SettingsStore` to collection
  expressions and the test project's file-scoped namespaces to block-scoped — no
  runtime behaviour changed, and all 64 tests (32 × `net8.0`/`net10.0`) still pass.
  The test project also adds `IDE0005` to its `NoWarn` (`STANDARD.md` 2.7): that
  rule only runs with `GenerateDocumentationFile` on, which the test project turns
  off, so gating it would otherwise force the doc file back on.
- Added a CodeQL `query-filters` block excluding `cs/unmanaged-code` and
  `cs/call-to-unmanaged-code` (`STANDARD.md` 4.4). This library has no P/Invoke, so
  the filter matches nothing here; it is carried to keep `codeql.yml` aligned with
  the canonical template (`STANDARD.md` 3.0.1), which non-native repos share
  harmlessly.
- Moved the build properties shared by both projects out of the individual csprojs
  and into the root `Directory.Build.props`. Both projects previously restated the
  same fifteen properties, which is fifteen chances for one copy to drift silently.
  Each csproj now carries only what is specific to it. `Directory.Packages.props`
  also gains `CentralPackageVersionOverrideEnabled=false`, so a stray inline
  `Version=` on a `PackageReference` is now an `NU1008` restore failure instead of
  being silently ignored in favour of the central version. Verified to be a
  no-op for consumers: packing at the same commit from a clean `obj/` before and
  after produces a byte-identical `.nuspec`, byte-identical `net8.0` and `net10.0`
  assemblies and XML docs, and identical `README.md`, icon and package metadata —
  the only difference anywhere in the package is the `.psmdcp` filename, which NuGet
  regenerates on every pack.

### Added

- CodeQL code scanning (`security-and-quality` query pack), weekly plus on every
  push and pull request. Analysis excludes `**/obj/**` and `**/bin/**`, so generated
  and compiled output — the xUnit auto-generated entry point among it — raises no
  findings.
- `SECURITY.md`, `CONTRIBUTING.md`, a pull request template, and a root `CLAUDE.md`.
  `SECURITY.md` states the scope this library does and does not claim — settings are
  stored as plain-text JSON and are explicitly not a place for secrets.
- Dependabot for NuGet and GitHub Actions, with minor and patch updates grouped
  and auto-merged behind CI, and majors left open for review. Major updates to
  `Microsoft.Extensions.DependencyInjection.Abstractions` are suppressed, because
  its floor is deliberately per-target-framework and an 8.x -> 10.x bump is never
  mergeable here.

### Fixed

- `AtomicFile` raised a sharing violation on Windows when two writers replaced
  the same settings file concurrently, or when the destination was held open by
  another handle. `File.Move(overwrite: true)` is `rename(2)` on POSIX, which
  tolerates both, but `MoveFileEx` on Windows, which does not. Windows now uses
  `File.Replace` (`ReplaceFile`) with a short retry. Present since 0.1.0 and
  never caught, because the concurrent-writer test had only ever run on Linux;
  identified while adding the Windows leg to the matrix and fixed in the same
  change, so the branch stays green.
- README claimed the package targets `net10.0` only; it has shipped `net8.0` and
  `net10.0` assemblies since 0.2.0.

## [0.3.0] — 2026-07-24

### Changed

- Runtime-aligned Microsoft platform dependencies now carry per-target-framework
  version floors instead of a single shared floor. In a library a
  `PackageReference` version is a minimum NuGet forces on every consumer
  (lowest-applicable-version resolution), so the previous single `10.0.x` floor on
  `Microsoft.Extensions.DependencyInjection.Abstractions` forced `net8.0` consumers
  off their own LTS servicing line. The `net8.0` assembly now floors it at `8.0.2`
  and the `net10.0` assembly at `10.0.10`, each on its own runtime-aligned line.
  `Spectre.Console` and `Spectre.Console.Cli` version independently of the
  runtime and remain single common floors.
- Updated NuGet dependencies to their latest stable versions: `Spectre.Console`
  0.57.2 (and the test-only `Spectre.Console.Testing` 0.57.2) and
  `Microsoft.SourceLink.GitHub` 10.0.301, plus the test-only
  `Microsoft.Extensions.DependencyInjection` 10.0.10 and `Microsoft.NET.Test.Sdk`
  18.8.1. `Spectre.Console.Cli` remains 0.55.0 — its latest stable, which only
  requires `Spectre.Console >= 0.55.0`.

## [0.2.0] — 2026-06-20

### Added

- Multi-targeting: the package now ships `net8.0` and `net10.0` assemblies (previously
  `net10.0` only), broadening the range of consuming runtimes.

### Changed

- Switched the internal synchronization primitive from `System.Threading.Lock` (net9+)
  to `object` so the same source compiles on `net8.0`.

## [0.1.1] — 2026-06-10

### Changed

- Updated NuGet dependencies to their latest stable versions: `Spectre.Console`
  0.56.0, `Microsoft.Extensions.DependencyInjection(.Abstractions)` 10.0.9, and
  `Microsoft.SourceLink.GitHub` 10.0.300 (plus the test-only `Microsoft.NET.Test.Sdk`,
  `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`, and
  `Spectre.Console.Testing`). `Spectre.Console.Cli` remains 0.55.0 — its latest
  stable, which only requires `Spectre.Console >= 0.55.0`.
- Adopted Central Package Management: package versions now live in a single
  `Directory.Packages.props`.
- Publishing now uses NuGet Trusted Publishing (OIDC short-lived keys) instead of
  a long-lived API-key secret.

## [0.1.0] — 2026-05-27

### Added — initial release

- **Strongly-typed settings** — `SettingsBase`: derive, back each property with
  a field, call `OnPropertyChanged()`. An instance is inert until bound at load,
  so deserializer setter calls never trigger a write.
- **Automatic / explicit persistence** — `PersistenceMode`. `Automatic` debounces
  a burst of changes into a single async write (default 250 ms window, configurable);
  `Explicit` persists only via `Save()` / `SaveAsync()`. Fire-and-forget write
  failures are surfaced through a configurable error handler, never swallowed.
- **`AddSettings<T>()`** DI extension with required-`SettingsDirectory` validation.
  Each class is persisted to its own `{ClassName}.json` file. Per-class singletons
  resolve through `ISettingsStore`, so an in-place reset is observed by injected
  references.
- **Tolerant JSON persistence** — atomic writes (temp-file + rename), missing
  properties default, unknown properties ignored, case-insensitive matching,
  string-valued enums.
- **Corrupt-file resilience** — a malformed settings file is copied to a
  `{file}.bak` sidecar and the class falls back to defaults, rather than
  crashing startup or letting the next write destroy the unreadable content.
- **`settings` command branch** — `list` and `reset` (`<SettingsClassName>` and
  `--all`), drop-in via `CommandConfiguratorExtensions.AddSettingsBranch()`. All
  commands honour `-v` / `--verbose`. `reset` confirms before overwriting
  (defaults to "no"; skip with `-f` / `--force`); `list` renders complex and
  collection values as compact JSON.
- **`ISettingsStore`** — enumerate registrations, resolve instances, and reset one
  or all classes at runtime.
- Full XML documentation on the public surface.
- Test suite (xUnit) with 32 tests covering load-on-missing-file, automatic
  persistence + round-trip, explicit persistence, debounce coalescing, reset /
  reset-all, tolerant deserialisation, corrupt-file backup, atomic writes, error
  surfacing, `settings list` value formatting, and end-to-end command flows
  (including the `reset` confirmation prompt).
- SourceLink, deterministic builds, published symbol packages (`snupkg`).
- `TreatWarningsAsErrors=true`, `AnalysisLevel=latest` — zero-warning public API.
- Package icon, with the editable source vector kept under `design/icons/`.

[Unreleased]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/compare/v0.3.0...v1.0.0
[0.3.0]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/releases/tag/v0.3.0
[0.2.0]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/releases/tag/v0.2.0
[0.1.1]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/releases/tag/v0.1.1
[0.1.0]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/releases/tag/v0.1.0
