using UnityEditor;
using UnityEngine;

public sealed class XunjuuPresentationSpriteImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if(!assetPath.EndsWith("InfusionHierbas_v2.png") && !assetPath.EndsWith("FlorMemoria_v2.png"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Sprite;importer.textureShape=TextureImporterShape.Texture2D;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.isReadable=true;importer.mipmapEnabled=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;
    }
}
