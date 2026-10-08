using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class XunjuuCaptureFlowValidation
{
    private const string Pending="Xunjuu.CaptureFlow.Pending";
    static XunjuuCaptureFlowValidation()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {SessionState.SetBool(Pending,false);Run();}
        };
    }
    [MenuItem("Tools/Xunjuu/Validar captura y fichas %&y")]
    public static void StartValidation()
    {
        if(EditorApplication.isPlaying)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/SampleScene.unity")
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }
    private static async void Run()
    {
        var saved=XunjuuFaunaCatalog.Entries.ToDictionary(e=>e.Id,e=>PlayerPrefs.HasKey("xunjuu.fauna.capturada."+e.Id)?PlayerPrefs.GetInt("xunjuu.fauna.capturada."+e.Id):-1);
        bool passed=false;
        try
        {
            await Task.Delay(2500);
            var director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var mission=UnityEngine.Object.FindFirstObjectByType<XunjuuLevel2KillMission>(FindObjectsInactive.Include);
            director.BeginGameForValidation();await Task.Delay(200);
            director.SkipIntroForValidation();await Task.Delay(500);
            director.SkipTimelineForValidation();await Task.Delay(500);
            director.GetComponent<XunjuuOpeningJourney>()?.FinishTutorial();
            director.ResetLevelProgressionForValidation();
            foreach(var flower in UnityEngine.Object.FindObjectsByType<MazahuaWordCollectible>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                director.CollectMazahuaWord(flower.MazahuaWord,flower.SpanishMeaning);
            Time.timeScale=1;await Task.Delay(1800);
            XunjuuWorldScaleValidation.Run(player);
            if(player.HasOrbitalWeapon() || !mission.IsAnimalPhase)throw new Exception("Arma prematura o fase de fauna ausente.");
            var animals=UnityEngine.Object.FindObjectsByType<XunjuuAnimalCapture>(FindObjectsSortMode.None).Where(a=>a.name.Contains("_Mision2_")).ToArray();
            if(animals.Length!=6)throw new Exception("Deben aparecer seis animales de mision.");
            foreach(var animal in animals)
            {
                var mesh=animal.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.name=="Fauna_Articulada");
                if(mesh==null || mesh.sharedMaterial.shader.name!="Xunjuu/AnimatedFauna")throw new Exception("Material incompatible en "+animal.name+": "+string.Join(";",animal.GetComponentsInChildren<Renderer>(true).Select(r=>r.name+"="+(r.sharedMaterial!=null?r.sharedMaterial.shader.name:"null")))+" motion="+animal.GetComponent<XunjuuAnimalSpriteMotion>()?.enabled);
                CheckVisiblePixels(animal);
                CheckFrameCycle(animal);
            }
            Directory.CreateDirectory("output/capture-flow");
            CaptureFrame("output/capture-flow/gameplay.png");
            var target=animals[0];
            player.transform.position=target.transform.position+Vector3.right*3;
            Physics.SyncTransforms();
            float start=target.GetComponent<XunjuuAnimalSpriteMotion>().WalkPhase;
            bool fled=false,runFrame=false;
            for(int i=0;i<30;i++)
            {await Task.Delay(100);fled|=target.GetComponent<Animal>().State==Animal.BehaviourState.Flee;runFrame|=target.GetComponent<XunjuuAnimalSpriteMotion>().CurrentFrame>=8;}
            if(!fled || target.GetComponent<XunjuuAnimalSpriteMotion>().WalkPhase==start)throw new Exception("La fauna no huye con animacion.");
            if(!runFrame)throw new Exception("La huida no selecciona los frames de carrera.");
            var browser=UnityEngine.Object.FindFirstObjectByType<XunjuuLudotecaBrowser>(FindObjectsInactive.Include);
            foreach(var animal in animals)
            {
                PlayerPrefs.DeleteKey("xunjuu.fauna.capturada."+animal.FaunaId);
                if(browser.ShowFaunaDetails(animal.Entry))throw new Exception("Ficha abierta antes de capturar.");
                animal.Capture();
                if(!browser.ShowFaunaDetails(animal.Entry))throw new Exception("Ficha no desbloqueada por captura.");
            }
            await Task.Delay(700);
            if(!mission.IsEnemyPhase || !player.HasOrbitalWeapon() || player.HasOrbitalAttackUnlocked())throw new Exception("Recompensa incorrecta tras seis capturas.");
            await CheckWeaponGrip(player);
            browser.Open();
            typeof(XunjuuLudotecaBrowser).GetMethod("ShowPage",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(browser,new object[]{Enum.Parse(typeof(XunjuuLudotecaBrowser).GetNestedType("Page",System.Reflection.BindingFlags.NonPublic),"Fauna"),false});
            browser.ShowFaunaDetails(XunjuuFaunaCatalog.Find("tlacuache"));
            await Task.Delay(500);
            CaptureFrame("output/capture-flow/ficha.png");
            await Task.Delay(300);
            browser.Close();
            await XunjuuFaunaNavigationValidation.Run();
            passed=true;
            Debug.Log("[CAPTURE FLOW PASS] Escena real: 6 animales visibles, huida animada, sin arma antes de captura, arma despues, fichas bloqueadas/desbloqueadas.");
        }
        catch(Exception e){Debug.LogError("[CAPTURE FLOW FAIL] "+e);}
        finally
        {
            foreach(var item in saved){string key="xunjuu.fauna.capturada."+item.Key;if(item.Value<0)PlayerPrefs.DeleteKey(key);else PlayerPrefs.SetInt(key,item.Value);}
            PlayerPrefs.Save();Time.timeScale=1;
            EditorApplication.ExitPlaymode();
            if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(passed?0:1);
        }
    }
    private static async Task CheckWeaponGrip(PlayerController player)
    {
        var weapon=player.GetComponentInChildren<OrbitalWeapon>();
        var pose=player.GetComponent<XunjuuCompleteSpriteAnimator>();
        var renderer=weapon.GetComponentInChildren<SpriteRenderer>();
        if(!weapon.IsAtRest() || !weapon.IsWeaponVisible)throw new Exception("Arma sin agarre o invisible al equipar.");
        Quaternion rest=weapon.transform.rotation;
        if(!weapon.ExecuteAttack())throw new Exception("No inicia el tajo.");
        float deadline=Time.realtimeSinceStartup+weapon.GetAttackDuration()+2f;
        while(weapon.IsBusy && Time.realtimeSinceStartup<deadline)
        {
            await Task.Delay(20);
            if(Vector3.Distance(renderer.transform.position,pose.HandGripWorld)>.025f)
                throw new Exception("El mango se separa de la mano durante el tajo.");
        }
        await Task.Delay(100);
        if(!weapon.IsAtRest() || Quaternion.Angle(rest,weapon.transform.rotation)>1f)
            throw new Exception("El tajo no regresa al reposo.");
        CaptureFrame("output/capture-flow/weapon-equipped.png");
        Debug.Log("[WEAPON GRIP PASS] Mango unido a la mano durante el tajo y retorno al reposo.");
    }

    private static void CheckVisiblePixels(XunjuuAnimalCapture animal)
    {
        // Render the actual animated material, then compare it with the mesh hidden.
        var mesh=animal.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Fauna_Articulada");
        var go=new GameObject("QA_Fauna_Camera",typeof(Camera));
        var camera=go.GetComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;
        camera.orthographic=true;camera.orthographicSize=3;camera.nearClipPlane=.01f;camera.farClipPlane=12;
        camera.transform.rotation=mesh.transform.rotation;
        camera.transform.position=mesh.bounds.center-camera.transform.forward*6;
        var rt=new RenderTexture(128,128,24);var previous=RenderTexture.active;
        var image=new Texture2D(128,128,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();var visible=image.GetPixels32();
            Directory.CreateDirectory("output/fauna-cycles");
            File.WriteAllBytes("output/fauna-cycles/"+animal.FaunaId+".png",image.EncodeToPNG());
            mesh.enabled=false;camera.Render();image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();var hidden=image.GetPixels32();
            int changed=visible.Where((c,i)=>Mathf.Abs(c.r-hidden[i].r)+Mathf.Abs(c.g-hidden[i].g)+Mathf.Abs(c.b-hidden[i].b)>20).Count();
            if(changed<30)throw new Exception("Animal invisible al renderizar: "+animal.name+" pixeles="+changed);
        }
        finally{mesh.enabled=true;RenderTexture.active=previous;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(go);}
    }
    private static void CheckFrameCycle(XunjuuAnimalCapture animal)
    {
        var motion=animal.GetComponent<XunjuuAnimalSpriteMotion>();
        if(motion==null || !motion.HasFrameAnimation)throw new Exception("Ciclo ausente: "+animal.FaunaId);
        var atlas=Resources.Load<Texture2D>("Sprites/Animals/CyclesV2/"+animal.FaunaId);
        var signatures=new System.Collections.Generic.HashSet<int>();
        for(int frame=0;frame<16;frame++)
        {
            var pixels=atlas.GetPixels(frame%4*128,(3-frame/4)*128,128,128);
            int hash=17,count=0;
            unchecked { foreach(var pixel in pixels) { Color32 c=pixel;hash=hash*31+c.GetHashCode();if(c.a>0)count++;if(c.a!=0 && c.a!=255)throw new Exception("Alfa desigual"); } }
            if(count<100)throw new Exception("Frame vacio");
            if(!signatures.Add(hash))throw new Exception("Pose duplicada: "+animal.FaunaId+"/"+frame);
        }
        var filter=animal.GetComponentsInChildren<MeshFilter>().First(f=>f.name=="Fauna_Articulada");
        var vertices=filter.sharedMesh.vertices;
        var pose=typeof(XunjuuAnimalSpriteMotion).GetMethod("Pose",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        pose.Invoke(motion,new object[]{0f,1f});var first=filter.sharedMesh.uv;
        pose.Invoke(motion,new object[]{1.5f,1f});var second=filter.sharedMesh.uv;
        if(first.SequenceEqual(second) || !vertices.SequenceEqual(filter.sharedMesh.vertices))throw new Exception("UV inmovil o pivote deformado");
        pose.Invoke(motion,new object[]{0f,0f});
        Debug.Log("[FRAME CYCLE PASS] "+animal.FaunaId+": 16 poses distintas, quad estable, alpha binario.");
    }
    private static void CaptureFrame(string path)
    {
        var camera=Camera.main;
        var overlays=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras=overlays.Select(c=>c.worldCamera).ToArray();
        var distances=overlays.Select(c=>c.planeDistance).ToArray();
        var target=new RenderTexture(1920,1080,24);var old=RenderTexture.active;var oldTarget=camera.targetTexture;
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;
            foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+1;}
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally
        {
            for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];}
            camera.targetTexture=oldTarget;RenderTexture.active=old;
            UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(image);
        }
    }
}
