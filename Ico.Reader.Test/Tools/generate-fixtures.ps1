<#
.SYNOPSIS
    Regenerates the ICO test fixtures and their reference pixel data.

.DESCRIPTION
    Fixtures are committed to the repository, so tests never need these tools. Run this only when
    the fixture set changes.

    Requires ImageMagick (magick), icoutils (icotool) and the Windows SDK resource compiler
    (rc.exe). Paths are taken from -Magick / -IcoTool / -Rc, then the matching environment
    variable, then PATH, then a few well-known install locations.

    Reference pixels come from `icotool -x`, an independent ICO implementation, so the decoder is
    checked against something other than itself. They are stored gzipped as raw RGBA.

.EXAMPLE
    pwsh ./generate-fixtures.ps1
#>
[CmdletBinding()]
param(
    [string]$Magick = $env:ICO_READER_MAGICK,
    [string]$IcoTool = $env:ICO_READER_ICOTOOL,
    [string]$Rc = $env:ICO_READER_RC
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$resourcesDir = Join-Path $PSScriptRoot '..\Resources' | Resolve-Path
$sourcePng = Join-Path $resourcesDir 'sample.png'
$icoDir = Join-Path $resourcesDir 'Ico'
$curDir = Join-Path $resourcesDir 'Cur'
$expectedDir = Join-Path $resourcesDir 'Expected'
$peDir = Join-Path $resourcesDir 'Pe'
$workDir = Join-Path ([System.IO.Path]::GetTempPath()) "ico-reader-fixtures-$PID"

$Sizes = @(16, 32, 48, 256)
$BitDepths = @(1, 4, 8, 24, 32)
$MaskedSize = 32

#region tool discovery

function Resolve-Tool {
    param(
        [string]$Explicit,
        [string]$CommandName,
        [string[]]$Probe,
        [string]$Hint
    )

    if ($Explicit) {
        if (-not (Test-Path $Explicit)) { throw "$CommandName not found at '$Explicit'." }
        return (Resolve-Path $Explicit).Path
    }

    $onPath = Get-Command $CommandName -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    foreach ($candidate in $Probe) {
        $found = @(Get-ChildItem -Path $candidate -ErrorAction SilentlyContinue | Sort-Object FullName -Descending)
        if ($found.Count -gt 0) { return $found[0].FullName }
    }

    throw "$CommandName not found. $Hint"
}

$Magick = Resolve-Tool -Explicit $Magick -CommandName 'magick' -Probe @(
    'C:\Program Files\ImageMagick-*\magick.exe'
) -Hint 'Install ImageMagick or set ICO_READER_MAGICK.'

$IcoTool = Resolve-Tool -Explicit $IcoTool -CommandName 'icotool' -Probe @(
    "$env:USERPROFILE\Downloads\icoutils-*\bin\icotool.exe",
    'C:\Tools\icoutils-*\bin\icotool.exe'
) -Hint 'Download icoutils (win32 build) or set ICO_READER_ICOTOOL.'

$Rc = Resolve-Tool -Explicit $Rc -CommandName 'rc' -Probe @(
    'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\rc.exe'
) -Hint 'Install the Windows 10/11 SDK or set ICO_READER_RC.'

Write-Host "magick  : $Magick"
Write-Host "icotool : $IcoTool"
Write-Host "rc      : $Rc"

#endregion

#region helpers

function Invoke-Tool {
    param([string]$Path, [string[]]$Arguments)

    # icotool warns on stderr for lossy conversions we intentionally ask for, so only the exit
    # code decides success.
    $output = & $Path @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "$([System.IO.Path]::GetFileName($Path)) $($Arguments -join ' ') failed with $LASTEXITCODE :`n$output"
    }
}

function New-SourcePng {
    param([int]$Size, [int]$BitDepth, [switch]$Masked, [string]$Destination)

    $sourceImage = if ($Masked) { Join-Path $workDir 'sample_masked.png' } else { $sourcePng }
    $arguments = @($sourceImage, '-resize', "${Size}x${Size}")

    # icotool refuses to reduce bit depth, so ImageMagick has to produce the palette first.
    # Alpha has to survive for masked variants: icotool derives the AND mask from it.
    $target = switch ($BitDepth) {
        1 { $arguments += @('-colors', '2') ; "PNG8:$Destination" }
        4 { $arguments += @('-colors', '16') ; "PNG8:$Destination" }
        8 { $arguments += @('-colors', '256'); "PNG8:$Destination" }
        24 { if (-not $Masked) { $arguments += @('-alpha', 'off') }; $Destination }
        default { $Destination }
    }

    Invoke-Tool -Path $Magick -Arguments ($arguments + $target)
}

