# Derrota con papel picado y tarjeta translúcida

Resultado: PASS en Unity 6000.3.11f1, en la copia aislada del proyecto. No se cambia la escena abierta en el editor principal ni se genera un ejecutable.

Se verificaron el relleno de tarjeta al 60 %, la ausencia de Outline que duplicaría su opacidad, la textura del título con transparencia, la fuente decorativa Bangers de los botones y las tres orlas florales. Se revisaron capturas de Unity a 1600 × 900 y 1024 × 768: el fondo permanece visible, el texto cabe y los controles no se superponen.

La misma ejecución provocó una muerte real con el inventario abierto, comprobó el ocultamiento del HUD y la pausa de música y mundo, navegó entre los botones, reintentó mediante recarga real, repitió la muerte con movimiento reducido y volvió al inicio mediante Enter. Sin excepciones del juego. El defecto preexistente del indexador del editor UnityEditor.Search.SearchDatabase se excluye del registro.

- Evidencia: `Exports/Pantalla_Muerte_PapelPicado_20261027`.
- Log: `output/qa/death-screen-papel-picado-20261027.log`.
- Validador: `Assets/Editor/Validation/XunjuuDeathScreenValidation.cs`.
- Título generado con la herramienta integrada de imágenes: `Assets/Resources/Sprites/UI/DeathTitle_PapelPicado_20261027.png`.
- Prompt completo: `Docs/Design/TITULO_MUERTE_PAPEL_PICADO_20261027_PROMPT.txt`.
