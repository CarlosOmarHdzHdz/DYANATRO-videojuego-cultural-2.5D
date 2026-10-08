using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class XunjuuProductionSmoke
{
    private const string Flag="Temp/XunjuuProductionSmoke.flag";
    private const string Key="Xunjuu.ProductionSmoke";
    static XunjuuProductionSmoke()
    {
        EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)) Run();
        };
    }
    private static void Poll()
    {
        if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying) return;
        File.Delete(Flag); SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
    }
    private static async void Run()
    {
        try
        {
            await Task.Delay(3500);
            var director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            if(director==null) throw new Exception("No director in active scene.");
            director.BeginGameForValidation();
            await Task.Delay(1200);
            if(director.IsGameplayHudVisible) throw new Exception("HUD leaked into intro.");
            director.SkipIntroForValidation();
            await Task.Delay(1200);
            director.SkipTimelineForValidation();
            await Task.Delay(3500);
            if(!director.IsGameplayHudVisible) throw new Exception("HUD not restored.");
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var health=UnityEngine.Object.FindFirstObjectByType<BarraVidaFrames>();
            if(player==null||health==null||health.imagenBarra.sprite==null) throw new Exception("Player or health missing.");
            int models=0;
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if(renderer.GetComponentInParent<XunjuuEnvironment3D>()!=null || renderer.transform.parent?.name=="Visual_3D") models++;
            if(models==0) throw new Exception("No 3D environment instantiated.");
            Directory.CreateDirectory("output/production-validation");
            ScreenCapture.CaptureScreenshot("output/production-validation/gameplay.png");
            File.WriteAllText("output/production-validation/runtime.txt","PASS: main scene, intro isolation, HUD restoration, player and health present, 3D mesh renderers="+models+". "+DateTime.Now.ToString("O"));
            await Task.Delay(1500);
        }
        catch(Exception error)
        {
            Directory.CreateDirectory("output/production-validation");
            File.WriteAllText("output/production-validation/runtime.txt","FAIL: "+error);
            Debug.LogException(error);
        }
        finally { SessionState.SetBool(Key,false); EditorApplication.ExitPlaymode(); }
    }
}
