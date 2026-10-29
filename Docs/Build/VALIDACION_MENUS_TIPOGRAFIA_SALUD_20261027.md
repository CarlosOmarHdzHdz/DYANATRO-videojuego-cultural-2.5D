# Validación: tipografía, botones y reacción al daño

Resultado final: PASS en Unity 6000.3.11f1. Se empleó la copia aislada `output/qa/windows-20261016/project`; no se alteró la escena abierta en el editor principal. No se generó un ejecutable nuevo.

## Tipografía y menús

- La comprobación recorre los textos clásicos y TMP cargados y confirma la familia compartida Liberation Sans. Se comprueban ñ y vocales acentuadas. Logo bordado y título ilustrado de derrota conservados.
- Capturas de inicio y pausa a 1600 × 900 y 1024 × 768: iconos presentes, siete acciones separadas en pausa, etiquetas dentro de sus cajas y contraste de botones de al menos 4.5:1 en estado normal.
- Se comprueba la existencia de CanvasRenderer y geometría en los iconos activos.
- Mouse, flechas, Tab, Enter, ajuste de volumen, movimiento reducido, apertura/cierre de ludoteca e inventario, confirmaciones y recargas reales de Reiniciar/Menú inicial superados.

## Salud y golpes

- El daño reduce la salud real inmediatamente y activa la reacción del sprite. No cambia la posición ni escala físicas del personaje.
- Se muestrea el descenso intermedio del relleno y su proporción final; se verifica que una curación durante el descenso invierta correctamente la dirección.
- Pausa inmediata: el relleno, el destello y el vaivén se congelan en el mismo estado. El shader se limpia al terminar la reacción.
- El preparado real del inventario recupera 40 puntos durante 5 segundos. Se pausa a mitad de recuperación y se comprueba su detención, reanudación, resultado final y límite máximo.
- Con movimiento reducido queda el destello sin vaivén. Curarse no dispara una reacción de golpe.
- Prueba adicional de la barra bordada a 100, 75, 50, 25, 1 y 0 puntos; máximo de 200, límites, inicio sin sobrescribir salud, dirección y recuperación. Se comprueba que PlayerController respeta un máximo serializado de 200.
- Una muerte durante la reacción limpia el destello y mantiene el flujo de derrota, reintento y regreso al inicio.

## Evidencia

- `Exports/Menus_Tipografia_Salud_20261027`: menús, detalle del protagonista normal/al recibir daño, barra y resultados.
- Logs finales: `output/qa/presentation-combat-20261027-v4.log` y `output/qa/presentation-health-death-20261027.log`.
- Validadores: `XunjuuMenuStartupValidation`, `XunjuuPresentationCombatChecks`, `XunjuuHealthVisualValidation` y `XunjuuDeathScreenValidation`.
- Sin excepciones del juego durante las ejecuciones finales. El fallo preexistente del indexador `UnityEditor.Search.SearchDatabase` del editor aislado queda excluido de la captura de errores del juego.
