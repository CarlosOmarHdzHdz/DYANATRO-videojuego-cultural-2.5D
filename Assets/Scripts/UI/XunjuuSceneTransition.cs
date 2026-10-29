using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Loading presentation shared by startup, scene changes and intro preparation.</summary>
public sealed class XunjuuSceneTransition : MonoBehaviour
{
    private static XunjuuSceneTransition instance;
    private static readonly Color Gold = new Color(.91f, .70f, .34f);
    private static readonly Color Paper = new Color(.97f, .94f, .85f);
    private Canvas canvas;
    private CanvasGroup group;
    private RawImage backdrop;
    private RectTransform loadingCard;
    private RectTransform progressTrack;
    private RectTransform progressFill;
    private RectTransform progressHead;
    private RectTransform logo;
    private Text progressLabel;
    private Button skipButton;
    private Action skipAction;
    private Coroutine fade;
    private Coroutine loading;
    private bool loadingScene;
    private float worldProgressOffset;
    private float targetProgress;
    private float displayedProgress;
    private int lastPercent = -1;

    public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy;
    public float Progress => displayedProgress;
    // El estado sigue disponible para diagnóstico sin añadir letras pequeñas al arte.
    public string Status { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; }

    public static void ShowStartup()
    {
        var ui = Ensure();
        ui.worldProgressOffset = ui.loadingScene ? .35f : 0f;
        ui.Show("Reuniendo los elementos del territorio…", ui.worldProgressOffset, false, null);
    }

    public static void SetStartupStage(float value, string status)
    {
        var ui = Ensure();
        if (ui.loadingScene) return;
        ui.SetStatus(status);
        ui.SetProgress(Mathf.Lerp(ui.worldProgressOffset, 1f, Mathf.Clamp01(value)));
    }

    public static void ShowIntro(Action skip)
    {
        Ensure().Show("Preparando la introducción…", .12f, true, skip);
    }

    public static void UpdateIntroProgress(float value, string status)
    {
        var ui = Ensure();
        ui.SetStatus(status);
        ui.SetProgress(value);
    }

    public static void Dismiss()
    {
        if (instance != null) instance.BeginFade();
    }

    public static void LoadScene(int buildIndex, string sceneName)
    {
        var ui = Ensure();
        ui.Show("Abriendo el camino hacia la aldea…", .02f, false, null);
        ui.loadingScene = true;
        ui.loading = ui.StartCoroutine(ui.LoadSceneRoutine(buildIndex, sceneName));
    }

    private static XunjuuSceneTransition Ensure()
    {
        if (instance != null) return instance;
        var existing = FindFirstObjectByType<XunjuuSceneTransition>();
        if (existing != null) return existing;
        return new GameObject("Xunjuu_Transicion_Persistente").AddComponent<XunjuuSceneTransition>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Build();
    }

    private void Build()
    {
        var root = new GameObject("Canvas_Transicion", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        root.AddComponent<GraphicRaycaster>();

        var background = Rect("Paisaje_Carga", root.transform, Vector2.zero, Vector2.zero);
        Stretch(background, Vector2.zero);
        backdrop = background.gameObject.AddComponent<RawImage>();
        backdrop.texture = Resources.Load<Texture2D>("Sprites/UI/MenuHighlands_20261019");
        backdrop.color = backdrop.texture != null ? Color.white : new Color(.06f, .13f, .11f);
        backdrop.raycastTarget = false;
        var veil = Surface("Velo_De_Lectura", root.transform, Vector2.zero, Vector2.zero, new Color(.015f, .045f, .035f, .22f));
        Stretch(veil.rectTransform, Vector2.zero);
        veil.raycastTarget = true;

        AddBorder(root.transform, true);
        AddBorder(root.transform, false);

        logo = XunjuuBranding.Create(root.transform, new Vector2(1010, 340), Vector2.zero);
        logo.anchorMin = logo.anchorMax = new Vector2(.66f, .65f);

        var card = Surface("Tarjeta_Transicion", root.transform, new Vector2(1180, 156),
            Vector2.zero, new Color(.035f, .085f, .07f, .96f));
        loadingCard = card.rectTransform;
        loadingCard.anchorMin = loadingCard.anchorMax = new Vector2(.5f, .18f);
        ((XunjuuRoundedImage)card).CornerRadius = 18;
        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, .38f);
        outline.effectDistance = new Vector2(1, -1);
        var cardShadow = card.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0, .015f, .01f, .3f);
        cardShadow.effectDistance = new Vector2(0, -7);

