#Requires -Version 5.1
<#
    A Bumpy Ride — Deutsche Übersetzung
    PowerShell-Installer

    Verwendung:
      1) Automatischer Modus: Doppelklick auf install.ps1 (Rechtsklick > "Mit PowerShell ausführen")
         Oder in einer PowerShell-Konsole: .\install.ps1
         Der Installer erkennt deine Steam-Installation automatisch.

      2) Manueller Modus: Kopiere den Ordner patch-de in das Spielverzeichnis
         (z. B. F:\Steam\steamapps\common\A Bumpy Ride\patch-de),
         dann starte install.ps1 wie oben. Der Installer erkennt automatisch,
         dass er sich bereits im Spielverzeichnis befindet.

      3) Manueller Modus mit Eingabeaufforderung: Falls die automatische Erkennung
         fehlschlägt, fragt dich der Installer nach dem Spielpfad.
#>

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ManifestPath = Join-Path $ScriptDir 'manifest.json'
$RetocExe   = Join-Path $ScriptDir 'retoc.exe'
$AssetsDir  = Join-Path $ScriptDir 'patched_assets'

# ----------------------------------------------------------------------------
# Helpers
# ----------------------------------------------------------------------------
function Write-Step([string]$msg)  { Write-Host "`n[SCHRITT] $msg" -ForegroundColor Cyan }
function Write-Info([string]$msg)  { Write-Host "  $msg" -ForegroundColor Gray }
function Write-OK([string]$msg)    { Write-Host "  OK: $msg" -ForegroundColor Green }
function Write-Warn([string]$msg)  { Write-Host "  HINWEIS: $msg" -ForegroundColor Yellow }
function Fail([string]$msg) {
    Write-Host "`nFEHLER: $msg" -ForegroundColor Red
    Write-Host "`nDrücke die Eingabetaste, um zu beenden..." -ForegroundColor Gray
    [void](Read-Host)
    exit 1
}

function Get-Sha256($path) {
    (Get-FileHash -Algorithm SHA256 -Path $path).Hash.ToLower()
}

# Wrapper für retoc — retoc gibt seine Konfiguration auf stderr aus,
# was unter PowerShell 5.1 + ErrorActionPreference Stop als fataler Fehler behandelt wird.
function Invoke-Retoc {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $oldEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $RetocExe @Arguments 2>&1 | Out-String
    } finally {
        $ErrorActionPreference = $oldEAP
    }
    return $output
}

# ----------------------------------------------------------------------------
# Vorwort
# ----------------------------------------------------------------------------
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  A Bumpy Ride — Deutsche Übersetzung (Mod ABR-de)" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host ""

if (-not (Test-Path $ManifestPath)) { Fail "manifest.json wurde neben install.ps1 nicht gefunden." }
if (-not (Test-Path $RetocExe))     { Fail "retoc.exe wurde neben install.ps1 nicht gefunden." }
if (-not (Test-Path $AssetsDir))    { Fail "patched_assets/ wurde neben install.ps1 nicht gefunden." }

$Manifest = Get-Content $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
Write-Info "Mod: $($Manifest.mod_name)"
Write-Info "Version: $($Manifest.mod_version) (veröffentlicht am $($Manifest.mod_date))"
Write-Info "Autor: $($Manifest.mod_author)"

# ----------------------------------------------------------------------------
# Schritt 1 — Erkennung des Spielordners
# ----------------------------------------------------------------------------
Write-Step "Erkennung des Spielordners"

function Test-GameRoot([string]$candidate) {
    if (-not $candidate -or -not (Test-Path $candidate)) { return $false }
    return (Test-Path (Join-Path $candidate 'ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc'))
}

$GameRoot = $null

# 1a — Manueller Modus: vom ScriptDir aus nach oben gehen
$probe = $ScriptDir
for ($i = 0; $i -lt 6; $i++) {
    if (Test-GameRoot $probe) { $GameRoot = $probe; break }
    $parent = Split-Path -Parent $probe
    if (-not $parent -or $parent -eq $probe) { break }
    $probe = $parent
}
if ($GameRoot) {
    Write-OK "Manueller Modus erkannt: $GameRoot"
}

