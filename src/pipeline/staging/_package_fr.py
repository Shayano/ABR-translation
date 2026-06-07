"""
Pipeline de packaging FR.

Produit dans staging/legacy_patched_FR/ :
- BP patchés (KissE Replacement avec strings_BP.json, SpecialPassenger + QuestTicket exclus)
- Maps patchées (KissE Replacement avec strings_maps.json + rename trick .umap->.uasset)
- 7 enums patchés (datatable_text_patcher --inject-enum)
- Tutorial_Table patchée (DTP étendu)
- SkinButtonTable patchée (datatable_text_patcher --inject)

Workflow FR complet (identique à DE, l'étape MainMap est à part) :
    python staging/_package_fr.py     # ce script
    python staging/_patch_bpop.py fr  # BPOP pour SpecialPassenger + QuestTicket
    python staging/_rebundle.py fr    # staging/legacy_patched_FR -> patch-fr/patched_assets

Spécifique FR : MainMap.umap N'EST PAS patchée ici. Elle l'est à l'install time
par patch-fr/MainMapPatcher.exe (cf patch-fr/install.ps1 étape 5/3b) car son .uexp
de ~2.3 GB ne tient pas dans un Int32 MemoryStream UAssetAPI. DE/ES/JP n'ont pas
ce binaire et laissent MainMap vanilla (perte de 2 strings hardcodées acceptée).

Tous les vanilla viennent de staging/_vanilla_post_update_legacy/.
"""
import os, shutil, subprocess, json, sys

# Force UTF-8 sur stdout pour éviter les UnicodeEncodeError sur la console Windows
try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

import pathlib
ROOT = str(pathlib.Path(__file__).resolve().parents[1])
TRANS_FR = os.path.join(ROOT, 'translations', 'fr')
VANILLA = os.path.join(ROOT, 'staging', '_vanilla_post_update_legacy')
PATCHED_FR = os.path.join(ROOT, 'staging', 'legacy_patched_FR')
WORKDIR = os.path.join(ROOT, 'staging', 'fr_workdir')

KISSE = os.path.join(ROOT, 'tools', 'KismetEditor', 'KissE.exe')
KISSE_DIR = os.path.dirname(KISSE)
USMAP = os.path.join(KISSE_DIR, 'ABumpyRide.usmap')
DTP = os.path.join(ROOT, 'scripts', 'datatable_text_patcher', 'bin', 'Release', 'net9.0', 'datatable_text_patcher.exe')

# Reset workdir
if os.path.exists(WORKDIR):
    shutil.rmtree(WORKDIR)
os.makedirs(WORKDIR)
os.makedirs(PATCHED_FR, exist_ok=True)

LEGACY_BP_DIR = os.path.join(WORKDIR, 'legacy_BP')
LEGACY_MAPS_DIR = os.path.join(WORKDIR, 'legacy_maps')
ENUM_OUT_DIR = os.path.join(WORKDIR, 'enum_out')
DT_OUT_DIR = os.path.join(WORKDIR, 'datatable_out')

os.makedirs(ENUM_OUT_DIR, exist_ok=True)
os.makedirs(DT_OUT_DIR, exist_ok=True)


def safe_print(s):
    try:
        print(s)
    except Exception:
        try:
            sys.stdout.buffer.write((s + '\n').encode('utf-8', errors='replace'))
        except Exception:
            print(s.encode('ascii', errors='replace').decode('ascii'))


def run(cmd, cwd=None, env=None):
    safe_print(f'>>> {" ".join(repr(c) if " " in c else c for c in cmd)}')
    if cwd:
        safe_print(f'    cwd={cwd}')
    result = subprocess.run(cmd, cwd=cwd, env=env, capture_output=True,
                            text=True, encoding='utf-8', errors='replace')
    out = (result.stdout or '').strip()
    err = (result.stderr or '').strip()
    if result.returncode != 0:
        safe_print(f'    [exit {result.returncode}] (note: KissE crashes cosmétiques sur stdout = ignorer si BAKs créés)')
        if out:
            for line in out.splitlines()[-15:]:
                safe_print(f'    OUT: {line}')
        if err:
            for line in err.splitlines()[-15:]:
                safe_print(f'    ERR: {line}')
    else:
        if out:
            for line in out.splitlines()[-8:]:
                safe_print(f'    {line}')
    return result


