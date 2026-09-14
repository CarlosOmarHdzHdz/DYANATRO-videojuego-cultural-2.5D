# Mejoras de Xunjuu

## Componentes e Inspector

Se conservan los componentes vinculados a la escena y sus GUID. No se deben agregar otros controladores de movimiento o salud al mismo jugador.

| Sistema | Componente | Configuracion |
|---|---|---|
| Jugador | PlayerController | Rigidbody y CapsuleCollider 3D; Ground Layer debe incluir el Terrain; vida inicial/maxima 100; cooldown de mano 0.62 s, arma 0.72 s y especial 5 s |
| Sprites | XunjuuCompleteSpriteAnimator | Hojas en Resources/Sprites/Player; caminar 8x2 y complementaria 4x4; transparencia real; escala compensada por altura de celda; base ajustada al collider |
| Salto | PlayerController | Solo inicia apoyado y sin ataque activo; comprueba superficie bajo el collider e ignora el propio jugador; pose aerea sostenida |
| Arma | OrbitalWeapon | Radio mundial 2.05 m; velocidad 900 grados/s; altura 0.62 m; independiente del giro/espejado del jugador |
| Vida | BarraVidaFrames / XunjuuHealthBarVisual | Imagen y texto asignados; recorte especifico de la fuente de cinco filas; barra anclada arriba a la izquierda |
| Estado | GameStateManager | Registro automatico de vida, inventario y objetivos; oculta y bloquea interaccion durante introduccion/prologo |
| Controles | DyanatroGameDirector | Panel de 410x520; diez filas; boton Controles en Pausa; Canvas Scaler de 1920x1080 y ajuste 0.5 |
| Animales | Animal | Estados Idle, Wander y Flee; huida con distancia de seguridad; colision continua; detencion si no hay direccion libre |
| Enemigos | XunjuuLevel2KillMission | Cuota maxima cinco incluso al contar automaticamente la jerarquia; conservar el grupo de enemigos y sus eventos |
| Jefe | XunjuuBossNivel1 | Golpe 14 y embestida 24; alcance considera colliders y rango de inicio; busca salud tambien en padres/hijos |
| Entorno | XunjuuEnvironment3D | Se agrega desde el director; carga EnvironmentCatalog; reemplaza visuales manteniendo componentes de interaccion |

## Recursos 3D

- Prefabs editables: `Assets/Prefabs/EntornoMazahua/`.
- Modelos FBX originales: `Assets/Art/EntornoMazahua/Models/`.
- Materiales compartidos: `Assets/Art/EntornoMazahua/Materials/`.
- Catalogo con referencias: `Assets/Resources/EnvironmentCatalog.asset`.
- Nuevas plantas: `Flor_3D.prefab` y `Milpa_3D.prefab`; mallas combinadas de baja complejidad.
- Archivo Blender original: `../output/modelos_mazahua/Entorno_Mazahua_Editable.blend`.

El bosque usa pino, oyamel y encino. La escala y la rotacion varian; los troncos tienen colliders y los modelos se ocultan al cortar su arbol. La visibilidad tramada del shader permite ver al jugador cuando un modelo se interpone. Los LODGroup actuales tienen un nivel y descarte a distancia; no contienen versiones simplificadas adicionales.

Se agregan casas y piedras en ubicaciones libres cercanas al inicio. Los limites fisicos rodean los extremos de los Terrain y tienen cerros decorativos. La profundidad procede de geometria 3D y niebla; no se usa un sistema independiente de capas 2D ni niebla volumetrica costosa.

## Reproduccion y pruebas

1. Esperar la recompilacion antes de iniciar Play. Los reemplazos del entorno se construyen al comenzar la escena.
2. Comprobar que el prologo oculta vida, inventario, controles y objetivos.
3. Tras omitir el prologo, verificar controles, caminar, salto, pausa y alternancia de Controles.
4. Comprobar que los pies apoyan sobre terreno inclinado y que el marco permanece al llegar a cero vida.
5. Completar la progresion y comprobar cinco enemigos y los impactos del jefe.

Las herramientas `XunjuuProductionValidation.RunAll` y `XunjuuProductionSmoke` permiten repetir pruebas de componentes y una apertura de la escena real. Los resultados se guardan en `output/production-validation/`.

Las pruebas de componentes verifican: salud inicial y limite; dano 100 a 86; alcance del jefe; transparencia del PNG; 16 cuadros de caminar; radio de orbita en ocho giros/escalas; restauracion del HUD; referencias de modelos y colliders. La validacion visual exporta estados de salud y un conjunto de modelos. No sustituyen una partida completa ni una medicion de FPS en el equipo objetivo.

## Limites de esta revision

Las poses de ataque y salto siguen usando los cuadros complementarios existentes; no se han generado nuevas vistas frontal/trasera ni un rig de manos. La navegacion de animales utiliza fisica y evitacion local, no un NavMesh horneado. Los sprites pequenos se importan sin compresion destructiva para conservar bordes y alfa. La composicion completa, las rutas y el rendimiento necesitan revision dentro de la escena real.
