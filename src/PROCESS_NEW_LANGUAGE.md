# Processus complet pour traduire ABR dans une nouvelle langue

Document de référence opérationnel. **Lire entièrement avant d'attaquer une nouvelle langue.**
Suit la convention `<lang>` = code court (jp, ko, zh, it, ru, etc.) et `<LANG>` = code en majuscules.

Hypothèses : translations FR/DE/ES existantes servent de référence. Pipeline UE5.3 ABR.

---

## Phase 0 - Décisions à prendre avant de coder

1. **Registre game-to-player** (cf `TRANSLATION_RULES.md` section 2.7) :
   - tutoiement (FR/ES) / `du` (DE) / `ですます調` (JP) - selon convention casual gaming de la langue
2. **Variant régional** : standard ? variante (LATAM, brésilien, traditionnel/simplifié...) ?
3. **Script CJK / non-Latin** ? Si oui, **anticiper l'override de police** (Phase 4).

---

## Phase 1 - Initialisation de la structure (~5 min)

### 1.1. Cloner les scripts d'init depuis une langue existante

```powershell
cp staging/_init_jp_structure.py staging/_init_<lang>_structure.py
cp staging/_init_patch_jp.py staging/_init_patch_<lang>.py
```

Éditer les nouveaux fichiers et remplacer `jp` / `JP` / `Japonais` / `ですます調` par les valeurs cibles.

### 1.2. Lancer l'init

```powershell
python staging/_init_<lang>_structure.py
python staging/_init_patch_<lang>.py
```

Produit :
- `translations/<lang>/` avec 10 JSONs (enums + skinbuttontable + strings_BP + strings_maps)
- `staging/legacy_patched_<LANG>/` (vide)
- `patch-<lang>/` avec installeur PowerShell, manifest, README

### 1.3. **OBLIGATOIRE** : injecter les 10 strings absentes de l'extract initial

```powershell
python staging/_inject_extra_strings.py <lang>
```

Sinon : `Click to lock`, `Watch out for tornadoes!`, `OWNED`, `PAINT`, `Money made today: `, etc. resteront silencieusement en EN dans le build final. **Ce script est idempotent**, on peut le relancer sans risque. `_inject_extra_strings.py --audit` vérifie l'état pour toutes les langues.

Voir `memory/reference_extra_strings_not_in_extract.md` pour la liste canonique et le pourquoi.

### 1.4. **OBLIGATOIRE** : retirer les 8 strings NameMap UMG interdites

Ces strings sont des identifiants UMG (`Float`, `Pulsate`, `Lock`, `Quest 1/2/3`, `Unlocked Item`, `Unlocked Text`) qui crashent au pickup Actionnaire si traduites. Cloner `_clean_jp_forbidden_strings.py` (qui retire les bonnes entrées et les 3 fichiers entiers `W_WonStocks`, `NPCPointer`, `PopUp` du JSON) :

```powershell
cp staging/_clean_jp_forbidden_strings.py staging/_clean_<lang>_forbidden_strings.py
# (éditer juste le chemin JP_BP -> <lang>_BP)
python staging/_clean_<lang>_forbidden_strings.py
```

Voir `TRANSLATION_RULES.md` section 2.5.bis pour la liste complète.

---

## Phase 2 - Traduction (~10-30h selon volume)

### 2.1. Volume

| JSON | Entries | Notes |
|---|---|---|
| `enum_buildingtype.json` | 10 | tous traduits |
| `enum_freighttype.json` | 13 | tous traduits |
| `enum_questtype.json` | 6 | tous traduits |
| `enum_passengerenum.json` | 50 | tous traduits |
| `enum_titleblurbsrainy.json` | 16 | tous traduits |
| `enum_questline.json` | 11 | tous traduits |
| `enum_titleblurbs.json` | 245 | tous traduits (catch phrases) |
| `skinbuttontable.json` | 90 | **seules les Description** (30 entrées) - Name et Author restent EN (noms propres) |
| `strings_maps.json` | 105 | **NewTutorialLevel tutorial + ELEVATION uniquement** - enseignes western restent EN |
| `strings_BP.json` | ~700 | ~525 à traduire (le reste = noms propres EN, internes techniques) |

### 2.2. Règles de traduction (résumé)

Lire `TRANSLATION_RULES.md` en entier. Points clés :

- **Jamais traduits** : noms propres (skins, stations, régions, auteurs), enseignes western des `*_SubLvl.umap`, `On`/`Off`, unités impériales (`FT`, `Miles`), tags HTML/RichText, asset paths, console commands, debug logs, placeholders UMG, `AM`/`PM`.
- **Toujours traduits** : dialogues, narration, didactiques, descriptions, blurbs, libellés UI, statistiques, boutons.
- **Encoding UTF-16 LE** obligatoire pour caractères > 127 (KissE le fait auto).
- **Glossaire FR/DE/JP** dans `TRANSLATION_RULES.md` sections 8-9 - étendre pour nouvelles langues.

### 2.3. Stratégie pratique : script d'application massive

