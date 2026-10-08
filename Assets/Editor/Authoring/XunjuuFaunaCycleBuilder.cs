using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class XunjuuFaunaCycleBuilder
{
    [MenuItem("Tools/Xunjuu/Construir ciclos y verificar captura %&j")]
    public static void BuildAndValidate()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Deten Play antes de construir ciclos."); return; }
        Build();
        XunjuuCaptureFlowValidation.StartValidation();
    }

    public static void Build()
    {
        const int cell = 128;
        string destination = "Assets/Resources/Sprites/Animals/CyclesV2";
        Directory.CreateDirectory(destination);
        foreach (var entry in XunjuuFaunaCatalog.Entries)
        {
            var source = new Texture2D(2,2,TextureFormat.RGBA32,false);
            if (!source.LoadImage(File.ReadAllBytes("Assets/Art/Animals/CyclesV2/"+entry.Id+".png")))
                throw new InvalidOperationException("No se pudo leer "+entry.Id);
            var pixels = source.GetPixels32();
            var bounds = new RectInt[16];
            int largest = 1;
            for (int f=0;f<16;f++)
            {
                int left=Mathf.RoundToInt(f%4*source.width/4f),right=Mathf.RoundToInt((f%4+1)*source.width/4f);
                int bottom=Mathf.RoundToInt((3-f/4)*source.height/4f),top=Mathf.RoundToInt((4-f/4)*source.height/4f);
                int minX=right,minY=top,maxX=left,maxY=bottom;
                for(int y=bottom;y<top;y++) for(int x=left;x<right;x++)
                    if(pixels[y*source.width+x].a>=220) {minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
                if(maxX<=minX || maxY<=minY)throw new InvalidOperationException("Frame vacio: "+entry.Id+"/"+f);
                bounds[f]=new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
                largest=Math.Max(largest,Math.Max(bounds[f].width,bounds[f].height));
            }
            float scale=104f/largest;
            var output=new Texture2D(512,512,TextureFormat.RGBA32,false);
            var result=new Color32[512*512];
            for(int f=0;f<16;f++)
            {
                RectInt b=bounds[f];
                int w=Mathf.RoundToInt(b.width*scale),h=Mathf.RoundToInt(b.height*scale);
                int startX=f%4*cell+(cell-w)/2;
                int lift=f>=8 && (f%4==2) ? 4 : 0;
                int startY=(3-f/4)*cell+12+lift;
                for(int y=0;y<h;y++) for(int x=0;x<w;x++)
                {
                    int sx=b.x+Mathf.Min(b.width-1,Mathf.FloorToInt(x/scale));
                    int sy=b.y+Mathf.Min(b.height-1,Mathf.FloorToInt(y/scale));
                    Color32 p=pixels[sy*source.width+sx];
                    if(p.a<220)continue;
                    p.a=255;
                    result[(startY+y)*512+startX+x]=p;
                }
            }
            output.SetPixels32(result);output.Apply();
            string path=destination+"/"+entry.Id+".png";
            File.WriteAllBytes(path,output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=512;importer.isReadable=true;
            importer.SaveAndReimport();
            Debug.Log("[FAUNA CYCLES] "+entry.Id+": 8 walk + 8 run, pivot fijo, alpha uniforme.");
        }
        AssetDatabase.SaveAssets();
    }
}
