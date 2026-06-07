"""
Helper reutilisable : injecte les 10 strings absentes de l'extract initial
(`fr_strings_BP_all.json`) mais presentes dans le bytecode du jeu.

A appeler imperativement apres tout `_init_<lang>_structure.py` ou tout
re-baseline post-update du jeu, AVANT de commencer la traduction.

Doc : memory/reference_extra_strings_not_in_extract.md
Usage :
    python staging/_inject_extra_strings.py <lang>
    # exemple : python staging/_inject_extra_strings.py jp
"""
import json, os, sys

# Liste canonique - voir memory/reference_extra_strings_not_in_extract.md
EXTRA_STRINGS = {
    'EndOfDayScreen_Paper.uasset': [
        'Money made today: ',
    ],
    'NewShopMenu.uasset': [
        'OWNED',
        'PAINT',
    ],
    'QuestBoard.uasset': [
        'Click to lock',
        'Click to unlock',
    ],
    'TrainScreen.uasset': [
        'Click to drop explosives',
        'Tracks may be slippery!',
        'Watch out for tornadoes!',
        'Water usage is twice as fast!',
        'You are lost!',
    ],
}

import pathlib
ROOT = str(pathlib.Path(__file__).resolve().parents[1])


def inject(lang):
    bp_path = os.path.join(ROOT, 'translations', lang, 'strings_BP.json')
    if not os.path.exists(bp_path):
        sys.exit(f'ERROR: {bp_path} not found - run _init_{lang}_structure.py first')

    with open(bp_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    added = 0
    for asset_name, originals in EXTRA_STRINGS.items():
        asset = next((a for a in data if a['FileName'] == asset_name), None)
        if asset is None:
            asset = {'FileName': asset_name, 'Values': []}
            data.append(asset)
            print(f'  Created asset block: {asset_name}')
        existing = {v.get('Original') for v in asset['Values']}
        for orig in originals:
            if orig not in existing:
                asset['Values'].append({'Original': orig, 'NewValue': ''})
                added += 1
                print(f'  Added (empty, to translate): {asset_name}: {orig!r}')

    with open(bp_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    if added:
        print(f'\nInjected {added} new strings into {bp_path}')
        print('-> All NewValue are empty. Add translations before running the package pipeline.')
    else:
        print(f'\nAll 10 extra strings already present in {bp_path}. Nothing to do.')


def audit_all_langs():
    """Audit : print which extra strings are present (and translated) per lang."""
    trans_dir = os.path.join(ROOT, 'translations')
    if not os.path.isdir(trans_dir):
        sys.exit(f'ERROR: {trans_dir} not found')
    langs = sorted(d for d in os.listdir(trans_dir)
                   if os.path.isdir(os.path.join(trans_dir, d)))
    for lang in langs:
        bp_path = os.path.join(trans_dir, lang, 'strings_BP.json')
        if not os.path.exists(bp_path):
            continue
        with open(bp_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
        present = 0
        translated = 0
        total = sum(len(v) for v in EXTRA_STRINGS.values())
        for asset_name, originals in EXTRA_STRINGS.items():
            asset = next((a for a in data if a['FileName'] == asset_name), None)
            if asset is None:
                continue
            existing = {v.get('Original'): v.get('NewValue', '') for v in asset['Values']}
            for orig in originals:
                if orig in existing:
                    present += 1
                    if existing[orig].strip():
                        translated += 1
        flag = 'OK' if present == total and translated == total else 'GAP'
        print(f'  [{flag}] {lang}: {present}/{total} present, {translated}/{total} translated')


if __name__ == '__main__':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

    if len(sys.argv) < 2:
        print('Usage:')
        print(f'  python {sys.argv[0]} <lang>     # inject into translations/<lang>/strings_BP.json')
        print(f'  python {sys.argv[0]} --audit    # report which langs have all 10 strings translated')
        sys.exit(1)

    arg = sys.argv[1]
    if arg == '--audit':
        audit_all_langs()
    else:
        inject(arg)
