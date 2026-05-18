# A Bumpy Ride - Traducción al español (mod no oficial)

> 🌍 **Otros idiomas** : ver [README.md](README.md) para la lista completa de traducciones disponibles.

Mod de traducción al español para [A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/), un juego indie de simulación ferroviaria en Steam.

**Versión actual : 1.4.7** (17 de mayo de 2026)
**Motor del juego : Unreal Engine 5.3.2 (IoStore)**

> 🆕 **v1.4.7** : hotfix de un bug silencioso desde v1.4.5 - la segunda ocurrencia de ` law signs`, ` hours` y ` times` en las tareas del Accionista permanecía en inglés (`Obedece 3 law signs` en vez de `Obedece 3 señales`). Causa : el wrapper de `BPOffsetPatcher` deduplicaba entradas `Original` idénticas, por lo que solo se parcheaba la primera ocurrencia bytecode. Bonus : `QuestTicket.uasset` ahora también está traducido (panel `Destino: estación más cercana` en vez de `Destination: Nearest Station` - primera aplicación de `BPOffsetPatcher` a un Blueprint distinto a SpecialPassenger). Esta release acompaña la primera **traducción al japonés** ([README.jp.md](README.jp.md)).

> Este mod no está desarrollado ni respaldado por los creadores del juego. Es un proyecto de fans, ofrecido tal cual.

---

## Qué se traduce

- Toda la interfaz (menús, botones, opciones, atajos de teclado)
- Los diálogos del tutorial y los eventos del mapa principal (intro, notificaciones, hilo narrativo)
- Las descripciones de misiones, carga, pasajeros y edificios
- Los nombres y descripciones de los vagones y skins (salvo los nombres propios conservados en VO)
- Las pantallas de final del día, logros, estadísticas
- Los 62 objetivos de tareas del Accionista (vía BPOffsetPatcher)

**Voluntariamente en inglés** (por coherencia con el ambiente del juego) :
- Nombres propios : skins (Lavish, Stockton, Dayton...), estaciones, regiones, créditos
- Letreros de tiendas en pixel art (ambiente western 1900)
- `On` / `Off` (coherencia UI + ancho de los botones)
- Unidades imperiales (FT, millas)

**Registro** : `tú` (informal), variante neutra/España.

---

## Instalación

El mod se distribuye en dos formatos a elegir :
- **Instalador Windows** (`ABR-es_v1.4.7.zip`, ~30-100 MB) : instalador PowerShell que detecta Steam automáticamente, ~3-5 min
- **Drop-in prepatched** (`ABR-es_v1.4.7_prepatched.zip`, ~1,9 GB) : reemplazo directo de los archivos de container, cualquier OS (Windows / Linux / Steam Deck / macOS), sin instalador

### Pasos (drop-in prepatched)

