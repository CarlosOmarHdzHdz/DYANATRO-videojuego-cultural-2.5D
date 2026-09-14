using System;
using UnityEditor;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Importacion de imagenes de la ludoteca
// ACCION: evitar que las fotografias de interfaz pierdan nitidez por mipmaps.
// ============================================================================
public sealed class XunjuuLudotecaTextureImporter : AssetPostprocessor
{
    private const string LudotecaFolder = "Assets/Resources/Ludoteca/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(LudotecaFolder, StringComparison.OrdinalIgnoreCase))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.maxTextureSize = 2048;
        android.format = TextureImporterFormat.Automatic;
        android.compressionQuality = 85;
        importer.SetPlatformTextureSettings(android);
    }
}
