# Identidad bordada de DYANATR’O

La portada y las transiciones de carga comparten el logo `DYANATR’O`, sin subtítulo ni texto promocional pequeño. El bordado multicolor incorpora maíz, hojas, flores, bosque, montañas y un venado como referencias al recorrido del juego. Es una ilustración inspirada en textiles; no se atribuyen significados tradicionales a los motivos inventados.

## Recurso e integración

- PNG con transparencia: `Assets/Resources/Sprites/UI/DYANATRO_Logo_Tejido_20261027.png` (2172 × 724).
- `XunjuuBranding` carga una única textura compartida mediante Resources y ajusta su rectángulo a la proporción nativa, sin estirar las letras ni interceptar clics.
- El importador conserva el canal alfa, desactiva mipmaps y lectura de CPU y evita el redimensionamiento a potencias de dos. No se reemplaza el paisaje anterior.
- El original del primer logo permanece conservado en `output/imagegen`. La versión nueva se generó con la herramienta integrada de imágenes; el prompt completo está en `LOGO_TEJIDO_20261027_PROMPT.txt`.

## Pantallas

En inicio se mantienen el paisaje y las cuatro acciones: Iniciar, Ludoteca, Ajustes y Salir. Por petición posterior se retiró el personaje decorativo del menú; el protagonista jugable se conserva. Se retiran la frase de memoria y territorio, el descriptor de exploración, Las sombras del olvido, la descripción y el pie decorativo de teclado. La navegación mediante teclado permanece activa.

En carga quedan el logo, el paisaje, el bordado exterior, el porcentaje y la barra proporcional al trabajo completado. Se retiran el título explicativo, las etiquetas de etapas y los consejos pequeños. El estado de la carga continúa disponible en `XunjuuSceneTransition.Status` para diagnóstico. Durante la preparación de la introducción se conserva el botón Continuar.

El avance no se simula ni retrocede. Se mantienen el fundido de salida y el respeto por la opción de movimiento reducido. No se añade ningún video de apertura ni se modifica la historia, las misiones, la ludoteca o la interfaz de la partida.

## Comprobación

La validación automatizada existente se amplió para revisar el recurso compartido, la proporción de las letras, el canal alfa configurado, la ausencia de subtítulos y el hecho de que el logo no capture clics. El resultado y las capturas de la ejecución se registran en `output/qa/windows-20261016/project/output/menu-logo-validation-20261027`.

Las pruebas se ejecutan en una copia aislada con Unity 6000.3.11f1. No se cambia la escena abierta en el editor del usuario ni se genera un ejecutable nuevo.
