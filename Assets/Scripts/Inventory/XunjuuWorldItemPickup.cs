using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Recurso recogible del mundo
// Acción: mostrar una cosecha en el suelo y guardarla al tocar al protagonista.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuWorldItemPickup : MonoBehaviour
{
    [Header("Xunjuú v0.1 - Objeto")]
    [SerializeField] private string itemId = "mazorca";
    [SerializeField] private string displayName = "Mazorca";
    [SerializeField, Min(1)] private int amount = 1;
    [SerializeField] private Sprite icon;

    [Header("Xunjuú v0.1 - Aparición")]
    [SerializeField, Min(0f)] private float pickupDelay = 0.35f;
    [SerializeField, Min(0.1f)] private float launchDuration = 0.42f;
    [SerializeField, Min(0f)] private float launchHeight = 0.55f;
    [SerializeField, Min(0f)] private float bobHeight = 0.08f;
    [SerializeField, Min(0.1f)] private float bobSpeed = 3.2f;

    private SpriteRenderer itemRenderer;
    private Vector3 launchOrigin;
    private Vector3 landingPosition;
    private float spawnedAt;
    private bool collected;

    public string ItemId => itemId;
    public int Amount => amount;

    private void Awake()
    {
        itemRenderer = GetComponentInChildren<SpriteRenderer>();
        EnsurePhysics();
        spawnedAt = Time.time;
        launchOrigin = transform.position;
        landingPosition = launchOrigin;
    }

    private void Update()
    {
        float elapsed = Time.time - spawnedAt;
        if (elapsed < launchDuration)
        {
            float t = Mathf.Clamp01(elapsed / launchDuration);
            Vector3 position = Vector3.Lerp(launchOrigin, landingPosition, Mathf.SmoothStep(0f, 1f, t));
            position.y += Mathf.Sin(t * Mathf.PI) * launchHeight;
            transform.position = position;
        }
        else
        {
            float bob = Mathf.Sin((elapsed - launchDuration) * bobSpeed) * bobHeight;
            transform.position = landingPosition + Vector3.up * bob;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 2.4f) * 8f);
    }

    // ACCIÓN: crear un maíz visible aunque todavía no exista un prefab de cosecha.
    public static XunjuuWorldItemPickup Spawn(
        Vector3 position,
        string newItemId,
        string newDisplayName,
        int newAmount,
        Sprite newIcon)
    {
        GameObject drop = new GameObject("Cosecha_" + newDisplayName);
        drop.transform.position = position;

        SpriteRenderer renderer = drop.AddComponent<SpriteRenderer>();
        renderer.sprite = newIcon;
        renderer.sortingOrder = 620;

        XunjuuWorldItemPickup pickup = drop.AddComponent<XunjuuWorldItemPickup>();
        pickup.Configure(newItemId, newDisplayName, newAmount, newIcon);
        pickup.Launch(position);
        return pickup;
    }

    // ACCIÓN: permitir que un prefab personalizado reutilice los datos del cultivo.
    public void Configure(string newItemId, string newDisplayName, int newAmount, Sprite newIcon)
    {
        itemId = string.IsNullOrWhiteSpace(newItemId) ? "mazorca" : newItemId.Trim();
        displayName = string.IsNullOrWhiteSpace(newDisplayName) ? itemId : newDisplayName.Trim();
        amount = Mathf.Max(1, newAmount);
        icon = newIcon;

        if (itemRenderer == null)
            itemRenderer = GetComponentInChildren<SpriteRenderer>();
        if (itemRenderer == null)
            itemRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (icon != null)
            itemRenderer.sprite = icon;
        itemRenderer.sortingOrder = Mathf.Max(itemRenderer.sortingOrder, 620);
    }

    // ACCIÓN: lanzar la mazorca hacia un lado para que la cosecha sea legible.
    public void Launch(Vector3 origin)
    {
        spawnedAt = Time.time;
        launchOrigin = origin;
        Vector2 spread = Random.insideUnitCircle.normalized * Random.Range(0.38f, 0.72f);
        landingPosition = origin + new Vector3(spread.x, 0.08f, spread.y);
        transform.position = launchOrigin;
    }

    // ACCIÓN: transferir el recurso al inventario solamente después de recogerlo.
    public bool Collect(GameObject collector)
    {
        if (collected || Time.time - spawnedAt < pickupDelay)
            return false;

        XunjuuInventory inventory = collector != null ? collector.GetComponentInParent<XunjuuInventory>() : null;
        if (inventory == null)
            return false;
        if (!inventory.AddItem(itemId, displayName, amount, icon))
            return false;

        collected = true;
        Destroy(gameObject);
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollectFromCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCollectFromCollider(other);
    }

    private void TryCollectFromCollider(Collider other)
    {
        if (other == null || other.GetComponentInParent<PlayerController>() == null)
            return;
        Collect(other.gameObject);
    }

    private void EnsurePhysics()
    {
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger == null)
            trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = Mathf.Max(0.32f, trigger.radius);

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }
}
