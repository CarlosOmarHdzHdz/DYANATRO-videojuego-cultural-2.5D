using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Constructor del jefe Ocelotl
// Acción: cortar sprites, crear tres estados y generar el prefab completo.
// ============================================================================
public static class XunjuuOcelotlPrefabBuilder
{
    private const string WalkTexturePath = "Assets/Art/Ocelotl/Ocelotl-walk.png";
    private const string AttackTexturePath = "Assets/Art/Ocelotl/Ocelotl-attack-v1.png";
    private const string AnimationFolder = "Assets/Animations/Ocelotl";
    private const string IdleClipPath = AnimationFolder + "/Ocelotl_Descanso.anim";
    private const string WalkClipPath = AnimationFolder + "/Ocelotl_Caminar.anim";
    private const string AttackClipPath = AnimationFolder + "/Ocelotl_Ataque.anim";
    private const string ControllerPath = AnimationFolder + "/Ocelotl.controller";
    private const string PrefabPath = "Assets/Prefabs/Ocelotl.prefab";

    [MenuItem("Xunjuú v0.1/Jefe Ocelotl/Crear o actualizar prefab completo")]
    public static void BuildOcelotlPrefab()
    {
        EnsureFolderPath(AnimationFolder);
        EnsureFolderPath("Assets/Prefabs");
        ConfigureSpriteSheet(WalkTexturePath, "Ocelotl_Walk");
        ConfigureSpriteSheet(AttackTexturePath, "Ocelotl_Attack");

        Sprite[] walkFrames = LoadFrames(WalkTexturePath);
        Sprite[] attackFrames = LoadFrames(AttackTexturePath);
        if (walkFrames.Length != 25 || attackFrames.Length != 25)
        {
            Debug.LogError($"Xunjuú v0.1: Ocelotl requiere 25 cuadros por hoja. Caminar={walkFrames.Length}, Ataque={attackFrames.Length}.");
            return;
        }

        AnimationClip idleClip = CreateOrUpdateClip(IdleClipPath, walkFrames, 3f, true);
        AnimationClip walkClip = CreateOrUpdateClip(WalkClipPath, walkFrames, 10f, true);
        AnimationClip attackClip = CreateOrUpdateClip(AttackClipPath, attackFrames, 18f, false);
        AnimatorController controller = CreateOrUpdateController(idleClip, walkClip, attackClip);
        CreateOrUpdatePrefab(walkFrames[0], controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("[XUNJUU OCELOTL PASS] Prefab Ocelotl creado con Descanso, Caminar, Ataque, jefe agresivo y barra de vida.");
    }

    // ACCIÓN: importar cada hoja como 25 sprites ordenados de izquierda a derecha.
    private static void ConfigureSpriteSheet(string assetPath, string framePrefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("No se pudo importar " + assetPath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        SpriteMetaData[] sprites = new SpriteMetaData[25];
        const int cellSize = 256;
        for (int index = 0; index < sprites.Length; index++)
        {
            int column = index % 5;
            int rowFromTop = index / 5;
            sprites[index] = new SpriteMetaData
            {
                name = $"{framePrefix}_{index:00}",
                rect = new Rect(column * cellSize, 1280 - ((rowFromTop + 1) * cellSize), cellSize, cellSize),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            };
        }

#pragma warning disable CS0618
        importer.spritesheet = sprites;
#pragma warning restore CS0618
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadFrames(string assetPath)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();
    }

    // ACCIÓN: reutilizar Caminar a menor velocidad para producir el estado Descanso.
    private static AnimationClip CreateOrUpdateClip(string path, Sprite[] frames, float frameRate, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = System.IO.Path.GetFileNameWithoutExtension(path);
        clip.frameRate = frameRate;
        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int index = 0; index < frames.Length; index++)
        {
            keys[index] = new ObjectReferenceKeyframe
            {
                time = index / frameRate,
                value = frames[index]
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    // ACCIÓN: conectar Descanso, Caminar y Ataque con Speed, Attack y Charge.
    private static AnimatorController CreateOrUpdateController(AnimationClip idle, AnimationClip walk, AnimationClip attack)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        foreach (AnimatorControllerParameter parameter in controller.parameters.ToArray())
            controller.RemoveParameter(parameter);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            stateMachine.RemoveState(child.state);
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
            stateMachine.RemoveAnyStateTransition(transition);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Charge", AnimatorControllerParameterType.Trigger);

        AnimatorState idleState = stateMachine.AddState("Descanso", new Vector3(230f, 120f));
        AnimatorState walkState = stateMachine.AddState("Caminar", new Vector3(470f, 120f));
        AnimatorState attackState = stateMachine.AddState("Ataque", new Vector3(350f, 300f));
        idleState.motion = idle;
        walkState.motion = walk;
        attackState.motion = attack;
        stateMachine.defaultState = idleState;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        ConfigureConditionTransition(idleToWalk, AnimatorConditionMode.Greater, 0.1f, "Speed");
        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        ConfigureConditionTransition(walkToIdle, AnimatorConditionMode.Less, 0.1f, "Speed");

        AnimatorStateTransition attackTransition = stateMachine.AddAnyStateTransition(attackState);
        ConfigureTriggerTransition(attackTransition, "Attack");
        AnimatorStateTransition chargeTransition = stateMachine.AddAnyStateTransition(attackState);
        ConfigureTriggerTransition(chargeTransition, "Charge");

        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.92f;
        attackToIdle.duration = 0.06f;
        attackToIdle.hasFixedDuration = true;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void ConfigureConditionTransition(AnimatorStateTransition transition, AnimatorConditionMode mode, float threshold, string parameter)
    {
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.hasFixedDuration = true;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void ConfigureTriggerTransition(AnimatorStateTransition transition, string parameter)
    {
        transition.hasExitTime = false;
        transition.duration = 0.03f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    // ACCIÓN: montar los componentes físicos, visuales y de combate en Ocelotl.prefab.
    private static void CreateOrUpdatePrefab(Sprite initialSprite, RuntimeAnimatorController controller)
    {
        GameObject root = new GameObject("Ocelotl");
        // Xunjuu v0.1 - ACCION: dar al jefe una silueta claramente mayor
        // que la fauna y los enemigos comunes del nivel.
        root.transform.localScale = Vector3.one * 3.5f;
        try
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = initialSprite;
            renderer.sortingOrder = 20;

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.mass = 2.4f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.78f, 0f);
            collider.radius = 0.5f;
            collider.height = 1.65f;

            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.3f;

            XunjuuBossNivel1 boss = root.AddComponent<XunjuuBossNivel1>();
            SerializedObject serializedBoss = new SerializedObject(boss);
            serializedBoss.FindProperty("bossName").stringValue = "Gran Dyanatr'o / Ocelotl";
            serializedBoss.FindProperty("maxHealth").intValue = 900;
            serializedBoss.FindProperty("detectionRange").floatValue = 24f;
            serializedBoss.FindProperty("meleeRange").floatValue = 2.4f;
            serializedBoss.FindProperty("meleeHitRange").floatValue = 1.7f;
            serializedBoss.FindProperty("chaseSpeed").floatValue = 4.25f;
            serializedBoss.FindProperty("meleeCooldown").floatValue = 1.75f;
            serializedBoss.FindProperty("meleeImpactDelay").floatValue = 0.48f;
            serializedBoss.FindProperty("meleeAnimationDuration").floatValue = 1.38f;
            serializedBoss.FindProperty("meleeDamage").intValue = 16;
            serializedBoss.FindProperty("chargeCooldown").floatValue = 5.5f;
            serializedBoss.FindProperty("chargeWindup").floatValue = 0.28f;
            serializedBoss.FindProperty("chargeDamage").intValue = 28;
            serializedBoss.FindProperty("chargeHitRange").floatValue = 1.25f;
            serializedBoss.FindProperty("footstepInterval").floatValue = 0.58f;
            serializedBoss.FindProperty("healthBarOffset").vector3Value = new Vector3(0f, 2.35f, 0f);
            serializedBoss.FindProperty("animator").objectReferenceValue = animator;
            serializedBoss.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serializedBoss.ApplyModifiedPropertiesWithoutUndo();

            DyanatroSpriteDepthSorter sorter = root.AddComponent<DyanatroSpriteDepthSorter>();
            sorter.Configure(20, 10f);

            XunjuuWorldLabel label = root.AddComponent<XunjuuWorldLabel>();
            label.Configure("Mazahua: Gran Dyanatr'o\nEspanol: Ocelotl", true, new Vector3(0f, 2.75f, 0f));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ACCION: crear de forma segura cada nivel de una ruta de carpetas.
    private static void EnsureFolderPath(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string nextPath = currentPath + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, parts[index]);

            currentPath = nextPath;
        }
    }
}