# 1b — Automatischer Modus: Steam-Registry + libraryfolders.vdf
if (-not $GameRoot) {
    $steamPath = $null
    try {
        $steamPath = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath
    } catch {
        try { $steamPath = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam' -Name InstallPath -ErrorAction Stop).InstallPath } catch {}
    }
    if ($steamPath) {
        Write-Info "Steam erkannt: $steamPath"
        $vdf = Join-Path $steamPath 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            $libs = @($steamPath)
            $vdfLines = Get-Content $vdf
            foreach ($line in $vdfLines) {
                if ($line -match '"path"\s+"([^"]+)"') {
                    $libs += $Matches[1] -replace '\\\\', '\'
                }
            }
            foreach ($lib in $libs | Select-Object -Unique) {
                $candidate = Join-Path $lib 'steamapps\common\A Bumpy Ride'
                if (Test-GameRoot $candidate) { $GameRoot = $candidate; break }
            }
        }
    }
    if ($GameRoot) {
        Write-OK "Automatischer Modus erkannt: $GameRoot"
    } else {
        Write-Warn "Automatische Erkennung fehlgeschlagen."
    }
}

# 1c — Fallback: manuelle Eingabe
if (-not $GameRoot) {
    Write-Host ""
    Write-Host "Füge den vollständigen Pfad zum Spielordner ein" -ForegroundColor Yellow
    Write-Host "(der Ordner, der ABumpyRide\Content\Paks\ enthält)" -ForegroundColor Yellow
    Write-Host "Beispiel: F:\Steam\steamapps\common\A Bumpy Ride" -ForegroundColor Gray
    $manual = Read-Host 'Pfad'
    $manual = $manual.Trim('" ')
    if (Test-GameRoot $manual) {
        $GameRoot = $manual
        Write-OK "Manueller Pfad bestätigt: $GameRoot"
    } else {
        Fail "Der angegebene Pfad enthält kein ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc."
    }
}

$PaksDir = Join-Path $GameRoot 'ABumpyRide\Content\Paks'

# ----------------------------------------------------------------------------
# Schritt 2 — Integritätsprüfung des Spiels
# ----------------------------------------------------------------------------
Write-Step "Integritätsprüfung des Spiels"

$utocLive = Join-Path $PaksDir 'ABumpyRide-Windows.utoc'
$ucasLive = Join-Path $PaksDir 'ABumpyRide-Windows.ucas'
$pakLive  = Join-Path $PaksDir 'ABumpyRide-Windows.pak'

$utocSize = (Get-Item $utocLive).Length
$ucasSize = (Get-Item $ucasLive).Length
$expectedUtocSize = $Manifest.target_game.vanilla_files.'ABumpyRide-Windows.utoc'.size
$expectedUcasSize = $Manifest.target_game.vanilla_files.'ABumpyRide-Windows.ucas'.size

Write-Info "ABumpyRide-Windows.utoc: $('{0:N0}' -f $utocSize) Bytes (vanilla erwartet: $('{0:N0}' -f $expectedUtocSize))"
Write-Info "ABumpyRide-Windows.ucas: $('{0:N0}' -f $ucasSize) Bytes (vanilla erwartet: $('{0:N0}' -f $expectedUcasSize))"

