# A Bumpy Ride - Translation mods (unofficial)

> 🌍 **Other languages** : see [README.md](README.md) for the full list of available translations.

Unofficial translation mods for [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), an indie railroad-simulation game on Steam.

**Current version : 1.4.6** (May 16, 2026)
**Game engine : Unreal Engine 5.3.2 (IoStore)**

> 🆕 **v1.4.6** : recovers the 62 Shareholder task objectives that had been stuck in English since v1.0 due to the infinite-recursion crash. New custom patcher `BPOffsetPatcher` that solves two cumulated problems no existing tool handled : (1) internal offsets of statements relocated to the end of the bytecode, and (2) hardcoded `EX_IntConst` entry-points in the Blueprint's 47 internal callers. In-game : `See the sunset`, `Stay aboard until 9PM`, `Avoid the desert between 4AM and 6PM`, `Pick up some honey: 0/3`, `Tour the big tree photo spot`, etc. are now translated. Grammar fix on concatenated cargo fragments (e.g. FR `Récupérer du poires` → `Récupérer des poires`, DE `Sammle etwas Birnen` → `Sammle einige Birnen`). 2 `AM`/`PM` strings remain in English (minor duplicate-handling bug in the patcher, non-blocking).

> This mod is neither developed nor endorsed by the game's creators. It's a fan project, provided as is.

> ⚠️ This repository ships **French, German, and Spanish** translations. Pick the one you want (or none): the mod replaces the game's English UI, dialogue and notifications with the target language. The English-language audience can use this overview, but the in-game text becomes FR, DE, or ES depending on which package you install.

---

## Available languages

| Language | Detailed README | Register |
|---|---|---|
| 🇫🇷 Français (French) | [README.fr.md](README.fr.md) | `tu` (informal) |
| 🇩🇪 Deutsch (German) | [README.de.md](README.de.md) | `du` (informal) |
| 🇪🇸 Español (Spanish) | [README.es.md](README.es.md) | `tú` (informal, neutral / Spain) |

All three translations share these conventions :
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

1. **Installer zip** (`ABR-fr_v1.4.6.zip` / `ABR-de_v1.4.6.zip` / `ABR-es_v1.4.6.zip`, ~30-100 MB) : PowerShell installer for Windows, auto-detects Steam, ~3-5 min install
2. **Prepatched drop-in zip** (`ABR-fr_v1.4.6_prepatched.zip` / `ABR-de_v1.4.6_prepatched.zip` / `ABR-es_v1.4.6_prepatched.zip`, ~1.9 GB) : direct file replacement, works on any OS (Windows / Linux / Steam Deck / macOS), no installer required

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
5. Launch the game through Steam normally. Menus should be in FR, DE, or ES depending on your chosen package.

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

Only one `.ucas` container can be active at a time. To switch between FR, DE, and ES :
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
- **A few words stay in English on the QuestBoard and quest ticket** : `Lock` on the lock button above the quest tray, `DESTINATION:` on the side quest ticket. These are internal UMG identifiers (the widget sub-components) that caused a crash when translated. Known limitation as of v1.4.6, to be addressed in a future release via an alternative patching approach.
- **2 `AM`/`PM` strings (in the 9PM / 9AM Shareholder objectives) stay in English** : duplicate-handling bug in the new patcher (the 2nd occurrence of each duplicate is skipped). Non-blocking - "Stay aboard until 9PM" remains readable with an English `AM` next to the counter. Will be fixed in a future minor version.

---

## Why is this in English if the mods are non-English?

Two reasons. First, English-speaking players who stumble onto this repo deserve to know what the mods do and how to roll them back if they install one by mistake. Second, the technical write-up may be useful to anyone modding Unreal Engine 5.3 IoStore titles regardless of language.

---

## Credits & acknowledgments

- **Translations** :
  - **Français** : human translation by Shayano (native speaker), manually proofread
  - **Deutsch** and **Español** : AI-assisted translation produced with Claude Code (Anthropic), not human-proofread by a native speaker. Feedback and corrections welcome via GitHub issues.
- **Tools used in the patch pipeline** :
  - [retoc-rivals](https://github.com/natimerry/repak-rivals) - IoStore UE5.3 repackager
  - [KissE / KismetEditor](https://github.com/SolicenTEAM/KismetEditor) - Blueprint bytecode patcher
  - [Dumper-7](https://github.com/Encryqed/Dumper-7) - generates the game's `.usmap`
  - [UAssetAPI](https://github.com/atenfyr/UAssetAPI) - UE asset manipulation
- **Methodology** : developed in pair-programming with Claude Code (Anthropic) over multiple sessions.

---

## License

This mod is provided free of charge, without warranty, as is. Translated assets derive from the original game (owned by its authors) - the translations are free for personal use.

No commercial redistribution.
