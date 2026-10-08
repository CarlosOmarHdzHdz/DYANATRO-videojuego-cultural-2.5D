# Ludoteca: actividades jugables

La pantalla conserva el navegador interno, menú de madera oscura, fondo de manta y tres tarjetas
con acentos grana, añil y maíz, siguiendo la referencia de diseño. Fauna y sus
fichas desbloqueables siguen disponibles en el menú.

- **Memorama:** 18 parejas de motivos textiles en tres etapas de seis parejas.
  La baraja se mezcla en cada etapa, registra intentos y muestra una observación
  al descubrir una pareja. Dos tarjetas distintas no coincidentes se ocultan
  después de 1,2 segundos reales, incluso con el mundo pausado.
- **Sopa de letras:** cuadrícula aleatoria 10×10 con ocho términos. Cada partida
  coloca y comprueba las palabras antes de completar las letras de relleno;
  si no encuentra una distribución, usa filas mezcladas para garantizar que
  todas puedan resolverse. Seleccionar primera y última letra con ratón o toque;
  acepta ambos sentidos y destaca la selección con un motivo bordado.
- **Quiz:** cinco preguntas del vocabulario demostrativo existente. La posición
  correcta cambia. Los errores permiten reintentar sin avanzar; no se muestra éxito
  hasta identificar todos los significados.

Cada actividad muestra «¡EXCELENTE! Felicidades, completaste el minijuego» al cumplir su
objetivo, junto con su resultado y botones para repetir o regresar al menú.
No se guarda progreso de actividades entre sesiones. El vocabulario conserva su
carácter demostrativo: requiere revisión lingüística regional antes de publicación.

## Código y comprobaciones

- `Assets/Ludoteca/Runtime/XunjuuLudotecaBrowser.cs`: navegación y presentación.
- `Assets/Ludoteca/Runtime/XunjuuLudotecaBrowser.Gallery.cs`: fichas de galería.
- `Assets/Ludoteca/Runtime/XunjuuLudotecaBrowser.Minigames.cs`: actividades.
- `Assets/Ludoteca/Editor/Validation/XunjuuMinigameValidation.cs`: pruebas en Play Mode.
- Menú: `Tools > Xunjuu > Validar minijuegos`.
- Resultados y capturas: `output/minigames`.

Las pruebas ejecutan los callbacks reales de los botones. Cubren coincidencias,
errores, doble selección, reinicio, selección inversa, quiz y las tres pantallas de
éxito. No sustituyen una prueba táctil en un dispositivo Android físico.

## Acabado visual

El memorama usa 18 recortes cuadrados del atlas de motivos textiles. Sus reversos
muestran geometría en grana, azul y dorado. Al completar una actividad aparece
el sol ilustrado con transparencia en `Assets/Ludoteca/Resources/Ludoteca/Minigames/sol_felicidades_mazahua.png`
y el mensaje: «¡EXCELENTE! Felicidades, completaste el minijuego».

La sopa de letras utiliza una presentación académica: encabezado de actividad,
propósito de aprendizaje, filas A–J, columnas 1–10, guía de selección, vocabulario
con casillas de verificación y contador de avance. El tablero sigue siendo accesible
con ratón y toque, y no usa límite de tiempo.

## Rediseño visual del 23 de septiembre de 2026

Los ZIP `Ludoteca_Unity_Demo.zip` y `LudotecaDigitalWPF.zip` sirvieron como
referencias de jerarquía, paleta y contenido. Su código de demostración no sustituye
la lógica jugable de Unity. La lámina de iconos y la captura de interfaz entregadas
por el usuario se usaron como referencias visuales, no como fuente lingüística.

Dos atlas de ilustraciones con canal alfa se guardan en
`Assets/Ludoteca/Resources/Ludoteca/Icons/iconos_ludoteca_atlas.png` (tejedora, cofre,
hablante, venado, sol y diploma) y
`Assets/Ludoteca/Resources/Ludoteca/Icons/iconos_elementos_atlas.png` (maíz, vasija,
pavo, pergamino, banda textil y lluvia). Unity recorta cada celda en tiempo
de ejecución. Se crearon con el generador de imágenes integrado.

Inicio presenta una ruta de aprendizaje sobre la galería; Cultura y Lengua usan
fichas de lectura visual. El botón «Leer texto completo» permite consultar
los textos previos y regresar a las fichas sin salir de la ludoteca.

Prompts finales de generación:

1. `Transparent 3x2 Unity game UI atlas, six isolated evenly spaced warm earthy hand-painted icons: adult woman weaving on backstrap loom; wooden chest with textiles and pottery; elderly woman speaking; local deer; sun and puzzle piece; diploma. Follow the supplied visual references. Terracotta, indigo, muted gold, cream and dark umber outlines. No words, watermark, checkerboard or background. Preserve clear cell gutters and crisp silhouettes for small UI display.`
2. `Transparent 3x2 Unity game UI atlas in the same hand-painted style, six isolated objects: ripe yellow maize with leaves; brown geometric ceramic vessel; standing wild turkey; blank rolled parchment; horizontal grana-and-indigo woven textile; blue rain cloud. Follow the supplied visual references. Equal-size cells, transparent gutters, no labels, watermark, checkerboard or background.`

Los 18 motivos se recortan del atlas en cuadrados y se presentan sin nombre en
las cartas. El nombre y una observación aparecen únicamente al encontrar la pareja.

## Galería y actividades interactivas

La galería muestra nueve fichas grandes en cinco páginas de hasta dos fichas.
La portada destaca dos fotografías. Conserva las tres fotografías
acreditadas en `Assets/Ludoteca/Documentation/CREDITOS_IMAGENES.txt` y agrega seis
ilustraciones conceptuales del proyecto. Cada ficha se voltea en ambos sentidos
con tiempo no escalado —también funciona cuando la ludoteca pausa el mundo— y
muestra una explicación breve y su origen. Las ilustraciones se identifican
como tales y no se presentan como documentación histórica.

La barra lateral tiene hilos y rombos bordados, distingue la sección activa y numera sus seis rutas. Las
actividades ocupan el área completa de lectura, sin repetir el título general.
El memorama aprovecha el espacio inferior del tablero, anima el giro y muestra un dato al descubrir cada pareja; la sopa
usa casillas claras, coordenadas y guía didáctica; el quiz separa la pregunta,
el progreso y las respuestas sin avanzar tras un error.
La apertura de la ludoteca, los cambios de página, la selección bordada,
las preguntas y la felicitación usan animaciones breves en tiempo no escalado.
La prueba del menú `Tools > Xunjuu > Validar minijuegos` comprueba también las
cinco páginas de galería, el giro de ida y vuelta y los datos al reverso.
