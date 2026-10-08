using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Selecciona un unico animal cercano, muestra una indicacion clara y evita que
// varios ejemplares respondan a la misma pulsacion.
[DisallowMultipleComponent]
public sealed class XunjuuFaunaCaptureSystem : MonoBehaviour
{
    private static XunjuuFaunaCaptureSystem instance;
    private Transform player;
    private XunjuuAnimalCapture nearest;
    private CanvasGroup promptGroup;
    private Text promptText;
    private CanvasGroup noticeGroup;
    private Text noticeText;
    private Coroutine noticeRoutine;
    private float nextScan;
    private readonly Collider[] nearbyColliders = new Collider[32];

    private void Awake()
    {
        instance = this;
        BuildHud();
    }

    private void Update()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>()?.transform;

        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + .12f;
            nearest = FindNearestCapture();
        }

        bool visible = Time.timeScale > 0f && nearest != null && nearest.IsAvailable;
        if (promptGroup != null)
            promptGroup.alpha = visible ? 1f : 0f;
        if (visible && promptText != null)
        {
            XunjuuFaunaCatalog.Entry entry = nearest.Entry;
            promptText.text = "C  CAPTURAR Y REGISTRAR  ·  " + (entry != null ? entry.DisplayName.ToUpperInvariant() : "FAUNA");
        }

        if (visible && Input.GetKeyDown(KeyCode.C))
        {
            XunjuuAnimalCapture target = nearest;
            nearest = null;
            target.Capture();
        }
    }

    private XunjuuAnimalCapture FindNearestCapture()
    {
        if (player == null)
            return null;

        XunjuuAnimalCapture best = null;
        float bestDistanceSquared = float.MaxValue;
        int hitCount = Physics.OverlapSphereNonAlloc(
            player.position,
            5f,
            nearbyColliders,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = nearbyColliders[i];
            XunjuuAnimalCapture candidate = hit != null ? hit.GetComponentInParent<XunjuuAnimalCapture>() : null;
            if (candidate == null || !candidate.IsAvailable)
                continue;
            Vector3 delta = candidate.transform.position - player.position;
            delta.y = 0f;
            float distanceSquared = delta.sqrMagnitude;
            float captureDistanceSquared = candidate.CaptureDistance * candidate.CaptureDistance;
            if (distanceSquared <= captureDistanceSquared && distanceSquared < bestDistanceSquared)
            {
                best = candidate;
                bestDistanceSquared = distanceSquared;
            }
        }
        return best;
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("HUD_Captura_Fauna", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        GameStateManager.RegisterHud(canvasObject);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2550;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        promptGroup = CreateMessage(canvasObject.transform, "Indicacion_Captura", new Vector2(0f, 92f), new Vector2(720f, 58f), out promptText);
        promptGroup.alpha = 0f;
        noticeGroup = CreateMessage(canvasObject.transform, "Aviso_Captura", new Vector2(0f, 260f), new Vector2(680f, 92f), out noticeText);
        noticeGroup.alpha = 0f;
    }

    private static CanvasGroup CreateMessage(Transform parent, string name, Vector2 position, Vector2 size, out Text text)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, 0f);
        rect.anchorMax = new Vector2(.5f, 0f);
        rect.pivot = new Vector2(.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = new Color(.025f, .075f, .055f, .94f);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(.92f, .66f, .20f, .9f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject textObject = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 6f);
        textRect.offsetMax = new Vector2(-18f, -6f);
        text = textObject.GetComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 20);
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(.98f, .93f, .79f, 1f);
        text.raycastTarget = false;
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    public static void NotifyCapture(string displayName, bool newCard)
    {
        if (instance == null)
            return;
        if (instance.noticeRoutine != null)
            instance.StopCoroutine(instance.noticeRoutine);
        instance.noticeRoutine = instance.StartCoroutine(instance.ShowNotice(displayName, newCard));
    }

    private IEnumerator ShowNotice(string displayName, bool newCard)
    {
        noticeText.text = (newCard ? "NUEVA FICHA EN LA LUDOTECA\n" : "OBSERVACIÓN REGISTRADA\n") + displayName;
        noticeGroup.alpha = 1f;
        yield return new WaitForSeconds(2.4f);
        noticeGroup.alpha = 0f;
        noticeRoutine = null;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
