# Interfaz espaciada y exportación de árboles actuales

## Alcance confirmado

La superposición señalada estaba en la franja del objetivo: el bordado inferior
competía con las dos líneas de texto. Esta revisión no cambia las mallas, el
diseño ni la distribución de los árboles. Los cambios de vegetación que ya
existían antes de esta revisión se conservan; no se revierten ni se amplían.

## Interfaz

- Una tarjeta común para flores, fauna, huerto, sombras y cierre, de 660×120
  unidades de diseño. El título, contador, cuerpo y progreso tienen rectángulos
  distintos. El único rombo está en el margen del encabezado, no bajo las letras.
- Contador de avance independiente; barra interpolada sin alterar el progreso
  real de la misión. El huerto muestra los tres pasos previos al combate.
- Revelado de paneles con opacidad y escala contenida durante 0,22 segundos.
- Respuesta de botones a puntero, pulsación y selección con suavizado
  independiente de la tasa de cuadros y tiempo no escalado. Sigue funcionando
  con el mundo pausado. Crecimiento máximo de 1,2%, sin desplazar anclas.
- Botones de la ludoteca con geometría redondeada. Las cartas sin etiqueta y
  las letras de la sopa quedan fuera de la animación de escala para no competir
  con su volteo ni mover el tablero.
- Los adornos inferiores de Frame se omiten en placas de menos de 180 unidades
  de alto. Los paneles mayores conservan el bordado donde tiene margen propio.
- El morral no reconstruye todas sus casillas cuando cambia el inventario
  estando cerrado. Su estado se actualiza al abrirlo; el estado de receta se
  consulta a intervalos de 0,1 segundos, además de al cambiar recursos.

No se afirma una mejora medida de FPS: son cambios concretos de presentación y
trabajo de UI. Se mantienen las mecánicas, el orden de misiones y la música.

## Modelos para Blender

El exportador XunjuuTreeExport utiliza XunjuuArtMeshes.Get sin alterar su resultado.
Convierte orientación, conserva geometría y colores, y empaqueta un material PBR
mate. Los GLB tienen sus recursos integrados, sin texturas externas.

Paquete activo: Exports/Blender/Arboles_DYANATRO_para_Blender_20261019.zip.

- Pino_actual.glb: 6.900 vértices, 2.300 triángulos.
- Encino_actual.glb: 4.932 vértices, 1.644 triángulos.
- Dos PNG de referencia y LEEME con importación y límites del material.

La variante antigua no activa se guarda en la exportación completa, pero no en
el paquete de los dos árboles actuales. El shader específico de Unity no se
convierte en un shader de Blender: pueden variar iluminación y oclusión. No son
archivos .blend nativos y no se instaló Blender en este equipo para probarlos.

## Comprobaciones

XunjuuHerbalLearningValidation reproduce el momento de tres animales registrados:
comprueba límites, altura del texto, separación entre etiquetas, ausencia de
puntadas bajo el cuerpo y barra a mitad de avance. Captura el objetivo en 16:9,
4:3 y un detalle de 800×180. Comprueba también el hover y su retorno con tiempo
del mundo en cero, además de las pruebas existentes del morral y aprendizaje.

Los GLB se verifican por cabecera/versión, longitud, rangos de buffer, atributos
y límites de índices de triángulo. Se abren y leen todas las entradas del ZIP.
Esta comprobación estructural no equivale a una importación visual en Blender.
