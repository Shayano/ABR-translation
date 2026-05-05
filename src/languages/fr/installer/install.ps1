#Requires -Version 5.1
<#
    A Bumpy Ride — Traduction française
    Installeur PowerShell

    Usage :
      1) Mode automatique : double-cliquez install.ps1 (clic droit > "Exécuter avec PowerShell")
         OU dans une console PowerShell : .\install.ps1
         L'installeur détecte tout seul votre installation Steam.

      2) Mode manuel : déposez le dossier patch-fr dans le dossier du jeu
         (par exemple F:\Steam\steamapps\common\A Bumpy Ride\patch-fr),
         puis lancez install.ps1 de la même manière. L'installeur détecte
         qu'il est dans le dossier du jeu en remontant l'arborescence.

      3) Mode prompt : si la détection automatique échoue, l'installeur vous
         demandera de coller le chemin d'installation du jeu.
#>

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ManifestPath       = Join-Path $ScriptDir 'manifest.json'
$RetocExe           = Join-Path $ScriptDir 'retoc.exe'
$AssetsDir          = Join-Path $ScriptDir 'patched_assets'
$MainMapPatcherExe  = Join-Path $ScriptDir 'MainMapPatcher.exe'
$UsmapPath          = Join-Path $ScriptDir 'ABumpyRide.usmap'

# ----------------------------------------------------------------------------
# Helpers
# ----------------------------------------------------------------------------
function Write-Step([string]$msg)  { Write-Host "`n[ÉTAPE] $msg" -ForegroundColor Cyan }
function Write-Info([string]$msg)  { Write-Host "  $msg" -ForegroundColor Gray }
function Write-OK([string]$msg)    { Write-Host "  OK : $msg" -ForegroundColor Green }
function Write-Warn([string]$msg)  { Write-Host "  AVERTISSEMENT : $msg" -ForegroundColor Yellow }
function Fail([string]$msg) {
    Write-Host "`nERREUR : $msg" -ForegroundColor Red
    Write-Host "`nAppuyez sur Entrée pour fermer..." -ForegroundColor Gray
    [void](Read-Host)
    exit 1
}

function Get-Sha256($path) {
    (Get-FileHash -Algorithm SHA256 -Path $path).Hash.ToLower()
}

# Wrapper qui invoque retoc en ignorant les écritures stderr (retoc dump sa config sur stderr,
# ce qui sous PowerShell 5.1 + ErrorActionPreference Stop est traité comme une erreur fatale).
function Invoke-Retoc {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $oldEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $RetocExe @Arguments 2>&1 | Out-String
    } finally {
        $ErrorActionPreference = $oldEAP
    }
    # retoc renvoie souvent exit 1 même en cas de succès — on se fie à l'output réel
    # (ligne "action_X done" + comptage des fichiers produits) plutôt qu'à $LASTEXITCODE.
    return $output
}

# ----------------------------------------------------------------------------
# Préambule
# ----------------------------------------------------------------------------
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  A Bumpy Ride — Traduction française (mod ABR-fr)" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host ""

if (-not (Test-Path $ManifestPath))      { Fail "manifest.json introuvable à côté de install.ps1." }
if (-not (Test-Path $RetocExe))          { Fail "retoc.exe introuvable à côté de install.ps1." }
if (-not (Test-Path $AssetsDir))         { Fail "patched_assets/ introuvable à côté de install.ps1." }
if (-not (Test-Path $MainMapPatcherExe)) { Fail "MainMapPatcher.exe introuvable à côté de install.ps1." }
if (-not (Test-Path $UsmapPath))         { Fail "ABumpyRide.usmap introuvable à côté de install.ps1." }

$Manifest = Get-Content $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
Write-Info "Mod : $($Manifest.mod_name)"
Write-Info "Version : $($Manifest.mod_version) (publié $($Manifest.mod_date))"
Write-Info "Auteur : $($Manifest.mod_author)"

# ----------------------------------------------------------------------------
# Étape 1 — Détection du dossier du jeu
# ----------------------------------------------------------------------------
Write-Step "Détection du dossier d'installation du jeu"

function Test-GameRoot([string]$candidate) {
    if (-not $candidate -or -not (Test-Path $candidate)) { return $false }
    return (Test-Path (Join-Path $candidate 'ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc'))
}

