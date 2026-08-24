using UnityEditor;
using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Importación de objetos pixel art
// Acción: conservar el maíz nítido y listo para usar como Sprite.
// ============================================================================
public sealed class XunjuuItemSpriteImporter : AssetPostprocessor
{
    private const string CornAssetPath = "Assets/Resources/Sprites/Items/maiz.png";

    private void OnPreprocessTexture()
    {
        if (!assetPath.Equals(CornAssetPath, System.StringComparison.OrdinalIgnoreCase))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
