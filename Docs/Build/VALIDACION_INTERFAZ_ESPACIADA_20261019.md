# Entrega: interfaz espaciada y modelos para Blender

## Alcance

Se corrige la superposición de texto y bordado mostrada en el objetivo de fauna.
No se modifica el diseño ni la distribución de árboles respecto de la revisión
anterior. Se exportan sus dos mallas activas sin rediseñarlas.

## Comprobaciones realizadas

- PASS: texto del objetivo contenido, sin intersecciones entre título, contador
  e instrucción; sin puntadas bajo el cuerpo y progreso a mitad con tres capturas.
- PASS: botón con hover contenido y retorno al reposo mientras el mundo está
  pausado; paneles con tiempo no escalado. Cartas y letras no usan esa escala.
- PASS: cinco ítems de memoria, seis animales, herbolaria, identificación,
  preparación real, recuperación, morral, fichas y aviso central.
- PASS: música, pausa del morral y ludoteca, continuidad de la pista.
- PASS: progresión completa hasta jefe, recompensa y cierre.
- PASS: compilación Windows x64/Mono, sin recorte de código.
- PASS: estructura GLB 2.0, buffers, atributos, colores e índices de los modelos.
- ZIP de Windows: número de archivos coincide con la carpeta; todas las
  entradas se leen. El video coincide con el original por SHA-256.

Las capturas son renders de QA; no equivalen a probar todos los monitores.
La importación en Blender no se ha ejecutado, pues no está instalado en esta
máquina. Los archivos son GLB, no .blend; el material PBR no replica el shader
de oclusión/iluminación de Unity. No se promete una mejora medida de FPS.

## Entregas

- Juego: `Builds/Windows/DYANATRO_Windows_Interfaz_Espaciada_20261019.zip`.
  182 archivos; 273.907.866 bytes sin comprimir; ZIP de 98.750.544 bytes.
  SHA-256: `AA1E5D8AEFB865127C8D286D05A5DECBFD6375591649CBCEFDFF775BD4EB889F`.
- Blender: `Exports/Blender/Arboles_DYANATRO_para_Blender_20261019.zip`.
  Dos GLB, dos vistas PNG y LEEME; cinco archivos; 300.324 bytes.
  SHA-256: `473FF864C2F31FA0FF5815511DA418C512395878DEF0738499501E8FDC165AD3`.
- Vista del objetivo: `Exports/Interfaz_20261019/Objetivo_fauna_corregido.png`.

Evidencias: `output/qa/ui-final-buttons-20261019.log`,
`ui-motion-music-20261019.log`, `ui-motion-combat-20261019.log`,
`ui-spaced-build-final-20261019.log`, `ui-spaced-native-final-20261019.log` y
`ui-tree-export-20261019.log`, en la misma carpeta.

Los símbolos DoNotShip y un ZIP intermedio anterior a la corrección de una
advertencia de compilación se conservaron en `output/qa`, fuera de la entrega.
No se borraron las versiones anteriores ni se cerró el editor principal.
