# Xunjuu: Guardian del Bosque

## Concepto

Xunjuu es un videojuego de aventura y combate 2.5D en un bosque vivo. El jugador explora una llanura natural, protege el bosque de enemigos de fuego y usa una espada flotante para defenderse, abrir caminos y recuperar el equilibrio del lugar.

La experiencia debe sentirse sencilla, clara y jugable: caminar, explorar, combatir, esquivar bolas de fuego, cortar arboles interactivos cuando sea necesario y avanzar hacia zonas cada vez mas peligrosas.

## Datos del proyecto usados

- Escena principal: `Assets/Scenes/SampleScene.unity`
- Mundo: terreno, bosque, lago, montanas lejanas, nubes y zona llamada `mineria`
- Jugador: `PlayerController.cs`, sprite de descanso, caminar, salto y golpe
- Combate: ataque basico, espada flotante, ataque con espada y ataque orbital
- Enemigo: persecucion, vida, muerte y ataque con bola de fuego
- Naturaleza: arboles interactivos, hojas, aves, nubes y generador de bosque
- Prefabs principales: `Tree`, `arbolN`, `Ave`, `lorro`, `cloudPrefab`, `Fireball`, `SwordFloating`
- Sprites principales: jugador, enemigo, fuego, espada, vida, arboles, hojas, aves y terreno

## Fantasia del jugador

El jugador es el guardian de Xunjuu. No es un guerrero pesado: es rapido, pequeno frente al bosque y depende de moverse bien. La espada flotante es su herramienta especial: sirve como defensa, ataque fuerte y simbolo de su vinculo con el bosque.

## Objetivo principal

Restaurar el bosque derrotando enemigos de fuego y limpiando la zona de mineria. El primer prototipo puede terminar cuando el jugador derrota a todos los enemigos de la escena o llega a la zona `mineria` tras sobrevivir al combate.

## Loop principal

1. Explorar el bosque.
2. Detectar enemigos o zonas peligrosas.
3. Combatir con ataque basico, espada normal y espada orbital.
4. Esquivar bolas de fuego y administrar la vida.
5. Interactuar con arboles y elementos del ambiente.
6. Avanzar a una nueva zona o activar el objetivo final.

## Controles

- `WASD`: mover al personaje relativo a la camara.
- `Espacio`: saltar.
- `Click izquierdo`: ataque basico.
- `F`: ataque fuerte con espada.
- `E`: ataque orbital.
- `Click derecho`: rotar camara.

## Mecanicas principales

### Movimiento

El movimiento existente ya esta pensado para una camara orbital. La prioridad es que el jugador se sienta agil y que el combate no lo deje inmovil demasiado tiempo.

### Combate del jugador

El jugador tiene tres capas de ataque:

- Ataque basico: corto alcance, rapido, sirve contra enemigos cercanos y arboles interactivos.
- Espada con `F`: golpe mas fuerte, ideal para castigar enemigos cuando se acercan.
- Orbital con `E`: ataque defensivo de area, util cuando el jugador esta rodeado.

### Enemigos de fuego

Los enemigos patrullan, persiguen al jugador y lanzan bolas de fuego. Funcionan como la amenaza principal del prototipo. Su papel es obligar al jugador a moverse, no solo a presionar ataque.

### Bosque interactivo

Los arboles pueden reaccionar a golpes con hojas, vibracion y destruccion. Para el diseno base, conviene usarlos como obstaculos blandos y como elementos de atmosfera, no como recurso complejo todavia.

### Vida

La barra de vida debe ser visible durante el combate. El jugador pierde vida por contacto con enemigos y por proyectiles de fuego.

## Estructura de nivel MVP

### Zona 1: Claro inicial

Area segura para aprender movimiento, salto, camara y ataque basico. Debe tener algunos arboles, aves y nubes para mostrar el tono del juego.

### Zona 2: Bosque medio

Primer encuentro con enemigos. Debe haber espacio suficiente para esquivar bolas de fuego y probar la espada orbital.

### Zona 3: Lago y llanura

Zona abierta para combate con dos o tres enemigos. Aqui se puede ensenar que moverse y controlar distancia importa.

### Zona 4: Mineria

Zona final del prototipo. La presencia de mineria contrasta con el bosque y funciona como origen visual del problema. El objetivo final puede ser derrotar al enemigo mas fuerte o limpiar todos los enemigos alrededor.

## Condiciones de victoria y derrota

Victoria del prototipo:

- Derrotar a todos los enemigos activos de la escena, o
- Llegar a `mineria` y derrotar al enemigo final.

Derrota:

- La vida del jugador llega a 0.

## Progresion recomendada

Para el primer prototipo, no agregar inventario ni dialogos largos. La progresion puede basarse en encuentros:

- Encuentro 1: un enemigo, sin presion.
- Encuentro 2: dos enemigos cerca del lago.
- Encuentro 3: enemigo final en mineria.

Despues del MVP se puede agregar recoleccion de madera, mejoras de espada, animales que reaccionan al peligro y objetivos ambientales.

## UI necesaria

- Barra de vida del jugador.
- Texto breve de objetivo actual: "Llega a la mineria", "Derrota a los enemigos", "Bosque restaurado".
- Pantalla simple de victoria.
- Pantalla simple de derrota con opcion de reiniciar.

## Direccion visual

El juego debe verse como fantasia natural 2.5D:

- Sprites tipo recorte mirando a camara.
- Terreno amplio con arboles dispersos.
- Nubes y aves para dar vida.
- Enemigos de fuego como contraste de color y amenaza.
- Mineria como zona mas seca, oscura o vacia.

## Prioridad de implementacion

### Prioridad 1: Hacerlo jugable

- Confirmar que `SampleScene` abre sin errores.
- Verificar jugador, camara, vida y espada.
- Asegurar que el enemigo detecta al jugador y dispara `Fireball`.
- Crear una condicion de victoria simple.
- Crear una condicion de derrota simple.

### Prioridad 2: Objetivos

- Agregar un `GameManager` con conteo de enemigos vivos.
- Mostrar texto de objetivo.
- Reiniciar escena al perder.
- Mostrar victoria al derrotar enemigos.

### Prioridad 3: Nivel

- Ordenar encuentros en claro, lago y mineria.
- Ajustar distancias de deteccion y velocidad de enemigos.
- Colocar aves y nubes como ambientacion.

### Prioridad 4: Pulido

- Efectos visuales al recibir dano.
- Mejor respuesta de ataque con espada.
- Sonidos de golpe, salto, fuego y victoria.
- Pequenas particulas de hojas al golpear arboles.

## Nombre alternativo

Si se quiere un titulo con acento local mas fuerte, usar:

- `Xunjuu: Raices de Fuego`
- `Xunjuu: El Bosque Despierta`
- `Guardian de Xunjuu`

## MVP recomendado

El MVP mas realista es: una escena de bosque con jugador, camara orbital, tres enemigos de fuego, espada flotante, vida, derrota y victoria. Ese alcance aprovecha casi todo lo que ya existe y permite tener un videojuego jugable antes de ampliar sistemas.
