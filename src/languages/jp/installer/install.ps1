#Requires -Version 5.1
<#
    A Bumpy Ride - 日本語翻訳
    PowerShell インストーラー

    使い方:
      1) 自動モード: install.ps1 を右クリック > 「PowerShellで実行」
         または PowerShell コンソールで: .\install.ps1
         インストーラーは Steam のインストール先を自動的に検出します。

      2) 手動モード: patch-jp フォルダをゲームのフォルダにコピー
         (例: F:\Steam\steamapps\common\A Bumpy Ride\patch-jp)、
         その後 install.ps1 を上記のように実行。インストーラーは
         自分がゲームのフォルダ内にいることを自動的に認識します。

      3) プロンプト付き手動モード: 自動検出に失敗した場合、
         インストーラーはゲームのパスを尋ねます。
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
function Write-Step([string]$msg)  { Write-Host "`n[ステップ] $msg" -ForegroundColor Cyan }
function Write-Info([string]$msg)  { Write-Host "  $msg" -ForegroundColor Gray }
function Write-OK([string]$msg)    { Write-Host "  OK: $msg" -ForegroundColor Green }
function Write-Warn([string]$msg)  { Write-Host "  注意: $msg" -ForegroundColor Yellow }
function Fail([string]$msg) {
    Write-Host "`nエラー: $msg" -ForegroundColor Red
    Write-Host "`n終了するには Enter キーを押してください..." -ForegroundColor Gray
    [void](Read-Host)
    exit 1
}

function Get-Sha256($path) {
    (Get-FileHash -Algorithm SHA256 -Path $path).Hash.ToLower()
}

# Wrapper pour retoc - retoc emet sa configuration sur stderr,
# qui sous PowerShell 5.1 + ErrorActionPreference Stop est traite comme erreur fatale.
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
# Introduction
# ----------------------------------------------------------------------------
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  A Bumpy Ride - 日本語翻訳 (Mod ABR-jp)" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host ""

if (-not (Test-Path $ManifestPath)) { Fail "manifest.json が install.ps1 の隣に見つかりません。" }
if (-not (Test-Path $RetocExe))     { Fail "retoc.exe が install.ps1 の隣に見つかりません。" }
if (-not (Test-Path $AssetsDir))    { Fail "patched_assets/ が install.ps1 の隣に見つかりません。" }

$Manifest = Get-Content $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
Write-Info "Mod: $($Manifest.mod_name)"
Write-Info "バージョン: $($Manifest.mod_version) (公開日 $($Manifest.mod_date))"
Write-Info "作者: $($Manifest.mod_author)"

# ----------------------------------------------------------------------------
# Step 1 - ゲームフォルダ検出
# ----------------------------------------------------------------------------
Write-Step "ゲームフォルダの検出"

function Test-GameRoot([string]$candidate) {
    if (-not $candidate -or -not (Test-Path $candidate)) { return $false }
    return (Test-Path (Join-Path $candidate 'ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc'))
}

$GameRoot = $null

# 1a - 手動モード: ScriptDir から親へ遡る
$probe = $ScriptDir
for ($i = 0; $i -lt 6; $i++) {
    if (Test-GameRoot $probe) { $GameRoot = $probe; break }
    $parent = Split-Path -Parent $probe
    if (-not $parent -or $parent -eq $probe) { break }
    $probe = $parent
}
if ($GameRoot) {
    Write-OK "手動モードを検出: $GameRoot"
}

