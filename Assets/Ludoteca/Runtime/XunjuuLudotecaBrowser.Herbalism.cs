using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class XunjuuLudotecaBrowser
{
    private GameObject plantsRoot, plantDetailRoot;
    private readonly Dictionary<string, FaunaCardView> plantCards = new Dictionary<string, FaunaCardView>();

    private GameObject BuildPlantCollection(Transform parent)
    {
        var root = new GameObject("Coleccion_Herbolaria", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(.035f, .075f), new Vector2(.965f, .82f));
        int index = 0;
        foreach (var entry in XunjuuMedicinalPlantCatalog.Entries)
        {
            int column = index % 3, row = index / 3;
            var card = Panel(root.transform, "Ficha_Planta_" + entry.Id, new Color(.992f, .977f, .933f));
            SetRect(card.GetComponent<RectTransform>(), new Vector2(column / 3f + .012f, row == 0 ? .53f : .055f),
                new Vector2((column + 1) / 3f - .012f, row == 0 ? .975f : .50f));
            AddOutline(card, new Color(.70f, .68f, .55f), 1.5f);
            var accent = Panel(card.transform, "Hilo_Herbario", column == 0 ? GoldDark : column == 1 ? ForestGreen : TextileBlue);
            SetRect(accent.GetComponent<RectTransform>(), new Vector2(0, .986f), Vector2.one); accent.GetComponent<Image>().raycastTarget = false;
            TextBlock(card.transform, "HERBARIO / " + (index + 1).ToString("00"), 13, new Vector2(.5f, .92f), new Vector2(330, 22), TextAnchor.MiddleCenter, GoldDark);
            var name = TextBlock(card.transform, entry.DisplayName.ToUpperInvariant(), 24, new Vector2(.5f, .81f), new Vector2(330, 39), TextAnchor.MiddleCenter, ForestGreen);
            name.fontStyle = FontStyles.Bold;
            var plate = Panel(card.transform, "Lamina_Botanica", new Color(.92f, .94f, .86f, 1));
            SetRect(plate.GetComponent<RectTransform>(), new Vector2(.035f, .19f), new Vector2(.46f, .70f)); plate.GetComponent<Image>().raycastTarget = false;
            var portrait = new GameObject("Retrato", typeof(RectTransform), typeof(Image)); portrait.transform.SetParent(card.transform, false);
            SetRect(portrait.GetComponent<RectTransform>(), new Vector2(.055f, .21f), new Vector2(.44f, .68f));
            var image = portrait.GetComponent<Image>(); image.sprite = XunjuuMedicinalPlantCatalog.GetSprite(entry); image.preserveAspect = true; image.raycastTarget = false;
            var detail = TextBlock(card.transform, "", 19, new Vector2(.72f, .445f), new Vector2(185, 130), TextAnchor.UpperLeft, ForestDeep);
            var status = TextBlock(card.transform, "", 18, new Vector2(.5f, .105f), new Vector2(320, 31), TextAnchor.MiddleCenter, ForestGreen);
            status.fontStyle = FontStyles.Bold;
            var button = card.AddComponent<Button>(); button.targetGraphic = card.GetComponent<Image>(); button.onClick.AddListener(() => ShowPlantDetails(entry));
            plantCards[entry.Id] = new FaunaCardView { image = image, details = detail, status = status, expand = button };
            index++;
        }
        TextBlock(root.transform, "ARCHIVO BOTÁNICO  ·  6 ESPECIES  ·  OBSERVACIÓN Y MEMORIA BIOCULTURAL", 15,
            new Vector2(.5f, .018f), new Vector2(1190, 25), TextAnchor.MiddleCenter, ForestGreen);
        return root;
    }
    private void RefreshPlantCollection()
    {
        foreach (var entry in XunjuuMedicinalPlantCatalog.Entries)
        {
            if (!plantCards.TryGetValue(entry.Id, out var card)) continue;
            bool known = XunjuuMedicinalPlantCatalog.IsRegistered(entry.Id);
            card.image.color = known ? Color.white : new Color(.08f, .14f, .09f);
            card.details.text = known ? "<i>" + entry.ScientificName + "</i>\n\n" + entry.Type + "\nRegistro descubierto."
                : "Silueta por descubrir.\n\nAcércate y pulsa G para registrar esta planta.";
            card.status.text = known ? "VER FICHA" : "POR DESCUBRIR";
            card.status.color = known ? ForestGreen : TextileGrana; card.expand.interactable = known;
        }
    }
    private void OnPlantUnlocked()
    {
        if (!initialized) return;
        RefreshPlantCollection();
        if (currentPage == Page.Plants) pageTitle.text = "Herbolaria · " + XunjuuMedicinalPlantCatalog.RegisteredCount + "/" + XunjuuMedicinalPlantCatalog.Entries.Count;
    }
    public bool ShowPlantDetails(XunjuuMedicinalPlantCatalog.Entry entry)
    {
        if (entry == null || plantsRoot == null || !XunjuuMedicinalPlantCatalog.IsRegistered(entry.Id)) return false;
        ClosePlantDetails();
        foreach (var card in plantCards.Values) card.image.transform.parent.gameObject.SetActive(false);
        plantDetailRoot = Panel(plantsRoot.transform, "Detalle_Planta_" + entry.Id, Parchment);
        SetRect(plantDetailRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        TextBlock(plantDetailRoot.transform, entry.DisplayName.ToUpperInvariant(), 30, new Vector2(.48f, .94f), new Vector2(900, 55), TextAnchor.MiddleCenter, ForestGreen);
        Button(plantDetailRoot.transform, "VOLVER", new Vector2(.91f, .94f), new Vector2(145, 48), ClosePlantDetails, ForestGreen);
        var portrait = new GameObject("Retrato", typeof(RectTransform), typeof(Image)); portrait.transform.SetParent(plantDetailRoot.transform, false);
        SetRect(portrait.GetComponent<RectTransform>(), new Vector2(.025f, .25f), new Vector2(.29f, .86f));
        var image = portrait.GetComponent<Image>(); image.sprite = XunjuuMedicinalPlantCatalog.GetSprite(entry); image.preserveAspect = true; image.raycastTarget = false;
        TextBlock(plantDetailRoot.transform, entry.ScientificName, 22, new Vector2(.16f, .17f), new Vector2(330, 85), TextAnchor.MiddleCenter, ForestDeep);
        var reader = new GameObject("Lectura_Herbolaria", typeof(RectTransform), typeof(ScrollRect)); reader.transform.SetParent(plantDetailRoot.transform, false);
        SetRect(reader.GetComponent<RectTransform>(), new Vector2(.32f, .035f), new Vector2(.98f, .87f));
        var viewport = Panel(reader.transform, "Ventana", Parchment); SetRect(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one); viewport.AddComponent<RectMask2D>();
        var body = TextBlock(viewport.transform, XunjuuMedicinalPlantCatalog.Information(entry), 24, new Vector2(.5f, 1), new Vector2(750, 1200), TextAnchor.UpperLeft, ForestDeep);
        body.rectTransform.anchorMin = new Vector2(0, 1); body.rectTransform.anchorMax = Vector2.one; body.rectTransform.pivot = new Vector2(.5f, 1);
        body.rectTransform.sizeDelta = new Vector2(-24, 1200); body.rectTransform.anchoredPosition = Vector2.zero;
        body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = reader.GetComponent<ScrollRect>(); scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = body.rectTransform;
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 32; scroll.movementType = ScrollRect.MovementType.Clamped;
        Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
        statusText.text = "Ficha botánica y cultural · Desplázate para leer la información y sus fuentes";
        return true;
    }
    private void ClosePlantDetails()
    {
        if (plantDetailRoot != null) { plantDetailRoot.SetActive(false); Destroy(plantDetailRoot); plantDetailRoot = null; }
        foreach (var card in plantCards.Values) card.image.transform.parent.gameObject.SetActive(true);
    }
    public void OpenHerbalism()
    {
        if (!initialized) return;
        if (!IsOpen) Open();
        ShowPage(Page.Plants);
    }
}
