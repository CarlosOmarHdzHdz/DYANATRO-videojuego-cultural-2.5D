using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Control del protagonista
// Acción: integrar ataques, árboles, cultivos e inventario del nivel 1.
// ============================================================================
// PLAYER CONTROLLER - MOVIMIENTO + SISTEMA DE ATAQUES
// ============================================
// VERSIÓN: 3.2
// ============================================
// CARACTERÍSTICAS:
//   - Movimiento relativo a la cámara (WASD siempre hacia donde mira la cámara)
//   - Salto con física
//   - Ataque básico (Click izquierdo)
//   - Ataque con espada (F) - espada desaparece
//   - Ataque orbital (E) - espada gira alrededor
//   - Billboard (sprite siempre mira a cámara)
//   - Sistema de vida con barra visual
//   - Comunicación con cámara para rotación automática al caminar atrás
// ============================================

public class PlayerController : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN DE MOVIMIENTO ===")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;

    [Header("=== ATAQUE BÁSICO (Click izquierdo) ===")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1.15f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private int attackDamage = 14;
    [Tooltip("Cooldown total del golpe de mano. Debe ser mayor que su animacion.")]
    [SerializeField, Min(0.35f)] private float attackRate = 0.62f;
    [Tooltip("Duracion del estado de ataque; coincide con Attack.anim.")]
    [SerializeField, Min(0.3f)] private float attackDuration = 0.48f;
    [SerializeField, Range(0.02f, 0.4f)] private float basicAttackImpactDelay = 0.16f;

    [Header("=== ESPADA ORBITAL (Sistema) ===")]
    [SerializeField] private OrbitalWeapon orbitalWeapon;
    [SerializeField] private bool startWithoutWeapon = true;
    [SerializeField] private bool orbitalAttackUnlocked;

    [Header("=== ATAQUE CON ESPADA (F) ===")]
    [SerializeField] private int swordDamage = 38;
    [SerializeField] private float swordAttackRadius = 2.25f;
    [SerializeField] private KeyCode swordAttackKey = KeyCode.F;
    [SerializeField] private AudioClip swordSwingSound;
    [SerializeField] private AudioClip swordAttackSound;
    [SerializeField] private AudioClip swordImpactAccentSound;
    [SerializeField, Min(0.45f)] private float swordAttackCooldown = 0.72f;
    [SerializeField, Range(0.02f, 0.35f)] private float swordAttackImpactDelay = 0.15f;

    [Header("=== ATAQUE ORBITAL (E) ===")]
    [SerializeField] private KeyCode orbitalAttackKey = KeyCode.E;
    [SerializeField] private AudioClip orbitalAttackSound;
    [SerializeField, Min(1f)] private float orbitalAttackCooldown = 5f;

    [Header("=== CONFIGURACIÓN DE ROTACIÓN ===")]
    [SerializeField] private bool rotateToMovement = true;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("=== BARRA DE VIDA ===")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("=== COMPONENTES ===")]
    private Rigidbody rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource;
    private AudioSource attackAudioSource;
    private BarraVidaFrames barraVida;

    [Header("=== SONIDOS ===")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float footstepInterval = 0.38f;

    [Header("=== MUERTE ===")]
    [SerializeField] private float deathAnimationDuration = 1.45f;
    [SerializeField] private float deathFallAngle = 82f;
    [SerializeField] private bool fadeOutOnDeath = true;

    // Variables de movimiento
    private Vector3 moveInput;
    private bool isGrounded;
    private Vector3 moveDirection;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private bool wasGrounded;
    private bool isJumping = false;
    private float smoothSpeed = 0f;
    private float smoothSpeedVelocity = 0f;
    private float lastSetSpeed = -1f;
    private bool isDead = false;
    private float nextFootstepTime = 0f;
    private float nextOrbitalAttackTime;
    private Coroutine attackRoutine;
    private Coroutine attackAudioStopRoutine;
    private bool swordAnimationActive;

    // Variable para la cámara
    public bool IsMovingBackward { get; private set; } = false;

    // ============================================
    // INICIALIZACIÓN
    // ============================================

    void Awake()
    {
        currentHealth = maxHealth = 100;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (animator != null && animator.runtimeAnimatorController == null)
            animator = null;

        barraVida = FindFirstObjectByType<BarraVidaFrames>(FindObjectsInactive.Include);
        if (barraVida != null)
        {
            currentHealth = maxHealth;
            barraVida.EstablecerMaxima(maxHealth);
            barraVida.EstablecerVida(currentHealth);
        }
        else
        {
            currentHealth = maxHealth;
        }

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        ConfigureCombatTimings();
        ConfigureFallbackAudio();

        ConfigurarGroundCheck();
        ConfigurarAttackPoint();
        EnsureInventorySystems();
        ApplyInitialCombatLoadout();
    }

    // ========================================================================
    // Xunjuu v0.1 - Progresion de combate
    // ACCION: iniciar el nivel 1 solamente con el golpe basico.
    // ========================================================================
    private void ApplyInitialCombatLoadout()
    {
        if (!startWithoutWeapon)
            return;

        OrbitalWeapon[] startingWeapons = GetComponentsInChildren<OrbitalWeapon>(true);
        foreach (OrbitalWeapon weapon in startingWeapons)
        {
            if (weapon != null)
                Destroy(weapon.gameObject);
        }

        orbitalWeapon = null;
        orbitalAttackUnlocked = false;
    }

    // ACCIÓN Xunjuú v0.1: asegurar inventario editable e interfaz.
    void EnsureInventorySystems()
    {
        if (GetComponent<XunjuuInventory>() == null)
            gameObject.AddComponent<XunjuuInventory>();
        if (GetComponent<XunjuuInventoryUI>() == null)
            gameObject.AddComponent<XunjuuInventoryUI>();
    }

    void ConfigureFallbackAudio()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.18f;
            audioSource.volume = 0.95f;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        attackAudioSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        attackAudioSource.playOnAwake = false;
        attackAudioSource.loop = false;
        attackAudioSource.spatialBlend = 0.18f;
        attackAudioSource.volume = 0.95f;

        AudioClip handHit = Resources.Load<AudioClip>("Audio/golpe_mano");
        AudioClip macuahuitlHit = Resources.Load<AudioClip>("Audio/macuahuitl_golpe_carne");
        if (attackSound == null)
            attackSound = handHit;
        if (swordAttackSound == null)
            swordAttackSound = macuahuitlHit;
        if (swordSwingSound == null)
            swordSwingSound = XunjuuProceduralAudio.GetSwordWhoosh();
        if (swordImpactAccentSound == null)
            swordImpactAccentSound = XunjuuProceduralAudio.GetSwordImpactAccent();
        if (orbitalAttackSound == null)
            orbitalAttackSound = XunjuuProceduralAudio.GetOrbitalWhoosh();
        if (hitSound == null)
            hitSound = handHit;
        if (jumpSound == null)
            jumpSound = Resources.Load<AudioClip>("Audio/salto_pasto");
        if (deathSound == null)
            deathSound = Resources.Load<AudioClip>("Audio/jugador_muerte");
        if (footstepSound == null)
            footstepSound = Resources.Load<AudioClip>("Audio/pasos_pasto");
    }

    void ConfigurarGroundCheck()
    {
        Transform existingGroundCheck = transform.Find("GroundCheck");
        if (existingGroundCheck != null)
        {
            groundCheckPoint = existingGroundCheck;
            groundCheckPoint.localPosition = new Vector3(0, -0.5f, 0);
        }
        else if (groundCheckPoint == null)
        {
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.parent = transform;
            groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheckPoint = groundCheck.transform;
        }
    }

    void ConfigurarAttackPoint()
    {
        Transform existingAttackPoint = transform.Find("AttackPoint");
        if (existingAttackPoint != null)
        {
            attackPoint = existingAttackPoint;
        }
        else if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.parent = transform;
            attackPointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            attackPoint = attackPointObj.transform;
        }
    }

    // ============================================
    // UPDATE Y FIXEDUPDATE
    // ============================================

    void Update()
    {
        if (isDead)
            return;

        if (groundCheckPoint != null && groundCheckPoint.parent != transform)
        {
            groundCheckPoint.parent = transform;
            groundCheckPoint.localPosition = new Vector3(0, -0.5f, 0);
        }

        isGrounded = CheckGroundContact();

        if (!wasGrounded && isGrounded)
        {
            if (animator != null)
            {
                animator.SetBool("IsGrounded", true);
                animator.SetBool("IsJumping", false);
            }
            isJumping = false;
        }
        wasGrounded = isGrounded;

        // ============================================
        // MOVIMIENTO RELATIVO A LA CÁMARA
        // ============================================
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Detectar si camina hacia atrás (para la cámara)
        IsMovingBackward = verticalInput < 0;

        // Obtener dirección de la cámara
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        // Ignorar inclinación vertical (mantener en el plano horizontal)
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // Calcular movimiento relativo a la cámara
        moveInput = (camForward * verticalInput + camRight * horizontalInput).normalized;

        // Actualizar animator
        if (animator != null)
        {
            float targetSpeed = moveInput.magnitude;
            smoothSpeed = Mathf.SmoothDamp(smoothSpeed, targetSpeed, ref smoothSpeedVelocity, 0.15f);
            if (Mathf.Abs(smoothSpeed - lastSetSpeed) > 0.02f)
            {
                animator.SetFloat("Speed", smoothSpeed);
                lastSetSpeed = smoothSpeed;
            }
            animator.SetBool("IsGrounded", isGrounded);
            TrySetAnimatorBool("Attack", isAttacking && !swordAnimationActive);
        }

        // ============================================
        // VOLTEAR SPRITE SEGÚN DIRECCIÓN HORIZONTAL
        // ============================================
        if (spriteRenderer != null && moveInput.sqrMagnitude > 0.01f && Camera.main != null)
        {
            // La direccion visible se calcula en pantalla, no en los ejes del mundo.
            // Esto evita que el personaje mire al lado equivocado cuando la camara
            // esta girada respecto al terreno.
            float screenDirection = Vector3.Dot(moveInput, Camera.main.transform.right);
            if (Mathf.Abs(screenDirection) > 0.05f)
                spriteRenderer.flipX = screenDirection < 0f;
        }

        // ============================================
        // ATAQUE BÁSICO (Click izquierdo)
        // ============================================
        if (Time.time >= nextAttackTime && !isAttacking && !isJumping)
        {
            if (Input.GetButtonDown("Fire1") && !IsPointerOverInterface())
                Attack();
        }

        if (isAttacking)
            return;

        // ============================================
        // ATAQUE CON ESPADA (F)
        // ============================================
        if (Input.GetKeyDown(swordAttackKey) && orbitalWeapon != null && Time.time >= nextAttackTime)
        {
            SwordAttack();
        }

        // ============================================
        // ATAQUE ORBITAL (E)
        // ============================================
        if (Input.GetKeyDown(orbitalAttackKey) && orbitalWeapon != null && orbitalAttackUnlocked
            && Time.time >= nextOrbitalAttackTime && Time.time >= nextAttackTime)
        {
            OrbitalAttack();
        }

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded && !isAttacking && !isJumping)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            if (rb != null)
                rb.linearVelocity = Vector3.zero;
            return;
        }

        if (!isAttacking && !isJumping)
            Move();

        TryPlayFootsteps();

        if (rotateToMovement && !isAttacking && !isJumping)
            RotateTowardsMovement();

        // ============================================
        // BILLBOARD: El sprite siempre mira a la cámara
        // ============================================
        if (Camera.main != null)
        {
            Vector3 directionToCamera = transform.position - Camera.main.transform.position;
            directionToCamera.y = 0;
            if (directionToCamera != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            }
        }
    }

    // ============================================
    // MOVIMIENTO
    // ============================================

    private void Move()
    {
        Vector3 movement = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.z * moveSpeed);
        rb.linearVelocity = movement;
        if (moveInput.magnitude > 0.1f)
            moveDirection = moveInput.normalized;
    }

    private static bool IsPointerOverInterface()
    {
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events == null) return false;
        if (events.IsPointerOverGameObject()) return true;
        for (int i = 0; i < Input.touchCount; i++)
            if (events.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return true;
        return false;
    }

    private bool CheckGroundContact()
    {
        if (rb != null && rb.linearVelocity.y > 0.2f) return false;
        Collider shape = GetComponent<Collider>();
        Vector3 foot = shape != null ? new Vector3(shape.bounds.center.x, shape.bounds.min.y, shape.bounds.center.z)
            : groundCheckPoint.position;
        int mask = groundLayer.value != 0 ? groundLayer.value : ~0;
        foreach (RaycastHit hit in Physics.SphereCastAll(foot + Vector3.up * 0.25f, 0.12f,
            Vector3.down, 0.2f, mask, QueryTriggerInteraction.Ignore))
            if (!hit.collider.transform.IsChildOf(transform) && hit.normal.y > 0.45f) return true;
        return false;
    }

    private void TryPlayFootsteps()
    {
        if (audioSource == null || footstepSound == null || isDead || isJumping || isAttacking || !isGrounded)
            return;

        if (moveInput.magnitude < 0.18f || Time.time < nextFootstepTime)
            return;

        float footstepPitch = Random.Range(0.88f, 1.08f);
        audioSource.pitch = footstepPitch;
        audioSource.PlayOneShot(footstepSound, 0.42f);
        audioSource.pitch = 1f;
        float effectiveLength = footstepSound.length / Mathf.Max(0.1f, footstepPitch);
        nextFootstepTime = Time.time + Mathf.Max(footstepInterval, effectiveLength * 0.92f);
    }

    private void RotateTowardsMovement()
    {
        if (moveInput.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        isGrounded = false;
        wasGrounded = false;
        isJumping = true;

        if (animator != null)
        {
            animator.SetBool("IsGrounded", false);
            animator.SetBool("IsJumping", true);
            TrySetAnimatorTrigger("Jump");
        }

        if (jumpSound != null && audioSource != null)
            audioSource.PlayOneShot(jumpSound);

        Invoke(nameof(ResetJumpState), 0.5f);
    }

    private void ResetJumpState()
    {
        if (isGrounded)
        {
            isJumping = false;
            if (animator != null)
                animator.SetBool("IsJumping", false);
        }
        else
        {
            Invoke(nameof(ResetJumpState), 0.2f);
        }
    }

    // ============================================
    // ATAQUE BÁSICO (Click izquierdo)
    // ============================================

    private void Attack()
    {
        if (isDead || isAttacking || swordAnimationActive || Time.time < nextAttackTime)
            return;

        isAttacking = true;
        nextAttackTime = Time.time + Mathf.Max(attackRate, attackDuration);

        if (animator != null)
        {
            TrySetAnimatorBool("Attack", true);
            TrySetAnimatorTrigger("Attack");
        }

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(BasicAttackRoutine());
    }

    private System.Collections.IEnumerator BasicAttackRoutine()
    {
        float impactDelay = Mathf.Min(basicAttackImpactDelay, attackDuration * 0.75f);
        yield return new WaitForSeconds(impactDelay);

        bool hitSomething = DetectEnemies();
        AudioClip clip = hitSomething && hitSound != null ? hitSound : attackSound;
        PlaySynchronizedAttackSound(clip, hitSomething ? 1f : 0.72f, attackDuration - impactDelay);

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - impactDelay));
        ResetAttack();
        attackRoutine = null;
    }

    private void ResetAttack()
    {
        isAttacking = false;
        TrySetAnimatorBool("Attack", false);
    }

    private bool DetectEnemies()
    {
        Vector3 attackCenter = GetAttackCenter(attackRange * 0.65f);
        Collider[] hitObjects = Physics.OverlapSphere(attackCenter, attackRange, GetAttackLayerMask());
        System.Collections.Generic.HashSet<int> hitTargets = new System.Collections.Generic.HashSet<int>();
        bool hitSomething = false;

        foreach (Collider obj in hitObjects)
        {
            if (DamageEnemyOrInteractable(obj, attackDamage, hitTargets))
                hitSomething = true;
        }
        return hitSomething;
    }

    private Vector3 GetAttackCenter(float forwardOffset)
    {
        Vector3 direction = moveDirection.sqrMagnitude > 0.01f ? moveDirection.normalized : transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            direction = transform.forward;

        return transform.position + direction.normalized * forwardOffset + Vector3.up * 0.45f;
    }

    // ========================================================================
    // Xunjuu v0.1 - Receptor comun de ataques
    // ACCION: danar una sola vez enemigos, jefe, animales, arboles o cultivos.
    // MODIFICACION: agrega aqui otro componente de vida si se crea un objetivo.
    // ========================================================================
    private bool DamageEnemyOrInteractable(Collider obj, int damage, System.Collections.Generic.HashSet<int> hitTargets)
    {
        EnemyHealth enemyHealth = obj.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            if (!hitTargets.Add(enemyHealth.GetInstanceID()))
                return false;
            enemyHealth.TakeDamage(damage);
            return true;
        }

        EnemyFireBreath fireBreathEnemy = obj.GetComponentInParent<EnemyFireBreath>();
        if (fireBreathEnemy != null)
        {
            if (!hitTargets.Add(fireBreathEnemy.GetInstanceID()))
                return false;
            fireBreathEnemy.TakeDamage(damage);
            return true;
        }

        // ACCIÓN Xunjuú v0.1: entregar daño al jefe reutilizable del nivel.
        XunjuuBossNivel1 boss = obj.GetComponentInParent<XunjuuBossNivel1>();
        if (boss != null)
        {
            if (!hitTargets.Add(boss.GetInstanceID()))
                return false;
            boss.TakeDamage(damage);
            return true;
        }

        // ACCION: Pato y Venado del nivel 2 pueden recibir mano, espada y orbita.
        XunjuuAnimalHealth animalHealth = obj.GetComponentInParent<XunjuuAnimalHealth>();
        if (animalHealth != null)
        {
            if (!hitTargets.Add(animalHealth.GetInstanceID()))
                return false;
            animalHealth.TakeDamage(damage);
            return true;
        }

        XunjuuHitInteractable interactable = obj.GetComponentInParent<XunjuuHitInteractable>();
        if (interactable != null)
        {
            if (!hitTargets.Add(interactable.GetInstanceID()))
                return false;
            return interactable.ReceiveHit(damage, gameObject);
        }

        return false;
    }

    private int GetAttackLayerMask()
    {
        return enemyLayers.value | interactableLayers.value;
    }

    // ============================================
    // ATAQUE CON ESPADA (F) - Espada desaparece
    // ============================================

    private void SwordAttack()
    {
        if (isDead || isAttacking || Time.time < nextAttackTime)
            return;

        if (orbitalWeapon == null)
        {
            Debug.Log("⚠️ No tienes una espada equipada");
            return;
        }

        if (!orbitalWeapon.ExecuteAttack())
            return;

        Debug.Log("⚔️ ATAQUE CON ESPADA!");
        isAttacking = true;
        swordAnimationActive = true;
        float swordDuration = Mathf.Max(0.2f, orbitalWeapon.GetAttackDuration());
        nextAttackTime = Time.time + Mathf.Max(swordAttackCooldown, swordDuration);

        if (animator != null)
        {
            // Xunjuu v0.1 - ACCION: reservar Attack para el golpe con la mano.
            // El Macuahuitl usa el SwordAttack original del spritesheet del jugador.
            if (HasAnimatorParameter("Attack", AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger("Attack");
            else
                TrySetAnimatorBool("Attack", false);
            if (HasAnimatorParameter("SwordAttack", AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger("SwordAttack");

            // Xunjuu v0.1 - ACCION: reproducir directamente el tajo original.
            // Esto evita que una transicion heredada encadene el golpe de mano.
            if (!TryPlayAnimatorState("SwordAttack"))
                SetAnimatorTriggerIfAvailable("SwordAttack");
        }

        PlaySynchronizedAttackSound(swordSwingSound, 0.72f, swordDuration);

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(SwordAttackRoutine(swordDuration));
    }

    private System.Collections.IEnumerator SwordAttackRoutine(float swordDuration)
    {
        float impactDelay = Mathf.Min(swordAttackImpactDelay, swordDuration * 0.7f);
        yield return new WaitForSeconds(impactDelay);

        Vector3 attackPosition = GetAttackCenter(1.15f);
        Collider[] hitEnemies = Physics.OverlapSphere(attackPosition, swordAttackRadius, GetAttackLayerMask());
        bool hitSomething = false;
        System.Collections.Generic.HashSet<int> hitTargets = new System.Collections.Generic.HashSet<int>();
        foreach (Collider enemy in hitEnemies)
        {
            if (!DamageEnemyOrInteractable(enemy, swordDamage, hitTargets))
                continue;
            hitSomething = true;
            Debug.Log($"⚔️ ESPADA! {swordDamage} de daño a {enemy.name}");
        }

        PlaySynchronizedAttackSound(swordAttackSound, hitSomething ? 1f : 0.8f, swordDuration - impactDelay);
        if (swordImpactAccentSound != null && audioSource != null)
            audioSource.PlayOneShot(swordImpactAccentSound, hitSomething ? 0.88f : 0.52f);
        yield return new WaitForSeconds(Mathf.Max(0f, swordDuration - impactDelay));
        ResetSwordAttack();
        attackRoutine = null;
    }

    // ========================================================================
    // Xunjuu v0.1 - Cierre exclusivo del Macuahuitl
    // ACCION: volver a Idle sin permitir que aparezca Attack despues de F.
    // ========================================================================
    private void ResetSwordAttack()
    {
        swordAnimationActive = false;
        if (animator != null)
        {
            if (HasAnimatorParameter("Attack", AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger("Attack");
            if (HasAnimatorParameter("SwordAttack", AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger("SwordAttack");
            TrySetAnimatorBool("Attack", false);
            TryPlayAnimatorState("Idle");
        }
        ResetAttack();
    }

    // ============================================
    // ATAQUE ORBITAL (E) - Espada gira alrededor
    // ============================================

    private void OrbitalAttack()
    {
        if (isDead)
            return;

        if (orbitalWeapon == null || !orbitalAttackUnlocked)
        {
            Debug.Log("Xunjuu v0.1: Orbitasword se obtiene al completar la mision secundaria del nivel 2.");
            return;
        }

        if (!orbitalWeapon.ExecuteOrbitalAttack())
            return;

        Debug.Log("⚔️ ATAQUE ORBITAL!");
        nextOrbitalAttackTime = Time.time + orbitalAttackCooldown;
        nextAttackTime = Time.time + 1.1f;

        if (animator != null)
        {
            SetAnimatorTriggerIfAvailable("SwordAttack");
        }

        if (orbitalAttackSound != null)
            PlaySynchronizedAttackSound(orbitalAttackSound, 0.92f, orbitalWeapon.GetOrbitalDuration());

        StartCoroutine(OrbitalDamageRoutine());
    }

    private System.Collections.IEnumerator OrbitalDamageRoutine()
    {
        while (orbitalWeapon != null && orbitalWeapon.IsOrbiting())
        {
            if (orbitalWeapon.CanHit())
            {
                Vector3 attackPosition = orbitalWeapon.GetWeaponPosition();
                Collider[] hitEnemies = Physics.OverlapSphere(attackPosition, swordAttackRadius, GetAttackLayerMask());
                System.Collections.Generic.HashSet<int> hitTargets = new System.Collections.Generic.HashSet<int>();

                foreach (Collider enemy in hitEnemies)
                {
                    if (DamageEnemyOrInteractable(enemy, orbitalWeapon.GetOrbitalDamage(), hitTargets))
                    {
                        Debug.Log($"🌀 ATAQUE ORBITAL! {orbitalWeapon.GetOrbitalDamage()} de daño a {enemy.name}");
                    }
                }

                orbitalWeapon.RegisterHit();
            }

            yield return null;
        }

        Debug.Log("✅ Ataque orbital finalizado");
    }

    // ========================================================================
    // Xunjuu v0.1 - Audio de combate sincronizado
    // ACCION: ajustar suavemente el clip a la ventana visual y detener cualquier
    // cola anterior para que el sonido nunca dure mas que la animacion.
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
            ? Mathf.Clamp(clip.length / visualDuration, 1f, 1.6f)
            : 1f;
        attackAudioSource.Play();
        attackAudioStopRoutine = StartCoroutine(StopAttackAudioAfter(visualDuration));
    }

    private System.Collections.IEnumerator StopAttackAudioAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (attackAudioSource != null)
        {
            attackAudioSource.Stop();
            attackAudioSource.pitch = 1f;
        }
        attackAudioStopRoutine = null;
    }

    private void ConfigureCombatTimings()
    {
        attackDuration = Mathf.Max(0.48f, attackDuration);
        attackRate = Mathf.Max(attackDuration + 0.12f, attackRate);
        basicAttackImpactDelay = Mathf.Clamp(basicAttackImpactDelay, 0.05f, attackDuration * 0.75f);
        swordAttackCooldown = Mathf.Max(0.62f, swordAttackCooldown);
        swordAttackImpactDelay = Mathf.Clamp(swordAttackImpactDelay, 0.05f, 0.3f);
        orbitalAttackCooldown = Mathf.Max(2f, orbitalAttackCooldown);
        footstepInterval = Mathf.Max(0.5f, footstepInterval);
    }

    // ACCIÓN Xunjuú v0.1: evitar avisos cuando el Animator no tiene el disparador solicitado.
    private void SetAnimatorTriggerIfAvailable(string parameterName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
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

    // ============================================
    // GESTIÓN DE ESPADA ORBITAL
    // ============================================

    public void SetOrbitalWeapon(OrbitalWeapon weapon)
    {
        SetOrbitalWeapon(weapon, true);
    }

    // ACCION: desbloquear Orbitasword solamente cuando una mision entregue el arma.
    public void SetOrbitalWeapon(OrbitalWeapon weapon, bool unlockOrbitalAttack)
    {
        orbitalWeapon = weapon;
        orbitalAttackUnlocked = weapon != null && unlockOrbitalAttack;
        Debug.Log(orbitalAttackUnlocked
            ? "Xunjuu v0.1: macuahuitl y Orbitasword desbloqueados por recompensa."
            : "Xunjuu v0.1: macuahuitl equipado sin ataque orbital.");
    }

    public bool HasOrbitalWeapon() => orbitalWeapon != null;
    public bool HasOrbitalAttackUnlocked() => orbitalWeapon != null && orbitalAttackUnlocked;
    public float OrbitalCooldownRemaining => Mathf.Max(0f, nextOrbitalAttackTime - Time.time);

    // Xunjuu v0.1 - ACCION: entregar el ataque especial sin volver a crear el arma.
    public bool UnlockOrbitalAttack()
    {
        if (orbitalWeapon == null)
        {
            Debug.LogWarning("Xunjuu v0.1: no se puede desbloquear Orbitasword sin Macuahuitl.");
            return false;
        }
        if (orbitalAttackUnlocked)
            return false;

        orbitalAttackUnlocked = true;
        nextOrbitalAttackTime = 0f;
        Debug.Log("Xunjuu v0.1: Orbitasword desbloqueado despues de derrotar a los Dyanatr'o.");
        return true;
    }

    // ACCION: retirar cualquier arma heredada al preparar el nivel 1.
    public void RemoveEquippedWeapon()
    {
        if (orbitalWeapon != null)
            Destroy(orbitalWeapon.gameObject);
        orbitalWeapon = null;
        orbitalAttackUnlocked = false;
        nextOrbitalAttackTime = 0f;
    }

    // ============================================
    // SISTEMA DE VIDA
    // ============================================

    public void SetHealth(int newHealth)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);

        if (barraVida != null)
        {
            barraVida.EstablecerVida(currentHealth);
        }

        Debug.Log($"❤️ Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log("💀 Jugador derrotado");
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isAttacking = false;
        swordAnimationActive = false;
        isJumping = false;
        moveInput = Vector3.zero;
        moveDirection = Vector3.zero;
        IsMovingBackward = false;
        CancelInvoke();
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        attackRoutine = null;
        if (attackAudioStopRoutine != null)
            StopCoroutine(attackAudioStopRoutine);
        attackAudioStopRoutine = null;
        if (attackAudioSource != null)
            attackAudioSource.Stop();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            TrySetAnimatorBool("Attack", false);
            TrySetAnimatorBool("IsJumping", false);
            TrySetAnimatorBool("IsDead", true);
            TrySetAnimatorTrigger("Death");
            TryPlayAnimatorState("Death");
        }

        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        else if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        StartCoroutine(DeathVisualRoutine());
    }

    private System.Collections.IEnumerator DeathVisualRoutine()
    {
        float elapsed = 0f;
        Quaternion startRotation = transform.rotation;
        Vector3 euler = startRotation.eulerAngles;
        float side = spriteRenderer != null && spriteRenderer.flipX ? 1f : -1f;
        Quaternion endRotation = Quaternion.Euler(euler.x, euler.y, side * deathFallAngle);
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color endColor = new Color(startColor.r * 0.72f, startColor.g * 0.72f, startColor.b * 0.72f, fadeOutOnDeath ? 0.55f : startColor.a);
        Vector3 startScale = transform.localScale;
        Vector3 endScale = new Vector3(startScale.x * 1.04f, startScale.y * 0.86f, startScale.z);

        while (elapsed < deathAnimationDuration)
        {
            float t = Mathf.Clamp01(elapsed / deathAnimationDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, eased);
            transform.localScale = Vector3.Lerp(startScale, endScale, eased);

            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(startColor, endColor, eased);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRotation;
        transform.localScale = endScale;
        if (spriteRenderer != null)
            spriteRenderer.color = endColor;
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
                return true;
        }

        return false;
    }

    private void TrySetAnimatorBool(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            animator.SetBool(parameterName, value);
    }

    private void TrySetAnimatorTrigger(string parameterName)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(parameterName);
    }

    private bool TryPlayAnimatorState(string stateName)
    {
        if (animator == null || !animator.HasState(0, Animator.StringToHash(stateName)))
            return false;

        animator.Play(stateName, 0, 0f);
        return true;
    }

    private bool IsAnimatorStateActive(string stateName)
    {
        if (animator == null)
            return false;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            return true;
        return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(stateName);
    }

    public int GetCurrentHealth()
    {
        if (barraVida != null)
        {
            return (int)barraVida.GetVidaActual();
        }
        return currentHealth;
    }

    public int GetMaxHealth() => maxHealth;

    // ============================================
    // MÉTODOS PÚBLICOS AUXILIARES
    // ============================================

    public bool IsAttacking() => isAttacking;
    public float BasicAttackCooldown => attackRate;
    public float BasicAttackDuration => attackDuration;
    public float SwordAttackCooldown => swordAttackCooldown;
    public bool IsAttackAudioPlaying => attackAudioSource != null && attackAudioSource.isPlaying;
    public bool IsSwordAnimationActive => swordAnimationActive;
    public bool IsBasicAttackAnimationPlaying => IsAnimatorStateActive("Attack");
    public bool IsSwordAttackAnimationPlaying => IsAnimatorStateActive("SwordAttack");
    public bool IsDead() => isDead;
    public bool IsGrounded() => isGrounded;
    public bool IsJumping() => isJumping;
    public Vector3 GetMoveDirection() => moveDirection;
    public Vector3 GetVelocity() => rb.linearVelocity;

    // ============================================
    // VISUALIZACIÓN EN EDITOR
    // ============================================

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    private void OnValidate()
    {
        ConfigureCombatTimings();
    }
}
