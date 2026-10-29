# Validación de arranque, salida y fondos

## Corrección

La animación de entrada del morral ya exige CanvasGroup y recupera la referencia
con comprobaciones de nulo de Unity tanto en Awake como en OnEnable. La corrutina
comprueba su existencia antes de escribir. Se evita ?? para esa referencia nativa.
Esto cubre creación inactiva, caché perdida y reapertura, no solamente un arranque
del ejecutable. No se eliminan la animación, el inventario ni sus funciones.

## Pruebas

- PASS: XunjuuMenuStartupValidation observa errores desde Awake. Cero excepciones
  del juego; un CanvasGroup por animador, tres ciclos de creación inactiva/caché
  restablecida, cuatro ciclos de apertura del morral.
- PASS: seis acciones de pausa, botones separados, confirmación de salida,
  cancelación sin reanudar, reanudación que cierra el diálogo y botón final que
  termina Play mediante QuitGame en el editor. En Windows llama a Application.Quit.
- PASS: fondo cargado como Texture2D, capturas de inicio, pausa y confirmación;
  pausa también renderizada en proporción 4:3.
- PASS: regresión de aprendizaje, recolección, inventario, fichas, preparación,
  recuperación, avisos, minijuegos y distribución de interfaces.
- PASS: silencio inicial, pausa musical, morral, ludoteca y continuidad de pista.
- PASS: compilación Windows x64/Mono con todos los recursos del perfil y sin
  recorte de código; video coincidente con el original por SHA-256.
- Arranque nativo revisado en modo batch sin gráficos, sin excepciones del juego.
  No equivale a recorrer manualmente todo el ejecutable ni probar el cierre
  gráfico en todos los sistemas Windows.

La excepción preexistente de UnityEditor.Search.SearchDatabase del editor QA se
excluye por su traza específica; no se ignoran errores de componentes del juego.
La nueva comprobación detectó además una importación inicial del arte como
cubemap: se fijó explícitamente Texture2D y el metarchivo definitivo lo conserva.

## Paquete

`Builds/Windows/DYANATRO_Windows_Menus_Ilustrados_20261019.zip`.

- 182 archivos; 278.631.194 bytes sin comprimir.
- ZIP: 102.182.189 bytes.
- SHA-256: `C700BACC698BE11D4D6B83F4D7D1AFDBC77DE9C65791FD9492F37A136F926D7F`.
- Todas las entradas se abrieron y leyeron; su número coincide con la carpeta.
- Se conservaron las entregas anteriores. Los símbolos DoNotShip se trasladaron
  sin borrarlos a `output/qa/menu-final-symbols-20261019`.

Evidencias en `output/qa`: `menu-startup-backgrounds-retry-20261019.log`,
`menu-final-gameplay-20261019.log`, `menu-final-music-20261019.log`,
`menu-final-build-20261019.log`, `menu-final-native-20261019.log`.
Las capturas entregables se copiaron a `Exports/Menus_Ilustrados_20261019`.

El [documento de arte](../Design/FONDOS_MENU_Y_PAUSA_20261019.md) conserva el prompt
final, la ruta del PNG y el uso de imagegen integrado. La misma ilustración usa
tratamientos de color distintos en inicio y pausa. No cambia el mundo jugable.
