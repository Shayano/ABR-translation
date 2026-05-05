#Requires -Version 5.1
<#
    A Bumpy Ride — Traduction française : désinstalleur
    Restaure les fichiers vanilla depuis _ABRfr_backup/.
#>

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Step([string]$msg) { Write-Host "`n[ÉTAPE] $msg" -ForegroundColor Cyan }
function Write-Info([string]$msg) { Write-Host "  $msg" -ForegroundColor Gray }
function Write-OK([string]$msg)   { Write-Host "  OK : $msg" -ForegroundColor Green }
function Fail([string]$msg) {
    Write-Host "`nERREUR : $msg" -ForegroundColor Red
    Write-Host "`nAppuyez sur Entrée pour fermer..." -ForegroundColor Gray
    [void](Read-Host); exit 1
}

Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  A Bumpy Ride — Désinstallation de la traduction française" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host ""

# Détection du dossier du jeu (mêmes 3 modes que install.ps1)
function Test-GameRoot([string]$c) {
    if (-not $c -or -not (Test-Path $c)) { return $false }
    return (Test-Path (Join-Path $c 'ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc'))
}

$GameRoot = $null
$probe = $ScriptDir
for ($i = 0; $i -lt 6; $i++) {
    if (Test-GameRoot $probe) { $GameRoot = $probe; break }
    $parent = Split-Path -Parent $probe
    if ($parent -eq $probe) { break }
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
    Write-Host "Collez le chemin du jeu (dossier qui contient ABumpyRide\Content\Paks)" -ForegroundColor Yellow
    $manual = (Read-Host 'Chemin').Trim('" ')
    if (Test-GameRoot $manual) { $GameRoot = $manual } else { Fail "Chemin invalide." }
}

$PaksDir   = Join-Path $GameRoot 'ABumpyRide\Content\Paks'
$BackupDir = Join-Path $PaksDir '_ABRfr_backup'

Write-Step "Restauration depuis la sauvegarde"
if (-not (Test-Path $BackupDir)) {
    Fail "Aucune sauvegarde trouvée à $BackupDir. Si vous avez supprimé la sauvegarde, utilisez Steam > Propriétés > Fichiers locaux > Vérifier l'intégrité."
}

foreach ($f in @('ABumpyRide-Windows.utoc', 'ABumpyRide-Windows.ucas', 'ABumpyRide-Windows.pak')) {
    $src = Join-Path $BackupDir $f
    $dst = Join-Path $PaksDir $f
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-OK "Restauré : $f"
    }
}

$resp = Read-Host "Supprimer aussi le dossier de sauvegarde ($BackupDir) ? [o/N]"
if ($resp -match '^[oOyY]') {
    Remove-Item $BackupDir -Recurse -Force
    Write-OK "Sauvegarde supprimée."
} else {
    Write-Info "Sauvegarde conservée à $BackupDir"
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Désinstallation terminée." -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Appuyez sur Entrée pour fermer..." -ForegroundColor Gray
[void](Read-Host)
