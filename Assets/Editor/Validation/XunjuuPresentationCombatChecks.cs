using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class XunjuuPresentationCombatChecks
{
    const string Output = "output/presentation-combat-validation-20261027";
    public static void RunHealthAndDeath()
    {
        XunjuuHealthVisualValidation.Run();
        XunjuuDeathScreenValidation.Run();
    }
    public static void Typography()
    {
        Check(XunjuuTypography.Regular.name == "LiberationSans", "Shared UI font missing");
        foreach (var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Check(text.font == XunjuuTypography.Regular, "Nonuniform UI font: " + text.name + " / " + text.font?.name);
        foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Check(text.font == XunjuuTypography.DistanceField, "Nonuniform TMP font: " + text.name);
        Check(XunjuuTypography.Regular.HasCharacter('ñ') && XunjuuTypography.Regular.HasCharacter('í'), "Missing Spanish characters");
        Canvas.ForceUpdateCanvases();
        foreach (var icon in UnityEngine.Object.FindObjectsByType<XunjuuMenuActionIcon>(FindObjectsSortMode.None))
        {
            Check(icon.canvasRenderer != null, "Menu icon has no CanvasRenderer");
            var mesh = icon.canvasRenderer.GetMesh();
            Check(mesh != null && mesh.vertexCount > 0, "Menu icon has no visible geometry");
        }
    }
    public static async Task HealthAndHits(Canvas canvas)
    {
        Directory.CreateDirectory(Output);
        Typography();
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        var bar = UnityEngine.Object.FindFirstObjectByType<BarraVidaFrames>();
        var feedback = player.GetComponent<XunjuuPlayerHitFeedback>();
        Check(player != null && bar != null && feedback != null, "Missing player health components");
        var image = bar.imagenBarra.transform.Find("RellenoContinuo").GetComponent<Image>();
        Check(player.GetCurrentHealth() == player.GetMaxHealth() && image.fillAmount > .999f, "Initial HP mismatch");
        var renderer = player.GetComponent<SpriteRenderer>();
        Check(renderer.sharedMaterial.HasProperty("_HitAmount") && renderer.sharedMaterial.HasProperty("_HitOffset"), "Sprite shader lacks hit reaction");
        CapturePlayer(player, Output + "/protagonista-normal.png");
        int hitCount = feedback.HitCount;
        player.SetHealth(60);
        Check(player.GetCurrentHealth() == 60 && feedback.HitCount == hitCount + 1 && feedback.FlashAmount > 0, "Damage must apply immediately and trigger reaction");
        await Task.Delay(45);
        Check(bar.GetVidaMostrada() > 60 && bar.GetVidaMostrada() < 100, "Health display must transition, not snap");
        Time.timeScale = 0;
        float pausedBar = bar.GetVidaMostrada(), pausedFlash = feedback.FlashAmount;
        var pausedOffset = feedback.VisualOffset;
        Vector3 position = player.transform.position, scale = player.transform.localScale;
        await Task.Delay(100);
        Check(bar.GetVidaMostrada() == pausedBar && feedback.FlashAmount == pausedFlash && feedback.VisualOffset == pausedOffset, "Pause failed to freeze HP/hit animation");
        Check(player.transform.position == position && player.transform.localScale == scale, "Hit reaction moved physics transform");
        CapturePlayer(player, Output + "/protagonista-impacto.png");
        await XunjuuSettlementValidation.CaptureCanvas(canvas, Output + "/vida-descenso.png");
        Time.timeScale = 1;
        await Wait(() => Mathf.Abs(bar.GetVidaMostrada() - 60) < .006f && !feedback.IsReacting, "Health/hit reaction did not settle");
        Check(feedback.FlashAmount == 0 && feedback.VisualOffset == Vector2.zero && Mathf.Abs(image.fillAmount - .6f) < .001f, "HP fill or sprite tint did not reset");
        player.SetHealth(40); await Task.Delay(45);
        float descending = bar.GetVidaMostrada(); int hits = feedback.HitCount;
        player.RestoreHealth(40); await Task.Delay(60);
        Check(bar.GetVidaMostrada() > descending && feedback.HitCount == hits, "Heal reversal failed or triggered hurt reaction");
        await Wait(() => Mathf.Abs(bar.GetVidaMostrada() - 80) < .006f, "Healing display did not settle");
        player.SetHealth(35);
        await Wait(() => Mathf.Abs(bar.GetVidaMostrada() - 35) < .006f, "Setup HP did not settle");
        var inventory = player.GetComponent<XunjuuInventory>();
        var crafting = player.GetComponent<XunjuuHerbalCrafting>();
        inventory.AddItem(XunjuuHerbalCrafting.RemedyId, "Infusión de hierbas", 1);
        Check(crafting.TryUseRemedy(), "Could not use actual remedy");
        await Task.Delay(1200);
        Check(player.GetCurrentHealth() > 35 && player.GetCurrentHealth() < 75 && bar.GetVidaMostrada() <= player.GetCurrentHealth(), "Remedy must restore health over time");
        Time.timeScale = 0;
        int pausedHealth = player.GetCurrentHealth(); pausedBar = bar.GetVidaMostrada(); await Task.Delay(150);
        Check(player.GetCurrentHealth() == pausedHealth && bar.GetVidaMostrada() == pausedBar, "Recovery progressed while paused");
        await XunjuuSettlementValidation.CaptureCanvas(canvas, Output + "/vida-recuperacion.png");
        Time.timeScale = 1;
        await Wait(() => !crafting.IsRecovering && Mathf.Abs(bar.GetVidaMostrada() - 75) < .006f, "Recovery did not finish");
        Check(player.GetCurrentHealth() == 75 && Mathf.Abs(image.fillAmount - .75f) < .001f, "Recovery final HP must be +40, proportional to bar");
        XunjuuPresentationPreferences.SetReducedMotion(true); player.SetHealth(74); await Task.Delay(45);
        Check(feedback.FlashAmount > 0 && feedback.VisualOffset == Vector2.zero, "Reduced motion should retain flash without recoil");
        XunjuuPresentationPreferences.SetReducedMotion(false);
        player.RestoreHealth(999); await Wait(() => !feedback.IsReacting && image.fillAmount > .999f, "Full heal failed to clamp");
        Typography();
        File.WriteAllText(Output + "/salud-impactos.txt", "PASS: full/partial HP, immediate real damage with continuous HUD animation, shader flash/recoil without physical transform changes, pause freezes reaction and HUD, clearing hit properties, heal reversal, actual remedy +40 over 5 seconds, pause during recovery, correct fill proportions, full-health clamp, reduced motion, shared Liberation Sans UI/TMP font.\n");
    }
    static async Task Wait(Func<bool> condition, string message)
    { var start = DateTime.UtcNow; while (!condition()) { if ((DateTime.UtcNow - start).TotalSeconds > 12) throw new Exception(message); await Task.Delay(25); } }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void CapturePlayer(PlayerController player, string path)
    {
        var original = Camera.main;
        var host = new GameObject("Camera_Player_QA", typeof(Camera)); var camera = host.GetComponent<Camera>(); camera.CopyFrom(original);
        var sprite = player.GetComponent<SpriteRenderer>();
        var all = player.GetComponentsInChildren<Transform>(true); var layers = all.Select(t => t.gameObject.layer).ToArray();
        var rt = RenderTexture.GetTemporary(640, 640, 24); var previous = RenderTexture.active;
        var texture = new Texture2D(640, 640, TextureFormat.RGB24, false);
        try
        {
            foreach (var item in all) item.gameObject.layer = 30;
            camera.cullingMask = 1 << 30; camera.targetTexture = rt; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f, .09f, .08f);
            camera.fieldOfView = 30; camera.transform.rotation = original.transform.rotation;
            camera.transform.position = sprite.bounds.center - camera.transform.forward * Mathf.Max(3, sprite.bounds.size.y * 2.7f);
            camera.Render(); RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < all.Length; i++) all[i].gameObject.layer = layers[i];
            RenderTexture.active = previous; camera.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(host);
        }
    }
}
