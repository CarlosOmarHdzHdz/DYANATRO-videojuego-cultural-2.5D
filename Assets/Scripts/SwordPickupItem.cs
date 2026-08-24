using UnityEngine;

public class SwordPickupItem : MonoBehaviour
{
    // ========================================================================
    // Xunjuu v0.1 - Recompensa de mision
    // ACCION: equipar el arma, desbloquear Orbitasword y registrar su sprite.
    // ========================================================================
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 2.4f;
    [SerializeField] private float pickupScale = 0.42f;
    [SerializeField] private Sprite inventoryIcon;

    private Vector3 startPosition;
    private Vector3 originalScale;
    private OrbitalWeapon orbitalWeapon;
    private bool pickedUp;

    void Awake()
    {
        orbitalWeapon = GetComponent<OrbitalWeapon>();
        originalScale = transform.localScale;
        EnsurePickupCollider();
    }

    void Start()
    {
        startPosition = transform.position;
        transform.localScale = originalScale * pickupScale;
    }

    void Update()
    {
        if (pickedUp)
            return;

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPosition + Vector3.up * bob;
        transform.rotation = Quaternion.Euler(0f, 0f, 200f + Mathf.Sin(Time.time * 2f) * 8f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (pickedUp)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        Equip(player);
    }

    void Equip(PlayerController player)
    {
        pickedUp = true;
        Sprite rewardIcon = inventoryIcon;
        if (rewardIcon == null)
            rewardIcon = GetComponentInChildren<SpriteRenderer>()?.sprite;
        transform.SetParent(player.transform, false);

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
            Destroy(rigidbody);

        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;

        if (orbitalWeapon == null)
            orbitalWeapon = GetComponent<OrbitalWeapon>();

        if (orbitalWeapon != null)
        {
            transform.localScale = originalScale;
            orbitalWeapon.Initialize(player.transform);
            player.SetOrbitalWeapon(orbitalWeapon, true);
        }

        XunjuuInventory inventory = player.GetComponent<XunjuuInventory>();
        if (inventory != null)
            inventory.AddItem("macuahuitl", "Macuahuitl", 1, rewardIcon);

        DyanatroGameDirector director = FindFirstObjectByType<DyanatroGameDirector>();
        if (director != null)
            director.OnSwordCollected();

        Destroy(this);
    }

    public void ConfigureInventoryIcon(Sprite icon)
    {
        inventoryIcon = icon;
    }

    void EnsurePickupCollider()
    {
        Collider pickupCollider = GetComponent<Collider>();
        if (pickupCollider == null)
            pickupCollider = gameObject.AddComponent<SphereCollider>();

        pickupCollider.isTrigger = true;

        SphereCollider sphere = pickupCollider as SphereCollider;
        if (sphere != null)
            sphere.radius = Mathf.Max(sphere.radius, 1.1f);

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }
}
