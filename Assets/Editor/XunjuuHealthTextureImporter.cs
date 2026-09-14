using UnityEditor;
using UnityEngine;

public sealed class XunjuuHealthTextureImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (assetPath != "Assets/Resources/Health/MazahuaHealthSheet.png") return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.alphaIsTransparency = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
}
