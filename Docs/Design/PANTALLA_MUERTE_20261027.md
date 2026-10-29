# Pantalla de derrota: entre flores y memoria

La pantalla se inspira visualmente en el Día de Muertos de México: cempasúchil, veladoras, papel picado, pan de muerto y calaveritas decoradas. La ilustración es una escena ficticia para el videojuego; no pretende documentar una ceremonia mazahua específica.

El centro mantiene el logo bordado y el mensaje «HAS CAÍDO», ahora con letras multicolor de papel picado, flores y puntadas. Las acciones Reintentar y Menú inicial usan ámbar y magenta, la familia Liberation Sans compartida con el resto de las interfaces y orlas florales. El texto aclara que reintentar comienza una partida nueva; no se promete restaurar un punto de control inexistente. Las decoraciones quedan en los bordes del paisaje, sin competir con las acciones.

La tarjeta central tiene un relleno ciruela al 60 % de opacidad. Se desactiva su Outline porque ese efecto duplica toda la geometría y vuelve casi opacas las superficies translúcidas. La nueva orla dibuja únicamente líneas, flores y puntadas, manteniendo visible el fondo a través de la tarjeta. Los textos de explicación conservan una fuente de lectura sencilla.

La imagen se ajusta a la proporción de la pantalla sin estirarse. Textos y botones son componentes nativos de Unity. La entrada se revela con un fundido breve, desactivado cuando se elige movimiento reducido. La navegación funciona con mouse, Tab, flechas y Enter.

Al detectar la muerte se cierran inventario, ajustes, confirmaciones y ludoteca, se ocultan HUD y ayudas, se permite terminar la animación de caída y después se desactiva el control del personaje y se pausa la partida. La música permanece detenida. La pantalla se coloca sobre los demás paneles del director.

Se retiró únicamente el personaje decorativo del menú de inicio. No se eliminan el protagonista, las misiones, el combate ni otros contenidos.

## Recurso

- Fondo: `Assets/Resources/Sprites/UI/DeathOfrenda_20261027.png`.
- Generado con la herramienta integrada de imágenes; prompt completo en `PANTALLA_MUERTE_20261027_PROMPT.txt`.
- Una textura, sin mipmaps ni lectura de CPU, ajustada mediante AspectRatioFitter. No se crean objetos del mundo para decorar esta pantalla.
- Integración en `DyanatroGameDirector.BuildDeathInterface`.
- Título con alfa: `Assets/Resources/Sprites/UI/DeathTitle_PapelPicado_20261027.png`, generado con la herramienta integrada; prompt en `TITULO_MUERTE_PAPEL_PICADO_20261027_PROMPT.txt`.
- Orlas ligeras: `XunjuuPapelPicadoTrim`; botones: `XunjuuDeathScreenStyle`; familia tipográfica: `XunjuuTypography`.
- Prueba funcional en `XunjuuDeathScreenValidation`; la evidencia se registra al completar la ejecución aislada de Unity.
