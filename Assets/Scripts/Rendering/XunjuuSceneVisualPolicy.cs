using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// One policy for decorative vegetation. Trigger callbacks/damage remain available;
// terrain, houses, trunks and actors retain their physical collision.
public sealed class XunjuuSceneVisualPolicy : MonoBehaviour
{
    public const float OccludedVisibility=.24f;
    private readonly HashSet<int> visited=new HashSet<int>();
    private readonly Dictionary<Material,Material> foliageMaterials=new Dictionary<Material,Material>();
    private Material spriteMaterial;
    private Material actorMaterial;
    public int SoftColliderCount {get;private set;}

    public static bool IsActor(Transform t)
    {
        return t.GetComponentInParent<PlayerController>()!=null || t.GetComponentInParent<Animal>()!=null ||
            t.GetComponentInParent<EnemyHealth>()!=null || t.GetComponentInParent<EnemyFireBreath>()!=null || t.GetComponentInParent<XunjuuBossNivel1>()!=null;
    }
    public static bool IsSoftDecoration(Transform t)
    {
        if(IsActor(t) || t.GetComponentInParent<TreeInteractivo>()!=null)return false;
        if(t.GetComponentInParent<MazahuaWordCollectible>()!=null)return true;
        for(Transform p=t;p!=null;p=p.parent)
        {
            string n=p.name.ToLowerInvariant();
            if(n.Contains("tronco") || n.Contains("tocon") || n.Contains("casa") || n.Contains("house") || n.Contains("arbol") || n.Contains("tree"))return false;
            if(n.Contains("pasto") || n.Contains("grass") || n.Contains("flor") || n.Contains("flower") || n.Contains("arbusto") || n.Contains("bush") || n.Contains("shrub") || n.Contains("fern") || n.Contains("helecho") || n.Contains("maiz") || n.Contains("milpa") || n.Contains("meadow") || n.StartsWith("pradera_"))return true;
        }
        return false;
    }
    private IEnumerator Start()
    {
        spriteMaterial=new Material(Shader.Find("Xunjuu/AtlasSprite")){name="Flora_Alpha_Uniforme",renderQueue=2900};
        spriteMaterial.SetFloat("_KeyMode",2);
        actorMaterial=new Material(spriteMaterial){name="Fauna_Alpha_Uniforme",renderQueue=3000};
        while(enabled){Refresh();yield return new WaitForSecondsRealtime(1f);}
    }
    public void Refresh()
    {
        foreach(Collider c in FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(!(c is TerrainCollider) && IsSoftDecoration(c.transform) && !c.isTrigger)
            {if(c is MeshCollider mesh && !mesh.convex)c.enabled=false;else c.isTrigger=true;SoftColliderCount++;}
        foreach(Renderer r in FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(visited.Contains(r.GetInstanceID()) || r is TrailRenderer || r.GetComponent<TextMesh>()!=null)continue;
            if(r is SpriteRenderer actor && IsActor(r.transform) && r.GetComponentInParent<PlayerController>()==null)
            {
                visited.Add(r.GetInstanceID());actor.sharedMaterial=actorMaterial;Color c=actor.color;c.a=1;actor.color=c;
                var sorting=actor.GetComponent<DyanatroSpriteDepthSorter>()??actor.gameObject.AddComponent<DyanatroSpriteDepthSorter>();sorting.Configure(12000,10);continue;
            }
            if(!IsSoftDecoration(r.transform))continue;
            visited.Add(r.GetInstanceID());
            if(r is SpriteRenderer sr)
            {
                sr.sharedMaterial=spriteMaterial;
                Color tint=sr.color;tint.a=1;sr.color=tint;
                var sorter=sr.GetComponent<DyanatroSpriteDepthSorter>()??sr.gameObject.AddComponent<DyanatroSpriteDepthSorter>();
                sorter.Configure(-12000,10);
            }
            else
            {
                var original=r.sharedMaterial;
                if(original==null)continue;
                if(!foliageMaterials.TryGetValue(original,out Material material))
                {
                    material=new Material(original){name=original.name+"_SoftFlora",renderQueue=2450};
                    if(material.HasProperty("_ZWrite"))material.SetFloat("_ZWrite",0);
                    else if(material.HasProperty("_Surface")){material.SetFloat("_Surface",1);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");}
                    if(material.HasProperty("_Visibility"))material.SetFloat("_Visibility",1);
                    foliageMaterials.Add(original,material);
                }
                r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
    }
    private void OnDestroy()
    {if(spriteMaterial!=null)Destroy(spriteMaterial);if(actorMaterial!=null)Destroy(actorMaterial);foreach(var m in foliageMaterials.Values)if(m!=null)Destroy(m);}
}
