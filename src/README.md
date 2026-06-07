# Mod sources - A Bumpy Ride translations

This `src/` folder contains everything used to produce the translation mods: the
PowerShell installers for Windows (alternative to the prepatched drop-in), the
translation JSON sources, the C# sources of the custom tools, the build pipeline
scripts, and the master translation rules document.

End users don't normally need to touch this folder - they download the prepatched
zip for their language directly. This directory exists for:
- transparency about what is modified in the game
- letting a contributor fix a translation or add a new language
- archiving the sources before any external fork disappears

If you intend to maintain or fork this project, the entry point is
[`MAINTAINER.md`](MAINTAINER.md).

---

## Structure

```
src/
├── README.md                    <- this file
├── MAINTAINER.md                <- complete guide for forking / maintaining
├── TRANSLATION_RULES.md         <- universal + per-language translation rules
├── PROCESS_NEW_LANGUAGE.md      <- step-by-step process for a new language
├── tools_src/                   <- custom tool sources (language-agnostic)
│   ├── mainmap_patcher/         <- MainMap install-time patcher (FR)
│   ├── bp_offset_patcher/       <- SP + QuestTicket BPOffsetPatcher
│   ├── bp_string_patcher/       <- backup BP string patcher (rare)
│   └── datatable_text_patcher/  <- DataTable + TextProperty patcher
├── pipeline/                    <- Python build pipeline (mirror of /staging/ + /releases/)
│   ├── staging/_package_de.py, _package_es.py, _patch_bpop.py, _rebundle.py, ...
│   └── releases/_build_prepatched.py, _make_v148_installer_zips.py, _sync_github_repo_src.py
└── languages/                   <- one subfolder per target language
    ├── fr/
    │   ├── installer/           <- ready-to-ship Windows installer (= patch-fr/ in working repo)
    │   │   ├── install.ps1, uninstall.ps1, manifest.json
    │   │   ├── retoc.exe, oo2core_9_win64.dll
    │   │   ├── MainMapPatcher.exe, ABumpyRide.usmap
    │   │   └── patched_assets/  <- ~150 patched .uasset/.umap
    │   └── translations/        <- JSON sources that produced patched_assets
    ├── de/, es/, jp/            <- same structure
```

> The binaries inside `installer/` (retoc.exe, MainMapPatcher.exe, oo2core,
> ABumpyRide.usmap) come from `tools/` in the working repo and are committed for
> reproducibility. `oo2core_9_win64.dll` is proprietary Oodle middleware (if
> missing from your fork, get a copy from any retoc release on
> https://github.com/trumank/retoc).

---

## How to install via PowerShell (FR example)

The installer in `languages/fr/installer/` is exactly the content of the
historical Windows zip (`ABR-fr_v1.4.X.zip`). It works in two detection modes:

- **Drop-in**: run from a directory in the game hierarchy (rare in practice)
- **Steam auto-detect**: reads `HKCU:\Software\Valve\Steam\SteamPath` and parses
  `libraryfolders.vdf` to find A Bumpy Ride across all Steam libraries

Complete pipeline (~3-5 min, ~12 GB temp space required):

1. Backup `ABumpyRide-Windows.{utoc,ucas,pak}` into `Paks/_ABRfr_backup/`
2. `retoc.exe to-legacy <Paks/> <legacy_dir/> --filter "BP"` then `--filter ".umap"`
   - extracts the .uasset/.uexp/.umap from the Oodle-compressed vanilla `.ucas`
3. Overlay `installer/patched_assets/*` on top -> ~150 files replaced with their FR versions
4. **FR-only step**: `MainMapPatcher.exe` patches `MainMap.uexp` (2.3 GB, beyond
   KissE's reach) in two passes - `--target=intro` then `--target=staff`
5. `retoc.exe to-zen <legacy_dir/> <fr.utoc>` - repackage in Zen IoStore format
6. Re-injection into the vanilla container via `unpack-raw` + filter chunks +
   `pack-raw`
7. Copy the final `.utoc/.ucas` into `Paks/`

To run:
```powershell
cd src/languages/fr/installer
.\install.ps1
```

> Prerequisites: PowerShell 5.1+ (bundled with Windows 10/11), .NET 8 runtime
> (bundled in `MainMapPatcher.exe` self-contained, no separate install needed).

---

## Adding a new language

See `PROCESS_NEW_LANGUAGE.md` for the detailed 5-phase process. Express summary:

1. `mkdir translations/<lang>/` and clone the 10 JSON file structure from `de/`
2. Adapt `patch-<lang>/manifest.json` (language, register, version)
3. Run `pipeline/staging/_inject_extra_strings.py <lang>` to inject the 10
   strings missing from the initial extract
4. Translate the JSONs (follow `TRANSLATION_RULES.md`)
5. Clone `pipeline/staging/_package_de.py` -> `_package_<lang>.py` (adapt paths)
6. Run the rebuild: `_package_<lang>.py` -> `_patch_bpop.py <lang>` -> `_rebundle.py <lang>`
7. Test in-game (Shareholder pickup is mandatory, see MAINTAINER.md Pitfall #1)
8. Standard release workflow (see MAINTAINER.md)

For CJK languages (JP, ZH, KR), see `pipeline/staging/_make_jp_font_overrides.py`
for the font fallback pattern.

---

## How to rebuild the C# tools

Source code is in `tools_src/<tool>/` with a `.csproj`. Standard build:

```powershell
cd tools_src/bp_offset_patcher
dotnet build -c Release
# Output in bin/Release/net8.0/BPOffsetPatcher.exe
```

| Tool | Target framework | Single-file publish (for distribution) |
|---|---|---|
| `bp_offset_patcher` | net8.0 | `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true` |
| `bp_string_patcher` | net8.0 | same |
| `mainmap_patcher` | net8.0 | same (shipped as single-file in patch-fr/) |
| `datatable_text_patcher` | net9.0 | same |

UAssetAPI is referenced via the Shayano fork (with the UTF-8/16 encoding fix
required for accented characters). When rebuilding, either:
- Point the `.csproj` to the published NuGet UAssetAPI 1.1.0, or
- Clone https://github.com/Shayano/UAssetAPI next to the tool and adjust the
  `<ProjectReference>` path.

---

## External tools used (not bundled)

For transparency, here are the third-party tools used in the pipeline:

| Tool | Repo | Note |
|---|---|---|
| KissE / KismetEditor | [SolicenTEAM/KismetEditor](https://github.com/SolicenTEAM/KismetEditor) | Used as-is (most pipeline-specific patches have been merged upstream: UAssetAPI 1.1.0, spinner fix, `--patch-assignments`, `--patch-all-functions`) |
| UAssetAPI | [atenfyr/UAssetAPI](https://github.com/atenfyr/UAssetAPI) | [Shayano/UAssetAPI fork](https://github.com/Shayano/UAssetAPI) required (UTF-16 encoding fix for accents, not merged upstream) |
| retoc | [trumank/retoc](https://github.com/trumank/retoc) | Used as-is (UE5.3 supported natively) |
| Dumper-7 | [Encryqed/Dumper-7](https://github.com/Encryqed/Dumper-7) | Used as-is, produces `installer/ABumpyRide.usmap` (only needed if the game's UE version changes - rare) |
