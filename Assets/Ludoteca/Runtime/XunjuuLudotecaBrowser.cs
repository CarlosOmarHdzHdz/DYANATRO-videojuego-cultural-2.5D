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
public sealed partial class XunjuuLudotecaBrowser : MonoBehaviour
{
    private enum Page { Home, Culture, Gallery, Fauna, Language, MiniGame, Survey }

    // Tintes del prototipo entregado: manta, añil, grana, maíz y madera oscura.
    private static readonly Color ForestBackground = new Color(0.095f, 0.075f, 0.061f, 0.99f);
    private static readonly Color ForestDeep = new Color(0.141f, 0.114f, 0.102f, 1f);
    private static readonly Color ForestGreen = new Color(0.16f, 0.34f, 0.26f, 1f);
    private static readonly Color ForestGreenLight = new Color(0.235f, 0.42f, 0.29f, 1f);
    private static readonly Color Parchment = new Color(0.949f, 0.914f, 0.847f, 1f);
    private static readonly Color ParchmentShade = new Color(0.914f, 0.863f, 0.761f, 1f);
    private static readonly Color Gold = new Color(0.788f, 0.541f, 0.173f, 1f);
    private static readonly Color GoldDark = new Color(0.39f, 0.27f, 0.16f, 1f);
    private static readonly Color TextileGrana = new Color(0.639f, 0.192f, 0.165f, 1f);
    private static readonly Color TextileBlue = new Color(0.184f, 0.373f, 0.541f, 1f);

    [Serializable]
    private sealed class SurveyRecord
    {
        public string project = "Xunjuu";
        public string createdAt;
        public int[] answers;
    }

    private sealed class FaunaCardView
    {
        public Image image;
        public TMP_Text details;
        public TMP_Text status;
        public Button expand;
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
    private CanvasGroup browserFade;
    private CanvasGroup contentFade;
    private RectTransform browserWindowRect;
    private Coroutine browserEntrance;
    private Coroutine pageEntrance;
    private GameObject galleryRoot;
    private GameObject homeRoot;
    private GameObject cultureRoot;
    private GameObject languageRoot;
    private GameObject faunaRoot;
    private GameObject faunaDetailRoot;
    private GameObject miniGameRoot;
    private GameObject miniGameHub;
    private GameObject contentOrnaments;
    private GameObject surveyRoot;
    private GameObject readingOverlay;
    private TMP_Text pageTitle;
    private TMP_Text pageBody;
    private TMP_Text addressText;
    private TMP_Text tabTitle;
    private TMP_Text statusText;
    private Image pageAccentBar;
    private TMP_Text miniGameText;
    private TMP_Text surveyText;
    private Button[] miniGameAnswerButtons;
    private readonly Stack<Page> history = new Stack<Page>();
    private readonly Dictionary<Page, Image> navigationImages = new Dictionary<Page, Image>();
    private readonly Dictionary<Page, TMP_Text> navigationLabels = new Dictionary<Page, TMP_Text>();
    private readonly Dictionary<Page, TMP_Text> navigationNumbers = new Dictionary<Page, TMP_Text>();
    private readonly Dictionary<Page, GameObject> navigationAccents = new Dictionary<Page, GameObject>();
    private readonly Dictionary<Page, Sprite> atlasIcons = new Dictionary<Page, Sprite>();
    private readonly Dictionary<string, Sprite> photoSprites = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, FaunaCardView> faunaCards = new Dictionary<string, FaunaCardView>();
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
        if(browserEntrance!=null)StopCoroutine(browserEntrance);
        browserEntrance=StartCoroutine(AnimateBrowserEntrance());
        history.Clear();
        ShowPage(Page.Home, false);
    }

    public void Close()
    {
        if (browserRoot == null)
            return;
        if(browserEntrance!=null){StopCoroutine(browserEntrance);browserEntrance=null;}
        if(pageEntrance!=null){StopCoroutine(pageEntrance);pageEntrance=null;}
        if(browserFade!=null)browserFade.alpha=1f;
        if(contentFade!=null)contentFade.alpha=1f;
        if(browserWindowRect!=null)browserWindowRect.localScale=Vector3.one;
        if(pageTitle!=null)pageTitle.rectTransform.localScale=Vector3.one;
        browserRoot.SetActive(false);
        Time.timeScale = previousTimeScale;
        closedFrame = Time.frameCount;
    }

    private void Update()
    {
        TickMemory();
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
        browserRoot = Panel(hostCanvas.transform, "Navegador_Ludoteca", ForestBackground);
        browserFade=browserRoot.AddComponent<CanvasGroup>();
        Canvas browserCanvas = browserRoot.AddComponent<Canvas>();
        browserCanvas.overrideSorting = true;
        browserCanvas.sortingOrder = 10000;
        browserRoot.AddComponent<GraphicRaycaster>();

        Sprite clothingPhoto = LoadPhoto("Ludoteca/Gallery/vestimenta_mazahua");
        Sprite dancePhoto = LoadPhoto("Ludoteca/Gallery/danza_pastoras");
        Sprite milpaPhoto = LoadPhoto("Ludoteca/Gallery/milpa_mexico");

        GameObject window = Panel(browserRoot.transform, "Ventana", ForestDeep);
        browserWindowRect=window.GetComponent<RectTransform>();
        SetRect(window.GetComponent<RectTransform>(), new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.935f));
        AddOutline(window, GoldDark, 2f);

        GameObject top = Panel(window.transform, "Barra_Navegador", ForestDeep);
        SetRect(top.GetComponent<RectTransform>(), new Vector2(0f, 0.90f), Vector2.one);
        AddOutline(top, GoldDark, 1f);
        Button(top.transform, "<", new Vector2(0.035f, 0.5f), new Vector2(66f, 54f), Back, new Color(.29f,.23f,.20f,1f));
        Button(top.transform, "INICIO", new Vector2(0.115f, 0.5f), new Vector2(140f, 54f), () => ShowPage(Page.Home), new Color(.29f,.23f,.20f,1f));
        Button(top.transform, "X", new Vector2(0.96f, 0.5f), new Vector2(62f, 54f), Close, TextileGrana);

