# A Bumpy Ride translation rules - reusable across all languages

Consolidated reference document. Every new translation (FR, ES, DE, etc.) must
follow these rules. If a rule is unclear, ask **before** translating, not after.

---

## 1. General philosophy

**Target audience: general public, not enthusiasts.**
A Bumpy Ride is a casual adventure game about trains, not a simulator for railfans.
Prefer descriptive and understandable terms over technically correct but obscure
jargon.

| EN | Avoid (technical) | Use (descriptive) |
|---|---|---|
| Tender | `Tender` (as-is) | `wagon-réservoir` / `Wassertank` (water tank wagon) |
| Caboose | (technical) | `fourgon de queue` / `Schlusswagen` (rear/brake wagon) |
| Throttle | - | `régulateur` / `Regler` (regulator, intuitive in steam context) |
| Switch / switchstand | - | `aiguillage` / `Weiche` (switch) |

**General rule**: if a non-enthusiast player would not understand, rephrase.

---

## 2. What we NEVER translate

### 2.1. Proper nouns (universal)

**All proper nouns stay as-is in EN**, no exception, in every context. They are
part of the game's visual/narrative identity.

- **Train skins**: Comet, Forgotten, Bilge Rat, Bolt, Big Dipper, Shimmer,
  Theodore, Lavish, Stockton, Delta, Movie Star, Dayton, Zuma, Huntie, Courage,
  Rover, Hidden Rose, Little Engine, Bootlegger, Conrad, Very Useful, Jupiter,
  Voyager, Lil' Belle, Neko Neon, Spirit, Sweet Toot, Texas, General,
  Sacramento - **all kept in EN**.
- **Character skins**: Margot, Pacifica, Theodore, etc. - kept in EN.
- **Station names**: Misty Creek, Cattail, Birchwood, Snowball, Aurora,
  Pinecone, Dustbowl, Chugwater, Bloomwater, Colby, Nowhere, Pearbury, Bumblebee,
  Blowhistle, Sugarcube, Eagle Nest, Seaside, etc. - kept in EN.
- **Region/area names** (AreaDiscovered.uasset): WHISTLING PEAKS, LILLI FOREST,
  RIO FRONTERA, PUT PRAIRIE - kept in EN.
- **Authors/contributors** in Basic_Credits.uasset: all kept in EN
  (Nathaniel Onandia, Harrison Hudson, Bobenny, Kobold, Eddie Sand, RWD, etc.).

### 2.2. Shop signs / urban background (.umap _SubLvl)

**All shop signs and background buildings stay in EN.** This covers files
`Frst_*_SubLvl.umap`, `Snw_*_SubLvl.umap`, `Dsrt_*_SubLvl.umap`, `Plns_*_SubLvl.umap`.

Reason: these signs are part of each town's visual identity (western/circa 1900
atmosphere). Translating `GROCERY` -> `ÉPICERIE` breaks the atmosphere and mixes
proper nouns with descriptives on the same storefront.

Covers both proper nouns (`MAXIMILLIAN'S`, `STEVENS & SONS`, `HUDSON ENGINE CO.`,
`HARVEY'S`, `KIMBALL FIRE CO.`) and generic descriptives (`GROCERY`, `BAKERY`,
`RESTAURANT`, `HARDWARE`, `POST OFFICE`, `INSURANCE`, `BAKED GOODS`, `MEAT & CHEESE`,
`MUSIC, DANCING & FOOD`, `CONFECTIONARY`, `DRESSMAKER`, `MILLINERY`, `HABERDASHERY`,
`ARCADE`, etc.).

Also: decorative fragments (`KE`, `ba`, `ry` - pieces of a large sign) -> leave empty.

### 2.3. `On` / `Off` (UI toggles)

**Never translate `On`, `Off`, `on`, `off`** in any context.

Reasons:
1. **Cross-asset consistency**: already left in EN elsewhere, don't mix.
2. **UI width constraints**: `Activé`/`Désactivé`, `Eingeschaltet`/`Ausgeschaltet`
   don't fit in boxes sized for 2-3 letters.

Known cases: `WeatherToggle.uasset`, `New_SettingsMenu.uasset`.

### 2.4. Units of measurement

**Never convert values or unit abbreviations.** The game uses the imperial system
(FT, miles) - that's an intentional design choice.

The **label** (English word naming the quantity) can and should be translated
normally; only numeric values and unit abbreviations stay intact:

