# Casas y vegetación adicionales

Se añaden siete modelos 3D reutilizables: `Casa_Cal_Corredor`, `Casa_Granero`, `Casa_Chimenea`, `Tronco_Caido`, `Tocon_Raices`, `Arbusto_Florido` y `Arbusto_Bayas`. Las casas mantienen adobe, tejas y madera del entorno previo, con volúmenes o accesorios diferenciados. Los troncos incluyen extremos cortados, corteza, ramas y musgo; el tocón tiene raíces visibles.

Los prefabs se generan en `Assets/Prefabs/EntornoMazahua/Refined`, y las mallas en `Assets/Art/EntornoMazahua/Refined`. Los prefabs existentes se preservan para conservar sus ajustes manuales. El constructor de la aldea intenta colocar siete casas en puntos libres, comprobando terreno y colisiones. La cantidad efectiva depende del espacio disponible.

La cobertura móvil de vegetación pasa de 9 × 9 a 13 × 13 sectores de 20 m: aproximadamente de 180 × 180 m a 260 × 260 m, algo más del doble de superficie. No se agranda el terreno físico ni se cambian los límites de las misiones. Los sectores externos tienen menos detalle, se enriquecen al acercarse y se descargan al alejarse. Se amplían también los grupos de árboles del fondo.

La hoja `Assets/Resources/Sprites/TextureTerrain/FloraExpansion_4x2.png` aporta ocho variedades 2D: arbusto verde, arbusto florido, helecho, hierba, flores amarillas, flores rosadas, hierba seca y arbusto con bayas. Se generó con la herramienta integrada de imágenes; se comprobó que tiene canal alfa y se conserva su transparencia. Las plantas decorativas no añaden colisiones sólidas.

## Prompt de la hoja 2D

Game vegetation sprite atlas for a rural highland village fantasy RPG. Precisely 4 columns by 2 rows, eight individual isolated plants. Genuine transparent background alpha, no checkerboard, no ground plane, no text or grid. Equal square cells, complete plant centered in each cell with roots at same bottom baseline and generous clear gutters. Row1: rounded leafy olive green shrub; low bush with small white flowers; fern with arching fronds; cluster of narrow sage green wild grasses. Row2: tiny clump yellow meadow flowers; pale pink wildflowers; dry ochre grass clump; low leafy shrub with small red berries. Stylized hand-painted pixel-art game asset, crisp defined pixel clusters, muted olive and sage foliage, warm natural highlights upper left. Detailed readable leaves, modest color saturation, consistent scale and lighting, slight elevated three-quarter view, no pots, no dirt rectangles, no drop shadows, no characters. Uniform 4x2 atlas, landscape 1536x1024 or similar. Plants entirely inside separate cells, no touching. Intended for 2D billboards among 3D trees and adobe houses.

## Verificación

La prueba de Play comprueba los sectores ampliados, casas colocadas, importación de la flora, mallas nuevas no vacías y las mecánicas anteriores. Los resultados y su fecha quedan en `runtime-after.txt`, `motion-checks.txt` y `models.txt`. Se generan vistas individuales de los modelos nuevos y una captura de la escena para inspección visual. No se afirma una tasa de fotogramas sin medirla.
