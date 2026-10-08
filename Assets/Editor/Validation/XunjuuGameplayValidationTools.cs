using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Validación del nivel 1
// Acción: probar árboles, ataques del macuahuitl y cosecha de maíz en Play Mode.
// ============================================================================
[InitializeOnLoad]
public static class XunjuuGameplayValidationTools
{
    private const string PendingKey = "Xunjuu.v0.1.GameplayValidationPending";
    private static bool validationRunning;

    static XunjuuGameplayValidationTools()
    {
        EnsureSubscribed();
    }

    private static void EnsureSubscribed()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
    }

    [MenuItem("Xunjuú v0.1/Pruebas/Validar árboles, espada y maíz")]
    [MenuItem("Tools/Xunjuu/Validar arboles espada y maiz %&g")]
    private static void StartValidation()
    {
        EnsureSubscribed();
        SessionState.SetBool(PendingKey, true);
        if (EditorApplication.isPlaying)
            RunValidation();
        else
            EditorApplication.EnterPlaymode();
    }

    private static void HandlePlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            EditorApplication.delayCall += RunValidation;
    }

    // ACCIÓN: ejecutar el recorrido automático sin guardar cambios en la escena.
    private static async void RunValidation()
    {
        if (validationRunning)
            return;
        validationRunning = true;

        bool passed = true;
        try
        {
            // ACCIÓN: el menú inicial pausa el tiempo; la prueba necesita animaciones activas.
            Time.timeScale = 1f;
            await Task.Delay(1200);
            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
                throw new InvalidOperationException("No se encontró al protagonista.");

            XunjuuInventory inventory = player.GetComponent<XunjuuInventory>();
            if (inventory == null)
                inventory = player.gameObject.AddComponent<XunjuuInventory>();

            TreeInteractivo[] trees = UnityEngine.Object.FindObjectsByType<TreeInteractivo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int treeVisuals = trees.Count(tree => tree != null
                && tree.gameObject.activeInHierarchy
                && tree.GetComponentInChildren<SpriteRenderer>()?.sprite != null);
            int invalidTreeVisuals = trees.Count(tree =>
            {
                Sprite sprite = tree != null ? tree.GetComponentInChildren<SpriteRenderer>()?.sprite : null;
                string name = sprite != null && sprite.texture != null
                    ? sprite.texture.name.ToLowerInvariant().Replace(" ", string.Empty)
                    : string.Empty;
                return name == "arbolllo" || name == "tree1";
            });
            string treeTextureNames = string.Join(", ", trees
                .Select(tree => tree != null ? tree.GetComponentInChildren<SpriteRenderer>()?.sprite?.texture?.name : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct());
            if (treeVisuals == 0 || invalidTreeVisuals > 0)
                throw new InvalidOperationException($"Árboles inválidos: visibles={treeVisuals}, variantes incorrectas={invalidTreeVisuals}, texturas=[{treeTextureNames}].");

            OrbitalWeapon weapon = player.GetComponentInChildren<OrbitalWeapon>();
            if (weapon == null)
            {
                GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Weapons/SwordFloating.prefab");
                if (swordPrefab == null)
                    throw new InvalidOperationException("No se encontró SwordFloating.prefab.");
                GameObject sword = UnityEngine.Object.Instantiate(swordPrefab, player.transform);
                sword.name = "Macuahuitl_Prueba_Xunjuu";
                weapon = sword.GetComponent<OrbitalWeapon>();
                if (weapon == null)
                    weapon = sword.AddComponent<OrbitalWeapon>();
                weapon.Initialize(player.transform);
                player.SetOrbitalWeapon(weapon);
            }

            if (!weapon.ExecuteAttack())
                throw new InvalidOperationException("El ataque normal no pudo iniciar.");
            if (!weapon.UsesOriginalPlayerAttackAnimation || weapon.IsWeaponVisible)
                throw new InvalidOperationException("El tajo no esta usando el SwordAttack original del protagonista.");
            await Task.Delay(650);
            if (!weapon.IsAtRest())
                throw new InvalidOperationException("La espada no regresó a la espalda después del tajo.");

            if (!weapon.ExecuteOrbitalAttack())
                throw new InvalidOperationException("Orbitasword no pudo iniciar.");
            await Task.Delay(Mathf.CeilToInt((weapon.GetOrbitalDuration() + 0.25f) * 1000f));
            if (!weapon.IsAtRest())
                throw new InvalidOperationException("La espada no regresó después de Orbitasword.");

            XunjuuCrop crop = UnityEngine.Object.FindObjectsByType<XunjuuCrop>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy);
            if (crop == null)
                throw new InvalidOperationException("No se encontró un cultivo activo.");

            int before = inventory.GetAmount("mazorca");
            crop.SetReady();
            crop.ReceiveHit(99, player.gameObject);
            crop.ReceiveHit(99, player.gameObject);
            await Task.Delay(550);

            XunjuuWorldItemPickup pickup = UnityEngine.Object.FindObjectsByType<XunjuuWorldItemPickup>(FindObjectsSortMode.None)
                .FirstOrDefault(item => item != null && item.ItemId == "mazorca");
            if (pickup == null)
                throw new InvalidOperationException("El cultivo no soltó el sprite de maíz.");
            int droppedAmount = pickup.Amount;
            if (!pickup.Collect(player.gameObject))
                throw new InvalidOperationException("El maíz no pudo recogerse.");
            await Task.Delay(100);

            int after = inventory.GetAmount("mazorca");
            if (after != before + droppedAmount)
                throw new InvalidOperationException($"Inventario incorrecto: antes={before}, después={after}, esperado={before + droppedAmount}.");

            Debug.Log($"[XUNJUU VALIDATION PASS] Árboles={treeVisuals}; tajo=OK; Orbitasword=OK; maíz soltado y recogido={droppedAmount}.");
        }
        catch (Exception exception)
        {
            passed = false;
            Debug.LogError("[XUNJUU VALIDATION FAIL] " + exception.Message + "\n" + exception.StackTrace);
        }
        finally
        {
            SessionState.SetBool(PendingKey, false);
            validationRunning = false;
            await Task.Delay(350);
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            Debug.Log(passed ? "Xunjuú v0.1: validación finalizada correctamente." : "Xunjuú v0.1: la validación encontró errores.");
        }
    }
}