        GameObject address = Panel(top.transform, "Direccion", new Color(.09f,.075f,.064f,1f));
        SetRect(address.GetComponent<RectTransform>(), new Vector2(0.18f, 0.20f), new Vector2(0.86f, 0.80f));
        AddOutline(address, new Color(.52f,.42f,.33f,.65f), 1f);
        addressText = Text(address.transform, "xunjuu://ludoteca/inicio", 22, TextAnchor.MiddleLeft, ParchmentShade);
        addressText.rectTransform.offsetMin = new Vector2(20f, 0f);

        GameObject nav = Panel(window.transform, "Navegacion", ForestDeep);
        SetRect(nav.GetComponent<RectTransform>(), new Vector2(0f, 0.055f), new Vector2(0.21f, 0.90f));
        AddOutline(nav, GoldDark, 1f);
        TextBlock(nav.transform, "LUDOTECA DIGITAL", 27, new Vector2(0.5f, 0.90f), new Vector2(340f, 48f), TextAnchor.MiddleCenter, Gold).fontStyle = FontStyles.Bold;
        TextBlock(nav.transform, "ARCHIVO CULTURAL INTERACTIVO", 14, new Vector2(0.5f, 0.855f), new Vector2(340f, 26f), TextAnchor.MiddleCenter, ParchmentShade);
        TextBlock(nav.transform, "EXPLORA  /  APRENDE  /  PRACTICA", 14, new Vector2(0.5f, 0.815f), new Vector2(340f, 26f), TextAnchor.MiddleCenter, Gold);
        NavButton(nav.transform, "Cultura", 0.76f, Page.Culture);
        NavButton(nav.transform, "Galería", 0.65f, Page.Gallery);
        NavButton(nav.transform, "Fauna", 0.54f, Page.Fauna);
        NavButton(nav.transform, "Lengua mazahua", 0.43f, Page.Language);
        NavButton(nav.transform, "Minijuegos", 0.32f, Page.MiniGame);
        NavButton(nav.transform, "Evaluación", 0.21f, Page.Survey);
        TextBlock(nav.transform, "06 SECCIONES  ·  03 ACTIVIDADES", 14, new Vector2(.5f,.12f), new Vector2(315f,27f), TextAnchor.MiddleCenter, ParchmentShade);
        CreateCardIcon(nav.transform, ElementIcon(4), new Vector2(.5f,.073f), new Vector2(250f,43f));
        AddSidebarEmbroidery(nav.transform);

