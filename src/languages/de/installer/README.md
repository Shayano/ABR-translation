# A Bumpy Ride — Deutsche Übersetzung (Mod ABR-de)

Inoffizieller deutscher Übersetzungs-Mod für **A Bumpy Ride**.

---

## Schnellinstallation

1. **Entpacke diesen Ordner** an einem beliebigen Ort (z. B. auf den Desktop).
2. **Rechtsklick** auf `install.ps1` → **Mit PowerShell ausführen**.
3. Der Installer erkennt deine Steam-Installation automatisch und startet
   den Vorgang. **Das dauert 3 bis 5 Minuten**.
4. Starte das Spiel wie gewohnt über Steam.

> Falls Windows das Skript blockiert, öffne PowerShell als Administrator und gib ein:
> ```
> Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
> ```
> und starte den Installer erneut.

---

## Manueller Modus

Falls die automatische Erkennung fehlschlägt (Steam an einem ungewöhnlichen
Ort, mehrere Bibliotheken usw.), kannst du **diesen Ordner `patch-de`
direkt in das Spielverzeichnis kopieren**:

```
F:\Steam\steamapps\common\A Bumpy Ride\patch-de\install.ps1
```

Der Installer erkennt automatisch, dass er sich im Spielverzeichnis
befindet, und fragt nicht mehr nach dem Pfad.

---

## Deinstallation

Führe `uninstall.ps1` per Rechtsklick → Mit PowerShell ausführen aus.
Der Deinstaller stellt die vanilla-Dateien aus der Sicherung
`_ABRde_backup/` wieder her, die bei der Installation angelegt wurde.

Falls die Sicherung nicht mehr existiert, kannst du auch **die
Spielintegrität prüfen** über Steam:
*A Bumpy Ride > Eigenschaften > Installierte Dateien > Integrität der Spieldateien überprüfen*.

---

## Voraussetzungen

- **Windows 10 oder 11**
- **PowerShell 5.1 oder höher** (auf Windows 10/11 vorinstalliert)
- **Ca. 12 GB freier Speicher** auf dem Laufwerk, auf dem `%TEMP%` liegt
  (während der Installation kurzzeitig genutzt, danach wieder freigegeben)
- **Das Spiel A Bumpy Ride über Steam installiert**, in der Originalversion
  (der Mod zielt auf die vanilla-Version — falls Steam das Spiel aktualisiert
  hat, kann der Mod inkompatibel sein und ein Update benötigen)

---

## Bei Problemen

Der Installer gibt klare Meldungen aus. Falls etwas schiefgeht:

- **„Die installierten Spieldateien stimmen nicht exakt mit den vanilla-Dateien überein"**
  → entweder ist der Mod bereits installiert, oder Steam hat das Spiel aktualisiert.
  Versuche zuerst, **die Integrität der Dateien** über Steam zu prüfen.

- **„Nicht genug freier Speicher"** → der Installer benötigt ca. 12 GB frei
  auf dem `%TEMP%`-Laufwerk. Schaffe Platz und starte ihn erneut.

- **„retoc.exe nicht gefunden"** → der Ordner `patch-de` wurde nicht
  vollständig entpackt. Entpacke ihn erneut.

- **„Die vanilla-Extraktion hat zu wenige Assets erzeugt"** →
  prüfe die Spielintegrität über Steam.

- **Anderer Fehler**: Kopiere die Fehlermeldung und melde sie dem Modder.

---

## Was übersetzt ist

- Alle **Tutorial-Dialoge**, Quest-Beschreibungen, Frachttypen
  und Passagiere.
- Die **vollständige Oberfläche** (Menüs, Optionen, Erfolge, Statistiken,
  Tagesabschluss-Bildschirm).
- Die **Skin-Beschreibungen** für Züge und Charaktere.
- Die **Gebäude- und Quest-Typen** (über die internen Enums des Spiels).

## Was bewusst auf Englisch bleibt

- Die **Eigennamen**: Skins (Comet, Forgotten, Theodore...), Stationen
  (Eagle Nest, Seaside, Aurora...), entdeckte Regionen (Whistling Peaks,
  Lilli Forest...).
- Die **Ladenschilder** in den Städten — Erhalt der Western-Atmosphäre.
- Die `On` / `Off`-Schalter in den Optionen — UI-Platzbeschränkung.
- Die **Credits** (Namen der Mitwirkenden und des Entwicklerteams).

---

## Credits

Deutsche Übersetzung: **Shayano**
Werkzeuge: [retoc-rivals](https://github.com/natimerry/repak-rivals),
[KismetEditor](https://github.com/SolicenTEAM/KismetEditor),
[UAssetAPI](https://github.com/atenfyr/UAssetAPI),
[Dumper-7](https://github.com/Encryqed/Dumper-7).

A Bumpy Ride © Choo-Choo Games. Dieser Mod ist inoffiziell und wird
weder vom Spielentwickler unterstützt noch genehmigt.
