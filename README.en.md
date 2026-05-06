# A Bumpy Ride - French translation (unofficial mod)

> 🇫🇷 **Lecteurs francophones** : la version française du README se trouve dans [README.md](README.md).

Unofficial French translation mod for [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), an indie railroad-simulation game on Steam.

**Current version: 1.3.1** (May 6, 2026)
**Game engine: Unreal Engine 5.3.2 (IoStore)**

> This mod is neither developed nor endorsed by the game's creators. It's a fan project, provided as is.

> ⚠️ This is a **French-only** translation. The mod replaces the game's English UI, dialogue and notifications with French text. If you don't want French in your game, don't install it.

---

## What's translated

- The entire UI (menus, buttons, settings, keybindings)
- Tutorial dialogue and main-map events (intro, notifications, story beats)
- All quest, freight, passenger and building labels
- Wagon and skin names + descriptions (proper nouns kept in original English)
- End-of-day screens, achievements, statistics

**Intentionally left in English** (to preserve the game's flavor):
- Proper nouns: skins (Lavish, Stockton, Dayton…), stations, regions, credits authors
- Pixel-art shop signs (1900s western aesthetic)
- `On` / `Off` (UI consistency + button-width constraints)
- Imperial units (FT, miles)

---

## Installation

The mod ships as a single zip containing the three game-container files, already patched. It's a straight file replacement, no installer.

### Steps

1. Download `ABR-fr_v1.3.1_prepatched.zip` from [Releases](../../releases)
2. **Close the game** if it's running
3. Locate your A Bumpy Ride `Paks` folder:
   - **Windows**   : `<Steam library>\steamapps\common\A Bumpy Ride\ABumpyRide\Content\Paks\`
   - **Steam Deck**: `~/.steam/steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
   - **Linux**     : `~/.local/share/Steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
4. Extract the zip into that `Paks/` folder. Three existing files will be overwritten:
   ```
   ABumpyRide-Windows.utoc
   ABumpyRide-Windows.ucas
   ABumpyRide-Windows.pak
   ```
   No need to back the originals up — Steam can restore them anytime (see uninstall).
5. Launch the game through Steam normally. Menus should be in French.

> Technical note: the patched `.ucas` is ~5.2 GB (vs ~1.6 GB vanilla) because the build pipeline doesn't re-Oodle-compress on output. It's fully functional, just heavier on disk.

---

## Uninstall / revert to the original game

You don't have to manage a manual backup — Steam can restore the vanilla files in one click:

1. In your Steam library, **right-click A Bumpy Ride** → *Properties*
2. *Installed Files* → **Verify integrity of game files**
3. Steam detects the three modified files and re-downloads them (~1.6 GB)
4. Next launch, the game is back in English

This same trick is your safety net: if anything ever feels broken, run an integrity check and you're back to a clean slate without rummaging through folders.

---

## Compatibility

| Aspect | Status |
|---|---|
| Game version | A Bumpy Ride as of May 6, 2026 (Steam app id `2540610`) |
| Save files | Compatible — the mod doesn't touch any save data |
| Multiplayer | ABR has no multiplayer — N/A |
| Game updates | After every official game patch, you'll need to reinstall the latest mod build (otherwise the game can crash on startup) |

---

## Known issues

- **Game crashes on launch after install**: your installed game version is likely newer than the one this mod targets. Run a Steam integrity check to revert to vanilla and wait for an updated mod build.
- **Some text stays in English**: it's most likely a proper noun deliberately preserved (skins, stations, regions). If it's an actual UI string that's missing a translation, please [open an issue](../../issues) with a screenshot.
- **Garbled characters (ä, õ, etc.) instead of proper accents**: a sign of zip-extraction corruption. Re-download and re-extract with a tool that handles large files cleanly (7-Zip, Windows 10/11 built-in extractor, Ark on Steam Deck).

---

## Why is this in English if the mod is French-only?

Two reasons. First, English-speaking players who stumble onto this repo deserve to know what the mod does and how to roll it back if they install it by mistake. Second, the technical write-up below may be useful to anyone modding Unreal Engine 5.3 IoStore titles regardless of language.

---

## Credits & acknowledgments

- **Mod**: Shayano
- **Tools used in the patch pipeline**:
  - [retoc-rivals](https://github.com/natimerry/repak-rivals) — IoStore UE5.3 repackager
  - [KissE / KismetEditor](https://github.com/SolicenTEAM/KismetEditor) (patched fork by Shayano) — Blueprint bytecode patcher
  - [Dumper-7](https://github.com/Encryqed/Dumper-7) — generates the game's `.usmap`
  - [UAssetAPI](https://github.com/atenfyr/UAssetAPI) — UE asset manipulation
- **Methodology**: developed in pair-programming with Claude Code (Anthropic) over roughly ten sessions.

---

## License

This mod is provided free of charge, without warranty, as is. Translated assets derive from the original game (owned by its authors) — the French translation is free for personal use.

No commercial redistribution.
