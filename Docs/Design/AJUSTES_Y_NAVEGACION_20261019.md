# Ajustes y navegación del prototipo

## Alcance

Mejoras sobre el juego existente, sin sustituir árboles, modificar el mapa,
inventar vocabulario mazahua ni cambiar la secuencia de aprendizaje y combate.
Se conserva la ilustración ya integrada en inicio y pausa.

## Uso

- **Ajustes**, disponible desde inicio y pausa: volumen de música del recorrido
  entre 0 y 100 % del nivel base, sin modificar voces ni efectos.
- **Movimiento de menús: reducido**: paisaje inmóvil, entradas sin cambio de
  escala y botones sin ampliación. No modifica animaciones del personaje,
  videos, partículas del mundo ni las mecánicas de los minijuegos.
- Ambos valores se recuerdan con PlayerPrefs al cerrar Ajustes. No son un
  guardado de partida. La música sigue detenida antes de jugar y en pausa;
  cambiar el volumen no inicia ni reinicia la pista.
- TAB / MAYÚS + TAB recorre controles; flechas permiten navegar y ajustar el
  deslizador; ENTER activa el control seleccionado. El borde claro señala el
  botón enfocado. Estas mejoras se delimitan a inicio, pausa y sus diálogos;
  no se afirma compatibilidad completa con mando o lector de pantalla.
- ESC cierra primero Ajustes, confirmación o ayuda de controles. Un segundo
  ESC permite continuar cuando vuelve a quedar visible la pausa.

## Protección del avance

Reiniciar, volver al menú y salir desde pausa requieren confirmación. La opción
inicial del diálogo es **Seguir jugando**, no la acción destructiva. Cancelar
restaura el foco del botón de origen y mantiene el mundo detenido.

**Reiniciar** carga una partida nueva y comienza la introducción. **Menú
inicial** vuelve al menú, sin comenzar otra partida automáticamente. Los avisos
explican que el avance y objetos de la sesión no se guardan; las fichas
descubiertas y los ajustes se conservan. No se ha añadido un guardado completo.

## Implementación

`XunjuuPresentationSettings`: ventana compartida, sin modificar AudioListener.
`XunjuuPresentationPreferences`: claves independientes y cambios de presentación.
`XunjuuMenuNavigation`: navegación explícita con foco limitado al diálogo superior,
sin apropiarse de la entrada de la ludoteca.
`DyanatroGameDirector`: integración de ajustes, confirmaciones y recargas distintas.

Las animaciones usan tiempo no escalado, pero la música conserva las reglas de
pausa anteriores. Se evita buscar repetidamente el borde de cada botón durante
cada frame; no se atribuye a este cambio una mejora de FPS medida.
