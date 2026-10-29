using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class XunjuuHerbalLearningValidation
{
    private const string Flag="Xunjuu.HerbalLearningValidation";
    private const string Output="output/herbal-learning-validation";
    static XunjuuHerbalLearningValidation(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        Directory.CreateDirectory(Output);EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
    }
    private static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false))Test();
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Flag+".done",false))
        {SessionState.SetBool(Flag+".done",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+".code",1));}
    }
    private static async void Test()
    {
        SessionState.SetBool(Flag,false);int code=1;
        var preferences=new Dictionary<string,(bool existed,int value)>();
        foreach(var entry in XunjuuMedicinalPlantCatalog.Entries)
        {string key=XunjuuMedicinalPlantCatalog.SavePrefix+entry.Id;preferences[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetInt(key));}
        foreach(var entry in XunjuuFaunaCatalog.Entries)
        {string key="xunjuu.fauna.capturada."+entry.Id;preferences[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetInt(key));}
        try
        {
            var director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            Check(director!=null,"Missing director");
            float until=Time.realtimeSinceStartup+50;
            while(Time.realtimeSinceStartup<until&&XunjuuSettlementPlan.Fields.Count==0)await Task.Delay(100);
            Check(XunjuuSettlementPlan.Houses.Count>=16,"Expanded village not ready");
            Canvas mainCanvas=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.name=="Dyanatro_UI");
            await XunjuuSettlementValidation.CaptureCanvas(mainCanvas,Output+"/menu.png");
            director.OpenLudoteca();
            var previewBrowser=UnityEngine.Object.FindFirstObjectByType<XunjuuLudotecaBrowser>();
            var page=typeof(XunjuuLudotecaBrowser).GetNestedType("Page",System.Reflection.BindingFlags.NonPublic);
            typeof(XunjuuLudotecaBrowser).GetMethod("ShowPage",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                .Invoke(previewBrowser,new object[]{Enum.Parse(page,"MiniGame"),false});
            foreach(string game in new[]{"StartMemory","StartWordSearch","StartQuiz"})
            {
                typeof(XunjuuLudotecaBrowser).GetMethod(game,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(previewBrowser,null);
                await Task.Delay(300);
                await XunjuuSettlementValidation.CaptureCanvas(mainCanvas,Output+"/"+game+".png");
            }
            previewBrowser.Close();
            director.BeginGameForValidation();
            for(int i=0;i<90&&!director.IsGameplayHudVisible;i++)
            {
                if(director.IsIntroVideoVisible)director.SkipIntroForValidation();
                if(director.IsTimelinePrologueVisible)director.SkipTimelineForValidation();
                await Task.Delay(100);
            }
            director.GetComponent<XunjuuOpeningJourney>()?.FinishTutorial();
            await Task.Delay(700);
            var pause=typeof(DyanatroGameDirector).GetMethod("SetPause",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var controls=typeof(DyanatroGameDirector).GetMethod("TogglePauseControls",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            pause.Invoke(director,new object[]{true});controls.Invoke(director,null);await Task.Delay(250);
            Check(!director.IsGameplayHudVisible&&GameObject.Find("Controles_En_Pausa")!=null,"Pause controls or HUD hiding failed");
            await XunjuuSettlementValidation.CaptureCanvas(mainCanvas,Output+"/pausa.png");
            pause.Invoke(director,new object[]{false});await Task.Delay(200);
            Check(director.IsGameplayHudVisible,"HUD did not resume");
            var learning=director.GetComponent<XunjuuHerbalLearningMission>();
            Check(learning!=null&&learning.Stage==XunjuuHerbalLearningMission.LearningStage.Dormant,"Learning started before flowers");
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var inventory=player.GetComponent<XunjuuInventory>();
            var progression=UnityEngine.Object.FindFirstObjectByType<XunjuuLevel2KillMission>();
            foreach(var flower in UnityEngine.Object.FindObjectsByType<MazahuaWordCollectible>(FindObjectsSortMode.None))
                flower.SendMessage("OnTriggerEnter",player.GetComponent<Collider>());
            Check(inventory.Items.Count(s=>s.Kind==XunjuuItemCatalog.ItemKind.MemoryFlower)==5,"Memory flowers not added as items");
            Check(progression.CurrentPhase==XunjuuLevel2KillMission.MissionPhase.Animals,"Flowers did not unlock animals");
            learning.Begin();
            Check(learning.Stage==XunjuuHerbalLearningMission.LearningStage.Dormant,"Herbal mission started before animals");
            var animals=UnityEngine.Object.FindObjectsByType<XunjuuAnimalCapture>(FindObjectsSortMode.None).Where(a=>a.IsAvailable).ToArray();
            Check(animals.Length==6,"Expected six capture objectives");
            foreach(var animal in animals)
            {
                animal.Capture();
                if(progression.DefeatedAnimals==3)
                {
                    await Task.Delay(400);
                    var questCanvas=progression.GetComponentsInChildren<Canvas>().First(c=>c.name=="HUD_Progresion_Nivel1");
                    ValidateQuest(questCanvas.GetComponentInChildren<XunjuuQuestCard>());
                    await XunjuuSettlementValidation.CaptureCanvas(questCanvas,Output+"/objetivo-fauna.png");
                    await XunjuuSettlementValidation.CaptureCanvas(questCanvas,Output+"/objetivo-fauna-detalle.png",800,180,1);
                    await XunjuuSettlementValidation.CaptureCanvas(questCanvas,Output+"/objetivo-fauna-4x3.png",1024,768);
                }
            }
            Check(progression.CurrentPhase==XunjuuLevel2KillMission.MissionPhase.Herbal,"Animals did not unlock herbal phase");
            Check(!progression.CompleteHerbalPreparation(),"Combat unlocked before identification and preparation");
            Check(learning.Stage==XunjuuHerbalLearningMission.LearningStage.Gather,"Animals did not unlock learning mission");
            Check(learning.GuideTarget!=null,"Garden guide missing");
            await Task.Delay(300);
            await CaptureGame(Output+"/recorrido.png");
            await XunjuuSettlementValidation.CaptureCanvas(director.GetComponentsInChildren<Canvas>().First(c=>c.name=="HUD_Aprendizaje_Plantas"),Output+"/mision-huerto.png");
            var plants=UnityEngine.Object.FindObjectsByType<XunjuuMedicinalPlant>(FindObjectsSortMode.None);
            var required=plants.Where(p=>p.Entry.Id=="manzanilla").Take(2).Concat(plants.Where(p=>p.Entry.Id=="hierbabuena").Take(1)).ToArray();
            Check(required.Length==3,"Missing recipe plants");
            foreach(var plant in required)
            {
                player.transform.position=plant.transform.position+Vector3.right;
                if(player.TryGetComponent<Rigidbody>(out var rb))rb.position=player.transform.position;
                Check(plant.TryHarvest(player,out string message),"Harvest failed: "+message);
            }
            learning.RefreshProgress();
            Check(learning.Stage==XunjuuHerbalLearningMission.LearningStage.Study,"Gather counts failed");
            var browser=UnityEngine.Object.FindFirstObjectByType<XunjuuLudotecaBrowser>();browser.OpenHerbalism();
            Check(browser.ShowPlantDetails(XunjuuMedicinalPlantCatalog.Find("manzanilla")),"Chamomile card failed");
            await Task.Delay(250);
            await XunjuuSettlementValidation.CaptureCanvas(mainCanvas,Output+"/ficha-manzanilla.png");
            Check(browser.ShowPlantDetails(XunjuuMedicinalPlantCatalog.Find("hierbabuena")),"Mint card failed");
            learning.RefreshProgress();Check(learning.CardsRead==2,"Cards not registered as viewed");
            Check(learning.Stage==XunjuuHerbalLearningMission.LearningStage.Study,"Study skipped identification question");
            browser.Close();
            Check(!learning.TryIdentifyPlant("hierbabuena"),"Wrong identification accepted");
            Check(learning.Stage==XunjuuHerbalLearningMission.LearningStage.Study,"Wrong answer advanced mission");
            await Task.Delay(200);
            await XunjuuSettlementValidation.CaptureCanvas(director.GetComponentsInChildren<Canvas>().First(c=>c.name=="HUD_Aprendizaje_Plantas"),Output+"/identificacion.png");
            Check(learning.TryIdentifyPlant("manzanilla")&&learning.IdentificationCorrect,"Correct identification rejected");
            Check(learning.Stage==XunjuuHerbalLearningMission.LearningStage.Prepare,"Identification did not unlock preparation");
            Check(progression.CurrentPhase==XunjuuLevel2KillMission.MissionPhase.Herbal,"Combat started before preparation");
            var crafting=player.GetComponent<XunjuuHerbalCrafting>();Check(crafting.TryCraft(),"Real recipe failed");
            learning.RefreshProgress();Check(learning.Stage==XunjuuHerbalLearningMission.LearningStage.Recover,"Preparation stage failed");
            Check(progression.CurrentPhase==XunjuuLevel2KillMission.MissionPhase.Enemies,"Prepared infusion did not unlock combat");
            player.SetHealth(player.GetMaxHealth());
            int remedies=inventory.GetAmount(XunjuuHerbalCrafting.RemedyId);
            Check(!crafting.TryUseRemedy(),"Full-health remedy consumed");
            Check(inventory.GetAmount(XunjuuHerbalCrafting.RemedyId)==remedies,"Full-health inventory changed");
            Check(learning.Stage==XunjuuHerbalLearningMission.LearningStage.Recover,"Full health incorrectly completed mission");
            player.GetComponent<XunjuuInventoryUI>().SetVisible(true);await Task.Delay(250);
            Check(Time.timeScale==0,"Inventory did not pause world");
            var closeButton=player.GetComponent<XunjuuInventoryUI>().GetComponentsInChildren<Button>().First(b=>b.name=="Cerrar");
            var motion=closeButton.GetComponent<XunjuuButtonMotion>();Check(motion!=null,"Missing button motion");
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            motion.OnPointerEnter(pointer);await Task.Delay(160);
            Check(motion.transform.localScale.x>1.005f&&motion.transform.localScale.x<1.014f,"Hover does not animate while paused or exceeds safe size");
            motion.OnPointerExit(pointer);await Task.Delay(200);
            Check(Mathf.Abs(motion.transform.localScale.x-1)<.001f,"Button did not return to rest");
            await XunjuuSettlementValidation.CaptureCanvas(player.GetComponentInChildren<Canvas>(),Output+"/morral.png");
            ValidateMorral(player.GetComponent<XunjuuInventoryUI>());
            await XunjuuSettlementValidation.CaptureCanvas(player.GetComponentInChildren<Canvas>(),Output+"/morral-4x3.png",1024,768);
            await XunjuuSettlementValidation.CaptureCanvas(player.GetComponentInChildren<Canvas>(),Output+"/morral-16x10.png",1280,800);
            await XunjuuSettlementValidation.CaptureCanvas(player.GetComponentInChildren<Canvas>(),Output+"/morral-ultrawide.png",2560,1080);
            player.GetComponent<XunjuuInventoryUI>().SetVisible(false);
            Check(Time.timeScale>0,"Inventory did not resume world");
            player.SetHealth(player.GetMaxHealth()-40);Check(crafting.TryUseRemedy(),"Remedy use failed");
            await Task.Delay(5700);learning.RefreshProgress();
            Check(crafting.TotalRecovered==40,"Recovery amount wrong");
            Check(inventory.Items.Count(s=>s.Kind==XunjuuItemCatalog.ItemKind.MemoryFlower)==5,"Crafting consumed memory flowers");
            Check(learning.Stage==XunjuuHerbalLearningMission.LearningStage.Completed,"Recovery did not complete learning mission");
            var notice=director.GetComponent<XunjuuMissionNotice>();
            Check(notice!=null&&notice.HasNotice&&notice.MazahuaText=="B’epji","Bilingual completion missing");
            Check(UnityEngine.Object.FindObjectsByType<XunjuuMissionNotice>(FindObjectsSortMode.None).Length==1,"Multiple mission notice controllers");
            Check(XunjuuMissionNotice.For(UnityEngine.Object.FindFirstObjectByType<XunjuuLevel2KillMission>())==notice,"Combat does not share completion notice");
            Canvas noticeCanvas=director.GetComponentsInChildren<Canvas>().First(c=>c.name=="HUD_Aviso_Unico");
            var top=noticeCanvas.GetComponentsInChildren<Text>().First(t=>t.name=="Mazahua_Arriba");
            var bottom=noticeCanvas.GetComponentsInChildren<Text>().First(t=>t.name=="Espanol_Abajo");
            Check(top.rectTransform.anchoredPosition.y>bottom.rectTransform.anchoredPosition.y,"Language order wrong");
            Check(top.transform.parent.GetComponent<RectTransform>().anchoredPosition==Vector2.zero,"Notice not centered");
            Check(GameObject.Find("Aviso_Cambio_Mision")==null,"Legacy duplicate banner exists");
            await Task.Delay(300);await XunjuuSettlementValidation.CaptureCanvas(noticeCanvas,Output+"/aviso-unico.png");
            await CaptureGame(Output+"/aviso-en-juego.png");
            File.WriteAllText(Output+"/result.txt","PASS: five collectible memory items -> six captured animals -> herbal mission -> identification and real crafting -> combat; recipe consumes only 2 chamomile + 1 mint; memory flowers preserved; two visible botanical cards; wrong identification rejected; inventory pauses/resumes; no full-health consumption; 40 gradual health recovery; one centered bilingual panel; UI captures.\n"+DateTime.Now.ToString("o"));
            Debug.Log("[HERBAL LEARNING QA PASS]");code=0;
        }
        catch(Exception e){File.WriteAllText(Output+"/result.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            foreach(var pair in preferences){if(pair.Value.existed)PlayerPrefs.SetInt(pair.Key,pair.Value.value);else PlayerPrefs.DeleteKey(pair.Key);}PlayerPrefs.Save();
            Time.timeScale=1;SessionState.SetInt(Flag+".code",code);SessionState.SetBool(Flag+".done",true);EditorApplication.ExitPlaymode();
        }
    }
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static void ValidateQuest(XunjuuQuestCard card)
    {
        Check(card!=null,"Missing redesigned quest card");Canvas.ForceUpdateCanvases();
        var texts=card.GetComponentsInChildren<Text>();var frame=card.GetComponent<RectTransform>().rect;
        foreach(var text in texts)
        {
            Check(text.preferredHeight<=text.rectTransform.rect.height+1,"Quest text clipped: "+text.name);
            var rect=new Rect(text.rectTransform.anchoredPosition-text.rectTransform.sizeDelta*.5f,text.rectTransform.sizeDelta);
            Check(frame.Contains(rect.min)&&frame.Contains(rect.max),"Quest text leaves card");
        }
        for(int i=0;i<texts.Length;i++)for(int j=i+1;j<texts.Length;j++)
        {
            var a=new Rect(texts[i].rectTransform.anchoredPosition-texts[i].rectTransform.sizeDelta*.5f,texts[i].rectTransform.sizeDelta);
            var b=new Rect(texts[j].rectTransform.anchoredPosition-texts[j].rectTransform.sizeDelta*.5f,texts[j].rectTransform.sizeDelta);
            Check(!a.Overlaps(b),"Quest labels overlap: "+texts[i].name+" / "+texts[j].name);
        }
        Check(!card.GetComponentsInChildren<Transform>().Any(t=>t.name=="Puntada"),"Old embroidery remains under quest text");
        var progress=card.GetComponentsInChildren<RectTransform>().First(t=>t.name=="Avance_Objetivo");
        Check(progress.localScale.x>.45f&&progress.localScale.x<.55f,"Quest progress not tracking three captures");
    }
    public static void PrepareForCombat(PlayerController player,DyanatroGameDirector director)
    {
        var browser=UnityEngine.Object.FindFirstObjectByType<XunjuuLudotecaBrowser>();browser.Close();
        var learning=director.GetComponent<XunjuuHerbalLearningMission>();
        foreach(string id in new[]{"manzanilla","manzanilla","hierbabuena"})
        {
            var plant=UnityEngine.Object.FindObjectsByType<XunjuuMedicinalPlant>(FindObjectsSortMode.None).First(p=>p.Entry.Id==id&&p.IsAvailable);
            player.transform.position=plant.transform.position+Vector3.right;
            if(player.TryGetComponent<Rigidbody>(out var body))body.position=player.transform.position;
            Check(plant.TryHarvest(player,out string reason),reason);
        }
        learning.RefreshProgress();browser.OpenHerbalism();
        browser.ShowPlantDetails(XunjuuMedicinalPlantCatalog.Find("manzanilla"));
        browser.ShowPlantDetails(XunjuuMedicinalPlantCatalog.Find("hierbabuena"));
        browser.Close();learning.RefreshProgress();
        Check(learning.TryIdentifyPlant("manzanilla"),"Identification failed");
        Check(player.GetComponent<XunjuuHerbalCrafting>().TryCraft(),"Preparation failed");learning.RefreshProgress();
    }
    private static void ValidateMorral(XunjuuInventoryUI ui)
    {
        Canvas.ForceUpdateCanvases();
        var panel=ui.GetComponentsInChildren<RectTransform>().First(t=>t.name=="Panel_Inventario");
        foreach(var text in panel.GetComponentsInChildren<Text>())
            Check(text.preferredHeight<=text.rectTransform.rect.height+2,"Truncated inventory text: "+text.name+" ("+text.preferredHeight+" / "+text.rectTransform.rect.height+")");
        var names=new[]{"Cuadricula_Objetos","Ficha_Objeto","Preparacion_Herbal"};
        var rects=panel.GetComponentsInChildren<RectTransform>().Where(r=>names.Contains(r.name)).ToArray();
        for(int i=0;i<rects.Length;i++)
        {
            var a=new Rect(rects[i].anchoredPosition-rects[i].sizeDelta*.5f,rects[i].sizeDelta);
            Check(panel.rect.Contains(a.min)&&panel.rect.Contains(a.max),"Inventory panel leaves frame: "+rects[i].name);
            for(int j=i+1;j<rects.Length;j++)
            {
                var b=new Rect(rects[j].anchoredPosition-rects[j].sizeDelta*.5f,rects[j].sizeDelta);
                Check(!a.Overlaps(b),"Overlapping inventory panels: "+rects[i].name+" / "+rects[j].name);
            }
        }
    }
    private static async Task CaptureGame(string path)
    {
        var camera=Camera.main;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay&&c.isActiveAndEnabled).ToArray();
        var parents=canvases.Select(c=>c.transform.parent).ToArray();
        var positions=canvases.Select(c=>c.transform.localPosition).ToArray();
        var scales=canvases.Select(c=>c.transform.localScale).ToArray();
        var rotations=canvases.Select(c=>c.transform.localRotation).ToArray();
        var cameras=canvases.Select(c=>c.worldCamera).ToArray();
        var distances=canvases.Select(c=>c.planeDistance).ToArray();
        var previousTarget=camera.targetTexture;var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;
        try
        {
            foreach(var canvas in canvases){canvas.transform.SetParent(null,false);canvas.transform.localScale=Vector3.one;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.2f;}
            await Task.Delay(180);Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.Destroy(image);
        }
        finally
        {
            for(int i=0;i<canvases.Length;i++){var c=canvases[i];c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=cameras[i];c.planeDistance=distances[i];c.transform.SetParent(parents[i],false);c.transform.localPosition=positions[i];c.transform.localScale=scales[i];c.transform.localRotation=rotations[i];}
            camera.targetTexture=previousTarget;rt.Release();UnityEngine.Object.Destroy(rt);
        }
    }
}
