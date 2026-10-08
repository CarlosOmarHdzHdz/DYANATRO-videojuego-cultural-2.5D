using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class XunjuuMinigameValidation
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    private const string Pending="Xunjuu.MinigameValidation";
    static XunjuuMinigameValidation()
    {
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);Run();}};
    }
    [MenuItem("Tools/Xunjuu/Validar minijuegos")]
    public static void Start()
    {
        if(EditorApplication.isPlaying)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/SampleScene.unity")
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    private static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Flags).Invoke(o,args);
    private static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Flags).GetValue(o);
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    private static void Click(XunjuuLudotecaBrowser b,string label)
    {
        var root=Field<GameObject>(b,"browserRoot");
        root.GetComponentsInChildren<Button>().First(x=>x.name=="Boton_"+label).onClick.Invoke();
    }
    private static void Success(XunjuuLudotecaBrowser b)
    {
        Check(Field<bool>(b,"activityComplete"),"Activity not completed");
        Check(Field<GameObject>(b,"activityRoot").GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Felicidades, completaste el minijuego")),"Missing success message");
    }
    private static async Task WaitGalleryFlip(object view)
    {
        for(int i=0;i<30 && Field<Coroutine>(view,"Flip")!=null;i++)await Task.Delay(50);
        Check(Field<Coroutine>(view,"Flip")==null,"Gallery flip did not finish while paused");
    }
    private static void Capture(string name)
    {
        typeof(XunjuuCaptureFlowValidation).GetMethod("CaptureFrame",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"output/minigames/"+name+".png"});
    }
    private static async void Run()
    {
        XunjuuLudotecaBrowser b=null;
        bool passed=false;
        try
        {
            await Task.Delay(2000);Directory.CreateDirectory("output/minigames");
            b=UnityEngine.Object.FindFirstObjectByType<XunjuuLudotecaBrowser>(FindObjectsInactive.Include);
            Check(b!=null,"Browser missing");b.Open();await Task.Delay(300);Capture("inicio");
            Click(b,"Galería");Capture("galeria");
            Click(b,"Ficha_0");
            var galleryViews=Field<Array>(b,"galleryViews");
            await WaitGalleryFlip(galleryViews.GetValue(0));
            Check(Field<RectTransform>(galleryViews.GetValue(0),"Rect").rect.width>=550f,"Gallery image layout is still too small");
            Check(Field<GameObject>(galleryViews.GetValue(0),"Back").activeSelf,"Gallery card did not reveal information");
            Check(Mathf.Abs(Field<RectTransform>(galleryViews.GetValue(0),"Rect").localScale.x-1f)<.01f,"Gallery flip did not finish while paused");
            Capture("galeria-reverso");
            Click(b,"Ficha_0");await WaitGalleryFlip(galleryViews.GetValue(0));
            Check(!Field<GameObject>(galleryViews.GetValue(0),"Back").activeSelf,"Gallery card did not return to photo");
            Click(b,"SIGUIENTE");Check(Field<int>(b,"galleryPage")==1,"Gallery next page failed");Capture("galeria-2");
            Click(b,"SIGUIENTE");Check(Field<int>(b,"galleryPage")==2,"Third gallery page missing");Capture("galeria-3");
            Click(b,"SIGUIENTE");Check(Field<int>(b,"galleryPage")==3,"Fourth gallery page missing");Capture("galeria-4");
            Click(b,"SIGUIENTE");Check(Field<int>(b,"galleryPage")==4,"Fifth gallery page missing");Capture("galeria-5");
            Check(!Field<Button>(galleryViews.GetValue(1),"Button").gameObject.activeSelf,"Final gallery page should contain one centered card");
            Check(Mathf.Abs(Field<RectTransform>(galleryViews.GetValue(0),"Rect").anchorMin.x-.5f)<.01f,"Final gallery card is not centered");
            Click(b,"ANTERIOR");Check(Field<int>(b,"galleryPage")==3,"Gallery previous page failed");
            Click(b,"Cultura");Capture("cultura");
            Click(b,"LEER TEXTO COMPLETO");Check(Field<GameObject>(b,"readingOverlay").activeSelf,"Culture reading did not open");Capture("lectura-cultura");Click(b,"VOLVER A FICHAS");
            Click(b,"Lengua mazahua");Capture("lengua");
            Click(b,"LEER TEXTO COMPLETO");Check(Field<GameObject>(b,"readingOverlay").activeSelf,"Language reading did not open");Click(b,"VOLVER A FICHAS");
            Click(b,"Minijuegos");await Task.Delay(200);Capture("menu");
            Click(b,"JUGAR MEMORAMA");
            var deck=Field<int[]>(b,"memoryDeck");var cards=Field<Button[]>(b,"tiles");
            Check(!Field<GameObject>(b,"miniGameHub").activeSelf,"Activity hub should hide while playing");
            Check(!Field<GameObject>(b,"contentOrnaments").activeSelf,"Page ornaments should not overlap the activity");
            Check(!Field<TMP_Text>(b,"pageTitle").gameObject.activeSelf,"Duplicate page title should hide during activities");
            Check(cards.All(card=>card.GetComponent<RectTransform>().rect.width>=130f),"Memory cards are too small");
            Check(Field<GameObject>(b,"memoryBoard").transform.Find("Superficie_De_Juego").GetComponent<RectTransform>().anchorMin.y<.10f,"Memory board still leaves unused space below");
            Check(Field<GameObject>(b,"activityRoot").transform.Find("Cuaderno_De_Observacion")==null,"Redundant lower memory panel still exists");
            int different=Array.FindIndex(deck,x=>x!=deck[0]);
            cards[0].onClick.Invoke();await Task.Delay(350);
            Check(cards[0].transform.Find("Marco_Frontal").gameObject.activeSelf,"Memory card did not animate open");
            cards[0].onClick.Invoke();Check(Field<int>(b,"pairs")==0,"Same card matched");
            cards[different].onClick.Invoke();await Task.Delay(1600);Check(Field<int>(b,"firstTile")==-1,"Mismatch timer must work while paused");
            Capture("memorama");
            for(int stage=0;stage<3;stage++)
            {
                deck=Field<int[]>(b,"memoryDeck");cards=Field<Button[]>(b,"tiles");
                for(int value=stage*6;value<(stage+1)*6;value++)
                    foreach(int index in Enumerable.Range(0,12).Where(i=>deck[i]==value))cards[index].onClick.Invoke();
                Check(Field<int>(b,"pairs")==6*(stage+1),"Memory stage did not count six pairs");
                if(stage<2){Click(b,"CONTINUAR");Check(Field<int>(b,"memoryStage")==stage+1,"Memory stage did not advance");}
            }
            Success(b);Capture("memorama-completado");Click(b,"JUGAR DE NUEVO");Check(Field<int>(b,"pairs")==0,"Memory restart failed");Click(b,"VOLVER");
            Check(Field<GameObject>(b,"miniGameHub").activeSelf,"Activity hub did not return");
            Check(Field<GameObject>(b,"contentOrnaments").activeSelf,"Page ornaments did not return");
            Click(b,"JUGAR SOPA DE LETRAS");cards=Field<Button[]>(b,"tiles");
            string[] words=Field<string[]>(b,"searchWords");
            char[] letters=Field<char[]>(b,"searchLetters");
            var placements=Field<Dictionary<string,int[]>>(b,"searchPlacements");
            Check(cards.Length==100 && words.Length==8 && placements.Count==8,"Random 10x10 search with eight terms missing");
            foreach(string word in words)
            {
                Check(placements.TryGetValue(word,out int[] path) && path.Length==word.Length,"Word placement missing: "+word);
                Check(new string(path.Select(index=>letters[index]).ToArray())==word,"Grid does not contain: "+word);
                int dx=path[1]%10-path[0]%10,dy=path[1]/10-path[0]/10;
                Check(Math.Abs(dx)<=1 && Math.Abs(dy)<=1 && (dx!=0 || dy!=0),"Invalid word direction: "+word);
                Check(path.Skip(1).Select((cell,i)=>cell-path[i]).All(step=>step==path[1]-path[0]),"Word path is not straight: "+word);
            }
            string firstGrid=new string(letters);
            cards[0].onClick.Invoke();
            Check(Field<GameObject[]>(b,"searchStitches")[0].activeSelf,"Selected letter lacks embroidery");
            await Task.Delay(220);Capture("sopa-seleccion");
            cards[0].onClick.Invoke();Check(!Field<bool>(b,"activityComplete"),"Invalid selection completed activity");
            await Task.Delay(250);Capture("sopa");
            var activity=Field<GameObject>(b,"activityRoot");
            Check(activity.transform.Find("Cuadricula_Academica")!=null,"Academic word-search grid missing");
            Check(activity.transform.Find("Guia_Didactica")!=null,"Didactic guide missing");
            Check(cards[0].GetComponent<Image>().color.r>.8f,"Word-search cells should have a light reading surface");
            Check(activity.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("AVANCE · 0 / 8")),"Word-search progress missing");
            for(int i=0;i<words.Length;i++)
            {
                int[] path=placements[words[i]];
                cards[path[i==0?path.Length-1:0]].onClick.Invoke();
                cards[path[i==0?0:path.Length-1]].onClick.Invoke();
            }
            Check(Field<HashSet<string>>(b,"foundWords").Count==8,"Not all words were found");
            Check(Field<GameObject[]>(b,"searchStitches").Any(stitch=>stitch!=null && stitch.activeSelf),"Found terms lack embroidery");
            Success(b);await Task.Delay(380);Capture("sopa-completada");
            Click(b,"JUGAR DE NUEVO");
            Check(new string(Field<char[]>(b,"searchLetters"))!=firstGrid,"Replay did not randomize the puzzle");
            Click(b,"VOLVER");
            Click(b,"INICIAR QUIZ");cards=Field<Button[]>(b,"miniGameAnswerButtons");
            Check(Field<GameObject>(b,"activityRoot").transform.Find("Ficha_De_Pregunta")!=null,"Quiz question panel missing");
            cards[(Field<int>(b,"quizCorrectSlot")+1)%3].onClick.Invoke();Check(Field<int>(b,"miniGameIndex")==0,"Wrong answer advanced");Capture("quiz");
            for(int i=0;i<5;i++)cards[Field<int>(b,"quizCorrectSlot")].onClick.Invoke();
            Success(b);await Task.Delay(380);Capture("quiz-completado");Click(b,"VOLVER A MINIJUEGOS");
            Check(Time.timeScale==0,"World unpaused inside library");b.Close();
            File.WriteAllText("output/minigames/validation.txt","PASS: gallery and sidebar navigation; expanded memory layout, flip and 18 pairs; randomized 10x10 word search with eight valid placements, reverse selection, embroidered selection and replay; quiz retry and success; unscaled entrance and completion animations; three success panels while the world is paused.");
            Debug.Log("[MINIGAMES PASS] Three playable activities and success screens verified.");
            passed=true;
        }
        catch(Exception e){Debug.LogError("[MINIGAMES FAIL] "+e);File.WriteAllText("output/minigames/validation.txt","FAIL: "+e);}
        finally{if(b!=null && b.IsOpen)b.Close();EditorApplication.ExitPlaymode();if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(passed?0:1);}
    }
}
