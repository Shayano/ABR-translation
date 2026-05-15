# A Bumpy Ride - Translation Mods

Unofficial translation mods for [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), an indie railroad-simulation game on Steam.

**Current version : 1.4.3** (May 15, 2026)
**Game engine : Unreal Engine 5.3.2 (IoStore)**

> 🆕 **v1.4.3 highlights** : critical fix for a crash at Shareholder pickup (main-quest blocker), plus 10 additional strings translated (QuestBoard task labels + on-train weather/TNT warnings). See language READMEs for details.

> Not developed or endorsed by the game's creators. Fan project, provided as is.

---

## Choose your language / Choisissez votre langue / Sprachauswahl

| Language | README | Installer | Drop-in |
|---|---|---|---|
| 🇫🇷 **Français** | [README.fr.md](README.fr.md) | `ABR-fr_v1.4.3.zip` | (regenerate locally via install.ps1) |
| 🇩🇪 **Deutsch** | [README.de.md](README.de.md) | `ABR-de_v1.4.3.zip` | (regenerate locally via install.ps1) |
| 🇬🇧 English (overview only) | [README.en.md](README.en.md) | - | - |

Downloads are available in [Releases](../../releases).

---

## What you get

- **Français** (FR-FR, registre `tu`) : interface complète, dialogues, tutoriel, quêtes, skins, achievements. Noms propres et enseignes western en VO.
- **Deutsch** (DE-DE, Register `du`) : vollständige UI, Dialoge, Tutorial, Quests, Skins, Erfolge. Eigennamen und Western-Schilder im Original.

Both translations share the same conventions :
- Proper nouns (skins, stations, regions, credits) kept in English
- `On` / `Off` buttons kept in English (UI width constraints)
- Imperial units (FT, miles) preserved
- Pixel-art shop signs kept in English (1900s western atmosphere)

---

## Installation

Each language ships in two formats. See the README of your language for full instructions :
- **Installer zip** (~70-90 MB) : PowerShell installer for Windows, auto-detects Steam, ~3-5 min install
- **Prepatched zip** (~2 GB) : drop-in for any OS (Windows / Linux / Steam Deck / macOS), no installer

Only one `.ucas` container can be active at a time. To switch languages, run a Steam integrity check on A Bumpy Ride (this restores vanilla), then install the other language.

---

## License

Free for personal use. No commercial redistribution. Translated assets derive from the original game (owned by its authors).
