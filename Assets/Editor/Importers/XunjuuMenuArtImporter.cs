using UnityEditor;
using UnityEngine;

public sealed class XunjuuMenuArtImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        bool isLogo=assetPath==XunjuuBranding.AssetPath;
        bool isDeathTitle=assetPath=="Assets/Resources/"+XunjuuDeathScreenStyle.TitlePath+".png";
        if(!isLogo&&!isDeathTitle&&assetPath!="Assets/Resources/Sprites/UI/MenuHighlands_20261019.png"
            &&assetPath!="Assets/Resources/Sprites/UI/DeathOfrenda_20261027.png")return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.sRGBTexture=true;
        importer.isReadable=false;importer.mipmapEnabled=false;importer.alphaIsTransparency=isLogo||isDeathTitle;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
        importer.npotScale=TextureImporterNPOTScale.None;
    }
}
