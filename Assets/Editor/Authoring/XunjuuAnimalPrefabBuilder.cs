using System;
using System.Linq;
using System.Collections.Generic;
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
    private const string AnimationFolder = "Assets/Animations/Legacy/AnimalesNuevos";
    private const string PrefabFolder = "Assets/Resources/Prefabs/Animals";
    private const string RegionalFaunaAtlas = "Assets/Resources/Sprites/Animals/FaunaMazahua_3x2.png";

    [MenuItem("Xunjuu v0.1/Animales/Crear prefabs de animales nuevos %&b")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/Animations/Legacy", "AnimalesNuevos");
        EnsureFolder("Assets/Resources/Prefabs", "Animals");

        BuildAnimal("Venado", ArtFolder + "/VenadoCaminar.png", 5, 5, 256, 256, 5, 1.9f, new Vector3(0f, 0.95f, 0f), new Vector3(1.05f, 1.85f, 0.72f));
        BuildAnimal("Zorro", ArtFolder + "/ZorroCaminarSaltar.png", 5, 5, 256, 256, 5, 0.82f, new Vector3(0f, 0.72f, 0f), new Vector3(1.18f, 1.35f, 0.68f));
        BuildRegionalFauna();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Venado.prefab");
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("[XUNJUU ANIMALES PASS] Venado, Zorro y seis especies regionales capturables creadas; Pato queda solo como recurso legado.");
    }

    [MenuItem("Xunjuu v0.1/Animales/Crear fauna regional capturable")]
    public static void BuildRegionalFauna()
    {
        EnsureFolder("Assets/Resources", "Sprites");
        EnsureFolder("Assets/Resources/Sprites", "Animals");
        EnsureFolder("Assets/Resources/Prefabs", "Animals");
        ConfigureRegionalAtlas();

        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(RegionalFaunaAtlas)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, sprite => sprite, StringComparer.Ordinal);

        foreach (XunjuuFaunaCatalog.Entry entry in XunjuuFaunaCatalog.Entries)
        {
            string spriteName = "Fauna_" + entry.Id;
            if (!sprites.TryGetValue(spriteName, out Sprite sprite))
                throw new InvalidOperationException("No se encontro " + spriteName + " en " + RegionalFaunaAtlas);
            BuildStaticFaunaPrefab(entry, sprite);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[XUNJUU FAUNA PASS] Seis especies regionales capturables creadas sin Pato.");
    }

    private static void ConfigureRegionalAtlas()
    {
        AssetDatabase.ImportAsset(RegionalFaunaAtlas, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(RegionalFaunaAtlas) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("No se pudo importar " + RegionalFaunaAtlas);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 120f;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        string[] ids = { "venado_cola_blanca", "conejo_serrano", "coyote", "zorra_gris", "tlacuache", "ardilla_gris" };
        SpriteMetaData[] metadata = new SpriteMetaData[ids.Length];
        const int cellWidth = 512;
        const int cellHeight = 512;
        for (int index = 0; index < ids.Length; index++)
        {
            int column = index % 3;
            int rowFromTop = index / 3;
            metadata[index] = new SpriteMetaData
            {
                name = "Fauna_" + ids[index],
                rect = new Rect(column * cellWidth, 1024 - ((rowFromTop + 1) * cellHeight), cellWidth, cellHeight),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(.5f, 0f)
            };
        }

#pragma warning disable CS0618
        importer.spritesheet = metadata;
#pragma warning restore CS0618
        importer.SaveAndReimport();
    }

    private static void BuildStaticFaunaPrefab(XunjuuFaunaCatalog.Entry entry, Sprite sprite)
    {
        string prefabName = "Fauna_" + PascalId(entry.Id);
        GameObject root = new GameObject(prefabName);
        try
        {
            root.transform.localScale = Vector3.one * entry.WorldScale;

            GameObject visual = new GameObject("Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 710;
            visual.AddComponent<CloudBillboard>();
            DyanatroSpriteDepthSorter sorter = visual.AddComponent<DyanatroSpriteDepthSorter>();
            sorter.Configure(710, 10f);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = entry.ColliderCenter;
            collider.size = entry.ColliderSize;

            Animal animal = root.AddComponent<Animal>();
            animal.canFlee = true;
            animal.ConfigureSpecies(entry.Species);

            XunjuuAnimalCapture capture = root.AddComponent<XunjuuAnimalCapture>();
            capture.Configure(entry.Id);
            root.AddComponent<XunjuuTerrainGrounding>();
            root.AddComponent<XunjuuAnimalSpriteMotion>();

            XunjuuWorldLabel label = root.AddComponent<XunjuuWorldLabel>();
            label.Configure("Español: " + entry.DisplayName + "\nC: capturar y registrar", true, entry.ColliderCenter + Vector3.up * 1.05f);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + prefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static string PascalId(string id)
    {
        return string.Concat(id.Split('_').Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
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