- `ELEVATION: 624 FT` -> `ALTITUDE : 624 FT` ✅ (label translated, unit kept)
- `ELEVATION: 624 FT` -> `ALTITUDE : 190 m` ❌ (conversion forbidden)
- `Distance Traveled: 50 Miles` -> `Distance parcourue : 50 Miles` ✅
- `Distance Traveled: 50 Miles` -> `Distance parcourue : 80 Km` ❌

### 2.5. Technical internals (never displayed to the player)

Always keep empty - these are internal labels, not user-facing content.

- **HTML/RichText tags**: `<Shakey>`, `</>`, `<cf>`, etc.
- **Asset paths**: `Map.png`, etc.
- **UE console commands**: `r.ScreenPercentage `, `r.*`, etc.
- **Debug messages**: `MultiGate Node failed! Out of bounds indexing of the out pins...`
- **Default UMG placeholders**: `Text`, `Text Block`, `Pop up text`
  (the widget replaces them at runtime).
- **Effect tags**: `TNTShake`, etc.
- **Internal enum labels**: e.g. `BP_Roundhouse_Engines.uasset` -
  `Both In And On`, `Right Out, Left In, Both On` - not visible in-game.

### 2.5.ter. SpecialPassenger.uexp: untranslatable BP (cause of infinite recursion crash)

**`SpecialPassenger.uasset/uexp` must stay vanilla.** Any attempt to patch its 72
strings (62 translatable) triggers an **infinite recursion on the UE5 side**
(`Infinite script recursion detected at 90 calls`) that crashes the game on the
pickup of certain Shareholder tasks.

**Observed symptom**: non-deterministic crash on Shareholder pickup depending on
the displayed task. Some tasks pass, others crash systematically.

**Diagnostic performed (v1.4.4)**:
- Full minidump (9 GB) shows 90 `ProcessInternal` recursions in the UE5 VM
- Error message found in memory: `"Infinite script recursion ({0} calls) detected"`
- Rigorous bisection: SP isolated as the culprit among 30+ BP candidates

**Patch methods tried that ALL FAIL**:
- `KissE.exe` (original v1.4.0): breaks an EX_Jump via change-of-length
- `BPStringPatcher.exe` (placeholder+branch, normally safe): breaks anyway despite the technique

**Hypothesis**: the bytecode complexity of SpecialPassenger (62 strings spread
across many conditional branches evaluating task type) creates patterns that
neither KissE nor BPStringPatcher handle correctly.

**Consequence on the JSON side**: the 62 task translations remain in
`translations/fr/strings_BP.json` and the DE/ES JSONs but are **not applied
in the build**. SpecialPassenger.uasset/uexp ships vanilla in
`patch-fr/patched_assets/` and `patch-de/patched_assets/`.

> **2026-05-15 update**: a new tool, `BPOffsetPatcher`, validated in v1.4.5 the
> SP rebuild via edit-in-place + global shift map + caller patching. The 62
> objectives ship translated. **But** the v1.4.8 BPOP regression shows that the
> tool can still corrupt SP/QT under certain JSON changes (cumulative delta of
> ~23 bytes is enough). See `MAINTAINER.md` Pitfall #1 for the fallback recipe.

**Strings concerned** (62 in FR/DE): "See the sunset", "Stay aboard until 9PM",
"Don't open your map", "Desert/Prairie/Mountains/Forest", "Ride the train for at
least X hours", "Avoid the [biome] between X and Y", "Always see the sky",
"See Lava", "Travel through X tunnels", "Pass by X different stations",
"View the [Big Stack Summit / Lake Polari / Pleasant Pond / Bayou Bel Nuit]",
"Tour the [photo spot]", "Obey/Disobey X law signs", "Whistle back with X
different vehicles/trains", "Spin on a turntable", "Run into a dead end",
"Drive through water", "Reach max speed going downhill", "Avoid traveling
backwards", "Run out of water", "Lean the train X times", "Pick up some
[pears/honey/hay bales/ice cubes/cheese]", "Get close to a tornado",
"Experience a blizzard", "Breathe dusty air", "Reach 1000ft elevation",
"Run into X pedestrians".

**Future research paths**: manual targeted binary patching (byte-by-byte on each
EX_StringConst without touching surrounding bytecode), or wait for a new UE5 /
KissE version that handles this pattern better. The current production answer
is BPOffsetPatcher (see MAINTAINER.md).

