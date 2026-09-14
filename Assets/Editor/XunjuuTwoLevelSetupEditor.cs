using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ============================================================================
// Xunjuu beta v0.1 - Preparacion editable del primer nivel jugable
// ACCION: crea cinco flores, combate, animales, jefe, recompensa y bosques seguros.
// MODIFICACION: despues de ejecutar, todo queda visible en Hierarchy e Inspector.
// ============================================================================
public static class XunjuuTwoLevelSetupEditor
{
    private const string AppliedMarkerPath = "Assets/Editor/XunjuuTwoLevelSetup.applied";
    private const string ProgressionRootName = "Progresion_Niveles_Xunjuu";
    private const string EnemyPrefabPath = "Assets/Prefabs/Enemigo_Dyanatro_Nivel2.prefab";
    private const string WeaponRewardPath = "Assets/Prefabs/MisionSecundaria_Macuahuitl.prefab";
    private const string PendingRewardPath = "Assets/Prefabs/Recompensa_Nivel2_Pendiente.prefab";
    private const string BossPrefabPath = "Assets/Prefabs/Ocelotl.prefab";
    private const string DuckPrefabPath = "Assets/Resources/Prefabs/Animals/Pato.prefab";
    private const string DeerPrefabPath = "Assets/Resources/Prefabs/Animals/Venado.prefab";

