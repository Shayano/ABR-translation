# Règles de traduction A Bumpy Ride — réutilisables pour toutes langues

Document de référence consolidé. Toute nouvelle traduction (FR, ES, DE, etc.)
doit suivre ces règles. Si une règle n'est pas claire, demander **avant** de
traduire — pas après.

---

## 1. Philosophie générale

**Public cible : grand public, pas passionnés.**
A Bumpy Ride est un jeu d'aventure/casual sur les trains, pas un simulateur
pour ferrovipathes. Préférer des termes descriptifs et compréhensibles plutôt
que des termes techniques corrects mais obscurs.

| EN | À éviter (technique) | À utiliser (descriptif) |
|---|---|---|
| Tender | `Tender` (tel quel) | `wagon-réservoir` ou `réservoir` (court) |
| Caboose | (technique) | `fourgon de queue` |
| Throttle | — | `régulateur` (intuitif en contexte vapeur) |
| Switch / switchstand | — | `aiguillage` / `levier d'aiguillage` |

**Règle générale** : si un joueur lambda non-passionné ne comprend pas, reformuler.

---

## 2. Ce qu'on ne traduit JAMAIS

### 2.1. Noms propres (universel)

**Tous les noms propres restent tels quels en EN**, sans exception, dans tous
les contextes. Ils représentent l'identité visuelle/narrative du jeu.

- **Skins de trains** : Comet, Forgotten, Bilge Rat, Bolt, Big Dipper, Shimmer,
  Theodore, Lavish, Stockton, Delta, Movie Star, Dayton, Zuma, Huntie, Courage,
  Rover, Hidden Rose, Little Engine, Bootlegger, Conrad, Very Useful, Jupiter,
  Voyager, Lil' Belle, Neko Neon, Spirit, Sweet Toot, Texas, General,
  Sacramento — **tous gardés en EN**.
- **Skins de personnages** : Margot, Pacifica, Theodore, etc. — gardés en EN.
- **Noms de stations/gares** : Misty Creek, Cattail, Birchwood, Snowball, Aurora,
  Pinecone, Dustbowl, Chugwater, Bloomwater, Colby, Nowhere, Pearbury, Bumblebee,
  Blowhistle, Sugarcube, Eagle Nest, Seaside, etc. — gardés en EN.
- **Noms de régions/zones découvertes** (AreaDiscovered.uasset) : WHISTLING
  PEAKS, LILLI FOREST, RIO FRONTERA, PUT PRAIRIE — gardés en EN.
- **Auteurs/contributors** dans Basic_Credits.uasset : tous gardés en EN
  (Nathaniel Onandia, Harrison Hudson, Bobenny, Kobold, Eddie Sand, RWD, etc.).

### 2.2. Enseignes de magasins / décors urbains (.umap _SubLvl)

**Toutes les enseignes de magasins et bâtiments du décor restent en EN.**
Cela couvre les fichiers `Frst_*_SubLvl.umap`, `Snw_*_SubLvl.umap`,
`Dsrt_*_SubLvl.umap`, `Plns_*_SubLvl.umap`.

Raison : ces enseignes font partie de l'identité visuelle de chaque ville
(ambiance western/époque ~1900). Traduire `GROCERY` → `ÉPICERIE` casse
l'ambiance et mélange noms propres et descriptifs sur les façades.

Couvre aussi bien les noms propres (`MAXIMILLIAN'S`, `STEVENS & SONS`,
`HUDSON ENGINE CO.`, `HARVEY'S`, `KIMBALL FIRE CO.`) que les descriptifs
génériques (`GROCERY`, `BAKERY`, `RESTAURANT`, `HARDWARE`, `POST OFFICE`,
`INSURANCE`, `BAKED GOODS`, `MEAT & CHEESE`, `MUSIC, DANCING & FOOD`,
`CONFECTIONARY`, `DRESSMAKER`, `MILLINERY`, `HABERDASHERY`, `ARCADE`, etc.).

Aussi : fragments décoratifs (`KE`, `ba`, `ry` — morceaux d'une grosse
enseigne) → laisser vide.

### 2.3. `On` / `Off` (toggles UI)

**Ne jamais traduire `On`, `Off`, `on`, `off`** dans aucun contexte.

Raisons :
1. **Cohérence cross-asset** : déjà laissés en EN ailleurs, on ne crée pas un mix.
2. **Contraintes de largeur UI** : `Activé`/`Désactivé` ne rentrent pas dans les
   cases dimensionnées pour 2-3 lettres.

