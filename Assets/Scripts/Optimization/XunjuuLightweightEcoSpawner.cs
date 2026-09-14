using System.Collections.Generic;
using UnityEngine;

// Demo ambiental ligero para poblar flora y fauna sin cargar el CPU.
// Usa sprites, reciclaje por distancia y movimiento simple para fauna.
[DisallowMultipleComponent]
public sealed class XunjuuLightweightEcoSpawner : MonoBehaviour
{
    private const string RootName = "Demo_Flora_Fauna_Ligera";

    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Terrain terrain;

    [Header("Flora")]
    [SerializeField] private Sprite[] floraSprites;
    [SerializeField, Min(0)] private int floraCount = 120;
    [SerializeField, Min(8f)] private float floraSpawnRadius = 90f;
    [SerializeField, Min(8f)] private float floraVisibleRadius = 75f;
    [SerializeField, Min(12f)] private float floraRecycleRadius = 120f;
    [SerializeField] private Vector2 floraScaleRange = new Vector2(0.85f, 1.65f);
    [SerializeField] private float floraYOffset = 0.02f;

    [Header("Fauna ambiental")]
    [SerializeField] private Sprite[] faunaSprites;
    [SerializeField] private bool loadFaunaSpritesFromResourcePrefabs = true;
    [SerializeField, Min(0)] private int faunaCount = 8;
    [SerializeField, Min(8f)] private float faunaSpawnRadius = 65f;
    [SerializeField, Min(8f)] private float faunaVisibleRadius = 70f;
    [SerializeField, Min(12f)] private float faunaRecycleRadius = 110f;
    [SerializeField] private Vector2 faunaScaleRange = new Vector2(0.75f, 1.15f);
    [SerializeField] private Vector2 faunaSpeedRange = new Vector2(0.55f, 1.15f);
    [SerializeField, Min(1f)] private float faunaWanderRadius = 16f;
    [SerializeField, Min(0.3f)] private float faunaDecisionInterval = 2.2f;
    [SerializeField] private float faunaYOffset = 0.05f;

    [Header("Rendimiento")]
    [SerializeField] private bool generateOnStart;
    [SerializeField, Min(0.05f)] private float visibilityUpdateInterval = 0.25f;
    [SerializeField, Min(0.05f)] private float billboardUpdateInterval = 0.35f;
    [SerializeField] private int seed = 2609;
    [SerializeField] private int sortingBaseOrder = 80;
    [SerializeField] private float sortingOrderScale = 10f;

    private readonly List<FloraItem> flora = new List<FloraItem>();
    private readonly List<FaunaItem> fauna = new List<FaunaItem>();
    private Transform root;
    private float nextVisibilityUpdate;
    private float nextBillboardUpdate;

    private sealed class FloraItem
    {
        public GameObject Root;
        public SpriteRenderer Renderer;
    }

    private sealed class FaunaItem
    {
        public GameObject Root;
        public SpriteRenderer Renderer;
        public Vector3 Target;
        public float Speed;
        public float NextDecisionTime;
    }

    private void Start()
    {
        if (generateOnStart)
            GenerateDemo();
    }

    private void Update()
    {
        if (root == null)
            return;

        ResolveRuntimeReferences();

        if (Time.time >= nextVisibilityUpdate)
        {
            UpdateVisibilityAndRecycling();
            nextVisibilityUpdate = Time.time + visibilityUpdateInterval;
        }

        UpdateFaunaMovement(Time.deltaTime);

        if (Time.time >= nextBillboardUpdate)
        {
            FaceCameraAndSort();
            nextBillboardUpdate = Time.time + billboardUpdateInterval;
        }
    }

