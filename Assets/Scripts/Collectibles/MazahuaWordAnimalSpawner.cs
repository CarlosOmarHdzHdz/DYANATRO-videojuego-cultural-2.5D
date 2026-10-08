using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Animales de coleccionable
// Acción: permitir asignar y ajustar prefabs desde cada objeto en Hierarchy.
// ============================================================================
[DisallowMultipleComponent]
public class MazahuaWordAnimalSpawner : MonoBehaviour
{
    [Header("Prefabs de animales")]
    [SerializeField] private GameObject[] animalPrefabs;

    [Header("Aparicion")]
    [SerializeField] private int minAnimals = 2;
    [SerializeField] private int maxAnimals = 4;
    [SerializeField] private float spawnRadius = 5.5f;
    [SerializeField] private float minDistance = 1.8f;
    [SerializeField] private float groundOffset = 0.12f;

    private bool hasSpawned;

    public GameObject[] AnimalPrefabs => animalPrefabs;

    public void Configure(GameObject[] prefabs, int minCount, int maxCount, float radius)
    {
        animalPrefabs = prefabs;
        minAnimals = Mathf.Max(1, minCount);
        maxAnimals = Mathf.Max(minAnimals, maxCount);
        spawnRadius = Mathf.Max(2f, radius);
    }

    public void SpawnForWord(string word)
    {
        if (hasSpawned)
            return;

        GameObject[] prefabs = GetAnimalPrefabs();
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("No hay prefabs de animales para la palabra mazahua: " + word);
            return;
        }

        hasSpawned = true;
        int count = Random.Range(minAnimals, maxAnimals + 1);
        GameObject root = GameObject.Find("Animales_Palabras_Mazahuas");
        if (root == null)
            root = new GameObject("Animales_Palabras_Mazahuas");

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[i % prefabs.Length];
            if (prefab == null)
                continue;

            Vector3 position = FindGroundedSpawnPoint(i, count);
            GameObject animal = Instantiate(prefab, position, Quaternion.identity, root.transform);
            animal.name = "Animal_" + word + "_" + prefab.name;
            PrepareAnimal(animal, i);
        }
    }

    // ACCIÓN: probar el spawner seleccionado sin recoger la palabra.
    [ContextMenu("Xunjuú v0.1/Probar aparición de animales")]
    public void PreviewSpawn()
    {
        hasSpawned = false;
        SpawnForWord(transform.parent != null ? transform.parent.name : gameObject.name);
    }

    // ACCIÓN: permitir una nueva prueba de aparición.
    [ContextMenu("Xunjuú v0.1/Reiniciar spawner")]
    public void ResetSpawner()
    {
        hasSpawned = false;
    }

    private GameObject[] GetAnimalPrefabs()
    {
        if (animalPrefabs != null && animalPrefabs.Length > 0)
            return animalPrefabs;

        return Resources.LoadAll<GameObject>("Prefabs/Animals");
    }

    private Vector3 FindGroundedSpawnPoint(int index, int count)
    {
        float angle = ((360f / Mathf.Max(1, count)) * index + Random.Range(-24f, 24f)) * Mathf.Deg2Rad;
        float radius = Random.Range(minDistance, spawnRadius);
        Vector3 target = transform.position + new Vector3(Mathf.Cos(angle) * radius, 20f, Mathf.Sin(angle) * radius);

        if (Physics.Raycast(target, Vector3.down, out RaycastHit hit, 80f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * groundOffset;

        return new Vector3(target.x, transform.position.y, target.z);
    }

    private void PrepareAnimal(GameObject animal, int index)
    {
        SpriteRenderer renderer = animal.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 700 + index;
            if (animal.GetComponentInChildren<CloudBillboard>() == null)
                renderer.gameObject.AddComponent<CloudBillboard>();

            DyanatroSpriteDepthSorter sorter = renderer.GetComponent<DyanatroSpriteDepthSorter>();
            if (sorter == null)
                sorter = renderer.gameObject.AddComponent<DyanatroSpriteDepthSorter>();
            sorter.Configure(700, 10f);
        }

        Collider collider = animal.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider box = animal.AddComponent<BoxCollider>();
            box.size = new Vector3(0.8f, 0.8f, 0.8f);
            box.center = new Vector3(0f, 0.4f, 0f);
        }

        Rigidbody rigidbody = animal.GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = animal.AddComponent<Rigidbody>();

        rigidbody.useGravity = true;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        if (animal.GetComponent<Animal>() == null)
            animal.AddComponent<Animal>();
    }
}
