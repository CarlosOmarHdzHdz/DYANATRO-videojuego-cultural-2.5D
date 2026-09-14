using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// ============================================================================
// Xunjuú v0.1 - Árbol interactivo
// Acción: permitir talar, recoger madera, asignar prefabs y restaurar el árbol.
// ============================================================================
[DisallowMultipleComponent]
public class TreeInteractivo : XunjuuHitInteractable
{
    [Header("Xunjuú v0.1 - Resistencia")]
    [FormerlySerializedAs("health")]
    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField] private bool respawnAfterCut = true;
    [SerializeField, Min(1f)] private float respawnSeconds = 45f;

    [Header("Xunjuú v0.1 - Recurso de madera")]
    [SerializeField] private string woodItemId = "madera";
    [SerializeField] private string woodDisplayName = "Madera";
    [FormerlySerializedAs("woodDrop")]
    [SerializeField] private GameObject woodDropPrefab;
    [SerializeField] private Sprite woodInventoryIcon;
    [FormerlySerializedAs("woodAmount")]
    [SerializeField, Min(1)] private int woodAmount = 5;

    [Header("Xunjuú v0.1 - Efectos opcionales")]
    [SerializeField] private ParticleSystem leafParticles;
    [SerializeField, Min(1)] private int leafBurstCount = 18;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destroySound;

    [Header("Xunjuú v0.1 - Prefabs y variantes")]
    [SerializeField] private Sprite[] treeVariants;
    [SerializeField] private Color[] variantColors;
    [SerializeField] private bool randomizeVisualOnStart = true;
    [SerializeField] private Vector2 randomScaleRange = new Vector2(0.9f, 1.1f);

    [Header("Xunjuú v0.1 - Apariencia de árbol")]
    [SerializeField] private bool normalizeAgainstPlayer = true;
    [SerializeField] private Vector2 heightInPlayerHeights = new Vector2(3.1f, 4.2f);
    [SerializeField, Range(0f, 0.25f)] private float trunkGroundSink = 0.08f;

    private SpriteRenderer[] spriteRenderers;
    private Collider[] treeColliders;
    private AudioSource audioSource;
    private int currentHealth;
    private bool isCut;
    private Vector3 stableLocalPosition;
    private Vector3 stableLocalScale;

    public override bool IsVisuallyAvailable => !isCut;
    public bool IsAlive => !isCut && currentHealth > 0;
    public int CurrentHealth => currentHealth;

    private void Awake()
    {
        CacheComponents();
        Ensure3DCollider();
        currentHealth = maxHealth;
        stableLocalPosition = transform.localPosition;
        stableLocalScale = transform.localScale;

        if (randomizeVisualOnStart)
            ApplyRandomVariant();
    }

    private IEnumerator Start()
    {
        yield return null;
        NormalizeVisualAgainstPlayer();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        woodAmount = Mathf.Max(1, woodAmount);
        respawnSeconds = Mathf.Max(1f, respawnSeconds);
        randomScaleRange.x = Mathf.Max(0.2f, randomScaleRange.x);
        randomScaleRange.y = Mathf.Max(randomScaleRange.x, randomScaleRange.y);
        heightInPlayerHeights.x = Mathf.Max(1.8f, heightInPlayerHeights.x);
        heightInPlayerHeights.y = Mathf.Max(heightInPlayerHeights.x, heightInPlayerHeights.y);
    }

    // ACCIÓN: descontar resistencia usando un golpe por puño y más fuerza con espada.
    public override bool ReceiveHit(int damage, GameObject source)
    {
        if (!IsAlive)
            return false;

        int hitPower = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, damage) / 15f));
        currentHealth -= hitPower;
        PlayHitFeedback();

        if (currentHealth <= 0)
            CutTree(source);

        return true;
    }

    public void HitTree(int damage = 1)
    {
        ReceiveHit(Mathf.Max(1, damage) * 15, null);
    }

    // ACCIÓN: enviar la madera al inventario y usar un prefab solamente como apoyo opcional.
    private void CutTree(GameObject source)
    {
        isCut = true;
        currentHealth = 0;

        if (destroySound != null && audioSource != null)
            audioSource.PlayOneShot(destroySound);
        if (leafParticles != null)
            leafParticles.Emit(leafBurstCount * 2);

        XunjuuInventory inventory = FindSourceInventory(source);
        bool stored = inventory != null && inventory.AddItem(woodItemId, woodDisplayName, woodAmount, woodInventoryIcon);
        if (!stored && woodDropPrefab != null)
        {
            GameObject drop = Instantiate(woodDropPrefab, transform.position + Vector3.up * 0.25f, Quaternion.identity);
            drop.name = "Recurso_" + woodDisplayName;
        }

        SetTreeVisible(false);
        if (respawnAfterCut)
            StartCoroutine(RestoreAfterDelay());
    }

    // ACCIÓN: restaurar el árbol para que el nivel no pierda vegetación permanentemente.
    private IEnumerator RestoreAfterDelay()
    {
        yield return new WaitForSeconds(respawnSeconds);
        RestoreTree();
    }

    [ContextMenu("Xunjuú v0.1/Restaurar árbol")]
    public void RestoreTree()
    {
        StopAllCoroutines();
        if (stableLocalScale == Vector3.zero)
        {
            stableLocalPosition = transform.localPosition;
            stableLocalScale = transform.localScale;
        }
        isCut = false;
        currentHealth = maxHealth;
        transform.localPosition = stableLocalPosition;
        transform.localScale = stableLocalScale;
        SetTreeVisible(true);
    }

    // ACCIÓN: probar otra variante visual desde el menú del componente.
    [ContextMenu("Xunjuú v0.1/Aplicar variante aleatoria")]
    public void ApplyRandomVariant()
    {
        CacheComponents();
        Sprite selectedVariant = GetRandomUsableTreeSprite();
        if (spriteRenderers.Length > 0 && selectedVariant != null)
            spriteRenderers[0].sprite = selectedVariant;

        if (spriteRenderers.Length > 0 && variantColors != null && variantColors.Length > 0)
            spriteRenderers[0].color = variantColors[Random.Range(0, variantColors.Length)];

        float scale = Random.Range(randomScaleRange.x, randomScaleRange.y);
        transform.localScale = stableLocalScale == Vector3.zero ? transform.localScale * scale : stableLocalScale * scale;
    }

    // ACCIÓN: igualar la altura de todas las variantes usando al protagonista como referencia.
    [ContextMenu("Xunjuú v0.1/Normalizar escala y apoyar en el piso")]
    public void NormalizeVisualAgainstPlayer()
    {
        CacheComponents();
        SpriteRenderer treeRenderer = GetPrimaryRenderer();
        if (treeRenderer == null || treeRenderer.sprite == null)
            return;

        if (normalizeAgainstPlayer)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            SpriteRenderer playerVisual = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
            float playerHeight = playerVisual != null ? playerVisual.bounds.size.y : 1.4f;
            float treeHeight = Mathf.Max(0.01f, treeRenderer.bounds.size.y);
            float variation = Mathf.Repeat(Mathf.Abs(transform.position.x * 0.173f + transform.position.z * 0.319f), 1f);
            float targetHeight = playerHeight * Mathf.Lerp(heightInPlayerHeights.x, heightInPlayerHeights.y, variation);
            float correction = Mathf.Clamp(targetHeight / treeHeight, 0.18f, 4f);
            transform.localScale *= correction;
        }

        SnapVisibleTrunkToGround();
        stableLocalPosition = transform.localPosition;
        stableLocalScale = transform.localScale;
        Ensure3DCollider();
    }

    private Sprite GetRandomUsableTreeSprite()
    {
        System.Collections.Generic.List<Sprite> usable = new System.Collections.Generic.List<Sprite>();
        SpriteRenderer primary = GetPrimaryRenderer();
        if (primary != null && IsUsableTreeSprite(primary.sprite))
            usable.Add(primary.sprite);

        if (treeVariants != null)
        {
            foreach (Sprite variant in treeVariants)
            {
                if (IsUsableTreeSprite(variant) && !usable.Contains(variant))
                    usable.Add(variant);
            }
        }

        return usable.Count > 0 ? usable[Random.Range(0, usable.Count)] : null;
    }

    private static bool IsUsableTreeSprite(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return false;

        string textureName = sprite.texture.name.ToLowerInvariant().Replace(" ", string.Empty);
        if (textureName == "arbolllo" || textureName == "tree1")
            return false;

        return sprite.rect.width >= 48f && sprite.rect.height >= 64f;
    }

    private SpriteRenderer GetPrimaryRenderer()
    {
        SpriteRenderer selected = null;
        float largestArea = 0f;
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer == null || renderer.sprite == null)
                continue;

            float area = renderer.bounds.size.x * renderer.bounds.size.y;
            if (area <= largestArea)
                continue;
            selected = renderer;
            largestArea = area;
        }
        return selected;
    }

    // ACCIÓN: colocar la base visible del tronco sobre el Terrain sin dejar árboles flotando.
    private void SnapVisibleTrunkToGround()
    {
        SpriteRenderer primary = GetPrimaryRenderer();
        if (primary == null)
            return;

        float groundY = transform.position.y;
        bool foundTerrain = false;
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            bool inside = transform.position.x >= origin.x && transform.position.x <= origin.x + size.x
                && transform.position.z >= origin.z && transform.position.z <= origin.z + size.z;
            if (!inside)
                continue;

            groundY = terrain.SampleHeight(transform.position) + origin.y;
            foundTerrain = true;
            break;
        }

        if (!foundTerrain && Physics.Raycast(transform.position + Vector3.up * 80f, Vector3.down, out RaycastHit hit, 180f))
            groundY = hit.point.y;

        float delta = (groundY - trunkGroundSink) - primary.bounds.min.y;
        transform.position += Vector3.up * delta;
    }

    private void PlayHitFeedback()
    {
        if (leafParticles != null)
            leafParticles.Emit(leafBurstCount);
        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        StopCoroutine(nameof(ShakeTree));
        StartCoroutine(ShakeTree());
    }

    private IEnumerator ShakeTree()
    {
        Vector3 origin = transform.localPosition;
        for (int i = 0; i < 4; i++)
        {
            transform.localPosition = origin + new Vector3(i % 2 == 0 ? 0.045f : -0.045f, 0f, 0f);
            yield return null;
        }
        transform.localPosition = origin;
        stableLocalPosition = origin;
    }

    private void CacheComponents()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        treeColliders = GetComponentsInChildren<Collider>(true);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hitSound != null || destroySound != null))
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ACCIÓN: crear un bloqueo 3D en el tronco sin entrar en conflicto con colliders 2D.
    private void Ensure3DCollider()
    {
        CapsuleCollider capsule = GetComponentInChildren<CapsuleCollider>(true);
        if (capsule == null)
        {
            GameObject blocker = new GameObject("TreeBlocker3D");
            blocker.transform.SetParent(transform, false);
            blocker.layer = gameObject.layer;
            capsule = blocker.AddComponent<CapsuleCollider>();
        }

        capsule.transform.localPosition = Vector3.zero;
        capsule.center = new Vector3(0f, 0.34f, 0f);
        capsule.radius = 0.22f;
        capsule.height = 0.78f;
        capsule.direction = 1;
        capsule.isTrigger = false;
        treeColliders = GetComponentsInChildren<Collider>(true);
    }

    private void SetTreeVisible(bool visible)
    {
        CacheComponents();
        foreach (SpriteRenderer renderer in spriteRenderers)
            renderer.enabled = visible;
        foreach (Collider collider in treeColliders)
            collider.enabled = visible;

        XunjuuWorldLabel label = GetComponent<XunjuuWorldLabel>();
        if (label != null)
            label.enabled = visible;
    }

    public bool IsAliveLegacy()
    {
        return IsAlive;
    }

    public int GetHealth()
    {
        return currentHealth;
    }
}
