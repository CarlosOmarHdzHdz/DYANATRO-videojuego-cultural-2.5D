# Validación: interfaz indie, territorio y huerto

Fecha: 19 de octubre de 2026. Unity 6000.3.11f1, Windows x64/Mono.
Las pruebas usan una copia aislada; no guardan SampleScene ni cierran el editor
principal del usuario. La entrega conserva la escena habilitada y desactiva el
recorte de código administrado y módulos del motor.

## Resultado

- Distribución: PASS. 18 viviendas, 10 milpas, 19 accesos conectados, 500 plantas
  de maíz en sprites y 24 ejemplares de seis plantas en un huerto.
  Se verifican reservas, curvas, normales superiores y copas de árboles activos
  fuera de caminos, casas, campos y huerto. Solo formas pino/encino.
- Recorrido e ítems: PASS. Cinco flores recogidas mediante su controlador real
  de trigger, seis capturas, misión del huerto, cosecha, dos fichas visibles,
  respuesta incorrecta rechazada y correcta aceptada, receta real y combate.
  La prueba invoca el trigger; no reproduce manualmente todo el desplazamiento.
- Morral: PASS. Paneles principales contenidos y sin intersección; altura
  preferida del texto no supera su área. Apertura/cierre pausa y reanuda el mundo.
  La fabricación y el uso no consumen las cinco flores de memoria.
- Salud: PASS. No se gasta con salud completa; recuperación real de 40 puntos
  en cinco segundos; cierre formativo sin exigir daño para abrir el combate.
- Flujo completo: PASS. Flores → animales → huerto → preparación → Macuahuitl →
  cinco sombras → Orbitasword → Ocelotl → recompensa/cierre. Comprueba arma,
  audio, patrulla, proyectil, colocación del jefe y activaciones por fase.
- Música: PASS. Silencio antes de jugar; pausa y continuidad de la pista con
  menú de pausa, ludoteca, morral y pausa externa. El morral no reinicia la pista.
- Capturas: menú, pausa, ficha botánica, identificación, aviso, morral y los
  tres minijuegos revisados. Lienzos simulados: 1600×900, 1280×800, 1024×768 y
  2560×1080 para el morral. No es certificación en todos los monitores.
- Compilación Windows: PASS. 273.901.243 bytes informados por Unity.
- Arranque nativo: PASS en modo batch sin gráficos, proceso propio detenido
  tras comprobar carga del juego y ausencia de excepciones en su registro.
  No valida reproducción de video ni interacción gráfica del ejecutable.

## Entrega

- Carpeta: `Builds/Windows/DYANATRO_Indie_Regional_20261019`.
- Paquete: `Builds/Windows/DYANATRO_Windows_Indie_Regional_20261019.zip`.
- 182 archivos; 273.903.370 bytes sin comprimir, incluido LEEME.
- ZIP: 98.748.358 bytes. Todas sus entradas de archivo se abrieron y leyeron;
  su número coincide con el de la carpeta distribuida. Sin documentos Office/PDF.
- SHA-256 ZIP: `01EFA2195CBD7665E0721AEB53D2F460F3D0742164EDDA97B4ECE5E4CB004425`.
- El video introductorio coincide byte a byte mediante SHA-256:
  `CC900F5101493ED862E21C6960D3E17630C3972E41E683927C58F903234FBA57`.

Los símbolos de depuración DoNotShip se movieron, sin borrarlos, a
`output/qa/regional-symbols-20261019`. No forman parte del ZIP.
Las entregas anteriores se conservan.

## Evidencias locales

- `output/qa/regional-settlement-20261019.log`
- `output/qa/regional-final-ui-20261019.log`
- `output/qa/regional-combat-active-20261019.log`
- `output/qa/regional-music-20261019.log`
- `output/qa/regional-build-20261019.log`
- `output/qa/regional-native-20261019.log`
- Capturas y resultados: `output/qa/windows-20261016/project/output/`.

Se actualizaron comprobaciones heredadas de la ayuda siempre desplegada, arma
oculta durante todo tajo y actores de reserva inactivos: ahora se prueba el
comportamiento vigente, sin modificar el combate para satisfacer esas
expectativas antiguas. La excepción preexistente de indexación de SearchDatabase
del editor QA no aparece en el arranque del ejecutable; no es un fallo de juego.

La [historia integrada](../Design/HISTORIA_MEMORIA_Y_TERRITORIO_20261019.md) y la
[revisión de flora](../Design/REVISION_FLORA_REGIONAL_20261019.md) separan ficción,
representación cultivada y datos regionales. Sigue siendo necesaria revisión
lingüística/comunitaria y una evaluación educativa con participantes.
