using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnemyFireBreath : MonoBehaviour
{
    public bool HasFootstepAudio => footstepSound != null && audioSource != null;
    public bool HasAttackAudio => attackCrySound != null && attackAudioSource != null;
    public int ProjectilesFired { get; private set; }
    [Header("=== VIDA ===")]
    [SerializeField] private int maxHealth = 75;
    private int currentHealth;

    [Header("=== BARRA DE VIDA ===")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthText;
    [SerializeField] private Canvas healthCanvas;

    [Header("=== COMPORTAMIENTO ===")]
    [SerializeField] private float detectionRange = 14f;
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float chaseSpeed = 2.45f;
    [SerializeField] private float wanderSpeed = 1.85f;
    [SerializeField] private float wanderRadius = 9f;
    [SerializeField] private float idleTime = 1.1f;
    [SerializeField, Min(2f)] private float wanderRetargetInterval = 5f;
    [SerializeField, Min(0.5f)] private float obstacleCheckDistance = 1.35f;
    [Tooltip("Tiempo minimo entre proyectiles del Dyanatr'o.")]
    [SerializeField, Min(0.8f)] private float attackCooldown = 2.25f;
    [SerializeField, Range(0.1f, 0.7f)] private float attackWindup = 0.3f;
    [SerializeField, Range(0.2f, 1f)] private float attackRecovery = 0.62f;

    [Header("=== DAÑO ===")]
    [SerializeField] private int contactDamage = 6;
    [SerializeField] private int fireballDamage = 9;

    [Header("=== BOLA DE FUEGO ===")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireballSpeed = 6.4f;
    [SerializeField, Min(0.2f)] private float projectileHeight = 0.85f;
    [SerializeField, Min(0.1f)] private float projectileForwardOffset = 0.68f;

    [Header("=== ANIMACIONES ===")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("=== SONIDOS ===")]
    [SerializeField] private AudioClip attackCrySound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField, Min(0.15f)] private float footstepInterval = 0.54f;

    // Nombres exactos de las animaciones
    private readonly string WALK_ANIM = "WalkEnemy";
    private readonly string ATTACK_ANIM = "AtackEnemy";  // ← Con una sola 't'
    private readonly string DEATH_ANIM = "Death";

    private Transform player;
    private Rigidbody rb;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isAttacking = false;
    private bool isDead = false;
    private bool isIdle = false;
    private float nextAttackTime = 0f;
    private float nextDamageTime = 0f;
    [SerializeField, Min(0.25f)] private float contactDamageCooldown = 1f;
    private AudioSource audioSource;
    private AudioSource attackAudioSource;
    private float nextFootstepTime;
    private Coroutine attackAudioStopRoutine;
    private string currentAnimationName;
    private float nextWanderRetargetTime;
    private float nextProgressCheckTime;
    private Vector3 lastProgressPosition;

    public float AttackCooldown => attackCooldown;
    public float AttackAnimationDuration => attackWindup + attackRecovery;
    public float ContactDamageCooldown => contactDamageCooldown;
    public float WanderRadius => wanderRadius;

    void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Xunjuu beta - ACCION: garantizar colision y movimiento en otra PC,
        // aunque el prefab sea reemplazado por una variante sin componentes.
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.6f, 0f);
            capsule.radius = 0.42f;
            capsule.height = 1.25f;
            capsule.isTrigger = false;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.35f;
        AudioSource[] sources = GetComponents<AudioSource>();
        attackAudioSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        attackAudioSource.playOnAwake = false;
        attackAudioSource.loop = false;
        attackAudioSource.spatialBlend = 0.35f;

        if (attackCrySound == null)
            attackCrySound = Resources.Load<AudioClip>("Audio/enemigo_gato_ataque");
        if (deathSound == null)
            deathSound = Resources.Load<AudioClip>("Audio/enemigo_muerte");
        if (footstepSound == null)
            footstepSound = Resources.Load<AudioClip>("Audio/pasos_pasto");

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null && animator.runtimeAnimatorController == null)
            animator = null;

        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.parent = transform;
            firePoint = firePointObj.transform;
        }
        EnsureFirePointAboveGround();

        UpdateHealthUI();

        if (healthCanvas != null)
            healthCanvas.gameObject.SetActive(false);

        ChooseNewTarget();
        StartCoroutine(WanderRoutine());
    }

    void Update()
    {
        if (isDead) return;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;

        float distanceToPlayer = HorizontalDistance(transform.position, player.position);

        if (healthCanvas != null)
            healthCanvas.gameObject.SetActive(distanceToPlayer <= detectionRange);

        // ============================================
        // PERSECUCIÓN
        // ============================================
        if (distanceToPlayer <= detectionRange && !isAttacking)
        {
            // Reactivar animación si estaba congelada
            if (animator != null && animator.speed == 0f)
                animator.speed = 1f;

            Vector3 direction = FindClearDirection(Flatten(player.position - transform.position).normalized);
            rb.linearVelocity = new Vector3(direction.x * chaseSpeed, rb.linearVelocity.y, direction.z * chaseSpeed);

            // Flip sprite
            if (spriteRenderer != null && direction.x != 0)
                spriteRenderer.flipX = direction.x < 0;

            // Animación caminar
            PlayAnimationState(WALK_ANIM, 1f);

            // Atacar
            if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime && !isAttacking)
            {
                StartCoroutine(Attack());
            }
        }
    }

    IEnumerator WanderRoutine()
    {
        while (!isDead)
        {
            // Esperar si está quieto
            if (isIdle)
            {
                yield return new WaitForSeconds(idleTime);
                isIdle = false;
                ChooseNewTarget();

                // Reactivar animación al moverse
                PlayAnimationState(WALK_ANIM, 1f);
            }

            float distanceToPlayer = player != null ? HorizontalDistance(transform.position, player.position) : Mathf.Infinity;
            bool isChasing = distanceToPlayer <= detectionRange;

            if (!isChasing && !isAttacking && !isDead)
            {
                if (Time.time >= nextWanderRetargetTime)
                    ChooseNewTarget();
                if (Time.time >= nextProgressCheckTime)
                {
                    if (HorizontalDistance(transform.position, lastProgressPosition) < 0.12f)
                        ChooseNewTarget();
                    lastProgressPosition = transform.position;
                    nextProgressCheckTime = Time.time + 1.25f;
                }

                Vector3 direction = FindClearDirection(Flatten(targetPosition - transform.position).normalized);
                if (direction.sqrMagnitude < 0.001f)
                {
                    ChooseNewTarget();
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }
                rb.linearVelocity = new Vector3(direction.x * wanderSpeed, rb.linearVelocity.y, direction.z * wanderSpeed);

                // Flip sprite
                if (spriteRenderer != null && direction.x != 0)
                    spriteRenderer.flipX = direction.x < 0;

                // Activar animación caminar
                PlayAnimationState(WALK_ANIM, 1f);

                // Llegó al destino
                if (HorizontalDistance(transform.position, targetPosition) < 0.65f)
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                    isIdle = true;

                    // Congelar animación (no hay Idle)
                    PlayAnimationState(WALK_ANIM, 0.22f);
                }
            }
            else if (!isChasing && !isAttacking)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    void ChooseNewTarget()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;
        float distance = Random.Range(wanderRadius * 0.4f, wanderRadius);
        targetPosition = startPosition + new Vector3(direction.x * distance, 0f, direction.y * distance);
        targetPosition.y = transform.position.y;
        nextWanderRetargetTime = Time.time + wanderRetargetInterval;
        nextProgressCheckTime = Time.time + 1.25f;
        lastProgressPosition = transform.position;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        XunjuuEnemyDefeatEvents.Report(gameObject);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Reproducir animación de muerte
        if (animator != null)
        {
            PlayAnimationState(DEATH_ANIM, 1f, true);
        }

        if (healthCanvas != null)
            healthCanvas.gameObject.SetActive(false);

        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound, 0.95f);
        if (attackAudioSource != null)
            attackAudioSource.Stop();

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        StartCoroutine(WaitForDeathAnimation());
    }

    IEnumerator WaitForDeathAnimation()
    {
        float animationLength = GetDeathAnimationLength();
        yield return new WaitForSeconds(animationLength);
        Destroy(gameObject);
    }

    float GetDeathAnimationLength()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in clips)
            {
                if (clip.name == DEATH_ANIM)
                    return clip.length;
            }
        }
        return 2f;
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    IEnumerator Attack()
    {
        if (isDead) yield break;

        isAttacking = true;
        float animationDuration = attackWindup + attackRecovery;
        nextAttackTime = Time.time + Mathf.Max(attackCooldown, animationDuration);

        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        // Reproducir animación de ataque
        if (animator != null)
        {
            PlayAnimationState(ATTACK_ANIM, 1f, true);
        }

        PlaySynchronizedAttackSound(attackCrySound, 0.88f, animationDuration);

        yield return new WaitForSeconds(attackWindup);

        ShootFireball();

        yield return new WaitForSeconds(attackRecovery);

        isAttacking = false;
    }

    void ShootFireball()
    {
        if (isDead) return;
        if (player == null) return;
        if (firePoint == null) return;

        // Xunjuu v0.1 - ACCION: disparar desde el centro visible del enemigo.
        // Esto evita que el proyectil nazca debajo del Terrain y se destruya al instante.
        Vector3 targetPoint = player.position + Vector3.up * projectileHeight;
        Vector3 horizontalDirection = Flatten(targetPoint - transform.position).normalized;
        Vector3 visualCenter = spriteRenderer != null
            ? spriteRenderer.bounds.center
            : transform.position + Vector3.up * projectileHeight;
        Vector3 spawnPoint = visualCenter + horizontalDirection * projectileForwardOffset;
        spawnPoint.y = Mathf.Max(spawnPoint.y, transform.position.y + projectileHeight);
        firePoint.position = spawnPoint;

        GameObject fireball = fireballPrefab != null
            ? Instantiate(fireballPrefab, firePoint.position, Quaternion.identity)
            : CreateFallbackFireball();

        fireball.SetActive(true);
        Rigidbody projectileBody = fireball.GetComponent<Rigidbody>();
        if (projectileBody == null)
            projectileBody = fireball.AddComponent<Rigidbody>();
        projectileBody.useGravity = false;
        projectileBody.isKinematic = true;
        projectileBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Vector3 direction = (targetPoint - firePoint.position).normalized;

        FireballProjectile projectile = fireball.GetComponent<FireballProjectile>();
        if (projectile == null)
            projectile = fireball.AddComponent<FireballProjectile>();

        projectile.Initialize(direction, fireballSpeed, fireballDamage);
        ProjectilesFired++;
    }

    // Xunjuu v0.1 - ACCION: reparar prefabs antiguos cuyo FirePoint quedo bajo el suelo.
    private void EnsureFirePointAboveGround()
    {
        if (firePoint == null)
            return;

        Vector3 center = spriteRenderer != null
            ? spriteRenderer.bounds.center
            : transform.position + Vector3.up * projectileHeight;
        center.y = Mathf.Max(center.y, transform.position.y + projectileHeight);
        firePoint.position = center + transform.right * projectileForwardOffset;
        firePoint.localScale = Vector3.one;
    }

    void LateUpdate()
    {
        TryPlayFootstep();
    }

    // Xunjuu v0.1 - ACCION: reproducir pasos graves mientras el enemigo se mueve.
    private void TryPlayFootstep()
    {
        if (isDead || isAttacking || rb == null || audioSource == null || footstepSound == null || Time.time < nextFootstepTime)
            return;
        Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontal.sqrMagnitude < 0.18f)
            return;

        audioSource.pitch = Random.Range(0.76f, 0.88f);
        audioSource.PlayOneShot(footstepSound, 0.34f);
        audioSource.pitch = 1f;
        nextFootstepTime = Time.time + Mathf.Max(footstepInterval, footstepSound.length * 0.95f);
    }

    GameObject CreateFallbackFireball()
    {
        GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireball.name = "Fireball_Runtime";
        fireball.transform.position = firePoint.position;
        fireball.transform.localScale = Vector3.one * 0.35f;

        Collider collider = fireball.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;

        Renderer renderer = fireball.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(1f, 0.32f, 0.05f, 1f);

        return fireball;
    }

    void OnCollisionStay(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player") && Time.time >= nextDamageTime)
        {
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                int newHealth = playerController.GetCurrentHealth() - contactDamage;
                playerController.SetHealth(newHealth);
                nextDamageTime = Time.time + contactDamageCooldown;
            }
        }
    }

    // ========================================================================
    // Xunjuu v0.1 - Voz enemiga sincronizada
    // ACCION: la voz de ataque termina junto con la animacion y no se acumula.
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

    // ========================================================================
    // Xunjuu v0.1 - Patrulla y animacion estable del Dyanatr'o
    // ACCION: cambiar de estado una sola vez y rodear obstaculos sin reiniciar
    // WalkEnemy en cada cuadro.
    // ========================================================================
    private void PlayAnimationState(string stateName, float playbackSpeed, bool restart = false)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;

        animator.speed = Mathf.Max(0.05f, playbackSpeed);
        if (!restart && currentAnimationName == stateName)
            return;

        animator.Play(stateName, 0, 0f);
        currentAnimationName = stateName;
    }

    private Vector3 FindClearDirection(Vector3 desiredDirection)
    {
        desiredDirection = Flatten(desiredDirection).normalized;
        if (desiredDirection.sqrMagnitude < 0.001f)
            return Vector3.zero;
        if (IsDirectionClear(desiredDirection))
            return desiredDirection;

        Vector3 side = new Vector3(-desiredDirection.z, 0f, desiredDirection.x);
        Vector3 left = (desiredDirection + side * 0.82f).normalized;
        if (IsDirectionClear(left))
            return left;
        Vector3 right = (desiredDirection - side * 0.82f).normalized;
        if (IsDirectionClear(right))
            return right;
        return Vector3.zero;
    }

    private bool IsDirectionClear(Vector3 direction)
    {
        Vector3 origin = transform.position + Vector3.up * 0.58f;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            obstacleCheckDistance,
            ~0,
            QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                continue;
            return false;
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        if (Application.isPlaying)
            Gizmos.DrawWireSphere(startPosition, wanderRadius);
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

    private void OnValidate()
    {
        detectionRange = Mathf.Max(4f, detectionRange);
        attackRange = Mathf.Clamp(attackRange, 2f, detectionRange);
        attackWindup = Mathf.Clamp(attackWindup, 0.15f, 0.7f);
        attackRecovery = Mathf.Clamp(attackRecovery, 0.25f, 1f);
        attackCooldown = Mathf.Max(attackWindup + attackRecovery + 0.8f, attackCooldown);
        contactDamageCooldown = Mathf.Max(0.75f, contactDamageCooldown);
        footstepInterval = Mathf.Max(0.5f, footstepInterval);
        fireballSpeed = Mathf.Max(1f, fireballSpeed);
        wanderRadius = Mathf.Max(4f, wanderRadius);
        wanderSpeed = Mathf.Max(0.5f, wanderSpeed);
        idleTime = Mathf.Clamp(idleTime, 0.6f, 3f);
        wanderRetargetInterval = Mathf.Max(2f, wanderRetargetInterval);
        obstacleCheckDistance = Mathf.Max(0.5f, obstacleCheckDistance);
    }
}
