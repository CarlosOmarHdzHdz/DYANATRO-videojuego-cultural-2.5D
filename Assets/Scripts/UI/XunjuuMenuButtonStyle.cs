using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Botones de inicio y pausa: superficies suaves, icono por acción y foco visible.
public static class XunjuuMenuButtonStyle
{
    public static Color Accent(string action)
    {
        switch (action.ToUpperInvariant())
        {
            case "INICIAR": case "CONTINUAR": return new Color(.12f, .56f, .53f);
            case "LUDOTECA": return new Color(.76f, .39f, .20f);
            case "CONTROLES": return new Color(.43f, .48f, .73f);
            case "AJUSTES": return new Color(.28f, .48f, .72f);
            case "REINICIAR": return new Color(.73f, .53f, .19f);
            case "MENÚ INICIAL": case "MENU INICIAL": return new Color(.31f, .55f, .43f);
            default: return new Color(.68f, .28f, .35f);
        }
    }
    public static Button Create(Transform parent, string name, string text, Vector2 position, Vector2 size, UnityAction action)
    {
        Color accent = Accent(text);
        var root = XunjuuInterfaceTheme.Panel(parent, name, size, position, Color.Lerp(new Color(.025f, .055f, .065f), accent, .62f));
        var image = root.GetComponent<XunjuuRoundedImage>(); image.CornerRadius = 14; image.raycastTarget = true;
        var shadow = root.AddComponent<Shadow>(); shadow.effectColor = new Color(0, .01f, .015f, .28f); shadow.effectDistance = new Vector2(0, -5);
        var outline = root.AddComponent<Outline>(); outline.effectColor = Color.Lerp(accent, XunjuuInterfaceTheme.Paper, .25f); outline.effectDistance = new Vector2(1, -1);
        var button = root.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        var colors = button.colors; colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.20f, 1.20f, 1.20f); colors.selectedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(.77f, .77f, .77f); colors.disabledColor = new Color(.5f, .5f, .5f, .65f);
        colors.fadeDuration = .12f; button.colors = colors; root.AddComponent<XunjuuButtonMotion>();
        var tile = XunjuuInterfaceTheme.Panel(root.transform, "Icono_Accion", new Vector2(38, 38), new Vector2(-size.x * .5f + 36, 0), new Color(.025f, .055f, .065f, .42f));
        var drawing = new GameObject("Trazo_Icono", typeof(RectTransform)); drawing.transform.SetParent(tile.transform, false);
        XunjuuInterfaceTheme.Rect(drawing, new Vector2(32, 32), Vector2.zero);
        var icon = drawing.AddComponent<XunjuuMenuActionIcon>(); icon.Action = text; icon.color = new Color(.96f, .90f, .72f); icon.raycastTarget = false;
        var label = XunjuuInterfaceTheme.Label(root.transform, "Etiqueta", text, size.x > 400 ? 26 : 23,
            new Vector2(size.x - 126, size.y - 10), new Vector2(6, 0), XunjuuInterfaceTheme.Paper, TextAnchor.MiddleLeft);
        label.fontStyle = FontStyle.Bold;
        var next = XunjuuInterfaceTheme.Label(root.transform, "Indicador", "›", 32, new Vector2(24, 42), new Vector2(size.x * .5f - 26, 0), new Color(.90f, .88f, .75f));
        next.fontStyle = FontStyle.Normal;
        return button;
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class XunjuuMenuActionIcon : MaskableGraphic
{
    public string Action { get; set; }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear(); string action = (Action ?? "").ToUpperInvariant();
        if (action == "INICIAR" || action == "CONTINUAR")
        { Triangle(mesh, new Vector2(-5, -8), new Vector2(-5, 8), new Vector2(8, 0)); }
        else if (action == "LUDOTECA")
        { Box(mesh, -10, -7, 9, 14); Box(mesh, 1, -7, 9, 14); Line(mesh, new Vector2(0, -9), new Vector2(0, 8)); }
        else if (action == "AJUSTES")
        { for (int i = -1; i <= 1; i++) { float y = i * 7; Line(mesh, new Vector2(-10, y), new Vector2(10, y)); Box(mesh, i == 0 ? 3 : -6, y - 2, 3, 4); } }
        else if (action == "CONTROLES")
        { Box(mesh, -3, 3, 6, 6); Box(mesh, -3, -6, 6, 6); Box(mesh, -12, -6, 6, 6); Box(mesh, 6, -6, 6, 6); }
        else if (action == "REINICIAR")
        {
            Vector2 previous = new Vector2(8, 0);
            for (int i = 1; i <= 16; i++) { float a = i * Mathf.PI * 1.65f / 16; var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 8; Line(mesh, previous, p); previous = p; }
            Triangle(mesh, new Vector2(8, 2), new Vector2(3, -4), new Vector2(12, -4));
        }
        else if (action.Contains("INICIAL"))
        { Line(mesh, new Vector2(-11, 0), new Vector2(0, 9)); Line(mesh, new Vector2(0, 9), new Vector2(11, 0)); Box(mesh, -7, -9, 14, 10); }
        else
        { Line(mesh, new Vector2(-8, -9), new Vector2(-8, 9)); Line(mesh, new Vector2(-8, 9), new Vector2(1, 9)); Line(mesh, new Vector2(-8, -9), new Vector2(1, -9)); Line(mesh, new Vector2(-3, 0), new Vector2(11, 0)); Line(mesh, new Vector2(6, 5), new Vector2(11, 0)); Line(mesh, new Vector2(6, -5), new Vector2(11, 0)); }
    }
    void Box(VertexHelper mesh, float x, float y, float w, float h)
    {
        Vector2 a = new Vector2(x, y), b = new Vector2(x, y + h), c = new Vector2(x + w, y + h), d = new Vector2(x + w, y);
        Line(mesh, a, b); Line(mesh, b, c); Line(mesh, c, d); Line(mesh, d, a);
    }
    void Line(VertexHelper mesh, Vector2 a, Vector2 b)
    { Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * .8f; Triangle(mesh, a - n, a + n, b + n); Triangle(mesh, a - n, b + n, b - n); }
    void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c)
    { int start = mesh.currentVertCount; var v = UIVertex.simpleVert; v.color = color; v.position = a; mesh.AddVert(v); v.position = b; mesh.AddVert(v); v.position = c; mesh.AddVert(v); mesh.AddTriangle(start, start + 1, start + 2); }
}
