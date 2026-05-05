# Sources du mod — A Bumpy Ride traductions

Ce dossier `src/` contient tout ce qui a servi à fabriquer les mods de traduction : les installeurs PowerShell pour Windows (alternative au drop-in pré-patché), les JSONs sources de traduction, les sources C# de l'outil custom MainMapPatcher, et le document maître des règles de traduction.

L'utilisateur final n'a normalement pas à toucher à ce dossier — il télécharge directement le zip pré-patché de la langue qu'il veut. Ce répertoire existe pour :
- transparence sur ce qui a été modifié dans le jeu
- permettre à un contributeur de corriger une trad ou d'ajouter une nouvelle langue
- archiver les sources avant que les forks externes ne disparaissent

---

## Structure

```
src/
├── README.md                  ← ce fichier
├── TRANSLATION_RULES.md       ← règles de traduction universelles + per-langue
├── tools_src/                 ← sources des outils custom (langue-agnostique)
│   └── mainmap_patcher/       ← Program.cs + .csproj
└── languages/                 ← un sous-dossier par langue cible
    └── fr/                    ← traduction française (1.3.0, prête)
        ├── installer/         ← pipeline Windows complet pour FR
        │   ├── install.ps1
        │   ├── uninstall.ps1
        │   ├── manifest.json
        │   ├── retoc.exe
        │   ├── oo2core_9_win64.dll
        │   ├── MainMapPatcher.exe
        │   ├── ABumpyRide.usmap
        │   └── patched_assets/   ← 78 .uasset/.umap déjà patchés
        └── translations/       ← JSONs sources qui ont produit les patched_assets
            ├── fr_strings_BP_translated.json
            ├── fr_strings_maps_translated.json
            ├── enum_*_fr.json
            └── skinbuttontable_fr.json
```

> Les binaires de `installer/` (retoc.exe, MainMapPatcher.exe, oo2core, ABumpyRide.usmap) sont identiques pour toutes les langues. Pour le moment ils sont dupliqués par langue — quand on aura plus de 2 langues stables, on factorisera dans un `shared/` au niveau de `src/`.

---

## Comment patcher via PowerShell (cas FR)

L'installeur dans `languages/fr/installer/` est exactement le contenu du zip Windows historique (`ABR-fr_v1.3.0.zip`). Il fonctionne en deux modes de détection :

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
cd src/languages/fr/installer
.\install.ps1
```

> Pré-requis : PowerShell 5.1+ (inclus dans Windows 10/11), .NET 8 runtime (inclus dans `MainMapPatcher.exe` self-contained — pas d'install séparée).

---

## Ajouter une nouvelle langue

Pour ajouter une langue (par exemple ES, DE, IT) :

1. Crée `src/languages/<code>/` avec la même structure que `fr/`
2. Repars de `src/languages/fr/translations/` comme template :
   - copie les JSONs, vide le champ `NewValue` (ou `translation`) de chaque entrée
   - traduis selon les règles de `src/TRANSLATION_RULES.md` (Tier 1 universel + Tier 2 spécifique à ta langue)
3. Génère les `patched_assets/` :
   - **BP/maps** : KissE avec les JSONs `*_translated.json` ou `*_fr.json` adaptés (cf. section ci-dessous)
   - **Enums** : outil `datatable_text_patcher --inject-enum` (sources sur le repo principal de l'auteur, pas bundlé ici)
   - **DataTable TextProperty** (SkinButtonTable) : `datatable_text_patcher --inject-textproperty`
4. Adapte `installer/install.ps1` :
   - change le nom du backup directory (par exemple `_ABRes_backup`)
   - change le nom du temp directory
   - traduis les messages PowerShell dans la langue cible
5. Mets à jour `installer/manifest.json` (mod_version, langue, counts d'assets)

---

## Comment relancer le patch des assets BP/maps (KissE)

Les 78 fichiers de `languages/fr/installer/patched_assets/` sont produits avec [KissE](https://github.com/SolicenTEAM/KismetEditor) (fork patché par l'auteur, voir crédits du README principal) à partir des JSONs de `languages/fr/translations/`.

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

> Note : pour traduire les chaînes hardcodées dans MainMap.uexp dans une autre langue, il faut adapter `Program.cs` — les constantes `ORIGINAL_INTRO/FRENCH_INTRO` et `ORIGINAL_STAFF/FRENCH_STAFF` sont les sources et leur traduction. Refacto suggéré quand on ajoutera ES : extraire ces constantes dans un fichier de config (JSON par langue) lu au démarrage, plutôt que d'avoir un binaire MainMapPatcher par langue.

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
