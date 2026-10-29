using UnityEngine;
using UnityEngine.UI;

// Orla de papel y flores: geometría ligera, sin duplicar el relleno translúcido.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class XunjuuPapelPicadoTrim : MaskableGraphic
{
    public bool IsButton { get; set; }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = GetPixelAdjustedRect();
        float inset = IsButton ? 6 : 12;
        float x = r.width * .5f - inset, y = r.height * .5f - inset;
        Line(mesh, new Vector2(-x, -y), new Vector2(x, -y), 1);
        Line(mesh, new Vector2(-x, y), new Vector2(x, y), 1);
        Line(mesh, new Vector2(-x, -y), new Vector2(-x, y), 1);
        Line(mesh, new Vector2(x, -y), new Vector2(x, y), 1);
        if (IsButton)
        {
            Flower(mesh, new Vector2(-x + 28, 0), 11);
            Flower(mesh, new Vector2(x - 28, 0), 11);
            for (int i = -5; i <= 5; i++)
            {
                float at = i * 25;
                Diamond(mesh, new Vector2(at, y - 6), 2);
                Diamond(mesh, new Vector2(at, -y + 6), 2);
            }
        }
        else
        {
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                    Flower(mesh, new Vector2(sx * (x - 17), sy * (y - 17)), 12);
            for (int i = -6; i <= 6; i++)
            {
                Diamond(mesh, new Vector2(i * 22, y - 7), 2);
                Diamond(mesh, new Vector2(i * 22, -y + 7), 2);
            }
        }
    }
    void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width)
    {
        Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
        Quad(mesh, a - normal, a + normal, b + normal, b - normal);
    }
    void Diamond(VertexHelper mesh, Vector2 p, float radius)
        => Quad(mesh, p + Vector2.up * radius, p + Vector2.right * radius, p + Vector2.down * radius, p + Vector2.left * radius);
    void Flower(VertexHelper mesh, Vector2 p, float radius)
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new Vector2(-direction.y, direction.x);
            Quad(mesh, p + direction * 3, p + direction * radius * .62f + side * 3,
                p + direction * radius, p + direction * radius * .62f - side * 3);
        }
        Diamond(mesh, p, 2);
    }
    void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int index = mesh.currentVertCount;
        Add(mesh, a); Add(mesh, b); Add(mesh, c); Add(mesh, d);
        mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index, index + 2, index + 3);
    }
    void Add(VertexHelper mesh, Vector2 point)
    {
        var vertex = UIVertex.simpleVert; vertex.position = point; vertex.color = color; mesh.AddVert(vertex);
    }
    public static XunjuuPapelPicadoTrim Attach(Transform parent, Color accent, bool button)
    {
        var root = new GameObject("Orla_Papel_Picado", typeof(RectTransform)); root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var trim = root.AddComponent<XunjuuPapelPicadoTrim>(); trim.IsButton = button; trim.color = accent; trim.raycastTarget = false;
        return trim;
    }
}
