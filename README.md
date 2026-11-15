<p align="center">
  <img src="assets/icon.png" width="112" alt="Emoji Column icon">
</p>

<h1 align="center">Emoji Column</h1>

<p align="center">
  <b>A real "Emoji" column for Windows File Explorer.</b><br>
  Tag any file with ✅ ⏳ ⚠️ 💰 🔥 — without renaming it.
</p>

<p align="center">
  <a href="https://github.com/marco3079/emoji-column/actions/workflows/ci.yml"><img src="https://github.com/marco3079/emoji-column/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/marco3079/emoji-column/releases/latest"><img src="https://img.shields.io/github/v/release/marco3079/emoji-column?display_name=tag&sort=semver" alt="Latest release"></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6?logo=windows" alt="Windows 10 | 11">
  <img src="https://img.shields.io/badge/.NET%20Framework-4.8-512BD4" alt=".NET Framework 4.8">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green" alt="MIT License"></a>
</p>

<p align="center">
  <a href="README.pt-BR.md">🇧🇷 Leia em português</a>
</p>

---

<p align="center">
  <img src="assets/screenshots/explorer-column.png" alt="Explorer details view with a color Emoji column and a My description column" width="520">
  &nbsp;
  <img src="assets/screenshots/picker.png" alt="Emoji picker window" width="300">
</p>

## Why

Windows lets you sort and group files by dozens of columns, but there is no quick way to *mark* a file —
"already paid", "needs review", "important". Renaming files breaks links and scripts; color labels
don't exist. **Emoji Column** adds a native Explorer column that you can sort, group and filter by,
backed by a tiny tag stored inside the file's own NTFS metadata.

## Features

- 🎨 **Color emojis in Explorer** — 48 emojis are drawn as full-color icons (Fluent Emoji 3D), right in the *Details* view.
- 🧩 **Native columns** — *Emoji* (color), *Emoji (text)* (sort, group and stack by it, any emoji) and *My description*.
- 📝 **"My description" column** — free text per file, edited from the right-click menu, in the Details pane (<kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>P</kbd>) or in *Properties → Details*.
- 🖱️ **Right-click to tag** — *Emoji* submenu with quick emojis, a picker with 48 emojis + any custom one (<kbd>Win</kbd> + <kbd>.</kbd>), and *Remove emoji*.
- 📚 **Multi-select** — tag a whole selection at once; one picker window for all files.
- 🕒 **Doesn't touch your files** — the name, the content **and the "Date modified"** stay exactly the same.
- 🔗 **Plays nice with other handlers** — if a file type already has a property handler (Office, images…), it is chained, not replaced, and restored on uninstall.
- ⌨️ **Command line** — `EmojiColumn.exe set ✅ file.csv` for scripts and automation.
- 🌎 **English and Portuguese** UI, picked automatically from the Windows language.
- 🪶 **Tiny** — ~80 KB, no runtime to install (uses .NET Framework 4.8, built into Windows 10/11).

## Install

1. Download `EmojiColumn-x.y.z.zip` from the [latest release](https://github.com/marco3079/emoji-column/releases/latest) and extract it anywhere.
2. Run `EmojiColumn.exe`, review the file types and click **Install / Update** (asks for administrator rights).
3. Accept the prompt to restart Explorer.
4. In a folder, right-click a column header → **More…** → tick **Emoji** and **My description** (and **Emoji (text)** if you want to sort or group by emoji).

> [!TIP]
> To use the same columns in every folder of the same kind, open *View → Options → View* and click **Apply to Folders**.

The app copies itself to `C:\Program Files\EmojiColumn` and appears in *Settings → Apps* for a clean uninstall.

## Usage

**Explorer:** select one or more files → right-click → **Show more options** → **Emoji** → pick one, or **Description…** to type a description.
You can also click the *My description* field in the Details pane (<kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>P</kbd>) and type.

**Command line:**

```powershell
EmojiColumn.exe set ✅ .\invoice-2026-11.csv .\card-bill-2026-11.csv
EmojiColumn.exe get *.csv          # prints "✅  path" for each file (wildcards supported)
EmojiColumn.exe clear .\invoice-2026-11.csv
EmojiColumn.exe pick .\report.pdf  # opens the picker
EmojiColumn.exe set-desc "Paid on 11/10" .\invoice-2026-11.csv
EmojiColumn.exe clear-desc .\invoice-2026-11.csv
EmojiColumn.exe install --ext .csv,.pdf,.xlsx --quiet
EmojiColumn.exe uninstall --quiet
```

**PowerShell, no app needed** — the tag and the description are plain UTF-8 alternate data streams named `emoji` and `description`:

```powershell
Get-Content .\file.csv -Stream emoji
Set-Content .\file.csv -Stream emoji -Value '✅' -Encoding UTF8
```

## How it works

```mermaid
flowchart LR
    A[Explorer<br>Details view] -- "EmojiColumn.Emoji?" --> B[Property system]
    B -- ".csv → CLSID" --> C[EmojiColumn.Handler.dll<br>property handler]
    C -- reads --> D[(file.csv:emoji<br>NTFS stream)]
    C -. "other properties" .-> E[Original handler<br>if any]
    F[Right-click menu / CLI] -- writes --> D
    F -- SHChangeNotify --> A
```

- `schema/EmojiColumn.propdesc` registers two Shell properties, **`EmojiColumn.Emoji`** (read-only) and **`EmojiColumn.Description`** (editable), so they show up in the *Choose Details* list.
- `EmojiColumn.Handler.dll` is a COM [property handler](https://learn.microsoft.com/windows/win32/properties/property-handlers) registered for the chosen extensions. It reads the `:emoji` stream and passes every other property through to the handler that was there before.
- `EmojiColumn.exe` is the manager, picker and CLI. It writes the stream, restores the file's timestamps and tells Explorer to refresh.

More details in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Limitations

- **NTFS only.** Alternate data streams are lost when a file goes to FAT/exFAT drives, e-mail, ZIP files or most cloud services (OneDrive keeps the local copy tagged, but the tag doesn't sync).
- **48 color emojis.** Explorer can only draw icons that are declared in advance, so the color column covers the 48 emojis of the picker. Any other emoji (from <kbd>Win</kbd> + <kbd>.</kbd>) is saved and shows in *Emoji (text)*.
- **Per file type.** Windows binds property handlers to extensions, so the column works for the types you enable (19 common ones by default). Folders can't be tagged.
- On Windows 11 the entry is in the classic menu (**Show more options** or <kbd>Shift</kbd> + right-click).
- Windows Search doesn't index the emoji (sorting/grouping in Explorer works; searching `emoji:✅` may not).

## Build from source

No Visual Studio or SDK needed — the C# compiler ships with Windows:

```powershell
git clone https://github.com/marco3079/emoji-column.git
cd emoji-column
.\build.ps1 -Zip      # output in .\dist
```

## Uninstall

*Settings → Apps → Emoji Column → Uninstall*, or `EmojiColumn.exe uninstall`. Tags stay in your files (harmless, invisible)
and come back if you reinstall.

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md). To add a color emoji, append it to `src/Shared/EmojiPalette.cs` and run `tools/fetch-emoji.ps1`. For security issues, see [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE) © 2026 marco3079. Emoji artwork: [Fluent Emoji](https://github.com/microsoft/fluentui-emoji) © Microsoft, MIT — see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
