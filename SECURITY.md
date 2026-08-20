# Security policy

## Reporting a vulnerability

Report privately through GitHub's **Report a vulnerability** button under this
repository's Security tab, which opens a private advisory visible only to the
maintainers. Please do not open a public issue for a suspected vulnerability.

Include the affected package and version, what an attacker can achieve, and a
reproduction if you have one.

You can expect an acknowledgement within 7 days, an assessment within 14, and
credit in the advisory and changelog unless you ask otherwise.

## Supported versions

Only the latest released minor of each package receives security fixes. These are
pre-1.0 libraries and there are no long-term support branches.

## Scope

This library writes application settings to **plain-text JSON** on the local
filesystem, at a directory the consuming application chooses. Three things are
explicitly **not** claimed:

- **Settings are not secrets.** Nothing is encrypted, obfuscated, or held in
  protected memory, and the file is created with whatever permissions the calling
  process's umask and the parent directory give it. Do not store API keys, tokens
  or passwords in a `SettingsBase` class. Use
  [NextIteration.SpectreConsole.Auth](https://github.com/StuartMeeks/NextIteration.SpectreConsole.Auth)
  for credentials — that is what it is for.
- **The consumer chooses the directory, and owns it.** `SettingsDirectory` is
  required and unvalidated beyond being a path; pointing it at a world-writable
  location, or at a path assembled from untrusted input, is the caller's decision
  and the caller's exposure.
- **Atomic writes are a crash-consistency guarantee, not a concurrency one.**
  `AtomicFile` guarantees a reader sees either the whole old file or the whole new
  file. It does not serialise writers: two processes writing concurrently observe
  last-write-wins, and one process's changes can be lost.

In scope and welcome: anything that breaks *within* those boundaries — a write that
leaves a partial or corrupt file readable, a path in the library itself that escapes
`SettingsDirectory`, deserialisation of a settings file causing something worse than
a thrown exception, or a settings value reaching disk somewhere other than the file
it was registered for. Reports that only restate a documented limitation above are
not vulnerabilities.
