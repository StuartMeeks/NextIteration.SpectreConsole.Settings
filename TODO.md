# TODO

The initial release (0.1.0) is feature-complete against the original brief.
Outstanding ideas for future versions:

## Persistence

- **`INotifyPropertyChanged` interop** — optionally raise the standard event from
  `OnPropertyChanged()` so settings objects can bind to data-bound UIs as well as
  drive persistence.
- **Source generator** — emit the field + `OnPropertyChanged()` setter boilerplate
  from a `[Setting]` attribute, so consumers declare only the property.
- **External change detection** — optionally watch the settings file and reload
  when another process rewrites it (last-writer-wins today).
- **Backup-on-corrupt-load** — when a malformed file falls back to defaults,
  side-car the unreadable file (e.g. `AppSettings.json.bak`) before the next write
  overwrites it, instead of only surfacing the parse error to the handler.

## Commands

- **`settings get` / `settings set`** — read or mutate a single property by name
  from the CLI, for scripting.
- **Confirmation prompt on `reset`** — mirror the `--force` pattern from the Auth
  package's `accounts delete`.

## Tooling

- Decide whether to expose hardened file permissions (Unix `0600`) as an opt-in
  for settings that happen to hold semi-sensitive values.
