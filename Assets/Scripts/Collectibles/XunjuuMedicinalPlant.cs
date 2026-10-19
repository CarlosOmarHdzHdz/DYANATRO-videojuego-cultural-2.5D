using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class XunjuuMedicinalPlant : MonoBehaviour
{
    [SerializeField] private string plantId = "manzanilla";
    private bool harvested;
    private SpriteRenderer visual;
    public XunjuuMedicinalPlantCatalog.Entry Entry => XunjuuMedicinalPlantCatalog.Find(plantId);
    public bool IsAvailable => !harvested && isActiveAndEnabled && Entry != null;
    public const float HarvestDistance = 3.2f;

    public void Configure(string id)
    {
        plantId = id;
        visual = GetComponent<SpriteRenderer>();
        if (visual == null) visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = XunjuuMedicinalPlantCatalog.GetSprite(Entry);
        visual.sortingOrder = 600;
        var collider = GetComponent<BoxCollider>();
        if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = Vector3.up * 1.1f;
        collider.size = new Vector3(visual.sprite != null ? visual.sprite.bounds.size.x * .8f : 1.1f, 2.2f, .9f);
    }

    public bool TryHarvest(PlayerController player, out string message)
    {
        message = string.Empty;
        if (!IsAvailable || player == null || Time.timeScale <= 0 || player.GetCurrentHealth() <= 0) return false;
        Vector3 delta = transform.position - player.transform.position; delta.y = 0;
        if (delta.sqrMagnitude > HarvestDistance * HarvestDistance) return false;
        var inventory = player.GetComponent<XunjuuInventory>();
        if (inventory == null || !inventory.AddItem(Entry.Id, Entry.DisplayName, 1, XunjuuMedicinalPlantCatalog.GetSprite(Entry)))
        { message = "Morral lleno: libera un espacio antes de recolectar."; return false; }
        harvested = true;
        bool newCard = XunjuuMedicinalPlantCatalog.Register(Entry.Id);
        message = Entry.DisplayName + " +1 · " + (newCard ? "Nueva ficha en la ludoteca" : "Ingrediente guardado en el morral");
        StartCoroutine(HarvestAnimation());
        return true;
    }
    private void LateUpdate()
    {
        if (visual != null && Camera.main != null)
        {
            Vector3 forward = Camera.main.transform.forward; forward.y = 0;
            if (forward.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(forward);
        }
    }
    private IEnumerator HarvestAnimation()
    {
        foreach (var c in GetComponents<Collider>()) c.enabled = false;
        Vector3 origin = transform.position, scale = transform.localScale;
        for (float t = 0; t < .35f; t += Time.deltaTime)
        {
            float p = Mathf.Clamp01(t / .35f);
            transform.position = origin + Vector3.up * Mathf.Sin(p * Mathf.PI) * .45f;
            transform.localScale = scale * (1 - p * .9f);
            visual.color = new Color(1, 1, 1, 1 - p);
            yield return null;
        }
        Destroy(gameObject);
    }
}