### 2.5.bis. UMG identifiers in the NameMap (CRITICAL, cause of crashes)

**Some strings in the `*_strings_BP_translated.json` JSONs actually correspond
to UMG identifier names** (sub-widgets, Widget animations, UE5 classes) stored in
the `.uasset` NameMap. KissE treats them as FText and replaces them, which breaks
runtime `FindChildWidget("Lock_Panel")` / `PlayAnimation("Pulsate")` calls and
triggers an **EXCEPTION_ACCESS_VIOLATION** when the widget is instantiated.

First detected on the Shareholder quest in v1.4.0 -> v1.4.1 hotfix. Five
Blueprints contain strings that must NOT be translated:

| Blueprint | Forbidden strings | Why |
|---|---|---|
| `W_WonStocks.uasset` | `Float` | Widget animation name (`Float_INST`) + UE5 type (`MovieSceneFloatTrack`) |
| `NPCPointer.uasset` | `Pulsate` | Widget animation name (`Pulsate_INST`) |
| `QuestBoard.uasset` | `Lock` | Animation name (`Lock_INST`) + sub-widget (`Lock_Panel`) + texture refs (`LockIcon_Locked`, `LockIcon_Unlocked`) |
| `QuestTicket.uasset` | `Quest 1`, `Quest 2`, `Quest 3` | UMG sub-widget names (`Quest 1 check`, `Quest 1 Text`, etc., which contain the quest objectives) |
| `PopUp.uasset` | `Unlocked Item`, `Unlocked Text` | Sub-widget names (`UnlockedImage`, `UnlockedItem`, `UnlockedText`) |

**Consequence on the JSON side**: these 8 entries were removed from
`translations/fr/strings_BP.json`, `translations/de/strings_BP.json` and
`translations/es/strings_BP.json` in v1.4.1. Do not re-add them.

**Consequence on the patch side**: the 5 corresponding `.uasset/.uexp` files
ship VANILLA in `patch-fr/patched_assets/` and `patch-de/patched_assets/`.

**How to detect this kind of string in the future**:
1. Before patching a new `.uasset`, dump its NameMap entries.
2. For each `Original` to patch, check whether it appears **alone** in the
   NameMap (= standalone entry, not FText with namespace+KeyValue).
3. If yes, it's probably an identifier. Test in-game before shipping -
   especially on widgets that have animations (`*_INST`) or named sub-widgets.
4. Be careful: not all "isolated NameMap entries" crash. Many are also used as
   FText (Speed Up, Slow Down, Awards, Quit, Settings, etc. all validated hours
   of gameplay despite the pattern). Static audit is a HINT, not a proof -
   confirm by in-game bisection.

### 2.6. Authors and references in DataTables

Credits, contributors, studio/historical references -> always in EN.

---

## 2.7. Informal vs formal address

**Per-language decision** (option B confirmed 2026-05-04): each language follows
its natural casual-gaming convention. Languages can diverge on register without
this being an inconsistency - it's what pro localizations do.

| Language | Game-to-player register | Justification |
|---|---|---|
| FR | **informal** (`tu`) | French casual gaming convention for a cozy family game. Switched on 2026-05-07 across 45 BP strings (Tutorial_Table, NewShopMenu, StaffBoard, AreYouSure, PlayerTrain, etc.) for consistency with enums already in `tu`. **Exception**: the *diegetic catch phrases* of the title screen (`translations/fr/enum_titleblurbs.json`) where an NPC formally addresses travelers keep their formal register (`Vos tickets, s'il vous plaît ?` = conductor, `Votre attention s'il vous plaît...` = station announcement, `VOUS. NE. PASSEREZ. PAS !!` = French Gandalf quote). |
| ES | **informal** (`tú`) | Casual gaming convention in ES. `usted` reserved for formal contexts. |
| DE | **`du` informal** (confirmed 2026-05-05) | German casual gaming convention for a family game. `Sie` reserved for corporate/serious games. |
| JP | **ですます調 (standard polite form)** (confirmed 2026-05-16) | No tu/vous formal in JP. ですます調 is the casual-gaming-friendly norm (equivalent to German `du`: polite but not distant). 敬語 (humble/respectful keigo) avoided as too solemn. Playful style: exclamations 「！」 OK, final particles 「ね」「よ」 accepted in NPC dialogues. Short UI orders often use the nominal or neutral imperative form (`購入` rather than `購入してください`). |
| ZH | (to be decided) | No equivalent grammatical marker. |

