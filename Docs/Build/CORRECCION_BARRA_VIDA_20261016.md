# Corrección de la barra de vida

La interfaz seleccionaba una de dieciséis ilustraciones con niveles de llenado
irregulares. La vida real cambiaba, pero la barra podía saltar entre estados
o parecer inmóvil, especialmente durante la recuperación de un punto cada vez.

Ahora se conserva el marco bordado y se dibuja un relleno horizontal continuo.
`BarraVidaFrames` separa la vida real de la cantidad mostrada y aproxima esta
última al valor real con SmoothDamp (0.16 s de suavizado). El cambio visual no
retrasa el daño, la derrota ni la curación. Al invertir la dirección se elimina
la velocidad visual contraria. La animación usa tiempo de juego y respeta la pausa.
El indicador numérico conserva siempre los puntos reales de salud.

## Verificación

`XunjuuHealthVisualValidation.Run` terminó con PASS en una copia aislada:

- Daño y curación suaves y monótonos en 100, 75, 50, 25, 1 y 0 puntos.
- Llenado proporcional, sin selección de estados discretos ni sobrepasar el objetivo.
- Máximo de 200, límites superior/inferior, pausa y orden de inicialización.
- Integración con PlayerController y el preparado: +40 puntos en 5 segundos.
- Inversión de daño a curación sin que la barra continúe bajando.
- Inspección de imágenes a 100 %, 50 % y 0 %.

La compilación Windows x64 completa terminó con Success y el nuevo EXE abrió
y respondió sin excepciones en su registro de arranque. El ZIP pasó su prueba
de integridad: 183 archivos, 267,038,406 bytes sin comprimir, 91,852,777 comprimidos.
No se modificaron las cantidades de daño ni las reglas de recuperación.

Entrega: `Builds/Windows/DYANATRO_Windows_VidaCorregida_20261016.zip`.
Se conserva el paquete anterior sin sobrescribirlo.
