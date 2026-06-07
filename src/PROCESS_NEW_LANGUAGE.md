# Complete process to translate ABR into a new language

Operational reference document. **Read entirely before tackling a new language.**
Follows the convention `<lang>` = short code (jp, ko, zh, it, ru, etc.) and `<LANG>` = uppercase code.

Assumptions: existing FR/DE/ES translations serve as reference. ABR UE5.3 pipeline.

---

## Phase 0 - Decisions to make before coding

1. **Game-to-player register** (cf `TRANSLATION_RULES.md` section 2.7):
   - informal (FR/ES) / `du` (DE) / `ですます調` (JP) - depending on the casual gaming
     convention of the language
2. **Regional variant**: standard? variant (LATAM, Brazilian, traditional/simplified...)?
3. **CJK / non-Latin script**? If yes, **anticipate font overrides** (Phase 4).

---

## Phase 1 - Structure initialization (~5 min)

### 1.1. Clone the init scripts from an existing language

```powershell
cp staging/_init_jp_structure.py staging/_init_<lang>_structure.py
cp staging/_init_patch_jp.py staging/_init_patch_<lang>.py
```

Edit the new files and replace `jp` / `JP` / `Japonais` / `ですます調` with the
target values.

### 1.2. Run the init

```powershell
python staging/_init_<lang>_structure.py
python staging/_init_patch_<lang>.py
```

Produces:
- `translations/<lang>/` with 10 JSONs (enums + skinbuttontable + strings_BP + strings_maps)
- `staging/legacy_patched_<LANG>/` (empty)
- `patch-<lang>/` with PowerShell installer, manifest, README

### 1.3. **MANDATORY**: inject the 10 strings missing from the initial extract

```powershell
python staging/_inject_extra_strings.py <lang>
```

Otherwise: `Click to lock`, `Watch out for tornadoes!`, `OWNED`, `PAINT`,
`Money made today: `, etc. will silently stay in EN in the final build. **This
script is idempotent**, you can re-run it without risk. `_inject_extra_strings.py
--audit` checks the state for every language.

See `memory/reference_extra_strings_not_in_extract.md` for the canonical list and
the rationale.

### 1.4. **MANDATORY**: remove the 8 forbidden UMG NameMap strings

These strings are UMG identifiers (`Float`, `Pulsate`, `Lock`, `Quest 1/2/3`,
`Unlocked Item`, `Unlocked Text`) that crash on Shareholder pickup if translated.
Clone `_clean_jp_forbidden_strings.py` (which removes the right entries and the
3 entire files `W_WonStocks`, `NPCPointer`, `PopUp` from the JSON):

```powershell
cp staging/_clean_jp_forbidden_strings.py staging/_clean_<lang>_forbidden_strings.py
# (just edit the path JP_BP -> <lang>_BP)
python staging/_clean_<lang>_forbidden_strings.py
```

See `TRANSLATION_RULES.md` section 2.5.bis for the complete list.

---

## Phase 2 - Translation (~10-30h depending on volume)

### 2.1. Volume

| JSON | Entries | Notes |
|---|---|---|
| `enum_buildingtype.json` | 10 | all translated |
| `enum_freighttype.json` | 13 | all translated |
| `enum_questtype.json` | 6 | all translated |
| `enum_passengerenum.json` | 50 | all translated |
| `enum_titleblurbsrainy.json` | 16 | all translated |
| `enum_questline.json` | 11 | all translated |
| `enum_titleblurbs.json` | 245 | all translated (catch phrases) |
| `skinbuttontable.json` | 90 | **only the Descriptions** (30 entries) - Name and Author stay EN (proper nouns) |
| `strings_maps.json` | 105 | **NewTutorialLevel tutorial + ELEVATION only** - western signs stay EN |
| `strings_BP.json` | ~700 | ~525 to translate (the rest = proper nouns EN, technical internals) |

### 2.2. Translation rules (summary)

Read `TRANSLATION_RULES.md` fully. Key points:

