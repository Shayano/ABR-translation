"""Build the 4 installer ZIPs for v1.4.8 (FR / DE / ES / JP)."""
import zipfile, os, sys
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding='utf-8')
except Exception:
    pass

ROOT = Path(r'F:\Tools\ABR-fr')
OUT_DIR = ROOT / 'releases' / 'github_repo' / 'dist_v1.4.8'
OUT_DIR.mkdir(parents=True, exist_ok=True)

LANGS = ['fr', 'de', 'es', 'jp']

for lang in LANGS:
    src = ROOT / f'patch-{lang}'
    out_zip = OUT_DIR / f'ABR-{lang}_v1.4.8.zip'
    if out_zip.exists():
        out_zip.unlink()

    EXCLUDE_DIRS = {'patched_assets.bak_fr', 'patched_assets.bak_de', 'patched_assets.bak_es', 'patched_assets.bak_jp'}
    EXCLUDE_EXT = {'.png'}
    files = []
    for root, dirs, names in os.walk(src):
        dirs[:] = [d for d in dirs if d not in EXCLUDE_DIRS]
        for n in names:
            if Path(n).suffix.lower() in EXCLUDE_EXT:
                continue
            files.append(Path(root) / n)
    files.sort()

    total = sum(f.stat().st_size for f in files)
    with zipfile.ZipFile(out_zip, 'w', compression=zipfile.ZIP_DEFLATED,
                         compresslevel=6, allowZip64=True) as zf:
        for f in files:
            arcname = f'patch-{lang}/' + f.relative_to(src).as_posix()
            zf.write(f, arcname=arcname)

    out_size = out_zip.stat().st_size
    ratio = (1 - out_size / total) * 100
    print(f'{lang}: {len(files)} files | source {total/1e6:.1f} MB -> zip {out_size/1e6:.1f} MB ({ratio:.1f}% smaller)')

print(f'\nDone. Outputs in {OUT_DIR}')
