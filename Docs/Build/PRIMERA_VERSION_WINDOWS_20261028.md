# Ejecutable de la primera versión

Se compiló la copia situada en `D:/AnyDesk/DYANATRO_Primera_Version_20260824`, correspondiente al primer registro `6965d736b9e0836ed56e34ed1ca2a9a2f5c7be32` del 24 de agosto de 2026. No se incorporaron los cambios posteriores del proyecto actual.

## Entrega

- ZIP: `D:/AnyDesk/DYANATRO_Primera_Version_Windows_20260824.zip`.
- Ejecutable: `D:/AnyDesk/DYANATRO_Primera_Version_Windows_20260824/DYANATRO.exe`.
- Windows x64, Unity 6000.3.11f1, Mono, sin desarrollo, eliminación de código administrado desactivada.
- ZIP: 52.22 MiB, 183 archivos verificados individualmente mediante SHA-256 contra la carpeta compilada.
- SHA-256 del ZIP: `D46B5A4D83FC9C3FC7F0A1D1D8FFB065495BA3C27FE5CFB6E0D8C833F967EDE0`.

## Proceso y verificación

La compilación se realizó en `output/qa/primera-version-20260824/project`, conservando la sesión del editor del usuario. Se compararon por hash 963 archivos de Assets, Packages y ProjectSettings con la copia de AnyDesk antes de compilar: cero diferencias. El único código añadido fue un constructor de Editor en la copia aislada.

La configuración antigua definía `ENABLE_UNITY_COLLECTIONS_CHECKS` para Standalone, lo que provocaba errores de AtomicSafetyHandle al generar el reproductor Mono sin desarrollo. Se retiró ese símbolo únicamente de las opciones de la copia de compilación. El proyecto original conserva sus archivos y configuración. No se modificaron escenas, mecánicas ni recursos para resolver la compilación.

Unity informó `Build Finished, Result: Success`, cero errores y un tamaño de compilación de 170243170 bytes. Se incluyó la escena habilitada `Assets/Scenes/SampleScene.unity`. El video de introducción empaquetado coincide por hash con el original.

Se abrió el EXE generado y se comprobaron visualmente el escenario, el personaje original, los elementos del entorno, la pausa y el inventario. El registro de la prueba no contiene excepciones del juego. El video original produce advertencias de marcas de tiempo y color de Media Foundation; no se cambió el archivo. Esta es una comprobación de arranque y navegación, no una partida completa ni una medición de rendimiento. Al terminar se cerró exclusivamente el reproductor usado para la prueba.

## Evidencia

- `output/qa/primera-version-20260824/build-release.log`.
- `output/qa/primera-version-20260824/build-result.txt`.
- `output/qa/primera-version-20260824/player-native.log`.
- `output/qa/primera-version-20260824/source-manifest.json`.
- `output/qa/primera-version-20260824/package-result.json`.

Para jugar se extrae el ZIP completo y se abre DYANATRO.exe. La carpeta DYANATRO_Data, MonoBleedingEdge y las bibliotecas deben permanecer junto al ejecutable. Se incluyen instrucciones y controles en LEEME.txt.
