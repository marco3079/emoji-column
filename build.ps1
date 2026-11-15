<#
.SYNOPSIS
  Builds Emoji Column with the C# compiler that ships with Windows (.NET Framework 4.8).
  No Visual Studio or .NET SDK needed.

.EXAMPLE
  .\build.ps1             # builds into .\dist
  .\build.ps1 -Zip        # also creates .\dist\EmojiColumn-<version>.zip
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$Zip
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dist = Join-Path $root 'dist'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "C# compiler not found at $csc (.NET Framework 4.8 is required)." }

New-Item -ItemType Directory -Force $dist | Out-Null

$common = @('/nologo', '/target:library', '/platform:anycpu', '/optimize+', '/warnaserror+', '/nowarn:1607',
            '/langversion:5', '/utf8output', '/codepage:65001')
if ($Configuration -eq 'Debug') { $common += '/debug:full', '/define:DEBUG' } else { $common += '/debug:pdbonly' }

$shared = Get-ChildItem (Join-Path $root 'src\Shared') -Filter *.cs | ForEach-Object FullName

function Invoke-Csc([string[]]$CscArgs) {
    & $csc @CscArgs
    if ($LASTEXITCODE -ne 0) { throw "csc failed with exit code $LASTEXITCODE" }
}

Write-Host '> EmojiColumn.Handler.dll'
$handlerSrc = Get-ChildItem (Join-Path $root 'src\Handler') -Filter *.cs | ForEach-Object FullName
Invoke-Csc ($common + @("/out:$dist\EmojiColumn.Handler.dll") + $shared + $handlerSrc)

Write-Host '> EmojiColumn.exe'
$appSrc = Get-ChildItem (Join-Path $root 'src\App') -Filter *.cs | ForEach-Object FullName
$appArgs = $common -replace '^/target:library$', '/target:winexe'
$appArgs += @(
    "/out:$dist\EmojiColumn.exe",
    "/win32icon:$root\assets\icon.ico",
    "/win32manifest:$root\src\App\app.manifest",
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll'
)
Invoke-Csc ($appArgs + $shared + $appSrc)

Copy-Item (Join-Path $root 'src\App\App.config') (Join-Path $dist 'EmojiColumn.exe.config') -Force
# Color emoji icons (Fluent Emoji, MIT) packed as Win32 resources in a resource-only DLL.
Write-Host '> EmojiColumn.Icons.dll'
$res = Join-Path $dist 'EmojiColumn.Icons.res'
& (Join-Path $root 'tools\New-IconResource.ps1') -SourceDir (Join-Path $root 'assets\emoji') -OutFile $res
$stub = Join-Path $dist 'IconsStub.cs'
Set-Content $stub '// Resource-only assembly: holds the color emoji icons drawn in the Emoji column.' -Encoding ascii
Invoke-Csc @('/nologo', '/target:library', '/optimize+', "/win32res:$res", "/out:$dist\EmojiColumn.Icons.dll", $stub)
Remove-Item $stub, $res

# Property schema: one <enum> per palette emoji, each pointing at its icon resource.
$palette = [regex]::Matches([IO.File]::ReadAllText((Join-Path $root 'src\Shared\EmojiPalette.cs')),
    'new\[\] \{ "([^"]+)", "([^"]+)" \}')
$iconsDll = Join-Path $dist 'EmojiColumn.Icons.dll'
$enums = for ($i = 0; $i -lt $palette.Count; $i++) {
    $value = $i + 1
    $res = if (Test-Path $iconsDll) { "{INSTALLDIR}\EmojiColumn.Icons.dll,-$(100 + $value)" } else { '%systemroot%\system32\imageres.dll,-1405' }
    $text = [Security.SecurityElement]::Escape($palette[$i].Groups[1].Value)
    "          <enum name=`"E$value`" value=`"$value`" text=`"$text`"><image res=`"$res`" /></enum>"
}
$schema = [IO.File]::ReadAllText((Join-Path $root 'schema\EmojiColumn.propdesc')).Replace('<!--EMOJI-ENUMS-->', ($enums -join "`r`n").TrimStart())
[IO.File]::WriteAllText((Join-Path $dist 'EmojiColumn.propdesc'), $schema, (New-Object Text.UTF8Encoding $false))
Write-Host "> EmojiColumn.propdesc ($($palette.Count) color emojis)"
Copy-Item (Join-Path $root 'LICENSE') (Join-Path $dist 'LICENSE.txt') -Force
Copy-Item (Join-Path $root 'THIRD-PARTY-NOTICES.md') (Join-Path $dist 'THIRD-PARTY-NOTICES.txt') -Force

if ($Zip) {
    $version = (Get-Item "$dist\EmojiColumn.exe").VersionInfo.ProductVersion
    $zipPath = Join-Path $dist "EmojiColumn-$version.zip"
    Remove-Item $zipPath -ErrorAction SilentlyContinue
    $files = 'EmojiColumn.exe', 'EmojiColumn.exe.config', 'EmojiColumn.Handler.dll', 'EmojiColumn.Icons.dll', 'EmojiColumn.propdesc', 'LICENSE.txt', 'THIRD-PARTY-NOTICES.txt' |
        ForEach-Object { Join-Path $dist $_ }
    Compress-Archive -Path $files -DestinationPath $zipPath
    $hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $(Split-Path $zipPath -Leaf)" | Set-Content -Encoding ascii "$zipPath.sha256"
    Write-Host "> $zipPath"
}

Write-Host "Build succeeded: $dist"
