# A Bumpy Ride - Translation mods (unofficial)

> 🌍 **Other languages** : see [README.md](README.md) for the full list of available translations.

Unofficial translation mods for [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), an indie railroad-simulation game on Steam.

**Current version : 1.4.4** (May 15, 2026)
**Game engine : Unreal Engine 5.3.2 (IoStore)**

> 🆕 **v1.4.4** : fixes the remaining intermittent crash at Shareholder pickup. Diagnosis via full crashdump (9 GB) : UE5 infinite recursion in the `SpecialPassenger` Blueprint (defines task objectives shown above each Shareholder), caused by a corrupted `EX_Jump` offset in the patched bytecode. Attempt to re-patch via the safe `BPStringPatcher` tool also failed (likely due to bytecode complexity : 62 strings spread across many conditional branches). Decision : `SpecialPassenger` reverts to vanilla in this release. The 62 task objectives stay in English (`See the sunset`, `Stay aboard until 9PM`, `Avoid the desert between X and Y`, `Obey every law sign`, etc.). The rest of the game stays fully translated.

> This mod is neither developed nor endorsed by the game's creators. It's a fan project, provided as is.

> ⚠️ This repository ships **French and German** translations. Pick the one you want (or none) — the mod replaces the game's English UI, dialogue and notifications with the target language. The English-language audience can use this overview, but the in-game text becomes FR or DE depending on which package you install.

---

## Available languages

| Language | Detailed README | Register |
|---|---|---|
| 🇫🇷 Français (French) | [README.fr.md](README.fr.md) | `tu` (informal) |
| 🇩🇪 Deutsch (German) | [README.de.md](README.de.md) | `du` (informal) |

Both translations share these conventions :
- Proper nouns (skins, stations, regions, credits authors) kept in English
- `On` / `Off` buttons kept in English (UI width constraints)
- Imperial units (FT, miles) preserved
- Pixel-art shop signs kept in English (1900s western atmosphere)

---

## What's translated

- The entire UI (menus, buttons, settings, keybindings)
- Tutorial dialogue and main-map events (intro, notifications, story beats)
- All quest, freight, passenger and building labels
- Wagon and skin names + descriptions (proper nouns kept in original English)
- End-of-day screens, achievements, statistics

---

## Installation

Each language ships in two formats :

1. **Installer zip** (`ABR-fr_v1.4.4.zip` / `ABR-de_v1.4.4.zip`, ~35-70 MB) : PowerShell installer for Windows, auto-detects Steam, ~3-5 min install
2. **Prepatched zip** (drop-in, ~2 GB) : not published officially since v1.4.3 - regenerate locally by running `install.ps1` and zipping the produced `.ucas/.utoc/.pak`

### Drop-in steps

1. Download the prepatched zip for your chosen language from [Releases](../../releases)
2. **Close the game** if it's running
3. Locate your A Bumpy Ride `Paks` folder :
   - **Windows**   : `<Steam library>\steamapps\common\A Bumpy Ride\ABumpyRide\Content\Paks\`
   - **Steam Deck**: `~/.steam/steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
   - **Linux**     : `~/.local/share/Steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
4. Extract the zip into that `Paks/` folder. Three existing files will be overwritten :
   ```
   ABumpyRide-Windows.utoc
   ABumpyRide-Windows.ucas
   ABumpyRide-Windows.pak
   ```
   No need to back up the originals - Steam can restore them anytime (see uninstall).
5. Launch the game through Steam normally. Menus should be in FR or DE depending on your chosen package.

> Technical note: the patched `.ucas` is ~5.2 GB (vs ~1.6 GB vanilla) because the build pipeline doesn't re-Oodle-compress on output. It's fully functional, just heavier on disk.

---

## Uninstall / revert to the original game

You don't have to manage a manual backup - Steam can restore the vanilla files in one click :

1. In your Steam library, **right-click A Bumpy Ride** → *Properties*
2. *Installed Files* → **Verify integrity of game files**
3. Steam detects the three modified files and re-downloads them (~1.6 GB)
4. Next launch, the game is back in English

This same trick is your safety net : if anything ever feels broken, run an integrity check and you're back to a clean slate without rummaging through folders.

### Switching languages

Only one `.ucas` container can be active at a time. To switch from FR to DE (or vice versa) :
1. Run a Steam integrity check (restores vanilla)
2. Install the other language package

---

## Compatibility

| Aspect | Status |
|---|---|
| Game version | A Bumpy Ride as of May 12, 2026 - last targeted Steam update (Steam app id `2540610`) |
| Save files | Compatible - the mod doesn't touch any save data |
| Multiplayer | ABR has no multiplayer - N/A |
| Game updates | After every official game patch, you'll need to reinstall the latest mod build (otherwise the game can crash on startup) |

---

## Known issues

- **Game crashes on launch after install** : your installed game version is likely newer than the one this mod targets. Run a Steam integrity check to revert to vanilla and wait for an updated mod build.
- **Some text stays in English** : most likely a proper noun deliberately preserved (skins, stations, regions). If it's an actual UI string that's missing a translation, please [open an issue](../../issues) with a screenshot.
- **Garbled characters (ä, é, ö, etc.) instead of proper accents** : a sign of zip-extraction corruption. Re-download and re-extract with a tool that handles large files cleanly (7-Zip, Windows 10/11 built-in extractor, Ark on Steam Deck).
- **A few words stay in English on the QuestBoard and quest ticket** : `Lock` on the lock button above the quest tray, `DESTINATION:` on the side quest ticket. These are internal UMG identifiers (the widget sub-components) that caused a crash when translated. Known limitation in v1.4.3 - to be addressed in a future release via an alternative patching approach.
- **The 62 Shareholder task objectives stay in English** (`See the sunset`, `Stay aboard until 9PM`, `Don't open your map`, `Avoid the [biome] between X and Y`, `Obey every law sign`, `View the [scenic spot]`, etc.). The `SpecialPassenger` Blueprint (containing those 62 strings in its bytecode) refuses to be patched without crashing - likely due to the complexity of its conditional branches. Known limitation in v1.4.4. The rest of the Shareholder flow (quest title, map markers, objective validation) is translated.

---

## Why is this in English if the mods are non-English?

Two reasons. First, English-speaking players who stumble onto this repo deserve to know what the mods do and how to roll them back if they install one by mistake. Second, the technical write-up may be useful to anyone modding Unreal Engine 5.3 IoStore titles regardless of language.

---

## Credits & acknowledgments

- **Translations** : Shayano
- **Tools used in the patch pipeline** :
  - [retoc-rivals](https://github.com/natimerry/repak-rivals) - IoStore UE5.3 repackager
  - [KissE / KismetEditor](https://github.com/SolicenTEAM/KismetEditor) (patched fork by Shayano) - Blueprint bytecode patcher
  - [Dumper-7](https://github.com/Encryqed/Dumper-7) - generates the game's `.usmap`
  - [UAssetAPI](https://github.com/atenfyr/UAssetAPI) - UE asset manipulation
- **Methodology** : developed in pair-programming with Claude Code (Anthropic) over multiple sessions.

---

## License

This mod is provided free of charge, without warranty, as is. Translated assets derive from the original game (owned by its authors) - the translations are free for personal use.

No commercial redistribution.
