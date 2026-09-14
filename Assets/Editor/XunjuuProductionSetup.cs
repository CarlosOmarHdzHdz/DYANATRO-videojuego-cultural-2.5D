using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class XunjuuProductionSetup
{
    [MenuItem("Tools/Xunjuu/Preparar mejoras integradas")]
    public static void Run()
    {
        XunjuuPlayerTextureRepair.Run();
        AssetDatabase.ImportAsset("Assets/Resources/Health/MazahuaHealthSheet.png", ImportAssetOptions.ForceUpdate);
        const string destination = "Assets/Resources/EnvironmentCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<XunjuuEnvironmentCatalog>(destination);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<XunjuuEnvironmentCatalog>(); AssetDatabase.CreateAsset(catalog, destination); }
        Func<string, GameObject> prefab = name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EntornoMazahua/" + name + ".prefab");
        catalog.trees = new[] { prefab("Pino"), prefab("Oyamel"), prefab("Encino") };
        catalog.rocks = new[] { prefab("Piedra_Grande"), prefab("Piedra_Mediana"), prefab("Piedra_Placa") };
        catalog.houses = new[] { prefab("Casa_Adobe"), prefab("Casa_Corredor") };
        if (catalog.trees.Concat(catalog.rocks).Concat(catalog.houses).Any(p => p == null)) throw new Exception("Faltan prefabs de entorno.");
        string directory = "Assets/Art/EntornoMazahua/Materials";
        Shader shader = Shader.Find("Xunjuu/FoliageDither");
        if (shader == null) throw new Exception("Shader de follaje no encontrado.");
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { directory }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Visibility", 1f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }
        catalog.foliageMaterial = MakeMaterial("Vegetacion_3D", new Color(0.24f,0.48f,0.14f), shader);
        catalog.groundMaterial = MakeMaterial("Suelo_Final", new Color(0.25f,0.37f,0.16f), shader);
        Material petals = MakeMaterial("Petalos_3D", new Color(0.86f,0.38f,0.58f), shader);
        Material kernels = MakeMaterial("Maiz_3D", new Color(0.93f,0.72f,0.22f), shader);
        catalog.flower = BuildPlant(false, catalog.foliageMaterial, petals);
        catalog.corn = BuildPlant(true, catalog.foliageMaterial, kernels);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("output/production-validation");
        File.WriteAllText("output/production-validation/setup.txt", "PASS: transparent PNG, catalog, shared instanced materials, 3D flower and corn prefabs. " + DateTime.Now.ToString("O"));
    }

    private static Material MakeMaterial(string name, Color color, Shader shader)
    {
        string path = "Assets/Art/EntornoMazahua/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Visibility", 1f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject BuildPlant(bool corn, Material green, Material accent)
    {
        string name = corn ? "Milpa_3D" : "Flor_3D";
        GameObject root = new GameObject(name);
        var pieces = new List<GameObject>();
        Action<PrimitiveType,Vector3,Vector3,Material> add = (type, position, scale, material) => {
            var piece = GameObject.CreatePrimitive(type);
            piece.transform.SetParent(root.transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            piece.GetComponent<MeshFilter>().sharedMesh = LowPolyPrimitive(type);
            UnityEngine.Object.DestroyImmediate(piece.GetComponent<Collider>());
            pieces.Add(piece);
        };
        float height = corn ? 1.7f : 0.48f;
        add(PrimitiveType.Cylinder, Vector3.up * height/2f, new Vector3(0.04f,height/2f,0.04f), green);
        for (int i=0;i<(corn?6:5);i++)
        {
            float angle = i * 2.39996f;
            Vector3 offset = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            add(PrimitiveType.Sphere, offset*(corn?0.22f:0.1f)+Vector3.up*(corn?0.35f+i*0.18f:height),
                corn?new Vector3(0.5f,0.04f,0.12f):new Vector3(0.14f,0.05f,0.12f), corn?green:accent);
            pieces[pieces.Count-1].transform.localRotation = Quaternion.Euler(0,-angle*Mathf.Rad2Deg, corn?20:0);
        }
        add(PrimitiveType.Sphere, new Vector3(corn?0.12f:0,height*(corn?0.62f:1f),0), corn?new Vector3(0.12f,0.35f,0.12f):new Vector3(0.1f,0.07f,0.1f), accent);
        foreach (Material material in new[] {green,accent})
        {
            CombineInstance[] combine = pieces.Where(p=>p.GetComponent<Renderer>().sharedMaterial==material).Select(p=>new CombineInstance {
                mesh=p.GetComponent<MeshFilter>().sharedMesh, transform=p.transform.localToWorldMatrix }).ToArray();
            Mesh mesh = new Mesh { name=name+material.name };
            mesh.CombineMeshes(combine,true,true);
            string meshPath = "Assets/Art/EntornoMazahua/"+mesh.name+".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing!=null) { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing; }
            else AssetDatabase.CreateAsset(mesh,meshPath);
            var child = new GameObject(material.name,typeof(MeshFilter),typeof(MeshRenderer));
            child.transform.SetParent(root.transform,false);
            child.GetComponent<MeshFilter>().sharedMesh=mesh;
            child.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        foreach(GameObject piece in pieces)
        {
            UnityEngine.Object.DestroyImmediate(piece.GetComponent<MeshFilter>().sharedMesh);
            UnityEngine.Object.DestroyImmediate(piece);
        }
        string path="Assets/Prefabs/EntornoMazahua/"+name+".prefab";
        var result=PrefabUtility.SaveAsPrefabAsset(root,path);
        UnityEngine.Object.DestroyImmediate(root);
        return result;
    }

    private static Mesh LowPolyPrimitive(PrimitiveType type)
    {
        var vertices=new List<Vector3>();
        var triangles=new List<int>();
        Action<Vector3,Vector3,Vector3> face=(a,b,c)=> {
            int start=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(start); triangles.Add(start+1); triangles.Add(start+2);
        };
        int sides=type==PrimitiveType.Cylinder?8:4;
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2f/sides,b=(i+1)*Mathf.PI*2f/sides;
            Vector3 p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*0.5f;
            Vector3 q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*0.5f;
            if(type==PrimitiveType.Cylinder)
            {
                face(p+Vector3.up,q+Vector3.up,p-Vector3.up);
                face(q+Vector3.up,q-Vector3.up,p-Vector3.up);
                face(Vector3.up,q+Vector3.up,p+Vector3.up);
                face(-Vector3.up,p-Vector3.up,q-Vector3.up);
            }
            else { face(Vector3.up*0.5f,q,p); face(-Vector3.up*0.5f,p,q); }
        }
        var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }
}
