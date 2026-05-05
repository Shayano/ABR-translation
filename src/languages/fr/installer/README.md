# A Bumpy Ride — Traduction française (mod ABR-fr)

Mod de traduction française non officiel pour **A Bumpy Ride**.

---

## Installation rapide

1. **Décompressez ce dossier** quelque part (par exemple sur votre Bureau).
2. **Faites un clic droit** sur `install.ps1` → **Exécuter avec PowerShell**.
3. L'installeur détecte automatiquement votre installation Steam et lance le
   processus. **Comptez 3 à 5 minutes**.
4. Lancez le jeu via Steam comme d'habitude.

> Si Windows bloque le script, ouvrez PowerShell en administrateur et tapez :
> ```
> Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
> ```
> puis relancez l'installeur.

---

## Mode dépôt manuel

Si la détection automatique échoue (Steam dans un emplacement inhabituel,
plusieurs bibliothèques, etc.), vous pouvez **déposer ce dossier `patch-fr`
directement dans votre dossier du jeu** :

```
F:\Steam\steamapps\common\A Bumpy Ride\patch-fr\install.ps1
```

L'installeur détecte automatiquement qu'il est lancé depuis le dossier du
jeu et procède sans demande de confirmation de chemin.

---

## Désinstallation

Lancez `uninstall.ps1` avec un clic droit → Exécuter avec PowerShell.
Le désinstalleur restaure les fichiers vanilla depuis la sauvegarde
`_ABRfr_backup/` créée à l'installation.

Si la sauvegarde n'existe plus, vous pouvez aussi **vérifier l'intégrité
des fichiers du jeu** depuis Steam :
*A Bumpy Ride > Propriétés > Fichiers installés > Vérifier l'intégrité*.

---

## Prérequis

- **Windows 10 ou 11**
- **PowerShell 5.1 ou supérieur** (préinstallé sur Windows 10/11)
- **Environ 12 Go d'espace libre** sur le lecteur où se trouve `%TEMP%`
  (utilisé temporairement par le pipeline d'installation, libéré ensuite)
- **Le jeu A Bumpy Ride installé via Steam**, dans sa version d'origine
  (le mod cible la version vanilla — si Steam a mis le jeu à jour, le mod
  peut être incompatible et nécessiter une mise à jour)

---

## En cas de problème

L'installeur affiche les messages en clair. Si quelque chose échoue :

- **« Le jeu installé ne correspond pas exactement aux fichiers vanilla »**
  → soit le mod est déjà installé, soit Steam a mis le jeu à jour. Essayez
  d'abord de **vérifier l'intégrité des fichiers** depuis Steam.

- **« Espace disque insuffisant »** → l'installeur a besoin de ~12 Go libres
  sur le lecteur de `%TEMP%`. Libérez de l'espace et relancez.

- **« retoc.exe introuvable »** → le dossier `patch-fr` n'a pas été
  décompressé en entier. Recommencez l'extraction.

- **« L'extraction du vanilla a produit trop peu d'assets »** →
  vérifiez l'intégrité du jeu via Steam.

- **Autre erreur** : copiez le message d'erreur et signalez-le à l'auteur
  du mod.

---

## Ce qui est traduit

- Tous les **dialogues de tutoriel**, descriptions de quêtes, types de fret
  et de passagers.
- L'**interface complète** (menus, options, achievements, statistiques,
  écran de fin de journée).
- Les **descriptions des skins de trains et de personnages**.
- Les **types de bâtiments et de quêtes** (via les enums internes du jeu).

## Ce qui reste en anglais (volontairement)

- Les **noms propres** : skins (Comet, Forgotten, Theodore...), stations
  (Eagle Nest, Seaside, Aurora...), zones découvertes (Whistling Peaks,
  Lilli Forest...).
- Les **enseignes des magasins** dans les villes — préservation de
  l'ambiance western d'époque.
- Les libellés `On` / `Off` dans les options — contraintes UI.
- Les **crédits** (noms des contributeurs et de l'équipe de développement).

---

## Crédits

Traduction française : **Shayano**
Outils : [retoc-rivals](https://github.com/natimerry/repak-rivals),
[KismetEditor](https://github.com/SolicenTEAM/KismetEditor),
[UAssetAPI](https://github.com/atenfyr/UAssetAPI),
[Dumper-7](https://github.com/Encryqed/Dumper-7).

A Bumpy Ride © Choo-Choo Games. Ce mod est non officiel et n'est ni
soutenu ni endossé par l'éditeur du jeu.
