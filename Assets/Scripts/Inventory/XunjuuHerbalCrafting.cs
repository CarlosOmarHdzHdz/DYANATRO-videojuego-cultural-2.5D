using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class XunjuuHerbalCrafting : MonoBehaviour
{
    public const string RemedyId = "preparado_camino";
    public const int Recovery = 40;
    public const float Duration = 5f;
    private static readonly Dictionary<string, int> ingredients = new Dictionary<string, int> { { "manzanilla", 2 }, { "hierbabuena", 1 } };
    private XunjuuInventory inventory;
    private PlayerController player;
    private DyanatroGameDirector director;
    private float elapsed;
    private int restored;
    private int actualRecovered;
    private bool recovering;
    public bool IsRecovering => recovering;
    public string LastMessage { get; private set; } = "Receta ficticia: 2 manzanillas + 1 hierbabuena.";
    public bool CanCraft => inventory != null && inventory.GetAmount("manzanilla") >= 2 && inventory.GetAmount("hierbabuena") >= 1;
    private void Awake()
    {
        inventory = GetComponent<XunjuuInventory>(); player = GetComponent<PlayerController>();
        director = FindFirstObjectByType<DyanatroGameDirector>();
    }

    public bool TryCraft()
    {
        if (player == null || player.GetCurrentHealth() <= 0) return false;
        if (!CanCraft) { LastMessage = "Faltan ingredientes: necesitas 2 manzanillas y 1 hierbabuena."; return false; }
        if (!inventory.TryExchange(ingredients, RemedyId, "Infusión de hierbas", XunjuuMedicinalPlantCatalog.RemedySprite))
        { LastMessage = "No hay espacio para el preparado. Los ingredientes se conservaron."; return false; }
        LastMessage = "Preparado fabricado +1. Úsalo con H cuando te falte vida.";
        return true;
    }
    public bool TryUseRemedy()
    {
        if (player == null || player.GetCurrentHealth() <= 0) { LastMessage = "No se puede usar durante la derrota."; return false; }
        if (recovering) { LastMessage = "La recuperación ya está en curso; no se gastó otro preparado."; return false; }
        if (player.GetCurrentHealth() >= player.GetMaxHealth()) { LastMessage = "Tu salud está completa; conserva el preparado."; return false; }
        if (inventory == null || !inventory.RemoveItem(RemedyId)) { LastMessage = "Fabrica un preparado en el morral [I]."; return false; }
        recovering = true; elapsed = 0; restored = 0; actualRecovered = 0;
        LastMessage = "Recuperando hasta 40 puntos de salud durante 5 segundos.";
        return true;
    }
    private void Update()
    {
        bool playable = director == null || director.IsGameplayHudVisible;
        if (playable && Time.timeScale > 0 && Input.GetKeyDown(KeyCode.H))
        { TryUseRemedy(); XunjuuMedicinalPlantSystem.Notify(LastMessage); }
        if (playable && Time.timeScale > 0) AdvanceRecovery(Time.deltaTime);
    }
    // Determinista y verificable: la pausa no avanza el reloj, y la derrota no revive.
    public void AdvanceRecovery(float deltaTime)
    {
        if (!recovering || deltaTime <= 0) return;
        if (player == null || player.GetCurrentHealth() <= 0) { recovering = false; LastMessage = "Recuperación interrumpida por la derrota."; return; }
        elapsed = Mathf.Min(Duration, elapsed + deltaTime);
        int target = Mathf.FloorToInt(Recovery * elapsed / Duration);
        if (target > restored) actualRecovered += player.RestoreHealth(target - restored);
        restored = target;
        if (elapsed >= Duration || player.GetCurrentHealth() >= player.GetMaxHealth())
        { recovering = false; LastMessage = "Recuperación finalizada: +" + actualRecovered + " puntos de salud."; }
    }
}