---

## 3. What we translate normally

- Dialogues, narration, didactics, tutorials.
- Item descriptions, blurbs, types (freight, quests, passengers, buildings).
- Generic UI labels (Cancel, Confirm, Apply, Settings, Options, etc. - cf.
  `staging\translations_dict.ps1`).
- Statistics (Distance Traveled, Passengers Delivered, etc.).
- Action buttons.
- Game messages (achievements, notifications, pop-ups).

---

## 4. Encoding

**Force UTF-16 LE (`Encoding.Unicode`) on every FString containing characters
> 127** (accents: é, à, ô, ù, etc.).

If UAssetAPI writes UTF-8 into an ASCII-sized slot, the game crashes.
See `memory/reference_uassetapi_text_encoding.md`.

---

## 5. Cross-language consistency

To translate into a new language (ES, DE, etc.):

1. **Reuse these rules in full** - the "never translate" list is universal
   (proper nouns, signs, units, On/Off, internals).
2. Build a dictionary equivalent to `staging\translations_dict.ps1` for the new
   language.
3. Adapt the lay vocabulary to the target culture (a painful technical term in
   FR may be clear in DE and vice versa).
4. Test in-game and confirm choices with the user - each language can add its
   own specific rules.

---

## 6. Cumulative confirmations (history of user decisions)

- **2026-05-04**: train/character skins = proper nouns -> EN.
- **2026-05-04**: station names = proper nouns -> EN.
- **2026-05-03**: no railway jargon (Tender, etc.).
- **2026-05-04**: `On`/`Off` never translated (consistency + UI width).
- **2026-05-04**: region/area names = proper nouns -> EN.
- **2026-05-04**: shop signs (all `.umap _SubLvl`) -> EN (western/period
  atmosphere + proper nouns).
- **2026-05-04**: no unit conversion (FT, miles stay as-is).
- **2026-05-04**: authors/credits -> EN.
- **2026-05-04**: systematic formal address in FR (consistency with existing translations).
- **2026-05-04**: option B on register - per-language decision, no cross-language
  imposition. ES will be in `tú` (casual gaming convention) despite FR in `vous`.
- **2026-05-05**: DE confirmed in informal `du` (family game).
- **2026-05-05**: UI length constraints to respect for DE (and any language
  averaging longer than EN) - see section 7 below.
- **2026-05-15**: v1.4.1 hotfix - 8 strings removed (Float, Pulsate, Lock,
  Quest 1/2/3, Unlocked Item/Text) because they are UMG identifiers in the
  NameMap, translation -> Shareholder pickup crash. See section 2.5.bis.
- **2026-05-15**: v1.4.4 hotfix - `SpecialPassenger.uexp` stays vanilla (62
  Shareholder task strings in EN). Neither KissE nor BPStringPatcher patches it
  without breaking an EX_Jump in a deep conditional branch. Diagnosis via full
  minidump (9 GB) + rigorous bisection. See section 2.5.ter.
- **2026-05-16**: opening of JP (Japanese) translation. Register: ですます調
  (standard polite, equivalent to German `du`). No unit conversion (FT/miles
  intact), proper nouns EN, On/Off EN, western signs EN. UTF-16 LE encoding
  mandatory (CJK characters). UI constraints revisited: 1 CJK char ≈ 2 Latin
  chars wide, so budgets EN/2 approximately.
- **2026-05-16+**: BPOffsetPatcher unlocks SP+QT translation for FR/DE/ES/JP
  (v1.4.5+). Sections 2.5.ter and 2.5.bis are kept as historical context.
- **2026-06-07**: v1.4.8 BPOP regression discovered - even BPOP can corrupt SP/QT
  under certain JSON changes. Documented in MAINTAINER.md Pitfall #1, with safe
  fallback "restore SP+QT from previous working release".

---

## 7. UI length constraints (per language)

German is on average **30-40% longer than English**. Several fields have fixed
on-screen width and don't tolerate expansion. These budgets are **strict**: if
the translation overflows, rephrase or abbreviate until it fits.

### 7.1. Shop tabs / categories (≤ 8 characters)

`NewShopMenu.uasset` - shop window tab buttons.

