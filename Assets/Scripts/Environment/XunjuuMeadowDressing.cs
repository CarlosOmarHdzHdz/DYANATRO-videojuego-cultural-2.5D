using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Deterministic decorative patches follow exploration. No mission, crop, enemy or pickup is moved.
[DisallowMultipleComponent]
public sealed class XunjuuMeadowDressing : MonoBehaviour
{
    private const float Cell = 20f;
    private const int Radius = 4;
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
    public bool VillageReady { get; private set; }
    public float VegetatedSpan=>(Radius*2+1)*Cell;

    private IEnumerator Start()
    {
        var actor=FindFirstObjectByType<PlayerController>();
        if(actor==null) yield break;
        player=actor.transform;origin=player.position;terrains=Terrain.activeTerrains;
        XunjuuSettlementPlan.Initialize(origin);
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
        if(camera!=null){camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=Mathf.Max(camera.farClipPlane,1600f);}
        BuildVillage();
        BuildMainPath();
        VillageReady = true;
        BuildMountainBackdrop();
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
                        ReleasePatch(chunks[key]);chunks.Remove(key);
                    }
                    BuildPatch(key);
                    yield return null;
                }
            var expired=new List<Vector2Int>();
            foreach(var entry in chunks)
                if(Mathf.Max(Mathf.Abs(entry.Key.x-center.x),Mathf.Abs(entry.Key.y-center.y))>Radius+1)expired.Add(entry.Key);
            foreach(var key in expired)
            {
                ReleasePatch(chunks[key]);chunks.Remove(key);detailedChunks.Remove(key);
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
                if(XunjuuSettlementPlan.Reserved(p,4f)||!Ground(p,out float y))continue;p.y=y;
                int form=XunjuuRegionalTree.SelectForm(p);
                var tree=XunjuuArtMeshes.Create("Arbol"+form,transform,"Arboleda_Lejana");
                tree.transform.position=p;
                float treeHeight=XunjuuWorldScale.TreeHeight(player,(float)random.NextDouble());
                float meshHeight=tree.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
                XunjuuWorldScale.SetUniformWorldScale(tree.transform,treeHeight/Mathf.Max(.01f,meshHeight));
                tree.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                tree.transform.position+=Vector3.up*(y-tree.GetComponent<Renderer>().bounds.min.y);
                var renderer=tree.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
                tree.AddComponent<XunjuuRegionalTree>().Configure(form,renderer);
                var lod=tree.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.018f,new Renderer[]{renderer})});lod.RecalculateBounds();
            }
        }
    }
    private void BuildMountainBackdrop()
    {
        var random=new System.Random(15431);
        const int mountainCount=28;
        for(int index=0;index<mountainCount;index++)
        {
            float angle=index*Mathf.PI*2f/mountainCount+(float)random.NextDouble()*.08f;
            int layer=index%2;
            float radius=layer==0?520f:760f;
            Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            Vector3 point=origin+direction*radius;
            point.y=origin.y-55f;
            var mountain=XunjuuArtMeshes.Create("SierraSuave"+layer,transform,"Sierra_Lejana_"+(index+1).ToString("00"));
            mountain.transform.position=point;
            mountain.transform.rotation=Quaternion.LookRotation(-direction,Vector3.up);
            mountain.transform.localScale=new Vector3(layer==0?1f:1.48f,(layer==0?.92f:1.25f)+(float)random.NextDouble()*.07f,1f);
            var renderer=mountain.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
    private void BuildVillage()
    {
        Bounds garden=XunjuuSettlementPlan.HerbGarden;
        Vector3 gardenGate=garden.center+Vector3.right*(garden.extents.x+1f);
        var gardenAccess=XunjuuSettlementPlan.CreateAccess(gardenGate,XunjuuSettlementPlan.MainPathAt(gardenGate.z),1.05f,100);
        XunjuuSettlementPlan.Accesses.Add(gardenAccess);
        BuildAccessPath(transform,gardenAccess,100);
        Vector3[] sites={new Vector3(-21,0,19),new Vector3(30,0,42),new Vector3(-45,0,62),new Vector3(55,0,94),new Vector3(-28,0,120),new Vector3(34,0,-41),new Vector3(-64,0,-37),new Vector3(86,0,14),new Vector3(-92,0,26),new Vector3(72,0,-85),new Vector3(-84,0,102),new Vector3(21,0,-117),
            new Vector3(-45,0,157),new Vector3(50,0,157),new Vector3(103,0,130),new Vector3(-108,0,-95),new Vector3(64,0,-151),new Vector3(-38,0,-153),new Vector3(30,0,194),new Vector3(-51,0,198),new Vector3(112,0,-32),new Vector3(-111,0,152),new Vector3(-65,0,-196),new Vector3(34,0,-194)};
        string[] models={"Casa_Adobe_Detallada","Casa_Corredor_Detallada","Casa_Adobe_Detallada","Casa_Cal_Corredor","Casa_Granero","Casa_Chimenea","Casa_Cal_Corredor","Casa_Chimenea","Casa_Corredor_Detallada","Casa_Granero","Casa_Adobe_Detallada","Casa_Cal_Corredor"};
        for(int i=0;i<sites.Length;i++)
        {
            float houseScale=XunjuuWorldScale.HouseFactor(player);
            string model = models[i % models.Length];
            Vector3 localSize=XunjuuArtMeshes.HouseSize(model);
            Vector3 size=localSize*houseScale;
            Bounds modelBounds=XunjuuArtMeshes.Get(model).bounds;
            Vector3 p=origin+sites[i];bool found=false;
            for(int attempt=0;attempt<96;attempt++)
            {
                Vector3 candidate=p+new Vector3(Mathf.Sin(attempt*2.399f),0,Mathf.Cos(attempt*2.399f))*(attempt*.85f);
                if(!Ground(candidate,out float y))continue;
                candidate.y=y;
                if(OnPath(candidate)||InBuilding(candidate))continue;
                Vector3 localSite=candidate-origin;
                if(Mathf.Abs(candidate.x-XunjuuSettlementPlan.MainPathAt(candidate.z).x)<Mathf.Max(size.x,size.z)*.5f+4f)continue;
                Vector3 facing=XunjuuSettlementPlan.MainPathAt(candidate.z)-candidate;facing.y=0;
                Quaternion buildingRotation=Quaternion.LookRotation(-facing.normalized,Vector3.up);
                Bounds reserved = new Bounds(candidate + buildingRotation * (modelBounds.center * houseScale), Vector3.zero);
                foreach (var corner in new[]{new Vector3(-1,-1,-1),new Vector3(-1,-1,1),new Vector3(-1,1,-1),new Vector3(-1,1,1),new Vector3(1,-1,-1),new Vector3(1,-1,1),new Vector3(1,1,-1),new Vector3(1,1,1)})
                    reserved.Encapsulate(candidate + buildingRotation * ((modelBounds.center + Vector3.Scale(modelBounds.extents,corner)) * houseScale));
                if (!XunjuuSettlementPlan.CanPlaceField(reserved)) continue;
                bool uneven=false;
                foreach(var corner in new[]{new Vector3(-size.x*.5f,0,-size.z*.5f),new Vector3(size.x*.5f,0,size.z*.5f),new Vector3(-size.x*.5f,0,size.z*.5f),new Vector3(size.x*.5f,0,-size.z*.5f)})
                    if(!Ground(candidate+buildingRotation*corner,out float height)||Mathf.Abs(height-y)>.8f){uneven=true;break;}
                if(uneven)continue;
                bool blocked=false;
                // Reserve the full scaled roof/porch footprint, not the old
                // small wall box, before placing a larger building.
                foreach(Collider hit in Physics.OverlapBox(candidate+buildingRotation*(modelBounds.center*houseScale),modelBounds.extents*houseScale+new Vector3(.8f,.1f,.8f),buildingRotation,~0,QueryTriggerInteraction.Collide))
                    if(!(hit is TerrainCollider) && !XunjuuSceneVisualPolicy.IsSoftDecoration(hit.transform)){blocked=true;break;}
                if(blocked)continue;
                Vector3 door = candidate + buildingRotation * (new Vector3(-1.35f,0,-localSize.z*.5f-(model.Contains("Corredor")?2.3f:.7f))*houseScale);
                Vector3 junction = XunjuuSettlementPlan.MainPathAt(door.z);
                var candidateAccess = XunjuuSettlementPlan.CreateAccess(door,junction,1.05f,i);
                foreach (Vector3 point in candidateAccess.points)
                {
                    foreach (Bounds existing in buildings) if(XunjuuSettlementPlan.ContainsXZ(existing,point,1.2f)){blocked=true;break;}
                    if (XunjuuSettlementPlan.ContainsXZ(XunjuuSettlementPlan.HerbGarden,point,1.2f)) blocked=true;
                    foreach(Collider hit in Physics.OverlapSphere(point+Vector3.up*.5f,.8f,~0,QueryTriggerInteraction.Ignore))
                        if(!(hit is TerrainCollider) && !XunjuuSceneVisualPolicy.IsSoftDecoration(hit.transform)){blocked=true;break;}
                    if (blocked) break;
                }
                if(blocked)continue;
                p=candidate;found=true;break;
            }
            if(!found)continue;
            var house=XunjuuArtMeshes.Create(model,transform);
            XunjuuWorldScale.SetUniformWorldScale(house.transform,houseScale);
            house.transform.position=p;
            // Front doors face the initial clearing, rather than a random rear wall.
            Vector3 face=XunjuuSettlementPlan.MainPathAt(p.z)-p;face.y=0;
            house.transform.rotation=Quaternion.LookRotation(-face.normalized,Vector3.up);
            var collider=house.AddComponent<BoxCollider>();
            collider.center=new Vector3(0,localSize.y*.5f,0);collider.size=localSize;
            Physics.SyncTransforms();
            Bounds footprint=house.GetComponent<Renderer>().bounds;footprint.Expand(new Vector3(2,0,2));buildings.Add(footprint);
            XunjuuSettlementPlan.Houses.Add(footprint);
            Vector3 doorstep = p + house.transform.rotation * (new Vector3(-1.35f,0,-localSize.z*.5f-(model.Contains("Corredor")?2.3f:.7f))*houseScale);
            var access = XunjuuSettlementPlan.CreateAccess(doorstep,XunjuuSettlementPlan.MainPathAt(doorstep.z),1.05f,i);
            XunjuuSettlementPlan.Accesses.Add(access);
            BuildAccessPath(house.transform.parent, access, i);
            for(int pot=0;pot<4;pot++)
            {
                var shrub=XunjuuArtMeshes.Create("Arbusto",house.transform,"Jardin_Lateral");
                shrub.transform.localPosition=new Vector3(localSize.x*.5f+.7f,0,-2+pot*1.3f);
                XunjuuWorldScale.SetUniformWorldScale(shrub.transform,1f);
                Vector3 at=shrub.transform.position;if(Ground(at,out float y))shrub.transform.position=new Vector3(at.x,y,at.z);
            }
        }
    }

    private void BuildAccessPath(Transform parent, XunjuuSettlementPlan.Access access, int index)
    {
        var builder = new XunjuuArtMeshes.Builder(3071+index);
        Vector3[] points=access.points;
        if(points==null)points=new[]{access.door,access.junction};
        int steps=points.Length-1;
        for(int i=0;i<steps;i++)
        {
            Vector3 a=points[i],b=points[i+1];
            Vector3 tangentA=points[Mathf.Min(i+1,steps)]-points[Mathf.Max(i-1,0)];
            Vector3 tangentB=points[Mathf.Min(i+2,steps)]-points[i];
            Vector3 sideA=Vector3.Cross(Vector3.up,tangentA.normalized)*access.width*(1f+.10f*Mathf.Sin(i*.67f+index));
            Vector3 sideB=Vector3.Cross(Vector3.up,tangentB.normalized)*access.width*(1f+.10f*Mathf.Sin((i+1)*.67f+index));
            for(int strip=0;strip<6;strip++)
            {
                float l=Mathf.Lerp(-1,1,strip/6f),r=Mathf.Lerp(-1,1,(strip+1)/6f);
                Vector3 al=a+sideA*l,ar=a+sideA*r,bl=b+sideB*l,br=b+sideB*r;
                if(!Ground(al,out float ay)||!Ground(ar,out float ry)||!Ground(bl,out float by)||!Ground(br,out float dy))continue;
                al.y=ay+.09f;ar.y=ry+.09f;bl.y=by+.09f;br.y=dy+.09f;
                builder.Quad(al,bl,br,ar,Color.white);
            }
        }
        var go=new GameObject("Acceso_Casa_"+(index+1),typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        Mesh mesh=builder.Build(go.name); accessMeshes.Add(mesh); go.GetComponent<MeshFilter>().sharedMesh=mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial=GetPathMaterial();
        go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
    }

    private void BuildMainPath()
    {
        var builder=new XunjuuArtMeshes.Builder(4109);
        for(float z=origin.z-XunjuuSettlementPlan.VillagePathExtent;z<origin.z+XunjuuSettlementPlan.VillagePathExtent;z+=.5f)
        {
            Vector3 a=XunjuuSettlementPlan.MainPathAt(z),b=XunjuuSettlementPlan.MainPathAt(z+.5f);
            float width=XunjuuSettlementPlan.MainPathHalfWidth(z),nextWidth=XunjuuSettlementPlan.MainPathHalfWidth(z+.5f);
            for(int strip=0;strip<12;strip++)
            {
                float l=Mathf.Lerp(-1f,1f,strip/12f),r=Mathf.Lerp(-1f,1f,(strip+1)/12f);
                Vector3 sideA=Vector3.Cross(Vector3.up,(XunjuuSettlementPlan.MainPathAt(z+.25f)-XunjuuSettlementPlan.MainPathAt(z-.25f)).normalized)*width;
                Vector3 sideB=Vector3.Cross(Vector3.up,(XunjuuSettlementPlan.MainPathAt(z+.75f)-XunjuuSettlementPlan.MainPathAt(z+.25f)).normalized)*nextWidth;
                Vector3 al=a+sideA*l,ar=a+sideA*r,bl=b+sideB*l,br=b+sideB*r;
                if(!Ground(al,out float ay)||!Ground(ar,out float ry)||!Ground(bl,out float by)||!Ground(br,out float dy))continue;
                al.y=ay+.09f;ar.y=ry+.09f;bl.y=by+.09f;br.y=dy+.09f;
                builder.Quad(al,bl,br,ar,Color.white);
            }
        }
        var go=new GameObject("Camino_Principal_Aldea",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        Mesh mesh=builder.Build(go.name);accessMeshes.Add(mesh);go.GetComponent<MeshFilter>().sharedMesh=mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial=GetPathMaterial();go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
    }

    private Material pathMaterial;
    private readonly List<Mesh> accessMeshes=new List<Mesh>();
    private Material GetPathMaterial()
    {
        if(pathMaterial==null)pathMaterial=new Material(Shader.Find("Xunjuu/FootpathSoil")){name="Tierra_Compactada_Grava"};
        return pathMaterial;
    }

    private void ReleasePatch(GameObject patch)
    {
        if(patch==null)return;
        foreach(var filter in patch.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null)Destroy(filter.sharedMesh);
        Destroy(patch);
    }
    private void BuildPatch(Vector2Int key)
    {
        int seed=unchecked(key.x*73856093 ^ key.y*19349663 ^ 60908);
        var b=new XunjuuArtMeshes.Builder(seed);
        var road=new XunjuuArtMeshes.Builder(seed);
        var go=new GameObject("Pradera_"+key.x+"_"+key.y,typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(transform,false);chunks[key]=go;
        for(int segment=0;segment<40;segment++)
        {
            float z=key.y*Cell+segment*.5f;
            if(Mathf.Abs(z-origin.z)<XunjuuSettlementPlan.VillagePathExtent)continue;
            float x=XunjuuSettlementPlan.MainPathAt(z).x;
            if(x+2.5f<key.x*Cell || x-2.5f>=(key.x+1)*Cell)continue;
            float nextX=XunjuuSettlementPlan.MainPathAt(z+.5f).x;
            float width=XunjuuSettlementPlan.MainPathHalfWidth(z);
            float nextWidth=XunjuuSettlementPlan.MainPathHalfWidth(z+.5f);
            for(int strip=0;strip<8;strip++)
            {
            float left=Mathf.Lerp(-width,width,strip/8f),right=Mathf.Lerp(-width,width,(strip+1)/8f);
            float min=key.x*Cell,max=(key.x+1)*Cell;
            float al=Mathf.Clamp(x+left,min,max),ar=Mathf.Clamp(x+right,min,max);
            float dl=Mathf.Clamp(nextX+Mathf.Lerp(-nextWidth,nextWidth,strip/8f),min,max);
            float dr=Mathf.Clamp(nextX+Mathf.Lerp(-nextWidth,nextWidth,(strip+1)/8f),min,max);
            if(ar-al<.001f && dr-dl<.001f)continue;
            Vector3 a=new Vector3(al,0,z),c=new Vector3(ar,0,z);
            Vector3 d=new Vector3(dr,0,z+.5f),e=new Vector3(dl,0,z+.5f);
            if(Ground(a,out float ay)&&Ground(c,out float cy)&&Ground(d,out float dy)&&Ground(e,out float ey))
            {
                a.y=ay+.035f;c.y=cy+.035f;d.y=dy+.035f;e.y=ey+.035f;
                float grain=Mathf.PerlinNoise((x+left)*2.4f,z*2.4f);
                Color soil=Color.Lerp(new Color(.55f,.43f,.27f),new Color(.61f,.49f,.32f),grain);
                if(strip==0||strip==7)soil=Color.Lerp(soil,new Color(.36f,.40f,.21f),.48f);
                if(strip==2||strip==5)soil*=.96f;
                road.Quad(a,e,d,c,Color.white);
                // Low flat stones add irregular detail without solid obstacles.
                if(grain>.59f && ar-al>.1f)
                {
                    Vector3 at=(a+c+d+e)*.25f;
                    b.Ellipsoid(at,new Vector3(.065f,.018f,.095f),new Color(.55f,.51f,.39f),5,2);
                }
            }
            }
        }
        // All positions baked into one chunk mesh; dozens of plants cost one draw call.
        Vector2Int playerCell=CellAt(player.position);
        int ring=Mathf.Max(Mathf.Abs(key.x-playerCell.x),Mathf.Abs(key.y-playerCell.y));
        if(ring<=4)detailedChunks.Add(key);
        for(int cluster=0;cluster<(ring<=4?18:8);cluster++)
        {
            Vector3 center=new Vector3((key.x+b.Range(.05f,.95f))*Cell,0,(key.y+b.Range(.05f,.95f))*Cell);
            if(!Ground(center,out float y)||XunjuuSettlementPlan.Reserved(center,1f))continue;
            center.y=y;
            for(int tuft=0;tuft<9;tuft++)
            {
                Vector3 p=center+new Vector3(b.Range(-2.5f,2.5f),0,b.Range(-2.5f,2.5f));
                if(!Ground(p,out float ground)||XunjuuSettlementPlan.Reserved(p,.6f))continue;p.y=ground+.015f;
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
                // Cobertura generica de bosque templado; no adjudicar especies
                // locales a flores o bayas cuya identidad no esta documentada.
                int[] cover={0,3,6};int variant=cover[Mathf.Min(2,(int)b.Range(0,3))];sr.sprite=floraVariants!=null&&floraVariants[variant]!=null?floraVariants[variant]:flower;
                sr.color=Color.white;
                float height=variant==0||variant==1||variant==7?b.Range(.9f,1.45f):b.Range(.45f,.85f);
                float factor=height/Mathf.Max(.01f,sr.sprite.bounds.size.y);plant.transform.localScale=Vector3.one*factor;
            }
        }
        if(ring<=4)
        for(int prop=0;prop<3;prop++)
        {
            Vector3 point=new Vector3((key.x+b.Range(.12f,.88f))*Cell,0,(key.y+b.Range(.12f,.88f))*Cell);
            if(!Ground(point,out float ground)||XunjuuSettlementPlan.Reserved(point,2f))continue;
            string[] types={"Tronco_Caido","Tocon_Raices","Arbusto","Arbusto"};
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
        var roadObject=new GameObject("Camino_Texturizado",typeof(MeshFilter),typeof(MeshRenderer));roadObject.transform.SetParent(go.transform,false);
        roadObject.GetComponent<MeshFilter>().sharedMesh=road.Build("Tierra_"+key);
        roadObject.GetComponent<MeshRenderer>().sharedMaterial=GetPathMaterial();
        roadObject.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
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
        return XunjuuSettlementPlan.OnPath(p) || new Vector2(p.x-origin.x,p.z-origin.z).sqrMagnitude<16f;
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
        foreach(var entry in chunks)ReleasePatch(entry.Value);
        foreach(var mesh in accessMeshes)if(mesh!=null)Destroy(mesh);
        if(sky!=null)Destroy(sky);
        if(pathMaterial!=null)Destroy(pathMaterial);
        if(floraVariants!=null)foreach(var sprite in floraVariants)if(sprite!=null)Destroy(sprite);
        Shader.SetGlobalVector("_XunjuuPlayerPosition",Vector4.zero);
    }
}
