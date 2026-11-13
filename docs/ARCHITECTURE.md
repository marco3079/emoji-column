# Architecture

Emoji Column has three pieces, all built by `build.ps1` with the C# compiler included in Windows.

| Piece | File | Role |
|---|---|---|
| Property schema | `schema/EmojiColumn.propdesc` | Declares the Shell property `EmojiColumn.Emoji` (FMTID `{F95F067F-2802-4D52-BCA0-52F0C3630128}`, PID 2) so Explorer lists it under *More…* columns. |
| Property handler | `EmojiColumn.Handler.dll` | In-process COM server (CLSID `{25F4796A-9D1C-4E44-9C7E-D768BEC844F3}`) implementing `IInitializeWithFile`, `IPropertyStore` and `IPropertyStoreCapabilities`. |
| App | `EmojiColumn.exe` | Manager window, emoji picker, context-menu target, CLI and installer. |

## Where the emoji is stored

In an NTFS alternate data stream named `emoji` (`file.csv:emoji`), UTF-8, max 256 bytes.
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
