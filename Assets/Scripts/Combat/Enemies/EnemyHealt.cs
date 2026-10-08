using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    private int currentHealth;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private AudioClip deathSound;
    private AudioSource audioSource;
    private bool deathReported;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.35f;

        if (deathSound == null)
            deathSound = Resources.Load<AudioClip>("Audio/enemigo_muerte");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"💥 Enemy took {damage} damage! Health: {currentHealth}");

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.red;
            Invoke(nameof(ResetColor), 0.1f);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ResetColor()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white;
        }
    }

    void Die()
    {
        if (deathReported)
            return;
        deathReported = true;
        XunjuuEnemyDefeatEvents.Report(gameObject);
        Debug.Log($"💀 Enemy died!");

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 0.95f);
        }

        Destroy(gameObject);
    }
}
