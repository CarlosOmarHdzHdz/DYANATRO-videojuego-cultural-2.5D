# Fondos de inicio y pausa: arte e integración

## Recurso guardado

`Assets/Resources/Sprites/UI/MenuHighlands_20261019.png`.
Generado con la habilidad imagegen mediante la herramienta integrada, sin CLI.
El original se conserva en el directorio de imágenes generadas; el juego usa la
copia dentro de Assets. No se utiliza una ruta temporal para cargarlo.

Es una ilustración decorativa, no una fotografía documental de una comunidad.
No modifica los árboles, casas, caminos ni cultivos del mapa jugable.

## Diseño de las pantallas

Inicio utiliza el paisaje con color cálido y el protagonista original en primer
plano. Pausa usa el mismo paisaje con un tratamiento más frío y oscuro. La
ilustración se ajusta mediante recorte proporcional, sin estirar sus formas.
Una deriva lenta de cinco por tres píxeles da movimiento al fondo; el panel y
sus botones permanecen fijos. No se produce movimiento rápido ni parpadeo.

Las bandas exteriores tienen rombos más pequeños y espaciados; se eliminan
las columnas decorativas del área de texto del panel principal. El paisaje no
incluye letras ni botones: todo el texto y las acciones siguen siendo UI real.

## Prompt final utilizado

Use case: stylized-concept. Asset type: production-ready wide background image for the main menu and pause menu of DYANATR’O, an independent 2.5D cultural adventure set in the rural highlands of the Mazahua region in central Mexico. Create only the background artwork, not a screenshot or UI mockup. Wide landscape 16:9, high-resolution finely painted indie game environment. A peaceful winding earth footpath leads through modest adobe houses with weathered clay tile roofs, cultivated maize plots, irregular pine and oak silhouettes, layered blue-green highland hills. Warm early-morning haze, soft golden rim light, rich deep forest greens, muted teal atmospheric distance, warm ochre soil. Mature tasteful painterly illustration, restrained brush texture, clear depth and organic forms, not photorealism, not blocky pixel art. Scene detail is concentrated on the left and bottom third; keep the rightmost 45 percent quiet, lower contrast and darker green for an opaque menu card that will be rendered separately by Unity. Avoid focal clutter in the center. Edge-to-edge coherent landscape, no frame. No text, no letters, no numbers, no logos, no watermark, no buttons, no panels, no interface, no characters, no invented ceremonial symbols, no pyramids or Aztec imagery. The game's existing world trees and characters are unchanged; this is decorative menu art only.

## Corrección del error de arranque

XunjuuPanelReveal requiere CanvasGroup y lo obtiene con comprobaciones de nulo
de Unity, sin el operador CLR ?? para esa referencia nativa. OnEnable recupera
el componente aunque la caché se haya perdido tras una recarga. La corrutina
también comprueba que siga existiendo antes de escribir su opacidad.

## Salida desde pausa

Seis acciones: continuar, ludoteca, controles, reiniciar, menú inicial y salir
del juego. Salir abre una confirmación que advierte del avance de sesión no
guardado. Cancelar mantiene la pausa; reanudar cierra el diálogo. Confirmar llama
al mismo QuitGame del inicio: Application.Quit en Windows y fin de Play en Unity.

XunjuuMenuStartupValidation registra errores desde Awake, incluidos los que
ocurren antes de que comiencen las demás pruebas. Verifica paneles inactivos,
restauración de caché, reaperturas, seis botones separados, fondo importado,
confirmación, cancelación y salida de Play mediante el botón de confirmar.
