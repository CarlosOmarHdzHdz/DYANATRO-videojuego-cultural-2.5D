using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Validacion de progresion
// Accion: comprobar nivel 1 sin arma, recompensa, inventario, arboles y niebla.
// ============================================================================
[InitializeOnLoad]
public static class XunjuuProgressionValidationTools
{
    private const string PendingKey = "Xunjuu.v0.1.ProgressionValidationPending";
    private static bool running;

    static XunjuuProgressionValidationTools()
    {
        EnsureSubscribed();
    }

    private static void EnsureSubscribed()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
    }

    [MenuItem("Xunjuu v0.1/Pruebas/Validar progresion nivel 1 y 2")]
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

    // ACCION: ejecutar pruebas temporales sin guardar cambios en la escena.
    private static async void RunValidation()
    {
        if (running)
            return;
        running = true;
        bool passed = true;

        try
        {
            Time.timeScale = 1f;
            await Task.Delay(1800);

            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
                throw new InvalidOperationException("No se encontro Player1.");
            if (player.HasOrbitalWeapon() || player.HasOrbitalAttackUnlocked())
                throw new InvalidOperationException("Player1 inicio con el arma o Orbitasword desbloqueado.");

            if (RenderSettings.fog)
                throw new InvalidOperationException("La niebla global sigue activa.");

            TreeInteractivo[] trees = UnityEngine.Object.FindObjectsByType<TreeInteractivo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int visibleTrees = trees.Count(tree => tree != null
                && tree.gameObject.activeInHierarchy
                && tree.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.enabled && !renderer.forceRenderingOff && renderer.sprite != null));
            if (visibleTrees == 0)
                throw new InvalidOperationException("No hay arboles con SpriteRenderer visible en Game.");

            GameObject rewardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MisionSecundaria_Macuahuitl.prefab");
            if (rewardPrefab == null)
                throw new InvalidOperationException("No se creo el prefab de recompensa del nivel 2.");

            GameObject rewardObject = UnityEngine.Object.Instantiate(rewardPrefab);
            XunjuuSecondaryMissionWeaponReward reward = rewardObject.GetComponent<XunjuuSecondaryMissionWeaponReward>();
            if (reward == null)
                throw new InvalidOperationException("La recompensa no tiene su componente de mision.");
            reward.CompleteMission();
            await Task.Delay(250);

            if (!player.HasOrbitalWeapon() || player.HasOrbitalAttackUnlocked())
                throw new InvalidOperationException("La recompensa debe equipar el Macuahuitl y mantener Orbitasword bloqueado.");

            XunjuuInventory inventory = player.GetComponent<XunjuuInventory>();
            XunjuuInventory.Slot weaponSlot = inventory != null
                ? inventory.Items.FirstOrDefault(slot => slot.ItemId == "macuahuitl")
                : null;
            if (weaponSlot == null || weaponSlot.Icon == null)
                throw new InvalidOperationException("El macuahuitl no aparece con sprite en el inventario.");

            if (!player.UnlockOrbitalAttack() || !player.HasOrbitalAttackUnlocked())
                throw new InvalidOperationException("Orbitasword no pudo desbloquearse en la fase posterior de enemigos.");

            Debug.Log($"[XUNJUU PROGRESION PASS] Nivel1 sin arma; arboles visibles={visibleTrees}; niebla=OFF; Macuahuitl antes de Orbitasword; inventario=OK.");
        }
        catch (Exception exception)
        {
            passed = false;
            Debug.LogError("[XUNJUU PROGRESION FAIL] " + exception.Message + "\n" + exception.StackTrace);
        }
        finally
        {
            SessionState.SetBool(PendingKey, false);
            running = false;
            await Task.Delay(350);
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            Debug.Log(passed ? "Xunjuu v0.1: validacion de progresion finalizada." : "Xunjuu v0.1: la validacion de progresion encontro errores.");
        }
    }
}