def copy_vanilla_to_workdir():
    """Copie les BPs et maps vanilla dans le workdir pour patch."""
    print('\n=== Step 1 : copy vanilla BP/maps to workdir ===')
    src_root = os.path.join(VANILLA, 'ABumpyRide', 'Content')
    if not os.path.exists(src_root):
        sys.exit(f'Vanilla not found at {src_root}')

    bp_count = 0
    map_count = 0
    for root, dirs, files in os.walk(src_root):
        for f in files:
            src = os.path.join(root, f)
            rel = os.path.relpath(src, src_root)
            dst = os.path.join(LEGACY_BP_DIR, 'ABumpyRide', 'Content', rel)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copy2(src, dst)
            if f.endswith('.uasset') or f.endswith('.uexp'):
                bp_count += 1
            elif f.endswith('.umap'):
                map_count += 1
    print(f'  Copied to legacy_BP: {bp_count} .uasset/.uexp + {map_count} .umap')


def kisse_replace_bp():
    """Run KissE Replacement on .uasset BPs using translations/fr/strings_BP.json.

    IMPORTANT EXCLUSIONS:
    - SpecialPassenger.uasset: KissE breaks its EX_Jump offsets -> Shareholder pickup
      recursion crash.
    - QuestTicket.uasset     : same issue (discovered in v1.4.8).
    Both BPs must be patched via BPOffsetPatcher only. After this pipeline finishes,
    run `python staging/_patch_bpop.py fr` to translate them via BPOP (and TEST the
    Shareholder pickup in-game - see MAINTAINER.md "Pitfall #1" for the safe fallback
    if it still crashes).
    """
    print('\n=== Step 2: KissE BP Replacement (SpecialPassenger + QuestTicket excluded -> BPOP) ===')
    json_in = os.path.join(LEGACY_BP_DIR, 'BP_translations.json')
    with open(os.path.join(TRANS_FR, 'strings_BP.json'), 'r', encoding='utf-8-sig') as f:
        bp_data = json.load(f)
    BPOP_ONLY = {'SpecialPassenger.uasset', 'QuestTicket.uasset'}
    bp_data_for_kisse = [e for e in bp_data if e.get('FileName') not in BPOP_ONLY]
    excluded = len(bp_data) - len(bp_data_for_kisse)
    print(f'  Excluded {excluded} entries (SpecialPassenger + QuestTicket, BPOP-only - run staging/_patch_bpop.py fr after)')
    with open(json_in, 'w', encoding='utf-8') as f:
        json.dump(bp_data_for_kisse, f, indent=2, ensure_ascii=False)

    result = run([
        KISSE,
        json_in,
        LEGACY_BP_DIR,
        f'--map={USMAP}',
        '--version=5.3',
        '--process:all',
        '--patch-assignments',
        '--patch-all-functions',
    ], cwd=KISSE_DIR)

    # Cleanup BAKs
    bak_count = 0
    for root, dirs, files in os.walk(LEGACY_BP_DIR):
        for f in files:
            if f.endswith('.bak'):
                os.remove(os.path.join(root, f))
                bak_count += 1
    print(f'  Cleaned {bak_count} .bak files')
    os.remove(json_in)


def kisse_replace_maps():
    """KissE skip les .umap -> rename trick."""
    print('\n=== Step 3 : KissE Maps Replacement (rename trick) ===')
    umaps = []
    for root, dirs, files in os.walk(LEGACY_BP_DIR):
        for f in files:
            if f.endswith('.umap'):
                umaps.append(os.path.join(root, f))
    print(f'  Found {len(umaps)} .umap files to rename')

    # Rename .umap -> .uasset (le pair .uexp existe déjà tel quel - pas de rename pour lui)
    renamed = []
    for p in umaps:
        new = p.replace('.umap', '.uasset')
        os.rename(p, new)
        renamed.append((p, new))

    # Adapter le JSON FR pour les maps (FileName: .umap -> .uasset)
    maps_json_src = os.path.join(TRANS_FR, 'strings_maps.json')
    with open(maps_json_src, 'r', encoding='utf-8-sig') as f:
        maps_data = json.load(f)
    for asset in maps_data:
        if asset['FileName'].endswith('.umap'):
            asset['FileName'] = asset['FileName'].replace('.umap', '.uasset')

    json_in = os.path.join(LEGACY_BP_DIR, 'maps_translations.json')
    with open(json_in, 'w', encoding='utf-8') as f:
        json.dump(maps_data, f, indent=2, ensure_ascii=False)

    result = run([
        KISSE,
        json_in,
        LEGACY_BP_DIR,
        f'--map={USMAP}',
        '--version=5.3',
        '--process:all',
        '--patch-assignments',
        '--patch-all-functions',
    ], cwd=KISSE_DIR)

    # Cleanup BAKs (encore une fois)
    bak_count = 0
    for root, dirs, files in os.walk(LEGACY_BP_DIR):
        for f in files:
            if f.endswith('.bak'):
                os.remove(os.path.join(root, f))
                bak_count += 1
    print(f'  Cleaned {bak_count} .bak files')

    # Rename back .uasset -> .umap pour les maps
    for orig, renamed_path in renamed:
        if os.path.exists(renamed_path):
            os.rename(renamed_path, orig)
    os.remove(json_in)


