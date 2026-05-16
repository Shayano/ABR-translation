# A Bumpy Ride - Translation Mods

Unofficial translation mods for [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), an indie railroad-simulation game on Steam.

**Current version : 1.4.6** (May 16, 2026)
**Game engine : Unreal Engine 5.3.2 (IoStore)**

> 🆕 **v1.4.6 highlights** : recovers the 62 Shareholder task objectives that had been stuck in English since v1.0 due to the infinite-recursion crash. New custom patcher `BPOffsetPatcher` solves two cumulated problems no existing tool handled : (1) internal offsets of statements relocated to the end of the bytecode, and (2) hardcoded `EX_IntConst` entry-points in the Blueprint's 47 internal callers. In-game : "See the sunset", "Stay aboard until 9PM", "Pick up some honey: 0/3", "Tour the big tree photo spot", etc. are now translated. Grammar fix on concatenated cargo fragments. 2 `AM`/`PM` strings remain in English (minor non-blocking duplicate-handling bug).

> Not developed or endorsed by the game's creators. Fan project, provided as is.

---

## Choose your language / Choisissez votre langue / Sprachauswahl

| Language | README | Installer | Drop-in |
|---|---|---|---|
| 🇫🇷 **Français** | [README.fr.md](README.fr.md) | `ABR-fr_v1.4.6.zip` | (regenerate locally via install.ps1) |
| 🇩🇪 **Deutsch** | [README.de.md](README.de.md) | `ABR-de_v1.4.6.zip` | (regenerate locally via install.ps1) |
| 🇪🇸 **Español** | [README.es.md](README.es.md) | `ABR-es_v1.4.6.zip` | (regenerate locally via install.ps1) |
| 🇬🇧 English (overview only) | [README.en.md](README.en.md) | - | - |

Downloads are available in [Releases](../../releases).

---

## What you get

- **Français** (FR-FR, registre `tu`) : interface complète, dialogues, tutoriel, quêtes, skins, achievements. Noms propres et enseignes western en VO.
- **Deutsch** (DE-DE, Register `du`) : vollständige UI, Dialoge, Tutorial, Quests, Skins, Erfolge. Eigennamen und Western-Schilder im Original.
- **Español** (ES, registro `tú`, variante neutra/España) : UI completa, diálogos, tutorial, misiones, skins, logros. Nombres propios y letreros western en VO.

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

---

## Translation credits

- **Français** and **Deutsch** : human translation, manually proofread.
- **Español** : AI-assisted translation produced with Claude Code (Anthropic). Not human-proofread by a native Spanish speaker. Feedback and corrections welcome via GitHub issues.
