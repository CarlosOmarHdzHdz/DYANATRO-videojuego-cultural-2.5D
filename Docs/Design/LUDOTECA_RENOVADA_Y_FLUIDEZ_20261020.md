# Ludoteca renovada y reducción de trabajo repetido

## Interfaz

Se retiraron las dos imitaciones de navegador: las pestañas superiores y la barra
con dirección ficticia. No se eliminó ninguna sección ni recurso multimedia.
Una cabecera compacta identifica la ludoteca e incluye Volver y Cerrar. Inicio
es una opción real en el menú lateral, junto a las siete secciones anteriores.

Se unificaron las superficies claras, el contraste, los encabezados, los márgenes
y los acentos de sección. El bordado queda en los márgenes de la barra lateral.
Cada página incorpora una descripción breve. Las fichas de fauna se distribuyen
como las de herbolaria: nombre, imagen, información y acceso a la lectura.

El memorama conserva los 18 motivos y sus tres etapas. Las cartas son cuadradas,
más grandes y sin nombres sobre la imagen; la observación aparece separada del
tablero. La sopa conserva ocho términos, distribución aleatoria y selección con
bordado; sus celdas ahora también son cuadradas. El quiz y la evaluación mantienen
sus preguntas, puntuación y reglas; no se alteró el instrumento de evaluación.

ESC vuelve primero desde una lectura, ficha o actividad, y después permite salir
de la ludoteca. La preferencia de movimiento reducido también se aplica a las
entradas, tarjetas, letras bordadas y animaciones de respuesta de la ludoteca.

## Fluidez: cambios de implementación

- La cámara del mundo no renderiza detrás de la ludoteca opaca de pantalla
  completa cuando esta usa Canvas Overlay. Se conserva su referencia y se
  restaura al cerrar o desactivar el componente. Las UI que requieran cámara
  no usan esa suspensión. El contenido del escenario no se destruye.
- La guía busca objetivos cada 0,18 segundos, o antes si el objetivo se
  desactiva. Mantiene actualizada la dirección; no recrea la etiqueta si los
  metros y el nombre no cambiaron. No busca mientras la ludoteca o morral
  ocultan el HUD.
- La política visual clasifica una vez cada collider y renderer descubierto,
  en lugar de recorrer sus ancestros repetidamente en cada revisión.
- El optimizador reutiliza los componentes de árboles y cultivos mientras
  su jerarquía no cambie. Detecta objetos nuevos y elimina entradas expiradas.
  Una herramienta que cambie componentes dentro de la misma jerarquía puede
  llamar a InvalidateObjectCache para forzar una reconstrucción.
- La conversión visual recuerda sprites ya inspeccionados. Las tareas de
  descubrimiento se suspenden con el mundo pausado solo después de completar
  la preparación inicial. No dependen de un plazo arbitrario de arranque.
- Orden de profundidad y oclusión evitan recalcular resultados estáticos;
  la oclusión sigue comprobando cambios de estado de árboles en reposo.
- Las etiquetas ocultas evitan consultas de cámara; las visibles comparten
  la cámara del fotograma. El HUD conserva una caché de lienzos e invalida
  esa caché al registrar un HUD nuevo.
- El control del personaje no procesa movimiento ni ataques de entrada al
  estar pausado. Los clics de una interfaz no inician golpes en el escenario.

No se redujeron resolución, cantidad de viviendas, milpas, árboles, plantas,
enemigos ni efectos del perfil. No se cambiaron las traducciones ni se añadieron
afirmaciones culturales o médicas. No se modificaron los prefabs de animales
editados previamente por el usuario.

## Comparación diagnóstica

Promedios observados por llamada en el editor aislado, tras calentamiento:

| Rutina | Antes | Después |
| --- | ---: | ---: |
| Revisión de política visual | 39,92 ms | 2,42 ms |
| Actualización del registro de interacción | 24,15 ms | 3,84 ms |
| Guía en su ruta repetida/caché | 2,659 ms | 0,0022 ms |

Se forzaron 12 llamadas para las dos primeras rutinas y 120 para la guía en
el mismo fotograma. La guía en caché no representa el coste de una búsqueda
nueva. La escena generada tenía aproximadamente 5.600 renderers (5.578 antes,
5.591 después); no es una prueba de escenas idénticas píxel por píxel.
El contador de asignación de Mono devolvió cero y no se utiliza como evidencia
de ausencia de GC. Estos tiempos no son FPS, ni una garantía de rendimiento
en otro equipo. No se ha medido aquí un incremento global de FPS en Windows.
