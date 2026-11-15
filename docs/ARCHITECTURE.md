# Architecture

Emoji Column has three pieces, all built by `build.ps1` with the C# compiler included in Windows.

| Piece | File | Role |
|---|---|---|
| Property schema | `schema/EmojiColumn.propdesc` | Declares the Shell property `EmojiColumn.Emoji` (FMTID `{F95F067F-2802-4D52-BCA0-52F0C3630128}`, PID 2) so Explorer lists it under *More…* columns. |
| Property handler | `EmojiColumn.Handler.dll` | In-process COM server (CLSID `{25F4796A-9D1C-4E44-9C7E-D768BEC844F3}`) implementing `IInitializeWithFile`, `IPropertyStore` and `IPropertyStoreCapabilities`. |
| App | `EmojiColumn.exe` | Manager window, emoji picker, context-menu target, CLI and installer. |

## Where the emoji is stored

In NTFS alternate data streams, UTF-8: `file.csv:emoji` (max 256 bytes) and `file.csv:description` (max 4 KB).
The file's main content is never opened for writing. Because NTFS updates *Date modified*
when any stream changes, `EmojiStore` saves and restores the timestamps around every write.

## Reading: the property handler

Windows binds property handlers to file extensions:

```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\PropertySystem\PropertyHandlers\.csv
    (Default) = {25F4796A-9D1C-4E44-9C7E-D768BEC844F3}
```

The handler needs the file **path** to open the `:emoji` stream, so it implements
`IInitializeWithFile`, which requires `DisableProcessIsolation = 1` on its CLSID (it runs
inside Explorer instead of the isolated `prevhost`/`prophost` process).

### Chaining

If the extension already had a handler (for example the Office handler for `.docx`), the
installer saves its CLSID under `HKLM\SOFTWARE\EmojiColumn\Chain` before replacing it.
At runtime the Emoji handler creates the original handler, initializes it with a stream on the
same file and exposes all of its properties plus `EmojiColumn.Emoji`.

* **Read-only access** (columns, tooltips, Details pane): the original values are copied with
  `PropVariantCopy` and the original handler and file stream are released immediately, so
  Explorer doesn't keep the file open.
* **Read/write access** (editing a property in the Details pane): the original handler stays
  alive and receives `SetValue`/`Commit`.

Uninstalling puts the saved CLSID back.

## Color emojis

Explorer draws text columns with GDI, which has no color fonts, so the emoji in
`EmojiColumn.Emoji` (text) shows in black and white. Color comes from a second
property, `EmojiColumn.EmojiIcon`, declared with `drawControl="IconList"`:

* Its value (`VT_BLOB`) is a serialized property store (MS-PROPSTORE) holding
  `System.PropList.StatusIcons = "prop:EmojiColumn.EmojiIndex"`,
  `System.PropList.StatusIconsDisplayFlag = 2` (icon only) and `EmojiColumn.EmojiIndex = n`.
  The handler builds it with `PSCreateMemoryPropertyStore` + `IPersistSerializedPropStorage`
  and caches one blob per emoji.
* `EmojiColumn.EmojiIndex` is an enumerated `UInt32`; each `<enum>` has an
  `<image res="…\EmojiColumn.Icons.dll,-1NN" />`. Explorer only accepts Win32 icon
  resources here, and the list is fixed when the schema is registered.
* `build.ps1` generates the enumeration from `src/Shared/EmojiPalette.cs` and packs
  `assets/emoji/NN.png` (Fluent Emoji 3D) into `EmojiColumn.Icons.dll`
  (`tools/New-IconResource.ps1` writes the `.res`, `csc /win32res` links it).

## Editing the description in Explorer

`EmojiColumn.Description` is not innate, and the handler reports it as writable in
`IPropertyStoreCapabilities`. When Explorer opens the store for writing (Details pane,
*Properties → Details*), `SetValue` keeps the new text and `Commit` writes the
`:description` stream. So that the field shows up in those panes, the installer sets
`PreviewDetails` / `FullDetails` under `HKLM\SOFTWARE\Classes\SystemFileAssociations\<ext>`
only for types that don't define their own lists, and removes them on uninstall.

## Writing: the app

* Context menu: a static cascading verb under `HKLM\SOFTWARE\Classes\*\shell\EmojiColumn`.
  Explorer starts one process per selected file; for `pick`, the first process becomes the
  primary (named mutex) and the others send their path over a named pipe, so the user sees a
  single picker for the whole selection.
* After writing, `SHChangeNotify(SHCNE_UPDATEITEM)` tells Explorer to re-read the item.

## Install layout

| What | Where |
|---|---|
| Files | `%ProgramFiles%\EmojiColumn\` |
| COM registration | `HKLM\SOFTWARE\Classes\CLSID\{25F4796A-…}` (`mscoree.dll`, .NET 4 runtime) |
| Schema | registered with `PSRegisterPropertySchema` |
| State | `HKLM\SOFTWARE\EmojiColumn` (`Version`, `InstallDir`, `Extensions`, `Chain\`) |
| Apps & Features entry | `HKLM\…\Uninstall\EmojiColumn` |

All keys are written to the 64-bit registry view. Updating while Explorer holds the DLL is
handled by moving the old file aside and deleting it on the next reboot.
