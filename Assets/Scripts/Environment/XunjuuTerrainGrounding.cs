using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Ajuste de actores al terreno
// ACCION: mantener el borde visible de enemigos y jefes a nivel del piso.
// MODIFICACION: Ground Offset y Follow Terrain se editan desde el Inspector.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuTerrainGrounding : MonoBehaviour
{
    [Header("Xunjuu v0.1 - Suelo")]
    [SerializeField] private bool followTerrain = true;
    [SerializeField] private bool disableGravity = true;
    [SerializeField, Range(-0.2f, 0.3f)] private float groundOffset = 0.04f;

    private Rigidbody body;
    private SpriteRenderer spriteRenderer;
    private float visualBottomOffset;
    private bool visualOffsetReady;

    public bool IsGroundedCorrectly(float tolerance = 0.18f)
    {
        if (!TryGetGroundHeight(transform.position, out float groundY))
            return true;

        CacheVisualBottomOffset();
        float visibleBottom = transform.position.y + visualBottomOffset;
        return Mathf.Abs(visibleBottom - (groundY + groundOffset)) <= tolerance;
    }

    private void Awake()
    {
        CacheComponents();
        SnapNow();
    }

    private void OnEnable()
    {
        CacheComponents();
        SnapNow();
    }

    private void FixedUpdate()
    {
        if (followTerrain)
            SnapNow();
    }

    // ACCION: recalcular la altura inmediatamente al activar una fase de mision.
    [ContextMenu("Xunjuu v0.1/Ajustar al terreno")]
    public void SnapNow()
    {
        CacheComponents();
        CacheVisualBottomOffset();
        if (!TryGetGroundHeight(transform.position, out float groundY))
            return;

        Vector3 groundedPosition = transform.position;
        groundedPosition.y = groundY - visualBottomOffset + groundOffset;
        transform.position = groundedPosition;

        if (body != null)
        {
            body.position = groundedPosition;
            if (!body.isKinematic)
                body.linearVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            if (disableGravity)
                body.useGravity = false;
        }
    }

    private void CacheComponents()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void CacheVisualBottomOffset()
    {
        if (visualOffsetReady || spriteRenderer == null)
            return;

        visualBottomOffset = spriteRenderer.bounds.min.y - transform.position.y;
        visualOffsetReady = true;
    }

    private bool TryGetGroundHeight(Vector3 position, out float groundY)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null || !ContainsXZ(terrain, position))
                continue;

            groundY = terrain.SampleHeight(position) + terrain.transform.position.y;
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 80f, Vector3.down, 180f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                continue;
            groundY = hit.point.y;
            return true;
        }

        groundY = position.y;
        return false;
    }

    private static bool ContainsXZ(Terrain terrain, Vector3 point)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        return point.x >= origin.x && point.x <= origin.x + size.x
            && point.z >= origin.z && point.z <= origin.z + size.z;
    }
}
