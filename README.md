# Xunjúu / DYANATRO

Proyecto Unity **6000.3.11f1**. Escena principal: `Assets/Scenes/SampleScene.unity`.
Abrir esta carpeta desde Unity Hub; no abrir los respaldos como si fueran la versión activa.

## Dónde trabajar

```text
Assets/
  Scripts/                 Código del juego, agrupado por responsabilidad
    Core/                  Inicio, historia y estado general
    Player/                Control y animación del protagonista
    Animals/               Comportamiento, animación, captura y catálogo de fauna
    Combat/                Enemigos, jefe y armas
    Environment/           Terreno, vegetación, nubes y modelos procedurales
    Camera/                Seguimiento y oclusión de cámara
    Rendering/             Orden de dibujo y política visual
    UI/                    Vida y presentación general
    Inventory/             Morral, interfaz e ítems
    Ludoteca/              Fichas e información desbloqueable
    Missions/              Misiones y recompensas
    Collectibles/          Coleccionables y motivos
    Interaction/           Interacciones y etiquetas
    Crops/                 Cultivos
    Audio/                 Sonidos procedurales
    Optimization/          Optimización y población ligera
    Diagnostics/           Capturas usadas por las verificaciones
  Editor/
    Authoring/             Constructores de prefabs y configuración de niveles
    Importers/             Reglas de importación y reparación de texturas
    Validation/            Verificaciones del juego
    Build/                 Herramientas de compilación
    ProjectTools/          Migración controlada de estructura
  Resources/               Recursos cargados por ruta: NO mover sin actualizar código
    Sprites/Player/        Atlas utilizados por el protagonista
    Sprites/Animals/       Retratos y ciclos normalizados utilizados por la fauna
  Sprites/                 Sprites referenciados por GUID, organizados por tema
  Animations/              Clips y controladores (incluye Legacy y LegacySprites)
  Art/                     Fuentes y modelos de trabajo
    Animals/CyclesV2/      Originales de generación, no son los ciclos normalizados
    Source/Player/         Hojas fuente del personaje
    Archive/Player/        Respaldos históricos conservados; no usarlos como arte nuevo
  Prefabs/                 Prefabs referenciados directamente
    Animals/               Fauna
    Combat/                Armas, proyectiles, enemigos y jefe
    Environment/           Árboles y nubes; EntornoMazahua conserva su catálogo
    Missions/              Recompensas de misión
    Collectibles/          Objetos recolectables
  Environment/Terrain/     Datos del terreno y capas de superficie
  Settings/Input/          Acciones y configuración de entrada
  Settings/Rendering/      Ajustes globales y perfil visual
  Scenes/                  Escenas
Docs/
  Design/                  Historia y diseño del juego
  Development/             Configuración y guía de estructura
Tools/Maintenance/         Mantenimiento local (fuera de la importación de Unity)
Archive/                   Respaldos y referencias históricas, fuera de Assets
output/
  character-preview/       Propuestas generadas, NO integradas automáticamente
  project-organization/    Mapa de movimientos, hashes y respaldo de rutas anteriores
```

`Assets/2D_Completo` es un subsistema previo con sus propios recursos. Se conserva
aislado; no se ha borrado ni mezclado con la escena principal. `TextMesh Pro`,
`Packages`, `ProjectSettings` y `Settings` mantienen su estructura de Unity.

## Reglas para no romper referencias

- Mover assets desde Unity y conservar siempre su `.meta` (GUID).
- No renombrar clases/archivos de componentes por estética sin revisar escenas y prefabs.
- No cambiar rutas bajo `Resources`: llamadas a `Resources.Load` dependen de ellas.
- Un original en `Art` y una versión normalizada en `Resources` no son duplicados descartables.
- No sustituir un atlas jugable por una propuesta de `output/character-preview` sin
  recortar, alinear pivotes, configurar Point/no compresión y verificar el ciclo.
- No colocar scripts de herramientas en `Scripts`: deben permanecer bajo `Editor`.
- No añadir archivos generados de `Library`, `Builds`, `.vs` o logs al código del juego.

## Verificación

En Unity: **Tools > Xunjuu > Validar captura y fichas** comprueba fauna, navegación,
captura, fichas y recompensa. Las verificaciones de movimiento del protagonista
están en `Assets/Editor/Validation/XunjuuMotionValidation.cs`.

La reorganización conserva la lógica y los identificadores; los registros en
`output/project-organization` documentan cada ruta anterior y nueva.

## Documentos existentes

- [Historia jugable](Docs/Design/HISTORIA_JUGABLE_ACTUAL.md)
- [Diseño general](Docs/Design/DISENO_VIDEOJUEGO_XUNJUU.md)
- [Diseño de historia](Docs/Design/DISENO_SOLO_HISTORIA_DYANATRO.md)
- [Configuración y mejoras](Docs/Development/CONFIGURACION_MEJORAS.md)
- [Estructura y mantenimiento](Docs/Development/ESTRUCTURA.md)
