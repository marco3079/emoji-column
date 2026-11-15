# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [1.2.0] - 2026-11-15

### Added
- **Color emojis in Explorer.** The *Emoji* column now draws full-color icons (Fluent Emoji 3D, MIT) for the 48 emojis of the picker, using an `IconList` property backed by `EmojiColumn.Icons.dll`.
- The picker and the right-click menu show the color icons too.

### Changed
- The text column is now called *Emoji (text)* / *Emoji (texto)*; *Emoji* is the color column. Add it once via *More…* in the column header menu.

## [1.1.0] - 2026-11-15

### Added
- **"My description" column** (`EmojiColumn.Description`, "Minha descrição" in Portuguese): free text per file, stored in the `:description` stream.
- Edit it from the context menu (*Emoji → Description…*), the Details pane, *Properties → Details*, or the CLI (`set-desc`, `clear-desc`, `edit-desc`).
- `get` now also prints the description.

### Fixed
- Quick-emoji labels in the context menu were garbled (wrong text encoding) in 1.0.0.

## [1.0.0] - 2026-11-13

### Added
- `EmojiColumn.Emoji` Shell property and property handler: a native **Emoji** column in File Explorer.
- Emoji stored in an NTFS alternate data stream; file name, content and *Date modified* are preserved.
- Chaining to existing property handlers, restored on uninstall.
- *Emoji* context-menu submenu with quick emojis, picker and remove action; multi-selection support.
- Manager window to choose file types, install, uninstall and restart Explorer.
- Command line: `set`, `get`, `clear`, `pick`, `install`, `uninstall` (with wildcards).
- English and Portuguese (Brazil) user interface.

[1.2.0]: https://github.com/marco3079/emoji-column/releases/tag/v1.2.0
[1.1.0]: https://github.com/marco3079/emoji-column/releases/tag/v1.1.0
[1.0.0]: https://github.com/marco3079/emoji-column/releases/tag/v1.0.0