- **Never translated**: proper nouns (skins, stations, regions, authors), western
  signs in `*_SubLvl.umap`, `On`/`Off`, imperial units (`FT`, `Miles`), HTML/RichText
  tags, asset paths, console commands, debug logs, UMG placeholders, `AM`/`PM`.
- **Always translated**: dialogues, narration, didactics, descriptions, blurbs,
  UI labels, statistics, buttons.
- **UTF-16 LE encoding** mandatory for characters > 127 (KissE does it
  automatically).
- **FR/DE/JP glossary** in `TRANSLATION_RULES.md` sections 8-9 - extend for new
  languages.

### 2.3. Practical strategy: mass apply script

Proven approach (cf `_apply_jp_strings_BP.py`):
1. Read `translations/de/strings_BP.json` to get all `Original` entries to
   translate and an example NewValue
2. Write a Python script with a big `TRAD = { "EN": "<lang>", ... }` dictionary
3. The script walks the `<lang>` JSON and applies the mapping
4. For enums and skinbuttontable: same but with `source/translation` instead of
   `Original/NewValue`

---

## Phase 3 - Pack pipeline (KissE + DTP + BPOffsetPatcher) (~5 min)

### 3.1. Clone `_package_jp.py`

```powershell
cp staging/_package_jp.py staging/_package_<lang>.py
```

Replace every `jp` / `JP` with `<lang>` / `<LANG>`. The script does:
1. Copy vanilla -> workdir
2. KissE BP Replacement (reads `translations/<lang>/strings_BP.json`)
3. KissE map Replacement (rename trick .umap -> .uasset)
4. DTP `--inject-enum` × 7 enums
5. DTP `--inject-enum` on `Tutorial_Table.uasset` (DialogueStructure DataTable)
6. DTP `--inject` on `SkinButtonTable.uasset`
7. Consolidation in `staging/legacy_patched_<LANG>/`
8. **`post_consolidation_<lang>_fixes()`**: applies BPOffsetPatcher (SP +
   QuestTicket) + font overrides if CJK

> **Note (post v1.4.8)**: `_package_de.py` and `_package_es.py` no longer embed
> the BPOffsetPatcher step (they only EXCLUDE SP+QT from KissE so they ship
> vanilla). The maintainer then runs `python staging/_patch_bpop.py <lang>` as a
> separate step. If you clone `_package_jp.py` (which still does the BPOP step
> inline), it should still work - but consider following the DE/ES pattern for
> easier maintenance.

### 3.2. BPOffsetPatcher

The tool `tools/bp_offset_patcher/` patches SpecialPassenger AND QuestTicket
with edit-in-place + shift map (avoids the Shareholder pickup recursion crash
caused by KissE breaking the EX_Jump via change-of-length).

**Unified wrapper (post v1.4.8)**: `staging/_patch_bpop.py <lang>` (generalized
from the JP wrapper, handles SP + QT for any language).

**CRITICAL RULE**: the wrapper NEVER deduplicates identical `Original` entries
in the input JSON. BPOffsetPatcher patches bytecode occurrences in entry order;
if ` law signs` appears twice in the bytecode (Obey + Disobey), you need 2
entries in the JSON. Otherwise the 2nd occurrence stays in EN (known regression
JP v1.0.4 -> v1.0.5, DE/ES v1.4.6 -> v1.4.7).

See `MAINTAINER.md` Pitfall #1 for full details on BPOffsetPatcher and the
v1.4.8 regression where +23 bytes of cumulative shift can corrupt SP/QT, with
the safe fallback recipe (restore SP+QT from a previous working release).

### 3.3. CJK only: font overrides

If the language uses characters outside Latin Extended-A (Japanese, Chinese,
Korean, Arabic, Hebrew, Thai...), the game's pre-rendered `UFont` bitmap atlases
(`Pixel_Times_Font`, `AwfullyDigital_Font`, `Cavalhatriz_Font`,
`Pixel_Times_Bold_Font`) do **not** contain the required glyphs -> tofu in-game.

