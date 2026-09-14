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
                && actual.imagenBarra.sprite.name == "MazahuaHealth_00";
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
            foreach (int health in new[] { 100, 75, 50, 25, 1, 0, 65, 100 })
            {
                bar.EstablecerVida(health);
                if (bar.GetVidaActual() != health) throw new Exception("Health mismatch");
                if (bar.imagenBarra.sprite == null) throw new Exception("Visual not installed");
                int expectedFrame = Mathf.RoundToInt((1f - health / 100f) * 15f);
                if (bar.imagenBarra.sprite.name != "MazahuaHealth_" + expectedFrame.ToString("00"))
                    throw new Exception("Incorrect health frame at " + health);
                Render(bar.imagenBarra, folder + "/health-" + health + ".png");
            }
            bar.EstablecerMaxima(200);
            bar.EstablecerVida(100);
            if (bar.imagenBarra.sprite.name != "MazahuaHealth_08" || bar.textoVida.text != "VIDA  100 / 200") throw new Exception("Max-health mismatch");
            bar.EstablecerVida(999);
            if (bar.GetVidaActual() != 200) throw new Exception("Heal clamp failed");
            bar.EstablecerVida(-100);
            if (bar.GetVidaActual() != 0) throw new Exception("Damage clamp failed");
            bar.EstablecerVida(40);
            typeof(BarraVidaFrames).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(bar, null);
            if (bar.GetVidaActual() != 40) throw new Exception("Start overwrote health");
            File.WriteAllText(folder + "/result.txt", "PASS: 16 complete health frames; 100/75/50/25/1/0, healing, maximum 200, clamps and Start order.\n" + DateTime.Now.ToString("O"));
        }
        catch (Exception e)
        {
            File.WriteAllText(folder + "/result.txt", "FAIL: " + e);
            Debug.LogException(e);
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
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
