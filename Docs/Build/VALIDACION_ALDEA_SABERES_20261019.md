# Revision de aldea y Saberes del huerto

Motor: Unity 6000.3.11f1. Validacion en copia aislada del proyecto; no se guardo
la escena original ni se alteraron sus GUID, prefabs, paquetes o controles.

## Resultados automaticos

- Aldea: **PASS**, 18 casas frente a 11 en la entrega previa; diez milpas,
  diecinueve accesos conectados y 500 cultivos en sprite. Lineas centrales
  curvas y normales de carretera orientadas hacia arriba. Se conservan 24
  plantas en un solo huerto y las seis especies originales.
- Aprendizaje: **PASS**, cinco flores desbloquean la mision complementaria;
  recoleccion real de dos manzanillas y una hierbabuena; dos fichas visibles;
  pregunta de reconocimiento con rechazo de respuesta incorrecta y aceptacion
  de la correcta; receta real; sin gasto de objetos con vida completa;
  recuperacion real de 40 puntos y final de mision.
- Presentacion: **PASS**, un controlador y una tarjeta central compartidos con
  combate, mazahua arriba y espanol abajo, centro comprobado y banner antiguo
  ausente. Se verificaron pausa, controles y ocultamiento/restauracion del HUD.
- Audio: **PASS**, silencio antes de jugar, entrada musical y tres ciclos de
  pausa/reanudacion conservando la posicion; pausa en ludoteca y pausa externa.

Registros locales:

- `output/qa/windows-20261016/organic-village-final-20261019.log`
- `output/qa/windows-20261016/herbal-learning-final-20261019.log`
- `output/qa/windows-20261016/music-final-village-20261019.log`

Capturas de menu, pausa, cuaderno, fichas, identificacion, morral y aviso:
`output/qa/windows-20261016/project/output/herbal-learning-validation`.
Se inspeccionaron capturas individuales de interfaz y vistas del terreno.
Las capturas de composicion que convierten temporalmente el HUD a camara no
sustituyen la revision nativa del orden de renderizado de ScreenSpaceOverlay.

El entorno de QA presenta el aviso previo del indexador UnityEditor.Search y
advertencias de codificacion del video. No se presentan como fallos nuevos del
codigo de juego. Las preferencias botanicas se restauran al terminar las pruebas.
No se completo una partida manual hasta el jefe ni una evaluacion academica con
participantes. No se certifican traducciones nuevas ni una ganancia de FPS.

## Entrega Windows

Compilacion aprobada, Windows x64/Mono, escena original habilitada y sin recorte
de codigo. Registro: `output/qa/windows-20261016/build-aldea-saberes-20261019.log`.

- Carpeta: `Builds/Windows/DYANATRO_Aldea_Saberes_20261019`.
- Paquete: `Builds/Windows/DYANATRO_Windows_Aldea_Saberes_20261019.zip`.
- ZIP: 96865551 bytes.
- SHA-256: `4265CDFC73B9F052C287FB3EE776F789486CA8B1C69F0118C90FBD014175646B`.

Se conserva el video original por hash y se excluyen los simbolos Burst DoNotShip
del paquete, sin borrarlos: `output/qa/windows-20261016/burst-debug-aldea-saberes-20261019`.
La comprobacion nativa sin interfaz inicia el motor del ejecutable generado;
no equivale a una partida manual. El EXE requiere toda su carpeta extraida.
