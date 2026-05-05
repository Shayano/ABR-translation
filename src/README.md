# Sources du mod — A Bumpy Ride traduction française

Ce dossier `src/` contient tout ce qui a servi à fabriquer le mod : l'installeur PowerShell pour Windows (alternative au drop-in pré-patché), les JSONs sources de traduction, les sources C# de l'outil custom MainMapPatcher, et le document maître des règles de traduction.

L'utilisateur final n'a normalement pas à toucher à ce dossier — il télécharge directement le zip pré-patché. Ce répertoire existe pour :
- transparence sur ce qui a été modifié dans le jeu
- permettre à un contributeur de relancer le pipeline (corriger une trad, ajouter une langue)
- archiver les sources avant que les forks externes ne disparaissent

---

## Structure

```
src/
├── README.md                  ← ce fichier
├── TRANSLATION_RULES.md       ← règles de traduction (ce qu'on traduit, ce qu'on garde en VO)
├── installer/                 ← pipeline Windows (PowerShell)
│   ├── install.ps1            ← installeur (détecte Steam, backup, patch, install)
│   ├── uninstall.ps1          ← restore depuis le backup automatique
│   ├── manifest.json          ← métadonnées (version, hashes vanilla attendus)
│   ├── README.md              ← README utilisateur Windows
│   ├── retoc.exe              ← repackager IoStore UE5.3 (fork natimerry/repak-rivals)
│   ├── oo2core_9_win64.dll    ← Oodle decoder (lecture du .ucas vanilla)
│   ├── MainMapPatcher.exe     ← outil custom (cf. tools_src/mainmap_patcher/)
│   ├── ABumpyRide.usmap       ← mappings UAssetAPI (généré via Dumper-7)
│   └── patched_assets/        ← 78 .uasset/.umap déjà patchés (BPs, maps, enums, datatable)
├── translations/              ← JSONs sources qui ont produit les patched_assets
│   ├── fr_strings_BP_translated.json     ← format KissE (Original/NewValue par fichier)
│   ├── fr_strings_maps_translated.json   ← idem pour les .umap
│   ├── enum_*_fr.json                     ← enums (TitleScreenBlurbs, BuildingType, …)
│   └── skinbuttontable_fr.json           ← descriptions de skins (DataTable TextProperty)
└── tools_src/                 ← sources C# des outils custom à nous
    └── mainmap_patcher/
        ├── Program.cs         ← patche MainMap.uexp (>2 Go, hors KissE)
        └── MainMapPatcher.csproj
```

---

## Comment patcher via PowerShell

L'installeur dans `installer/` est exactement le contenu du zip Windows historique (`ABR-fr_v1.3.0.zip`). Il fonctionne en deux modes de détection :

- **Drop-in** : exécuté depuis un dossier dans la hiérarchie du jeu (rare en pratique)
- **Auto Steam** : lit `HKCU:\Software\Valve\Steam\SteamPath` et parse `libraryfolders.vdf` pour trouver A Bumpy Ride dans toutes les bibliothèques Steam

Pipeline complet (~3-5 min, ~12 Go d'espace temporaire requis) :

1. Backup `ABumpyRide-Windows.{utoc,ucas,pak}` vers `Paks/_ABRfr_backup/`
2. `retoc.exe to-legacy <Paks/> <legacy_dir/> --filter "BP"` puis `--filter ".umap"` — extrait les .uasset/.uexp/.umap depuis le `.ucas` Oodle vanilla
3. Overlay `installer/patched_assets/*` par-dessus → 78 fichiers remplacés par leurs versions FR
4. **Étape 5/3b** : `MainMapPatcher.exe` patche `MainMap.uexp` (2,3 Go, hors capacités KissE) en deux passes — `--target=intro` puis `--target=staff`
5. `retoc.exe to-zen <legacy_dir/> <fr.utoc>` — repackage en format Zen IoStore
6. Re-injection dans le container vanilla via `unpack-raw` + filter chunks + `pack-raw`
7. Copy du `.utoc/.ucas` final dans `Paks/`

Lancement :
```powershell
cd src/installer
.\install.ps1
```

> Pré-requis : PowerShell 5.1+ (inclus dans Windows 10/11), .NET 8 runtime (inclus dans `MainMapPatcher.exe` self-contained — pas d'install séparée).

---

## Comment relancer le patch des assets BP/maps (KissE)

Les 78 fichiers de `installer/patched_assets/` sont produits avec [KissE](https://github.com/SolicenTEAM/KismetEditor) (fork patché par l'auteur, voir [crédits du README principal](../README.md#crédits--remerciements)) à partir des JSONs de `translations/`.

Pipeline KissE typique pour un seul asset (par exemple si on veut corriger une trad) :

```powershell
# Le .usmap est requis sinon UAssetAPI charge tout en RawExport et KissE no-op
Set-Location <chemin>\KismetEditor

.\KissE.exe `
    F:\path\to\fr_strings_BP_translated.json `
    F:\path\to\Asset.uasset `
    --version=5.3 `
    --patch-assignments `
    --patch-all-functions `
    --map=ABumpyRide.usmap
```

Pour les enums et SkinButtonTable (DataTable avec TextProperty), on n'utilise pas KissE mais l'outil `datatable_text_patcher` (séparé, sources sur le repo principal — pas bundlé ici).

---

## Comment rebuild MainMapPatcher

```powershell
cd tools_src/mainmap_patcher
dotnet publish -c Release -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true
```

Produit `bin/Release/net8.0/win-x64/publish/MainMapPatcher.exe` (~38 Mo, .NET 8 runtime inclus).

Dépendance : [UAssetAPI 1.1.0](https://github.com/atenfyr/UAssetAPI). Pour rebuild proprement, soit :
- pointer le `.csproj` sur le NuGet UAssetAPI 1.1.0 publié
- soit cloner le fork [Shayano/UAssetAPI](https://github.com/Shayano/UAssetAPI) (avec le fix encoding UTF-8/16 pour les accents) à côté et ajuster le `<ProjectReference>`

---

## Forks externes utilisés (non bundlés)

Pour transparence, voici les outils tiers utilisés, avec lien vers les forks Shayano qui contiennent les patches spécifiques au pipeline :

| Outil | Repo upstream | Fork avec patches |
|---|---|---|
| KissE / KismetEditor | [SolicenTEAM/KismetEditor](https://github.com/SolicenTEAM/KismetEditor) | [Shayano/KismetEditor](https://github.com/Shayano/KismetEditor) (UAssetAPI 1.1.0 + spinner + `--patch-assignments` + `--patch-all-functions`) |
| UAssetAPI | [atenfyr/UAssetAPI](https://github.com/atenfyr/UAssetAPI) | [Shayano/UAssetAPI](https://github.com/Shayano/UAssetAPI) (fix encoding UTF-16 pour accents) |
| retoc-rivals | [natimerry/repak-rivals](https://github.com/natimerry/repak-rivals) | utilisé tel quel (UE5.3 supporté nativement) |
| Dumper-7 | [Encryqed/Dumper-7](https://github.com/Encryqed/Dumper-7) | utilisé tel quel — produit `installer/ABumpyRide.usmap` |