        progressLabel = Label("Porcentaje", loadingCard, "", 30, new Vector2(.5f, .75f), new Vector2(160, 48), Paper);

        var track = Surface("Barra_Carga", loadingCard, new Vector2(1096, 12), new Vector2(0, -8),
            new Color(.015f, .04f, .03f));
        progressTrack = track.rectTransform;
        var fill = Surface("Progreso", progressTrack, Vector2.zero, Vector2.zero, Gold);
        progressFill = fill.rectTransform;
        progressFill.anchorMin = Vector2.zero;
        progressFill.anchorMax = new Vector2(0, 1);
        progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
        progressHead = Diamond(progressTrack, "Puntada_Actual", Vector2.zero, 14, Paper);

        var skip = XunjuuInterfaceTheme.Button(loadingCard, "Omitir", "CONTINUAR",
            new Vector2(400, 40), new Vector2(0, -48), () => skipAction?.Invoke());
        skipButton = skip;
        skipButton.gameObject.SetActive(false);

        group = root.AddComponent<CanvasGroup>();
        group.alpha = 0;
        root.SetActive(false);
    }

    private void Show(string status, float initial, bool allowSkip, Action onSkip)
    {
        if (fade != null) { StopCoroutine(fade); fade = null; }
        if (loading != null && !loadingScene) { StopCoroutine(loading); loading = null; }
        loadingScene = false;
        skipAction = onSkip;
        Status = status;
        targetProgress = displayedProgress = Mathf.Clamp01(initial);
        lastPercent = -1;
        skipButton.gameObject.SetActive(allowSkip);
        skipButton.interactable = allowSkip;
        canvas.gameObject.SetActive(true);
        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;
        UpdatePresentation();
    }

    private void SetStatus(string status)
    {
        if (IsVisible && !string.IsNullOrWhiteSpace(status)) Status = status;
    }

    private void SetProgress(float value)
    {
        targetProgress = Mathf.Max(targetProgress, Mathf.Clamp01(value));
    }

    private void Update()
    {
        if (!IsVisible) return;
        // Never invent progress. Ease only toward the most recent completed stage.
        displayedProgress = XunjuuPresentationPreferences.ReducedMotion ? targetProgress :
            Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * .95f);
        UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        progressFill.anchorMax = new Vector2(displayedProgress, 1);
        progressFill.gameObject.SetActive(displayedProgress > .001f);
        progressHead.anchorMin = progressHead.anchorMax = new Vector2(displayedProgress, .5f);
        progressHead.anchoredPosition = Vector2.zero;
        int percent = Mathf.FloorToInt(displayedProgress * 100f + .01f);
        if (percent != lastPercent) { progressLabel.text = percent + "%"; lastPercent = percent; }
        if (backdrop.texture != null)
        {
            float aspect = backdrop.rectTransform.rect.width / Mathf.Max(1, backdrop.rectTransform.rect.height);
            float source = backdrop.texture.width / (float)backdrop.texture.height;
            float w = Mathf.Min(1, aspect / source), h = Mathf.Min(1, source / aspect);
            backdrop.uvRect = new Rect((1-w)*.5f, (1-h)*.5f, w, h);
        }
        bool reduced = XunjuuPresentationPreferences.ReducedMotion;
        logo.localScale = Vector3.one * (reduced ? 1 : 1 + Mathf.Sin(Time.unscaledTime * 1.1f) * .004f);
        progressHead.localRotation = Quaternion.Euler(0, 0, reduced ? 45 : 45 + Mathf.Sin(Time.unscaledTime * 2) * 8);
    }

    private IEnumerator LoadSceneRoutine(int buildIndex, string sceneName)
    {
        // Allow the loading view to render before scene loading starts.
        yield return null;
        AsyncOperation operation = buildIndex >= 0 ? SceneManager.LoadSceneAsync(buildIndex) : SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError("No se pudo cargar la escena '" + sceneName + "'.");
            loadingScene = false;
            BeginFade();
            yield break;
        }
        operation.allowSceneActivation = false;
        while (operation.progress < .9f)
        {
            SetStatus("Abriendo el escenario y sus caminos…");
            SetProgress(Mathf.Lerp(.02f, .35f, operation.progress / .9f));
            yield return null;
        }
        SetProgress(.35f);
        yield return null;
        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
        loading = null;
        // The new director reports the remaining world-generation stages.
    }

    private void BeginFade()
    {
        if (!IsVisible || fade != null) return;
        targetProgress = 1f;
        fade = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        group.interactable = false;
        float catchUp = 0;
        while (displayedProgress < .999f && catchUp < .35f)
        {
            catchUp += Time.unscaledDeltaTime;
            displayedProgress = Mathf.MoveTowards(displayedProgress, 1, Time.unscaledDeltaTime * 3);
            yield return null;
        }
        displayedProgress = 1;
        UpdatePresentation();
        yield return null;
        float elapsed = 0, start = group.alpha;
        float duration = XunjuuPresentationPreferences.ReducedMotion ? .12f : .65f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, 0, Mathf.SmoothStep(0, 1, elapsed / duration));
            yield return null;
        }
        group.alpha = 0;
        group.blocksRaycasts = false;
        canvas.gameObject.SetActive(false);
        fade = null;
        loadingScene = false;
        skipAction = null;
    }

    private void AddBorder(Transform parent, bool top)
    {
        var border = Surface(top ? "Bordado_Superior" : "Bordado_Inferior", parent,
            new Vector2(0, 42), Vector2.zero, new Color(.025f, .065f, .05f, .8f));
        border.rectTransform.anchorMin = new Vector2(0, top ? 1 : 0);
        border.rectTransform.anchorMax = new Vector2(1, top ? 1 : 0);
        border.rectTransform.pivot = new Vector2(.5f, top ? 1 : 0);
        Color[] colors = { Gold, new Color(.74f,.28f,.40f), new Color(.35f,.65f,.52f), new Color(.43f,.65f,.77f) };
        for (int i = 0; i < 17; i++)
        {
            var stitch = Diamond(border.transform, "Puntada", Vector2.zero, 6, colors[i % colors.Length]);
            stitch.anchorMin = stitch.anchorMax = new Vector2((i + 1f) / 18f, .5f);
        }
    }

    private static RectTransform Diamond(Transform parent, string name, Vector2 position, float size, Color color)
    {
        var image = Surface(name, parent, new Vector2(size, size), position, color);
        ((XunjuuRoundedImage)image).CornerRadius = 0;
        image.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        return image.rectTransform;
    }

    private static Image Surface(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        return XunjuuInterfaceTheme.Panel(parent, name, size, position, color).GetComponent<Image>();
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchorMin = rect.anchorMax = anchor;
        return rect;
    }

    private static void Stretch(RectTransform rect, Vector2 inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
    }

    private static Text Label(string name, Transform parent, string value, int size, Vector2 anchor, Vector2 dimensions, Color color)
    {
        var rect = Rect(name, parent, anchor, dimensions);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = XunjuuInterfaceTheme.Font;
        text.fontSize = size;
        text.text = value;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
