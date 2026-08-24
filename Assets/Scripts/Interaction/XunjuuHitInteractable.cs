using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Base de interacción por golpe
// Acción: unificar los objetos que reaccionan a ataques del protagonista.
// ============================================================================
public abstract class XunjuuHitInteractable : MonoBehaviour
{
    public abstract bool ReceiveHit(int damage, GameObject source);

    public virtual bool IsVisuallyAvailable => true;
    public virtual bool IsInteractionAvailable => IsVisuallyAvailable;

    // ACCIÓN: localizar el inventario del objeto que inició la interacción.
    protected XunjuuInventory FindSourceInventory(GameObject source)
    {
        if (source != null)
        {
            XunjuuInventory sourceInventory = source.GetComponentInParent<XunjuuInventory>();
            if (sourceInventory != null)
                return sourceInventory;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        return player != null ? player.GetComponent<XunjuuInventory>() : null;
    }
}
