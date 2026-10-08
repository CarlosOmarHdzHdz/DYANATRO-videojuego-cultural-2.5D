using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Shared, vertex-coloured models: a single mesh/material per model, no primitive GameObject forests.
public static class XunjuuArtMeshes
{
    private static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
    private static Material material;
    private static Material ridgeMaterial;
    public static Material Material
    {
        get
        {
            if (material == null) material = new Material(Shader.Find("Xunjuu/PaintedWorld"))
                { name = "Pintura_Mate_Compartida", enableInstancing = true };
            return material;
        }
    }
    public static GameObject Create(string model, Transform parent, string name = null)
    {
        var go = new GameObject(name ?? model, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = Get(model);
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = Material;
        if(model.StartsWith("SierraSuave"))
        {
            if(ridgeMaterial==null)ridgeMaterial=new Material(Shader.Find("Xunjuu/DistantRidge")){name="Sierras_Pintadas"};
            renderer.sharedMaterial=ridgeMaterial;
        }
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        if(model=="Tronco_Caido" || model=="Tocon_Raices")
        {
            var collider=go.AddComponent<MeshCollider>();
            collider.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
            collider.convex=true;collider.isTrigger=false;
        }
        return go;
    }
    public static Mesh Get(string model)
    {
        if (cache.TryGetValue(model, out Mesh mesh) && mesh != null) return mesh;
        var b = new Builder(491);
        if (model.StartsWith("Casa")) House(b, model.Contains("Corredor"),model.Contains("Cal")?1:model.Contains("Granero")?2:model.Contains("Chimenea")?3:0);
        else if (model.StartsWith("Arbol")) Tree(b, model.EndsWith("2") ? 2 : model.EndsWith("1") ? 1 : 0);
        else if (model.StartsWith("Montana")) Mountain(b, model.EndsWith("2") ? 2 : model.EndsWith("1") ? 1 : 0);
        else if (model.StartsWith("SierraSuave")) DistantRidge(b,model.EndsWith("1"));
        else if (model == "BordadoOrbital") Motif(b);
        else if (model == "Helecho") Fern(b, Vector3.zero, 1f);
        else if (model == "Piedra") b.Ellipsoid(new Vector3(0,.35f,0),new Vector3(1,.7f,.8f),new Color(.48f,.49f,.38f),8,4);
        else if (model == "Arbusto") Bush(b, Vector3.zero, 1f);
        else if (model == "Tronco_Caido") FallenLog(b);
        else if (model == "Tocon_Raices") Stump(b);
        else if (model == "Arbusto_Florido" || model == "Arbusto_Bayas")
        {
            Bush(b,Vector3.zero,1.35f);
            for(int i=0;i<24;i++)b.Ellipsoid(new Vector3(b.Range(-.48f,.48f),b.Range(.40f,.92f),b.Range(-.45f,.45f)),Vector3.one*(model.EndsWith("Bayas")?.065f:.095f),model.EndsWith("Bayas")?new Color(.64f,.16f,.11f):new Color(.94f,.86f,.60f),5,2);
        }
        mesh = b.Build(model); cache[model] = mesh; return mesh;
    }

    public static Vector3 HouseSize(string model)=>new Vector3(model.Contains("Granero")?9.8f:model.Contains("Corredor")?8.4f:7.2f,3.9f,model.Contains("Granero")?7.6f:5.8f);

    private static void House(Builder b, bool porch,int variant=0)
    {
        Color adobe = new Color(.70f,.49f,.28f), clay = new Color(.62f,.22f,.095f);
        Color stone = new Color(.37f,.40f,.34f), wood = new Color(.25f,.13f,.065f);
        if(variant==1)adobe=new Color(.83f,.78f,.63f);
        if(variant==2){adobe=new Color(.57f,.40f,.25f);clay=new Color(.48f,.24f,.13f);}
        if(variant==3)adobe=new Color(.76f,.57f,.36f);
        float w = variant==2?9.8f:porch ? 8.4f : 7.2f, d = variant==2?7.6f:5.8f, eave = 3.9f, ridge = 5.8f;
        b.Box(new Vector3(0,1.95f,0),new Vector3(w,3.9f,d),adobe);
        b.Box(new Vector3(0,.36f,0),new Vector3(w+.10f,.72f,d+.10f),stone*.78f);
        // Plastered gable walls, tiled roof with individually curved overlapping courses.
        for (int side=-1;side<=1;side+=2)
        {
            float z=side*d*.5f;
            b.Triangle(new Vector3(-w*.5f,eave,z),new Vector3(0,ridge-.1f,z),new Vector3(w*.5f,eave,z),adobe*.94f);
            for(int row=0;row<3;row++) for(int col=0;col<12;col++)
                b.Ellipsoid(new Vector3(-w*.5f+(col+.5f)*w/12f,row*.24f+.17f,z),
                    new Vector3(w/12f*.98f,.29f,.30f),stone*b.Range(.84f,1.17f),5,3);
            // Recessed door and shuttered window on both elevations.
            b.Box(new Vector3(-1.35f,1.58f,z+side*.035f),new Vector3(1.56f,2.42f,.12f),wood*.47f);
            for(int plank=0;plank<6;plank++)
                b.Box(new Vector3(-1.97f+plank*.247f,1.60f,z+side*.11f),new Vector3(.232f,2.21f,.10f),wood*b.Range(.83f,1.12f));
            b.Box(new Vector3(-1.35f,2.91f,z+side*.12f),new Vector3(1.94f,.22f,.26f),wood);
            b.Box(new Vector3(-1.35f,1.23f,z+side*.20f),new Vector3(1.40f,.13f,.10f),wood*1.3f);
            b.Box(new Vector3(-1.04f,1.63f,z+side*.25f),new Vector3(.17f,.10f,.11f),new Color(.60f,.47f,.20f));
            b.Box(new Vector3(1.65f,2.23f,z+side*.03f),new Vector3(1.30f,1.23f,.12f),wood*.40f);
            for(int k=0;k<5;k++) b.Box(new Vector3(1.13f+k*.26f,2.23f,z+side*.15f),new Vector3(.11f,1.34f,.16f),wood);
            foreach(float y in new[]{1.52f,2.94f}) b.Box(new Vector3(1.65f,y,z+side*.17f),new Vector3(1.6f,.15f,.27f),wood*1.12f);
            for(int step=0;step<3;step++) b.Box(new Vector3(-1.35f,.07f+step*.08f,z+side*(.95f-step*.23f)),new Vector3(2.1f,.14f+step*.16f,.55f),stone*1.12f);
            for(int i=0;i<70;i++)
            {
                float x=b.Range(-w*.48f,w*.48f), y=b.Range(.87f,3.65f);
                if((x > -2.35f && x < -.30f && y < 3.2f) || (x>.78f && x<2.55f && y>1.4f && y<3.15f)) continue;
                b.Box(new Vector3(x,y,z+side*.018f),new Vector3(b.Range(.12f,.5f),b.Range(.025f,.095f),.04f),adobe*b.Range(.87f,1.09f));
            }
        }
        for(int side=-1;side<=1;side+=2)
        {
            b.Box(new Vector3(side*(w*.5f+.04f),.79f,0),new Vector3(.13f,.15f,d),clay*.85f);
            b.Box(new Vector3(side*(w*.5f+.025f),2.20f,.4f),new Vector3(.12f,1.25f,1.12f),wood*.5f);
            for(int k=0;k<5;k++)b.Box(new Vector3(side*(w*.5f+.12f),2.20f,-.08f+k*.24f),new Vector3(.15f,1.35f,.12f),wood*1.1f);
            foreach(float y in new[]{1.48f,2.92f})b.Box(new Vector3(side*(w*.5f+.13f),y,.4f),new Vector3(.25f,.15f,1.40f),wood);
            for(int j=0;j<55;j++)
            {
                float y=b.Range(.95f,3.65f),z=b.Range(-d*.47f,d*.47f);
                if(z>-.35f&&z<1.15f&&y>1.35f&&y<3.1f)continue;
                b.Box(new Vector3(side*(w*.5f+.018f),y,z),new Vector3(.04f,b.Range(.03f,.08f),b.Range(.2f,.65f)),adobe*b.Range(.87f,1.08f));
            }
            for(int row=0;row<3;row++) for(int col=0;col<10;col++)
                b.Ellipsoid(new Vector3(side*w*.5f,row*.24f+.17f,-d*.5f+(col+.5f)*d/10f),new Vector3(.3f,.29f,d/10f*.98f),stone*b.Range(.83f,1.16f),5,3);
            b.Box(new Vector3(side*w*.5f,3.85f,0),new Vector3(.25f,.22f,d+.9f),wood);
            for(int beam=0;beam<9;beam++) b.Beam(new Vector3(0,ridge-.16f,-d*.5f+beam*d/8f),new Vector3(side*(w*.5f+.42f),eave-.10f,-d*.5f+beam*d/8f),.14f,wood,6);
            for(int course=0;course<9;course++) for(int tile=0;tile<15;tile++)
            {
                float t0=course/9f,t1=(course+1.13f)/9f;
                float x0=side*(w*.5f+.48f)*t0,x1=side*(w*.5f+.48f)*t1;
                float y0=Mathf.Lerp(ridge,eave-.12f,t0)+.08f,y1=Mathf.Lerp(ridge,eave-.12f,t1)+.08f;
                float z=-d*.5f-.38f+tile*(d+.76f)/15f;
                Color tileColor=clay*b.Range(.83f,1.18f);
                for(int seg=0;seg<5;seg++)
                {
                    float a=seg*Mathf.PI/5f,c=(seg+1)*Mathf.PI/5f,r=.23f;
                    b.Quad(new Vector3(x0,y0+Mathf.Sin(a)*.085f,z+Mathf.Cos(a)*r),new Vector3(x1,y1+Mathf.Sin(a)*.085f,z+Mathf.Cos(a)*r),
                        new Vector3(x1,y1+Mathf.Sin(c)*.085f,z+Mathf.Cos(c)*r),new Vector3(x0,y0+Mathf.Sin(c)*.085f,z+Mathf.Cos(c)*r),tileColor);
                    b.Quad(new Vector3(x1,y1+Mathf.Sin(a)*.085f,z+Mathf.Cos(a)*r),new Vector3(x1,y1+Mathf.Sin(a)*.085f-.05f,z+Mathf.Cos(a)*r),
                        new Vector3(x1,y1+Mathf.Sin(c)*.085f-.05f,z+Mathf.Cos(c)*r),new Vector3(x1,y1+Mathf.Sin(c)*.085f,z+Mathf.Cos(c)*r),tileColor*.74f);
                }
            }
        }
        for(int i=0;i<14;i++) b.Beam(new Vector3(0,ridge+.06f,-d*.5f-.38f+i*.48f),new Vector3(0,ridge+.06f,-d*.5f+.10f+i*.48f),.18f,clay*1.08f,7);
        if(porch)
        {
            for(int i=0;i<4;i++) b.Beam(new Vector3(-w*.5f+.35f+i*(w-.7f)/3f,0,-d*.5f-1.8f),new Vector3(-w*.5f+.35f+i*(w-.7f)/3f,2.9f,-d*.5f-1.8f),.10f,wood,6);
            b.Box(new Vector3(0,2.98f,-d*.5f-1.7f),new Vector3(w+.2f,.16f,.2f),wood);
            for(int i=0;i<22;i++) b.Beam(new Vector3(-w*.5f+i*w/21f,3.58f,-d*.5f-.03f),new Vector3(-w*.5f+i*w/21f,3.03f,-d*.5f-1.96f),.15f,clay*b.Range(.85f,1.18f),6);
        }
        if(variant==3)
        {
            b.Box(new Vector3(2.1f,5.05f,1.6f),new Vector3(.85f,2.1f,.85f),stone);
            b.Box(new Vector3(2.1f,6.18f,1.6f),new Vector3(1.05f,.20f,1.05f),clay);
            b.Box(new Vector3(2.1f,6.29f,1.6f),new Vector3(.58f,.025f,.58f),wood*.25f);
        }
        if(variant==1)
        {
            foreach(int side in new[]{-1,1})b.Box(new Vector3(0,.98f,side*(d*.5f+.04f)),new Vector3(w,.3f,.10f),clay);
            for(int i=0;i<3;i++){b.Box(new Vector3(1.65f,1.35f,-d*.5f-.28f),new Vector3(1.65f,.25f,.48f),wood);Bush(b,new Vector3(1.1f+i*.5f,1.45f,-d*.5f-.3f),.45f);}
        }
        if(variant==2)
        {
            for(int i=0;i<4;i++)b.Box(new Vector3(3.25f,.45f+i*.58f,-d*.5f-.7f),new Vector3(1.25f,.52f,.9f),new Color(.66f,.55f,.30f));
            for(int i=0;i<7;i++)b.Beam(new Vector3(-w*.5f+i*w/6f,.65f,-d*.5f-.07f),new Vector3(-w*.5f+i*w/6f,3.8f,-d*.5f-.07f),.065f,wood,5);
        }
    }

    private static void FallenLog(Builder b)
    {
        Color bark=new Color(.28f,.19f,.12f),cut=new Color(.64f,.47f,.27f);
        Vector3 a=new Vector3(-1.25f,.34f,0),c=new Vector3(1.25f,.34f,.18f);
        b.Beam(a,c,.34f,bark,10);
        Vector3 axis=(c-a).normalized;
        foreach(int end in new[]{-1,1})
        {
            Vector3 center=end<0?a:c;
            for(int ring=0;ring<4;ring++)b.Beam(center+axis*end*(.005f+ring*.004f),center+axis*end*(.01f+ring*.004f),.30f-ring*.065f,cut*(ring%2==0?1f:.76f),10);
        }
        for(int i=0;i<10;i++){float angle=i*Mathf.PI/5;Vector3 offset=new Vector3(0,Mathf.Cos(angle)*.34f,Mathf.Sin(angle)*.34f);b.Beam(a+offset,c+offset,.022f,bark*b.Range(.7f,1.2f),4);}
        b.Beam(new Vector3(.3f,.48f,.06f),new Vector3(.65f,.94f,.32f),.10f,bark,6);
        for(int i=0;i<5;i++)b.Ellipsoid(new Vector3(b.Range(-.8f,.8f),.63f,b.Range(-.1f,.15f)),new Vector3(.45f,.07f,.3f),new Color(.30f,.38f,.14f),6,2);
    }
    private static void Stump(Builder b)
    {
        Color bark=new Color(.29f,.20f,.12f);
        b.Beam(Vector3.zero,new Vector3(.04f,.72f,0),.34f,bark,9);
        b.Beam(new Vector3(.04f,.725f,0),new Vector3(.04f,.745f,0),.29f,new Color(.67f,.50f,.29f),9);
        b.Beam(new Vector3(.04f,.746f,0),new Vector3(.04f,.75f,0),.17f,new Color(.53f,.36f,.19f),9);
        for(int i=0;i<6;i++){float a=i*Mathf.PI/3;b.Beam(new Vector3(0,.24f,0),new Vector3(Mathf.Cos(a)*.70f,.04f,Mathf.Sin(a)*.70f),.10f,bark,5);}
    }

    private static void Tree(Builder b,int kind)
    {
        Color bark=new Color(.28f,.19f,.105f);
        float height=kind==2?7.2f:9.2f;
        b.Beam(Vector3.zero,new Vector3(.12f,height*.81f,.07f),.17f,bark,8);
        for(int root=0;root<5;root++) { float a=root*1.256f; b.Beam(new Vector3(0,.42f,0),new Vector3(Mathf.Cos(a)*.6f,.03f,Mathf.Sin(a)*.6f),.10f,bark,5); }
        if(kind==2)
        {
            for(int branch=0;branch<9;branch++)
            {
                float a=branch*2.39996f, r=b.Range(.8f,2.25f);
                Vector3 end=new Vector3(Mathf.Cos(a)*r,b.Range(4.3f,6.8f),Mathf.Sin(a)*r);
                b.Beam(new Vector3(0,2.6f,0),end,.09f,bark,6);
                for(int leaf=0;leaf<4;leaf++) b.Ellipsoid(end+b.Vector(-.68f,.68f),new Vector3(1.7f,1.05f,1.55f),new Color(.25f,.39f,.115f)*b.Range(.85f,1.20f),6,3);
            }
        }
        else
        {
            for(int level=0;level<7;level++)
            {
                float y=2.6f+level*.87f, radius=2.55f*(1f-level/8f);
                for(int branch=0;branch<5;branch++)
                {
                    float a=branch*1.2566f+level*.65f;
                    Vector3 end=new Vector3(Mathf.Cos(a)*radius,y-.25f,Mathf.Sin(a)*radius);
                    b.Beam(new Vector3(0,y+.18f,0),end,.04f,bark,4);
                    for(int tuft=0;tuft<2;tuft++)
                    {
                        Vector3 p=Vector3.Lerp(new Vector3(0,y+.28f,0),end,(tuft+1)/2f);
                        b.Ellipsoid(p,new Vector3(radius*.95f,.65f,radius*.8f),new Color(.13f,kind==0?.32f:.38f,.20f)*b.Range(.88f,1.17f),5,3);
                    }
                }
            }
            b.Ellipsoid(new Vector3(0,8.6f,0),new Vector3(.56f,1.1f,.56f),new Color(.20f,.39f,.20f),6,3);
        }
    }

    private static void DistantRidge(Builder b,bool far)
    {
        // Wide foothills: continuous profiles, painted forest and exposed rock.
        const int columns=64, rows=12;
        Vector3[,] points=new Vector3[columns+1,rows+1];
        for(int x=0;x<=columns;x++)
        {
            float u=x/(float)columns;
            float crest=57f+Mathf.PerlinNoise(u*5.2f,far?8.1f:2.7f)*42f+Mathf.Sin(u*17f)*3f;
            // Sink ribbon ends below the horizon so overlaps cannot expose a vertical cut.
            float shoulder=Mathf.SmoothStep(0,1,Mathf.Min(u,1-u)/.16f);
            crest=Mathf.Lerp(35f,crest,shoulder);
            for(int y=0;y<=rows;y++)
            {
                float v=y/(float)rows;
                points[x,y]=new Vector3((u-.5f)*280f,v*crest,Mathf.Sin(v*Mathf.PI)*22f);
            }
        }
        for(int x=0;x<columns;x++)for(int y=0;y<rows;y++)
        {
            float v=y/(float)rows;
            float patches=Mathf.PerlinNoise(x*.17f,y*.3f);
            Color forest=far?new Color(.47f,.61f,.65f):new Color(.30f,.43f,.32f);
            Color rock=far?new Color(.58f,.69f,.71f):new Color(.46f,.53f,.39f);
            Color color=Color.Lerp(forest,rock,Mathf.Clamp01(patches*.28f+v*.12f));
            b.Quad(points[x,y],points[x,y+1],points[x+1,y+1],points[x+1,y],color);
        }
    }

    private static void Mountain(Builder b, int kind)
    {
        Color baseColor = kind == 0
            ? new Color(.25f, .31f, .29f)
            : kind == 1 ? new Color(.31f, .34f, .29f) : new Color(.27f, .29f, .33f);
        float[] centers = { -7.5f, 0f, 7.1f };
        float[] widths = { 11.5f, 15.2f, 10.4f };
        float[] heights = { 8.4f, 12.5f, 7.7f };
        for (int peakIndex = 0; peakIndex < centers.Length; peakIndex++)
        {
            float x = centers[peakIndex] + b.Range(-1.1f, 1.1f);
            float width = widths[peakIndex] * b.Range(.88f, 1.12f);
            float height = heights[peakIndex] * b.Range(.88f, 1.13f);
            float depth = width * .34f;
            Vector3 leftFront = new Vector3(x - width * .5f, 0f, depth);
            Vector3 rightFront = new Vector3(x + width * .5f, 0f, depth);
            Vector3 rightBack = new Vector3(x + width * .5f, 0f, -depth);
            Vector3 leftBack = new Vector3(x - width * .5f, 0f, -depth);
            Vector3 summit = new Vector3(x + b.Range(-width * .12f, width * .12f), height, b.Range(-depth * .15f, depth * .15f));
            Color light = baseColor * b.Range(1.02f, 1.16f);
            Color mid = baseColor * b.Range(.87f, 1.02f);
            Color shade = baseColor * b.Range(.67f, .82f);
            b.Triangle(leftFront, rightFront, summit, light);
            b.Triangle(rightFront, rightBack, summit, mid);
            b.Triangle(rightBack, leftBack, summit, shade);
            b.Triangle(leftBack, leftFront, summit, mid);
            b.Quad(leftBack, rightBack, rightFront, leftFront, baseColor * .68f);

            // Planos secundarios rompen la silueta triangular uniforme.
            Vector3 shoulder = Vector3.Lerp(leftFront, summit, .48f) + new Vector3(width * .08f, -height * .08f, .02f);
            b.Triangle(leftFront, shoulder, summit, baseColor * 1.18f);
        }
    }
    public static void Grass(Builder b,Vector3 at,float size)
    {
        for(int i=0;i<9;i++)
        {
            float a=b.Range(0,6.283f),h=b.Range(.18f,.53f)*size;
            Vector3 start=at+new Vector3(b.Range(-.22f,.22f),0,b.Range(-.22f,.22f))*size;
            Vector3 side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.045f*size;
            Vector3 tip=start+Vector3.up*h+side*3f;
            b.Triangle(start-side,start+side,tip,new Color(.32f,.43f,.12f)*b.Range(.83f,1.27f));
        }
    }
    public static void Fern(Builder b,Vector3 at,float size)
    {
        for(int frond=0;frond<7;frond++)
        {
            float a=frond*.8976f; Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),side=Vector3.Cross(dir,Vector3.up);
            for(int pair=1;pair<7;pair++)
            {
                float t=pair/7f;
                Vector3 p=at+size*(dir*t*.8f+Vector3.up*Mathf.Sin(t*2f)*.52f);
                float width=(1-t)*.25f*size;
                for(int sign=-1;sign<=1;sign+=2)
                    b.Triangle(p-dir*.08f*size,p+side*width*sign+dir*.1f*size,p+dir*.08f*size,new Color(.17f,.37f,.16f)*b.Range(.9f,1.15f));
            }
        }
    }
    public static void Bush(Builder b,Vector3 at,float size)
    {
        for(int i=0;i<6;i++) b.Ellipsoid(at+new Vector3(b.Range(-.4f,.4f),b.Range(.25f,.6f),b.Range(-.4f,.4f))*size,
            new Vector3(.72f,.6f,.69f)*size,new Color(.27f,.40f,.13f)*b.Range(.85f,1.2f),6,3);
    }
    private static void Motif(Builder b)
    {
        // Fantasy flower inspired by embroidered floral/geometric forms; not a claimed ceremonial object.
        Color silver=new Color(.82f,.82f,.69f),grana=new Color(.68f,.08f,.22f),gold=new Color(.95f,.65f,.16f);
        b.Ellipsoid(Vector3.zero,new Vector3(.34f,.34f,.13f),gold,8,3);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4f;
            Vector3 dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0), side=new Vector3(-dir.y,dir.x,0);
            Vector3 p=dir*.23f;
            b.Quad(p-side*.11f,p+dir*.20f-side*.15f,p+dir*.52f,p+dir*.20f+side*.15f,silver);
            b.Quad(p+dir*.06f-side*.055f+Vector3.back*.02f,p+dir*.20f-side*.085f+Vector3.back*.02f,
                p+dir*.39f+Vector3.back*.02f,p+dir*.20f+side*.085f+Vector3.back*.02f,i%2==0?grana:gold);
        }
    }

    public sealed class Builder
    {
        private readonly List<Vector3> vertices=new List<Vector3>();
        private readonly List<Color> colors=new List<Color>();
        private readonly List<int> indices=new List<int>();
        private readonly System.Random random;
        public Builder(int seed) { random=new System.Random(seed); }
        public float Range(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
        public Vector3 Vector(float min,float max)=>new Vector3(Range(min,max),Range(min,max),Range(min,max));
        public void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
        { int start=vertices.Count; vertices.Add(a);vertices.Add(b);vertices.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);indices.Add(start);indices.Add(start+1);indices.Add(start+2); }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color) { Triangle(a,b,c,color);Triangle(a,c,d,color); }
        public void Box(Vector3 c,Vector3 size,Color color)
        {
            Vector3 s=size*.5f;
            Vector3 a=c+new Vector3(-s.x,-s.y,-s.z),b=c+new Vector3(s.x,-s.y,-s.z),d=c+new Vector3(-s.x,-s.y,s.z),e=c+new Vector3(s.x,-s.y,s.z),u=Vector3.up*size.y;
            Quad(a,a+u,b+u,b,color);Quad(e,e+u,d+u,d,color);Quad(d,d+u,a+u,a,color);Quad(b,b+u,e+u,e,color);Quad(a+u,d+u,e+u,b+u,color);Quad(a,b,e,d,color);
        }
        public void Beam(Vector3 a,Vector3 b,float radius,Color color,int sides=6)
        {
            Quaternion rotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
            for(int i=0;i<sides;i++)
            {
                float x=i*Mathf.PI*2/sides,y=(i+1)*Mathf.PI*2/sides;
                Vector3 p=rotation*new Vector3(Mathf.Cos(x),0,Mathf.Sin(x))*radius,q=rotation*new Vector3(Mathf.Cos(y),0,Mathf.Sin(y))*radius;
                Quad(a+p,b+p,b+q,a+q,color);Triangle(b,b+p,b+q,color);Triangle(a,a+q,a+p,color);
            }
        }
        public void Ellipsoid(Vector3 center,Vector3 size,Color color,int sides=7,int rings=4)
        {
            for(int r=0;r<rings;r++)for(int s=0;s<sides;s++)
            {
                float a=s*6.283185f/sides,c=(s+1)*6.283185f/sides,p=-Mathf.PI*.5f+r*Mathf.PI/rings,q=-Mathf.PI*.5f+(r+1)*Mathf.PI/rings;
                Quad(center+Point(a,p,size),center+Point(a,q,size),center+Point(c,q,size),center+Point(c,p,size),color*Range(.95f,1.05f));
            }
        }
        private static Vector3 Point(float a,float p,Vector3 size)=>Vector3.Scale(new Vector3(Mathf.Cos(a)*Mathf.Cos(p),Mathf.Sin(p),Mathf.Sin(a)*Mathf.Cos(p)),size*.5f);
        public Mesh Build(string name)
        { var mesh=new Mesh {name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh; }
    }
}
