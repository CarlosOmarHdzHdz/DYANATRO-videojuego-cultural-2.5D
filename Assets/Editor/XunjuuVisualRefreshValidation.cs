using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class XunjuuRefinedAtlasImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if(!assetPath.EndsWith("MateoRefined_4x4.png") && !assetPath.EndsWith("MateoDirectional_4x8.png") && !assetPath.EndsWith("MateoActions_4x8.png") && !assetPath.EndsWith("MacuahuitlEquipment.png") && !assetPath.EndsWith("MateoWalkBalanced_4x8.png") && !assetPath.EndsWith("MateoWalkSides_4x3.png"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.isReadable=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
        importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
    }
}

[InitializeOnLoad]
public static class XunjuuVisualRefreshValidation
{
    private const string Flag="Temp/XunjuuVisualRefresh.flag",Key="Xunjuu.VisualRefresh.Pending";
    private const string Output="output/visual-review";
    private static readonly List<string> errors=new List<string>();
    static XunjuuVisualRefreshValidation()
    {
        EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=state=>
        {if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))RunRuntime();};
    }
    private static void Poll()
    {
        if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;
        File.Delete(Flag);Prepare();SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
    }
    [MenuItem("Tools/Xunjuu/Revision visual/Preparar modelos y comprobar")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Output);
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/Player/MateoWalkSides_4x3.png",ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/TextureTerrain/FloraExpansion_4x2.png",ImportAssetOptions.ForceUpdate);
        var floraImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Sprites/TextureTerrain/FloraExpansion_4x2.png");
        if(floraImporter!=null){XunjuuFloraExpansionImporter.Configure(floraImporter);floraImporter.SaveAndReimport();}
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/Player/MateoRefined_4x4.png",ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/Player/MateoDirectional_4x8.png",ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/Player/MateoWalkBalanced_4x8.png",ImportAssetOptions.ForceUpdate);
        var walkImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Sprites/Player/MateoWalkBalanced_4x8.png");
        if(walkImporter!=null)
        {
            walkImporter.textureType=TextureImporterType.Sprite;walkImporter.spriteImportMode=SpriteImportMode.Single;
            walkImporter.isReadable=true;walkImporter.mipmapEnabled=false;walkImporter.filterMode=FilterMode.Point;
            walkImporter.wrapMode=TextureWrapMode.Clamp;walkImporter.textureCompression=TextureImporterCompression.Uncompressed;
            walkImporter.npotScale=TextureImporterNPOTScale.None;walkImporter.maxTextureSize=2048;
            walkImporter.SaveAndReimport();
        }
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/Player/MateoActions_4x8.png",ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/Player/MacuahuitlEquipment.png",ImportAssetOptions.ForceUpdate);
        Directory.CreateDirectory("Assets/Art/EntornoMazahua/Refined");
        Directory.CreateDirectory("Assets/Prefabs/EntornoMazahua/Refined");
        AssetDatabase.Refresh();
        string materialPath="Assets/Art/EntornoMazahua/Refined/Pintura_Mate.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(XunjuuArtMeshes.Material);AssetDatabase.CreateAsset(material,materialPath);}
        string[] models={"Casa_Adobe_Detallada","Casa_Corredor_Detallada","Arbol0","Arbol1","Arbol2","Helecho","Arbusto","Piedra","BordadoOrbital","Casa_Cal_Corredor","Casa_Granero","Casa_Chimenea","Tronco_Caido","Tocon_Raices","Arbusto_Florido","Arbusto_Bayas"};
        foreach(string model in models)
        {
            string path="Assets/Art/EntornoMazahua/Refined/"+model+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=UnityEngine.Object.Instantiate(XunjuuArtMeshes.Get(model));AssetDatabase.CreateAsset(mesh,path);}
            else {EditorUtility.CopySerialized(XunjuuArtMeshes.Get(model),mesh);EditorUtility.SetDirty(mesh);}
            var go=new GameObject(model,typeof(MeshFilter),typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            if(model.StartsWith("Casa")){var c=go.AddComponent<BoxCollider>();c.center=new Vector3(0,2,0);c.size=XunjuuArtMeshes.HouseSize(model);}
            if(model.StartsWith("Arbol")){var c=go.AddComponent<CapsuleCollider>();c.radius=.18f;c.height=2.5f;c.center=Vector3.up*1.25f;}
            string prefabPath="Assets/Prefabs/EntornoMazahua/Refined/"+model+".prefab";
            if(!File.Exists(prefabPath))PrefabUtility.SaveAsPrefabAsset(go,prefabPath);
            if(model=="Tronco_Caido"||model=="Tocon_Raices")
            {
                var contents=PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var c=contents.GetComponent<MeshCollider>();
                    if(c==null)c=contents.AddComponent<MeshCollider>();
                    c.sharedMesh=contents.GetComponent<MeshFilter>().sharedMesh;c.convex=true;c.isTrigger=false;c.enabled=true;
                    PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            UnityEngine.Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        foreach(string name in new[]{"Xunjuu/PaintedWorld","Xunjuu/HighlandSky","Xunjuu/AtlasSprite"})
        {
            Shader shader=Shader.Find(name);Check(shader!=null,"Shader missing: "+name);
            foreach(var error in ShaderUtil.GetShaderMessages(shader))Check(error.severity.ToString()!="Error",name+": "+error.message);
        }
        RenderHouse();
        foreach(string model in new[]{"Casa_Cal_Corredor","Casa_Granero","Casa_Chimenea","Tronco_Caido","Tocon_Raices","Arbusto_Florido","Arbusto_Bayas"})RenderHouse(model,model+".png");
        File.WriteAllText(Output+"/models.txt","PASS: "+models.Length+" editable model prefabs, material and meshes; source scene preserved. "+DateTime.Now.ToString("O"));
    }
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    private static async void RunRuntime()
    {
        bool previousBackground=Application.runInBackground;
        Application.runInBackground=true;
        Application.logMessageReceived+=OnLog;errors.Clear();
        try
        {
            await Task.Delay(3500);
            var director=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();Check(director!=null,"Director missing.");
            director.BeginGameForValidation();
            Check(!director.IsGameplayHudVisible,"HUD leaked at introduction start.");
            await Task.Delay(700);
            // Video decoding and first-time shader compilation vary between machines.
            // Wait for observable state instead of assuming each intro stage lasts 700ms.
            for(int attempt=0;attempt<100 && !director.IsGameplayHudVisible;attempt++)
            { director.SkipIntroForValidation();director.SkipTimelineForValidation();await Task.Delay(200); }
            Check(director.IsGameplayHudVisible,"Gameplay HUD not restored.");
            Check(!RenderSettings.fog,"Fog is enabled.");
            var dressing=UnityEngine.Object.FindFirstObjectByType<XunjuuMeadowDressing>();
            for(int attempt=0;attempt<160 && (dressing==null || dressing.PatchCount<169);attempt++)
            {await Task.Delay(200);dressing=UnityEngine.Object.FindFirstObjectByType<XunjuuMeadowDressing>();}
            Check(dressing!=null&&dressing.PatchCount>=169,"Expanded meadow patches were not built.");
            Check(dressing.Buildings.Count>=4,"Additional houses could not find safe placement.");
            var policy=UnityEngine.Object.FindFirstObjectByType<XunjuuSceneVisualPolicy>();
            if(policy!=null)policy.Refresh();
            int solidWood=0;
            Physics.SyncTransforms();
            foreach(var collider in dressing.GetComponentsInChildren<MeshCollider>())
            {
                if(collider.name!="Tronco_Caido"&&collider.name!="Tocon_Raices")continue;
                Check(collider.enabled&&!collider.isTrigger&&collider.convex,"Wood lost solid collision.");
                Check(!XunjuuSceneVisualPolicy.IsSoftDecoration(collider.transform),"Wood classified as soft flora.");
                Bounds bounds=collider.bounds;
                Check(collider.Raycast(new Ray(bounds.center+Vector3.up*(bounds.extents.y+2),Vector3.down),out _,bounds.size.y+4),"Wood collider has no physical surface.");
                solidWood++;
            }
            Check(solidWood>0,"No grounded solid wood was placed.");
            File.WriteAllText(Output+"/wood-collision.txt","PASS: "+solidWood+" grounded logs/stumps retain convex solid collisions after visual policy refresh. "+DateTime.Now.ToString("O"));
            var flora=Resources.Load<Texture2D>("Sprites/TextureTerrain/FloraExpansion_4x2");
            Check(flora!=null&&flora.isReadable,"Expanded flora not imported.");
            foreach(string model in new[]{"Casa_Cal_Corredor","Casa_Granero","Casa_Chimenea","Tronco_Caido","Tocon_Raices","Arbusto_Florido","Arbusto_Bayas"})
                Check(XunjuuArtMeshes.Get(model).vertexCount>0,"Empty added model: "+model);
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();Check(player!=null,"Player missing.");
            var poses=player.GetComponent<XunjuuCompleteSpriteAnimator>();Check(poses!=null&&poses.HasRefreshedArt,"Refined atlas not imported.");
            Check(poses.HasDirectionalArt,"Directional atlas not imported.");
            Check(!player.HasOrbitalWeapon(),"Player started with a weapon.");
            await XunjuuMotionValidation.CheckOpeningAndScene(player,director,poses);
            var directionField=typeof(PlayerController).GetField("moveDirection",BindingFlags.Instance|BindingFlags.NonPublic);
            Vector3 savedDirection=player.GetMoveDirection();
            var savedFov=Camera.main.fieldOfView;
            var savedRotation=Camera.main.transform.rotation;
            director.enabled=false;
            Camera.main.fieldOfView=24;
            Camera.main.transform.LookAt(player.transform.position+Vector3.up*.65f);
            for(int i=0;i<8;i++)
            {
                float a=i*45*Mathf.Deg2Rad;
                Vector2 screen=new Vector2(-Mathf.Sin(a),-Mathf.Cos(a));
                Check(XunjuuCompleteSpriteAnimator.DirectionIndex(screen)==i,"Direction quantization failed: "+i);
                Vector3 right=Camera.main.transform.right,forward=Camera.main.transform.forward;right.y=0;forward.y=0;
                directionField.SetValue(player,right.normalized*screen.x+forward.normalized*screen.y);
                await Task.Delay(120);
                Check(poses.FacingIndex==i,"Runtime direction failed: "+i+" got "+poses.FacingIndex);
                await XunjuuFrameCapture.Save(player,Output+"/direction-"+i+".png");
            }
            directionField.SetValue(player,savedDirection);Camera.main.fieldOfView=savedFov;
            Camera.main.transform.rotation=savedRotation;director.enabled=true;
            await XunjuuFrameCapture.Save(player,Output+"/gameplay-after.png");
            var inv=player.GetComponent<XunjuuInventory>();Check(inv!=null,"Inventory missing.");
            Sprite corn=Resources.Load<Sprite>("Sprites/Items/maiz");
            Texture2D flowerTexture=Resources.Load<Texture2D>("Sprites/TextureTerrain/flor");
            Sprite flowerIcon=flowerTexture!=null?Sprite.Create(flowerTexture,new Rect(0,0,flowerTexture.width,flowerTexture.height),new Vector2(.5f,.5f),100):null;
            inv.AddItem("qa-maiz","Maíz",7,corn);inv.AddItem("qa-flor","Flor del camino",3,flowerIcon);
            inv.SelectIndex(inv.Items.Count-1);int selected=inv.SelectedIndex;
            var ui=player.GetComponent<XunjuuInventoryUI>();ui.SetVisible(true);await Task.Delay(600);
            Check(ui.IsVisible&&inv.SelectedIndex==selected,"Inventory presentation changed selection.");await XunjuuFrameCapture.Save(player,Output+"/inventory-after.png");ui.SetVisible(false);
            inv.RemoveItem("qa-maiz",7);inv.RemoveItem("qa-flor",3);
            if(flowerIcon!=null)UnityEngine.Object.Destroy(flowerIcon);
            typeof(PlayerController).GetMethod("Jump",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);
            await Task.Delay(200);Check(player.IsJumping(),"Jump state was not entered.");
            await XunjuuFrameCapture.Save(player,Output+"/jump-after.png");
            await Task.Delay(2400);Check(player.IsGrounded(),"Player failed to land after jump.");
            typeof(PlayerController).GetMethod("Attack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);
            await Task.Delay(165);await XunjuuFrameCapture.Save(player,Output+"/attack-after.png");
            await Task.Delay(650);Check(!player.IsAttacking(),"Basic attack did not finish.");
            for(int i=0;i<4;i++)director.CollectMazahuaWord("qa-flower-"+i,"Prueba");
            Check(!player.HasOrbitalWeapon(),"Weapon granted before fifth flower.");
            director.CollectMazahuaWord("qa-flower-4","Prueba");await Task.Delay(400);
            Check(player.HasOrbitalWeapon(),"Fifth flower did not grant weapon.");
            Check(!player.HasOrbitalAttackUnlocked(),"Orbital unlocked before its mission.");
            var weapon=player.GetComponentInChildren<OrbitalWeapon>();Check(weapon!=null,"Reward weapon missing.");
            Check(weapon.GetComponentInChildren<SpriteRenderer>().sprite.name=="Macuahuitl_Equipo_Completo","Full equipment sprite not assigned.");
            await Task.Delay(5000);
            await XunjuuFrameCapture.Save(player,Output+"/weapon-reward-after.png");
            typeof(PlayerController).GetMethod("SwordAttack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);
            Check(weapon.IsBusy,"Equipped attack failed.");await Task.Delay(170);
            await XunjuuFrameCapture.Save(player,Output+"/weapon-attack-after.png");
            Check(Vector3.Distance(weapon.transform.position,poses.HandGripWorld)<.015f,"Weapon grip detached from current frame.");
            await Task.Delay(600);
            Check(weapon.IsAtRest(),"Equipped weapon did not return to rest.");
            Check(weapon.ExecuteOrbitalAttack(),"Orbit could not start.");await Task.Delay(250);await XunjuuFrameCapture.Save(player,Output+"/orbital-after.png");
            Vector3 offset=weapon.transform.position-player.transform.position;
            Check(Mathf.Abs(new Vector2(offset.x,offset.z).magnitude-2.05f)<.05f,"Orbital radius changed.");
            await Task.Delay(1600);Check(!weapon.IsOrbiting(),"Orbit did not end.");
            await XunjuuMotionValidation.CheckAnimals(player);
            await XunjuuMotionValidation.CheckStoryProgression(player,director);
            await XunjuuMotionValidation.CaptureRoof(player,director);
            int originalHealth=player.GetCurrentHealth();player.SetHealth(86);Check(player.GetCurrentHealth()==86,"Health update failed.");player.SetHealth(originalHealth);
            Check(errors.Count==0,string.Join("\n",errors));
            File.WriteAllText(Output+"/runtime-after.txt","PASS: introduction start/HUD; 8 directional pixel-art views; starts unarmed; four flowers unarmed; fifth flower grants weapon, orbital remains locked; equipped attack/recovery; no fog; "+dressing.PatchCount+" meadow patches; "+dressing.Buildings.Count+" houses; inventory stacks/selection; jump/landing; basic attack completion; health update; orbital radius 2.05m, activation and completion.\nCamera: "+Camera.main.transform.position+" FOV "+Camera.main.fieldOfView+"; sky "+RenderSettings.skybox.shader.name+".\n"+DateTime.Now.ToString("O"));
            await Task.Delay(400);
        }
        catch(Exception e){File.WriteAllText(Output+"/runtime-after.txt","FAIL: "+e);Debug.LogException(e);}
        finally{Application.runInBackground=previousBackground;Application.logMessageReceived-=OnLog;SessionState.SetBool(Key,false);EditorApplication.ExitPlaymode();}
    }
    private static void OnLog(string message,string trace,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    private static void Capture(string name)
    {ScreenCapture.CaptureScreenshot(Output+"/"+name);}
    private static void RenderHouse(string model="Casa_Adobe_Detallada",string filename="house-after.png")
    {
        var root=new GameObject("Preview_House");
        var house=XunjuuArtMeshes.Create(model,root.transform);house.layer=31;
        var garden=XunjuuArtMeshes.Create("Arbusto",root.transform);garden.transform.position=new Vector3(-4.1f,.1f,-1.5f);garden.layer=31;
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.layer=31;ground.transform.SetParent(root.transform);ground.transform.position=new Vector3(0,-.17f,0);ground.transform.localScale=new Vector3(16,.2f,14);
        var groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMaterial.color=new Color(.49f,.44f,.29f);ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
        var cameraObject=new GameObject("Preview_Camera",typeof(Camera));cameraObject.transform.SetParent(root.transform);
        var camera=cameraObject.GetComponent<Camera>();camera.cullingMask=1<<31;camera.transform.position=new Vector3(-11,8,-14);camera.transform.LookAt(new Vector3(0,2,0));camera.fieldOfView=39;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.70f,.77f,.70f);
        if(!model.StartsWith("Casa"))
        {
            garden.SetActive(false);ground.transform.localScale=new Vector3(6,.2f,6);
            Bounds bounds=house.GetComponent<Renderer>().bounds;float extent=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
            camera.orthographic=true;camera.orthographicSize=extent*.75f;camera.transform.position=bounds.center+new Vector3(-extent,extent*.8f,-extent);camera.transform.LookAt(bounds.center);
        }
        var lightObject=new GameObject("Preview_Sun",typeof(Light));lightObject.transform.SetParent(root.transform);
        var sun=lightObject.GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.05f;sun.transform.rotation=Quaternion.Euler(45,-25,0);sun.color=new Color(1,.94f,.81f);sun.cullingMask=1<<31;
        var rt=new RenderTexture(1400,1000,24);camera.targetTexture=rt;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();
        File.WriteAllBytes(Output+"/"+filename,image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(groundMaterial);
    }
}
