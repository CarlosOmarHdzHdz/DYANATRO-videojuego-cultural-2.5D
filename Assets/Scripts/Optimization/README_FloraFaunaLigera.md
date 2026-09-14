# Demo de flora y fauna ligera

Este demo sirve para poblar el escenario sin volver pesado el juego en un equipo con procesador Intel Core i5. La idea es usar sprites sencillos, pocos animales activos y reciclaje por distancia.

## Uso en Unity

1. Crear un GameObject vacio en la escena y llamarlo `EcoDemo`.
2. Agregar el componente `XunjuuLightweightEcoSpawner`.
3. Asignar el `Player`, la `Main Camera` y el `Terrain` si no se detectan solos.
4. En `Flora Sprites`, colocar sprites de pino, oyamel, encino, arbusto, pasto o flores.
5. En `Fauna Sprites`, colocar sprites de pato, venado, ave u otra fauna ambiental.
6. Para probar sin afectar misiones, dejar `Generate On Start` apagado y usar el menu contextual del componente: `Xunjuu/Demo ligero de flora y fauna/Generar`.

## Valores recomendados

- Flora Count: 90 a 140.
- Fauna Count: 6 a 10.
- Flora Visible Radius: 70 a 80.
- Fauna Visible Radius: 60 a 75.
- Visibility Update Interval: 0.25.
- Billboard Update Interval: 0.35.

## Reglas de rendimiento

- La flora no lleva collider ni scripts individuales.
- La fauna se mueve con rutas simples, sin NavMesh.
- Los objetos lejanos se reciclan cerca del jugador.
- Los sprites se orientan hacia la camara desde un solo script.
- No se crean ni destruyen objetos continuamente durante la partida.

## Contenido recomendado para el proyecto

- Flora principal: pino, oyamel y encino.
- Flora secundaria: arbustos, pasto alto, flores y plantas de milpa.
- Fauna ambiental: pato, venado y aves.
- Fauna de mayor cuidado: colocar pocos ejemplares y evitar que todos tengan fisica, audio o IA compleja al mismo tiempo.
