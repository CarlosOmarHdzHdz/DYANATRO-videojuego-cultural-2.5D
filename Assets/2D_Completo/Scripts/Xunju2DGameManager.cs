using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Xunju2DGameManager : MonoBehaviour
{
    [SerializeField] private Xunju2DPlayerController player;
    [SerializeField] private AudioClip backgroundMusic;

    private Canvas canvas;
    private GameObject menuPanel;
    private GameObject pausePanel;
    private GameObject prologuePanel;
    private GameObject hudPanel;
    private Text healthText;
    private Text objectiveText;
    private Text missionFeedbackText;
    private bool started;
    private bool inPrologue;
    private bool paused;
    private int wordGoal = 6;
    private readonly HashSet<string> collectedWords = new HashSet<string>();

    void Start()
    {
        BuildUi();
        PlayMusic();
        SetGameActive(false);
        menuPanel.SetActive(true);
        pausePanel.SetActive(false);
        prologuePanel.SetActive(false);
        hudPanel.SetActive(false);
    }

    void Update()
    {
        if (!started && Input.GetKeyDown(KeyCode.Return))
            StartGame();

        if (started && !inPrologue && Input.GetKeyDown(KeyCode.Escape))
            SetPause(!paused);

        if (healthText != null && player != null)
            healthText.text = player.CurrentHealth + " / " + player.MaxHealth;
    }

    void BuildUi()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObject = new GameObject("UI_2D_Completo");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        menuPanel = FullPanel("MenuInicio_2D", new Color(0.03f, 0.04f, 0.035f, 0.94f));
        Label(menuPanel.transform, "XUNJUU 2D", 76, new Vector2(0f, 180f), new Vector2(900f, 120f), new Color(1f, 0.86f, 0.42f));
        Label(menuPanel.transform, "San Felipe del Progreso - Las Sombras del Olvido", 34, new Vector2(0f, 95f), new Vector2(1100f, 80f), Color.white);
        Button(menuPanel.transform, "Iniciar", new Vector2(0f, -45f), StartGame);
        Button(menuPanel.transform, "Salir", new Vector2(0f, -135f), QuitGame);

        pausePanel = FullPanel("Pausa_2D", new Color(0f, 0f, 0f, 0.76f));
        Label(pausePanel.transform, "PAUSA", 70, new Vector2(0f, 160f), new Vector2(700f, 100f), Color.white);
        Button(pausePanel.transform, "Continuar", new Vector2(0f, 35f), delegate { SetPause(false); });
        Button(pausePanel.transform, "Reiniciar", new Vector2(0f, -55f), Restart);

        prologuePanel = FullPanel("Prologo_2D", new Color(0f, 0f, 0f, 0.42f));
        Label(prologuePanel.transform, "PROLOGO", 48, new Vector2(0f, 245f), new Vector2(900f, 80f), new Color(1f, 0.86f, 0.42f));

        hudPanel = new GameObject("HUD_2D");
        hudPanel.transform.SetParent(canvas.transform, false);
        healthText = Label(hudPanel.transform, "100 / 100", 34, new Vector2(-760f, 470f), new Vector2(260f, 60f), Color.white);
        objectiveText = Label(hudPanel.transform, "Mision: recoge la espada y recupera 6 palabras mazahuas (0/6)", 24, new Vector2(0f, 500f), new Vector2(1350f, 50f), Color.white);
        missionFeedbackText = Label(hudPanel.transform, string.Empty, 32, new Vector2(0f, 410f), new Vector2(1100f, 80f), new Color(1f, 0.86f, 0.42f));
    }

    GameObject FullPanel(string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    Text Label(Transform parent, string text, int size, Vector2 position, Vector2 dimensions, Color color)
    {
        GameObject labelObject = new GameObject("Text_" + text);
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Text label = labelObject.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        return label;
    }

    void Button(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject("Button_" + text);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(320f, 68f);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.17f, 0.27f, 0.19f, 0.98f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Text label = Label(buttonObject.transform, text, 30, Vector2.zero, rect.sizeDelta, Color.white);
        label.raycastTarget = false;
    }

    public void StartGame()
    {
        if (inPrologue)
            return;

        started = true;
        menuPanel.SetActive(false);
        hudPanel.SetActive(true);
        StartCoroutine(Prologue());
    }

    IEnumerator Prologue()
    {
        inPrologue = true;
        SetGameActive(false);
        prologuePanel.SetActive(true);
        Text body = Label(prologuePanel.transform, string.Empty, 30, new Vector2(0f, -250f), new Vector2(1250f, 220f), Color.white);
        string[] beats =
        {
            "Mateo Jnatr'o escucha el llamado del Nguemuru.",
            "San Felipe del Progreso aparece entre cerros, plaza y memoria mazahua.",
            "Las Dyanatr'o quieren borrar lengua, relatos y vocabulario.",
            "Recoge la espada, documenta palabras y protege la voz jñatjo."
        };

        foreach (string beat in beats)
        {
            body.text = beat;
            yield return new WaitForSecondsRealtime(2.4f);
        }

        Destroy(body.gameObject);
        prologuePanel.SetActive(false);
        SetGameActive(true);
        inPrologue = false;
    }

    public void OnSwordCollected()
    {
        ShowMissionFeedback("Espada recuperada. Ahora puedes defender las palabras.");
        UpdateObjective();
    }

    public void CollectMazahuaWord(string word, string meaning)
    {
        if (word == null || word.Trim().Length == 0 || !collectedWords.Add(word))
            return;

        ShowMissionFeedback("Palabra recuperada: " + word + " - " + meaning);
        UpdateObjective();
    }

    void UpdateObjective()
    {
        int count = collectedWords.Count;
        if (objectiveText == null)
            return;

        if (count >= wordGoal)
            objectiveText.text = "Mision cumplida: vocabulario jñatjo documentado. Derrota a las Dyanatr'o restantes.";
        else
            objectiveText.text = "Mision: recoge la espada y recupera palabras mazahuas (" + count + "/" + wordGoal + ")";
    }

    void ShowMissionFeedback(string message)
    {
        if (missionFeedbackText == null)
            return;

        StopCoroutine(nameof(HideMissionFeedback));
        missionFeedbackText.text = message;
        StartCoroutine(nameof(HideMissionFeedback));
    }

    IEnumerator HideMissionFeedback()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (missionFeedbackText != null)
            missionFeedbackText.text = string.Empty;
    }

    void SetGameActive(bool value)
    {
        Time.timeScale = value ? 1f : 0f;
        if (player != null)
            player.SetCanMove(value);
    }

    void SetPause(bool value)
    {
        paused = value;
        pausePanel.SetActive(value);
        SetGameActive(!value);
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void PlayMusic()
    {
        if (backgroundMusic == null)
            return;

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = backgroundMusic;
        source.loop = true;
        source.volume = 0.45f;
        source.Play();
    }
}
