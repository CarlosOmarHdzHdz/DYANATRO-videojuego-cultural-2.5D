using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Ejecutar en una copia aislada: no guarda escenas ni modifica fichas o partidas.
[InitializeOnLoad]
public static class XunjuuMusicPauseValidation
{
    private const string Flag = "Xunjuu.MusicPauseValidation";
    private const string Output = "output/music-pause-validation";
    static XunjuuMusicPauseValidation() { EditorApplication.playModeStateChanged += Changed; }

    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(Flag, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag, false)) Test();
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Flag + ".done", false))
        {
            SessionState.SetBool(Flag + ".done", false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Flag + ".code", 1));
        }
    }

    private static async void Test()
    {
        SessionState.SetBool(Flag, false);
        int code = 1;
        try
        {
            await Task.Delay(1400);
            var director = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            Check(director != null, "Falta director");
            var source = director.GetComponent<AudioSource>();
            Check(source != null && source.clip != null, "Falta pista musical");
            Check(!source.playOnAwake && !source.isPlaying, "Musica activa en menu");
            director.OpenLudoteca();
            await Task.Delay(200);
            Check(!source.isPlaying, "Musica activa en ludoteca desde menu");
            var browser = UnityEngine.Object.FindFirstObjectByType<XunjuuLudotecaBrowser>();
            Check(browser != null && browser.IsOpen, "No se abrio ludoteca");
            browser.Close();
            await Task.Delay(150);
            Check(!source.isPlaying, "Cerrar ludoteca en menu inicio musica");

            director.BeginGameForValidation();
            bool sawIntroduction = false;
            bool sawPrologue = false;
            for (int i = 0; i < 100 && !director.IsGameplayHudVisible; i++)
            {
                Check(!source.isPlaying, "Musica superpuesta a intro o prologo");
                if (director.IsIntroVideoVisible) { sawIntroduction = true; director.SkipIntroForValidation(); }
                if (director.IsTimelinePrologueVisible) { sawPrologue = true; director.SkipTimelineForValidation(); }
                await Task.Delay(100);
            }
            Check(sawIntroduction && sawPrologue && director.IsGameplayHudVisible, "No se completo introduccion");
            await Task.Delay(1400);
            Check(source.isPlaying, "No inicio musica al comenzar recorrido");
            Check(Mathf.Abs(source.volume - director.GameplayMusicVolume) < .03f, "Volumen de partida incorrecto");

            var pause = typeof(DyanatroGameDirector).GetMethod("SetPause", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(pause != null, "Falta control de pausa");
            // Usar una posicion distinta del inicio permite detectar reinicios de la pista.
            source.time = Mathf.Min(5f, source.clip.length * .3f);
            await Task.Delay(250);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                pause.Invoke(director, new object[] { true });
                Check(!source.isPlaying && Time.timeScale == 0f, "La pausa no detuvo musica");
                int sample = source.timeSamples;
                await Task.Delay(350);
                Check(source.timeSamples == sample, "La pista avanza estando pausada");
                pause.Invoke(director, new object[] { false });
                await Task.Delay(200);
                Check(source.isPlaying && source.timeSamples >= sample, "La pista se reinicio al reanudar");
            }

            director.OpenLudoteca();
            await Task.Delay(250);
            Check(browser.IsOpen && !source.isPlaying, "Musica activa al abrir ludoteca desde partida");
            int librarySample = source.timeSamples;
            await Task.Delay(250);
            Check(source.timeSamples == librarySample, "La pista avanza en ludoteca");
            browser.Close();
            await Task.Delay(250);
            Check(source.isPlaying && source.timeSamples >= librarySample, "Cerrar ludoteca reinicio musica");

            var morral=UnityEngine.Object.FindFirstObjectByType<PlayerController>().GetComponent<XunjuuInventoryUI>();
            int morralSample=source.timeSamples;morral.SetVisible(true);await Task.Delay(250);
            Check(Time.timeScale==0&&!source.isPlaying,"El morral no pausó música y mundo");
            Check(Mathf.Abs(source.timeSamples-morralSample)<source.clip.frequency/5,"El morral alteró el punto musical");
            morral.SetVisible(false);await Task.Delay(250);
            Check(source.isPlaying&&source.timeSamples>=morralSample,"El morral reinició la pista al cerrar");

            Time.timeScale = 0f;
            await Task.Delay(200);
            Check(!source.isPlaying, "Pausa externa no detuvo musica");
            Time.timeScale = 1f;
            await Task.Delay(200);
            Check(source.isPlaying, "Pausa externa no restauro musica");

            Camera camera = Camera.main;
            Check(camera != null && XunjuuCameraFrame.HasCamera, "Falta camara compartida");
            Check(Vector3.Angle(XunjuuCameraFrame.Forward, camera.transform.forward) < .01f, "Billboards cambiaron orientacion");
            Vector3 ground = camera.transform.forward; ground.y = 0f; ground.Normalize();
            Check(Vector3.Angle(XunjuuCameraFrame.GroundForward, ground) < .01f, "Orden de sprites incorrecto");
            File.WriteAllText(Path.Combine(Output, "result.txt"),
                "PASS: silent main menu, library from menu, intro and prologue; gameplay music and fade; three pause/resume cycles retain sample position; library and external pause; shared camera axes match.\n" + DateTime.Now.ToString("o"));
            Debug.Log("[MUSIC PAUSE QA PASS]");
            code = 0;
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(Output, "result.txt"), "FAIL: " + error);
            Debug.LogException(error);
        }
        finally
        {
            Time.timeScale = 1f;
            SessionState.SetInt(Flag + ".code", code);
            SessionState.SetBool(Flag + ".done", true);
            EditorApplication.ExitPlaymode();
        }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
