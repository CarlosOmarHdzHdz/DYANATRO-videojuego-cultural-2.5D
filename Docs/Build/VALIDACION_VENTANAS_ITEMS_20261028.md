# Ventanas e ítems: validación

Resultado final: PASS con Unity 6000.3.11f1, en la copia aislada `output/qa/windows-20261016/project`. No se cambia la escena abierta en el editor principal ni se genera un ejecutable nuevo.

## Ventanas

La validación general de inicio y pausa verificó los ajustes nuevos, las confirmaciones, la ayuda de controles, navegación mediante teclado, conservación de foco, silenciamiento y preferencias, varias aperturas de inventario, recargas reales y ausencia de excepciones del juego. También se mantuvieron las pruebas de salud y reacción a los golpes.

La prueba específica verificó porcentaje de volumen, incrementos y decrementos, límites de 0 y 100 %, estado del interruptor de movimiento, cierre por X y por el botón inferior, recuperación del foco y conservación de valores al reabrir. Se revisaron capturas a 1600 × 900 y 1024 × 768.

## Ítems

Se pobló el morral de la copia de prueba con plantas, una flor de memoria, una infusión y un material. Se verificaron nombres, iconos, categorías y cantidades. La selección repetida mantiene los mismos objetos de tarjeta, sin destruir y recrear la cuadrícula. El filtro de plantas muestra únicamente ingredientes y los rótulos largos caben en sus tarjetas.

La preparación real consumió exactamente 2 manzanillas y 1 hierbabuena, generó una infusión y conservó la flor de memoria. El botón se deshabilitó al faltar ingredientes. Usar una infusión con 60 puntos de vida consumió una unidad y dejó la recuperación en espera mientras el morral estaba abierto; al cerrarlo, la recuperación gradual llegó a 100 puntos. Se verificó la familia tipográfica compartida.

## Evidencia

- Capturas y resultado: `Exports/Ventanas_Items_20261028`.
- Prueba general: `output/qa/modal-items-20261028.log`, marcador `[MENU STARTUP QA PASS]`.
- Prueba específica final: `output/qa/modal-items-focused-20261028-v2.log`, marcador `[MODAL ITEMS QA PASS]`.
- Validador específico: `Assets/Editor/Validation/XunjuuModalItemsValidation.cs`.
- El defecto preexistente del indexador `UnityEditor.Search.SearchDatabase` de la copia del editor se excluye del registro de excepciones del juego.