        GameObject content = Panel(window.transform, "Contenido", Parchment);
        contentFade=content.AddComponent<CanvasGroup>();
        SetRect(content.GetComponent<RectTransform>(), new Vector2(0.21f, 0.055f), new Vector2(1f, 0.90f));
        AddOutline(content, ParchmentShade, 1f);
        pageAccentBar = Panel(content.transform, "Acento_Seccion", TextileGrana).GetComponent<Image>();
        SetRect(pageAccentBar.rectTransform, new Vector2(0f,.992f), Vector2.one);
        pageAccentBar.raycastTarget = false;
        contentOrnaments = new GameObject("Ornamentos_De_Pagina", typeof(RectTransform));
        contentOrnaments.transform.SetParent(content.transform, false);
        SetRect(contentOrnaments.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        AddContentOrnaments(contentOrnaments.transform);
        pageTitle = TextBlock(content.transform, string.Empty, 42, new Vector2(0.5f, 0.91f), new Vector2(1260f, 90f), TextAnchor.MiddleCenter, GoldDark);
        pageTitle.fontStyle = FontStyles.Bold;
        readingOverlay = Panel(content.transform, "Lectura_Extendida", Parchment);
        SetRect(readingOverlay.GetComponent<RectTransform>(), new Vector2(.035f,.075f), new Vector2(.965f,.82f));
        AddOutline(readingOverlay, ParchmentShade, 2f);
        pageBody = TextBlock(readingOverlay.transform, string.Empty, 27, new Vector2(.5f,.46f),
            new Vector2(1150f,570f), TextAnchor.UpperLeft, ForestDeep);
        pageBody.lineSpacing = 1.1f;
        Button(readingOverlay.transform, "VOLVER A FICHAS", new Vector2(.84f,.93f),
            new Vector2(245f,52f), CloseReading, ForestGreen);
        readingOverlay.SetActive(false);

        homeRoot = BuildHomeIntro(content.transform);
        cultureRoot = BuildCulturePage(content.transform);
        languageRoot = BuildLanguagePage(content.transform);
        galleryRoot = BuildGallery(content.transform,
            clothingPhoto != null ? clothingPhoto : tree,
            dancePhoto != null ? dancePhoto : flower,
            milpaPhoto != null ? milpaPhoto : cloud);
        faunaRoot = BuildFaunaCollection(content.transform);
        miniGameRoot = BuildMiniGame(content.transform);
        surveyRoot = BuildSurvey(content.transform);

        GameObject footer = Panel(window.transform, "Estado", ForestDeep);
        SetRect(footer.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0.055f));
        statusText = Text(footer.transform, "ESPACIO CULTURAL DIGITAL     ·     EXPLORAR   /   APRENDER   /   RECORDAR", 18, TextAnchor.MiddleLeft, Gold);
        statusText.rectTransform.offsetMin = new Vector2(20f, 0f);
        BuildBrowserTabs(browserRoot.transform);
        browserRoot.SetActive(false);
    }

    private void BuildBrowserTabs(Transform parent)
    {
        GameObject tabs = Panel(parent, "Pestanas", new Color(0f, 0f, 0f, 0f));
        SetRect(tabs.GetComponent<RectTransform>(), new Vector2(0.045f, 0.94f), new Vector2(0.48f, 0.99f));
        tabs.GetComponent<Image>().raycastTarget = false;

        GameObject homeTab = Panel(tabs.transform, "Pestana_Inicio", new Color(0.84f, 0.82f, 0.74f, 1f));
        SetRect(homeTab.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.38f, 1f));
        AddOutline(homeTab, ParchmentShade, 1f);
        Text(homeTab.transform, "INICIO", 20, TextAnchor.MiddleCenter, ForestDeep).fontStyle = FontStyles.Bold;

        GameObject routeTab = Panel(tabs.transform, "Pestana_Ruta", Parchment);
        SetRect(routeTab.GetComponent<RectTransform>(), new Vector2(0.35f, 0f), Vector2.one);
        AddOutline(routeTab, ParchmentShade, 1f);
        tabTitle = Text(routeTab.transform, "xunjuu://ludoteca/inicio", 19, TextAnchor.MiddleLeft, ForestDeep);
        tabTitle.rectTransform.offsetMin = new Vector2(18f, 0f);
    }

    private void NavButton(Transform parent, string label, float y, Page page)
    {
        Button button = Button(parent, label, new Vector2(0.5f, y), new Vector2(310f, 72f), () => ShowPage(page), new Color(.20f,.16f,.14f,1f));
        navigationImages[page] = button.GetComponent<Image>();
        TMP_Text title = button.GetComponentInChildren<TMP_Text>();
        title.rectTransform.anchorMin = new Vector2(.31f, 0f);
        title.rectTransform.anchorMax = new Vector2(.84f, 1f);
        title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.fontSize = 20;
        navigationLabels[page] = title;
        int number=(int)page;
        if(page==Page.MiniGame)number=5;
        else if(page==Page.Survey)number=6;
        else if(page==Page.Fauna)number=3;
        else if(page==Page.Language)number=4;
        else if(page==Page.Gallery)number=2;
        else if(page==Page.Culture)number=1;
        TMP_Text index=TextBlock(button.transform,number.ToString("00"),15,
            new Vector2(.91f,.5f),new Vector2(35f,30f),TextAnchor.MiddleCenter,Gold);
        index.fontStyle=FontStyles.Bold;
        navigationNumbers[page]=index;

        GameObject accent = Panel(button.transform, "Acento_Activo", PageAccent(page));
        SetRect(accent.GetComponent<RectTransform>(), new Vector2(0f,.06f), new Vector2(.012f,.94f));
        accent.GetComponent<Image>().raycastTarget = false;
        navigationAccents[page] = accent;

        GameObject iconPlate = Panel(button.transform, "Placa_Icono", new Color(.12f,.09f,.075f,1f));
        SetRect(iconPlate.GetComponent<RectTransform>(), new Vector2(.035f,.10f), new Vector2(.25f,.90f));
        iconPlate.GetComponent<Image>().raycastTarget = false;
        Sprite icon = NavigationIcon(page);
        if (icon != null)
        {
            GameObject item = new GameObject("Icono_" + page, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(iconPlate.transform, false);
            SetRect(item.GetComponent<RectTransform>(), new Vector2(.02f,.02f), new Vector2(.98f,.98f));
            Image image = item.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
    }

    private void AddSidebarEmbroidery(Transform parent)
    {
        foreach(float x in new[]{.018f,.982f})
        {
            GameObject rail=Panel(parent,"Hilo_Lateral",GoldDark);
            SetRect(rail.GetComponent<RectTransform>(),new Vector2(x-.002f,.19f),new Vector2(x+.002f,.80f));
            rail.GetComponent<Image>().raycastTarget=false;
            for(int i=0;i<8;i++)
                CreateDiamond(parent,new Vector2(x,.215f+i*.078f),9f,
                    i%3==0?TextileGrana:i%3==1?TextileBlue:Gold,ParchmentShade);
        }
        for(int i=0;i<9;i++)
        {
            float x=.10f+i*.10f;
            Color thread=i%3==0?TextileGrana:i%3==1?TextileBlue:Gold;
            CreateDiamond(parent,new Vector2(x,.965f),10f,thread,ParchmentShade);
            CreateDiamond(parent,new Vector2(x,.155f),9f,thread,ParchmentShade);
        }
    }

    private GameObject BuildHomeIntro(Transform parent)
    {
        GameObject intro = Panel(parent, "Ruta_De_Aprendizaje", new Color(.99f,.97f,.92f,1f));
        SetRect(intro.GetComponent<RectTransform>(), new Vector2(.055f,.665f), new Vector2(.945f,.795f));
        AddOutline(intro, ParchmentShade, 2f);
        CreateCardIcon(intro.transform, ElementIcon(3), new Vector2(.075f,.5f), new Vector2(94f,94f));
        TextBlock(intro.transform, "RUTA DE APRENDIZAJE", 22, new Vector2(.38f,.72f),
            new Vector2(470f,38f), TextAnchor.MiddleLeft, TextileGrana).fontStyle = FontStyles.Bold;
        TextBlock(intro.transform, "Observa el territorio, explora sus fichas y practica con tres minijuegos.", 21,
            new Vector2(.57f,.31f), new Vector2(900f,50f), TextAnchor.MiddleLeft, ForestDeep);
        return intro;
    }

    private GameObject BuildCulturePage(Transform parent)
    {
        GameObject root = new GameObject("Cultura_Visual", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(.035f,.075f), new Vector2(.965f,.80f));
        EditorialCard(root.transform, "INDUMENTARIA Y TEXTILES",
            "Observa prendas, materiales y técnicas. La galería reúne imágenes para conocer sus detalles.",
            NavigationIcon(Page.Culture), new Vector2(.17f,.54f), TextileGrana);
        EditorialCard(root.transform, "MILPA Y TERRITORIO",
            "El maíz y la milpa aparecen en el paisaje del juego. Explora sus imágenes y vocabulario.",
            ElementIcon(0), new Vector2(.50f,.54f), Gold);
        EditorialCard(root.transform, "MEMORIA Y TRADICIÓN",
            "Las celebraciones, las palabras y los relatos invitan a escuchar a la comunidad.",
            NavigationIcon(Page.Language), new Vector2(.83f,.54f), ForestGreen);
        TextBlock(root.transform, "El contenido cultural y lingüístico requiere revisión con fuentes y personas de la comunidad mazahua.",
            17, new Vector2(.5f,.13f), new Vector2(1200f,42f), TextAnchor.MiddleCenter, GoldDark);
        Button(root.transform, "LEER TEXTO COMPLETO", new Vector2(.5f,.045f),
            new Vector2(300f,48f), OpenReading, ForestGreen);
        return root;
    }

    private GameObject BuildLanguagePage(Transform parent)
    {
        GameObject root = new GameObject("Lengua_Visual", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(.045f,.085f), new Vector2(.955f,.78f));

        GameObject vocabulary = Panel(root.transform, "Vocabulario_Demostrativo", new Color(.99f,.97f,.92f,1f));
        SetRect(vocabulary.GetComponent<RectTransform>(), new Vector2(.025f,.10f), new Vector2(.60f,.93f));
        AddOutline(vocabulary, ForestGreen, 2f);
        TextBlock(vocabulary.transform, "VOCABULARIO DEMOSTRATIVO", 24, new Vector2(.5f,.88f),
            new Vector2(580f,48f), TextAnchor.MiddleCenter, ForestGreen).fontStyle = FontStyles.Bold;
        TextBlock(vocabulary.transform,
            "jñiñi  ·  pueblo\nndechjö  ·  maíz\nmúbú  ·  corazón / vida\nnu t'eje  ·  bosque\nndajma  ·  flor\nlala  ·  pato\npjanteje  ·  venado",
            24, new Vector2(.5f,.44f), new Vector2(490f,315f), TextAnchor.MiddleLeft, ForestDeep);

        GameObject note = Panel(root.transform, "Nota_De_Lengua", new Color(.99f,.97f,.92f,1f));
        SetRect(note.GetComponent<RectTransform>(), new Vector2(.635f,.10f), new Vector2(.975f,.93f));
        AddOutline(note, Gold, 2f);
        CreateCardIcon(note.transform, NavigationIcon(Page.Language), new Vector2(.5f,.73f), new Vector2(156f,156f));
        TextBlock(note.transform, "ESCUCHAR Y APRENDER", 22, new Vector2(.5f,.43f),
            new Vector2(400f,45f), TextAnchor.MiddleCenter, GoldDark).fontStyle = FontStyles.Bold;
        TextBlock(note.transform,
            "La escritura, la variante regional, el contexto y la pronunciación deben validarse antes de publicar estas fichas.",
            20, new Vector2(.5f,.21f), new Vector2(360f,140f), TextAnchor.MiddleCenter, ForestDeep);
        Button(root.transform, "LEER TEXTO COMPLETO", new Vector2(.5f,.045f),
            new Vector2(300f,48f), OpenReading, ForestGreen);
        return root;
    }

    private void EditorialCard(Transform parent, string title, string body, Sprite art, Vector2 anchor, Color accent)
    {
        GameObject card = Panel(parent, "Ficha_" + title, new Color(.99f,.97f,.92f,1f));
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(355f,435f);
        rect.anchoredPosition = Vector2.zero;
        AddOutline(card, accent, 2f);
        Shadow shadow = card.AddComponent<Shadow>();
        shadow.effectColor = new Color(.24f,.16f,.10f,.15f);
        shadow.effectDistance = new Vector2(0f,-5f);
        CreateCardIcon(card.transform, art, new Vector2(.5f,.73f), new Vector2(145f,145f));
        TextBlock(card.transform, title, 24, new Vector2(.5f,.49f),
            new Vector2(320f,56f), TextAnchor.MiddleCenter, accent).fontStyle = FontStyles.Bold;
        TextBlock(card.transform, body, 20, new Vector2(.5f,.30f),
            new Vector2(305f,134f), TextAnchor.MiddleCenter, ForestDeep);
    }

    private void OpenReading()
    {
        readingOverlay.transform.SetAsLastSibling();
        readingOverlay.SetActive(true);
    }

    private void CloseReading()
    {
        if (readingOverlay != null) readingOverlay.SetActive(false);
    }

    private GameObject BuildGallery(Transform parent, Sprite tree, Sprite flower, Sprite cloud)
    {
        return BuildInteractiveGallery(parent, tree, flower, cloud);
    }

    private GameObject BuildFaunaCollection(Transform parent)
    {
        GameObject root = new GameObject("Coleccion_Fauna", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(.025f, .055f), new Vector2(.975f, .82f));

        int index = 0;
        foreach (XunjuuFaunaCatalog.Entry entry in XunjuuFaunaCatalog.Entries)
        {
            int column = index % 3;
            int row = index / 3;
            Vector2 anchor = new Vector2(.17f + column * .33f, row == 0 ? .70f : .27f);
            GameObject card = Panel(root.transform, "Ficha_Fauna_" + entry.Id, new Color(1f, .98f, .90f, 1f));
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = anchor;
            cardRect.anchorMax = anchor;
            cardRect.pivot = new Vector2(.5f, .5f);
            cardRect.sizeDelta = new Vector2(385f, 250f);
            cardRect.anchoredPosition = Vector2.zero;
            AddOutline(card, column == 1 ? TextileBlue : column == 2 ? Gold : TextileGrana, 4f);

            GameObject visual = new GameObject("Retrato", typeof(RectTransform), typeof(Image));
            visual.transform.SetParent(card.transform, false);
            SetRect(visual.GetComponent<RectTransform>(), new Vector2(.035f, .16f), new Vector2(.44f, .90f));
            Image image = visual.GetComponent<Image>();
            image.sprite = XunjuuFaunaCatalog.GetSprite(entry);
            image.preserveAspect = true;
            image.raycastTarget = false;

            TMP_Text name = TextBlock(card.transform, entry.DisplayName.ToUpperInvariant(), 22, new Vector2(.71f, .78f), new Vector2(205f, 54f), TextAnchor.MiddleCenter, GoldDark);
            name.fontStyle = FontStyles.Bold;
            TMP_Text details = TextBlock(card.transform, string.Empty, 16, new Vector2(.71f, .47f), new Vector2(205f, 105f), TextAnchor.UpperLeft, ForestDeep);
            TMP_Text status = TextBlock(card.transform, string.Empty, 18, new Vector2(.71f, .14f), new Vector2(205f, 42f), TextAnchor.MiddleCenter, ForestGreen);
            status.fontStyle = FontStyles.Bold;
            Button expand=card.AddComponent<Button>();
            expand.targetGraphic=card.GetComponent<Image>();
            expand.onClick.AddListener(()=>ShowFaunaDetails(entry));
            faunaCards[entry.Id] = new FaunaCardView { image = image, details = details, status = status, expand=expand };
            index++;
        }
        return root;
    }

    private void RefreshFaunaCollection()
    {
        foreach (XunjuuFaunaCatalog.Entry entry in XunjuuFaunaCatalog.Entries)
        {
            if (!faunaCards.TryGetValue(entry.Id, out FaunaCardView card))
                continue;
            bool captured = XunjuuFaunaCatalog.IsCaptured(entry.Id);
            card.image.color = captured ? Color.white : new Color(.07f, .11f, .09f, 1f);
            card.details.text = captured
                ? "<i>" + entry.ScientificName + "</i>\n" + entry.Habitat
                : "Silueta por descubrir.\nAcércate a un ejemplar y pulsa C para registrarlo.";
            card.status.text = captured ? "VER FICHA +" : "POR DESCUBRIR";
            card.expand.interactable=captured;
            card.status.color = captured ? ForestGreen : TextileGrana;
        }
    }

    private void OnEnable() { XunjuuFaunaCatalog.CollectionChanged+=OnFaunaUnlocked; }
    private void OnDisable() { XunjuuFaunaCatalog.CollectionChanged-=OnFaunaUnlocked; }
    private void OnFaunaUnlocked()
    {
        if(!initialized)return;
        RefreshFaunaCollection();
        if(currentPage==Page.Fauna)pageTitle.text="Archivo de fauna · "+XunjuuFaunaCatalog.CapturedCount+"/6";
    }

    public bool ShowFaunaDetails(XunjuuFaunaCatalog.Entry entry)
    {
        if(entry==null || faunaRoot==null || !XunjuuFaunaCatalog.IsCaptured(entry.Id))return false;
        CloseFaunaDetails();
        foreach(var card in faunaCards.Values)card.image.transform.parent.gameObject.SetActive(false);
        faunaDetailRoot=Panel(faunaRoot.transform,"Detalle_"+entry.Id,Parchment);
        SetRect(faunaDetailRoot.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
        TextBlock(faunaDetailRoot.transform,entry.DisplayName.ToUpperInvariant(),30,new Vector2(.48f,.94f),new Vector2(900,55),TextAnchor.MiddleCenter,GoldDark);
        Button(faunaDetailRoot.transform,"VOLVER",new Vector2(.91f,.94f),new Vector2(145,48),CloseFaunaDetails,ForestGreen);
        var portrait=new GameObject("Retrato",typeof(RectTransform),typeof(Image));
        portrait.transform.SetParent(faunaDetailRoot.transform,false);
        SetRect(portrait.GetComponent<RectTransform>(),new Vector2(.025f,.27f),new Vector2(.29f,.86f));
        portrait.GetComponent<Image>().sprite=XunjuuFaunaCatalog.GetSprite(entry);
        portrait.GetComponent<Image>().preserveAspect=true;
        TextBlock(faunaDetailRoot.transform,entry.ScientificName,22,new Vector2(.16f,.19f),new Vector2(320,80),TextAnchor.MiddleCenter,ForestDeep);
        var scrollObject=new GameObject("Lectura",typeof(RectTransform),typeof(ScrollRect));
        scrollObject.transform.SetParent(faunaDetailRoot.transform,false);
        SetRect(scrollObject.GetComponent<RectTransform>(),new Vector2(.32f,.055f),new Vector2(.98f,.87f));
        var viewport=Panel(scrollObject.transform,"Ventana",Parchment);
        SetRect(viewport.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
        viewport.AddComponent<RectMask2D>();
        TMP_Text body=TextBlock(viewport.transform,XunjuuFaunaCatalog.ExpandedInformation(entry),24,new Vector2(.5f,1),new Vector2(760,1000),TextAnchor.UpperLeft,ForestDeep);
        body.rectTransform.anchorMin=new Vector2(0,1);body.rectTransform.anchorMax=Vector2.one;
        body.rectTransform.pivot=new Vector2(.5f,1);body.rectTransform.sizeDelta=new Vector2(-24,1000);body.rectTransform.anchoredPosition=Vector2.zero;
        body.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll=scrollObject.GetComponent<ScrollRect>();
        scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=body.rectTransform;
        scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=32;scroll.movementType=ScrollRect.MovementType.Clamped;
        Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;
        statusText.text="Ficha desbloqueada · desplázate sobre el texto para leer más · VOLVER regresa a la colección";
        return true;
    }
    private void CloseFaunaDetails()
    {
        if(faunaDetailRoot!=null){faunaDetailRoot.SetActive(false);Destroy(faunaDetailRoot);faunaDetailRoot=null;}
        foreach(var card in faunaCards.Values)card.image.transform.parent.gameObject.SetActive(true);
        if(statusText!=null && currentPage==Page.Fauna)statusText.text="Selecciona una ficha capturada para ampliar su información";
    }

    // Xunjuú v0.1 - ACCION: navegar sin cargar escenas o aplicaciones externas.
    private void ShowPage(Page page, bool remember = true)
    {
        if (contentOrnaments != null) contentOrnaments.SetActive(true);
        CloseFaunaDetails();
        CloseReading();
        if (remember && page != currentPage)
            history.Push(currentPage);
        currentPage = page;
        UpdateNavigationState();
        homeRoot.SetActive(page == Page.Home);
        cultureRoot.SetActive(page == Page.Culture);
        languageRoot.SetActive(page == Page.Language);
        galleryRoot.SetActive(page == Page.Home || page == Page.Gallery);
        SetRect(galleryRoot.GetComponent<RectTransform>(), new Vector2(.04f,.08f),
            page == Page.Home ? new Vector2(.96f,.62f) : new Vector2(.96f,.78f));
        if (page == Page.Home || page == Page.Gallery)
            SetGalleryMode(page == Page.Gallery);
        faunaRoot.SetActive(page == Page.Fauna);
        miniGameRoot.SetActive(page == Page.MiniGame);
        surveyRoot.SetActive(page == Page.Survey);

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
                statusText.text = "Toca una ficha para conocerla · Fotografías acreditadas e ilustraciones conceptuales";
                break;
            case Page.Fauna:
                SetPage("Archivo de fauna · " + XunjuuFaunaCatalog.CapturedCount + "/" + XunjuuFaunaCatalog.Entries.Count, "fauna", string.Empty);
                RefreshFaunaCollection();
                statusText.text = "Coleccion de observacion · captura sin combate con la tecla C";
                break;
            case Page.Language:
                SetPage("Lengua mazahua", "lengua", "VOCABULARIO DEMOSTRATIVO\n\njñiñi - pueblo\nndechjö - maiz\nmúbú - corazon / vida\nnu t'eje - el bosque\nndajma - flor\nlala - pato\npjanteje - venado\n\nAntes de publicar se deberan validar escritura, variante regional, contexto y pronunciacion con una fuente especializada.");
                break;
            case Page.MiniGame:
                SetPage("Minijuegos de la Cultura Mazahua", "minijuegos", string.Empty);
                RefreshMiniGame();
                break;
            case Page.Survey:
                SetPage("Evaluacion de la experiencia", "evaluacion", string.Empty);
                RefreshSurvey();
                break;
        }
        if(pageEntrance!=null)StopCoroutine(pageEntrance);
        if(contentFade!=null && browserRoot.activeInHierarchy)
            pageEntrance=StartCoroutine(AnimatePageEntrance());
    }

    private System.Collections.IEnumerator AnimateBrowserEntrance()
    {
        float elapsed=0f;
        while(elapsed<.18f)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/.18f));
            browserFade.alpha=Mathf.Lerp(.42f,1f,t);
            browserWindowRect.localScale=Vector3.one*Mathf.Lerp(.982f,1f,t);
            yield return null;
        }
        browserFade.alpha=1f;
        browserWindowRect.localScale=Vector3.one;
        browserEntrance=null;
    }

    private System.Collections.IEnumerator AnimatePageEntrance()
    {
        float elapsed=0f;
        while(elapsed<.20f)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/.20f));
            contentFade.alpha=Mathf.Lerp(.72f,1f,t);
            pageTitle.rectTransform.localScale=Vector3.one*Mathf.Lerp(.96f,1f,t);
            yield return null;
        }
        contentFade.alpha=1f;
        pageTitle.rectTransform.localScale=Vector3.one;
        pageEntrance=null;
    }

    private void SetPage(string title, string route, string body)
    {
        pageTitle.text = title;
        addressText.text = "xunjuu://ludoteca/" + route;
        if (tabTitle != null)
            tabTitle.text = "xunjuu://ludoteca/" + route;
        pageBody.text = body;
        statusText.text = "Explora  ·  Aprende  ·  Pon a prueba lo aprendido";
    }

    private void UpdateNavigationState()
    {
        foreach (KeyValuePair<Page, Image> entry in navigationImages)
        {
            if (entry.Value == null)
                continue;
            bool active = entry.Key == currentPage;
            entry.Value.color = active ? Parchment : new Color(.20f,.16f,.14f,1f);
            if (navigationLabels.TryGetValue(entry.Key, out TMP_Text label))
                label.color = active ? ForestDeep : Parchment;
            if (navigationNumbers.TryGetValue(entry.Key, out TMP_Text number))
                number.color = active ? PageAccent(entry.Key) : Gold;
            if (navigationAccents.TryGetValue(entry.Key, out GameObject accent))
                accent.SetActive(active);
        }
        Color pageColor = PageAccent(currentPage);
        if (pageTitle != null) pageTitle.color = pageColor;
        if (pageAccentBar != null) pageAccentBar.color = pageColor;
    }

    private static Color PageAccent(Page page)
    {
        switch (page)
        {
            case Page.Culture: return TextileGrana;
            case Page.Gallery: return Gold;
            case Page.Fauna: return ForestGreenLight;
            case Page.Language: return ForestGreen;
            case Page.MiniGame: return TextileBlue;
            case Page.Survey: return TextileGrana;
            default: return GoldDark;
        }
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
        SetRect(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        miniGameHub = new GameObject("Seleccion_De_Actividades", typeof(RectTransform));
        miniGameHub.transform.SetParent(root.transform, false);
        SetRect(miniGameHub.GetComponent<RectTransform>(), new Vector2(0.035f, 0.075f), new Vector2(0.965f, 0.80f));
        TextBlock(miniGameHub.transform, "TRES RETOS PARA OBSERVAR, RELACIONAR Y RECORDAR", 17,
            new Vector2(.5f,.965f), new Vector2(930f,32f), TextAnchor.MiddleCenter, GoldDark).fontStyle=FontStyles.Bold;

        GameObject memoryCard = CreateMiniGameCard(miniGameHub.transform, "MEMORAMA", new Vector2(0.17f, 0.50f), TextileGrana);
        BuildMemoramaPreview(memoryCard.transform);

        GameObject wordSearchCard = CreateMiniGameCard(miniGameHub.transform, "SOPA DE LETRAS", new Vector2(0.50f, 0.50f), TextileBlue);
        BuildWordSearchPreview(wordSearchCard.transform);

        GameObject quizCard = CreateMiniGameCard(miniGameHub.transform, "QUIZ", new Vector2(0.83f, 0.50f), Gold);
        CreateCardIcon(quizCard.transform, ElementIcon(1), new Vector2(.5f,.59f), new Vector2(172,172));
        TextBlock(quizCard.transform, "OBJETOS Y PALABRAS", 21, new Vector2(.5f,.39f), new Vector2(300,34), TextAnchor.MiddleCenter, TextileGrana).fontStyle=FontStyles.Bold;
        TextBlock(quizCard.transform, "Reconoce elementos culturales.\nCinco preguntas, sin límite de tiempo.", 19, new Vector2(.5f,.29f), new Vector2(300,74), TextAnchor.MiddleCenter, ForestDeep);
        CreateVisualButton(quizCard.transform,"INICIAR QUIZ",.12f,StartQuiz);
        return root;
    }

    private GameObject CreateMiniGameCard(Transform parent, string title, Vector2 anchor, Color borderColor)
    {
        GameObject card = Panel(parent, "Tarjeta_" + title, new Color(.992f, .977f, .942f, 1f));
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(355f, 470f);
        rect.anchoredPosition = Vector2.zero;
        AddOutline(card, borderColor, 2.5f);
        Shadow shadow = card.AddComponent<Shadow>();
        shadow.effectColor = new Color(.24f,.16f,.10f,.18f);
        shadow.effectDistance = new Vector2(0f,-6f);
        shadow.useGraphicAlpha = true;
        AddMiniGameTextileTrim(card.transform, borderColor);

        TextBlock(card.transform, title, 29, new Vector2(0.5f, 0.90f), new Vector2(320f, 50f), TextAnchor.MiddleCenter, GoldDark).fontStyle = FontStyles.Bold;
        CreateRule(card.transform, 0.81f, ParchmentShade);
        AddMiniGameCardIcon(card.transform,title);
        return card;
    }

    private void AddMiniGameCardIcon(Transform parent,string title)
    {
        if(title=="MEMORAMA")
        {
            CreateCardIcon(parent,XunjuuFaunaCatalog.GetSprite(XunjuuFaunaCatalog.Find("venado_cola_blanca")),new Vector2(.86f,.90f),new Vector2(42,38));
            CreateCardIcon(parent,ElementIcon(0),new Vector2(.14f,.90f),new Vector2(42,38));
        }
        else if(title=="QUIZ")CreateCardIcon(parent,LoadPhoto("Ludoteca/Minigames/sol_felicidades_mazahua"),new Vector2(.86f,.90f),new Vector2(42,38));
        else
        {
            for(int i=0;i<9;i++)
            {
                var icon=Panel(parent,"Icono_Cuadricula",i%2==0?TextileBlue:ForestGreen);
                var rect=icon.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.86f+(i%3)*.035f,.87f-(i/3)*.035f);rect.sizeDelta=new Vector2(9,9);rect.anchoredPosition=Vector2.zero;
                icon.GetComponent<Image>().raycastTarget=false;
            }
        }
    }

    private void CreateCardIcon(Transform parent,Sprite sprite,Vector2 anchor,Vector2 size)
    {
        if(sprite==null)return;
        var icon=new GameObject("Icono_Actividad",typeof(RectTransform),typeof(Image));icon.transform.SetParent(parent,false);
        var rect=icon.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size;rect.anchoredPosition=Vector2.zero;
        var image=icon.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
    }

    private void AddMiniGameTextileTrim(Transform parent, Color borderColor)
    {
        GameObject stripe = Panel(parent, "Banda_Textil", borderColor);
        SetRect(stripe.GetComponent<RectTransform>(), new Vector2(0f,.982f), Vector2.one);
        stripe.GetComponent<Image>().raycastTarget = false;
        for (int index = 0; index < 5; index++)
            CreateDiamond(parent, new Vector2(.38f + index * .06f,.035f), 7f,
                index % 2 == 0 ? borderColor : Gold, Parchment);
    }

    private void BuildMemoramaPreview(Transform parent)
    {
        CreatePreviewTile(parent, "VENADO", 0, new Vector2(0.31f, 0.59f), TextileGrana);
        CreatePreviewTile(parent, "MILPA", 9, new Vector2(0.69f, 0.59f), Gold);
        TextBlock(parent, "18 motivos textiles\n3 etapas de 6 parejas", 20, new Vector2(0.5f, 0.34f), new Vector2(300f, 58f), TextAnchor.MiddleCenter, ForestDeep);
        CreateVisualButton(parent, "JUGAR MEMORAMA", 0.12f, StartMemory);
    }

    private void BuildWordSearchPreview(Transform parent)
    {
        TextBlock(parent, "VOCABULARIO Y TERRITORIO", 16, new Vector2(.5f,.75f), new Vector2(300f,26f), TextAnchor.MiddleCenter, TextileBlue).fontStyle = FontStyles.Bold;
        string[] preview = {"MAZAHUA", "AMILPAR", "ZBOSQUE", "AFLORES", "HPUEBLO"};
        for (int row = 0; row < preview.Length; row++)
        {
            for (int column = 0; column < preview[row].Length; column++)
            {
                bool marked = row == 0 || (row == 1 && column >= 1 && column <= 5);
                TMP_Text letter = TextBlock(parent, preview[row][column].ToString(), 20,
                    new Vector2(.18f + column * .106f,.67f - row * .072f),
                    new Vector2(32f,30f), TextAnchor.MiddleCenter, marked ? TextileBlue : ForestDeep);
                letter.fontStyle = marked ? FontStyles.Bold : FontStyles.Normal;
            }
        }
        TextBlock(parent, "4 PALABRAS  ·  SIN LÍMITE DE TIEMPO", 16, new Vector2(.5f,.30f), new Vector2(320f,36f), TextAnchor.MiddleCenter, GoldDark);
        CreateVisualButton(parent, "JUGAR SOPA DE LETRAS", 0.12f, StartWordSearch);
    }

    private void CreatePreviewTile(Transform parent, string label, int motifIndex, Vector2 anchor, Color borderColor)
    {
        GameObject tile = Panel(parent, "Ficha_" + label, Parchment);
        RectTransform rect = tile.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(124f, 124f);
        rect.anchoredPosition = Vector2.zero;
        AddOutline(tile, borderColor, 2f);
        Sprite illustration = MemoryMotifSprite(motifIndex);
        var visual=Panel(tile.transform,"Ilustracion",Color.white);
        RectTransform visualRect=visual.GetComponent<RectTransform>();
        visualRect.anchorMin=visualRect.anchorMax=new Vector2(.5f,.5f);
        visualRect.pivot=new Vector2(.5f,.5f);
        visualRect.sizeDelta=new Vector2(106f,106f);
        visualRect.anchoredPosition=Vector2.zero;
        visual.GetComponent<Image>().sprite=illustration;
        visual.GetComponent<Image>().preserveAspect=true;
        visual.GetComponent<Image>().raycastTarget=false;
        if(illustration==null)visual.SetActive(false);
    }

    private void CreateVisualButton(Transform parent, string label, float anchorY, UnityEngine.Events.UnityAction action)
    {
        Button(parent, label, new Vector2(0.5f, anchorY), new Vector2(305f, 54f), action, ForestGreen);
    }

    private void ShowMiniGameNotice(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void RefreshMiniGame()
    {
        EndActivity();
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
        surveyText = TextBlock(root.transform, string.Empty, 34, new Vector2(0.5f, 0.74f), new Vector2(1160f, 240f), TextAnchor.MiddleCenter, ForestDeep);
        Button(root.transform, "Totalmente de acuerdo", new Vector2(0.25f, 0.38f), new Vector2(350f, 70f), () => AnswerSurvey(4), ForestGreenLight);
        Button(root.transform, "De acuerdo", new Vector2(0.75f, 0.38f), new Vector2(320f, 70f), () => AnswerSurvey(3), ForestGreen);
        Button(root.transform, "En desacuerdo", new Vector2(0.25f, 0.18f), new Vector2(350f, 70f), () => AnswerSurvey(2), GoldDark);
        Button(root.transform, "Totalmente en desacuerdo", new Vector2(0.75f, 0.18f), new Vector2(390f, 70f), () => AnswerSurvey(1), TextileGrana);
        Button(root.transform, "Anterior", new Vector2(0.12f, 0.04f), new Vector2(190f, 54f), PreviousSurvey, ForestDeep);
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
        if(photoSprites.TryGetValue(resourcePath,out Sprite cached))return cached;
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
            return null;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "Ludoteca_" + texture.name;
        photoSprites[resourcePath]=sprite;
        return sprite;
    }

    private Sprite ElementIcon(int index)
    {
        string key = "Ludoteca/Icons/iconos_elementos_atlas#" + index;
        if (photoSprites.TryGetValue(key, out Sprite cached)) return cached;
        Texture2D atlas = Resources.Load<Texture2D>("Ludoteca/Icons/iconos_elementos_atlas");
        if (atlas == null || index < 0 || index >= 6) return null;
        float width = atlas.width / 3f;
        float height = atlas.height / 2f;
        // Los flecos del textil rozan la celda vecina: un pequeño margen evita
        // que aparezcan fragmentos ajenos junto al icono de lluvia.
        float insetX=width*.04f, insetY=height*.04f;
        Sprite sprite = Sprite.Create(atlas,
            new Rect((index % 3) * width+insetX, (index < 3 ? 1 : 0) * height+insetY,
                width-2f*insetX, height-2f*insetY),
            new Vector2(.5f,.5f), 100f);
        sprite.name = "Icono_Elemento_" + index;
        photoSprites[key] = sprite;
        return sprite;
    }

    private Sprite NavigationIcon(Page page)
    {
        if (atlasIcons.TryGetValue(page, out Sprite cached)) return cached;
        Texture2D atlas = Resources.Load<Texture2D>("Ludoteca/Icons/iconos_ludoteca_atlas");
        if (atlas != null)
        {
            int index;
            switch (page)
            {
                case Page.Culture: index = 0; break;
                case Page.Gallery: index = 1; break;
                case Page.Language: index = 2; break;
                case Page.Fauna: index = 3; break;
                case Page.MiniGame: index = 4; break;
                case Page.Survey: index = 5; break;
                default: return null;
            }
            float width = atlas.width / 3f;
            float height = atlas.height / 2f;
            Rect cell = new Rect((index % 3) * width, (index < 3 ? 1 : 0) * height, width, height);
            Sprite sprite = Sprite.Create(atlas, cell, new Vector2(.5f,.5f), 100f);
            sprite.name = "Icono_Ludoteca_" + page;
            atlasIcons[page] = sprite;
            return sprite;
        }
        switch(page)
        {
            case Page.Culture:return LoadPhoto("Ludoteca/Gallery/vestimenta_mazahua");
            case Page.Gallery:return LoadPhoto("Ludoteca/Gallery/danza_pastoras");
            case Page.Fauna:return XunjuuFaunaCatalog.GetSprite(XunjuuFaunaCatalog.Find("venado_cola_blanca"));
            case Page.Language:return Resources.Load<Sprite>("Sprites/Items/maiz");
            case Page.MiniGame:return LoadPhoto("Ludoteca/Minigames/sol_felicidades_mazahua");
            case Page.Survey:return LoadPhoto("Sprites/TextureTerrain/flor");
            default:return null;
        }
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

    private static void AddOutline(GameObject target, Color color, float thickness)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(thickness, -thickness);
    }

    private void AddTextilePattern(Transform parent, float anchorY)
    {
        const int motifCount = 11;
        for (int index = 0; index < motifCount; index++)
        {
            float x = 0.08f + index * 0.084f;
            Color accent = index % 3 == 0 ? Gold : index % 3 == 1 ? TextileGrana : TextileBlue;
            CreateDiamond(parent, new Vector2(x, anchorY), 16f, accent, ForestDeep);
        }
    }

    private void AddContentOrnaments(Transform parent)
    {
        CreateRule(parent, 0.84f, ParchmentShade);
        CreateRule(parent, 0.045f, ParchmentShade);
        for (int index = 0; index < 9; index++)
            CreateDiamond(parent, new Vector2(.38f + index * .03f, .817f), 7f,
                index % 2 == 0 ? TextileGrana : TextileBlue, Parchment);
        CreateCardIcon(parent, ElementIcon(2), new Vector2(.045f,.91f), new Vector2(56f,56f));
        CreateCardIcon(parent, ElementIcon(0), new Vector2(.955f,.91f), new Vector2(56f,56f));
    }

    private static void CreateRule(Transform parent, float anchorY, Color color)
    {
        GameObject line = new GameObject("Linea_Ornamental", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.055f, anchorY);
        rect.anchorMax = new Vector2(0.945f, anchorY);
        rect.sizeDelta = new Vector2(0f, 2f);
        Image image = line.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void CreateDiamond(Transform parent, Vector2 anchor, float size, Color outerColor, Color innerColor)
    {
        GameObject diamond = new GameObject("Motivo_Textil", typeof(RectTransform), typeof(Image));
        diamond.transform.SetParent(parent, false);
        RectTransform diamondRect = diamond.GetComponent<RectTransform>();
        diamondRect.anchorMin = anchor;
        diamondRect.anchorMax = anchor;
        diamondRect.sizeDelta = new Vector2(size, size);
        diamondRect.anchoredPosition = Vector2.zero;
        diamondRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image diamondImage = diamond.GetComponent<Image>();
        diamondImage.color = outerColor;
        diamondImage.raycastTarget = false;

        GameObject center = new GameObject("Centro", typeof(RectTransform), typeof(Image));
        center.transform.SetParent(diamond.transform, false);
        RectTransform centerRect = center.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(size * 0.42f, size * 0.42f);
        centerRect.anchoredPosition = Vector2.zero;
        Image centerImage = center.GetComponent<Image>();
        centerImage.color = innerColor;
        centerImage.raycastTarget = false;
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
        text.overflowMode = TextOverflowModes.Truncate;
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
        AddOutline(item, new Color(Parchment.r,Parchment.g,Parchment.b,.28f), 1f);
        Button button = item.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.13f,1.13f,1.13f,1f);
        colors.pressedColor = new Color(.77f,.77f,.77f,1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(.68f,.68f,.68f,.8f);
        colors.fadeDuration = .12f;
        button.colors = colors;
        TMP_Text labelText = Text(item.transform, label, size.y >= 68f ? 22 : 19, TextAnchor.MiddleCenter, Parchment);
        labelText.fontStyle = FontStyles.Bold;
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
