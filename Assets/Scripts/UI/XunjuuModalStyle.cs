using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class XunjuuModalStyle
{
    public static readonly Color Surface = new Color(.035f, .085f, .09f, .95f);
    public static readonly Color Inset = new Color(.075f, .14f, .15f, .92f);
    public static readonly Color Accent = new Color(.37f, .76f, .70f);
    public static readonly Color Quiet = new Color(.72f, .81f, .79f);
    public static GameObject Card(Transform parent, string name, Vector2 size, Vector2 at)
    {
        var root = XunjuuInterfaceTheme.Panel(parent, name, size, at, Surface);
        root.GetComponent<XunjuuRoundedImage>().CornerRadius = 24;
        var border = new GameObject("Borde_Fino", typeof(RectTransform), typeof(CanvasRenderer)); border.transform.SetParent(root.transform, false);
        var rect = border.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var line = border.AddComponent<XunjuuModalOutline>(); line.color = new Color(.56f, .75f, .68f, .40f); line.raycastTarget = false;
        root.AddComponent<XunjuuPanelReveal>(); return root;
    }
    public static Button Action(Transform parent, string name, string text, Vector2 size, Vector2 at, UnityAction action, Color accent)
    {
        var root = XunjuuInterfaceTheme.Panel(parent, name, size, at, Color.Lerp(Surface, accent, .36f));
        root.GetComponent<XunjuuRoundedImage>().CornerRadius = 12; root.GetComponent<Image>().raycastTarget = true;
        var border = root.AddComponent<Outline>(); border.effectDistance = new Vector2(1, -1); border.effectColor = new Color(accent.r, accent.g, accent.b, .62f);
        var button = root.AddComponent<Button>(); button.targetGraphic = root.GetComponent<Image>(); button.onClick.AddListener(action);
        var colors = button.colors; colors.highlightedColor = colors.selectedColor = new Color(1.23f, 1.23f, 1.23f); colors.pressedColor = new Color(.76f, .76f, .76f); colors.fadeDuration = .12f; button.colors = colors;
        var label = XunjuuInterfaceTheme.Label(root.transform, "Etiqueta", text, 21, size - (size.x <= 60 ? new Vector2(4, 4) : new Vector2(24, 8)), Vector2.zero, XunjuuInterfaceTheme.Paper);
        label.fontStyle = FontStyle.Bold; root.AddComponent<XunjuuButtonMotion>(); return button;
    }
    public static void Rule(Transform parent, float y, float width)
        => XunjuuInterfaceTheme.Panel(parent, "Separador_Suave", new Vector2(width, 1), new Vector2(0, y), new Color(.55f, .72f, .67f, .22f));
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class XunjuuModalOutline : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear(); Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(24, Mathf.Min(rect.width, rect.height) * .5f);
        const int segments = 10; int count = 0;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 center = new Vector2(corner < 2 ? rect.xMax - radius : rect.xMin + radius, corner == 0 || corner == 3 ? rect.yMax - radius : rect.yMin + radius);
            for (int i = 0; i <= segments; i++)
            {
                float angle = (90 - corner * 90 - i * 90f / segments) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var v = UIVertex.simpleVert; v.color = color; v.position = center + direction * radius; mesh.AddVert(v);
                v.position = center + direction * Mathf.Max(0, radius - 1.2f); mesh.AddVert(v); count++;
            }
        }
        for (int i = 0; i < count; i++)
        { int a = i * 2, b = (i + 1) % count * 2; mesh.AddTriangle(a, b, b + 1); mesh.AddTriangle(a, b + 1, a + 1); }
    }
}
