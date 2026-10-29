using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Microbenchmark repetible en la misma escena; no representa los FPS de Windows.
[InitializeOnLoad]
public static class XunjuuLudotecaPerformanceValidation
{
    private const string Flag="Xunjuu.LudotecaPerformance";
    private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [Serializable] private sealed class Sample { public string label;public int objects,iterations;public double policyMs,policyBytes,optimizerMs,optimizerBytes,guideMs,guideBytes; }
    static XunjuuLudotecaPerformanceValidation(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    private static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false))Test();
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Flag+".done",false))
        {SessionState.SetBool(Flag+".done",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+".code",1));}
    }
    private static (double ms,double bytes) Measure(Action work,int count)
    {
        work();long before=GC.GetAllocatedBytesForCurrentThread();var watch=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<count;i++)work();watch.Stop();return(watch.Elapsed.TotalMilliseconds/count,(GC.GetAllocatedBytesForCurrentThread()-before)/(double)count);
    }
    private static async void Test()
    {
        SessionState.SetBool(Flag,false);int code=1;
        try
        {
            var director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();director.BeginGameForValidation();
            for(int i=0;i<100&&!director.IsGameplayHudVisible;i++)
            {if(director.IsIntroVideoVisible)director.SkipIntroForValidation();if(director.IsTimelinePrologueVisible)director.SkipTimelineForValidation();await Task.Delay(100);}
            director.GetComponent<XunjuuOpeningJourney>()?.FinishTutorial();await Task.Delay(5000);Time.timeScale=1;
            var policy=director.GetComponent<XunjuuSceneVisualPolicy>();var optimizer=director.GetComponent<XunjuuLevelOptimizer>();
            var guide=typeof(DyanatroGameDirector).GetMethod("UpdateCollectibleGuide",Private);
            var a=Measure(policy.Refresh,12);var b=Measure(optimizer.RefreshObjects,12);var c=Measure(()=>guide.Invoke(director,null),120);
            var sample=new Sample{label=Environment.GetCommandLineArgs().Contains("-ludotecaBaseline")?"antes":"despues",objects=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,iterations=12,policyMs=a.ms,policyBytes=a.bytes,optimizerMs=b.ms,optimizerBytes=b.bytes,guideMs=c.ms,guideBytes=c.bytes};
            Directory.CreateDirectory("output/ludoteca-performance");File.WriteAllText("output/ludoteca-performance/"+sample.label+".json",JsonUtility.ToJson(sample,true));
            Debug.Log("[LUDOTECA PERFORMANCE PASS] "+JsonUtility.ToJson(sample));code=0;
        }
        catch(Exception e){Debug.LogException(e);}
        finally{Time.timeScale=1;SessionState.SetInt(Flag+".code",code);SessionState.SetBool(Flag+".done",true);EditorApplication.ExitPlaymode();}
    }
}
