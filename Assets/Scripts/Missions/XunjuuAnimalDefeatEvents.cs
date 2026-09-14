using System;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Canal de eventos para animales derrotados
// ACCION: permite que una mision cuente animales sin depender del prefab exacto.
// MODIFICACION: este archivo no necesita referencias en Inspector.
// ============================================================================
public static class XunjuuAnimalDefeatEvents
{
    public static event Action<GameObject> AnimalDefeated;

    // ACCION: XunjuuAnimalHealth llama este metodo una sola vez al llegar a cero.
    public static void Report(GameObject animal)
    {
        if (animal != null)
            AnimalDefeated?.Invoke(animal);
    }
}