function Get-IcoEntries {
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $count = [BitConverter]::ToUInt16($bytes, 4)
    $entries = [System.Collections.Generic.List[object]]::new()

    for ($i = 0; $i -lt $count; $i++) {
        $entryOffset = 6 + ($i * 16)
        $imageSize = [BitConverter]::ToUInt32($bytes, $entryOffset + 8)
        $imageOffset = [BitConverter]::ToUInt32($bytes, $entryOffset + 12)

        # Range slicing yields Object[], which BinaryWriter.Write binds to the wrong overload.
        $header = [byte[]]::new(16)
        [Array]::Copy($bytes, $entryOffset, $header, 0, 16)
        $image = [byte[]]::new($imageSize)
        [Array]::Copy($bytes, $imageOffset, $image, 0, $imageSize)

        $entries.Add([pscustomobject]@{ Header = $header; Image = $image })
    }

    return $entries
}

function Merge-Ico {
    param([string[]]$Inputs, [string]$Destination)

    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($path in $Inputs) {
        foreach ($entry in Get-IcoEntries -Path $path) { $entries.Add($entry) }
    }

    $stream = [System.IO.File]::Create($Destination)
    try {
        $writer = New-Object System.IO.BinaryWriter($stream)
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$entries.Count)

        $imageOffset = 6 + ($entries.Count * 16)
        foreach ($entry in $entries) {
            $header = $entry.Header.Clone()
            [BitConverter]::GetBytes([uint32]$entry.Image.Length).CopyTo($header, 8)
            [BitConverter]::GetBytes([uint32]$imageOffset).CopyTo($header, 12)
            $writer.Write($header)
            $imageOffset += $entry.Image.Length
        }

        foreach ($entry in $entries) { $writer.Write($entry.Image) }
        $writer.Flush()
    }
    finally {
        $stream.Dispose()
    }
}

function Write-ExpectedPixels {
    param([string]$IcoPath)

    $name = [System.IO.Path]::GetFileNameWithoutExtension($IcoPath)
    $count = (Get-IcoEntries -Path $IcoPath).Count

    for ($i = 1; $i -le $count; $i++) {
        $extracted = Join-Path $workDir "extract_${name}_$i.png"
        $raw = Join-Path $workDir "extract_${name}_$i.rgba"

        Invoke-Tool -Path $IcoTool -Arguments @('-x', '-i', "$i", '-o', $extracted, $IcoPath)
        Invoke-Tool -Path $Magick -Arguments @($extracted, '-depth', '8', "RGBA:$raw")

        $bytes = [System.IO.File]::ReadAllBytes($raw)
        $destination = Join-Path $expectedDir "$name.$($i - 1).rgba.gz"

        $fileStream = [System.IO.File]::Create($destination)
        try {
            $gzip = New-Object System.IO.Compression.GZipStream($fileStream, [System.IO.Compression.CompressionLevel]::Optimal)
            try { $gzip.Write($bytes, 0, $bytes.Length) } finally { $gzip.Dispose() }
        }
        finally {
            $fileStream.Dispose()
        }
    }
}

#endregion

foreach ($directory in @($icoDir, $expectedDir, $peDir, $workDir)) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}
Get-ChildItem -Path $icoDir, $expectedDir -File -ErrorAction SilentlyContinue | Remove-Item -Force

