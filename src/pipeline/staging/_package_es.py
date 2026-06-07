"""
Pipeline de packaging ES (adapté de _package_de.py).

Produit dans staging/legacy_patched_ES/ :
- BP patchés (KissE Replacement avec translations/es/strings_BP.json)
  * SpecialPassenger.uasset/.uexp re-patché ensuite via BPOffsetPatcher (v1.4.5 hotfix)
- Maps patchées (KissE Replacement avec translations/es/strings_maps.json + rename trick .umap→.uasset)
- 7 enums patchés (datatable_text_patcher --inject-enum)
- Tutorial_Table.uasset (DTP étendu)
- SkinButtonTable patchée (datatable_text_patcher --inject)

Tous les vanilla viennent de staging/_vanilla_post_update_legacy/.
"""
import os, shutil, subprocess, json, sys

try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

import pathlib
ROOT = str(pathlib.Path(__file__).resolve().parents[1])
TRANS_ES = os.path.join(ROOT, 'translations', 'es')
VANILLA = os.path.join(ROOT, 'staging', '_vanilla_post_update_legacy')
PATCHED_ES = os.path.join(ROOT, 'staging', 'legacy_patched_ES')
WORKDIR = os.path.join(ROOT, 'staging', 'es_workdir')

KISSE = os.path.join(ROOT, 'tools', 'KismetEditor', 'KissE.exe')
KISSE_DIR = os.path.dirname(KISSE)
USMAP = os.path.join(KISSE_DIR, 'ABumpyRide.usmap')
DTP = os.path.join(ROOT, 'scripts', 'datatable_text_patcher', 'bin', 'Release', 'net9.0', 'datatable_text_patcher.exe')
BPOP = os.path.join(ROOT, 'tools', 'bp_offset_patcher', 'bin', 'Release', 'net8.0', 'BPOffsetPatcher.exe')

if os.path.exists(WORKDIR):
    shutil.rmtree(WORKDIR)
os.makedirs(WORKDIR)
os.makedirs(PATCHED_ES, exist_ok=True)

LEGACY_BP_DIR = os.path.join(WORKDIR, 'legacy_BP')
LEGACY_MAPS_DIR = os.path.join(WORKDIR, 'legacy_maps')
ENUM_OUT_DIR = os.path.join(WORKDIR, 'enum_out')
DT_OUT_DIR = os.path.join(WORKDIR, 'datatable_out')
SP_OUT_DIR = os.path.join(WORKDIR, 'sp_out')

os.makedirs(ENUM_OUT_DIR, exist_ok=True)
os.makedirs(DT_OUT_DIR, exist_ok=True)
os.makedirs(SP_OUT_DIR, exist_ok=True)


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
        safe_print(f'    [exit {result.returncode}] (note: KissE peut crasher cosmetique sur stdout - ignorer si BAKs crees)')
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
    """Run KissE Replacement on strings_BP.json (excluding SpecialPassenger + QuestTicket: BPOP only).

    IMPORTANT EXCLUSIONS:
    - SpecialPassenger.uasset: KissE breaks its EX_Jump offsets -> Shareholder pickup
      recursion crash.
    - QuestTicket.uasset     : same issue (discovered in v1.4.8).
    After this pipeline finishes, run `python staging/_patch_bpop.py es` to translate
    these 2 BPs via BPOP (and TEST the Shareholder pickup in-game - see MAINTAINER.md
    "Pitfall #1" for the safe fallback if it still crashes).
    """
    print('\n=== Step 2: KissE BP Replacement (SpecialPassenger + QuestTicket excluded -> BPOP) ===')

    with open(os.path.join(TRANS_ES, 'strings_BP.json'), 'r', encoding='utf-8-sig') as f:
        bp_data = json.load(f)
    BPOP_ONLY = {'SpecialPassenger.uasset', 'QuestTicket.uasset'}
    bp_data_for_kisse = [e for e in bp_data if e.get('FileName') not in BPOP_ONLY]
    excluded = len(bp_data) - len(bp_data_for_kisse)
    print(f'  Excluded {excluded} entries (SpecialPassenger + QuestTicket, BPOP-only - run staging/_patch_bpop.py es after)')

    json_in = os.path.join(LEGACY_BP_DIR, 'BP_translations.json')
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

    bak_count = 0
    for root, dirs, files in os.walk(LEGACY_BP_DIR):
        for f in files:
            if f.endswith('.bak'):
                os.remove(os.path.join(root, f))
                bak_count += 1
    print(f'  Cleaned {bak_count} .bak files')
    os.remove(json_in)


def kisse_replace_maps():
    print('\n=== Step 3 : KissE Maps Replacement (rename trick) ===')
    umaps = []
    for root, dirs, files in os.walk(LEGACY_BP_DIR):
        for f in files:
            if f.endswith('.umap'):
                umaps.append(os.path.join(root, f))
    print(f'  Found {len(umaps)} .umap files to rename')

    renamed = []
    for p in umaps:
        new = p.replace('.umap', '.uasset')
        os.rename(p, new)
        renamed.append((p, new))

    maps_json_src = os.path.join(TRANS_ES, 'strings_maps.json')
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

    bak_count = 0
    for root, dirs, files in os.walk(LEGACY_BP_DIR):
        for f in files:
            if f.endswith('.bak'):
                os.remove(os.path.join(root, f))
                bak_count += 1
    print(f'  Cleaned {bak_count} .bak files')

    for orig, renamed_path in renamed:
        if os.path.exists(renamed_path):
            os.rename(renamed_path, orig)
    os.remove(json_in)


