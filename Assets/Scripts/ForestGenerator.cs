using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ForestOptimized : MonoBehaviour
{
    [Header("=== CONFIGURACIÃ“N DEL BOSQUE ===")]
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private int numberOfTrees = 100;

    [Header("=== TERRAIN ===")]
    [SerializeField] private Terrain terrain;

    [Header("=== OBJECT POOLING ===")]
    [SerializeField] private int poolSize = 150; // TamaÃ±o del pool (mayor que numberOfTrees)
    [SerializeField] private int treesPerFrame = 5; // Crear gradualmente (evita lag)

    [Header("=== DISTANCE CULLING ===")]
    [SerializeField] private float renderDistance = 40f; // Distancia para mostrar Ã¡rbol
    [SerializeField] private float collisionDistance = 20f; // Distancia para activar collider
    [SerializeField] private int updateIntervalFrames = 15; // Actualizar visibilidad cada N frames

    [Header("=== GENERACIÃ“N ALEATORIA ===")]
    [SerializeField] private int seed = 0;
    [SerializeField] private float minDistanceBetweenTrees = 8f;
    [SerializeField] private float minDistanceFromPlayer = 18f;
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float yOffset = 0f;

    // Pool de Ã¡rboles
    private List<GameObject> treePool = new List<GameObject>();
    private List<TreeData> treeData = new List<TreeData>();

    // Referencias
    private Transform player;
    private int frameCounter = 0;
    private bool isGenerated = false;

    // Estructura para guardar datos de cada Ã¡rbol
    [System.Serializable]
    private class TreeData
    {
        public Vector3 position;
        public bool isVisible;
        public TreeInteractivo treeScript;
        public Collider treeCollider;
        public SpriteRenderer spriteRenderer;
    }

    void Start()
    {
        // Buscar referencias
        if (terrain == null || terrain.terrainData == null)
            terrain = FindUsableTerrain();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            Debug.LogWarning("âš ï¸ No se encontrÃ³ el Player con tag 'Player'");

        if (treePrefab == null)
        {
            Debug.LogError("ForestOptimized necesita un Tree Prefab asignado.");
            enabled = false;
            return;
        }

        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning("ForestOptimized no encontro un Terrain con TerrainData. No se generara el bosque.");
            enabled = false;
            return;
        }

        // Configurar GPU Instancing
        ConfigureGPUInstancing();

        // Iniciar generaciÃ³n
        StartCoroutine(GenerateForestCoroutine());
    }

    Terrain FindUsableTerrain()
    {
        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        foreach (Terrain candidate in terrains)
        {
            if (candidate != null && candidate.terrainData != null)
                return candidate;
        }

        return null;
    }

    void ConfigureGPUInstancing()
    {
        if (treePrefab != null)
        {
            SpriteRenderer sr = treePrefab.GetComponent<SpriteRenderer>();
            if (sr != null && sr.material != null)
            {
                sr.material.enableInstancing = true;
                Debug.Log("âœ… GPU Instancing activado para Ã¡rboles");
            }
        }
    }

    IEnumerator GenerateForestCoroutine()
    {
        Debug.Log("ðŸŒ³ Iniciando generaciÃ³n del bosque optimizado...");

        if (treePrefab == null || terrain == null || terrain.terrainData == null)
            yield break;

        // Configurar semilla
        if (seed == 0)
            Random.InitState(System.DateTime.Now.Millisecond);
        else
            Random.InitState(seed);

        // Calcular posiciones de todos los Ã¡rboles
        List<Vector3> positions = CalculateTreePositions();

        // Crear pool de Ã¡rboles (tamaÃ±o fijo)
        int actualPoolSize = Mathf.Max(poolSize, numberOfTrees);

        for (int i = 0; i < actualPoolSize; i++)
        {
            GameObject tree = Instantiate(treePrefab);
            tree.SetActive(false);
            treePool.Add(tree);

            // Crear datos del Ã¡rbol
            TreeData data = new TreeData();
            data.treeScript = tree.GetComponent<TreeInteractivo>();
            data.treeCollider = tree.GetComponent<Collider>();
            data.spriteRenderer = tree.GetComponent<SpriteRenderer>();
            data.isVisible = false;

            // PosiciÃ³n (solo para los primeros numberOfTrees)
            if (i < positions.Count)
            {
                data.position = positions[i];
                tree.transform.position = data.position;
                SnapTreeToGround(tree.transform);
                data.position = tree.transform.position;
            }
            else
            {
                data.position = Vector3.zero;
            }

            treeData.Add(data);

            // Crear gradualmente para evitar lag
            if (i % treesPerFrame == 0)
                yield return null;
        }

        // Activar los primeros Ã¡rboles (los que estÃ¡n dentro del rango)
        UpdateAllTreesVisibility();

        isGenerated = true;
        Debug.Log($"âœ… Bosque generado! {numberOfTrees} Ã¡rboles en pool de {actualPoolSize}");
    }

    List<Vector3> CalculateTreePositions()
    {
        List<Vector3> positions = new List<Vector3>();

        if (terrain == null)
        {
            Debug.LogError("âŒ No hay Terrain asignado");
            return positions;
        }

        if (terrain.terrainData == null)
        {
            Debug.LogWarning("ForestOptimized: el Terrain asignado no tiene TerrainData.");
            return positions;
        }

        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        int maxAttempts = numberOfTrees * 20;
        int attempts = 0;

        while (positions.Count < numberOfTrees && attempts < maxAttempts)
        {
            // PosiciÃ³n aleatoria en todo el terreno
            float posX = Random.Range(terrainPos.x, terrainPos.x + terrainSize.x);
            float posZ = Random.Range(terrainPos.z, terrainPos.z + terrainSize.z);
            float posY = GetTerrainHeight(posX, posZ) + yOffset;

            Vector3 newPosition = new Vector3(posX, posY, posZ);

            if (player != null && Vector3.Distance(newPosition, player.position) < minDistanceFromPlayer)
            {
                attempts++;
                continue;
            }

            // Verificar pendiente
            float slope = GetTerrainSlope(posX, posZ);
            if (slope > maxSlopeAngle)
            {
                attempts++;
                continue;
            }

            // Verificar distancia con otras posiciones
            bool tooClose = false;
            foreach (Vector3 pos in positions)
            {
                if (Vector3.Distance(pos, newPosition) < minDistanceBetweenTrees)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                positions.Add(newPosition);
            }

            attempts++;
        }

        Debug.Log($"ðŸ“ {positions.Count} posiciones calculadas para Ã¡rboles");
        return positions;
    }

    float GetTerrainHeight(float x, float z)
    {
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 terrainPos = terrain.transform.position;
            float height = terrain.SampleHeight(new Vector3(x, 0, z));
            return height + terrainPos.y;
        }
        return 0f;
    }

    float GetTerrainSlope(float x, float z)
    {
        if (terrain == null || terrain.terrainData == null) return 0f;

        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        float normalizedX = (x - terrainPos.x) / terrainSize.x;
        float normalizedZ = (z - terrainPos.z) / terrainSize.z;

        if (normalizedX >= 0 && normalizedX <= 1 && normalizedZ >= 0 && normalizedZ <= 1)
        {
            return terrain.terrainData.GetSteepness(normalizedX, normalizedZ);
        }
        return 0f;
    }

    void SnapTreeToGround(Transform tree)
    {
        if (tree == null)
            return;

        Vector3 position = tree.position;
        float groundY = GetTerrainHeight(position.x, position.z);
        tree.position = new Vector3(position.x, groundY, position.z);

        SpriteRenderer renderer = tree.GetComponentInChildren<SpriteRenderer>();
        if (renderer == null)
            return;

        float delta = groundY - renderer.bounds.min.y;
        tree.position += Vector3.up * delta;
    }

    void Update()
    {
        if (!isGenerated) return;
        if (player == null) return;

        // Actualizar visibilidad cada N frames (ahorra CPU)
        frameCounter++;
        if (frameCounter >= updateIntervalFrames)
        {
            frameCounter = 0;
            UpdateAllTreesVisibility();
        }
    }

    void UpdateAllTreesVisibility()
    {
        int visibleCount = 0;

        for (int i = 0; i < treePool.Count && i < treeData.Count; i++)
        {
            GameObject tree = treePool[i];
            TreeData data = treeData[i];

            // Si el Ã¡rbol no tiene posiciÃ³n asignada, ignorar
            if (data.position == Vector3.zero) continue;

            // Calcular distancia al jugador
            float distance = Vector3.Distance(data.position, player.position);

            // DECISIÃ“N DE VISIBILIDAD
            bool shouldBeVisible = distance < renderDistance;

            // Activar/Desactivar el Ã¡rbol segÃºn distancia
            if (data.isVisible != shouldBeVisible)
            {
                data.isVisible = shouldBeVisible;
                tree.SetActive(shouldBeVisible);

                if (shouldBeVisible)
                    visibleCount++;
            }

            // ACTIVAR/DESACTIVAR COLLIDER segÃºn distancia (mÃ¡s cerca)
            if (data.treeCollider != null)
            {
                bool shouldHaveCollider = distance < collisionDistance;
                if (data.treeCollider.enabled != shouldHaveCollider)
                {
                    data.treeCollider.enabled = shouldHaveCollider;
                }
            }

            // OPTIMIZACIÃ“N: Reducir calidad de sprite si estÃ¡ lejos (opcional)
            if (data.spriteRenderer != null && shouldBeVisible)
            {
                // Si estÃ¡ cerca, calidad normal; si estÃ¡ lejos, reducir calidad
                if (distance > renderDistance * 0.7f)
                {
                    data.spriteRenderer.color = new Color(0.8f, 0.8f, 0.8f, 1f); // MÃ¡s oscuro/lejano
                }
                else
                {
                    data.spriteRenderer.color = Color.white;
                }
            }
        }

        // Debug cada cierto tiempo (opcional)
        if (Time.frameCount % 300 == 0)
        {
            Debug.Log($"ðŸŒ² Ãrboles visibles: {visibleCount}/{numberOfTrees}");
        }
    }

    // ============================================
    // MÃ‰TODOS PÃšBLICOS
    // ============================================

    [ContextMenu("Regenerar Bosque")]
    public void RegenerateForest()
    {
        ClearForest();
        StartCoroutine(GenerateForestCoroutine());
    }

    [ContextMenu("Limpiar Bosque")]
    public void ClearForest()
    {
        StopAllCoroutines();

        foreach (GameObject tree in treePool)
        {
            if (tree != null)
                Destroy(tree);
        }

        treePool.Clear();
        treeData.Clear();
        isGenerated = false;

        Debug.Log("ðŸ—‘ï¸ Bosque eliminado");
    }

    public GameObject GetNearestTree(Vector3 position)
    {
        GameObject nearest = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < treePool.Count && i < treeData.Count; i++)
        {
            if (!treePool[i].activeSelf) continue;

            float distance = Vector3.Distance(treeData[i].position, position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = treePool[i];
            }
        }

        return nearest;
    }

    // VisualizaciÃ³n en editor
    private void OnDrawGizmosSelected()
    {
        if (terrain != null && treeData != null)
        {
            Gizmos.color = Color.green;
            foreach (var data in treeData)
            {
                if (data != null && data.position != Vector3.zero)
                {
                    Gizmos.DrawWireCube(data.position, new Vector3(0.5f, 1f, 0.5f));
                }
            }
        }
    }
}