1. Descarga `ABR-es_v1.4.7_prepatched.zip` (ver [Releases](../../releases))
2. **Cierra el juego** si está abierto
3. Localiza la carpeta `Paks` de tu instalación de A Bumpy Ride :
   - **Windows**   : `<biblioteca Steam>\steamapps\common\A Bumpy Ride\ABumpyRide\Content\Paks\`
   - **Steam Deck**: `~/.steam/steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
   - **Linux**     : `~/.local/share/Steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
4. Extrae el zip en esa carpeta `Paks/`. Tres archivos existentes serán reemplazados :
   ```
   ABumpyRide-Windows.utoc
   ABumpyRide-Windows.ucas
   ABumpyRide-Windows.pak
   ```
   No hace falta hacer copia de seguridad de los originales : Steam puede restaurarlos en cualquier momento (ver desinstalación).
5. Lanza el juego por Steam normalmente. Los menús deben estar en español.

### Pasos (instalador Windows)

1. Descarga `ABR-es_v1.4.7.zip`
2. Cierra el juego si está abierto
3. Extrae el zip en una carpeta cualquiera
4. Lanza `install.ps1` (clic derecho > Ejecutar con PowerShell, ~3-5 min)

> Nota técnica : el `.ucas` parchado pesa ~5,2 GB (vs ~1,6 GB vanilla) porque el pipeline de generación no recomprime con Oodle. Funciona perfectamente, solo ocupa más espacio en disco.

---

## Desinstalación / volver a la versión original

No hace falta gestionar manualmente un backup. Steam restaura los archivos vanilla en un clic :

1. En la biblioteca de Steam, **clic derecho sobre A Bumpy Ride** → *Propiedades*
2. *Archivos instalados* → **Verificar la integridad de los archivos del juego**
3. Steam detecta los 3 archivos modificados y los vuelve a descargar (~1,6 GB)
4. En el próximo lanzamiento, el juego vuelve a estar en inglés, como originalmente

Este método también es tu red de seguridad : si el mod rompe algo, lanza una verificación de integridad y vuelves a un estado limpio sin tener que hurgar en las carpetas.

---

## Compatibilidad

| Aspecto | Estado |
|---|---|
| Versión del juego | A Bumpy Ride al 12 de mayo de 2026 - última actualización Steam apuntada (Steam app id `2540610`) |
| Partidas guardadas | Compatibles, el mod no toca ningún archivo de save |
| Multijugador | No hay multijugador en ABR - no aplica |
| Actualizaciones del juego | Después de cada parche oficial del juego, hay que reinstalar la versión más reciente del mod (de lo contrario el juego puede crashear al inicio) |
| Coexistencia FR/DE/ES/JP | Solo un container `.ucas` activo a la vez - para cambiar de idioma, desinstalar uno (verificación de integridad Steam) e instalar el otro |

---

## Problemas conocidos

- **El juego crashea al lanzar después de la instalación** : tu versión del juego probablemente es más reciente que la que apunta este mod. Lanza una verificación de integridad Steam para volver al vanilla y espera una actualización del mod.
- **Algunos textos permanecen en inglés** : probablemente son nombres propios conservados voluntariamente (skins, estaciones, regiones). Si es un texto de interfaz no traducido, [abre un issue](../../issues) con una captura de pantalla.
- **Caracteres extraños (ñ, á, etc.) en lugar de acentos correctos** : signo de corrupción durante la extracción del zip. Vuelve a descargar y reextraer con una herramienta que maneje bien los archivos grandes (7-Zip, herramienta integrada de Windows 10/11, Ark en Steam Deck).
- **Algunas palabras permanecen en inglés en el QuestBoard y el ticket de misión** : `Lock` en el botón de candado en la parte superior del tablero, `DESTINATION:` en el ticket de misión lateral. Son identificadores internos UMG (los sub-componentes gráficos del widget) que causaban un crash si se traducían. Limitación conocida en v1.4.7, a corregir en una futura versión vía un enfoque alternativo.
- **2 strings `AM`/`PM` (en los objetivos 9PM / 9AM del Accionista) permanecen en inglés** : bug de duplicados en el nuevo patcher (la 2ª ocurrencia de cada duplicado se omite). No bloqueante, se corregirá en una futura versión menor.

---

## Créditos y agradecimientos

- **Mod** : Shayano
- **Traducción** : traducción asistida por IA producida con Claude Code (Anthropic), no revisada por un hablante nativo. Comentarios y correcciones bienvenidos vía [GitHub issues](../../issues).
- **Herramientas usadas en el pipeline de parcheo** :
  - [retoc-rivals](https://github.com/natimerry/repak-rivals) - repackager IoStore UE5.3
  - [KissE / KismetEditor](https://github.com/SolicenTEAM/KismetEditor) - patcher de bytecode Blueprint
  - [Dumper-7](https://github.com/Encryqed/Dumper-7) - generación del `.usmap` del juego
  - [UAssetAPI](https://github.com/atenfyr/UAssetAPI) - manipulación de assets UE
- **Metodología** : desarrollado en pair-programming con Claude Code (Anthropic) durante varias sesiones.

---

## Licencia

Este mod se proporciona gratis, sin garantía, tal cual. Los assets traducidos derivan del juego original (propiedad de sus autores) - la traducción al español es de libre uso personal.

No se permite redistribución comercial.
