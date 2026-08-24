using System.Collections;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Comportamiento territorial de fauna
// ACCION: pasear, huir, evitar obstaculos y permanecer dentro de una zona.
// MODIFICACION: especie, velocidades y territorio se editan en cada prefab.
// ============================================================================
[DisallowMultipleComponent]
public class Animal : MonoBehaviour
{
    public enum AnimalSpecies
    {
        Generic,
        Duck,
        Deer
    }

    [Header("Xunjuu v0.1 - Especie")]
    [SerializeField] private AnimalSpecies species = AnimalSpecies.Generic;
    public bool canFlee = true;

    [Header("Xunjuu v0.1 - Movimiento")]
    [SerializeField, Min(1f)] private float detectionRange = 9f;
    [SerializeField, Min(0.2f)] private float fleeSpeed = 5f;
    [SerializeField, Min(0.2f)] private float walkSpeed = 1.8f;
    [SerializeField, Min(0.2f)] private float idleTime = 2f;
    [SerializeField, Min(2f)] private float territoryRadius = 8f;
    [SerializeField, Min(0.5f)] private float obstacleCheckDistance = 1.4f;
    [SerializeField, Min(2f)] private float wanderRetargetInterval = 5f;
    [SerializeField, Min(0.5f)] private float stuckCheckInterval = 1.2f;

    [Header("Xunjuu v0.1 - Animaciones")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string idleAnimation = "Idle";
    [SerializeField] private string walkAnimation = "Walk";
    [SerializeField] private string runAnimation = "Run";

    [Header("Xunjuu v0.1 - Sonido de pisadas")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField, Min(0.15f)] private float footstepInterval = 0.48f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.28f;

    [Header("Xunjuu v0.1 - Voz del animal")]
    [Tooltip("Voz ambiental. Si queda vacia se genera una distinta para pato o venado.")]
    [SerializeField] private AudioClip ambientVocalSound;
    [Tooltip("Voz al recibir un golpe. Puede reemplazarse desde el prefab.")]
    [SerializeField] private AudioClip hurtVocalSound;
    [SerializeField, Range(0f, 1f)] private float vocalVolume = 0.72f;
    [SerializeField] private Vector2 vocalIntervalRange = new Vector2(6.5f, 12f);

    private Transform player;
    private Rigidbody body;
    private Vector3 homePosition;
    private Vector3 wanderTarget;
    private bool isFleeing;
    private bool isWaiting;
    private string currentAnimation;
    private Coroutine waitRoutine;
    private AudioSource audioSource;
    private AudioSource voiceAudioSource;
    private float nextFootstepTime;
    private float nextVocalTime;
    private bool wasFleeing;
    private float nextWanderRetargetTime;
    private float nextProgressCheckTime;
    private Vector3 lastProgressPosition;
    private float fleeUntilTime;

    public AnimalSpecies Species => species;
    public Vector3 TerritoryCenter => homePosition;
    public float TerritoryRadius => territoryRadius;
    public bool HasFootstepAudio => footstepSound != null && audioSource != null;
    public bool HasVocalAudio => ResolveVocalClip() != null && voiceAudioSource != null;
    public float VocalVolume => vocalVolume;
    public float WalkSpeed => walkSpeed;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponentInChildren<SpriteRenderer>();
        animator = animator != null ? animator : GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController == null)
            animator = null;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.68f;
        if (footstepSound == null)
            footstepSound = Resources.Load<AudioClip>("Audio/pasos_pasto");

        AudioSource[] sources = GetComponents<AudioSource>();
        voiceAudioSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        voiceAudioSource.playOnAwake = false;
        voiceAudioSource.loop = false;
        voiceAudioSource.spatialBlend = 0.58f;
        voiceAudioSource.minDistance = 4f;
        voiceAudioSource.maxDistance = 32f;
        ResolveVocalClip();
        nextVocalTime = Time.time + Random.Range(vocalIntervalRange.x, vocalIntervalRange.y);

