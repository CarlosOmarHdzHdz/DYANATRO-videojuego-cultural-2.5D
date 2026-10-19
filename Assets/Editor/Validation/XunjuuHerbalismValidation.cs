using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Escena aislada: no guarda SampleScene ni altera registros del jugador.
[InitializeOnLoad]
public static class XunjuuHerbalismValidation
{
    private const string Pending = "Xunjuu.HerbalismValidation.Pending";
    private const string Output = "output/herbalism-validation";
    static XunjuuHerbalismValidation() { EditorApplication.playModeStateChanged += OnState; }

    [MenuItem("Tools/Xunjuu/Validar herbolaria y recetas")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Inicia la validación fuera del modo de juego.");
        if (EditorSceneManager.GetActiveScene().isDirty && !Application.isBatchMode
            && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera";
        camera.transform.SetPositionAndRotation(new Vector3(100, 12, 84), Quaternion.Euler(30, 0, 0));
        camera.GetComponent<Camera>().backgroundColor = new Color(.15f, .26f, .20f);
        var terrain = new TerrainData { heightmapResolution = 33, size = new Vector3(220, 10, 220) };
        Terrain.CreateTerrainGameObject(terrain);
        var player = new GameObject("Player_Herbolaria_QA", typeof(Rigidbody), typeof(SpriteRenderer), typeof(PlayerController));
        player.tag = "Player"; player.transform.position = new Vector3(100, .1f, 100);
        player.GetComponent<Rigidbody>().isKinematic = true; player.GetComponent<PlayerController>().enabled = false;
        player.AddComponent<XunjuuInventory>(); player.AddComponent<XunjuuHerbalCrafting>().enabled = false;
        player.AddComponent<XunjuuInventoryUI>();
        new GameObject("Herbolaria_QA", typeof(XunjuuMedicinalPlantSystem));
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    private static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) Test();
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending + ".Finished", false))
        {
            SessionState.SetBool(Pending + ".Finished", false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Pending + ".Exit", 1));
        }
    }
    private static async void Test()
    {
        var saved = new Dictionary<string, int>();
        foreach (var e in XunjuuMedicinalPlantCatalog.Entries)
        {
            string key = XunjuuMedicinalPlantCatalog.SavePrefix + e.Id;
            saved[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : -1;
            PlayerPrefs.DeleteKey(key);
        }
        SessionState.SetBool(Pending, false);
        try
        {
            await Task.Delay(1400);
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var inventory = player.GetComponent<XunjuuInventory>();
            var crafting = player.GetComponent<XunjuuHerbalCrafting>();
            var system = UnityEngine.Object.FindFirstObjectByType<XunjuuMedicinalPlantSystem>();
            Check(XunjuuMedicinalPlantCatalog.Entries.Count == 6, "El herbario debe contener seis especies.");
            Check(system.PlantCount >= 16, "Faltan plantas en el terreno.");
            foreach (var entry in XunjuuMedicinalPlantCatalog.Entries)
            {
                var sprite = XunjuuMedicinalPlantCatalog.GetSprite(entry);
                Check(sprite != null && sprite.name.EndsWith("_Realista") && sprite.texture.width >= 512, "Falta lámina detallada: " + entry.Id);
                Check(Mathf.Abs(sprite.bounds.min.y) < .001f && Mathf.Abs(sprite.bounds.size.y - 2.25f) < .001f, "Pivote o escala incorrectos: " + entry.Id);
                Check(sprite.texture.GetPixel(0, 0).a < .01f, "Fondo no transparente: " + entry.Id);
                Check(!XunjuuMedicinalPlantCatalog.Information(entry).Contains("Receta ficticia:"), "La ficha no debe incluir recetas.");
                bool spawned = false;
                foreach (var specimen in UnityEngine.Object.FindObjectsByType<XunjuuMedicinalPlant>(FindObjectsSortMode.None))
                    if (specimen.Entry.Id == entry.Id) spawned = true;
                Check(spawned, "No aparece en el escenario: " + entry.Id);
            }
            var herb = new GameObject("Planta_Prueba").AddComponent<XunjuuMedicinalPlant>();
            herb.transform.position = player.transform.position + Vector3.right; herb.Configure("manzanilla");
            Check(!XunjuuMedicinalPlantCatalog.IsRegistered("manzanilla"), "La ficha debe iniciar bloqueada.");
            Time.timeScale = 0;
            Check(!herb.TryHarvest(player, out _), "No debe recolectarse con el juego pausado.");
            Time.timeScale = 1;
            Check(herb.TryHarvest(player, out _), "Fallo de recolección.");
            Check(!herb.TryHarvest(player, out _), "Una planta se recolectó dos veces.");
            Check(inventory.GetAmount("manzanilla") == 1 && XunjuuMedicinalPlantCatalog.IsRegistered("manzanilla"), "No se guardó el ingrediente/ficha.");
            Check(!crafting.TryCraft() && inventory.GetAmount("manzanilla") == 1, "Receta incompleta consumió ingredientes.");
            inventory.AddItem("manzanilla", "Manzanilla", 1, XunjuuMedicinalPlantCatalog.GetSprite(XunjuuMedicinalPlantCatalog.Find("manzanilla")));
            inventory.AddItem("hierbabuena", "Hierbabuena", 1, XunjuuMedicinalPlantCatalog.GetSprite(XunjuuMedicinalPlantCatalog.Find("hierbabuena")));
            XunjuuMedicinalPlantCatalog.Register("hierbabuena");
            Check(crafting.TryCraft(), "No se fabricó la receta.");
            Check(inventory.GetAmount("manzanilla") == 0 && inventory.GetAmount("hierbabuena") == 0 && inventory.GetAmount(XunjuuHerbalCrafting.RemedyId) == 1, "Descuento incorrecto.");
            Check(!crafting.TryUseRemedy() && inventory.GetAmount(XunjuuHerbalCrafting.RemedyId) == 1, "Se consumió con salud completa.");
            player.SetHealth(35);
            Check(crafting.TryUseRemedy(), "No se pudo usar el preparado.");
            inventory.AddItem(XunjuuHerbalCrafting.RemedyId, "Preparado del camino", 1, XunjuuMedicinalPlantCatalog.RemedySprite);
            Check(!crafting.TryUseRemedy() && inventory.GetAmount(XunjuuHerbalCrafting.RemedyId) == 1, "Se apilaron recuperaciones.");
            crafting.AdvanceRecovery(2.5f); Check(player.GetCurrentHealth() == 55, "Recuperación parcial incorrecta.");
            crafting.AdvanceRecovery(0); Check(player.GetCurrentHealth() == 55, "La pausa avanzó la recuperación.");
            crafting.AdvanceRecovery(2.5f); Check(player.GetCurrentHealth() == 75 && !crafting.IsRecovering, "Recuperación final incorrecta.");
            player.SetHealth(95); Check(crafting.TryUseRemedy(), "Fallo cerca del máximo.");
            crafting.AdvanceRecovery(5); Check(player.GetCurrentHealth() == 100, "Se excedió la salud máxima.");
            TestCapacity();
            var host = new GameObject("Ludoteca_QA", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            host.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            host.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            var browser = host.AddComponent<XunjuuLudotecaBrowser>(); browser.Initialize(host.GetComponent<Canvas>(), null, null, null);
            browser.OpenHerbalism();
            Check(!browser.ShowPlantDetails(XunjuuMedicinalPlantCatalog.Find("sabila")), "Se abrió una ficha sin descubrir.");
            await Task.Delay(300); CaptureCanvas(host.GetComponent<Canvas>(), Output + "/herbolaria-parcial.png");
            foreach (var entry in XunjuuMedicinalPlantCatalog.Entries) XunjuuMedicinalPlantCatalog.Register(entry.Id);
            Check(XunjuuMedicinalPlantCatalog.RegisteredCount == 6, "No se desbloquearon las seis fichas.");
            foreach (var entry in XunjuuMedicinalPlantCatalog.Entries)
                Check(browser.ShowPlantDetails(entry), "No se abre la ficha: " + entry.Id);
            Check(browser.ShowPlantDetails(XunjuuMedicinalPlantCatalog.Find("manzanilla")), "No se abre la ficha registrada.");
            await Task.Delay(300); CaptureCanvas(host.GetComponent<Canvas>(), Output + "/ficha-manzanilla.png");
            browser.Close(); browser.OpenHerbalism();
            await Task.Delay(300); CaptureCanvas(host.GetComponent<Canvas>(), Output + "/ludoteca-herbolaria.png");
            browser.Close();
            inventory.AddItem("manzanilla", "Manzanilla", 2, XunjuuMedicinalPlantCatalog.GetSprite(XunjuuMedicinalPlantCatalog.Find("manzanilla")));
            inventory.AddItem("hierbabuena", "Hierbabuena", 1, XunjuuMedicinalPlantCatalog.GetSprite(XunjuuMedicinalPlantCatalog.Find("hierbabuena")));
            var ui = player.GetComponent<XunjuuInventoryUI>(); ui.SetVisible(true);
            await Task.Delay(300); CaptureCanvas(ui.GetComponentInChildren<Canvas>(), Output + "/morral-receta.png");
            player.SetHealth(0);
            Check(player.RestoreHealth(40) == 0 && !crafting.TryUseRemedy(), "La receta revivió a un personaje derrotado.");
            File.WriteAllText(Output + "/results.txt", "PASS: six catalog entries and sprites; six species spawned; six unlocked details; locked detail blocked; no recipes in ludoteca; paused harvest; unique harvest; persistent card; missing ingredients; atomic recipe; capacity rollback; full-health no-consumption; recovery stacking blocked; progressive +40/5s; max-health clamp; no resurrection; UI captures.\n");
            SessionState.SetInt(Pending + ".Exit", 0);
        }
        catch (Exception error)
        {
            Debug.LogException(error); File.WriteAllText(Output + "/results.txt", "FAIL: " + error);
            SessionState.SetInt(Pending + ".Exit", 1);
        }
        finally
        {
            Time.timeScale = 1;
            foreach (var pair in saved) { if (pair.Value < 0) PlayerPrefs.DeleteKey(pair.Key); else PlayerPrefs.SetInt(pair.Key, pair.Value); }
            PlayerPrefs.Save();
            SessionState.SetBool(Pending + ".Finished", true); EditorApplication.ExitPlaymode();
        }
    }
    private static void TestCapacity()
    {
        var go = new GameObject("Inventario_Capacidad_QA"); var inv = go.AddComponent<XunjuuInventory>();
        typeof(XunjuuInventory).GetField("maxDifferentItems", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(inv, 2);
        inv.AddItem("manzanilla", "Manzanilla", 3); inv.AddItem("hierbabuena", "Hierbabuena", 2);
        var costs = new Dictionary<string, int> { { "manzanilla", 2 }, { "hierbabuena", 1 } };
        Check(!inv.TryExchange(costs, "preparado_camino", "Preparado", null), "Se fabricó sin capacidad.");
        Check(inv.GetAmount("manzanilla") == 3 && inv.GetAmount("hierbabuena") == 2, "Se perdieron ingredientes.");
        inv.RemoveItem("manzanilla", 1); Check(inv.TryExchange(costs, "preparado_camino", "Preparado", null), "No reutilizó un espacio liberado por la receta.");
        UnityEngine.Object.Destroy(go);
    }
    private static void CaptureCanvas(Canvas canvas, string path)
    {
        var camera = Camera.main; var target = new RenderTexture(1600, 900, 24);
        var mode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldTarget = camera.targetTexture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        RenderTexture.active = previous; camera.targetTexture = oldTarget; canvas.renderMode = mode; canvas.worldCamera = oldCamera;
        target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(texture);
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
