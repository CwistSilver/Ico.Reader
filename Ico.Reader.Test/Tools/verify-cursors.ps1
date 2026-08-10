<#
.SYNOPSIS
    Checks hand-authored .cur fixtures against the specification in CURSOR-FIXTURES.md.

.DESCRIPTION
    Cursor editors vary in what they write, and some tools silently save a cursor as an icon
    (type 1). This verifies the bytes rather than trusting the editor: file type, entry count,
    per-entry size, colour depth, hotspot and payload format.

    Run it after authoring the files and before handing them over.

.EXAMPLE
    pwsh ./verify-cursors.ps1
#>
[CmdletBinding()]
param(
    [string]$Path = (Join-Path $PSScriptRoot '..\Resources\Cur')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Each entry is @(width, height, bitCount, hotspotX, hotspotY); see CURSOR-FIXTURES.md.
$Expected = @(
    [pscustomobject]@{ File = 'cursor_32_1bpp.cur'; Entries = @(, @(32, 32, 1, 0, 0)) }
    [pscustomobject]@{ File = 'cursor_32_4bpp.cur'; Entries = @(, @(32, 32, 4, 5, 5)) }
    [pscustomobject]@{ File = 'cursor_32_8bpp.cur'; Entries = @(, @(32, 32, 8, 7, 23)) }
    [pscustomobject]@{ File = 'cursor_32_24bpp.cur'; Entries = @(, @(32, 32, 24, 31, 31)) }
    [pscustomobject]@{ File = 'cursor_32_32bpp.cur'; Entries = @(, @(32, 32, 32, 10, 20)) }
    [pscustomobject]@{ File = 'cursor_16_8bpp.cur'; Entries = @(, @(16, 16, 8, 1, 2)) }
    [pscustomobject]@{ File = 'cursor_48_32bpp.cur'; Entries = @(, @(48, 48, 32, 24, 0)) }
    [pscustomobject]@{ File = 'cursor_256_32bpp.cur'; Entries = @(, @(256, 256, 32, 128, 200)) }
    [pscustomobject]@{ File = 'cursor_32_8bpp_masked.cur'; Entries = @(, @(32, 32, 8, 3, 29)) }
    [pscustomobject]@{ File = 'cursor_multi.cur'; Entries = @(@(16, 16, 32, 4, 6), @(32, 32, 32, 8, 12), @(48, 48, 32, 12, 18)) }
    [pscustomobject]@{ File = 'cursor_multi_mixed.cur'; Entries = @(@(16, 16, 4, 4, 6), @(32, 32, 8, 8, 12), @(48, 48, 32, 12, 18)) }
)

function Read-CurEntries {
    param([string]$File)

    $bytes = [System.IO.File]::ReadAllBytes($File)
    $problems = [System.Collections.Generic.List[string]]::new()

    if ($bytes.Length -lt 6) {
        $problems.Add('file is shorter than a header')
        return [pscustomobject]@{ Entries = @(); Problems = $problems }
    }

    $reserved = [BitConverter]::ToUInt16($bytes, 0)
    $type = [BitConverter]::ToUInt16($bytes, 2)
    $count = [BitConverter]::ToUInt16($bytes, 4)

    if ($reserved -ne 0) { $problems.Add("reserved field is $reserved, must be 0") }
    if ($type -ne 2) { $problems.Add("file type is $type, must be 2 (the editor saved an icon, not a cursor)") }
    if ($count -eq 0) { $problems.Add('the directory is empty') }

    $entries = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $count; $i++) {
        $offset = 6 + ($i * 16)
        if ($offset + 16 -gt $bytes.Length) {
            $problems.Add("entry $i is truncated")
            break
        }

        $width = $bytes[$offset]
        $height = $bytes[$offset + 1]
        $imageSize = [int][BitConverter]::ToUInt32($bytes, $offset + 8)
        $imageOffset = [int][BitConverter]::ToUInt32($bytes, $offset + 12)

        if ($imageOffset + $imageSize -gt $bytes.Length) {
            $problems.Add("entry $i points past the end of the file")
            continue
        }

        $isPng = $bytes[$imageOffset] -eq 0x89 -and $bytes[$imageOffset + 1] -eq 0x50
        $bitCount = if ($isPng) { 32 } else { [BitConverter]::ToUInt16($bytes, $imageOffset + 14) }

        if (-not $isPng -and [BitConverter]::ToInt32($bytes, $imageOffset) -ne 40) {
            $problems.Add("entry $i is neither a 40 byte BMP header nor a PNG")
        }

        $entries.Add([pscustomobject]@{
                Width    = if ($width -eq 0) { 256 } else { [int]$width }
                Height   = if ($height -eq 0) { 256 } else { [int]$height }
                BitCount = [int]$bitCount
                HotspotX = [int][BitConverter]::ToUInt16($bytes, $offset + 4)
                HotspotY = [int][BitConverter]::ToUInt16($bytes, $offset + 6)
                Format   = if ($isPng) { 'PNG' } else { 'BMP' }
            })
    }

    return [pscustomobject]@{ Entries = $entries; Problems = $problems }
}

if (-not (Test-Path $Path)) {
    Write-Host "No cursor directory at '$Path'. Create it and save the fixtures there." -ForegroundColor Yellow
    exit 1
}

$failed = 0
foreach ($specification in $Expected) {
    $file = $specification.File
    $fullPath = Join-Path $Path $file
    if (-not (Test-Path $fullPath)) {
        Write-Host "MISSING  $file" -ForegroundColor Yellow
        $failed++
        continue
    }

    $result = Read-CurEntries -File $fullPath
    $problems = $result.Problems
    $actual = @($result.Entries)
    $wanted = @($specification.Entries)

    if ($actual.Count -ne $wanted.Count) {
        $problems.Add("has $($actual.Count) images, expected $($wanted.Count)")
    }
    else {
        for ($i = 0; $i -lt $wanted.Count; $i++) {
            $w = $wanted[$i]
            $a = $actual[$i]
            if ($a.Width -ne $w[0] -or $a.Height -ne $w[1]) {
                $problems.Add("image $i is $($a.Width)x$($a.Height), expected $($w[0])x$($w[1])")
            }
            if ($a.BitCount -ne $w[2]) {
                $problems.Add("image $i is $($a.BitCount) bpp, expected $($w[2]) bpp")
            }
            if ($a.HotspotX -ne $w[3] -or $a.HotspotY -ne $w[4]) {
                $problems.Add("image $i hotspot is ($($a.HotspotX),$($a.HotspotY)), expected ($($w[3]),$($w[4]))")
            }
        }
    }

    if ($problems.Count -eq 0) {
        $summary = ($actual | ForEach-Object { "$($_.Width)x$($_.Height) $($_.BitCount)bpp $($_.Format) hotspot($($_.HotspotX),$($_.HotspotY))" }) -join ' | '
        Write-Host "OK       $file  $summary" -ForegroundColor Green
    }
    else {
        Write-Host "FAILED   $file" -ForegroundColor Red
        foreach ($problem in $problems) { Write-Host "           - $problem" -ForegroundColor Red }
        $failed++
    }
}

Write-Host ''
if ($failed -eq 0) {
    Write-Host "All $($Expected.Count) cursor fixtures match the specification." -ForegroundColor Green
    exit 0
}

Write-Host "$failed of $($Expected.Count) cursor fixtures are missing or wrong." -ForegroundColor Red
exit 1
