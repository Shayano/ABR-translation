# A Bumpy Ride - Deutsche Übersetzung (inoffizieller Mod)

> 🌍 **Andere Sprachen** : siehe [README.md](README.md) für die vollständige Liste der verfügbaren Übersetzungen.

Inoffizieller Übersetzungs-Mod für [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), ein indie Eisenbahn-Simulationsspiel auf Steam.

**Aktuelle Version : 1.4.6** (16. Mai 2026)
**Spiel-Engine : Unreal Engine 5.3.2 (IoStore)**

> 🆕 **v1.4.6** : Wiederherstellung der 62 Aktionär-Aufgabenbeschreibungen, die seit v1.0 wegen des Absturzes durch unendliche Rekursion auf Englisch geblieben waren. Neuer Custom-Patcher `BPOffsetPatcher`, der zwei kumulierte Probleme löst, die kein bestehendes Tool behandelt hat : (1) die internen Offsets der ans Ende des Bytecodes verschobenen Statements und (2) die hartcodierten `EX_IntConst`-Entrypoints in den 47 internen Callern des Blueprints. Im Spiel : `Sonnenuntergang sehen`, `Bis 21 Uhr an Bord bleiben`, `Wüste zwischen 4 und 18 Uhr meiden`, `Sammle etwas Honig: 0/3`, `Großen Baum-Fotopunkt besuchen`, usw. Grammatische Umformulierung der Fracht-Fragmente (`Sammle etwas Birnen` → `Sammle einige Birnen`). Es bleiben 2 Strings `AM`/`PM` auf Englisch (Dubletten-Bug im Patcher, nicht blockierend).

> Dieser Mod wird weder von den Entwicklern des Spiels entwickelt noch unterstützt. Es ist ein Fan-Projekt, ohne Gewähr.

---

## Was übersetzt ist

- Die gesamte Benutzeroberfläche (Menüs, Schaltflächen, Optionen, Tastaturbelegungen)
- Tutorial-Dialoge und Hauptkarten-Ereignisse (Intro, Benachrichtigungen, Story-Elemente)
- Alle Quest-, Fracht-, Passagier- und Gebäude-Bezeichnungen
- Namen und Beschreibungen der Wagen und Skins (Eigennamen bleiben im Original-Englisch)
- End-of-Day-Bildschirme, Erfolge, Statistiken
- Registre `du` (informell, kasse Familienspiel)

**Bewusst auf Englisch gelassen** (zur Erhaltung der Spiel-Atmosphäre) :
- Eigennamen : Skins (Lavish, Stockton, Dayton…), Stationen, Regionen, Mitwirkende
- Ladenschilder in Pixel-Art (Western-Atmosphäre 1900)
- `On` / `Off` (UI-Konsistenz + Schaltflächenbreite)
- Imperiale Einheiten (FT, Meilen)

---

## Installation

Der Mod wird als Zip-Archiv ausgeliefert, das die 3 bereits gepatchten Spiel-Container-Dateien enthält. Es ist ein direkter Dateiaustausch ohne Installer.

### Schritte

