<#
.SYNOPSIS
  Downloads the Fluent Emoji (MIT, (c) Microsoft) 3D images for every emoji in
  src/Shared/EmojiPalette.cs into assets/emoji/NN.png (NN = enumeration value).
  The images are committed, so this only needs to run when the palette changes.
#>
param([string]$Ref = 'main')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$outDir = Join-Path $root 'assets\emoji'
New-Item -ItemType Directory -Force $outDir | Out-Null

$palette = [regex]::Matches([IO.File]::ReadAllText((Join-Path $root 'src\Shared\EmojiPalette.cs')),
    'new\[\] \{ "([^"]+)", "([^"]+)" \}')
$tree = (Invoke-RestMethod "https://api.github.com/repos/microsoft/fluentui-emoji/git/trees/$($Ref)?recursive=1").tree.path |
    Where-Object { $_ -like 'assets/*/3D/*.png' }

for ($i = 0; $i -lt $palette.Count; $i++) {
    $name = $palette[$i].Groups[2].Value
    $path = $tree | Where-Object { $_ -like "assets/$name/3D/*.png" -or $_ -like "assets/$name/Default/3D/*.png" } | Select-Object -First 1
    if (-not $path) { throw "Fluent Emoji asset not found for '$name'" }
    $escaped = (($path -split '/') | ForEach-Object { [Uri]::EscapeDataString($_) }) -join '/'
    $url = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/$Ref/$escaped"
    $file = Join-Path $outDir ('{0:00}.png' -f ($i + 1))
    Invoke-WebRequest $url -OutFile $file -UseBasicParsing
    Write-Host ('{0:00} {1}  <- {2}' -f ($i + 1), $palette[$i].Groups[1].Value, $path)
}
Invoke-WebRequest "https://raw.githubusercontent.com/microsoft/fluentui-emoji/$Ref/LICENSE" -OutFile (Join-Path $outDir 'LICENSE-fluentui-emoji.txt') -UseBasicParsing
