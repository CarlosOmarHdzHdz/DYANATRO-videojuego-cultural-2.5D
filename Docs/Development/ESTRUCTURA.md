# Estructura del proyecto

El proyecto activo es la raíz que contiene `Assets`, `Packages` y `ProjectSettings`.
Unity necesita esos nombres: no envolverlos en otra carpeta ni modificar su jerarquía.

## Responsabilidades

| Carpeta | Contenido |
| --- | --- |
| `Assets/Scripts/<sistema>` | Lógica ejecutable del juego. |
| `Assets/Editor/<función>` | Constructores, importadores, validaciones y herramientas del editor. |
| `Assets/Prefabs/<sistema>` | Prefabs de animales, combate, entorno, misiones y coleccionables. |
| `Assets/Environment/Terrain` | Datos de terreno y capas, conservando sus GUID. |
| `Assets/Settings` | Configuración de renderizado y controles. |
| `Assets/Resources` | Contrato de carga por nombre; conservar las rutas actuales. |
| `Assets/Art` | Material fuente y modelos; no confundir con atlas normalizados. |
| `Assets/Sprites`, `Animations`, `Materials`, `Audio` | Recursos importados clasificados por tipo y tema. |
| `Docs` | Documentación de diseño y desarrollo. |
| `Tools` | Herramientas locales que Unity no debe compilar. |
| `output` | Informes, capturas y propuestas; nunca contenido cargado por el juego. |
| `Archive` | Material histórico único y respaldos locales, no importados ni versionados. |

## Límites de esta limpieza

La versión `Assets/2D_Completo` conserva escenas y código propios: no es una caché.
Tampoco se eliminan terrenos, animaciones Legacy o sprites por su nombre. Pueden
seguir referenciados mediante GUID o ser originales de producción. Dos imágenes
visualmente similares no son necesariamente intercambiables.

`Library`, `Temp`, `Logs`, `.vs` y los `.csproj`/`.slnx` de la raíz son generados por
Unity o el IDE. Se excluyen de Git; no borrarlos mientras el editor los utiliza.
Los respaldos no forman parte de una compilación del juego.

## Limpieza de documentos académicos

Los Word, PDF, fotografías de informes y renders académicos se retiraron del
proyecto a una carpeta hermana `DYANATRO_Respaldo_Limpieza_*`. Cada traslado
conserva la ruta relativa y un `manifiesto.json` con tamaños y hashes SHA-256.
No son contenido del juego ni se deben volver a copiar dentro de `Assets`.
`Docs/Design`, `Docs/Development` y `Docs/Build` conservan únicamente la
documentación técnica correspondiente. Los borradores universitarios se
trabajan fuera de este proyecto Unity.

La herramienta `Tools/Maintenance/Clean-GameWorkspace.ps1` muestra primero un
plan. Con `-Apply` retira los materiales a un respaldo externo y comprueba
su integridad. Nunca sigue enlaces, sobrescribe destinos ni modifica las
cachés activas, los ejecutables o el toolchain aislado de `output/qa`.
Los formatos de Word/PDF se excluyen de Git para evitar reintroducirlos.

## Audio y trabajo por fotograma

La música de fondo se configura sin reproducción automática. Permanece
detenida en el menú, el video y el prólogo, y comienza con el recorrido jugable.
Pausa, ludoteca y cualquier pausa externa mediante `Time.timeScale = 0`
suspenden la pista sin reiniciarla. Al continuar se conserva su posición.
El video mantiene su propio audio; no se modificaron los archivos musicales.

`XunjuuCameraFrame` comparte una consulta de cámara por fotograma entre los
billboards, las plantas y los ordenadores de sprites. La interfaz evita
reescribir estados idénticos, y el optimizador almacena la clasificación de
colisiones y suspende sus recorridos mientras el juego está pausado.
Estas son reducciones de trabajo identificadas en código, no una promesa de
un porcentaje de mejora de FPS sin una medición de rendimiento independiente.

## Mantenimiento seguro

- Mover assets con Unity para conservar el archivo `.meta` y su GUID.
- Mantener las llamadas a `Resources.Load`, rutas de editor y documentación sincronizadas.
- No renombrar componentes serializados ni sustituir scripts por respaldos antiguos.
- Antes de borrar recursos, revisar GUID, cargas dinámicas y herramientas de generación.
- Para revisar la organización externa: `./Tools/Maintenance/Organize-Workspace.ps1`.
  Es una simulación por defecto; `-Apply` mueve sin sobrescribir y verifica SHA-256.
- Para migrar recursos: `Tools > Xunjuu > Organizar proyecto`. Produce un manifiesto
  en `output/project-organization`; si ya se ejecutó, no repite movimientos.
- Los informes de migración prueban integridad, no sustituyen las pruebas jugables.
- Ejecutar `Tools > Xunjuu > Validar captura y fichas` después de importar y compilar.

Las propuestas de skin en `output/character-preview` no están integradas por esta
reorganización. El arte fuente se conserva; las mecánicas no se rediseñan aquí.
