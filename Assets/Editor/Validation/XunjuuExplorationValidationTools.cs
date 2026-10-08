using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Validacion de exploracion
// ACCION: comprobar cinco flores, progresion beta y un bosque por cada Terrain.
// ============================================================================
public static class XunjuuExplorationValidationTools
{
    [MenuItem("Xunjuu v0.1/Pruebas/Validar palabras, animales y bosque %&v")]
    public static void ValidateExploration()
    {
        MazahuaWordCollectible[] words = UnityEngine.Object.FindObjectsByType<MazahuaWordCollectible>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (words.Length != 5)
            throw new InvalidOperationException("Se esperaban 5 flores editables y se encontraron " + words.Length + ".");

        float minimumDistance = float.MaxValue;
        for (int first = 0; first < words.Length; first++)
        {
            for (int second = first + 1; second < words.Length; second++)
            {
                Vector3 delta = words[first].transform.position - words[second].transform.position;
                delta.y = 0f;
                minimumDistance = Mathf.Min(minimumDistance, delta.magnitude);
            }
        }

        if (minimumDistance < 25.5f)
            throw new InvalidOperationException("Hay palabras demasiado juntas: " + minimumDistance.ToString("0.0") + " unidades.");

        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player != null && words.Any(word => HorizontalDistance(player.transform.position, word.transform.position) > 50f))
            throw new InvalidOperationException("Hay flores fuera del recorrido cercano de 50 unidades.");

        GameObject[] regionalFauna = Resources.LoadAll<GameObject>("Prefabs/Animals")
            .Where(prefab => prefab != null && prefab.name.StartsWith("Fauna_", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (regionalFauna.Length != 6
            || regionalFauna.Any(prefab => prefab.GetComponent<Animal>() == null
                || prefab.GetComponent<XunjuuAnimalCapture>() == null
                || prefab.GetComponent<XunjuuTerrainGrounding>() == null
                || prefab.GetComponentInChildren<SpriteRenderer>()?.sprite == null))
            throw new InvalidOperationException("La fauna regional necesita seis prefabs con IA, captura, suelo y sprite.");
        MazahuaWordAnimalSpawner[] spawners = UnityEngine.Object.FindObjectsByType<MazahuaWordAnimalSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (spawners.Length != words.Length)
            throw new InvalidOperationException("Cada flor debe conservar su spawner de fauna.");

        GameObject forest = GameObject.Find("Bosque_Dyanatro_Generado");
        Terrain[] terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (forest == null || forest.transform.childCount != terrains.Length)
            throw new InvalidOperationException("Debe existir un grupo de bosque por cada Terrain.");

        int treeTotal = 0;
        float closestTrees = float.MaxValue;
        for (int groupIndex = 0; groupIndex < forest.transform.childCount; groupIndex++)
        {
            Transform group = forest.transform.GetChild(groupIndex);
            TreeInteractivo[] groupTrees = group.GetComponentsInChildren<TreeInteractivo>(true);
            if (groupTrees.Length < 40)
                throw new InvalidOperationException(group.name + " no tiene suficientes arboles.");
            if (groupTrees.Any(tree => tree.GetComponentInChildren<Collider>(true) == null))
                throw new InvalidOperationException(group.name + " contiene arboles sin collider 3D persistente.");

            treeTotal += groupTrees.Length;
            for (int first = 0; first < groupTrees.Length; first++)
            {
                for (int second = first + 1; second < groupTrees.Length; second++)
                {
                    Vector3 delta = groupTrees[first].transform.position - groupTrees[second].transform.position;
                    delta.y = 0f;
                    closestTrees = Mathf.Min(closestTrees, delta.magnitude);
                }
            }
        }
        if (closestTrees < 9.95f)
            throw new InvalidOperationException("Hay arboles separados menos de 10 unidades: " + closestTrees.ToString("0.00"));
        if (player != null)
        {
            int treesNearSpawn = forest.GetComponentsInChildren<TreeInteractivo>(true)
                .Count(tree => HorizontalDistance(player.transform.position, tree.transform.position) <= 75f);
            if (treesNearSpawn < 18)
                throw new InvalidOperationException("La zona de aparicion sigue vacia: solo hay " + treesNearSpawn + " arboles cercanos.");
        }

        GameObject cornRoot = GameObject.Find("Milpas_Maiz_Dyanatro");
        if (cornRoot == null || cornRoot.transform.childCount != 10)
            throw new InvalidOperationException("Deben existir diez milpas editables y separadas.");
        float closestCornFields = float.MaxValue;
        for (int first = 0; first < cornRoot.transform.childCount; first++)
        {
            for (int second = first + 1; second < cornRoot.transform.childCount; second++)
            {
                closestCornFields = Mathf.Min(
                    closestCornFields,
                    HorizontalDistance(cornRoot.transform.GetChild(first).position, cornRoot.transform.GetChild(second).position));
            }
        }
        if (closestCornFields < 27.5f)
            throw new InvalidOperationException("Hay milpas traslapadas: separacion " + closestCornFields.ToString("0.0") + ".");

        XunjuuLevel2KillMission levelTwo = UnityEngine.Object.FindFirstObjectByType<XunjuuLevel2KillMission>(FindObjectsInactive.Include);
        if (levelTwo == null || levelTwo.EnemiesToDefeat != 5 || levelTwo.AnimalsToDefeat != 6 || levelTwo.RequireAnimalDefeats)
            throw new InvalidOperationException("La progresion no esta configurada en 6 capturas y 5 enemigos secuenciales.");

        XunjuuAnimalHealth[] levelTwoAnimals = UnityEngine.Object.FindObjectsByType<XunjuuAnimalHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        XunjuuAnimalHealth[] missionAnimals = levelTwoAnimals.Where(animal => animal.name.Contains("_Mision2_")).ToArray();
        if (missionAnimals.Length != 6)
            throw new InvalidOperationException("La mision debe conservar seis puntos de fauna convertibles en Play Mode.");
        if (missionAnimals.Any(animal => animal.GetComponent<XunjuuTerrainGrounding>() == null))
            throw new InvalidOperationException("La fauna necesita ajuste continuo al terreno.");
        if (player != null && missionAnimals.Any(animal => HorizontalDistance(player.transform.position, animal.transform.position) > 50f))
            throw new InvalidOperationException("Hay animales de mision a mas de 50 unidades del inicio.");

        GameObject[] missionEnemies = UnityEngine.Object.FindObjectsByType<EnemyFireBreath>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(enemy => enemy.gameObject)
            .Concat(UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(enemy => enemy.gameObject))
            .Where(enemy => enemy.name.StartsWith("Enemigo_Nivel2_"))
            .Distinct()
            .ToArray();
        if (missionEnemies.Length != 5 || (player != null && missionEnemies.Any(enemy => HorizontalDistance(player.transform.position, enemy.transform.position) > 50f)))
            throw new InvalidOperationException("Los cinco enemigos deben quedar dentro del recorrido cercano de 50 unidades.");
        if (missionEnemies.Any(enemy => enemy.GetComponent<XunjuuTerrainGrounding>() == null))
            throw new InvalidOperationException("Los enemigos necesitan ajuste continuo al terreno.");
        EnemyFireBreath cooldownEnemy = missionEnemies.Select(enemy => enemy.GetComponent<EnemyFireBreath>()).FirstOrDefault(enemy => enemy != null);
        if (cooldownEnemy == null || cooldownEnemy.AttackCooldown < cooldownEnemy.AttackAnimationDuration + 0.75f
            || cooldownEnemy.ContactDamageCooldown < 0.75f || cooldownEnemy.WanderRadius < 8f)
            throw new InvalidOperationException("El Dyanatr'o no conserva cooldowns o patrulla ampliada.");

        Transform bossPoint = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == "Punto_Aparicion_Jefe_Final");
        if (bossPoint == null || (player != null && HorizontalDistance(player.transform.position, bossPoint.position) > 50f))
            throw new InvalidOperationException("El punto del jefe final esta fuera del recorrido cercano.");

        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Boss/Ocelotl.prefab");
        XunjuuBossNivel1 boss = bossPrefab != null ? bossPrefab.GetComponent<XunjuuBossNivel1>() : null;
        if (boss == null || boss.MaxHealth < 900 || bossPrefab.GetComponent<Collider>() == null
            || bossPrefab.transform.localScale.x < 3.4f)
            throw new InvalidOperationException("Ocelotl no tiene la vida o colision requeridas para el combate final.");
        if (boss.MeleeCooldown < boss.MeleeAnimationDuration + 0.2f || boss.ChargeCooldown < 5f)
            throw new InvalidOperationException("Ocelotl no conserva sus cooldowns de ataque.");

        if (player == null || player.BasicAttackDuration < 0.47f
            || player.BasicAttackCooldown < player.BasicAttackDuration + 0.1f
            || player.SwordAttackCooldown < 0.65f)
            throw new InvalidOperationException("Player1 no conserva los tiempos sincronizados de ataque.");

        if (!EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == "Assets/Scenes/SampleScene.unity"))
            throw new InvalidOperationException("SampleScene no esta habilitada en Build Settings.");

