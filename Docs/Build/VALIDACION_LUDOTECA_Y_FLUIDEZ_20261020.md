# Validación de ludoteca y fluidez · 20 de octubre de 2026

## Alcance

Rediseño de la ludoteca y reducción de trabajo repetido, sin sustituir los
recursos culturales, los animales, el mapa ni la secuencia de misiones.
Se conservaron las modificaciones previas del usuario. No se hicieron commits
ni publicaciones en GitHub durante esta entrega.

El [documento de diseño](../Design/LUDOTECA_RENOVADA_Y_FLUIDEZ_20261020.md)
describe la navegación, la suspensión del renderizado de fondo, las cachés y
los límites de las mediciones. Las capturas están en
`Exports/Ludoteca_Renovada_20261020`.

## Comprobaciones de interfaz

`XunjuuLudotecaDesignValidation`: no hay pestañas ni dirección ficticia; ocho
destinos laterales; texto y botones sin encimarse en la navegación; interacción
por raycast de puntero; cámara del mundo suspendida y restaurada; movimiento
del personaje ignorado durante pausa; apertura y cierre repetidos sin recuperar
un estado de tiempo obsoleto.

Se renderizaron las ocho páginas, la lectura de galería y los tres minijuegos.
Se revisaron sus capturas; herbolaria y sopa también se capturaron en 4:3.
Se verificaron los 18 pares del memorama, cartas cuadradas sin nombres, retorno
correcto del encabezado al salir, ocho palabras aleatorias y resolubles con
selección bordada, y finalización del quiz. La prueba restaura las fichas y
preferencias que encontró antes de ejecutarse.

## Regresión del escenario y preparación inicial

`ludoteca-world-ready-20261020.log`: PASS y salida 0. Conserva 18 viviendas,
10 milpas, 19 accesos conectados, 500 sprites de maíz y 24 plantas medicinales
de seis especies en el huerto. Comprueba reservas libres, desplazamiento
ortogonal, escala de caminata, cinco flores y morral.

Una prueba previa detectó que suspender el descubrimiento tras 12 segundos
podía anticiparse a un arranque lento. Se sustituyó el plazo por la condición
real de preparación de árboles y parcelas. El primer log fallido se conserva
como diagnóstico, no como validación de la versión final.

Las pruebas de arranque y menús de `ludoteca-menu-regression-20261020.log`
terminaron con PASS y salida 0: CanvasGroup, reaperturas del morral, navegación,
ajustes, confirmaciones y destinos distintos de recarga.

## Medición acotada

`ludoteca-baseline-20261020.log` y `ludoteca-performance-after-20261020.log`
terminaron con salida 0. Los JSON antes/después se conservaron junto a las
capturas. Se observaron menores costes de las rutinas repetidas: política
visual 39,92 → 2,42 ms; registro del optimizador 24,15 → 3,84 ms; ruta caliente
de guía 2,659 → 0,0022 ms por llamada.

Son microbenchmarks del editor y no una medición de FPS del ejecutable. No se
infieren porcentajes globales de mejora ni ausencia de asignaciones de memoria.
No se redujo calidad gráfica para producir esos resultados.

Los errores preexistentes de UnityEditor.Search.SearchDatabase se excluyen por
su traza específica cuando se recogen errores de interfaz; no se ignoran
excepciones de los componentes del juego. Las pruebas usaron una copia aislada
y no cerraron ni reiniciaron el editor del usuario.

## Pruebas finales y distribución

Con el código final, terminaron con PASS y salida 0:

- `ludoteca-release-ui-20261020.log`: interfaz, cámara, entradas pausadas,
  aperturas y cierres repetidos, navegación y los tres minijuegos.
- `ludoteca-release-learning-20261020.log`: cinco flores, seis registros,
  identificación de plantas, fichas, ingredientes y recuperación gradual.
- `ludoteca-release-music-20261020.log`: silencio inicial, pausa, muestras
  conservadas, morral, ludoteca y cámara compartida restaurada.
- `ludoteca-release-combat-20261020.log`: progresión completa hasta Ocelotl
  y cierre del nivel.
- `ludoteca-release-build-20261020.log`: Windows x64/Mono sin stripping
  administrado ni eliminación de módulos del motor; compilación completa.

`ludoteca-release-native-20261020.log`: arranque del EXE en batch sin gráficos,
inicialización correcta y cero excepciones detectadas. Esto no sustituye una
sesión manual gráfica completa ni un perfil de FPS en distintos equipos.
Solo se cerró el proceso iniciado expresamente para esta comprobación.

Paquete: `Builds/Windows/DYANATRO_Windows_Ludoteca_Renovada_20261020.zip`.

- 182 archivos; 278.648.168 bytes sin comprimir.
- ZIP de 102.187.701 bytes.
- SHA-256: `43F74EE1B6A6DD7367169A7D1ABD6E17379C6B4460E85E02C672517057726B16`.
- Todas las entradas se abrieron y leyeron; el número coincide con la carpeta.
- Video original conservado por comparación SHA-256 con StreamingAssets.
- Símbolos DoNotShip conservados en `output/qa/ludoteca-release-symbols-20261020`,
  fuera de la distribución. No se borraron las entregas anteriores.
