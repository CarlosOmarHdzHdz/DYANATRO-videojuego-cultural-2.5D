using UnityEngine;
using UnityEngine.UI;

/// <summary>Identidad compartida por inicio y carga, sin subtítulos ni fondos opacos.</summary>
public static class XunjuuBranding
{
    public const string ResourcePath = "Sprites/UI/DYANATRO_Logo_Tejido_20261027";
    public const string AssetPath = "Assets/Resources/" + ResourcePath + ".png";
    private static Texture2D logo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { logo = null; }

    public static RectTransform Create(Transform parent, Vector2 size, Vector2 position)
    {
        var root = new GameObject("Logo_DYANATRO", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rect = XunjuuInterfaceTheme.Rect(root, size, position);
        if (logo == null) logo = Resources.Load<Texture2D>(ResourcePath);

        if (logo != null)
        {
            var image = root.AddComponent<RawImage>();
            image.texture = logo;
            image.color = Color.white;
            image.raycastTarget = false;
            // Ajustar el contenedor a la proporción nativa sin deformar las letras.
            float aspect = logo.width / (float)logo.height;
            float width = Mathf.Min(size.x, size.y * aspect);
            rect.sizeDelta = new Vector2(width, width / aspect);
        }
        else
        {
            // Mantener el nombre visible si falta el recurso al importar el proyecto.
            var label = XunjuuInterfaceTheme.Label(root.transform, "Nombre_Alternativo", "DYANATR’O", 64,
                size, Vector2.zero, XunjuuInterfaceTheme.Paper);
            label.fontStyle = FontStyle.Bold;
            Debug.LogWarning("No se encontró el logo bordado: " + ResourcePath);
        }
        return rect;
    }
}
