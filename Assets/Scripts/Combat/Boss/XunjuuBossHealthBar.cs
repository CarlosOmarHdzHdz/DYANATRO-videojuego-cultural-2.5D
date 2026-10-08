using UnityEngine;
using UnityEngine.UI;

// ============================================================================
// Xunjuú v0.1 - Barra de vida de jefe
// Acción: generar y orientar una barra de vida legible sobre el prefab del jefe.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuBossHealthBar : MonoBehaviour
{
    private RectTransform fillRect;
    private Text titleText;
    private Text valueText;
    private Camera targetCamera;

    // ACCIÓN: crear una barra sin requerir Canvas, Slider ni referencias manuales.
    public static XunjuuBossHealthBar Create(Transform owner, string title, Vector3 localOffset)
    {
        GameObject root = new GameObject("BarraVida_Jefe");
        root.transform.SetParent(owner, false);
        root.transform.localPosition = localOffset;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 900;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(330f, 58f);
        // Xunjuu v0.1 - ACCION: el jefe puede duplicar su tamano sin que la
        // barra de vida también cubra el escenario completo.
        Vector3 ownerScale = owner.lossyScale;
        float largestScale = Mathf.Max(0.0001f, Mathf.Abs(ownerScale.x), Mathf.Abs(ownerScale.y), Mathf.Abs(ownerScale.z));
        canvasRect.localScale = Vector3.one * (0.012f / largestScale);

        XunjuuBossHealthBar bar = root.AddComponent<XunjuuBossHealthBar>();
        bar.CreateVisuals(title);
        return bar;
    }

    private void CreateVisuals(string title)
    {
        Image border = CreateImage("Marco", transform, new Color(0.08f, 0.055f, 0.06f, 0.96f));
        RectTransform borderRect = border.rectTransform;
        borderRect.anchorMin = new Vector2(0.5f, 0f);
        borderRect.anchorMax = new Vector2(0.5f, 0f);
        borderRect.pivot = new Vector2(0.5f, 0f);
        borderRect.anchoredPosition = new Vector2(0f, 0f);
        borderRect.sizeDelta = new Vector2(320f, 28f);

        Image background = CreateImage("Fondo", border.transform, new Color(0.22f, 0.1f, 0.11f, 1f));
        Stretch(background.rectTransform, new Vector2(5f, 5f), new Vector2(-5f, -5f));

        Image fill = CreateImage("Relleno", background.transform, new Color(0.84f, 0.12f, 0.12f, 1f));
        fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        fillRect.pivot = new Vector2(0f, 0.5f);

        GameObject titleObject = new GameObject("Nombre");
        titleObject.transform.SetParent(transform, false);
        titleText = titleObject.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontStyle = FontStyle.Bold;
        titleText.fontSize = 18;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.82f, 0.34f, 1f);
        titleText.text = title;
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(360f, 25f);

        GameObject valueObject = new GameObject("Vida_Numerica");
        valueObject.transform.SetParent(border.transform, false);
        valueText = valueObject.AddComponent<Text>();
        valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valueText.fontStyle = FontStyle.Bold;
        valueText.fontSize = 15;
        valueText.alignment = TextAnchor.MiddleCenter;
        valueText.color = Color.white;
        Stretch(valueText.rectTransform, Vector2.zero, Vector2.zero);
    }

    // ACCIÓN: actualizar el relleno rojo según el daño recibido por el jefe.
    public void SetHealth(int current, int maximum)
    {
        if (fillRect == null)
            return;

        float percentage = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f;
        fillRect.anchorMax = new Vector2(percentage, 1f);
        fillRect.offsetMax = new Vector2(percentage <= 0f ? 0f : -3f, -3f);
        if (valueText != null)
            valueText.text = $"{Mathf.Max(0, current)} / {Mathf.Max(1, maximum)}";
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.transform.position);
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }
}
