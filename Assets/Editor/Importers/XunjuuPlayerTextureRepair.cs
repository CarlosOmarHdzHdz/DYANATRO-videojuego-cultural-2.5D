using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class XunjuuPlayerTextureRepair
{
    [MenuItem("Tools/Xunjuu/Reparar transparencia protagonista")]
    public static void Run()
    {
        const string root = "Assets/Resources/Sprites/Player/";
        string path = root + "MacuahuitlWalk_8x2.png";
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(path));
        Color32[] pixels = texture.GetPixels32();
        int width = texture.width, height = texture.height;
        var queue = new Queue<int>();
        var visited = new bool[pixels.Length];
        System.Action<int> add = i => {
            if (visited[i]) return;
            visited[i] = true;
            Color32 p = pixels[i];
            if (p.r <= 12 && p.g <= 12 && p.b <= 12) queue.Enqueue(i);
        };
        // Flood only black connected to cell edges, preserving enclosed black outlines.
        int cellWidth = width / 8, cellHeight = height / 2;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (x % cellWidth == 0 || x % cellWidth == cellWidth - 1 || y % cellHeight == 0 || y % cellHeight == cellHeight - 1)
                    add(y * width + x);
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            pixels[i].a = 0;
            int x = i % width, y = i / width;
            if (x > 0) add(i - 1);
            if (x + 1 < width) add(i + 1);
            if (y > 0) add(i - width);
            if (y + 1 < height) add(i + width);
        }
        Directory.CreateDirectory("output/player-backup");
        string backup = "output/player-backup/MacuahuitlWalk_8x2.png";
        if (!File.Exists(backup)) File.Copy(path, backup);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        Configure(path);
        Configure(root + "MacuahuitlComplete_4x4.png");
    }

    private static void Configure(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
