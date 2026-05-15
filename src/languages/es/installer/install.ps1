#Requires -Version 5.1
<#
    A Bumpy Ride - Traducción al español
    Instalador PowerShell

    Uso:
      1) Modo automático: doble clic en install.ps1 (clic derecho > "Ejecutar con PowerShell")
         O en una consola PowerShell: .\install.ps1
         El instalador detecta automáticamente tu instalación de Steam.

      2) Modo manual: copia la carpeta patch-es dentro de la carpeta del juego
         (por ejemplo F:\Steam\steamapps\common\A Bumpy Ride\patch-es),
         luego ejecuta install.ps1 de la misma forma. El instalador detecta
         que está en la carpeta del juego al recorrer hacia arriba.

      3) Modo manual con prompt: si la detección automática falla, el instalador
         te pedirá pegar la ruta de instalación del juego.
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
function Write-Step([string]$msg)  { Write-Host "`n[PASO] $msg" -ForegroundColor Cyan }
function Write-Info([string]$msg)  { Write-Host "  $msg" -ForegroundColor Gray }
function Write-OK([string]$msg)    { Write-Host "  OK: $msg" -ForegroundColor Green }
function Write-Warn([string]$msg)  { Write-Host "  AVISO: $msg" -ForegroundColor Yellow }
function Fail([string]$msg) {
    Write-Host "`nERROR: $msg" -ForegroundColor Red
    Write-Host "`nPulsa Intro para cerrar..." -ForegroundColor Gray
    [void](Read-Host)
    exit 1
}

function Get-Sha256($path) {
    (Get-FileHash -Algorithm SHA256 -Path $path).Hash.ToLower()
}

# Wrapper que invoca retoc ignorando los mensajes en stderr (retoc imprime su config en stderr,
# lo que con PowerShell 5.1 + ErrorActionPreference Stop se trata como error fatal).
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
# Preámbulo
# ----------------------------------------------------------------------------
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  A Bumpy Ride - Traducción al español (mod ABR-es)" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host ""

if (-not (Test-Path $ManifestPath)) { Fail "manifest.json no encontrado junto a install.ps1." }
if (-not (Test-Path $RetocExe))     { Fail "retoc.exe no encontrado junto a install.ps1." }
if (-not (Test-Path $AssetsDir))    { Fail "patched_assets/ no encontrado junto a install.ps1." }

$Manifest = Get-Content $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
Write-Info "Mod: $($Manifest.mod_name)"
Write-Info "Versión: $($Manifest.mod_version) (publicado el $($Manifest.mod_date))"
Write-Info "Autor: $($Manifest.mod_author)"

# ----------------------------------------------------------------------------
# Paso 1 - Detección de la carpeta del juego
# ----------------------------------------------------------------------------
Write-Step "Detección de la carpeta de instalación del juego"

function Test-GameRoot([string]$candidate) {
    if (-not $candidate -or -not (Test-Path $candidate)) { return $false }
    return (Test-Path (Join-Path $candidate 'ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc'))
}

$GameRoot = $null

# 1a - Modo manual: subir desde ScriptDir mientras ABumpyRide/Content/Paks exista cerca
$probe = $ScriptDir
for ($i = 0; $i -lt 6; $i++) {
    if (Test-GameRoot $probe) { $GameRoot = $probe; break }
    $parent = Split-Path -Parent $probe
    if (-not $parent -or $parent -eq $probe) { break }
    $probe = $parent
}
if ($GameRoot) {
    Write-OK "Modo manual detectado: $GameRoot"
}

