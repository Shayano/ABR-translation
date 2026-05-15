# A Bumpy Ride - Traducción al español (mod ABR-es)

Mod de traducción al español no oficial para **A Bumpy Ride**.

---

## Instalación rápida

1. **Descomprime esta carpeta** en cualquier sitio (por ejemplo en el Escritorio).
2. **Clic derecho** en `install.ps1` → **Ejecutar con PowerShell**.
3. El instalador detecta automáticamente tu instalación de Steam y arranca
   el proceso. **Tarda entre 3 y 5 minutos**.
4. Lanza el juego desde Steam como siempre.

> Si Windows bloquea el script, abre PowerShell como administrador y escribe:
> ```
> Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
> ```
> y vuelve a lanzar el instalador.

---

## Modo manual

Si la detección automática falla (Steam en una ubicación inusual, varias
bibliotecas, etc.), puedes **copiar esta carpeta `patch-es`
directamente dentro de la carpeta del juego**:

```
F:\Steam\steamapps\common\A Bumpy Ride\patch-es\install.ps1
```

El instalador detecta automáticamente que se ejecuta desde la carpeta del
juego y procede sin pedirte la ruta.

---

## Desinstalación

Ejecuta `uninstall.ps1` con clic derecho → Ejecutar con PowerShell.
El desinstalador restaura los archivos vanilla desde la copia de seguridad
`_ABRes_backup/` creada durante la instalación.

Si la copia de seguridad ya no existe, también puedes **verificar la
integridad de los archivos del juego** desde Steam:
*A Bumpy Ride > Propiedades > Archivos instalados > Verificar integridad*.

---

## Requisitos

- **Windows 10 u 11**
- **PowerShell 5.1 o superior** (preinstalado en Windows 10/11)
- **Aproximadamente 12 GB libres** en la unidad donde está `%TEMP%`
  (utilizado temporalmente durante la instalación, liberado al final)
- **El juego A Bumpy Ride instalado vía Steam**, en su versión original
  (el mod apunta a la versión vanilla - si Steam ha actualizado el juego,
  el mod podría ser incompatible y necesitar una actualización)

---

## En caso de problema

El instalador muestra mensajes claros. Si algo falla:

- **«El juego instalado no coincide exactamente con los archivos vanilla»**
  → o bien el mod ya está instalado, o Steam ha actualizado el juego.
  Prueba primero a **verificar la integridad de los archivos** desde Steam.

- **«Espacio insuficiente»** → el instalador necesita ~12 GB libres
  en la unidad de `%TEMP%`. Libera espacio y vuelve a lanzarlo.

- **«retoc.exe no encontrado»** → la carpeta `patch-es` no se ha
  descomprimido por completo. Vuelve a extraer.

- **«La extracción del vanilla ha producido demasiado pocos assets»** →
  verifica la integridad del juego vía Steam.

- **Otro error**: copia el mensaje de error y comunícaselo al autor del mod.

---

## Lo que está traducido

- Todos los **diálogos del tutorial**, descripciones de misiones, tipos
  de carga y pasajeros.
- La **interfaz completa** (menús, opciones, logros, estadísticas,
  pantalla de fin de día).
- Las **descripciones de skins** de trenes y de personajes.
- Los **tipos de edificios y misiones** (vía las enumeraciones internas
  del juego).

## Lo que se queda en inglés (intencionalmente)

- Los **nombres propios**: skins (Comet, Forgotten, Theodore...), estaciones
  (Eagle Nest, Seaside, Aurora...), zonas descubiertas (Whistling Peaks,
  Lilli Forest...).
- Los **rótulos de tiendas** en las ciudades - preservación del ambiente
  western de la época.
- Las etiquetas `On` / `Off` en las opciones - restricciones de UI.
- Los **créditos** (nombres de los colaboradores y del equipo de desarrollo).

---

## Créditos

Traducción al español: **Shayano**
Herramientas: [retoc-rivals](https://github.com/natimerry/repak-rivals),
[KismetEditor](https://github.com/SolicenTEAM/KismetEditor),
[UAssetAPI](https://github.com/atenfyr/UAssetAPI),
[Dumper-7](https://github.com/Encryqed/Dumper-7).

A Bumpy Ride © Choo-Choo Games. Este mod es no oficial y no está
respaldado ni aprobado por el editor del juego.
