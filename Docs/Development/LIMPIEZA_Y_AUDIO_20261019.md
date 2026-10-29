# Limpieza del proyecto y pausa musical

## Alcance

Se retiraron 875 archivos ajenos a la ejecucion del juego: documentos Word/PDF,
fotografias de informes, exportaciones academicas, renders y herramientas de
redaccion. Se conservaron fuera del proyecto, en dos carpetas hermanas:

- `DYANATRO_Respaldo_Limpieza_20261019-124338`: 700 archivos, 168565537 bytes.
- `DYANATRO_Respaldo_Limpieza_20261019-temporales`: 175 archivos, 142711634 bytes.

Los manifiestos contienen origen, destino y SHA-256 de cada archivo. Los hashes
se comprobaron tras cada traslado. No se eliminaron datos de forma irreversible.
No habia Word/PDF entre los assets activos. Las copias historicas en `Archive`,
las caches activas, los toolchains y las entregas anteriores no se modificaron.

## Estructura conservada

- `Assets`: escenas, codigo, prefabs, arte, audio y recursos de la ludoteca.
- `Packages`, `ProjectSettings`: configuracion Unity.
- `Docs/Design`, `Docs/Development`, `Docs/Build`: documentacion tecnica.
- `Tools/Maintenance`: herramientas de organizacion seguras, con simulacion.
- `Builds`: ejecutables completos. `output`: pruebas y registros locales.

Se preservaron las rutas de `Resources`, los archivos `.meta` y los GUID.
Los Word/PDF y `output` quedan excluidos de nuevas incorporaciones a Git.

## Correccion musical

La pista de fondo permanece detenida en el menu inicial, al abrir la ludoteca
desde ese menu, durante el video y en el prologo. Empieza al entrar al recorrido
jugable con el volumen de partida y una entrada gradual. La pausa usa
`AudioSource.Pause`/`UnPause`, no reinicia la pista. Tambien responde a la ludoteca
durante la partida y a pausas externas que establezcan `Time.timeScale = 0`.
El video conserva su audio propio. No se cambiaron los archivos de musica.

## Reduccion de trabajo

- Una consulta de camara por fotograma compartida por billboards y ordenadores
  de sprites, en lugar de consultas por objeto y propiedad.
- Se evitan asignaciones identicas a estados del HUD y al orden de sprites.
- El aviso de recoleccion solo reconstruye su texto al cambiar de planta.
- La clasificacion de colisiones de cultivos se calcula al registrar objetos,
  no en cada actualizacion de distancia. Sus recorridos se suspenden en pausa.

No se afirma una mejora porcentual de FPS: requiere un perfil de rendimiento
comparativo que no forma parte de estas comprobaciones.

## Validacion realizada

Unity 6000.3.11f1, copia aislada del proyecto:

- `XunjuuMusicPauseValidation.Run`: PASS. Menu, ludoteca desde menu, video y
  prologo silenciosos; entrada musical; tres ciclos de pausa y reanudacion
  conservando muestras; ludoteca durante partida y pausa externa; ejes de camara.
- `XunjuuSettlementValidation.Run`: PASS. 11 casas, diez milpas sin colisiones
  de reserva, doce accesos conectados, 500 cultivos en sprite, 24 medicinales
  restringidas al huerto, seis especies, receta y cinco flores.

Registros: `output/qa/windows-20261016/music-pause-20261019.log` y
`settlement-clean-20261019.log`. El entorno aislado mantiene un aviso previo del
indexador `UnityEditor.Search` y advertencias de codificacion del video; no se
confunden con errores del codigo de juego. No se realizo una partida completa
hasta el combate final ni un estudio de rendimiento.

## Entrega Windows

- Ejecutable: `Builds/Windows/DYANATRO_Optimizado_20261019/DYANATRO.exe`.
- ZIP: `Builds/Windows/DYANATRO_Windows_Optimizado_20261019.zip`.
- Tamano ZIP: 96860058 bytes; 182 archivos, 268619306 bytes sin comprimir.
- SHA-256: `98BE90318BCAAD1CFFD85EE8026D2EDF1D0D0A5D861E5B4E6908DA5E78B67C0F`.
- Compilacion: `Build Finished, Result: Success`, Windows x64/Mono, misma escena
  habilitada y sin recorte de codigo. ZIP leido completo y cotejado con la
  cantidad de archivos de la entrega; ningun Word/PDF incluido. El video de
  introduccion coincide por hash con el original.
- Arranque nativo de comprobacion sin interfaz: sin excepciones de juego
  detectadas en el registro de esa prueba. No sustituye una partida completa.
- Simbolos Burst `DoNotShip` conservados fuera del paquete, en
  `output/qa/windows-20261016/burst-debug-clean-20261019`.
