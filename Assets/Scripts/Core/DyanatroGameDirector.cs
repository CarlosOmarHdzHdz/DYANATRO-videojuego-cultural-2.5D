using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;

public class DyanatroGameDirector : MonoBehaviour
{
    // Palette derived from the textile-color study supplied with the project.
    // It is used only by the presentation layer of the main menu.
    private static readonly Color MenuIndigo = new Color(0.055f, 0.075f, 0.18f, 1f);
    private static readonly Color MenuIndigoLight = new Color(0.12f, 0.17f, 0.34f, 1f);
    private static readonly Color MenuGrana = new Color(0.68f, 0.12f, 0.24f, 1f);
    private static readonly Color MenuMagenta = new Color(0.78f, 0.18f, 0.42f, 1f);
    private static readonly Color MenuYellow = new Color(0.96f, 0.72f, 0.18f, 1f);
    private static readonly Color MenuGreen = new Color(0.19f, 0.52f, 0.30f, 1f);
    private static readonly Color MenuCream = new Color(0.96f, 0.92f, 0.80f, 1f);

    public bool IsInPrologue => inPrologue;
    public bool IsGameplayHudVisible => gameStarted && !inPrologue && !IsIntroVideoVisible && !deathScreenShown;
    public bool IsIntroVideoVisible => introVideoPanel != null && introVideoPanel.activeInHierarchy;
    public bool IsTimelinePrologueVisible => prologuePanel != null && prologuePanel.activeInHierarchy;
    public bool IsControlsSidebarVisible => controlsSidebar != null && controlsSidebar.activeInHierarchy;
    // ========================================================================
    // Xunjuú v0.1 - Dirección editable del nivel 1
    // Acción: conservar coleccionables, bosque y milpas creados en Hierarchy.
    // ========================================================================
    [Header("Prefabs")]
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private GameObject[] additionalTreePrefabs;
    [SerializeField] private GameObject cloudPrefab;
    [SerializeField] private GameObject[] animalPrefabs;

    [Header("Audio")]
    [SerializeField] private AudioClip backgroundMusic;
    [Tooltip("Volumen de la musica mientras se muestra el menu inicial.")]
    [SerializeField, Range(0.2f, 1f)] private float menuMusicVolume = 0.82f;
    [Tooltip("Reduccion aplicada al entrar a la partida. 0.55 equivale a bajar 55 por ciento.")]
    [SerializeField, Range(0f, 0.8f)] private float gameplayMusicReduction = 0.55f;
    [SerializeField, Min(0.1f)] private float musicFadeDuration = 0.85f;

    [Header("Sprites UI y ambiente")]
    [SerializeField] private Sprite menuBackgroundSprite;
    [SerializeField] private Sprite playerMenuSprite;
    [SerializeField] private Sprite enemyMenuSprite;
    [SerializeField] private Sprite treeMenuSprite;
    [SerializeField] private Sprite cloudMenuSprite;
    [SerializeField] private Sprite bushSprite;
    [SerializeField] private Sprite flowerSprite;

    [Header("Environment")]
    [Tooltip("Cantidad objetivo de arboles dentro del bosque de cada Terrain.")]
    [SerializeField, Min(1)] private int treesPerTerrain = 80;
    [Tooltip("Separacion horizontal minima entre arboles del mismo bosque.")]
    [SerializeField, Min(1f)] private float treeMinimumSpacing = 10f;
    [Tooltip("Radio utilizado para distribuir cada bosque alrededor de su zona.")]
    [SerializeField, Min(12f)] private float forestRadiusPerTerrain = 90f;
    [SerializeField] private int bushCount = 1320;
    [SerializeField] private int cloudCount = 26;
    [SerializeField] private float treeMinDistanceFromPlayer = 6f;
    [SerializeField] private float maxTreeSlope = 33f;
    [SerializeField] private float mountainSlope = 28f;
    [SerializeField] private float vegetationSink = 0.22f;
    [SerializeField] private int tallGrassPatchCount = 380;
    [SerializeField] private float tallGrassHeight = 1.15f;
    [SerializeField] private float tallGrassMinDistanceFromPlayer = 1.35f;
    [SerializeField] private float tallGrassRadius = 105f;
    [SerializeField] private int cornFieldCount = 10;
    [SerializeField] private int cornRowsPerField = 5;
    [SerializeField] private int cornStalksPerRow = 10;
    [SerializeField] private float cornFieldRadius = 105f;
    [SerializeField, Min(12f)] private float cornFieldMinimumSpacing = 28f;
    [SerializeField] private int animalsPerWordMin = 2;
    [SerializeField] private int animalsPerWordMax = 4;
    [SerializeField] private float animalsPerWordRadius = 6f;
    [SerializeField] private float wordCollectibleMinDistanceFromPlayer = 42f;
    [SerializeField] private float wordCollectibleExplorationRadius = 155f;
    [SerializeField] private float wordCollectibleMinDistanceFromEnemy = 38f;
    [SerializeField] private float wordCollectibleMinDistanceBetweenWords = 45f;
    [SerializeField] private TerrainLayer grassTerrainLayer;
    [SerializeField] private TerrainLayer mountainTerrainLayer;

    [Header("Xunjuu v0.1 - Progresion de niveles")]
    [Tooltip("Cantidad de flores necesarias para terminar el primer nivel.")]
    [SerializeField, Min(1)] private int mazahuaWordGoal = 5;
    [Tooltip("Cantidad de flores que se crean como objetos editables.")]
    [SerializeField, Min(1)] private int levelOneCollectibleCount = 5;
    [Tooltip("Espacio libre alrededor de flores, enemigos y puntos importantes al generar arboles.")]
    [SerializeField, Min(3f)] private float treeClearanceFromObjectives = 9f;
    [Tooltip("Ancho libre de las rutas directas entre el jugador y las flores.")]
    [SerializeField, Min(2f)] private float treeClearanceFromRoutes = 4.5f;
    [Tooltip("Objeto de Hierarchy que entrega el macuahuitl despues de las flores.")]
    [SerializeField] private XunjuuSecondaryMissionWeaponReward levelOneWeaponReward;
    [Tooltip("Controlador del segundo nivel que inicia despues de las cuatro flores.")]
    [SerializeField] private XunjuuLevel2KillMission levelTwoMission;

    [Header("Xunjuu v0.1 - Ritmo del prologo")]
    [SerializeField, Range(4f, 10f)] private float prologueBeatDuration = 6.75f;
    [SerializeField, Range(0.8f, 3f)] private float prologueCameraMoveDuration = 1.8f;
    [SerializeField, Range(0.6f, 1f)] private float introVideoPlaybackSpeed = 0.88f;

    [Header("Xunjuu v0.1 - Duracion de mensajes")]
    [SerializeField, Range(2f, 8f)] private float missionFeedbackDuration = 3.8f;
    [SerializeField, Range(3f, 10f)] private float completedFeedbackDuration = 6f;

    [Header("Xunjuu v0.1 - Guia compacta de controles")]
    [Tooltip("Muestra la barra de ayuda al comenzar la partida.")]
    [SerializeField] private bool showControlsSidebar = true;
    [Tooltip("Posicion desde la esquina superior derecha, debajo del nombre del protagonista.")]
    [SerializeField] private Vector2 controlsSidebarPosition = new Vector2(-22f, -180f);
    [SerializeField] private Vector2 controlsSidebarSize = new Vector2(440f, 304f);

    [Header("Xunjuu v0.1 - Ludoteca digital")]
    [Tooltip("Abre la ludoteca interna despues de completar el combate final.")]
    [SerializeField] private bool openLudotecaAfterLevelCompletion = true;
    [SerializeField, Range(2f, 12f)] private float ludotecaCompletionDelay = 7f;

    private PlayerController player;
    private Transform playerTransform;
    private Camera mainCamera;
    private Canvas uiCanvas;
    private GameObject mainMenuPanel;
    private GameObject pausePanel;
    private GameObject deathPanel;
    private GameObject prologuePanel;
    private GameObject introVideoPanel;
    private GameObject hudRoot;
    private GameObject controlsSidebar;
    private XunjuuLudotecaBrowser ludotecaBrowser;
    private Button introSkipButton;
    private Button prologueSkipButton;
    private Image healthFill;
    private RawImage introVideoImage;
    private GameObject introVideoTranslationOverlay;
    private Text introVideoSpanishText;
    private Text introVideoMazahuaText;
    private VideoPlayer introVideoPlayer;
    private RenderTexture introVideoTexture;
    private Sprite runtimeTallGrassSprite;
    private Sprite runtimeCornSprite;
    private Sprite runtimeMazahuaFlowerSprite;
    private Sprite runtimeGroundBushSprite;
    private Sprite runtimeGroundFlowerSprite;
    private Sprite runtimeGroundStoneSprite;
    private Sprite runtimeGuideArrowSprite;
    private Material runtimeFurrowMaterial;
    private Text objectiveText;
    private RectTransform guideArrowRect;
    private GameObject collectibleGuideRoot;
    private Image guideArrowImage;
    private Text guideDistanceText;
    private Text protagonistNameText;
    private Text missionFeedbackText;
    private Outline missionFeedbackOutline;
    private Shadow missionFeedbackShadow;
    private AudioSource musicSource;
    private Coroutine musicVolumeRoutine;
    private Vector3 cameraVelocity;
    private bool gameStarted;
    private bool inPrologue;
    private bool isPaused;
    private bool deathScreenShown;
    private bool skipPrologueRequested;
    private bool skipIntroRequested;
    private bool missionFeedbackCompletedVisible;
    private bool levelOneCompleted;
    private bool terrainPresentationApplied;
    private int mazahuaWordsCollected;
    private readonly List<GameObject> delayedEnemies = new List<GameObject>();
    private readonly HashSet<string> collectedMazahuaWords = new HashSet<string>();
    private readonly Dictionary<Sprite, float> transparentBottomCache = new Dictionary<Sprite, float>();
    private readonly Dictionary<Texture2D, Texture2D> readableTextureCache = new Dictionary<Texture2D, Texture2D>();
    private readonly string[] mazahuaWords =
    {
        "jñatjo|mazahua|Mazahua: jñatjo.|Español: mazahua.",
        "jñaa|palabra / voz / lengua|Mazahua: na jñaa.|Español: una palabra.",
        "jñiñi|pueblo|Mazahua: na jñiñi.|Español: un pueblo.",
        "xiskuama|documento|Mazahua: xiskuama.|Español: documento.",
        "b'epji|trabajo|Mazahua: b'epji.|Español: trabajo.",
        "skuama|papel / libro|Mazahua: skuama.|Español: papel o libro.",
        "ngunxorú|escuela|Mazahua: ngunxorú.|Español: escuela.",
        "Xonijomu|lugar de memoria|Mazahua: Xonijomu.|Español: lugar de memoria."
    };
    private readonly string[] introVideoSpanishLines =
    {
        "Esta aventura de ficción está ambientada en San Felipe del Progreso.",
        "Alimentos: maiz, frijol, haba, calabaza, papa, trigo y hortalizas.",
        "Mateo Jnatr'o es P'antreje Jnatr'o, el cazador que habla.",
        "Lluvia, milpa y alimento en peligro.",
        "Mateo debe ser el cazador que habla y proteger al pueblo.",
        "Mateo comienza sin arma: primero aprende a caminar, saltar, golpear y usar el morral.",
        "Cinco flores-palabra recuperadas: comienza el registro de la fauna, todavía sin arma.",
        "Seis animales desorientados deben ser resguardados y registrados antes de seguir el rastro de los Dyanatr'o.",
        "Tras vencer a los cinco Dyanatr'o, el giro protector abre el combate contra Ocelotl.",
        "Con el camino libre, Mateo puede volver y compartir las palabras recuperadas."
    };
    private readonly string[] introVideoMazahuaLines =
    {
        "San Felipe del Progreso mi na jñiñi jñatjo.",
        "Jñona: maiz, frijol, haba, calabaza, papa, trigo, hortalizas.",
        "Mateo Jnatr'o: P'antreje Jnatr'o, mepjanteje ko na jñaa.",
        "Dyeb'e, milpa, jñona: dya joo.",
        "Mateo: P'antreje Jnatr'o; tsasú na jñiñi.",
        "Tsansa ndájná. Pesi Macuahuitl.",
        "Tsansa tsicha ndájná. Tsasú na jñiñi.",
        "Jñincho Dyanatr'o.",
        "Macuahuitl. Orbitasword.",
        "Tsicha ndájná: Otontecuhtli. Jñiñi tsasú."
    };

    void Awake()
    {
        mainCamera = Camera.main;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.GetComponent<PlayerController>();
            playerTransform = playerObject.transform;

            // Hoja completa del protagonista: se activa al obtener el arma.
            if (playerObject.GetComponent<XunjuuCompleteSpriteAnimator>() == null)
                playerObject.AddComponent<XunjuuCompleteSpriteAnimator>();
        }

