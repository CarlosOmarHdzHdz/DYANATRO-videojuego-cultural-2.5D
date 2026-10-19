using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Solo en la copia de QA: no guarda la escena ni registra fichas del jugador.
[InitializeOnLoad]
public static class XunjuuSettlementValidation
{
    private const string Flag="Xunjuu.SettlementValidation";
    private const string Output="output/settlement-validation";
    static XunjuuSettlementValidation(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        Directory.CreateDirectory(Output);
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/Items/InfusionHierbas_v2.png",ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/Collectibles/FlorMemoria_v2.png",ImportAssetOptions.ForceUpdate);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
    }
    public static void InspectArt()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(string path in new[]{"Sprites/Items/InfusionHierbas_v2","Sprites/Collectibles/FlorMemoria_v2"})
        {
            string asset="Assets/Resources/"+path+".png";
            AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceUpdate);
            Debug.Log("[ART QA] "+asset+" exists="+File.Exists(asset)+" guid="+AssetDatabase.AssetPathToGUID(asset)
                +" assets="+string.Join(",",AssetDatabase.LoadAllAssetsAtPath(asset).Select(o=>o.GetType().Name+":"+o.name))
                +" resources="+string.Join(",",Resources.LoadAll<UnityEngine.Object>(path).Select(o=>o.GetType().Name+":"+o.name)));
        }
    }
    private static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag,false))Test();
        if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Flag+".done",false))
        {
            SessionState.SetBool(Flag+".done",false);
            if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+".code",1));
        }
    }
    private static async void Test()
    {
        SessionState.SetBool(Flag,false);
        try
        {
            float until=Time.realtimeSinceStartup+35;
            XunjuuMeadowDressing meadow=null;
            while(Time.realtimeSinceStartup<until)
            {
                meadow=UnityEngine.Object.FindFirstObjectByType<XunjuuMeadowDressing>();
                if(meadow!=null && meadow.VillageReady && XunjuuSettlementPlan.Fields.Count>0)break;
                await Task.Delay(150);
            }
            Check(meadow!=null && meadow.VillageReady,"Village not ready");
            Check(XunjuuSettlementPlan.Houses.Count>=8,"Too few houses: "+XunjuuSettlementPlan.Houses.Count);
            int target=(int)typeof(DyanatroGameDirector).GetField("cornFieldCount",BindingFlags.Instance|BindingFlags.NonPublic)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>());
            Check(XunjuuSettlementPlan.Fields.Count==target,"Fields: "+XunjuuSettlementPlan.Fields.Count+" / "+target);
            foreach(var field in XunjuuSettlementPlan.Fields)
            {
                foreach(var house in XunjuuSettlementPlan.Houses)Check(!XunjuuSettlementPlan.OverlapXZ(field,house),"Field hits house");
                Check(!XunjuuSettlementPlan.OverlapXZ(field,XunjuuSettlementPlan.HerbGarden),"Field hits garden");
                for(float x=field.min.x;x<=field.max.x;x+=1)
                    for(float z=field.min.z;z<=field.max.z;z+=1)Check(!XunjuuSettlementPlan.OnPath(new Vector3(x,0,z)),"Field hits path");
            }
            foreach(var access in XunjuuSettlementPlan.Accesses)
                Check(Mathf.Abs(access.junction.x-XunjuuSettlementPlan.MainPathAt(access.junction.z).x)<.01f,"Access disconnected");
            Check(XunjuuSettlementPlan.Accesses.Count>=XunjuuSettlementPlan.Houses.Count,"Missing house accesses");
            var crops=UnityEngine.Object.FindObjectsByType<XunjuuCrop>(FindObjectsSortMode.None);
            Check(crops.Length>100,"Missing crops");
            foreach(var crop in crops)
            {
                var sprite=crop.GetComponent<SpriteRenderer>();
                Check(sprite!=null && sprite.enabled && sprite.sprite!=null,"Corn sprite disabled");
                Check(!crop.GetComponentsInChildren<MeshRenderer>().Any(r=>r.GetComponent<TextMesh>()==null && r.GetComponent<TMPro.TMP_Text>()==null),"Corn converted to mesh: "+crop.name);
            }
            var director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            director.BeginGameForValidation();
            for(int i=0;i<45;i++)
            {
                if(director.IsIntroVideoVisible)director.SkipIntroForValidation();
                if(director.IsTimelinePrologueVisible)director.SkipTimelineForValidation();
                if(director.IsGameplayHudVisible)break;
                await Task.Delay(150);
            }
            await Task.Delay(1200);
            var plants=UnityEngine.Object.FindObjectsByType<XunjuuMedicinalPlant>(FindObjectsSortMode.None);
            Check(plants.Length>=18,"Garden plants: "+plants.Length);
            Check(plants.Select(p=>p.Entry.Id).Distinct().Count()==6,"Not all six medicinal species");
            foreach(var plant in plants)Check(XunjuuSettlementPlan.ContainsXZ(XunjuuSettlementPlan.HerbGarden,plant.transform.position),"Medicine outside garden");
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            Vector3 forward=PlayerController.CameraRelativeMovement(0,1,35),right=PlayerController.CameraRelativeMovement(1,0,35);
            Check(Mathf.Abs(Vector3.Dot(forward,right))<.0001f,"Skewed movement basis");
            Check(Mathf.Abs(PlayerController.CameraRelativeMovement(1,1,35).magnitude-1)<.0001f,"Diagonal faster");
            var animator=player.GetComponent<XunjuuCompleteSpriteAnimator>();
            var walk=(Sprite[])typeof(XunjuuCompleteSpriteAnimator).GetField("balancedWalk",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(animator);
            Check(walk!=null,"Missing walk sprites");
            for(int row=0;row<8;row++)for(int frame=1;frame<4;frame++)
                Check(Mathf.Abs(walk[row*4].pixelsPerUnit-walk[row*4+frame].pixelsPerUnit)<.001f,"Scale changes in walk cycle");
            var remedyTexture=Resources.Load<Texture2D>("Sprites/Items/InfusionHierbas_v2");
            Check(XunjuuMedicinalPlantCatalog.RemedySprite.name=="InfusionHierbas_v2","Old placeholder remedy: "+XunjuuMedicinalPlantCatalog.RemedySprite.name
                +"; texture="+(remedyTexture!=null?remedyTexture.name+" readable="+remedyTexture.isReadable:"null"));
            var flowers=UnityEngine.Object.FindObjectsByType<MazahuaWordCollectible>(FindObjectsSortMode.None);
            Check(flowers.Length==5,"Changed flower mission");
            foreach(var flower in flowers)
                Check(flower.GetComponentsInChildren<SpriteRenderer>().Any(s=>s.enabled&&s.sprite!=null&&s.sprite.name=="FlorMemoria_v2"),"Old flower");
            var inventory=player.GetComponent<XunjuuInventory>();
            inventory.AddItem("manzanilla","Manzanilla",2,XunjuuMedicinalPlantCatalog.GetSprite(XunjuuMedicinalPlantCatalog.Find("manzanilla")));
            inventory.AddItem("hierbabuena","Hierbabuena",1,XunjuuMedicinalPlantCatalog.GetSprite(XunjuuMedicinalPlantCatalog.Find("hierbabuena")));
            Check(player.GetComponent<XunjuuHerbalCrafting>().TryCraft(),"Recipe failed");
            player.GetComponent<XunjuuInventoryUI>().SetVisible(true);
            await Task.Delay(250);
            await CaptureCanvas(player.GetComponent<XunjuuInventoryUI>().GetComponentInChildren<Canvas>(),Output+"/morral-herbolaria.png");
            player.GetComponent<XunjuuInventoryUI>().SetVisible(false);
            CaptureWorld(XunjuuSettlementPlan.Origin+new Vector3(0,145,-112),XunjuuSettlementPlan.Origin+Vector3.forward*10,145,Output+"/distribucion.png");
            Vector3 garden=XunjuuSettlementPlan.HerbGarden.center;
            if(XunjuuSettlementPlan.TryGround(garden,out float ground))garden.y=ground;
            CaptureWorld(garden+new Vector3(0,22,-25),garden,21,Output+"/huerto.png");
            Bounds first=XunjuuSettlementPlan.Houses[0];
            CaptureWorld(first.center+new Vector3(21,18,-23),first.center,23,Output+"/casa-acceso.png");
            File.WriteAllText(Output+"/result.txt","PASS: "+XunjuuSettlementPlan.Houses.Count+" houses; "+XunjuuSettlementPlan.Fields.Count+" fields; "+XunjuuSettlementPlan.Accesses.Count+" connected accesses; "+crops.Length+" sprite crops; "+plants.Length+" medicinal plants restricted to one garden; six species; orthogonal movement; consistent walk scale; revised inventory, remedy and five collectible flowers.\n");
            Debug.Log("[SETTLEMENT VALIDATION PASS]");SessionState.SetInt(Flag+".code",0);
        }
        catch(Exception e){File.WriteAllText(Output+"/result.txt","FAIL: "+e);Debug.LogException(e);SessionState.SetInt(Flag+".code",1);}
        finally{SessionState.SetBool(Flag+".done",true);EditorApplication.ExitPlaymode();}
    }
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static async Task CaptureCanvas(Canvas canvas,string path)
    {
        var host=new GameObject("UI_Camera_QA",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=5;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.06f,.09f,.08f);camera.cullingMask=1<<30;
        camera.transform.position=new Vector3(0,0,-10);camera.nearClipPlane=.1f;camera.farClipPlane=5;
        var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
        Transform oldParent=canvas.transform.parent;
        Vector3 oldPosition=canvas.transform.localPosition,oldScale=canvas.transform.localScale;Quaternion oldRotation=canvas.transform.localRotation;
        var elements=canvas.GetComponentsInChildren<Transform>(true);var layers=elements.Select(t=>t.gameObject.layer).ToArray();
        canvas.transform.SetParent(null,false);canvas.transform.localScale=Vector3.one;
        foreach(var element in elements)element.gameObject.layer=30;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        await Task.Delay(150);Canvas.ForceUpdateCanvases();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
        File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;
        canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
        canvas.transform.SetParent(oldParent,false);canvas.transform.localPosition=oldPosition;canvas.transform.localScale=oldScale;canvas.transform.localRotation=oldRotation;
        for(int i=0;i<elements.Length;i++)elements[i].gameObject.layer=layers[i];
        camera.targetTexture=null;rt.Release();
        UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(host);
    }
    private static void CaptureWorld(Vector3 at,Vector3 target,float size,string path)
    {
        var host=new GameObject("Camera_QA",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(target-at));camera.orthographic=true;camera.orthographicSize=size;
        camera.farClipPlane=1500;camera.nearClipPlane=.1f;camera.clearFlags=CameraClearFlags.Skybox;
        var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
        File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;rt.Release();
        UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(host);
    }
}
