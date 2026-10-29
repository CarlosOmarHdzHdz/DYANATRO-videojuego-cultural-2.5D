using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class XunjuuLudotecaDesignValidation
{
    private const string Flag="Xunjuu.LudotecaDesign";
    private const string Output="output/ludoteca-design";
    private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static readonly List<string> errors=new List<string>();
    static XunjuuLudotecaDesignValidation(){EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived+=Log;}
    public static void Run(){Directory.CreateDirectory(Output);EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");errors.Clear();SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    private static void Log(string message,string trace,LogType type)
    {if(SessionState.GetBool(Flag,false)&&(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)&&!trace.Contains("UnityEditor.Search.SearchDatabase"))errors.Add(message);}
    private static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false))Test();
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Flag+".done",false))
        {SessionState.SetBool(Flag+".done",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+".code",1));}
    }
    private static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
    private static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static void Labels(Transform root)
    {
        foreach(var text in root.GetComponentsInChildren<TMP_Text>())
        {
            if(string.IsNullOrWhiteSpace(text.text)||text.rectTransform.rect.height<=0)continue;
            text.ForceMeshUpdate();
            Check(text.preferredHeight<=text.rectTransform.rect.height+2,"Text clipped: "+text.text+" ("+text.preferredHeight+" / "+text.rectTransform.rect.height+")");
        }
    }
    private static async void Test()
    {
        int code=1;var saved=new Dictionary<string,(bool exists,int value)>();
        bool hadMotion=PlayerPrefs.HasKey(XunjuuPresentationPreferences.MotionKey),motion=XunjuuPresentationPreferences.ReducedMotion;
        var random=UnityEngine.Random.state;
        foreach(var e in XunjuuFaunaCatalog.Entries){string key="xunjuu.fauna.capturada."+e.Id;saved[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetInt(key));}
        foreach(var e in XunjuuMedicinalPlantCatalog.Entries){string key=XunjuuMedicinalPlantCatalog.SavePrefix+e.Id;saved[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetInt(key));}
        try
        {
            await WaitUntil(()=>
            {
                var transition=UnityEngine.Object.FindFirstObjectByType<XunjuuSceneTransition>();
                var transitionCanvas=transition!=null?transition.GetComponentInChildren<Canvas>(true):null;
                return GameObject.Find("MenuInicio")!=null&&(transitionCanvas==null||!transitionCanvas.gameObject.activeInHierarchy);
            },30000,"World startup transition did not finish");
            Check(errors.Count==0,string.Join("; ",errors));
            var browser=UnityEngine.Object.FindFirstObjectByType<XunjuuLudotecaBrowser>();
            var canvas=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.name=="Dyanatro_UI");var worldCamera=Camera.main;int mask=worldCamera.cullingMask;browser.Open();await Task.Delay(300);
            Check(worldCamera!=null&&worldCamera.enabled&&worldCamera.cullingMask==0&&Camera.main==worldCamera,"Library must retain an active display camera without drawing world geometry");
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            typeof(PlayerController).GetField("moveInput",Private).SetValue(player,Vector3.right);
            Call(player,"Update");Check(Field<Vector3>(player,"moveInput")==Vector3.zero,"Player still processes movement while paused");
            var root=Field<GameObject>(browser,"browserRoot");
            Check(!root.GetComponentsInChildren<Transform>(true).Any(t=>new[]{"Pestanas","Barra_Navegador","Direccion"}.Contains(t.name)),"Old browser chrome remains");
            Check(!root.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text.Contains("xunjuu://")),"Fake URL remains");
            var nav=root.GetComponentsInChildren<RectTransform>().First(t=>t.name=="Navegacion");var navButtons=nav.GetComponentsInChildren<Button>();Check(navButtons.Length==8,"Missing navigation destination");
            Labels(nav);
            var hits=new List<RaycastResult>();var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,navButtons[0].transform.position)};
            EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==navButtons[0],"Navigation is not reachable by pointer");
            for(int i=0;i<navButtons.Length;i++)for(int j=i+1;j<navButtons.Length;j++)
            {
                var a=navButtons[i].GetComponent<RectTransform>();var b=navButtons[j].GetComponent<RectTransform>();
                Check(Mathf.Abs(a.position.y-b.position.y)>Mathf.Min(a.rect.height,b.rect.height)*a.lossyScale.y,"Navigation buttons overlap");
            }
            foreach(var e in XunjuuFaunaCatalog.Entries)XunjuuFaunaCatalog.RegisterCapture(e.Id);
            foreach(var e in XunjuuMedicinalPlantCatalog.Entries)XunjuuMedicinalPlantCatalog.Register(e.Id);
            var pageType=typeof(XunjuuLudotecaBrowser).GetNestedType("Page",BindingFlags.NonPublic);
            foreach(string page in new[]{"Home","Culture","Gallery","Fauna","Plants","Language","MiniGame","Survey"})
            {
                Call(browser,"ShowPage",Enum.Parse(pageType,page),false);await Task.Delay(250);
                Labels(Field<TMP_Text>(browser,"pageTitle").transform);Labels(Field<TMP_Text>(browser,"pageSubtitle").transform);
                if(page=="Home")Labels(Field<GameObject>(browser,"homeRoot").transform);
                if(page=="Fauna")Labels(Field<GameObject>(browser,"faunaRoot").transform);
                if(page=="Plants")Labels(Field<GameObject>(browser,"plantsRoot").transform);
                await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/"+page+".png");
                if(page=="Plants")await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/Herbolaria-4x3.png",1024,768);
                if(page=="Gallery")
                {
                    Call(browser,"FlipGalleryCard",0);await Task.Delay(400);
                    await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/Galeria-lectura.png");
                }
            }
            Call(browser,"ShowPage",Enum.Parse(pageType,"MiniGame"),false);Call(browser,"StartMemory");await Task.Delay(300);
            var tiles=Field<Button[]>(browser,"tiles");Check(tiles.Length==12,"Memory stage missing cards");
            foreach(var tile in tiles){var rect=tile.GetComponent<RectTransform>();Check(Mathf.Abs(rect.rect.width-rect.rect.height)<.01f,"Memory image not square");Check(!tile.GetComponentsInChildren<TMP_Text>().Any(t=>!string.IsNullOrWhiteSpace(t.text)),"Memory cards gained labels");}
            await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/Memorama.png");
            XunjuuPresentationPreferences.SetReducedMotion(true);
            for(int stage=0;stage<3;stage++)
            {
                int[] deck=Field<int[]>(browser,"memoryDeck");
                foreach(int value in deck.Distinct())
                {int[] pair=Enumerable.Range(0,deck.Length).Where(i=>deck[i]==value).ToArray();Call(browser,"PickMemory",pair[0]);Call(browser,"PickMemory",pair[1]);}
                if(stage<2)Call(browser,"AdvanceMemoryStage");
            }
            Check(Field<int>(browser,"pairs")==18&&Field<bool>(browser,"activityComplete"),"Memory progression not completed");
            Call(browser,"ShowPage",Enum.Parse(pageType,"Home"),false);Check(Field<TMP_Text>(browser,"pageTitle").gameObject.activeSelf,"Leaving activity hid page heading");
            Call(browser,"ShowPage",Enum.Parse(pageType,"MiniGame"),false);UnityEngine.Random.InitState(672);Call(browser,"StartWordSearch");await Task.Delay(250);
            char[] first=(char[])Field<char[]>(browser,"searchLetters").Clone();Call(browser,"StartWordSearch");await Task.Delay(250);
            Check(!first.SequenceEqual(Field<char[]>(browser,"searchLetters")),"Search board did not randomize");
            var placements=Field<Dictionary<string,int[]>>(browser,"searchPlacements");Check(placements.Count==8,"Search lost terms");
            foreach(var path in placements.Values){Call(browser,"PickLetter",path[0]);Call(browser,"PickLetter",path[path.Length-1]);}
            Check(Field<HashSet<string>>(browser,"foundWords").Count==8,"Search solutions fail");Check(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Bordado_De_Seleccion"),"Embroidery selection missing");
            Call(browser,"StartWordSearch");await Task.Delay(300);await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/Sopa.png");await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/Sopa-4x3.png",1024,768);
            Call(browser,"StartQuiz");await Task.Delay(300);await XunjuuSettlementValidation.CaptureCanvas(canvas,Output+"/Quiz.png");
            for(int i=0;i<5;i++)Call(browser,"AnswerQuiz",Field<int>(browser,"quizCorrectSlot"));Check(Field<bool>(browser,"activityComplete"),"Quiz completion broken");
            browser.Close();Check(!browser.IsOpen&&Time.timeScale==0&&worldCamera.enabled&&worldCamera.cullingMask==mask,"Library failed to restore camera or main-menu pause");
            Time.timeScale=1;browser.Close();Check(Time.timeScale==1,"Repeated Close restored an obsolete pause state");Time.timeScale=0;
            browser.Open();browser.Open();browser.Close();Check(Time.timeScale==0&&worldCamera.enabled,"Repeated Open lost camera or pause state");
            Check(errors.Count==0,string.Join("; ",errors));
            File.WriteAllText(Output+"/result.txt","PASS: no fake browser tabs or URL; 8 navigation destinations; heading and navigation legibility; all pages captured; square unlabelled memory cards; all 18 pairs; restored heading; randomized 8-word search and embroidered selections; quiz completion; main-menu pause preserved; zero game errors.\n");Debug.Log("[LUDOTECA DESIGN PASS]");code=0;
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText(Output+"/result.txt","FAIL: "+e);}
        finally
        {
            foreach(var item in saved){if(item.Value.exists)PlayerPrefs.SetInt(item.Key,item.Value.value);else PlayerPrefs.DeleteKey(item.Key);}
            XunjuuPresentationPreferences.SetReducedMotion(motion);if(!hadMotion)PlayerPrefs.DeleteKey(XunjuuPresentationPreferences.MotionKey);PlayerPrefs.Save();UnityEngine.Random.state=random;
            SessionState.SetBool(Flag,false);SessionState.SetInt(Flag+".code",code);SessionState.SetBool(Flag+".done",true);Time.timeScale=1;EditorApplication.ExitPlaymode();
        }
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
}