**Fix** (cf `_make_jp_font_overrides.py`): clone `Engine/EngineFonts/Roboto.uasset`
(a composite font chaining Roboto Latin + DroidSansFallback CJK, already shipped
by UE) and rename it to each of ABR's 4 fonts. The override pak loads our
versions and the engine handles the CJK fallback automatically.

```powershell
python staging/_make_<lang>_font_overrides.py   # clone from _make_jp_font_overrides.py
```

Trade-off: the pixel-art / digital look is lost for Latin chars (replaced by
Roboto). Acceptable to ship a readable version. See `memory/project_abr_jp_setup.md`
for details.

### 3.4. Run the pipeline

```powershell
python staging/_package_<lang>.py
```

Expected typical logs:
- KissE BP: ~130 assets processed
- KissE maps: ~5 .umap
- DTP: 7 enums + Tutorial_Table + SkinButtonTable patched
- BPOffsetPatcher SP: `Strings: 64 replacements, 0 skipped` (62-70 depending on language)
- BPOffsetPatcher QuestTicket: `Strings: 3 replacements, 0 skipped`

If you cloned `_package_de.py` (BPOP NOT inline), also run:

```powershell
python staging/_patch_bpop.py <lang>
```

---

## Phase 4 - Bundle release (instant)

### 4.1. Clone the right bundle script

**Recommended (post v1.4.8)**: use the generic `staging/_rebundle.py` which
preserves your manifest values (doesn't hardcode versions):

```powershell
python staging/_rebundle.py <lang>
```

**Legacy**: the old `_bundle_jp.py` / `_bundle_de.py` / `_bundle_es.py` scripts
hardcode an old `mod_version` and overwrite your manifest. **Do NOT use them
for new releases**. They are kept for historical reference of how the initial
seed releases were done.

If you really need an inline bundle script for a new language:

```powershell
cp staging/_bundle_jp.py staging/_bundle_<lang>.py
```

Replace `jp`/`JP` with `<lang>`/`<LANG>` + bump the version (start at `1.0.0`).

### 4.2. What it produces

Copies `staging/legacy_patched_<LANG>/` to `patch-<lang>/patched_assets/` and
updates `patch-<lang>/manifest.json` (only the `bundle{}` section with
`_rebundle.py`, the whole thing with the legacy scripts).

Expected stats: ~75 .uasset + ~75 .uexp + ~3 .umap = ~150-155 files, ~76-90 MB.

---

## Phase 5 - In-game tests (~30 min)

1. Drop `patch-<lang>/` into the Steam game folder
2. Right-click `install.ps1` -> "Run with PowerShell" (~3-5 min, ~12 GB temp)
3. Launch the game

**Validation checklist** (in order of criticality):

| Test | If KO |
|---|---|
| Game launches, main menu shows | Blocking asset broken: check retoc/pack-raw log |
| Tutorial texts show in the language | KissE didn't patch: check `_package_<lang>.log` |
| For CJK: glyphs show (no tofu) | Missing or unloaded font overrides |
| Shareholder pickup doesn't crash (play 5+ tasks) | BPOffsetPatcher wasn't applied on SP or QuestTicket - run `_patch_bpop.py <lang>` |
| `OWNED`/`PAINT` in the shop translated | Phase 1.3 not done, or KissE failed |
| `Click to lock`/`unlock` on QuestBoard | Same |
| `Watch out for tornadoes!` etc. (TrainScreen) | Same |
| Shareholder task `Obey 3 law signs` or equivalent: **` law signs` translated** | BPOffsetPatcher wrapper dedup'd the entries (critical regression) |

### 5.1. Binary audits (sanity check)

```powershell
# Count residual EN strings in SP binary
python -c "import os; d=open('staging/legacy_patched_<LANG>/ABumpyRide/Content/Chooch/BP/Actors/Passenger/SpecialPassenger.uexp','rb').read(); print(f'\"law signs\" EN: {d.count(b\" law signs\")}')"
```

Must return `0`. Otherwise, the BPOffsetPatcher wrapper dedup'd the entries (=
pipeline bug).

