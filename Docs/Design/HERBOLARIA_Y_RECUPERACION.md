# Herbolaria: registro, morral y recuperación

## Recorrido jugable

1. Al iniciar el recorrido se generan hasta 24 ejemplares de seis especies: manzanilla, hierbabuena, árnica mexicana, sábila, romero y toronjil morado. Se concentran en un único huerto fijo al suroeste del inicio, sobre terreno válido y sin obstáculos sólidos; no aparecen por todo el mapa.
2. Acercarse a menos de 3,2 unidades muestra **G · Recolectar y registrar**. Una interacción recolecta un ejemplar, reproduce una animación, guarda una unidad en el morral y desbloquea su ficha persistente en la ludoteca.
3. La sección **Herbolaria** muestra seis tarjetas en una cuadrícula de tres columnas y dos filas, con ilustraciones distintas y siluetas para las plantas no descubiertas. El botón dice únicamente **VER FICHA**. Incluye identificación ilustrativa, contexto biocultural, fuentes y advertencias, sin recetas ni acciones de fabricación en la ludoteca.
4. Abrir **I · Morral**. El panel **Preparación de hierbas** muestra cantidades disponibles. **Preparar infusión** intercambia **2 manzanillas + 1 hierbabuena** por **1 Infusión de hierbas**. Si faltan ingredientes o espacio no se descuenta nada. Su identificador interno sigue siendo `preparado_camino` por compatibilidad.
5. **H** usa la infusión; también se puede seleccionar en el morral y pulsar **Usar**. Recupera hasta **40 puntos de vida en 5 segundos de tiempo jugable**. La pausa detiene la recuperación, no se sobrepasa el máximo, no se reviven personajes derrotados y no se acumulan recuperaciones simultáneas. Con salud completa no se consume.

Las plantas no son flores-palabra, no cuentan para la misión de fauna y no alteran el desbloqueo del arma. Las fichas se conservan mediante `PlayerPrefs`; las cantidades permanecen en el inventario de la sesión, como los otros recursos existentes. No se añade un guardado completo de partida. La recolección es finita por recorrido: al reiniciar la escena se regeneran los ejemplares.

La receta vigente utiliza manzanilla y hierbabuena. Las otras cuatro especies amplían el registro y los recursos recolectables; no se inventan nuevas recetas ni efectos curativos para ellas.

## Arte integrado

Los seis PNG detallados están en `Assets/Resources/Sprites/Plants/HerbalRealistic`.
Se generaron con `image_gen` tomando `FloraExpansion_4x2.png` como referencia de
vegetación: sombreado naturalista, textura de hojas, follaje irregular y luz cálida.
Las láminas sustituyen los dibujos geométricos; el mismo sprite se usa en el mundo,
las fichas y el morral. Se conserva el alfa original y se calcula un rectángulo de
sprite con pivote en la base para evitar plantas flotantes. Altura visual: 2,25
unidades de juego. Importación sin compresión, hasta 1024 píxeles, sin mipmaps.
Los prompts completos están en `Docs/Design/HERBOLARIA_ARTE_PROMPTS.json`.

## Separación entre cultura y ficción

La herbolaria es un tema biocultural documentado. Las plantas ilustradas y la receta de recuperación son una adaptación lúdica, no un inventario botánico exhaustivo de las comunidades mazahuas. La receta, cantidades y puntos de vida son ficticios: no equivalen a instrucciones de consumo, dosis o eficacia terapéutica. Las imágenes no identifican ejemplares reales. Nombres en mazahua y usos locales específicos requieren validación lingüística, botánica y comunitaria antes de su publicación como contenido cultural definitivo.

## Fuentes consultadas

- García, S. A., y Guzmán Mendoza, R. *Conocimiento tradicional asociado al uso de plantas medicinales en migrantes mazahuas de una comunidad indígena de San José del Rincón, Estado de México*. Huellas de la Migración, 1(1), 195–220. Número de 2016; página editorial publicada en 2017. https://huellasdelamigracion.uaemex.mx/article/view/4438
- INIFAP. *Plantas medicinales, tradición ancestral*. Contexto de manzanilla y hierbabuena en México. https://www.gob.mx/inifap/es/articulos/plantas-medicinales-tradicion-ancestral
- CIBNOR. *Romero*. Descripción y origen mediterráneo. https://www.cibnor.gob.mx/personal/bmurillo/hierbas/romero.php
- SEMARNAT. *Medicina tradicional mexicana, valioso conocimiento ancestral y legado biocultural*. Referencia del toronjil morado (*Agastache mexicana*). https://www.gob.mx/semarnat/articulos/medicina-tradicional-mexicana-valioso-conocimiento-ancestral-y-legado-biocultural

Se redactan síntesis originales: no se reproducen ilustraciones ni recetas del artículo.

## Verificación

En Unity: **Tools > Xunjuu > Validar herbolaria y recetas**. Utiliza una escena temporal aislada; restaura los registros de plantas al terminar. Resultados y capturas: `output/herbalism-validation`. No modifica la escena principal ni la misión de animales.