Approche éprouvée (cf `_apply_jp_strings_BP.py`) :
1. Lire `translations/de/strings_BP.json` pour avoir tous les `Original` à traduire et un exemple de NewValue
2. Écrire un script Python avec un gros `TRAD = { "EN": "<lang>", ... }` dictionnaire
3. Le script parcourt le JSON `<lang>` et applique le mapping
4. Pour les enums et skinbuttontable : pareil mais avec `source/translation` au lieu de `Original/NewValue`

---

## Phase 3 - Pipeline pack (KissE + DTP + BPOffsetPatcher) (~5 min)

### 3.1. Cloner `_package_jp.py`

```powershell
cp staging/_package_jp.py staging/_package_<lang>.py
```

Remplacer toutes les occurrences `jp` / `JP` par `<lang>` / `<LANG>`. Le script fait :
1. Copie vanilla → workdir
2. KissE Replacement BP (lit `translations/<lang>/strings_BP.json`)
3. KissE Replacement maps (rename trick .umap → .uasset)
4. DTP `--inject-enum` × 7 enums
5. DTP `--inject-enum` sur `Tutorial_Table.uasset` (DataTable DialogueStructure)
6. DTP `--inject` sur `SkinButtonTable.uasset`
7. Consolidation dans `staging/legacy_patched_<LANG>/`
8. **`post_consolidation_<lang>_fixes()`** : applique BPOffsetPatcher (SP + QuestTicket) + font overrides si CJK

### 3.2. BPOffsetPatcher

L'outil `tools/bp_offset_patcher/` patche SpecialPassenger ET QuestTicket avec edit-in-place + shift map (évite le crash récursion Actionnaire dû à KissE qui casse les EX_Jump par change-of-length).

**Wrapper unifié** : `staging/_apply_bp_offset.py <lang>` (généralisé depuis le wrapper JP).

**RÈGLE CRITIQUE** : le wrapper ne dédoublonne **JAMAIS** les `Original` identiques dans le JSON d'entrée. BPOffsetPatcher patche les occurrences bytecode dans l'ordre des entries ; si ` law signs` apparaît 2 fois en bytecode (Obey + Disobey), il faut 2 entries dans le JSON. Sinon la 2e occurrence reste en EN (régression connue JP v1.0.4 → v1.0.5, DE/ES v1.4.6 → v1.4.7).

Voir `memory/reference_bp_offset_patcher.md` pour les détails de l'outil.

### 3.3. Cas CJK uniquement : font overrides

Si la langue utilise des caractères hors Latin Extended-A (japonais, chinois, coréen, arabe, hébreu, thaï...), les polices `UFont` bitmap pré-rendues du jeu (`Pixel_Times_Font`, `AwfullyDigital_Font`, `Cavalhatriz_Font`, `Pixel_Times_Bold_Font`) ne contiennent **pas** les glyphes nécessaires → tofu en jeu.

**Fix** (cf `_make_jp_font_overrides.py`) : cloner `Engine/EngineFonts/Roboto.uasset` (composite font qui chaîne Roboto Latin + DroidSansFallback CJK, déjà embarquée par UE) et le renommer vers chacune des 4 polices ABR. L'override pak charge nos versions et le moteur fait le fallback CJK automatiquement.

```powershell
python staging/_make_<lang>_font_overrides.py   # cloner depuis _make_jp_font_overrides.py
```

Trade-off : le look pixel-art / digital est perdu pour les chars Latin (remplacé par Roboto). Acceptable pour livrer une version lisible. Voir `memory/project_abr_jp_setup.md` pour le détail.

### 3.4. Lancer le pipeline

```powershell
python staging/_package_<lang>.py
```

Logs typiques attendus :
- KissE BP : ~130 assets traités
- KissE maps : ~5 .umap
- DTP : 7 enums + Tutorial_Table + SkinButtonTable patchés
- BPOffsetPatcher SP : `Strings: 64 replacements, 0 skipped` (62-70 selon langue)
- BPOffsetPatcher QuestTicket : `Strings: 3 replacements, 0 skipped`

---

## Phase 4 - Bundle release (instantané)

### 4.1. Cloner `_bundle_jp.py`

```powershell
cp staging/_bundle_jp.py staging/_bundle_<lang>.py
```

Remplacer `jp`/`JP` par `<lang>`/`<LANG>` + bumper la version (commencer à `1.0.0`).

### 4.2. Lancer

```powershell
python staging/_bundle_<lang>.py
```

Copie `staging/legacy_patched_<LANG>/` vers `patch-<lang>/patched_assets/` et met à jour `patch-<lang>/manifest.json`.

Stats attendues : ~75 .uasset + ~75 .uexp + ~3 .umap = ~150-155 fichiers, ~76-90 MB.

---

## Phase 5 - Tests in-game (~30 min)

1. Drop `patch-<lang>/` dans le dossier du jeu Steam
2. Clic droit `install.ps1` → "Exécuter avec PowerShell" (~3-5 min, ~12 GB temp)
3. Lancer le jeu

