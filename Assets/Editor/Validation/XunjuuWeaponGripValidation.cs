using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Focused, reversible Play Mode check. Never saves or rebuilds scene assets.
[InitializeOnLoad]
public static class XunjuuWeaponGripValidation
{
    private const string Flag = "Temp/XunjuuWeaponGrip.flag";
    private const string Key = "Xunjuu.WeaponGrip.Pending";
    private const string Output = "output/weapon-grip-review";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static XunjuuWeaponGripValidation()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) Run();
        };
    }

    private static void Poll()
    {
        if (!File.Exists(Flag) || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Flag);
        Start();
    }

    [MenuItem("Tools/Xunjuu/Validacion/Comprobar agarre y guia")]
    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, Private).SetValue(target, value);

    private static T Get<T>(object target, string field)
        => (T)target.GetType().GetField(field, Private).GetValue(target);

    private static void Call(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, Private).Invoke(target, args);

    private static async void Run()
    {
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var report = new StringBuilder();
        GameObject previewRoot = null;
        try
        {
            await Task.Delay(1800);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            DyanatroGameDirector director = Object.FindFirstObjectByType<DyanatroGameDirector>();
            Check(player != null && director != null, "Missing gameplay scene.");
            var poses = player.GetComponent<XunjuuCompleteSpriteAnimator>();
            Check(poses != null && poses.HasDirectionalArt && poses.HasSideWalkCorrection, "Missing directional atlases.");
            player.enabled = false;
            poses.enabled = false;
            director.enabled = false;
            Time.timeScale = 1f;
            player.GetComponent<Rigidbody>().isKinematic = true;
            player.GetComponent<Animator>().enabled = false;
            player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            player.GetComponent<Rigidbody>().position = Vector3.zero;
            Physics.SyncTransforms();
            player.transform.localScale = Vector3.one;
            player.gameObject.layer = 31;
            player.RemoveEquippedWeapon();
            await Task.Delay(30);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Weapons/SwordFloating.prefab");
            Check(prefab != null, "Missing weapon prefab.");
            var weaponObject = Object.Instantiate(prefab, player.transform);
            var pickup = weaponObject.GetComponent<SwordPickupItem>();
            if (pickup != null) Object.Destroy(pickup);
            var floating = weaponObject.GetComponent<FloatingSword>();
            if (floating != null) Object.Destroy(floating);
            foreach (var collider in weaponObject.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var weapon = weaponObject.GetComponent<OrbitalWeapon>();
            weapon.Initialize(player.transform);
            player.SetOrbitalWeapon(weapon, false);
            weapon.enabled = false;
            foreach (Transform child in weaponObject.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
            SpriteRenderer actor = player.GetComponent<SpriteRenderer>();
            actor.enabled = true;
            actor.forceRenderingOff = false;
            actor.color = Color.white;
            actor.sortingOrder = 12000;
            var sorter = actor.GetComponent<DyanatroSpriteDepthSorter>();
            if (sorter != null) sorter.enabled = false;
            SpriteRenderer blade = weaponObject.GetComponent<SpriteRenderer>();
            SpriteRenderer fingers = weaponObject.transform.Find("Macuahuitl_Mano_Sujecion").GetComponent<SpriteRenderer>();
            Check(Mathf.Abs(blade.sprite.rect.height / blade.sprite.pixelsPerUnit / XunjuuWorldScale.PlayerLocalHeight - .62f) < .001f,
                "Weapon height must remain proportional to character.");

            previewRoot = new GameObject("QA_Agarre_Preview");
            Camera camera = new GameObject("QA_Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(previewRoot.transform);
            camera.transform.position = new Vector3(0, .32f, -10);
            camera.orthographic = true;
            camera.orthographicSize = 1.12f;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f, .16f, .18f);
            camera.enabled = false;

            var walk = Get<Sprite[]>(poses, "balancedWalk");
            var idle = Get<Sprite[]>(poses, "directionalFrames");
            var actions = Get<Sprite[]>(poses, "directionalActions");
            int[] idleRows = {0,7,2,5,4,5,6,7};
            int[] walkRows = {0,7,2,5,4,5,2,7};
            for (int direction = 0; direction < 8; direction++)
            {
                Set(poses, "<FacingIndex>k__BackingField", direction);
                for (int mode = 0; mode < 3; mode++)
                {
                    int frames = mode == 0 ? 1 : 4;
                    for (int frame = 0; frame < frames; frame++)
                    {
                        bool walking = mode == 1;
                        actor.sprite = walking ? walk[walkRows[direction] * 4 + frame]
                            : mode == 2 ? actions[idleRows[direction] * 4 + frame] : idle[idleRows[direction] * 4];
                        actor.sharedMaterial = Get<Material>(poses, "directionalMaterial");
                        actor.flipX = direction == 1 || direction == 3 || (walking && direction == 6);
                        Call(weapon, "ApplyHandGrip");
                        Check(poses.HandGripSprite != null, "No visible hand at " + actor.sprite.name);
                        Check(Vector3.Distance(blade.transform.position, poses.HandGripWorld) < .0001f, "Grip detached from " + actor.sprite.name);
                        Check(Vector3.Distance(fingers.transform.position, poses.HandGripWorld) < .0001f, "Finger crop detached.");
                        Check(fingers.sortingOrder > blade.sortingOrder && fingers.sortingOrder > actor.sortingOrder,
                            "Handle hides fingers.");
                        Check(blade.sortingOrder < actor.sortingOrder, "Resting blade covers character's face.");
                        Check(fingers.flipX == actor.flipX && Quaternion.Angle(fingers.transform.rotation, actor.transform.rotation) < .01f,
                            "Finger patch rotated/mirrored independently of actor.");
                        string name = $"direction-{direction}-{(mode == 0 ? "idle" : walking ? "walk" : "action")}-{frame}";
                        await Task.Delay(30);
                        Capture(camera, Output + "/" + name + ".png", 512, 512);
                        report.AppendLine("PASS " + name + " | " + actor.sprite.name);
                    }
                }
            }

            Set(poses, "<FacingIndex>k__BackingField", 6);
            actor.sprite = idle[6 * 4]; actor.flipX = false;
            weapon.enabled = true;
            report.AppendLine("Render state: enabled=" + actor.enabled + ", active=" + actor.gameObject.activeInHierarchy
                + ", bounds=" + actor.bounds + ", material=" + actor.sharedMaterial.name + ", position=" + actor.transform.position);
            Check(weapon.ExecuteAttack(), "Equipped attack did not start.");
            await Task.Delay(150);
            Check(Vector3.Distance(blade.transform.position, poses.HandGripWorld) < .001f, "Attack detached handle.");
            Capture(camera, Output + "/attack.png", 512, 512);
            await Task.Delay(550);
            Check(weapon.IsAtRest(), "Attack did not recover to hand.");
            Check(weapon.ExecuteOrbitalAttack(), "Special attack failed.");
            await Task.Delay(150);
            Check(!fingers.enabled && !blade.enabled, "Hand copy remains during orbit.");
            await Task.Delay(1500);
            Check(!weapon.IsOrbiting() && weapon.IsAtRest() && fingers.enabled, "Orbit failed to restore grip.");
            report.AppendLine("PASS real attack/recovery, orbital hiding and grip restoration.");

            RectTransform arrow = Get<RectTransform>(director, "guideArrowRect");
            var guide = Get<GameObject>(director, "collectibleGuideRoot");
            Check(arrow.sizeDelta == new Vector2(28,28), "Guide arrow oversized.");
            Check(guide.GetComponent<RectTransform>().anchorMin == new Vector2(.5f,0), "Guide not at screen edge.");
            Check(!arrow.GetComponent<Image>().raycastTarget && arrow.GetComponent<Outline>() == null, "Guide intercepts input/has heavy outline.");
            Call(director, "SetCollectibleGuideVisible", false);
            Check(!guide.activeSelf, "Guide background visible when guide is hidden.");
            Call(director, "SetCollectibleGuideVisible", true);
            Get<Text>(director, "guideDistanceText").text = "Dyanatr’o: 54 m";

            Canvas canvas = new GameObject("QA_HUD", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.transform.SetParent(previewRoot.transform);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
            canvas.planeDistance = 2; canvas.gameObject.layer = 31;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080);
            var guideCopy = Object.Instantiate(guide, canvas.transform, false);
            foreach (Transform child in guideCopy.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
            Canvas.ForceUpdateCanvases();
            Capture(camera, Output + "/weapon-and-guide.png", 960, 540);
            report.AppendLine("PASS small fixed bottom-edge chevron, no input interception, whole-widget visibility.");
            report.AppendLine(DateTime.Now.ToString("O"));
            File.WriteAllText(Output + "/validation.txt", report.ToString());
            Debug.Log("Weapon/guide QA: PASS (8 orientations, idle/walk/actions, attack/orbit recovery).");
        }
        catch (Exception error)
        {
            File.WriteAllText(Output + "/validation.txt", report + "\nFAIL: " + error);
            Debug.LogException(error);
        }
        finally
        {
            if (previewRoot != null) Object.Destroy(previewRoot);
            Application.runInBackground = background;
            SessionState.SetBool(Key, false);
            EditorApplication.ExitPlaymode();
        }
    }

    private static void Capture(Camera camera, string path, int width, int height)
    {
        var target = RenderTexture.GetTemporary(width, height, 24);
        RenderTexture previous = RenderTexture.active;
        RenderTexture savedTarget = camera.targetTexture;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = savedTarget; RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target); Object.Destroy(image);
        }
    }
}
