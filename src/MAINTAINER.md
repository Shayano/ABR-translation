# MAINTAINER.md - ABR-translation

> Complete guide for picking up, evolving, or forking the **A Bumpy Ride** translation project. If you only want to install the mod, read the [README](README.md). This document is for the person touching the code and releases.

**Before you start**, read these 3 sibling docs:
- [`TRANSLATION_RULES.md`](TRANSLATION_RULES.md) - translation conventions (register, proper nouns, On/Off, etc.)
- [`PROCESS_NEW_LANGUAGE.md`](PROCESS_NEW_LANGUAGE.md) - step-by-step process to add a new language (5 phases + cheat sheet)
- [The user README](README.md) - overview for players

---

## Table of contents

1. [Quick start - rebuild a language + release](#quick-start)
2. [Repo layout](#repo-layout)
3. [Tools, dependencies, machine setup](#tools-and-dependencies)
4. [End-to-end release workflow](#release-workflow)
5. [Reacting to a game update](#reacting-to-a-game-update)
6. [Adding a new language](#adding-a-new-language)
7. [⚠️ Known pitfalls + fix recipes](#known-pitfalls)
8. [Troubleshooting](#troubleshooting)
9. [Technical architecture: how the patching works](#technical-architecture)
10. [External references](#external-references)

---

## Quick start

You have already cloned the repo, installed the dependencies (see [dedicated section](#tools-and-dependencies)) and you want to ship a v1.4.9 release that only changes the DE translation:

```powershell
# 1. Edit the language JSONs
#    -> translations/de/strings_BP.json, etc.

# 2. Bump the DE manifest (and all the others if it's a multi-language release)
#    -> patch-de/manifest.json: mod_version "1.4.8" -> "1.4.9", mod_date, add changelog entry

# 3. Rebuild DE
python staging/_package_de.py     # KissE + DTP (excludes SP + QuestTicket)
python staging/_patch_bpop.py de  # BPOffsetPatcher for SP + QuestTicket
python staging/_rebundle.py de    # copies staging/legacy_patched_DE -> patch-de/patched_assets

# 4. ⚠️ TEST THE SHAREHOLDER PICKUP IN-GAME
#    -> patch-de\install.ps1 ; launch the game ; spawn a Shareholder on the MainMap.
#    If it crashes, see "Known pitfalls -> SP/QT crash" below.

# 5. Update the READMEs with the new version
#    -> edit the ~6 README*.md in releases/github_repo/ (versions + dates + highlights)

# 6. Sync src/, commit, push
python releases/_sync_github_repo_src.py
cd releases/github_repo
git add -A
git commit -m "v1.4.9 - <short description>"
git push origin main

# 7. Build the installer zips (4 languages)
#    -> duplicate releases/_make_v148_installer_zips.py into _make_v149_... and bump 1.4.8 -> 1.4.9
python releases/_make_v149_installer_zips.py

# 8. Build the prepatched zips (4 languages, ~30-60 min, ~8 GB)
python releases/_build_prepatched.py

# 9. Create the GitHub release + upload
cd releases/github_repo
gh release create v1.4.9 --repo Shayano/ABR-translation \
  --title "v1.4.9 - <short description>" \
  --notes-file ../_release_notes_v1.4.9.md
gh release upload v1.4.9 dist_v1.4.9/*.zip --repo Shayano/ABR-translation
```

That's the gist. The rest of this doc explains each tool and why this order.

---

## Repo layout

```
F:\Tools\ABR-fr\                       # working repo (local workspace)
├── translations/                      # TRANSLATION SOURCES (canonical JSONs)
│   ├── de/                            # German
│   │   ├── strings_BP.json            # ~1000 UI/dialogue strings
│   │   ├── strings_maps.json          # strings inside .umap files
│   │   ├── enum_*.json (x7)           # enums (passenger types, freight, etc.)
│   │   ├── skinbuttontable.json       # skin descriptions
│   │   └── _budget_chars.json         # UI constraints (max chars per string)
│   ├── es/                            # Spanish (same structure)
│   ├── jp/                            # Japanese (same structure)
│   └── (FR lives in staging/ for historical reasons, see Quirks below)
│
├── patch-fr/                          # PATCH BUILD for French (= distributable bundle)
│   ├── manifest.json                  # version + changelog + target vanilla hashes
│   ├── install.ps1                    # Windows PowerShell installer
│   ├── uninstall.ps1                  # uninstaller
│   ├── README.md                      # user doc for this language
│   ├── retoc.exe, oo2core_9_win64.dll # IoStore repackaging binaries
│   ├── MainMapPatcher.exe (FR only)   # patches MainMap bytecode at install time
│   ├── ABumpyRide.usmap (FR only)     # UE5 mappings used by MainMapPatcher
│   └── patched_assets/                # ~150 patched .uasset/.uexp/.umap (installer input)
│       └── ABumpyRide/Content/...
├── patch-de/                          # idem for German
├── patch-es/                          # idem for Spanish
├── patch-jp/                          # idem for Japanese
│
├── staging/                           # BUILD WORKSPACE (intermediaries, scripts)
│   ├── _vanilla_post_update_legacy/   # vanilla UE Legacy (patch input)
│   ├── legacy_patched_FR/             # output of _package_fr.py (before bundle)
│   ├── legacy_patched_DE/             # output of _package_de.py
│   ├── legacy_patched_ES/             # output of _package_es.py
│   ├── _package_de.py                 # DE REBUILD PIPELINE (KissE + DTP)
│   ├── _package_es.py                 # ES rebuild pipeline
│   ├── _rebundle.py                   # generic: staging/legacy_patched_<LANG>/ -> patch-<lang>/
│   ├── _patch_bpop.py                 # PATCH BPOffsetPatcher for SP + QT (run after _package_*.py)
│   ├── _restore_bpop_from_release.py  # FALLBACK: restore SP+QT from previous release if BPOP crashes
│   ├── _apply_de_*.py                 # historical "seed" scripts (one-shot, normally not used)
│   ├── fr_strings_BP_translated.json  # FR TRANSLATION SOURCE (historical - not in translations/fr/)
│   └── (many other one-shot / debug scripts)
│
├── tools/                             # BINARY TOOLS + SOURCES
│   ├── KismetEditor/                  # KissE.exe (binary) + ABumpyRide.usmap
│   ├── KismetEditor-src/              # C# source (Shayano fork)
│   ├── bp_offset_patcher/             # BPOffsetPatcher (source + bin/Release/net8.0/BPOffsetPatcher.exe)
│   ├── bp_string_patcher/             # BPStringPatcher (idem)
│   ├── mainmap_patcher/               # MainMapPatcher (idem, binary copied to patch-fr/)
│   ├── retoc/                         # retoc.exe (binary) + oo2core_9_win64.dll
│   ├── retoc-src/, retoc-rivals-src/  # sources (different forks)
│   ├── repak-rivals-src/              # repak source (UE5.3 compatible)
│   ├── UAssetAPI-src/                 # UAssetAPI source
│   ├── Dumper-7-src/                  # Dumper-7 source (generates the .usmap)
│   ├── UE4SS/                         # UE4SS (used by Dumper-7 as a proxy)
│   ├── UEExtractor/                   # initial .ucas extraction
│   ├── UnrealLocres/, UnrealMappingsDumper-src/
│   ├── UnrealPakTool/, ZenTools/      # historical alternatives
│   └── dump_exports/                  # small custom tool
│
├── scripts/                           # AUXILIARY PYTHON + C# TOOLS
│   ├── datatable_text_patcher/        # DTP: patches DataTable + StrPropertyData (.NET 9)
│   ├── uasset_test/                   # mini UAssetAPI diagnostic tool
│   ├── lint_de_budgets.py             # UI width linter (cf _budget_chars.json)
│   ├── decode_locres.py, extract_*.py # initial extraction tools (historical)
│   └── (other diagnostic scripts)
│
├── releases/                          # BUILD + DISTRIBUTION
│   ├── _build_prepatched.py           # builds the 4 drop-in 1.9 GB zips
│   ├── _make_v148_installer_zips.py   # builds the 4 installer zips (~35 MB each)
│   ├── _sync_github_repo_src.py       # sync working repo -> releases/github_repo/src/
│   ├── _release_notes_v1.4.8.md       # notes file for gh release create
│   └── github_repo/                   # CLONE of the public Shayano/ABR-translation repo
│       ├── README.md, README.{en,fr,de,es,jp}.md
│       ├── src/                       # PUBLIC source mirror (auto-synced)
│       │   ├── languages/{fr,de,es,jp}/translations/   # translation JSONs
│       │   ├── languages/{fr,de,es,jp}/installer/      # patch-<lang>/ contents
│       │   └── tools_src/             # C# tool sources
│       └── dist_v1.4.X/               # output zips (uploaded to Releases)
│
├── .claude/                           # Claude Code config (safety hooks)
│   ├── settings.json                  # enables 2 PreToolUse hooks
│   └── hooks/
│       ├── check-src-on-push.ps1      # blocks `git push` if src/ is out of sync
│       └── check-readmes-on-release.ps1   # blocks `gh release create v*` if READMEs are stale
│
├── TRANSLATION_RULES.md               # translation CONVENTIONS (all languages)
├── PROCESS_NEW_LANGUAGE.md            # PROCESS for adding a language
└── MAINTAINER.md                      # this file
```

### Quirks to know

- **FR has no `translations/fr/` folder**: for historical reasons, FR translations live in `staging/fr_strings_BP_translated.json` and `staging/enum_*_fr.json`. The sync script handles this.
- **Sync is one-way**: `releases/_sync_github_repo_src.py` copies working repo -> `releases/github_repo/src/`. If a contributor modifies the public repo via PR, **backport manually** their changes into `translations/<lang>/` (otherwise the next sync will overwrite their changes). Detailed recipe in "Reacting to a community PR" below.
- **`patch-fr/` contains embedded tools** (retoc.exe, oo2core, MainMapPatcher, .usmap) that the other `patch-<lang>/` reuse via `_build_prepatched.py`. So FR must always be present.
- **`releases/github_repo/` is a real git clone** of Shayano/ABR-translation. Commits/pushes happen from this folder, not from the working repo (which is local-only, no remote).

---

## Tools and dependencies

### Machine setup (one-time)

| Tool | Version | Purpose | Link |
|---|---|---|---|
| **Python** | 3.11+ (stdlib only) | scripts in staging/, releases/ | https://www.python.org/ |
| **.NET 8 SDK** | 8.0.x | rebuild BPOffsetPatcher / BPStringPatcher / MainMapPatcher (C#) | https://dotnet.microsoft.com/download |
| **.NET 9 SDK** | 9.0.x | rebuild datatable_text_patcher (C#) | https://dotnet.microsoft.com/download |
| **PowerShell** | 7+ | `install.ps1` installers + hooks | https://github.com/PowerShell/PowerShell/releases |
| **gh CLI** | 2.x | `gh release create / upload / view`, auth via `gh auth login` | https://cli.github.com/ |
| **Git** | 2.40+ | (Windows: Git for Windows) | https://git-scm.com/ |
| **A Bumpy Ride** | latest Steam | mandatory for in-game testing | Steam app id 2540610 |

### Binary tools bundled in the repo

All binaries are pre-compiled and committed. You only need to rebuild them if you modify their source.

| Binary | Path | Source | Rebuild |
|---|---|---|---|
| **KissE.exe** | `tools/KismetEditor/KissE.exe` | `tools/KismetEditor-src/` (Shayano/KismetEditor fork) | `dotnet build -c Release` in the src folder |
| **BPOffsetPatcher.exe** | `tools/bp_offset_patcher/bin/Release/net8.0/BPOffsetPatcher.exe` | `tools/bp_offset_patcher/Program.cs` | `dotnet build -c Release tools/bp_offset_patcher/` |
| **BPStringPatcher.exe** | `tools/bp_string_patcher/bin/Release/net8.0/BPStringPatcher.exe` | `tools/bp_string_patcher/Program.cs` | idem |
| **MainMapPatcher.exe** | `patch-fr/MainMapPatcher.exe` (single-file) | `tools/mainmap_patcher/Program.cs` | `dotnet publish -c Release -r win-x64 --self-contained` |
| **datatable_text_patcher.exe** | `scripts/datatable_text_patcher/bin/Release/net9.0/datatable_text_patcher.exe` | `scripts/datatable_text_patcher/Program.cs` | `dotnet build -c Release` |
| **retoc.exe** | `tools/retoc/retoc.exe` (+ `oo2core_9_win64.dll`) | external (https://github.com/trumank/retoc) | pre-compiled binary |

### GitHub forks maintained by Shayano

The project depends on several patched forks. If you want to upgrade a tool, fork from Shayano's fork.

| Fork | Upstream | Why fork |
|---|---|---|
| **Shayano/KismetEditor** | SolicenTEAM/KismetEditor | Bumps UAssetAPI 1.0.2 -> 1.1.0, fixes spinner output redirected, fixes Visit-based offset, adds `--patch-assignments`/`--patch-all-functions` support |
| **Shayano/UAssetAPI** | atenfyr/UAssetAPI | Fixes UTF-8/UTF-16 encoding, fixes Infinity float crash on .umap |
| **Shayano/retoc** | trumank/retoc | Fixes bulk_data_padding for UE5.6 (NOT useful for ABR which is UE5.3) |
| **Shayano/repak-rivals** | natimerry/repak-rivals | UE5.3 IoStore support (but 108-byte drift on Zen->Legacy->Zen roundtrip, unresolved) |
| **Shayano/UnrealMappingsDumper** | Daivy03/UnrealMappingsDumper | Patches UE5=1, V1 (buggy, we use Dumper-7 instead) |

Upstream PRs on SolicenTEAM/KismetEditor: see `tools/KismetEditor-src/` README + Shayano's commit log (3 merged #4-6, 2 open #7-8 as of 2026-05-12).

### Generating the `.usmap` (required by KissE and BPOffsetPatcher)

The `.usmap` file holds UE5 mappings describing all of the game's type layouts. ABR doesn't ship one, so we have to generate it. Validated method:

1. Compile **Dumper-7** from `tools/Dumper-7-src/` (CMake + MSVC; `cmake --preset=msvc-release && cmake --build --preset=msvc-release`)
2. The produced binary is a DLL loaded via **UE4SS** (`tools/UE4SS/`):
   - Rename the Dumper-7 DLL to `dwmapi.dll`
   - Drop it in `<Steam>/A Bumpy Ride/ABumpyRide/Binaries/Win64/`
   - Launch the game, wait, press F8 (UE4SS config), then F6 to unload
3. The `.usmap` is generated in `ABumpyRide/Binaries/Win64/Dumper-7/`
4. Copy to `tools/KismetEditor/ABumpyRide.usmap` (used by KissE) AND `patch-fr/ABumpyRide.usmap` (used by MainMapPatcher at install time)

Only redo this when the game changes (devs recompile). The current `.usmap` is V3 ZStd 344 KB, 0 RawExports failed on UAssetAPI.

---

## Release workflow

A complete release takes ~1h30 (including 30-60 min of prepatched build). Detailed:

### 1. Edit translations

Edit the JSONs in `translations/<lang>/`. For FR, edit `staging/fr_*_translated.json` instead. Follow `TRANSLATION_RULES.md`.

If you touch entries under `FileName: "SpecialPassenger.uasset"` or `"QuestTicket.uasset"`, **be careful**: this is the major pitfall (see [Known pitfalls](#known-pitfalls)).

### 2. Bump the 4 manifests

For each `patch-<lang>/manifest.json`:
- `mod_version`: "1.4.X" -> "1.4.X+1"
- `mod_date`: new date YYYY-MM-DD
- Add a changelog entry in `changelog: { "1.4.X+1": "..." }` (clear text in French for FR, English for DE/ES/JP)

For languages that **haven't changed functionally** (cosmetic bumps), write "Cosmetic version bump for unified release. Binaries identical to vX.Y.Z." in the changelog.

### 3. Rebuild the languages that changed

```powershell
# For each modified language:
python staging/_package_<lang>.py     # KissE + DTP (~5-10 min)
python staging/_patch_bpop.py <lang>  # BPOP for SP+QT (~10 sec)
python staging/_rebundle.py <lang>    # copy to patch-<lang>/patched_assets/
```

**DO NOT run the old `_bundle_de.py` / `_bundle_es.py`**: they hardcode an old `mod_version` and overwrite your manifest.

If you only touched SP/QT entries (rare), you can skip `_package_<lang>.py` and run just `_patch_bpop.py <lang>` then `_rebundle.py <lang>`.

### 4. Test in-game

**Mandatory every release**: install the patch (`patch-<lang>\install.ps1`), launch the game, do:
- Shareholder pickup on the MainMap (= tests SP + QuestTicket)
- First tutorial run (= tests NewTutorialLevel)
- Open the shop staff and click on upgrades (= tests ActiveStaffIcon)
- Buy and switch a wagon (= tests NewShopMenu)

If Shareholder pickup crashes -> see [Known pitfalls](#known-pitfalls) -> SP/QT crash.

### 5. Update the READMEs

Edit in `releases/github_repo/`:
- `README.md` (root): line `**Current version : X.Y.Z** (date)`, highlights, language table with zip names
- `README.en.md`: `**Current version` line, highlights
- `README.fr.md`: `**Version actuelle` line, highlights
- `README.de.md`: `**Aktuelle Version` line, highlights
- `README.es.md`: `**Versión actual` line, highlights
- `README.jp.md`: `**現在のバージョン` line, highlights

Also: replace `_v1.4.X.zip` -> `_v1.4.X+1.zip` in all files (installer zip filenames). PowerShell one-liner:

```powershell
Get-ChildItem releases\github_repo\README*.md | ForEach-Object {
  $c = Get-Content -Raw -Path $_.FullName -Encoding UTF8
  [System.IO.File]::WriteAllText($_.FullName, ($c -replace '_v1\.4\.X', '_v1.4.X+1'), [System.Text.UTF8Encoding]::new($false))
}
```

The `check-readmes-on-release.ps1` hook will block `gh release create vX.Y.Z` if a README is out of date.

### 6. Sync + commit + push

```powershell
python releases/_sync_github_repo_src.py    # copies translations/, patch-*/, tools_src/, .md
cd releases/github_repo
git add -A
git commit -m "vX.Y.Z - <description>"
git push origin main
```

The `check-src-on-push.ps1` hook validates that `src/` is aligned with `translations/`, `patch-*/`, `tools/`, `.md` before pushing.

### 7. Build installer zips (fast, ~30 sec)

Duplicate `releases/_make_v148_installer_zips.py` into `_make_v149_installer_zips.py`, replace every occurrence of `1.4.8` with `1.4.9`, then:

```powershell
python releases/_make_v149_installer_zips.py
```

Output: `releases/github_repo/dist_v1.4.9/ABR-{fr,de,es,jp}_v1.4.9.zip` (~30-70 MB each).

### 8. Build prepatched zips (long, 30-60 min, ~8 GB)

```powershell
python releases/_build_prepatched.py
```

The script reads `mod_version` from `patch-fr/manifest.json`, so no script edits needed. Output: `dist_v1.4.9/ABR-{fr,de,es,jp}_v1.4.9_prepatched.zip` (~1.9 GB each).

**Prerequisite**: `<Steam>/.../A Bumpy Ride/ABumpyRide/Content/Paks/_ABRfr_backup/` must exist and contain the 3 vanilla files `ABumpyRide-Windows.{utoc,ucas,pak}`. Otherwise the script fails with "Missing vanilla file". Recipe to recreate the backup:

```powershell
# If _ABRfr_backup is missing but another backup exists (e.g. _ABRes_backup):
$src = "F:\Steam\steamapps\common\A Bumpy Ride\ABumpyRide\Content\Paks\_ABRes_backup"
$dst = "F:\Steam\steamapps\common\A Bumpy Ride\ABumpyRide\Content\Paks\_ABRfr_backup"
Copy-Item -Recurse $src $dst

# If no backup at all: Verify integrity in Steam to restore vanilla, then:
mkdir <Paks>/_ABRfr_backup
Move-Item <Paks>/ABumpyRide-Windows.* <Paks>/_ABRfr_backup/
```

### 9. Create the GitHub release + upload

```powershell
# Write release notes (short! follow the pattern in _release_notes_v1.4.8.md)
# Edit releases/_release_notes_v1.4.9.md

cd releases\github_repo
gh release create v1.4.9 --repo Shayano/ABR-translation `
  --title "v1.4.9 - <short description>" `
  --notes-file ..\_release_notes_v1.4.9.md

# Upload (5-30 min depending on bandwidth)
gh release upload v1.4.9 dist_v1.4.9\*.zip --repo Shayano/ABR-translation
```

The `check-readmes-on-release.ps1` hook fires on `gh release create v*` and verifies that the 5 main READMEs (md + en/fr/de/es) mention the target version in their header line.

### 10. Post-release (optional)

- **Clean up older releases** to save GitHub storage: `gh release delete-asset <tag> <asset_name> --yes`. Convention: only keep prepatched zips (~1.9 GB) on the latest release, delete `_prepatched.zip` from older releases (installer .zip 30-70 MB stay).
- **Comment on the merged community PR** if applicable: `gh pr comment <N> --repo Shayano/ABR-translation --body "..."`.

---

## Reacting to a game update

When A Bumpy Ride publishes an official patch, the vanilla `.utoc/.ucas/.pak` change. The existing mod keeps working as long as the target vanilla hashes haven't changed, but if devs retouched certain assets, some translations will silently get lost (KissE patches a vanilla that no longer exists).

Quick recipe (the long version is in the `project_abr_update_pipeline` memory note, to be externalized in a future iteration):

1. Steam "Verify integrity" to fetch the new vanilla
2. Back up the 3 new vanilla files in `<Paks>/_ABRfr_backup/`
3. Re-extract the new vanilla as UE Legacy via `retoc to-legacy`, put the result in `staging/_vanilla_post_update_legacy/` (overwriting the old one)
4. Diff against the previous vanilla to identify retouched assets
5. Re-run `_package_<lang>.py` for each language (re-applies translations on the new vanilla)
6. **Audit the `Patched M/N` outputs**: if M < N, some strings no longer match -> dev rewordings, fix manually in the JSONs
7. Update the `vanilla_files` (size + sha256) in the 4 manifests
8. Bump version, test in-game, release as usual

Validated on the 2026-05-07 update (-> v1.3.2) and 2026-05-12 (-> v1.4.7).

---

## Adding a new language

See [`PROCESS_NEW_LANGUAGE.md`](PROCESS_NEW_LANGUAGE.md) for the detailed 5-phase process (very well documented).

Express summary (for a Latin-script language without CJK):
1. `mkdir translations/<lang>/` and create the 10 JSON files (see list in PROCESS doc)
2. Copy `patch-de/` -> `patch-<lang>/` and adapt `manifest.json`
3. Run `staging/_inject_extra_strings.py <lang>` to add the 10 strings missing from the initial extract
4. Translate the JSONs
5. `python staging/_package_<lang>.py` (create this script by cloning `_package_de.py`, adapt the TRANS_<LANG>, PATCHED_<LANG>, WORKDIR paths)
6. `python staging/_patch_bpop.py <lang>` (this script already handles all languages)
7. `python staging/_rebundle.py <lang>`
8. Mandatory in-game test (Shareholder pickup)
9. Follow the standard release workflow

For CJK languages (JP, ZH, KR), you additionally need a font override (see `staging/_make_jp_font_overrides.py` as a template). Details in `reference_font_cjk_override` memory (to be externalized).

---

## Known pitfalls

### 🚨 #1: SP + QuestTicket Shareholder pickup recursion crash (MAJOR PITFALL)

**Symptom**: when picking up a Shareholder on the MainMap, brutal crash with crash.log showing ~90 stacked `ABumpyRide_Win64_Shipping` frames and `EXCEPTION_ACCESS_VIOLATION reading address 0xffffffffffffffff`. This is UE5's "Infinite script recursion" signature.

**Cause**: `SpecialPassenger.uasset/.uexp` (62 Shareholder objectives) and `QuestTicket.uasset/.uexp` (3 DESTINATION strings) ONLY tolerate BPOffsetPatcher. KissE applies a placeholder+branch technique that shifts EX_Jump offsets, and these 2 BPs have complex conditional branches that collapse into infinite recursion.

**Current protection**: `_package_de.py` and `_package_es.py` already EXCLUDE SP+QT from the KissE run (v1.4.8 fix). They end up as vanilla in `staging/legacy_patched_<LANG>/`. You then need to run `python staging/_patch_bpop.py <lang>` which re-patches them via BPOffsetPatcher.

**Sanity check**: after `_package_<lang>.py + _patch_bpop.py + _rebundle.py`, check the sizes:
- SP vanilla = 115941 bytes
- SP correct (BPOP) = 116200-116400 bytes depending on language
- SP broken (KissE) ~ 120524 bytes (delta ~+4583 -> red alert)
- QT vanilla ~ 35073 bytes
- QT correct (BPOP) ~ 35460-35490 bytes depending on language
- QT broken (KissE) ~ 35859 bytes (delta ~+786 -> red alert)

**BPOP regression (v1.4.8, non diagnosed)**: even BPOffsetPatcher can break SP/QT when the JSON strings are modified and the cumulative delta exceeds some threshold. In v1.4.8, +23 cumulative bytes on SP from 3 ß->ss + Stationen->Bahnhöfen changes were enough to crash.

**Fallback recipe if the crash persists**:

```powershell
# Restore SP+QT from a previous working release
python staging/_restore_bpop_from_release.py de v1.4.7
python staging/_restore_bpop_from_release.py es v1.4.7
python staging/_rebundle.py de es
# Test in-game. Should work (v1.4.7 binaries are known-good).
# Cost: lose the translation changes that touched SP/QT (rare, often 0-3 strings)
```

**To investigate one day**: debug BPOffsetPatcher to understand why +23 bytes of global shift produces a precise offset corruption on a specific instruction. Source in `tools/bp_offset_patcher/Program.cs`. Hypothesis: an `EX_Jump` that was pointing to a bytecode offset whose instruction has been displaced by a fraction (~1-3 bytes) because of an unhandled string-length rounding.

### #2: Forbidden strings (NameMap UMG) - AV crash on Shareholder pickup (different from #1)

8 strings must NEVER be translated in any JSON: `Float`, `Pulsate`, `Lock`, `Quest 1`, `Quest 2`, `Quest 3`, `Unlocked Item`, `Unlocked Text`. They appear in `W_WonStocks`, `NPCPointer`, `QuestBoard`, `QuestTicket`, `PopUp` but they are **internal UMG identifiers**, not displayed text. Translating them breaks `FindChildWidget`/`PlayAnimation` calls and triggers `EXCEPTION_ACCESS_VIOLATION`.

Symptom similar to #1 (crash on Shareholder pickup) but different cause. If SP+QT hashes are OK and the crash persists, check that these 8 strings are absent from the JSONs.

### #3: `ß` invisible in-game (DE)

The game's bitmap font lacks the Eszett glyph (U+00DF). Every `ß` shows as an empty character. Accepted substitution: `ss` (Swiss German spelling). Applied globally in v1.4.8 to DE JSONs. Umlauts `ä ö ü` render fine.

Unimplemented alternative: font override like Japanese (pattern in `staging/_make_jp_font_overrides.py`, but we lose the pixel-art aesthetic).

### #4: MainMap not directly translatable (FR only so far)

`MainMap.umap` has a ~2.3 GB uexp that doesn't fit in an Int32 MemoryStream. KissE refuses to load it. Custom solution: `MainMapPatcher.exe` (binary in `patch-fr/`, source in `tools/mainmap_patcher/`) that isolates the `ExecuteUbergraph_MainMap` export and applies placeholder+branch. Run during `install.ps1` in two passes (`--target=intro` then `--target=staff`).

Not used for DE/ES/JP because MainMap intro+staff strings haven't been translated for these languages (accepted limitation).

### #5: KissE Infinity bug on some `.umap`

`UAssetAPI 1.1.0` (used by KissE) crashes on ABR `.umap` files containing `Infinity` floats (`NewTutorialLevel`, `MainMap`, `Shop`). Fix applied in the Shayano/UAssetAPI fork (see historical `reference_kisse_infinity_bug`). If you rebuild KissE from source and this bug resurfaces, check that you're starting from the Shayano fork (commit with `UAssetAPI.dll.before_infinity_fix` as a witness).

### #6: KissE silent no-op without `--map=ABumpyRide.usmap`

Without the `--map=<usmap>` flag, UAssetAPI falls back to `RawExport` and KissE does nothing (but doesn't crash!). Always pass the `.usmap` argument. The `_package_<lang>.py` scripts already do this.

### #7: KissE `--version=5.3` (not `-v=5.3`)

The Shayano/KismetEditor fork wants `--version=5.3` (long form), not the short form `-v=5.3`. Otherwise it assumes another UE version and goes haywire.

### #8: DataTable / FText outside bytecode

The KissE pipeline only touches `.uexp` bytecode. For `DataTable` (skin descriptions, achievements, Tutorial_Table) and `FText` that aren't in bytecode, use `datatable_text_patcher` (already called by `_package_<lang>.py`). If you add a new asset with untranslated strings, check whether it's bytecode (KissE) or property data (DTP).

### #9: Sync overwrites changes from a community PR

If a contributor submits a PR on the public repo, **their changes don't arrive automatically** in the working repo. After merging, backport manually:

```powershell
# 1. Pull the merged PR locally
cd releases/github_repo
git pull origin main

# 2. Copy the files the PR modified back into translations/<lang>/
$src_dir = "releases\github_repo\src\languages\es\translations"
foreach ($f in 'enum_buildingtype.json','enum_passengerenum.json','enum_questtype.json','enum_titleblurbs.json','enum_titleblurbsrainy.json','strings_BP.json','strings_maps.json') {
  Copy-Item "$src_dir\$f" "translations\es\$f" -Force
}

# 3. Verify the sync check is happy
python releases/_sync_github_repo_src.py --check
```

If you skip this, the next `_sync_github_repo_src.py` will overwrite the PR with the local version (= revert).

### #10: Stale vanilla hashes after a game update

The `target_game.vanilla_files.{size,sha256}` in each `patch-<lang>/manifest.json` document the target vanilla version. Update them after each post-update re-baseline of the game, otherwise the PowerShell installers refuse to install if the user doesn't have exactly that vanilla.

### #11: Em-dashes (user preference)

Global convention: use only the classic hyphen `-` (U+002D). Never `—` (U+2014, em-dash) or `–` (U+2013, en-dash). Check new changelogs and release notes. The linter `staging/_clean_jp_forbidden_strings.py` (misnamed, checks all files) flags occurrences.

### #12: Co-Authored-By Claude in commits

NEVER add a `Co-Authored-By: Claude` or `Generated with Claude Code` trailer to commits in the public Shayano/ABR-translation repo. The maintainer is the sole author.

---

## Troubleshooting

### `Missing vanilla file: F:\Steam\...\_ABRfr_backup\ABumpyRide-Windows.utoc`

`_build_prepatched.py` requires a vanilla backup in `_ABRfr_backup`. If you have `_ABRes_backup` or `_ABRjp_backup` (created by a previous installer), copy it to `_ABRfr_backup`: `Copy-Item -Recurse <src_bk> <dst_bk>`. If no backup at all, do Steam "Verify Integrity" then create the backup manually.

### `KissE crashes with "Bad import index"`

That's what happens with PlayerTrain, TutorialTeleport, ForestTeleport. KissE's output is misleading but the BP is poorly patched. Solution: use `BPStringPatcher.exe` (placeholder+branch on an isolated export). See `staging/_apply_de_strings_BP.py` historically, or write a dedicated wrapper.

### `gh release upload` fails with "Network connection lost"

Retry (upload is resumable). If it persists: check connection, use `gh release upload --clobber` to overwrite partial uploads.

### The PreToolUse hook blocks `git push` with `permissionDecision: ask`

Case 1: `check-src-on-push.ps1` says `src/` is out of sync. Run `python releases/_sync_github_repo_src.py` (without `--check`) to align.

Case 2: `check-readmes-on-release.ps1` says READMEs are out of date. Update the 5 main READMEs so that they mention the target version in their header line (`**Current version : X.Y.Z**`, `**Version actuelle**`, etc.).

### `_package_<lang>.py` fails with `vanilla not found`

`staging/_vanilla_post_update_legacy/` is missing. That's the re-extracted UE Legacy vanilla. Recipe to rebuild: `retoc to-legacy <vanilla.utoc/ucas/pak>` -> `staging/_vanilla_post_update_legacy/`. See `_build_prepatched.py` for the retoc invocation pattern.

### Game crashes at startup after install

Probably the mod's target vanilla diverged from what the user installed (game update). Ask the user to do Steam "Verify Integrity" to restore vanilla, wait for a re-aligned release. Maintainer side: see [Reacting to a game update](#reacting-to-a-game-update).

---

## Technical architecture

### ABR asset format

The game is UE5.3.2 IoStore. The 3 container files:
- `ABumpyRide-Windows.utoc` (2 MB): Table of Contents
- `ABumpyRide-Windows.ucas` (1.6 GB): Oodle-compressed assets
- `ABumpyRide-Windows.pak` (10 MB): PAK container

Individual assets (.uasset/.uexp/.umap) are packaged in **Zen** format inside IoStore. The pipeline has to:
1. Decompress (`retoc to-legacy`: Zen -> Legacy) to produce readable .uasset/.uexp
2. Patch the Legacy assets (KissE, DTP, BPOP, MainMapPatcher)
3. Repackage (`retoc to-zen` then `retoc pack-raw`) to rebuild the containers

### The 4 patching tools

| Tool | Target | When to use |
|---|---|---|
| **KissE** | `.uexp` bytecode (`EX_StringConst`, `EX_TextConst`) | General case: all UI/dialogue BPs except SP+QT |
| **datatable_text_patcher** (DTP) | `DataTable` (skins, achievements, Tutorial_Table) + `TextProperty` (non-bytecode FText) | DataTable assets or assets with non-bytecode props |
| **BPOffsetPatcher** (BPOP) | `.uexp` bytecode with complex branches (SP + QT) | When KissE breaks EX_Jump (= recursion crash). Edit-in-place + global shift map + caller patching. |
| **MainMapPatcher** | `ExecuteUbergraph_MainMap` export of the 2.3 GB `.umap` | Hardcoded MainMap.uexp strings (intro + "New Staff Member Unlocked!"). FR only. |
| **BPStringPatcher** (BPSP) | `.uexp` bytecode when KissE fails with "Bad import index" | PlayerTrain v1.3.3 (4 strings), extended ForestTeleport (1 string). Backup mode, rarely used. |

### Why these tools are homemade

Upstream KissE had bugs: spinner output redirected, missing Visit-based offset fix, missing `--patch-assignments`. The Shayano fork fixes those.

BPOffsetPatcher, BPStringPatcher, MainMapPatcher were written from scratch for this project because no existing UE tool knew how to modify UE5.3.2 bytecode without breaking EX_Jump.

datatable_text_patcher extends UAssetAPI to walk `DataTableExport.Table.Data` -> `StrPropertyData` (KissE doesn't descend into DataTables, see historical limitation).

### Release strategy: drop-in vs installer

- **Installer zip** (~30-70 MB): contains the `patched_assets/` + `install.ps1`. The installer runs `retoc to-zen` at install time to produce the final .utoc/.ucas/.pak on the user's machine. Windows only. ~3-5 min install.
- **Prepatched zip** (~1.9 GB): directly contains the 3 final `.utoc/.ucas/.pak`. Drop-in on any OS. The user replaces 3 files and that's it.

Trade-off: drop-in is heavy (the patched `.ucas` is ~5.2 GB vs ~1.6 GB vanilla because the pipeline doesn't re-Oodle), but Linux/Steam Deck/macOS friendly.

### Sync repo workflow

```
F:\Tools\ABR-fr\                       <-- working repo (canonical sources)
   |
   |  python releases/_sync_github_repo_src.py
   v
F:\Tools\ABR-fr\releases\github_repo\src\    <-- local mirror
   |
   |  git add + commit + push
   v
github.com/Shayano/ABR-translation     <-- public repo
```

The working repo is local-only (no git remote). All distribution goes through `releases/github_repo/` which is a real clone of the public repo.

---

## External references

- **Public repo**: https://github.com/Shayano/ABR-translation
- **Steam game page**: https://store.steampowered.com/app/2540610/A_Bumpy_Ride/
- **Dev's Discord**: we were invited to open a discussion thread (see English README) - the dev is open to feedback
- **KismetEditor upstream**: https://github.com/SolicenTEAM/KismetEditor (Shayano PRs #4-6 merged, #7-8 open)
- **retoc**: https://github.com/trumank/retoc
- **UAssetAPI**: https://github.com/atenfyr/UAssetAPI
- **Dumper-7**: https://github.com/Encryqed/Dumper-7
- **UE4SS**: https://github.com/UE4SS-RE/RE-UE4SS

---

## Handy snippets

### "How many SP+QT entries (no-dedup)?"

```powershell
python -c "import json; bp=json.load(open('translations/de/strings_BP.json',encoding='utf-8-sig')); print({e['FileName']:sum(1 for v in e.get('Values',[]) if v.get('Original') and v.get('NewValue')) for e in bp if e['FileName'] in ('SpecialPassenger.uasset','QuestTicket.uasset')})"
```

Expected: `{'SpecialPassenger.uasset': 68, 'QuestTicket.uasset': N}` (N varies per language).

### "Which assets were retouched between v1.4.7 and v1.4.8?"

```powershell
$v147 = "<path to extracted v1.4.7 zip>\patch-de\patched_assets"
$v148 = "patch-de\patched_assets"
Get-ChildItem $v147 -Recurse -File -Filter '*.uexp' | ForEach-Object {
  $rel = $_.FullName.Substring($v147.Length)
  $v = Join-Path $v148 $rel
  if (Test-Path $v) {
    $d = (Get-Item $v).Length - $_.Length
    if ($d -ne 0) { "$($_.Name) delta $d" }
  }
}
```

### "Clean up an old release to free GitHub storage"

```powershell
foreach ($asset in 'ABR-fr_v1.4.6_prepatched.zip','ABR-de_v1.4.6_prepatched.zip','ABR-es_v1.4.6_prepatched.zip') {
  gh release delete-asset v1.4.6 $asset --repo Shayano/ABR-translation --yes
}
```

### "Run the DE budgets linter (strict UI widths)"

```powershell
python scripts/lint_de_budgets.py
```

Verifies that UI-constrained strings (PAINT, UPGRADES, BUY, etc.) respect their `max_chars` (cf `translations/de/_budget_chars.json`).
