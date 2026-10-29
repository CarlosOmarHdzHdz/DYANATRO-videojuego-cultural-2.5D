# Tipografía, menús y reacción al daño

## Familia tipográfica

Las interfaces usan Liberation Sans: textos, botones, controles, avisos, inventario, barra de vida, nombres del mundo y ludoteca. Se reutiliza la fuente libre ya incluida en TextMesh Pro; no se depende de las fuentes instaladas en Windows. La versión SDF de la ludoteca pertenece a la misma familia. Los pesos normal y negrita mantienen la jerarquía sin introducir otra tipografía.

`XunjuuTypography` centraliza los recursos y normaliza una sola vez los textos previamente serializados de la escena. Los constructores de interfaces nuevas asignan la familia compartida al crearse; no se realizan búsquedas globales cada fotograma. Las imágenes del logo bordado y de «HAS CAÍDO» se conservan.

## Inicio y pausa

Los botones de estos dos menús comparten `XunjuuMenuButtonStyle`: esquinas suaves, profundidad discreta, iconos de acción, indicador de avance y foco claro para teclado. Se conservan sus nombres, callbacks y distribución para mantener la navegación.

- Iniciar/Continuar: verde petróleo.
- Ludoteca: terracota.
- Controles: azul lavanda.
- Ajustes: azul pizarra.
- Reiniciar: ocre.
- Menú inicial: verde salvia.
- Salir: vino.

El rediseño de superficies se limita a inicio y pausa. Las demás interfaces adoptan la fuente uniforme. Los botones de la pantalla de muerte conservan sus motivos de papel picado y sustituyen la fuente Bangers por la familia común.

## Golpes

`PlayerController.SetHealth` detecta una disminución real y activa `XunjuuPlayerHitFeedback`. El gesto dura 0.34 segundos: destello cálido, pequeño vaivén y compresión del sprite. Las propiedades por renderer del shader `Xunjuu/AtlasSprite` animan exclusivamente los vértices dibujados del personaje: no desplazan su Rigidbody ni cambian su escala o collider. Se mantiene el daño real inmediato y el equilibrio existente del combate.

El efecto no se activa al curar, se limpia antes de la animación de muerte y se congela al pausar. Con movimiento reducido se conserva la señal de color, sin vaivén. Las propiedades nuevas del shader valen cero para los demás elementos del mundo.

## Salud

La barra bordada conserva el relleno continuo y el suavizado de daño y curación. El contador indica la salud real; el relleno se aproxima a ella progresivamente, sin alterar el resultado del combate. La transición invierte el sentido al recibir una curación durante un descenso.

Se corrige el avance de un fotograma que podía producirse al activar la pausa a mitad de una actualización. La barra, la reacción y el HUD alternativo comprueban explícitamente la escala de tiempo antes de avanzar. El personaje respeta su máximo serializado y Start ya no sobrescribe el daño que pudiera haber recibido tras Awake.

Las pruebas usan el preparado real del inventario: +40 puntos durante 5 segundos, pausa intermedia, proporción del relleno y límite máximo. La lógica de la receta y sus cantidades permanecen iguales.