    [ContextMenu("Xunjuu/Demo ligero de flora y fauna/Generar")]
    public void GenerateDemo()
    {
        ResolveRuntimeReferences();
        ClearDemo();

        Random.InitState(seed == 0 ? System.DateTime.Now.Millisecond : seed);

        GameObject rootObject = new GameObject(RootName);
        rootObject.transform.SetParent(transform, false);
        root = rootObject.transform;

        Sprite[] resolvedFlora = ResolveFloraSprites();
        Sprite[] resolvedFauna = ResolveFaunaSprites();
        Sprite fallbackFlora = resolvedFlora.Length == 0 ? CreateFallbackSprite(new Color(0.18f, 0.42f, 0.18f)) : null;
        Sprite fallbackFauna = resolvedFauna.Length == 0 ? CreateFallbackSprite(new Color(0.55f, 0.36f, 0.18f)) : null;

        for (int i = 0; i < floraCount; i++)
        {
            Sprite sprite = resolvedFlora.Length > 0 ? resolvedFlora[i % resolvedFlora.Length] : fallbackFlora;
            FloraItem item = CreateFloraItem(i, sprite);
            item.Root.transform.position = RandomPointAroundPlayer(floraSpawnRadius, 10f, floraYOffset);
            float scale = Random.Range(floraScaleRange.x, floraScaleRange.y);
            item.Root.transform.localScale = Vector3.one * scale;
            flora.Add(item);
        }

        for (int i = 0; i < faunaCount; i++)
        {
            Sprite sprite = resolvedFauna.Length > 0 ? resolvedFauna[i % resolvedFauna.Length] : fallbackFauna;
            FaunaItem item = CreateFaunaItem(i, sprite);
            item.Root.transform.position = RandomPointAroundPlayer(faunaSpawnRadius, 14f, faunaYOffset);
            item.Target = RandomPointNear(item.Root.transform.position, faunaWanderRadius, faunaYOffset);
            item.Speed = Random.Range(faunaSpeedRange.x, faunaSpeedRange.y);
            item.NextDecisionTime = Time.time + Random.Range(0.2f, faunaDecisionInterval);
            float scale = Random.Range(faunaScaleRange.x, faunaScaleRange.y);
            item.Root.transform.localScale = Vector3.one * scale;
            fauna.Add(item);
        }

        UpdateVisibilityAndRecycling();
        FaceCameraAndSort();
    }

    [ContextMenu("Xunjuu/Demo ligero de flora y fauna/Limpiar")]
    public void ClearDemo()
    {
        flora.Clear();
        fauna.Clear();

        Transform previousRoot = root != null ? root : transform.Find(RootName);
        if (previousRoot != null)
            DestroySmart(previousRoot.gameObject);

        root = null;
    }

    private FloraItem CreateFloraItem(int index, Sprite sprite)
    {
        GameObject itemObject = new GameObject("Flora_Ligera_" + index.ToString("000"));
        itemObject.transform.SetParent(root, false);

        SpriteRenderer renderer = itemObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingBaseOrder;

        return new FloraItem
        {
            Root = itemObject,
            Renderer = renderer
        };
    }

    private FaunaItem CreateFaunaItem(int index, Sprite sprite)
    {
        GameObject itemObject = new GameObject("Fauna_Ligera_" + index.ToString("000"));
        itemObject.transform.SetParent(root, false);

        SpriteRenderer renderer = itemObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingBaseOrder + 20;

        return new FaunaItem
        {
            Root = itemObject,
            Renderer = renderer
        };
    }

    private void UpdateVisibilityAndRecycling()
    {
        Vector3 center = GetCenter();

        for (int i = 0; i < flora.Count; i++)
        {
            FloraItem item = flora[i];
            if (item == null || item.Root == null)
                continue;

            float distance = FlatDistance(center, item.Root.transform.position);
            if (distance > floraRecycleRadius)
                item.Root.transform.position = RandomPointAroundPlayer(floraSpawnRadius, 18f, floraYOffset);

            item.Root.SetActive(FlatDistance(center, item.Root.transform.position) <= floraVisibleRadius);
        }

        for (int i = 0; i < fauna.Count; i++)
        {
            FaunaItem item = fauna[i];
            if (item == null || item.Root == null)
                continue;

            float distance = FlatDistance(center, item.Root.transform.position);
            if (distance > faunaRecycleRadius)
            {
                item.Root.transform.position = RandomPointAroundPlayer(faunaSpawnRadius, 15f, faunaYOffset);
                item.Target = RandomPointNear(item.Root.transform.position, faunaWanderRadius, faunaYOffset);
            }

            item.Root.SetActive(FlatDistance(center, item.Root.transform.position) <= faunaVisibleRadius);
        }
    }

    private void UpdateFaunaMovement(float deltaTime)
    {
        for (int i = 0; i < fauna.Count; i++)
        {
            FaunaItem item = fauna[i];
            if (item == null || item.Root == null || !item.Root.activeSelf)
                continue;

            Vector3 position = item.Root.transform.position;
            Vector3 toTarget = Flatten(item.Target - position);

            if (Time.time >= item.NextDecisionTime || toTarget.sqrMagnitude < 0.5f)
            {
                item.Target = RandomPointNear(position, faunaWanderRadius, faunaYOffset);
                item.NextDecisionTime = Time.time + faunaDecisionInterval + Random.Range(-0.35f, 0.75f);
                toTarget = Flatten(item.Target - position);
            }

            if (toTarget.sqrMagnitude < 0.001f)
                continue;

            Vector3 direction = toTarget.normalized;
            Vector3 nextPosition = position + direction * item.Speed * deltaTime;
            nextPosition = SnapToGround(nextPosition, faunaYOffset);
            item.Root.transform.position = nextPosition;

            if (item.Renderer != null && Mathf.Abs(direction.x) > 0.03f)
                item.Renderer.flipX = direction.x < 0f;
        }
    }

