using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class XunjuuDeathScreenStyle
{
    public const string TitlePath = "Sprites/UI/DeathTitle_PapelPicado_20261027";
    static Font DisplayFont => XunjuuTypography.Regular;

    public static void Title(Transform parent)
    {
        var title = Resources.Load<Texture2D>(TitlePath);
        if (title != null)
        {
            var root = new GameObject("Titulo_Papel_Picado", typeof(RectTransform), typeof(RawImage)); root.transform.SetParent(parent, false);
            float width = Mathf.Min(790, 146 * title.width / (float)title.height);
            XunjuuInterfaceTheme.Rect(root, new Vector2(width, width * title.height / title.width), new Vector2(0, 30));
            var image = root.GetComponent<RawImage>(); image.texture = title; image.raycastTarget = false;
        }
        else
        {
            var label = XunjuuInterfaceTheme.Label(parent, "Titulo_Papel_Picado", "HAS CAÍDO", 76, new Vector2(850, 130), new Vector2(0, 30), new Color(1, .73f, .3f));
            if (DisplayFont != null) label.font = DisplayFont;
        }
    }

    public static Button Button(Transform parent, string name, string value, Vector2 position, UnityAction action, bool retry)
    {
        Color accent = retry ? new Color(1, .70f, .27f) : new Color(1, .56f, .78f);
        Color fill = retry ? new Color(.32f, .12f, .025f, .94f) : new Color(.22f, .045f, .26f, .94f);
        var root = XunjuuInterfaceTheme.Panel(parent, name, new Vector2(470, 84), position, fill);
        root.GetComponent<XunjuuRoundedImage>().CornerRadius = 5;
        root.GetComponent<Image>().raycastTarget = true;
        var button = root.AddComponent<Button>(); button.targetGraphic = root.GetComponent<Image>(); button.onClick.AddListener(action);
        var palette = button.colors; palette.normalColor = Color.white;
        palette.highlightedColor = palette.selectedColor = new Color(1.55f, 1.55f, 1.55f);
        palette.pressedColor = new Color(.72f, .72f, .72f); palette.fadeDuration = .12f; palette.colorMultiplier = 1;
        button.colors = palette; root.AddComponent<XunjuuButtonMotion>();
        XunjuuPapelPicadoTrim.Attach(root.transform, accent, true);
        var label = XunjuuInterfaceTheme.Label(root.transform, "Etiqueta", value, 32, new Vector2(330, 52), Vector2.zero, new Color(1, .95f, .84f));
        if (DisplayFont != null) label.font = DisplayFont;
        var shadow = label.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.03f, .01f, .04f, .8f); shadow.effectDistance = new Vector2(0, -2);
        return button;
    }
}
