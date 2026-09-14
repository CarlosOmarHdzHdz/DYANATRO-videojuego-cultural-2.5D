using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Constructor de animales editables
// Accion: importar los sprites del paquete y crear prefabs listos para palabras.
// ============================================================================
public static class XunjuuAnimalPrefabBuilder
{
    private const string ArtFolder = "Assets/Art/Animals";
    private const string AnimationFolder = "Assets/Animation/AnimalesNuevos";
    private const string PrefabFolder = "Assets/Resources/Prefabs/Animals";

    [MenuItem("Xunjuu v0.1/Animales/Crear prefabs de animales nuevos %&b")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/Animation", "AnimalesNuevos");
        EnsureFolder("Assets/Resources/Prefabs", "Animals");

        BuildAnimal("Pato", ArtFolder + "/Pato.png", 6, 1, 90, 90, 6, 1.75f, new Vector3(0f, 0.38f, 0f), new Vector3(0.82f, 0.72f, 0.55f));
        BuildAnimal("Venado", ArtFolder + "/VenadoCaminar.png", 5, 5, 256, 256, 5, 1.9f, new Vector3(0f, 0.95f, 0f), new Vector3(1.05f, 1.85f, 0.72f));
        BuildAnimal("Zorro", ArtFolder + "/ZorroCaminarSaltar.png", 5, 5, 256, 256, 5, 0.82f, new Vector3(0f, 0.72f, 0f), new Vector3(1.18f, 1.35f, 0.68f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Venado.prefab");
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("[XUNJUU ANIMALES PASS] Prefabs Pato, Venado y Zorro creados con animacion, fisica y comportamiento.");
    }

    private static void BuildAnimal(
        string animalName,
        string texturePath,
        int columns,
        int rows,
        int cellWidth,
        int cellHeight,
        int movementFrameCount,
        float scale,
        Vector3 colliderCenter,
        Vector3 colliderSize)
    {
        ConfigureSpriteSheet(texturePath, animalName, columns, rows, cellWidth, cellHeight);
        Sprite[] frames = LoadFrames(texturePath);
        if (frames.Length == 0)
            throw new InvalidOperationException("No se encontraron cuadros para " + animalName);

        // Xunjuu v0.1 - ACCION: usar una sola fila coherente de movimiento.
        // Las hojas grandes contienen varias filas; reproducir las 25 celdas
        // seguidas hacia que el cuerpo saltara y cambiara de postura.
        Sprite[] movementFrames = frames.Take(Mathf.Clamp(movementFrameCount, 1, frames.Length)).ToArray();
        Sprite[] idleFrames = movementFrames.Take(Mathf.Min(2, movementFrames.Length)).ToArray();
        AnimationClip idle = CreateClip(AnimationFolder + "/" + animalName + "_Idle.anim", idleFrames, 1.4f);
        AnimationClip walk = CreateClip(AnimationFolder + "/" + animalName + "_Walk.anim", movementFrames, 7f);
        AnimationClip run = CreateClip(AnimationFolder + "/" + animalName + "_Run.anim", movementFrames, 11f);
        AnimatorController controller = CreateController(AnimationFolder + "/" + animalName + ".controller", idle, walk, run);
        CreatePrefab(animalName, frames[0], controller, scale, colliderCenter, colliderSize);
    }

    // Xunjuu v0.1 - ACCION: cortar hojas horizontales o cuadriculadas desde su borde superior.
    private static void ConfigureSpriteSheet(string assetPath, string prefix, int columns, int rows, int cellWidth, int cellHeight)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
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

        SpriteMetaData[] metadata = new SpriteMetaData[columns * rows];
        for (int index = 0; index < metadata.Length; index++)
        {
            int column = index % columns;
            int rowFromTop = index / columns;
            metadata[index] = new SpriteMetaData
            {
                name = prefix + "_" + index.ToString("00"),
                rect = new Rect(column * cellWidth, rows * cellHeight - ((rowFromTop + 1) * cellHeight), cellWidth, cellHeight),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            };
        }

#pragma warning disable CS0618
        importer.spritesheet = metadata;
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

    private static AnimationClip CreateClip(string path, Sprite[] frames, float frameRate)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = frameRate;
        EditorCurveBinding binding = new EditorCurveBinding { path = string.Empty, type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
        ObjectReferenceKeyframe[] keys = frames.Select((frame, index) => new ObjectReferenceKeyframe { time = index / frameRate, value = frame }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateController(string path, AnimationClip idle, AnimationClip walk, AnimationClip run)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states.ToArray())
            machine.RemoveState(child.state);

        AnimatorState idleState = machine.AddState("Idle", new Vector3(220f, 100f));
        AnimatorState walkState = machine.AddState("Walk", new Vector3(430f, 100f));
        AnimatorState runState = machine.AddState("Run", new Vector3(430f, 230f));
        idleState.motion = idle;
        walkState.motion = walk;
        runState.motion = run;
        machine.defaultState = idleState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    // Xunjuu v0.1 - ACCION: crear un prefab remoto asignable desde cualquier palabra.
    private static void CreatePrefab(string animalName, Sprite initialSprite, RuntimeAnimatorController controller, float scale, Vector3 colliderCenter, Vector3 colliderSize)
    {
        GameObject root = new GameObject(animalName);
        try
        {
            root.transform.localScale = Vector3.one * scale;
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = initialSprite;
            renderer.sortingOrder = 710;

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = colliderCenter;
            collider.size = colliderSize;

            Animal animal = root.AddComponent<Animal>();
            animal.canFlee = true;
            if (animalName == "Pato")
                animal.ConfigureSpecies(Animal.AnimalSpecies.Duck);
            else if (animalName == "Venado")
                animal.ConfigureSpecies(Animal.AnimalSpecies.Deer);

            // Xunjuu v0.1 - ACCION: Pato y Venado participan en el nivel 2.
            // MODIFICACION: su vida queda disponible dentro de cada prefab.
            if (animalName == "Pato" || animalName == "Venado")
            {
                XunjuuAnimalHealth health = root.AddComponent<XunjuuAnimalHealth>();
                health.ConfigureMaxHealth(animalName == "Venado" ? 84 : 56);
            }

            // Xunjuu v0.1 - ACCION: mantener patas y pezuñas sobre el Terrain.
            root.AddComponent<XunjuuTerrainGrounding>();

            root.AddComponent<CloudBillboard>();
            DyanatroSpriteDepthSorter sorter = root.AddComponent<DyanatroSpriteDepthSorter>();
            sorter.Configure(710, 10f);

            XunjuuWorldLabel label = root.AddComponent<XunjuuWorldLabel>();
            string bilingualName = animalName == "Pato"
                ? "Mazahua: tizi\nEspañol: pato"
                : animalName == "Venado"
                    ? "Mazahua: pjantr'eje\nEspañol: venado"
                    : "Español: " + animalName;
            label.Configure(bilingualName, animalName == "Pato" || animalName == "Venado", colliderCenter + Vector3.up * 1.15f);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + animalName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