1. Lade `ABR-de_v1.4.6.zip` herunter (siehe [Releases](../../releases)) - ab v1.4.3 wird offiziell nur das PowerShell-Installer-Zip veröffentlicht ; das Drop-in-Prepatched-Zip kann lokal regeneriert werden, indem du `install.ps1` ausführst und dann die erzeugten `.ucas/.utoc/.pak`-Dateien zippst
2. **Schließe das Spiel**, falls es läuft
3. Suche den Ordner `Paks` deiner A Bumpy Ride Installation :
   - **Windows**   : `<Steam-Bibliothek>\steamapps\common\A Bumpy Ride\ABumpyRide\Content\Paks\`
   - **Steam Deck**: `~/.steam/steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
   - **Linux**     : `~/.local/share/Steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
4. Entpacke das Zip in diesen `Paks/`-Ordner. Drei vorhandene Dateien werden überschrieben :
   ```
   ABumpyRide-Windows.utoc
   ABumpyRide-Windows.ucas
   ABumpyRide-Windows.pak
   ```
   Du musst die Originale nicht sichern : Steam kann sie jederzeit wiederherstellen (siehe Deinstallation).
5. Starte das Spiel wie gewohnt über Steam. Das Menü sollte auf Deutsch sein.

> Technische Anmerkung : Die gepatchte `.ucas` ist ~5,2 GB groß (gegenüber ~1,6 GB vanilla), da die Build-Pipeline beim Ausgang keine Oodle-Neukomprimierung durchführt. Es funktioniert einwandfrei, ist nur größer auf der Festplatte.

---

## Deinstallation / Zurück zur Originalversion

Du musst kein manuelles Backup verwalten. Steam kann die Vanilla-Dateien in einem Klick wiederherstellen :

1. In der Steam-Bibliothek, **Rechtsklick auf A Bumpy Ride** → *Eigenschaften*
2. *Installierte Dateien* → **Integrität der Spieldateien überprüfen**
3. Steam erkennt die 3 geänderten Dateien und lädt sie erneut herunter (~1,6 GB)
4. Beim nächsten Start ist das Spiel wieder auf Englisch, wie ursprünglich

Diese Methode ist auch dein Sicherheitsnetz : falls der Mod etwas kaputt macht, starte eine Integritätsprüfung und du bist zurück auf einem sauberen Stand, ohne in den Ordnern wühlen zu müssen.

---

## Kompatibilität

| Aspekt | Status |
|---|---|
| Spielversion | A Bumpy Ride am 12. Mai 2026 - letztes anvisiertes Steam-Update (Steam app id `2540610`) |
| Spielstände | Kompatibel, der Mod berührt keine Save-Dateien |
| Multiplayer | Kein Multiplayer in ABR - irrelevant |
| Spiel-Updates | Bei jedem offiziellen Spielpatch musst du die aktuelle Mod-Version neu installieren (sonst kann das Spiel beim Start abstürzen) |
| Koexistenz FR/DE | Nur ein `.ucas`-Container aktiv gleichzeitig - um die Sprache zu wechseln, deinstalliere die eine (Steam-Integritätsprüfung) und installiere die andere |

---

## Bekannte Probleme

- **Spiel stürzt beim Start nach Installation ab** : Deine installierte Spielversion ist wahrscheinlich neuer als die, auf die dieser Mod abzielt. Führe eine Steam-Integritätsprüfung durch, um zur Vanilla-Version zurückzukehren, und warte auf eine aktualisierte Mod-Version.
- **Einige Texte bleiben auf Englisch** : Wahrscheinlich Eigennamen, die bewusst beibehalten wurden (Skins, Stationen, Regionen). Falls es ein tatsächlicher UI-String ohne Übersetzung ist, [öffne ein Issue](../../issues) mit einem Screenshot.
- **Verstümmelte Zeichen (ä, õ, etc.) statt korrekter Umlaute** : Zeichen einer Zip-Extraktions-Korruption. Lade erneut herunter und entpacke mit einem Tool, das große Dateien korrekt verarbeitet (7-Zip, Windows 10/11 Bordmittel, Ark auf Steam Deck).
- **Einige Wörter bleiben auf Englisch im QuestBoard und im Quest-Ticket** : `Lock` auf dem Schloss-Button oben in der Quest-Tafel, `DESTINATION:` auf dem seitlichen Quest-Ticket. Dies sind interne UMG-Bezeichner (die Sub-Komponenten der Widgets), die einen Absturz verursachten, wenn sie übersetzt wurden. Akzeptierte Einschränkung für v1.4.3 ; soll in einer zukünftigen Version über einen alternativen Ansatz korrigiert werden.
- **2 Strings `AM`/`PM` (im 9PM / 9AM der Aktionär-Aufgaben) bleiben auf Englisch** : Dubletten-Bug im neuen Patcher (die 2. Vorkommen jeder Dublette wird ignoriert). Nicht blockierend - "Bis 21 Uhr an Bord bleiben" bleibt lesbar mit einem englischen "AM" daneben. Wird in einer zukünftigen Minor-Version behoben.

---

## Credits & Danksagungen

- **Mod** : Shayano
- **Im Patch-Pipeline verwendete Werkzeuge** :
  - [retoc-rivals](https://github.com/natimerry/repak-rivals) - IoStore UE5.3 Repackager
  - [KissE / KismetEditor](https://github.com/SolicenTEAM/KismetEditor) - Blueprint-Bytecode-Patcher
  - [Dumper-7](https://github.com/Encryqed/Dumper-7) - generiert das `.usmap` des Spiels
  - [UAssetAPI](https://github.com/atenfyr/UAssetAPI) - UE-Asset-Manipulation
- **Methodik** : Pair-Programming mit Claude Code (Anthropic) über etwa zehn Sessions.

---

## Lizenz

Dieser Mod wird kostenlos und ohne Gewährleistung, im Ist-Zustand, bereitgestellt. Die übersetzten Assets stammen aus dem Originalspiel (Eigentum der Entwickler) - die deutsche Übersetzung ist zur persönlichen Verwendung frei.

Keine kommerzielle Weiterverteilung.
