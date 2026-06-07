# MAINTAINER.md - ABR-translation

Guide for picking up the maintenance of this project. If you only want to install the mod, read the [README](README.md).

**Companion docs**:
- [`TRANSLATION_RULES.md`](TRANSLATION_RULES.md) - translation conventions
- [`PROCESS_NEW_LANGUAGE.md`](PROCESS_NEW_LANGUAGE.md) - how to add a new language

---

## Table of contents

1. [Quick start: ship a release](#quick-start)
2. [Prerequisites](#prerequisites)
3. [Repo layout (the dirs you actually touch)](#repo-layout)
4. [The 5 binaries you need](#binaries)
5. [Detailed release workflow](#release-workflow)
6. [Reacting to a community PR](#community-pr)
7. [Reacting to a game update](#game-update)
8. [Adding a new language](#new-language)
9. [Pitfalls (the only section you really must read)](#pitfalls)
10. [Troubleshooting](#troubleshooting)

---

## Quick start

You've cloned the repo. You've installed the prerequisites. You want to ship a new version that includes a translation tweak in DE:

```powershell
# 1. Edit the JSONs
#    -> translations/de/strings_BP.json (or any other JSON)

# 2. Bump the manifests (mod_version, mod_date, add changelog entry)
#    -> patch-de/manifest.json (the language you touched)
#    -> patch-fr/, patch-es/, patch-jp/ manifests too if you ship a unified release

# 3. Rebuild the language
python staging/_package_de.py     # KissE + DTP (excludes SP + QuestTicket)
python staging/_patch_bpop.py de  # BPOffsetPatcher for SP + QuestTicket
python staging/_rebundle.py de    # staging/legacy_patched_DE -> patch-de/patched_assets

# 4. TEST IN-GAME (mandatory - especially the Shareholder pickup)
#    If it crashes, see "Pitfalls" #1 for the safe fallback.

# 5. Update READMEs (versions + dates in headers, zip filenames)
#    -> releases/github_repo/README*.md (the 6 of them: root + en/fr/de/es/jp)

# 6. Sync src/, commit, push
python releases/_sync_github_repo_src.py
cd releases/github_repo
git add -A
git commit -m "vX.Y.Z - <description>"
git push origin main

# 7. Build the 4 installer zips (~30 sec)
#    Duplicate releases/_make_v148_installer_zips.py into _make_vX.Y.Z_..., search/replace 1.4.8 -> X.Y.Z
python releases/_make_vX.Y.Z_installer_zips.py

# 8. Build the 4 prepatched zips (~30-60 min, ~8 GB total)
$env:ABR_STEAM_PAKS = '<your Steam path>/A Bumpy Ride/ABumpyRide/Content/Paks'
python releases/_build_prepatched.py

# 9. Create the GitHub release + upload the 8 zips
cd releases/github_repo
gh release create vX.Y.Z --repo Shayano/ABR-translation `
  --title "vX.Y.Z - <description>" `
  --notes-file ../_release_notes_vX.Y.Z.md
gh release upload vX.Y.Z dist_vX.Y.Z/*.zip --repo Shayano/ABR-translation
```

The rest of this doc expands what each step does and why.

---

## Prerequisites

For ongoing maintenance (no rebuilding tools from source):

| Tool | Version | Why |
|---|---|---|
| **Python** | 3.11+ (stdlib only, no pip deps) | the build pipeline (staging/, releases/) |
| **PowerShell** | 7+ | installers + safety hooks |
| **gh CLI** | 2.x (logged in via `gh auth login`) | create releases, upload zips |
| **Git** | 2.40+ | obvious |
| **A Bumpy Ride** | latest Steam build | mandatory for in-game testing |

Only needed if you want to rebuild the C# tools from source (rare):
- **.NET 8 SDK** for BPOffsetPatcher, BPStringPatcher, MainMapPatcher
- **.NET 9 SDK** for datatable_text_patcher

---

## Repo layout

Only the directories you'll actually touch are listed here. The repo contains other directories (`tools/Dumper-7-src/`, `tools/UE4SS/`, `tools/UEExtractor/`, etc.) used during initial bring-up; you can ignore them for normal maintenance.

```
<repo>/
├── translations/                  # SOURCES (edit these to change translations)
│   └── fr/, de/, es/, jp/         # 10 JSON files per language
│
├── patch-{fr,de,es,jp}/           # ONE BUILD per language (ships in the installer zip)
│   ├── manifest.json              # version + changelog + target vanilla hashes
│   ├── install.ps1                # Windows installer
│   ├── uninstall.ps1
│   ├── README.md                  # user doc for that language
│   └── patched_assets/            # ~150 patched .uasset/.uexp/.umap (built by the pipeline)
│
├── patch-{fr,de,es,jp}/           # all 4 patches ship retoc.exe + oo2core_9_win64.dll for IoStore repackaging
│
├── patch-fr/                      # FR ships 2 extra binaries the others don't have
│   ├── MainMapPatcher.exe + ABumpyRide.usmap # FR-only: patches 2 hardcoded strings in MainMap.uexp (~2.3 GB)
│   │                                         # at install time. KissE can't load MainMap (Int32 MemoryStream
│   │                                         # limit). DE/ES/JP leave these 2 strings ("Oh no!...", "New Staff
│   │                                         # Member!") in English: ~2 strings out of ~1500, accepted loss
│   │                                         # vs the cost of porting MainMapPatcher to each language. See Pitfall #4.
│
├── staging/                       # BUILD WORKSPACE
│   ├── _vanilla_post_update_legacy/   # vanilla UE Legacy (pipeline input)
│   ├── legacy_patched_{FR,DE,ES,JP}/  # pipeline output, before bundling
│   │
│   ├── _package_fr.py             # rebuild pipeline for FR (KissE + DTP)
│   ├── _package_de.py             # rebuild pipeline for DE
│   ├── _package_es.py             # rebuild pipeline for ES
│   ├── _rebundle.py               # copies staging/legacy_patched_* -> patch-*/patched_assets
│   ├── _patch_bpop.py             # BPOffsetPatcher for SP + QuestTicket
│   ├── _restore_bpop_from_release.py  # fallback: pull SP+QT from a previous release zip
│   ├── _inject_extra_strings.py   # NEW LANGUAGE only: inject the 10 strings missing from initial extract
│   └── _make_jp_font_overrides.py # NEW CJK LANGUAGE only: clone font with CJK fallback
│
├── tools/                         # BUILT BINARIES (committed)
│   ├── KismetEditor/KissE.exe + ABumpyRide.usmap
│   ├── bp_offset_patcher/bin/Release/net8.0/BPOffsetPatcher.exe
│   └── retoc/retoc.exe + oo2core_9_win64.dll
│
├── scripts/datatable_text_patcher/bin/Release/net9.0/datatable_text_patcher.exe
│
├── releases/
│   ├── _build_prepatched.py       # builds the 4 drop-in zips (~1.9 GB each)
│   ├── _make_v148_installer_zips.py    # template - duplicate per release
│   ├── _sync_github_repo_src.py   # working repo -> public mirror
│   └── github_repo/               # CLONE of Shayano/ABR-translation (commits push from here)
│       ├── README*.md             # 6 READMEs (root + en/fr/de/es/jp)
│       ├── src/                   # public mirror of sources
│       └── dist_vX.Y.Z/           # zips waiting to be uploaded
│
├── .claude/hooks/                 # safety hooks (validate before push / release)
├── TRANSLATION_RULES.md
├── PROCESS_NEW_LANGUAGE.md
└── MAINTAINER.md                  # this file
```

### Quirks to know

- **Sync is one-way**: `releases/_sync_github_repo_src.py` copies working repo -> `releases/github_repo/src/`. If a contributor opens a PR on the public repo, you must **backport manually** (see [Reacting to a community PR](#community-pr)).
- **The working repo is local-only** (no `git remote`). All distribution goes through `releases/github_repo/` which is a real clone of the public repo.

---

## Binaries

5 executables drive the whole pipeline. They are committed in the repo - **you don't need to rebuild them for normal maintenance**.

| Binary | Where | Why |
|---|---|---|
| **KissE.exe** | `tools/KismetEditor/` | Patches the `.uexp` bytecode (most UI/dialogue strings) |
| **datatable_text_patcher.exe** | `scripts/datatable_text_patcher/bin/Release/net9.0/` | Patches `DataTable` rows + `TextProperty` (skins, achievements, tutorial table) |
| **BPOffsetPatcher.exe** | `tools/bp_offset_patcher/bin/Release/net8.0/` | The ONLY tool that can safely patch `SpecialPassenger` and `QuestTicket` - KissE breaks their EX_Jump offsets |
| **MainMapPatcher.exe** | `patch-fr/` | FR only: patches the 2.3 GB `MainMap.uexp` at install time |
| **retoc.exe** | `tools/retoc/` (+ `oo2core_9_win64.dll`) | Decompresses/repackages the IoStore containers |

Sources are in `tools/<tool>-src/` (KismetEditor, UAssetAPI, Dumper-7) and `tools/<tool>/Program.cs` for the custom C# tools. Rebuild with `dotnet build -c Release` if you ever modify them.

Note: `oo2core_9_win64.dll` is proprietary Oodle middleware. If you fork the public repo and the DLL is missing, get it from any retoc release on https://github.com/trumank/retoc.

---

## Release workflow

A full release takes ~1h30, mostly the prepatched zip build.

### 1. Edit translations

Edit the JSONs in `translations/<lang>/`. Follow `TRANSLATION_RULES.md`.

If you touch entries under `FileName: "SpecialPassenger.uasset"` or `"QuestTicket.uasset"`, this is the major pitfall - see [Pitfalls #1](#pitfalls).

### 2. Bump the 4 manifests

For each `patch-<lang>/manifest.json`:
- `mod_version`: "1.4.X" -> "1.4.X+1"
- `mod_date`: new date YYYY-MM-DD
- Add a changelog entry under `changelog: { "1.4.X+1": "..." }`

For languages that didn't change functionally, write something like "Cosmetic version bump for unified release. Binaries identical to v1.4.X." in their changelog.

### 3. Rebuild the languages that changed

```powershell
# For each modified language:
python staging/_package_<lang>.py     # KissE + DTP (~5-10 min)
python staging/_patch_bpop.py <lang>  # BPOffsetPatcher for SP+QT (~10 sec)
python staging/_rebundle.py <lang>    # copy to patch-<lang>/patched_assets/
```

The legacy `_bundle_de.py` / `_bundle_es.py` scripts hardcode an old `mod_version` and would overwrite your manifest. Use `_rebundle.py` instead (it only updates the `bundle{}` block).

### 4. Test in-game

Install via `patch-<lang>\install.ps1`, launch the game, and run through:
1. **Shareholder pickup** (= tests SP + QuestTicket) - if it crashes, see Pitfalls #1
2. First tutorial run (= tests NewTutorialLevel)
3. Open the shop staff and click on upgrades (= tests ActiveStaffIcon)
4. Buy and switch a wagon (= tests NewShopMenu)

### 5. Update READMEs

Edit in `releases/github_repo/`:
- `README.md` (root): `**Current version : X.Y.Z** (date)` header + highlights + zip names in the table
- `README.en.md`, `README.fr.md`, `README.de.md`, `README.es.md`, `README.jp.md`: same header pattern in the right language

To replace zip filenames in bulk:

```powershell
Get-ChildItem releases\github_repo\README*.md | ForEach-Object {
  $c = Get-Content -Raw -Path $_.FullName -Encoding UTF8
  [System.IO.File]::WriteAllText($_.FullName, ($c -replace '_v1\.4\.X', '_v1.4.X+1'), [System.Text.UTF8Encoding]::new($false))
}
```

The `check-readmes-on-release.ps1` hook will block `gh release create vX.Y.Z` if a README is out of date.

### 6. Sync, commit, push

```powershell
python releases/_sync_github_repo_src.py
cd releases/github_repo
git add -A
git commit -m "vX.Y.Z - <description>"
git push origin main
```

The `check-src-on-push.ps1` hook validates that `src/` is aligned with `translations/`, `patch-*/`, `tools/`, `.md` before pushing.

### 7. Build installer zips (fast)

Duplicate `releases/_make_v148_installer_zips.py` into `_make_vX.Y.Z_installer_zips.py`, replace every `1.4.8` with `X.Y.Z`, then:

```powershell
python releases/_make_vX.Y.Z_installer_zips.py
```

Output: `releases/github_repo/dist_vX.Y.Z/ABR-{fr,de,es,jp}_vX.Y.Z.zip` (~30-70 MB each).

### 8. Build prepatched zips (long)

The script needs to know where your Steam install is. Set the env var once:

```powershell
$env:ABR_STEAM_PAKS = '<Your Steam path>\A Bumpy Ride\ABumpyRide\Content\Paks'
python releases/_build_prepatched.py
```

Inside `$env:ABR_STEAM_PAKS`, the script expects a `_ABRfr_backup/` directory containing the 3 vanilla files `ABumpyRide-Windows.{utoc,ucas,pak}`. If you don't have that backup yet:

```powershell
# Steam "Verify Integrity" first to get vanilla files in place, then:
$paks = $env:ABR_STEAM_PAKS
New-Item -ItemType Directory "$paks/_ABRfr_backup"
Copy-Item "$paks/ABumpyRide-Windows.utoc" "$paks/_ABRfr_backup/"
Copy-Item "$paks/ABumpyRide-Windows.ucas" "$paks/_ABRfr_backup/"
Copy-Item "$paks/ABumpyRide-Windows.pak" "$paks/_ABRfr_backup/"
```

The script reads `mod_version` from `patch-fr/manifest.json`, so it auto-detects the version. Output: `dist_vX.Y.Z/ABR-{fr,de,es,jp}_vX.Y.Z_prepatched.zip` (~1.9 GB each, ~8 GB total).

### 9. Create the release + upload

```powershell
# Write release notes (short - follow the pattern of releases/_release_notes_v1.4.8.md)
# Then:
cd releases/github_repo
gh release create vX.Y.Z --repo Shayano/ABR-translation `
  --title "vX.Y.Z - <description>" `
  --notes-file ../_release_notes_vX.Y.Z.md
gh release upload vX.Y.Z dist_vX.Y.Z/*.zip --repo Shayano/ABR-translation
```

### Post-release housekeeping (optional)

Delete prepatched zips from older releases to save GitHub storage (installers stay):

```powershell
foreach ($asset in 'ABR-fr_vX.Y.Z_prepatched.zip','ABR-de_vX.Y.Z_prepatched.zip',...) {
  gh release delete-asset vX.Y.Z $asset --repo Shayano/ABR-translation --yes
}
```

---

## Community PR

If a contributor opens a PR on the public Shayano/ABR-translation, **their changes don't arrive automatically** in your working repo. After merging:

```powershell
cd releases/github_repo
git pull origin main

# Backport the modified files into translations/<lang>/
# Example for an ES PR that touched 7 files:
$src = "releases\github_repo\src\languages\es\translations"
foreach ($f in 'enum_buildingtype.json','enum_passengerenum.json','enum_questtype.json','enum_titleblurbs.json','enum_titleblurbsrainy.json','strings_BP.json','strings_maps.json') {
  Copy-Item "$src\$f" "translations\es\$f" -Force
}

# Verify the sync check is happy
python releases/_sync_github_repo_src.py --check
```

If you skip this backport, the next sync will overwrite the PR with your local version.

---

## Game update

When the devs ship a game patch, the vanilla `.utoc/.ucas/.pak` change. Your existing mod keeps working as long as the target vanilla hashes match, but if devs retouched assets you patched, the corresponding translations silently break (KissE patches a vanilla that no longer exists).

Recipe:
1. Steam "Verify Integrity" to fetch the new vanilla
2. Back up the 3 new vanilla files in `<Paks>/_ABRfr_backup/`
3. Re-extract the new vanilla as UE Legacy via `retoc to-legacy`, put the result in `staging/_vanilla_post_update_legacy/` (overwrite)
4. Diff against the previous vanilla to identify retouched assets
5. Re-run `_package_<lang>.py` for each language
6. **Audit the `Patched M/N` outputs**: if M < N, some strings no longer match (dev rewordings) - fix manually in the JSONs
7. Update the `vanilla_files` (size + sha256) in the 4 manifests
8. Bump version, test in-game, release

Validated on the 2026-05-07 update (-> v1.3.2) and 2026-05-12 (-> v1.4.7).

---

## New language

See [`PROCESS_NEW_LANGUAGE.md`](PROCESS_NEW_LANGUAGE.md) for the detailed 5-phase process.

Express summary (Latin-script language):
1. `mkdir translations/<lang>/` and create the 10 JSON files
2. Copy `patch-de/` -> `patch-<lang>/` and adapt `manifest.json`
3. Run `staging/_inject_extra_strings.py <lang>` to inject the 10 strings missing from the initial extract
4. Translate the JSONs
5. Clone `staging/_package_de.py` -> `staging/_package_<lang>.py`, adapt paths
6. `python staging/_package_<lang>.py` then `python staging/_patch_bpop.py <lang>` then `python staging/_rebundle.py <lang>`
7. Test in-game (Shareholder pickup mandatory)
8. Standard release workflow

For CJK languages (JP, ZH, KR), see `staging/_make_jp_font_overrides.py` for the font fallback pattern.

---

## Pitfalls

### 🚨 #1: Shareholder pickup recursion crash (the major one)

**Symptom**: when picking up a Shareholder on the MainMap, brutal crash with `EXCEPTION_ACCESS_VIOLATION reading address 0xffffffffffffffff` and ~90 stacked `ABumpyRide_Win64_Shipping` frames in the crash log. UE5's "Infinite script recursion" signature.

**Cause**: `SpecialPassenger.uasset/.uexp` and `QuestTicket.uasset/.uexp` ONLY tolerate BPOffsetPatcher. KissE breaks their EX_Jump offsets.

**Protection in current pipeline**: `_package_de.py` and `_package_es.py` exclude SP+QT from KissE. You must run `python staging/_patch_bpop.py <lang>` to apply BPOffsetPatcher.

**Sanity check sizes** after the pipeline:
- SP vanilla = 115941 bytes; correct (BPOP) = 116200-116400; broken (KissE) ~120524
- QT vanilla ~35073; correct (BPOP) = 35460-35490; broken (KissE) ~35859

**Known regression (v1.4.8, not yet diagnosed)**: even BPOffsetPatcher can break SP/QT when the JSON entries are modified and the cumulative byte delta exceeds some threshold. In v1.4.8, +23 bytes of cumulative shift on SP from 3 minor string changes was enough.

**Fallback if BPOP still crashes**:

```powershell
python staging/_restore_bpop_from_release.py de v1.4.7   # download SP+QT from a known-good release
python staging/_restore_bpop_from_release.py es v1.4.7
python staging/_rebundle.py de es
```

You lose the translation changes that touched SP/QT (usually 0-3 strings), but the game stops crashing.

**To investigate someday**: debug BPOffsetPatcher (source in `tools/bp_offset_patcher/Program.cs`) to understand why a small cumulative shift produces a precise offset corruption. Probable cause: an `EX_Jump` pointing to a bytecode offset whose instruction has been displaced by a fraction (~1-3 bytes) due to an unhandled string-length rounding.

### #2: 8 forbidden UMG NameMap strings (separate cause of Shareholder pickup crash)

Never translate these strings: `Float`, `Pulsate`, `Lock`, `Quest 1`, `Quest 2`, `Quest 3`, `Unlocked Item`, `Unlocked Text`. They appear in `W_WonStocks`, `NPCPointer`, `QuestBoard`, `QuestTicket`, `PopUp` but are internal UMG identifiers, not displayed text. Translating them breaks `FindChildWidget`/`PlayAnimation` calls and triggers `EXCEPTION_ACCESS_VIOLATION`.

If SP+QT hashes are OK and the crash persists, check these 8 strings are absent from the JSONs.

### #3: `ß` invisible in-game (DE)

The game's bitmap font lacks the Eszett glyph. Every `ß` shows as an empty character. Accepted substitution: `ss` (Swiss German). Applied globally in v1.4.8 to DE JSONs. Umlauts `ä ö ü` render fine.

### #4: MainMap can only be FR

`MainMap.umap` has a ~2.3 GB uexp that doesn't fit in an Int32 MemoryStream. KissE can't load it. The custom `MainMapPatcher.exe` (in `patch-fr/`) handles this for FR only, at install time. DE/ES/JP MainMap intro+staff strings are not translated (accepted limitation).

### #5: KissE silent no-op without `--map=ABumpyRide.usmap`

Without the `--map=<usmap>` flag, UAssetAPI falls back to `RawExport` and KissE does nothing (but doesn't crash). The pipeline scripts always pass it.

### #6: KissE wants `--version=5.3` (long form, not `-v=5.3`)

The Shayano/KismetEditor fork wants `--version=5.3`. The short form `-v=5.3` makes it assume a different UE version.

### #7: DataTables / non-bytecode FText don't go through KissE

For `DataTable` rows (skin descriptions, achievements, Tutorial_Table), use `datatable_text_patcher` (already called by `_package_<lang>.py`). KissE only walks `.uexp` bytecode.

### #8: Stale vanilla hashes after a game update

The `target_game.vanilla_files.{size,sha256}` in each manifest document the target vanilla version. Update them after every post-update re-baseline, otherwise the PowerShell installers refuse to install if the user's vanilla doesn't match.

---

## Troubleshooting

### `Missing vanilla file: ...\_ABRfr_backup\ABumpyRide-Windows.utoc`

Either `ABR_STEAM_PAKS` env var is not set (so the script looks at the default Windows Steam path), or the `_ABRfr_backup/` directory is missing. Recipe in [Release workflow step 8](#8-build-prepatched-zips-long).

### `KissE crashes with "Bad import index"`

Happens on PlayerTrain, TutorialTeleport, ForestTeleport. Use `BPStringPatcher.exe` (in `tools/bp_string_patcher/`) as a fallback - placeholder+branch on isolated export. Rarely needed today.

### `gh release upload` fails with "Network connection lost"

Retry (upload is resumable). Use `--clobber` to overwrite partial uploads.

### A PreToolUse hook blocks `git push` or `gh release create`

Case A: `check-src-on-push.ps1` says `src/` is out of sync -> run `python releases/_sync_github_repo_src.py`.

Case B: `check-readmes-on-release.ps1` says READMEs are stale -> update the version line in the 5 main READMEs.

### Game crashes at startup after install

The mod's target vanilla diverged from what the user has installed (game update). Ask the user to "Verify Integrity" in Steam; you re-baseline and ship an aligned release.
