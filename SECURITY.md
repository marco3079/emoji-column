# Security Policy

Emoji Column registers an in-process Shell extension that runs inside `explorer.exe`, so we
take security reports seriously.

## Supported versions

Only the latest release receives fixes.

## Reporting a vulnerability

Please **do not** open a public issue. Use GitHub's
[private vulnerability reporting](https://github.com/marco3079/emoji-column/security/advisories/new)
with steps to reproduce and the affected version. You should get an answer within 7 days.

## Scope notes

- The handler only *reads* the `:emoji` stream (max 256 bytes) and never executes or parses file content itself.
- Installation writes to `HKLM` and `%ProgramFiles%` and therefore requires administrator rights; the app never elevates silently.