def datatable_inject_enum(asset_path_rel, json_name):
    src = os.path.join(VANILLA, asset_path_rel)
    if not os.path.exists(src):
        print(f'  SKIP: {asset_path_rel} not found in vanilla')
        return None
    json_in = os.path.join(TRANS_ES, json_name)
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
    print('\n=== Step 4b : datatable_text_patcher --inject-enum (Tutorial_Table) ===')
    src = os.path.join(VANILLA, 'ABumpyRide/Content/Chooch/BP/Dialogue/Tutorial_Table.uasset')
    if not os.path.exists(src):
        print(f'  SKIP: Tutorial_Table not found at {src}')
        return
    strings_bp = os.path.join(TRANS_ES, 'strings_BP.json')
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
    json_in = os.path.join(WORKDIR, '_tutorial_table_es_trad.json')
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
    json_in = os.path.join(TRANS_ES, 'skinbuttontable.json')
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


def patch_specialpassenger():
    """BPOffsetPatcher sur SpecialPassenger (v1.4.5 hotfix)."""
    print('\n=== Step 6 : BPOffsetPatcher (SpecialPassenger) ===')
    sp_rel = 'ABumpyRide/Content/Chooch/BP/Actors/Passenger/SpecialPassenger.uasset'
    src = os.path.join(VANILLA, sp_rel)
    if not os.path.exists(src):
        print(f'  SKIP: SpecialPassenger not found at {src}')
        return

    # Extract SP entries from strings_BP.json
    with open(os.path.join(TRANS_ES, 'strings_BP.json'), 'r', encoding='utf-8-sig') as f:
        bp = json.load(f)
    sp_entries = []
    seen = set()
    for entry in bp:
        if entry.get('FileName') == 'SpecialPassenger.uasset':
            for v in entry.get('Values', []):
                o = v.get('Original', ''); n = v.get('NewValue', '')
                if o and n and o not in seen:
                    sp_entries.append({'Original': o, 'Translation': n})
                    seen.add(o)
            break
    if not sp_entries:
        print('  No SpecialPassenger entries found in strings_BP.json')
        return

    json_in = os.path.join(WORKDIR, '_sp_es_trad.json')
    with open(json_in, 'w', encoding='utf-8') as f:
        json.dump(sp_entries, f, ensure_ascii=False, indent=2)

    out_dir_rel = os.path.join(SP_OUT_DIR, 'ABumpyRide', 'Content', 'Chooch', 'BP', 'Actors', 'Passenger')
    os.makedirs(out_dir_rel, exist_ok=True)

    result = run([
        BPOP,
        src,
        out_dir_rel,
        USMAP,
        '--export=ExecuteUbergraph_SpecialPassenger',
        f'--strings-json={json_in}',
    ])

    out_uasset = os.path.join(out_dir_rel, 'SpecialPassenger.uasset')
    out_uexp = os.path.join(out_dir_rel, 'SpecialPassenger.uexp')
    if os.path.exists(out_uasset) and os.path.exists(out_uexp):
        print(f'  Patched SpecialPassenger.uasset/.uexp ({len(sp_entries)} entries)')
        # Print size delta
        vexp = os.path.join(VANILLA, sp_rel.replace('.uasset', '.uexp'))
        if os.path.exists(vexp):
            d = os.path.getsize(out_uexp) - os.path.getsize(vexp)
            print(f'  .uexp delta: {d:+d} bytes (vanilla {os.path.getsize(vexp)} -> patched {os.path.getsize(out_uexp)})')
    else:
        print(f'  FAILED: SpecialPassenger not produced')


def consolidate():
    """Consolide tous les patches dans staging/legacy_patched_ES/."""
    print('\n=== Step 7 : consolidation -> staging/legacy_patched_ES/ ===')

    if os.path.exists(PATCHED_ES):
        old_info = os.path.join(PATCHED_ES, '_CONSOLIDATION_INFO.json')
        if os.path.exists(old_info):
            os.remove(old_info)
        for item in os.listdir(PATCHED_ES):
            p = os.path.join(PATCHED_ES, item)
            if os.path.isdir(p):
                shutil.rmtree(p)
            else:
                os.remove(p)
    else:
        os.makedirs(PATCHED_ES)

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
                    dst = os.path.join(PATCHED_ES, rel)
                    os.makedirs(os.path.dirname(dst), exist_ok=True)
                    shutil.copy2(src, dst)
                    info['sources'][label] += 1
                    if f.endswith('.uasset'):
                        info['patched_uasset_count'] += 1
                    elif f.endswith('.uexp'):
                        info['patched_uexp_count'] += 1
                    elif f.endswith('.umap'):
                        info['patched_umap_count'] += 1

    # IMPORTANT : SP_OUT_DIR consolidated LAST so it overrides any KissE attempt on SP
    copy_if_different_from_vanilla(LEGACY_BP_DIR, 'kisse_BP_maps')
    copy_if_different_from_vanilla(ENUM_OUT_DIR, 'datatable_enums')
    copy_if_different_from_vanilla(DT_OUT_DIR, 'datatable_skinbutton')
    copy_if_different_from_vanilla(SP_OUT_DIR, 'bp_offset_patcher_SP')

    info_path = os.path.join(PATCHED_ES, '_CONSOLIDATION_INFO.json')
    with open(info_path, 'w', encoding='utf-8') as f:
        json.dump(info, f, indent=2, ensure_ascii=False)

    print(f'  uassets: {info["patched_uasset_count"]}')
    print(f'  uexp:    {info["patched_uexp_count"]}')
    print(f'  umap:    {info["patched_umap_count"]}')
    print(f'  Sources: {info["sources"]}')


copy_vanilla_to_workdir()
kisse_replace_bp()
kisse_replace_maps()
inject_all_enums()
inject_tutorial_table()
inject_skinbuttontable()
patch_specialpassenger()
consolidate()

print('\n=== DONE ===')
print(f'Output: {PATCHED_ES}')