Cas connus : `WeatherToggle.uasset`, `New_SettingsMenu.uasset`.

### 2.4. Unités de mesure

**Ne jamais convertir les valeurs ni l'abréviation d'unité.** Le jeu utilise
le système impérial (FT, miles) — c'est un choix de design assumé.

Le **label** (mot anglais désignant la grandeur) peut et doit être traduit
normalement ; seules les valeurs numériques et l'abréviation d'unité restent
intactes :

- `ELEVATION: 624 FT` → `ALTITUDE : 624 FT` ✅ (label traduit, unité conservée)
- `ELEVATION: 624 FT` → `ALTITUDE : 190 m` ❌ (conversion interdite)
- `Distance Traveled: 50 Miles` → `Distance parcourue : 50 Miles` ✅
- `Distance Traveled: 50 Miles` → `Distance parcourue : 80 Km` ❌

### 2.5. Internes techniques (jamais affichés au joueur)

À garder vide systématiquement — ce sont des labels internes, pas du contenu user-facing.

- **Tags HTML/RichText** : `<Shakey>`, `</>`, `<cf>`, etc.
- **Asset paths** : `Map.png`, etc.
- **Commandes console UE** : `r.ScreenPercentage `, `r.*`, etc.
- **Messages debug** : `MultiGate Node failed! Out of bounds indexing of the out pins...`
- **Placeholders UMG par défaut** : `Text`, `Text Block`, `Pop up text`
  (le widget les remplace au runtime).
- **Tags d'effets** : `TNTShake`, etc.
- **Labels enum internes** : ex `BP_Roundhouse_Engines.uasset` —
  `Both In And On`, `Right Out, Left In, Both On` — non visibles en jeu.

### 2.5.ter. SpecialPassenger.uexp : BP intraduisible (cause de crash récursion infinie)

**`SpecialPassenger.uasset/uexp` doit rester vanilla.** Toute tentative de patche
de ses 72 (62 traduisibles) strings cause une **récursion infinie côté UE5**
(`Infinite script recursion detected at 90 calls`) qui plante le jeu à la pickup
de certaines tâches d'Actionnaire.

**Symptôme observé** : crash non-déterministe au pickup d'Actionnaire selon la
tâche affichée. Certaines tâches passent, d'autres font crasher systématiquement.

**Diagnostic effectué (v1.4.4)** :
- Full minidump (9 GB) montre 90 récursions `ProcessInternal` dans la VM UE5
- Message d'erreur trouvé en mémoire : `"Infinite script recursion ({0} calls) detected"`
- Bissection rigoureuse : SP isolé comme coupable parmi 30+ BPs candidats

**Méthodes de patche testées qui ÉCHOUENT toutes** :
- `KissE.exe` (v1.4.0 original) : casse un EX_Jump par change-of-length
- `BPStringPatcher.exe` (placeholder+branch, normalement safe) : casse aussi malgré la technique

