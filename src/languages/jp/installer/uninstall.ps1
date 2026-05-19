#Requires -Version 5.1
<#
    A Bumpy Ride - 日本語翻訳: アンインストール
    _ABRjp_backup/ から vanilla ファイルを復元します。
#>

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Step([string]$msg) { Write-Host "`n[ステップ] $msg" -ForegroundColor Cyan }
function Write-Info([string]$msg) { Write-Host "  $msg" -ForegroundColor Gray }
function Write-OK([string]$msg)   { Write-Host "  OK: $msg" -ForegroundColor Green }
function Fail([string]$msg) {
    Write-Host "`nエラー: $msg" -ForegroundColor Red
    Write-Host "`n終了するには Enter キーを押してください..." -ForegroundColor Gray
    [void](Read-Host); exit 1
}

Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  A Bumpy Ride - 日本語翻訳のアンインストール" -ForegroundColor Magenta
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
    Write-Host "ゲームのパスを貼り付けてください (ABumpyRide\Content\Paks を含むフォルダ)" -ForegroundColor Yellow
    $manual = (Read-Host 'パス').Trim('" ')
    if (Test-GameRoot $manual) { $GameRoot = $manual } else { Fail "無効なパスです。" }
}

$PaksDir   = Join-Path $GameRoot 'ABumpyRide\Content\Paks'
$BackupDir = Join-Path $PaksDir '_ABRjp_backup'

Write-Step "バックアップからの復元"
if (-not (Test-Path $BackupDir)) {
    Fail "$BackupDir にバックアップが見つかりません。削除した場合は Steam > プロパティ > ローカルファイル > 整合性を確認 を使用してください。"
}

foreach ($f in @('ABumpyRide-Windows.utoc', 'ABumpyRide-Windows.ucas', 'ABumpyRide-Windows.pak')) {
    $src = Join-Path $BackupDir $f
    $dst = Join-Path $PaksDir $f
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-OK "復元: $f"
    }
}

$resp = Read-Host "バックアップフォルダ ($BackupDir) も削除しますか? [y/N]"
if ($resp -match '^[yYjJ]') {
    Remove-Item $BackupDir -Recurse -Force
    Write-OK "バックアップを削除しました。"
} else {
    Write-Info "バックアップを $BackupDir に保持しました。"
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  アンインストール完了。" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "終了するには Enter キーを押してください..." -ForegroundColor Gray
[void](Read-Host)