$GameRoot = $null

# 1a — Mode drop-in : remonter depuis ScriptDir tant que ABumpyRide/Content/Paks existe à proximité
$probe = $ScriptDir
for ($i = 0; $i -lt 6; $i++) {
    if (Test-GameRoot $probe) { $GameRoot = $probe; break }
    if ([string]::IsNullOrEmpty($probe)) { break }
    $parent = Split-Path -Parent $probe
    if ([string]::IsNullOrEmpty($parent) -or $parent -eq $probe) { break }
    $probe = $parent
}
if ($GameRoot) {
    Write-OK "Mode dépôt-manuel détecté : $GameRoot"
}

# 1b — Mode auto : registry Steam + libraryfolders.vdf
if (-not $GameRoot) {
    $steamPath = $null
    try {
        $steamPath = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath
    } catch {
        try { $steamPath = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam' -Name InstallPath -ErrorAction Stop).InstallPath } catch {}
    }
    if ($steamPath) {
        Write-Info "Steam détecté : $steamPath"
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
        Write-OK "Mode automatique détecté : $GameRoot"
    } else {
        Write-Warn "Détection automatique échouée."
    }
}

# 1c — Fallback : prompt manuel
if (-not $GameRoot) {
    Write-Host ""
    Write-Host "Veuillez coller le chemin complet du dossier du jeu" -ForegroundColor Yellow
    Write-Host "(le dossier qui contient ABumpyRide\Content\Paks\)" -ForegroundColor Yellow
    Write-Host "Exemple : F:\Steam\steamapps\common\A Bumpy Ride" -ForegroundColor Gray
    $manual = Read-Host 'Chemin'
    $manual = $manual.Trim('" ')
    if (Test-GameRoot $manual) {
        $GameRoot = $manual
        Write-OK "Chemin manuel validé : $GameRoot"
    } else {
        Fail "Le chemin fourni ne contient pas ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc."
    }
}

$PaksDir = Join-Path $GameRoot 'ABumpyRide\Content\Paks'

# ----------------------------------------------------------------------------
# Étape 2 — Vérification de l'état du jeu
# ----------------------------------------------------------------------------
Write-Step "Vérification de l'intégrité du jeu"

$utocLive = Join-Path $PaksDir 'ABumpyRide-Windows.utoc'
$ucasLive = Join-Path $PaksDir 'ABumpyRide-Windows.ucas'
$pakLive  = Join-Path $PaksDir 'ABumpyRide-Windows.pak'

$utocSize = (Get-Item $utocLive).Length
$ucasSize = (Get-Item $ucasLive).Length
$expectedUtocSize = $Manifest.target_game.vanilla_files.'ABumpyRide-Windows.utoc'.size
$expectedUcasSize = $Manifest.target_game.vanilla_files.'ABumpyRide-Windows.ucas'.size

Write-Info "ABumpyRide-Windows.utoc : $('{0:N0}' -f $utocSize) octets (vanilla attendu : $('{0:N0}' -f $expectedUtocSize))"
Write-Info "ABumpyRide-Windows.ucas : $('{0:N0}' -f $ucasSize) octets (vanilla attendu : $('{0:N0}' -f $expectedUcasSize))"

if ($utocSize -ne $expectedUtocSize -or $ucasSize -ne $expectedUcasSize) {
    Write-Warn "Le jeu installé ne correspond pas exactement aux fichiers vanilla attendus."
    Write-Warn "Cela peut signifier :"
    Write-Warn "  (a) le mod ABR-fr est déjà installé (relancez désinstall.ps1 d'abord)"
    Write-Warn "  (b) le jeu a été mis à jour par Steam (ce mod cible la version d'origine)"
    Write-Warn "  (c) un autre mod est installé"
    Write-Host ""
    $resp = Read-Host "Voulez-vous quand même tenter l'installation ? [o/N]"
    if ($resp -notmatch '^[oOyY]') { Fail "Installation annulée par l'utilisateur." }
    Write-Warn "Poursuite forcée — risque d'échec ou de comportement inattendu."
} else {
    Write-OK "Le jeu correspond à la version vanilla attendue."
}

# ----------------------------------------------------------------------------
# Étape 3 — Vérif espace disque libre
# ----------------------------------------------------------------------------
Write-Step "Vérification de l'espace disque temporaire"

$tempRoot = $env:TEMP
$drive = (Get-Item $tempRoot).PSDrive
$freeGB = [math]::Round($drive.Free / 1GB, 1)
$requiredGB = [int]$Manifest.install_requirements.free_disk_space_gb
$driveLetter = $drive.Name + ':'
Write-Info "Lecteur temporaire ($driveLetter) : $freeGB Go libres (requis : $requiredGB Go)"
if ($freeGB -lt $requiredGB) {
    Fail "Espace disque insuffisant sur le lecteur $driveLetter pour le pipeline d'installation."
}

# ----------------------------------------------------------------------------
# Étape 4 — Backup
# ----------------------------------------------------------------------------
Write-Step "Sauvegarde des fichiers vanilla"
$BackupDir = Join-Path $PaksDir '_ABRfr_backup'
if (Test-Path $BackupDir) {
    Write-Info "Sauvegarde existante détectée — non-écrasée (la sauvegarde initiale est la plus fiable)."
} else {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
    Copy-Item $utocLive (Join-Path $BackupDir 'ABumpyRide-Windows.utoc') -Force
    Copy-Item $ucasLive (Join-Path $BackupDir 'ABumpyRide-Windows.ucas') -Force
    Copy-Item $pakLive  (Join-Path $BackupDir 'ABumpyRide-Windows.pak')  -Force
    Write-OK "Sauvegarde créée dans $BackupDir"
}

# ----------------------------------------------------------------------------
# Étape 5 — Pipeline retoc
# ----------------------------------------------------------------------------
Write-Step "Construction du container traduit (peut prendre 3 à 5 minutes)"

$WorkDir = Join-Path $env:TEMP 'ABRfr_install'
if (Test-Path $WorkDir) { Remove-Item $WorkDir -Recurse -Force }
New-Item -ItemType Directory -Path $WorkDir | Out-Null

$LegacyDir   = Join-Path $WorkDir 'legacy'
$ZenUtoc     = Join-Path $WorkDir 'fr.utoc'
$ZenChunks   = Join-Path $WorkDir 'zen_chunks'
$RawChunks   = Join-Path $WorkDir 'rawchunks'
$OutUtoc     = Join-Path $WorkDir 'out.utoc'

Write-Info "5/1 — Extraction des assets vanilla en format legacy (BP)..."
$null = Invoke-Retoc @('to-legacy', $PaksDir, $LegacyDir, '--version', 'UE5_3', '--filter', 'BP')
Write-Info "5/2 — Extraction des assets vanilla (maps)..."
$null = Invoke-Retoc @('to-legacy', $PaksDir, $LegacyDir, '--version', 'UE5_3', '--filter', '.umap')

$bpCount = (Get-ChildItem $LegacyDir -Recurse -Filter '*.uasset').Count
$mapCount = (Get-ChildItem $LegacyDir -Recurse -Filter '*.umap').Count
if ($bpCount -lt 100 -or $mapCount -lt 5) {
    Fail "L'extraction du vanilla a produit trop peu d'assets (BP=$bpCount, maps=$mapCount). Vérifiez l'intégrité du jeu."
}
Write-OK "$bpCount BP + $mapCount maps extraits."

Write-Info "5/3 — Application des fichiers traduits par-dessus..."
Copy-Item -Path (Join-Path $AssetsDir '*') -Destination $LegacyDir -Recurse -Force
Write-OK "Assets traduits superposés."

Write-Info "5/3b — Traduction des chaînes hardcodées de la carte principale (patch bytecode du Level Blueprint)..."
$mainmapInput      = Join-Path $LegacyDir 'ABumpyRide\Content\MainMap.umap'
$mainmapIntroOut   = Join-Path $WorkDir 'mainmap_intro'
$mainmapStaffOut   = Join-Path $WorkDir 'mainmap_staff'
if (-not (Test-Path $mainmapInput)) {
    Fail "MainMap.umap introuvable dans l'extraction legacy ($mainmapInput)."
}

function Invoke-MainMapPatcher {
    param([string]$InUmap, [string]$OutDir, [string]$Target)
    $result = $null
    $exit = 0
    $oldEAP = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $result = & $MainMapPatcherExe $InUmap $OutDir $UsmapPath "--target=$Target" 2>&1
        $exit = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $oldEAP
    }
    if ($exit -ne 0) {
        Write-Host ($result | Out-String) -ForegroundColor Yellow
        Fail "MainMapPatcher --target=$Target a échoué (code $exit)."
    }
    foreach ($ext in 'umap','uexp','ubulk') {
        if (-not (Test-Path (Join-Path $OutDir "MainMap.$ext"))) {
            Fail "MainMapPatcher --target=$Target n'a pas produit MainMap.$ext."
        }
    }
}

