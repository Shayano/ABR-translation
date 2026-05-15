#Requires -Version 5.1
<#
    A Bumpy Ride - Traducción al español: desinstalador
    Restaura los archivos vanilla desde _ABRes_backup/.
#>

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Step([string]$msg) { Write-Host "`n[PASO] $msg" -ForegroundColor Cyan }
function Write-Info([string]$msg) { Write-Host "  $msg" -ForegroundColor Gray }
function Write-OK([string]$msg)   { Write-Host "  OK: $msg" -ForegroundColor Green }
function Fail([string]$msg) {
    Write-Host "`nERROR: $msg" -ForegroundColor Red
    Write-Host "`nPulsa Intro para cerrar..." -ForegroundColor Gray
    [void](Read-Host); exit 1
}

Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  A Bumpy Ride - Desinstalación de la traducción al español" -ForegroundColor Magenta
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
    Write-Host "Pega la ruta del juego (carpeta que contiene ABumpyRide\Content\Paks)" -ForegroundColor Yellow
    $manual = (Read-Host 'Ruta').Trim('" ')
    if (Test-GameRoot $manual) { $GameRoot = $manual } else { Fail "Ruta inválida." }
}

$PaksDir   = Join-Path $GameRoot 'ABumpyRide\Content\Paks'
$BackupDir = Join-Path $PaksDir '_ABRes_backup'

Write-Step "Restauración desde la copia de seguridad"
if (-not (Test-Path $BackupDir)) {
    Fail "No se encontró ninguna copia de seguridad en $BackupDir. Si la has eliminado, usa Steam > Propiedades > Archivos locales > Verificar integridad."
}

foreach ($f in @('ABumpyRide-Windows.utoc', 'ABumpyRide-Windows.ucas', 'ABumpyRide-Windows.pak')) {
    $src = Join-Path $BackupDir $f
    $dst = Join-Path $PaksDir $f
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-OK "Restaurado: $f"
    }
}

$resp = Read-Host "¿Eliminar también la carpeta de copia de seguridad ($BackupDir)? [s/N]"
if ($resp -match '^[sSyY]') {
    Remove-Item $BackupDir -Recurse -Force
    Write-OK "Copia de seguridad eliminada."
} else {
    Write-Info "Copia de seguridad conservada en $BackupDir"
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Desinstalación completada." -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Pulsa Intro para cerrar..." -ForegroundColor Gray
[void](Read-Host)
