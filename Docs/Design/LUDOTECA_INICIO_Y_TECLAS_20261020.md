# Ludoteca: portada, transición y controles

## Corrección del aviso sobre la ludoteca

El aviso de Unity «Display 1 / No cameras rendering» aparecía porque la optimización anterior deshabilitaba la única cámara. No era una imagen de la ludoteca. Ahora la cámara permanece habilitada: mientras se usa la ludoteca opaca en un Canvas overlay, su máscara de dibujo es cero y limpia el fondo con un color sólido. Al cerrar se recuperan su máscara, fondo y modo de limpieza originales. No se cambia el paisaje ni se eliminan objetos.

## Inicio de partida

- El inicio se protege contra clics repetidos y cierra cualquier ludoteca o ajuste abierto antes de iniciar la secuencia.
- Se limpia el foco del botón inicial y se restaura el tiempo de juego.
- Antes de preparar el video se permite dibujar un estado de carga con ayuda para saltar la introducción.
- La preparación tiene un límite de cuatro segundos medidos sin depender de la pausa. Si el reproductor deja de avanzar durante seis segundos, se continúa al prólogo. El video original sigue incluido.
- El prólogo utiliza tiempo no escalado; al terminar devuelve el tiempo a uno. No se puede abrir la ludoteca sobre la introducción.

Estas protecciones cubren estados de pausa y esperas del reproductor; no constituyen una medición de rendimiento en todos los equipos.

## Portada y presentación

Inicio tiene un panel editorial y tres rutas: galería, fauna/herbolaria y minijuegos. Las fotografías completas y sus fichas reversibles siguen en Galería. La fotografía de muestra conserva el crédito y licencia. Los accesos y colores reutilizan el sistema gráfico existente, con bordado discreto en la navegación, superficies redondeadas y jerarquía de lectura. No se vuelve a introducir una barra de dirección ficticia.

## Ayuda de controles

La ayuda desplegable y la de pausa comparten glifos nativos de interfaz: cuatro teclas WASD, barra espaciadora, teclas de acción y Escape. El ratón dibuja el botón izquierdo resaltado. Las teclas tienen base, relieve, borde y brillo; las esquinas son más pequeñas que las de los paneles. No se cargan texturas nuevas para esos glifos. Se mantienen las descripciones y las restricciones de acciones según el avance del juego.

## Alcance

No se alteran árboles, casas, caminos, milpas, animales, recetas, objetivos, historia ni contenidos educativos en esta revisión. Se mantienen las animaciones de páginas, botones y fichas, y la preferencia de movimiento reducido.
