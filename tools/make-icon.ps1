<#
.SYNOPSIS
  Regenerates assets/icon.ico (a smiley on a column-like card) with System.Drawing.
  The icon is committed, so this only needs to run when the design changes.
#>
param([string]$Out = (Join-Path $PSScriptRoot '..\assets\icon.ico'))

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

function New-RoundedRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 256.0

    # Card with "rows" (an Explorer details view)
    $card = New-RoundedRect (12 * $s) (28 * $s) (232 * $s) (200 * $s) (34 * $s)
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF 0, $size), ([System.Drawing.Color]::FromArgb(255, 59, 130, 246)), ([System.Drawing.Color]::FromArgb(255, 30, 64, 175))
    $g.FillPath($grad, $card)
    if ($size -ge 32) {
        $row = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(110, 255, 255, 255))
        foreach ($y in 70, 112, 154) {
            $g.FillPath($row, (New-RoundedRect (118 * $s) ($y * $s) (100 * $s) (20 * $s) (10 * $s)))
        }
    }

    # Smiley (the emoji column)
    $cx = 70 * $s; $cy = 128 * $s; $r = 52 * $s
    if ($size -lt 32) { $cx = 128 * $s; $r = 78 * $s }
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 250, 204, 21))), $cx - $r, $cy - $r, 2 * $r, 2 * $r)
    $ink = [System.Drawing.Color]::FromArgb(255, 120, 53, 15)
    $eye = $r * 0.16
    $g.FillEllipse((New-Object System.Drawing.SolidBrush $ink), $cx - $r * 0.38 - $eye, $cy - $r * 0.25 - $eye, 2 * $eye, 2.4 * $eye)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush $ink), $cx + $r * 0.38 - $eye, $cy - $r * 0.25 - $eye, 2 * $eye, 2.4 * $eye)
    $pen = New-Object System.Drawing.Pen $ink, ([Math]::Max(1.2, $r * 0.13))
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
    $g.DrawArc($pen, $cx - $r * 0.55, $cy - $r * 0.45, $r * 1.1, $r * 1.05, 25, 130)
    $g.Dispose()
    return $bmp
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$images = foreach ($size in $sizes) {
    $ms = New-Object System.IO.MemoryStream
    $bmp = New-IconBitmap $size
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    , $ms.ToArray()
}

$buf = New-Object System.IO.MemoryStream
$w = [System.IO.BinaryWriter]::new($buf)
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$images[$i].Length); $w.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Flush()
[System.IO.File]::WriteAllBytes([System.IO.Path]::GetFullPath($Out), $buf.ToArray())

# PNG preview for the README
$png = New-IconBitmap 256
$png.Save((Join-Path (Split-Path $Out) 'icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$png.Dispose()
Write-Host "Wrote $Out"