def datatable_inject_enum(asset_path_rel, json_name):
    """Inject un enum traduit via datatable_text_patcher."""
    src = os.path.join(VANILLA, asset_path_rel)
    if not os.path.exists(src):
        print(f'  SKIP: {asset_path_rel} not found in vanilla')
        return None
    json_in = os.path.join(TRANS_FR, json_name)
    out_dir = os.path.join(ENUM_OUT_DIR, os.path.dirname(asset_path_rel))
    os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(ENUM_OUT_DIR, asset_path_rel)
    src_uexp = src.replace('.uasset', '.uexp')
    out_uexp = out.replace('.uasset', '.uexp')

    result = run([DTP, '--inject-enum', src, USMAP, json_in, out])
    if os.path.exists(out) and os.path.exists(src_uexp):
        if not os.path.exists(out_uexp):
            shutil.copy2(src_uexp, out_uexp)
    return out


def inject_all_enums():
    """7 enums."""
    print('\n=== Step 4 : datatable_text_patcher --inject-enum (7 enums) ===')
    enums = [
        ('ABumpyRide/Content/Chooch/BP/Enums/BuildingType.uasset', 'enum_buildingtype.json'),
        ('ABumpyRide/Content/Chooch/BP/Enums/FreightType.uasset', 'enum_freighttype.json'),
        ('ABumpyRide/Content/Chooch/BP/Actors/Passenger/PassengerEnum.uasset', 'enum_passengerenum.json'),
        ('ABumpyRide/Content/Chooch/BP/Enums/QuestLine.uasset', 'enum_questline.json'),
        ('ABumpyRide/Content/Chooch/BP/Enums/QuestType.uasset', 'enum_questtype.json'),
        ('ABumpyRide/Content/Chooch/BP/Enums/TitleScreenBlurbs.uasset', 'enum_titleblurbs.json'),
        ('ABumpyRide/Content/Chooch/BP/Enums/TitleScreenBlurbsRainy.uasset', 'enum_titleblurbsrainy.json'),
    ]
    ok = 0
    for asset_rel, json_name in enums:
        out = datatable_inject_enum(asset_rel, json_name)
        if out and os.path.exists(out):
            ok += 1
    print(f'  Patched {ok}/{len(enums)} enums')


def inject_tutorial_table():
    """Tutorial_Table.uasset (DataTable DialogueStructure) - DTP étendu walk DataTableExport + StrPropertyData."""
    print('\n=== Step 4b : datatable_text_patcher --inject-enum (Tutorial_Table) ===')
    src = os.path.join(VANILLA, 'ABumpyRide/Content/Chooch/BP/Dialogue/Tutorial_Table.uasset')
    if not os.path.exists(src):
        print(f'  SKIP: Tutorial_Table not found at {src}')
        return
    strings_bp = os.path.join(TRANS_FR, 'strings_BP.json')
    with open(strings_bp, 'r', encoding='utf-8-sig') as f:
        bp = json.load(f)
    tutorial_entries = []
    seen = set()
    for entry in bp:
        if entry.get('FileName') == 'Tutorial_Table.uasset':
            for v in entry.get('Values', []):
                o = v.get('Original', ''); n = v.get('NewValue', '')
                if o and n and o != n and o not in seen:
                    tutorial_entries.append({'source': o, 'translation': n})
                    seen.add(o)
            break
    if not tutorial_entries:
        print('  No Tutorial_Table entries found in strings_BP.json')
        return
    json_in = os.path.join(WORKDIR, '_tutorial_table_fr_trad.json')
    with open(json_in, 'w', encoding='utf-8') as f:
        json.dump(tutorial_entries, f, ensure_ascii=False, indent=2)
    out_rel = 'ABumpyRide/Content/Chooch/BP/Dialogue/Tutorial_Table.uasset'
    out = os.path.join(ENUM_OUT_DIR, out_rel)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    result = run([DTP, '--inject-enum', src, USMAP, json_in, out])
    if os.path.exists(out):
        print(f'  Patched Tutorial_Table.uasset ({len(tutorial_entries)} entries)')
    else:
        print(f'  FAILED: Tutorial_Table not produced')


