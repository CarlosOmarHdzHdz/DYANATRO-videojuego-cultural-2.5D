using System.Collections.Generic;
using UnityEngine;

public class MazahuaMotifVoxel2_5D : MonoBehaviour
{
    [Header("Voxel Layout")]
    [SerializeField, Range(7, 17)] private int radius = 11;
    [SerializeField, Min(0.03f)] private float blockSize = 0.18f;
    [SerializeField, Min(0f)] private float blockGap = 0.015f;
    [SerializeField, Min(0.02f)] private float baseHeight = 0.12f;
    [SerializeField, Min(0.02f)] private float redHeight = 0.22f;
    [SerializeField, Min(0.02f)] private float outlineHeight = 0.26f;

    [Header("Colors")]
    [SerializeField] private Color red = new Color(0.72f, 0.06f, 0.08f);
    [SerializeField] private Color ivory = new Color(0.93f, 0.88f, 0.78f);
    [SerializeField] private Color outline = new Color(0.05f, 0.07f, 0.08f);
    [SerializeField] private Color shadowRed = new Color(0.42f, 0.02f, 0.04f);

    [Header("Game Feel")]
    [SerializeField] private bool addColliders;
    [SerializeField] private bool bobInPlayMode = true;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.035f;
    [SerializeField, Min(0f)] private float bobSpeed = 1.7f;

    private const string GeneratedChildName = "GeneratedVoxel";
    private readonly List<Material> runtimeMaterials = new List<Material>();
    private Vector3 startLocalPosition;

    void Awake()
    {
        startLocalPosition = transform.localPosition;
        Rebuild();
    }

    void OnValidate()
    {
        radius = Mathf.Clamp(radius, 7, 17);
        blockSize = Mathf.Max(0.03f, blockSize);
        blockGap = Mathf.Max(0f, blockGap);
        baseHeight = Mathf.Max(0.02f, baseHeight);
        redHeight = Mathf.Max(0.02f, redHeight);
        outlineHeight = Mathf.Max(0.02f, outlineHeight);
        bobAmplitude = Mathf.Max(0f, bobAmplitude);
        bobSpeed = Mathf.Max(0f, bobSpeed);
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || !bobInPlayMode)
            return;

        float offset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = startLocalPosition + Vector3.up * offset;
    }

    [ContextMenu("Rebuild Motif")]
    public void Rebuild()
    {
        ClearGeneratedChildren();
        ClearRuntimeMaterials();

        Material redMaterial = CreateMaterial(red);
        Material ivoryMaterial = CreateMaterial(ivory);
        Material outlineMaterial = CreateMaterial(outline);
        Material shadowRedMaterial = CreateMaterial(shadowRed);

        float step = blockSize + blockGap;

        for (int z = -radius; z <= radius; z++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                MotifCell cell = GetCell(x, z);
                if (cell == MotifCell.Empty)
                    continue;

                float height = GetHeight(cell);
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = GeneratedChildName;
                cube.transform.SetParent(transform, false);
                cube.transform.localPosition = new Vector3(x * step, height * 0.5f, z * step);
                cube.transform.localScale = new Vector3(blockSize, height, blockSize);

                MeshRenderer meshRenderer = cube.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = GetMaterial(cell, redMaterial, ivoryMaterial, outlineMaterial, shadowRedMaterial);
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                meshRenderer.receiveShadows = true;

                if (!addColliders)
                {
                    Collider collider = cube.GetComponent<Collider>();
                    if (collider != null)
                        DestroyGeneratedObject(collider);
                }
            }
        }
    }

    private MotifCell GetCell(int x, int z)
    {
        int diamondDistance = Mathf.Abs(x) + Mathf.Abs(z);
        if (diamondDistance > radius)
            return MotifCell.Empty;

        if (diamondDistance >= radius - 1)
            return ((x + z + radius) & 1) == 0 ? MotifCell.Red : MotifCell.Ivory;

        if (IsCardinalAccent(x, z))
            return MotifCell.Red;

        if (IsNearPetal(x, z, 1.35f))
            return MotifCell.Red;

        if (IsNearPetal(x, z, 2.15f))
            return MotifCell.Outline;

        if (Mathf.Abs(x) <= 1 && Mathf.Abs(z) <= 1)
            return MotifCell.Ivory;

        if ((Mathf.Abs(x) == 2 && z == 0) || (Mathf.Abs(z) == 2 && x == 0))
            return MotifCell.Outline;

        return MotifCell.Empty;
    }

    private bool IsNearPetal(int x, int z, float halfWidth)
    {
        if (x == 0 && z == 0)
            return false;

        Vector2 point = new Vector2(x, z);
        Vector2[] directions =
        {
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down,
            new Vector2(1f, 1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 direction = directions[i];
            float along = Vector2.Dot(point, direction);
            if (along < 2.2f || along > radius - 3.2f)
                continue;

            float taperedWidth = Mathf.Lerp(halfWidth, halfWidth * 0.45f, Mathf.InverseLerp(2.2f, radius - 3.2f, along));
            float sideDistance = Mathf.Abs(Cross(point, direction));
            if (sideDistance <= taperedWidth)
                return true;
        }

        return false;
    }

    private bool IsCardinalAccent(int x, int z)
    {
        int inner = radius - 4;
        bool verticalStem = Mathf.Abs(x) <= 1 && Mathf.Abs(z) >= inner - 1 && Mathf.Abs(z) <= inner + 1;
        bool horizontalStem = Mathf.Abs(z) <= 1 && Mathf.Abs(x) >= inner - 1 && Mathf.Abs(x) <= inner + 1;
        bool diagonalDot = Mathf.Abs(Mathf.Abs(x) - Mathf.Abs(z)) == 1 && Mathf.Abs(x) + Mathf.Abs(z) == radius - 2;

        return verticalStem || horizontalStem || diagonalDot;
    }

    private float GetHeight(MotifCell cell)
    {
        switch (cell)
        {
            case MotifCell.Red:
                return redHeight;
            case MotifCell.Outline:
                return outlineHeight;
            case MotifCell.ShadowRed:
                return redHeight * 0.85f;
            default:
                return baseHeight;
        }
    }

    private Material GetMaterial(MotifCell cell, Material redMaterial, Material ivoryMaterial, Material outlineMaterial, Material shadowRedMaterial)
    {
        switch (cell)
        {
            case MotifCell.Red:
                return redMaterial;
            case MotifCell.Outline:
                return outlineMaterial;
            case MotifCell.ShadowRed:
                return shadowRedMaterial;
            default:
                return ivoryMaterial;
        }
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = "MotifVoxelMaterial";
        material.color = color;
        runtimeMaterials.Add(material);
        return material;
    }

    private void ClearGeneratedChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == GeneratedChildName)
                DestroyGeneratedObject(child.gameObject);
        }
    }

    private void ClearRuntimeMaterials()
    {
        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            if (runtimeMaterials[i] != null)
                DestroyGeneratedObject(runtimeMaterials[i]);
        }

        runtimeMaterials.Clear();
    }

    private void DestroyGeneratedObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private enum MotifCell
    {
        Empty,
        Ivory,
        Red,
        Outline,
        ShadowRed
    }
}
