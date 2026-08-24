using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Xunju2DSceneBuilder
{
    private const string Root = "Assets/2D_Completo";
    private const string ScenePath = Root + "/Scenes/Xunju_2D_Completo.unity";
    private const string GeneratedPath = Root + "/Generated";

    [MenuItem("Xunjuu/Crear escena 2D completa")]
    public static void CreateScene()
    {
        Directory.CreateDirectory(GeneratedPath);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Xunju_2D_Completo";

        Sprite playerSprite = LoadSprite("Assets/2D_Completo/Datos/Sprites/descanso.png", "Assets/Sprites/descanso.png");
        Sprite enemySprite = LoadSprite("Assets/2D_Completo/Datos/Sprites/enemigo.png", "Assets/Sprites/enemigo.png");
        Sprite treeSprite = LoadSprite("Assets/2D_Completo/Datos/Sprites/arbol lloron .png", "Assets/2D_Completo/Datos/Sprites/tree.png", "Assets/Sprites/arbol lloron .png", "Assets/Sprites/tree.png");
        Sprite bushSprite = LoadSprite("Assets/2D_Completo/Datos/Sprites/arbusto.png", "Assets/Sprites/TextureTerrain/arbusto.png");
        Sprite flowerSprite = LoadSprite("Assets/2D_Completo/Datos/Sprites/flor.png", "Assets/Sprites/TextureTerrain/flor.png");
        Sprite cloudSprite = LoadSprite("Assets/2D_Completo/Datos/Sprites/clound.png", "Assets/Sprites/clound.png");
        Sprite swordSprite = LoadSprite("Assets/2D_Completo/Datos/Sprites/ARMA DESCANSA.png", "Assets/Sprites/ARMA DESCANSA.png");
        AudioClip music = LoadAudio("Assets/2D_Completo/Datos/Audio/mazahua_bosque_pino_bgm_v2.wav", "Assets/Audio/mazahua_bosque_pino_bgm_v2.wav");
        Sprite groundSprite = CreateGroundSprite();
        Sprite grassSprite = CreateTallGrassSprite();

        Camera camera = CreateCamera();
        GameObject world = new GameObject("Mundo_2D_Completo");

        CreateSanFelipeBackground(world.transform);
        CreateGround(world.transform, groundSprite);
        CreateClouds(world.transform, cloudSprite);
        Xunju2DPlayerController player = CreatePlayer(world.transform, playerSprite);
        camera.GetComponent<Xunju2DCameraFollow>().SetTarget(player.transform);
        CreateSwordPickup(world.transform, swordSprite);
        CreateTrees(world.transform, treeSprite);
        CreateTallGrass(world.transform, grassSprite);
        CreateVegetation(world.transform, bushSprite, flowerSprite, treeSprite);
        CreateMazahuaWords(world.transform);
        CreateEnemies(world.transform, enemySprite);

        GameObject managerObject = new GameObject("GameManager_2D_Completo");
        Xunju2DGameManager manager = managerObject.AddComponent<Xunju2DGameManager>();
        SerializedObject managerSerialized = new SerializedObject(manager);
        managerSerialized.FindProperty("player").objectReferenceValue = player;
        managerSerialized.FindProperty("backgroundMusic").objectReferenceValue = music;
        managerSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Xunjuu 2D", "Escena 2D completa creada en:\n" + ScenePath, "Listo");
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6.2f;
        camera.backgroundColor = new Color(0.95f, 0.52f, 0.34f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<Xunju2DCameraFollow>();
        return camera;
    }

    private static Xunju2DPlayerController CreatePlayer(Transform parent, Sprite playerSprite)
    {
        GameObject player = CreateSpriteObject("Mateo_2D", playerSprite, new Vector3(0f, -0.5f, 0f), parent, 20);
        player.tag = "Player";
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.58f, 0.95f);
        collider.offset = new Vector2(0f, 0.05f);
        Xunju2DPlayerController controller = player.AddComponent<Xunju2DPlayerController>();
        player.AddComponent<Xunju2DDepthSorter>().Configure(20, 100);

        GameObject attackPoint = new GameObject("AttackPoint_2D");
        attackPoint.transform.SetParent(player.transform, false);
        attackPoint.transform.localPosition = new Vector3(0.75f, 0f, 0f);

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("attackPoint").objectReferenceValue = attackPoint.transform;
        serialized.FindProperty("attackLayers").intValue = LayerMask.GetMask("Default");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static void CreateSwordPickup(Transform parent, Sprite swordSprite)
    {
        if (swordSprite == null)
            return;

        GameObject sword = CreateSpriteObject("Espada_Item_2D", swordSprite, new Vector3(2.8f, 0.25f, 0f), parent, 38);
        sword.transform.localScale = Vector3.one * 0.42f;
        CircleCollider2D collider = sword.AddComponent<CircleCollider2D>();
        collider.radius = 0.8f;
        collider.isTrigger = true;
        sword.AddComponent<Xunju2DSwordPickupItem>();
    }

    private static void CreateEnemies(Transform parent, Sprite enemySprite)
    {
        Vector3[] positions =
        {
            new Vector3(5f, 1.5f, 0f), new Vector3(-6f, 2.2f, 0f), new Vector3(8f, -3f, 0f),
            new Vector3(-9f, -2f, 0f), new Vector3(12f, 4f, 0f), new Vector3(-13f, 3.5f, 0f)
        };

        foreach (Vector3 position in positions)
        {
            GameObject enemy = CreateSpriteObject("Dyanatro_2D", enemySprite, position, parent, 10);
            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;
            enemy.AddComponent<Xunju2DEnemy>();
            enemy.AddComponent<Xunju2DDepthSorter>().Configure(10, 100);
        }
    }

    private static void CreateTrees(Transform parent, Sprite treeSprite)
    {
        UnityEngine.Random.InitState(72);
        for (int i = 0; i < 96; i++)
        {
            Vector3 position = new Vector3(UnityEngine.Random.Range(-22f, 22f), UnityEngine.Random.Range(-9.5f, 9.5f), 0f);
            if (Vector2.Distance(position, Vector2.zero) < 2.8f)
                position += position.normalized * 3.4f;

            GameObject tree = CreateSpriteObject("Arbol_2D", treeSprite, position, parent, 0);
            float scale = UnityEngine.Random.Range(0.95f, 1.85f);
            tree.transform.localScale = Vector3.one * scale;
            BoxCollider2D collider = tree.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.55f, 0.45f);
            collider.offset = new Vector2(0f, -0.78f);
            tree.AddComponent<Xunju2DTree>();
            tree.AddComponent<Xunju2DDepthSorter>().Configure(0, 100);
        }
    }

    private static void CreateTallGrass(Transform parent, Sprite grassSprite)
    {
        UnityEngine.Random.InitState(144);
        GameObject root = new GameObject("PastoAlto_2D");
        root.transform.SetParent(parent, false);

        for (int i = 0; i < 360; i++)
        {
            Vector3 position = new Vector3(UnityEngine.Random.Range(-23f, 23f), UnityEngine.Random.Range(-10.5f, 10.5f), 0f);
            if (Vector2.Distance(position, Vector2.zero) < 1.25f)
                continue;

            GameObject grass = CreateSpriteObject("Pasto_Alto_2D", grassSprite, position, root.transform, -8);
            grass.transform.localScale = new Vector3(UnityEngine.Random.Range(0.65f, 1.25f), UnityEngine.Random.Range(0.75f, 1.18f), 1f);
            grass.AddComponent<Xunju2DDepthSorter>().Configure(-8, 100);
        }
    }

    private static void CreateVegetation(Transform parent, Sprite bushSprite, Sprite flowerSprite, Sprite treeSprite)
    {
        UnityEngine.Random.InitState(301);
        GameObject root = new GameObject("Vegetacion_SanFelipe_2D");
        root.transform.SetParent(parent, false);

        for (int i = 0; i < 240; i++)
        {
            Vector3 position = new Vector3(UnityEngine.Random.Range(-23f, 23f), UnityEngine.Random.Range(-10f, 10f), 0f);
            if (Vector2.Distance(position, Vector2.zero) < 1.7f)
                continue;

            Sprite sprite = bushSprite;
            string name = "Arbusto_2D";
            float scale = UnityEngine.Random.Range(0.65f, 1.35f);
            float roll = UnityEngine.Random.value;
            if (roll > 0.82f && flowerSprite != null)
            {
                sprite = flowerSprite;
                name = "Flor_2D";
                scale = UnityEngine.Random.Range(0.55f, 0.95f);
            }
            else if (roll > 0.64f && treeSprite != null)
            {
                sprite = treeSprite;
                name = "Arbolito_2D";
                scale = UnityEngine.Random.Range(0.35f, 0.72f);
            }

            GameObject obj = CreateSpriteObject(name, sprite, position, root.transform, -12);
            obj.transform.localScale = Vector3.one * scale;
            obj.AddComponent<Xunju2DDepthSorter>().Configure(-12, 100);
        }
    }

    private static void CreateMazahuaWords(Transform parent)
    {
        string[] words =
        {
            "jñatjo|lengua mazahua",
            "jñaa|palabra / voz",
            "jñiñi|pueblo",
            "xiskuama|documento",
            "b'epji|trabajo comunitario",
            "skuama|libro / papel",
            "ngunxorú|escuela",
            "Xonijomu|lugar de memoria"
        };

        Vector3[] positions =
        {
            new Vector3(-4.5f, 2.8f, 0f), new Vector3(5.8f, 2.4f, 0f), new Vector3(-8.2f, -2.7f, 0f),
            new Vector3(8.5f, -2.4f, 0f), new Vector3(-13f, 4.8f, 0f), new Vector3(13f, 4.4f, 0f),
            new Vector3(-15f, -6.2f, 0f), new Vector3(15f, -5.7f, 0f)
        };

        GameObject root = new GameObject("Palabras_Mazahuas_2D");
        root.transform.SetParent(parent, false);

        for (int i = 0; i < words.Length; i++)
        {
            string[] parts = words[i].Split('|');
            string word = parts[0];
            string meaning = parts.Length > 1 ? parts[1] : "memoria";
            GameObject obj = new GameObject("Palabra_2D_" + word);
            obj.transform.SetParent(root.transform, false);
            obj.transform.position = positions[i];

            CircleCollider2D collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = 0.85f;
            collider.isTrigger = true;
            Xunju2DWordCollectible collectible = obj.AddComponent<Xunju2DWordCollectible>();
            collectible.Configure(word, meaning);

            GameObject glow = CreateSpriteObject("Brillo_" + word, CreateCircleSprite("brillo_palabra_2d.png", new Color(1f, 0.75f, 0.18f, 0.88f)), Vector3.zero, obj.transform, 55);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = Vector3.one * 0.58f;

            GameObject labelObject = new GameObject("Texto_" + word);
            labelObject.transform.SetParent(obj.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = word + "\n" + meaning;
            label.fontSize = 34;
            label.characterSize = 0.08f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(1f, 0.92f, 0.55f, 1f);
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 60;
        }
    }

    private static void CreateClouds(Transform parent, Sprite cloudSprite)
    {
        if (cloudSprite == null)
            return;

        UnityEngine.Random.InitState(31);
        GameObject root = new GameObject("Nubes_2D");
        root.transform.SetParent(parent, false);

        for (int i = 0; i < 16; i++)
        {
            GameObject cloud = CreateSpriteObject("Nube_2D", cloudSprite, new Vector3(UnityEngine.Random.Range(-25f, 25f), UnityEngine.Random.Range(6.9f, 10.8f), 0f), root.transform, -870);
            cloud.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.1f, 2.55f);
            SpriteRenderer renderer = cloud.GetComponent<SpriteRenderer>();
            renderer.color = new Color(1f, 0.88f, 0.74f, 0.46f);
            Xunju2DCloudDrift drift = cloud.AddComponent<Xunju2DCloudDrift>();
            SerializedObject serialized = new SerializedObject(drift);
            serialized.FindProperty("speed").floatValue = UnityEngine.Random.Range(0.025f, 0.12f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CreateGround(Transform parent, Sprite groundSprite)
    {
        GameObject ground = CreateSpriteObject("Piso_Pasto_2D", groundSprite, Vector3.zero, parent, -1000);
        ground.transform.localScale = new Vector3(52f, 30f, 1f);
    }

    private static void CreateSanFelipeBackground(Transform parent)
    {
        GameObject root = new GameObject("Fondo_SanFelipeDelProgreso_2D");
        root.transform.SetParent(parent, false);

        GameObject sky = CreateSpriteObject("Cielo_Atardecer_2D", CreateSkySprite(), new Vector3(0f, 3.2f, 0f), root.transform, -2500);
        sky.transform.localScale = new Vector3(42f, 18f, 1f);

        GameObject hills = CreateSpriteObject("Cerros_SanFelipe_2D", CreateHillSprite(), new Vector3(0f, 1.3f, 0f), root.transform, -2100);
        hills.transform.localScale = new Vector3(30f, 8f, 1f);

        GameObject town = CreateSpriteObject("Plaza_Parroquia_Kiosco_2D", CreateTownSprite(), new Vector3(0f, 0.2f, 0f), root.transform, -1850);
        town.transform.localScale = new Vector3(17f, 6.2f, 1f);
    }

    private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector3 position, Transform parent, int sortingOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return obj;
    }

    private static Sprite LoadSprite(params string[] paths)
    {
        foreach (string path in paths)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
        }
        return null;
    }

    private static AudioClip LoadAudio(params string[] paths)
    {
        foreach (string path in paths)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                return clip;
        }
        return null;
    }

    private static Sprite CreateGroundSprite()
    {
        return CreateOrLoadSprite("piso_pasto_2d.png", 128, 128, delegate(Texture2D texture)
        {
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                    float n2 = Mathf.PerlinNoise(x * 0.23f + 9f, y * 0.23f + 5f);
                    Color grass = Color.Lerp(new Color(0.18f, 0.39f, 0.14f), new Color(0.45f, 0.62f, 0.21f), n1);
                    Color earth = new Color(0.36f, 0.25f, 0.13f);
                    texture.SetPixel(x, y, Color.Lerp(grass, earth, Mathf.SmoothStep(0.7f, 1f, n2) * 0.35f));
                }
            }
        }, 64f, FilterMode.Point);
    }

    private static Sprite CreateTallGrassSprite()
    {
        return CreateOrLoadSprite("pasto_alto_2d.png", 256, 256, delegate(Texture2D texture)
        {
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    texture.SetPixel(x, y, clear);

            UnityEngine.Random.InitState(95);
            for (int i = 0; i < 82; i++)
            {
                int baseX = UnityEngine.Random.Range(18, 238);
                int height = UnityEngine.Random.Range(125, 242);
                float bend = UnityEngine.Random.Range(-44f, 44f);
                DrawBlade(texture, baseX, height, bend);
            }
        }, 165f, FilterMode.Bilinear);
    }

    private static Sprite CreateSkySprite()
    {
        return CreateOrLoadSprite("fondo_cielo_atardecer_2d.png", 256, 128, delegate(Texture2D texture)
        {
            for (int y = 0; y < texture.height; y++)
            {
                float t = y / (float)(texture.height - 1);
                Color bottom = new Color(0.97f, 0.45f, 0.28f, 1f);
                Color top = new Color(0.45f, 0.63f, 0.92f, 1f);
                Color color = Color.Lerp(bottom, top, t);
                for (int x = 0; x < texture.width; x++)
                    texture.SetPixel(x, y, color);
            }
        }, 64f, FilterMode.Bilinear);
    }

    private static Sprite CreateHillSprite()
    {
        return CreateOrLoadSprite("cerros_san_felipe_2d.png", 256, 96, delegate(Texture2D texture)
        {
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    texture.SetPixel(x, y, clear);

            for (int x = 0; x < texture.width; x++)
            {
                float ridge = 35f + Mathf.Sin(x * 0.035f) * 12f + Mathf.Sin(x * 0.11f) * 5f;
                for (int y = 0; y < ridge; y++)
                {
                    float t = y / Mathf.Max(1f, ridge);
                    Color color = Color.Lerp(new Color(0.22f, 0.22f, 0.17f, 1f), new Color(0.36f, 0.29f, 0.2f, 1f), t);
                    texture.SetPixel(x, y, color);
                }
            }
        }, 64f, FilterMode.Bilinear);
    }

    private static Sprite CreateTownSprite()
    {
        return CreateOrLoadSprite("plaza_san_felipe_2d.png", 256, 128, delegate(Texture2D texture)
        {
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    texture.SetPixel(x, y, clear);

            DrawRect(texture, 0, 0, 256, 24, new Color(0.32f, 0.36f, 0.21f, 0.95f));
            DrawRect(texture, 86, 24, 84, 34, new Color(0.7f, 0.58f, 0.43f, 0.96f));
            DrawRect(texture, 99, 58, 58, 18, new Color(0.8f, 0.69f, 0.52f, 0.98f));
            DrawRect(texture, 112, 76, 32, 32, new Color(0.72f, 0.62f, 0.5f, 0.98f));
            DrawRect(texture, 122, 86, 12, 18, new Color(0.28f, 0.2f, 0.18f, 1f));
            DrawRect(texture, 134, 76, 16, 44, new Color(0.76f, 0.68f, 0.55f, 0.98f));
            DrawRect(texture, 139, 108, 6, 8, new Color(0.23f, 0.18f, 0.15f, 1f));
            DrawTriangle(texture, 94, 76, 162, 76, 128, 104, new Color(0.53f, 0.23f, 0.18f, 1f));
            DrawRect(texture, 42, 24, 38, 18, new Color(0.55f, 0.32f, 0.2f, 0.92f));
            DrawTriangle(texture, 34, 42, 88, 42, 61, 66, new Color(0.39f, 0.16f, 0.13f, 0.95f));
            DrawRect(texture, 50, 44, 22, 18, new Color(0.78f, 0.63f, 0.36f, 0.95f));
            DrawRect(texture, 185, 24, 42, 20, new Color(0.53f, 0.31f, 0.22f, 0.92f));
            DrawTriangle(texture, 178, 44, 234, 44, 206, 68, new Color(0.4f, 0.17f, 0.12f, 0.95f));
        }, 64f, FilterMode.Point);
    }

    private static Sprite CreateCircleSprite(string fileName, Color color)
    {
        return CreateOrLoadSprite(fileName, 64, 64, delegate(Texture2D texture)
        {
            Vector2 center = new Vector2(31.5f, 31.5f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / 31.5f;
                    Color pixel = color;
                    pixel.a *= Mathf.Clamp01(1f - Mathf.SmoothStep(0.62f, 1f, d));
                    texture.SetPixel(x, y, pixel);
                }
            }
        }, 64f, FilterMode.Bilinear);
    }

    private static Sprite CreateOrLoadSprite(string fileName, int width, int height, Action<Texture2D> paint, float pixelsPerUnit, FilterMode filterMode)
    {
        string path = GeneratedPath + "/" + fileName;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        paint(texture);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.filterMode = filterMode;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void DrawBlade(Texture2D texture, int baseX, int height, float bend)
    {
        Color dark = new Color(0.07f, 0.32f, 0.08f, 1f);
        Color mid = new Color(0.32f, 0.76f, 0.16f, 1f);
        Color light = new Color(0.78f, 0.98f, 0.18f, 1f);
        for (int y = 0; y < height; y++)
        {
            float t = y / (float)height;
            int x = Mathf.RoundToInt(baseX + bend * Mathf.SmoothStep(0f, 1f, t));
            int width = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(6f, 1f, t)));
            Color color = Color.Lerp(dark, t > 0.7f ? light : mid, t);
            for (int ox = -width; ox <= width; ox++)
            {
                int px = x + ox;
                if (px < 0 || px >= texture.width || y < 0 || y >= texture.height)
                    continue;
                Color current = texture.GetPixel(px, y);
                Color blended = Color.Lerp(current, color, 0.72f);
                blended.a = 1f;
                texture.SetPixel(px, y, blended);
            }
        }
    }

    private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
            for (int px = x; px < x + width; px++)
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    texture.SetPixel(px, py, color);
    }

    private static void DrawTriangle(Texture2D texture, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
    {
        int minX = Mathf.Min(x1, Mathf.Min(x2, x3));
        int maxX = Mathf.Max(x1, Mathf.Max(x2, x3));
        int minY = Mathf.Min(y1, Mathf.Min(y2, y3));
        int maxY = Mathf.Max(y1, Mathf.Max(y2, y3));
        float area = Edge(x1, y1, x2, y2, x3, y3);
        if (Mathf.Approximately(area, 0f))
            return;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float w0 = Edge(x2, y2, x3, y3, x, y);
                float w1 = Edge(x3, y3, x1, y1, x, y);
                float w2 = Edge(x1, y1, x2, y2, x, y);
                bool inside = area > 0f ? w0 >= 0f && w1 >= 0f && w2 >= 0f : w0 <= 0f && w1 <= 0f && w2 <= 0f;
                if (inside && x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    texture.SetPixel(x, y, color);
            }
        }
    }

    private static float Edge(int ax, int ay, int bx, int by, int cx, int cy)
    {
        return (cx - ax) * (by - ay) - (cy - ay) * (bx - ax);
    }
}

