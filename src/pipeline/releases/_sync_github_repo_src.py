"""Synchronise releases/github_repo/src/ depuis les sources canoniques du repo principal.

Sources canoniques :
- translations FR/DE/ES/JP : translations/<lang>/*.json
- installer par langue : patch-<lang>/* (hors patched_assets.bak_*)
- outils source : tools/{bp_string_patcher,bp_offset_patcher,mainmap_patcher}/{Program.cs,*.csproj}
                  scripts/datatable_text_patcher/{Program.cs,*.csproj}
- docs racine : TRANSLATION_RULES.md, PROCESS_NEW_LANGUAGE.md

Usage :
    python releases/_sync_github_repo_src.py            # copie effective
    python releases/_sync_github_repo_src.py --check    # dry-run, exit 1 si diff

Utilise --check dans le hook PreToolUse pour bloquer un push si src/ est en retard.
"""
from __future__ import annotations
import hashlib
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'releases' / 'github_repo' / 'src'

PATCH_LANGS = ['fr', 'de', 'es', 'jp']

TOOL_DIRS = [
    ('tools', 'bp_string_patcher'),
    ('tools', 'bp_offset_patcher'),
    ('tools', 'mainmap_patcher'),
    ('scripts', 'datatable_text_patcher'),
]

DOC_FILES = ['TRANSLATION_RULES.md', 'PROCESS_NEW_LANGUAGE.md', 'MAINTAINER.md']

# Scripts du pipeline build qui doivent etre dans le repo public (un fork doit pouvoir
# rebuild). Listed explicitement plutot que de tout copier staging/ (qui contient
# plein de scripts one-shot historiques + workdirs).
PIPELINE_SCRIPTS = [
    'staging/_package_fr.py',
    'staging/_package_de.py',
    'staging/_package_es.py',
    'staging/_rebundle.py',
    'staging/_patch_bpop.py',
    'staging/_restore_bpop_from_release.py',
    'staging/_inject_extra_strings.py',
    'staging/_make_jp_font_overrides.py',
    'releases/_sync_github_repo_src.py',
    'releases/_build_prepatched.py',
    'releases/_make_v148_installer_zips.py',
]

EXCLUDE_DIR_NAMES = {'bin', 'obj', '.vs'}


def file_hash(path: Path) -> str:
    h = hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(1 << 20), b''):
            h.update(chunk)
    return h.hexdigest()


def plan_pairs() -> list[tuple[Path, Path]]:
    """Retourne (src_file, dest_file) pour tous les fichiers qui devraient exister dans src/."""
    pairs: list[tuple[Path, Path]] = []

    # FR / DE / ES / JP translations
    for lang in ['fr', 'de', 'es', 'jp']:
        src_dir = ROOT / 'translations' / lang
        if not src_dir.exists():
            continue
        for src in sorted(src_dir.glob('*.json')):
            pairs.append((src, DEST / 'languages' / lang / 'translations' / src.name))

    # Installer par langue
    for lang in PATCH_LANGS:
        src_dir = ROOT / f'patch-{lang}'
        dest_dir = DEST / 'languages' / lang / 'installer'
        if not src_dir.exists():
            continue
        for src in src_dir.rglob('*'):
            if not src.is_file():
                continue
            rel = src.relative_to(src_dir)
            if rel.parts and rel.parts[0].startswith('patched_assets.bak'):
                continue
            pairs.append((src, dest_dir / rel))

    # Outils source
    for parent, tool in TOOL_DIRS:
        src_dir = ROOT / parent / tool
        if not src_dir.exists():
            continue
        dest_dir = DEST / 'tools_src' / tool
        for src in src_dir.rglob('*'):
            if not src.is_file():
                continue
            rel = src.relative_to(src_dir)
            if any(part in EXCLUDE_DIR_NAMES for part in rel.parts[:-1]):
                continue
            pairs.append((src, dest_dir / rel))

    # Docs racine
    for name in DOC_FILES:
        src = ROOT / name
        if src.exists():
            pairs.append((src, DEST / name))

    # Pipeline build scripts (Python) -> src/pipeline/
    for rel_path in PIPELINE_SCRIPTS:
        src = ROOT / rel_path
        if src.exists():
            # Conserve la structure : staging/_foo.py -> pipeline/staging/_foo.py
            pairs.append((src, DEST / 'pipeline' / rel_path))

    return pairs


MANAGED_DIRS = [
    DEST / 'languages',
    DEST / 'tools_src',
    DEST / 'pipeline',
]
MANAGED_ROOT_FILES = {DEST / name for name in DOC_FILES}


def is_managed(path: Path) -> bool:
    """Le sync ne touche que ce qui est sous languages/ ou tools_src/, plus les .md listés."""
    if path in MANAGED_ROOT_FILES:
        return True
    for d in MANAGED_DIRS:
        try:
            path.relative_to(d)
            return True
        except ValueError:
            continue
    return False


def existing_dest_files() -> set[Path]:
    """Fichiers sous src/ que le sync gère (pour détecter les orphelins à supprimer)."""
    out: set[Path] = set()
    if DEST.exists():
        for p in DEST.rglob('*'):
            if p.is_file() and is_managed(p):
                out.add(p)
    return out


def diff(pairs: list[tuple[Path, Path]]) -> tuple[list[str], list[str], list[str]]:
    """Retourne (à_créer, à_mettre_à_jour, à_supprimer) en chemins relatifs à DEST."""
    expected_dest = {dest for _, dest in pairs}
    actual_dest = existing_dest_files()

    to_create: list[str] = []
    to_update: list[str] = []

    for src, dest in pairs:
        rel = str(dest.relative_to(DEST)).replace('\\', '/')
        if not dest.exists():
            to_create.append(rel)
            continue
        if dest.stat().st_size != src.stat().st_size:
            to_update.append(rel)
            continue
        if file_hash(src) != file_hash(dest):
            to_update.append(rel)

    to_delete = [
        str(p.relative_to(DEST)).replace('\\', '/')
        for p in actual_dest - expected_dest
    ]

    return sorted(to_create), sorted(to_update), sorted(to_delete)


def sync(pairs: list[tuple[Path, Path]]) -> None:
    """Copie src→dest pour chaque pair, puis supprime les orphelins."""
    expected_dest = {dest for _, dest in pairs}

    for src, dest in pairs:
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dest)

    for p in existing_dest_files() - expected_dest:
        try:
            p.unlink()
        except OSError:
            pass

    # Nettoyer les dossiers vides
    for d in sorted([p for p in DEST.rglob('*') if p.is_dir()], reverse=True):
        try:
            d.rmdir()
        except OSError:
            pass


def main() -> int:
    check = '--check' in sys.argv
    quiet = '--quiet' in sys.argv

    pairs = plan_pairs()
    to_create, to_update, to_delete = diff(pairs)

    total_diff = len(to_create) + len(to_update) + len(to_delete)

    if check:
        if total_diff == 0:
            if not quiet:
                print(f'OK : src/ aligne sur les sources ({len(pairs)} fichiers verifies)')
            return 0
        if not quiet:
            print(f'DIFF detecte : {len(to_create)} a creer, {len(to_update)} a mettre a jour, {len(to_delete)} a supprimer')
            for rel in (to_create[:10] + to_update[:10] + to_delete[:10]):
                print(f'  - {rel}')
            if total_diff > 30:
                print(f'  ... (+{total_diff - 30} autres)')
        return 1

    if total_diff == 0:
        print('Rien a synchroniser.')
        return 0

    print(f'Synchronisation : +{len(to_create)} ~{len(to_update)} -{len(to_delete)}')
    sync(pairs)
    print('Sync terminee.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
