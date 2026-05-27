# NextIteration.SpectreConsole.Settings

[![NuGet](https://img.shields.io/nuget/v/NextIteration.SpectreConsole.Settings.svg)](https://www.nuget.org/packages/NextIteration.SpectreConsole.Settings/)
[![Downloads](https://img.shields.io/nuget/dt/NextIteration.SpectreConsole.Settings.svg)](https://www.nuget.org/packages/NextIteration.SpectreConsole.Settings/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![CI](https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/actions/workflows/ci.yml/badge.svg)](https://github.com/StuartMeeks/NextIteration.SpectreConsole.Settings/actions/workflows/ci.yml)

Strongly-typed, JSON-persisted settings and ready-made `settings` commands for CLI tools built on [Spectre.Console](https://spectreconsole.net/).

Stop hand-rolling the same `~/.app/config.json` + load/save boilerplate into every CLI you build. Declare a settings class, register it, and inject it into any command. Change a property and it's persisted — automatically and debounced, or explicitly when you say so — and `my-cli settings list` / `reset` just work.

---

## Features

- **Strongly-typed settings** — derive from `SettingsBase`, back each property with a field, call `OnPropertyChanged()`. That's the whole contract.
- **Automatic or explicit persistence** — `Automatic` mode debounces a burst of changes into a single async disk write; `Explicit` mode persists only when you call `Save()` / `SaveAsync()`.
- **`settings` command branch** — `list` and `reset` wired into your existing `CommandApp` with a single call.
- **Atomic writes** — a crash mid-write never leaves a half-written settings file on disk.
- **Tolerant deserialisation** — added a property? It defaults. Removed one? The old key is ignored. No migration ceremony for ordinary schema evolution.
- **Errors are never swallowed** — a failed fire-and-forget write is routed to a configurable error handler (default: one line to stderr).
- **Zero compiler warnings, fully documented public surface** — `<GenerateDocumentationFile>` on, analyzers on, `TreatWarningsAsErrors` on.

---

## Install

```shell
dotnet add package NextIteration.SpectreConsole.Settings
```

Targets `net10.0`.

---

## Quick start

### 1. Declare a settings class

Derive from `SettingsBase`, back each property with a field, and call `OnPropertyChanged()` from the setter:

```csharp
using NextIteration.SpectreConsole.Settings;

public sealed class AppSettings : SettingsBase
{
    private DataSourceMode _mode = DataSourceMode.File;

    public DataSourceMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            OnPropertyChanged();
        }
    }
}

public enum DataSourceMode { File, Api }
```

### 2. Register it

One call per settings class. `SettingsDirectory` is required — there's no smart default:

```csharp
using NextIteration.SpectreConsole.Settings;

services.AddSettings<AppSettings>(opts =>
{
    opts.SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".my-cli", "settings");
});

// A second class — this one persists only on explicit Save():
services.AddSettings<CacheSettings>(opts =>
{
    opts.SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".my-cli", "settings");
    opts.PersistenceMode = PersistenceMode.Explicit;
});
```

### 3. Hook the `settings` branch into your configurator

```csharp
app.Configure(config =>
{
    config.AddSettingsBranch();
    // ... your other commands
});
```

### 4. Inject and use it from any command

```csharp
public sealed class SwitchModeCommand(AppSettings settings) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        settings.Mode = DataSourceMode.Api;
        // No explicit Save() needed — persisted automatically (debounced).
        return 0;
    }
}
```

That's it. Each settings class is serialised to its own JSON file in the directory, named after the class (e.g. `AppSettings.json`). On the next launch it's deserialised and handed to your commands.

---

## The `settings` branch

| Command | Description |
|---|---|
| `settings list` | Table of every registered settings class and its current property values. |
| `settings reset <SettingsClassName>` | Reset one settings class to default values and persist. |
| `settings reset --all` | Reset every registered settings class to defaults and persist. |

Every command accepts `-v` / `--verbose` for full stack-trace output when something goes wrong.

```console
$ my-cli settings list
AppSettings (Automatic)
/home/me/.my-cli/settings/AppSettings.json
┌──────────┬──────┐
│ Property │ Value│
├──────────┼──────┤
│ Mode     │ Api  │
└──────────┴──────┘

$ my-cli settings reset AppSettings
Reset 'AppSettings' to defaults.
```

---

## Persistence model

### Automatic (default)

Every `OnPropertyChanged()` schedules a **debounced** asynchronous write. A burst of changes in the same synchronous call stack — or within the debounce window — coalesces into a single disk write, so setting five properties in a row touches the file once. The default window is 250 ms; tune it per class via `SettingsOptions.DebounceInterval`.

Writes are fire-and-forget, but failures are **never silently swallowed** — they're routed to `SettingsOptions.ErrorHandler` (default: a single line to stderr).

### Explicit

`OnPropertyChanged()` becomes a no-op; nothing is written until you call `Save()` or `SaveAsync()`.

- `Save()` — fire-and-forget. Convenient, but in a command that mutates-then-returns the process may exit before the write lands.
- `SaveAsync()` — awaitable. Use it when you need the write to be on disk before the command returns. Exceptions propagate to the awaiter rather than the error handler.

Both methods work in **either** mode — call `SaveAsync()` in `Automatic` mode too when you want to flush deterministically.

### Loading

On startup each registered class is deserialised from its JSON file. If the file doesn't exist, a default-constructed instance is used — no exception. Property assignments performed by the deserializer during load never trigger a write; persistence only kicks in for changes you make afterwards.

---

## Schema evolution

Settings classes change over time. The default serializer is tolerant:

- **Added a property?** Files written by older versions simply lack the key, so it takes its constructed default.
- **Removed a property?** The now-unknown key in older files is ignored.
- Property matching is case-insensitive; enums round-trip as readable strings; comments and trailing commas are tolerated on read.

Supply your own `SettingsOptions.SerializerOptions` if you need different behaviour.

---

## Nested values & keeping it simple

Persistence handles whatever `System.Text.Json` can serialise — nested objects, lists, and dictionaries all round-trip to and from the JSON file without extra work. Two things to know once you go beyond scalars:

- **Automatic save only fires on the top-level setter.** `OnPropertyChanged()` runs when you assign a property *on the settings object* — not when you mutate *inside* a nested object or collection:

  ```csharp
  settings.Profile = new Profile { Name = "Ada" };   // ✅ persisted (the setter ran)
  settings.Profile.Name = "Ada";                     // ❌ not detected in Automatic mode
  settings.RecentFiles.Add("notes.txt");             // ❌ not detected
  ```

  To persist a nested change: reassign the whole property, model nested values as immutable `record`s and swap them (`settings.Profile = settings.Profile with { Name = "Ada" };`), or call `settings.Save()` / `await settings.SaveAsync()` after mutating.

- **`settings list` renders complex values as compact JSON**, so the table stays readable instead of printing a type name.

### Recommendation: prefer scalar properties

Keep your life simple — make settings properties **scalars** (`string`, `bool`, numbers, `enum`, `DateTime`, `Guid`, `Uri`, …) wherever you can. A flat scalar settings class never hits the in-place-mutation gotcha above, reads cleanly in `settings list`, and is trivial to reason about. Reach for a nested object or collection only when you're happy to assign it wholesale.

---

## Resetting at runtime

`ISettingsStore` is registered as a singleton and aggregates every class you registered. It powers `settings reset`, and you can use it directly:

```csharp
public sealed class FactoryResetCommand(ISettingsStore store) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        await store.ResetAllAsync();
        return 0;
    }
}
```

A reset restores defaults **in place** on the live instance and persists immediately — any command that injected the settings object directly observes the change without a restart.

---

## Configuration reference

`AddSettings<T>(opts => …)` accepts a `SettingsOptions`:

| Option | Default | Notes |
|---|---|---|
| `SettingsDirectory` | — (required) | Absolute path; throws at registration if unset. |
| `PersistenceMode` | `Automatic` | `Automatic` (debounced) or `Explicit`. |
| `DebounceInterval` | `250 ms` | Coalescing window for automatic writes. |
| `ErrorHandler` | stderr line | Invoked when a fire-and-forget write fails. |
| `SerializerOptions` | tolerant default | Override the `JsonSerializerOptions`. |

`T` must derive from `SettingsBase` and have a public parameterless constructor (used for defaults and by the deserializer).

---

## Requirements

- **.NET 10.0** or later
- **Spectre.Console** 0.54+ and **Spectre.Console.Cli** 0.53+
- **Microsoft.Extensions.DependencyInjection.Abstractions** 10.0+

Everything else is transitive.

---

## Contributing

Issues and PRs welcome. The [TODO](TODO.md) tracks outstanding ideas.

When contributing code, please keep the zero-warning, fully-documented public surface. `TreatWarningsAsErrors` is on for a reason.

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for release notes.

---

## License

[MIT](LICENSE) © Stuart Meeks

Built for — and unaffiliated with — the excellent [Spectre.Console](https://github.com/spectreconsole/spectre.console) project.
