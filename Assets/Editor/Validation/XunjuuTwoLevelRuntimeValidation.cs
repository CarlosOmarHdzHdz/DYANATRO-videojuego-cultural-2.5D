using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// ============================================================================
// Xunjuu v0.1 - Prueba del flujo completo de dos niveles
// ACCION: valida flores, arma, Orbitasword, objetivos, jefe y recompensa.
// MODIFICACION: es una prueba temporal; no guarda cambios realizados en Play.
// ============================================================================
[InitializeOnLoad]
public static class XunjuuTwoLevelRuntimeValidation
{
    private const string PendingKey = "Xunjuu.v0.1.TwoLevelRuntimeValidationPending";
    private const string AutoRunFlag = "Temp/XunjuuRunTwoLevelValidation.flag";
    private static bool validationRunning;

    static XunjuuTwoLevelRuntimeValidation()
    {
        EnsureSubscribed();
        if (File.Exists(AutoRunFlag))
            EditorApplication.update += PollAutoRun;
    }

    private static void PollAutoRun()
    {
        if (!File.Exists(AutoRunFlag) || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        EditorApplication.update -= PollAutoRun;
        File.Delete(AutoRunFlag);
        EditorApplication.delayCall += StartValidation;
    }

    private static void EnsureSubscribed()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeChange;
        EditorApplication.playModeStateChanged += HandlePlayModeChange;
    }

    [MenuItem("Xunjuu v0.1/Pruebas/Validar flujo completo de dos niveles %&l")]
    [MenuItem("Tools/Xunjuu/Validar primera beta jugable")]
    public static void StartValidation()
    {
        EnsureSubscribed();
        SessionState.SetBool(PendingKey, true);
        if (EditorApplication.isPlaying)
            RunValidation();
        else
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity")
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
    }

    private static void HandlePlayModeChange(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            EditorApplication.delayCall += RunValidation;
    }

