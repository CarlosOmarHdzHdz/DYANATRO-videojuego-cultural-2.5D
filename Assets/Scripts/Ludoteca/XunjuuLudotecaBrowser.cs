using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================================
// Xunjuú v0.1 - Navegador interno de la ludoteca
// ACCION: mostrar el demo y registrar la evaluacion sin salir del videojuego.
// COMPATIBILIDAD: utiliza solamente uGUI y persistentDataPath de Unity para
// funcionar tanto en Windows como en Android.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuLudotecaBrowser : MonoBehaviour
{
    private enum Page { Home, Culture, Gallery, Language, MiniGame, Survey }

    [Serializable]
    private sealed class SurveyRecord
    {
        public string project = "Xunjuu";
        public string createdAt;
        public int[] answers;
    }

    private readonly string[] surveyQuestions =
    {
        "La ludoteca fue facil de comprender.",
        "El diseño visual representa adecuadamente la cultura mazahua.",
        "La informacion cultural fue clara y comprensible.",
        "La galeria ayudo a reconocer elementos de la cultura mazahua.",
        "Las palabras en mazahua despertaron interes por conocer la lengua.",
        "El minijuego reforzo el aprendizaje cultural.",
        "La navegacion entre las secciones fue sencilla.",
        "La ludoteca complementa correctamente la historia del videojuego.",
        "Los videojuegos son utiles para difundir culturas originarias.",
        "La ludoteca cumplio con su objetivo educativo."
    };

    private readonly string[] miniGameTerms =
    {
        "jñiñi|pueblo|maiz|bosque",
        "ndechjö|maiz|flor|venado",
        "ndajma|flor|pato|escuela",
        "lala|pato|pueblo|corazon",
        "pjant'eje|venado|maiz|libro"
    };

    private Canvas hostCanvas;
    private GameObject browserRoot;
    private GameObject galleryRoot;
    private GameObject miniGameRoot;
    private GameObject surveyRoot;
    private TMP_Text pageTitle;
    private TMP_Text pageBody;
    private TMP_Text addressText;
    private TMP_Text statusText;
    private TMP_Text miniGameText;
    private TMP_Text surveyText;
    private readonly Stack<Page> history = new Stack<Page>();
    private readonly int[] surveyAnswers = new int[10];
    private Page currentPage;
    private int miniGameIndex;
    private int miniGameScore;
    private int surveyIndex;
    private int closedFrame = -10;
    private float previousTimeScale = 1f;
    private bool initialized;

    public bool IsOpen => browserRoot != null && browserRoot.activeSelf;
    public bool BlocksPauseInput => IsOpen || Time.frameCount <= closedFrame + 1;

    // Xunjuú v0.1 - ACCION: preparar el navegador con recursos ya existentes.
    public void Initialize(Canvas canvas, Sprite tree, Sprite flower, Sprite cloud)
    {
        if (initialized || canvas == null)
            return;

        hostCanvas = canvas;
        BuildBrowser(tree, flower, cloud);
        initialized = true;
    }

    public void Open()
    {
        if (!initialized)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        browserRoot.SetActive(true);
        browserRoot.transform.SetAsLastSibling();
        history.Clear();
        ShowPage(Page.Home, false);
    }

    public void Close()
    {
        if (browserRoot == null)
            return;
        browserRoot.SetActive(false);
        Time.timeScale = previousTimeScale;
        closedFrame = Time.frameCount;
    }

    private void Update()
    {
        // En Android el boton fisico Atras produce Escape dentro de Unity.
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // ========================================================================
    // Xunjuú v0.1 - Ventana interna
    // ACCION: construir una interfaz adaptable con barra, direccion y secciones.
    // ========================================================================
    private void BuildBrowser(Sprite tree, Sprite flower, Sprite cloud)
    {
        browserRoot = Panel(hostCanvas.transform, "Navegador_Ludoteca", new Color(0.02f, 0.035f, 0.03f, 0.99f));
        Canvas browserCanvas = browserRoot.AddComponent<Canvas>();
        browserCanvas.overrideSorting = true;
        browserCanvas.sortingOrder = 10000;
        browserRoot.AddComponent<GraphicRaycaster>();

        Sprite clothingPhoto = LoadPhoto("Ludoteca/vestimenta_mazahua");
        Sprite dancePhoto = LoadPhoto("Ludoteca/danza_pastoras");
        Sprite milpaPhoto = LoadPhoto("Ludoteca/milpa_mexico");

        GameObject window = Panel(browserRoot.transform, "Ventana", new Color(0.07f, 0.10f, 0.08f, 1f));
        SetRect(window.GetComponent<RectTransform>(), new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.965f));

        GameObject top = Panel(window.transform, "Barra_Navegador", new Color(0.12f, 0.17f, 0.14f, 1f));
        SetRect(top.GetComponent<RectTransform>(), new Vector2(0f, 0.90f), Vector2.one);
        Button(top.transform, "<", new Vector2(0.035f, 0.5f), new Vector2(66f, 54f), Back, new Color(0.22f, 0.31f, 0.25f));
        Button(top.transform, "Inicio", new Vector2(0.105f, 0.5f), new Vector2(120f, 54f), () => ShowPage(Page.Home), new Color(0.22f, 0.31f, 0.25f));
        Button(top.transform, "X", new Vector2(0.96f, 0.5f), new Vector2(62f, 54f), Close, new Color(0.54f, 0.16f, 0.13f));

        GameObject address = Panel(top.transform, "Direccion", new Color(0.035f, 0.055f, 0.045f, 1f));
        SetRect(address.GetComponent<RectTransform>(), new Vector2(0.18f, 0.20f), new Vector2(0.86f, 0.80f));
        addressText = Text(address.transform, "xunjuu://ludoteca/inicio", 25, TextAnchor.MiddleLeft, new Color(0.76f, 0.88f, 0.78f));
        addressText.rectTransform.offsetMin = new Vector2(20f, 0f);

        GameObject nav = Panel(window.transform, "Navegacion", new Color(0.055f, 0.08f, 0.065f, 1f));
        SetRect(nav.GetComponent<RectTransform>(), new Vector2(0f, 0.055f), new Vector2(0.21f, 0.90f));
        TextBlock(nav.transform, "LUDOTECA\nDIGITAL", 34, new Vector2(0.5f, 0.90f), new Vector2(300f, 110f), TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.24f));
        NavButton(nav.transform, "Cultura", 0.74f, Page.Culture);
        NavButton(nav.transform, "Galeria", 0.62f, Page.Gallery);
        NavButton(nav.transform, "Lengua mazahua", 0.50f, Page.Language);
        NavButton(nav.transform, "Minijuego", 0.38f, Page.MiniGame);
        NavButton(nav.transform, "Evaluacion", 0.26f, Page.Survey);

        GameObject content = Panel(window.transform, "Contenido", new Color(0.93f, 0.94f, 0.89f, 1f));
        SetRect(content.GetComponent<RectTransform>(), new Vector2(0.21f, 0.055f), new Vector2(1f, 0.90f));
        pageTitle = TextBlock(content.transform, string.Empty, 46, new Vector2(0.5f, 0.91f), new Vector2(1260f, 90f), TextAnchor.MiddleLeft, new Color(0.10f, 0.25f, 0.15f));
        pageBody = TextBlock(content.transform, string.Empty, 30, new Vector2(0.5f, 0.50f), new Vector2(1260f, 650f), TextAnchor.UpperLeft, new Color(0.11f, 0.14f, 0.12f));
        pageBody.lineSpacing = 1.1f;

        galleryRoot = BuildGallery(content.transform,
            clothingPhoto != null ? clothingPhoto : tree,
            dancePhoto != null ? dancePhoto : flower,
            milpaPhoto != null ? milpaPhoto : cloud);
        miniGameRoot = BuildMiniGame(content.transform);
        surveyRoot = BuildSurvey(content.transform);

        GameObject footer = Panel(window.transform, "Estado", new Color(0.09f, 0.13f, 0.105f, 1f));
        SetRect(footer.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0.055f));
        statusText = Text(footer.transform, "Demo local - compatible con Windows y Android", 20, TextAnchor.MiddleLeft, new Color(0.72f, 0.82f, 0.74f));
        statusText.rectTransform.offsetMin = new Vector2(20f, 0f);
        browserRoot.SetActive(false);
    }

    private void NavButton(Transform parent, string label, float y, Page page)
    {
        Button(parent, label, new Vector2(0.5f, y), new Vector2(285f, 68f), () => ShowPage(page), new Color(0.16f, 0.28f, 0.19f));
    }

    private GameObject BuildGallery(Transform parent, Sprite tree, Sprite flower, Sprite cloud)
    {
        GameObject root = new GameObject("Galeria_Multimedia", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.78f));
        GalleryCard(root.transform, "VESTIMENTA\nQuexquemetl mazahua", tree, new Vector2(0.18f, 0.52f), new Color(0.70f, 0.78f, 0.30f));
        GalleryCard(root.transform, "TRADICION\nDanza de las Pastoras", flower, new Vector2(0.50f, 0.52f), new Color(0.26f, 0.52f, 0.30f));
        GalleryCard(root.transform, "TERRITORIO\nCampos de maiz", cloud, new Vector2(0.82f, 0.52f), new Color(0.42f, 0.68f, 0.78f));
        return root;
    }

    private void GalleryCard(Transform parent, string label, Sprite sprite, Vector2 anchor, Color fallback)
    {
        GameObject card = Panel(parent, "Ficha_" + label, new Color(0.10f, 0.16f, 0.12f, 1f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = anchor;
        cardRect.anchorMax = anchor;
        cardRect.sizeDelta = new Vector2(350f, 390f);
        cardRect.anchoredPosition = Vector2.zero;

        GameObject visual = new GameObject("Imagen", typeof(RectTransform), typeof(Image));
        visual.transform.SetParent(card.transform, false);
        SetRect(visual.GetComponent<RectTransform>(), new Vector2(0.10f, 0.24f), new Vector2(0.90f, 0.92f));
        Image image = visual.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = sprite != null ? Color.white : fallback;
        TextBlock(card.transform, label, 24, new Vector2(0.5f, 0.12f), new Vector2(325f, 78f), TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.34f));
    }

    // Xunjuú v0.1 - ACCION: navegar sin cargar escenas o aplicaciones externas.
    private void ShowPage(Page page, bool remember = true)
    {
        if (remember && page != currentPage)
            history.Push(currentPage);
        currentPage = page;
        galleryRoot.SetActive(page == Page.Home || page == Page.Gallery);
        miniGameRoot.SetActive(page == Page.MiniGame);
        surveyRoot.SetActive(page == Page.Survey);
        pageBody.gameObject.SetActive(page != Page.Gallery && page != Page.MiniGame && page != Page.Survey);
        pageBody.rectTransform.sizeDelta = page == Page.Home ? new Vector2(1260f, 170f) : new Vector2(1260f, 650f);
        pageBody.rectTransform.anchoredPosition = page == Page.Home ? new Vector2(0f, 155f) : Vector2.zero;

        switch (page)
        {
            case Page.Home:
                SetPage("Explora la cultura mazahua", "inicio", "Consulta historias, imagenes y palabras; despues pon a prueba lo aprendido.\nSelecciona una seccion del menu lateral. Todo permanece dentro del videojuego.");
                break;
            case Page.Culture:
                SetPage("Cultura mazahua", "cultura", "VESTIMENTA\nLa indumentaria es una expresion de identidad comunitaria.\n\nGASTRONOMIA\nLa milpa y el maiz forman parte del entorno representado en el juego.\n\nFESTIVIDADES Y TRADICIONES\nSe presentaran celebraciones, memoria oral y practicas comunitarias con fuentes documentales.\n\nEl contenido definitivo debera ser revisado con fuentes academicas y, cuando sea posible, con integrantes o especialistas de la comunidad mazahua.");
                break;
            case Page.Gallery:
                SetPage("Galeria multimedia", "galeria", string.Empty);
                statusText.text = "Imagenes: Wikimedia Commons - licencias CC BY-SA";
                break;
            case Page.Language:
                SetPage("Lengua mazahua", "lengua", "VOCABULARIO DEMOSTRATIVO\n\njñiñi - pueblo\nndechjö - maiz\nmúbú - corazon / vida\nnu t'eje - el bosque\nndajma - flor\nlala - pato\npjanteje - venado\n\nAntes de publicar se deberan validar escritura, variante regional, contexto y pronunciacion con una fuente especializada.");
                break;
            case Page.MiniGame:
                SetPage("Minijuego: relaciona la palabra", "minijuego", string.Empty);
                RefreshMiniGame();
                break;
            case Page.Survey:
                SetPage("Evaluacion de la experiencia", "evaluacion", string.Empty);
                RefreshSurvey();
                break;
        }
    }

    private void SetPage(string title, string route, string body)
    {
        pageTitle.text = title;
        addressText.text = "xunjuu://ludoteca/" + route;
        pageBody.text = body;
        statusText.text = "Demo local - compatible con Windows y Android";
    }

    private void Back()
    {
        ShowPage(history.Count > 0 ? history.Pop() : Page.Home, false);
    }

    // ========================================================================
    // Xunjuú v0.1 - Minijuego
    // ACCION: relacionar cinco palabras con su significado.
    // ========================================================================
    private GameObject BuildMiniGame(Transform parent)
    {
        GameObject root = new GameObject("Minijuego", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.78f));
        miniGameText = TextBlock(root.transform, string.Empty, 36, new Vector2(0.5f, 0.74f), new Vector2(1080f, 210f), TextAnchor.MiddleCenter, new Color(0.10f, 0.24f, 0.14f));
        Button(root.transform, "Opcion A", new Vector2(0.20f, 0.28f), new Vector2(290f, 72f), () => AnswerMiniGame(0), new Color(0.18f, 0.42f, 0.24f));
        Button(root.transform, "Opcion B", new Vector2(0.50f, 0.28f), new Vector2(290f, 72f), () => AnswerMiniGame(1), new Color(0.18f, 0.42f, 0.24f));
        Button(root.transform, "Opcion C", new Vector2(0.80f, 0.28f), new Vector2(290f, 72f), () => AnswerMiniGame(2), new Color(0.18f, 0.42f, 0.24f));
        return root;
    }

    private void RefreshMiniGame()
    {
        if (miniGameIndex >= miniGameTerms.Length)
        {
            miniGameText.text = "Actividad terminada\nResultado: " + miniGameScore + " de " + miniGameTerms.Length + "\nToca cualquier opcion para reiniciar.";
            SetMiniGameLabels("Reiniciar", "Reiniciar", "Reiniciar");
            return;
        }
        string[] data = miniGameTerms[miniGameIndex].Split('|');
        miniGameText.text = "¿Que significa \"" + data[0] + "\"?\nPregunta " + (miniGameIndex + 1) + " de " + miniGameTerms.Length + "   Aciertos: " + miniGameScore;
        SetMiniGameLabels(data[1], data[2], data[3]);
    }

    private void AnswerMiniGame(int option)
    {
        if (miniGameIndex >= miniGameTerms.Length)
        {
            miniGameIndex = 0;
            miniGameScore = 0;
        }
        else
        {
            if (option == 0) miniGameScore++;
            miniGameIndex++;
        }
        RefreshMiniGame();
    }

    private void SetMiniGameLabels(string first, string second, string third)
    {
        string[] labels = { first, second, third };
        Button[] buttons = miniGameRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length && i < labels.Length; i++)
            buttons[i].GetComponentInChildren<TMP_Text>().text = labels[i];
    }

    // ========================================================================
    // Xunjuú v0.1 - Evaluacion
    // ACCION: guardar diez respuestas Likert localmente y sin recopilar identidad.
    // ========================================================================
    private GameObject BuildSurvey(Transform parent)
    {
        GameObject root = new GameObject("Evaluacion", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.80f));
        surveyText = TextBlock(root.transform, string.Empty, 34, new Vector2(0.5f, 0.74f), new Vector2(1160f, 240f), TextAnchor.MiddleCenter, new Color(0.10f, 0.23f, 0.14f));
        Button(root.transform, "Totalmente de acuerdo", new Vector2(0.25f, 0.38f), new Vector2(350f, 70f), () => AnswerSurvey(4), new Color(0.16f, 0.42f, 0.23f));
        Button(root.transform, "De acuerdo", new Vector2(0.75f, 0.38f), new Vector2(320f, 70f), () => AnswerSurvey(3), new Color(0.30f, 0.48f, 0.22f));
        Button(root.transform, "En desacuerdo", new Vector2(0.25f, 0.18f), new Vector2(350f, 70f), () => AnswerSurvey(2), new Color(0.58f, 0.40f, 0.14f));
        Button(root.transform, "Totalmente en desacuerdo", new Vector2(0.75f, 0.18f), new Vector2(390f, 70f), () => AnswerSurvey(1), new Color(0.58f, 0.23f, 0.14f));
        Button(root.transform, "Anterior", new Vector2(0.12f, 0.04f), new Vector2(190f, 54f), PreviousSurvey, new Color(0.22f, 0.28f, 0.24f));
        return root;
    }

    private void AnswerSurvey(int answer)
    {
        surveyAnswers[surveyIndex] = answer;
        if (surveyIndex < surveyQuestions.Length - 1)
        {
            surveyIndex++;
            RefreshSurvey();
        }
        else
        {
            SaveSurvey();
        }
    }

    private void PreviousSurvey()
    {
        surveyIndex = Mathf.Max(0, surveyIndex - 1);
        RefreshSurvey();
    }

    private void RefreshSurvey()
    {
        surveyText.text = "Pregunta " + (surveyIndex + 1) + " de " + surveyQuestions.Length + "\n\n" + surveyQuestions[surveyIndex]
            + (surveyAnswers[surveyIndex] > 0 ? "\nRespuesta guardada: " + LikertLabel(surveyAnswers[surveyIndex]) : string.Empty);
    }

    private string LikertLabel(int value)
    {
        if (value == 4) return "Totalmente de acuerdo";
        if (value == 3) return "De acuerdo";
        if (value == 2) return "En desacuerdo";
        return "Totalmente en desacuerdo";
    }

    private void SaveSurvey()
    {
        for (int i = 0; i < surveyAnswers.Length; i++)
        {
            if (surveyAnswers[i] != 0) continue;
            surveyIndex = i;
            statusText.text = "Falta responder la pregunta " + (i + 1) + ".";
            RefreshSurvey();
            return;
        }

        SurveyRecord record = new SurveyRecord { createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), answers = (int[])surveyAnswers.Clone() };
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "evaluacion_ludoteca_xunjuu.json");
            File.WriteAllText(path, JsonUtility.ToJson(record, true));
            surveyText.text = "Evaluacion guardada correctamente.\nGracias por participar en la prueba piloto.";
            statusText.text = "Respuestas guardadas en el dispositivo.";
        }
        catch (Exception exception)
        {
            statusText.text = "No fue posible guardar: " + exception.Message;
        }
    }

    // Xunjuú v0.1 - ACCION: convertir las fotografias locales en sprites.
    private Sprite LoadPhoto(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
            return null;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "Ludoteca_" + texture.name;
        return sprite;
    }

    // Xunjuú v0.1 - ACCION: mantener estilos y tamaños consistentes.
    private GameObject Panel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private TMP_Text Text(Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        GameObject item = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        item.transform.SetParent(parent, false);
        SetRect(item.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        TMP_Text text = item.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = size;
        text.alignment = ToTmpAlignment(alignment);
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private TMP_Text TextBlock(Transform parent, string value, int size, Vector2 anchor, Vector2 dimensions, TextAnchor alignment, Color color)
    {
        TMP_Text text = Text(parent, value, size, alignment, color);
        text.rectTransform.anchorMin = anchor;
        text.rectTransform.anchorMax = anchor;
        text.rectTransform.sizeDelta = dimensions;
        text.rectTransform.anchoredPosition = Vector2.zero;
        return text;
    }

    private Button Button(Transform parent, string label, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction action, Color color)
    {
        GameObject item = new GameObject("Boton_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        Image image = item.GetComponent<Image>();
        image.color = color;
        Button button = item.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        TMP_Text labelText = Text(item.transform, label, 25, TextAnchor.MiddleCenter, Color.white);
        labelText.raycastTarget = false;
        return button;
    }

    // Xunjuu v0.1 - ACCION: conservar las alineaciones originales con texto SDF nitido.
    private TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
    {
        switch (alignment)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }

    private void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