| EN | FR (reference) | DE budget | DE proposal |
|---|---|---|---|
| `UPGRADES` | `AMÉLIO.` (7) | **≤ 8** | `UPGRADES` (8) or `TUNING` (6) |
| `FLAGS` | `DRAPEAUX` (8) | **≤ 8** | `FLAGGEN` (7) or `FAHNEN` (6) |
| `PAINT` | `COULEURS` (8) | **≤ 8** | `FARBEN` (6) |
| `BUY` | `ACHETER` (7) | **≤ 8** | `KAUFEN` (6) |
| `COST` | `COÛT` (4) | **≤ 8** | `KOSTEN` (6) or `PREIS` (5) |

### 7.2. Main menu - directions / shortcuts (≤ EN length)

`IA_TurnLeft.uasset`, `IA_TurnRight.uasset`, `IA_SpeedUp.uasset`,
`IA_SlowDown.uasset`, etc. - direction action descriptions displayed in the help
bar / shortcut menu.

| EN | EN length | FR (reference) | DE budget |
|---|---|---|---|
| `Turn Left` | 9 | `à gauche` (8) | **≤ 9** -> `Links` (5) |
| `Turn Right` | 10 | `à droite` (8) | **≤ 10** -> `Rechts` (6) |
| `Speed Up` | 8 | (see FR) | **≤ 8** -> `Schneller` (9) NO, `Beschl.` (7) or rephrased |
| `Slow Down` | 9 | (see FR) | **≤ 9** -> `Bremsen` (7) |

**Rule**: if a direction/action doesn't fit within the EN length, abbreviate or
pick a shorter synonym. **Never** overflow - the help bar truncates.

### 7.3. Settings menu - option labels

`New_SettingsMenu.uasset` - settings checkboxes. The width is generous (FR was
able to fit `Sensibilité caméra`, 18 chars), but some boxes are restricted:

| EN | FR (reference) | Note |
|---|---|---|
| `Auto Board` | `Embarq. auto` (12) | abbreviated in FR |
| `Reset` | `Réinit.` (7) | abbreviated in FR (narrow box) |
| `Saves & Backups` | `Sauvegardes` (11) | simplified in FR |
| `Tender Icon` | `Icône réservoir` (15) | OK |

**DE rule**: if the literal translation overflows, abbreviate with a period
(`Einst.`, `Zurücks.`) or pick a shorter term. Test in-game before finalizing.

### 7.4. Validation method

For every constrained string, add a comment (`_max_chars`) in the DE JSON or use
the file `translations/de/_budget_chars.json` which maps `KeyValue` ->
`max_chars`. A linter can then check that `len(NewValue) <= max_chars` before
patching.

---

## 8. DE (German) glossary

Dictionary of recurring terms for cross-asset consistency. Enrich as translation
progresses.

### 8.1. Railway terms (general public, not jargon)

| EN | DE recommended | Notes |
|---|---|---|
| Train | `Zug` | universal |
| Engine / Locomotive | `Lokomotive` (long) or `Lok` (3) | `Lok` under tight constraints |
| Tender | **`Wassertank`** (water tank, 10) or **`Tank`** (4, short) | Do **NOT** keep `Tender` as-is - it's also a technical railway term in DE. Consistent with the general-public philosophy (FR uses `réservoir`). |
| Caboose | `Schlusswagen` or `Bremserwagen` | or simply `Wagen am Ende` |
| Cupola | `Aussichtskuppel` (15) or `Kuppel` (6) | |
| Gondola | `Flachwagen` (10) or `Plattformwagen` | |
| Track | `Gleis` / `Schiene` | |
| Switch / switchstand | `Weiche` / `Weichenhebel` | |
| Whistle | `Pfeife` (verb: `pfeifen`) | |
| Brake / Brakeman | `Bremse` / `Bremser` | |
| Conductor | `Schaffner` | |
| Fireman | `Heizer` | |
| Throttle | `Regler` or `Drossel` | |
| Coal | `Kohle` | |
| Steam | `Dampf` | |
| Water tower | `Wasserturm` | |
| Freight | `Fracht` | |

### 8.2. UI / buttons / actions

