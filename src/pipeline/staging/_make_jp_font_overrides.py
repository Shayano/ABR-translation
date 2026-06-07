"""
Generation des polices override pour le mod JP.

Strategie : cloner Engine/Content/EngineFonts/Roboto.uasset (composite font qui chaine
RobotoRegular + DroidSansFallback pour CJK) et renommer vers les chemins ABR :
 - /Game/Chooch/Art/UI/Fonts/Pixel_Times_Font
 - /Game/Chooch/Art/UI/Fonts/AwfullyDigital_Font
 - /Game/Chooch/Art/UI/Fonts/Cavalhatriz_Font
 - /Game/Chooch/Art/UI/Fonts/Pixel_Times_Bold_Font

Le moteur charge nos overrides via priorite pak -> tout texte utilisant ces fonts
beneficie automatiquement du fallback DroidSansFallback pour les glyphes CJK.

Trade-off : on perd le look pixel-art Pixel Times et le look digital AwfullyDigital,
remplaces par Roboto Regular + DroidSans pour CJK. Mais c'est la SEULE facon de
faire afficher les caracteres japonais sans refaire les atlases.
"""
import json, os, shutil, subprocess

ROOT = r'F:\Tools\ABR-fr'
UAGUI = os.path.join(ROOT, 'tools', 'UAssetGUI.exe')
ROBOTO_SRC = os.path.join(ROOT, 'staging', 'jp_fonts_legacy', 'Engine', 'Content', 'EngineFonts', 'Roboto.uasset')
WORK = os.path.join(ROOT, 'staging', 'jp_font_overrides')
os.makedirs(WORK, exist_ok=True)

# Liste des fonts a override (nom interne -> chemin asset complet sans extension)
TARGETS = [
    ('Pixel_Times_Font', '/Game/Chooch/Art/UI/Fonts/Pixel_Times_Font'),
    ('AwfullyDigital_Font', '/Game/Chooch/Art/UI/Fonts/AwfullyDigital_Font'),
    ('Cavalhatriz_Font', '/Game/Chooch/Art/UI/Fonts/Cavalhatriz_Font'),
    ('Pixel_Times_Bold_Font', '/Game/Chooch/Art/UI/Fonts/Pixel_Times_Bold_Font'),
]

# Step 1 : dump Roboto.uasset to JSON (base template)
template_json = os.path.join(WORK, '_roboto_template.json')
if not os.path.exists(template_json):
    print('Dumping Roboto.uasset -> JSON template...')
    subprocess.run([UAGUI, 'tojson', ROBOTO_SRC, template_json, 'VER_UE5_3'], check=False)
    if not os.path.exists(template_json):
        raise SystemExit('Template JSON not produced - UAssetGUI may have UI mode issue')

with open(template_json, 'r', encoding='utf-8') as f:
    template = json.load(f)

OLD_NAME = 'Roboto'
OLD_PATH = '/Engine/EngineFonts/Roboto'

for new_name, new_path in TARGETS:
    print(f'\n=== Generating override for {new_name} ===')
    # Deep-copy the template via JSON re-parse
    asset = json.loads(json.dumps(template))

    # Patch NameMap : Roboto -> new_name, /Engine/EngineFonts/Roboto -> new_path
    new_namemap = []
    for entry in asset['NameMap']:
        if entry == OLD_NAME:
            new_namemap.append(new_name)
        elif entry == OLD_PATH:
            new_namemap.append(new_path)
        else:
            new_namemap.append(entry)
    asset['NameMap'] = new_namemap

    # Patch Exports[0].ObjectName : Roboto -> new_name
    for exp in asset.get('Exports', []):
        if exp.get('ObjectName') == OLD_NAME:
            exp['ObjectName'] = new_name

    # Patch FolderName
    if asset.get('FolderName') == OLD_PATH:
        asset['FolderName'] = new_path

    # Write JSON
    rel_dir = os.path.dirname(new_path).lstrip('/').replace('Game/', 'ABumpyRide/Content/')
    out_dir = os.path.join(WORK, rel_dir)
    os.makedirs(out_dir, exist_ok=True)
    out_json = os.path.join(out_dir, f'{new_name}.json')
    out_uasset = os.path.join(out_dir, f'{new_name}.uasset')
    with open(out_json, 'w', encoding='utf-8') as f:
        json.dump(asset, f, indent=2, ensure_ascii=False)

    # Convert JSON -> uasset
    result = subprocess.run([UAGUI, 'fromjson', out_json, out_uasset],
                            capture_output=True, text=True, encoding='utf-8', errors='replace')
    if result.returncode != 0:
        print(f'  [WARN] fromjson exit {result.returncode}')
        print(f'    {(result.stdout or "")[-500:]}')
        print(f'    {(result.stderr or "")[-500:]}')
    if os.path.exists(out_uasset):
        out_uexp = out_uasset.replace('.uasset', '.uexp')
        if os.path.exists(out_uexp):
            print(f'  OK: {out_uasset} ({os.path.getsize(out_uasset)} bytes) + .uexp ({os.path.getsize(out_uexp)} bytes)')
        else:
            print(f'  WARN: .uasset created but no .uexp')
    else:
        print(f'  FAIL: {out_uasset} not created')

print('\n=== DONE ===')
print(f'Overrides ready in {WORK}/ABumpyRide/Content/Chooch/Art/UI/Fonts/')
print('Next step: copy these to patch-jp/patched_assets/ and re-bundle')
