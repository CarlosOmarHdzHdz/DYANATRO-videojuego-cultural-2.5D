# Ventanas e ítems

Las ventanas de ajustes, confirmación y ayuda comparten superficies translúcidas de verde petróleo, bordes finos redondeados y la familia Liberation Sans. El borde dibuja únicamente su contorno y no duplica el relleno de la tarjeta.

## Ajustes

La ventana «A tu ritmo» separa música y presentación en dos tarjetas. El volumen muestra su porcentaje e incorpora botones de diez puntos porcentuales, además del deslizador y su control con flechas. El volumen siempre queda entre 0 y 100 %. La presentación incluye una miniatura del paisaje y un interruptor con estado visible. El movimiento reducido detiene la deriva de la vista previa y los efectos de entrada, respetando la preferencia ya existente.

Se conservan los cambios inmediatos, el guardado de preferencias y el retorno del foco al botón que abrió la ventana. Hay cierre mediante X, Listo/volver y Escape. Abrir ajustes desde pausa no reanuda el juego ni la música.

## Confirmaciones y controles

Reiniciar, volver al inicio y salir presentan una tarjeta con icono de aviso, consecuencias de la acción y botones separados. «Seguir jugando» recibe el foco inicial. Las acciones y la conservación de las fichas mantienen el comportamiento anterior. La ayuda de controles conserva sus teclas dibujadas y adopta el nuevo marco.

## Ítems del morral

- Cuadrícula de cinco columnas con tarjetas de 136 × 132, iconos más grandes, nombre visible, categoría y etiqueta de cantidad.
- Categorías de memoria, plantas, preparados y materiales; la categoría aparece por escrito y se acompaña de un acento de color.
- Se reutilizan las ilustraciones existentes del juego, respetando su proporción. Las plantas y la infusión recuperan su icono desde el catálogo cuando el slot no lo tiene asignado. Los materiales sin sprite muestran un símbolo de recurso.
- El retrato del objeto seleccionado tiene una base clara para distinguir las plantas y una descripción con más espacio.
- La preparación muestra la infusión, su efecto dentro del juego y filas ilustradas de los ingredientes con existencias y cantidades requeridas. La receta conserva 2 manzanillas + 1 hierbabuena y la recuperación de hasta 40 puntos durante 5 segundos.
- Seleccionar objetos y cambiar filtros reutiliza tarjetas. Se elimina la destrucción y reconstrucción de toda la cuadrícula en cada actualización; se mantienen el desplazamiento y los componentes de los controles.

Los identificadores, las cantidades de la receta, las categorías y las mecánicas del inventario no cambian. La mejora se aplica a la presentación de los ítems en el morral.

## Implementación

`XunjuuModalStyle` centraliza las tarjetas y acciones; `XunjuuPresentationSettings` construye los ajustes; `DyanatroGameDirector` mantiene las confirmaciones y ayuda; `XunjuuInventoryUI` presenta las tarjetas y reutiliza sus vistas.
