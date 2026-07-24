# Changelog

All notable changes to `NextIteration.SpectreConsole.Settings` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

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

[0.3.0]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/releases/tag/v0.3.0
[0.2.0]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/releases/tag/v0.2.0
[0.1.1]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/releases/tag/v0.1.1
[0.1.0]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/releases/tag/v0.1.0
