# DYANATR’O: aldea, memoria y saberes del huerto

## Secuencia jugable

Tutorial de controles → cinco flores de memoria → registro de seis animales →
Saberes del huerto → identificación y preparación → Macuahuitl → cinco sombras →
giro protector → Ocelotl → cierre del recorrido.

Las flores originales siguen enseñando vocabulario y conservan su identidad.
Ahora se clasifican como objetos de memoria del morral; no son ingredientes.
La misión medicinal empieza únicamente después de las seis capturas. El combate
se abre después de identificar correctamente la planta y fabricar la infusión,
no al recoger las flores ni solamente al registrar animales.

## Saberes del huerto

1. Recolectar con G dos manzanillas y una hierbabuena en el huerto.
2. Consultar sus dos fichas desbloqueadas en Ludoteca > Herbolaria.
3. Identificar la manzanilla por las flores blancas con centro amarillo.
   La respuesta incorrecta ofrece una pista y permite volver a consultar.
4. Combinar los tres ingredientes reales en I > Morral. No se otorgan recursos
   gratis. Esta preparación desbloquea el arma y el combate.
5. Usar H cuando falte salud. Es aprendizaje adicional: se puede continuar con
   salud completa sin gastar el preparado ni tener que hacerse daño.

La infusión recupera hasta 40 puntos en cinco segundos de tiempo jugable.
La pausa detiene la recuperación; no supera el máximo, no se apila, no revive y
no se consume con vida completa. La receta y el efecto son ficción, no consejos
médicos. Árnica, sábila, romero y toronjil son ítems de registro educativo; no se
inventan recetas para ellos.

Se observan recolecciones, consultas, fabricación y recuperación reales. La
pregunta de identificación es una actividad formativa, no un instrumento validado
de tesis. Las fichas persisten mediante PlayerPrefs; el morral y la misión son
de la sesión. No se introduce un nuevo guardado completo.

## Territorio y colocación

Se mantienen 24 candidatos de vivienda; solo se colocan los que superan las
pruebas de terreno y reservas. Los accesos tienen curvas, desviaciones laterales
y ancho variable. El camino visible y la reserva usan la misma línea central.
Las milpas conservan sus sprites de maíz y respetan casas, caminos y huerto.

Los árboles activos se representan con formas artísticas de pino y encino. La
comprobación incluye el radio horizontal de la copa y 0,65 unidades adicionales.
Los árboles que invaden reservas se buscan reubicar sobre terreno válido, sin
borrarlos. Se evita que el optimizador reactive los antiguos sprites de árboles.

Las seis plantas medicinales permanecen en un huerto cultivado, no distribuidas
como si todas fueran flora silvestre autóctona. La vegetación decorativa sin
identificación botánica no se etiqueta como una especie regional certificada.
Véase [revisión de fuentes y límites](REVISION_FLORA_REGIONAL_20261019.md).

## Interfaz indie

Verde profundo, manta, latón y bordado discreto; superficies redondeadas con
geometría nativa, iconos detallados y separación de áreas funcionales.

El morral separa TODO, MEMORIA, PLANTAS y PREPARADOS. La cuadrícula ocupa una
columna; la ficha del ítem va debajo; la receta tiene su propia columna. Se
muestran las casillas de la capacidad real, con desplazamiento cuando haga falta.
Abrirlo pausa el mundo y oculta el resto del HUD. Cerrarlo recupera el estado previo.

Los lienzos conservan el diseño completo en distintas proporciones de pantalla.
La importación mejorada afecta solo a los seis recursos herbales y los dos iconos
detallados de flor e infusión; no cambia el filtrado del pixel art del protagonista.

## Un solo resultado de misión

XunjuuMissionNotice atiende flores, fauna, combate y herbolaria mediante una
sola tarjeta central. Filtra repeticiones y oculta el aviso durante la pausa.
Arriba muestra vocabulario existente; abajo, el resultado en español. Las
palabras guía no son traducciones literales de todo el mensaje inferior.

Se conservan jñaa, jñiñi y b'epji como vocabulario del proyecto. Los nuevos relatos
están en español y se distingue la ficción de los contenidos culturales. Se
requiere revisión lingüística/comunitaria antes de certificar textos mazahuas.

## Pruebas

- XunjuuSettlementValidation.Run: casas, curvas, accesos, reservas, copas de árboles,
  maíz en sprites, huerto, movimiento y recursos visuales.
- XunjuuHerbalLearningValidation.Run: cinco ítems de memoria, seis capturas, orden
  del huerto, cosecha real, dos fichas, identificación, receta, apertura del
  combate, pausa del morral, salud y conservación de flores de memoria.
  Comprueba separación de paneles y altura del texto del morral. Genera capturas
  con lienzos simulados 16:9, 16:10, 4:3 y ultrapanorámico.
- XunjuuTwoLevelRuntimeValidation.StartValidation: combate, armas, jefe y cierre.
- XunjuuMusicPauseValidation.Run: silencio antes de jugar, pausas y reanudación.

Se ejecutan en copia aislada y restauran los registros usados. No sustituyen
una partida manual completa ni una evaluación educativa con participantes.