def inject_skinbuttontable():
    print('\n=== Step 5 : datatable_text_patcher --inject (SkinButtonTable) ===')
    src = os.path.join(VANILLA, 'ABumpyRide/Content/Chooch/BP/UI/Menus/Shop/SkinButtonTable.uasset')
    json_in = os.path.join(TRANS_FR, 'skinbuttontable.json')
    out_rel = 'ABumpyRide/Content/Chooch/BP/UI/Menus/Shop/SkinButtonTable.uasset'
    out_dir = os.path.join(DT_OUT_DIR, os.path.dirname(out_rel))
    os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(DT_OUT_DIR, out_rel)
    result = run([DTP, '--inject', src, USMAP, json_in, out])
    if os.path.exists(out):
        src_uexp = src.replace('.uasset', '.uexp')
        out_uexp = out.replace('.uasset', '.uexp')
        if os.path.exists(src_uexp) and not os.path.exists(out_uexp):
            shutil.copy2(src_uexp, out_uexp)
        print(f'  Patched SkinButtonTable.uasset')
    else:
        print(f'  FAILED: SkinButtonTable not produced')


def consolidate():
    """Consolide tous les patches dans staging/legacy_patched_FR/.

    Stratégie : pour chaque fichier patché, comparer avec vanilla. S'il diffère, le copier
    dans legacy_patched_FR/. Sinon ignorer (= pas de modification effective).
    """
    print('\n=== Step 6 : consolidation -> staging/legacy_patched_FR/ ===')

    # Reset
    if os.path.exists(PATCHED_FR):
        old_info = os.path.join(PATCHED_FR, '_CONSOLIDATION_INFO.json')
        if os.path.exists(old_info):
            os.remove(old_info)
        for item in os.listdir(PATCHED_FR):
            p = os.path.join(PATCHED_FR, item)
            if os.path.isdir(p):
                shutil.rmtree(p)
            else:
                os.remove(p)
    else:
        os.makedirs(PATCHED_FR)

    info = {
        'patched_uasset_count': 0,
        'patched_uexp_count': 0,
        'patched_umap_count': 0,
        'sources': {},
    }

    def file_differs(a, b):
        if not os.path.exists(a) or not os.path.exists(b):
            return True
        return os.path.getsize(a) != os.path.getsize(b) or open(a, 'rb').read() != open(b, 'rb').read()

    def copy_if_different_from_vanilla(src_root, label):
        nonlocal info
        info['sources'][label] = 0
        for root, dirs, files in os.walk(src_root):
            for f in files:
                if not (f.endswith('.uasset') or f.endswith('.uexp') or f.endswith('.umap')):
                    continue
                src = os.path.join(root, f)
                rel = os.path.relpath(src, src_root)
                vanilla_eq = os.path.join(VANILLA, rel)
                if not os.path.exists(vanilla_eq):
                    continue
                if file_differs(src, vanilla_eq):
                    dst = os.path.join(PATCHED_FR, rel)
                    os.makedirs(os.path.dirname(dst), exist_ok=True)
                    shutil.copy2(src, dst)
                    info['sources'][label] += 1
                    if f.endswith('.uasset'):
                        info['patched_uasset_count'] += 1
                    elif f.endswith('.uexp'):
                        info['patched_uexp_count'] += 1
                    elif f.endswith('.umap'):
                        info['patched_umap_count'] += 1

    copy_if_different_from_vanilla(LEGACY_BP_DIR, 'kisse_BP_maps')
    copy_if_different_from_vanilla(ENUM_OUT_DIR, 'datatable_enums')
    copy_if_different_from_vanilla(DT_OUT_DIR, 'datatable_skinbutton')

    info_path = os.path.join(PATCHED_FR, '_CONSOLIDATION_INFO.json')
    with open(info_path, 'w', encoding='utf-8') as f:
        json.dump(info, f, indent=2, ensure_ascii=False)

    print(f'  uassets: {info["patched_uasset_count"]}')
    print(f'  uexp:    {info["patched_uexp_count"]}')
    print(f'  umap:    {info["patched_umap_count"]}')
    print(f'  Sources: {info["sources"]}')


# Run pipeline
copy_vanilla_to_workdir()
kisse_replace_bp()
kisse_replace_maps()
inject_all_enums()
inject_tutorial_table()
inject_skinbuttontable()
consolidate()

print('\n=== DONE ===')
print(f'Output: {PATCHED_FR}')
print('Next: python staging/_patch_bpop.py fr  (BPOP for SpecialPassenger + QuestTicket)')
print('Then: python staging/_rebundle.py fr    (legacy_patched_FR -> patch-fr/patched_assets)')
