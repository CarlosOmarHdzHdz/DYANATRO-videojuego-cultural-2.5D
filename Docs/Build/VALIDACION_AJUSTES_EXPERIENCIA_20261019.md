# Validación de ajustes y experiencia

## Cambios

Se conserva el mundo y contenido de la entrega anterior. Se incorporan ajustes
de música y movimiento de menús, navegación explícita con foco visible,
confirmación de acciones que descartan la sesión y destinos distintos para
reiniciar o regresar al menú. Véase
[diseño y límites](../Design/AJUSTES_Y_NAVEGACION_20261019.md).

## Evidencia de ejecución en copia aislada

- `presentation-usability-final-20261019.log`: PASS y código de salida 0.
  Cero excepciones del juego observadas desde Awake; creación inactiva y
  reapertura de CanvasGroup; cuatro ciclos del morral; siete acciones separadas
  en pausa; navegación mediante eventos de flechas y Submit de Unity; ajustes
  que no inician el juego; foco seguro y recuperación del botón de origen;
  volumen en menú, pausa y partida; movimiento reducido; confirmación,
  cancelación y recargas reales para Reiniciar y Menú inicial. La prueba
  restaura las preferencias anteriores.
- `presentation-regression-herbal-20261019.log`: PASS y salida 0. Cinco
  flores-ítem, seis registros de fauna, huerto, fichas, identificación,
  preparación real, consumo de ingredientes y recuperación gradual de salud.
- `presentation-regression-music-20261019.log`: PASS y salida 0. Silencio
  previo al recorrido, tres ciclos de pausa, continuidad de muestras de la
  pista, morral, ludoteca y pausa externa sin reiniciar la música.
- `presentation-regression-combat-timing-20261019.log`: PASS y salida 0.
  Recorrido completo: flores, fauna, aprendizaje de plantas, macuahuitl,
  cinco sombras, giro protector, Ocelotl y cierre de nivel.

Una comprobación antigua de cooldown falló por comparar 550 ms de espera de
pared con un cooldown de 620 ms de juego: el editor puede atender la espera
después de ese plazo. Se verifica ahora el deadline real establecido y el
rechazo dentro de una ventana futura controlada en el mismo frame. No se
modificaron PlayerController ni sus valores de combate para hacer pasar la
prueba. El nivel musical esperado también respeta la preferencia elegida.

La primera importación de un script nuevo detectó un GUID con longitud errónea;
se corrigió antes de ejecutar las pruebas y generar la entrega. Los logs fallidos
se conservan como diagnóstico, no como evidencia de la versión final.

## Revisión visual y límites

Capturas de inicio, pausa, ajustes y confirmación en
`Exports/Experiencia_20261019`. Ajustes y pausa se renderizaron también en 4:3;
se inspeccionaron distribución, textos y tamaño del control de volumen. No se
han medido FPS ni certificado compatibilidad de todos los mandos o lectores de
pantalla. Se comprueba el mecanismo de navegación con eventos del sistema UI,
no una sesión manual completa con cada teclado físico.

Los diagnósticos previos de UnityEditor.Search.SearchDatabase se excluyen solo
por su traza específica en la prueba de arranque; no se ocultan excepciones
de componentes del juego. Ningún editor del usuario se cerró ni se reinició.

## Compilación y paquete final

- `presentation-release-build-20261019.log`: compilación Windows x64/Mono
  completa y salida 0. Se mantienen las escenas habilitadas y el perfil sin
  stripping administrado ni eliminación de módulos del motor.
- `presentation-release-native-20261019.log`: arranque del EXE en batch sin
  gráficos, inicialización correcta y sin excepciones detectadas. Se cerró
  únicamente el proceso iniciado para esta comprobación. No sustituye una
  sesión manual gráfica completa ni prueba todos los equipos Windows.
- Video de StreamingAssets idéntico al original por SHA-256.
- ZIP: `Builds/Windows/DYANATRO_Windows_Experiencia_Mejorada_20261019.zip`.
- 182 archivos, 278.643.481 bytes sin comprimir; ZIP de 102.185.965 bytes.
- SHA-256: `608B48ADB4AEB7447E071B370E6B09A9010F45351F30CC8CB61BF5F69B08F3A9`.
- Todas las entradas del ZIP se abrieron y leyeron; su número coincide con la
  carpeta de distribución. Los símbolos DoNotShip se conservaron fuera del
  paquete en `output/qa/presentation-release-symbols-20261019`.

Se conservan las versiones anteriores. No se realizó commit ni publicación en
GitHub durante esta mejora; la entrega y sus fuentes están en el equipo local.
