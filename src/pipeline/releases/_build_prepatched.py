"""
Build prepatched zips for FR/DE/ES from the vanilla backup.

Usage:
    python releases/_build_prepatched.py            # all 3 languages
    python releases/_build_prepatched.py fr de      # specific languages

Inputs:
    F:/Steam/.../_ABRfr_backup/ABumpyRide-Windows.{utoc,ucas,pak}    # vanilla
    F:/Tools/ABR-fr/patch-{lang}/patched_assets/                     # patched files
    F:/Tools/ABR-fr/patch-fr/MainMapPatcher.exe (FR only)
    F:/Tools/ABR-fr/patch-fr/ABumpyRide.usmap    (FR only)

Outputs:
    F:/Tools/ABR-fr/releases/github_repo/dist_v<VERSION>/ABR-<lang>_v<VERSION>_prepatched.zip
"""
import json
import os
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

ROOT = Path(__file__).resolve().parents[1]
# Steam install path: override with env var ABR_STEAM_PAKS for non-default installs.
# Default = typical Windows Steam location.
STEAM_PAKS = Path(os.environ.get(
    'ABR_STEAM_PAKS',
    r'C:\Program Files (x86)\Steam\steamapps\common\A Bumpy Ride\ABumpyRide\Content\Paks'
))
VANILLA_BACKUP = STEAM_PAKS / '_ABRfr_backup'  # vanilla ABumpyRide-Windows.{utoc,ucas,pak}

# Tools: pulled from patch-fr/ (FR ships all of them including MainMapPatcher)
RETOC = ROOT / 'patch-fr' / 'retoc.exe'
MAINMAP_PATCHER = ROOT / 'patch-fr' / 'MainMapPatcher.exe'
USMAP = ROOT / 'patch-fr' / 'ABumpyRide.usmap'


def get_version() -> str:
    """Pull version from patch-fr/manifest.json (single source of truth)."""
    m = json.loads((ROOT / 'patch-fr' / 'manifest.json').read_text(encoding='utf-8-sig'))
    return m['mod_version']


def run(cmd, label):
    print(f'  >>> {label}')
    # retoc prints config to stderr; capture and ignore non-zero stderr-only failures.
    result = subprocess.run([str(c) for c in cmd], capture_output=True, text=True,
                            encoding='utf-8', errors='replace')
    if result.returncode != 0:
        print(f'      ERROR exit {result.returncode}')
        for line in (result.stdout + '\n' + result.stderr).splitlines()[-15:]:
            print(f'      | {line}')
        raise SystemExit(1)


def validate_inputs():
    for f in ('ABumpyRide-Windows.utoc', 'ABumpyRide-Windows.ucas', 'ABumpyRide-Windows.pak'):
        if not (VANILLA_BACKUP / f).exists():
            raise SystemExit(f'Missing vanilla file: {VANILLA_BACKUP / f}')
    for f in ('global.utoc', 'global.ucas'):
        if not (STEAM_PAKS / f).exists():
            raise SystemExit(f'Missing global container: {STEAM_PAKS / f}')
    for t in (RETOC, MAINMAP_PATCHER, USMAP):
        if not t.exists():
            raise SystemExit(f'Missing tool: {t}')


def stage_vanilla_paks(stage_dir: Path):
    """Create a fresh Paks/ dir with vanilla ABumpyRide files + global container.

    retoc to-legacy needs global.utoc/ucas (ScriptObjects) alongside the main container.
    The Steam Paks dir mixes vanilla + currently-installed mod, so we build a clean
    vanilla Paks dir for retoc to operate on.
    """
    if stage_dir.exists():
        shutil.rmtree(stage_dir)
    stage_dir.mkdir(parents=True)
    for f in ('ABumpyRide-Windows.utoc', 'ABumpyRide-Windows.ucas', 'ABumpyRide-Windows.pak'):
        shutil.copy2(VANILLA_BACKUP / f, stage_dir / f)
    for f in ('global.utoc', 'global.ucas'):
        shutil.copy2(STEAM_PAKS / f, stage_dir / f)


