using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Prueba aislada: evita que los generadores antiguos de la escena principal
// oculten regresiones de la fauna y del nuevo entorno durante CI.
[InitializeOnLoad]
public static class XunjuuFaunaEnvironmentValidation
{
    private const string PendingKey = "Xunjuu.FaunaEnvironmentValidation.Pending";

    static XunjuuFaunaEnvironmentValidation()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void StartValidation()
    {
        PrepareIsolatedScene();
        SessionState.SetBool(PendingKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void PrepareIsolatedScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(new Vector3(110f, 95f, 25f), Quaternion.Euler(48f, 0f, 0f));

        GameObject lightObject = new GameObject("Directional Light", typeof(Light));
        lightObject.GetComponent<Light>().type = LightType.Directional;
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        TerrainData terrainData = new TerrainData { heightmapResolution = 33, size = new Vector3(220f, 20f, 220f) };
        GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);
        terrain.name = "Terrain_QA_Fauna";

        GameObject playerObject = new GameObject("Player_QA", typeof(Rigidbody), typeof(PlayerController));
        playerObject.tag = "Player";
        playerObject.transform.position = new Vector3(110f, .2f, 110f);
        playerObject.GetComponent<Rigidbody>().isKinematic = true;
        playerObject.GetComponent<PlayerController>().enabled = false;

        new GameObject("Entorno_QA", typeof(XunjuuMeadowDressing));

        GameObject animalRoot = new GameObject("Mision_2_Fauna_Regional_QA");
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Animals")
            .Where(prefab => prefab != null && prefab.name.StartsWith("Fauna_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(prefab => prefab.name, StringComparer.Ordinal)
            .ToArray();
        for (int index = 0; index < prefabs.Length; index++)
        {
            GameObject animal = UnityEngine.Object.Instantiate(prefabs[index]);
            animal.name = "Fauna_Mision2_" + (index + 1).ToString("00") + "_" + prefabs[index].name;
            animal.transform.SetParent(animalRoot.transform, false);
            animal.transform.position = new Vector3(84f + index * 9f, .2f, 92f + (index % 2) * 9f);
            if (index == 0 && animal.GetComponent<XunjuuAnimalHealth>() == null)
                animal.AddComponent<XunjuuAnimalHealth>();
        }

        GameObject enemyRoot = new GameObject("Enemigos_QA");
        enemyRoot.SetActive(false);
        XunjuuLevel2KillMission mission = new GameObject("Mision_QA").AddComponent<XunjuuLevel2KillMission>();
        SetField(mission, "animalGroupRoot", animalRoot);
        SetField(mission, "enemyGroupRoot", enemyRoot);
        SetField(mission, "missionActive", true);
        SetField(mission, "createMissionHud", false);
        SetField(mission, "repositionObjectivesNearPlayer", false);
        SetField(mission, "controlGroupActivation", true);
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new MissingFieldException(target.GetType().Name, name);
        field.SetValue(target, value);
        EditorUtility.SetDirty((UnityEngine.Object)target);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            SessionState.SetBool(PendingKey, false);
            Run();
        }
    }

    private static async void Run()
    {
        bool passed = false;
        const string collectionKey = "xunjuu.fauna.capturada.tlacuache";
        bool hadCollectionValue = PlayerPrefs.HasKey(collectionKey);
        int previousCollectionValue = PlayerPrefs.GetInt(collectionKey, 0);
        try
        {
            Time.timeScale = 1f;
            await Task.Delay(700);

            XunjuuMeadowDressing dressing = UnityEngine.Object.FindFirstObjectByType<XunjuuMeadowDressing>();
            for (int attempt = 0; attempt < 160 && (dressing == null || dressing.PatchCount < 81); attempt++)
            {
                await Task.Delay(50);
                dressing = UnityEngine.Object.FindFirstObjectByType<XunjuuMeadowDressing>();
            }
            if (dressing == null || dressing.PatchCount < 81 || Mathf.Abs(dressing.VegetatedSpan - 180f) > .1f)
                throw new InvalidOperationException("La pradera compacta de 180 m no termino de generarse.");
            if (dressing.Buildings.Count < 8)
                throw new InvalidOperationException("El poblado ampliado solo coloco " + dressing.Buildings.Count + " casas.");
            int mountains = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Count(renderer => renderer.name.StartsWith("Sierra_Lejana_", StringComparison.Ordinal));
            if (mountains < 20)
                throw new InvalidOperationException("El fondo solo genero " + mountains + " montanas.");

            GameObject[] regionalPrefabs = Resources.LoadAll<GameObject>("Prefabs/Animals")
                .Where(prefab => prefab != null && prefab.name.StartsWith("Fauna_", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (regionalPrefabs.Length != 6 || regionalPrefabs.Any(prefab => prefab.GetComponent<XunjuuAnimalCapture>() == null))
                throw new InvalidOperationException("No existen seis prefabs regionales capturables.");
            if (regionalPrefabs.Any(prefab => prefab.GetComponentInChildren<SpriteRenderer>()?.sprite == null))
                throw new InvalidOperationException("Hay fauna regional sin sprite.");

            XunjuuLevel2KillMission mission = UnityEngine.Object.FindFirstObjectByType<XunjuuLevel2KillMission>();
            if (mission == null || !mission.IsAnimalPhase)
                throw new InvalidOperationException("La fase de captura no inicio.");
            XunjuuAnimalCapture[] missionAnimals = UnityEngine.Object.FindObjectsByType<XunjuuAnimalCapture>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(animal => animal.name.Contains("_Mision2_"))
                .ToArray();
            if (missionAnimals.Length != 6 || missionAnimals.Select(animal => animal.FaunaId).Distinct().Count() != 6)
                throw new InvalidOperationException("La mision no contiene seis especies regionales distintas.");
            if (missionAnimals.Any(animal => animal.name.IndexOf("Pato", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException("Pato sigue presente en la mision regional.");

            foreach(var animal in missionAnimals)
            {
                var motion=animal.GetComponent<XunjuuAnimalSpriteMotion>();
                var renderer=animal.GetComponentInChildren<SpriteRenderer>();
                var articulated=animal.GetComponentInChildren<MeshFilter>();
                if(motion==null || articulated==null || !motion.HasFrameAnimation)
                    throw new InvalidOperationException("Fauna sin articulacion: "+animal.name);
                var pose=typeof(XunjuuAnimalSpriteMotion).GetMethod("Pose",BindingFlags.Instance|BindingFlags.NonPublic);
                pose.Invoke(motion,new object[]{0f,1f});
                var first=articulated.sharedMesh.uv;
                pose.Invoke(motion,new object[]{1.5f,1f});
                var second=articulated.sharedMesh.uv;
                if(!first.Where((p,i)=>(p-second[i]).sqrMagnitude>.00001f).Any())
                    throw new InvalidOperationException("Patas inmoviles: "+animal.name);
            }
            foreach(var ridge in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Sierra_Lejana_")))
                if(Vector2.Distance(new Vector2(ridge.transform.position.x,ridge.transform.position.z),new Vector2(110f,110f))<500f)
                    throw new InvalidOperationException("Sierra demasiado cerca del poblado.");
            RenderPreview();

            XunjuuAnimalHealth legacyHealth = missionAnimals.Select(animal => animal.GetComponent<XunjuuAnimalHealth>()).FirstOrDefault(health => health != null);
            if (legacyHealth != null)
            {
                int healthBefore = legacyHealth.CurrentHealth;
                legacyHealth.TakeDamage(9999);
                if (legacyHealth.CurrentHealth != healthBefore)
                    throw new InvalidOperationException("Un animal capturable todavia recibe dano.");
            }

            foreach (XunjuuAnimalCapture animal in missionAnimals)
                XunjuuAnimalCaptureEvents.Report(animal.gameObject, animal.FaunaId);
            await Task.Delay(150);
            if (!mission.IsEnemyPhase || mission.DefeatedAnimals != 6)
                throw new InvalidOperationException("Seis capturas no abrieron la fase de enemigos.");

            PlayerPrefs.DeleteKey(collectionKey);
            if (!XunjuuFaunaCatalog.RegisterCapture("tlacuache") || !XunjuuFaunaCatalog.IsCaptured("tlacuache"))
                throw new InvalidOperationException("La ficha capturada no se guardo en la coleccion.");

            GameObject canvasObject = new GameObject("Canvas_Ludoteca_QA", typeof(Canvas));
            XunjuuLudotecaBrowser ludoteca = new GameObject("Ludoteca_QA").AddComponent<XunjuuLudotecaBrowser>();
            ludoteca.Initialize(canvasObject.GetComponent<Canvas>(), null, null, null);
            FieldInfo cardsField = typeof(XunjuuLudotecaBrowser).GetField("faunaCards", BindingFlags.Instance | BindingFlags.NonPublic);
            IDictionary cards = cardsField != null ? cardsField.GetValue(ludoteca) as IDictionary : null;
            if (cards == null || cards.Count != 6)
                throw new InvalidOperationException("La Ludoteca no construyo las seis tarjetas de fauna.");

            passed = true;
            Debug.Log("[XUNJUU FAUNA/ENTORNO PASS] fauna=6; capturas=6; tarjetas=6; zona=" + dressing.VegetatedSpan + "m; casas=" + dressing.Buildings.Count + "; montanas=" + mountains + ".");
        }
        catch (Exception exception)
        {
            Debug.LogError("[XUNJUU FAUNA/ENTORNO FAIL] " + exception);
        }
        finally
        {
            if (hadCollectionValue) PlayerPrefs.SetInt(collectionKey, previousCollectionValue); else PlayerPrefs.DeleteKey(collectionKey);
            PlayerPrefs.Save();
            EditorApplication.ExitPlaymode();
            if (Application.isBatchMode)
                EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    private static void RenderPreview()
    {
        var camera=Camera.main;
        var position=camera.transform.position;
        var rotation=camera.transform.rotation;
        var target=new RenderTexture(1280,720,24);
        var previous=RenderTexture.active;
        var previousTarget=camera.targetTexture;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.transform.SetPositionAndRotation(new Vector3(110f,14f,66f),Quaternion.Euler(12f,0,0));
            camera.targetTexture=target;
            camera.Render();
            RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,1280,720),0,0);
            image.Apply();
            System.IO.Directory.CreateDirectory("output/fauna-landscape");
            System.IO.File.WriteAllBytes("output/fauna-landscape/preview.png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previousTarget;
            RenderTexture.active=previous;
            camera.transform.SetPositionAndRotation(position,rotation);
            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(image);
        }
    }
}
