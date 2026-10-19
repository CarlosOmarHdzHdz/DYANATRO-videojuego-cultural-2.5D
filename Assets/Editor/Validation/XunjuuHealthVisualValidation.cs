using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class XunjuuHealthVisualValidation
{
    private const string Flag = "Temp/ValidateMazahuaHealth.flag";
    static XunjuuHealthVisualValidation() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (File.Exists("Temp/VerifyHealthInGame.flag") && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            if (!EditorApplication.isPlaying)
            {
                SessionState.SetBool("HealthGameStarted", false);
                SessionState.SetBool("HealthTimelineSkipped", false);
                SessionState.SetFloat("HealthPreviewStart", (float)EditorApplication.timeSinceStartup);
                EditorApplication.isPlaying = true;
                return;
            }
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat("HealthPreviewStart", 0) < 12) return;
            if (!SessionState.GetBool("HealthGameStarted", false))
            {
                var director = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
                if (director != null) director.BeginGameForValidation();
                SessionState.SetBool("HealthGameStarted", true);
                SessionState.SetFloat("HealthPreviewStart", (float)EditorApplication.timeSinceStartup);
                return;
            }
            var actual = UnityEngine.Object.FindFirstObjectByType<BarraVidaFrames>();
            var runningDirector = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            if (runningDirector != null && runningDirector.IsIntroVideoVisible)
            {
                runningDirector.SkipIntroForValidation();
                SessionState.SetFloat("HealthPreviewStart", (float)EditorApplication.timeSinceStartup);
                return;
            }
            if (SessionState.GetBool("HealthTimelineSkipped", false) == false && runningDirector != null)
            {
                runningDirector.SkipTimelineForValidation();
                SessionState.SetBool("HealthTimelineSkipped", true);
                SessionState.SetFloat("HealthPreviewStart", (float)EditorApplication.timeSinceStartup);
                return;
            }
            bool valid = actual != null && actual.imagenBarra != null && actual.imagenBarra.sprite != null
                && actual.imagenBarra.sprite.name == "MazahuaHealth_Continuous";
            File.WriteAllText("output/health-validation/game.txt", (valid ? "PASS" : "FAIL") + ": runtime health bar connected. " + DateTime.Now.ToString("O"));
            ScreenCapture.CaptureScreenshot("output/health-validation/game.png");
            File.Delete("Temp/VerifyHealthInGame.flag");
            SessionState.SetBool("HealthGameStarted", false);
            SessionState.SetBool("HealthTimelineSkipped", false);
            SessionState.SetFloat("HealthPreviewStop", (float)EditorApplication.timeSinceStartup + 4);
        }
        float stop = SessionState.GetFloat("HealthPreviewStop", 0);
        if (stop > 0 && EditorApplication.timeSinceStartup > stop)
        {
            SessionState.SetFloat("HealthPreviewStop", 0);
            EditorApplication.isPlaying = false;
        }
        if (!File.Exists(Flag) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Flag);
        Run();
    }

    [MenuItem("Tools/Xunjuu/Validar barra de vida mazahua")]
    public static void Run()
    {
        string folder = "output/health-validation";
        Directory.CreateDirectory(folder);
        GameObject root = null;
        try
        {
            root = new GameObject("HealthValidation", typeof(RectTransform));
            root.hideFlags = HideFlags.HideAndDontSave;
            var imageObject = new GameObject("Relleno", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(root.transform, false);
            var textObject = new GameObject("VidaTexto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(root.transform, false);
            BarraVidaFrames bar = root.AddComponent<BarraVidaFrames>();
            bar.imagenBarra = imageObject.GetComponent<Image>();
            bar.textoVida = textObject.GetComponent<Text>();
            bar.EstablecerMaxima(100);
            bar.EstablecerVida(100);
            foreach (int health in new[] { 100, 75, 50, 25, 1, 0, 65, 100 })
            {
                float before = bar.GetVidaMostrada();
                bar.EstablecerVida(health);
                if (bar.GetVidaActual() != health) throw new Exception("Health mismatch");
                if (bar.imagenBarra.sprite == null) throw new Exception("Visual not installed");
                if (bar.imagenBarra.sprite.name != "MazahuaHealth_Continuous") throw new Exception("Discrete frame still active");
                bar.ActualizarTransicion(.04f);
                float intermediate = bar.GetVidaMostrada();
                if (before != health && !(intermediate > Mathf.Min(before, health) && intermediate < Mathf.Max(before, health)))
                    throw new Exception("Transition snapped or moved backwards at " + health);
                for (int i = 0; i < 120; i++)
                {
                    float previous = bar.GetVidaMostrada();
                    bar.ActualizarTransicion(1f / 60f);
                    float next = bar.GetVidaMostrada();
                    if (health < before && (next > previous || next < health)) throw new Exception("Damage overshoot");
                    if (health > before && (next < previous || next > health)) throw new Exception("Heal overshoot");
                }
                Image fill = bar.imagenBarra.transform.Find("RellenoContinuo").GetComponent<Image>();
                if (Mathf.Abs(fill.fillAmount - health / 100f) > .0001f) throw new Exception("Incorrect continuous fill at " + health);
                float paused = bar.GetVidaMostrada(); bar.ActualizarTransicion(0);
                if (bar.GetVidaMostrada() != paused) throw new Exception("Animation advanced while paused");
                Render(bar.imagenBarra, folder + "/health-" + health + ".png");
            }
            bar.EstablecerMaxima(200);
            bar.EstablecerVida(100);
            if (Mathf.Abs(bar.imagenBarra.transform.Find("RellenoContinuo").GetComponent<Image>().fillAmount - .5f) > .001f
                || bar.textoVida.text != "VIDA  100 / 200") throw new Exception("Max-health mismatch");
            bar.EstablecerVida(999);
            if (bar.GetVidaActual() != 200) throw new Exception("Heal clamp failed");
            bar.EstablecerVida(-100);
            if (bar.GetVidaActual() != 0) throw new Exception("Damage clamp failed");
            bar.EstablecerVida(40);
            typeof(BarraVidaFrames).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(bar, null);
            if (bar.GetVidaActual() != 40) throw new Exception("Start overwrote health");
            ValidateRecovery(root, bar, folder);
            File.WriteAllText(folder + "/result.txt", "PASS: continuous fill; smooth monotonic damage and healing; 100/75/50/25/1/0; maximum 200; clamps; pause; Start order; real PlayerController and remedy +40/5s; direction reversal.\n" + DateTime.Now.ToString("O"));
            Debug.Log("[HEALTH VALIDATION PASS] Continuous damage and healing verified.");
        }
        catch (Exception e)
        {
            File.WriteAllText(folder + "/result.txt", "FAIL: " + e);
            Debug.LogException(e);
            if (Application.isBatchMode) throw;
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void ValidateRecovery(GameObject root, BarraVidaFrames bar, string folder)
    {
        var host = new GameObject("Player_Health_QA"); host.transform.SetParent(root.transform);
        var player = host.AddComponent<PlayerController>(); player.enabled = false;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(PlayerController).GetMethod("Awake", flags).Invoke(player, null);
        typeof(PlayerController).GetField("barraVida", flags).SetValue(player, bar);
        bar.EstablecerMaxima(100); player.SetHealth(35);
        for (int i = 0; i < 120; i++) bar.ActualizarTransicion(1f / 60f);
        var inventory = host.AddComponent<XunjuuInventory>();
        var crafting = host.AddComponent<XunjuuHerbalCrafting>(); crafting.enabled = false;
        typeof(XunjuuHerbalCrafting).GetMethod("Awake", flags).Invoke(crafting, null);
        inventory.AddItem(XunjuuHerbalCrafting.RemedyId, "Preparado", 1);
        if (!crafting.TryUseRemedy()) throw new Exception("Could not start recovery");
        float first = bar.GetVidaMostrada();
        for (int i = 0; i < 300; i++)
        {
            crafting.AdvanceRecovery(1f / 60f); bar.ActualizarTransicion(1f / 60f);
            if (bar.GetVidaMostrada() > bar.GetVidaActual() || bar.GetVidaMostrada() < first) throw new Exception("Recovery HUD jumped");
            if (i == 149) Render(bar.imagenBarra, folder + "/recovery-halfway.png");
        }
        crafting.AdvanceRecovery(.001f);
        if (player.GetCurrentHealth() != 75) throw new Exception("Recovery amount changed");
        player.SetHealth(45); bar.ActualizarTransicion(.04f);
        float damaged = bar.GetVidaMostrada(); player.RestoreHealth(40); bar.ActualizarTransicion(.04f);
        if (bar.GetVidaMostrada() <= damaged) throw new Exception("Healing continued in the damage direction");
    }

    private static void Render(Image image, string path)
    {
        RenderTexture rt = RenderTexture.GetTemporary(300, 164, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        Texture2D output = new Texture2D(300, 164, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0.12f, 0.16f, 0.18f, 1));
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, 300, 164, 0);
            Rect r = image.sprite.rect;
            Texture2D sheet = image.sprite.texture;
            Rect uv = new Rect(r.x / sheet.width, r.y / sheet.height, r.width / sheet.width, r.height / sheet.height);
            Graphics.DrawTexture(new Rect(0, 0, 300, 164), sheet, uv, 0, 0, 0, 0, Color.white, image.material);
            Image fill = image.transform.Find("RellenoContinuo").GetComponent<Image>();
            float scale = 300f / image.sprite.rect.width;
            Vector2 position = fill.rectTransform.anchoredPosition * scale;
            Vector2 size = fill.rectTransform.sizeDelta * scale;
            if (fill.fillAmount > 0f)
                Graphics.DrawTexture(new Rect(position.x, -position.y, size.x * fill.fillAmount, size.y),
                    fill.sprite.texture, new Rect(0, 0, fill.fillAmount, 1), 0, 0, 0, 0, Color.white, fill.material);
            GL.PopMatrix();
            output.ReadPixels(new Rect(0, 0, 300, 164), 0, 0);
            output.Apply();
            File.WriteAllBytes(path, output.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(output);
        }
    }
}
