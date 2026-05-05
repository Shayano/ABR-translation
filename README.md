# A Bumpy Ride — Traduction française (mod non-officiel)

Mod de traduction française pour [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), un jeu de simulation ferroviaire indé sur Steam.

**Version actuelle : 1.3.0** (5 mai 2026)
**Moteur du jeu : Unreal Engine 5.3.2 (IoStore)**

> Ce mod n'est ni développé ni soutenu par les créateurs du jeu. C'est un travail de fan, fourni en l'état.

---

## Ce qui est traduit

- L'intégralité de l'interface (menus, boutons, paramètres, raccourcis clavier)
- Les dialogues du tutoriel et de la carte principale (intro, événements, notifications)
- Tous les libellés de quêtes, de fret, de passagers et de bâtiments
- Les noms et descriptions des wagons et des skins (excepté noms propres conservés en VO)
- Les écrans de fin de journée, succès, statistiques

**Volontairement laissé en anglais** (par cohérence avec l'ambiance du jeu) :
- Noms propres : skins (Lavish, Stockton, Dayton…), stations, régions, auteurs des crédits
- Enseignes des magasins en pixel art (ambiance western 1900)
- `On` / `Off` (cohérence UI + contraintes de largeur des cases)
- Unités impériales (FT, miles)

---

## Installation

Deux modes selon votre plateforme.

### 🪟 Windows (recommandé)

Utilise un installeur PowerShell qui dérive le patch à partir des fichiers vanilla de votre installation. Sécurisé (backup automatique du jeu original) et léger à télécharger (~67 Mo).

1. Téléchargez `ABR-fr_v1.3.0.zip` (cf. section [Téléchargements](#téléchargements))
2. Extrayez-le n'importe où sur votre PC
3. **Fermez le jeu si ouvert**, puis double-cliquez sur `install.ps1`
   - Si Windows bloque l'exécution : clic droit → *Exécuter avec PowerShell*
   - Si SmartScreen avertit : *Plus d'infos* → *Exécuter quand même*
4. L'installeur détecte automatiquement le jeu via Steam et patche en place
5. Comptez 4–5 min sur SSD, ~12 Go d'espace temporaire

Pour désinstaller : exécutez `uninstall.ps1` (restaure depuis le backup automatique `_ABRfr_backup`).

### 🎮 Steam Deck / Linux

Sur Steam Deck (et systèmes Linux en général), l'installeur PowerShell ne tourne pas. Utilisez le zip pré-patché : c'est un drop-in direct, plus simple mais ~3 Go à télécharger.

1. Téléchargez `ABR-fr_v1.3.0_prepatched.zip` (cf. section [Téléchargements](#téléchargements))
2. Localisez le dossier Paks de votre installation :
   - Steam Deck : `~/.steam/steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
3. Sauvegardez les 3 fichiers vanilla dans un sous-dossier `_backup_vanilla` :
   ```
   ABumpyRide-Windows.utoc
   ABumpyRide-Windows.ucas
   ABumpyRide-Windows.pak
   ```
4. Extrayez les 3 fichiers du zip dans le dossier `Paks/` (à côté du `_backup_vanilla`)
5. Lancez le jeu via Steam normalement

Pour désinstaller : supprimez les 3 fichiers patchés et remettez ceux de `_backup_vanilla`.

> Note technique : le `.ucas` patché fait ~5,2 Go (vs 1,6 Go vanilla) car le pipeline de génération ne re-compresse pas avec Oodle. C'est fonctionnel, juste plus lourd sur disque.

---

## Téléchargements

Allez voir l'onglet **Releases** de ce dépôt pour télécharger les zips :

- `ABR-fr_v1.3.0.zip` (~67 Mo) — pour Windows
- `ABR-fr_v1.3.0_prepatched.zip` (~3 Go) — pour Steam Deck / Linux

---

## Compatibilité

| Aspect | Statut |
|---|---|
| Version du jeu | A Bumpy Ride au 5 mai 2026 (Steam app id `2540610`) |
| Sauvegardes | Compatibles, le mod est entièrement réversible |
| Multijoueur | Pas de multi dans ABR — non concerné |
| Saison/DLC | Aucun pour le moment |
| Mise à jour du jeu | À chaque patch officiel, le mod doit être réinstallé (sinon le jeu peut crasher) |

---

## Problèmes connus

- **Le jeu crashe au lancement après l'install** : votre version du jeu est probablement plus récente que celle ciblée par le mod. Désinstallez via `uninstall.ps1` (Windows) ou en restaurant depuis `_backup_vanilla` (Steam Deck), puis attendez une mise à jour du mod.
- **Certains textes restent en anglais** : ce sont probablement des noms propres conservés volontairement (skins, stations, régions). Si c'est un texte d'interface non traduit, [ouvrez une issue](#) avec une capture d'écran.
- **Caractères bizarres (ä, õ, etc.)** au lieu d'accents corrects : signe d'un encodage cassé. Réinstallez le mod ; sur Steam Deck, vérifiez que le zip a été extrait sans corruption.

---

## Crédits & remerciements

- **Mod** : Shayano
- **Outils** :
  - [retoc-rivals](https://github.com/natimerry/repak-rivals) — repackager IoStore UE5.3
  - [KissE / KismetEditor](https://github.com/SolicenTEAM/KismetEditor) (fork patché par Shayano) — patcher de bytecode Blueprint
  - [Dumper-7](https://github.com/Encryqed/Dumper-7) — génération du `.usmap` du jeu
  - [UAssetAPI](https://github.com/atenfyr/UAssetAPI) — manipulation des assets UE
- **Méthodologie** : développé en pair-programming avec Claude Code (Anthropic) sur ~10 sessions.

---

## Licence

Ce mod est fourni gratuitement, sans garantie, en l'état. Les assets traduits dérivent du jeu original (propriété de ses auteurs) — la traduction française est libre d'usage personnel.

Pas de redistribution commerciale.
