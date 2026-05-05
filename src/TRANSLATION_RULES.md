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
| FR | **vouvoiement** (`vous`) | Convention déjà appliquée dans toutes les trads existantes (Tutorial_Table, NewShopMenu, StaffBoard, AreYouSure...). |
| ES | **tutoiement** (`tú`) | Convention casual gaming en ES. `usted` réservé aux contextes formels. |
| DE | (à décider) | Probable `du` informel pour un jeu famille. |
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
