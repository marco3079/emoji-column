<#
.SYNOPSIS
  Packs assets/emoji/NN.png into a Win32 .res file: one RT_GROUP_ICON per emoji
  (resource id 100 + NN) with sizes 16-64 px, ready for csc /win32res.
  Explorer's IconList columns can only show icons stored as Win32 resources.
#>
param(
    [Parameter(Mandatory)] [string]$SourceDir,
    [Parameter(Mandatory)] [string]$OutFile
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sizes = 16, 20, 24, 32, 40, 48, 64
$RT_ICON = 3; $RT_GROUP_ICON = 14

$stream = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $stream

function Write-Resource([int]$type, [int]$id, [byte[]]$data, [uint16]$flags) {
    $w.Write([uint32]$data.Length)
    $w.Write([uint32]32)                       # header size
    $w.Write([uint16]0xFFFF); $w.Write([uint16]$type)
    $w.Write([uint16]0xFFFF); $w.Write([uint16]$id)
    $w.Write([uint32]0)                        # data version
    $w.Write([uint16]$flags)
    $w.Write([uint16]0)                        # language neutral
    $w.Write([uint32]0); $w.Write([uint32]0)   # version, characteristics
    $w.Write($data)
    while ($stream.Position % 4) { $w.Write([byte]0) }
}

function Get-Png([System.Drawing.Image]$src, [int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.PixelOffsetMode = 'HighQuality'
    $g.SmoothingMode = 'AntiAlias'
    $g.CompositingQuality = 'HighQuality'
    $g.DrawImage($src, 0, 0, $size, $size)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return , $ms.ToArray()
}

# Null resource that starts every .res file.
Write-Resource 0 0 (New-Object byte[] 0) 0

$files = Get-ChildItem $SourceDir -Filter '*.png' | Where-Object { $_.BaseName -match '^\d+$' } | Sort-Object { [int]$_.BaseName }
foreach ($file in $files) {
    $value = [int]$file.BaseName
    $src = [System.Drawing.Image]::FromFile($file.FullName)
    $group = New-Object System.IO.MemoryStream
    $gw = New-Object System.IO.BinaryWriter $group
    $gw.Write([uint16]0); $gw.Write([uint16]1); $gw.Write([uint16]$sizes.Count)
    for ($k = 0; $k -lt $sizes.Count; $k++) {
        $png = Get-Png $src $sizes[$k]
        $iconId = 1000 + $value * 10 + $k
        Write-Resource $RT_ICON $iconId $png 0x1010
        $gw.Write([byte]$sizes[$k]); $gw.Write([byte]$sizes[$k]); $gw.Write([byte]0); $gw.Write([byte]0)
        $gw.Write([uint16]1); $gw.Write([uint16]32); $gw.Write([uint32]$png.Length); $gw.Write([uint16]$iconId)
    }
    $src.Dispose()
    $gw.Flush()
    Write-Resource $RT_GROUP_ICON (100 + $value) $group.ToArray() 0x1030
}

$w.Flush()
[IO.File]::WriteAllBytes($OutFile, $stream.ToArray())
Write-Host "> $(Split-Path $OutFile -Leaf) ($($files.Count) icons)"
