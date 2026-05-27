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

## Commands

> `settings get` / `set` was considered and dropped: the library favours flat
> scalar settings (see the README "keeping it simple" note), and a single-property
> CLI getter/setter added conversion/complex-type surface without enough payoff.

## Tooling

- Decide whether to expose hardened file permissions (Unix `0600`) as an opt-in
  for settings that happen to hold semi-sensitive values.