    private static async void RunValidation()
    {
        if (validationRunning)
            return;

        validationRunning = true;
        bool passed = true;
        try
        {
            Time.timeScale = 1f;
            await Task.Delay(1800);

            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            DyanatroGameDirector director = UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
            XunjuuLevel2KillMission levelTwo = UnityEngine.Object.FindFirstObjectByType<XunjuuLevel2KillMission>(FindObjectsInactive.Include);
            MazahuaWordCollectible[] flowers = UnityEngine.Object.FindObjectsByType<MazahuaWordCollectible>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (player == null || director == null || levelTwo == null || flowers.Length != 5)
                throw new InvalidOperationException("Faltan Player, Director, mision beta o las cinco flores.");
            if (Mathf.Abs(director.CurrentMusicVolume - director.MenuMusicVolume) > 0.03f)
                throw new InvalidOperationException("La musica no inicio con el volumen fuerte del menu.");

            // ACCION: comprobar por separado los dos botones de salto.
            director.BeginGameForValidation();
            await Task.Delay(350);
            if (!director.IsIntroVideoVisible)
                throw new InvalidOperationException("El panel del video inicial no se mostro.");
            GameObject translationOverlay = GameObject.Find("Traduccion_Bilingue_Corregida");
            Text[] translationLines = translationOverlay != null ? translationOverlay.GetComponentsInChildren<Text>(true) : Array.Empty<Text>();
            if (translationOverlay == null || translationLines.Length < 2
                || !translationLines.Any(line => line.text.StartsWith("ESPAÑOL"))
                || !translationLines.Any(line => line.text.StartsWith("MAZAHUA")))
                throw new InvalidOperationException("El video no mostro la traduccion bilingue corregida.");
            director.SkipIntroForValidation();
            await Task.Delay(650);
            if (!director.IsTimelinePrologueVisible || !director.IsInPrologue)
                throw new InvalidOperationException("Saltar el video no abrio el segundo prologo.");
            director.SkipTimelineForValidation();
            await Task.Delay(350);
            director.GetComponent<XunjuuOpeningJourney>()?.FinishTutorial();
            await Task.Delay(100);
            if (director.IsInPrologue || director.IsTimelinePrologueVisible)
                throw new InvalidOperationException("El segundo boton no salto el prologo de texto.");
            if (Mathf.Abs(director.CurrentMusicVolume - director.GameplayMusicVolume) > 0.03f
                || Mathf.Abs(director.GameplayMusicVolume / director.MenuMusicVolume - 0.45f) > 0.01f)
                throw new InvalidOperationException("La musica no bajo 55 por ciento al comenzar la partida.");
            if (!director.IsControlsSidebarVisible)
                throw new InvalidOperationException("La guia compacta de controles no aparecio al iniciar la partida.");
            GameObject controlsGuide = GameObject.Find("Guia_Controles_Inicio");
            RectTransform controlsRect = controlsGuide != null ? controlsGuide.GetComponent<RectTransform>() : null;
            Text[] controlsLabels = controlsGuide != null ? controlsGuide.GetComponentsInChildren<Text>(true) : Array.Empty<Text>();
            if (controlsRect == null || controlsRect.anchorMin.x < 0.99f || controlsRect.anchorMin.y < 0.99f
                || controlsLabels.Length < 2 || controlsLabels.Min(label => label.fontSize) < 20)
                throw new InvalidOperationException("La guia de controles no quedo legible en la esquina superior derecha.");

            director.ResetLevelProgressionForValidation();
            player.RemoveEquippedWeapon();
            foreach (OrbitalWeapon staleWeapon in player.GetComponentsInChildren<OrbitalWeapon>(true))
            {
                if (staleWeapon != null)
                    UnityEngine.Object.Destroy(staleWeapon.gameObject);
            }
            foreach (XunjuuRewardPlaceholder oldReward in UnityEngine.Object.FindObjectsByType<XunjuuRewardPlaceholder>(FindObjectsSortMode.None))
                UnityEngine.Object.Destroy(oldReward.gameObject);
            await Task.Delay(180);

            if (player.HasOrbitalWeapon() || player.HasOrbitalAttackUnlocked())
                throw new InvalidOperationException("El jugador inicio con la recompensa antes de completar el nivel 1.");
            if (UnityEngine.Object.FindFirstObjectByType<XunjuuBossNivel1>() != null)
                throw new InvalidOperationException("Ocelotl aparecio antes de completar las misiones.");

            // ACCION: comprobar en Play Mode que mano, cooldown y audio terminan
            // junto con Attack.anim antes de avanzar la progresion de la beta.
            player.SendMessage("Attack", SendMessageOptions.DontRequireReceiver);
            await Task.Delay(90);
            if (!player.IsAttacking())
                throw new InvalidOperationException("El golpe de mano no inicio su animacion.");
            await Task.Delay(460);
            if (player.IsAttacking() || player.IsAttackAudioPlaying)
                throw new InvalidOperationException("El golpe de mano o su sonido exceden Attack.anim.");

            player.SendMessage("Attack", SendMessageOptions.DontRequireReceiver);
            await Task.Delay(25);
            if (player.IsAttacking())
                throw new InvalidOperationException("El golpe de mano ignoro su cooldown.");
            await Task.Delay(90);

            // ACCION: simular la recoleccion de las cinco flores sin mover al jugador.
            foreach (MazahuaWordCollectible flower in flowers)
                director.CollectMazahuaWord(flower.MazahuaWord, flower.SpanishMeaning);
            await Task.Delay(300);

            if (player.HasOrbitalWeapon() || player.HasOrbitalAttackUnlocked())
                throw new InvalidOperationException("Las flores no deben entregar arma antes de capturar la fauna.");
            if (!levelTwo.IsAnimalPhase || !levelTwo.AnimalGroupVisible || levelTwo.EnemyGroupVisible)
                throw new InvalidOperationException("Despues de las flores deben aparecer solo los seis animales.");

            XunjuuAnimalCapture[] animals = UnityEngine.Object.FindObjectsByType<XunjuuAnimalCapture>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(animal => animal.name.Contains("_Mision2_"))
                .Take(6)
                .ToArray();
            if (animals.Length != 6)
                throw new InvalidOperationException($"Contenido animal incompleto: animales={animals.Length}/6.");
            if (animals.Any(animal => animal.GetComponent<Animal>() == null
                || animal.Entry == null
                || animal.GetComponentInChildren<SpriteRenderer>()?.sprite == null)
                || animals.Select(animal => animal.FaunaId).Distinct().Count() != 6)
                throw new InvalidOperationException("La fauna regional no cargo seis fichas, sprites y comportamientos distintos.");
            Vector3[] animalPositionsBeforeMovement = animals.Select(animal => animal.transform.position).ToArray();
            await Task.Delay(850);
            if (!animals.Where(animal => animal != null).Select((animal, index) =>
                    HorizontalDistance(animal.transform.position, animalPositionsBeforeMovement[index])).Any(distance => distance > 0.12f))
                throw new InvalidOperationException("Los animales aparecieron, pero ninguno recorrio su territorio.");
            if (animals.Any(animal => HorizontalDistance(player.transform.position, animal.transform.position) > 50f))
                throw new InvalidOperationException("La mision de fauna aparecio a mas de 50 unidades del jugador.");
            if (MinimumHorizontalSpacing(animals.Select(animal => animal.transform).ToArray()) < 12f)
                throw new InvalidOperationException("Los animales de mision siguen apareciendo demasiado juntos.");
            if (UnityEngine.Object.FindFirstObjectByType<XunjuuBossNivel1>() != null)
                throw new InvalidOperationException("Ocelotl aparecio durante la mision de animales.");

            foreach (XunjuuAnimalCapture animal in animals)
                XunjuuAnimalCaptureEvents.Report(animal.gameObject, animal.FaunaId);
            await Task.Delay(300);

            if (!player.HasOrbitalWeapon() || player.HasOrbitalAttackUnlocked())
                throw new InvalidOperationException("Las seis capturas deben entregar Macuahuitl, pero Orbitasword debe seguir bloqueado.");
            OrbitalWeapon equippedWeapon = player.GetComponentInChildren<OrbitalWeapon>(true);
            if (equippedWeapon == null || !equippedWeapon.IsAtRest()
                || !equippedWeapon.IsWeaponVisible || !equippedWeapon.IsRenderedBehindPlayer())
                throw new InvalidOperationException("El Macuahuitl no quedo visible detras de la espalda.");
            player.SendMessage("SwordAttack", SendMessageOptions.DontRequireReceiver);
            bool basicAttackAppearedDuringSword = false;
            for (int sample = 0; sample < 5; sample++)
            {
                await Task.Delay(70);
                basicAttackAppearedDuringSword |= player.IsBasicAttackAnimationPlaying;
            }
            if (!player.IsAttacking() || !equippedWeapon.IsBusy || equippedWeapon.IsWeaponVisible)
                throw new InvalidOperationException("El Macuahuitl no inicio su tajo sincronizado.");
            if (basicAttackAppearedDuringSword)
                throw new InvalidOperationException("SwordAttack encadeno incorrectamente la animacion de puno.");
            await Task.Delay(170);
            if (player.IsAttacking() || equippedWeapon.IsBusy || player.IsAttackAudioPlaying)
                throw new InvalidOperationException("El sonido del Macuahuitl dura mas que SwordAttack.anim.");
            if (!equippedWeapon.IsWeaponVisible || !equippedWeapon.IsRenderedBehindPlayer())
                throw new InvalidOperationException("El Macuahuitl no regreso detras del jugador al terminar F.");
            if (!levelTwo.IsEnemyPhase || !levelTwo.EnemyGroupVisible || levelTwo.AnimalGroupVisible)
                throw new InvalidOperationException("Despues de seis capturas deben aparecer solo los cinco Dyanatr'o.");
            if (player.HasOrbitalAttackUnlocked())
                throw new InvalidOperationException("Orbitasword se desbloqueo antes de terminar los cinco enemigos.");

            EnemyFireBreath[] fireEnemies = UnityEngine.Object.FindObjectsByType<EnemyFireBreath>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EnemyHealth[] regularEnemies = UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject[] enemies = fireEnemies.Select(enemy => enemy.gameObject)
                .Concat(regularEnemies.Select(enemy => enemy.gameObject))
                .Where(enemy => enemy.name.StartsWith("Enemigo_"))
                .Distinct()
                .Take(5)
                .ToArray();

            if (enemies.Length != 5)
                throw new InvalidOperationException($"Contenido enemigo incompleto: enemigos={enemies.Length}/5.");
            if (fireEnemies.Where(enemy => enemy.name.StartsWith("Enemigo_")).Any(enemy => !enemy.HasFootstepAudio || !enemy.HasAttackAudio))
                throw new InvalidOperationException("Un Dyanatr'o no cargo sus pisadas o voz de ataque.");
            Vector3[] enemyPositionsBeforeMovement = enemies.Select(enemy => enemy.transform.position).ToArray();
            await Task.Delay(850);
            if (!enemies.Where(enemy => enemy != null).Select((enemy, index) =>
                    HorizontalDistance(enemy.transform.position, enemyPositionsBeforeMovement[index])).Any(distance => distance > 0.12f))
                throw new InvalidOperationException("Los Dyanatr'o aparecieron, pero ninguno patrullo el terreno.");
            if (enemies.Any(enemy => HorizontalDistance(player.transform.position, enemy.transform.position) > 50f))
                throw new InvalidOperationException("La mision de enemigos aparecio a mas de 50 unidades del jugador.");
            if (enemies.Any(enemy => enemy.GetComponent<XunjuuTerrainGrounding>() == null
                || !enemy.GetComponent<XunjuuTerrainGrounding>().IsGroundedCorrectly()))
                throw new InvalidOperationException("Un Dyanatr'o quedo debajo o encima del terreno.");
            if (MinimumHorizontalSpacing(enemies.Select(enemy => enemy.transform).ToArray()) < 8f)
                throw new InvalidOperationException("Los Dyanatr'o aparecieron traslapados.");
            if (UnityEngine.Object.FindFirstObjectByType<XunjuuBossNivel1>() != null)
                throw new InvalidOperationException("Ocelotl aparecio antes de derrotar al quinto Dyanatr'o.");

            // ACCION: acercar temporalmente al jugador y comprobar que el
            // enemigo vuelve a lanzar una bola de fuego por encima del suelo.
            EnemyFireBreath attackingEnemy = fireEnemies.FirstOrDefault(enemy => enemy.name.StartsWith("Enemigo_"));
            if (attackingEnemy == null)
                throw new InvalidOperationException("No existe un Dyanatr'o de fuego para validar su ataque.");
            Vector3 originalPlayerPosition = player.transform.position;
            player.transform.position = attackingEnemy.transform.position + Vector3.right * 6f;
            Physics.SyncTransforms();
            int firedBefore = attackingEnemy.ProjectilesFired;
            await Task.Delay(2600);
            if (attackingEnemy.ProjectilesFired <= firedBefore)
                throw new InvalidOperationException("El Dyanatr'o detecto al jugador, pero no lanzo su proyectil.");
            player.transform.position = originalPlayerPosition;
            Physics.SyncTransforms();

            foreach (GameObject enemy in enemies)
                XunjuuEnemyDefeatEvents.Report(enemy);
            await Task.Delay(350);

            if (!levelTwo.ObjectivesCompleted)
                throw new InvalidOperationException(
                    $"Derrotar todos los enemigos no mostro al jefe: enemigos={levelTwo.DefeatedEnemies}/{levelTwo.EnemiesToDefeat}.");
            if (!player.HasOrbitalAttackUnlocked())
                throw new InvalidOperationException("Completar los cinco enemigos no desbloqueo Orbitasword.");

            XunjuuBossNivel1 boss = UnityEngine.Object.FindFirstObjectByType<XunjuuBossNivel1>();
            if (boss == null)
                throw new InvalidOperationException("El jefe final no aparecio.");
            if (boss.transform.localScale.x < 3.4f)
                throw new InvalidOperationException("Ocelotl no conserva la nueva escala doble.");
            if (!boss.HasFootstepAudio || !boss.HasAttackAudio)
                throw new InvalidOperationException("Ocelotl no cargo sus pisadas, rugido o embestida.");
            if (HorizontalDistance(player.transform.position, boss.transform.position) > 50f)
                throw new InvalidOperationException("Ocelotl aparecio a mas de 50 unidades del jugador.");
            if (HorizontalDistance(player.transform.position, boss.transform.position) < 28f)
                throw new InvalidOperationException("Ocelotl aparecio demasiado cerca del jugador.");
            XunjuuTerrainGrounding bossGrounding = boss.GetComponent<XunjuuTerrainGrounding>();
            if (bossGrounding == null || !bossGrounding.IsGroundedCorrectly())
                throw new InvalidOperationException("Ocelotl no aparecio correctamente sobre el terreno.");
            if (HasBlockingColliderNearBoss(boss, 8f))
                throw new InvalidOperationException("Ocelotl aparecio dentro de un arbol u otro obstaculo.");

            boss.TakeDamage(999999);
            await Task.Delay(250);
            if (!levelTwo.Completed || UnityEngine.Object.FindFirstObjectByType<XunjuuRewardPlaceholder>() == null)
                throw new InvalidOperationException("Derrotar al jefe no creo la recompensa provisional.");

            Debug.Log("[XUNJUU BETA PASS] 5 flores -> 6 capturas -> Macuahuitl -> 5 Dyanatr'o -> Orbitasword -> Ocelotl -> nivel completado.");
        }
        catch (Exception exception)
        {
            passed = false;
            Debug.LogError("[XUNJUU FLUJO FAIL] " + exception.Message + "\n" + exception.StackTrace);
        }
        finally
        {
            SessionState.SetBool(PendingKey, false);
            validationRunning = false;
            await Task.Delay(350);
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            Debug.Log(passed ? "Xunjuu v0.1: flujo de dos niveles validado." : "Xunjuu v0.1: el flujo de niveles necesita revision.");
            if (Application.isBatchMode)
                EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        Vector3 delta = first - second;
        delta.y = 0f;
        return delta.magnitude;
    }

    private static float MinimumHorizontalSpacing(Transform[] actors)
    {
        float minimum = float.MaxValue;
        for (int first = 0; first < actors.Length; first++)
        {
            for (int second = first + 1; second < actors.Length; second++)
                minimum = Mathf.Min(minimum, HorizontalDistance(actors[first].position, actors[second].position));
        }
        return minimum;
    }

    private static bool HasBlockingColliderNearBoss(XunjuuBossNivel1 boss, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(boss.transform.position + Vector3.up, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit == null || hit is TerrainCollider || hit.transform.IsChildOf(boss.transform))
                continue;
            if (hit.GetComponentInParent<PlayerController>() != null)
                continue;
            return true;
        }
        return false;
    }
}
