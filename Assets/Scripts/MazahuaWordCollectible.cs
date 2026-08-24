using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Coleccionable mazahua editable
// Acción: conservar cada palabra en Hierarchy y exponer sus datos en Inspector.
// ============================================================================
[DisallowMultipleComponent]
public class MazahuaWordCollectible : MonoBehaviour
{
    [Header("Xunjuú v0.1 - Contenido")]
    [SerializeField] private string mazahuaWord;
    [SerializeField] private string spanishMeaning;
    [TextArea(2, 5)]
    [SerializeField] private string mazahuaStory;
    [TextArea(2, 5)]
    [SerializeField] private string spanishStory;

    [Header("Xunjuú v0.1 - Presentación")]
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float bobSpeed = 2.2f;
    [SerializeField] private bool destroyOnCollect;
    [Tooltip("Solo para misiones alternativas. La beta activa los animales al reunir las cinco flores.")]
    [SerializeField] private bool spawnAssignedAnimalsImmediately;

    private Vector3 startPosition;
    private bool collected;

    public string MazahuaWord => mazahuaWord;
    public string SpanishMeaning => spanishMeaning;
    public bool IsCollected => collected;

    public void Configure(string word, string meaning, string storyMazahua = "", string storySpanish = "")
    {
        mazahuaWord = word;
        spanishMeaning = meaning;
        mazahuaStory = storyMazahua;
        spanishStory = storySpanish;
        gameObject.name = "Palabra_" + word;
        RefreshEditableLabel(true);
    }

    private void OnValidate()
    {
        bobHeight = Mathf.Max(0f, bobHeight);
        bobSpeed = Mathf.Max(0f, bobSpeed);
        RefreshEditableLabel(false);
    }

    void Start()
    {
        startPosition = transform.position;
        Collider collider = GetComponent<Collider>();
        if (collider == null)
            collider = gameObject.AddComponent<SphereCollider>();

        collider.isTrigger = true;

        SphereCollider sphere = collider as SphereCollider;
        if (sphere != null)
            sphere.radius = Mathf.Max(0.85f, sphere.radius);

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }

    void Update()
    {
        if (collected)
            return;

        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.Rotate(Vector3.up, 35f * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        // Xunjuu v0.1 - ACCION: guardar la flor-palabra como objeto real del inventario.
        XunjuuInventory inventory = player.GetComponent<XunjuuInventory>();
        if (inventory == null)
            inventory = player.gameObject.AddComponent<XunjuuInventory>();

        Sprite icon = ResolveInventoryIcon();
        string itemId = "palabra_" + NormalizeItemId(mazahuaWord);
        string displayName = mazahuaWord + " - " + spanishMeaning;
        if (!inventory.AddItem(itemId, displayName, 1, icon))
            return;

        collected = true;

        MazahuaWordAnimalSpawner animalSpawner = GetComponentInChildren<MazahuaWordAnimalSpawner>();
        if (spawnAssignedAnimalsImmediately && animalSpawner != null)
            animalSpawner.SpawnForWord(mazahuaWord);

        DyanatroGameDirector director = FindFirstObjectByType<DyanatroGameDirector>();
        if (director != null)
            director.CollectMazahuaWord(mazahuaWord, spanishMeaning, mazahuaStory, spanishStory);

        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    // Xunjuu v0.1 - ACCION: reutilizar el sprite visible de la flor en el inventario.
    private Sprite ResolveInventoryIcon()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.sprite != null && renderer.color.a > 0.5f)
                return renderer.sprite;
        }

        return Resources.Load<Sprite>("Sprites/Collectibles/TsirajnaNeDyebe");
    }

    private static string NormalizeItemId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "memoria";

        return value.Trim().ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("'", string.Empty)
            .Replace("ñ", "n")
            .Replace("ú", "u");
    }

    // ACCIÓN: actualizar el nombre visible cuando se edita la palabra.
    private void RefreshEditableLabel(bool createIfMissing)
    {
        if (string.IsNullOrWhiteSpace(mazahuaWord))
            return;

        XunjuuWorldLabel label = GetComponent<XunjuuWorldLabel>();
        if (label == null && createIfMissing)
            label = gameObject.AddComponent<XunjuuWorldLabel>();
        if (label != null)
            label.Configure(mazahuaWord + "\n" + spanishMeaning, true, new Vector3(0f, 1.28f, 0f));
    }

    // ACCIÓN: reactivar el coleccionable durante las pruebas del nivel.
    [ContextMenu("Xunjuú v0.1/Restaurar coleccionable")]
    public void RestoreCollectible()
    {
        collected = false;
        gameObject.SetActive(true);
        startPosition = transform.position;
    }
}
