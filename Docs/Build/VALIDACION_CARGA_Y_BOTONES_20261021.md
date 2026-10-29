# Pantalla de carga y colores de los menús

La carga muestra el paisaje existente, el nombre DYANATR’O, un motivo floral geométrico y una tarjeta con progreso, etapas y una indicación breve. El ancho de la barra corresponde a las etapas completadas; su animación no inventa avance ni retrocede. El paso al menú utiliza un fundido y respeta la preferencia de movimiento reducido. No se reproduce ni se incluye el video de apertura retirado.

Los botones usan fondos completos por acción: verde para comenzar o continuar, rosa para ludoteca, azul para controles, violeta para ajustes, ámbar para reiniciar, turquesa para menú inicial y rojo para salir. Se conserva el texto, el foco y la navegación por teclado.

## Verificación

Se ejecutó `XunjuuMenuStartupValidation.Run` en la copia aislada `output/qa/windows-20261016/project`, con Unity 6000.3.11f1. El resultado fue **PASS**. El editor del proyecto principal permaneció abierto.

Se comprobaron la ausencia de reproductor de video durante el arranque; el ancho proporcional y monotónico de la barra; movimiento reducido; colores de botones diferenciados y contraste de texto normal igual o superior a 4.5:1; textos de carga sin recortes; pausa, controles, ajustes, morral, confirmaciones, reinicio y retorno al menú mediante recargas reales. Se revisaron capturas de carga, inicio y pausa a 1600×900 y 1024×768.

- Registro: `output/qa/carga-botones-20261021.log`.
- Capturas y resultado: `Exports/Interfaz_Carga_Botones_20261021/`.

La verificación usa renderizado de Unity y acciones sobre los botones; no constituye una medición de FPS ni una partida manual completa. No se generó un ejecutable nuevo.