**Checklist de validation** (ordre de criticité) :

| Test | Si KO |
|---|---|
| Le jeu lance, le menu principal s'affiche | Asset cassé bloquant : check log retoc/pack-raw |
| Les textes du tutoriel s'affichent dans la langue | KissE n'a pas patché : check `_package_<lang>.log` |
| Pour CJK : les glyphes s'affichent (pas de tofu) | Font overrides manquants ou non chargés |
| Pickup Actionnaire ne crashe pas (jouer 5+ tâches) | BPOffsetPatcher n'a pas été appliqué sur SP ou QuestTicket |
| `OWNED`/`PAINT` du shop traduits | Phase 1.3 non faite, ou KissE a échoué |
| `Click to lock`/`unlock` sur QuestBoard | Idem |
| `Watch out for tornadoes!` etc. (TrainScreen) | Idem |
| Tâche Actionnaire `Obey 3 law signs` ou équivalent : **` law signs` traduit** | Wrapper BPOffsetPatcher a dédoublonné les entries (régression critique) |

### 5.1. Audits binaires (sanity check)

```powershell
# Count residual EN strings in SP binary
python -c "import os; d=open('staging/legacy_patched_<LANG>/ABumpyRide/Content/Chooch/BP/Actors/Passenger/SpecialPassenger.uexp','rb').read(); print(f'\"law signs\" EN: {d.count(b\" law signs\")}')"
```

Doit retourner `0`. Sinon, le wrapper BPOffsetPatcher a dédoublonné les entries (= bug pipeline).

```powershell
# Audit completeness des 10 extra strings
python staging/_inject_extra_strings.py --audit
```

Doit lister `[OK] <lang>: 10/10 present, 10/10 translated`.

---

## Récapitulatif rapide (cheat sheet)

```powershell
# Phase 1 (~5 min)
python staging/_init_<lang>_structure.py
python staging/_init_patch_<lang>.py
python staging/_inject_extra_strings.py <lang>
python staging/_clean_<lang>_forbidden_strings.py

# Phase 2 (traduire)
# - éditer translations/<lang>/*.json à la main / script
# - si CJK : python staging/_make_<lang>_font_overrides.py

# Phase 3 (~5 min)
python staging/_package_<lang>.py

# Phase 4 (instantané)
python staging/_bundle_<lang>.py

# Phase 5 (test in-game)
# - drop patch-<lang>/ dans le jeu + install.ps1
```

---

## Pièges historiques (à ne PAS refaire)

| Symptôme in-game | Cause | Fix |
|---|---|---|
| Tofu/carrés vides sur tous les textes JP/CN/KR | UFont bitmap atlas sans glyphes CJK | Phase 3.3 - font overrides composite Roboto+DroidSansFallback |
| Crash au pickup Actionnaire | SP ou QuestTicket patché par KissE seul (EX_Jump cassé) | BPOffsetPatcher (Phase 3.2) |
| Crash au pickup Actionnaire malgré BPOffsetPatcher | 8 strings UMG NameMap (Float, Pulsate, Lock, Quest 1/2/3, Unlocked Item/Text) traduites | Phase 1.4 - cleaner |
| `Click to lock`, `Watch out for tornadoes!`, `OWNED`, `PAINT` etc. en EN | 10 strings absentes du JSON source initial | Phase 1.3 - `_inject_extra_strings.py` |
| `Obey 3 law signs` (2e occurrence en EN) | Wrapper BPOffsetPatcher a dédoublonné le JSON | Garder les doublons (`_apply_bp_offset.py` actuel) |
| Crash au load MainMap | BPOffsetPatcher v1/v2 ancien (caller EX_IntConst non shiftés) | Utiliser v3+ de l'outil (commit dfb806a+) |
| Crash au click shop staff (Bartender etc.) | KissE offset non recalculé par Visit-based | KissE fork Shayano dfa30cf (déjà inclus dans `tools/KismetEditor/`) |
| `Money made today: ` en EN | String absente du JSON | Phase 1.3 |

---

## Mémoires complémentaires à lire

- `memory/MEMORY.md` - index complet
- `memory/reference_translation_rules.md` - règles éditoriales détaillées
- `memory/reference_release_pipeline.md` - pipeline détaillé
- `memory/reference_bp_offset_patcher.md` - outil critique
- `memory/reference_extra_strings_not_in_extract.md` - les 10 strings + pourquoi
- `memory/reference_caller_intconst_pattern.md` - pattern Ubergraph + callers
- `memory/feedback_crossLang_diff_first.md` - méthode d'investigation des crashs
- `memory/feedback_new_lang_init_completeness.md` - règle Phase 1.3
- `memory/feedback_namemap_identifier_crash.md` - règle Phase 1.4
- `memory/feedback_specialpassenger_intraduisible.md` - contexte historique SP

---

## Version de ce document

- 2026-05-17 : version initiale après livraison JP v1.0.5 et fix rétro DE/ES v1.4.7 (bug dédup BPOffsetPatcher)