# Pass 1: intro string
Invoke-MainMapPatcher -InUmap $mainmapInput -OutDir $mainmapIntroOut -Target 'intro'
# Pass 2: "New Staff Member Unlocked!" — chaîne the intro pass output as input
$introResult = Join-Path $mainmapIntroOut 'MainMap.umap'
Invoke-MainMapPatcher -InUmap $introResult -OutDir $mainmapStaffOut -Target 'staff'

# Final pass output → overlay sur le legacy dir
foreach ($ext in 'umap','uexp','ubulk') {
    $patched = Join-Path $mainmapStaffOut "MainMap.$ext"
    $target  = Join-Path $LegacyDir "ABumpyRide\Content\MainMap.$ext"
    Copy-Item $patched $target -Force
}
Write-OK "Intro et notification staff de MainMap traduites et superposées."

Write-Info "5/4 — Reconversion en format Zen..."
$null = Invoke-Retoc @('to-zen', $LegacyDir, $ZenUtoc, '--version', 'UE5_3')
if (-not (Test-Path $ZenUtoc)) { Fail "to-zen a échoué." }
Write-OK "Container Zen intermédiaire généré."

Write-Info "5/5 — Décompression des chunks vanilla (étape la plus longue, ~30s)..."
$null = Invoke-Retoc @('unpack-raw', (Join-Path $PaksDir 'ABumpyRide-Windows.utoc'), $RawChunks)
if (-not (Test-Path (Join-Path $RawChunks 'manifest.json'))) { Fail "unpack-raw du vanilla a échoué." }

