using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Recompensa pendiente del nivel 2
// ACCION: marca el lugar donde se agregara la recompensa definitiva mas adelante.
// MODIFICACION: reemplaza este prefab en XunjuuLevel2KillMission desde Inspector.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuRewardPlaceholder : MonoBehaviour
{
    [Header("Xunjuu v0.1 - Datos editables")]
    [Tooltip("Nombre temporal visible para identificar la recompensa pendiente.")]
    [SerializeField] private string rewardName = "Recompensa pendiente";

    public string RewardName => rewardName;
}
