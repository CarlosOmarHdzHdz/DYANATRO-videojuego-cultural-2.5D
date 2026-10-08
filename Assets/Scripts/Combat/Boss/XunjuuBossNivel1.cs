using System.Collections;
using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Jefe agresivo del nivel 1
// Acción: perseguir, embestir, golpear al protagonista y administrar su vida.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuBossNivel1 : MonoBehaviour
{
    [Header("Xunjuú v0.1 - Identidad")]
    [SerializeField] private string bossName = "Jaguar de la Sombra";
    [SerializeField, Min(1)] private int maxHealth = 480;

    [Header("Xunjuú v0.1 - Persecución agresiva")]
    [SerializeField, Min(1f)] private float detectionRange = 22f;
    [SerializeField, Min(0.5f)] private float meleeRange = 2.25f;
    [SerializeField, Min(0.5f)] private float meleeHitRange = 2.15f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 4.1f;
    [SerializeField, Min(0.8f)] private float meleeCooldown = 1.75f;
    [SerializeField, Range(0.15f, 0.8f)] private float meleeImpactDelay = 0.48f;
    [SerializeField, Min(0.5f)] private float meleeAnimationDuration = 1.38f;
    [SerializeField, Min(1)] private int meleeDamage = 14;

    [Header("Xunjuú v0.1 - Embestida")]
    [SerializeField, Min(0.5f)] private float chargeMinDistance = 5.5f;
    [SerializeField, Min(0.5f)] private float chargeSpeed = 9.5f;
    [SerializeField, Min(0.1f)] private float chargeDuration = 0.48f;
    [SerializeField, Min(1f)] private float chargeCooldown = 5.5f;
    [SerializeField, Range(0.1f, 0.6f)] private float chargeWindup = 0.28f;
    [SerializeField, Min(1)] private int chargeDamage = 24;
    [SerializeField, Min(0.5f)] private float chargeHitRange = 1.65f;

    [Header("Xunjuú v0.1 - Efectos opcionales")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField, Min(0.15f)] private float footstepInterval = 0.58f;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2.65f, 0f);

    private Transform player;
    private Rigidbody body;
    private AudioSource audioSource;
    private AudioSource attackAudioSource;
    private XunjuuBossHealthBar healthBar;
    private int currentHealth;
    private bool isDead;
    private bool isAttacking;
    private bool isCharging;
    private float nextMeleeTime;
    private float nextChargeTime;
    private Vector3 chargeDirection;
    private Vector3 lastFacingDirection = Vector3.forward;
    private float nextFootstepTime;
    private Coroutine attackAudioStopRoutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool HasFootstepAudio => footstepSound != null && audioSource != null;
    public bool HasAttackAudio => attackSound != null && chargeSound != null && attackAudioSource != null;
    public float MeleeCooldown => meleeCooldown;
    public float MeleeAnimationDuration => meleeAnimationDuration;
    public float ChargeCooldown => chargeCooldown;

    // Xunjuu v0.1 - ACCION: la mision escucha este evento para entregar recompensa.
    public event System.Action<XunjuuBossNivel1> Defeated;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponentInChildren<SpriteRenderer>();
        animator = animator != null ? animator : GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController == null)
            animator = null;

        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        EnsureBossCollider();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.3f;
        AudioSource[] sources = GetComponents<AudioSource>();
        attackAudioSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        attackAudioSource.playOnAwake = false;
        attackAudioSource.loop = false;
        attackAudioSource.spatialBlend = 0.3f;
        if (attackSound == null)
            attackSound = XunjuuProceduralAudio.GetBossRoar();
        if (chargeSound == null)
            chargeSound = XunjuuProceduralAudio.GetBossCharge();
        if (hurtSound == null)
            hurtSound = Resources.Load<AudioClip>("Audio/macuahuitl_golpe_carne");
        if (deathSound == null)
            deathSound = Resources.Load<AudioClip>("Audio/enemigo_muerte");
        if (footstepSound == null)
            footstepSound = Resources.Load<AudioClip>("Audio/pasos_pasto");

        EnsureHealthBar();
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (isDead)
            return;

        if (player == null)
            FindPlayer();
        if (player == null)
            return;

        float distance = HorizontalDistance(player.position, transform.position);
        if (distance > detectionRange)
        {
            StopMovement();
            return;
        }

        FacePlayer();
        if (isAttacking || isCharging)
            return;

        if (distance <= EffectiveHitRange(meleeRange) && Time.time >= nextMeleeTime)
        {
            StartCoroutine(MeleeAttack());
            return;
        }

        if (distance >= chargeMinDistance && Time.time >= nextChargeTime)
        {
            StartCoroutine(ChargeAttack());
            return;
        }

        ChasePlayer();
    }

    // ACCIÓN: recibir daño desde los ataques de mano, espada u Orbitasword del jugador.
    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        EnsureHealthBar();
        if (healthBar != null)
            healthBar.SetHealth(currentHealth, maxHealth);
        PlaySound(hurtSound, 0.82f);
        StartCoroutine(FlashDamage());

        if (currentHealth <= 0)
            Die();
    }

    private void ChasePlayer()
    {
        Vector3 direction = Flatten(player.position - transform.position).normalized;
        SetVelocity(direction * chaseSpeed);
        SetAnimatorFloat("Speed", 1f);
    }

    private void LateUpdate()
    {
        if (isDead || body == null || body.isKinematic || audioSource == null || footstepSound == null || Time.time < nextFootstepTime)
            return;
        Vector3 horizontal = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        if (horizontal.sqrMagnitude < 0.3f)
            return;

        audioSource.pitch = Random.Range(0.86f, 0.96f);
        audioSource.PlayOneShot(footstepSound, 0.52f);
        audioSource.pitch = 1f;
        nextFootstepTime = Time.time + Mathf.Max(footstepInterval, footstepSound.length * 0.95f);
    }

    private IEnumerator MeleeAttack()
    {
        isAttacking = true;
        nextMeleeTime = Time.time + Mathf.Max(meleeCooldown, meleeAnimationDuration);
        StopMovement();
        SetAnimatorTrigger("Attack");
        PlaySynchronizedAttackSound(attackSound, 0.95f, meleeAnimationDuration);

        yield return new WaitForSeconds(meleeImpactDelay);
        if (!isDead && CanHitPlayer(meleeHitRange, lastFacingDirection, 0.05f))
            DamagePlayer(meleeDamage);

        yield return new WaitForSeconds(Mathf.Max(0f, meleeAnimationDuration - meleeImpactDelay));
        isAttacking = false;
    }

    private IEnumerator ChargeAttack()
    {
        isCharging = true;
        float chargeAnimationDuration = chargeWindup + chargeDuration;
        nextChargeTime = Time.time + Mathf.Max(chargeCooldown, chargeAnimationDuration);
        StopMovement();
        SetAnimatorTrigger("Charge");
        PlaySynchronizedAttackSound(chargeSound, 1f, chargeAnimationDuration);

        yield return new WaitForSeconds(chargeWindup);
        if (player != null)
            chargeDirection = Flatten(player.position - transform.position).normalized;

        float elapsed = 0f;
        bool hitPlayer = false;
        while (!isDead && elapsed < chargeDuration)
        {
            SetVelocity(chargeDirection * chargeSpeed);
            if (!hitPlayer && CanHitPlayer(chargeHitRange, chargeDirection, -0.1f))
            {
                DamagePlayer(chargeDamage);
                hitPlayer = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopMovement();
        isCharging = false;
    }

    private void DamagePlayer(int damage)
    {
        // El objeto etiquetado como Player puede ser un hijo del personaje;
        // buscar en ambos sentidos evita que el jefe golpee sin aplicar daño.
        PlayerController controller = player != null
            ? player.GetComponentInParent<PlayerController>()
            : null;
        if (controller == null && player != null)
            controller = player.GetComponentInChildren<PlayerController>();
        if (controller != null)
            controller.SetHealth(controller.GetCurrentHealth() - damage);
    }

    // Xunjuu v0.1 - ACCION: confirmar distancia y direccion antes de aplicar dano.
    private bool CanHitPlayer(float hitRange, Vector3 attackDirection, float minimumDot)
    {
        if (player == null)
            return false;

        Vector3 toPlayer = Flatten(player.position - transform.position);
        if (toPlayer.magnitude > EffectiveHitRange(Mathf.Max(hitRange, isAttacking ? meleeRange : hitRange)))
            return false;
        if (toPlayer.sqrMagnitude < 0.001f || attackDirection.sqrMagnitude < 0.001f)
            return true;

        return Vector3.Dot(attackDirection.normalized, toPlayer.normalized) >= minimumDot;
    }

    private float EffectiveHitRange(float configuredRange)
    {
        Collider own = GetComponent<Collider>();
        Collider target = player != null ? player.GetComponentInParent<Collider>() : null;
        float ownRadius = own != null ? Mathf.Max(own.bounds.extents.x, own.bounds.extents.z) : 0f;
        float targetRadius = target != null ? Mathf.Max(target.bounds.extents.x, target.bounds.extents.z) : 0f;
        return Mathf.Max(configuredRange, ownRadius + targetRadius + 0.3f);
    }

    private IEnumerator FlashDamage()
    {
        if (spriteRenderer == null)
            yield break;

        Color original = spriteRenderer.color;
        spriteRenderer.color = new Color(1f, 0.38f, 0.38f, original.a);
        yield return new WaitForSeconds(0.09f);
        if (spriteRenderer != null)
            spriteRenderer.color = original;
    }

    // ACCIÓN: terminar el combate, ocultar la barra y liberar la instancia del jefe.
    // Xunjuu v0.1 - ACCION: avisar al controlador de nivel antes de destruir el jefe.
    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        StopAllCoroutines();
        if (attackAudioSource != null)
            attackAudioSource.Stop();
        StopMovement();
        if (body != null)
            body.isKinematic = true;

        SetAnimatorTrigger("Death");
        PlaySound(deathSound, 1f);
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        if (healthBar != null)
            healthBar.gameObject.SetActive(false);

        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;
        Defeated?.Invoke(this);
        Destroy(gameObject, 1.4f);
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    // ACCION: reconstruir la barra si Unity uso recarga rapida de escena.
    private void EnsureHealthBar()
    {
        if (healthBar == null)
            healthBar = GetComponentInChildren<XunjuuBossHealthBar>(true);
        if (healthBar == null)
            healthBar = XunjuuBossHealthBar.Create(transform, bossName, healthBarOffset);
        if (healthBar != null)
            healthBar.SetHealth(currentHealth, maxHealth);
    }

    private void FacePlayer()
    {
        Vector3 direction = Flatten(player.position - transform.position);
        if (direction.sqrMagnitude < 0.001f)
            return;
        lastFacingDirection = direction.normalized;

        // Los sprites 2.5D se mantienen de frente a cámara; sólo se espejan al perseguir.
        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
            spriteRenderer.flipX = direction.x < 0f;
    }

    private void SetVelocity(Vector3 velocity)
    {
        if (body != null && !body.isKinematic)
            body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);
    }

    private void StopMovement()
    {
        if (body != null && !body.isKinematic)
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
        SetAnimatorFloat("Speed", 0f);
    }

    private void EnsureBossCollider()
    {
        if (GetComponentInChildren<Collider>() != null)
            return;

        CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.72f, 0f);
        collider.radius = 0.48f;
        collider.height = 1.5f;
    }

    private void SetAnimatorTrigger(string parameterName)
    {
        if (animator == null)
            return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
            {
                animator.SetTrigger(parameterName);
                return;
            }
        }
    }

    private void SetAnimatorFloat(string parameterName, float value)
    {
        if (animator == null)
            return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }

    // ========================================================================
    // Xunjuu v0.1 - Audio del jefe sincronizado
    // ACCION: reproducir la voz en una fuente separada y cerrarla con la accion.
    // ========================================================================
    private void PlaySynchronizedAttackSound(AudioClip clip, float volume, float visualDuration)
    {
        if (clip == null || attackAudioSource == null || visualDuration <= 0.02f)
            return;

        if (attackAudioStopRoutine != null)
            StopCoroutine(attackAudioStopRoutine);
        attackAudioSource.Stop();
        attackAudioSource.clip = clip;
        attackAudioSource.volume = Mathf.Clamp01(volume);
        attackAudioSource.pitch = clip.length > visualDuration
            ? Mathf.Clamp(clip.length / visualDuration, 1f, 1.45f)
            : 1f;
        attackAudioSource.Play();
        attackAudioStopRoutine = StartCoroutine(StopAttackAudioAfter(visualDuration));
    }

    private IEnumerator StopAttackAudioAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (attackAudioSource != null)
        {
            attackAudioSource.Stop();
            attackAudioSource.pitch = 1f;
        }
        attackAudioStopRoutine = null;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        return Flatten(first - second).magnitude;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.78f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.2f, 0.16f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, meleeHitRange);
        Gizmos.color = new Color(0.9f, 0.35f, 0.05f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, chargeHitRange);
    }

    private void OnValidate()
    {
        meleeAnimationDuration = Mathf.Max(0.8f, meleeAnimationDuration);
        meleeImpactDelay = Mathf.Clamp(meleeImpactDelay, 0.15f, meleeAnimationDuration * 0.75f);
        meleeCooldown = Mathf.Max(meleeAnimationDuration + 0.25f, meleeCooldown);
        chargeWindup = Mathf.Clamp(chargeWindup, 0.1f, 0.6f);
        chargeCooldown = Mathf.Max(chargeWindup + chargeDuration + 1.5f, chargeCooldown);
        footstepInterval = Mathf.Max(0.52f, footstepInterval);
    }
}