| EN | DE recommended | Length |
|---|---|---|
| Apply | `Anwenden` | 8 |
| Cancel | `Abbrechen` | 9 |
| Confirm | `Bestätigen` | 10 |
| Reset | `Zurücks.` or `Reset` (anglicism) | 8 / 5 |
| Close | `Schliessen` (ß->ss per v1.4.8 FuelFire review) | 10 |
| Back | `Zurück` | 6 |
| Return | `Zurück` | 6 |
| Settings | `Einstellungen` | 13 |
| Options | `Optionen` | 8 |
| Audio | `Audio` | 5 |
| Graphics | `Grafik` | 6 |
| Gameplay | `Gameplay` (common anglicism) | 8 |
| Controls | `Steuerung` | 9 |
| Credits | `Mitwirkende` or `Credits` (anglicism) | 11 / 7 |
| Tutorial | `Tutorial` | 8 |
| Quit | `Beenden` | 7 |
| Exit | `Verlassen` or `Beenden` | 9 / 7 |
| Stats | `Statistik` | 9 |
| Map | `Karte` | 5 |
| Load | `Laden` | 5 |
| Save | `Speichern` | 9 |
| Delete | `Löschen` | 7 |
| Buy | `Kaufen` | 6 |
| Level | `Level` (anglicism) or `Stufe` | 5 |
| Day | `Tag` | 3 |
| Money | `Geld` | 4 |
| Yes | `Ja` | 2 |
| No | `Nein` | 4 |

### 8.3. Characters / jobs

| EN | DE | Notes |
|---|---|---|
| Bouncer | `Türsteher` | |
| Bartender | `Barkeeper` | |
| Brakeman | `Bremser` | |
| Fireman | `Heizer` | |
| Conductor | `Schaffner` | |
| Engineer | `Lokführer` | |
| Freight Agent | `Frachtagent` | |
| Pyrotechnist | `Pyrotechniker` | |
| Early Bird | `Frühaufsteher` | |
| Big 'Fella | (proper noun - keep EN?) | |

### 8.4. Tone

- **`du` register**: address the player informally everywhere (`du brauchst`,
  `dein Zug`, `dich`).
- **No jargon**: prefer `schneller` to `Beschleunigung`, `bremsen` to `verzögern`.
- **Playful/casual tone**: exclamations OK (`Los geht's!`, `Klasse!`).
- **Anglicisms accepted** when common in DE gaming (`Level`, `Upgrade`,
  `Gameplay`, `Reset`, `Highscore`).
- **`ß` -> `ss`** everywhere since v1.4.8 (the in-game font lacks the Eszett
  glyph; Swiss German uses `ss` as standard substitute).

---

## 9. JP (Japanese) glossary

Dictionary of recurring terms for cross-asset consistency. Enrich as translation
progresses.

### 9.1. General JP conventions

- **Style**: ですます調 (standard polite form, neither formal keigo nor familiar speech).
- **Final particles** in NPC dialogues: 「ね」「よ」「な」 OK for a playful tone.
- **Onomatopoeia**: adapt (`Hello` -> `こんにちは` or `やあ` depending on casual context).
- **CJK punctuation**:
  - Prefer 「」 for quotes, full-width ！ and ？ for exclamations in long dialogues.
  - For short UI strings: minimal punctuation, half-width OK.
- **Numbers and units**: half-width (ASCII) for `624 FT`, `50 Miles`, `Day 3`
  (consistent with game identity and readability).
- **Proper nouns**: stay in EN (cf. rule 2.1).
- **Common anglicisms** accepted in katakana when JP gaming usage adopts them:
  アップグレード (Upgrade), レベル (Level), セーブ (Save), マップ (Map),
  メニュー (Menu), アイテム (Item).

### 9.2. Railway terms (general public, not jargon)

| EN | JP recommended | Notes |
|---|---|---|
| Train | `列車` or `汽車` | `列車` neutral, `汽車` for old-school steam |
| Engine / Locomotive | `機関車` (3) | universal |
| Tender | `炭水車` (3) or `タンク` (3) | Don't keep `Tender` as-is (jargon). FR uses `réservoir`, DE `Tank`. |
| Caboose | `車掌車` (3) | rear wagon |
| Cupola | `展望台` or `屋根の見張り` | |
| Gondola | `無蓋車` (3) | open wagon |
| Track / Rail | `線路` (2) or `レール` (3) | |
| Switch / switchstand | `分岐器` / `転てつ機` | technical; prefer `ポイント` (katakana, common) |
| Whistle | `汽笛` (2) (verb `鳴らす`) | |
| Brake / Brakeman | `ブレーキ` / `制動手` | |
| Conductor | `車掌` (2) | |
| Fireman | `機関助士` (4) or `火夫` (2) | `火夫` shorter but archaic |
| Engineer / Driver | `運転士` (3) | |
| Throttle | `スロットル` or `加減弁` | |
| Coal | `石炭` (2) | |
| Steam | `蒸気` (2) | |
| Water tower | `給水塔` (3) | |
| Freight | `貨物` (2) | |
| Passenger | `乗客` (2) | |
| Station | `駅` (1) | (names stay EN: not `Aurora 駅`, just `Aurora`) |

