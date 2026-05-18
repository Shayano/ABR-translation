# A Bumpy Ride - Translation Mods

Unofficial translation mods for [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), an indie railroad-simulation game on Steam.

**Current version : 1.4.7** (May 17, 2026)
**Game engine : Unreal Engine 5.3.2 (IoStore)**

> 🆕 **v1.4.7 highlights** : **first Japanese release** (`ですます調` register, ~900 strings translated) with a CJK font fix using a composite Roboto + DroidSansFallback override - no more empty-tofu glyphs. Bonus on **DE / ES**: fixed a silent bug where the second occurrence of ` law signs`, ` hours`, ` times` in Shareholder tasks stayed in English since v1.4.5 (now `Befolge 3 Schilder` / `Obedece 3 señales` instead of mixed-language), plus first-time `QuestTicket` patch (`Destination: Nearest Station` → `Ziel: Nächste Station` / `Destino: estación más cercana`). FR unchanged binaries from v1.4.6 (cosmetic version bump for cross-language alignment).

> Not developed or endorsed by the game's creators. Fan project, provided as is.

---

## Choose your language / Choisissez votre langue / Sprachauswahl / 言語を選択

| Language | README | Installer | Drop-in |
|---|---|---|---|
| 🇫🇷 **Français** | [README.fr.md](README.fr.md) | `ABR-fr_v1.4.7.zip` | `ABR-fr_v1.4.7_prepatched.zip` |
| 🇩🇪 **Deutsch** | [README.de.md](README.de.md) | `ABR-de_v1.4.7.zip` | `ABR-de_v1.4.7_prepatched.zip` |
| 🇪🇸 **Español** | [README.es.md](README.es.md) | `ABR-es_v1.4.7.zip` | `ABR-es_v1.4.7_prepatched.zip` |
| 🇯🇵 **日本語** | [README.jp.md](README.jp.md) | `ABR-jp_v1.4.7.zip` | `ABR-jp_v1.4.7_prepatched.zip` |
| 🇬🇧 English (overview only) | [README.en.md](README.en.md) | - | - |

Downloads are available in [Releases](../../releases).

---

## What you get

- **Français** (FR-FR, registre `tu`) : interface complète, dialogues, tutoriel, quêtes, skins, achievements. Noms propres et enseignes western en VO.
- **Deutsch** (DE-DE, Register `du`) : vollständige UI, Dialoge, Tutorial, Quests, Skins, Erfolge. Eigennamen und Western-Schilder im Original.
- **Español** (ES, registro `tú`, variante neutra/España) : UI completa, diálogos, tutorial, misiones, skins, logros. Nombres propios y letreros western en VO.
- **日本語** (JP, ですます調) : 完全な UI、チュートリアル、クエスト、スキン説明、実績、ダイアログ。固有名詞 (列車名、駅名、地域名) は英語のまま。

All four translations share the same conventions :
- Proper nouns (skins, stations, regions, credits) kept in English
- `On` / `Off` buttons kept in English (UI width constraints)
- Imperial units (FT, miles) preserved
- Pixel-art shop signs kept in English (1900s western atmosphere).

The Japanese release additionally overrides 4 of the game's fonts with a composite Roboto + DroidSansFallback so that CJK glyphs (Hiragana / Katakana / Kanji) render correctly. Trade-off : the original pixel-art / digital aesthetic of `Pixel Times` and `AwfullyDigital` fonts is replaced by Roboto for all UI text.

---

## Installation

Each language ships in two formats. See the README of your language for full instructions :
- **Installer zip** (~30-70 MB) : PowerShell installer for Windows, auto-detects Steam, ~3-5 min install
- **Prepatched zip** (~2 GB) : drop-in for any OS (Windows / Linux / Steam Deck / macOS), no installer

Only one `.ucas` container can be active at a time. To switch languages, run a Steam integrity check on A Bumpy Ride (this restores vanilla), then install the other language.

---

## License

Free for personal use. No commercial redistribution. Translated assets derive from the original game (owned by its authors).

---

## Translation credits

- **Français** : human translation, manually proofread by the author (native speaker).
- **Deutsch**, **Español** and **日本語** : AI-assisted translation produced with Claude Code (Anthropic). Not human-proofread by a native speaker. Feedback and corrections welcome via GitHub issues.
