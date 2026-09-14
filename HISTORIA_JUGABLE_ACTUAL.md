# Xunjúu: el camino de Mateo

## Alcance y base del relato

Esta versión se apoya en la escena y los sistemas que ya existen: Mateo, la comunidad y la milpa; flores con palabras; morral; macuahuitl; patos y venados; Dyanatr'o; habilidad orbital; Ocelotl y recompensa final. No añade mundos, NPC, ceremonias, rescates ni sistemas de memoria que el juego todavía no implementa.

Es ficción del juego, no reconstrucción de una tradición mazahua. Se conservan las palabras y textos lingüísticos anteriores; los diálogos nuevos están en español, pendientes de revisión por hablantes antes de traducirlos. Los nombres del enemigo y la asociación fantástica entre sombras y fauna no se presentan como creencias de una comunidad real.

## Conflicto

Una presencia extraña altera los caminos y la milpa. Mateo desea proteger a su comunidad, pero comienza sin experiencia ni arma. Su primer recurso es aprender: conocer el terreno, escuchar las palabras que conserva el lugar y prepararse antes de buscar el origen del peligro.

Las cinco flores son la primera tarea concreta, no una recompensa gratuita ni un arma escondida desde el principio. Cada flor aporta una palabra al morral. Al reunirlas, Mateo gana la confianza de la comunidad y recibe el macuahuitl. La amenaza se revela por etapas, de sus efectos sobre el entorno a las sombras que la provocan.

## Secuencia implementada

1. **Contexto breve.** Se presenta el peligro y el carácter ficticio del relato, sin anunciar que Mateo ya posee un arma. El archivo de video anterior se conserva; los subtítulos españoles superpuestos se actualizan para concordar con las metas reales. Los segmentos modificados indican que su traducción está pendiente, sin inventar texto mazahua. La nueva secuencia de apertura explica el orden de los desbloqueos.
2. **Antes del camino: tutorial sin arma.** En el claro se comprueba desplazamiento real con WASD, salto y aterrizaje con Espacio, golpe de mano con clic y apertura del morral con I. No hay animales de misión ni enemigos activos todavía. Las flores no pueden consumirse durante la práctica. El botón para omitir controles no concede objetos ni habilidades.
3. **Cinco palabras para volver.** La práctica termina y comienza la búsqueda de cinco flores-palabra. Se mantiene el inventario real y la meta existente.
4. **El sendero alterado.** La quinta flor entrega el macuahuitl. Se enseña F como ataque del arma, conservando el clic para la mano. Se activa la fase existente de seis animales afectados por la sombra. El sistema actual registra derrotas y retira esas instancias; el texto no promete una mecánica de rescate inexistente.
5. **El origen del desorden.** Al completar los seis animales, se activa la fase de cinco Dyanatr'o. La historia identifica estos encuentros como el rastro hacia la causa del problema, no como encargos inconexos.
6. **El guardián del paso.** Después de los cinco enemigos se desbloquea E y aparece Ocelotl. El giro protector es una habilidad fantástica cuya ornamentación visual recuerda bordados; no se describe como un arma o ritual mazahua histórico.
7. **Volver para compartir.** La derrota del jefe y la recompensa cierran el arco: el camino vuelve a abrirse y Mateo comparte lo aprendido. El final se muestra al observar la fase Completed del sistema existente, no antes.

## Principios de presentación

- Contexto breve antes de actuar, explicación del control cuando se necesita.
- Panel lateral inferior para no cubrir al protagonista; vida sin fondo rectangular.
- Recompensas controladas exclusivamente por las misiones existentes.
- No se modifica el daño, la salud ni la cantidad de objetivos para acomodar la narración.
- Las propuestas de cinco mundos de documentos anteriores quedan como ideas futuras, no como contenido ya jugable.

## Archivos

`Assets/Scripts/XunjuuOpeningJourney.cs` contiene el tutorial y los textos de transición. `DyanatroGameDirector.cs` inicia la práctica al terminar la apertura. `MazahuaWordCollectible.cs` evita gastar flores durante el tutorial. Las misiones mantienen la propiedad de recompensas y desbloqueos.