if ($utocSize -ne $expectedUtocSize -or $ucasSize -ne $expectedUcasSize) {
    Write-Warn "Die installierten Spieldateien stimmen nicht exakt mit den vanilla-Dateien überein."
    Write-Warn "Mögliche Ursachen:"
    Write-Warn "  (a) Der Mod ABR-de ist bereits installiert (führe zuerst uninstall.ps1 aus)"
    Write-Warn "  (b) Steam hat das Spiel aktualisiert (dieser Mod zielt auf die ursprüngliche Version)"
    Write-Warn "  (c) Ein anderer Mod ist installiert"
    Write-Host ""
    $resp = Read-Host "Möchtest du die Installation trotzdem versuchen? [j/N]"
    if ($resp -notmatch '^[jJyY]') { Fail "Installation vom Benutzer abgebrochen." }
    Write-Warn "Erzwungene Fortsetzung — Risiko von Fehlern oder unerwartetem Verhalten."
} else {
    Write-OK "Das Spiel entspricht der erwarteten vanilla-Version."
}

# ----------------------------------------------------------------------------
# Schritt 3 — Prüfung des freien Speicherplatzes
# ----------------------------------------------------------------------------
Write-Step "Prüfung des freien Speicherplatzes"

$tempRoot = $env:TEMP
$drive = (Get-Item $tempRoot).PSDrive
$freeGB = [math]::Round($drive.Free / 1GB, 1)
$requiredGB = [int]$Manifest.install_requirements.free_disk_space_gb
$driveLetter = $drive.Name + ':'
Write-Info "Temporäres Laufwerk ($driveLetter): $freeGB GB frei (benötigt: $requiredGB GB)"
if ($freeGB -lt $requiredGB) {
    Fail "Nicht genug freier Speicher auf Laufwerk $driveLetter für die Installations-Pipeline."
}

# ----------------------------------------------------------------------------
# Schritt 4 — Sicherung
# ----------------------------------------------------------------------------
Write-Step "Sicherung der vanilla-Dateien"
$BackupDir = Join-Path $PaksDir '_ABRde_backup'
if (Test-Path $BackupDir) {
    Write-Info "Sicherung existiert bereits — sie wird nicht überschrieben (die ursprüngliche Sicherung ist die zuverlässigste)."
} else {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
    Copy-Item $utocLive (Join-Path $BackupDir 'ABumpyRide-Windows.utoc') -Force
    Copy-Item $ucasLive (Join-Path $BackupDir 'ABumpyRide-Windows.ucas') -Force
    Copy-Item $pakLive  (Join-Path $BackupDir 'ABumpyRide-Windows.pak')  -Force
    Write-OK "Sicherung in $BackupDir erstellt."
}

# ----------------------------------------------------------------------------
# Schritt 5 — retoc-Pipeline
# ----------------------------------------------------------------------------
Write-Step "Erstellung des übersetzten Containers (kann 3 bis 5 Minuten dauern)"

$WorkDir = Join-Path $env:TEMP 'ABRde_install'
if (Test-Path $WorkDir) { Remove-Item $WorkDir -Recurse -Force }
New-Item -ItemType Directory -Path $WorkDir | Out-Null

$LegacyDir   = Join-Path $WorkDir 'legacy'
$ZenUtoc     = Join-Path $WorkDir 'de.utoc'
$ZenChunks   = Join-Path $WorkDir 'zen_chunks'
$RawChunks   = Join-Path $WorkDir 'rawchunks'
$OutUtoc     = Join-Path $WorkDir 'out.utoc'

Write-Info "5/1 — Extraktion der vanilla-Assets im legacy-Format (BP)..."
$null = Invoke-Retoc @('to-legacy', $PaksDir, $LegacyDir, '--version', 'UE5_3', '--filter', 'BP')
Write-Info "5/2 — Extraktion der vanilla-Assets (Maps)..."
$null = Invoke-Retoc @('to-legacy', $PaksDir, $LegacyDir, '--version', 'UE5_3', '--filter', '.umap')

$bpCount = (Get-ChildItem $LegacyDir -Recurse -Filter '*.uasset').Count
$mapCount = (Get-ChildItem $LegacyDir -Recurse -Filter '*.umap').Count
if ($bpCount -lt 100 -or $mapCount -lt 5) {
    Fail "Die vanilla-Extraktion hat zu wenige Assets erzeugt (BP=$bpCount, Maps=$mapCount). Prüfe die Spielintegrität."
}
Write-OK "$bpCount BP + $mapCount Maps extrahiert."

