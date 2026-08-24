using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ============================================================================
// Xunjuu v0.1 - Interfaz visual de inventario
// Accion: mostrar el sprite, nombre y cantidad de cada objeto recogido.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuInventoryUI : MonoBehaviour
{
    [Header("Xunjuu v0.1 - Presentacion")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;
    [SerializeField] private bool startVisible;
    [SerializeField] private Color panelColor = new Color(0.035f, 0.06f, 0.045f, 0.94f);
    [SerializeField] private Color accentColor = new Color(1f, 0.65f, 0.16f, 1f);
    [SerializeField] private Color textColor = new Color(1f, 0.96f, 0.82f, 1f);

    private XunjuuInventory inventory;
    private GameObject panel;
    private RectTransform itemContainer;
    private Font interfaceFont;
    private Text selectedItemText;

    private void Start()
    {
        inventory = GetComponent<XunjuuInventory>();
        if (inventory == null)
            inventory = gameObject.AddComponent<XunjuuInventory>();

        EnsureEventSystem();
        BuildInterface();
        inventory.Changed += Refresh;
        SetVisible(startVisible);
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();

        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.DownArrow))
            inventory.SelectNext();
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.UpArrow))
            inventory.SelectPrevious();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.Changed -= Refresh;
    }

    // ACCION: construir una interfaz desplazable compatible con teclado y Android.
    private void BuildInterface()
    {
        Transform previous = transform.Find("Inventario_Xunjuu_UI");
        if (previous != null)
            Destroy(previous.gameObject);

        interfaceFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI" }, 18);
        GameObject canvasObject = new GameObject("Inventario_Xunjuu_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2600;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject buttonObject = CreateUiObject("Boton_Inventario", canvasObject.transform, typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-28f, 28f);
        buttonRect.sizeDelta = new Vector2(250f, 58f);
        buttonObject.GetComponent<Image>().color = panelColor;
        buttonObject.GetComponent<Button>().onClick.AddListener(Toggle);
        CreateText("Texto", buttonObject.transform, "INVENTARIO [I]", 23, accentColor, TextAnchor.MiddleCenter);

        panel = CreateUiObject("Panel_Inventario", canvasObject.transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-28f, -20f);
        panelRect.sizeDelta = new Vector2(470f, 620f);
        panel.GetComponent<Image>().color = panelColor;

        Text title = CreateText("Titulo", panel.transform, "INVENTARIO DE MATEO", 31, accentColor, TextAnchor.UpperCenter);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -24f);
        title.rectTransform.sizeDelta = new Vector2(-32f, 48f);

        Text subtitle = CreateText("Subtitulo", panel.transform, "Objetos y memoria recuperada", 18, textColor, TextAnchor.MiddleCenter);
        subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, -58f);
        subtitle.rectTransform.sizeDelta = new Vector2(-36f, 26f);

        // Xunjuu v0.1 - ACCION: controles visibles para elegir objetos en orden.
        GameObject previousButton = CreateUiObject("Seleccion_Anterior", panel.transform, typeof(Image), typeof(Button));
        RectTransform previousRect = previousButton.GetComponent<RectTransform>();
        previousRect.anchorMin = new Vector2(0f, 1f);
        previousRect.anchorMax = new Vector2(0f, 1f);
        previousRect.pivot = new Vector2(0f, 1f);
        previousRect.anchoredPosition = new Vector2(24f, -91f);
        previousRect.sizeDelta = new Vector2(54f, 42f);
        previousButton.GetComponent<Image>().color = accentColor;
        previousButton.GetComponent<Button>().onClick.AddListener(inventory.SelectPrevious);
        CreateText("Texto", previousButton.transform, "<", 28, panelColor, TextAnchor.MiddleCenter);

        GameObject nextButton = CreateUiObject("Seleccion_Siguiente", panel.transform, typeof(Image), typeof(Button));
        RectTransform nextRect = nextButton.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(1f, 1f);
        nextRect.anchorMax = new Vector2(1f, 1f);
        nextRect.pivot = new Vector2(1f, 1f);
        nextRect.anchoredPosition = new Vector2(-24f, -91f);
        nextRect.sizeDelta = new Vector2(54f, 42f);
        nextButton.GetComponent<Image>().color = accentColor;
        nextButton.GetComponent<Button>().onClick.AddListener(inventory.SelectNext);
        CreateText("Texto", nextButton.transform, ">", 28, panelColor, TextAnchor.MiddleCenter);

        selectedItemText = CreateText("Objeto_Seleccionado", panel.transform, "Seleccion: ninguna", 19, textColor, TextAnchor.MiddleCenter);
        selectedItemText.rectTransform.anchorMin = new Vector2(0f, 1f);
        selectedItemText.rectTransform.anchorMax = new Vector2(1f, 1f);
        selectedItemText.rectTransform.pivot = new Vector2(0.5f, 1f);
        selectedItemText.rectTransform.anchoredPosition = new Vector2(0f, -91f);
        selectedItemText.rectTransform.sizeDelta = new Vector2(-170f, 42f);

        GameObject viewportObject = CreateUiObject("Viewport_Objetos", panel.transform, typeof(Image), typeof(Mask));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(24f, 28f);
        viewportRect.offsetMax = new Vector2(-24f, -145f);
        viewportObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = true;

        GameObject contentObject = CreateUiObject("Objetos_Con_Sprites", viewportObject.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        itemContainer = contentObject.GetComponent<RectTransform>();
        itemContainer.anchorMin = new Vector2(0f, 1f);
        itemContainer.anchorMax = new Vector2(1f, 1f);
        itemContainer.pivot = new Vector2(0.5f, 1f);
        itemContainer.anchoredPosition = Vector2.zero;
        itemContainer.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = panel.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = itemContainer;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    // ========================================================================
    // Xunjuu v0.1 - Objetos visuales
    // ACCION: reconstruir filas con los sprites reales guardados en inventario.
    // ========================================================================
    private void Refresh()
    {
        if (itemContainer == null || inventory == null)
            return;

        for (int index = itemContainer.childCount - 1; index >= 0; index--)
        {
            Transform child = itemContainer.GetChild(index);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        if (inventory.Items.Count == 0)
        {
            Text empty = CreateText("Inventario_Vacio", itemContainer, "Todavia no hay objetos.", 22, textColor, TextAnchor.MiddleCenter);
            empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            if (selectedItemText != null)
                selectedItemText.text = "Seleccion: ninguna";
            return;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
            CreateItemRow(inventory.Items[index], index);

        if (selectedItemText != null)
            selectedItemText.text = inventory.SelectedItem != null
                ? "Seleccion: " + inventory.SelectedItem.DisplayName
                : "Seleccion: ninguna";
    }

    private void CreateItemRow(XunjuuInventory.Slot slot, int index)
    {
        GameObject row = CreateUiObject("Objeto_" + slot.ItemId, itemContainer, typeof(Image), typeof(LayoutElement), typeof(Button));
        bool selected = index == inventory.SelectedIndex;
        row.GetComponent<Image>().color = selected
            ? new Color(0.32f, 0.28f, 0.08f, 0.98f)
            : new Color(0.11f, 0.17f, 0.12f, 0.96f);
        row.GetComponent<LayoutElement>().preferredHeight = 76f;
        int capturedIndex = index;
        row.GetComponent<Button>().onClick.AddListener(() => inventory.SelectIndex(capturedIndex));

        GameObject iconObject = CreateUiObject("Sprite_" + slot.ItemId, row.transform, typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        iconRect.sizeDelta = new Vector2(58f, 58f);
        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = slot.Icon;
        iconImage.preserveAspect = true;
        iconImage.color = slot.Icon != null ? Color.white : new Color(accentColor.r, accentColor.g, accentColor.b, 0.25f);

        string prefix = selected ? "> " : string.Empty;
        Text label = CreateText("Nombre_Cantidad", row.transform, prefix + slot.DisplayName + "   x" + slot.Amount, 22, textColor, TextAnchor.MiddleLeft);
        label.rectTransform.offsetMin = new Vector2(82f, 0f);
        label.rectTransform.offsetMax = new Vector2(-12f, 0f);
    }

    // ACCION: abrir o cerrar el panel conservando el boton visible.
    public void Toggle()
    {
        SetVisible(panel == null || !panel.activeSelf);
    }

    public void SetVisible(bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }

    private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject result = new GameObject(name, components);
        result.transform.SetParent(parent, false);
        return result;
    }

    private Text CreateText(string name, Transform parent, string value, int size, Color color, TextAnchor anchor)
    {
        GameObject textObject = CreateUiObject(name, parent, typeof(Text));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = interfaceFont;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem_Xunjuu", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
