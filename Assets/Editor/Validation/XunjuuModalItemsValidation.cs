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
public static class XunjuuModalItemsValidation
{
    const string Flag="Xunjuu.ModalItemsValidation", Output="output/modal-items-validation-20261028";
    static readonly List<string> errors=new List<string>();
    static XunjuuModalItemsValidation(){EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived+=Log;}
    public static void Run()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        Directory.CreateDirectory(Output);EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        errors.Clear();SessionState.SetBool(Flag,true);SessionState.SetBool(Flag+".done",false);EditorApplication.EnterPlaymode();
    }
    static void Log(string text,string trace,LogType type)
    {
        if(!SessionState.GetBool(Flag,false)||trace.Contains("UnityEditor.Search.SearchDatabase"))return;
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text+"\n"+trace);
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Flag,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode)Test();
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Flag+".done",false))
        {SessionState.SetBool(Flag,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+".code",1));}
    }
    static async void Test()
    {
        int code=1;float volumeBefore=XunjuuPresentationPreferences.MusicVolume;bool motionBefore=XunjuuPresentationPreferences.ReducedMotion;
        bool volumeExisted=PlayerPrefs.HasKey(XunjuuPresentationPreferences.MusicKey),motionExisted=PlayerPrefs.HasKey(XunjuuPresentationPreferences.MotionKey);
        try
        {
            await Wait(()=>GameObject.Find("MenuInicio")!=null&&UnityEngine.Object.FindFirstObjectByType<XunjuuSceneTransition>()?.IsVisible==false,"Startup incomplete");
            var director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();var canvas=GameObject.Find("Dyanatro_UI").GetComponent<Canvas>();
            XunjuuPresentationPreferences.SetReducedMotion(false);
            var opener=GameObject.Find("MenuButton_AJUSTES").GetComponent<Button>();opener.Select();opener.onClick.Invoke();await Task.Delay(350);
            var settings=GameObject.Find("Ajustes_Presentacion");var slider=settings.GetComponentInChildren<Slider>();slider.value=.55f;
            Check(settings.GetComponentsInChildren<Text>().First(t=>t.name=="Porcentaje").text=="55 %","Volume feedback mismatch");
            Check(EventSystem.current.currentSelectedGameObject==slider.gameObject,"Slider did not get initial focus");
            GameObject.Find("Bajar_Volumen").GetComponent<Button>().onClick.Invoke();Check(Mathf.Abs(slider.value-.45f)<.001f,"Volume minus failed");
            GameObject.Find("Subir_Volumen").GetComponent<Button>().onClick.Invoke();Check(Mathf.Abs(slider.value-.55f)<.001f,"Volume plus failed");
            slider.value=0;GameObject.Find("Bajar_Volumen").GetComponent<Button>().onClick.Invoke();Check(slider.value==0,"Volume below zero");
            slider.value=1;GameObject.Find("Subir_Volumen").GetComponent<Button>().onClick.Invoke();Check(slider.value==1,"Volume above one");slider.value=.55f;
            GameObject.Find("Movimiento_Menus").GetComponent<Button>().onClick.Invoke();await Task.Delay(150);
            Check(XunjuuPresentationPreferences.ReducedMotion&&GameObject.Find("Interruptor").transform.Find("Indicador").GetComponent<RectTransform>().anchoredPosition.x==13,"Motion switch state mismatch");
            TextFits(settings.transform);
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/ajustes.png");await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/ajustes-4x3.png",1024,768);
            GameObject.Find("Cerrar_Ajustes_Icono").GetComponent<Button>().onClick.Invoke();await Task.Delay(100);
            Check(!director.IsSettingsOpen&&EventSystem.current.currentSelectedGameObject==opener.gameObject,"Close lost opener focus");
            opener.onClick.Invoke();await Task.Delay(150);Check(Mathf.Abs(GameObject.Find("Volumen_Musica").GetComponent<Slider>().value-.55f)<.001f,"Settings did not persist");
            GameObject.Find("Cerrar_Ajustes").GetComponent<Button>().onClick.Invoke();XunjuuPresentationPreferences.SetReducedMotion(false);
            director.BeginGameForValidation();
            await Wait(()=>{if(director.IsIntroVideoVisible)director.SkipIntroForValidation();if(director.IsTimelinePrologueVisible)director.SkipTimelineForValidation();return director.IsGameplayHudVisible;},"Gameplay incomplete");
            director.GetComponent<XunjuuOpeningJourney>()?.FinishTutorial();await Task.Delay(200);
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();var inventory=player.GetComponent<XunjuuInventory>();var ui=player.GetComponent<XunjuuInventoryUI>();
            foreach(var id in new[]{"manzanilla","hierbabuena","arnica"})
            {var entry=XunjuuMedicinalPlantCatalog.Find(id);inventory.AddItem(id,entry.DisplayName,id=="manzanilla"?3:id=="hierbabuena"?2:1,XunjuuMedicinalPlantCatalog.GetSprite(entry));}
            XunjuuItemCatalog.Remember("palabra_validacion","jñiñi","pueblo");inventory.AddItem("palabra_validacion","Flor de memoria",1,XunjuuPresentationSprites.Load("Sprites/Collectibles/FlorMemoria_v2",1.3f,new Vector2(.5f,0)));
            inventory.AddItem(XunjuuHerbalCrafting.RemedyId,"Infusión de hierbas",1,XunjuuMedicinalPlantCatalog.RemedySprite);inventory.AddItem("madera_validacion","Madera",7);
            ui.SetVisible(true);await Task.Delay(350);
            var panel=GameObject.Find("Panel_Inventario");var cells=panel.transform.Find("Cuadricula_Objetos/Objetos_Con_Sprites");
            int[] ids=cells.Cast<Transform>().Select(t=>t.GetInstanceID()).ToArray();
            Check(ids.Length==inventory.Capacity&&Time.timeScale==0,"Inventory capacity or pause mismatch");
            var first=cells.GetChild(0).GetComponent<Button>();first.onClick.Invoke();cells.GetChild(2).GetComponent<Button>().onClick.Invoke();first.onClick.Invoke();
            Check(ids.SequenceEqual(cells.Cast<Transform>().Select(t=>t.GetInstanceID())),"Selecting items rebuilt the grid");
            Check(cells.GetChild(0).Find("Nombre_Objeto").GetComponent<Text>().text=="Manzanilla","Item name absent");
            Check(cells.GetChild(0).Find("Etiqueta_Cantidad/Cantidad").GetComponent<Text>().text=="×3","Item quantity absent");
            Check(cells.GetChild(0).Find("Icono").GetComponent<Image>().sprite!=null,"Plant icon absent");
            var inventoryCanvas=ui.GetComponentInChildren<Canvas>();
            TextFits(panel.transform);
            await XunjuuSettlementValidation.CaptureCanvas(inventoryCanvas,Output+"/items-morral.png");await XunjuuSettlementValidation.CaptureCanvas(inventoryCanvas,Output+"/items-morral-4x3.png",1024,768);
            GameObject.Find("Filtro_2").GetComponent<Button>().onClick.Invoke();
            Check(cells.Cast<Transform>().Where(t=>t.gameObject.activeSelf&&t.GetComponent<Button>().interactable).All(t=>t.Find("Tipo").GetComponent<Text>().text=="PLANTA"),"Plant filter includes other kinds");
            await XunjuuSettlementValidation.CaptureCanvas(inventoryCanvas,Output+"/items-plantas.png");
            GameObject.Find("Filtro_0").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Preparar").GetComponent<Button>().onClick.Invoke();
            Check(inventory.GetAmount("manzanilla")==1&&inventory.GetAmount("hierbabuena")==1&&inventory.GetAmount(XunjuuHerbalCrafting.RemedyId)==2,"Crafting recipe changed");
            Check(!GameObject.Find("Preparar").GetComponent<Button>().interactable,"Crafting should disable without ingredients");
            Check(inventory.GetAmount("palabra_validacion")==1,"Crafting consumed memory flower");
            player.SetHealth(60);await Task.Delay(150);
            Check(GameObject.Find("Usar").GetComponent<Button>().interactable,"Remedy cannot be used with missing HP");GameObject.Find("Usar").GetComponent<Button>().onClick.Invoke();
            Check(inventory.GetAmount(XunjuuHerbalCrafting.RemedyId)==1&&player.GetCurrentHealth()==60,"Using remedy must queue gradual recovery while paused");
            ui.SetVisible(false);Check(Time.timeScale==1,"Closing inventory failed to resume");
            await Wait(()=>!player.GetComponent<XunjuuHerbalCrafting>().IsRecovering,"Remedy failed to finish");Check(player.GetCurrentHealth()==100,"Recovery amount changed");
            XunjuuPresentationCombatChecks.Typography();Check(errors.Count==0,string.Join("\n",errors));
            File.WriteAllText(Output+"/result.txt","PASS: settings volume feedback, +/- controls and clamps, live motion preview/switch, close focus and persistence; 16:9/4:3 renders; shared font; item names/icons/categories/counts; stable pooled cards across selection, filters, real recipe and consumption, memory flowers preserved, paused recovery queue and +40 HP after resume; zero game errors.\n");
            Debug.Log("[MODAL ITEMS QA PASS]");code=0;
        }
        catch(Exception e){File.WriteAllText(Output+"/result.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            XunjuuPresentationPreferences.SetMusicVolume(volumeBefore);XunjuuPresentationPreferences.SetReducedMotion(motionBefore);
            if(!volumeExisted)PlayerPrefs.DeleteKey(XunjuuPresentationPreferences.MusicKey);if(!motionExisted)PlayerPrefs.DeleteKey(XunjuuPresentationPreferences.MotionKey);PlayerPrefs.Save();
            SessionState.SetInt(Flag+".code",code);SessionState.SetBool(Flag+".done",true);EditorApplication.ExitPlaymode();
        }
    }
    static void TextFits(Transform root)
    {Canvas.ForceUpdateCanvases();foreach(var text in root.GetComponentsInChildren<Text>())Check(text.preferredHeight<=text.rectTransform.rect.height+2,"Clipped text: "+text.name+" / "+text.text);}
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static async Task Wait(Func<bool> condition,string message)
    {var start=DateTime.UtcNow;while(!condition()){if((DateTime.UtcNow-start).TotalSeconds>45)throw new Exception(message);await Task.Delay(100);}}
}