        ValidateInventorySequence();
        Debug.Log($"[XUNJUU EXPLORACION PASS] flores=5; fauna regional=6; separacion flores={minimumDistance:0.0}; terrenos={terrains.Length}; arboles={treeTotal}; milpas=10/{closestCornFields:0.0}m; beta=6 capturas+5 enemigos; inventario=OK.");
    }

    private static GameObject RequireAnimalPrefab(string animalName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Animals/" + animalName + ".prefab");
        if (prefab == null || prefab.GetComponent<Animal>() == null || prefab.GetComponent<Animator>() == null || prefab.GetComponent<Collider>() == null)
            throw new InvalidOperationException("El prefab " + animalName + " no esta completo.");
        return prefab;
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        Vector3 delta = first - second;
        delta.y = 0f;
        return delta.magnitude;
    }

    private static void ValidateInventorySequence()
    {
        GameObject testObject = new GameObject("Prueba_Inventario_Xunjuu");
        try
        {
            XunjuuInventory inventory = testObject.AddComponent<XunjuuInventory>();
            inventory.AddItem("palabra_jnaa", "jnaa - palabra", 1);
            inventory.AddItem("palabra_jnini", "jnini - pueblo", 1);
            if (inventory.SelectedIndex != 0)
                throw new InvalidOperationException("El inventario no selecciono el primer objeto.");
            inventory.SelectNext();
            if (inventory.SelectedIndex != 1 || inventory.SelectedItem == null)
                throw new InvalidOperationException("La seleccion secuencial del inventario no funciona.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }
    }
}
