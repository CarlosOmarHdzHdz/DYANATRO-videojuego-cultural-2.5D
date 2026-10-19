# DYANATR’O: entrega completa para Windows

Fecha: 16 de octubre de 2026. Motor: Unity 6000.3.11f1 (3000ef702840).

## Entrega

- Ejecutable: `Builds/Windows/DYANATRO_Completo_20261016/DYANATRO.exe`.
- Paquete: `Builds/Windows/DYANATRO_Windows_Completo_20261016.zip`.
- ZIP: 91,850,006 bytes. Carpeta completa: 267,036,524 bytes, 183 archivos.
- SHA-256 del ZIP: `CE9BE530F930066F10011DF6AF1811C72B93D18FC8AFA81806FB9A47DC43F021`.
- La verificación de integridad del ZIP terminó con `Everything is Ok`.

## Corrección

La carpeta generada `Assets/2D_Completo/Library` estaba siendo importada por
el proyecto principal como si fuera contenido de juego. Esto introducía DLL
de referencia de Unity y provocaba el conflicto `Unity.Multiplayer.Center.Common.ref`.
Se trasladaron esa caché y su archivo `.meta` a
`Archive/UnityCache_20261016_windows`, sin borrar los originales ni tocar
la caché activa de la raíz. La consola del editor original mostró cero errores
después de la recompilación. No se guardó ni modificó la escena abierta.

La instalación de Unity no contenía las variaciones del reproductor Windows.
Se obtuvo el soporte oficial de Mono de la misma revisión y se usó un entorno
de compilación aislado bajo `output/qa/windows-20261016/toolchain`.
No se sobrescribieron archivos de la instalación de Unity en Program Files.

## Compilación

Se compararon por hash 1,633 archivos de contenido de Assets con la copia
aislada: cero diferencias. Se conservaron Packages, ProjectSettings, Resources,
StreamingAssets y la escena habilitada `Assets/Scenes/SampleScene.unity`.
Solo se excluyeron cachés y registros generados del proyecto anidado.
El video de introducción de origen coincide por hash con el empaquetado.

Se compiló Windows x64, Mono, sin desarrollo y con eliminación de código
administrado desactivada. No se eliminaron mecánicas, paquetes, recursos de
juego ni escenas habilitadas para resolver el problema.

Resultado del motor: `Build Finished, Result: Success`.
Registro local: `output/qa/windows-20261016/build.log`.

## Comprobación del ejecutable

Se abrió el EXE generado, no una simulación del editor, y se comprobó:

- Menú principal y acceso a la ludoteca.
- Seis fichas de herbolaria y sus siluetas bloqueadas antes del registro.
- Memorama: tablero y volteo con ilustración textil.
- Sopa de letras: ocho palabras, selección correcta y marcado bordado.
- Apertura del quiz con cinco preguntas.
- Reproducción del video y transición al prólogo y al escenario jugable.
- Escenario con terreno texturizado, vegetación, personaje e interfaz.
- Apertura del morral y panel de fabricación existente.

No se observaron excepciones de ejecución en esta prueba. La computadora
probada hizo la transición automática de Direct3D 12 a Direct3D 11.
Persisten avisos no bloqueantes sobre un glifo de verificación de la fuente
y las marcas de tiempo del video H.264; no se modificaron esos recursos.
No se realizó una partida completa hasta el combate final.
El juego de prueba se cerró y el editor original se dejó abierto.

El ZIP incluye `LEEME.txt`. Para compartirlo, debe extraerse y conservarse
toda la carpeta; el archivo EXE no es autónomo.
