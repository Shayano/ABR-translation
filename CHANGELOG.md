# Changelog

## 1.3.0 — 2026-05-05

Trois dernières chaînes hardcodées traduites :
- `Money made today: ` → `Recettes du jour : ` (écran fin de journée)
- `Water usage is twice as fast!` → `Consommation d'eau doublée !` (avertissement consommation)
- `New Staff Member Unlocked!` → `Nouveau personnel débloqué !` (notification dans MainMap)

Côté outillage interne : extension de MainMapPatcher pour gérer une seconde cible (`--target=staff` en plus de `--target=intro`), avec support des `EX_TextConst` (FScriptText nesté).

## 1.2.0 — 2026-05-05

Traduction de l'intro de la carte principale (« Oh no! The tracks on your map got ruined… » → « Oh non ! Les rails de votre carte ont été abîmés… »).

Première traduction d'un bytecode embarqué dans un asset `.uexp` >2 Go que UAssetAPI/KissE refusent de charger normalement. Le patch est appliqué à l'install via un outil custom (`MainMapPatcher.exe`) qui isole l'export `ExecuteUbergraph_MainMap` et applique l'algorithme placeholder + branch détour de KissE.

Ajoute `MainMapPatcher.exe` (~38 Mo) et `ABumpyRide.usmap` (344 Ko) à la trousse d'install.

## 1.1.0 — 2026-05-04

Inclusion des 7 enums (TitleScreenBlurbs, BuildingType, FreightType, QuestLine, QuestType, PassengerEnum, TitleScreenBlurbsRainy) et de SkinButtonTable (descriptions de skins) qui étaient absents en 1.0.0.

## 1.0.0 — 2026-05-03

Version initiale — BPs et maps traduits.
