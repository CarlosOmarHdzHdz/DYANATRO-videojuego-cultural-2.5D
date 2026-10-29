using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class XunjuuMedicinalPlantSystem : MonoBehaviour
{
    private static XunjuuMedicinalPlantSystem instance;
    private readonly List<XunjuuMedicinalPlant> plants = new List<XunjuuMedicinalPlant>();
    private PlayerController player;
    private DyanatroGameDirector director;
    private Text prompt, notice;
    private float noticeUntil;
    private bool spawned;
    private XunjuuMedicinalPlant promptedPlant;
    public int PlantCount => plants.Count;
    private void Awake() { instance = this; director = GetComponent<DyanatroGameDirector>(); BuildHud(); }
    private void Update()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        bool playable = player != null && player.GetCurrentHealth() > 0 && Time.timeScale > 0
            && (director == null || director.IsGameplayHudVisible);
        if (playable && !spawned) { spawned = true; SpawnPlants(); }
        XunjuuMedicinalPlant nearest = playable ? FindNearest() : null;
        GameObject promptPanel = prompt.transform.parent.gameObject;
        GameObject noticePanel = notice.transform.parent.gameObject;
        bool showPrompt = nearest != null;
        bool showNotice = playable && Time.unscaledTime < noticeUntil;
        if (promptPanel.activeSelf != showPrompt) promptPanel.SetActive(showPrompt);
        if (noticePanel.activeSelf != showNotice) noticePanel.SetActive(showNotice);
        if (nearest != promptedPlant)
        {
            promptedPlant = nearest;
            if (nearest != null)
                prompt.text = "G  RECOLECTAR Y REGISTRAR  ·  " + nearest.Entry.DisplayName.ToUpperInvariant();
        }
        if (nearest == null) return;
        if (Input.GetKeyDown(KeyCode.G))
        {
            nearest.TryHarvest(player, out string message);
            if (!string.IsNullOrEmpty(message)) Notify(message);
        }
    }
    private XunjuuMedicinalPlant FindNearest()
    {
        XunjuuMedicinalPlant best = null;
        float distance = XunjuuMedicinalPlant.HarvestDistance * XunjuuMedicinalPlant.HarvestDistance;
        foreach (var plant in plants)
        {
            if (plant == null || !plant.IsAvailable) continue;
            Vector3 delta = plant.transform.position - player.transform.position; delta.y = 0;
            if (delta.sqrMagnitude < distance) { best = plant; distance = delta.sqrMagnitude; }
        }
        return best;
    }
    private void SpawnPlants()
    {
        var random = new System.Random(14026);
        var root = new GameObject("Huerto_Plantas_Medicinales");
        root.transform.SetParent(transform, true);
        XunjuuSettlementPlan.Initialize(player.transform.position);
        Bounds garden = XunjuuSettlementPlan.HerbGarden;
        // Un único huerto fijo; no se dispersa ni se regenera alrededor del jugador.
        for (int i = 0; i < 24; i++)
        {
            for (int attempt = 0; attempt < 90; attempt++)
            {
                Vector3 point = garden.center + new Vector3(((float)random.NextDouble() - .5f) * 22f, 0,
                    ((float)random.NextDouble() - .5f) * 18f);
                if (XunjuuSettlementPlan.OnPath(point, 1f)) continue;
                if (!TryGround(point, out float height)) continue;
                point.y = height + .035f;
                bool blocked = false;
                foreach (var collider in Physics.OverlapSphere(point + Vector3.up * .8f, 1f, ~0, QueryTriggerInteraction.Ignore))
                    if (!(collider is TerrainCollider) && !XunjuuSceneVisualPolicy.IsSoftDecoration(collider.transform)) { blocked = true; break; }
                if (blocked) continue;
                foreach (var plant in plants) if ((plant.transform.position - point).sqrMagnitude < 6.8f) { blocked = true; break; }
                if (blocked) continue;
                var entry = XunjuuMedicinalPlantCatalog.Entries[i % XunjuuMedicinalPlantCatalog.Entries.Count];
                var go = new GameObject("Planta_Medicinal_" + entry.Id);
                go.transform.SetParent(root.transform); go.transform.position = point;
                var harvest = go.AddComponent<XunjuuMedicinalPlant>(); harvest.Configure(entry.Id);
                plants.Add(harvest); break;
            }
        }
        var sign = new GameObject("Huerto_Medicinal_Indicacion"); sign.transform.SetParent(root.transform);
        Vector3 at = garden.center + new Vector3(garden.extents.x - 1f, 0, 1.8f);
        if (TryGround(at, out float signY)) at.y = signY;
        sign.transform.position = at;
        sign.AddComponent<XunjuuWorldLabel>().Configure("HUERTO DE PLANTAS\nExplora y registra con G", true, Vector3.up * 2.2f);
    }
    private static bool TryGround(Vector3 point, out float height)
    {
        foreach (var terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;
            Vector3 p = point - terrain.transform.position, size = terrain.terrainData.size;
            if (p.x < 1 || p.z < 1 || p.x > size.x - 1 || p.z > size.z - 1) continue;
            if (terrain.terrainData.GetSteepness(p.x / size.x, p.z / size.z) > 30) continue;
            if (terrain.terrainData.IsHole(Mathf.Clamp((int)(p.x / size.x * terrain.terrainData.holesResolution), 0, terrain.terrainData.holesResolution - 1),
                Mathf.Clamp((int)(p.z / size.z * terrain.terrainData.holesResolution), 0, terrain.terrainData.holesResolution - 1))) continue;
            height = terrain.SampleHeight(point) + terrain.transform.position.y; return true;
        }
        height = 0; return false;
    }
    private void BuildHud()
    {
        var hud = new GameObject("HUD_Herbolaria", typeof(Canvas), typeof(CanvasScaler));
        hud.transform.SetParent(transform, false);
        hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        hud.GetComponent<Canvas>().sortingOrder = 2560;
        var scaler = hud.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        GameStateManager.RegisterHud(hud);
        prompt = Message(hud.transform, "Recolectar", 154, 58);
        notice = Message(hud.transform, "Registro_Herbolaria", 270, 76);
        prompt.transform.parent.gameObject.SetActive(false); notice.transform.parent.gameObject.SetActive(false);
    }
    private static Text Message(Transform parent, string name, float y, float height)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
        rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(740, height);
        panel.GetComponent<Image>().color = new Color(.08f, .18f, .13f, .94f); panel.GetComponent<Image>().raycastTarget = false;
        var outline = panel.AddComponent<Outline>(); outline.effectColor = new Color(.74f, .59f, .27f); outline.effectDistance = new Vector2(2, -2);
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text)); go.transform.SetParent(panel.transform, false);
        var text = go.GetComponent<Text>(); text.font = XunjuuTypography.Regular; text.fontSize = 23;
        text.color = new Color(1, .95f, .81f); text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(14, 4); text.rectTransform.offsetMax = new Vector2(-14, -4);
        return text;
    }
    public static void Notify(string message)
    { if (instance != null) { instance.notice.text = message; instance.noticeUntil = Time.unscaledTime + 3.5f; } }
    private void OnDestroy() { if (instance == this) instance = null; }
}
