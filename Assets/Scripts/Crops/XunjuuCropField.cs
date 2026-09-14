using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Milpa editable
// Acción: agrupar cultivos y reservar prefabs para regenerarlos manualmente.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuCropField : MonoBehaviour
{
    [Header("Xunjuú v0.1 - Milpa")]
    [SerializeField] private string fieldDisplayName = "Milpa de maíz";
    [SerializeField] private GameObject cropPrefab;
    [SerializeField] private Terrain terrain;
    [SerializeField, Min(1)] private int rows = 4;
    [SerializeField, Min(1)] private int cropsPerRow = 9;
    [SerializeField, Min(0.25f)] private float rowSpacing = 1.35f;
    [SerializeField, Min(0.25f)] private float cropSpacing = 1.2f;

    public GameObject CropPrefab => cropPrefab;

    // ACCIÓN: registrar una milpa generada para poder modificarla después.
    public void ConfigureExisting(string displayName, int rowCount, int columnCount)
    {
        fieldDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Milpa" : displayName;
        rows = Mathf.Max(1, rowCount);
        cropsPerRow = Mathf.Max(1, columnCount);

        XunjuuWorldLabel label = GetComponent<XunjuuWorldLabel>();
        if (label == null)
            label = gameObject.AddComponent<XunjuuWorldLabel>();
        label.Configure(fieldDisplayName, true, new Vector3(0f, 2.8f, 0f));
    }

    // ACCIÓN: crear cultivos usando el prefab que el diseñador asigne más adelante.
    [ContextMenu("Xunjuú v0.1/Generar cultivos desde prefab")]
    public void GenerateFromPrefab()
    {
        if (cropPrefab == null)
        {
            Debug.LogWarning("Xunjuú v0.1: asigna un Crop Prefab antes de generar la milpa.", this);
            return;
        }

        ClearGeneratedCrops();
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < cropsPerRow; column++)
            {
                Vector3 local = new Vector3(
                    (column - (cropsPerRow - 1) * 0.5f) * cropSpacing,
                    0f,
                    (row - (rows - 1) * 0.5f) * rowSpacing);
                Vector3 world = transform.TransformPoint(local);
                if (terrain != null)
                    world.y = terrain.SampleHeight(world) + terrain.transform.position.y;

                GameObject crop = Instantiate(cropPrefab, world, transform.rotation, transform);
                crop.name = $"Cultivo_{row + 1:00}_{column + 1:00}";
                if (crop.GetComponent<XunjuuCrop>() == null)
                    crop.AddComponent<XunjuuCrop>();
            }
        }
    }

    // ACCIÓN: limpiar solamente los cultivos generados, sin borrar la milpa ni sus surcos.
    [ContextMenu("Xunjuú v0.1/Limpiar cultivos generados")]
    public void ClearGeneratedCrops()
    {
        XunjuuCrop[] crops = GetComponentsInChildren<XunjuuCrop>(true);
        foreach (XunjuuCrop crop in crops)
        {
            if (crop == null || crop.transform == transform)
                continue;

            if (Application.isPlaying)
                Destroy(crop.gameObject);
            else
                DestroyImmediate(crop.gameObject);
        }
    }
}