    // ACCION: preparador opcional para una escena nueva; se conserva solo para uso manual.
    // MODIFICACION: no se registra al abrir Unity ni al iniciar Play Mode.
    private static void RunAutomaticSetupOnce()
    {
        // MODIFICACION: una vez creado el marcador, este script no debe tocar Play Mode.
        // Unity vuelve a ejecutar InitializeOnLoad tras recargar assemblies; comprobarlo
        // primero evita que el juego se cierre al iniciar normalmente.
        if (File.Exists(AppliedMarkerPath))
            return;

        if (EditorApplication.isPlaying)
        {
            // ACCION: esperar al siguiente regreso al Editor sin forzar la salida del juego.
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunAutomaticSetupOnce;
            return;
        }

        PrepareTwoLevels();
        if (GameObject.Find(ProgressionRootName) == null)
            return;

        File.WriteAllText(AppliedMarkerPath, "Xunjuu v0.1 - niveles 1 y 2 preparados el 2026-07-14.");
        AssetDatabase.ImportAsset(AppliedMarkerPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Xunjuu/Preparar niveles 1 y 2 editables %&n", priority = 0)]
    public static void PrepareTwoLevels()
    {
        DyanatroGameDirector director = Object.FindFirstObjectByType<DyanatroGameDirector>();
        if (director == null)
        {
            Debug.LogError("Xunjuu v0.1: no se encontro DyanatroGameDirector en la escena.");
            return;
        }

        // ACCION: actualizar Pato y Venado para que incluyan vida del nivel 2.
        XunjuuPlayerCombatAnimatorBuilder.RepairPlayerCombatAnimator();
        XunjuuAnimalPrefabBuilder.BuildAll();
        XunjuuOcelotlPrefabBuilder.BuildOcelotlPrefab();
        CreatePendingRewardPrefab();
        GameObject enemyPrefab = CreateOrLoadEnemyPrefab();
        RemoveUnusedLegacyAnimationFromEnemyPrefab();

        GameObject duckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DuckPrefabPath);
        GameObject deerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeerPrefabPath);
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        GameObject weaponRewardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponRewardPath);
        GameObject pendingRewardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PendingRewardPath);

        if (enemyPrefab == null || duckPrefab == null || deerPrefab == null || bossPrefab == null || weaponRewardPrefab == null)
        {
            Debug.LogError("Xunjuu v0.1: faltan prefabs para preparar los niveles.");
            return;
        }

        RemoveExistingProgressionObjects();
        GameObject progressionRoot = new GameObject(ProgressionRootName);

        // ACCION: crear una recompensa del nivel 1 que no muestra el arma antes de tiempo.
        GameObject levelOneRewardObject = (GameObject)PrefabUtility.InstantiatePrefab(weaponRewardPrefab, progressionRoot.scene);
        levelOneRewardObject.name = "Mision_1_Recompensa_Macuahuitl";
        levelOneRewardObject.transform.SetParent(progressionRoot.transform, false);
        XunjuuSecondaryMissionWeaponReward levelOneReward = levelOneRewardObject.GetComponent<XunjuuSecondaryMissionWeaponReward>();

        GameObject levelTwoRoot = new GameObject("Misiones_2_3_4_Editables");
        levelTwoRoot.transform.SetParent(progressionRoot.transform, false);
        XunjuuLevel2KillMission levelTwoMission = levelTwoRoot.AddComponent<XunjuuLevel2KillMission>();

        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None)
            .Where(terrain => terrain != null && terrain.terrainData != null)
            .OrderBy(terrain => terrain.name)
            .ToArray();
        if (terrains.Length == 0)
        {
            Object.DestroyImmediate(progressionRoot);
            Debug.LogError("Xunjuu v0.1: no hay Terrain para colocar los objetivos.");
            return;
        }

        PlayerController scenePlayer = Object.FindFirstObjectByType<PlayerController>();
        ConfigurePlayerCombatTimings(scenePlayer);
        Vector3 missionAnchor = scenePlayer != null ? scenePlayer.transform.position : terrains[0].transform.position + terrains[0].terrainData.size * 0.5f;

        GameObject enemyRoot = new GameObject("Mision_3_Enemigos_5_Dyanatro");
        enemyRoot.transform.SetParent(levelTwoRoot.transform, false);
        CreateEditableEnemies(enemyPrefab, enemyRoot.transform, terrains, missionAnchor, 5);

        GameObject animalRoot = new GameObject("Mision_2_Animales_6_Pato_Venado");
        animalRoot.transform.SetParent(levelTwoRoot.transform, false);
        CreateEditableAnimals(duckPrefab, deerPrefab, animalRoot.transform, terrains, missionAnchor);

        Transform bossPoint = CreatePoint(
            "Punto_Aparicion_Jefe_Final",
            levelTwoRoot.transform,
            GetMissionPlacement(terrains, missionAnchor, 0, 1, 44f, 32f));
        Transform rewardPoint = CreatePoint("Punto_Recompensa_Nivel_2", levelTwoRoot.transform, bossPoint.position + new Vector3(3f, 0f, 3f));

        // ACCION: conectar todos los objetos sin ocultar campos en Inspector.
        SerializedObject missionData = new SerializedObject(levelTwoMission);
        missionData.FindProperty("enemiesToDefeat").intValue = 5;
        missionData.FindProperty("autoCountEnemiesFromHierarchy").boolValue = false;
        missionData.FindProperty("animalsToDefeat").intValue = 6;
        missionData.FindProperty("requireAnimalDefeats").boolValue = true;
        missionData.FindProperty("autoCountAnimalsFromHierarchy").boolValue = true;
        missionData.FindProperty("missionActive").boolValue = false;
        missionData.FindProperty("enemyGroupRoot").objectReferenceValue = enemyRoot;
        missionData.FindProperty("animalGroupRoot").objectReferenceValue = animalRoot;
        missionData.FindProperty("repositionObjectivesNearPlayer").boolValue = true;
        missionData.FindProperty("missionMinimumDistance").floatValue = 24f;
        missionData.FindProperty("missionMaximumDistance").floatValue = 48f;
        missionData.FindProperty("missionMinimumSpacing").floatValue = 10f;
        missionData.FindProperty("animalMinimumSpacing").floatValue = 14f;
        missionData.FindProperty("enemyMinimumSpacing").floatValue = 10f;
        missionData.FindProperty("bossMinimumDistance").floatValue = 32f;
        missionData.FindProperty("bossMaximumDistance").floatValue = 44f;
        missionData.FindProperty("bossClearRadius").floatValue = 11f;
        missionData.FindProperty("finalBossPrefab").objectReferenceValue = bossPrefab;
        missionData.FindProperty("finalBossSpawnPoint").objectReferenceValue = bossPoint;
        missionData.FindProperty("rewardPrefab").objectReferenceValue = pendingRewardPrefab;
        missionData.FindProperty("rewardSpawnPoint").objectReferenceValue = rewardPoint;
        missionData.FindProperty("phaseBannerDuration").floatValue = 5f;
        missionData.ApplyModifiedPropertiesWithoutUndo();

        enemyRoot.SetActive(false);
        animalRoot.SetActive(false);

        // ACCION: fijar cinco flores y bosques con diez unidades de separacion.
        SerializedObject directorData = new SerializedObject(director);
        directorData.FindProperty("mazahuaWordGoal").intValue = 5;
        directorData.FindProperty("levelOneCollectibleCount").intValue = 5;
        directorData.FindProperty("levelOneWeaponReward").objectReferenceValue = levelOneReward;
        directorData.FindProperty("levelTwoMission").objectReferenceValue = levelTwoMission;
        directorData.FindProperty("treesPerTerrain").intValue = 80;
        directorData.FindProperty("treeMinimumSpacing").floatValue = 10f;
        directorData.FindProperty("forestRadiusPerTerrain").floatValue = 90f;
        directorData.FindProperty("treeMinDistanceFromPlayer").floatValue = 6f;
        directorData.FindProperty("treeClearanceFromObjectives").floatValue = 9f;
        directorData.FindProperty("treeClearanceFromRoutes").floatValue = 4.5f;
        directorData.FindProperty("bushCount").intValue = 1320;
        directorData.FindProperty("tallGrassPatchCount").intValue = 380;
        directorData.FindProperty("tallGrassRadius").floatValue = 105f;
        directorData.FindProperty("cornFieldCount").intValue = 10;
        directorData.FindProperty("cornRowsPerField").intValue = 5;
        directorData.FindProperty("cornStalksPerRow").intValue = 10;
        directorData.FindProperty("cornFieldRadius").floatValue = 105f;
        directorData.FindProperty("cornFieldMinimumSpacing").floatValue = 28f;
        directorData.FindProperty("wordCollectibleMinDistanceFromPlayer").floatValue = 14f;
        directorData.FindProperty("wordCollectibleMinDistanceFromEnemy").floatValue = 12f;
        directorData.FindProperty("showControlsSidebar").boolValue = true;
        directorData.FindProperty("controlsSidebarPosition").vector2Value = new Vector2(-22f, -180f);
        directorData.FindProperty("controlsSidebarSize").vector2Value = new Vector2(332f, 250f);
        directorData.FindProperty("menuMusicVolume").floatValue = 0.82f;
        directorData.FindProperty("gameplayMusicReduction").floatValue = 0.55f;
        directorData.FindProperty("musicFadeDuration").floatValue = 0.85f;
        directorData.FindProperty("prologueBeatDuration").floatValue = 6.75f;
        directorData.FindProperty("missionFeedbackDuration").floatValue = 3.8f;
        directorData.FindProperty("completedFeedbackDuration").floatValue = 6f;
        directorData.ApplyModifiedPropertiesWithoutUndo();

        director.ConfigureWordPlacement(26f, 48f);
        director.GenerateEditableMazahuaWordCollectibles();
        director.GenerateEditableForest();
        director.GenerateEditableCornFields();

        EditorUtility.SetDirty(director);
        EditorUtility.SetDirty(levelTwoMission);
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = progressionRoot;

        Debug.Log("[XUNJUU BETA PREPARADA] 5 flores -> Macuahuitl -> 6 animales -> 8 Dyanatr'o -> Orbitasword -> Ocelotl -> nivel completado; bosques restaurados.");
    }

    // ACCION: quitar el componente Animation antiguo de FirePoint. El ataque
    // usa Animator y proyectiles, por lo que este componente solo generaba avisos Legacy.
    private static void RemoveUnusedLegacyAnimationFromEnemyPrefab()
    {
        if (!File.Exists(EnemyPrefabPath))
            return;

        GameObject contents = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
        try
        {
            foreach (Animation animation in contents.GetComponentsInChildren<Animation>(true))
                Object.DestroyImmediate(animation);

            // Xunjuu v0.1 - ACCION: restaurar el lanzamiento de proyectiles
            // incluso cuando el prefab conserva valores antiguos del Inspector.
            EnemyFireBreath fireEnemy = contents.GetComponent<EnemyFireBreath>();
            if (fireEnemy != null)
            {
                SerializedObject enemyData = new SerializedObject(fireEnemy);
                enemyData.FindProperty("detectionRange").floatValue = 14f;
                enemyData.FindProperty("attackRange").floatValue = 8f;
                enemyData.FindProperty("attackCooldown").floatValue = 2.25f;
                enemyData.FindProperty("attackWindup").floatValue = 0.3f;
                enemyData.FindProperty("attackRecovery").floatValue = 0.62f;
                enemyData.FindProperty("contactDamageCooldown").floatValue = 1f;
                enemyData.FindProperty("footstepInterval").floatValue = 0.54f;
                enemyData.FindProperty("wanderSpeed").floatValue = 1.85f;
                enemyData.FindProperty("wanderRadius").floatValue = 9f;
                enemyData.FindProperty("idleTime").floatValue = 1.1f;
                enemyData.FindProperty("wanderRetargetInterval").floatValue = 5f;
                enemyData.FindProperty("obstacleCheckDistance").floatValue = 1.35f;
                enemyData.FindProperty("fireballSpeed").floatValue = 6.4f;
                enemyData.FindProperty("projectileHeight").floatValue = 0.85f;
                enemyData.FindProperty("projectileForwardOffset").floatValue = 0.68f;
                enemyData.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform firePoint = contents.transform.Find("FirePoint");
            if (firePoint != null)
            {
                firePoint.localPosition = new Vector3(0.55f, 0.72f, 0f);
                firePoint.localScale = Vector3.one;
            }
            PrefabUtility.SaveAsPrefabAsset(contents, EnemyPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // ACCION: guardar el enemigo existente como prefab reutilizable del nivel 2.
    private static GameObject CreateOrLoadEnemyPrefab()
    {
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (existingPrefab != null)
            return existingPrefab;

        EnemyHealth sourceHealth = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(health => health != null
                && health.gameObject.scene.IsValid()
                && health.GetComponent<XunjuuBossNivel1>() == null);
        GameObject sourceObject = sourceHealth != null ? sourceHealth.gameObject : null;

        // ACCION: aceptar tambien el enemigo de fuego usado por la escena original.
        if (sourceObject == null)
        {
            EnemyFireBreath fireSource = Object.FindObjectsByType<EnemyFireBreath>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(enemy => enemy != null && enemy.gameObject.scene.IsValid());
            sourceObject = fireSource != null ? fireSource.gameObject : null;
        }

        if (sourceObject == null)
            return null;

        return PrefabUtility.SaveAsPrefabAsset(sourceObject, EnemyPrefabPath);
    }

    // ACCION: crear el prefab vacio que podra reemplazarse mas adelante.
    private static void CreatePendingRewardPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PendingRewardPath);
        if (existing != null)
            return;

        GameObject reward = new GameObject("Recompensa_Nivel2_Pendiente");
        try
        {
            reward.AddComponent<XunjuuRewardPlaceholder>();
            PrefabUtility.SaveAsPrefabAsset(reward, PendingRewardPath);
        }
        finally
        {
            Object.DestroyImmediate(reward);
        }
    }

    private static void RemoveExistingProgressionObjects()
    {
        GameObject oldRoot = GameObject.Find(ProgressionRootName);
        if (oldRoot != null)
            Object.DestroyImmediate(oldRoot);

        // Xunjuu v0.1 - ACCION: conservar enemigos, decoraciones y prefabs
        // colocados manualmente fuera del grupo administrado por este asistente.
    }

    private static void CreateEditableEnemies(GameObject prefab, Transform parent, Terrain[] terrains, Vector3 anchor, int count)
    {
        for (int index = 0; index < count; index++)
        {
            GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            enemy.name = $"Enemigo_Nivel2_{index + 1:00}";
            enemy.transform.SetParent(parent, true);
            enemy.transform.position = GetMissionPlacement(terrains, anchor, index, count, 44f, 17f);
            XunjuuTerrainGrounding grounding = enemy.GetComponent<XunjuuTerrainGrounding>();
            if (grounding == null)
                grounding = enemy.AddComponent<XunjuuTerrainGrounding>();
            grounding.SnapNow();
            XunjuuWorldLabel label = enemy.GetComponent<XunjuuWorldLabel>();
            if (label == null)
                label = enemy.AddComponent<XunjuuWorldLabel>();
            label.Configure("Mazahua: Dyanatr'o\nEspanol: sombra del olvido", true, new Vector3(0f, 2.1f, 0f));
            enemy.SetActive(true);
        }
    }

    // ACCION: separar patos y venados en zonas logicas, editables y sin trampas.
    private static void CreateEditableAnimals(GameObject duck, GameObject deer, Transform parent, Terrain[] terrains, Vector3 anchor)
    {
        GameObject duckZone = new GameObject("Zona_Patos_Tizi");
        duckZone.transform.SetParent(parent, false);
        GameObject deerZone = new GameObject("Zona_Venados_Pjantreje");
        deerZone.transform.SetParent(parent, false);

        CreateAnimalZone(duck, duckZone.transform, terrains, anchor, 3, 30f, 12f, "tizi", "pato");
        CreateAnimalZone(deer, deerZone.transform, terrains, anchor, 3, 38f, 192f, "pjantr'eje", "venado");
    }

    private static void CreateAnimalZone(
        GameObject prefab,
        Transform parent,
        Terrain[] terrains,
        Vector3 anchor,
        int count,
        float radius,
        float angleOffset,
        string mazahuaName,
        string spanishName)
    {
        for (int index = 0; index < count; index++)
        {
            GameObject animal = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            animal.name = $"{prefab.name}_Mision2_{index + 1:00}";
            animal.transform.SetParent(parent, true);
            animal.transform.position = GetMissionPlacement(terrains, anchor, index, count, radius, angleOffset);
            if (animal.GetComponent<XunjuuAnimalHealth>() == null)
                animal.AddComponent<XunjuuAnimalHealth>();
            XunjuuWorldLabel label = animal.GetComponent<XunjuuWorldLabel>();
            if (label == null)
                label = animal.AddComponent<XunjuuWorldLabel>();
            label.Configure($"Mazahua: {mazahuaName}\nEspanol: {spanishName}", true, new Vector3(0f, 1.8f, 0f));
            animal.SetActive(true);
        }
    }

    // ACCION: colocar cada fase cerca del jugador y siempre sobre un Terrain valido.
    private static Vector3 GetMissionPlacement(Terrain[] terrains, Vector3 anchor, int index, int total, float radius, float angleOffset)
    {
        Vector3 fallback = anchor;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            float angle = (angleOffset + (360f / Mathf.Max(1, total)) * index + attempt * 37f) * Mathf.Deg2Rad;
            float adjustedRadius = radius + (attempt % 3) * 2.5f;
            Vector3 point = anchor + new Vector3(Mathf.Cos(angle) * adjustedRadius, 0f, Mathf.Sin(angle) * adjustedRadius);
            Terrain terrain = terrains.FirstOrDefault(candidate => IsPointInsideTerrain(candidate, point));
            if (terrain == null)
                continue;

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            point.x = Mathf.Clamp(point.x, origin.x + 6f, origin.x + size.x - 6f);
            point.z = Mathf.Clamp(point.z, origin.z + 6f, origin.z + size.z - 6f);
            point.y = terrain.SampleHeight(point) + origin.y + 0.12f;
            fallback = point;
            Physics.SyncTransforms();
            if (IsPlacementClear(point, 2.8f))
                return point;
        }
        return fallback;
    }

    private static bool IsPointInsideTerrain(Terrain terrain, Vector3 point)
    {
        if (terrain == null || terrain.terrainData == null)
            return false;
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        return point.x >= origin.x && point.x <= origin.x + size.x
            && point.z >= origin.z && point.z <= origin.z + size.z;
    }

    // ACCION: repartir objetivos entre todos los terrenos y pegarlos a su altura.
    private static Vector3 GetPlacement(Terrain[] terrains, int index, int total, float radius)
    {
        Terrain terrain = terrains[Mathf.Abs(index) % terrains.Length];
        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 center = new Vector3(
            terrainOrigin.x + terrainSize.x * 0.5f,
            terrainOrigin.y,
            terrainOrigin.z + terrainSize.z * 0.5f);
        Vector3 fallback = center;
        for (int attempt = 0; attempt < 28; attempt++)
        {
            float angle = ((360f / Mathf.Max(1, total)) * index + attempt * 47f) * Mathf.Deg2Rad;
            float adjustedRadius = radius + (attempt % 4) * 4f;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * adjustedRadius, 0f, Mathf.Sin(angle) * adjustedRadius);
            point.x = Mathf.Clamp(point.x, terrainOrigin.x + 5f, terrainOrigin.x + terrainSize.x - 5f);
            point.z = Mathf.Clamp(point.z, terrainOrigin.z + 5f, terrainOrigin.z + terrainSize.z - 5f);
            point.y = terrain.SampleHeight(point) + terrainOrigin.y + 0.15f;
            fallback = point;
            Physics.SyncTransforms();
            if (IsPlacementClear(point, 2.4f))
                return point;
        }
        return fallback;
    }

    // ACCION: evitar animales, enemigos o jefe dentro de arboles y decoraciones.
    private static bool IsPlacementClear(Vector3 point, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(point + Vector3.up * 0.8f, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit == null || hit is TerrainCollider)
                continue;
            if (hit.GetComponentInParent<PlayerController>() != null)
                return false;
            if (hit.GetComponentInParent<TreeInteractivo>() != null
                || hit.GetComponentInParent<EnemyHealth>() != null
                || hit.GetComponentInParent<EnemyFireBreath>() != null
                || hit.GetComponentInParent<Animal>() != null)
                return false;
        }
        return true;
    }

    private static Transform CreatePoint(string objectName, Transform parent, Vector3 position)
    {
        GameObject pointObject = new GameObject(objectName);
        pointObject.transform.SetParent(parent, true);
        pointObject.transform.position = position;
        return pointObject.transform;
    }

    // ========================================================================
    // Xunjuu v0.1 - Tiempos del protagonista
    // ACCION: guardar en la escena los cooldowns validados sin tocar sus assets.
    // ========================================================================
    private static void ConfigurePlayerCombatTimings(PlayerController player)
    {
        if (player == null)
            return;

        SerializedObject playerData = new SerializedObject(player);
        playerData.FindProperty("attackRate").floatValue = 0.62f;
        playerData.FindProperty("attackDuration").floatValue = 0.48f;
        playerData.FindProperty("basicAttackImpactDelay").floatValue = 0.16f;
        playerData.FindProperty("swordAttackCooldown").floatValue = 0.72f;
        playerData.FindProperty("swordAttackImpactDelay").floatValue = 0.15f;
        playerData.FindProperty("orbitalAttackCooldown").floatValue = 5f;
        playerData.FindProperty("footstepInterval").floatValue = 0.5f;
        playerData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(player);
    }
}
