using UnityEngine;
using UnityEngine.UI;

// Ajustes compartidos por inicio y pausa, aplicados al instante.
public sealed class XunjuuPresentationSettings : MonoBehaviour
{
    private Slider volume;
    private Text percentage, motionState;
    private Image switchTrack;
    private RectTransform switchKnob;
    public bool IsOpen => gameObject.activeSelf;

    public static XunjuuPresentationSettings Create(Transform parent, DyanatroGameDirector owner)
    {
        var root = XunjuuInterfaceTheme.Panel(parent, "Ajustes_Presentacion", Vector2.zero, Vector2.zero, new Color(.01f, .025f, .035f, .74f));
        var rect = root.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().raycastTarget = true;
        var settings = root.AddComponent<XunjuuPresentationSettings>();
        var box = XunjuuModalStyle.Card(root.transform, "Marco_Ajustes", new Vector2(1080, 770), Vector2.zero);
        Label(box.transform, "SobreTitulo", "PERSONALIZA TU RECORRIDO", 15, new Vector2(800, 28), new Vector2(-76, 326), XunjuuModalStyle.Accent, TextAnchor.MiddleLeft);
        Label(box.transform, "Titulo", "A tu ritmo", 42, new Vector2(800, 58), new Vector2(-76, 278), XunjuuInterfaceTheme.Paper, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        Label(box.transform, "Subtitulo", "Sonido y comodidad visual", 22, new Vector2(800, 34), new Vector2(-76, 230), XunjuuModalStyle.Quiet, TextAnchor.MiddleLeft);
        var dismiss = XunjuuModalStyle.Action(box.transform, "Cerrar_Ajustes_Icono", "×", new Vector2(46, 46), new Vector2(478, 306), settings.Close, XunjuuModalStyle.Quiet);
        dismiss.GetComponentInChildren<Text>().fontSize = 30;
        XunjuuModalStyle.Rule(box.transform, 194, 952);

        var sound = Section(box.transform, "Tarjeta_Sonido", new Vector2(-244, -5));
        Label(sound.transform, "Categoria", "MÚSICA", 15, new Vector2(400, 26), new Vector2(0, 126), XunjuuModalStyle.Accent, TextAnchor.MiddleLeft);
        Label(sound.transform, "Musica", "Volumen del recorrido", 25, new Vector2(400, 40), new Vector2(0, 84), XunjuuInterfaceTheme.Paper, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        settings.percentage = Label(sound.transform, "Porcentaje", "", 32, new Vector2(400, 44), new Vector2(0, 30), XunjuuInterfaceTheme.Paper);
        settings.volume = MakeSlider(sound.transform);
        var less = XunjuuModalStyle.Action(sound.transform, "Bajar_Volumen", "−", new Vector2(36, 36), new Vector2(-197, -27), () => settings.volume.value -= .1f, XunjuuModalStyle.Quiet);
        var more = XunjuuModalStyle.Action(sound.transform, "Subir_Volumen", "+", new Vector2(36, 36), new Vector2(197, -27), () => settings.volume.value += .1f, XunjuuModalStyle.Accent);
        Label(sound.transform, "Minimo", "0 %", 15, new Vector2(70, 24), new Vector2(-137, -65), XunjuuModalStyle.Quiet, TextAnchor.MiddleLeft);
        Label(sound.transform, "Maximo", "100 %", 15, new Vector2(70, 24), new Vector2(137, -65), XunjuuModalStyle.Quiet, TextAnchor.MiddleRight);
        Label(sound.transform, "Ayuda_Sonido", "Solo ajusta la música.\nLas voces y los efectos se conservan.", 19, new Vector2(400, 54), new Vector2(0, -111), XunjuuModalStyle.Quiet, TextAnchor.MiddleLeft);
        Label(sound.transform, "Estado_Sonido", "En inicio y pausa, la música se detiene.", 16, new Vector2(400, 25), new Vector2(0, -148), XunjuuModalStyle.Quiet, TextAnchor.MiddleLeft);

        var comfort = Section(box.transform, "Tarjeta_Movimiento", new Vector2(244, -5));
        Label(comfort.transform, "Categoria", "PRESENTACIÓN", 15, new Vector2(400, 26), new Vector2(0, 126), XunjuuModalStyle.Accent, TextAnchor.MiddleLeft);
        Label(comfort.transform, "Movimiento", "Movimiento de menús", 25, new Vector2(400, 40), new Vector2(0, 84), XunjuuInterfaceTheme.Paper, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        var preview = XunjuuInterfaceTheme.Panel(comfort.transform, "Vista_Previa", new Vector2(400, 82), new Vector2(0, 20), XunjuuModalStyle.Surface);
        preview.AddComponent<RectMask2D>(); XunjuuMenuBackdrop.Attach(preview.transform, false);
        var reduce = XunjuuModalStyle.Action(comfort.transform, "Movimiento_Menus", "Movimiento reducido", new Vector2(400, 50), new Vector2(0, -57), settings.ToggleMotion, XunjuuModalStyle.Accent);
        var reduceLabel = reduce.GetComponentInChildren<Text>(); reduceLabel.fontSize = 20;
        reduceLabel.alignment = TextAnchor.MiddleLeft; reduceLabel.rectTransform.sizeDelta = new Vector2(290, 42); reduceLabel.rectTransform.anchoredPosition = new Vector2(-37, 0);
        var track = XunjuuInterfaceTheme.Panel(reduce.transform, "Interruptor", new Vector2(56, 28), new Vector2(153, 0), XunjuuModalStyle.Inset); settings.switchTrack = track.GetComponent<Image>();
        var knob = XunjuuInterfaceTheme.Panel(track.transform, "Indicador", new Vector2(20, 20), Vector2.zero, XunjuuInterfaceTheme.Paper); settings.switchKnob = knob.GetComponent<RectTransform>();
        Label(comfort.transform, "Ayuda_Movimiento", "Reduce el movimiento del fondo\ny los efectos de los botones.", 19, new Vector2(400, 54), new Vector2(0, -111), XunjuuModalStyle.Quiet, TextAnchor.MiddleLeft);
        settings.motionState = Label(comfort.transform, "Estado_Movimiento", "", 16, new Vector2(400, 25), new Vector2(0, -148), XunjuuModalStyle.Accent, TextAnchor.MiddleLeft);

        XunjuuModalStyle.Rule(box.transform, -202, 952);
        Label(box.transform, "Ayuda_Guardado", "Los cambios se aplican al instante.", 20, new Vector2(580, 36), new Vector2(-186, -251), XunjuuInterfaceTheme.Paper, TextAnchor.MiddleLeft);
        Label(box.transform, "Ayuda_Teclado", "TAB / FLECHAS  navegar    ·    ESC  volver", 16, new Vector2(580, 30), new Vector2(-186, -289), XunjuuModalStyle.Quiet, TextAnchor.MiddleLeft);
        var close = XunjuuModalStyle.Action(box.transform, "Cerrar_Ajustes", "Listo, volver", new Vector2(270, 58), new Vector2(337, -266), settings.Close, XunjuuModalStyle.Accent);
        settings.volume.onValueChanged.AddListener(value => { XunjuuPresentationPreferences.SetMusicVolume(value); settings.Refresh(); });
        XunjuuMenuNavigation.Configure(root, owner, 20, settings.volume, less, more, reduce, close, dismiss);
        root.SetActive(false); return settings;
    }
    static Text Label(Transform parent, string name, string value, int size, Vector2 dimensions, Vector2 at, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        => XunjuuInterfaceTheme.Label(parent, name, value, size, dimensions, at, color, align);
    static GameObject Section(Transform parent, string name, Vector2 at)
    {
        var section = XunjuuInterfaceTheme.Panel(parent, name, new Vector2(460, 335), at, XunjuuModalStyle.Inset);
        section.GetComponent<XunjuuRoundedImage>().CornerRadius = 16; return section;
    }
    static Slider MakeSlider(Transform parent)
    {
        var root = new GameObject("Volumen_Musica", typeof(RectTransform), typeof(Slider)); root.transform.SetParent(parent, false);
        XunjuuInterfaceTheme.Rect(root, new Vector2(336, 50), new Vector2(0, -27));
        var hit = root.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
        var slider = root.GetComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
        XunjuuInterfaceTheme.Panel(root.transform, "Pista", new Vector2(320, 8), Vector2.zero, new Color(.14f, .24f, .25f));
        var fillArea = new GameObject("Zona_Relleno", typeof(RectTransform)); fillArea.transform.SetParent(root.transform, false); XunjuuInterfaceTheme.Rect(fillArea, new Vector2(320, 8), Vector2.zero);
        var fill = XunjuuInterfaceTheme.Panel(fillArea.transform, "Relleno", Vector2.zero, Vector2.zero, XunjuuModalStyle.Accent);
        var fillRect = fill.GetComponent<RectTransform>(); fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        var handleArea = new GameObject("Zona_Control", typeof(RectTransform)); handleArea.transform.SetParent(root.transform, false); XunjuuInterfaceTheme.Rect(handleArea, new Vector2(320, 26), Vector2.zero);
        var handle = XunjuuInterfaceTheme.Panel(handleArea.transform, "Control", new Vector2(26, 0), Vector2.zero, XunjuuInterfaceTheme.Paper);
        handle.GetComponent<Image>().raycastTarget = true; slider.fillRect = fillRect; slider.handleRect = handle.GetComponent<RectTransform>(); slider.targetGraphic = handle.GetComponent<Image>();
        var colors = slider.colors; colors.selectedColor = colors.highlightedColor = new Color(1, .91f, .61f); colors.pressedColor = XunjuuModalStyle.Accent; colors.fadeDuration = .12f; slider.colors = colors;
        return slider;
    }
    public void Open() { Refresh(); gameObject.SetActive(true); transform.SetAsLastSibling(); }
    public void Close() { XunjuuPresentationPreferences.Save(); gameObject.SetActive(false); }
    void ToggleMotion() { XunjuuPresentationPreferences.SetReducedMotion(!XunjuuPresentationPreferences.ReducedMotion); Refresh(); }
    void Refresh()
    {
        volume.SetValueWithoutNotify(XunjuuPresentationPreferences.MusicVolume);
        percentage.text = Mathf.RoundToInt(volume.value * 100) + " %";
        bool reduced = XunjuuPresentationPreferences.ReducedMotion;
        motionState.text = reduced ? "Activado · presentación más estable" : "Desactivado · presentación con movimiento";
        switchTrack.color = reduced ? new Color(.16f, .57f, .49f) : new Color(.14f, .24f, .25f);
        switchKnob.anchoredPosition = new Vector2(reduced ? 13 : -13, 0);
    }
}
