using System.Collections;
using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Cultivo interactivo
// Acción: cultivar, podar con golpes, cosechar y guardar productos.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuCrop : XunjuuHitInteractable
{
    public enum CropState
    {
        Empty,
        Growing,
        Ready
    }

    [Header("Xunjuú v0.1 - Identidad")]
    [SerializeField] private string cropId = "maiz";
    [SerializeField] private string cropDisplayName = "Maíz";
    [SerializeField] private bool showIndividualName;

    [Header("Xunjuú v0.1 - Crecimiento")]
    [SerializeField] private CropState state = CropState.Ready;
    [SerializeField] private Sprite[] growthSprites;
    [SerializeField, Min(1f)] private float growthSeconds = 22f;
    [SerializeField, Min(1)] private int pruningHitsToHarvest = 2;
    [SerializeField] private bool regrowsAutomatically = true;
    [SerializeField, Min(0f)] private float regrowDelay = 12f;

    [Header("Xunjuú v0.1 - Cosecha")]
    [SerializeField] private string harvestItemId = "mazorca";
    [SerializeField] private string harvestDisplayName = "Mazorca";
    [SerializeField, Min(1)] private int harvestAmount = 2;
    [SerializeField] private Sprite inventoryIcon;
    [SerializeField] private GameObject harvestDropPrefab;

    private SpriteRenderer cropRenderer;
    private Collider cropCollider;
    private int remainingPruningHits;
    private float growthStartedAt;
    private Vector3 stableScale;
    private Coroutine shakeRoutine;

    public CropState State => state;
    public string CropId => cropId;
    public override bool IsVisuallyAvailable => state != CropState.Empty;
    public override bool IsInteractionAvailable => true;

    private void Awake()
    {
        cropRenderer = GetComponentInChildren<SpriteRenderer>();
        if (inventoryIcon == null)
            inventoryIcon = Resources.Load<Sprite>("Sprites/Items/maiz");
        stableScale = transform.localScale;
        remainingPruningHits = pruningHitsToHarvest;
        EnsureCollider();
        EnsureEditableName();
        ApplyVisualState();
    }

    private void Update()
    {
        if (state != CropState.Growing)
            return;

        float progress = Mathf.Clamp01((Time.time - growthStartedAt) / growthSeconds);
        ApplyGrowthVisual(progress);
        if (progress >= 1f)
            SetReady();
    }

    private void OnValidate()
    {
        growthSeconds = Mathf.Max(1f, growthSeconds);
        pruningHitsToHarvest = Mathf.Max(1, pruningHitsToHarvest);
        harvestAmount = Mathf.Max(1, harvestAmount);
    }

    // ACCIÓN: plantar en suelo vacío o podar y cosechar una planta existente.
    public override bool ReceiveHit(int damage, GameObject source)
    {
        if (state == CropState.Empty)
        {
            Plant();
            return true;
        }

        if (state == CropState.Growing)
        {
            growthStartedAt -= Mathf.Min(growthSeconds * 0.2f, Mathf.Max(1, damage) * 0.08f);
            PlayHitAnimation();
            return true;
        }

        remainingPruningHits--;
        PlayHitAnimation();
        if (remainingPruningHits <= 0)
            Harvest(source);
        return true;
    }

    // ACCIÓN: iniciar manualmente el ciclo de cultivo desde el Inspector.
    [ContextMenu("Xunjuú v0.1/Cultivar")]
    public void Plant()
    {
        StopAllCoroutines();
        state = CropState.Growing;
        growthStartedAt = Time.time;
        remainingPruningHits = pruningHitsToHarvest;
        SetRendererVisible(true);
        ApplyGrowthVisual(0f);
    }

    // ACCIÓN: dejar la planta lista para probar la cosecha.
    [ContextMenu("Xunjuú v0.1/Marcar como lista")]
    public void SetReady()
    {
        state = CropState.Ready;
        remainingPruningHits = pruningHitsToHarvest;
        SetRendererVisible(true);
        ApplyGrowthVisual(1f);
    }

    // ACCIÓN: soltar la mazorca en el mundo y dejar preparado el siguiente ciclo.
    private void Harvest(GameObject source)
    {
        Vector3 dropPosition = transform.position + Vector3.up * 0.32f;
        if (harvestDropPrefab != null)
        {
            GameObject drop = Instantiate(harvestDropPrefab, dropPosition, Quaternion.identity);
            drop.name = "Cosecha_" + harvestDisplayName;
            XunjuuWorldItemPickup pickup = drop.GetComponent<XunjuuWorldItemPickup>();
            if (pickup == null)
                pickup = drop.AddComponent<XunjuuWorldItemPickup>();
            pickup.Configure(harvestItemId, harvestDisplayName, harvestAmount, inventoryIcon);
            pickup.Launch(dropPosition);
        }
        else
        {
            XunjuuWorldItemPickup.Spawn(dropPosition, harvestItemId, harvestDisplayName, harvestAmount, inventoryIcon);
        }

        state = CropState.Empty;
        SetRendererVisible(false);
        if (regrowsAutomatically)
            StartCoroutine(Regrow());
    }

    private IEnumerator Regrow()
    {
        yield return new WaitForSeconds(regrowDelay);
        Plant();
    }

    private void ApplyVisualState()
    {
        if (state == CropState.Empty)
            SetRendererVisible(false);
        else if (state == CropState.Ready)
            SetReady();
        else
        {
            growthStartedAt = Time.time;
            ApplyGrowthVisual(0f);
        }
    }

    private void ApplyGrowthVisual(float progress)
    {
        SetRendererVisible(true);
        if (cropRenderer == null)
            return;

        if (growthSprites != null && growthSprites.Length > 0)
        {
            int index = Mathf.Clamp(Mathf.FloorToInt(progress * growthSprites.Length), 0, growthSprites.Length - 1);
            if (growthSprites[index] != null)
                cropRenderer.sprite = growthSprites[index];
        }

        float scale = Mathf.Lerp(0.38f, 1f, Mathf.SmoothStep(0f, 1f, progress));
        transform.localScale = new Vector3(stableScale.x * scale, stableScale.y * scale, stableScale.z);
        cropRenderer.color = Color.Lerp(new Color(0.48f, 0.8f, 0.24f, 1f), Color.white, progress);
    }

    private void PlayHitAnimation()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        Vector3 origin = transform.localPosition;
        transform.localPosition = origin + Vector3.right * 0.05f;
        yield return null;
        transform.localPosition = origin - Vector3.right * 0.05f;
        yield return null;
        transform.localPosition = origin;
    }

    private void EnsureCollider()
    {
        cropCollider = GetComponent<Collider>();
        if (cropCollider == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.65f, 0f);
            capsule.radius = 0.32f;
            capsule.height = 1.3f;
            capsule.isTrigger = false;
            cropCollider = capsule;
        }
    }

    private void EnsureEditableName()
    {
        string editableName = string.IsNullOrWhiteSpace(cropDisplayName) ? cropId : cropDisplayName;
        XunjuuWorldLabel label = GetComponent<XunjuuWorldLabel>();
        if (label != null)
            label.Configure(editableName, showIndividualName, new Vector3(0f, 1.65f, 0f));
    }

    private void SetRendererVisible(bool visible)
    {
        if (cropRenderer != null)
            cropRenderer.enabled = visible;
        if (cropCollider != null)
            cropCollider.enabled = visible || state == CropState.Empty;
    }
}
