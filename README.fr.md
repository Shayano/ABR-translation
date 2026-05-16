# A Bumpy Ride - Traduction française (mod non-officiel)

> 🌍 **Autres langues** : voir [README.md](README.md) pour la liste complète des traductions disponibles.

Mod de traduction française pour [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), un jeu de simulation ferroviaire indé sur Steam.

**Version actuelle : 1.4.6** (16 mai 2026)
**Moteur du jeu : Unreal Engine 5.3.2 (IoStore)**

> 🆕 **v1.4.6** : récupération des 62 objectifs de tâches d'Actionnaire qui étaient restés en anglais depuis v1.0 à cause du crash récursion infinie. Nouveau patcher custom `BPOffsetPatcher` qui résout deux problèmes cumulés qu'aucun outil existant ne traitait : (1) les offsets internes des statements déplacés à la fin du bytecode et (2) les `EX_IntConst` d'entry-point hardcodés dans les 47 callers internes du Blueprint. Au menu côté joueur : `Voir le coucher de soleil`, `Rester à bord jusqu'à 21h`, `Éviter le désert entre 4h et 18h`, `Récupérer du miel : 0/3`, `Visiter le grand arbre spot photo`, etc. Reformulation grammaticale des fragments fret (`Récupérer du poires` → `Récupérer des poires`). Reste 2 strings `AM`/`PM` en anglais (bug doublons mineur, non bloquant).

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

Le mod est distribué sous deux formats au choix :
- **Installeur Windows** (`ABR-fr_v1.4.6.zip`, ~30-100 Mo) : installeur PowerShell qui détecte Steam automatiquement, ~3-5 min
- **Drop-in prepatché** (`ABR-fr_v1.4.6_prepatched.zip`, ~1,9 Go) : remplacement direct des fichiers de container, tout OS (Windows / Linux / Steam Deck / macOS), sans installeur

### Étapes (drop-in prepatché)

1. Téléchargez `ABR-fr_v1.4.6_prepatched.zip` (cf. [Releases](../../releases))
2. **Fermez le jeu** s'il est ouvert
3. Localisez le dossier `Paks` de votre installation A Bumpy Ride :
   - **Windows**   : `<bibliothèque Steam>\steamapps\common\A Bumpy Ride\ABumpyRide\Content\Paks\`
   - **Steam Deck**: `~/.steam/steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
   - **Linux**     : `~/.local/share/Steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
4. Extrayez le zip dans ce dossier `Paks/`. Trois fichiers existants vont être remplacés :
   ```
   ABumpyRide-Windows.utoc
   ABumpyRide-Windows.ucas
   ABumpyRide-Windows.pak
   ```
   Pas besoin de sauvegarder les originaux : Steam peut les restaurer à tout moment (cf. désinstallation).
5. Lancez le jeu via Steam normalement. Le menu doit être en français.

> Note technique : le `.ucas` patché fait ~5,2 Go (vs ~1,6 Go vanilla) parce que le pipeline de génération ne re-compresse pas avec Oodle. C'est fonctionnel, juste plus lourd sur disque.

---

## Désinstallation / retour à la version originale

Pas besoin de gérer manuellement un backup. Steam sait restaurer les fichiers vanilla en une étape :

1. Dans la bibliothèque Steam, **clic droit sur A Bumpy Ride** → *Propriétés*
2. *Fichiers installés* → **Vérifier l'intégrité des fichiers du jeu**
3. Steam détecte que les 3 fichiers sont modifiés et les re-télécharge (~1,6 Go)
4. Au prochain lancement, le jeu est en anglais, comme à l'origine

Cette même méthode fonctionne en cas de problème : si le mod casse quelque chose, lance une vérification d'intégrité et tu reviens à un état propre sans avoir à fouiller dans les dossiers.

---

## Compatibilité

| Aspect | Statut |
|---|---|
| Version du jeu | A Bumpy Ride au 12 mai 2026 - dernière mise à jour Steam ciblée (Steam app id `2540610`) |
| Sauvegardes | Compatibles, le mod ne touche à aucun fichier de save |
| Multijoueur | Pas de multi dans ABR - non concerné |
| Mise à jour du jeu | À chaque patch officiel du jeu, il faudra réinstaller la version à jour du mod (sinon le jeu peut crasher au lancement) |
| Cohabitation FR/DE/ES | Un seul container `.ucas` actif à la fois - pour changer de langue, désinstaller l'un (vérification d'intégrité Steam) puis installer l'autre |

---

## Problèmes connus

- **Le jeu crashe au lancement après l'install** : votre version du jeu est probablement plus récente que celle ciblée par le mod. Lancez une vérification d'intégrité Steam pour revenir au vanilla, et attendez une mise à jour du mod.
- **Certains textes restent en anglais** : ce sont probablement des noms propres conservés volontairement (skins, stations, régions). Si c'est un texte d'interface non traduit, [ouvrez une issue](../../issues) avec une capture d'écran.
- **Caractères bizarres (ä, õ, etc.) au lieu d'accents corrects** : signe d'une corruption à l'extraction du zip. Re-téléchargez et ré-extrayez avec un outil qui gère bien les fichiers volumineux (7-Zip, l'outil intégré Windows 10/11, Ark sur Steam Deck).
- **Quelques mots restent en anglais sur le QuestBoard et le ticket de quête** : `Lock` sur le bouton cadenas en haut du tableau, `DESTINATION:` sur le ticket de quête latéral. Ce sont des identifiants internes UMG (les sous-composants graphiques du widget) qui causaient un crash s'ils étaient traduits. Limitation connue en v1.4.6, à corriger dans une future version via une approche alternative.
- **2 strings `AM`/`PM` (sur le 9PM / 9AM des tâches Actionnaire) restent en anglais** : bug doublons dans le nouveau patcher (la 2e occurrence de chaque doublon est ignorée). Non bloquant - "Rester à bord jusqu'à 21h" reste lisible avec un "AM" anglais à côté du compteur. Sera corrigé dans une future version mineure.

---

## Crédits & remerciements

- **Mod** : Shayano
- **Outils utilisés pour le pipeline de patch** :
  - [retoc-rivals](https://github.com/natimerry/repak-rivals) - repackager IoStore UE5.3
  - [KissE / KismetEditor](https://github.com/SolicenTEAM/KismetEditor) - patcher de bytecode Blueprint
  - [Dumper-7](https://github.com/Encryqed/Dumper-7) - génération du `.usmap` du jeu
  - [UAssetAPI](https://github.com/atenfyr/UAssetAPI) - manipulation des assets UE
- **Méthodologie** : développé en pair-programming avec Claude Code (Anthropic) sur ~10 sessions.

---

## Licence

Ce mod est fourni gratuitement, sans garantie, en l'état. Les assets traduits dérivent du jeu original (propriété de ses auteurs) - la traduction française est libre d'usage personnel.

Pas de redistribution commerciale.