Write-Info "5/3 — Anwendung der übersetzten Dateien..."
Copy-Item -Path (Join-Path $AssetsDir '*') -Destination $LegacyDir -Recurse -Force
Write-OK "Übersetzte Assets übergelegt."

Write-Info "5/4 — Rückkonvertierung in das Zen-Format..."
$null = Invoke-Retoc @('to-zen', $LegacyDir, $ZenUtoc, '--version', 'UE5_3')
if (-not (Test-Path $ZenUtoc)) { Fail "to-zen fehlgeschlagen." }
Write-OK "Zwischen-Container im Zen-Format erzeugt."

Write-Info "5/5 — Entpacken der vanilla-Chunks (längster Schritt, ca. 30 s)..."
$null = Invoke-Retoc @('unpack-raw', (Join-Path $PaksDir 'ABumpyRide-Windows.utoc'), $RawChunks)
if (-not (Test-Path (Join-Path $RawChunks 'manifest.json'))) { Fail "unpack-raw der vanilla-Datei fehlgeschlagen." }

Write-Info "5/6 — Entpacken der übersetzten Chunks..."
$null = Invoke-Retoc @('unpack-raw', $ZenUtoc, $ZenChunks)

Write-Info "5/7 — Überlagerung der übersetzten Chunks..."
$rawManifest = Get-Content (Join-Path $RawChunks 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$validChunkIds = @{}
foreach ($p in $rawManifest.chunk_paths.PSObject.Properties.Name) { $validChunkIds[$p] = $true }
$copied = 0
Get-ChildItem (Join-Path $ZenChunks 'chunks') | ForEach-Object {
    if ($validChunkIds.ContainsKey($_.Name)) {
        Copy-Item $_.FullName (Join-Path $RawChunks "chunks\$($_.Name)") -Force
        $copied++
    }
}
Write-OK "$copied übersetzte Chunks integriert."

Write-Info "5/8 — Repacking des finalen Containers..."
$null = Invoke-Retoc @('pack-raw', $RawChunks, $OutUtoc, '--container-header-version=NoExportInfo')
$OutUcas = [System.IO.Path]::ChangeExtension($OutUtoc, '.ucas')
if (-not (Test-Path $OutUtoc) -or -not (Test-Path $OutUcas)) { Fail "pack-raw fehlgeschlagen." }
$ucasMb = [math]::Round((Get-Item $OutUcas).Length / 1MB)
Write-OK "Übersetzter Container erstellt ($ucasMb MB)."

# ----------------------------------------------------------------------------
# Schritt 6 — Installation
# ----------------------------------------------------------------------------
Write-Step "Installation in den Spielordner"
Copy-Item $OutUtoc $utocLive -Force
Copy-Item $OutUcas $ucasLive -Force
Write-OK "Installiert: $utocLive"
Write-OK "Installiert: $ucasLive"

# ----------------------------------------------------------------------------
# Schritt 7 — Aufräumen
# ----------------------------------------------------------------------------
Write-Step "Aufräumen der temporären Dateien"
Remove-Item $WorkDir -Recurse -Force -ErrorAction SilentlyContinue
Write-OK "Fertig."

# ----------------------------------------------------------------------------
# Abschluss
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Installation abgeschlossen!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Starte das Spiel wie gewohnt über Steam — die Texte sollten" -ForegroundColor White
Write-Host "jetzt auf Deutsch erscheinen." -ForegroundColor White
Write-Host ""
Write-Host "Zum Deinstallieren des Mods: führe uninstall.ps1 aus" -ForegroundColor Gray
Write-Host "(stellt die vanilla-Dateien aus $BackupDir wieder her)" -ForegroundColor Gray
Write-Host ""
Write-Host "Drücke die Eingabetaste, um zu beenden..." -ForegroundColor Gray
[void](Read-Host)