### 9.3. UI / buttons / actions

| EN | JP recommended | Width (CJK chars) |
|---|---|---|
| Apply | `適用` | 2 |
| Cancel | `キャンセル` | 5 |
| Confirm | `確認` | 2 |
| Reset | `リセット` | 4 |
| Close | `閉じる` | 3 |
| Back | `戻る` | 2 |
| Return | `戻る` | 2 |
| Settings | `設定` | 2 |
| Options | `オプション` | 5 |
| Audio | `オーディオ` or `音声` | 5 / 2 |
| Graphics | `グラフィック` or `画面` | 6 / 2 |
| Gameplay | `ゲームプレイ` | 6 |
| Controls | `操作` | 2 |
| Credits | `クレジット` | 5 |
| Tutorial | `チュートリアル` | 7 |
| Quit | `終了` | 2 |
| Exit | `戻る` or `終了` | 2 |
| Stats | `統計` | 2 |
| Map | `マップ` | 3 |
| Load | `ロード` | 3 |
| Save | `セーブ` | 3 |
| Delete | `削除` | 2 |
| Buy | `購入` | 2 |
| Level | `レベル` | 3 |
| Day | `日目` or `Day` | 2 |
| Money | `お金` | 2 |
| Yes | `はい` | 2 |
| No | `いいえ` | 3 |
| Pause | `一時停止` or `ポーズ` | 4 / 3 |
| Resume | `再開` | 2 |
| Upgrade | `アップグレード` or `強化` | 7 / 2 |
| Paint | `塗装` | 2 |
| Flags | `フラッグ` or `旗` | 4 / 1 |
| Cost | `価格` | 2 |
| Unlock | `解放` | 2 |
| Locked | `ロック中` | 4 |
| Unlocked | `解放済み` | 4 |
| Skip | `スキップ` | 4 |
| Continue | `続ける` | 3 |
| Start | `スタート` or `開始` | 4 / 2 |
| Reach | `達成` | 2 |
| Complete | `完了` | 2 |

### 9.4. Characters / jobs

| EN | JP | Notes |
|---|---|---|
| Bouncer | `用心棒` | security |
| Bartender | `バーテンダー` | |
| Brakeman | `制動手` | |
| Fireman | `機関助士` | |
| Conductor | `車掌` | |
| Engineer | `運転士` | |
| Freight Agent | `貨物係` | |
| Pyrotechnist | `花火師` | |
| Early Bird | `早起き` | |
| Big 'Fella | (proper noun - keep EN) | |
| Shareholder | `株主` | (`Actionnaire` in FR) |

### 9.5. JP UI constraints (width)

**Key rule**: 1 CJK character ≈ 2 Latin characters wide. EN/2 budgets are a good
approximation.

| Field | EN budget | Recommended JP budget | Proposal |
|---|---|---|---|
| Shop UPGRADES | ≤ 8 EN | ≤ 4 CJK | `強化` (2) |
| Shop FLAGS | ≤ 8 EN | ≤ 4 CJK | `フラッグ` (4) or `旗` (1) |
| Shop PAINT | ≤ 8 EN | ≤ 4 CJK | `塗装` (2) |
| Shop BUY | ≤ 8 EN | ≤ 4 CJK | `購入` (2) |
| Shop COST | ≤ 8 EN | ≤ 4 CJK | `価格` (2) |
| Turn Left | ≤ 9 EN | ≤ 5 CJK | `左へ` (2) |
| Turn Right | ≤ 10 EN | ≤ 5 CJK | `右へ` (2) |
| Speed Up | ≤ 8 EN | ≤ 4 CJK | `加速` (2) |
| Slow Down | ≤ 9 EN | ≤ 5 CJK | `減速` (2) |
