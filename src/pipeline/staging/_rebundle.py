"""
Re-bundle a language patch after rebuild via _package_<lang>.py.

Unlike the historical _bundle_de.py / _bundle_es.py scripts (which overwrite
mod_version/mod_date/changelog with hardcoded values - artifacts of the initial
DE/ES seed releases), this wrapper ONLY touches the bundle{} block of the
manifest (uasset/uexp/umap counts, total_files, approx_size_mb). Everything else
is preserved as-is, which makes it safe to re-run for every release.

Usage:
    python staging/_rebundle.py de
    python staging/_rebundle.py es
    python staging/_rebundle.py de es     # multiple languages at once
"""
from __future__ import annotations
import json, os, shutil, sys
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

ROOT = Path(__file__).resolve().parents[1]


def rebundle(lang: str) -> None:
    lang_upper = lang.upper()
    staged = ROOT / 'staging' / f'legacy_patched_{lang_upper}'
    patch_dir = ROOT / f'patch-{lang}'
    patched_assets = patch_dir / 'patched_assets'
    manifest_path = patch_dir / 'manifest.json'

    if not staged.exists():
        sys.exit(f'ERROR: source missing -> {staged}')
    if not manifest_path.exists():
        sys.exit(f'ERROR: manifest missing -> {manifest_path}')

    print(f'\n=== rebundle {lang} ===')

    # 1. Reset patched_assets/
    if patched_assets.exists():
        shutil.rmtree(patched_assets)
    patched_assets.mkdir(parents=True)

    # 2. Copy (skip _CONSOLIDATION_INFO.json)
    counts = {'uasset': 0, 'uexp': 0, 'umap': 0}
    total_size = 0
    file_count = 0
    for root, _dirs, files in os.walk(staged):
        for f in files:
            if f == '_CONSOLIDATION_INFO.json':
                continue
            src = Path(root) / f
            rel = src.relative_to(staged)
            dst = patched_assets / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)
            file_count += 1
            ext = src.suffix.lstrip('.').lower()
            if ext in counts:
                counts[ext] += 1
            total_size += src.stat().st_size

    size_mb = round(total_size / (1024 * 1024), 1)
    print(f'  copied {file_count} files ({size_mb} MB) -> patch-{lang}/patched_assets/')
    print(f'  uassets={counts["uasset"]}, uexp={counts["uexp"]}, umap={counts["umap"]}')

    # 3. Update ONLY the bundle{} section of the manifest, preserve everything else
    with manifest_path.open('r', encoding='utf-8-sig') as f:
        manifest = json.load(f)

    prev_version = manifest.get('mod_version', '?')
    manifest['bundle'] = {
        'patched_uasset_count': counts['uasset'],
        'patched_umap_count': counts['umap'],
        'patched_uexp_count': counts['uexp'],
        'total_files': file_count,
        'approx_size_mb': size_mb,
    }

    with manifest_path.open('w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
        f.write('\n')

    print(f'  manifest.bundle{{}} updated (mod_version preserved: v{prev_version})')


def main(argv: list[str]) -> None:
    if len(argv) < 2:
        sys.exit('Usage: python staging/_rebundle.py <lang> [<lang> ...]')
    for lang in argv[1:]:
        rebundle(lang)
    print('\nOK.')


if __name__ == '__main__':
    main(sys.argv)
