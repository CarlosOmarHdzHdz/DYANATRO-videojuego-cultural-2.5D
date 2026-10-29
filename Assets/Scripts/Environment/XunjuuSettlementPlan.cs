using System.Collections.Generic;
using UnityEngine;

// Reservas compartidas: casas, accesos, cultivos y huerto nunca se superponen.
public static class XunjuuSettlementPlan
{
    public struct Access { public Vector3 door, junction; public float width; public Vector3[] points; }
    public const float VillagePathExtent = 230f;
    private static bool initialized;
    public static Vector3 Origin { get; private set; }
    public static readonly List<Bounds> Houses = new List<Bounds>();
    public static readonly List<Bounds> Fields = new List<Bounds>();
    public static readonly List<Access> Accesses = new List<Access>();
    public static Bounds HerbGarden => new Bounds(Origin + new Vector3(-34, 0, -26), new Vector3(26, 1000, 22));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { initialized = false; Houses.Clear(); Fields.Clear(); Accesses.Clear(); }
    public static void Initialize(Vector3 origin)
    {
        if (initialized) return;
        Origin = origin; initialized = true;
    }
    public static Vector3 MainPathAt(float z)
    {
        float d = z - Origin.z;
        float bend = Mathf.Sin(d * .033f) * 17f + Mathf.Sin(d * .081f) * 5f
            + (Mathf.Sin(d * .157f + .6f) - Mathf.Sin(.6f)) * 1.5f;
        return new Vector3(Origin.x + bend, Origin.y, z);
    }
    public static float MainPathHalfWidth(float z)
        => 2.15f + Mathf.Sin((z - Origin.z) * .093f) * .30f + Mathf.Sin(z * .29f) * .13f;

    public static Access CreateAccess(Vector3 door, Vector3 junction, float width, int seed)
    {
        Vector3 direction = junction - door; direction.y = 0f;
        float length = direction.magnitude;
        Vector3 side = Vector3.Cross(direction.normalized, Vector3.up);
        float bend = Mathf.Min(15f, length * .28f) * (seed % 2 == 0 ? 1f : -1f);
        Vector3 first = door + direction * .28f + side * bend;
        Vector3 second = door + direction * .72f + side * bend * .55f;
        int count = Mathf.Max(8, Mathf.CeilToInt(length / 1.2f));
        var points = new Vector3[count + 1];
        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count, u = 1f - t;
            points[i] = u*u*u*door + 3f*u*u*t*first + 3f*u*t*t*second + t*t*t*junction;
        }
        points[0] = door; points[count] = junction;
        return new Access { door = door, junction = junction, width = width, points = points };
    }

    public static float AccessDistance(Vector3 p, Access access)
    {
        if (access.points == null || access.points.Length < 2) return SegmentDistance(p, access.door, access.junction);
        float distance = float.PositiveInfinity;
        for (int i = 1; i < access.points.Length; i++)
            distance = Mathf.Min(distance, SegmentDistance(p, access.points[i-1], access.points[i]));
        return distance;
    }
    public static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        p.y = a.y = b.y = 0;
        Vector3 delta = b - a;
        float t = delta.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector3.Dot(p - a, delta) / delta.sqrMagnitude) : 0;
        return Vector3.Distance(p, a + delta * t);
    }
    public static bool OnPath(Vector3 p, float margin = 0)
    {
        if (Mathf.Abs(p.x - MainPathAt(p.z).x) < MainPathHalfWidth(p.z) + .8f + margin) return true;
        foreach (Access access in Accesses)
            if (AccessDistance(p, access) < access.width * 1.2f + .25f + margin) return true;
        return false;
    }
    public static bool ContainsXZ(Bounds b, Vector3 p, float margin = 0)
        => p.x >= b.min.x - margin && p.x <= b.max.x + margin && p.z >= b.min.z - margin && p.z <= b.max.z + margin;
    public static bool OverlapXZ(Bounds a, Bounds b, float margin = 0)
        => a.min.x - margin < b.max.x && a.max.x + margin > b.min.x && a.min.z - margin < b.max.z && a.max.z + margin > b.min.z;
    public static bool Reserved(Vector3 p, float margin = 0, bool includeGarden = true)
    {
        if (OnPath(p, margin) || (includeGarden && ContainsXZ(HerbGarden, p, margin))) return true;
        foreach (Bounds b in Houses) if (ContainsXZ(b, p, margin)) return true;
        foreach (Bounds b in Fields) if (ContainsXZ(b, p, margin)) return true;
        return false;
    }
    public static bool CanPlaceField(Bounds footprint)
    {
        if (OverlapXZ(footprint, HerbGarden, 2f)) return false;
        foreach (Bounds b in Houses) if (OverlapXZ(footprint, b, 3f)) return false;
        foreach (Bounds b in Fields) if (OverlapXZ(footprint, b, 5f)) return false;
        for (float x = footprint.min.x; x <= footprint.max.x; x += 1f)
            for (float z = footprint.min.z; z <= footprint.max.z; z += 1f)
                if (OnPath(new Vector3(x, 0, z), 1f)) return false;
        return true;
    }
    public static void ClearGroundDecoration(Bounds footprint)
    {
        foreach(var sprite in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if(!ContainsXZ(footprint,sprite.transform.position,.6f))continue;
            if(sprite.GetComponentInParent<XunjuuCrop>()!=null || sprite.GetComponentInParent<MazahuaWordCollectible>()!=null
                || sprite.GetComponentInParent<XunjuuMedicinalPlant>()!=null || XunjuuSceneVisualPolicy.IsActor(sprite.transform))continue;
            if(XunjuuSceneVisualPolicy.IsSoftDecoration(sprite.transform))sprite.gameObject.SetActive(false);
        }
    }
    public static bool TryGround(Vector3 p, out float height)
    {
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t == null || t.terrainData == null) continue;
            Vector3 local = p - t.transform.position, size = t.terrainData.size;
            if (local.x < 1 || local.z < 1 || local.x > size.x - 1 || local.z > size.z - 1) continue;
            if (t.terrainData.GetSteepness(local.x / size.x, local.z / size.z) > 25f) continue;
            int resolution = t.terrainData.holesResolution;
            if (t.terrainData.IsHole(Mathf.Clamp((int)(local.x / size.x * resolution), 0, resolution - 1),
                Mathf.Clamp((int)(local.z / size.z * resolution), 0, resolution - 1))) continue;
            height = t.SampleHeight(p) + t.transform.position.y; return true;
        }
        height = 0; return false;
    }
}
