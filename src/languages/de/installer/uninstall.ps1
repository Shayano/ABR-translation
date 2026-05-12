#Requires -Version 5.1
<#
    A Bumpy Ride — Deutsche Übersetzung: Deinstallation
    Stellt die vanilla-Dateien aus _ABRde_backup/ wieder her.
#>

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Step([string]$msg) { Write-Host "`n[SCHRITT] $msg" -ForegroundColor Cyan }
function Write-Info([string]$msg) { Write-Host "  $msg" -ForegroundColor Gray }
function Write-OK([string]$msg)   { Write-Host "  OK: $msg" -ForegroundColor Green }
function Fail([string]$msg) {
    Write-Host "`nFEHLER: $msg" -ForegroundColor Red
    Write-Host "`nDrücke die Eingabetaste, um zu beenden..." -ForegroundColor Gray
    [void](Read-Host); exit 1
}

Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  A Bumpy Ride — Deinstallation der deutschen Übersetzung" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host ""

function Test-GameRoot([string]$c) {
    if (-not $c -or -not (Test-Path $c)) { return $false }
    return (Test-Path (Join-Path $c 'ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc'))
}

$GameRoot = $null
$probe = $ScriptDir
for ($i = 0; $i -lt 6; $i++) {
    if (Test-GameRoot $probe) { $GameRoot = $probe; break }
    $parent = Split-Path -Parent $probe
    if (-not $parent -or $parent -eq $probe) { break }
    $probe = $parent
}
if (-not $GameRoot) {
    try {
        $sp = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath
        $vdf = Join-Path $sp 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            $libs = @($sp)
            foreach ($line in (Get-Content $vdf)) {
                if ($line -match '"path"\s+"([^"]+)"') { $libs += $Matches[1] -replace '\\\\','\' }
            }
            foreach ($lib in ($libs | Select-Object -Unique)) {
                $cand = Join-Path $lib 'steamapps\common\A Bumpy Ride'
                if (Test-GameRoot $cand) { $GameRoot = $cand; break }
            }
        }
    } catch {}
}
if (-not $GameRoot) {
    Write-Host "Füge den Spielpfad ein (der Ordner, der ABumpyRide\Content\Paks enthält)" -ForegroundColor Yellow
    $manual = (Read-Host 'Pfad').Trim('" ')
    if (Test-GameRoot $manual) { $GameRoot = $manual } else { Fail "Ungültiger Pfad." }
}

$PaksDir   = Join-Path $GameRoot 'ABumpyRide\Content\Paks'
$BackupDir = Join-Path $PaksDir '_ABRde_backup'

Write-Step "Wiederherstellung aus der Sicherung"
if (-not (Test-Path $BackupDir)) {
    Fail "Keine Sicherung in $BackupDir gefunden. Falls du sie gelöscht hast, nutze Steam > Eigenschaften > Lokale Dateien > Integrität überprüfen."
}

foreach ($f in @('ABumpyRide-Windows.utoc', 'ABumpyRide-Windows.ucas', 'ABumpyRide-Windows.pak')) {
    $src = Join-Path $BackupDir $f
    $dst = Join-Path $PaksDir $f
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-OK "Wiederhergestellt: $f"
    }
}

$resp = Read-Host "Soll der Sicherungsordner ($BackupDir) ebenfalls gelöscht werden? [j/N]"
if ($resp -match '^[jJyY]') {
    Remove-Item $BackupDir -Recurse -Force
    Write-OK "Sicherung gelöscht."
} else {
    Write-Info "Sicherung in $BackupDir behalten."
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Deinstallation abgeschlossen." -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Drücke die Eingabetaste, um zu beenden..." -ForegroundColor Gray
[void](Read-Host)
