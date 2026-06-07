"""
Re-patch SpecialPassenger AND QuestTicket via BPOffsetPatcher for a given language.

WHY THIS SCRIPT EXISTS:
  These 2 Blueprints only tolerate BPOffsetPatcher. KissE breaks their EX_Jump
  offsets and triggers an infinite recursion crash when picking up a Shareholder
  (canonical signature documented in patch-de/manifest.json changelog v1.4.4 and
  v1.4.8). Historical pipelines _package_de.py and _package_es.py let these BPs
  go through KissE -> crash.

  As of v1.4.8, the _package_*.py pipelines have been patched to EXCLUDE SP+QT
  from KissE -> they end up as vanilla in staging/legacy_patched_<LANG>/. This
  script takes over: it extracts SP+QT entries from translations/<lang>/strings_BP.json,
  applies BPOffsetPatcher to the vanilla binaries, and overwrites the outputs in
  patch-<lang>/patched_assets/ AND staging/legacy_patched_<LANG>/.

KNOWN LIMITATION (to investigate):
  Under certain combinations of strings (e.g. +23 octets cumulative delta on SP
  in v1.4.8 from 3 minimal changes), BPOffsetPatcher itself produces a binary
  that also crashes. Until the root cause is understood, the safe fallback is
  to restore SP+QT from the binary of the previous working release (see
  staging/_restore_bpop_from_release.py).

Usage:
    python staging/_patch_bpop.py de
    python staging/_patch_bpop.py es
    python staging/_patch_bpop.py de es
"""
from __future__ import annotations
import json, os, shutil, subprocess, sys
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

ROOT = Path(r'F:\Tools\ABR-fr')
VANILLA = ROOT / 'staging' / '_vanilla_post_update_legacy'
USMAP = ROOT / 'tools' / 'KismetEditor' / 'ABumpyRide.usmap'
BPOP = ROOT / 'tools' / 'bp_offset_patcher' / 'bin' / 'Release' / 'net8.0' / 'BPOffsetPatcher.exe'

# Config for BPs that only tolerate BPOffsetPatcher
BPOP_BPS = [
    {
        'name': 'SpecialPassenger.uasset',
        'rel_dir': Path('ABumpyRide/Content/Chooch/BP/Actors/Passenger'),
        'export': 'ExecuteUbergraph_SpecialPassenger',
    },
    {
        'name': 'QuestTicket.uasset',
        'rel_dir': Path('ABumpyRide/Content/Chooch/BP/UI/TrainScreen'),
        'export': 'ExecuteUbergraph_QuestTicket',
    },
]


def extract_entries(bp_data: list, filename: str) -> list[dict]:
    """Extract all entries for a given FileName, WITHOUT deduplication.

    No-dedup is critical: otherwise BPOP misses the 2nd occurrences of ' law signs',
    ' hours', ' times' etc. (see DE v1.4.7 changelog for the historical bug)."""
    out = []
    for entry in bp_data:
        if entry.get('FileName') == filename:
            for v in entry.get('Values', []):
                o, n = v.get('Original', ''), v.get('NewValue', '')
                if o and n:
                    out.append({'Original': o, 'Translation': n})
            break
    return out


def run_bpop_for_bp(lang: str, bp_cfg: dict, bp_data: list) -> bool:
    """Run BPOP on a single BP. Returns True on success, False otherwise."""
    name = bp_cfg['name']
    rel_dir = bp_cfg['rel_dir']
    export = bp_cfg['export']
    base = name.removesuffix('.uasset')

    entries = extract_entries(bp_data, name)
    if not entries:
        print(f'  [skip] {name}: no entries in strings_BP.json')
        return True

    print(f'  {name}: {len(entries)} entries extracted (duplicates kept)')

    workdir = ROOT / 'staging' / f'{lang}_{base}_rebuild'
    if workdir.exists():
        shutil.rmtree(workdir)
    workdir.mkdir(parents=True)
    out_dir = workdir / rel_dir
    out_dir.mkdir(parents=True)

    json_in = workdir / f'_{base}_{lang}_trad.json'
    with json_in.open('w', encoding='utf-8') as f:
        json.dump(entries, f, ensure_ascii=False, indent=2)

    vanilla_uasset = VANILLA / rel_dir / f'{base}.uasset'
    vanilla_uexp = VANILLA / rel_dir / f'{base}.uexp'
    if not vanilla_uasset.exists():
        print(f'  [skip] {name}: vanilla missing -> {vanilla_uasset}')
        return False

    cmd = [
        str(BPOP),
        str(vanilla_uasset),
        str(out_dir),
        str(USMAP),
        f'--export={export}',
        f'--strings-json={json_in}',
    ]
    result = subprocess.run(cmd, capture_output=True, text=True,
                            encoding='utf-8', errors='replace')
    if result.returncode != 0:
        print(f'  [FAIL] {name}: BPOP exit {result.returncode}')
        print('  STDERR:', (result.stderr or '')[-800:])
        return False

    out_uasset = out_dir / f'{base}.uasset'
    out_uexp = out_dir / f'{base}.uexp'
    if not (out_uasset.exists() and out_uexp.exists()):
        print(f'  [FAIL] {name}: BPOP output not found')
        return False

    d = out_uexp.stat().st_size - vanilla_uexp.stat().st_size
    print(f'  {name}: .uexp delta {d:+d} bytes (vanilla {vanilla_uexp.stat().st_size} -> patched {out_uexp.stat().st_size})')

    # Overwrite in patch-<lang>/patched_assets/ AND staging/legacy_patched_<LANG>/
    targets = [
        ROOT / f'patch-{lang}' / 'patched_assets' / rel_dir,
        ROOT / 'staging' / f'legacy_patched_{lang.upper()}' / rel_dir,
    ]
    for t in targets:
        t.mkdir(parents=True, exist_ok=True)
        for ext in ('.uasset', '.uexp'):
            shutil.copy2(out_dir / f'{base}{ext}', t / f'{base}{ext}')
    print(f'  {name}: overwritten in patch-{lang}/ + staging/legacy_patched_{lang.upper()}/')
    return True


def run_for_lang(lang: str) -> None:
    print(f'\n=== BPOP patch for {lang} ===')
    trans = ROOT / 'translations' / lang / 'strings_BP.json'
    if not trans.exists():
        sys.exit(f'ERROR: source missing -> {trans}')
    with trans.open('r', encoding='utf-8-sig') as f:
        bp_data = json.load(f)

    ok = all(run_bpop_for_bp(lang, bp_cfg, bp_data) for bp_cfg in BPOP_BPS)
    if not ok:
        print(f'\n  [WARN] {lang}: at least one BP failed. Test in-game (Shareholder pickup).')
        print(f'  Safe fallback: python staging/_restore_bpop_from_release.py {lang} <previous_tag>')


def main(argv: list[str]) -> None:
    if len(argv) < 2:
        sys.exit('Usage: python staging/_patch_bpop.py <lang> [<lang> ...]\n'
                 'IMPORTANT: test in-game (Shareholder pickup) after. If crash,\n'
                 'safe fallback: python staging/_restore_bpop_from_release.py <lang> <previous_tag>')
    for lang in argv[1:]:
        run_for_lang(lang)
    print('\nOK. DO NOT FORGET: test the Shareholder pickup in-game.')


if __name__ == '__main__':
    main(sys.argv)
