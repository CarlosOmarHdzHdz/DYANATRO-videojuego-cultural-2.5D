# Validación: ludoteca, inicio y glifos de controles

Complementa y corrige la entrega `Ludoteca_Renovada_20261020`. La suspensión anterior de la cámara provocaba el aviso del Game View. Esta revisión mantiene la cámara activa y omite únicamente el dibujo del mundo detrás de la ludoteca overlay. El estado se restaura al cerrar.

Diseño: `Docs/Design/LUDOTECA_INICIO_Y_TECLAS_20261020.md`.
Capturas revisadas: `Exports/Ludoteca_Controles_20261020`.

## Pruebas que finalizaron con éxito

| Registro en output/qa | Comprobación | Resultado |
| --- | --- | --- |
| ludoteca-controles-design-20261020-r2.log | Cámara activa con máscara cero; restauración; ocho páginas; portada sin texto recortado; navegación; 18 pares; sopa aleatoria de ocho términos y selección bordada; quiz | PASS, salida 0 |
| ludoteca-controles-startup-20261020-r4.log | Clics repetidos en Inicio de la ludoteca; cámara para Display 1; restauración de máscara, color y limpieza; clic real y duplicado en INICIAR; estado de carga; un solo reproductor; llegada al juego sin pausa residual; diez glifos y geometría del ratón; ajustes, morral, pausa, reinicio y retorno real al menú | PASS, salida 0 |
| ludoteca-controles-music-20261020-r2.log | Silencio en menú, ludoteca inicial e introducción; música en partida; pausa conserva posición; cierre de ludoteca y morral restaura reproducción; ejes compartidos de cámara | PASS, salida 0 |
| ludoteca-controles-flow-20261020-r2.log | Flores → seis registros de fauna → huerto y fichas → preparación → Macuahuitl → cinco sombras → giro protector → Ocelotl → cierre | PASS, salida 0 |
| ludoteca-controles-build-20261020.log | Compilación Windows x64/Mono, escena principal completa, sin stripping administrado ni eliminación de módulos del motor | PASS, salida 0 |
| ludoteca-controles-native-20261020.log | Arranque del ejecutable con batch/nographics durante 15 segundos | Cero coincidencias de errores o excepciones; solo se cerró el proceso de esta prueba |

La ayuda se revisó visualmente en 1600×900 y 1024×768, y el panel de pausa en 1600×900. El ratón dibuja el botón izquierdo en color; WASD son cuatro teclas independientes. El panel desplegable no se encima con su botón de ayuda.

Durante el desarrollo se corrigieron el GUID del nuevo parcial, la generación de malla del glifo y su CanvasRenderer. También se protegió la restauración de capas de la herramienta de capturas frente a objetos temporales que desaparecen mientras se espera un cuadro. Los registros intermedios fallidos no representan el paquete final. El defecto preexistente de UnityEditor.Search.SearchDatabase se excluye por su traza específica en las pruebas que registran errores, no por un filtro general de excepciones.

Todas las pruebas se ejecutaron en el proyecto aislado `output/qa/windows-20261016/project`, sin cerrar los editores del usuario ni guardar modificaciones de prueba en sus escenas. Se preservaron cambios existentes, incluidos prefabs de fauna. No se hizo commit ni push.

## Paquete

- Carpeta: `Builds/Windows/DYANATRO_Ludoteca_Controles_Corregidos_20261020`.
- Ejecutable: `DYANATRO.exe`.
- ZIP: `Builds/Windows/DYANATRO_Windows_Ludoteca_Controles_20261020.zip`.
- 182 archivos; 278.654.300 bytes descomprimidos; 102.189.822 bytes de ZIP.
- SHA-256: `0658715FF7D0A44E8614D83D3543274FB7690D0705682386FE1E2D0C2A2E2880`.
- Se leyeron íntegramente las entradas del ZIP. El video incluido coincide por SHA-256 con `Assets/StreamingAssets/IntroPrologoMazahua.mp4`.
- Los símbolos de depuración generados, no necesarios para jugar, se conservaron fuera de la distribución en `output/qa/ludoteca-controles-symbols-20261020`. No se borraron versiones anteriores.

## Límites

El arranque nativo sin gráficos no sustituye una partida manual gráfica ni mide FPS. Las capturas se renderizan con una cámara de QA; por eso la regresión «No cameras rendering» se comprueba además exigiendo una cámara habilitada para la pantalla principal en el flujo real. No se promete que cualquier equipo quede libre de tirones. Si Unity conserva un Play Mode de la versión anterior, detener Play antes de probar la nueva.
