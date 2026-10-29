# Validación de pantalla de muerte e inicio

Resultado final: PASS con Unity 6000.3.11f1, ejecutado en la copia aislada `output/qa/windows-20261016/project`. No se cambió la escena abierta en el editor principal ni se compiló un ejecutable nuevo.

## Comprobaciones

- El inicio mantiene sus cuatro acciones y ya no instancia el personaje decorativo; el protagonista jugable existe y conserva salud al comenzar.
- Se provocó la muerte mediante `PlayerController.SetHealth(0)`, con el inventario abierto para comprobar su cierre y la restauración correcta de la pausa.
- La derrota oculta HUD y ayudas, deshabilita el controlador, pausa el mundo y la música, y muestra la ilustración nueva por encima de los paneles del director.
- Se revisaron capturas de Unity en 1600 × 900 y 1024 × 768; no hay texto cortado ni botones superpuestos.
- Reintentar recibe el foco inicial. La flecha derecha selecciona Menú inicial. La activación por Enter funciona.
- Dos invocaciones seguidas de Reintentar producen una sola transición. Se verificó una recarga real y la recuperación del estado jugable con salud.
- Se repitió la muerte con movimiento reducido: la pantalla aparece sin animación de entrada. Menú inicial recarga la escena y devuelve el foco a Iniciar.
- Sin excepciones del juego durante la ejecución final. El defecto preexistente del indexador `UnityEditor.Search.SearchDatabase` se excluye del registro de fallos del juego.

## Evidencia

- Validador: `Assets/Editor/Validation/XunjuuDeathScreenValidation.cs`.
- Log final: `output/qa/death-screen-20261027-v2.log`.
- Resultado y capturas: `Exports/Pantalla_Muerte_DiaDeMuertos_20261027`.
- Prompt y detalles del recurso: `Docs/Design/PANTALLA_MUERTE_20261027.md` y `Docs/Design/PANTALLA_MUERTE_20261027_PROMPT.txt`.