# 1b - Modo auto: registro Steam + libraryfolders.vdf
if (-not $GameRoot) {
    $steamPath = $null
    try {
        $steamPath = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath
    } catch {
        try { $steamPath = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam' -Name InstallPath -ErrorAction Stop).InstallPath } catch {}
    }
    if ($steamPath) {
        Write-Info "Steam detectado: $steamPath"
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
        Write-OK "Modo automático detectado: $GameRoot"
    } else {
        Write-Warn "Detección automática fallida."
    }
}

# 1c - Fallback: prompt manual
if (-not $GameRoot) {
    Write-Host ""
    Write-Host "Pega la ruta completa de la carpeta del juego" -ForegroundColor Yellow
    Write-Host "(la carpeta que contiene ABumpyRide\Content\Paks\)" -ForegroundColor Yellow
    Write-Host "Ejemplo: F:\Steam\steamapps\common\A Bumpy Ride" -ForegroundColor Gray
    $manual = Read-Host 'Ruta'
    $manual = $manual.Trim('" ')
    if (Test-GameRoot $manual) {
        $GameRoot = $manual
        Write-OK "Ruta manual validada: $GameRoot"
    } else {
        Fail "La ruta indicada no contiene ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc."
    }
}

$PaksDir = Join-Path $GameRoot 'ABumpyRide\Content\Paks'

# ----------------------------------------------------------------------------
# Paso 2 - Verificación del estado del juego
# ----------------------------------------------------------------------------
Write-Step "Verificación de la integridad del juego"

$utocLive = Join-Path $PaksDir 'ABumpyRide-Windows.utoc'
$ucasLive = Join-Path $PaksDir 'ABumpyRide-Windows.ucas'
$pakLive  = Join-Path $PaksDir 'ABumpyRide-Windows.pak'

$utocSize = (Get-Item $utocLive).Length
$ucasSize = (Get-Item $ucasLive).Length
$expectedUtocSize = $Manifest.target_game.vanilla_files.'ABumpyRide-Windows.utoc'.size
$expectedUcasSize = $Manifest.target_game.vanilla_files.'ABumpyRide-Windows.ucas'.size

Write-Info "ABumpyRide-Windows.utoc: $('{0:N0}' -f $utocSize) bytes (vanilla esperado: $('{0:N0}' -f $expectedUtocSize))"
Write-Info "ABumpyRide-Windows.ucas: $('{0:N0}' -f $ucasSize) bytes (vanilla esperado: $('{0:N0}' -f $expectedUcasSize))"

if ($utocSize -ne $expectedUtocSize -or $ucasSize -ne $expectedUcasSize) {
    Write-Warn "El juego instalado no coincide exactamente con los archivos vanilla esperados."
    Write-Warn "Esto puede significar:"
    Write-Warn "  (a) el mod ABR-es ya está instalado (ejecuta antes uninstall.ps1)"
    Write-Warn "  (b) Steam ha actualizado el juego (este mod apunta a la versión original)"
    Write-Warn "  (c) hay otro mod instalado"
    Write-Host ""
    $resp = Read-Host "¿Quieres intentar la instalación de todos modos? [s/N]"
    if ($resp -notmatch '^[sSyY]') { Fail "Instalación cancelada por el usuario." }
    Write-Warn "Continuación forzada - riesgo de fallo o comportamiento inesperado."
} else {
    Write-OK "El juego coincide con la versión vanilla esperada."
}

# ----------------------------------------------------------------------------
# Paso 3 - Verificación de espacio libre
# ----------------------------------------------------------------------------
Write-Step "Verificación del espacio libre temporal"

$tempRoot = $env:TEMP
$drive = (Get-Item $tempRoot).PSDrive
$freeGB = [math]::Round($drive.Free / 1GB, 1)
$requiredGB = [int]$Manifest.install_requirements.free_disk_space_gb
$driveLetter = $drive.Name + ':'
Write-Info "Unidad temporal ($driveLetter): $freeGB GB libres (requerido: $requiredGB GB)"
if ($freeGB -lt $requiredGB) {
    Fail "Espacio insuficiente en la unidad $driveLetter para el pipeline de instalación."
}

# ----------------------------------------------------------------------------
# Paso 4 - Copia de seguridad
# ----------------------------------------------------------------------------
Write-Step "Copia de seguridad de los archivos vanilla"
$BackupDir = Join-Path $PaksDir '_ABRes_backup'
if (Test-Path $BackupDir) {
    Write-Info "Copia de seguridad existente - no se sobrescribe (la copia inicial es la más fiable)."
} else {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
    Copy-Item $utocLive (Join-Path $BackupDir 'ABumpyRide-Windows.utoc') -Force
    Copy-Item $ucasLive (Join-Path $BackupDir 'ABumpyRide-Windows.ucas') -Force
    Copy-Item $pakLive  (Join-Path $BackupDir 'ABumpyRide-Windows.pak')  -Force
    Write-OK "Copia de seguridad creada en $BackupDir"
}

# ----------------------------------------------------------------------------
# Paso 5 - Pipeline retoc
# ----------------------------------------------------------------------------
Write-Step "Construcción del container traducido (puede tardar 3 a 5 minutos)"

$WorkDir = Join-Path $env:TEMP 'ABRes_install'
if (Test-Path $WorkDir) { Remove-Item $WorkDir -Recurse -Force }
New-Item -ItemType Directory -Path $WorkDir | Out-Null

$LegacyDir   = Join-Path $WorkDir 'legacy'
$ZenUtoc     = Join-Path $WorkDir 'es.utoc'
$ZenChunks   = Join-Path $WorkDir 'zen_chunks'
$RawChunks   = Join-Path $WorkDir 'rawchunks'
$OutUtoc     = Join-Path $WorkDir 'out.utoc'

Write-Info "5/1 - Extracción de los assets vanilla en formato legacy (BP)..."
$null = Invoke-Retoc @('to-legacy', $PaksDir, $LegacyDir, '--version', 'UE5_3', '--filter', 'BP')
Write-Info "5/2 - Extracción de los assets vanilla (maps)..."
$null = Invoke-Retoc @('to-legacy', $PaksDir, $LegacyDir, '--version', 'UE5_3', '--filter', '.umap')

$bpCount = (Get-ChildItem $LegacyDir -Recurse -Filter '*.uasset').Count
$mapCount = (Get-ChildItem $LegacyDir -Recurse -Filter '*.umap').Count
if ($bpCount -lt 100 -or $mapCount -lt 5) {
    Fail "La extracción del vanilla ha producido demasiado pocos assets (BP=$bpCount, maps=$mapCount). Verifica la integridad del juego."
}
Write-OK "$bpCount BP + $mapCount maps extraídos."

Write-Info "5/3 - Aplicación de los archivos traducidos por encima..."
Copy-Item -Path (Join-Path $AssetsDir '*') -Destination $LegacyDir -Recurse -Force
Write-OK "Assets traducidos superpuestos."

Write-Info "5/4 - Reconversión a formato Zen..."
$null = Invoke-Retoc @('to-zen', $LegacyDir, $ZenUtoc, '--version', 'UE5_3')
if (-not (Test-Path $ZenUtoc)) { Fail "to-zen ha fallado." }
Write-OK "Container Zen intermedio generado."

Write-Info "5/5 - Descompresión de los chunks vanilla (paso más largo, ~30s)..."
$null = Invoke-Retoc @('unpack-raw', (Join-Path $PaksDir 'ABumpyRide-Windows.utoc'), $RawChunks)
if (-not (Test-Path (Join-Path $RawChunks 'manifest.json'))) { Fail "unpack-raw del vanilla ha fallado." }

Write-Info "5/6 - Descompresión de los chunks traducidos..."
$null = Invoke-Retoc @('unpack-raw', $ZenUtoc, $ZenChunks)

Write-Info "5/7 - Superposición de los chunks traducidos..."
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
Write-OK "$copied chunks traducidos integrados."

Write-Info "5/8 - Reempaquetado del container final..."
$null = Invoke-Retoc @('pack-raw', $RawChunks, $OutUtoc, '--container-header-version=NoExportInfo')
$OutUcas = [System.IO.Path]::ChangeExtension($OutUtoc, '.ucas')
if (-not (Test-Path $OutUtoc) -or -not (Test-Path $OutUcas)) { Fail "pack-raw ha fallado." }
$ucasMb = [math]::Round((Get-Item $OutUcas).Length / 1MB)
Write-OK "Container traducido construido ($ucasMb MB)."

# ----------------------------------------------------------------------------
# Paso 6 - Instalación
# ----------------------------------------------------------------------------
Write-Step "Instalación en la carpeta del juego"
Copy-Item $OutUtoc $utocLive -Force
Copy-Item $OutUcas $ucasLive -Force
Write-OK "Instalado: $utocLive"
Write-OK "Instalado: $ucasLive"

# ----------------------------------------------------------------------------
# Paso 7 - Limpieza
# ----------------------------------------------------------------------------
Write-Step "Limpieza de los archivos temporales"
Remove-Item $WorkDir -Recurse -Force -ErrorAction SilentlyContinue
Write-OK "Listo."

# ----------------------------------------------------------------------------
# Conclusión
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  ¡Instalación completada!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Lanza el juego desde Steam como siempre - el texto debería" -ForegroundColor White
Write-Host "mostrarse ahora en español." -ForegroundColor White
Write-Host ""
Write-Host "Para desinstalar el mod: ejecuta uninstall.ps1" -ForegroundColor Gray
Write-Host "(restaura los archivos vanilla desde $BackupDir)" -ForegroundColor Gray
Write-Host ""
Write-Host "Pulsa Intro para cerrar..." -ForegroundColor Gray
[void](Read-Host)
