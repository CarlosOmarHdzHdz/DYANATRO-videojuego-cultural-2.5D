# Logo bordado en carga e inicio

Se integró la nueva identidad DYANATR’O en `BuildMainMenuInterface` y en `XunjuuSceneTransition`. La textura se comparte mediante `XunjuuBranding`. Se eliminaron los subtítulos y las letras pequeñas decorativas de ambas pantallas, conservando los botones del menú y el porcentaje con su barra de carga real.

## Verificación ejecutada

`XunjuuMenuStartupValidation.Run` terminó con resultado **PASS** en Unity 6000.3.11f1 el 27 de octubre de 2026. Se utilizó la copia aislada `output/qa/windows-20261016/project`; no se operó la escena abierta en el editor principal.

Se verificó la carga del logo en ambas pantallas, proporción nativa, configuración del canal alfa, ausencia de captura de clics por la imagen, ausencia de subtítulos y textos decorativos pequeños, avance proporcional y monotónico, movimiento reducido, navegación por teclado, colores y contraste de botones, apertura de ludoteca y ajustes, pausa, controles, reapertura del morral, confirmaciones, reinicio real de escena y retorno al menú. No se registraron excepciones del juego en la prueba.

Se revisaron visualmente las capturas de inicio y carga a 1600 × 900 y 1024 × 768. El logo no se recorta ni se deforma y no invade los controles. La textura PNG conserva transparencia real (alfa 0 en la esquina). El índice de búsqueda del editor aislado emitió su excepción preexistente `UnityEditor.Search.SearchDatabase`, excluida explícitamente de la lista de errores del juego por el validador existente.

- Registro de Unity: `output/qa/logo-tejido-20261027.log`.
- Resultado original: `output/qa/windows-20261016/project/output/menu-logo-validation-20261027/result.txt`.
- Capturas de carga e inicio y copia del resultado: `Exports/Interfaz_Logo_Tejido_20261027/`.
- Diseño e integración: `Docs/Design/IDENTIDAD_INICIO_Y_CARGA_20261027.md`.
- Prompt de la herramienta integrada de generación de imágenes: `Docs/Design/LOGO_TEJIDO_20261027_PROMPT.txt`.

Esta ejecución valida la presentación y sus flujos, no constituye una prueba manual completa ni una medición de FPS. No se creó un nuevo ejecutable ni se publicó en GitHub.
