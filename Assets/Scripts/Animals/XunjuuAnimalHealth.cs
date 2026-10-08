using UnityEngine;

// ============================================================================
// Compatibilidad con animales antiguos que podian recibir dano.
// La fauna con XunjuuAnimalCapture ignora esta vida y se registra con C.
// MODIFICACION: cambia Vida maxima y Efectos desde el prefab del animal.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuAnimalHealth : MonoBehaviour
{
    [Header("Xunjuu v0.1 - Vida del animal")]
    [Tooltip("Cantidad de dano necesaria para derrotar al animal.")]
    [SerializeField, Min(1)] private int maxHealth = 56;

    [Header("Xunjuu v0.1 - Efectos opcionales")]
    [Tooltip("Efecto que aparece al derrotar al animal. Puede dejarse vacio.")]
    [SerializeField] private GameObject defeatEffect;
    [Tooltip("Sonido que se reproduce al derrotar al animal. Puede dejarse vacio.")]
    [SerializeField] private AudioClip defeatSound;

    private int currentHealth;
    private bool defeatReported;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // Xunjuu v0.1 - ACCION: permitir que el constructor asigne vida distinta
    // a pato y venado sin ocultar el valor editable del prefab.
    public void ConfigureMaxHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        currentHealth = maxHealth;
    }

    // ACCION: PlayerController utiliza este metodo para golpes de mano y arma.
    public void TakeDamage(int damage)
    {
        if (GetComponent<XunjuuAnimalCapture>() != null || defeatReported || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (currentHealth <= 0)
            Defeat();
        else
            GetComponent<Animal>()?.PlayHurtVocal();
    }

    // ACCION: informar una sola vez, mostrar efectos y retirar la instancia.
    private void Defeat()
    {
        if (defeatReported)
            return;

        defeatReported = true;
        XunjuuAnimalDefeatEvents.Report(gameObject);

        if (defeatEffect != null)
            Instantiate(defeatEffect, transform.position, Quaternion.identity);
        Animal animal = GetComponent<Animal>();
        AudioClip finalDefeatSound = defeatSound != null ? defeatSound : animal?.GetDefeatVocalClip();
        if (finalDefeatSound != null)
            AudioSource.PlayClipAtPoint(finalDefeatSound, transform.position, 0.82f);

        foreach (Collider animalCollider in GetComponentsInChildren<Collider>(true))
            animalCollider.enabled = false;

        Destroy(gameObject);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }
}