    private void FaceCameraAndSort()
    {
        Quaternion facing = Quaternion.identity;
        bool hasCamera = targetCamera != null;
        if (hasCamera)
            facing = Quaternion.LookRotation(targetCamera.transform.forward, Vector3.up);

        for (int i = 0; i < flora.Count; i++)
        {
            FloraItem item = flora[i];
            if (item == null || item.Root == null || !item.Root.activeSelf)
                continue;

            if (hasCamera)
                item.Root.transform.rotation = facing;
            if (item.Renderer != null)
                item.Renderer.sortingOrder = sortingBaseOrder + Mathf.RoundToInt(-item.Root.transform.position.z * sortingOrderScale);
        }

        for (int i = 0; i < fauna.Count; i++)
        {
            FaunaItem item = fauna[i];
            if (item == null || item.Root == null || !item.Root.activeSelf)
                continue;

            if (hasCamera)
                item.Root.transform.rotation = facing;
            if (item.Renderer != null)
                item.Renderer.sortingOrder = sortingBaseOrder + 20 + Mathf.RoundToInt(-item.Root.transform.position.z * sortingOrderScale);
        }
    }

    private Sprite[] ResolveFloraSprites()
    {
        if (floraSprites != null && floraSprites.Length > 0)
            return floraSprites;

        List<Sprite> sprites = new List<Sprite>();
        AddResourceSprites(sprites, "Sprites/TextureTerrain/arbusto");
        AddResourceSprites(sprites, "Sprites/TextureTerrain/flor");
        AddResourceSprites(sprites, "Sprites/Collectibles/TsirajnaNeDyebe");
        return sprites.ToArray();
    }

    private Sprite[] ResolveFaunaSprites()
    {
        if (faunaSprites != null && faunaSprites.Length > 0)
            return faunaSprites;

        List<Sprite> sprites = new List<Sprite>();
        if (loadFaunaSpritesFromResourcePrefabs)
        {
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Animals");
            for (int i = 0; i < prefabs.Length; i++)
            {
                SpriteRenderer renderer = prefabs[i] != null ? prefabs[i].GetComponentInChildren<SpriteRenderer>(true) : null;
                if (renderer != null && renderer.sprite != null && !sprites.Contains(renderer.sprite))
                    sprites.Add(renderer.sprite);
            }
        }
        return sprites.ToArray();
    }

    private void AddResourceSprites(List<Sprite> sprites, string path)
    {
        Sprite single = Resources.Load<Sprite>(path);
        if (single != null && !sprites.Contains(single))
            sprites.Add(single);

        Sprite[] multiple = Resources.LoadAll<Sprite>(path);
        for (int i = 0; i < multiple.Length; i++)
        {
            if (multiple[i] != null && !sprites.Contains(multiple[i]))
                sprites.Add(multiple[i]);
        }
    }

    private Vector3 RandomPointAroundPlayer(float radius, float minRadius, float yOffset)
    {
        Vector3 center = GetCenter();
        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;
        direction.Normalize();

        float distance = Mathf.Sqrt(Random.Range(minRadius * minRadius, radius * radius));
        Vector3 point = center + new Vector3(direction.x * distance, 0f, direction.y * distance);
        return SnapToGround(point, yOffset);
    }

    private Vector3 RandomPointNear(Vector3 center, float radius, float yOffset)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        Vector3 point = center + new Vector3(offset.x, 0f, offset.y);
        return SnapToGround(point, yOffset);
    }

    private Vector3 SnapToGround(Vector3 point, float yOffset)
    {
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideTerrain = point.x >= terrainPosition.x
                && point.x <= terrainPosition.x + terrainSize.x
                && point.z >= terrainPosition.z
                && point.z <= terrainPosition.z + terrainSize.z;

            if (insideTerrain)
                point.y = terrain.SampleHeight(point) + terrainPosition.y + yOffset;
        }

        return point;
    }

    private Vector3 GetCenter()
    {
        return player != null ? player.position : transform.position;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void ResolveRuntimeReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (terrain == null || terrain.terrainData == null)
        {
            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            for (int i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] != null && terrains[i].terrainData != null)
                {
                    terrain = terrains[i];
                    break;
                }
            }
        }
    }

    private static Sprite CreateFallbackSprite(Color color)
    {
        const int width = 32;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        for (int y = 4; y < height - 3; y++)
        {
            for (int x = 8; x < width - 8; x++)
            {
                float normalizedY = (float)y / height;
                int halfWidth = Mathf.RoundToInt(Mathf.Lerp(4f, 10f, normalizedY));
                if (Mathf.Abs(x - width / 2) <= halfWidth)
                    pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = "EcoFallbackSprite";
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), 32f);
    }

    private static void DestroySmart(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
