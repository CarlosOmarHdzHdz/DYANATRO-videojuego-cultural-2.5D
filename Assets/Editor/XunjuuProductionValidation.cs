using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class XunjuuProductionValidation
{
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Fields).SetValue(owner, value);
    private static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethod(name, Fields).Invoke(owner, values);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static void RunAll()
    {
        XunjuuProductionSetup.Run();
        Run();
        XunjuuHealthVisualValidation.Run();
    }

    public static void Run()
    {
        Directory.CreateDirectory("output/production-validation");
        var owned = new List<GameObject>();
        try
        {
            var actor = new GameObject("PlayerTest", typeof(CapsuleCollider), typeof(SpriteRenderer), typeof(Rigidbody));
            owned.Add(actor);
            var player = actor.AddComponent<PlayerController>();
            Call(player, "Awake");
            Check(player.GetCurrentHealth() == 100, "Initial health must be 100.");
            var child = new GameObject("PlayerTaggedChild"); child.transform.SetParent(actor.transform); owned.Add(child);
            var bossObject = new GameObject("BossTest", typeof(CapsuleCollider)); owned.Add(bossObject);
            var boss = bossObject.AddComponent<XunjuuBossNivel1>();
            Set(boss,"player",child.transform); Set(boss,"isAttacking",true); Set(boss,"meleeRange",2.4f);
            actor.transform.position = Vector3.forward * 2.3f;
            Check((bool)Call(boss,"CanHitPlayer",1.7f,Vector3.forward,0.05f), "Boss misses at attack initiation distance.");
            Call(boss,"DamagePlayer",14);
            Check(player.GetCurrentHealth()==86,"Boss failed to damage nested player.");
            actor.transform.position = Vector3.forward * 10f;
            Check(!(bool)Call(boss,"CanHitPlayer",1.7f,Vector3.forward,0.05f),"Boss hit outside reach.");
            player.SetHealth(999); Check(player.GetCurrentHealth()==100,"Health clamp failed.");

            Texture2D walk = Resources.Load<Texture2D>("Sprites/Player/MacuahuitlWalk_8x2");
            Check(walk!=null && walk.isReadable,"Missing readable walk sheet.");
            Color32[] pixels=walk.GetPixels32();
            int transparent=0, opaque=0;
            foreach(Color32 pixel in pixels) { if(pixel.a==0) transparent++; if(pixel.a>200) opaque++; }
            Check(pixels[0].a==0 && transparent>pixels.Length/3 && opaque>1000,"Walk alpha invalid.");
            var sheetAnimator=actor.AddComponent<XunjuuCompleteSpriteAnimator>();
            Call(sheetAnimator,"Awake");
            Sprite[] frames=(Sprite[])typeof(XunjuuCompleteSpriteAnimator).GetField("walkFrames",Fields).GetValue(sheetAnimator);
            Check(frames.Length==16,"Walk frames missing.");

            var weaponObject=new GameObject("WeaponTest",typeof(SpriteRenderer)); owned.Add(weaponObject);
            weaponObject.transform.SetParent(actor.transform);
            var weapon=weaponObject.AddComponent<OrbitalWeapon>();
            Set(weapon,"player",actor.transform); Set(weapon,"orbitalTimer",100f); Set(weapon,"isOrbiting",true);
            Set(weapon,"equippedScale",Vector3.one);
            for(int i=0;i<8;i++)
            {
                actor.transform.rotation=Quaternion.Euler(0,i*45f,0);
                actor.transform.localScale=Vector3.one*(1+i*0.1f);
                actor.GetComponent<SpriteRenderer>().flipX=i%2==0;
                Set(weapon,"currentAngle",i*45f);
                Call(weapon,"UpdateOrbitasword");
                Vector3 delta=weapon.transform.position-actor.transform.position;
                Check(Mathf.Abs(new Vector2(delta.x,delta.z).magnitude-2.05f)<0.001f,"Orbital radius changes with player transform.");
            }

            var directorObject=new GameObject("DirectorTest"); owned.Add(directorObject);
            var director=directorObject.AddComponent<DyanatroGameDirector>();
            var hud=new GameObject("HUDTest",typeof(RectTransform)); owned.Add(hud); hud.SetActive(false);
            Set(director,"hudRoot",hud); Set(director,"inPrologue",true); Set(director,"gameStarted",true);
            Check(!director.IsGameplayHudVisible,"HUD visible in prologue.");
            Call(director,"FinishPrologue");
            Check(hud.activeSelf && director.IsGameplayHudVisible,"HUD not restored after prologue.");

            var catalog=Resources.Load<XunjuuEnvironmentCatalog>("EnvironmentCatalog");
            Check(catalog!=null && catalog.trees.Length==3 && catalog.flower!=null && catalog.corn!=null,"Environment catalog incomplete.");
            foreach(GameObject prefab in catalog.trees)
                Check(prefab.GetComponentInChildren<MeshRenderer>()!=null && prefab.GetComponent<Collider>()!=null,"Tree mesh/collider absent.");
            File.WriteAllText("output/production-validation/tests.txt","PASS: health initialization/clamp; boss hit at 2.3m and miss at 10m; nested player damage 100 -> 86; 16 walk frames and true alpha; constant 2.05m orbit across 8 rotations/scales/flips; prologue HUD restoration; 3D tree meshes/colliders and plant prefabs.\n"+DateTime.Now.ToString("O"));
            RenderModels(catalog);
        }
        finally { foreach(GameObject item in owned) if(item!=null) UnityEngine.Object.DestroyImmediate(item); }
    }

    private static void RenderModels(XunjuuEnvironmentCatalog catalog)
    {
        var preview=new PreviewRenderUtility();
        var root=new GameObject("EnvironmentPreview");
        try
        {
            GameObject[] prefabs={catalog.houses[0],catalog.houses[1],catalog.trees[0],catalog.trees[1],catalog.trees[2],catalog.rocks[0],catalog.corn,catalog.flower};
            Vector3[] positions={new Vector3(-6,0,2),new Vector3(2,0,4),new Vector3(-7,0,-3),new Vector3(-2,0,-3),new Vector3(4,0,-3),new Vector3(8,0,-3),new Vector3(1,0,-6),new Vector3(3,0,-6)};
            for(int i=0;i<prefabs.Length;i++) UnityEngine.Object.Instantiate(prefabs[i],positions[i],Quaternion.identity,root.transform);
            preview.AddSingleGO(root);
            preview.camera.transform.position=new Vector3(28,24,-42);
            preview.camera.transform.LookAt(new Vector3(0,2,0));
            preview.camera.nearClipPlane=0.1f;
            preview.camera.farClipPlane=150f;
            preview.camera.backgroundColor=new Color(0.68f,0.79f,0.83f);
            preview.camera.clearFlags=CameraClearFlags.SolidColor;
            preview.lights[0].intensity=1.1f;
            preview.lights[0].transform.rotation=Quaternion.Euler(45,-30,0);
            preview.lights[1].intensity=0.65f;
            preview.BeginPreview(new Rect(0,0,1600,900),GUIStyle.none);
            preview.Render(true);
            Texture texture=preview.EndPreview();
            var rt=RenderTexture.GetTemporary(1600,900,0);
            Graphics.Blit(texture,rt);
            RenderTexture previous=RenderTexture.active;
            RenderTexture.active=rt;
            var png=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            png.ReadPixels(new Rect(0,0,1600,900),0,0); png.Apply();
            Color32[] pixels=png.GetPixels32();
            Color32 background=pixels[0];
            int changed=0;
            foreach(Color32 pixel in pixels)
                if(Mathf.Abs(pixel.r-background.r)+Mathf.Abs(pixel.g-background.g)+Mathf.Abs(pixel.b-background.b)>25) changed++;
            Check(changed>pixels.Length/100,"Environment preview is blank.");
            File.WriteAllBytes("output/production-validation/environment.png",png.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(png);
            RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt);
            foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find("Xunjuu/FoliageDither")))
                Check(message.severity.ToString()!="Error","Foliage shader: "+message.message);
        }
        finally { preview.Cleanup(); if(root!=null) UnityEngine.Object.DestroyImmediate(root); }
    }
}
