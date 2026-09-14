using System;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Eventos de enemigos
// Accion: avisar a las misiones cuando un enemigo es derrotado.
// ============================================================================
public static class XunjuuEnemyDefeatEvents
{
    public static event Action<GameObject> EnemyDefeated;

    public static void Report(GameObject enemy)
    {
        if (enemy != null)
            EnemyDefeated?.Invoke(enemy);
    }
}
