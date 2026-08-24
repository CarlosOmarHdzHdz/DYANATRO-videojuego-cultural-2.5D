using UnityEngine;

public class CloudSpawner2_5D : MonoBehaviour
{
    [Header("=== PREFAB ===")]
    [SerializeField] private GameObject cloudPrefab;

    [Header("=== CANTIDAD ===")]
    [SerializeField] private int cloudCount = 42;

    [Header("=== DISPERSIÓN (desde el spawner) ===")]
    [SerializeField] private float rangeX = 230f;
    [SerializeField] private float rangeZ = 260f;
    [SerializeField] private float minHeight = 58f;
    [SerializeField] private float maxHeight = 95f;

    [Header("=== TAMAÑO ===")]
    [SerializeField] private float minScale = 1.65f;
    [SerializeField] private float maxScale = 4.15f;

    void Start()
    {
        // Asegurar que el spawner NO es hijo de nadie
        transform.SetParent(null);

        SpawnClouds();
    }

    void SpawnClouds()
    {
        if (cloudPrefab == null)
        {
            Debug.LogError("❌ No hay Cloud Prefab");
            return;
        }

        // Limpiar nubes viejas
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        for (int i = 0; i < cloudCount; i++)
        {
            // ============================================
            // POSICIÓN MUNDIAL = SPAWNER + OFFSET
            // ============================================
            float offsetX = Random.Range(-rangeX, rangeX);
            float offsetZ = Random.Range(-rangeZ, rangeZ);
            float y = Random.Range(minHeight, maxHeight);

            // Posición mundial (NO local)
            Vector3 worldPosition = transform.position + new Vector3(offsetX, y, offsetZ);

            float scale = Random.Range(minScale, maxScale);

            // Crear nube como HIJA del spawner (pero posición mundial)
            GameObject cloud = Instantiate(cloudPrefab, worldPosition, Quaternion.identity, transform);
            cloud.transform.localScale = Vector3.one * scale;
            cloud.tag = "Cloud";

            SpriteRenderer renderer = cloud.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = -2400 + i;
                renderer.color = new Color(1f, 1f, 1f, Random.Range(0.68f, 0.92f));
            }

            if (cloud.GetComponent<CloudBillboard>() == null)
                cloud.AddComponent<CloudBillboard>();
        }

        Debug.Log($"☁️ {cloudCount} nubes en posición MUNDIAL: {transform.position}");
        Debug.Log($"   Rango X: ±{rangeX}, Rango Z: ±{rangeZ}");
    }

    [ContextMenu("Regenerar Nubes")]
    public void RegenerateClouds()
    {
        SpawnClouds();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(rangeX * 2, maxHeight * 2, rangeZ * 2));

        // Marcar el centro
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 2f);
    }
}
