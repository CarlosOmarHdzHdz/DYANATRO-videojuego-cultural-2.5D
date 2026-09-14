using UnityEngine;
using UnityEngine.Events;

// ============================================================================
// Xunjuu v0.1 - Recompensa editable de arma
// ACCION: entregar el prefab del macuahuitl; Orbitasword se obtiene mas adelante.
// MODIFICACION: puede reutilizarse en cualquier nivel desde el Inspector.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuSecondaryMissionWeaponReward : MonoBehaviour
{
    [Header("Xunjuu v0.1 - Recompensa de mision")]
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform rewardPoint;
    [SerializeField] private PlayerController player;
    [SerializeField] private Sprite inventoryIcon;
    [SerializeField] private bool equipImmediately = true;
    [SerializeField] private bool unlockOrbitalOnGrant;
    [SerializeField] private bool missionCompleted;

    [Header("Xunjuu v0.1 - Eventos editables")]
    [SerializeField] private UnityEvent onWeaponRewardGranted;

    public bool MissionCompleted => missionCompleted;

    // ACCION: devolver la recompensa a su estado inicial durante pruebas.
    [ContextMenu("Xunjuu v0.1/Reiniciar recompensa")]
    public void ResetRewardState()
    {
        missionCompleted = false;
    }

    // ACCION: llamar este metodo desde el objetivo final de la mision secundaria.
    [ContextMenu("Xunjuu v0.1/Completar mision y entregar macuahuitl")]
    public void CompleteMission()
    {
        if (missionCompleted || weaponPrefab == null)
            return;

        ResolvePlayer();
        if (player == null)
        {
            Debug.LogWarning("Xunjuu v0.1: no se encontro Player1 para entregar el macuahuitl.");
            return;
        }

        missionCompleted = true;
        if (equipImmediately)
            EquipReward();
        else
            SpawnPickupReward();

        onWeaponRewardGranted?.Invoke();
    }

    private void EquipReward()
    {
        GameObject weaponObject = Instantiate(weaponPrefab, player.transform);
        weaponObject.name = "Macuahuitl_Recompensa_Nivel2";

        SwordPickupItem pickup = weaponObject.GetComponent<SwordPickupItem>();
        if (pickup != null)
            Destroy(pickup);
        FloatingSword floating = weaponObject.GetComponent<FloatingSword>();
        if (floating != null)
            Destroy(floating);

        foreach (Collider weaponCollider in weaponObject.GetComponentsInChildren<Collider>(true))
            weaponCollider.enabled = false;

        Rigidbody body = weaponObject.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        OrbitalWeapon weapon = weaponObject.GetComponent<OrbitalWeapon>();
        if (weapon == null)
            weapon = weaponObject.AddComponent<OrbitalWeapon>();
        weapon.Initialize(player.transform);
        player.SetOrbitalWeapon(weapon, unlockOrbitalOnGrant);
        RegisterWeaponInInventory(weaponObject);

        DyanatroGameDirector director = FindFirstObjectByType<DyanatroGameDirector>();
        if (director != null)
            director.OnSwordCollected();
    }

    private void SpawnPickupReward()
    {
        Vector3 position = rewardPoint != null ? rewardPoint.position : transform.position + Vector3.up;
        GameObject weaponObject = Instantiate(weaponPrefab, position, Quaternion.identity);
        weaponObject.name = "Macuahuitl_Recompensa_Recogible";
        SwordPickupItem pickup = weaponObject.GetComponent<SwordPickupItem>();
        if (pickup == null)
            pickup = weaponObject.AddComponent<SwordPickupItem>();
        pickup.ConfigureInventoryIcon(ResolveIcon(weaponObject));
    }

    private void RegisterWeaponInInventory(GameObject weaponObject)
    {
        XunjuuInventory inventory = player.GetComponent<XunjuuInventory>();
        if (inventory != null)
            inventory.AddItem("macuahuitl", "Macuahuitl", 1, ResolveIcon(weaponObject));
    }

    private Sprite ResolveIcon(GameObject weaponObject)
    {
        return inventoryIcon != null ? inventoryIcon : weaponObject.GetComponentInChildren<SpriteRenderer>()?.sprite;
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.GetComponent<PlayerController>();
    }
}