**Hypothèse** : la complexité du bytecode de SpecialPassenger (62 strings dans de
nombreuses branches conditionnelles d'évaluation de type de tâche) crée des
patterns que ni KissE ni BPStringPatcher ne gèrent correctement.

**Conséquence côté JSON** : les 62 traductions de tâches restent dans
`staging/fr_strings_BP_translated.json` et les JSONs DE/ES mais ne sont **pas
appliquées au build**. SpecialPassenger.uasset/uexp est livré vanilla dans
`patch-fr/patched_assets/` et `patch-de/patched_assets/`.

**Strings concernées** (62 en FR/DE) : "See the sunset", "Stay aboard until 9PM",
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

**Voies de recherche futures** : approche manuelle de patche binaire ciblé
(byte-by-byte sur chaque EX_StringConst sans toucher au bytecode autour),
ou attendre une nouvelle version d'UE5 / KissE qui gère mieux ce pattern.

### 2.5.bis. Identifiants UMG dans la NameMap (CRITIQUE, cause de crash)

**Certaines strings du JSON `*_strings_BP_translated.json` correspondent en réalité à
des noms d'identifiants UMG** (sub-widgets, animations Widget, classes UE5) stockés
dans la NameMap du `.uasset`. KissE les traite comme des FText et les remplace, ce
qui casse les appels runtime `FindChildWidget("Lock_Panel")` / `PlayAnimation("Pulsate")`
et déclenche une **EXCEPTION_ACCESS_VIOLATION** quand le widget est instancié.

Détecté pour la première fois sur la quête Shareholder (Actionnaire) en v1.4.0 →
hotfix v1.4.1. Cinq Blueprints contiennent des strings qu'il ne faut PAS traduire :

| Blueprint | Strings interdites | Pourquoi |
|---|---|---|
| `W_WonStocks.uasset` | `Float` | Nom d'animation Widget (`Float_INST`) + type UE5 (`MovieSceneFloatTrack`) |
| `NPCPointer.uasset` | `Pulsate` | Nom d'animation Widget (`Pulsate_INST`) |
| `QuestBoard.uasset` | `Lock` | Nom d'animation (`Lock_INST`) + sub-widget (`Lock_Panel`) + texture refs (`LockIcon_Locked`, `LockIcon_Unlocked`) |
| `QuestTicket.uasset` | `Quest 1`, `Quest 2`, `Quest 3` | Noms de sub-widgets UMG (`Quest 1 check`, `Quest 1 Text`, etc., qui contiennent les objectifs de quête) |
| `PopUp.uasset` | `Unlocked Item`, `Unlocked Text` | Noms de sub-widgets (`UnlockedImage`, `UnlockedItem`, `UnlockedText`) |

**Conséquence côté JSON** : ces 8 entrées ont été retirées de
`staging/fr_strings_BP_translated.json`, `translations/de/strings_BP.json` et
`translations/es/strings_BP.json` à la v1.4.1. Ne pas les rajouter.

**Conséquence côté patch** : les 5 fichiers `.uasset/.uexp` correspondants sont
livrés en VANILLA dans `patch-fr/patched_assets/` et `patch-de/patched_assets/`.

**Comment détecter ce type de string dans le futur** :
1. Avant de patcher un nouveau `.uasset`, dumper les entrées de sa NameMap.
2. Pour chaque `Original` à patcher, vérifier s'il apparaît **isolé** dans la
   NameMap (= entrée standalone, pas FText avec namespace+KeyValue).
3. Si oui, c'est probablement un identifiant. Tester in-game avant de shipper —
   en particulier sur les widgets qui ont des animations (`*_INST`) ou des
   sub-widgets nommés.
4. Attention : tous les "isolated NameMap entries" ne crashent pas. Beaucoup
   sont aussi utilisés comme FText (Speed Up, Slow Down, Awards, Quit, Settings,
   etc. ont tous validé des heures de gameplay malgré le pattern). L'audit
   statique est un INDICE, pas une preuve — confirmer par bissection en jeu.

### 2.6. Auteurs et références dans DataTables

Crédits, contributors, références studio/historiques → toujours en EN.

---

## 2.7. Tutoiement vs vouvoiement

**Décision par langue** (option B confirmée 2026-05-04) : chaque langue
suit sa convention casual gaming naturelle. Les langues peuvent diverger
sur le registre sans que ce soit une incohérence — c'est ce que font les
localisations pro.

| Langue | Registre game-to-player | Justification |
|---|---|---|
| FR | **tutoiement** (`tu`) | Convention casual gaming FR pour jeu cosy famille. Bascule effectuée 2026-05-07 sur 45 strings BP (Tutorial_Table, NewShopMenu, StaffBoard, AreYouSure, PlayerTrain, etc.) pour cohérence avec les enums déjà en `tu`. **Exception** : les *catch phrases diégétiques* du title screen (`enum_titleblurbs_fr.json`) où un PNJ s'adresse formellement à des voyageurs gardent leur vouvoiement (`Vos tickets, s'il vous plaît ?` = contrôleur, `Votre attention s'il vous plaît...` = annonce gare, `VOUS. NE. PASSEREZ. PAS !!` = citation VF Gandalf). |
| ES | **tutoiement** (`tú`) | Convention casual gaming en ES. `usted` réservé aux contextes formels. |
| DE | **`du` informel** (confirmé 2026-05-05) | Convention casual gaming DE pour jeu famille. `Sie` réservé aux jeux corporate/serious. |
| JP | **ですます調 (forme polie standard)** (confirmé 2026-05-16) | Pas de tu/vous formel en JP. ですます調 est la norme casual gaming friendly (équivalent du `du` allemand : poli mais pas distant). 敬語 (keigo formel humble/respectueux) évité car trop solennel. Style enjoué : exclamations 「！」 OK, particules finales 「ね」「よ」 acceptées dans les dialogues PNJ. Les ordres UI courts utilisent souvent la forme nominale ou impérative neutre (`購入` plutôt que `購入してください`). |
| ZH | (à décider) | Pas de marque grammaticale équivalente. |

---

## 3. Ce qu'on traduit normalement

- Dialogues, narration, didactiques, tutoriels.
- Descriptions d'items, blurbs, types (de fret, quêtes, passagers, bâtiments).
- Libellés UI génériques (Cancel, Confirm, Apply, Settings, Options, etc. —
  cf. `staging\translations_dict.ps1`).
- Statistiques (Distance Traveled, Passengers Delivered, etc.).
- Boutons d'action.
- Messages de jeu (achievements, notifications, pop-ups).

---

## 4. Encoding

**Forcer UTF-16 LE (`Encoding.Unicode`) sur toute FString contenant des
caractères > 127** (accents : é, à, ô, ù, etc.).

Si UAssetAPI écrit du UTF-8 dans un slot dimensionné ASCII, le jeu crash.
Cf. `memory/reference_uassetapi_text_encoding.md`.

---

## 5. Cohérence inter-langues

Pour traduire vers une nouvelle langue (ES, DE, etc.) :

1. **Réutiliser ces règles intégralement** — la liste de "ne jamais traduire"
   est universelle (noms propres, enseignes, unités, On/Off, internes).
2. Construire un dico équivalent à `staging\translations_dict.ps1` pour la
   nouvelle langue.
3. Adapter le lexique grand public à la culture cible (un terme technique
   pénible en FR peut être clair en DE et inversement).
4. Tester en jeu et confirmer les choix avec l'utilisateur — chaque langue
   peut ajouter ses propres règles spécifiques.

---

## 6. Confirmations cumulées (historique des décisions user)

- **2026-05-04** : skins de trains/personnages = noms propres → EN.
- **2026-05-04** : noms de stations = noms propres → EN.
- **2026-05-03** : ne pas garder le jargon ferroviaire (Tender, etc.).
- **2026-05-04** : `On`/`Off` jamais traduits (cohérence + largeur UI).
- **2026-05-04** : noms de régions/zones découvertes = noms propres → EN.
- **2026-05-04** : enseignes de magasins (toutes les `.umap _SubLvl`) → EN
  (ambiance western/époque + noms propres).
- **2026-05-04** : pas de conversion d'unités (FT, miles restent tels quels).
- **2026-05-04** : auteurs/credits → EN.
- **2026-05-04** : vouvoiement systématique en FR (cohérence avec trads existantes).
- **2026-05-04** : option B sur le registre — décision par langue, pas d'imposition cross-langue. ES sera en `tú` (convention casual gaming) malgré le FR en `vous`.
- **2026-05-05** : DE confirmé en `du` informel (jeu famille).
- **2026-05-05** : contraintes de longueur UI à respecter pour DE (et toute langue ≥ EN en moyenne) — voir section 7 ci-dessous.
- **2026-05-15** : hotfix v1.4.1 — 8 strings retirées (Float, Pulsate, Lock, Quest 1/2/3, Unlocked Item/Text) car identifiants UMG dans la NameMap, traduction → crash au pickup Actionnaire. Cf. section 2.5.bis.
- **2026-05-15** : hotfix v1.4.4 — `SpecialPassenger.uexp` reste vanilla (62 strings de tâches d'Actionnaire en EN). Ni KissE ni BPStringPatcher ne le patche sans casser un EX_Jump dans une branche conditionnelle profonde. Diagnostic via full minidump (9 GB) + bissection rigoureuse. Cf. section 2.5.ter.
- **2026-05-16** : ouverture de la traduction JP (japonais). Registre : ですます調 (poli standard, équivalent du `du` DE). Pas de conversion d'unités (FT/miles intacts), noms propres EN, On/Off EN, enseignes western EN. Encoding UTF-16 LE obligatoire (caractères CJK). Contraintes UI revisitées : 1 caractère CJK ≈ 2 caractères latins de largeur, donc budgets EN/2 environ.

---

## 7. Contraintes de longueur UI (par langue)

L'allemand est en moyenne **30-40% plus long** que l'anglais. Plusieurs
champs ont une largeur fixe à l'écran et ne tolèrent pas l'expansion.
Ces budgets sont **stricts** : si la trad dépasse, il faut reformuler ou
abréger jusqu'à rentrer.

### 7.1. Onglets / catégories du shop (≤ 8 caractères)

`NewShopMenu.uasset` — boutons d'onglet de la vitrine du shop.

| EN | FR (référence) | Budget DE | Proposition DE |
|---|---|---|---|
| `UPGRADES` | `AMÉLIO.` (7) | **≤ 8** | `UPGRADES` (8) ou `TUNING` (6) |
| `FLAGS` | `DRAPEAUX` (8) | **≤ 8** | `FLAGGEN` (7) ou `FAHNEN` (6) |
| `PAINT` | `COULEURS` (8) | **≤ 8** | `FARBEN` (6) |
| `BUY` | `ACHETER` (7) | **≤ 8** | `KAUFEN` (6) |
| `COST` | `COÛT` (4) | **≤ 8** | `KOSTEN` (6) ou `PREIS` (5) |

### 7.2. Menu principal — directions / raccourcis (≤ longueur EN)

`IA_TurnLeft.uasset`, `IA_TurnRight.uasset`, `IA_SpeedUp.uasset`,
`IA_SlowDown.uasset`, etc. — descriptions des actions de direction
affichées dans la barre d'aide / le menu de raccourcis.

| EN | Longueur EN | FR (référence) | Budget DE |
|---|---|---|---|
| `Turn Left` | 9 | `à gauche` (8) | **≤ 9** → `Links` (5) |
| `Turn Right` | 10 | `à droite` (8) | **≤ 10** → `Rechts` (6) |
| `Speed Up` | 8 | (à voir FR) | **≤ 8** → `Schneller` (9) NON, `Beschl.` (7) ou `Schneller` reformulé |
| `Slow Down` | 9 | (à voir FR) | **≤ 9** → `Bremsen` (7) |

**Règle** : si une direction/action ne tient pas dans la longueur EN, abréger
ou choisir un synonyme court. Ne **jamais** dépasser — la barre d'aide tronque.

### 7.3. Settings menu — labels d'options

`New_SettingsMenu.uasset` — cases de paramètres. La largeur est généreuse
(le FR a pu mettre `Sensibilité caméra` 18 chars), mais certaines cases
sont restreintes :

| EN | FR (référence) | Constat |
|---|---|---|
| `Auto Board` | `Embarq. auto` (12) | abrégé en FR |
| `Reset` | `Réinit.` (7) | abrégé en FR (case étroite) |
| `Saves & Backups` | `Sauvegardes` (11) | simplifié en FR |
| `Tender Icon` | `Icône réservoir` (15) | OK |

**Règle DE** : si la trad littérale dépasse, abréger avec un point (`Einst.`,
`Zurücks.`) ou choisir un terme plus court. Tester en jeu avant de finaliser.

### 7.4. Méthode de validation

Pour chaque chaîne sous contrainte, ajouter dans le JSON DE un commentaire
(`_max_chars`) ou utiliser le fichier `translations/de/_budget_chars.json`
qui mappe `KeyValue` → `max_chars`. Un linter peut alors vérifier
que `len(NewValue) <= max_chars` avant patch.

---

## 8. Glossaire DE (allemand)

Dictionnaire de termes récurrents pour cohérence inter-asset. À enrichir
au fil de la traduction.

### 8.1. Termes ferroviaires (grand public, pas jargon)

| EN | DE recommandé | Notes |
|---|---|---|
| Train | `Zug` | universel |
| Engine / Locomotive | `Lokomotive` (long) ou `Lok` (3) | `Lok` dans contraintes serrées |
| Tender | **`Wassertank`** (réservoir d'eau, 10) ou **`Tank`** (4, court) | Ne **PAS** garder `Tender` tel quel — c'est aussi un terme ferroviaire technique en DE. Cohérent avec la philosophie grand public (FR utilise `réservoir`). |
| Caboose | `Schlusswagen` ou `Bremserwagen` | ou simplement `Wagen am Ende` |
| Cupola | `Aussichtskuppel` (15) ou `Kuppel` (6) | |
| Gondola | `Flachwagen` (10) ou `Plattformwagen` | |
| Track | `Gleis` / `Schiene` | |
| Switch / switchstand | `Weiche` / `Weichenhebel` | |
| Whistle | `Pfeife` (verbe : `pfeifen`) | |
| Brake / Brakeman | `Bremse` / `Bremser` | |
| Conductor | `Schaffner` | |
| Fireman | `Heizer` | |
| Throttle | `Regler` ou `Drossel` | |
| Coal | `Kohle` | |
| Steam | `Dampf` | |
| Water tower | `Wasserturm` | |
| Freight | `Fracht` | |

### 8.2. UI / boutons / actions

| EN | DE recommandé | Longueur |
|---|---|---|
| Apply | `Anwenden` | 8 |
| Cancel | `Abbrechen` | 9 |
| Confirm | `Bestätigen` | 10 |
| Reset | `Zurücks.` ou `Reset` (anglicisme) | 8 / 5 |
| Close | `Schließen` | 9 |
| Back | `Zurück` | 6 |
| Return | `Zurück` | 6 |
| Settings | `Einstellungen` | 13 |
| Options | `Optionen` | 8 |
| Audio | `Audio` | 5 |
| Graphics | `Grafik` | 6 |
| Gameplay | `Gameplay` (anglicisme courant) | 8 |
| Controls | `Steuerung` | 9 |
| Credits | `Mitwirkende` ou `Credits` (anglicisme) | 11 / 7 |
| Tutorial | `Tutorial` | 8 |
| Quit | `Beenden` | 7 |
| Exit | `Verlassen` ou `Beenden` | 9 / 7 |
| Stats | `Statistik` | 9 |
| Map | `Karte` | 5 |
| Load | `Laden` | 5 |
| Save | `Speichern` | 9 |
| Delete | `Löschen` | 7 |
| Buy | `Kaufen` | 6 |
| Level | `Level` (anglicisme) ou `Stufe` | 5 |
| Day | `Tag` | 3 |
| Money | `Geld` | 4 |
| Yes | `Ja` | 2 |
| No | `Nein` | 4 |

### 8.3. Personnages / postes

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
| Big 'Fella | (nom propre — laisser EN ?) | |

### 8.4. Tonalité

- **Registre `du`** : tutoie le joueur partout (`du brauchst`, `dein Zug`, `dich`).
- **Pas de jargon** : préférer `schneller` à `Beschleunigung`, `bremsen` à `verzögern`.
- **Ton enjoué/casual** : exclamations OK (`Los geht's!`, `Klasse!`).
- **Anglicismes acceptés** quand ils sont courants en gaming DE (`Level`,
  `Upgrade`, `Gameplay`, `Reset`, `Highscore`).

---

## 9. Glossaire JP (japonais)

Dictionnaire de termes récurrents pour cohérence inter-asset. À enrichir
au fil de la traduction.

### 9.1. Conventions générales JP

- **Style** : ですます調 (forme polie standard, ni keigo formel ni langage familier).
- **Particules finales** dans dialogues PNJ : 「ね」「よ」「な」 OK pour ton enjoué.
- **Onomatopées** : adapter (`Hello` → `こんにちは` ou `やあ` selon contexte casual).
- **Ponctuation CJK** :
  - Préférer 「」 pour les guillemets, ！ et ？ full-width pour les exclamations
    dans les dialogues longs.
  - Pour les chaînes courtes UI : ponctuation minimale, half-width OK.
- **Nombres et unités** : half-width (ASCII) pour `624 FT`, `50 Miles`, `Day 3`
  (cohérent avec l'identité du jeu et lisibilité).
- **Noms propres** : restent en EN (cf. règle 2.1).
- **Anglicismes courants** acceptés en katakana quand l'usage gaming JP les a
  adoptés : アップグレード (Upgrade), レベル (Level), セーブ (Save), マップ (Map),
  メニュー (Menu), アイテム (Item).

### 9.2. Termes ferroviaires (grand public, pas jargon)

| EN | JP recommandé | Notes |
|---|---|---|
| Train | `列車` ou `汽車` | `列車` neutre, `汽車` pour vapeur old-school |
| Engine / Locomotive | `機関車` (3) | universel |
| Tender | `炭水車` (3) ou `タンク` (3) | Pas garder `Tender` tel quel (jargon). FR utilise `réservoir`, DE `Tank`. |
| Caboose | `車掌車` (3) | wagon de queue |
| Cupola | `展望台` ou `屋根の見張り` | |
| Gondola | `無蓋車` (3) | wagon ouvert |
| Track / Rail | `線路` (2) ou `レール` (3) | |
| Switch / switchstand | `分岐器` / `転てつ機` | technique ; préférer `ポイント` (katakana, courant) |
| Whistle | `汽笛` (2) (verbe `鳴らす`) | |
| Brake / Brakeman | `ブレーキ` / `制動手` | |
| Conductor | `車掌` (2) | |
| Fireman | `機関助士` (4) ou `火夫` (2) | `火夫` plus court mais vieilli |
| Engineer / Driver | `運転士` (3) | |
| Throttle | `スロットル` ou `加減弁` | |
| Coal | `石炭` (2) | |
| Steam | `蒸気` (2) | |
| Water tower | `給水塔` (3) | |
| Freight | `貨物` (2) | |
| Passenger | `乗客` (2) | |
| Station | `駅` (1) | (les noms restent EN : `Aurora 駅` non, juste `Aurora`) |

### 9.3. UI / boutons / actions

| EN | JP recommandé | Largeur (caractères CJK) |
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
| Audio | `オーディオ` ou `音声` | 5 / 2 |
| Graphics | `グラフィック` ou `画面` | 6 / 2 |
| Gameplay | `ゲームプレイ` | 6 |
| Controls | `操作` | 2 |
| Credits | `クレジット` | 5 |
| Tutorial | `チュートリアル` | 7 |
| Quit | `終了` | 2 |
| Exit | `戻る` ou `終了` | 2 |
| Stats | `統計` | 2 |
| Map | `マップ` | 3 |
| Load | `ロード` | 3 |
| Save | `セーブ` | 3 |
| Delete | `削除` | 2 |
| Buy | `購入` | 2 |
| Level | `レベル` | 3 |
| Day | `日目` ou `Day` | 2 |
| Money | `お金` | 2 |
| Yes | `はい` | 2 |
| No | `いいえ` | 3 |
| Pause | `一時停止` ou `ポーズ` | 4 / 3 |
| Resume | `再開` | 2 |
| Upgrade | `アップグレード` ou `強化` | 7 / 2 |
| Paint | `塗装` | 2 |
| Flags | `フラッグ` ou `旗` | 4 / 1 |
| Cost | `価格` | 2 |
| Unlock | `解放` | 2 |
| Locked | `ロック中` | 4 |
| Unlocked | `解放済み` | 4 |
| Skip | `スキップ` | 4 |
| Continue | `続ける` | 3 |
| Start | `スタート` ou `開始` | 4 / 2 |
| Reach | `達成` | 2 |
| Complete | `完了` | 2 |

### 9.4. Personnages / postes

| EN | JP | Notes |
|---|---|---|
| Bouncer | `用心棒` | sécurité |
| Bartender | `バーテンダー` | |
| Brakeman | `制動手` | |
| Fireman | `機関助士` | |
| Conductor | `車掌` | |
| Engineer | `運転士` | |
| Freight Agent | `貨物係` | |
| Pyrotechnist | `花火師` | |
| Early Bird | `早起き` | |
| Big 'Fella | (nom propre — laisser EN) | |
| Shareholder | `株主` | (`Actionnaire` en FR) |

### 9.5. Contraintes UI JP (largeur)

**Règle clé** : 1 caractère CJK ≈ 2 caractères latins de largeur.
Les budgets EN/2 sont une bonne approximation.

| Champ | Budget EN | Budget JP recommandé | Proposition |
|---|---|---|---|
| Shop UPGRADES | ≤ 8 EN | ≤ 4 CJK | `強化` (2) |
| Shop FLAGS | ≤ 8 EN | ≤ 4 CJK | `フラッグ` (4) ou `旗` (1) |
| Shop PAINT | ≤ 8 EN | ≤ 4 CJK | `塗装` (2) |
| Shop BUY | ≤ 8 EN | ≤ 4 CJK | `購入` (2) |
| Shop COST | ≤ 8 EN | ≤ 4 CJK | `価格` (2) |
| Turn Left | ≤ 9 EN | ≤ 5 CJK | `左へ` (2) |
| Turn Right | ≤ 10 EN | ≤ 5 CJK | `右へ` (2) |
| Speed Up | ≤ 8 EN | ≤ 4 CJK | `加速` (2) |
| Slow Down | ≤ 9 EN | ≤ 5 CJK | `減速` (2) |