```powershell
# Audit completeness of the 10 extra strings
python staging/_inject_extra_strings.py --audit
```

Must list `[OK] <lang>: 10/10 present, 10/10 translated`.

---

## Quick recap (cheat sheet)

```powershell
# Phase 1 (~5 min)
python staging/_init_<lang>_structure.py
python staging/_init_patch_<lang>.py
python staging/_inject_extra_strings.py <lang>
python staging/_clean_<lang>_forbidden_strings.py

# Phase 2 (translate)
# - edit translations/<lang>/*.json by hand / script
# - if CJK: python staging/_make_<lang>_font_overrides.py

# Phase 3 (~5 min)
python staging/_package_<lang>.py
python staging/_patch_bpop.py <lang>   # only if you cloned from _package_de/es.py (BPOP not inline)

# Phase 4 (instant)
python staging/_rebundle.py <lang>

# Phase 5 (in-game test)
# - drop patch-<lang>/ into the game + install.ps1
# - MANDATORY: test Shareholder pickup before releasing
```

---

## Historical pitfalls (do NOT redo)

| In-game symptom | Cause | Fix |
|---|---|---|
| Tofu/empty squares on all JP/CN/KR text | UFont bitmap atlas without CJK glyphs | Phase 3.3 - composite Roboto+DroidSansFallback font overrides |
| Crash on Shareholder pickup | SP or QuestTicket patched by KissE alone (EX_Jump broken) | BPOffsetPatcher (Phase 3.2) |
| Crash on Shareholder pickup despite BPOffsetPatcher | 8 UMG NameMap strings (Float, Pulsate, Lock, Quest 1/2/3, Unlocked Item/Text) translated | Phase 1.4 - cleaner |
| Crash on Shareholder pickup despite BPOffsetPatcher + clean cleaner | **v1.4.8 BPOP regression** (cumulative delta > ~20 bytes on SP) | Fallback: `python staging/_restore_bpop_from_release.py <lang> <previous_tag>` (see MAINTAINER.md Pitfall #1) |
| `Click to lock`, `Watch out for tornadoes!`, `OWNED`, `PAINT` etc. in EN | 10 strings missing from initial source JSON | Phase 1.3 - `_inject_extra_strings.py` |
| `Obey 3 law signs` (2nd occurrence in EN) | BPOffsetPatcher wrapper dedup'd the JSON | Keep duplicates (`_patch_bpop.py` does this correctly) |
| Crash on MainMap load | Old BPOffsetPatcher v1/v2 (caller EX_IntConst not shifted) | Use v3+ of the tool (commit dfb806a+) |
| Crash on shop staff click (Bartender etc.) | KissE Visit-based offset not recomputed | KissE Shayano fork dfa30cf (already included in `tools/KismetEditor/`) |
| `Money made today: ` in EN | String missing from JSON | Phase 1.3 |

---

## Additional notes to read

The detailed historical memory entries live in the maintainer's local Claude
Code memory (not committed in this repo). The critical points are duplicated in:

- `MAINTAINER.md` - Pitfalls section (especially #1 SP+QT crash + recipe, #2
  forbidden UMG strings, #3 ß in DE, #4 MainMap, etc.)
- This document - Phase by phase

If you fork this project and need more historical context, the public commit log
+ changelogs in `patch-<lang>/manifest.json` carry the detailed history of each
release.

---

## Version of this document

- 2026-05-17: initial version after JP v1.0.5 delivery + retroactive DE/ES v1.4.7
  fix (BPOffsetPatcher dedup bug).
- 2026-06-07: English translation + v1.4.8 updates (`_rebundle.py`,
  `_patch_bpop.py`, `_restore_bpop_from_release.py`, BPOP regression note).
