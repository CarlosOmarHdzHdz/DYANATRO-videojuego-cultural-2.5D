# Revisión de aldea, herbolaria y presentación

Se mantienen la escena, la progresión, las cinco flores-palabra, el combate,
la fauna, la ludoteca y los minijuegos. La distribución se aplica al iniciar
el juego; no se guardaron cambios sobre la escena que estaba abierta en Unity.

## Implementación

- `XunjuuSettlementPlan` comparte reservas de viviendas, accesos, parcelas y
  huerto. El huerto queda fijo al suroeste del origen, con seis especies.
- Las casas se distribuyen en un área mayor y sus puertas se orientan hacia
  el camino principal. Cada vivienda colocada tiene su acceso propio.
- Las milpas se generan después de las viviendas. Se verifica el rectángulo
  completo contra edificios, caminos, terreno, obstáculos sólidos y otras
  parcelas. Se conservan diez parcelas y los cultivos interactivos.
- El maíz usa `Textures/Maiz_Milpa_Recortado`: ya no se convierte a un modelo
  3D genérico. Se respetan sus dimensiones y el color original.
- Los caminos tienen una superficie granular calculada en coordenadas del
  mundo. Una malla subdividida sigue el terreno para evitar tramos enterrados.
- La caminata usa ejes ortogonales relativos a la cámara, un solo giro de
  billboard y una escala uniforme para los cuatro cuadros de cada ciclo.
- La flor inicial usa un sprite nuevo, permanece erguida y tiene un balanceo
  suave; se desactiva la antigua segunda flor utilizada como aura.
- El panel se llama «Preparación de hierbas» y el objeto «Infusión de hierbas».
  `preparado_camino` sigue siendo su identificador interno, por compatibilidad.

## Ilustraciones incorporadas

Se utilizó la herramienta integrada de creación de imágenes (no el CLI).
Los PNG conservan su canal alfa y se importan explícitamente como texturas 2D.
Se recortan los márgenes al crear el sprite, sin alterar los originales.

- `Assets/Resources/Sprites/Items/InfusionHierbas_v2.png`.
- `Assets/Resources/Sprites/Collectibles/FlorMemoria_v2.png`.

### Prompt del frasco

Create a polished single isolated inventory sprite of a rustic herbal preparation flask for a Mexican highland cultural 2.5D videogame. Warm terracotta earthenware flask with short neck and cork stopper, thin woven cord, small cream blank tag, amber liquid rim, two small realistic mint leaves and one tiny chamomile bloom tied to neck. Detailed hand-painted pixel-art matching realistic vegetation sprites; natural proportions, crisp clean silhouette, earthy terracotta green cream and amber. Single centered three-quarter-front object occupying 78% square canvas. Genuinely transparent alpha background. No scene, floor, UI, letters, border, watermark, neon, face or magic. Readable at 64-96 pixels.

### Prompt de la flor

Create a single living collectible flower sprite for a Mexican highland cultural 2.5D pixel-art videogame. One upright sprig, slim natural green stem, two slender leaves, vivid deep magenta eight-petal bloom and golden-yellow center; subtle embroidery-inspired geometric rhythm within natural layered petals. Detailed hand-painted crisp pixel-art, realistic game vegetation style with believable shading and fine warm dark outline. Complete plant including stem base, centered side/front view, 78% canvas height, comfortably padded, genuinely transparent alpha background. No flat textile patch, pot, face, kawaii, scene, ground, external glow, UI, writing, border or watermark.

## Comprobación

La prueba automática de la escena verificó viviendas con acceso, diez milpas,
500 cultivos en sprite, 24 plantas medicinales dentro de un único huerto y
las seis especies. También comprobó las reservas de espacio, el movimiento
ortogonal, la escala de los ciclos, la receta y las cinco flores originales.
La interfaz se revisó además directamente en el ejecutable de Windows.

Los ensayos y sus capturas están bajo `output/qa/windows-20261016`, sin incluirse
en el paquete del juego. No sustituyen una partida completa hasta el final.
