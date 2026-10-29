using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Exporta las mallas existentes; no modifica escena, distribución ni modelos.
public static class XunjuuTreeExport
{
    private static string F(float value)=>value.ToString("R",CultureInfo.InvariantCulture);
    [MenuItem("Tools/Xunjuu/Exportar árboles actuales para Blender")]
    public static void Run()
    {
        string directory=Path.GetFullPath("Exports/Blender/Arboles_Actuales_20261019");
        string[] args=Environment.GetCommandLineArgs();
        for(int i=0;i<args.Length-1;i++)if(args[i]=="-dyanatroTreeExport")directory=Path.GetFullPath(args[i+1]);
        Directory.CreateDirectory(directory);
        string[] models={"Arbol0","Arbol2","Arbol1"};
        string[] names={"Pino_actual","Encino_actual","Conifera_legacy_no_activa"};
        for(int i=0;i<models.Length;i++)
        {
            Mesh mesh=XunjuuArtMeshes.Get(models[i]);
            WriteGlb(mesh,names[i],Path.Combine(directory,names[i]+".glb"));
            Preview(models[i],Path.Combine(directory,names[i]+".png"));
            Debug.Log("[TREE EXPORT] "+names[i]+": "+mesh.vertexCount+" vertices, "+mesh.triangles.Length/3+" triangles");
        }
        Debug.Log("[TREE EXPORT PASS] "+directory);
    }
    private static void WriteGlb(Mesh mesh,string name,string path)
    {
        Vector3[] vertices=mesh.vertices,normals=mesh.normals;Color[] colors=mesh.colors;int[] triangles=mesh.triangles;
        Vector3 min=Vector3.positiveInfinity,max=Vector3.negativeInfinity;
        byte[] buffer;
        using(var binary=new MemoryStream())
        using(var writer=new BinaryWriter(binary))
        {
            foreach(Vector3 v in vertices){var p=new Vector3(-v.x,v.y,v.z);writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);min=Vector3.Min(min,p);max=Vector3.Max(max,p);}
            foreach(Vector3 n in normals){var p=n.sqrMagnitude>.01f?new Vector3(-n.x,n.y,n.z).normalized:Vector3.up;writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}
            foreach(Color c in colors){writer.Write(Mathf.Clamp01(c.r));writer.Write(Mathf.Clamp01(c.g));writer.Write(Mathf.Clamp01(c.b));writer.Write(1f);}
            for(int i=0;i<triangles.Length;i+=3){writer.Write((uint)triangles[i]);writer.Write((uint)triangles[i+2]);writer.Write((uint)triangles[i+1]);}
            buffer=binary.ToArray();
        }
        int positions=vertices.Length*12,normalBytes=normals.Length*12,colorBytes=colors.Length*16,indexBytes=triangles.Length*4;
        string vectorMin=$"[{F(min.x)},{F(min.y)},{F(min.z)}]",vectorMax=$"[{F(max.x)},{F(max.y)},{F(max.z)}]";
        string json="{\"asset\":{\"version\":\"2.0\",\"generator\":\"DYANATRO current-tree exporter\"},\"scene\":0,\"scenes\":[{\"nodes\":[0]}],"
            +"\"nodes\":[{\"name\":\""+name+"\",\"mesh\":0}],\"meshes\":[{\"name\":\""+name+"\",\"primitives\":[{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"COLOR_0\":2},\"indices\":3,\"material\":0}]}],"
            +"\"materials\":[{\"name\":\"Vegetacion_colores_de_vertices\",\"pbrMetallicRoughness\":{\"baseColorFactor\":[1,1,1,1],\"metallicFactor\":0,\"roughnessFactor\":0.92},\"doubleSided\":false}],"
            +"\"buffers\":[{\"byteLength\":"+buffer.Length+"}],\"bufferViews\":["
            +View(0,positions,34962)+","+View(positions,normalBytes,34962)+","+View(positions+normalBytes,colorBytes,34962)+","+View(positions+normalBytes+colorBytes,indexBytes,34963)+"],"
            +"\"accessors\":[{\"bufferView\":0,\"componentType\":5126,\"count\":"+vertices.Length+",\"type\":\"VEC3\",\"min\":"+vectorMin+",\"max\":"+vectorMax+"},"
            +Accessor(1,vertices.Length,"VEC3",5126)+","+Accessor(2,vertices.Length,"VEC4",5126)+","+Accessor(3,triangles.Length,"SCALAR",5125)+"]}";
        byte[] encoded=Encoding.UTF8.GetBytes(json);int padded=(encoded.Length+3)&~3;
        using(var file=File.Create(path))using(var output=new BinaryWriter(file))
        {
            output.Write(0x46546C67u);output.Write(2u);output.Write((uint)(12+8+padded+8+buffer.Length));
            output.Write((uint)padded);output.Write(0x4E4F534Au);output.Write(encoded);
            for(int i=encoded.Length;i<padded;i++)output.Write((byte)32);
            output.Write((uint)buffer.Length);output.Write(0x004E4942u);output.Write(buffer);
        }
    }
    private static string View(int offset,int length,int target)=>"{\"buffer\":0,\"byteOffset\":"+offset+",\"byteLength\":"+length+",\"target\":"+target+"}";
    private static string Accessor(int view,int count,string type,int component)=>"{\"bufferView\":"+view+",\"componentType\":"+component+",\"count\":"+count+",\"type\":\""+type+"\"}";
    private static void Preview(string model,string path)
    {
        var preview=new PreviewRenderUtility();var root=XunjuuArtMeshes.Create(model,null,"Vista_Arbol");
        try
        {
            preview.AddSingleGO(root);var bounds=root.GetComponent<Renderer>().bounds;
            preview.camera.transform.position=bounds.center+new Vector3(13,8,-18);preview.camera.transform.LookAt(bounds.center);
            preview.camera.orthographic=true;preview.camera.orthographicSize=bounds.size.y*.62f;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=100;
            preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.13f,.18f,.16f);
            preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(42,-35,0);preview.lights[1].intensity=.5f;
            preview.BeginPreview(new Rect(0,0,768,768),GUIStyle.none);preview.Render(true);Texture rendered=preview.EndPreview();
            var rt=RenderTexture.GetTemporary(768,768,0);var previous=RenderTexture.active;
            try{Graphics.Blit(rendered,rt);RenderTexture.active=rt;var image=new Texture2D(768,768,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,768,768),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        }
        finally{preview.Cleanup();if(root!=null)UnityEngine.Object.DestroyImmediate(root);}
    }
}