def build_lang(lang: str, version: str, out_zip_path: Path):
    """Build prepatched paks for one language and zip them."""
    patch_dir = ROOT / f'patch-{lang}'
    assets_dir = patch_dir / 'patched_assets'
    if not assets_dir.exists():
        raise SystemExit(f'Missing patched_assets: {assets_dir}')

    workdir = ROOT / 'staging' / f'prepatched_workdir_{lang}'
    if workdir.exists():
        shutil.rmtree(workdir)
    workdir.mkdir(parents=True)

    vanilla_paks = workdir / 'vanilla_paks'
    stage_vanilla_paks(vanilla_paks)
    legacy_dir = workdir / 'legacy'
    zen_utoc = workdir / f'{lang}.utoc'
    zen_chunks = workdir / 'zen_chunks'
    raw_chunks = workdir / 'rawchunks'
    out_utoc = workdir / 'out.utoc'

    # 1+2: Extract vanilla to legacy
    print(f'[{lang}] Step 1/8 : extract vanilla BPs to legacy')
    run([RETOC, 'to-legacy', vanilla_paks, legacy_dir, '--version', 'UE5_3', '--filter', 'BP'],
        'retoc to-legacy --filter BP')
    print(f'[{lang}] Step 2/8 : extract vanilla maps to legacy')
    run([RETOC, 'to-legacy', vanilla_paks, legacy_dir, '--version', 'UE5_3', '--filter', '.umap'],
        'retoc to-legacy --filter .umap')

    # 3: Overlay patched assets
    print(f'[{lang}] Step 3/8 : overlay patched assets on legacy')
    n = 0
    for f in assets_dir.rglob('*'):
        if f.is_file():
            rel = f.relative_to(assets_dir)
            dst = legacy_dir / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(f, dst)
            n += 1
    print(f'  overlaid {n} files')

    # 4: FR-only MainMapPatcher (intro + staff)
    if lang == 'fr':
        print(f'[{lang}] Step 4/8 : MainMapPatcher (intro + staff)')
        mainmap_in = legacy_dir / 'ABumpyRide' / 'Content' / 'MainMap.umap'
        intro_out = workdir / 'mainmap_intro'
        staff_out = workdir / 'mainmap_staff'
        run([MAINMAP_PATCHER, mainmap_in, intro_out, USMAP, '--target=intro'],
            'MainMapPatcher --target=intro')
        run([MAINMAP_PATCHER, intro_out / 'MainMap.umap', staff_out, USMAP, '--target=staff'],
            'MainMapPatcher --target=staff')
        for ext in ('umap', 'uexp', 'ubulk'):
            src = staff_out / f'MainMap.{ext}'
            if src.exists():
                shutil.copy2(src, legacy_dir / 'ABumpyRide' / 'Content' / f'MainMap.{ext}')
    else:
        print(f'[{lang}] Step 4/8 : skipped (no MainMapPatcher for {lang})')

    # 5: to-zen
    print(f'[{lang}] Step 5/8 : retoc to-zen')
    run([RETOC, 'to-zen', legacy_dir, zen_utoc, '--version', 'UE5_3'],
        'retoc to-zen')

    # 6+7: unpack-raw vanilla + zen
    print(f'[{lang}] Step 6/8 : retoc unpack-raw (vanilla)')
    run([RETOC, 'unpack-raw', vanilla_paks / 'ABumpyRide-Windows.utoc', raw_chunks],
        'retoc unpack-raw (vanilla)')
    print(f'[{lang}] Step 7/8 : retoc unpack-raw (translated)')
    run([RETOC, 'unpack-raw', zen_utoc, zen_chunks],
        'retoc unpack-raw (translated)')

    # 7b: Overlay translated chunks over raw chunks (only valid IDs)
    raw_manifest = json.loads((raw_chunks / 'manifest.json').read_text(encoding='utf-8'))
    valid_ids = set(raw_manifest['chunk_paths'].keys())
    copied = 0
    for c in (zen_chunks / 'chunks').iterdir():
        if c.name in valid_ids:
            shutil.copy2(c, raw_chunks / 'chunks' / c.name)
            copied += 1
    print(f'  overlaid {copied} translated chunks')

    # 8: pack-raw
    print(f'[{lang}] Step 8/8 : retoc pack-raw')
    run([RETOC, 'pack-raw', raw_chunks, out_utoc, '--container-header-version=NoExportInfo'],
        'retoc pack-raw')
    out_ucas = out_utoc.with_suffix('.ucas')

    # Zip the final paks
    print(f'[{lang}] Zipping prepatched bundle...')
    out_zip_path.parent.mkdir(parents=True, exist_ok=True)
    if out_zip_path.exists():
        out_zip_path.unlink()
    with zipfile.ZipFile(out_zip_path, 'w', compression=zipfile.ZIP_DEFLATED,
                          compresslevel=6, allowZip64=True) as zf:
        zf.write(out_utoc, arcname='ABumpyRide-Windows.utoc')
        zf.write(out_ucas, arcname='ABumpyRide-Windows.ucas')
        zf.write(vanilla_paks / 'ABumpyRide-Windows.pak', arcname='ABumpyRide-Windows.pak')

    size_mb = out_zip_path.stat().st_size / (1024 * 1024)
    print(f'[{lang}] Done. Output: {out_zip_path} ({size_mb:.0f} MB)')

    # Cleanup workdir to save space
    shutil.rmtree(workdir, ignore_errors=True)


def main():
    validate_inputs()
    version = get_version()
    print(f'Version: v{version}')
    print(f'Vanilla source: {VANILLA_BACKUP}')

    langs = sys.argv[1:] if len(sys.argv) > 1 else ['fr', 'de', 'es', 'jp']
    dist_dir = ROOT / 'releases' / 'github_repo' / f'dist_v{version}'

    for lang in langs:
        if lang not in ('fr', 'de', 'es', 'jp'):
            print(f'WARNING: unknown lang {lang!r}, skipping')
            continue
        out_zip = dist_dir / f'ABR-{lang}_v{version}_prepatched.zip'
        try:
            build_lang(lang, version, out_zip)
        except SystemExit as e:
            print(f'[{lang}] FAILED: {e}')
            raise


if __name__ == '__main__':
    main()
