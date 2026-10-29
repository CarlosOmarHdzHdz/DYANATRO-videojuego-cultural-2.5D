using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Una familia en UI clásica, TextMesh Pro y nombres del mundo. Los logos son imágenes.
public static class XunjuuTypography
{
    static Font regular;
    static TMP_FontAsset distanceField;
    public static Font Regular => regular != null ? regular :
        (regular = Resources.Load<Font>("Fonts/LiberationSans") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
    public static TMP_FontAsset DistanceField => distanceField != null ? distanceField :
        (distanceField = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"));
    public static void ApplyScene()
    {
        foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)) text.font = Regular;
        foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (DistanceField != null) text.font = DistanceField;
    }
}
