# CLAUDE.md — NextIteration.SpectreConsole.Settings

## This package

Strongly-typed, JSON-persisted settings for CLI tools built on Spectre.Console. A
consumer derives a class from `SettingsBase`, registers it with `AddSettings<T>` (giving
an explicit `SettingsDirectory` — there is no default), and injects it into their
commands. Each settings class gets its own `{ClassName}.json` file in that directory.
Property changes persist automatically on a debounced background write, or only on
`Save()` in `Explicit` mode. `AddSettingsCommands()` wires a ready-made `settings
list` / `settings reset` branch into an existing `CommandApp`. Nothing here consumes
another package in the estate, and nothing in the estate consumes it.

## Things that are easy to get wrong here

- **A `SettingsBase` instance is inert until the framework `Bind`s it.** That is what
  stops the JSON deserializer's own property assignments from scheduling a write during
  load — the deserializer runs first, `Bind` runs after. Moving `Bind` earlier turns
  every load into a write.
- **Automatic persistence is debounced *and* fire-and-forget.** Nothing signals that a
  write completed, so a test cannot sleep a fixed interval and assert; it must poll for
  the observable effect. That is what `Infrastructure/Wait.cs` is for. A negative
  assertion ("no write happened") is the one case that keeps an explicit quiet window,
  because there is nothing to wait for and too short a wait can only mask a bug.
- **`AtomicFile` deliberately uses a different replace primitive per platform.**
  `File.Move(overwrite: true)` is `rename(2)` on POSIX, which replaces a destination
  another handle holds open and serialises concurrent renames; on Windows it is
  `MoveFileEx`, which throws in both cases. Windows must go through `File.Replace`. Do
  not "simplify" the two branches back into one — that is a bug that shipped for three
  releases because the test matrix was Linux-only.
- **Tolerant deserialisation is the on-disk contract, not a convenience.** Unknown JSON
  properties are ignored and missing ones fall back to constructed defaults, which is
  what lets a consumer add or remove a setting without a migration. Tightening the
  serializer options — `UnmappedMemberHandling`, a strict naming policy, dropping
  `JsonStringEnumConverter` — breaks every settings file already on disk.
- **`SettingsDirectory` has no smart default and registration throws without one.** That
  is deliberate: guessing at `~/.config/{something}` on a consumer's behalf picks a name
  the consumer has to live with forever.

## Repository baseline

This repo conforms to
[NextIteration.Standards](https://github.com/StuartMeeks/NextIteration.Standards).
Build properties, test stack, CI shape, and branch protection are defined there, not
here. Before changing any of those, read `STANDARD.md`; if this repo needs to deviate,
that is an `EXCEPTIONS.md` entry in the standards repo, not a local difference.

## Non-negotiables

- **The build must be clean.** `TreatWarningsAsErrors` is on and analyzers run at
  `latest`. A warning is a build failure.
- **Tests must pass on every shipped target framework** (`net8.0` and `net10.0`). A change
  that only passes on one is not finished. Shipping a target you do not test is a defect,
  not a scoping decision.
- **Dependency floors are deliberate and per-TFM.** A `PackageReference` version in a
  library is a *minimum* NuGet forces on every consumer, so raising a floor is a
  consumer-visible change even when nothing in the code needs it. Never raise one to
  silence a warning. Here that is
  `Microsoft.Extensions.DependencyInjection.Abstractions`: 8.0.x for `net8.0`, 10.0.x for
  `net10.0`.
- **Public API changes need XML docs.** `GenerateDocumentationFile` is on and the public
  surface is fully documented.
- **Update `CHANGELOG.md`** under `[Unreleased]`, saying what changed and why.

## Dependabot

Minor and patch updates auto-merge behind CI. Major updates stay open for a human — that
is deliberate, not a backlog to clear. Packages with per-TFM floors have major updates
suppressed entirely via `ignore`; bump those by hand when a new .NET major lands.

## After opening a pull request

Watch CI to completion, report the real check results, then **offer to merge** in the same
message. Do not stop silently and wait to be asked.

- If branch protection blocks the merge, say so and offer `gh pr merge --admin`. These
  repos require a code-owner review only the maintainer can give, which is why `--admin` is
  the tool — but that mechanic is not the reason the offer is wanted. The reason is simply
  that the maintainer has grown comfortable delegating this to an agent, so treat the
  latest instruction as authoritative over this file.
- **Merge only on an explicit yes.** The offer is pre-approved; the action is not.
- Never offer while checks are failing or still running. Report that state instead.
- Report the checks that actually ran. A skipped check is not a passing check, and branch
  protection treats them differently from how they read in a summary.

## CI

The single required status check is `ci` — an aggregating gate over `build` and `test`.
Renaming those jobs is safe; the ruleset never names them. Do not make them required
checks directly.

`ci.yml` also carries a `release` job beyond the four `STANDARD.md` 3.1 names. It is
tag-gated and downstream of `publish`, and cuts the GitHub release from `CHANGELOG.md`.