# 1b - 自動モード: Steam レジストリ + libraryfolders.vdf
if (-not $GameRoot) {
    $steamPath = $null
    try {
        $steamPath = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath
    } catch {
        try { $steamPath = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam' -Name InstallPath -ErrorAction Stop).InstallPath } catch {}
    }
    if ($steamPath) {
        Write-Info "Steam を検出: $steamPath"
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
        Write-OK "自動モードで検出: $GameRoot"
    } else {
        Write-Warn "自動検出に失敗しました。"
    }
}

# 1c - フォールバック: 手動入力
if (-not $GameRoot) {
    Write-Host ""
    Write-Host "ゲームフォルダの完全なパスを貼り付けてください" -ForegroundColor Yellow
    Write-Host "(ABumpyRide\Content\Paks\ を含むフォルダ)" -ForegroundColor Yellow
    Write-Host "例: F:\Steam\steamapps\common\A Bumpy Ride" -ForegroundColor Gray
    $manual = Read-Host 'パス'
    $manual = $manual.Trim('" ')
    if (Test-GameRoot $manual) {
        $GameRoot = $manual
        Write-OK "手動パスを確認: $GameRoot"
    } else {
        Fail "指定されたパスに ABumpyRide\Content\Paks\ABumpyRide-Windows.utoc が含まれていません。"
    }
}

$PaksDir = Join-Path $GameRoot 'ABumpyRide\Content\Paks'

# ----------------------------------------------------------------------------
# Step 2 - ゲームの整合性チェック
# ----------------------------------------------------------------------------
Write-Step "ゲームの整合性チェック"

$utocLive = Join-Path $PaksDir 'ABumpyRide-Windows.utoc'
$ucasLive = Join-Path $PaksDir 'ABumpyRide-Windows.ucas'
$pakLive  = Join-Path $PaksDir 'ABumpyRide-Windows.pak'

$utocSize = (Get-Item $utocLive).Length
$ucasSize = (Get-Item $ucasLive).Length
$expectedUtocSize = $Manifest.target_game.vanilla_files.'ABumpyRide-Windows.utoc'.size
$expectedUcasSize = $Manifest.target_game.vanilla_files.'ABumpyRide-Windows.ucas'.size

Write-Info "ABumpyRide-Windows.utoc: $('{0:N0}' -f $utocSize) バイト (vanilla 期待値: $('{0:N0}' -f $expectedUtocSize))"
Write-Info "ABumpyRide-Windows.ucas: $('{0:N0}' -f $ucasSize) バイト (vanilla 期待値: $('{0:N0}' -f $expectedUcasSize))"

if ($utocSize -ne $expectedUtocSize -or $ucasSize -ne $expectedUcasSize) {
    Write-Warn "インストールされているゲームファイルが vanilla と完全に一致しません。"
    Write-Warn "考えられる原因:"
    Write-Warn "  (a) ABR-jp Mod が既にインストールされている (先に uninstall.ps1 を実行)"
    Write-Warn "  (b) Steam がゲームを更新した (この Mod は元のバージョンを対象)"
    Write-Warn "  (c) 別の Mod がインストールされている"
    Write-Host ""
    $resp = Read-Host "それでもインストールを試みますか? [y/N]"
    if ($resp -notmatch '^[yYjJ]') { Fail "ユーザーによりインストールが中止されました。" }
    Write-Warn "強制続行 - エラーや予期しない動作のリスクあり。"
} else {
    Write-OK "ゲームは期待される vanilla バージョンと一致します。"
}

# ----------------------------------------------------------------------------
# Step 3 - 空き容量チェック
# ----------------------------------------------------------------------------
Write-Step "空き容量チェック"

$tempRoot = $env:TEMP
$drive = (Get-Item $tempRoot).PSDrive
$freeGB = [math]::Round($drive.Free / 1GB, 1)
$requiredGB = [int]$Manifest.install_requirements.free_disk_space_gb
$driveLetter = $drive.Name + ':'
Write-Info "一時ドライブ ($driveLetter): $freeGB GB 空き (必要: $requiredGB GB)"
if ($freeGB -lt $requiredGB) {
    Fail "ドライブ $driveLetter にインストールパイプライン用の空き容量が不足しています。"
}

# ----------------------------------------------------------------------------
# Step 4 - バックアップ
# ----------------------------------------------------------------------------
Write-Step "vanilla ファイルのバックアップ"
$BackupDir = Join-Path $PaksDir '_ABRjp_backup'
if (Test-Path $BackupDir) {
    Write-Info "バックアップが既に存在します - 上書きしません (元のバックアップが最も信頼できます)。"
} else {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
    Copy-Item $utocLive (Join-Path $BackupDir 'ABumpyRide-Windows.utoc') -Force
    Copy-Item $ucasLive (Join-Path $BackupDir 'ABumpyRide-Windows.ucas') -Force
    Copy-Item $pakLive  (Join-Path $BackupDir 'ABumpyRide-Windows.pak')  -Force
    Write-OK "バックアップを $BackupDir に作成しました。"
}

# ----------------------------------------------------------------------------
# Step 5 - retoc パイプライン
# ----------------------------------------------------------------------------
Write-Step "翻訳済みコンテナの作成 (3~5 分かかる場合があります)"

$WorkDir = Join-Path $env:TEMP 'ABRjp_install'
if (Test-Path $WorkDir) { Remove-Item $WorkDir -Recurse -Force }
New-Item -ItemType Directory -Path $WorkDir | Out-Null

$LegacyDir   = Join-Path $WorkDir 'legacy'
$ZenUtoc     = Join-Path $WorkDir 'jp.utoc'
$ZenChunks   = Join-Path $WorkDir 'zen_chunks'
$RawChunks   = Join-Path $WorkDir 'rawchunks'
$OutUtoc     = Join-Path $WorkDir 'out.utoc'

Write-Info "5/1 - vanilla アセットの legacy 形式抽出 (BP)..."
$null = Invoke-Retoc @('to-legacy', $PaksDir, $LegacyDir, '--version', 'UE5_3', '--filter', 'BP')
Write-Info "5/2 - vanilla アセットの抽出 (Maps)..."
$null = Invoke-Retoc @('to-legacy', $PaksDir, $LegacyDir, '--version', 'UE5_3', '--filter', '.umap')

$bpCount = (Get-ChildItem $LegacyDir -Recurse -Filter '*.uasset').Count
$mapCount = (Get-ChildItem $LegacyDir -Recurse -Filter '*.umap').Count
if ($bpCount -lt 100 -or $mapCount -lt 5) {
    Fail "vanilla 抽出のアセットが少なすぎます (BP=$bpCount, Maps=$mapCount)。ゲームの整合性を確認してください。"
}
Write-OK "$bpCount BP + $mapCount Maps 抽出完了。"

Write-Info "5/3 - 翻訳ファイルの適用..."
Copy-Item -Path (Join-Path $AssetsDir '*') -Destination $LegacyDir -Recurse -Force
Write-OK "翻訳済みアセットを重ね合わせ完了。"

Write-Info "5/4 - Zen 形式への再変換..."
$null = Invoke-Retoc @('to-zen', $LegacyDir, $ZenUtoc, '--version', 'UE5_3')
if (-not (Test-Path $ZenUtoc)) { Fail "to-zen が失敗しました。" }
Write-OK "中間 Zen コンテナを作成しました。"

Write-Info "5/5 - vanilla チャンクの展開 (最長ステップ、約 30 秒)..."
$null = Invoke-Retoc @('unpack-raw', (Join-Path $PaksDir 'ABumpyRide-Windows.utoc'), $RawChunks)
if (-not (Test-Path (Join-Path $RawChunks 'manifest.json'))) { Fail "vanilla の unpack-raw が失敗しました。" }

Write-Info "5/6 - 翻訳済みチャンクの展開..."
$null = Invoke-Retoc @('unpack-raw', $ZenUtoc, $ZenChunks)

Write-Info "5/7 - 翻訳済みチャンクの重ね合わせ..."
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
Write-OK "$copied 個の翻訳チャンクを統合しました。"

Write-Info "5/8 - 最終コンテナの再パック..."
$null = Invoke-Retoc @('pack-raw', $RawChunks, $OutUtoc, '--container-header-version=NoExportInfo')
$OutUcas = [System.IO.Path]::ChangeExtension($OutUtoc, '.ucas')
if (-not (Test-Path $OutUtoc) -or -not (Test-Path $OutUcas)) { Fail "pack-raw が失敗しました。" }
$ucasMb = [math]::Round((Get-Item $OutUcas).Length / 1MB)
Write-OK "翻訳済みコンテナを作成しました ($ucasMb MB)。"

# ----------------------------------------------------------------------------
# Step 6 - インストール
# ----------------------------------------------------------------------------
Write-Step "ゲームフォルダへのインストール"
Copy-Item $OutUtoc $utocLive -Force
Copy-Item $OutUcas $ucasLive -Force
Write-OK "インストール: $utocLive"
Write-OK "インストール: $ucasLive"

# ----------------------------------------------------------------------------
# Step 7 - クリーンアップ
# ----------------------------------------------------------------------------
Write-Step "一時ファイルのクリーンアップ"
Remove-Item $WorkDir -Recurse -Force -ErrorAction SilentlyContinue
Write-OK "完了。"

# ----------------------------------------------------------------------------
# 終了
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  インストール完了!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "通常通り Steam からゲームを起動してください。" -ForegroundColor White
Write-Host "テキストが日本語で表示されます。" -ForegroundColor White
Write-Host ""
Write-Host "Mod をアンインストールするには: uninstall.ps1 を実行" -ForegroundColor Gray
Write-Host "($BackupDir から vanilla ファイルを復元)" -ForegroundColor Gray
Write-Host ""
Write-Host "終了するには Enter キーを押してください..." -ForegroundColor Gray
[void](Read-Host)
