# Changelog

All notable changes to `NextIteration.SpectreConsole.Settings` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

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
- **`settings` command branch** — `list` and `reset` (`<SettingsClassName>` and
  `--all`), drop-in via `CommandConfiguratorExtensions.AddSettingsBranch()`. All
  commands honour `-v` / `--verbose`.
- **`ISettingsStore`** — enumerate registrations, resolve instances, and reset one
  or all classes at runtime.
- Full XML documentation on the public surface.
- Test suite (xUnit) with 22 tests covering load-on-missing-file, automatic
  persistence + round-trip, explicit persistence, debounce coalescing, reset /
  reset-all, tolerant deserialisation, atomic writes, and error surfacing.
- SourceLink, deterministic builds, published symbol packages (`snupkg`).
- `TreatWarningsAsErrors=true`, `AnalysisLevel=latest` — zero-warning public API.

[0.1.0]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/releases/tag/v0.1.0
