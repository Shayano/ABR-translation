"""
Restore SpecialPassenger + QuestTicket binaries for a language from a previous
GitHub release that was known to work.

WHY: BPOffsetPatcher has a non-diagnosed regression (discovered in v1.4.8) that
can make SP/QT crash even via a "correct" BPOP run, when certain strings are
modified (a few cumulative bytes of delta are enough). When this happens, the
safest fallback is to pull the binaries from a previous working release (e.g.
v1.4.7) and accept losing the translation changes that touched SP/QT.

This script:
- Downloads the installer zip for the given release tag (via gh release download)
- Extracts SP + QT binaries
- Overwrites them in patch-<lang>/patched_assets/ AND staging/legacy_patched_<LANG>/

Usage:
    python staging/_restore_bpop_from_release.py de v1.4.7
    python staging/_restore_bpop_from_release.py es v1.4.7
"""
from __future__ import annotations
import shutil, subprocess, sys, zipfile
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

ROOT = Path(r'F:\Tools\ABR-fr')

BPOP_FILES = [
    Path('ABumpyRide/Content/Chooch/BP/Actors/Passenger/SpecialPassenger.uasset'),
    Path('ABumpyRide/Content/Chooch/BP/Actors/Passenger/SpecialPassenger.uexp'),
    Path('ABumpyRide/Content/Chooch/BP/UI/TrainScreen/QuestTicket.uasset'),
    Path('ABumpyRide/Content/Chooch/BP/UI/TrainScreen/QuestTicket.uexp'),
]


def restore(lang: str, tag: str) -> None:
    print(f'\n=== Restore SP+QT from {tag} for {lang} ===')

    workdir = ROOT / 'staging' / f'_restore_{lang}_{tag}'
    if workdir.exists():
        shutil.rmtree(workdir)
    workdir.mkdir(parents=True)

    zip_name = f'ABR-{lang}_{tag}.zip'
    zip_path = workdir / zip_name

    print(f'  gh release download {tag} -p {zip_name}')
    result = subprocess.run(
        ['gh', 'release', 'download', tag, '-p', zip_name,
         '--repo', 'Shayano/ABR-translation',
         '--dir', str(workdir)],
        capture_output=True, text=True, encoding='utf-8', errors='replace'
    )
    if result.returncode != 0:
        sys.exit(f'ERROR: gh download failed - {result.stderr}')

    print(f'  extracting {zip_path.name}')
    with zipfile.ZipFile(zip_path) as zf:
        zf.extractall(workdir / 'extracted')

    src_root = workdir / 'extracted' / f'patch-{lang}' / 'patched_assets'
    if not src_root.exists():
        sys.exit(f'ERROR: unexpected zip layout, missing {src_root}')

    targets = [
        ROOT / f'patch-{lang}' / 'patched_assets',
        ROOT / 'staging' / f'legacy_patched_{lang.upper()}',
    ]
    for rel in BPOP_FILES:
        src = src_root / rel
        if not src.exists():
            print(f'  [skip] {rel.name} missing from {tag} zip')
            continue
        for t in targets:
            dst = t / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)
        print(f'  restored {rel.name} ({src.stat().st_size} bytes)')

    print(f'\n  OK. Do not forget to re-rebundle: python staging/_rebundle.py {lang}')


def main(argv: list[str]) -> None:
    if len(argv) != 3:
        sys.exit('Usage: python staging/_restore_bpop_from_release.py <lang> <tag>\n'
                 'Example: python staging/_restore_bpop_from_release.py de v1.4.7')
    restore(argv[1], argv[2])


if __name__ == '__main__':
    main(sys.argv)
