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
    public enum BehaviourState { Idle, Wander, Flee, Alert }
    public BehaviourState State => isFleeing ? BehaviourState.Flee : isAlert ? BehaviourState.Alert : isWaiting ? BehaviourState.Idle : BehaviourState.Wander;

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
    [SerializeField, Min(0.5f)] private float fleeMemory = 2.4f;
    [SerializeField, Range(1.1f, 2f)] private float fleeReleaseDistanceMultiplier = 1.45f;

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
    private bool isAlert;
    private float alertSince, nextSenseTime, sensedDistance = float.PositiveInfinity;
    private bool seesPlayer;
    private Vector3 steering, escapeDirection;
    private float escapeUntil, avoidanceUntil;
    private int avoidanceSide = 1;
    private float hullRadius = .35f;
    private readonly RaycastHit[] navigationHits = new RaycastHit[32];
    public float ActualSpeed => body == null ? 0 : Flatten(body.linearVelocity).magnitude;

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
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Collider shape=GetComponent<Collider>();
        // Use the widest horizontal extent, including non-uniform prefab scale.
        // The narrower extent let the steering probe clear corners that the body hit.
        if(shape!=null) hullRadius=Mathf.Max(.22f,Mathf.Max(shape.bounds.extents.x,shape.bounds.extents.z)+.06f);
        avoidanceSide=GetInstanceID()%2==0?1:-1;

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

        if(Time.time>=nextSenseTime)
        {
            sensedDistance=HorizontalDistance(transform.position,player.position);
            Vector3 sight=player.position+Vector3.up*.6f-(transform.position+Vector3.up*.6f);
            float sightRange=wasFleeing?detectionRange*fleeReleaseDistanceMultiplier:detectionRange;
            seesPlayer=sensedDistance<=sightRange && !HasSolidHit(transform.position+Vector3.up*.6f,sight.normalized,sight.magnitude,.05f,true);
            nextSenseTime=Time.time+.15f;
        }
        float playerDistance = sensedDistance;
        bool playerInsideDetection = seesPlayer && playerDistance <= detectionRange;
        if(playerInsideDetection && !isAlert){isAlert=true;alertSince=Time.time;}
        if(!playerInsideDetection && !isFleeing)isAlert=false;
        bool immediateDanger=playerDistance<detectionRange*.55f;
        bool assessedDanger=isAlert && Time.time-alertSince>(species==AnimalSpecies.Deer?.35f:.7f);
        if (canFlee && playerInsideDetection && (immediateDanger || assessedDanger))
            fleeUntilTime = Mathf.Max(fleeUntilTime, Time.time + fleeMemory);

        bool recentlyAlerted = Time.time < fleeUntilTime;
        bool playerStillClose = wasFleeing && seesPlayer && playerDistance <= detectionRange * fleeReleaseDistanceMultiplier;
        isFleeing = canFlee && (playerStillClose || recentlyAlerted);
        if (isFleeing && !wasFleeing)
            PlayVocal(hurtVocalSound != null ? hurtVocalSound : ResolveVocalClip(), vocalVolume * 0.92f);
        if (wasFleeing && !isFleeing)
        {
            // Rest before returning to the original territory. Rebasing home after
            // each encounter let repeated chases move mission animals across the map.
            CancelWaiting();
            waitRoutine = StartCoroutine(WaitAtPoint());
        }
        wasFleeing = isFleeing;

        if (isFleeing)
        {
            CancelWaiting();
        }
    }

    private void FixedUpdate()
    {
        if (body == null)
            return;

        if(Time.time>=nextProgressCheckTime)
        {
            if(!isWaiting && (!isAlert || isFleeing) && HorizontalDistance(transform.position,lastProgressPosition)<.15f)
            { avoidanceSide=-avoidanceSide;escapeDirection=FindEscapeDirection();escapeUntil=Time.time+1.1f;SetNewWanderTarget(); }
            lastProgressPosition=transform.position;nextProgressCheckTime=Time.time+stuckCheckInterval;
        }
        if(Time.time<escapeUntil){MoveInDirection(FindClearDirection(escapeDirection),isFleeing?fleeSpeed*.7f:walkSpeed);return;}
        if(isAlert && !isFleeing){StopHorizontalMovement();return;}

        // Xunjuu v0.1 - ACCION: renovar el paseo si el animal lleva demasiado
        // tiempo en la misma ruta o un obstaculo lo dejo detenido.
        if (!isFleeing && !isWaiting)
        {
            if (Time.time >= nextWanderRetargetTime)
                SetNewWanderTarget();

        }

        if (isFleeing && player != null)
        {
            Vector3 away = Flatten(transform.position - player.position).normalized;
            if (away.sqrMagnitude < 0.001f)
                away = transform.right;
            Vector3 outward=Flatten(transform.position-homePosition);
            float boundary=Mathf.SmoothStep(0,1,Mathf.InverseLerp(territoryRadius*.8f,territoryRadius*1.5f,outward.magnitude));
            if(boundary>0)
            {
                Vector3 tangent=Vector3.Cross(Vector3.up,outward.normalized)*avoidanceSide;
                Vector3 alongBoundary=(tangent-outward.normalized*.65f).normalized;
                away=Vector3.Lerp(away,alongBoundary,boundary).normalized;
            }
            MoveInDirection(FindClearDirection(away), fleeSpeed);
            return;
        }

        Vector3 fromHome = Flatten(transform.position - homePosition);
        if (fromHome.magnitude > territoryRadius)
        {
            CancelWaiting();
            MoveInDirection(FindClearDirection(Flatten(homePosition - transform.position).normalized), walkSpeed * 1.25f);
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

    private void LateUpdate()
    {
        if (animator == null || body == null) return;
        float speed = new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude;
        UpdateAnimation(speed < 0.1f ? idleAnimation : isFleeing ? runAnimation : walkAnimation);
        animator.speed = speed < 0.1f ? 1f : Mathf.Clamp(speed / Mathf.Max(0.1f,
            currentAnimation == runAnimation ? fleeSpeed : walkSpeed), 0.5f, 2f);
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

        Vector3 current=Flatten(body.linearVelocity);
        Vector3 target=direction*speed;
        steering=Vector3.MoveTowards(current,target,(isFleeing?12f:5f)*Time.fixedDeltaTime);
        body.linearVelocity = new Vector3(steering.x, body.linearVelocity.y, steering.z);
        TryPlayFootstep(steering.magnitude);
        if (spriteRenderer != null)
        {
            Camera camera = Camera.main;
            float screenDirection = camera != null ? Vector3.Dot(steering, camera.transform.right) : steering.x;
            if (Mathf.Abs(screenDirection) > 0.25f)
                spriteRenderer.flipX = screenDirection < 0f;
        }
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
        if (Time.time>=avoidanceUntil && IsDirectionClear(desired))
            return desired;
        Vector3 best=Vector3.zero;float bestScore=float.NegativeInfinity;
        for(int i=0;i<12;i++)
        {
            float angle=i<=6?i*30f:(i-12)*30f;
            Vector3 alternative = Quaternion.Euler(0f, angle, 0f) * desired;
            if (!IsDirectionClear(alternative))continue;
            float score=Vector3.Dot(alternative,desired)*2f+Vector3.Dot(alternative,steering.normalized)*.8f;
            if(Mathf.Sign(angle)==avoidanceSide)score+=.35f;
            if(score>bestScore){best=alternative;bestScore=score;}
        }
        if(best.sqrMagnitude>.01f && Vector3.Dot(best,desired)<.95f)avoidanceUntil=Time.time+.45f;
        return best;
    }

    private bool IsDirectionClear(Vector3 direction)
    {
        Vector3 origin = transform.position + Vector3.up * (hullRadius+.2f);
        if(HasSolidHit(origin,direction,obstacleCheckDistance,hullRadius,false))return false;
        Vector3 ahead=transform.position+direction*obstacleCheckDistance;
        foreach(Terrain terrain in Terrain.activeTerrains)
        {
            Vector3 local=ahead-terrain.transform.position, size=terrain.terrainData.size;
            if(local.x<0 || local.z<0 || local.x>size.x || local.z>size.z)continue;
            return terrain.terrainData.GetSteepness(local.x/size.x,local.z/size.z)<38f;
        }
        return Physics.Raycast(ahead+Vector3.up*2,Vector3.down,4f,~0,QueryTriggerInteraction.Ignore);
    }

    private bool HasSolidHit(Vector3 origin,Vector3 direction,float distance,float radius,bool sight)
    {
        int count=Physics.SphereCastNonAlloc(origin,radius,direction,navigationHits,distance,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var hit=navigationHits[i];var collider=hit.collider;
            if(collider==null || collider.transform.IsChildOf(transform) || (sight && player!=null && collider.transform.IsChildOf(player)))continue;
            if(hit.normal.y>.65f || collider is TerrainCollider || XunjuuSceneVisualPolicy.IsSoftDecoration(collider.transform))continue;
            return true;
        }
        return false;
    }

    private Vector3 FindEscapeDirection()
    {
        Vector3 away=player!=null?Flatten(transform.position-player.position).normalized:Flatten(wanderTarget-transform.position).normalized;
        for(int i=0;i<12;i++)
        {Vector3 dir=Quaternion.Euler(0,i*30*avoidanceSide,0)*away;if(IsDirectionClear(dir))return dir;}
        return Vector3.zero;
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
            if (hit == null || hit is TerrainCollider || hit.transform.IsChildOf(transform) || XunjuuSceneVisualPolicy.IsSoftDecoration(hit.transform))
                continue;
            return false;
        }
        return true;
    }

    private Vector3 SnapPointToGround(Vector3 point)
    {
        foreach(Terrain terrain in Terrain.activeTerrains)
        {
            Vector3 local=point-terrain.transform.position,size=terrain.terrainData.size;
            if(local.x>=0 && local.z>=0 && local.x<=size.x && local.z<=size.z)
                return new Vector3(point.x,terrain.SampleHeight(point)+terrain.transform.position.y+.08f,point.z);
        }
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
        {steering=Vector3.MoveTowards(Flatten(body.linearVelocity),Vector3.zero,8f*Time.fixedDeltaTime);body.linearVelocity = new Vector3(steering.x, body.linearVelocity.y, steering.z);}
    }

    private void OnDisable(){CancelWaiting();isAlert=false;isFleeing=false;wasFleeing=false;steering=Vector3.zero;}
    private void OnEnable()
    {
        nextProgressCheckTime=Time.time+stuckCheckInterval;lastProgressPosition=transform.position;
        if(body!=null){homePosition=SnapPointToGround(transform.position);SetNewWanderTarget();}
    }

    private void UpdateAnimation(string animationName)
    {
        if (animator == null)
            return;

        int stateHash = Animator.StringToHash(animationName);
        if (!animator.HasState(0, stateHash))
        {
            animationName = animationName == runAnimation ? walkAnimation : idleAnimation;
            stateHash = Animator.StringToHash(animationName);
            if (!animator.HasState(0, stateHash))
                return;
        }

        if (currentAnimation == animationName)
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
        fleeMemory = Mathf.Max(0.5f, fleeMemory);
        fleeReleaseDistanceMultiplier = Mathf.Clamp(fleeReleaseDistanceMultiplier, 1.1f, 2f);
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