try {
    Write-Host "`nBuilding masked source (transparent corners exercise the AND mask)..."
    $maskPath = Join-Path $workDir 'mask.png'
    $maskedSource = Join-Path $workDir 'sample_masked.png'
    Invoke-Tool -Path $Magick -Arguments @('-size', '256x256', 'xc:black', '-fill', 'white', '-draw', 'circle 128,128 128,16', $maskPath)
    Invoke-Tool -Path $Magick -Arguments @($sourcePng, $maskPath, '-alpha', 'off', '-compose', 'CopyOpacity', '-composite', $maskedSource)

    $generated = [System.Collections.Generic.List[string]]::new()

    Write-Host "`nGenerating single-image fixtures..."
    foreach ($size in $Sizes) {
        foreach ($bitDepth in $BitDepths) {
            $intermediate = Join-Path $workDir "src_${size}_$bitDepth.png"
            $target = Join-Path $icoDir "icon_${size}_${bitDepth}bpp.ico"

            New-SourcePng -Size $size -BitDepth $bitDepth -Destination $intermediate
            Invoke-Tool -Path $IcoTool -Arguments @('-c', '-b', "$bitDepth", '-o', $target, $intermediate)

            $generated.Add($target)
            Write-Host "  $([System.IO.Path]::GetFileName($target))"
        }
    }

    Write-Host "`nGenerating masked fixtures..."
    foreach ($bitDepth in $BitDepths) {
        $intermediate = Join-Path $workDir "srcmask_${MaskedSize}_$bitDepth.png"
        $target = Join-Path $icoDir "icon_${MaskedSize}_${bitDepth}bpp_masked.ico"

        New-SourcePng -Size $MaskedSize -BitDepth $bitDepth -Masked -Destination $intermediate
        Invoke-Tool -Path $IcoTool -Arguments @('-c', '-b', "$bitDepth", '-o', $target, $intermediate)

        $generated.Add($target)
        Write-Host "  $([System.IO.Path]::GetFileName($target))"
    }

    Write-Host "`nGenerating multi-image fixtures..."
    $pngEmbedded = Join-Path $icoDir 'icon_256_png.ico'
    Invoke-Tool -Path $IcoTool -Arguments @('-c', '-r', $sourcePng, '-o', $pngEmbedded)
    $generated.Add($pngEmbedded)

    $multi = Join-Path $icoDir 'icon_multi.ico'
    Merge-Ico -Destination $multi -Inputs @(
        (Join-Path $icoDir 'icon_16_32bpp.ico'),
        (Join-Path $icoDir 'icon_32_32bpp.ico'),
        (Join-Path $icoDir 'icon_48_32bpp.ico')
    )
    $generated.Add($multi)

    # Mixed depths in one file, the way a real application icon is authored.
    $multiMixed = Join-Path $icoDir 'icon_multi_mixed.ico'
    Merge-Ico -Destination $multiMixed -Inputs @(
        (Join-Path $icoDir 'icon_16_4bpp.ico'),
        (Join-Path $icoDir 'icon_32_8bpp.ico'),
        (Join-Path $icoDir 'icon_48_32bpp.ico')
    )
    $generated.Add($multiMixed)

    # BMP entries plus a PNG-compressed 256px entry, the modern Windows layout.
    $multiPngBmp = Join-Path $icoDir 'icon_multi_png_bmp.ico'
    Merge-Ico -Destination $multiPngBmp -Inputs @(
        (Join-Path $icoDir 'icon_32_32bpp.ico'),
        (Join-Path $icoDir 'icon_48_32bpp.ico'),
        $pngEmbedded
    )
    $generated.Add($multiPngBmp)

    foreach ($path in @($multi, $multiMixed, $multiPngBmp)) {
        Write-Host "  $([System.IO.Path]::GetFileName($path))"
    }

    # Cursors are hand-authored (see CURSOR-FIXTURES.md); only their reference pixels are generated.
    $cursors = @(Get-ChildItem -Path $curDir -Filter '*.cur' -File -ErrorAction SilentlyContinue | Sort-Object Name)
    foreach ($cursor in $cursors) { $generated.Add($cursor.FullName) }

    Write-Host "`nExtracting reference pixels..."
    foreach ($path in $generated) {
        Write-ExpectedPixels -IcoPath $path
    }

    Write-Host "`nCompiling PE fixture resources..."
    $rcPath = Join-Path $peDir 'Fixtures.rc'
    $resPath = Join-Path $peDir 'Fixtures.res'

    # Group ids are what IcoData exposes as group names, so the tests reference these numbers.
    $rcLines = @(
        '// Generated by Tools/generate-fixtures.ps1. Do not edit by hand.',
        '',
        '1 ICON "../Ico/icon_32_32bpp.ico"',
        '2 ICON "../Ico/icon_multi.ico"',
        '3 ICON "../Ico/icon_16_8bpp.ico"',
        '4 ICON "../Ico/icon_multi_png_bmp.ico"',
        ''
    )

    if ($cursors.Count -gt 0) {
        $rcLines += @(
            '1 CURSOR "../Cur/cursor_32_32bpp.cur"',
            '2 CURSOR "../Cur/cursor_multi.cur"',
            '3 CURSOR "../Cur/cursor_16_8bpp.cur"'
        )
    }

    Set-Content -Path $rcPath -Value $rcLines -Encoding ASCII
    Invoke-Tool -Path $Rc -Arguments @('/nologo', '/fo', $resPath, $rcPath)

    $icoBytes = (Get-ChildItem $icoDir -File | Measure-Object Length -Sum).Sum
    $expectedBytes = (Get-ChildItem $expectedDir -File | Measure-Object Length -Sum).Sum
    Write-Host "`nDone."
    Write-Host ("  ico      {0,3} files, {1,7:N0} KB" -f (Get-ChildItem $icoDir -File).Count, ($icoBytes / 1KB))
    Write-Host ("  cur      {0,3} files (hand-authored, reference pixels regenerated)" -f $cursors.Count)
    Write-Host ("  expected {0,3} files, {1,7:N0} KB" -f (Get-ChildItem $expectedDir -File).Count, ($expectedBytes / 1KB))
    Write-Host ("  res          1 file,  {0,7:N0} KB" -f ((Get-Item $resPath).Length / 1KB))
}
finally {
    Remove-Item -Recurse -Force $workDir -ErrorAction SilentlyContinue
}