        ResolveProgressionReferences();
        // Aplicar el acabado antes del primer frame evita mostrar el terreno
        // cuadriculado mientras inicia la escena.
        ApplyExistingTerrainTexture();
        if (GetComponent<XunjuuEnvironment3D>() == null) gameObject.AddComponent<XunjuuEnvironment3D>();
        if (GetComponent<XunjuuSceneVisualPolicy>() == null) gameObject.AddComponent<XunjuuSceneVisualPolicy>();
        if (GetComponent<XunjuuFaunaCaptureSystem>() == null) gameObject.AddComponent<XunjuuFaunaCaptureSystem>();
    }
    void Start()
    {
        BuildInterface();
        SubscribeLudotecaToLevelCompletion();
        StartMusic();
        ImproveCameraStart();
        ApplySunsetMood();
        RemoveLegacyFogObjects();
        ApplyExistingTerrainTexture();
        DisableUnsupportedTerrainTreeSystem();
        DisableGeneratedForest();
        // Xunjuu v0.1 - ACCION: conservar los bosques editados en Hierarchy.
        SpawnGroundedForest(false);
        SpawnGroundDecorations();
        SpawnTallGrass();
        SpawnCornMilpas();
        SpawnMazahuaWordCollectibles();
        PrepareInitialGameState();
        SpawnClouds();
        EnsureLevelOptimizer();
        AttachDepthSorters();
        StartCoroutine(AttachDepthSortersDelayed());
    }

    // ========================================================================
    // Xunjuu v0.1 - Referencias de progresion
    // ACCION: recuperar objetos si la escena fue abierta en otra computadora.
    // MODIFICACION: las referencias tambien pueden asignarse desde Inspector.
    // ========================================================================
    void ResolveProgressionReferences()
    {
        if (levelOneWeaponReward == null)
            levelOneWeaponReward = FindFirstObjectByType<XunjuuSecondaryMissionWeaponReward>(FindObjectsInactive.Include);
        if (levelTwoMission == null)
            levelTwoMission = FindFirstObjectByType<XunjuuLevel2KillMission>(FindObjectsInactive.Include);
    }
    void Update()
    {
        UpdateHealthHud();
        UpdateCollectibleGuide();

        if (!gameStarted && mainMenuPanel != null && mainMenuPanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
            BeginGame();

        if (gameStarted && !inPrologue
            && (ludotecaBrowser == null || !ludotecaBrowser.BlocksPauseInput)
            && Input.GetKeyDown(KeyCode.Escape))
            SetPause(!isPaused);

        if (gameStarted && !deathScreenShown && player != null && player.IsDead())
        {
            deathScreenShown = true;
            StartCoroutine(ShowDeathScreenAfterDelay());
        }
    }

    void LateUpdate()
    {
        FollowPlayerCamera();
    }

    // ACCIÓN Xunjuú v0.1: usar un solo controlador para optimizar objetos lejanos.
    void EnsureLevelOptimizer()
    {
        XunjuuLevelOptimizer optimizer = GetComponent<XunjuuLevelOptimizer>();
        if (optimizer == null)
            optimizer = gameObject.AddComponent<XunjuuLevelOptimizer>();
        optimizer.RefreshObjects();
    }

    void PrepareInitialGameState()
    {
        ResolveProgressionReferences();
        if (levelTwoMission != null)
            levelTwoMission.ResetMission();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            delayedEnemies.Add(enemy);
            enemy.SetActive(false);
        }

        SetPlayerControl(false);
        Time.timeScale = 0f;
        mainMenuPanel.SetActive(true);
        pausePanel.SetActive(false);
        if (deathPanel != null)
            deathPanel.SetActive(false);
        prologuePanel.SetActive(false);
        if (introVideoPanel != null)
            introVideoPanel.SetActive(false);
        hudRoot.SetActive(false);
    }

    void BuildInterface()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Dyanatro_UI");
        uiCanvas = canvasObject.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 100;
        uiCanvas.pixelPerfect = true;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        BuildMainMenuInterface();

        pausePanel = CreateFullPanel("MenuPausa", new Color(MenuIndigo.r, MenuIndigo.g, MenuIndigo.b, 0.9f));
        CreateTextileBand(pausePanel.transform, "Banda_Superior", new Vector2(0f, 496f));
        CreateTextileBand(pausePanel.transform, "Banda_Inferior", new Vector2(0f, -496f));
        GameObject pauseFrame = CreateMenuPanel(
            pausePanel.transform,
            "MarcoPausa",
            Vector2.zero,
            new Vector2(450f, 640f),
            new Color(0.035f, 0.055f, 0.14f, 0.94f),
            new Color(MenuCream.r, MenuCream.g, MenuCream.b, 0.8f),
            2.5f);
        Text pauseTitle = CreateLabel(pauseFrame.transform, "PAUSA", 56, new Vector2(0f, 180f), new Vector2(370f, 76f), MenuCream);
        ApplyMenuHeadingStyle(pauseTitle, MenuCream);
        CreateMenuRule(pauseFrame.transform, new Vector2(0f, 126f), new Vector2(334f, 3f), MenuYellow);
        CreateButton(pauseFrame.transform, "Continuar", new Vector2(0f, 55f), () => SetPause(false));
        CreateButton(pauseFrame.transform, "Ludoteca", new Vector2(0f, -30f), OpenLudotecaFromPause);
        CreateButton(pauseFrame.transform, "Reiniciar", new Vector2(0f, -115f), RestartScene);
        CreateButton(pauseFrame.transform, "Controles", new Vector2(0f, -200f), () => {
            if (controlsSidebar != null) controlsSidebar.SetActive(!controlsSidebar.activeSelf);
        });
        CreateButton(pauseFrame.transform, "Menu inicial", new Vector2(0f, -285f), ReturnToMainMenu);

        deathPanel = CreateFullPanel("PantallaMuerte", new Color(0.035f, 0.006f, 0.008f, 0.92f));
        ApplyPanelSprite(deathPanel, menuBackgroundSprite, new Color(0.36f, 0.12f, 0.1f, 0.18f));
        AddUiSprite(deathPanel.transform, enemyMenuSprite, new Vector2(-420f, -180f), new Vector2(260f, 260f), new Color(0.62f, 0.55f, 0.7f, 0.8f));
        AddUiSprite(deathPanel.transform, treeMenuSprite, new Vector2(460f, -165f), new Vector2(330f, 330f), new Color(0.72f, 0.8f, 0.6f, 0.48f));
        AddUiSprite(deathPanel.transform, cloudMenuSprite, new Vector2(0f, 265f), new Vector2(720f, 250f), new Color(1f, 0.86f, 0.72f, 0.22f));
        CreateLabel(deathPanel.transform, "Has caido", 72, new Vector2(0f, 145f), new Vector2(900f, 110f), new Color(1f, 0.78f, 0.48f));
        CreateLabel(deathPanel.transform, "Las Dyanatr'o avanzan sobre la memoria del bosque", 30, new Vector2(0f, 55f), new Vector2(1150f, 80f), Color.white);
        CreateLabel(deathPanel.transform, "Vuelve a levantarte y recupera las palabras mazahuas.", 26, new Vector2(0f, 5f), new Vector2(1050f, 65f), new Color(0.82f, 0.92f, 0.78f));
        CreateButton(deathPanel.transform, "Reintentar", new Vector2(0f, -105f), RestartScene);
        CreateButton(deathPanel.transform, "Menu inicial", new Vector2(0f, -195f), ReturnToMainMenu);

        prologuePanel = CreateFullPanel("Prologo", new Color(0f, 0f, 0f, 0.48f));
        Text prologueTitle = CreateLabel(prologuePanel.transform, "PROLOGO", 86, new Vector2(0f, 250f), new Vector2(1150f, 120f), new Color(1f, 0.78f, 0.24f));
        ApplyPixelArcadeStyle(prologueTitle, new Color(1f, 0.78f, 0.24f), 4f);
        prologueSkipButton = CreateButton(prologuePanel.transform, "Saltar prologo", new Vector2(690f, 455f), SkipPrologue);

        introVideoPanel = CreateFullPanel("IntroVideoPrologo", Color.black);
        GameObject introImageObject = new GameObject("IntroVideoImage");
        introImageObject.transform.SetParent(introVideoPanel.transform, false);
        RectTransform introImageRect = introImageObject.AddComponent<RectTransform>();
        introImageRect.anchorMin = Vector2.zero;
        introImageRect.anchorMax = Vector2.one;
        introImageRect.offsetMin = Vector2.zero;
        introImageRect.offsetMax = Vector2.zero;
        introVideoImage = introImageObject.AddComponent<RawImage>();
        introVideoImage.color = Color.white;
        BuildIntroVideoTranslationOverlay();
        introSkipButton = CreateButton(introVideoPanel.transform, "Saltar intro", new Vector2(690f, 455f), SkipIntroVideo);
        introVideoPanel.SetActive(false);

        hudRoot = new GameObject("HUD", typeof(RectTransform));
        hudRoot.transform.SetParent(uiCanvas.transform, false);
        RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;
        objectiveText = CreateLabel(hudRoot.transform, GetMissionIntroText(), 24, new Vector2(0f, 488f), new Vector2(1200f, 72f), Color.white);
        objectiveText.alignment = TextAnchor.MiddleCenter;
        protagonistNameText = CreateLabel(
            hudRoot.transform,
            "Mazahua: P'antreje Jnatr'o\nEspañol: El cazador que habla",
            18,
            new Vector2(668f, 428f),
            new Vector2(450f, 58f),
            new Color(1f, 0.9f, 0.58f));
        protagonistNameText.alignment = TextAnchor.MiddleRight;

        BuildControlsSidebar(hudRoot.transform);

        BuildCollectibleGuide(hudRoot.transform);

        missionFeedbackText = CreateLabel(hudRoot.transform, string.Empty, 42, new Vector2(0f, 360f), new Vector2(1450f, 150f), new Color(1f, 0.88f, 0.42f));
        missionFeedbackText.alignment = TextAnchor.MiddleCenter;
        missionFeedbackOutline = missionFeedbackText.gameObject.AddComponent<Outline>();
        missionFeedbackShadow = missionFeedbackText.gameObject.AddComponent<Shadow>();
        ApplyMissionFeedbackStyle(false);

        // Xunjuú v0.1 - ACCION: crear el navegador al final para que siempre
        // aparezca delante del menu, del prologo y del HUD.
        GameObject ludotecaObject = new GameObject("Ludoteca_Navegador_Interno");
        ludotecaObject.transform.SetParent(uiCanvas.transform, false);
        ludotecaBrowser = ludotecaObject.AddComponent<XunjuuLudotecaBrowser>();
        ludotecaBrowser.Initialize(uiCanvas, treeMenuSprite, flowerSprite, cloudMenuSprite);
    }

    void BuildCollectibleGuide(Transform parent)
    {
        // A fixed bottom-edge compass cannot drift onto the character's face.
        collectibleGuideRoot = new GameObject("Guia_Objetivo", typeof(RectTransform), typeof(Image));
        collectibleGuideRoot.transform.SetParent(parent, false);
        RectTransform guideRect = collectibleGuideRoot.GetComponent<RectTransform>();
        guideRect.anchorMin = guideRect.anchorMax = new Vector2(.5f, 0f);
        guideRect.pivot = new Vector2(.5f, 0f);
        guideRect.anchoredPosition = new Vector2(0f, 24f);
        guideRect.sizeDelta = new Vector2(460f, 54f);
        Image background = collectibleGuideRoot.GetComponent<Image>();
        background.color = new Color(.07f, .12f, .10f, .82f);
        background.raycastTarget = false;
        GameObject guideObject = new GameObject("Direccion_Objetivo");
        guideObject.transform.SetParent(collectibleGuideRoot.transform, false);
        guideArrowRect = guideObject.AddComponent<RectTransform>();
        guideArrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        guideArrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        guideArrowRect.anchoredPosition = new Vector2(-204f, 0f);
        guideArrowRect.sizeDelta = new Vector2(28f, 28f);
        guideArrowImage = guideObject.AddComponent<Image>();
        guideArrowImage.sprite = GetGuideArrowSprite();
        guideArrowImage.color = new Color(.90f, .77f, .46f, 1f);
        guideArrowImage.raycastTarget = false;
        guideDistanceText = CreateLabel(collectibleGuideRoot.transform, string.Empty, 21,
            new Vector2(22f, 0f), new Vector2(388f, 44f), new Color(.95f, .93f, .84f));
        guideDistanceText.alignment = TextAnchor.MiddleLeft;
        guideDistanceText.horizontalOverflow = HorizontalWrapMode.Overflow;
        SetCollectibleGuideVisible(false);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    void UpdateCollectibleGuide()
    {
        bool canShowGuide = gameStarted && !inPrologue && !IsOpeningTutorialActive && !isPaused && !deathScreenShown && playerTransform != null && mainCamera != null;
        if (!canShowGuide)
        {
            SetCollectibleGuideVisible(false);
            return;
        }

        Transform target = null;
        string targetLabel = string.Empty;
        MazahuaWordCollectible nearest = levelTwoMission == null || levelTwoMission.CurrentPhase == XunjuuLevel2KillMission.MissionPhase.Locked
            ? FindNearestWordCollectible() : null;
        if (nearest != null)
        {
            target = nearest.transform;
            targetLabel = "Siguiente flor-palabra";
        }
        else if (levelTwoMission != null)
        {
            target = levelTwoMission.FindNearestActiveObjective(playerTransform.position);
            targetLabel = levelTwoMission.GetGuideLabel();
        }

        if (target == null)
        {
            SetCollectibleGuideVisible(false);
            return;
        }

        SetCollectibleGuideVisible(true);

        Vector3 toTarget = target.position - playerTransform.position;
        float distance = new Vector2(toTarget.x, toTarget.z).magnitude;
        Vector3 flatRight = mainCamera.transform.right;
        Vector3 flatForward = mainCamera.transform.forward;
        flatRight.y = 0f;
        flatForward.y = 0f;
        flatRight.Normalize();
        flatForward.Normalize();

        Vector2 guideDirection = new Vector2(Vector3.Dot(toTarget, flatRight), Vector3.Dot(toTarget, flatForward));
        if (guideDirection.sqrMagnitude < 0.001f)
            guideDirection = Vector2.up;
        guideDirection.Normalize();

        if (guideArrowRect != null)
        {
            float angle = Mathf.Atan2(guideDirection.y, guideDirection.x) * Mathf.Rad2Deg - 90f;
            guideArrowRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (guideDistanceText != null)
            guideDistanceText.text = $"{targetLabel}: {Mathf.RoundToInt(distance)} m";
    }

    MazahuaWordCollectible FindNearestWordCollectible()
    {
        MazahuaWordCollectible[] collectibles = FindObjectsByType<MazahuaWordCollectible>(FindObjectsSortMode.None);
        MazahuaWordCollectible nearest = null;
        float bestDistance = float.MaxValue;

        foreach (MazahuaWordCollectible collectible in collectibles)
        {
            if (collectible == null || !collectible.gameObject.activeInHierarchy)
                continue;

            float distance = XZDistance(playerTransform.position, collectible.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = collectible;
            }
        }

        return nearest;
    }

    void SetCollectibleGuideVisible(bool visible)
    {
        if (collectibleGuideRoot != null) collectibleGuideRoot.SetActive(visible);
        if (guideArrowImage != null)
            guideArrowImage.enabled = visible;
        if (guideDistanceText != null)
            guideDistanceText.enabled = visible;
    }

    Sprite GetGuideArrowSprite()
    {
        if (runtimeGuideArrowSprite != null)
            return runtimeGuideArrowSprite;

        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                // Open pixel chevron: no solid shaft, oversized fill or outline.
                int tipDistance = Mathf.Abs(2 * x - 31);
                bool chevron = y >= 9 && y <= 27 && Mathf.Abs(2 * y + tipDistance - 54) <= 4;
                texture.SetPixel(x, y, chevron ? Color.white : Color.clear);
            }
        }

        texture.Apply(false, false);
        runtimeGuideArrowSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
        runtimeGuideArrowSprite.name = "Flecha_Guia_Coleccionables";
        return runtimeGuideArrowSprite;
    }

    void BuildMainMenuInterface()
    {
        mainMenuPanel = CreateFullPanel("MenuInicio", MenuIndigo);
        AddMenuBackground(mainMenuPanel.transform);
        CreateTextileBand(mainMenuPanel.transform, "Banda_Textil_Superior", new Vector2(0f, 496f));
        CreateTextileBand(mainMenuPanel.transform, "Banda_Textil_Inferior", new Vector2(0f, -496f));

        // Existing scene sprites stay decorative and do not participate in gameplay.
        AddUiSprite(mainMenuPanel.transform, cloudMenuSprite, new Vector2(-630f, 300f), new Vector2(390f, 170f), new Color(1f, 1f, 1f, 0.18f));
        AddUiSprite(mainMenuPanel.transform, cloudMenuSprite, new Vector2(535f, 315f), new Vector2(390f, 170f), new Color(1f, 1f, 1f, 0.12f));
        AddUiSprite(mainMenuPanel.transform, treeMenuSprite, new Vector2(-765f, -190f), new Vector2(380f, 420f), new Color(0.68f, 0.84f, 0.52f, 0.58f));
        AddUiSprite(mainMenuPanel.transform, playerMenuSprite, new Vector2(-475f, -122f), new Vector2(300f, 360f), Color.white);
        CreateTextileColumn(mainMenuPanel.transform, new Vector2(-700f, 35f), 5);

        GameObject frame = CreateMenuPanel(
            mainMenuPanel.transform,
            "Marco_Principal",
            new Vector2(335f, 0f),
            new Vector2(690f, 610f),
            new Color(MenuIndigoLight.r, MenuIndigoLight.g, MenuIndigoLight.b, 0.94f),
            MenuCream,
            2.5f);

        CreateMenuRule(frame.transform, new Vector2(0f, 240f), new Vector2(550f, 3f), MenuYellow);
        Text title = CreateLabel(frame.transform, "DYANATR'O", 76, new Vector2(0f, 168f), new Vector2(610f, 105f), MenuCream);
        ApplyMenuHeadingStyle(title, MenuCream);
        Text subtitle = CreateLabel(frame.transform, "LAS SOMBRAS DEL OLVIDO", 30, new Vector2(0f, 98f), new Vector2(610f, 54f), MenuYellow);
        subtitle.fontStyle = FontStyle.Bold;
        CreateLabel(frame.transform, "MEMORIA  ·  TERRITORIO  ·  PALABRA", 17, new Vector2(0f, 45f), new Vector2(580f, 36f), new Color(0.72f, 0.88f, 0.70f, 1f));
        CreateMenuRule(frame.transform, new Vector2(0f, 15f), new Vector2(470f, 2f), new Color(MenuCream.r, MenuCream.g, MenuCream.b, 0.56f));

        CreateTextileMenuButton(frame.transform, "INICIAR", new Vector2(0f, -67f), BeginGame, MenuYellow);
        CreateTextileMenuButton(frame.transform, "LUDOTECA", new Vector2(0f, -158f), OpenLudoteca, MenuMagenta);
        CreateTextileMenuButton(frame.transform, "SALIR", new Vector2(0f, -249f), QuitGame, MenuGreen);

        CreateTextileColumn(frame.transform, new Vector2(-292f, 0f), 4);
        CreateTextileColumn(frame.transform, new Vector2(292f, 0f), 4);
    }

    void AddMenuBackground(Transform parent)
    {
        if (parent == null)
            return;

        GameObject background = new GameObject("Fondo_Visual_Menu");
        background.transform.SetParent(parent, false);
        background.transform.SetAsFirstSibling();
        RectTransform rect = background.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = background.AddComponent<Image>();
        image.sprite = menuBackgroundSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = menuBackgroundSprite != null
            ? new Color(0.23f, 0.30f, 0.52f, 0.18f)
            : new Color(0.03f, 0.04f, 0.11f, 0.18f);
        image.raycastTarget = false;
    }

    void CreateTextileBand(Transform parent, string name, Vector2 position)
    {
        GameObject band = CreateMenuPanel(
            parent,
            name,
            position,
            new Vector2(1940f, 80f),
            new Color(0.025f, 0.035f, 0.09f, 0.96f),
            new Color(MenuCream.r, MenuCream.g, MenuCream.b, 0.42f),
            1f);

        const int motifCount = 25;
        float spacing = 68f;
        float firstPosition = -((motifCount - 1) * spacing) * 0.5f;
        for (int index = 0; index < motifCount; index++)
        {
            Color accent = index % 4 == 0 ? MenuYellow : index % 4 == 1 ? MenuGrana : index % 4 == 2 ? MenuGreen : MenuMagenta;
            CreateMenuDiamond(band.transform, new Vector2(firstPosition + index * spacing, 0f), 21f, accent, MenuIndigo);
        }
    }

    void CreateTextileColumn(Transform parent, Vector2 position, int count)
    {
        float firstY = (count - 1) * 27f;
        for (int index = 0; index < count; index++)
        {
            Color accent = index % 2 == 0 ? MenuYellow : MenuMagenta;
            CreateMenuDiamond(parent, new Vector2(position.x, position.y + firstY - index * 54f), 19f, accent, MenuIndigo);
        }
    }

    void CreateMenuDiamond(Transform parent, Vector2 position, float size, Color accent, Color centerColor)
    {
        GameObject diamond = new GameObject("Motivo_Rombo");
        diamond.transform.SetParent(parent, false);
        RectTransform diamondRect = diamond.AddComponent<RectTransform>();
        diamondRect.sizeDelta = new Vector2(size, size);
        diamondRect.anchoredPosition = position;
        diamondRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image outer = diamond.AddComponent<Image>();
        outer.color = accent;
        outer.raycastTarget = false;

        GameObject center = new GameObject("Centro_Rombo");
        center.transform.SetParent(diamond.transform, false);
        RectTransform centerRect = center.AddComponent<RectTransform>();
        centerRect.sizeDelta = new Vector2(size * 0.46f, size * 0.46f);
        Image inner = center.AddComponent<Image>();
        inner.color = centerColor;
        inner.raycastTarget = false;
    }

    GameObject CreateMenuPanel(Transform parent, string name, Vector2 position, Vector2 size, Color fill, Color borderColor, float borderThickness)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = panel.AddComponent<Image>();
        image.color = fill;
        image.raycastTarget = false;

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(borderThickness, -borderThickness);
        return panel;
    }

    void CreateMenuRule(Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject rule = new GameObject("Separador_Textil");
        rule.transform.SetParent(parent, false);
        RectTransform rect = rule.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = rule.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    void ApplyMenuHeadingStyle(Text label, Color fillColor)
    {
        if (label == null)
            return;

        label.fontStyle = FontStyle.Bold;
        label.color = fillColor;
        Shadow shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.025f, 0.07f, 0.96f);
        shadow.effectDistance = new Vector2(4f, -4f);
        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(MenuGrana.r, MenuGrana.g, MenuGrana.b, 0.76f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);
    }

    Button CreateTextileMenuButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action, Color accent)
    {
        GameObject buttonObject = new GameObject("MenuButton_" + text);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(470f, 72f);
        rect.anchoredPosition = position;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.035f, 0.055f, 0.14f, 0.98f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = new Color(1f, 1f, 1f, 1f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject accentBar = new GameObject("Acento_Color");
        accentBar.transform.SetParent(buttonObject.transform, false);
        RectTransform accentRect = accentBar.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0.5f);
        accentRect.anchorMax = new Vector2(0f, 0.5f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(9f, 58f);
        accentRect.anchoredPosition = new Vector2(8f, 0f);
        Image accentImage = accentBar.AddComponent<Image>();
        accentImage.color = accent;
        accentImage.raycastTarget = false;

        CreateMenuDiamond(buttonObject.transform, new Vector2(-187f, 0f), 20f, accent, MenuIndigo);
        Text label = CreateLabel(buttonObject.transform, text, 27, new Vector2(15f, 0f), new Vector2(350f, 64f), MenuCream);
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;

        Text arrow = CreateLabel(buttonObject.transform, ">", 32, new Vector2(190f, 0f), new Vector2(42f, 62f), accent);
        arrow.fontStyle = FontStyle.Bold;
        return button;
    }

    GameObject CreateFullPanel(string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(uiCanvas.transform, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    void ApplyPanelSprite(GameObject panel, Sprite sprite, Color color)
    {
        if (panel == null || sprite == null)
            return;

        Image image = panel.GetComponent<Image>();
        if (image == null)
            return;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = color;

        GameObject veil = new GameObject("Oscurecedor");
        veil.transform.SetParent(panel.transform, false);
        RectTransform rect = veil.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image veilImage = veil.AddComponent<Image>();
        veilImage.color = new Color(0.02f, 0.025f, 0.02f, 0.72f);
    }

    Image AddUiSprite(Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color)
    {
        if (sprite == null)
            return null;

        GameObject spriteObject = new GameObject("Sprite_" + sprite.name);
        spriteObject.transform.SetParent(parent, false);
        RectTransform rect = spriteObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = spriteObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    void StartMusic()
    {
        if (backgroundMusic == null)
            return;

        musicSource = gameObject.GetComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = menuMusicVolume;
        musicSource.spatialBlend = 0f;
        musicSource.Play();
    }

    // ========================================================================
    // Xunjuu v0.1 - Mezcla musical menu/juego
    // ACCION: iniciar con presencia y bajar 55 por ciento al jugar.
    // ========================================================================
    void SetGameplayMusicVolume()
    {
        float gameplayVolume = menuMusicVolume * (1f - gameplayMusicReduction);
        if (musicVolumeRoutine != null)
            StopCoroutine(musicVolumeRoutine);
        musicVolumeRoutine = StartCoroutine(FadeMusicVolume(gameplayVolume));
    }

    IEnumerator FadeMusicVolume(float targetVolume)
    {
        if (musicSource == null)
            yield break;

        float initialVolume = musicSource.volume;
        float elapsed = 0f;
        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / musicFadeDuration);
            musicSource.volume = Mathf.Lerp(initialVolume, targetVolume, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicVolumeRoutine = null;
    }

    void ImproveCameraStart()
    {
        if (mainCamera == null || playerTransform == null)
            return;

        Camera cameraComponent = mainCamera.GetComponent<Camera>();
        if (cameraComponent != null)
        {
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(0.48f, 0.72f, 0.94f, 1f);
            cameraComponent.orthographic = false;
            cameraComponent.fieldOfView = 46f;
            cameraComponent.nearClipPlane = 0.05f;
            cameraComponent.farClipPlane = 2500f;
        }

        CameraOrbit orbit = mainCamera.GetComponent<CameraOrbit>();
        if (orbit != null)
            orbit.enabled = false;

        foreach (MonoBehaviour behaviour in mainCamera.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null || behaviour == this)
                continue;

            string behaviourName = behaviour.GetType().Name;
            if (behaviourName == "CameraFollow" || behaviourName == "CameraOrbit")
                behaviour.enabled = false;
        }

        Vector3 desired = playerTransform.position + new Vector3(0f, 8.8f, -16f);
        mainCamera.transform.position = desired;
        mainCamera.transform.LookAt(playerTransform.position + Vector3.up * 3.7f);
    }

    void ApplySunsetMood()
    {
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.48f, 0.72f, 0.94f, 1f);
        }

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.68f, 0.76f, 0.72f);
        RenderSettings.fog = false;

        Light sun = FindFirstObjectByType<Light>();
        if (sun != null)
        {
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 0.92f;
            sun.transform.rotation = Quaternion.Euler(42f, -38f, 0f);
        }

        RemoveLegacyFogObjects();
    }

    // ========================================================================
    // Xunjuu v0.1 - Ambiente sin niebla
    // ACCION: desactivar la niebla global y retirar restos del sistema anterior.
    // ========================================================================
    void RemoveLegacyFogObjects()
    {
        RenderSettings.fog = false;
        string[] obsoleteRoots =
        {
            "Fondo_Cielo_Dyanatro",
            "Fondo_Atardecer_Dyanatro",
            "Niebla_Dyanatro",
            "Niebla_Horizonte_Dyanatro"
        };
        foreach (string rootName in obsoleteRoots)
        {
            GameObject obsolete = GameObject.Find(rootName);
            if (obsolete != null)
                DestroySceneObject(obsolete);
        }

        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.parent != null)
                continue;

            string objectName = candidate.name.ToLowerInvariant();
            if (objectName.Contains("niebla") || objectName.Contains("haze") || objectName.Contains("fog_dyanatro"))
                DestroySceneObject(candidate.gameObject);
        }
    }

    void FollowPlayerCamera()
    {
        if (mainCamera == null || playerTransform == null || !gameStarted || isPaused || inPrologue)
            return;

        Vector3 targetPosition = playerTransform.position + new Vector3(0f, 8.8f, -16f);
        mainCamera.transform.position = Vector3.SmoothDamp(mainCamera.transform.position, targetPosition, ref cameraVelocity, 0.18f);
        Quaternion targetRotation = Quaternion.LookRotation((playerTransform.position + Vector3.up * 3.7f) - mainCamera.transform.position);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, Time.deltaTime * 8f);
    }

    Text CreateLabel(Transform parent, string text, int size, Vector2 position, Vector2 dimensions, Color color)
    {
        GameObject labelObject = new GameObject("Text_" + text);
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.AddComponent<RectTransform>();
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;
        Text label = labelObject.AddComponent<Text>();
        label.text = text;
        label.font = GetUiFont();
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }

    Font GetUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    Font GetPixelUiFont()
    {
        Font font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        return font != null ? font : GetUiFont();
    }

    void ApplyPixelArcadeStyle(Text label, Color fillColor, float thickness)
    {
        if (label == null)
            return;

        label.font = GetPixelUiFont();
        label.fontStyle = FontStyle.Bold;
        label.color = fillColor;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;

        Shadow shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.55f, 0.08f, 0.05f, 0.95f);
        shadow.effectDistance = new Vector2(thickness * 1.8f, -thickness * 1.8f);

        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.04f, 0.05f, 0.12f, 1f);
        outline.effectDistance = new Vector2(thickness, -thickness);
    }

    void ApplyReadableTextStyle(Text label, Color fillColor, float thickness)
    {
        if (label == null)
            return;

        label.color = fillColor;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(thickness * 1.8f, -thickness * 1.8f);

        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.03f, 0.04f, 0.05f, 0.95f);
        outline.effectDistance = new Vector2(thickness, -thickness);
    }

    Sprite GetMazahuaCollectibleFlowerSprite()
    {
        if (runtimeMazahuaFlowerSprite != null)
            return runtimeMazahuaFlowerSprite;

        runtimeMazahuaFlowerSprite = Resources.Load<Sprite>("Sprites/Collectibles/TsirajnaNeDyebe");
        if (runtimeMazahuaFlowerSprite != null)
            return runtimeMazahuaFlowerSprite;

        Texture2D texture = Resources.Load<Texture2D>("Sprites/Collectibles/TsirajnaNeDyebe");
        if (texture != null)
        {
            runtimeMazahuaFlowerSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                64f);
            return runtimeMazahuaFlowerSprite;
        }

        return flowerSprite;
    }

    Sprite LoadResourceSprite(ref Sprite cache, string resourcePath, Sprite fallback = null)
    {
        if (cache != null)
            return cache;

        cache = Resources.Load<Sprite>(resourcePath);
        if (cache != null)
            return cache;

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            cache = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), 96f);
            return cache;
        }

        return fallback;
    }

    Button CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject("Button_" + text);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(348f, 66f);
        rect.anchoredPosition = position;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(MenuIndigoLight.r, MenuIndigoLight.g, MenuIndigoLight.b, 0.98f);

        Color accent = GetTextileButtonAccent(text);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject stripe = new GameObject("Acento");
        stripe.transform.SetParent(buttonObject.transform, false);
        RectTransform stripeRect = stripe.AddComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0f, 0f);
        stripeRect.anchorMax = new Vector2(0f, 1f);
        stripeRect.pivot = new Vector2(0f, 0.5f);
        stripeRect.anchoredPosition = Vector2.zero;
        stripeRect.sizeDelta = new Vector2(10f, 0f);
        Image stripeImage = stripe.AddComponent<Image>();
        stripeImage.color = accent;
        stripeImage.raycastTarget = false;

        CreateMenuDiamond(buttonObject.transform, new Vector2(-128f, 0f), 24f, accent, MenuIndigo);
        Text label = CreateLabel(buttonObject.transform, text, 25, new Vector2(14f, 0f), new Vector2(245f, 58f), MenuCream);
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        Text arrow = CreateLabel(buttonObject.transform, ">", 25, new Vector2(132f, 0f), new Vector2(28f, 50f), accent);
        arrow.raycastTarget = false;
        return button;
    }

    Color GetTextileButtonAccent(string text)
    {
        if (text == "Continuar" || text == "Reintentar" || text.StartsWith("Saltar"))
            return MenuYellow;
        if (text == "Ludoteca")
            return MenuMagenta;
        if (text == "Reiniciar")
            return MenuGrana;
        if (text == "Menu inicial")
            return MenuGreen;
        return MenuYellow;
    }

    // ========================================================================
    // Xunjuu v0.1 - Traduccion corregida sobre el video
    // ACCION: cubrir el texto antiguo integrado en el MP4 y mostrar un par
    // Mazahua/Espanol equivalente para cada una de sus diez escenas.
    // ========================================================================
    void BuildIntroVideoTranslationOverlay()
    {
        if (introVideoPanel == null)
            return;

        introVideoTranslationOverlay = new GameObject("Traduccion_Bilingue_Corregida");
        introVideoTranslationOverlay.transform.SetParent(introVideoPanel.transform, false);
        RectTransform overlayRect = introVideoTranslationOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.03f, 0.025f);
        overlayRect.anchorMax = new Vector2(0.97f, 0.30f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = introVideoTranslationOverlay.AddComponent<Image>();
        overlayImage.color = new Color(0.018f, 0.028f, 0.035f, 0.985f);
        overlayImage.raycastTarget = false;
        Outline overlayOutline = introVideoTranslationOverlay.AddComponent<Outline>();
        overlayOutline.effectColor = new Color(1f, 0.7f, 0.18f, 1f);
        overlayOutline.effectDistance = new Vector2(3f, -3f);

        introVideoSpanishText = CreateLabel(
            introVideoTranslationOverlay.transform,
            string.Empty,
            26,
            new Vector2(0f, 55f),
            new Vector2(1690f, 86f),
            Color.white);
        introVideoSpanishText.alignment = TextAnchor.MiddleLeft;
        introVideoSpanishText.fontStyle = FontStyle.Bold;
        ApplyReadableTextStyle(introVideoSpanishText, Color.white, 1.5f);

        introVideoMazahuaText = CreateLabel(
            introVideoTranslationOverlay.transform,
            string.Empty,
            24,
            new Vector2(0f, -55f),
            new Vector2(1690f, 86f),
            new Color(1f, 0.86f, 0.32f, 1f));
        introVideoMazahuaText.alignment = TextAnchor.MiddleLeft;
        introVideoMazahuaText.fontStyle = FontStyle.Bold;
        ApplyReadableTextStyle(introVideoMazahuaText, new Color(1f, 0.86f, 0.32f, 1f), 1.5f);

        UpdateIntroVideoTranslation(0);
    }

    void UpdateIntroVideoTranslation(int forcedIndex = -1)
    {
        if (introVideoSpanishText == null || introVideoMazahuaText == null)
            return;

        int count = Mathf.Min(introVideoSpanishLines.Length, introVideoMazahuaLines.Length);
        if (count <= 0)
            return;

        int index = forcedIndex;
        if (index < 0)
        {
            double length = introVideoPlayer != null ? introVideoPlayer.length : 0d;
            double time = introVideoPlayer != null ? introVideoPlayer.time : 0d;
            double segmentLength = length > 0.01d ? length / count : 1d;
            index = Mathf.FloorToInt((float)(time / segmentLength));
        }

        index = Mathf.Clamp(index, 0, count - 1);
        introVideoSpanishText.text = "ESPAÑOL   " + introVideoSpanishLines[index];
        // Preserve the original language source without presenting outdated quest
        // counts or newly written Spanish as a verified Mazahua translation.
        introVideoMazahuaText.text = index > 0 && index < 5
            ? "MAZAHUA · TEXTO ORIGINAL   " + introVideoMazahuaLines[index]
            : "MAZAHUA   Traducción del nuevo relato pendiente de revisión lingüística.";
    }

    // ========================================================================
    // Xunjuu v0.1 - Barra lateral de controles
    // ACCION: presentar los controles basicos sin cubrir la vida ni la mision.
    // MODIFICACION: posicion, tamano y visibilidad se ajustan desde Inspector.
    // ========================================================================
    void BuildControlsSidebar(Transform parent)
    {
        if (!showControlsSidebar || parent == null)
            return;

        controlsSidebar = new GameObject("Guia_Controles_Inicio");
        controlsSidebar.transform.SetParent(parent, false);
        RectTransform panelRect = controlsSidebar.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = controlsSidebarPosition;
        Vector2 effectiveSize = new Vector2(
            410f,
            520f);
        panelRect.sizeDelta = effectiveSize;

        Image panelImage = controlsSidebar.AddComponent<Image>();
        panelImage.color = new Color(0.025f, 0.045f, 0.05f, 0.96f);
        panelImage.raycastTarget = false;
        Outline panelOutline = controlsSidebar.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.32f, 0.46f, 0.43f, 0.8f);
        panelOutline.effectDistance = new Vector2(1f, -1f);

        GameObject accent = new GameObject("Franja_Mazahua");
        accent.transform.SetParent(controlsSidebar.transform, false);
        RectTransform accentRect = accent.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(6f, 0f);
        Image accentImage = accent.AddComponent<Image>();
        accentImage.color = MenuMagenta;
        accentImage.raycastTarget = false;

        Text title = CreateLabel(
            controlsSidebar.transform,
            "CONTROLES",
            20,
            new Vector2(0f, effectiveSize.y * 0.5f - 27f),
            new Vector2(effectiveSize.x - 42f, 34f),
            new Color(1f, 0.82f, 0.3f, 1f));
        title.fontStyle = FontStyle.Bold;

        float ruleY = effectiveSize.y * 0.5f - 52f;
        CreateMenuRule(controlsSidebar.transform, new Vector2(0f, ruleY), new Vector2(effectiveSize.x - 44f, 2f), new Color(MenuCream.r, MenuCream.g, MenuCream.b, 0.38f));

        string[] leftKeys = { "W", "S", "A", "D", "ESPACIO" };
        string[] leftActions = { "Avanzar", "Retroceder", "Izquierda", "Derecha", "Saltar" };
        string[] rightKeys = { "CLIC IZQ.", "F", "E", "I", "ESC" };
        string[] rightActions = { "Atacar", "Macuahuitl", "Habilidad especial", "Inventario", "Pausa" };
        float firstRowY = effectiveSize.y * 0.5f - 82f;
        CreateControlColumn(controlsSidebar.transform, leftKeys, leftActions, -125f, 57f, firstRowY);
        CreateControlColumn(controlsSidebar.transform, rightKeys, rightActions, -125f, 57f, firstRowY - 215f);
        controlsSidebar.SetActive(false);
        var help = new GameObject("Boton_Ayuda_Controles", typeof(RectTransform), typeof(Image), typeof(Button));
        help.transform.SetParent(parent, false);
        RectTransform helpRect = help.GetComponent<RectTransform>();
        helpRect.anchorMin = helpRect.anchorMax = helpRect.pivot = new Vector2(1f, 1f);
        helpRect.anchoredPosition = new Vector2(-28f, -145f);
        helpRect.sizeDelta = new Vector2(176f, 42f);
        help.GetComponent<Image>().color = new Color(.16f, .12f, .10f, .92f);
        CreateLabel(help.transform, "?  CONTROLES", 18, Vector2.zero, helpRect.sizeDelta, MenuCream);
        help.GetComponent<Button>().onClick.AddListener(() => controlsSidebar.SetActive(!controlsSidebar.activeSelf));
    }

    void CreateControlColumn(Transform parent, string[] keys, string[] actions, float keyX, float actionX, float firstRowY)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            float y = firstRowY - i * 43f;
            GameObject keycap = new GameObject("Tecla_" + keys[i], typeof(RectTransform), typeof(Image));
            keycap.transform.SetParent(parent, false);
            RectTransform keyRect = keycap.GetComponent<RectTransform>();
            float keyWidth = keys[i].Length > 4 ? 84f : 52f;
            keyRect.sizeDelta = new Vector2(keyWidth, 28f);
            keyRect.anchoredPosition = new Vector2(keyX, y);
            Image keyImage = keycap.GetComponent<Image>();
            keyImage.color = new Color(0.08f, 0.18f, 0.20f, 1f);
            keyImage.raycastTarget = false;
            Outline keyOutline = keycap.AddComponent<Outline>();
            keyOutline.effectColor = new Color(0.35f, 0.55f, 0.48f, 0.8f);
            keyOutline.effectDistance = new Vector2(1f, -1f);

            Text key = CreateLabel(keycap.transform, keys[i], 17, Vector2.zero, new Vector2(keyWidth - 4f, 26f), Color.white);
            key.fontStyle = FontStyle.Bold;
            Text action = CreateLabel(parent, actions[i], 21, new Vector2(actionX, y), new Vector2(224f, 32f), new Color(.91f, .94f, .91f));
            action.alignment = TextAnchor.MiddleLeft;
        }
    }

    void BuildHealthBar(Transform parent)
    {
        GameObject frame = new GameObject("HealthFrame");
        frame.transform.SetParent(parent, false);
        RectTransform frameRect = frame.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 1f);
        frameRect.anchorMax = new Vector2(0f, 1f);
        frameRect.pivot = new Vector2(0f, 1f);
        frameRect.anchoredPosition = new Vector2(34f, -32f);
        frameRect.sizeDelta = new Vector2(430f, 74f);
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = new Color(0.05f, 0.05f, 0.04f, 0.78f);

        Text title = CreateLabel(frame.transform, "VIDA", 22, new Vector2(-165f, 18f), new Vector2(80f, 34f), new Color(1f, 0.86f, 0.45f));
        title.alignment = TextAnchor.MiddleLeft;

        GameObject back = new GameObject("HealthBack");
        back.transform.SetParent(frame.transform, false);
        RectTransform backRect = back.AddComponent<RectTransform>();
        backRect.sizeDelta = new Vector2(318f, 28f);
        backRect.anchoredPosition = new Vector2(47f, -6f);
        Image backImage = back.AddComponent<Image>();
        backImage.color = new Color(0.2f, 0.05f, 0.05f, 1f);

        GameObject fill = new GameObject("HealthFill");
        fill.transform.SetParent(back.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        healthFill = fill.AddComponent<Image>();
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.color = new Color(0.75f, 0.08f, 0.08f, 1f);
    }

    void BeginGame()
    {
        if (inPrologue) return;
        skipPrologueRequested = false;
        skipIntroRequested = false;
        SetGameplayMusicVolume();
        mainMenuPanel.SetActive(false);
        inPrologue = true;
        hudRoot.SetActive(false);
        StartCoroutine(IntroVideoThenPrologue());
    }

    void SkipIntroVideo()
    {
        skipIntroRequested = true;
        if (introSkipButton != null)
            introSkipButton.interactable = false;
        if (introVideoPlayer != null && introVideoPlayer.isPlaying)
            introVideoPlayer.Stop();
    }

    void SkipPrologue()
    {
        if (!inPrologue || prologuePanel == null || !prologuePanel.activeInHierarchy)
            return;

        // Xunjuu v0.1 - ACCION: el segundo boton corta inmediatamente la
        // linea de tiempo; la corrutina realiza la limpieza en el mismo frame.
        skipPrologueRequested = true;
        if (prologueSkipButton != null)
            prologueSkipButton.interactable = false;
    }

    // Xunjuu v0.1 - ACCION: puntos publicos usados exclusivamente por la
    // validacion automatica de los dos botones del prologo.
    public void BeginGameForValidation()
    {
        BeginGame();
    }

    public void SkipIntroForValidation()
    {
        SkipIntroVideo();
    }

    public void SkipTimelineForValidation()
    {
        SkipPrologue();
    }

    public float MenuMusicVolume => menuMusicVolume;
    public float GameplayMusicVolume => menuMusicVolume * (1f - gameplayMusicReduction);
    public float CurrentMusicVolume => musicSource != null ? musicSource.volume : 0f;

    IEnumerator IntroVideoThenPrologue()
    {
        gameStarted = true;
        Time.timeScale = 1f;
        SetPlayerControl(false);
        yield return StartCoroutine(PlayIntroVideo());
        yield return StartCoroutine(PrologueTimeline());
    }

    IEnumerator PlayIntroVideo()
    {
        if (introVideoPanel == null || introVideoImage == null)
            yield break;

        string videoPath = Path.Combine(Application.streamingAssetsPath, "IntroPrologoMazahua.mp4");
        if (!File.Exists(videoPath))
            yield break;

        skipIntroRequested = false;
        inPrologue = true;
        introVideoPanel.SetActive(true);
        introVideoPanel.transform.SetAsLastSibling();
        if (introVideoTranslationOverlay != null)
            introVideoTranslationOverlay.SetActive(true);
        if (introSkipButton != null)
        {
            introSkipButton.interactable = true;
            introSkipButton.transform.SetAsLastSibling();
        }

        if (introVideoTexture == null)
            introVideoTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);

        if (introVideoPlayer == null)
        {
            introVideoPlayer = introVideoPanel.AddComponent<VideoPlayer>();
            introVideoPlayer.playOnAwake = false;
            introVideoPlayer.isLooping = false;
            introVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            introVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            introVideoPlayer.skipOnDrop = true;
        }
        introVideoPlayer.playbackSpeed = introVideoPlaybackSpeed;

        introVideoImage.texture = introVideoTexture;
        introVideoPlayer.targetTexture = introVideoTexture;
        introVideoPlayer.source = VideoSource.Url;
        introVideoPlayer.url = videoPath;
        introVideoPlayer.Prepare();

        float prepareTimer = 0f;
        while (!introVideoPlayer.isPrepared && prepareTimer < 4f && !skipIntroRequested)
        {
            prepareTimer += Time.deltaTime;
            yield return null;
        }

        if (!skipIntroRequested && introVideoPlayer.isPrepared)
        {
            UpdateIntroVideoTranslation(0);
            introVideoPlayer.Play();
            yield return null;
            while (introVideoPlayer.isPlaying && !skipIntroRequested)
            {
                UpdateIntroVideoTranslation();
                yield return null;
            }
        }

        introVideoPlayer.Stop();
        introVideoPanel.SetActive(false);
        inPrologue = false;
        skipIntroRequested = false;
    }

    IEnumerator PrologueTimeline()
    {
        inPrologue = true;
        gameStarted = true;
        skipPrologueRequested = false;
        Time.timeScale = 1f;
        SetPlayerControl(false);
        prologuePanel.SetActive(true);
        prologuePanel.transform.SetAsLastSibling();
        if (prologueSkipButton != null)
        {
            prologueSkipButton.interactable = true;
            prologueSkipButton.transform.SetAsLastSibling();
        }

        Text chapterTitle = CreateLabel(prologuePanel.transform, string.Empty, 42, new Vector2(0f, 118f), new Vector2(1450f, 86f), new Color(1f, 0.78f, 0.24f));
        ApplyPixelArcadeStyle(chapterTitle, new Color(1f, 0.78f, 0.24f), 3f);

        Text spanishBody = CreateLabel(prologuePanel.transform, string.Empty, 30, new Vector2(0f, -82f), new Vector2(1540f, 185f), Color.white);
        spanishBody.fontStyle = FontStyle.Bold;
        ApplyReadableTextStyle(spanishBody, new Color(1f, 1f, 1f), 2.5f);

        Text mazahuaBody = CreateLabel(prologuePanel.transform, string.Empty, 25, new Vector2(0f, -286f), new Vector2(1540f, 165f), new Color(1f, 0.9f, 0.48f));
        mazahuaBody.fontStyle = FontStyle.Bold;
        ApplyReadableTextStyle(mazahuaBody, new Color(1f, 0.9f, 0.48f), 2.2f);

        string[,] beats =
        {
            {
                "I. EL PUEBLO",
                "Español: San Felipe del Progreso era un pueblo mazahua.",
                "Mazahua: San Felipe del Progreso mi na jñiñi jñatjo."
            },
            {
                "II. LA MILPA",
                "Español: Alimentos: maiz, frijol, haba, calabaza, papa, trigo y hortalizas.",
                "Mazahua: Jñona: maiz, frijol, haba, calabaza, papa, trigo, hortalizas."
            },
            {
                "III. MATEO JNATR'O",
                "Español: Mateo Jnatr'o es P'antreje Jnatr'o, el cazador que habla.",
                "Mazahua: Mateo Jnatr'o: P'antreje Jnatr'o, mepjanteje ko na jñaa."
            },
            {
                "IV. LA LLUVIA",
                "Español: Lluvia, milpa y alimento en peligro.",
                "Mazahua: Dyeb'e, milpa, jñona: dya joo."
            },
            {
                "V. LA PROFECIA",
                "Español: Mateo debe ser el cazador que habla y proteger al pueblo.",
                "Mazahua: Mateo: P'antreje Jnatr'o; tsasú na jñiñi."
            },
            {
                "VI. EL RITUAL",
                "Español: Recoge las flores y registra la fauna sin arma.",
                "Traducción mazahua de esta etapa pendiente de revisión."
            },
            {
                "VII. LAS CINCO FLORES",
                "Español: Recoge cinco flores. Protege al pueblo.",
                "Mazahua: Tsansa tsicha ndájná. Tsasú na jñiñi."
            },
            {
                "VIII. EL MACUAHUITL",
                "Español: El Macuahuitl se entrega después de registrar los seis animales.",
                "Traducción mazahua de esta etapa pendiente de revisión."
            },
            {
                "IX. LA FAUNA ALTERADA",
                "Español: La fauna del bosque está desorientada; Mateo debe registrarla sin hacerle daño.",
                "Texto mazahua pendiente de validación comunitaria."
            },
            {
                "X. LAS SOMBRAS",
                "Español: Ocho Dyanatr'o. Orbitasword.",
                "Mazahua: Jñincho Dyanatr'o. Orbitasword."
            },
            {
                "XI. EL GRAN DYANATR'O",
                "Español: Gran Dyanatr'o. Macuahuitl y Orbitasword.",
                "Mazahua: Gran Dyanatr'o. Macuahuitl, Orbitasword."
            },
            {
                "XII. LA OFRENDA",
                "Español: Cinco flores: Otontecuhtli. Pueblo protegido.",
                "Mazahua: Tsicha ndájná: Otontecuhtli. Jñiñi tsasú."
            }
        };

        // Keep the legacy story table above as archival content. The playable
        // introduction establishes the premise without granting/spoiling rewards.
        beats=new string[,]
        {
            {"XUNJÚU · EL CAMINO DE MATEO","Mateo conoce los caminos de su comunidad, pero una presencia extraña está alterando la milpa y desorientando a los animales.","Relato de ficción del juego. No representa una ceremonia ni una tradición histórica."},
            {"ANTES DE EMPUÑAR UN ARMA","Antes de salir, Mateo practica en el claro: caminar, saltar, defenderse con las manos y abrir su morral. El viaje empieza sin arma.","La práctica no entrega recompensas ni desbloquea ataques especiales."},
            {"LO QUE GUARDAN LAS FLORES","Cinco flores conservan palabras y recuerdos del lugar. Aprende a moverte; después recógelas para ganarte la confianza de tu comunidad.","El vocabulario mazahua existente se conserva; los nuevos diálogos están en español hasta contar con revisión lingüística."}
        };
        for (int i = 0; i < beats.GetLength(0); i++)
        {
            chapterTitle.text = beats[i, 0];
            spanishBody.text = beats[i, 1];
            mazahuaBody.text = beats[i, 2];
            StartCoroutine(MoveCameraToPrologueView(i, prologueCameraMoveDuration));
            float elapsed = 0f;
            while (elapsed < prologueBeatDuration && !skipPrologueRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (skipPrologueRequested)
                break;
        }

        Destroy(chapterTitle.gameObject);
        Destroy(spanishBody.gameObject);
        Destroy(mazahuaBody.gameObject);
        FinishPrologue();
    }

    void FinishPrologue()
    {
        if (prologuePanel != null)
            prologuePanel.SetActive(false);
        if (prologueSkipButton != null)
            prologueSkipButton.interactable = true;

        if (player != null)
            player.RemoveEquippedWeapon();
        // Xunjuu beta - Los enemigos y animales permanecen ocultos hasta
        // completar las cinco flores. La mision es la unica que los activa.
        if (objectiveText != null)
            objectiveText.text = GetMissionIntroText();
        SetPlayerControl(true);
        inPrologue = false;
        hudRoot.SetActive(true);
        skipPrologueRequested = false;
        var opening=GetComponent<XunjuuOpeningJourney>()??gameObject.AddComponent<XunjuuOpeningJourney>();
        opening.Begin(player,hudRoot.transform);
        if(objectiveText!=null)objectiveText.gameObject.SetActive(false);
    }

    public bool IsOpeningTutorialActive => GetComponent<XunjuuOpeningJourney>()?.TutorialActive==true;
    public void OnOpeningTutorialCompleted()
    {if(objectiveText!=null){objectiveText.gameObject.SetActive(true);objectiveText.text=GetMissionIntroText();}}

    public void OnSwordCollected()
    {
        ShowMissionFeedback("RECOMPENSA OBTENIDA\nMazahua: Mateo pesi Macuahuitl.\nEspañol: Mateo tiene el Macuahuitl.", true);
        if (objectiveText != null && levelOneCompleted)
            objectiveText.text = GetMissionCompletedHudText();
    }

    public void CollectMazahuaWord(string word, string meaning, string storyMazahua = "", string storySpanish = "")
    {
        if(IsOpeningTutorialActive)return;
        if (word == null || word.Trim().Length == 0 || !collectedMazahuaWords.Add(word))
            return;

        mazahuaWordsCollected = collectedMazahuaWords.Count;
        string storyText = string.IsNullOrWhiteSpace(storyMazahua) && string.IsNullOrWhiteSpace(storySpanish)
            ? $"Mazahua: {word}.\nEspañol: {meaning}."
            : $"{storyMazahua}\n{storySpanish}\nPalabra: {word} - {meaning}";
        ShowMissionFeedback(storyText);

        if (mazahuaWordsCollected >= mazahuaWordGoal)
            CompleteLevelOne();
        else if (objectiveText != null)
            objectiveText.text = GetMissionProgressText();
    }

    // ========================================================================
    // Xunjuu v0.1 - Final del nivel 1
    // ACCION: entregar el arma sin Orbitasword e iniciar la mision de animales.
    // MODIFICACION: cambia la recompensa o la mision desde el Inspector.
    // ========================================================================
    void CompleteLevelOne()
    {
        if (levelOneCompleted)
            return;

        levelOneCompleted = true;
        ResolveProgressionReferences();
        if (objectiveText != null)
        {
            objectiveText.text = GetMissionCompletedHudText();
            // Xunjuu v0.1 - ACCION: ceder la parte superior al HUD de fauna
            // para evitar dos objetivos dibujados uno encima del otro.
            objectiveText.gameObject.SetActive(false);
        }

        ShowMissionFeedback(
            "FLORES COMPLETADAS\n"
            + "Captura seis animales con C.",
            true);

        if (levelTwoMission != null)
            levelTwoMission.StartMission();
    }

    // ACCION: restablecer la progresion para las pruebas automaticas de Unity.
    public void ResetLevelProgressionForValidation()
    {
        collectedMazahuaWords.Clear();
        mazahuaWordsCollected = 0;
        levelOneCompleted = false;
        ResolveProgressionReferences();

        if (player != null)
            player.RemoveEquippedWeapon();
        if (levelOneWeaponReward != null)
            levelOneWeaponReward.ResetRewardState();
        if (levelTwoMission != null)
            levelTwoMission.ResetMission();
        if (objectiveText != null)
        {
            objectiveText.gameObject.SetActive(true);
            objectiveText.text = GetMissionIntroText();
        }
    }

    string GetMissionIntroText()
    {
        return $"Mazahua: Tsansa tsicha ndájná ({mazahuaWordsCollected}/{mazahuaWordGoal}).\nEspañol: Recoge cinco flores ({mazahuaWordsCollected}/{mazahuaWordGoal}).";
    }

    string GetMissionProgressText()
    {
        return $"Mazahua: Tsansa tsicha ndájná ({mazahuaWordsCollected}/{mazahuaWordGoal}).\nEspañol: Recoge cinco flores ({mazahuaWordsCollected}/{mazahuaWordGoal}).";
    }

    string GetMissionCompletedHudText()
    {
        return "Captura y registra seis especies con C.\nRecibirás el Macuahuitl al completar las capturas.";
    }

    void ShowMissionFeedback(string message, bool completed = false)
    {
        if (missionFeedbackText == null)
            return;

        StopCoroutine(nameof(HideMissionFeedbackAfterDelay));
        missionFeedbackText.text = message;
        missionFeedbackCompletedVisible = completed;
        ApplyMissionFeedbackStyle(completed);
        StartCoroutine(nameof(HideMissionFeedbackAfterDelay));
    }

    void ApplyMissionFeedbackStyle(bool completed)
    {
        if (missionFeedbackText == null)
            return;

        RectTransform rect = missionFeedbackText.GetComponent<RectTransform>();
        if (completed)
        {
            missionFeedbackText.font = GetPixelUiFont();
            missionFeedbackText.fontSize = 34;
            missionFeedbackText.fontStyle = FontStyle.Bold;
            missionFeedbackText.color = new Color(1f, 0.72f, 0.17f, 1f);
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(1180f, 150f);
                rect.anchoredPosition = new Vector2(0f, -160f);
            }
            if (missionFeedbackOutline != null)
            {
                missionFeedbackOutline.effectColor = new Color(0.035f, 0.04f, 0.12f, 1f);
                missionFeedbackOutline.effectDistance = new Vector2(3f, -3f);
            }
            if (missionFeedbackShadow != null)
            {
                missionFeedbackShadow.effectColor = new Color(0.55f, 0.08f, 0.03f, 0.95f);
                missionFeedbackShadow.effectDistance = new Vector2(6f, -6f);
            }
            return;
        }

        missionFeedbackText.font = GetUiFont();
        missionFeedbackText.fontSize = 26;
        missionFeedbackText.fontStyle = FontStyle.Bold;
        missionFeedbackText.color = new Color(1f, 0.88f, 0.42f, 1f);
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(1120f, 132f);
            rect.anchoredPosition = new Vector2(0f, -160f);
        }
        if (missionFeedbackOutline != null)
        {
            missionFeedbackOutline.effectColor = new Color(0.08f, 0.09f, 0.08f, 0.95f);
            missionFeedbackOutline.effectDistance = new Vector2(2.5f, -2.5f);
        }
        if (missionFeedbackShadow != null)
        {
            missionFeedbackShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            missionFeedbackShadow.effectDistance = new Vector2(4f, -4f);
        }
    }

    IEnumerator HideMissionFeedbackAfterDelay()
    {
        bool completed = missionFeedbackCompletedVisible;
        yield return new WaitForSeconds(completed ? 3f : missionFeedbackDuration);
        if (missionFeedbackText != null)
        {
            missionFeedbackText.text = string.Empty;
            missionFeedbackCompletedVisible = false;
            ApplyMissionFeedbackStyle(false);
        }
    }

    [ContextMenu("Generar coleccionables editables")]
    public void GenerateEditableMazahuaWordCollectibles()
    {
        SpawnMazahuaWordCollectibles(true);
    }

    // Xunjuu v0.1 - ACCION: aplicar una separacion practica sin perder exploracion.
    public void ConfigureWordPlacement(float minimumSpacing, float explorationRadius)
    {
        wordCollectibleMinDistanceBetweenWords = Mathf.Clamp(minimumSpacing, 12f, 90f);
        wordCollectibleExplorationRadius = Mathf.Max(wordCollectibleMinDistanceFromPlayer + 20f, explorationRadius);
    }

    [ContextMenu("Limpiar coleccionables editables")]
    public void ClearEditableMazahuaWordCollectibles()
    {
        GameObject root = GameObject.Find("Palabras_Mazahuas_Dyanatro");
        if (root != null)
            DestroySceneObject(root);
    }

    void SpawnMazahuaWordCollectibles()
    {
        SpawnMazahuaWordCollectibles(false);
    }

    void SpawnMazahuaWordCollectibles(bool forceRegenerate)
    {
        ResolvePlayerReferences();
        if (playerTransform == null || mazahuaWords == null || mazahuaWords.Length == 0)
            return;

        GameObject existingRoot = GameObject.Find("Palabras_Mazahuas_Dyanatro");
        if (existingRoot != null && existingRoot.transform.childCount > 0 && !forceRegenerate)
        {
            EnsureExistingWordCollectiblesAreEditable(existingRoot);
            return;
        }

        if (existingRoot != null)
            DestroySceneObject(existingRoot);

        GameObject root = new GameObject("Palabras_Mazahuas_Dyanatro");
        Vector3 center = playerTransform.position;
        List<Transform> enemyTransforms = GetEnemyTransformsForCollectiblePlacement();
        List<Vector3> placedWordPositions = new List<Vector3>();
        int collectibleCount = Mathf.Clamp(levelOneCollectibleCount, 1, mazahuaWords.Length);

        // Xunjuu beta - ACCION: crear las cinco flores de la primera mision.
        for (int i = 0; i < collectibleCount; i++)
        {
            string[] parts = mazahuaWords[i].Split('|');
            string word = parts.Length > 0 ? parts[0] : "jñaa";
            string meaning = parts.Length > 1 ? parts[1] : "memoria";
            string storyMazahua = parts.Length > 2 ? parts[2] : string.Empty;
            string storySpanish = parts.Length > 3 ? parts[3] : string.Empty;
            Vector3 position = FindExplorationPointForWord(i, collectibleCount, center, enemyTransforms, placedWordPositions);
            placedWordPositions.Add(position);

            GameObject wordObject = new GameObject("Palabra_" + word);
            wordObject.transform.SetParent(root.transform, false);
            wordObject.transform.position = position;

            MazahuaWordCollectible collectible = wordObject.AddComponent<MazahuaWordCollectible>();
            collectible.Configure(word, meaning, storyMazahua, storySpanish);

            GameObject editableExtras = new GameObject("Extras_Editables");
            editableExtras.transform.SetParent(wordObject.transform, false);
            editableExtras.transform.localPosition = Vector3.zero;

            GameObject spawnerObject = new GameObject("Spawner_Animales_" + word);
            spawnerObject.transform.SetParent(wordObject.transform, false);
            spawnerObject.transform.localPosition = Vector3.zero;
            MazahuaWordAnimalSpawner animalSpawner = spawnerObject.AddComponent<MazahuaWordAnimalSpawner>();
            animalSpawner.Configure(GetAnimalPrefabsForWord(i), animalsPerWordMin, animalsPerWordMax, animalsPerWordRadius);

            SphereCollider collider = wordObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 1.1f;

            Sprite collectibleFlower = GetMazahuaCollectibleFlowerSprite();

            GameObject glowObject = new GameObject("Aura_Tsirajna_ne_dyebe");
            glowObject.transform.SetParent(wordObject.transform, false);
            glowObject.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            glowObject.transform.localScale = Vector3.one * 1.34f;
            SpriteRenderer glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = collectibleFlower;
            glowRenderer.color = new Color(1f, 0.78f, 0.18f, 0.28f);
            glowRenderer.sortingOrder = 820;
            glowObject.AddComponent<CloudBillboard>();

            GameObject marker = new GameObject("Flor_Tsirajna_ne_dyebe");
            marker.transform.SetParent(wordObject.transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = Vector3.one * 1.05f;
            SpriteRenderer markerRenderer = marker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = collectibleFlower;
            markerRenderer.color = Color.white;
            markerRenderer.sortingOrder = 850;
            marker.AddComponent<CloudBillboard>();
            DyanatroSpriteDepthSorter sorter = marker.AddComponent<DyanatroSpriteDepthSorter>();
            sorter.Configure(850, 10f);

        }
    }

    void ResolvePlayerReferences()
    {
        if (playerTransform != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        playerTransform = playerObject.transform;
        player = playerObject.GetComponent<PlayerController>();
    }

    void EnsureExistingWordCollectiblesAreEditable(GameObject root)
    {
        if (root == null)
            return;

        MazahuaWordCollectible[] collectibles = root.GetComponentsInChildren<MazahuaWordCollectible>(true);
        for (int collectibleIndex = 0; collectibleIndex < collectibles.Length; collectibleIndex++)
        {
            MazahuaWordCollectible collectible = collectibles[collectibleIndex];
            if (collectible == null)
                continue;

            Collider collider = collectible.GetComponent<Collider>();
            if (collider == null)
                collider = collectible.gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;

            Rigidbody rigidbody = collectible.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = collectible.gameObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            MazahuaWordAnimalSpawner spawner = collectible.GetComponentInChildren<MazahuaWordAnimalSpawner>(true);
            if (spawner != null)
                spawner.Configure(GetAnimalPrefabsForWord(collectibleIndex), animalsPerWordMin, animalsPerWordMax, animalsPerWordRadius);
        }
    }

    void DestroySceneObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    GameObject[] GetAnimalPrefabsForWord(int wordIndex)
    {
        GameObject[] regionalFauna = Resources.LoadAll<GameObject>("Prefabs/Animals")
            .Where(prefab => prefab != null && prefab.name.StartsWith("Fauna_", System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(prefab => prefab.name, System.StringComparer.Ordinal)
            .ToArray();
        if (regionalFauna.Length > 0)
        {
            GameObject regionalFirst = regionalFauna[wordIndex % regionalFauna.Length];
            GameObject regionalSecond = regionalFauna[(wordIndex + 1) % regionalFauna.Length];
            return regionalFirst == regionalSecond ? new[] { regionalFirst } : new[] { regionalFirst, regionalSecond };
        }

        // Xunjuu v0.1 - ACCION: combinar prefabs asignados con todos los animales remotos disponibles.
        List<GameObject> combinedPrefabs = new List<GameObject>();
        if (animalPrefabs != null)
        {
            foreach (GameObject prefab in animalPrefabs)
            {
                if (prefab != null && prefab.name.IndexOf("Pato", System.StringComparison.OrdinalIgnoreCase) < 0 && !combinedPrefabs.Contains(prefab))
                    combinedPrefabs.Add(prefab);
            }
        }

        foreach (GameObject prefab in Resources.LoadAll<GameObject>("Prefabs/Animals"))
        {
            if (prefab != null && prefab.name.IndexOf("Pato", System.StringComparison.OrdinalIgnoreCase) < 0 && !combinedPrefabs.Contains(prefab))
                combinedPrefabs.Add(prefab);
        }

        GameObject[] availablePrefabs = combinedPrefabs.ToArray();

        if (availablePrefabs == null || availablePrefabs.Length == 0)
            return new GameObject[0];

        if (availablePrefabs.Length == 1)
            return availablePrefabs;

        GameObject first = availablePrefabs[wordIndex % availablePrefabs.Length];
        GameObject second = availablePrefabs[(wordIndex + 1) % availablePrefabs.Length];
        return first == second
            ? new[] { first }
            : new[] { first, second };
    }

    Vector3 FindExplorationPointForWord(int wordIndex, int totalWords, Vector3 center, List<Transform> enemies, List<Vector3> placedWords)
    {
        float bestScore = float.MinValue;
        Vector3 bestPoint = center + Vector3.forward * wordCollectibleMinDistanceFromPlayer;
        float baseAngle = (360f / Mathf.Max(1, totalWords)) * wordIndex;

        for (int attempt = 0; attempt < 96; attempt++)
        {
            float angle = (baseAngle + attempt * 47f + wordIndex * 17f) * Mathf.Deg2Rad;
            float radiusStep = (attempt % 8) / 7f;
            float radius = Mathf.Lerp(wordCollectibleMinDistanceFromPlayer, wordCollectibleExplorationRadius, radiusStep);
            radius += (wordIndex % 4) * 3.5f;

            Vector3 target = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 point = FindGroundPointNear(target, 7f) + Vector3.up * 0.75f;
            float score = ScoreWordCollectiblePoint(point, center, enemies, placedWords);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = point;
            }

            if (IsWordCollectiblePointValid(point, center, enemies, placedWords))
                return point;
        }

        return bestPoint;
    }

    float ScoreWordCollectiblePoint(Vector3 point, Vector3 center, List<Transform> enemies, List<Vector3> placedWords)
    {
        float playerDistance = XZDistance(point, center);
        float nearestEnemy = GetNearestEnemyDistance(point, enemies);
        float nearestWord = GetNearestPlacedWordDistance(point, placedWords);
        float score = playerDistance * 0.35f + nearestEnemy * 1.25f + nearestWord * 0.8f;
        if (playerDistance > wordCollectibleExplorationRadius)
            score -= (playerDistance - wordCollectibleExplorationRadius) * 20f;

        Terrain terrain = FindTerrainContaining(point);
        if (terrain != null)
        {
            float slope = GetTerrainSlopeAtWorld(terrain, point);
            if (slope > maxTreeSlope)
                score -= (slope - maxTreeSlope) * 3.5f;
        }

        return score;
    }

    bool IsWordCollectiblePointValid(Vector3 point, Vector3 center, List<Transform> enemies, List<Vector3> placedWords)
    {
        float playerDistance = XZDistance(point, center);
        if (playerDistance < wordCollectibleMinDistanceFromPlayer
            || playerDistance > wordCollectibleExplorationRadius)
            return false;

        if (GetNearestEnemyDistance(point, enemies) < wordCollectibleMinDistanceFromEnemy)
            return false;

        if (GetNearestPlacedWordDistance(point, placedWords) < wordCollectibleMinDistanceBetweenWords)
            return false;

        Terrain terrain = FindTerrainContaining(point);
        if (terrain != null && GetTerrainSlopeAtWorld(terrain, point) > maxTreeSlope)
            return false;

        return true;
    }

    List<Transform> GetEnemyTransformsForCollectiblePlacement()
    {
        List<Transform> enemies = new List<Transform>();

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            AddUniqueTransform(enemies, enemy != null ? enemy.transform : null);

        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            AddUniqueTransform(enemies, enemy != null ? enemy.transform : null);

        foreach (EnemyFireBreath enemy in FindObjectsByType<EnemyFireBreath>(FindObjectsSortMode.None))
            AddUniqueTransform(enemies, enemy != null ? enemy.transform : null);

        foreach (GameObject enemy in delayedEnemies)
            AddUniqueTransform(enemies, enemy != null ? enemy.transform : null);

        return enemies;
    }

    void AddUniqueTransform(List<Transform> list, Transform target)
    {
        if (target == null || list.Contains(target))
            return;

        list.Add(target);
    }

    float GetNearestEnemyDistance(Vector3 point, List<Transform> enemies)
    {
        if (enemies == null || enemies.Count == 0)
            return wordCollectibleMinDistanceFromEnemy * 2f;

        float nearest = float.MaxValue;
        foreach (Transform enemy in enemies)
        {
            if (enemy == null)
                continue;

            nearest = Mathf.Min(nearest, XZDistance(point, enemy.position));
        }

        return nearest == float.MaxValue ? wordCollectibleMinDistanceFromEnemy * 2f : nearest;
    }

    float GetNearestPlacedWordDistance(Vector3 point, List<Vector3> placedWords)
    {
        if (placedWords == null || placedWords.Count == 0)
            return wordCollectibleMinDistanceBetweenWords * 2f;

        float nearest = float.MaxValue;
        foreach (Vector3 placed in placedWords)
            nearest = Mathf.Min(nearest, XZDistance(point, placed));

        return nearest;
    }

    float XZDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    IEnumerator PopInTransform(Transform target)
    {
        Vector3 finalScale = target.localScale == Vector3.zero ? Vector3.one : target.localScale;
        target.localScale = Vector3.zero;
        float timer = 0f;
        while (timer < 0.35f)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / 0.35f);
            target.localScale = finalScale * t;
            yield return null;
        }
        target.localScale = finalScale;
    }

    void ActivateEnemies()
    {
        foreach (GameObject enemy in delayedEnemies)
        {
            if (enemy == null) continue;
            enemy.SetActive(true);
            SnapObjectToGround(enemy.transform, false);
        }

        if (delayedEnemies.Count > 0 && playerTransform != null)
        {
            GameObject extra = Instantiate(delayedEnemies[0]);
            extra.name = "Dyanatro_Prologo";
            extra.SetActive(true);
            extra.transform.position = FindGroundPointNear(playerTransform.position + playerTransform.forward * 8f, 4f);
            SnapObjectToGround(extra.transform, false);
        }
    }

    void SetPause(bool value)
    {
        if (inPrologue || deathScreenShown) return;
        isPaused = value;
        pausePanel.SetActive(value);
        Time.timeScale = value ? 0f : 1f;
        SetPlayerControl(!value);
    }

    IEnumerator ShowDeathScreenAfterDelay()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (introVideoPanel != null)
            introVideoPanel.SetActive(false);

        yield return new WaitForSecondsRealtime(1.35f);
        SetPlayerControl(false);

        if (deathPanel != null)
            deathPanel.SetActive(true);

        if (missionFeedbackText != null)
            missionFeedbackText.text = string.Empty;

        isPaused = true;
        Time.timeScale = 0f;
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    void ReturnToMainMenu()
    {
        RestartScene();
    }

    // ========================================================================
    // Xunjuú v0.1 - Acceso a la ludoteca
    // ACCION: abrir el demo desde el menu, la pausa o al terminar el nivel.
    // ========================================================================
    public void OpenLudoteca()
    {
        if (ludotecaBrowser != null)
            ludotecaBrowser.Open();
    }

    void OpenLudotecaFromPause()
    {
        // Xunjuú v0.1 - ACCION: salir formalmente de la pausa antes de abrir
        // la ludoteca; al cerrarla se restaura tiempo 1 y el jugador se mueve.
        if (isPaused)
            SetPause(false);
        OpenLudoteca();
    }

    void SubscribeLudotecaToLevelCompletion()
    {
        if (levelTwoMission == null)
            return;

        levelTwoMission.MissionCompleted -= HandleLevelCompletedForLudoteca;
        levelTwoMission.MissionCompleted += HandleLevelCompletedForLudoteca;
    }

    void HandleLevelCompletedForLudoteca()
    {
        if (openLudotecaAfterLevelCompletion)
            StartCoroutine(OpenLudotecaAfterDelay());
    }

    IEnumerator OpenLudotecaAfterDelay()
    {
        yield return new WaitForSecondsRealtime(ludotecaCompletionDelay);
        OpenLudoteca();
    }

    void OnDestroy()
    {
        if (levelTwoMission != null)
            levelTwoMission.MissionCompleted -= HandleLevelCompletedForLudoteca;
        if (runtimeGuideArrowSprite != null)
        {
            Destroy(runtimeGuideArrowSprite.texture);
            Destroy(runtimeGuideArrowSprite);
        }
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void SetPlayerControl(bool enabled)
    {
        if (player != null)
            player.enabled = enabled;
    }

    void UpdateHealthHud()
    {
        if (healthFill == null || player == null)
            return;

        float maxHealth = Mathf.Max(1, player.GetMaxHealth());
        healthFill.fillAmount = Mathf.Clamp01(player.GetCurrentHealth() / maxHealth);
    }

    void SpawnTallGrass()
    {
        if (tallGrassPatchCount <= 0)
            return;

        GameObject existing = GameObject.Find("PastoAlto_Dyanatro");
        if (existing != null)
            Destroy(existing);

        Sprite grassSprite = GetTallGrassSprite();
        if (grassSprite == null)
            return;

        GameObject root = new GameObject("PastoAlto_Dyanatro");
        Vector3 center = playerTransform != null ? playerTransform.position : Vector3.zero;
        int spawned = 0;
        int attempts = 0;

        while (spawned < tallGrassPatchCount && attempts < tallGrassPatchCount * 10)
        {
            attempts++;
            Vector2 circle = Random.insideUnitCircle * tallGrassRadius;
            Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);

            if (playerTransform != null && Vector3.Distance(candidate, playerTransform.position) < tallGrassMinDistanceFromPlayer)
                continue;

            Terrain terrain = FindTerrainContaining(candidate);
            if (terrain != null && GetTerrainSlopeAtWorld(terrain, candidate) > maxTreeSlope)
                continue;

            Vector3 ground = GroundPoint(candidate);
            GameObject patch = new GameObject("PastoAlto");
            patch.transform.SetParent(root.transform, false);
            patch.transform.position = ground + Vector3.up * 0.03f;
            patch.transform.rotation = Quaternion.identity;
            patch.transform.localScale = new Vector3(Random.Range(0.65f, 1.15f), Random.Range(0.38f, 0.72f) * tallGrassHeight, 1f);

            SpriteRenderer renderer = patch.AddComponent<SpriteRenderer>();
            renderer.sprite = grassSprite;
            renderer.color = Color.Lerp(new Color(.82f,.87f,.69f,1f), new Color(.64f,.75f,.57f,1f), Random.value);

            DyanatroSpriteDepthSorter sorter = patch.AddComponent<DyanatroSpriteDepthSorter>();
            sorter.Configure(-8, 10f);
            spawned++;
        }
    }

    Sprite GetTallGrassSprite()
    {
        if (runtimeTallGrassSprite != null)
            return runtimeTallGrassSprite;

        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        texture.name = "Pasto_Alto_Dyanatro_Texture";
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = new Color[256 * 256];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;
        texture.SetPixels(pixels);

        Color dark = new Color(.18f,.27f,.10f,1f);
        Color mid = new Color(.38f,.49f,.20f,1f);
        Color light = new Color(.66f,.70f,.33f,1f);

        for (int i = 0; i < 36; i++)
        {
            int baseX = Random.Range(18, 238);
            int baseY = Random.Range(0, 18);
            int height = Random.Range(118, 238);
            float bend = Random.Range(-48f, 48f);
            float sway = Random.Range(-10f, 10f);
            DrawGrassBlade(texture, baseX, baseY, height, bend, sway, dark, mid, light);
        }

        // Individual blade roots leave gaps; a filled rectangular base looked like a card.

        texture.Apply(false, false);
        runtimeTallGrassSprite = Sprite.Create(texture, new Rect(0f, 0f, 256f, 256f), new Vector2(0.5f, 0f), 165f);
        runtimeTallGrassSprite.name = "Pasto_Alto_Dyanatro";
        return runtimeTallGrassSprite;
    }

    void SpawnCornMilpas()
    {
        SpawnCornMilpas(false);
    }

    // ACCIÓN Xunjuú v0.1: generar milpas persistentes para editarlas en Hierarchy.
    [ContextMenu("Xunjuú v0.1/Generar milpas editables")]
    public void GenerateEditableCornFields()
    {
        SpawnCornMilpas(true);
    }

    [ContextMenu("Xunjuú v0.1/Limpiar milpas editables")]
    public void ClearEditableCornFields()
    {
        GameObject root = GameObject.Find("Milpas_Maiz_Dyanatro");
        if (root != null)
            DestroySceneObject(root);
    }

    void SpawnCornMilpas(bool forceRegenerate)
    {
        if (cornFieldCount <= 0 || cornRowsPerField <= 0 || cornStalksPerRow <= 0)
            return;

        Sprite cornSprite = GetCornStalkSprite();
        if (cornSprite == null)
            return;

        GameObject existing = GameObject.Find("Milpas_Maiz_Dyanatro");
        if (existing != null && existing.transform.childCount > 0 && !forceRegenerate)
        {
            PrepareExistingCornFields(existing);
            return;
        }
        if (existing != null)
            DestroySceneObject(existing);

        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (terrains.Length == 0)
            return;
        GameObject root = new GameObject("Milpas_Maiz_Dyanatro");
        Vector3 center = playerTransform != null ? playerTransform.position : Vector3.zero;
        int fields = 0;
        int attempts = 0;
        List<Vector3> fieldCenters = new List<Vector3>();
        List<Transform> protectedGameplayPoints = CollectProtectedForestPoints();

        while (fields < cornFieldCount && attempts < cornFieldCount * 80)
        {
            attempts++;
            Vector2 circle = Random.insideUnitCircle * cornFieldRadius;
            Vector3 basePosition = center + new Vector3(circle.x, 0f, circle.y);

            if (playerTransform != null && Vector3.Distance(basePosition, playerTransform.position) < treeMinDistanceFromPlayer * 2.2f)
                continue;

            Terrain terrain = FindTerrainContaining(basePosition);
            if (terrain == null || GetTerrainSlopeAtWorld(terrain, basePosition) > maxTreeSlope * 0.65f)
                continue;

            Vector3 fieldCenter = GroundPoint(basePosition);
            if (IsNearAnotherTree(fieldCenter, fieldCenters, cornFieldMinimumSpacing))
                continue;
            if (IsNearProtectedGameplayPoint(fieldCenter, protectedGameplayPoints, 9f))
                continue;

            GameObject field = new GameObject($"Milpa_Maiz_{fields + 1:00}");
            field.transform.SetParent(root.transform, false);
            field.transform.position = fieldCenter;
            XunjuuCropField cropField = field.AddComponent<XunjuuCropField>();
            cropField.ConfigureExisting("Milpa de maíz " + (fields + 1), cornRowsPerField, cornStalksPerRow);

            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 rowAxis = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 lineAxis = new Vector3(-rowAxis.z, 0f, rowAxis.x);
            float rowSpacing = Random.Range(1.15f, 1.45f);
            float stalkSpacing = Random.Range(1.05f, 1.35f);
            float furrowLength = (cornStalksPerRow - 1) * stalkSpacing + 2.4f;

            for (int row = 0; row < cornRowsPerField; row++)
            {
                float rowOffset = (row - (cornRowsPerField - 1) * 0.5f) * rowSpacing;
                CreateCornFurrow(field.transform, fieldCenter, rowAxis, lineAxis, rowOffset, furrowLength, rowSpacing * 0.58f);

                for (int stalk = 0; stalk < cornStalksPerRow; stalk++)
                {
                    float stalkOffset = (stalk - (cornStalksPerRow - 1) * 0.5f) * stalkSpacing;
                    Vector3 pos = fieldCenter + rowAxis * rowOffset + lineAxis * stalkOffset;
                    Terrain stalkTerrain = FindTerrainContaining(pos);
                    if (stalkTerrain == null || GetTerrainSlopeAtWorld(stalkTerrain, pos) > maxTreeSlope * 0.7f)
                        continue;

                    GameObject corn = new GameObject($"Maiz_F{fields + 1:00}_R{row + 1:00}_P{stalk + 1:00}");
                    corn.transform.SetParent(field.transform, false);
                    corn.transform.position = GroundPoint(pos);
                    float cornScale = Random.Range(0.34f, 0.52f);
                    corn.transform.localScale = new Vector3(cornScale * Random.Range(0.92f, 1.08f), cornScale * Random.Range(0.95f, 1.12f), 1f);

                    SpriteRenderer renderer = corn.AddComponent<SpriteRenderer>();
                    renderer.sprite = cornSprite;
                    renderer.color = Color.Lerp(new Color(0.94f, 1f, 0.62f, 1f), new Color(0.58f, 0.88f, 0.26f, 1f), Random.value * 0.45f);

                    DyanatroSpriteDepthSorter sorter = corn.AddComponent<DyanatroSpriteDepthSorter>();
                    sorter.Configure(-7, 10f);
                    corn.AddComponent<XunjuuCrop>();
                    XunjuuWorldLabel cropLabel = corn.AddComponent<XunjuuWorldLabel>();
                    cropLabel.Configure("Maíz", false, new Vector3(0f, 1.7f, 0f));
                    SnapVegetationSpriteToGround(corn.transform, true, 0.08f);
                }
            }

            fields++;
            fieldCenters.Add(fieldCenter);
        }

        if (fields < cornFieldCount)
            Debug.LogWarning($"Xunjuu v0.1: se generaron {fields}/{cornFieldCount} milpas sin traslapes.");
    }

    // ACCIÓN Xunjuú v0.1: completar componentes en milpas colocadas manualmente.
    void PrepareExistingCornFields(GameObject root)
    {
        if (root == null)
            return;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform field = root.transform.GetChild(i);
            XunjuuCropField cropField = field.GetComponent<XunjuuCropField>();
            if (cropField == null)
                cropField = field.gameObject.AddComponent<XunjuuCropField>();
            cropField.ConfigureExisting("Milpa de maíz " + (i + 1), cornRowsPerField, cornStalksPerRow);

            SpriteRenderer[] crops = field.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer cropRenderer in crops)
            {
                if (cropRenderer == null || !cropRenderer.gameObject.name.StartsWith("Maiz_"))
                    continue;
                if (cropRenderer.GetComponent<XunjuuCrop>() == null)
                    cropRenderer.gameObject.AddComponent<XunjuuCrop>();
            }
        }
    }

    void CreateCornFurrow(Transform parent, Vector3 fieldCenter, Vector3 rowAxis, Vector3 lineAxis, float rowOffset, float length, float width)
    {
        Vector3 center = fieldCenter + rowAxis * rowOffset;
        Vector3 lineDirection = lineAxis.normalized;
        Vector3 widthDirection = rowAxis.normalized;
        Vector3 halfLine = lineDirection * (length * 0.5f);
        Vector3 halfWidth = widthDirection * (width * 0.5f);

        GameObject furrow = new GameObject("Surco_Milpa");
        furrow.transform.SetParent(parent, false);
        furrow.transform.localPosition = Vector3.zero;

        int segments = Mathf.Max(6, Mathf.CeilToInt(length * 1.35f));
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 alongRow = center - halfLine + lineDirection * (length * t);
            Vector3 leftWorld = alongRow - halfWidth;
            Vector3 rightWorld = alongRow + halfWidth;
            Vector3 leftGround = GroundPoint(leftWorld);
            Vector3 rightGround = GroundPoint(rightWorld);
            int vi = i * 2;
            vertices[vi] = parent.InverseTransformPoint(new Vector3(leftWorld.x, leftGround.y + 0.045f, leftWorld.z));
            vertices[vi + 1] = parent.InverseTransformPoint(new Vector3(rightWorld.x, rightGround.y + 0.045f, rightWorld.z));
            uvs[vi] = new Vector2(0f, t * length * 0.55f);
            uvs[vi + 1] = new Vector2(1f, t * length * 0.55f);
        }

        for (int i = 0; i < segments; i++)
        {
            int vi = i * 2;
            int ti = i * 6;
            triangles[ti] = vi;
            triangles[ti + 1] = vi + 2;
            triangles[ti + 2] = vi + 1;
            triangles[ti + 3] = vi + 1;
            triangles[ti + 4] = vi + 2;
            triangles[ti + 5] = vi + 3;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Mesh_Surco_Milpa";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = furrow.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = furrow.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = GetFurrowMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    Material GetFurrowMaterial()
    {
        if (runtimeFurrowMaterial != null)
            return runtimeFurrowMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return null;

        runtimeFurrowMaterial = new Material(shader);
        runtimeFurrowMaterial.name = "Material_Surcos_Milpa_Runtime";
        Color earth = new Color(0.26f, 0.16f, 0.09f, 1f);
        if (runtimeFurrowMaterial.HasProperty("_BaseColor"))
            runtimeFurrowMaterial.SetColor("_BaseColor", earth);
        if (runtimeFurrowMaterial.HasProperty("_Color"))
            runtimeFurrowMaterial.SetColor("_Color", earth);

        Texture2D furrowTexture = Resources.Load<Texture2D>("Textures/Surcos_Milpa_Referencia");
        if (furrowTexture != null)
        {
            furrowTexture.wrapMode = TextureWrapMode.Repeat;
            furrowTexture.filterMode = FilterMode.Trilinear;
            furrowTexture.anisoLevel = 4;
            if (runtimeFurrowMaterial.HasProperty("_BaseMap"))
            {
                runtimeFurrowMaterial.SetTexture("_BaseMap", furrowTexture);
                runtimeFurrowMaterial.SetTextureScale("_BaseMap", new Vector2(1f, 3.2f));
            }
            if (runtimeFurrowMaterial.HasProperty("_MainTex"))
            {
                runtimeFurrowMaterial.SetTexture("_MainTex", furrowTexture);
                runtimeFurrowMaterial.SetTextureScale("_MainTex", new Vector2(1f, 3.2f));
            }
        }

        return runtimeFurrowMaterial;
    }

    Sprite GetCornStalkSprite()
    {
        if (runtimeCornSprite != null)
            return runtimeCornSprite;

        Texture2D importedCorn = Resources.Load<Texture2D>("Textures/Maiz_Milpa_Recortado");
        if (importedCorn == null)
            importedCorn = Resources.Load<Texture2D>("Textures/Maiz_Milpa_Referencia");
        if (importedCorn != null)
        {
            importedCorn.wrapMode = TextureWrapMode.Clamp;
            importedCorn.filterMode = FilterMode.Bilinear;
            float pixelsPerUnit = Mathf.Max(150f, importedCorn.height / 3.8f);
            runtimeCornSprite = Sprite.Create(importedCorn, new Rect(0f, 0f, importedCorn.width, importedCorn.height), new Vector2(0.5f, 0f), pixelsPerUnit);
            runtimeCornSprite.name = "Maiz_Milpa_Imagen";
            return runtimeCornSprite;
        }

        Texture2D texture = new Texture2D(192, 320, TextureFormat.RGBA32, false);
        texture.name = "Maiz_Milpa_Dyanatro_Texture";
        Color[] pixels = new Color[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;
        texture.SetPixels(pixels);

        Color stemDark = new Color(0.14f, 0.35f, 0.08f, 1f);
        Color stemMid = new Color(0.45f, 0.72f, 0.15f, 1f);
        Color leafLight = new Color(0.78f, 0.92f, 0.28f, 1f);
        Color cob = new Color(0.98f, 0.78f, 0.2f, 1f);
        int centerX = texture.width / 2;

        DrawCornLine(texture, centerX, 8, centerX + 4, 280, 7, stemDark, stemMid);
        DrawCornLine(texture, centerX - 7, 22, centerX - 3, 255, 4, stemDark, stemMid);

        for (int i = 0; i < 8; i++)
        {
            int y = 48 + i * 26;
            int side = i % 2 == 0 ? -1 : 1;
            int length = Random.Range(42, 74);
            int lift = Random.Range(16, 40);
            Color leaf = Color.Lerp(stemMid, leafLight, Random.Range(0.15f, 0.75f));
            DrawCornLeaf(texture, centerX + side * 2, y, centerX + side * length, y + lift, side, leaf);
        }

        DrawCornCob(texture, centerX - 22, 142, 13, 31, cob);
        DrawCornCob(texture, centerX + 25, 182, 12, 28, cob);
        DrawCornTassel(texture, centerX + 4, 280, leafLight);

        texture.Apply(false, false);
        runtimeCornSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), 118f);
        runtimeCornSprite.name = "Maiz_Milpa_Dyanatro";
        return runtimeCornSprite;
    }

    void DrawCornLeaf(Texture2D texture, int x0, int y0, int x1, int y1, int side, Color color)
    {
        int steps = Mathf.Max(1, Mathf.Abs(x1 - x0));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float curve = Mathf.Sin(t * Mathf.PI);
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t) - curve * 16f);
            int width = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(10f, 1f, t) * curve));
            for (int ox = -width; ox <= width; ox++)
            {
                Color pixel = color;
                pixel.a = Mathf.Clamp01(1f - Mathf.Abs(ox) / (width + 1f));
                BlendPixel(texture, x + ox * side, y, pixel);
            }
        }
    }

    void DrawCornLine(Texture2D texture, int x0, int y0, int x1, int y1, int width, Color bottom, Color top)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)Mathf.Max(1, steps);
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            Color color = Color.Lerp(bottom, top, t);
            for (int ox = -width; ox <= width; ox++)
                BlendPixel(texture, x + ox, y, color);
        }
    }

    void DrawCornCob(Texture2D texture, int cx, int cy, int radiusX, int radiusY, Color color)
    {
        for (int y = -radiusY; y <= radiusY; y++)
        {
            for (int x = -radiusX; x <= radiusX; x++)
            {
                float n = (x * x) / (float)(radiusX * radiusX) + (y * y) / (float)(radiusY * radiusY);
                if (n > 1f)
                    continue;

                Color pixel = Color.Lerp(new Color(0.55f, 0.82f, 0.16f, 1f), color, 0.72f + Random.value * 0.18f);
                pixel.a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.78f, 1f, n));
                BlendPixel(texture, cx + x, cy + y, pixel);
            }
        }
    }

    void DrawCornTassel(Texture2D texture, int x, int y, Color color)
    {
        for (int i = -4; i <= 4; i++)
            DrawCornLine(texture, x, y, x + i * 6, y + Random.Range(18, 34), 1, color, new Color(0.92f, 0.72f, 0.28f, 1f));
    }

    void DrawGrassBlade(Texture2D texture, int baseX, int baseY, int height, float bend, float sway, Color dark, Color mid, Color light)
    {
        for (int y = 0; y < height; y++)
        {
            float t = y / (float)height;
            float curve = Mathf.SmoothStep(0f, 1f, t);
            int px = Mathf.RoundToInt(baseX + bend * curve + Mathf.Sin(t * Mathf.PI) * sway);
            int py = baseY + y;
            int width = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(6f, 1f, t)));
            Color color = t > 0.68f ? Color.Lerp(mid, light, (t - 0.68f) / 0.32f) : Color.Lerp(dark, mid, t / 0.68f);

            for (int ox = -width; ox <= width; ox++)
            {
                Color pixel = color;
                pixel.a = Mathf.Clamp01(1f - Mathf.Abs(ox) / (width + 1f));
                BlendPixel(texture, px + ox, py, pixel);
            }
        }
    }

    void BlendPixel(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
            return;

        Color current = texture.GetPixel(x, y);
        Color blended = Color.Lerp(current, color, color.a);
        blended.a = Mathf.Clamp01(current.a + color.a * (1f - current.a));
        texture.SetPixel(x, y, blended);
    }

    void SpawnDistantSunsetHills()
    {
        if (playerTransform == null)
            return;

        GameObject oldRoot = GameObject.Find("Fondo_Atardecer_Dyanatro");
        if (oldRoot != null)
            Destroy(oldRoot);
        GameObject oldSkyRoot = GameObject.Find("Fondo_Cielo_Dyanatro");
        if (oldSkyRoot != null)
            Destroy(oldSkyRoot);

        GameObject root = new GameObject("Fondo_Cielo_Dyanatro");
        Sprite skyGradient = CreateSkyGradientSprite();
        Sprite farHill = CreateHillSprite(new Color(0.47f, 0.63f, 0.66f, 0.52f), new Color(0.29f, 0.46f, 0.42f, 0.62f));
        Sprite sun = CreateCircleSprite(new Color(1f, 0.88f, 0.48f, 0.42f));

        Vector3 center = playerTransform.position;
        GameObject skyObject = new GameObject("Cielo_Degradado_Pixel");
        skyObject.transform.SetParent(root.transform, false);
        skyObject.transform.position = center + new Vector3(0f, 36f, 214f);
        skyObject.transform.localScale = new Vector3(260f, 96f, 1f);
        SpriteRenderer skyRenderer = skyObject.AddComponent<SpriteRenderer>();
        skyRenderer.sprite = skyGradient;
        skyRenderer.sortingOrder = -2600;
        skyObject.AddComponent<CloudBillboard>();

        for (int i = 0; i < 4; i++)
        {
            GameObject hill = new GameObject("Cerro_Fondo_Azul");
            hill.transform.SetParent(root.transform, false);
            hill.transform.position = center + new Vector3(-110f + i * 72f, 8.5f + i * 0.8f, 150f + i * 28f);
            hill.transform.localScale = new Vector3(72f, 13f, 1f);
            SpriteRenderer renderer = hill.AddComponent<SpriteRenderer>();
            renderer.sprite = farHill;
            renderer.sortingOrder = -2200;
            renderer.color = new Color(1f, 1f, 1f, 0.74f);
            hill.AddComponent<CloudBillboard>();
        }

        GameObject sunObject = new GameObject("Sol_Fondo_Azul");
        sunObject.transform.SetParent(root.transform, false);
        sunObject.transform.position = center + new Vector3(52f, 36f, 180f);
        sunObject.transform.localScale = Vector3.one * 13f;
        SpriteRenderer sunRenderer = sunObject.AddComponent<SpriteRenderer>();
        sunRenderer.sprite = sun;
        sunRenderer.sortingOrder = -2300;
        sunObject.AddComponent<CloudBillboard>();
    }

    Sprite CreateHillSprite(Color top, Color bottom)
    {
        Texture2D texture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float ridge = 20f + Mathf.Sin(x * 0.07f) * 8f + Mathf.Sin(x * 0.19f) * 4f;
                bool visible = y < ridge;
                Color color = visible ? Color.Lerp(bottom, top, y / Mathf.Max(1f, ridge)) : Color.clear;
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), 32f);
    }

    Sprite CreateSkyGradientSprite()
    {
        Texture2D texture = new Texture2D(192, 96, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color top = new Color(0.34f, 0.62f, 0.86f, 1f);
        Color middle = new Color(0.58f, 0.78f, 0.93f, 1f);
        Color horizon = new Color(0.92f, 0.78f, 0.52f, 1f);

        for (int y = 0; y < texture.height; y++)
        {
            float t = y / (float)(texture.height - 1);
            Color row = t < 0.58f
                ? Color.Lerp(horizon, middle, t / 0.58f)
                : Color.Lerp(middle, top, (t - 0.58f) / 0.42f);

            for (int x = 0; x < texture.width; x++)
            {
                float cloudSoftness = Mathf.PerlinNoise(x * 0.025f, y * 0.045f) * 0.035f;
                Color pixel = new Color(
                    Mathf.Clamp01(row.r + cloudSoftness),
                    Mathf.Clamp01(row.g + cloudSoftness),
                    Mathf.Clamp01(row.b + cloudSoftness),
                    1f
                );
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
    }

    Sprite CreateHorizonHazeSprite()
    {
        Texture2D texture = new Texture2D(256, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < texture.height; y++)
        {
            float vertical = y / (float)(texture.height - 1);
            Color low = new Color(0.34f, 0.55f, 0.48f, 0.88f);
            Color mid = new Color(0.56f, 0.74f, 0.82f, 0.56f);
            Color high = new Color(0.60f, 0.78f, 0.96f, 0.08f);
            Color row = vertical < 0.48f
                ? Color.Lerp(low, mid, vertical / 0.48f)
                : Color.Lerp(mid, high, (vertical - 0.48f) / 0.52f);

            for (int x = 0; x < texture.width; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.035f, y * 0.08f) * 0.08f;
                Color pixel = row;
                pixel.a = Mathf.Clamp01(row.a + noise);
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), 32f);
    }

    Sprite CreateDistantTreeLineSprite()
    {
        Texture2D texture = new Texture2D(256, 48, TextureFormat.RGBA32, false);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
                texture.SetPixel(x, y, Color.clear);
        }

        for (int x = 0; x < texture.width; x++)
        {
            float ridge = 10f + Mathf.PerlinNoise(x * 0.045f, 3.4f) * 20f + Mathf.Sin(x * 0.11f) * 3f;
            for (int y = 0; y < ridge; y++)
            {
                float vertical = y / Mathf.Max(1f, ridge);
                Color color = Color.Lerp(new Color(0.14f, 0.32f, 0.22f, 0.62f), new Color(0.28f, 0.50f, 0.35f, 0.38f), vertical);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), 32f);
    }

    Sprite CreateCircleSprite(Color color)
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(31.5f, 31.5f);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / 31.5f;
                Color pixel = color;
                pixel.a *= Mathf.Clamp01(1f - Mathf.SmoothStep(0.65f, 1f, distance));
                texture.SetPixel(x, y, pixel);
            }
        }
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
    }

    void ApplyExistingTerrainTexture()
    {
        if (terrainPresentationApplied)
            return;

        terrainPresentationApplied = true;
        TerrainLayer grassLayer = CreateTexturedTerrainLayer("Pasto_Atardecer_Dyanatro", true);
        TerrainLayer earthLayer = CreateTexturedTerrainLayer("Tierra_Cerro_Dyanatro", false);
        TerrainLayer[] layers = new[] { grassLayer, earthLayer };

        foreach (Terrain terrain in FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            terrain.materialTemplate = CreateTerrainRuntimeMaterial();
            terrain.terrainData.terrainLayers = layers;
            PaintExistingTerrainLayers(terrain, layers.Length);
            terrain.drawInstanced = true;
        }
    }

    Material CreateTerrainRuntimeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (shader == null)
            shader = Shader.Find("Nature/Terrain/Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return null;

        Material material = new Material(shader);
        material.name = "Material_Terreno_Pasto_Dyanatro_Runtime";
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        return material;
    }

    TerrainLayer CreateTexturedTerrainLayer(string name, bool grass)
    {
        TerrainLayer layer = new TerrainLayer();
        layer.name = name;
        Texture2D importedGrass = grass ? Resources.Load<Texture2D>("Textures/jake-nackos-C2PCa6DhlYE-unsplash") : null;
        if (importedGrass != null)
        {
            importedGrass.wrapMode = TextureWrapMode.Repeat;
            importedGrass.filterMode = FilterMode.Trilinear;
            importedGrass.anisoLevel = 4;
        }
        layer.diffuseTexture = grass ? (importedGrass != null ? importedGrass : CreateGrassTerrainTexture(name + "_Texture")) : CreateEarthTerrainTexture(name + "_Texture");
        layer.tileSize = grass ? new Vector2(4.5f, 4.5f) : new Vector2(10f, 10f);
        return layer;
    }

    Texture2D CreateGrassTerrainTexture(string name)
    {
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.name = name;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float n1 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                float n2 = Mathf.PerlinNoise(x * 0.27f + 13f, y * 0.27f + 7f);
                Color green = Color.Lerp(new Color(0.19f, 0.38f, 0.14f), new Color(0.47f, 0.67f, 0.22f), n1);
                Color dirt = new Color(0.34f, 0.25f, 0.14f);
                Color color = Color.Lerp(green, dirt, Mathf.SmoothStep(0.72f, 0.98f, n2) * 0.32f);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply(false, false);
        return texture;
    }

    Texture2D CreateEarthTerrainTexture(string name)
    {
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.name = name;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.09f + 4f, y * 0.09f + 21f);
                Color color = Color.Lerp(new Color(0.31f, 0.25f, 0.18f), new Color(0.55f, 0.38f, 0.24f), n);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply(false, false);
        return texture;
    }

    void PaintExistingTerrainLayers(Terrain terrain, int layerCount)
    {
        TerrainData data = terrain.terrainData;
        if (data == null || data.alphamapWidth <= 0 || data.alphamapHeight <= 0 || layerCount <= 0)
            return;

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        float[,,] map = new float[height, width, layerCount];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float ny = y / (float)(height - 1);
                float slope = data.GetSteepness(nx, ny);

                if (layerCount == 1)
                {
                    map[y, x, 0] = 1f;
                    continue;
                }

                float mountain = Mathf.InverseLerp(mountainSlope - 7f, mountainSlope + 12f, slope);
                map[y, x, 0] = 1f - mountain;
                map[y, x, 1] = mountain;

                for (int i = 2; i < layerCount; i++)
                    map[y, x, i] = 0f;
            }
        }

        data.SetAlphamaps(0, 0, map);
    }

    void AttachDepthSorters()
    {
        foreach (SpriteRenderer renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (renderer == null)
                continue;

            string spriteName = renderer.sprite != null ? renderer.sprite.name.ToLowerInvariant() : string.Empty;
            string objectName = renderer.gameObject.name.ToLowerInvariant();
            bool isCloud = renderer.GetComponentInParent<CloudBillboard>() != null
                || objectName.Contains("cloud")
                || objectName.Contains("nube")
                || objectName.Contains("clound")
                || spriteName.Contains("cloud")
                || spriteName.Contains("nube")
                || spriteName.Contains("clound");

            if (isCloud)
            {
                renderer.sortingOrder = -1500;
                continue;
            }

            TreeInteractivo tree = renderer.GetComponentInParent<TreeInteractivo>();
            if (tree != null && tree.GetComponent<XunjuuTreeCameraOccluder>() == null)
                tree.gameObject.AddComponent<XunjuuTreeCameraOccluder>();

            if (objectName.Contains("fondo") || objectName.Contains("cerro"))
            {
                renderer.sortingOrder = -2200;
                continue;
            }

            DyanatroSpriteDepthSorter sorter = renderer.GetComponent<DyanatroSpriteDepthSorter>();
            if (sorter == null)
                sorter = renderer.gameObject.AddComponent<DyanatroSpriteDepthSorter>();

            int baseOrder = renderer.CompareTag("Player") || renderer.GetComponentInParent<PlayerController>() != null ? 15 : 0;
            sorter.Configure(baseOrder, 10f);
        }
    }

    IEnumerator AttachDepthSortersDelayed()
    {
        yield return null;
        yield return null;
        AttachDepthSorters();
    }

    IEnumerator MoveCameraToPrologueView(int beatIndex, float duration)
    {
        if (mainCamera == null || playerTransform == null)
            yield break;

        Vector3 focus = playerTransform.position + Vector3.up * 1.8f;
        Vector3 offset;

        switch (beatIndex)
        {
            case 0:
                offset = new Vector3(-4f, 8.5f, -15f);
                focus += Vector3.up * 1.2f;
                break;
            case 1:
                offset = new Vector3(0f, 16f, -30f);
                focus += new Vector3(0f, 8f, 20f);
                break;
            case 2:
                offset = new Vector3(10f, 12f, -20f);
                focus += new Vector3(0f, 4f, 10f);
                break;
            case 3:
                offset = new Vector3(-12f, 10f, -18f);
                focus += new Vector3(0f, 3f, 12f);
                break;
            case 4:
                offset = new Vector3(6f, 9.5f, -16f);
                focus += new Vector3(0f, 2.5f, 8f);
                break;
            case 5:
                offset = new Vector3(-10f, 11f, -19f);
                focus += new Vector3(0f, 4f, 11f);
                break;
            case 6:
                offset = new Vector3(12f, 12f, -22f);
                focus += new Vector3(0f, 5f, 14f);
                break;
            case 7:
                offset = new Vector3(0f, 13f, -24f);
                focus += new Vector3(0f, 5.5f, 18f);
                break;
            case 8:
                offset = new Vector3(-14f, 12f, -21f);
                focus += new Vector3(0f, 5f, 13f);
                break;
            case 9:
                offset = new Vector3(5f, 14f, -27f);
                focus += new Vector3(0f, 6f, 20f);
                break;
            default:
                offset = new Vector3(3f, 8.5f, -14f);
                focus += Vector3.up * 1.5f;
                break;
        }

        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        Vector3 targetPosition = focus + offset;
        Quaternion targetRotation = Quaternion.LookRotation(focus - targetPosition);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
    }
    void ImproveTerrains()
    {
        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        foreach (Terrain terrain in terrains)
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            TerrainLayer grass = CreateTerrainLayer("Pasto_Dyanatro", new Color(0.18f, 0.47f, 0.18f), 8f);
            TerrainLayer mountain = CreateTerrainLayer("Montana_Dyanatro", new Color(0.42f, 0.38f, 0.32f), 13f);
            terrain.terrainData.terrainLayers = new[] { grass, mountain };
            PaintTerrainBySlope(terrain);
            AddGrassDetails(terrain);
        }
    }

    TerrainLayer CreateTerrainLayer(string name, Color color, float tileSize)
    {
        TerrainLayer layer = new TerrainLayer();
        layer.name = name;
        layer.diffuseTexture = CreateSolidTexture(name + "_Texture", color);
        layer.tileSize = new Vector2(tileSize, tileSize);
        return layer;
    }

    Texture2D CreateSolidTexture(string name, Color color)
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.name = name;
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
        {
            float noise = 0.92f + Random.value * 0.16f;
            pixels[i] = color * noise;
            pixels[i].a = 1f;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    void PaintTerrainBySlope(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        float[,,] map = new float[height, width, 2];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float ny = y / (float)(height - 1);
                float slope = data.GetSteepness(nx, ny);
                float rock = Mathf.InverseLerp(mountainSlope - 8f, mountainSlope + 8f, slope);
                map[y, x, 0] = 1f - rock;
                map[y, x, 1] = rock;
            }
        }

        data.SetAlphamaps(0, 0, map);
    }

    void AddGrassDetails(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        DetailPrototype grass = new DetailPrototype();
        grass.prototypeTexture = CreateGrassBladeTexture();
        grass.renderMode = DetailRenderMode.GrassBillboard;
        grass.minWidth = 0.4f;
        grass.maxWidth = 0.9f;
        grass.minHeight = 0.55f;
        grass.maxHeight = 1.15f;
        grass.healthyColor = new Color(0.16f, 0.48f, 0.16f);
        grass.dryColor = new Color(0.56f, 0.48f, 0.22f);
        grass.noiseSpread = 0.45f;

        data.detailPrototypes = new[] { grass };
        data.SetDetailResolution(64, 16);

        int resolution = data.detailResolution;
        int[,] layer = new int[resolution, resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = x / (float)(resolution - 1);
                float ny = y / (float)(resolution - 1);
                float slope = data.GetSteepness(nx, ny);
                layer[y, x] = slope < maxTreeSlope && Random.value > 0.55f ? Random.Range(1, 4) : 0;
            }
        }

        data.SetDetailLayer(0, 0, 0, layer);
    }

    Texture2D CreateGrassBladeTexture()
    {
        Texture2D texture = new Texture2D(8, 16, TextureFormat.RGBA32, false);
        texture.name = "Pasto_Detail_Dyanatro";

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float center = Mathf.Abs(x - 3.5f);
                bool visible = center < Mathf.Lerp(0.4f, 2.6f, y / 15f);
                Color color = visible ? new Color(0.18f, 0.55f + y * 0.012f, 0.16f, 1f) : Color.clear;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    void DisableGeneratedForest()
    {
        ForestOptimized[] generators = FindObjectsByType<ForestOptimized>(FindObjectsSortMode.None);
        foreach (ForestOptimized generator in generators)
        {
            if (generator == null) continue;
            // Xunjuu beta - Detener el generador anterior sin borrar los
            // arboles editables que ya forman parte del escenario.
            generator.enabled = false;
        }
    }

    // ========================================================================
    // Xunjuu v0.1 - Arboles 2.5D
    // ACCION: retirar TreeInstances que exigen mallas 3D y usar los sprites editables.
    // ========================================================================
    void DisableUnsupportedTerrainTreeSystem()
    {
        foreach (Terrain terrain in FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            if (terrain == null || terrain.terrainData == null)
                continue;
            terrain.terrainData.treeInstances = System.Array.Empty<TreeInstance>();
            terrain.terrainData.treePrototypes = System.Array.Empty<TreePrototype>();
        }
    }

    void SpawnGroundedForest()
    {
        SpawnGroundedForest(false);
    }

    // ACCIÓN Xunjuú v0.1: crear árboles persistentes con prefabs editables.
    [ContextMenu("Xunjuú v0.1/Generar bosque editable")]
    public void GenerateEditableForest()
    {
        SpawnGroundedForest(true);
    }

    [ContextMenu("Xunjuú v0.1/Limpiar bosque editable")]
    public void ClearEditableForest()
    {
        GameObject root = GameObject.Find("Bosque_Dyanatro_Generado");
        if (root != null)
            DestroySceneObject(root);
    }

    void SpawnGroundedForest(bool forceRegenerate)
    {
        if (treePrefab == null && (additionalTreePrefabs == null || additionalTreePrefabs.Length == 0))
            return;

        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (terrains.Length == 0)
            return;

        GameObject oldRoot = GameObject.Find("Bosque_Dyanatro_Generado");
        if (oldRoot != null && oldRoot.transform.childCount > 0 && !forceRegenerate)
        {
            PrepareExistingTrees(oldRoot);
            return;
        }
        if (oldRoot != null)
            DestroySceneObject(oldRoot);

        GameObject root = new GameObject("Bosques_Por_Terreno");
        root.name = "Bosque_Dyanatro_Generado";
        int globalTreeIndex = 0;
        List<Transform> protectedGameplayPoints = CollectProtectedForestPoints();

        // ====================================================================
        // Xunjuu v0.1 - Un bosque por cada Terrain
        // ACCION: crear grupos independientes, editables y separados 10 unidades.
        // MODIFICACION: ajusta Trees Per Terrain, Tree Minimum Spacing y Radius.
        // ====================================================================
        for (int terrainIndex = 0; terrainIndex < terrains.Length; terrainIndex++)
        {
            Terrain terrain = terrains[terrainIndex];
            if (terrain == null || terrain.terrainData == null)
                continue;

            GameObject forestGroup = new GameObject($"Bosque_Terreno_{terrainIndex + 1:00}_{terrain.name}");
            forestGroup.transform.SetParent(root.transform, false);

            Vector3 forestCenter = GetForestCenterForTerrain(terrain);
            EnsureForestZoneMarker(forestGroup.transform, forestCenter, terrainIndex + 1);

            List<Vector3> treePositions = new List<Vector3>();
            int spawnedOnTerrain = 0;
            int attempts = 0;
            int targetCount = Mathf.Max(1, treesPerTerrain);
            float maximumRadius = Mathf.Min(
                forestRadiusPerTerrain,
                Mathf.Min(terrain.terrainData.size.x, terrain.terrainData.size.z) * 0.42f);

            while (spawnedOnTerrain < targetCount && attempts < targetCount * 180)
            {
                attempts++;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = maximumRadius * Mathf.Sqrt(Random.value);
                Vector3 candidate = forestCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 pos = SampleTerrainAtWorldXZ(terrain, candidate);

                if (playerTransform != null && XZDistance(pos, playerTransform.position) < treeMinDistanceFromPlayer + 4f)
                    continue;
                if (IsNearProtectedGameplayPoint(pos, protectedGameplayPoints, treeClearanceFromObjectives))
                    continue;
                if (IsInsideFlowerRoute(pos, protectedGameplayPoints, treeClearanceFromRoutes))
                    continue;
                if (GetTerrainSlopeAtWorld(terrain, pos) > maxTreeSlope)
                    continue;
                if (IsNearAnotherTree(pos, treePositions, treeMinimumSpacing))
                    continue;

                GameObject selectedTreePrefab = GetTreePrefabForSpawn(globalTreeIndex);
                if (selectedTreePrefab == null)
                    break;

                Vector3 groundedPos = GroundPoint(pos);
                GameObject tree = Instantiate(selectedTreePrefab, groundedPos, Quaternion.identity, forestGroup.transform);
                tree.name = $"Arbol_{terrainIndex + 1:00}_{spawnedOnTerrain + 1:000}_{selectedTreePrefab.name}";
                tree.SetActive(true);
                tree.transform.localScale *= Random.Range(0.72f, 1.02f);
                SnapTreeSpriteToGround(tree.transform);
                EnsureTreeVisibleInGame(tree);
                if (IsTreeFloatingAboveGround(tree.transform))
                    SnapTreeSpriteToGround(tree.transform);

                PrepareGeneratedTree(tree);
                treePositions.Add(tree.transform.position);
                spawnedOnTerrain++;
                globalTreeIndex++;
            }

            if (spawnedOnTerrain < targetCount)
                Debug.LogWarning($"Xunjuu v0.1: {forestGroup.name} genero {spawnedOnTerrain}/{targetCount} arboles por pendiente o espacio.");
        }

        if (Application.isPlaying)
            StartCoroutine(SettleGeneratedTrees(root.transform));
    }

    // ACCION: calcular el centro util de un Terrain sin depender de otra escena.
    Vector3 GetForestCenterForTerrain(Terrain terrain)
    {
        Terrain playerTerrain = playerTransform != null ? FindTerrainContaining(playerTransform.position) : null;
        if (playerTerrain == terrain)
            return SampleTerrainAtWorldXZ(terrain, playerTransform.position);

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        Vector3 candidate = new Vector3(origin.x + size.x * 0.5f, origin.y, origin.z + size.z * 0.5f);
        return SampleTerrainAtWorldXZ(terrain, candidate);
    }

    // ACCION: validar la distancia horizontal minima entre arboles del bosque.
    bool IsNearAnotherTree(Vector3 position, List<Vector3> placedTrees, float minimumDistance)
    {
        foreach (Vector3 placedTree in placedTrees)
        {
            if (XZDistance(position, placedTree) < minimumDistance)
                return true;
        }
        return false;
    }

    // ACCION: localizar flores, enemigos, animales y puntos de jefe/recompensa
    // para que puedan moverse en Hierarchy y regenerar el bosque sin bloqueos.
    List<Transform> CollectProtectedForestPoints()
    {
        List<Transform> points = new List<Transform>();
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid())
                continue;

            string objectName = candidate.name.ToLowerInvariant();
            bool important = candidate.GetComponent<MazahuaWordCollectible>() != null
                || candidate.GetComponent<XunjuuAnimalHealth>() != null
                || candidate.GetComponent<EnemyHealth>() != null
                || candidate.GetComponent<EnemyFireBreath>() != null
                || candidate.GetComponent<XunjuuBossNivel1>() != null
                || objectName.StartsWith("enemigo_")
                || objectName.Contains("_mision2_")
                || objectName.StartsWith("punto_aparicion_")
                || objectName.StartsWith("punto_recompensa_");
            if (important && !points.Contains(candidate))
                points.Add(candidate);
        }
        return points;
    }

    bool IsNearProtectedGameplayPoint(Vector3 position, List<Transform> points, float clearance)
    {
        foreach (Transform point in points)
        {
            if (point == null)
                continue;

            // Xunjuu v0.1 - ACCION: reservar un claro mayor alrededor del punto
            // editable del jefe para que su escala doble no choque con arboles.
            float requiredClearance = point.name.StartsWith("Punto_Aparicion_Jefe_Final", System.StringComparison.OrdinalIgnoreCase)
                ? Mathf.Max(clearance, 15f)
                : clearance;
            if (XZDistance(position, point.position) < requiredClearance)
                return true;
        }
        return false;
    }

    // ACCION: dejar un corredor directo hacia cada flor sin vaciar todo el bosque.
    bool IsInsideFlowerRoute(Vector3 position, List<Transform> points, float clearance)
    {
        if (playerTransform == null)
            return false;

        foreach (Transform point in points)
        {
            if (point == null || point.GetComponent<MazahuaWordCollectible>() == null)
                continue;
            if (DistanceToSegmentXZ(position, playerTransform.position, point.position) < clearance)
                return true;
        }
        return false;
    }

    float DistanceToSegmentXZ(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 a = new Vector2(start.x, start.z);
        Vector2 b = new Vector2(end.x, end.z);
        Vector2 segment = b - a;
        if (segment.sqrMagnitude < 0.001f)
            return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, segment) / segment.sqrMagnitude);
        return Vector2.Distance(p, a + segment * t);
    }

    // ACCION: agregar interaccion, nombre y orden visual al arbol generado.
    void PrepareGeneratedTree(GameObject tree)
    {
        DyanatroSpriteDepthSorter sorter = tree.GetComponent<DyanatroSpriteDepthSorter>();
        if (sorter == null)
            sorter = tree.AddComponent<DyanatroSpriteDepthSorter>();
        sorter.Configure(0, 10f);

        if (tree.GetComponent<TreeInteractivo>() == null)
            tree.AddComponent<TreeInteractivo>();

        // Xunjuu v0.1 - ACCION: guardar el bloqueo del tronco en Hierarchy.
        CapsuleCollider trunkCollider = tree.GetComponentInChildren<CapsuleCollider>(true);
        if (trunkCollider == null)
        {
            GameObject blocker = new GameObject("TreeBlocker3D");
            blocker.transform.SetParent(tree.transform, false);
            blocker.layer = tree.layer;
            trunkCollider = blocker.AddComponent<CapsuleCollider>();
        }
        trunkCollider.transform.localPosition = Vector3.zero;
        trunkCollider.center = new Vector3(0f, 0.34f, 0f);
        trunkCollider.radius = 0.22f;
        trunkCollider.height = 0.78f;
        trunkCollider.direction = 1;
        trunkCollider.isTrigger = false;

        XunjuuWorldLabel treeLabel = tree.GetComponent<XunjuuWorldLabel>();
        if (treeLabel == null)
            treeLabel = tree.AddComponent<XunjuuWorldLabel>();
        treeLabel.Configure("Arbol", false, new Vector3(0f, 3.2f, 0f));
    }

    Vector3 SampleTerrainAtWorldXZ(Terrain terrain, Vector3 point)
    {
        if (terrain == null || terrain.terrainData == null)
            return point;

        Vector3 basePosition = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float x = Mathf.Clamp(point.x, basePosition.x + 1f, basePosition.x + size.x - 1f);
        float z = Mathf.Clamp(point.z, basePosition.z + 1f, basePosition.z + size.z - 1f);
        float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + basePosition.y;
        return new Vector3(x, y, z);
    }

    void EnsureForestZoneMarker(Transform forestGroup, Vector3 forestCenter, int terrainNumber)
    {
        GameObject zone = new GameObject($"Zona_Bosque_Terreno_{terrainNumber:00}");
        zone.transform.SetParent(forestGroup, false);
        zone.transform.position = forestCenter;
        XunjuuWorldLabel label = zone.AddComponent<XunjuuWorldLabel>();
        label.Configure($"BOSQUE {terrainNumber}\nZona de arboles", true, new Vector3(0f, 5.5f, 0f));
    }

    GameObject GetTreePrefabForSpawn(int index)
    {
        List<GameObject> available = new List<GameObject>();
        if (treePrefab != null)
            available.Add(treePrefab);
        if (additionalTreePrefabs != null)
        {
            foreach (GameObject prefab in additionalTreePrefabs)
            {
                if (prefab != null && !available.Contains(prefab))
                    available.Add(prefab);
            }
        }
        return available.Count > 0 ? available[index % available.Count] : null;
    }

    // Xunjuu v0.1 - ACCION: preparar arboles anidados sin mover zonas o titulos.
    void PrepareExistingTrees(GameObject root)
    {
        if (root == null)
            return;

        foreach (TreeInteractivo treeComponent in root.GetComponentsInChildren<TreeInteractivo>(true))
        {
            if (treeComponent == null)
                continue;

            GameObject tree = treeComponent.gameObject;
            tree.SetActive(true);
            EnsureTreeVisibleInGame(tree);
            if (tree.GetComponent<XunjuuWorldLabel>() == null)
            {
                XunjuuWorldLabel label = tree.AddComponent<XunjuuWorldLabel>();
                label.Configure("Arbol", false, new Vector3(0f, 3.2f, 0f));
            }
        }
    }

    // ACCION: impedir que un prefab o un optimizador heredado oculte el arbol en Game.
    void EnsureTreeVisibleInGame(GameObject tree)
    {
        if (tree == null)
            return;

        foreach (SpriteRenderer renderer in tree.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null)
                continue;
            renderer.enabled = true;
            renderer.forceRenderingOff = false;
            renderer.gameObject.layer = 0;
        }
        tree.layer = 0;
    }

    IEnumerator SettleGeneratedTrees(Transform root)
    {
        if (root == null)
            yield break;

        for (int pass = 0; pass < 5; pass++)
        {
            yield return null;
            TreeInteractivo[] trees = root.GetComponentsInChildren<TreeInteractivo>(true);
            for (int i = trees.Length - 1; i >= 0; i--)
            {
                if (trees[i] == null)
                    continue;
                Transform tree = trees[i].transform;
                SnapTreeSpriteToGround(tree);
                if (IsTreeFloatingAboveGround(tree))
                    SnapTreeSpriteToGround(tree);
            }
        }
    }

    void SpawnGroundDecorations()
    {
        Sprite bushDecorationSprite = LoadResourceSprite(ref runtimeGroundBushSprite, "Sprites/TextureTerrain/arbusto", bushSprite != null ? bushSprite : treeMenuSprite);
        Sprite flowerDecorationSprite = LoadResourceSprite(ref runtimeGroundFlowerSprite, "Sprites/TextureTerrain/flor", flowerSprite);
        Sprite stoneDecorationSprite = LoadResourceSprite(ref runtimeGroundStoneSprite, "Sprites/TextureTerrain/piedra", null);
        Sprite decorationSprite = bushDecorationSprite != null ? bushDecorationSprite : treeMenuSprite;
        if (decorationSprite == null && flowerDecorationSprite == null && stoneDecorationSprite == null)
            return;

        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (terrains.Length == 0)
            return;
        List<Transform> protectedGameplayPoints = CollectProtectedForestPoints();

        GameObject oldRoot = GameObject.Find("Arbustos_Dyanatro_Generados");
        if (oldRoot != null)
            Destroy(oldRoot);

        GameObject root = new GameObject("Arbustos_Dyanatro_Generados");
        int spawned = 0;
        int attempts = 0;
        int targetDecorations = Mathf.Clamp(bushCount, 760, 1400);
        int spawnDecorationTarget = Mathf.Min(280, targetDecorations);
        List<Vector3> decorationPositions = new List<Vector3>();
        Terrain playerTerrain = playerTransform != null ? FindTerrainContaining(playerTransform.position) : null;

        while (spawned < targetDecorations && attempts < targetDecorations * 20)
        {
            attempts++;
            bool preferSpawnArea = playerTerrain != null
                && playerTransform != null
                && (spawned < spawnDecorationTarget || Random.value < 0.58f);
            Terrain terrain = preferSpawnArea ? playerTerrain : terrains[Random.Range(0, terrains.Length)];
            if (terrain == null || terrain.terrainData == null)
                continue;

            Vector3 pos = preferSpawnArea
                ? RandomPointNearOnTerrain(terrain, playerTransform.position, 5f, 78f)
                : RandomPointOnTerrain(terrain);
            if (playerTransform != null && Vector3.Distance(pos, playerTransform.position) < treeMinDistanceFromPlayer * 0.55f)
                continue;
            if (playerTransform != null && Vector3.Distance(pos, playerTransform.position) > 120f)
                continue;

            if (GetTerrainSlopeAtWorld(terrain, pos) > maxTreeSlope)
                continue;
            if (IsNearProtectedGameplayPoint(pos, protectedGameplayPoints, 3.5f))
                continue;

            bool tooClose = false;
            for (int i = 0; i < decorationPositions.Count; i++)
            {
                if (Vector3.Distance(pos, decorationPositions[i]) < 1.18f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            GameObject decoration = new GameObject("Decoracion_Suelo");
            decoration.transform.SetParent(root.transform, false);
            decoration.transform.position = pos;
            SpriteRenderer renderer = decoration.AddComponent<SpriteRenderer>();
            float roll = Random.value;
            float extraSink = 0.1f;
            if (roll > 0.62f && flowerDecorationSprite != null)
            {
                renderer.sprite = flowerDecorationSprite;
                decoration.name = "Flor";
                decoration.transform.localScale = Vector3.one * Random.Range(0.62f, 0.94f);
                renderer.color = Color.Lerp(new Color(1f, 1f, 1f, 1f), new Color(1f, 0.9f, 0.62f, 1f), Random.value * 0.25f);
                extraSink = 0.04f;
            }
            else if (roll > 0.48f && stoneDecorationSprite != null)
            {
                renderer.sprite = stoneDecorationSprite;
                decoration.name = "Piedra";
                decoration.transform.localScale = Vector3.one * Random.Range(0.48f, 0.82f);
                renderer.color = Color.Lerp(new Color(0.82f, 0.82f, 0.78f, 1f), new Color(0.58f, 0.62f, 0.57f, 1f), Random.value * 0.45f);
                extraSink = 0.03f;
            }
            else
            {
                renderer.sprite = decorationSprite != null ? decorationSprite : (flowerDecorationSprite != null ? flowerDecorationSprite : stoneDecorationSprite);
                decoration.name = "Arbusto";
                decoration.transform.localScale = Vector3.one * Random.Range(0.72f, 1.15f);
                renderer.color = Color.Lerp(new Color(0.82f, 1f, 0.82f, 1f), new Color(0.62f, 0.9f, 0.58f, 1f), Random.value);
                extraSink = 0.09f;
            }
            renderer.sortingOrder = -5;

            DyanatroSpriteDepthSorter sorter = decoration.AddComponent<DyanatroSpriteDepthSorter>();
            sorter.Configure(-6, 10f);

            SnapVegetationSpriteToGround(decoration.transform, true, extraSink);
            spawned++;
            decorationPositions.Add(pos);
        }
    }

    Vector3 RandomPointOnTerrain(Terrain terrain)
    {
        Vector3 terrainPos = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float x = terrainPos.x + Random.Range(0f, size.x);
        float z = terrainPos.z + Random.Range(0f, size.z);
        float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrainPos.y;
        return new Vector3(x, y, z);
    }

    // Xunjuu v0.1 - ACCION: llenar la zona inicial sin bloquear al jugador.
    Vector3 RandomPointNearOnTerrain(Terrain terrain, Vector3 center, float minimumRadius, float maximumRadius)
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        float radius = Mathf.Lerp(minimumRadius, maximumRadius, Mathf.Sqrt(Random.value));
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float x = Mathf.Clamp(center.x + direction.x * radius, origin.x + 2f, origin.x + size.x - 2f);
        float z = Mathf.Clamp(center.z + direction.y * radius, origin.z + 2f, origin.z + size.z - 2f);
        Vector3 point = new Vector3(x, origin.y, z);
        point.y = terrain.SampleHeight(point) + origin.y;
        return point;
    }

    float GetTerrainSlopeAtWorld(Terrain terrain, Vector3 worldPosition)
    {
        Vector3 local = worldPosition - terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float nx = Mathf.Clamp01(local.x / size.x);
        float nz = Mathf.Clamp01(local.z / size.z);
        return terrain.terrainData.GetSteepness(nx, nz);
    }

    Vector3 FindGroundPointNear(Vector3 origin, float radius)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            Vector3 candidate = origin + new Vector3(circle.x, 20f, circle.y);
            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 100f))
                return hit.point;
        }

        return GroundPoint(origin);
    }

    Vector3 GroundPoint(Vector3 position)
    {
        Terrain containingTerrain = FindTerrainContaining(position);
        if (containingTerrain != null)
        {
            float y = containingTerrain.SampleHeight(position) + containingTerrain.transform.position.y;
            return new Vector3(position.x, y, position.z);
        }

        if (Physics.Raycast(position + Vector3.up * 80f, Vector3.down, out RaycastHit hit, 200f))
            return hit.point;

        Terrain closest = null;
        float best = float.MaxValue;
        foreach (Terrain candidateTerrain in FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            if (candidateTerrain == null || candidateTerrain.terrainData == null) continue;
            float distance = Vector3.Distance(position, candidateTerrain.transform.position);
            if (distance < best)
            {
                best = distance;
                closest = candidateTerrain;
            }
        }

        if (closest != null)
        {
            float y = closest.SampleHeight(position) + closest.transform.position.y;
            return new Vector3(position.x, y, position.z);
        }

        return position;
    }

    Terrain FindTerrainContaining(Vector3 position)
    {
        foreach (Terrain terrain in FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            bool insideX = position.x >= origin.x && position.x <= origin.x + size.x;
            bool insideZ = position.z >= origin.z && position.z <= origin.z + size.z;
            if (insideX && insideZ)
                return terrain;
        }

        return null;
    }

    void SnapObjectToGround(Transform target, bool sinkIntoGround = true)
    {
        if (target == null)
            return;

        Vector3 ground = GroundPoint(target.position);
        target.position = new Vector3(target.position.x, ground.y, target.position.z);

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            float delta = ground.y - renderer.bounds.min.y;
            target.position += Vector3.up * delta;
            if (sinkIntoGround)
                target.position -= Vector3.up * vegetationSink;
        }
    }

    void SnapVegetationSpriteToGround(Transform target, bool sinkIntoGround = true, float extraSink = 0f)
    {
        if (target == null)
            return;

        Vector3 ground = GroundPoint(target.position);
        target.position = new Vector3(target.position.x, ground.y + 4f, target.position.z);

        SpriteRenderer renderer = target.GetComponentInChildren<SpriteRenderer>();
        if (renderer == null)
        {
            SnapObjectToGround(target, sinkIntoGround);
            return;
        }

        float bottomOffset = renderer.bounds.min.y - target.position.y;
        target.position = new Vector3(target.position.x, ground.y - bottomOffset, target.position.z);
        if (sinkIntoGround)
            target.position -= Vector3.up * (Mathf.Max(0.02f, vegetationSink * 0.65f) + Mathf.Max(0f, extraSink));
    }

    void SnapTreeSpriteToGround(Transform target)
    {
        if (target == null)
            return;

            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0)
        {
            SnapObjectToGround(target, true);
            return;
        }

        Vector3 ground = GroundPoint(target.position);
        target.position = new Vector3(target.position.x, ground.y, target.position.z);

        float visibleBottom = float.MaxValue;
        float maxHeight = 0f;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null)
                continue;

            renderer.enabled = true;
            renderer.forceRenderingOff = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            visibleBottom = Mathf.Min(visibleBottom, GetVisibleSpriteBottomWorldY(renderer));
            maxHeight = Mathf.Max(maxHeight, renderer.bounds.size.y);
        }

        if (visibleBottom == float.MaxValue)
        {
            SnapObjectToGround(target, true);
            return;
        }

        float baseSink = Mathf.Clamp(maxHeight * 0.025f, 0.05f, 0.22f);
        float delta = (ground.y - baseSink) - visibleBottom;
        if (Mathf.Abs(delta) > 0.01f)
            target.position += Vector3.up * delta;
    }

    bool IsTreeFloatingAboveGround(Transform target)
    {
        if (target == null)
            return true;

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0)
            return false;

        float groundY = GroundPoint(target.position).y;
        float visibleBottom = float.MaxValue;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null)
                continue;

            visibleBottom = Mathf.Min(visibleBottom, GetVisibleSpriteBottomWorldY(renderer));
        }

        if (visibleBottom == float.MaxValue)
            return false;

        return visibleBottom - groundY > 0.28f;
    }

    float GetVisibleSpriteBottomWorldY(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null || renderer.sprite.texture == null)
            return renderer != null ? renderer.bounds.min.y : 0f;

        Sprite sprite = renderer.sprite;
        float localY = GetVisibleSpriteBottomLocalY(sprite);
        return renderer.transform.TransformPoint(new Vector3(0f, localY, 0f)).y;
    }

    float GetVisibleSpriteBottomLocalY(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return 0f;

        if (transparentBottomCache.TryGetValue(sprite, out float cachedLocalY))
            return cachedLocalY;

        Texture2D texture = sprite.texture;
        Texture2D readableTexture = texture;
        Rect rect = sprite.textureRect;
        int xMin = Mathf.Clamp(Mathf.FloorToInt(rect.xMin), 0, texture.width - 1);
        int xMax = Mathf.Clamp(Mathf.CeilToInt(rect.xMax), 0, texture.width);
        int yMin = Mathf.Clamp(Mathf.FloorToInt(rect.yMin), 0, texture.height - 1);
        int yMax = Mathf.Clamp(Mathf.CeilToInt(rect.yMax), 0, texture.height);

        try
        {
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    if (readableTexture.GetPixel(x, y).a > 0.08f)
                    {
                        float localY = ((y - rect.yMin) - sprite.pivot.y) / sprite.pixelsPerUnit;
                        transparentBottomCache[sprite] = localY;
                        return localY;
                    }
                }
            }
        }
        catch (UnityException)
        {
            readableTexture = GetReadableTextureCopy(texture);
            if (readableTexture != null)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        if (readableTexture.GetPixel(x, y).a > 0.08f)
                        {
                            float localY = ((y - rect.yMin) - sprite.pivot.y) / sprite.pixelsPerUnit;
                            transparentBottomCache[sprite] = localY;
                            return localY;
                        }
                    }
                }
            }

            float fallbackLocalY = sprite.bounds.min.y + Mathf.Max(0.18f, sprite.bounds.size.y * 0.16f);
            transparentBottomCache[sprite] = fallbackLocalY;
            return fallbackLocalY;
        }

        transparentBottomCache[sprite] = 0f;
        return 0f;
    }

    Texture2D GetReadableTextureCopy(Texture2D source)
    {
        if (source == null)
            return null;

        if (readableTextureCache.TryGetValue(source, out Texture2D cached))
            return cached;

        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Texture2D readable = null;
        try
        {
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;
            readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
            readable.Apply(false, false);
            readableTextureCache[source] = readable;
            return readable;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    void SpawnClouds()
    {
        if (cloudPrefab == null)
            return;

        GameObject root = GameObject.Find("Nubes_Dyanatro");
        if (root == null)
            root = new GameObject("Nubes_Dyanatro");

        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Destroy(root.transform.GetChild(i).gameObject);

        Vector3 center = playerTransform != null ? playerTransform.position : Vector3.zero;
        int count = Mathf.Clamp(cloudCount, 18, 80);
        for (int i = 0; i < count; i++)
        {
            float layer = i / Mathf.Max(1f, count - 1f);
            float x = Random.Range(-230f, 230f);
            float y = Mathf.Lerp(58f, 92f, Random.value);
            float z = Mathf.Lerp(155f, 330f, layer) + Random.Range(-34f, 34f);
            Vector3 pos = center + new Vector3(x, y, z);
            GameObject cloud = Instantiate(cloudPrefab, pos, Quaternion.identity, root.transform);
            cloud.transform.localScale = Vector3.one * Random.Range(1.65f, 4.15f);

            SpriteRenderer renderer = cloud.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = -2400 + i;
                renderer.color = new Color(1f, 1f, 1f, Random.Range(0.68f, 0.92f));
            }

            if (cloud.GetComponent<CloudBillboard>() == null)
                cloud.AddComponent<CloudBillboard>();

            CloudStatic cloudStatic = cloud.GetComponent<CloudStatic>();
            if (cloudStatic == null)
                cloud.AddComponent<CloudStatic>();
        }
    }

    // Xunjuu v0.1 - ACCION: impedir valores imposibles al editar en otra PC.
    void OnValidate()
    {
        treesPerTerrain = Mathf.Max(1, treesPerTerrain);
        treeMinimumSpacing = Mathf.Max(10f, treeMinimumSpacing);
        forestRadiusPerTerrain = Mathf.Max(12f, forestRadiusPerTerrain);
        cornFieldMinimumSpacing = Mathf.Max(12f, cornFieldMinimumSpacing);
        treeClearanceFromObjectives = Mathf.Max(3f, treeClearanceFromObjectives);
        treeClearanceFromRoutes = Mathf.Max(2f, treeClearanceFromRoutes);
        levelOneCollectibleCount = Mathf.Clamp(levelOneCollectibleCount, 1, mazahuaWords.Length);
        mazahuaWordGoal = Mathf.Clamp(mazahuaWordGoal, 1, levelOneCollectibleCount);
        prologueBeatDuration = Mathf.Max(4f, prologueBeatDuration);
        prologueCameraMoveDuration = Mathf.Max(0.8f, prologueCameraMoveDuration);
        introVideoPlaybackSpeed = Mathf.Clamp(introVideoPlaybackSpeed, 0.6f, 1f);
        menuMusicVolume = Mathf.Clamp01(menuMusicVolume);
        gameplayMusicReduction = Mathf.Clamp(gameplayMusicReduction, 0f, 0.8f);
        missionFeedbackDuration = Mathf.Clamp(missionFeedbackDuration, 2f, 8f);
        completedFeedbackDuration = Mathf.Clamp(completedFeedbackDuration, 3f, 10f);
        ludotecaCompletionDelay = Mathf.Clamp(ludotecaCompletionDelay, 2f, 12f);
    }
}
