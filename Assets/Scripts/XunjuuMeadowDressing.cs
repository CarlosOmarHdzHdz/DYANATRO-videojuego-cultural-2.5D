using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Deterministic decorative patches follow exploration. No mission, crop, enemy or pickup is moved.
[DisallowMultipleComponent]
public sealed class XunjuuMeadowDressing : MonoBehaviour
{
    private const float Cell = 20f;
    private const int Radius = 6;
    private readonly Dictionary<Vector2Int,GameObject> chunks=new Dictionary<Vector2Int,GameObject>();
    private readonly HashSet<Vector2Int> detailedChunks=new HashSet<Vector2Int>();
    private readonly List<Bounds> buildings=new List<Bounds>();
    private Transform player;
    private Terrain[] terrains;
    private Sprite flower;
    private Sprite[] floraVariants;
    private Material sky;
    private Vector3 origin;
    public int PatchCount=>chunks.Count;
    public IReadOnlyList<Bounds> Buildings=>buildings;
    public float VegetatedSpan=>(Radius*2+1)*Cell;

    private IEnumerator Start()
    {
        var actor=FindFirstObjectByType<PlayerController>();
        if(actor==null) yield break;
        player=actor.transform;origin=player.position;terrains=Terrain.activeTerrains;
        BuildFloraSprites();
        flower=Resources.Load<Sprite>("Sprites/TextureTerrain/flor");
        if(flower==null)
        {
            Sprite[] sprites=Resources.LoadAll<Sprite>("Sprites/TextureTerrain/flor");
            if(sprites.Length>0)flower=sprites[0];
            else {var texture=Resources.Load<Texture2D>("Sprites/TextureTerrain/flor");if(texture!=null)flower=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,0),100);}
        }
        sky=new Material(Shader.Find("Xunjuu/HighlandSky")){name="Cielo_Sierras_Mazahuas"};
        RenderSettings.skybox=sky;RenderSettings.fog=false;
        Camera camera=Camera.main;
        if(camera!=null){camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=Mathf.Max(camera.farClipPlane,850f);}
        BuildVillage();
        BuildDistantGroves();
        foreach(var label in FindObjectsByType<XunjuuWorldLabel>(FindObjectsSortMode.None))
            if(label.DisplayName=="Protagonista" || label.DisplayName.StartsWith("BOSQUE "))label.Configure(label.DisplayName,false);
        while(player!=null)
        {
            Vector2Int center=CellAt(player.position);
            for(int ring=0;ring<=Radius;ring++)
                for(int x=-ring;x<=ring;x++)for(int z=-ring;z<=ring;z++)
                {
                    if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))!=ring)continue;
                    var key=center+new Vector2Int(x,z);
                    if(chunks.ContainsKey(key))
                    {
                        if(ring>4||detailedChunks.Contains(key))continue;
                        Destroy(chunks[key].GetComponent<MeshFilter>().sharedMesh);Destroy(chunks[key]);chunks.Remove(key);
                    }
                    BuildPatch(key);
                    yield return null;
                }
            var expired=new List<Vector2Int>();
            foreach(var entry in chunks)
                if(Mathf.Max(Mathf.Abs(entry.Key.x-center.x),Mathf.Abs(entry.Key.y-center.y))>Radius+1)expired.Add(entry.Key);
            foreach(var key in expired)
            {
                var filter=chunks[key].GetComponent<MeshFilter>();
                if(filter!=null)Destroy(filter.sharedMesh);
                Destroy(chunks[key]);chunks.Remove(key);detailedChunks.Remove(key);
            }
            yield return new WaitForSeconds(.5f);
        }
    }
    private void LateUpdate()
    {
        if(player!=null)Shader.SetGlobalVector("_XunjuuPlayerPosition",new Vector4(player.position.x,player.position.y+.55f,player.position.z,1));
    }
    private static Vector2Int CellAt(Vector3 p)=>new Vector2Int(Mathf.FloorToInt(p.x/Cell),Mathf.FloorToInt(p.z/Cell));
    private void BuildDistantGroves()
    {
        var random=new System.Random(6908);
        for(int group=0;group<110;group++)
        {
            float angle=group*2.39996f;
            float radius=125f+(float)random.NextDouble()*320f;
            Vector3 center=origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
            for(int i=0;i<4;i++)
            {
                Vector3 p=center+new Vector3((float)random.NextDouble()*14,0,(float)random.NextDouble()*14);
                if(OnPath(p)||!Ground(p,out float y))continue;p.y=y;
                var tree=XunjuuArtMeshes.Create("Arbol"+(group%3),transform,"Arboleda_Lejana");
                tree.transform.position=p;tree.transform.localScale=Vector3.one*(.8f+(float)random.NextDouble()*.5f);
                tree.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                var renderer=tree.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
                var lod=tree.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.018f,new Renderer[]{renderer})});lod.RecalculateBounds();
            }
        }
    }
    private void BuildVillage()
    {
        Vector3[] sites={new Vector3(-17,0,16),new Vector3(23,0,36),new Vector3(-43,0,47),new Vector3(40,0,65),new Vector3(-26,0,87),new Vector3(32,0,-34),new Vector3(-48,0,-26)};
        string[] models={"Casa_Adobe_Detallada","Casa_Corredor_Detallada","Casa_Adobe_Detallada","Casa_Cal_Corredor","Casa_Granero","Casa_Chimenea","Casa_Cal_Corredor"};
        for(int i=0;i<sites.Length;i++)
        {
            Vector3 size=XunjuuArtMeshes.HouseSize(models[i]);
            Vector3 p=origin+sites[i];bool found=false;
            for(int attempt=0;attempt<60;attempt++)
            {
                Vector3 candidate=p+new Vector3(Mathf.Sin(attempt*2.399f),0,Mathf.Cos(attempt*2.399f))*(attempt*.85f);
                if(!Ground(candidate,out float y))continue;
                candidate.y=y;
                if(OnPath(candidate)||InBuilding(candidate))continue;
                Vector3 localSite=candidate-origin;
                if(Mathf.Abs(localSite.x-Mathf.Sin(localSite.z*.018f)*10)<Mathf.Max(size.x,size.z)*.5f+3.5f)continue;
                Vector3 facing=origin-candidate;facing.y=0;
                Quaternion buildingRotation=Quaternion.LookRotation(-facing.normalized,Vector3.up);
                bool uneven=false;
                foreach(var corner in new[]{new Vector3(-size.x*.5f,0,-size.z*.5f),new Vector3(size.x*.5f,0,size.z*.5f),new Vector3(-size.x*.5f,0,size.z*.5f),new Vector3(size.x*.5f,0,-size.z*.5f)})
                    if(!Ground(candidate+buildingRotation*corner,out float height)||Mathf.Abs(height-y)>.8f){uneven=true;break;}
                if(uneven)continue;
                bool blocked=false;
                foreach(Collider hit in Physics.OverlapBox(candidate+Vector3.up*2.5f,new Vector3(size.x*.5f+1.2f,2.4f,size.z*.5f+1.8f),buildingRotation,~0,QueryTriggerInteraction.Collide))
                    if(!(hit is TerrainCollider)){blocked=true;break;}
                if(blocked)continue;
                p=candidate;found=true;break;
            }
            if(!found)continue;
            var house=XunjuuArtMeshes.Create(models[i],transform);
            house.transform.position=p;
            // Front doors face the initial clearing, rather than a random rear wall.
            Vector3 face=origin-p;face.y=0;
            house.transform.rotation=Quaternion.LookRotation(-face.normalized,Vector3.up);
            var collider=house.AddComponent<BoxCollider>();
            collider.center=new Vector3(0,2f,0);collider.size=new Vector3(size.x,4f,size.z);
            Physics.SyncTransforms();
            Bounds footprint=house.GetComponent<Renderer>().bounds;footprint.Expand(new Vector3(2,0,2));buildings.Add(footprint);
            for(int pot=0;pot<4;pot++)
            {
                var shrub=XunjuuArtMeshes.Create("Arbusto",house.transform,"Jardin_Lateral");
                shrub.transform.localPosition=new Vector3(4.9f,0,-2+pot*1.3f);
                Vector3 at=shrub.transform.position;if(Ground(at,out float y))shrub.transform.position=new Vector3(at.x,y,at.z);
            }
        }
    }
    private void BuildPatch(Vector2Int key)
    {
        int seed=unchecked(key.x*73856093 ^ key.y*19349663 ^ 60908);
        var b=new XunjuuArtMeshes.Builder(seed);
        var go=new GameObject("Pradera_"+key.x+"_"+key.y,typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(transform,false);chunks[key]=go;
        for(int segment=0;segment<40;segment++)
        {
            float z=key.y*Cell+segment*.5f;
            float x=origin.x+Mathf.Sin((z-origin.z)*.018f)*10;
            if(x<key.x*Cell || x>=(key.x+1)*Cell)continue;
            float nextX=origin.x+Mathf.Sin((z+.5f-origin.z)*.018f)*10;
            float width=2.05f+Mathf.Sin(z*.23f)*.10f;
            for(int strip=0;strip<8;strip++)
            {
            float left=Mathf.Lerp(-width,width,strip/8f),right=Mathf.Lerp(-width,width,(strip+1)/8f);
            Vector3 a=new Vector3(x+left,0,z),c=new Vector3(x+right,0,z);
            Vector3 d=new Vector3(nextX+right,0,z+.5f),e=new Vector3(nextX+left,0,z+.5f);
            if(Ground(a,out float ay)&&Ground(c,out float cy)&&Ground(d,out float dy)&&Ground(e,out float ey))
            {a.y=ay+.065f;c.y=cy+.065f;d.y=dy+.065f;e.y=ey+.065f;b.Quad(a,e,d,c,new Color(.45f,.40f,.26f)*b.Range(.98f,1.02f));}
            }
        }
        // All positions baked into one chunk mesh; dozens of plants cost one draw call.
        Vector2Int playerCell=CellAt(player.position);
        int ring=Mathf.Max(Mathf.Abs(key.x-playerCell.x),Mathf.Abs(key.y-playerCell.y));
        if(ring<=4)detailedChunks.Add(key);
        for(int cluster=0;cluster<(ring<=4?18:8);cluster++)
        {
            Vector3 center=new Vector3((key.x+b.Range(.05f,.95f))*Cell,0,(key.y+b.Range(.05f,.95f))*Cell);
            if(!Ground(center,out float y)||InBuilding(center)||OnPath(center))continue;
            center.y=y;
            for(int tuft=0;tuft<9;tuft++)
            {
                Vector3 p=center+new Vector3(b.Range(-2.5f,2.5f),0,b.Range(-2.5f,2.5f));
                if(!Ground(p,out float ground)||OnPath(p)||InBuilding(p))continue;p.y=ground+.015f;
                XunjuuArtMeshes.Grass(b,p,b.Range(.8f,1.65f));
                if(tuft==0)XunjuuArtMeshes.Fern(b,p,b.Range(.7f,1.25f));
                if(tuft==1&&cluster%4==0)XunjuuArtMeshes.Bush(b,p,b.Range(.7f,1.1f));
            }
            if(flower!=null && cluster%3==0)
            {
                var plant=new GameObject("Flora_2D_Variante",typeof(SpriteRenderer));plant.transform.SetParent(go.transform,false);
                plant.transform.position=center+Vector3.up*.025f;
                plant.transform.rotation=Quaternion.Euler(35,0,0);
                var sr=plant.GetComponent<SpriteRenderer>();
                int variant=Mathf.Min(7,(int)b.Range(0,8));sr.sprite=floraVariants!=null&&floraVariants[variant]!=null?floraVariants[variant]:flower;
                sr.color=Color.white;
                float height=variant==0||variant==1||variant==7?b.Range(.9f,1.45f):b.Range(.45f,.85f);
                float factor=height/Mathf.Max(.01f,sr.sprite.bounds.size.y);plant.transform.localScale=Vector3.one*factor;
            }
        }
        if(ring<=4)
        for(int prop=0;prop<3;prop++)
        {
            Vector3 point=new Vector3((key.x+b.Range(.12f,.88f))*Cell,0,(key.y+b.Range(.12f,.88f))*Cell);
            if(!Ground(point,out float ground)||InBuilding(point)||OnPath(point))continue;
            string[] types={"Tronco_Caido","Tocon_Raices","Arbusto_Florido","Arbusto_Bayas"};
            string model=types[Mathf.Min(3,(int)b.Range(0,4))];
            Quaternion rotation=Quaternion.Euler(0,b.Range(0,360),0);
            float scale=b.Range(.75f,1.25f);
            point.y=ground;
            bool solid=model=="Tronco_Caido"||model=="Tocon_Raices";
            if(solid && !FitWoodToGround(model,ref point,ref rotation,scale))continue;
            var detail=XunjuuArtMeshes.Create(model,go.transform);
            detail.transform.SetPositionAndRotation(point,rotation);detail.transform.localScale=Vector3.one*scale;
            detail.GetComponent<Renderer>().shadowCastingMode=solid?ShadowCastingMode.On:ShadowCastingMode.Off;
            if(solid)Physics.SyncTransforms();
        }
        var mesh=b.Build(go.name);go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=XunjuuArtMeshes.Material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
    }
    private bool FitWoodToGround(string model,ref Vector3 point,ref Quaternion rotation,float scale)
    {
        // Fit a support plane across the complete footprint, not just its centre.
        float length=(model=="Tronco_Caido"?1.25f:.65f)*scale;
        float width=(model=="Tronco_Caido"?.34f:.65f)*scale;
        Vector3 right=rotation*Vector3.right,forward=rotation*Vector3.forward;
        if(!Ground(point-right*length,out float left)||!Ground(point+right*length,out float end)||
           !Ground(point-forward*width,out float back)||!Ground(point+forward*width,out float front))return false;
        Vector3 along=right*(2*length)+Vector3.up*(end-left);
        Vector3 across=forward*(2*width)+Vector3.up*(front-back);
        Vector3 normal=Vector3.Cross(across,along).normalized;
        if(Vector3.Angle(normal,Vector3.up)>20f)return false;
        rotation=Quaternion.FromToRotation(Vector3.up,normal)*rotation;
        float support=float.NegativeInfinity,lowest=float.PositiveInfinity;
        for(int x=-2;x<=2;x++)for(int z=-1;z<=1;z++)
        {
            Vector3 offset=rotation*new Vector3(x*length*.5f,0,z*width);
            Vector3 sample=point+offset;
            if(OnPath(sample)||InBuilding(sample)||!Ground(sample,out float y))return false;
            float height=y-offset.y;support=Mathf.Max(support,height);lowest=Mathf.Min(lowest,height);
        }
        // Reject uneven ground instead of leaving an end suspended or deeply buried.
        if(support-lowest>.12f)return false;
        point.y=support-.025f*scale;
        foreach(Collider hit in Physics.OverlapBox(point+rotation*Vector3.up*.45f*scale,
            new Vector3(length+.35f*scale,.55f*scale,width+.3f*scale),rotation,~0,QueryTriggerInteraction.Collide))
            if(!(hit is TerrainCollider) && !XunjuuSceneVisualPolicy.IsSoftDecoration(hit.transform))return false;
        return true;
    }
    private bool InBuilding(Vector3 p)
    {
        foreach(Bounds bounds in buildings)if(p.x>=bounds.min.x&&p.x<=bounds.max.x&&p.z>=bounds.min.z&&p.z<=bounds.max.z)return true;
        return false;
    }
    private void BuildFloraSprites()
    {
        var atlas=Resources.Load<Texture2D>("Sprites/TextureTerrain/FloraExpansion_4x2");
        if(atlas==null||!atlas.isReadable)return;
        var pixels=atlas.GetPixels32();floraVariants=new Sprite[8];
        for(int i=0;i<8;i++)
        {
            int left=atlas.width*(i%4)/4,right=atlas.width*(i%4+1)/4;
            int bottom=atlas.height*(1-i/4)/2,top=atlas.height*(2-i/4)/2;
            int minX=right,maxX=left,minY=top,maxY=bottom;
            for(int y=bottom;y<top;y++)for(int x=left;x<right;x++)
                if(pixels[y*atlas.width+x].a>48){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            if(minX>maxX)continue;
            floraVariants[i]=Sprite.Create(atlas,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);
            floraVariants[i].name="FloraExpansion_"+i;
        }
    }
    private bool OnPath(Vector3 p)
    {
        Vector3 relative=p-origin;
        return Mathf.Abs(relative.x-Mathf.Sin(relative.z*.018f)*10)<2.5f || new Vector2(relative.x,relative.z).sqrMagnitude<16f;
    }
    private bool Ground(Vector3 p,out float y)
    {
        foreach(Terrain t in terrains)
        {
            if(t==null||t.terrainData==null)continue;
            Vector3 local=p-t.transform.position,size=t.terrainData.size;
            if(local.x<0||local.z<0||local.x>size.x||local.z>size.z)continue;
            y=t.SampleHeight(p)+t.transform.position.y;
            return t.terrainData.GetSteepness(local.x/size.x,local.z/size.z)<38f;
        }
        y=0;return false;
    }
    private void OnDestroy()
    {
        foreach(var entry in chunks)if(entry.Value!=null){var f=entry.Value.GetComponent<MeshFilter>();if(f!=null)Destroy(f.sharedMesh);}
        if(sky!=null)Destroy(sky);
        if(floraVariants!=null)foreach(var sprite in floraVariants)if(sprite!=null)Destroy(sprite);
        Shader.SetGlobalVector("_XunjuuPlayerPosition",Vector4.zero);
    }
}
