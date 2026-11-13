# Contributing

Thanks for your interest in Emoji Column! Bug reports, ideas and pull requests are welcome.

## Getting started

```powershell
git clone https://github.com/marco3079/emoji-column.git
cd emoji-column
.\build.ps1            # builds into .\dist
.\dist\EmojiColumn.exe # opens the manager
```

Requirements: Windows 10/11 (x64) with .NET Framework 4.8 — nothing else. The build uses
`csc.exe` from `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319` with **C# 5**, so newer syntax
(`$"..."`, `?.`, `nameof`, expression-bodied members) is not available.

## Testing a change to the handler

The handler is loaded by Explorer, so after rebuilding:

1. `.\dist\EmojiColumn.exe install --quiet` from an elevated terminal (copies the new DLL; the old one is moved aside).
2. Restart Explorer from the manager window (or `Stop-Process -Name explorer`).

## Guidelines

- Keep the handler small, defensive and allocation-light: it runs inside `explorer.exe`, so it must never throw or block.
- Every registry change made by the installer must be undone by `uninstall`.
- User-facing strings go in `src/App/Strings.cs`, in English and Portuguese.
- Follow `.editorconfig`; the build treats warnings as errors.
- Describe *why* in commit messages and update `CHANGELOG.md` under an `Unreleased` section.

## Releasing (maintainers)

1. Bump the version in `src/Shared/AssemblyInfo.Common.cs` and `CHANGELOG.md`.
2. Tag: `git tag v1.2.3 && git push --tags`. The *Release* workflow builds the ZIP and publishes the GitHub release.
