using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Xunju2DSwordPickupItem : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.16f;
    [SerializeField] private float bobSpeed = 2.2f;

    private Vector3 startPosition;
    private bool pickedUp;

    void Start()
    {
        startPosition = transform.position;
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = true;
    }

    void Update()
    {
        if (pickedUp)
            return;

        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.rotation = Quaternion.Euler(0f, 0f, -34f + Mathf.Sin(Time.time * 2f) * 8f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (pickedUp)
            return;

        Xunju2DPlayerController player = other.GetComponentInParent<Xunju2DPlayerController>();
        if (player == null)
            return;

        pickedUp = true;
        foreach (Collider2D collider in GetComponents<Collider2D>())
            collider.enabled = false;

        player.EquipSword(transform);

        Xunju2DGameManager manager = FindFirstObjectByType<Xunju2DGameManager>();
        if (manager != null)
            manager.OnSwordCollected();

        Destroy(this);
    }
}
