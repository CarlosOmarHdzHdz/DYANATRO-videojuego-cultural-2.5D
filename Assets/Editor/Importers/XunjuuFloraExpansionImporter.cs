using UnityEditor;
using UnityEngine;

public sealed class XunjuuFloraExpansionImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.EndsWith("FloraExpansion_4x2.png"))return;
        Configure((TextureImporter)assetImporter);
    }
    public static void Configure(TextureImporter importer)
    {
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.isReadable=true;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
    }
}