        transform.rotation = Quaternion.identity;
        homePosition = SnapPointToGround(transform.position);
        lastProgressPosition = transform.position;
        nextProgressCheckTime = Time.time + stuckCheckInterval;
        SetNewWanderTarget();
        UpdateAnimation(idleAnimation);
    }

    private void Update()
    {
        TryPlayAmbientVocal();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            return;

        float playerDistance = HorizontalDistance(transform.position, player.position);
        bool insideOuterTerritory = HorizontalDistance(transform.position, homePosition) < territoryRadius * 0.92f;
        bool recentlyAlerted = Time.time < fleeUntilTime;
        isFleeing = canFlee && insideOuterTerritory && (playerDistance <= detectionRange || recentlyAlerted);
        if (isFleeing && !wasFleeing)
            PlayVocal(hurtVocalSound != null ? hurtVocalSound : ResolveVocalClip(), vocalVolume * 0.92f);
        wasFleeing = isFleeing;

        if (isFleeing)
        {
            CancelWaiting();
            UpdateAnimation(runAnimation);
        }
        else if (!isWaiting)
        {
            UpdateAnimation(walkAnimation);
        }
    }

    private void FixedUpdate()
    {
        if (body == null)
            return;

        // Xunjuu v0.1 - ACCION: renovar el paseo si el animal lleva demasiado
        // tiempo en la misma ruta o un obstaculo lo dejo detenido.
        if (!isFleeing && !isWaiting)
        {
            if (Time.time >= nextWanderRetargetTime)
                SetNewWanderTarget();

            if (Time.time >= nextProgressCheckTime)
            {
                if (HorizontalDistance(transform.position, lastProgressPosition) < 0.1f)
                    SetNewWanderTarget();
                lastProgressPosition = transform.position;
                nextProgressCheckTime = Time.time + stuckCheckInterval;
            }
        }

        Vector3 fromHome = Flatten(transform.position - homePosition);
        if (fromHome.magnitude > territoryRadius)
        {
            CancelWaiting();
            MoveInDirection(Flatten(homePosition - transform.position).normalized, fleeSpeed);
            return;
        }

        if (isFleeing && player != null)
        {
            Vector3 away = Flatten(transform.position - player.position).normalized;
            Vector3 predicted = transform.position + away * 1.4f;
            if (HorizontalDistance(predicted, homePosition) > territoryRadius)
                away = Flatten(homePosition - transform.position).normalized;
            MoveInDirection(FindClearDirection(away), fleeSpeed);
            return;
        }

        if (isWaiting)
        {
            StopHorizontalMovement();
            return;
        }

        Vector3 toTarget = Flatten(wanderTarget - transform.position);
        if (toTarget.magnitude <= 0.55f)
        {
            waitRoutine = StartCoroutine(WaitAtPoint());
            return;
        }

        MoveInDirection(FindClearDirection(toTarget.normalized), walkSpeed);
    }

    // Xunjuu v0.1 - ACCION: aplicar parametros naturales desde el constructor de prefabs.
    public void ConfigureSpecies(AnimalSpecies newSpecies)
    {
        species = newSpecies;
        if (species == AnimalSpecies.Deer)
        {
            detectionRange = 12f;
            fleeSpeed = 6.5f;
            walkSpeed = 2.8f;
            idleTime = 0.9f;
            territoryRadius = 16f;
            wanderRetargetInterval = 3.8f;
            footstepVolume = 0.36f;
            vocalVolume = 0.78f;
            vocalIntervalRange = new Vector2(5f, 8.5f);
        }
        else if (species == AnimalSpecies.Duck)
        {
            detectionRange = 7f;
            fleeSpeed = 4.2f;
            walkSpeed = 1.85f;
            idleTime = 0.85f;
            territoryRadius = 12f;
            wanderRetargetInterval = 3.2f;
            footstepVolume = 0.34f;
            vocalVolume = 0.82f;
            vocalIntervalRange = new Vector2(3.8f, 6.5f);
        }
    }

    private void MoveInDirection(Vector3 direction, float speed)
    {
        direction = Flatten(direction).normalized;
        if (direction.sqrMagnitude < 0.001f)
        {
            StopHorizontalMovement();
            return;
        }

        body.linearVelocity = new Vector3(direction.x * speed, body.linearVelocity.y, direction.z * speed);
        TryPlayFootstep(speed);
        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.02f)
            spriteRenderer.flipX = direction.x < 0f;
    }

    // Xunjuu v0.1 - ACCION: variar las pisadas de pato y venado sobre pasto.
    private void TryPlayFootstep(float movementSpeed)
    {
        if (audioSource == null || footstepSound == null || movementSpeed < 0.2f || Time.time < nextFootstepTime)
            return;

        float speciesPitch = species == AnimalSpecies.Duck ? 1.28f : species == AnimalSpecies.Deer ? 0.88f : 1.05f;
        float finalPitch = speciesPitch * Random.Range(0.94f, 1.06f);
        audioSource.pitch = finalPitch;
        audioSource.PlayOneShot(footstepSound, footstepVolume);
        audioSource.pitch = 1f;
        float speciesInterval = species == AnimalSpecies.Duck ? footstepInterval * 0.78f : footstepInterval;
        float effectiveLength = footstepSound.length / Mathf.Max(0.1f, finalPitch);
        nextFootstepTime = Time.time + Mathf.Max(speciesInterval, effectiveLength * 0.9f);
    }

    // ========================================================================
    // Xunjuu v0.1 - Voces editables de pato y venado
    // ACCION: emitir una llamada ocasional y reaccionar al golpe sin acumularla.
    // ========================================================================
    private void TryPlayAmbientVocal()
    {
        if (Time.time < nextVocalTime || isFleeing)
            return;

        PlayVocal(ResolveVocalClip(), vocalVolume);
        nextVocalTime = Time.time + Random.Range(vocalIntervalRange.x, vocalIntervalRange.y);
    }

    public void PlayHurtVocal()
    {
        // Xunjuu v0.1 - ACCION: recordar el peligro para que no se detenga
        // inmediatamente despues de recibir un golpe.
        fleeUntilTime = Time.time + (species == AnimalSpecies.Deer ? 3f : 2.3f);
        CancelWaiting();
        PlayVocal(hurtVocalSound != null ? hurtVocalSound : ResolveVocalClip(), Mathf.Min(1f, vocalVolume * 1.12f));
        nextVocalTime = Time.time + Random.Range(vocalIntervalRange.x, vocalIntervalRange.y);
    }

    public AudioClip GetDefeatVocalClip()
    {
        return hurtVocalSound != null ? hurtVocalSound : ResolveVocalClip();
    }

    private AudioClip ResolveVocalClip()
    {
        if (ambientVocalSound != null)
            return ambientVocalSound;

        if (species == AnimalSpecies.Duck)
            ambientVocalSound = XunjuuProceduralAudio.GetDuckCall();
        else if (species == AnimalSpecies.Deer)
            ambientVocalSound = XunjuuProceduralAudio.GetDeerCall();

        return ambientVocalSound;
    }

    private void PlayVocal(AudioClip clip, float volume)
    {
        if (clip == null || voiceAudioSource == null)
            return;

        voiceAudioSource.Stop();
        float basePitch = species == AnimalSpecies.Duck ? 1.06f : species == AnimalSpecies.Deer ? 0.94f : 1f;
        voiceAudioSource.pitch = basePitch * Random.Range(0.96f, 1.04f);
        voiceAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // ACCION: probar direcciones laterales cuando un arbol u objeto bloquea el paso.
    private Vector3 FindClearDirection(Vector3 desired)
    {
        desired = Flatten(desired).normalized;
        if (IsDirectionClear(desired))
            return desired;

        Vector3 left = new Vector3(-desired.z, 0f, desired.x);
        if (IsDirectionClear(left))
            return left;
        Vector3 right = -left;
        if (IsDirectionClear(right))
            return right;

        SetNewWanderTarget();
        return Flatten(homePosition - transform.position).normalized;
    }

    private bool IsDirectionClear(Vector3 direction)
    {
        Vector3 origin = transform.position + Vector3.up * 0.55f + direction * 0.52f;
        if (!Physics.Raycast(origin, direction, out RaycastHit hit, obstacleCheckDistance, ~0, QueryTriggerInteraction.Ignore))
            return true;
        return hit.collider == null || hit.collider.transform.IsChildOf(transform);
    }

    // ACCION: elegir un punto dentro del territorio, sobre suelo y fuera de objetos.
    private void SetNewWanderTarget()
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * territoryRadius * 0.82f;
            Vector3 candidate = homePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
            candidate = SnapPointToGround(candidate);
            if (IsPointFree(candidate))
            {
                wanderTarget = candidate;
                nextWanderRetargetTime = Time.time + wanderRetargetInterval;
                return;
            }
        }
        wanderTarget = homePosition;
        nextWanderRetargetTime = Time.time + wanderRetargetInterval * 0.5f;
    }

    private bool IsPointFree(Vector3 point)
    {
        Collider[] hits = Physics.OverlapSphere(point + Vector3.up * 0.55f, 0.48f, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit == null || hit is TerrainCollider || hit.transform.IsChildOf(transform))
                continue;
            return false;
        }
        return true;
    }

    private Vector3 SnapPointToGround(Vector3 point)
    {
        Vector3 origin = new Vector3(point.x, point.y + 30f, point.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
                continue;
            return hit.point + Vector3.up * 0.08f;
        }
        return point;
    }

    private IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        UpdateAnimation(idleAnimation);
        StopHorizontalMovement();
        yield return new WaitForSeconds(Random.Range(idleTime * 0.75f, idleTime * 1.15f));
        isWaiting = false;
        waitRoutine = null;
        SetNewWanderTarget();
    }

    private void CancelWaiting()
    {
        if (waitRoutine != null)
            StopCoroutine(waitRoutine);
        waitRoutine = null;
        isWaiting = false;
    }

    private void StopHorizontalMovement()
    {
        if (body != null)
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
    }

    private void UpdateAnimation(string animationName)
    {
        if (animator == null || currentAnimation == animationName)
            return;
        currentAnimation = animationName;
        animator.Play(animationName, 0, 0f);
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
        detectionRange = Mathf.Max(1f, detectionRange);
        territoryRadius = Mathf.Max(2f, territoryRadius);
        obstacleCheckDistance = Mathf.Max(0.5f, obstacleCheckDistance);
        wanderRetargetInterval = Mathf.Max(2f, wanderRetargetInterval);
        stuckCheckInterval = Mathf.Max(0.5f, stuckCheckInterval);
        vocalIntervalRange.x = Mathf.Max(2f, vocalIntervalRange.x);
        vocalIntervalRange.y = Mathf.Max(vocalIntervalRange.x + 0.5f, vocalIntervalRange.y);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.color = species == AnimalSpecies.Deer ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(center, territoryRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
