using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(1000)]
public sealed class XunjuuEnvironment3D : MonoBehaviour
{
    private sealed class TreeVisual
    {
        public Transform owner, visual;
        public TreeInteractivo interaction;
        public SpriteRenderer[] sprites;
        public Renderer[] renderers;
        public Collider[] oldColliders;
        public Quaternion rotation;
        public float fade = 1f;
        public Bounds bounds;
    }
    private readonly List<TreeVisual> trees = new List<TreeVisual>();
    private readonly HashSet<int> converted = new HashSet<int>();
    private MaterialPropertyBlock block;
    private XunjuuEnvironmentCatalog catalog;
    private Camera view;
    private Transform player;

    private IEnumerator Start()
    {
        block = new MaterialPropertyBlock();
        catalog = Resources.Load<XunjuuEnvironmentCatalog>("EnvironmentCatalog");
        if (catalog == null) yield break;
        yield return null;
        yield return null;
        view = Camera.main;
        var controller = FindFirstObjectByType<PlayerController>();
        player = controller != null ? controller.transform : null;
        ConvertSprites();
        ConfigureLightingAndGround();
        if (GetComponent<XunjuuMeadowDressing>() == null) gameObject.AddComponent<XunjuuMeadowDressing>();
        AddBoundaries();
        while (enabled)
        {
            yield return new WaitForSeconds(2f);
            ConvertSprites();
        }
    }

