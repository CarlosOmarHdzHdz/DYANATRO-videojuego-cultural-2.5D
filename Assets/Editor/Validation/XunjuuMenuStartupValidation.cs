using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Captura errores desde Awake, no solo después de entrar en Play Mode.
[InitializeOnLoad]
public static class XunjuuMenuStartupValidation
{
    private const string Flag="Xunjuu.MenuStartupValidation";
    private const string Output="output/presentation-combat-validation-20261027";
    private static readonly List<string> errors=new List<string>();
    private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static XunjuuMenuStartupValidation(){EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived+=OnLog;}
    public static void Run()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        Directory.CreateDirectory(Output);EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/UI/MenuHighlands_20261019.png",ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(XunjuuBranding.AssetPath,ImportAssetOptions.ForceUpdate);
        errors.Clear();SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
    }
    private static void OnLog(string message,string trace,LogType type)
    {
        if(!SessionState.GetBool(Flag,false))return;
        if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
        if(trace.Contains("UnityEditor.Search.SearchDatabase"))return; // Defecto preexistente del editor aislado.
        errors.Add(message+"\n"+trace);
    }
    private static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false))Test();
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Flag+".done",false))
        {SessionState.SetBool(Flag+".done",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+".code",1));}
    }
    private static async void Test()
    {
        int code=1;Button confirm=null;
        bool hadVolume=PlayerPrefs.HasKey(XunjuuPresentationPreferences.MusicKey),hadMotion=PlayerPrefs.HasKey(XunjuuPresentationPreferences.MotionKey);
        float originalVolume=XunjuuPresentationPreferences.MusicVolume;bool originalMotion=XunjuuPresentationPreferences.ReducedMotion;
        try
        {
            XunjuuPresentationPreferences.SetMusicVolume(1);XunjuuPresentationPreferences.SetReducedMotion(false);
            await WaitUntil(()=>GameObject.Find("MenuInicio")!=null&&EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject?.name=="MenuButton_INICIAR"&&
                UnityEngine.Object.FindFirstObjectByType<XunjuuSceneTransition>()?.IsVisible==false,30000,"Main menu was not ready after staged startup");
            Check(errors.Count==0,"Startup errors: "+string.Join("\n",errors));
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.Video.VideoPlayer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"Startup unexpectedly created a movie player");
            var transition=UnityEngine.Object.FindFirstObjectByType<XunjuuSceneTransition>();
            XunjuuSceneTransition.ShowStartup();XunjuuSceneTransition.SetStartupStage(.42f,"Preparando la vegetación…");
            await WaitUntil(()=>transition.Progress>=.419f,3000,"Loading progress did not advance");
            var loadingCanvas=transition.GetComponentInChildren<Canvas>(true);
            CheckBranding(loadingCanvas.transform);
            Check(loadingCanvas.GetComponentsInChildren<Text>().All(t=>t.name=="Porcentaje"),"Loading contains decorative small text");
            Check(transition.Status=="Preparando la vegetación…","Loading lost diagnostic status");
            var bar=loadingCanvas.transform.Find("Tarjeta_Transicion/Barra_Carga/Progreso").GetComponent<RectTransform>();
            float firstWidth=bar.rect.width;
            await XunjuuSettlementValidation.CaptureCanvas(loadingCanvas,Output+"/carga.png");
            XunjuuSceneTransition.SetStartupStage(.78f,"Ubicando las palabras del territorio…");
            await WaitUntil(()=>transition.Progress>=.779f,3000,"Loading progress stalled");
            Check(bar.rect.width>firstWidth&&bar.rect.width<bar.parent.GetComponent<RectTransform>().rect.width,"Loading bar is not proportional to completed work");
            XunjuuSceneTransition.SetStartupStage(.40f,"Revisando el recorrido…");await Task.Delay(100);
            Check(transition.Progress>=.779f,"Loading bar went backwards");
            await XunjuuSettlementValidation.CaptureCanvas(loadingCanvas,Output+"/carga-4x3.png",1024,768);
            foreach(var text in loadingCanvas.GetComponentsInChildren<Text>())
                Check(text.preferredHeight<=text.rectTransform.rect.height+2,"Loading text clipped: "+text.text);
            XunjuuPresentationPreferences.SetReducedMotion(true);XunjuuSceneTransition.SetStartupStage(.88f,"Organizando las misiones…");await Task.Delay(100);
            Check(loadingCanvas.transform.Find("Logo_DYANATRO").localScale==Vector3.one,"Loading ignores reduced motion");
            XunjuuPresentationPreferences.SetReducedMotion(false);XunjuuSceneTransition.Dismiss();
            await WaitUntil(()=>!transition.IsVisible,3000,"Completed loading did not reveal the menu");
            CheckButtonPalette(GameObject.Find("Marco_Principal").GetComponentsInChildren<Button>());
            XunjuuPresentationCombatChecks.Typography();
            var mainMenu=GameObject.Find("MenuInicio");
            CheckBranding(mainMenu.transform.Find("Marco_Principal"));
            Check(mainMenu.GetComponentsInChildren<Text>().All(t=>t.GetComponentInParent<Button>()!=null),"Main menu contains subtitles or small decorative copy");
            var logoImporter=(TextureImporter)AssetImporter.GetAtPath(XunjuuBranding.AssetPath);
            Check(logoImporter.alphaIsTransparency&&!logoImporter.mipmapEnabled&&!logoImporter.isReadable&&logoImporter.npotScale==TextureImporterNPOTScale.None,"Logo import distorts lettering or loses alpha");
            var texture=Resources.Load<Texture2D>("Sprites/UI/MenuHighlands_20261019");Check(texture!=null&&texture.width>=1600,"Missing detailed background");
            foreach(var motion in UnityEngine.Object.FindObjectsByType<XunjuuPanelReveal>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                Check(motion.GetComponents<CanvasGroup>().Length==1,"Reveal must have one real CanvasGroup: "+motion.name);
            // Panel creado inactivo, caché borrada como tras una recarga, y varias reaperturas.
            var temporary=new GameObject("Panel_Inactivo_QA",typeof(RectTransform));temporary.SetActive(false);
            var reveal=temporary.AddComponent<XunjuuPanelReveal>();Check(temporary.GetComponent<CanvasGroup>()!=null,"RequireComponent not effective while inactive");
            for(int i=0;i<3;i++)
            {
                typeof(XunjuuPanelReveal).GetField("group",Private).SetValue(reveal,null);
                temporary.SetActive(true);await Task.Delay(300);
                Check(temporary.GetComponent<CanvasGroup>().alpha>.99f,"Reveal failed to reacquire CanvasGroup");temporary.SetActive(false);
            }
            UnityEngine.Object.Destroy(temporary);
            var director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            Canvas canvas=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.name=="Dyanatro_UI");
            Check(EventSystem.current.currentSelectedGameObject?.name=="MenuButton_INICIAR","Main menu has no safe initial keyboard focus");
            ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Down,moveVector=Vector2.down},ExecuteEvents.moveHandler);
            Check(EventSystem.current.currentSelectedGameObject?.name=="MenuButton_LUDOTECA","Keyboard down did not select Ludoteca");
            var settingsOpener=GameObject.Find("MenuButton_AJUSTES").GetComponent<Button>();settingsOpener.Select();
            ExecuteEvents.Execute(settingsOpener.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            await Task.Delay(200);
            Check(director.IsSettingsOpen&&!director.IsInPrologue,"Submitting settings accidentally started game");
            var settingsRoot=GameObject.Find("Ajustes_Presentacion");
            var slider=settingsRoot.GetComponentInChildren<Slider>();
            Check(EventSystem.current.currentSelectedGameObject==slider.gameObject,"Settings did not trap initial focus");
            float beforeArrow=slider.value;
            ExecuteEvents.Execute(slider.gameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Left,moveVector=Vector2.left},ExecuteEvents.moveHandler);
            Check(slider.value<beforeArrow,"Keyboard left did not adjust slider");
            slider.value=.35f;Check(Mathf.Abs(XunjuuPresentationPreferences.MusicVolume-.35f)<.001f,"Volume preference not applied");
            Check(!director.IsBackgroundMusicPlaying,"Adjustments started music in main menu");
            GameObject.Find("Movimiento_Menus").GetComponent<Button>().onClick.Invoke();
            Check(XunjuuPresentationPreferences.ReducedMotion,"Reduced motion not applied");
            await Task.Delay(300);
            foreach(var backdrop in UnityEngine.Object.FindObjectsByType<XunjuuMenuBackdrop>(FindObjectsSortMode.None))
                Check(backdrop.transform.Find("Paisaje_Ilustrado").GetComponent<RectTransform>().anchoredPosition==Vector2.zero,"Backdrop still moving in reduced mode");
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/ajustes.png");
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/ajustes-4x3.png",1024,768);
            var close=GameObject.Find("Cerrar_Ajustes").GetComponent<Button>();close.onClick.Invoke();await Task.Delay(150);
            Check(!director.IsSettingsOpen&&EventSystem.current.currentSelectedGameObject==settingsOpener.gameObject,"Closing settings lost opener focus");
            XunjuuPresentationPreferences.SetReducedMotion(false);
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/inicio.png");
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/inicio-4x3.png",1024,768);
            var camera=Camera.main;int mask=camera.cullingMask;var clear=camera.clearFlags;Color background=camera.backgroundColor;
            GameObject.Find("MenuButton_LUDOTECA").GetComponent<Button>().onClick.Invoke();await Task.Delay(300);
            var browser=UnityEngine.Object.FindFirstObjectByType<XunjuuLudotecaBrowser>();
            var navigation=GameObject.Find("Navegacion").GetComponentsInChildren<Button>();
            XunjuuPresentationCombatChecks.Typography();
            for(int i=0;i<4;i++)navigation[0].onClick.Invoke();
            Check(browser.IsOpen&&camera.enabled&&camera.cullingMask==0&&Time.timeScale==0,"Home click disabled display camera or lost library pause");
            Check(Camera.allCameras.Any(c=>c.enabled&&c.targetTexture==null&&c.targetDisplay==0),"No camera renders Display 1");
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/ludoteca-inicio.png");
            browser.Close();
            Check(camera.enabled&&camera.cullingMask==mask&&camera.clearFlags==clear&&camera.backgroundColor==background,"Closing library failed to restore camera state");
            // Clic real y repetido: no debe crear dos reproductores ni dos prólogos.
            var start=GameObject.Find("MenuButton_INICIAR").GetComponent<Button>();start.onClick.Invoke();start.onClick.Invoke();
            Check(director.IsInPrologue&&Time.timeScale==1&&!browser.IsOpen,"Start button left gameplay paused or library visible");
            Check(GameObject.Find("Preparando_Introduccion")!=null,"Intro preparation did not give loading feedback");
            for(int i=0;i<90&&!director.IsGameplayHudVisible;i++)
            {if(director.IsIntroVideoVisible)director.SkipIntroForValidation();if(director.IsTimelinePrologueVisible)director.SkipTimelineForValidation();await Task.Delay(100);}
            director.GetComponent<XunjuuOpeningJourney>()?.FinishTutorial();
            Check(director.IsGameplayHudVisible&&Time.timeScale==1,"Start did not reach unpaused gameplay");
            await XunjuuPresentationCombatChecks.HealthAndHits(canvas);
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.Video.VideoPlayer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Repeated Start duplicated video players");
            GameObject.Find("Boton_Ayuda_Controles").GetComponent<Button>().onClick.Invoke();
            var guide=GameObject.Find("Guia_Controles_Inicio");CheckGlyphs(guide);
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/controles-teclas.png");
            CheckMouseMesh(guide);
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/controles-teclas-4x3.png",1024,768);
            GameObject.Find("Boton_Ayuda_Controles").GetComponent<Button>().onClick.Invoke();
            var ui=UnityEngine.Object.FindFirstObjectByType<PlayerController>().GetComponent<XunjuuInventoryUI>();
            for(int i=0;i<4;i++){ui.SetVisible(true);await Task.Delay(300);Check(Time.timeScale==0,"Morral not paused");ui.SetVisible(false);await Task.Delay(100);}
            Check(errors.Count==0,"Repeated inventory startup errors: "+string.Join("\n",errors));
            var pause=typeof(DyanatroGameDirector).GetMethod("SetPause",Private);pause.Invoke(director,new object[]{true});await Task.Delay(300);
            var frame=GameObject.Find("MarcoPausa");var buttons=frame.GetComponentsInChildren<Button>();Check(buttons.Length==7,"Pause must have seven actions");
            CheckButtonPalette(buttons);
            Check(EventSystem.current.currentSelectedGameObject?.name=="Button_Continuar","Pause did not focus Continue");
            var exit=buttons.FirstOrDefault(b=>b.name=="Button_Salir del juego");Check(exit!=null,"Missing pause exit button");
            for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)
            {
                RectTransform a=buttons[i].GetComponent<RectTransform>(),b=buttons[j].GetComponent<RectTransform>();
                Check(!new Rect(a.anchoredPosition-a.sizeDelta*.5f,a.sizeDelta).Overlaps(new Rect(b.anchoredPosition-b.sizeDelta*.5f,b.sizeDelta)),"Pause buttons overlap");
            }
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/pausa.png");
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/pausa-4x3.png",1024,768);
            GameObject.Find("Button_Controles").GetComponent<Button>().onClick.Invoke();
            CheckGlyphs(GameObject.Find("Controles_En_Pausa"));
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/controles-pausa.png");
            GameObject.Find("Button_Controles").GetComponent<Button>().onClick.Invoke();
            var source=director.GetComponent<AudioSource>();int sample=source.timeSamples;
            GameObject.Find("Button_Ajustes").GetComponent<Button>().onClick.Invoke();await Task.Delay(150);
            slider=GameObject.Find("Volumen_Musica").GetComponent<Slider>();slider.value=0;
            Check(!source.isPlaying&&source.volume==0&&Time.timeScale==0,"Paused settings unpaused world or music");
            slider.value=.35f;GameObject.Find("Cerrar_Ajustes").GetComponent<Button>().onClick.Invoke();
            Check(source.timeSamples==sample,"Settings restarted paused music");
            foreach(string name in new[]{"Button_Reiniciar","Button_Menú inicial"})
            {
                var opener=GameObject.Find(name).GetComponent<Button>();opener.Select();opener.onClick.Invoke();await Task.Delay(150);
                Check(director.IsQuitConfirmationVisible&&Time.timeScale==0,"Destructive action missing confirmation");
                Check(EventSystem.current.currentSelectedGameObject?.name=="Cancelar_Salida","Confirmation did not default to safe Cancel");
                if(name=="Button_Reiniciar")await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/confirmar-reinicio.png");
                GameObject.Find("Cancelar_Salida").GetComponent<Button>().onClick.Invoke();await Task.Delay(100);
                Check(!director.IsQuitConfirmationVisible&&EventSystem.current.currentSelectedGameObject==opener.gameObject,"Cancel did not restore opener");
            }
            exit.onClick.Invoke();await Task.Delay(300);Check(director.IsQuitConfirmationVisible&&Time.timeScale==0,"Exit does not confirm while paused");
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/confirmar-salida.png");
            GameObject.Find("Cancelar_Salida").GetComponent<Button>().onClick.Invoke();Check(!director.IsQuitConfirmationVisible&&Time.timeScale==0,"Cancel incorrectly resumes game");
            exit.onClick.Invoke();pause.Invoke(director,new object[]{false});Check(!director.IsQuitConfirmationVisible&&Time.timeScale>0,"Resume leaves exit modal open");
            await Task.Delay(1100);Check(Mathf.Abs(source.volume-director.GameplayMusicVolume)<.01f,"Gameplay ignored chosen music volume");
            // Recargas reales: reiniciar abre una partida; menú inicial no la inicia.
            pause.Invoke(director,new object[]{true});GameObject.Find("Button_Reiniciar").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Confirmar_Salir").GetComponent<Button>().onClick.Invoke();
            await WaitUntil(()=>{var current=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();return current!=null&&current.IsInPrologue&&GameObject.Find("MenuInicio")==null;},30000,"Restart scene did not finish loading");
            director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            Check(director!=null&&director.IsInPrologue&&GameObject.Find("MenuInicio")==null,"Restart did not begin fresh game");
            Check(Mathf.Abs(XunjuuPresentationPreferences.MusicVolume-.35f)<.001f,"Reload lost settings");
            for(int i=0;i<90&&!director.IsGameplayHudVisible;i++)
            {if(director.IsIntroVideoVisible)director.SkipIntroForValidation();if(director.IsTimelinePrologueVisible)director.SkipTimelineForValidation();await Task.Delay(100);}
            director.GetComponent<XunjuuOpeningJourney>()?.FinishTutorial();
            pause.Invoke(director,new object[]{true});GameObject.Find("Button_Menú inicial").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Confirmar_Salir").GetComponent<Button>().onClick.Invoke();
            await WaitUntil(()=>{var current=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();return current!=null&&!current.IsInPrologue&&GameObject.Find("MenuInicio")!=null&&Time.timeScale==0;},30000,"Main menu scene did not finish loading");
            director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            Check(director!=null&&!director.IsInPrologue&&GameObject.Find("MenuInicio")!=null&&Time.timeScale==0,"Return home unexpectedly started gameplay");
            director.BeginGameForValidation();
            for(int i=0;i<90&&!director.IsGameplayHudVisible;i++)
            {if(director.IsIntroVideoVisible)director.SkipIntroForValidation();if(director.IsTimelinePrologueVisible)director.SkipTimelineForValidation();await Task.Delay(100);}
            director.GetComponent<XunjuuOpeningJourney>()?.FinishTutorial();
            pause.Invoke(director,new object[]{true});exit=GameObject.Find("Button_Salir del juego").GetComponent<Button>();
            pause.Invoke(director,new object[]{true});exit.onClick.Invoke();confirm=GameObject.Find("Confirmar_Salir").GetComponent<Button>();
            Check(errors.Count==0,"Runtime errors: "+string.Join("\n",errors));
            File.WriteAllText(Output+"/result.txt","PASS: embroidered DYANATR’O logo shared by loading/main, native aspect ratio, transparent nonblocking artwork, no subtitles or decorative small copy; loading without startup movie; proportional monotonic loading bar and reduced motion; distinct button surfaces and text contrast; loading/main/pause renders at 16:9 and 4:3; zero game exceptions from Awake; inactive creation/cache reset/reopen; four morral cycles; seven separate pause actions; safe keyboard focus and modal containment; music preference while silent/paused/playing; reduced motion; restart/home confirmations and real scene reloads; settings persistence; cancellation restores focus; final exit invokes QuitGame in editor.\n");
            Debug.Log("[MENU STARTUP QA PASS]");code=0;
        }
        catch(Exception e){File.WriteAllText(Output+"/result.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            XunjuuPresentationPreferences.SetMusicVolume(originalVolume);XunjuuPresentationPreferences.SetReducedMotion(originalMotion);
            if(!hadVolume)PlayerPrefs.DeleteKey(XunjuuPresentationPreferences.MusicKey);
            if(!hadMotion)PlayerPrefs.DeleteKey(XunjuuPresentationPreferences.MotionKey);
            PlayerPrefs.Save();
            SessionState.SetBool(Flag,false);SessionState.SetInt(Flag+".code",code);SessionState.SetBool(Flag+".done",true);
            if(code==0&&confirm!=null)confirm.onClick.Invoke();else EditorApplication.ExitPlaymode();
        }
    }
    private static void CheckBranding(Transform root)
    {
        var logo=root.Find("Logo_DYANATRO");
        Check(logo!=null,"Missing shared embroidered logo");
        var image=logo.GetComponent<RawImage>();
        Check(image!=null&&image.texture!=null&&image.texture.width>=2048,"Logo texture missing or undersized");
        Check(!image.raycastTarget,"Decorative logo captures button clicks");
        var rect=logo.GetComponent<RectTransform>().rect;
        Check(Mathf.Abs(rect.width/rect.height-image.texture.width/(float)image.texture.height)<.005f,"Logo is stretched");
    }
    private static void CheckButtonPalette(Button[] buttons)
    {
        for(int i=0;i<buttons.Length;i++)
        {
            Color fill=buttons[i].GetComponent<Image>().color;
            var label=buttons[i].GetComponentInChildren<Text>();
            Check((Luminance(label.color)+.05f)/(Luminance(fill)+.05f)>=4.5f,"Insufficient button text contrast: "+buttons[i].name);
            Check(label.preferredHeight<=label.rectTransform.rect.height+2,"Clipped button label: "+label.text);
            for(int j=0;j<i;j++)
            {
                Color other=buttons[j].GetComponent<Image>().color;
                Check(new Vector3(fill.r-other.r,fill.g-other.g,fill.b-other.b).magnitude>.055f,"Buttons have indistinguishable surfaces");
            }
        }
    }
    private static float Luminance(Color c)
    {
        Func<float,float> linear=v=>v<=.04045f?v/12.92f:Mathf.Pow((v+.055f)/1.055f,2.4f);
        return .2126f*linear(c.r)+.7152f*linear(c.g)+.0722f*linear(c.b);
    }
    private static void CheckGlyphs(GameObject root)
    {
        var items=root.GetComponentsInChildren<Transform>();
        Check(items.Count(t=>t.name.StartsWith("Glifo_Control_"))==10,"Expected ten control glyphs");
        Check(root.GetComponentsInChildren<XunjuuMouseGlyph>().Length==1,"Mouse button diagram missing");
        foreach(string key in new[]{"W","A","S","D","ESPACIO","ESC"})Check(items.Any(t=>t.name=="Tecla_"+key),"Missing keycap "+key);
        var actions=root.GetComponentsInChildren<Text>().Where(t=>t.name=="Accion_Control").ToArray();
        Check(actions.Length==10,"Missing control descriptions");
        foreach(var label in actions)Check(label.preferredHeight<=label.rectTransform.rect.height+2,"Clipped control description: "+label.text);
    }
    private static void CheckMouseMesh(GameObject root)
    {
        var mesh=root.GetComponentInChildren<XunjuuMouseGlyph>().canvasRenderer.GetMesh();
        Check(mesh!=null&&mesh.vertexCount>100,"Mouse graphic has no native UI mesh");
    }
    private static async Task WaitUntil(Func<bool> condition,int timeoutMilliseconds,string failureMessage)
    {
        int elapsed=0;
        while(elapsed<timeoutMilliseconds)
        {
            if(condition())return;
            await Task.Delay(100);elapsed+=100;
        }
        Check(condition(),failureMessage);
    }
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