Write-Info "5/6 — Décompression des chunks traduits..."
$null = Invoke-Retoc @('unpack-raw', $ZenUtoc, $ZenChunks)

Write-Info "5/7 — Superposition des chunks traduits..."
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
Write-OK "$copied chunks traduits intégrés."

Write-Info "5/8 — Repaquetage du container final..."
$null = Invoke-Retoc @('pack-raw', $RawChunks, $OutUtoc, '--container-header-version=NoExportInfo')
$OutUcas = [System.IO.Path]::ChangeExtension($OutUtoc, '.ucas')
if (-not (Test-Path $OutUtoc) -or -not (Test-Path $OutUcas)) { Fail "pack-raw a échoué." }
$ucasMb = [math]::Round((Get-Item $OutUcas).Length / 1MB)
Write-OK "Container traduit construit ($ucasMb Mo)."

# ----------------------------------------------------------------------------
# Étape 6 — Installation
# ----------------------------------------------------------------------------
Write-Step "Installation dans le dossier du jeu"
Copy-Item $OutUtoc $utocLive -Force
Copy-Item $OutUcas $ucasLive -Force
Write-OK "Installé : $utocLive"
Write-OK "Installé : $ucasLive"

# ----------------------------------------------------------------------------
# Étape 7 — Cleanup
# ----------------------------------------------------------------------------
Write-Step "Nettoyage des fichiers temporaires"
Remove-Item $WorkDir -Recurse -Force -ErrorAction SilentlyContinue
Write-OK "Terminé."

# ----------------------------------------------------------------------------
# Conclusion
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Installation terminée !" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Lancez le jeu via Steam comme d'habitude — le texte devrait" -ForegroundColor White
Write-Host "maintenant s'afficher en français." -ForegroundColor White
Write-Host ""
Write-Host "Pour désinstaller le mod : exécutez uninstall.ps1" -ForegroundColor Gray
Write-Host "(restaure les fichiers vanilla depuis $BackupDir)" -ForegroundColor Gray
Write-Host ""
Write-Host "Appuyez sur Entrée pour fermer..." -ForegroundColor Gray
[void](Read-Host)
