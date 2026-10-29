using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class XunjuuDeathScreenValidation
{
    const string Flag = "Xunjuu.DeathScreenValidation";
    const string Output = "output/death-screen-papel-picado-20261027";
    static readonly List<string> errors = new List<string>();
    static XunjuuDeathScreenValidation()
    {
        EditorApplication.playModeStateChanged += Changed;
        Application.logMessageReceived += OnLog;
    }
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/UI/DeathOfrenda_20261027.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/" + XunjuuDeathScreenStyle.TitlePath + ".png", ImportAssetOptions.ForceUpdate);
        errors.Clear(); SessionState.SetBool(Flag, true); EditorApplication.EnterPlaymode();
    }
    static void OnLog(string message, string trace, LogType type)
    {
        if (!SessionState.GetBool(Flag, false) || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        if (trace.Contains("UnityEditor.Search.SearchDatabase")) return;
        errors.Add(message + "\n" + trace);
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Flag, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) Test();
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Flag + ".done", false))
        {
            SessionState.SetBool(Flag, false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Flag + ".code", 1));
        }
    }
    static async void Test()
    {
        int code = 1;
        bool hadMotion = PlayerPrefs.HasKey(XunjuuPresentationPreferences.MotionKey);
        bool motion = XunjuuPresentationPreferences.ReducedMotion;
        try
        {
            await Wait(() => GameObject.Find("MenuInicio") != null && UnityEngine.Object.FindFirstObjectByType<XunjuuSceneTransition>()?.IsVisible == false, 45000, "Startup incomplete");
            XunjuuPresentationPreferences.SetReducedMotion(false);
            var menu = GameObject.Find("MenuInicio");
            Check(menu.GetComponentsInChildren<Image>().All(i => i.sprite == null), "Decorative player sprite remains in menu");
            Check(menu.GetComponentsInChildren<Button>().Length == 4, "Menu actions changed");
            var canvas = GameObject.Find("Dyanatro_UI").GetComponent<Canvas>();
            await XunjuuSettlementValidation.CaptureCanvas(canvas, Output + "/inicio-sin-personaje.png");
            var director = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            director.BeginGameForValidation(); await ReachGameplay();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            Check(player != null && player.GetCurrentHealth() > 0, "Playable hero removed");
            player.SetHealth(60); await Task.Delay(30);
            Check(player.GetComponent<XunjuuPlayerHitFeedback>().IsReacting, "Hit reaction did not start before fatal blow");
            player.GetComponent<XunjuuInventoryUI>().SetVisible(true);
            player.SetHealth(0);
            await Wait(() => GameObject.Find("PantallaMuerte") != null && Time.timeScale == 0, 6000, "Death overlay missing");
            await Task.Delay(350);
            var death = GameObject.Find("PantallaMuerte");
            Check(!director.IsGameplayHudVisible && !director.IsInventoryOpen && !player.enabled && !director.IsBackgroundMusicPlaying, "Death failed to close inventory, hide HUD, disable control or pause music");
            Check(player.GetComponent<XunjuuPlayerHitFeedback>().FlashAmount == 0, "Fatal hit left the sprite tinted");
            XunjuuPresentationCombatChecks.Typography();
            Check(death.transform.GetSiblingIndex() == death.transform.parent.childCount - 1, "Death screen is behind another panel");
            var art = death.transform.Find("Ofrenda_Dia_De_Muertos").GetComponent<RawImage>();
            Check(art.texture != null && art.texture.name == "DeathOfrenda_20261027" && !art.raycastTarget, "Missing or blocking Dia de Muertos art");
            Check(death.GetComponentsInChildren<Button>().Length == 2, "Death requires exactly two actions");
            var frame = death.transform.Find("Marco_Reintentar");
            Check(frame.GetComponent<Image>().color.a <= .65f && !frame.GetComponent<Outline>().enabled, "Frame is opaque or duplicates its fill");
            var titleArt = frame.Find("Titulo_Papel_Picado").GetComponent<RawImage>();
            Check(titleArt != null && titleArt.texture != null && !titleArt.raycastTarget, "Missing themed title artwork");
            Check(frame.GetComponentsInChildren<XunjuuPapelPicadoTrim>().Length == 3, "Missing floral frame or button borders");
            foreach (var button in frame.GetComponentsInChildren<Button>())
            {
                var label = button.GetComponentInChildren<Text>();
                Check(label.font == XunjuuTypography.Regular, "Missing shared interface font");
                Check(label.preferredWidth < label.rectTransform.rect.width, "Button label wraps");
            }
            Check(EventSystem.current.currentSelectedGameObject?.name == "Button_Reintentar", "Retry lacks default keyboard focus");
            ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right, moveVector = Vector2.right }, ExecuteEvents.moveHandler);
            Check(EventSystem.current.currentSelectedGameObject?.name == "Button_Menu inicial", "Keyboard navigation failed");
            foreach (var text in death.GetComponentsInChildren<Text>())
                Check(text.preferredHeight <= text.rectTransform.rect.height + 2, "Clipped death text: " + text.text);
            await XunjuuSettlementValidation.CaptureCanvas(canvas, Output + "/muerte-dia-de-muertos.png");
            await XunjuuSettlementValidation.CaptureCanvas(canvas, Output + "/muerte-4x3.png", 1024, 768);
            int first = director.GetInstanceID();
            var retry = GameObject.Find("Button_Reintentar").GetComponent<Button>();
            retry.onClick.Invoke(); retry.onClick.Invoke();
            await Wait(() => { var d = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>(); return d != null && d.GetInstanceID() != first && d.IsInPrologue; }, 60000, "Retry failed to reload");
            await ReachGameplay();
            director = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            Check(!player.IsDead() && player.GetCurrentHealth() > 0 && Time.timeScale == 1, "Retry did not restore playable state");
            XunjuuPresentationPreferences.SetReducedMotion(true);
            player.SetHealth(0);
            await Wait(() => GameObject.Find("PantallaMuerte") != null && Time.timeScale == 0, 6000, "Second death failed");
            Check(GameObject.Find("PantallaMuerte").GetComponent<CanvasGroup>().alpha == 1, "Reduced motion still fades death UI");
            int second = director.GetInstanceID();
            var home = GameObject.Find("Button_Menu inicial").GetComponent<Button>();
            home.Select(); ExecuteEvents.Execute(home.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            await Wait(() => { var d = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>(); return d != null && d.GetInstanceID() != second && !d.IsInPrologue && GameObject.Find("MenuInicio") != null && Time.timeScale == 0 && UnityEngine.Object.FindFirstObjectByType<XunjuuSceneTransition>()?.IsVisible == false; }, 60000, "Return to menu failed");
            Check(EventSystem.current.currentSelectedGameObject?.name == "MenuButton_INICIAR", "Menu lost focus after defeat");
            Check(errors.Count == 0, "Runtime errors: " + string.Join("\n", errors));
            File.WriteAllText(Output + "/result.txt", "PASS: shared Liberation Sans font, original embroidered logo and papel picado title preserved; translucent 60% frame; floral borders; unclipped labels; playable hero preserved; fatal hit clears reaction tint, closes inventory, hides HUD, pauses music/world and disables player; keyboard focus/navigation; 16:9 and 4:3 captures; repeated Retry guarded, reload restores health/control; reduced motion; keyboard submit returns to main menu; zero game errors.\n");
            Debug.Log("[DEATH SCREEN QA PASS]"); code = 0;
        }
        catch (Exception e)
        {
            var d = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            var transition = UnityEngine.Object.FindFirstObjectByType<XunjuuSceneTransition>();
            string state = "\nState: time=" + Time.timeScale + ", director=" + (d != null ? d.GetInstanceID().ToString() : "missing")
                + ", prologue=" + (d != null && d.IsInPrologue) + ", menu=" + (GameObject.Find("MenuInicio") != null)
                + ", transition=" + (transition != null && transition.IsVisible) + ", status=" + transition?.Status;
            File.WriteAllText(Output + "/result.txt", "FAIL: " + e + state);
            var canvas = GameObject.Find("Dyanatro_UI")?.GetComponent<Canvas>();
            if (canvas != null) await XunjuuSettlementValidation.CaptureCanvas(canvas, Output + "/failure-state.png");
            Debug.LogError(state); Debug.LogException(e);
        }
        finally
        {
            XunjuuPresentationPreferences.SetReducedMotion(motion);
            if (!hadMotion) PlayerPrefs.DeleteKey(XunjuuPresentationPreferences.MotionKey);
            PlayerPrefs.Save();
            SessionState.SetInt(Flag + ".code", code); SessionState.SetBool(Flag + ".done", true); EditorApplication.ExitPlaymode();
        }
    }
    static async Task ReachGameplay()
    {
        await Wait(() =>
        {
            var d = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            if (d == null) return false;
            if (d.IsIntroVideoVisible) d.SkipIntroForValidation();
            if (d.IsTimelinePrologueVisible) d.SkipTimelineForValidation();
            return d.IsGameplayHudVisible;
        }, 30000, "Gameplay did not start");
        UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>().GetComponent<XunjuuOpeningJourney>()?.FinishTutorial();
        await Task.Delay(200);
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static async Task Wait(Func<bool> condition, int milliseconds, string message)
    {
        var start = DateTime.UtcNow;
        while (!condition()) { if ((DateTime.UtcNow - start).TotalMilliseconds > milliseconds) throw new Exception(message); await Task.Delay(100); }
    }
}