    private void ConvertSprites()
    {
        foreach (SpriteRenderer sprite in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sprite == null || sprite.GetComponentInParent<PlayerController>() != null) continue;
            TreeInteractivo tree = sprite.GetComponentInParent<TreeInteractivo>();
            string name = (sprite.name + " " + (sprite.sprite != null ? sprite.sprite.name : "")).ToLowerInvariant();
            bool isTree = tree != null || name.Contains("arbol") || name.Contains("tree");
            bool isCorn = name.Contains("maiz") || name.Contains("milpa");
            bool isFlower = sprite.GetComponentInParent<MazahuaWordCollectible>() != null;
            if (!isTree && !isCorn && !isFlower) continue;
            Transform owner = tree != null ? tree.transform : sprite.transform;
            if (!converted.Add(owner.GetInstanceID())) continue;
            GameObject prefab = isTree ? catalog.trees[trees.Count % catalog.trees.Length]
                : isCorn ? catalog.corn : catalog.flower;
            if (prefab == null) continue;
            float height = isTree ? XunjuuWorldScale.TreeHeight(player, Random.value) : isCorn ? 1.7f : 0.5f;
            var old = owner.GetComponentsInChildren<Collider>(true);
            var originalSprites = owner.GetComponentsInChildren<SpriteRenderer>(true);
            GameObject visual = isTree ? XunjuuArtMeshes.Create("Arbol" + (trees.Count % 3), owner) : Instantiate(prefab, owner);
            visual.name = "Visual_3D";
            // Legacy billboard parents can have unequal X/Y/Z scale. Do not
            // inherit that distortion when replacing them with a 3D tree.
            if (isTree) XunjuuWorldScale.SetUniformWorldScale(visual.transform, 1f);
            Bounds bounds = GetBounds(visual);
            float factor = height / Mathf.Max(0.01f, bounds.size.y);
            visual.transform.localScale *= factor;
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            visual.transform.rotation = rotation;
            Vector3 at = owner.position;
            if (TryGround(at, out float ground)) at.y = ground;
            bounds = GetBounds(visual);
            visual.transform.position += new Vector3(at.x - bounds.center.x, at.y - bounds.min.y, at.z - bounds.center.z);
            if (isTree)
            {
                var trunk = visual.AddComponent<CapsuleCollider>();
                trunk.radius = 0.18f; trunk.height = 2.5f; trunk.center = Vector3.up * 1.25f;
            }
            foreach (SpriteRenderer original in originalSprites) original.enabled = false;
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            var lod = visual.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(0.025f, renderers) });
            lod.RecalculateBounds();
            // Keep trigger objectives intact; the tree's 3D trunk replaces its broad sprite collider.
            if (isTree) foreach (Collider collider in old) if (!collider.isTrigger) collider.enabled = false;
            trees.Add(new TreeVisual { owner = owner, visual = visual.transform, interaction = tree,
                sprites = originalSprites, renderers = renderers, oldColliders = isTree ? old : new Collider[0], rotation = rotation,
                bounds = GetBounds(visual) });
        }
    }

    private void LateUpdate()
    {
        if (view == null || player == null || block == null) return;
        Vector3 target = player.position + Vector3.up * 0.5f;
        Vector3 rayDirection = target - view.transform.position;
        Ray ray = new Ray(view.transform.position, rayDirection.normalized);
        foreach (TreeVisual entry in trees)
        {
            if (entry.owner == null || entry.visual == null) continue;
            foreach (SpriteRenderer sprite in entry.sprites) if (sprite != null) sprite.enabled = false;
            foreach (Collider collider in entry.oldColliders) if (collider != null && !collider.isTrigger) collider.enabled = false;
            bool alive = entry.interaction == null || entry.interaction.IsAlive;
            entry.visual.gameObject.SetActive(alive);
            if (!alive) continue;
            entry.visual.rotation = entry.rotation;
            Bounds bounds = entry.bounds;
            bounds.Expand(0.7f);
            bool blocking = bounds.IntersectRay(ray, out float distance) && distance < rayDirection.magnitude;
            float previousFade = entry.fade;
            entry.fade = Mathf.MoveTowards(entry.fade, blocking ? XunjuuSceneVisualPolicy.OccludedVisibility : 1f, Time.unscaledDeltaTime * 4f);
            if (Mathf.Approximately(previousFade, entry.fade)) continue;
            foreach (Renderer renderer in entry.renderers)
            {
                renderer.GetPropertyBlock(block);
                block.SetFloat("_Visibility", entry.fade);
                renderer.SetPropertyBlock(block);
            }
        }
    }

    private void ConfigureLightingAndGround()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.64f, 0.75f, 0.83f);
        RenderSettings.ambientEquatorColor = new Color(0.51f, 0.57f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.33f, 0.23f);
        RenderSettings.fog = false;
        QualitySettings.shadowDistance = 100f;
        QualitySettings.shadows = ShadowQuality.All;
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional)
            {
                light.intensity = 1.16f; light.color = new Color(1f, 0.91f, 0.75f);
                light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                light.shadows = LightShadows.Soft; light.shadowStrength = .68f; light.shadowBias = .035f;
            }
        foreach (MeshRenderer renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            string name = renderer.name.ToLowerInvariant();
            foreach (Material material in renderer.sharedMaterials)
                if (material != null) name += " " + material.name.ToLowerInvariant();
            if (name.Contains("checker") || name.Contains("prototype") || name.Contains("grid"))
                renderer.sharedMaterial = catalog.groundMaterial;
        }
    }

    private void AddVillageProps()
    {
        if (player == null) return;
        var random = new System.Random(20260908);
        for (int i = 0; i < 28; i++)
        {
            float angle = i * 2.39996f;
            float radius = i < 3 ? 26f : 18f + (float)random.NextDouble() * 85f;
            Vector3 at = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (!TryGround(at, out float y)) continue;
            at.y = y;
            bool blocked = false;
            foreach (Collider hit in Physics.OverlapSphere(at + Vector3.up * 2f, i < 3 ? 5f : 1.5f, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit is TerrainCollider)) { blocked = true; break; }
            if (blocked) continue;
            GameObject prefab = i < 3 ? catalog.houses[i % catalog.houses.Length] : catalog.rocks[i % catalog.rocks.Length];
            GameObject instance = Instantiate(prefab, at, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), transform);
            instance.transform.localScale *= 0.8f + (float)random.NextDouble() * 0.5f;
            Bounds bounds = GetBounds(instance);
            instance.transform.position += Vector3.up * (y - bounds.min.y);
        }
    }

    private void AddBoundaries()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains.Length == 0) return;
        Bounds bounds = new Bounds(terrains[0].transform.position, Vector3.zero);
        foreach (Terrain terrain in terrains)
        { bounds.Encapsulate(terrain.transform.position); bounds.Encapsulate(terrain.transform.position + terrain.terrainData.size); }
        for (int side = 0; side < 4; side++)
        {
            bool xSide = side < 2;
            float edge = side % 2 == 0 ? (xSide ? bounds.min.x : bounds.min.z) : (xSide ? bounds.max.x : bounds.max.z);
            GameObject wall = new GameObject("Limite_Terreno_" + side, typeof(BoxCollider));
            wall.transform.SetParent(transform);
            wall.transform.position = xSide ? new Vector3(edge, bounds.center.y, bounds.center.z) : new Vector3(bounds.center.x, bounds.center.y, edge);
            wall.GetComponent<BoxCollider>().size = xSide ? new Vector3(3f, bounds.size.y + 100f, bounds.size.z) : new Vector3(bounds.size.x, bounds.size.y + 100f, 3f);
            // The painted mountain skyline supplies the background; boundary colliders remain intact.
        }
    }

    private static bool TryGround(Vector3 at, out float height)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            Vector3 local = at - terrain.transform.position, size = terrain.terrainData.size;
            if (local.x >= 0 && local.z >= 0 && local.x <= size.x && local.z <= size.z)
            { height = terrain.SampleHeight(at) + terrain.transform.position.y; return true; }
        }
        height = at.y; return false;
    }

    private static Bounds GetBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(root.transform.position, Vector3.one);
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
