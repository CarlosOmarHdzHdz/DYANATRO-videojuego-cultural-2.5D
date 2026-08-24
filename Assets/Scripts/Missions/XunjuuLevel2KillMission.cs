using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// ============================================================================
// Xunjuu v0.1 - Progresion secuencial del primer nivel
// ACCION: activar 6 animales, despues 8 enemigos y finalmente al jefe Ocelotl.
// MODIFICACION: grupos, cantidades, puntos y eventos quedan editables en Inspector.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuLevel2KillMission : MonoBehaviour
{
    public enum MissionPhase
    {
        Locked,
        Animals,
        Enemies,
        Boss,
        Completed
    }

    [Header("Xunjuu v0.1 - Objetivos")]
    [SerializeField, Min(1)] private int animalsToDefeat = 6;
    [SerializeField, Min(1)] private int enemiesToDefeat = 8;
    [SerializeField] private bool requireAnimalDefeats = true;
    [SerializeField] private bool autoCountAnimalsFromHierarchy = true;
    [SerializeField] private bool autoCountEnemiesFromHierarchy = true;
    [SerializeField] private string requiredEnemyTag = "Enemy";
    [SerializeField] private string[] allowedAnimalNames = { "Pato", "Venado" };
    [SerializeField] private bool missionActive;

    [Header("Xunjuu v0.1 - Contenido editable en Hierarchy")]
    [SerializeField] private GameObject enemyGroupRoot;
    [SerializeField] private GameObject animalGroupRoot;
    [SerializeField] private bool controlGroupActivation = true;

    [Header("Xunjuu v0.1 - Distancia entre misiones")]
    [SerializeField] private bool repositionObjectivesNearPlayer = true;
    [SerializeField, Range(12f, 35f)] private float missionMinimumDistance = 24f;
    [SerializeField, Range(30f, 50f)] private float missionMaximumDistance = 48f;
    [SerializeField, Range(3f, 16f)] private float missionMinimumSpacing = 10f;
    [SerializeField, Range(8f, 18f)] private float animalMinimumSpacing = 14f;
    [SerializeField, Range(6f, 16f)] private float enemyMinimumSpacing = 10f;

    [Header("Xunjuu v0.1 - Jefe final")]
    [SerializeField] private GameObject finalBossPrefab;
    [SerializeField] private Transform finalBossSpawnPoint;
    [SerializeField, Range(24f, 40f)] private float bossMinimumDistance = 32f;
    [SerializeField, Range(32f, 50f)] private float bossMaximumDistance = 44f;
    [SerializeField, Range(6f, 16f)] private float bossClearRadius = 11f;

    [Header("Xunjuu v0.1 - Recompensa final")]
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private Transform rewardSpawnPoint;

    [Header("Xunjuu v0.1 - Interfaz")]
    [SerializeField] private bool createMissionHud = true;
    [SerializeField] private Text missionText;
    [SerializeField, Range(3f, 8f)] private float phaseBannerDuration = 5f;

    [Header("Xunjuu v0.1 - Eventos editables")]
    [SerializeField] private UnityEvent onMissionStarted;
    [SerializeField] private UnityEvent onAnimalMissionCompleted;
    [SerializeField] private UnityEvent onOrbitalUnlocked;
    [SerializeField] private UnityEvent onObjectivesCompleted;
    [SerializeField] private UnityEvent onBossSpawned;
    [SerializeField] private UnityEvent onMissionCompleted;

    private readonly HashSet<int> countedEnemies = new HashSet<int>();
    private readonly HashSet<int> countedAnimals = new HashSet<int>();
    private int defeatedEnemies;
    private int defeatedAnimals;
    private bool objectivesCompleted;
    private bool completed;
    private bool defeatEventsSubscribed;
    private bool bossSpawned;
    private MissionPhase phase = MissionPhase.Locked;
    private XunjuuBossNivel1 spawnedBoss;
    private GameObject spawnedReward;
    private GameObject missionHudRoot;
    private Text phaseBannerText;
    private Coroutine phaseBannerRoutine;

    // Xunjuú v0.1 - ACCION: permitir que sistemas independientes reaccionen
    // al final del nivel sin cambiar el orden de sus misiones.
    public event System.Action MissionCompleted;

    public int DefeatedEnemies => defeatedEnemies;
    public int DefeatedAnimals => defeatedAnimals;
    public int EnemiesToDefeat => enemiesToDefeat;
    public int AnimalsToDefeat => animalsToDefeat;
    public int RemainingEnemies => Mathf.Max(0, enemiesToDefeat - defeatedEnemies);
    public int RemainingAnimals => Mathf.Max(0, animalsToDefeat - defeatedAnimals);
    public bool RequireAnimalDefeats => requireAnimalDefeats;
    public bool MissionActive => phase == MissionPhase.Animals || phase == MissionPhase.Enemies;
    public bool ObjectivesCompleted => objectivesCompleted;
    public bool Completed => completed;
    public bool BossSpawned => bossSpawned;
    public bool EnemyGroupVisible => enemyGroupRoot != null && enemyGroupRoot.activeSelf;
    public bool AnimalGroupVisible => animalGroupRoot != null && animalGroupRoot.activeSelf;
    public bool IsAnimalPhase => phase == MissionPhase.Animals;
    public bool IsEnemyPhase => phase == MissionPhase.Enemies;
    public MissionPhase CurrentPhase => phase;

    private void Awake()
    {
        if (createMissionHud && missionText == null)
            BuildMissionHud();

        phase = missionActive ? MissionPhase.Animals : MissionPhase.Locked;
        ApplyPhaseVisibility();
        SetMissionHudActive(phase != MissionPhase.Locked);
        RefreshText();
    }

    private void OnEnable()
    {
        SubscribeToDefeatEvents();
    }

    private void Start()
    {
        if (phase == MissionPhase.Animals)
            onMissionStarted?.Invoke();
    }

    private void OnDisable()
    {
        UnsubscribeFromDefeatEvents();
        UnsubscribeFromBoss();
    }

    // ACCION: escuchar derrotas globales y filtrarlas por grupo y fase.
    private void SubscribeToDefeatEvents()
    {
        XunjuuEnemyDefeatEvents.EnemyDefeated -= RegisterEnemyDefeat;
        XunjuuAnimalDefeatEvents.AnimalDefeated -= RegisterAnimalDefeat;
        XunjuuEnemyDefeatEvents.EnemyDefeated += RegisterEnemyDefeat;
        XunjuuAnimalDefeatEvents.AnimalDefeated += RegisterAnimalDefeat;
        defeatEventsSubscribed = true;
    }

    private void UnsubscribeFromDefeatEvents()
    {
        if (!defeatEventsSubscribed)
            return;
        XunjuuEnemyDefeatEvents.EnemyDefeated -= RegisterEnemyDefeat;
        XunjuuAnimalDefeatEvents.AnimalDefeated -= RegisterAnimalDefeat;
        defeatEventsSubscribed = false;
    }

    // ========================================================================
    // Xunjuu v0.1 - Mision 2
    // ACCION: mostrar solo patos y venados al recibir el Macuahuitl.
    // ========================================================================
    [ContextMenu("Xunjuu v0.1/Iniciar mision de animales")]
    public void StartMission()
    {
        if (phase != MissionPhase.Locked || completed)
            return;

        SubscribeToDefeatEvents();
        if (autoCountAnimalsFromHierarchy)
        {
            int hierarchyCount = CountAnimalsInHierarchy();
            if (hierarchyCount > 0)
                animalsToDefeat = hierarchyCount;
        }

        phase = MissionPhase.Animals;
        missionActive = true;
        ApplyPhaseVisibility();
        RepositionActiveGroup(animalGroupRoot, 18f, false, animalMinimumSpacing);
        SetMissionHudActive(true);
        RefreshText();
        ShowPhaseBanner(
            "MISION 2 - FAUNA\n"
            + "Mazahua: Tizi, pjantr'eje: 6.\n"
            + "Español: Patos, venados: 6.");
        onMissionStarted?.Invoke();
    }

    // ACCION: contar un animal una sola vez y abrir la mision de enemigos.
    public void RegisterAnimalDefeat(GameObject animal)
    {
        if (phase != MissionPhase.Animals || !CanCount(animal) || !IsAllowedAnimal(animal.name))
            return;
        if (!BelongsToGroup(animal, animalGroupRoot) || !countedAnimals.Add(animal.GetInstanceID()))
            return;

        defeatedAnimals = Mathf.Min(animalsToDefeat, defeatedAnimals + 1);
        RefreshText();
        if (defeatedAnimals >= animalsToDefeat)
            BeginEnemyMission();
    }

    // ========================================================================
    // Xunjuu v0.1 - Mision 3
    // ACCION: retirar animales derrotados y mostrar solamente ocho Dyanatr'o.
    // ========================================================================
    private void BeginEnemyMission()
    {
        if (phase != MissionPhase.Animals)
            return;

        if (autoCountEnemiesFromHierarchy)
        {
            int hierarchyCount = CountEnemiesInHierarchy();
            if (hierarchyCount > 0)
                enemiesToDefeat = hierarchyCount;
        }

        phase = MissionPhase.Enemies;
        missionActive = true;
        ApplyPhaseVisibility();
        RepositionActiveGroup(enemyGroupRoot, 198f, true, enemyMinimumSpacing);
        RefreshText();
        ShowPhaseBanner(
            "MISION 3 - LAS SOMBRAS\n"
            + "Mazahua: Jñincho Dyanatr'o.\n"
            + "Español: Ocho Dyanatr'o.");
        onAnimalMissionCompleted?.Invoke();
    }

    // ACCION: contar un enemigo comun una sola vez durante su fase.
    public void RegisterEnemyDefeat(GameObject enemy)
    {
        if (phase != MissionPhase.Enemies || !CanCount(enemy) || enemy.GetComponentInParent<XunjuuBossNivel1>() != null)
            return;
        if (!BelongsToGroup(enemy, enemyGroupRoot))
            return;
        if (!string.IsNullOrWhiteSpace(requiredEnemyTag) && !enemy.CompareTag(requiredEnemyTag))
            return;
        if (!countedEnemies.Add(enemy.GetInstanceID()))
            return;

        defeatedEnemies = Mathf.Min(enemiesToDefeat, defeatedEnemies + 1);
        RefreshText();
        if (defeatedEnemies >= enemiesToDefeat)
            CompleteEnemiesAndSpawnBoss();
    }

    private bool CanCount(GameObject target)
    {
        return !objectivesCompleted && !completed && target != null;
    }

    private bool IsAllowedAnimal(string animalName)
    {
        if (allowedAnimalNames == null || allowedAnimalNames.Length == 0)
            return true;
        foreach (string allowedName in allowedAnimalNames)
        {
            if (!string.IsNullOrWhiteSpace(allowedName)
                && animalName.IndexOf(allowedName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    // ACCION: entregar Orbitasword despues del octavo enemigo y crear al jefe.
    private void CompleteEnemiesAndSpawnBoss()
    {
        if (objectivesCompleted || phase != MissionPhase.Enemies)
            return;

        objectivesCompleted = true;
        missionActive = false;
        phase = MissionPhase.Boss;
        ApplyPhaseVisibility();
        UnlockOrbitalAttack();
        RefreshText();
        ShowPhaseBanner(
            "ATAQUE ESPECIAL OBTENIDO\n"
            + "Mazahua: Orbitasword: E.\n"
            + "Español: Orbitasword: E.");
        onOrbitalUnlocked?.Invoke();
        onObjectivesCompleted?.Invoke();
        SpawnFinalBoss();
    }

    private void UnlockOrbitalAttack()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
            return;

        player.UnlockOrbitalAttack();
        XunjuuInventory inventory = player.GetComponent<XunjuuInventory>();
        OrbitalWeapon weapon = player.GetComponentInChildren<OrbitalWeapon>(true);
        Sprite icon = weapon != null ? weapon.GetComponentInChildren<SpriteRenderer>()?.sprite : null;
        if (inventory != null)
            inventory.AddItem("orbitasword", "Orbitasword - Ataque especial", 1, icon);
    }

    // ACCION: completar rapidamente la fase actual desde Inspector para pruebas.
    [ContextMenu("Xunjuu v0.1/Completar fase actual")]
    public void CompleteMission()
    {
        if (phase == MissionPhase.Animals)
        {
            defeatedAnimals = animalsToDefeat;
            BeginEnemyMission();
        }
        else if (phase == MissionPhase.Enemies)
        {
            defeatedEnemies = enemiesToDefeat;
            CompleteEnemiesAndSpawnBoss();
        }
    }

    private void SpawnFinalBoss()
    {
        if (bossSpawned || phase != MissionPhase.Boss)
            return;
        if (finalBossPrefab == null)
        {
            Debug.LogWarning("Xunjuu v0.1: asigna el prefab del jefe final.");
            return;
        }

        Vector3 position = finalBossSpawnPoint != null ? finalBossSpawnPoint.position : transform.position;
        Vector3 playerPosition = GetPlayerPosition(position);
        if (repositionObjectivesNearPlayer)
        {
            // ====================================================================
            // Xunjuu v0.1 - Punto seguro del jefe
            // ACCION: conservar primero el claro editable de Hierarchy cuando esta
            // a una distancia jugable; solo buscar otro punto si deja de ser valido.
            // ====================================================================
            if (!TryGetConfiguredBossPoint(playerPosition, out position))
                position = FindGroundedBossPoint(playerPosition);
        }
        Quaternion rotation = finalBossSpawnPoint != null ? finalBossSpawnPoint.rotation : Quaternion.identity;
        bossSpawned = true;
        GameObject bossObject = Instantiate(finalBossPrefab, position, rotation);
        bossObject.name = "Jefe_Final_Ocelotl";
        XunjuuTerrainGrounding bossGrounding = bossObject.GetComponent<XunjuuTerrainGrounding>();
        if (bossGrounding == null)
            bossGrounding = bossObject.AddComponent<XunjuuTerrainGrounding>();
        bossGrounding.SnapNow();
        spawnedBoss = bossObject.GetComponent<XunjuuBossNivel1>();
        if (spawnedBoss == null)
        {
            Debug.LogError("Xunjuu v0.1: Ocelotl necesita XunjuuBossNivel1.");
            bossSpawned = false;
            Destroy(bossObject);
            return;
        }

        XunjuuWorldLabel label = bossObject.GetComponent<XunjuuWorldLabel>();
        if (label == null)
            label = bossObject.AddComponent<XunjuuWorldLabel>();
        label.Configure("Mazahua: Gran Dyanatr'o / Ocelotl\nEspañol: Gran Dyanatr'o / Ocelotl", true, new Vector3(0f, 2.75f, 0f));

        spawnedBoss.Defeated += HandleBossDefeated;
        onBossSpawned?.Invoke();
        RefreshText();
        ShowPhaseBanner(
            "JEFE FINAL\n"
            + "Mazahua: Gran Dyanatr'o / Ocelotl.\n"
            + "Español: Gran Dyanatr'o / Ocelotl.");
    }

    private void HandleBossDefeated(XunjuuBossNivel1 boss)
    {
        if (boss != spawnedBoss || completed)
            return;

        UnsubscribeFromBoss();
        completed = true;
        phase = MissionPhase.Completed;
        SpawnReward();
        RefreshText();
        ShowPhaseBanner(
            "NIVEL 1 COMPLETADO\n"
            + "Mazahua: Tsicha ndájná: Otontecuhtli. Jñiñi tsasú.\n"
            + "Español: Cinco flores: Otontecuhtli. Pueblo protegido.");
        onMissionCompleted?.Invoke();
        MissionCompleted?.Invoke();
    }

    private void SpawnReward()
    {
        if (rewardPrefab == null || spawnedReward != null)
            return;
        Vector3 position = rewardSpawnPoint != null ? rewardSpawnPoint.position : transform.position + Vector3.up;
        Quaternion rotation = rewardSpawnPoint != null ? rewardSpawnPoint.rotation : Quaternion.identity;
        spawnedReward = Instantiate(rewardPrefab, position, rotation);
        spawnedReward.name = "Recompensa_Final_Nivel_1";
    }

    private void UnsubscribeFromBoss()
    {
        if (spawnedBoss != null)
            spawnedBoss.Defeated -= HandleBossDefeated;
    }

    // ACCION: reiniciar fases, contadores y apariciones sin duplicar contenido.
    [ContextMenu("Xunjuu v0.1/Reiniciar progresion")]
    public void ResetMission()
    {
        UnsubscribeFromBoss();
        if (spawnedBoss != null)
            Destroy(spawnedBoss.gameObject);
        if (spawnedReward != null)
            Destroy(spawnedReward);

        countedEnemies.Clear();
        countedAnimals.Clear();
        defeatedEnemies = 0;
        defeatedAnimals = 0;
        objectivesCompleted = false;
        completed = false;
        missionActive = false;
        bossSpawned = false;
        phase = MissionPhase.Locked;
        spawnedBoss = null;
        spawnedReward = null;
        ApplyPhaseVisibility();
        SetMissionHudActive(false);
        RefreshText();
    }

    // ACCION: devolver el objetivo activo mas cercano para la flecha del HUD.
    public Transform FindNearestActiveObjective(Vector3 origin)
    {
        if (phase == MissionPhase.Animals)
            return FindNearestComponent<XunjuuAnimalHealth>(animalGroupRoot, origin);
        if (phase == MissionPhase.Enemies)
            return FindNearestEnemy(origin);
        if (phase == MissionPhase.Boss && spawnedBoss != null)
            return spawnedBoss.transform;
        return null;
    }

    public string GetGuideLabel()
    {
        if (phase == MissionPhase.Animals)
            return "Animal de mision";
        if (phase == MissionPhase.Enemies)
            return "Dyanatr'o";
        if (phase == MissionPhase.Boss)
            return "Ocelotl";
        return string.Empty;
    }

    private Transform FindNearestComponent<T>(GameObject root, Vector3 origin) where T : Component
    {
        if (root == null)
            return null;
        Transform nearest = null;
        float best = float.MaxValue;
        foreach (T component in root.GetComponentsInChildren<T>(false))
        {
            if (component == null || !component.gameObject.activeInHierarchy)
                continue;
            float distance = HorizontalDistance(origin, component.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = component.transform;
            }
        }
        return nearest;
    }

    private Transform FindNearestEnemy(Vector3 origin)
    {
        if (enemyGroupRoot == null)
            return null;
        HashSet<Transform> candidates = new HashSet<Transform>();
        foreach (EnemyHealth enemy in enemyGroupRoot.GetComponentsInChildren<EnemyHealth>(false))
            if (enemy != null) candidates.Add(enemy.transform);
        foreach (EnemyFireBreath enemy in enemyGroupRoot.GetComponentsInChildren<EnemyFireBreath>(false))
            if (enemy != null) candidates.Add(enemy.transform);

        Transform nearest = null;
        float best = float.MaxValue;
        foreach (Transform candidate in candidates)
        {
            float distance = HorizontalDistance(origin, candidate.position);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }
        return nearest;
    }

    private void ApplyPhaseVisibility()
    {
        if (!controlGroupActivation)
            return;
        if (animalGroupRoot != null)
            animalGroupRoot.SetActive(phase == MissionPhase.Animals);
        if (enemyGroupRoot != null)
            enemyGroupRoot.SetActive(phase == MissionPhase.Enemies);
    }

    // ========================================================================
    // Xunjuu v0.1 - Colocacion cercana y segura de objetivos
    // ACCION: cada nueva fase aparece a menos de cincuenta unidades del jugador.
    // MODIFICACION: distancias y separacion se ajustan desde el Inspector.
    // ========================================================================
    private void RepositionActiveGroup(
        GameObject groupRoot,
        float angleOffset,
        bool addContinuousGrounding,
        float minimumActorSpacing)
    {
        if (!repositionObjectivesNearPlayer || groupRoot == null)
            return;

        List<Transform> actors = CollectMissionActors(groupRoot);
        if (actors.Count == 0)
            return;

        Vector3 center = GetPlayerPosition(groupRoot.transform.position);
        List<Vector3> placedPositions = new List<Vector3>();
        for (int index = 0; index < actors.Count; index++)
        {
            Transform actor = actors[index];
            Vector3 position = FindGroundedMissionPoint(
                center,
                index,
                actors.Count,
                angleOffset,
                groupRoot.transform,
                minimumActorSpacing,
                placedPositions);
            actor.position = position;
            placedPositions.Add(position);

            Rigidbody actorBody = actor.GetComponent<Rigidbody>();
            if (actorBody != null)
            {
                actorBody.position = position;
                if (!actorBody.isKinematic)
                    actorBody.linearVelocity = Vector3.zero;
            }

            if (!addContinuousGrounding)
                continue;

            XunjuuTerrainGrounding grounding = actor.GetComponent<XunjuuTerrainGrounding>();
            if (grounding == null)
                grounding = actor.gameObject.AddComponent<XunjuuTerrainGrounding>();
            grounding.SnapNow();
        }
    }

    private List<Transform> CollectMissionActors(GameObject groupRoot)
    {
        HashSet<Transform> uniqueActors = new HashSet<Transform>();
        foreach (XunjuuAnimalHealth animal in groupRoot.GetComponentsInChildren<XunjuuAnimalHealth>(true))
            if (animal != null) uniqueActors.Add(animal.transform);
        foreach (EnemyHealth enemy in groupRoot.GetComponentsInChildren<EnemyHealth>(true))
            if (enemy != null) uniqueActors.Add(enemy.transform);
        foreach (EnemyFireBreath enemy in groupRoot.GetComponentsInChildren<EnemyFireBreath>(true))
            if (enemy != null) uniqueActors.Add(enemy.transform);

        List<Transform> actors = new List<Transform>(uniqueActors);
        actors.Sort((first, second) => string.CompareOrdinal(first.name, second.name));
        return actors;
    }

    private Vector3 FindGroundedMissionPoint(
        Vector3 center,
        int index,
        int total,
        float angleOffset,
        Transform ignoredGroup,
        float minimumActorSpacing,
        List<Vector3> placedPositions)
    {
        Vector3 fallback = center;
        float distanceRange = Mathf.Max(0f, missionMaximumDistance - missionMinimumDistance);
        for (int attempt = 0; attempt < 32; attempt++)
        {
            float normalizedRing = ((index + attempt) % 4) / 3f;
            float radius = missionMinimumDistance + distanceRange * normalizedRing;
            float angle = angleOffset + (360f / Mathf.Max(1, total)) * index + attempt * 41f;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 candidate = center + new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
            if (!TryGetTerrainPoint(candidate, out Vector3 groundedPoint))
                continue;

            fallback = groundedPoint;
            if (IsFarEnoughFromPlacedActors(groundedPoint, placedPositions, minimumActorSpacing)
                && IsMissionPointClear(groundedPoint, missionMinimumSpacing * 0.5f, ignoredGroup))
                return groundedPoint;
        }
        return fallback;
    }

    // Xunjuu v0.1 - ACCION: buscar un claro amplio para que Ocelotl no nazca
    // dentro de arboles, milpas, animales ni otros objetos con colision.
    private Vector3 FindGroundedBossPoint(Vector3 center)
    {
        Vector3 bestPoint = center;
        int bestBlockers = int.MaxValue;
        float distanceRange = Mathf.Max(0f, bossMaximumDistance - bossMinimumDistance);

        for (int attempt = 0; attempt < 96; attempt++)
        {
            float normalizedRing = (attempt % 5) / 4f;
            float radius = bossMinimumDistance + distanceRange * normalizedRing;
            float angle = (306f + attempt * 47f) * Mathf.Deg2Rad;
            Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            if (!TryGetTerrainPoint(candidate, out Vector3 groundedPoint))
                continue;

            int blockers = CountMissionPointBlockers(groundedPoint, bossClearRadius, null);
            if (blockers < bestBlockers)
            {
                bestBlockers = blockers;
                bestPoint = groundedPoint;
            }
            if (blockers == 0)
                return groundedPoint;
        }

        return bestPoint;
    }

    private bool TryGetConfiguredBossPoint(Vector3 playerPosition, out Vector3 groundedPoint)
    {
        groundedPoint = finalBossSpawnPoint != null ? finalBossSpawnPoint.position : transform.position;
        if (finalBossSpawnPoint == null || !TryGetTerrainPoint(finalBossSpawnPoint.position, out groundedPoint))
            return false;

        float distance = HorizontalDistance(playerPosition, groundedPoint);
        if (distance < bossMinimumDistance || distance > bossMaximumDistance + 6f)
            return false;

        return IsMissionPointClear(groundedPoint, bossClearRadius, null);
    }

    private static bool IsFarEnoughFromPlacedActors(Vector3 point, List<Vector3> placedPositions, float minimumSpacing)
    {
        if (placedPositions == null)
            return true;
        foreach (Vector3 placed in placedPositions)
        {
            if (HorizontalDistance(point, placed) < minimumSpacing)
                return false;
        }
        return true;
    }

    private bool IsMissionPointClear(Vector3 point, float radius, Transform ignoredGroup)
    {
        return CountMissionPointBlockers(point, radius, ignoredGroup) == 0;
    }

    private int CountMissionPointBlockers(Vector3 point, float radius, Transform ignoredGroup)
    {
        Collider[] hits = Physics.OverlapSphere(point + Vector3.up * 0.8f, radius, ~0, QueryTriggerInteraction.Ignore);
        int blockers = 0;
        foreach (Collider hit in hits)
        {
            if (hit == null || hit is TerrainCollider)
                continue;
            if (ignoredGroup != null && hit.transform.IsChildOf(ignoredGroup))
                continue;
            if (hit.GetComponentInParent<PlayerController>() != null)
                continue;
            blockers++;
        }
        return blockers;
    }

    private static bool TryGetTerrainPoint(Vector3 point, out Vector3 groundedPoint)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
                continue;
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (point.x < origin.x || point.x > origin.x + size.x || point.z < origin.z || point.z > origin.z + size.z)
                continue;

            groundedPoint = new Vector3(point.x, terrain.SampleHeight(point) + origin.y, point.z);
            return true;
        }

        groundedPoint = point;
        return false;
    }

    private static Vector3 GetPlayerPosition(Vector3 fallback)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        return player != null ? player.transform.position : fallback;
    }

    private void SetMissionHudActive(bool active)
    {
        if (missionHudRoot != null)
            missionHudRoot.SetActive(active);
    }

    private void RefreshText()
    {
        if (missionText == null)
            return;
        switch (phase)
        {
            case MissionPhase.Animals:
                missionText.text =
                    $"Mazahua: Tizi, pjantr'eje: {RemainingAnimals}.\n"
                    + $"Español: Patos, venados: {RemainingAnimals}.";
                break;
            case MissionPhase.Enemies:
                missionText.text =
                    $"Mazahua: Dyanatr'o: {RemainingEnemies}.\n"
                    + $"Español: Dyanatr'o: {RemainingEnemies}.";
                break;
            case MissionPhase.Boss:
                missionText.text =
                    "Mazahua: Gran Dyanatr'o / Ocelotl.\n"
                    + "Español: Gran Dyanatr'o / Ocelotl.";
                break;
            case MissionPhase.Completed:
                missionText.text =
                    "Mazahua: Jñiñi tsasú.\n"
                    + "Español: Pueblo protegido.";
                break;
            default:
                missionText.text =
                    "Mazahua: Tsansa tsicha ndájná.\n"
                    + "Español: Recoge cinco flores.";
                break;
        }
    }

    // ACCION: crear un HUD compacto y un aviso grande para cada cambio de fase.
    private void BuildMissionHud()
    {
        GameObject canvasObject = new GameObject("HUD_Progresion_Nivel1", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        missionHudRoot = canvasObject;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2400;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = new GameObject("Panel_Objetivo", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -18f);
        panelRect.sizeDelta = new Vector2(1120f, 108f);
        panel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.035f, 0.92f);

        GameObject textObject = new GameObject("Texto_Objetivo", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 8f);
        textRect.offsetMax = new Vector2(-18f, -8f);
        missionText = textObject.GetComponent<Text>();
        missionText.font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI" }, 18);
        missionText.fontSize = 20;
        missionText.alignment = TextAnchor.MiddleCenter;
        missionText.color = new Color(1f, 0.88f, 0.42f, 1f);
        missionText.raycastTarget = false;

        GameObject bannerObject = new GameObject("Aviso_Cambio_Mision", typeof(RectTransform), typeof(Text), typeof(Outline));
        bannerObject.transform.SetParent(canvasObject.transform, false);
        RectTransform bannerRect = bannerObject.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRect.sizeDelta = new Vector2(1120f, 150f);
        bannerRect.anchoredPosition = new Vector2(0f, 145f);
        phaseBannerText = bannerObject.GetComponent<Text>();
        phaseBannerText.font = missionText.font;
        phaseBannerText.fontSize = 30;
        phaseBannerText.fontStyle = FontStyle.Bold;
        phaseBannerText.alignment = TextAnchor.MiddleCenter;
        phaseBannerText.color = new Color(1f, 0.72f, 0.16f, 1f);
        phaseBannerText.raycastTarget = false;
        bannerObject.GetComponent<Outline>().effectColor = new Color(0.03f, 0.04f, 0.04f, 1f);
        phaseBannerText.enabled = false;
    }

    private void ShowPhaseBanner(string message)
    {
        if (phaseBannerText == null)
            return;
        if (phaseBannerRoutine != null)
            StopCoroutine(phaseBannerRoutine);
        phaseBannerRoutine = StartCoroutine(ShowPhaseBannerRoutine(message));
    }

    private IEnumerator ShowPhaseBannerRoutine(string message)
    {
        phaseBannerText.text = message;
        phaseBannerText.enabled = true;
        yield return new WaitForSecondsRealtime(phaseBannerDuration);
        phaseBannerText.enabled = false;
        phaseBannerRoutine = null;
    }

    private void OnValidate()
    {
        enemiesToDefeat = Mathf.Max(1, enemiesToDefeat);
        animalsToDefeat = Mathf.Max(1, animalsToDefeat);
        requireAnimalDefeats = true;
        phaseBannerDuration = Mathf.Clamp(phaseBannerDuration, 3f, 8f);
        missionMaximumDistance = Mathf.Max(missionMinimumDistance, missionMaximumDistance);
        animalMinimumSpacing = Mathf.Max(missionMinimumSpacing, animalMinimumSpacing);
        enemyMinimumSpacing = Mathf.Max(missionMinimumSpacing, enemyMinimumSpacing);
        bossMaximumDistance = Mathf.Max(bossMinimumDistance, bossMaximumDistance);
    }

    private bool BelongsToGroup(GameObject target, GameObject groupRoot)
    {
        return target != null && groupRoot != null
            && (target == groupRoot || target.transform.IsChildOf(groupRoot.transform));
    }

    private int CountAnimalsInHierarchy()
    {
        return animalGroupRoot == null
            ? 0
            : animalGroupRoot.GetComponentsInChildren<XunjuuAnimalHealth>(true).Length;
    }

    private int CountEnemiesInHierarchy()
    {
        if (enemyGroupRoot == null)
            return 0;
        HashSet<GameObject> enemies = new HashSet<GameObject>();
        foreach (EnemyHealth enemy in enemyGroupRoot.GetComponentsInChildren<EnemyHealth>(true))
            if (enemy != null && enemy.GetComponentInParent<XunjuuBossNivel1>() == null) enemies.Add(enemy.gameObject);
        foreach (EnemyFireBreath enemy in enemyGroupRoot.GetComponentsInChildren<EnemyFireBreath>(true))
            if (enemy != null && enemy.GetComponentInParent<XunjuuBossNivel1>() == null) enemies.Add(enemy.gameObject);
        return enemies.Count;
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        Vector2 difference = new Vector2(first.x - second.x, first.z - second.z);
        return difference.magnitude;
    }
}
