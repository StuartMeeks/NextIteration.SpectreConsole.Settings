# Changelog

All notable changes to `NextIteration.SpectreConsole.Settings` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [0.1.0] — Unreleased

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

[0.1.0]: https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/tree/main
