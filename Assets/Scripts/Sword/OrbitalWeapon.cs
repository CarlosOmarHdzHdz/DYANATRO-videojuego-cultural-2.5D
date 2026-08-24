using System.Collections;
using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Macuahuitl equipado
// Acción: mantener el arma en la espalda, animar el tajo y ejecutar Orbitasword.
// ============================================================================
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public sealed class OrbitalWeapon : MonoBehaviour
{
    [Header("Xunjuú v0.1 - Ataque normal")]
    [SerializeField, Min(0.15f)] private float attackDuration = 0.44f;
    [SerializeField] private bool useOriginalPlayerAttackAnimation = true;
    [SerializeField, Range(0.1f, 1f)] private float equippedScaleMultiplier = 0.58f;
    [SerializeField, Min(0.2f)] private float slashForwardOffset = 1.08f;
    [SerializeField] private float slashHeight = 0.42f;
    [SerializeField, Range(45f, 220f)] private float slashArcDegrees = 118f;
    [SerializeField] private Vector3 restLocalPosition = new Vector3(0f, 0.17f, -0.5f);
    [SerializeField] private float restLocalAngle;

    [Header("Xunjuú v0.1 - Ataque especial Orbitasword")]
    [SerializeField, Min(90f)] private float orbitalSpeed = 900f;
    [SerializeField, Min(0.5f)] private float orbitalRadius = 2.05f;
    [SerializeField] private float orbitalHeight = 0.62f;
    [SerializeField, Min(0.25f)] private float orbitalDuration = 1.35f;
    [SerializeField, Min(1)] private int orbitalDamage = 30;
    [SerializeField, Min(0.05f)] private float orbitalHitCooldown = 0.22f;

    private Transform player;
    private SpriteRenderer playerRenderer;
    private SpriteRenderer swordRenderer;
    private DyanatroSpriteDepthSorter swordDepthSorter;
    private Vector3 originalPrefabScale;
    private Vector3 equippedScale;
    private Coroutine attackRoutine;
    private bool scaleInitialized;
    private bool isAttacking;
    private bool isOrbiting;
    private float orbitalTimer;
    private float nextHitTime;
    private float currentAngle;

    public bool IsBusy => isAttacking || isOrbiting;
    public bool UsesOriginalPlayerAttackAnimation => useOriginalPlayerAttackAnimation;
    public bool IsWeaponVisible => swordRenderer != null && swordRenderer.enabled;

    // ACCIÓN: equipar una sola vez y apagar el Animator antiguo que competía con el código.
    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
        swordRenderer = GetComponentInChildren<SpriteRenderer>();
        playerRenderer = FindPlayerRenderer();
        DisableCompetingDepthSorter();

        if (!scaleInitialized)
        {
            originalPrefabScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            equippedScale = originalPrefabScale * equippedScaleMultiplier;
            scaleInitialized = true;
        }

        Animator oldAnimator = GetComponent<Animator>();
        if (oldAnimator != null)
            oldAnimator.enabled = false;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        isAttacking = false;
        isOrbiting = false;
        ReturnToRest();
        Debug.Log("Xunjuú v0.1: macuahuitl equipado y listo.");
    }

    private void Update()
    {
        if (player == null)
            return;

        if (isOrbiting)
            UpdateOrbitasword();
        else if (!isAttacking)
            ReturnToRest();
    }

    // ACCIÓN: iniciar un tajo visible de preparación, golpe y recuperación.
    public bool ExecuteAttack()
    {
        if (player == null || IsBusy)
            return false;

        attackRoutine = StartCoroutine(AttackCoroutine());
        return true;
    }

    private IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        if (useOriginalPlayerAttackAnimation)
        {
            // Xunjuu v0.1 - ACCION: el spritesheet SwordAttack ya dibuja al
            // protagonista con el Macuahuitl; ocultar la copia de la espalda.
            ReturnToRest();
            SetVisible(false);
            yield return new WaitForSeconds(attackDuration);
            isAttacking = false;
            attackRoutine = null;
            ReturnToRest();
            yield break;
        }

        SetVisible(true);
        SetSwordSorting(false);

        float facing = GetFacingSign();
        Vector3 restPosition = GetRestPosition(facing);
        Quaternion restRotation = GetRestRotation(facing);
        Vector3 slashStart = GetClassicSlashPosition(facing, 0f);
        Quaternion slashStartRotation = GetClassicSlashRotation(facing, 0f);

        float timer = 0f;
        while (timer < attackDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / attackDuration);

            if (t < 0.18f)
            {
                float prepare = Mathf.SmoothStep(0f, 1f, t / 0.18f);
                transform.localPosition = Vector3.Lerp(restPosition, slashStart, prepare);
                transform.localRotation = Quaternion.Slerp(restRotation, slashStartRotation, prepare);
            }
            else if (t < 0.8f)
            {
                float strike = Mathf.SmoothStep(0f, 1f, (t - 0.18f) / 0.62f);
                transform.localPosition = GetClassicSlashPosition(facing, strike);
                transform.localRotation = GetClassicSlashRotation(facing, strike);
            }
            else
            {
                float recover = Mathf.SmoothStep(0f, 1f, (t - 0.8f) / 0.2f);
                Vector3 strikeEnd = GetClassicSlashPosition(facing, 1f);
                Quaternion strikeRotation = GetClassicSlashRotation(facing, 1f);
                transform.localPosition = Vector3.Lerp(strikeEnd, restPosition, recover);
                transform.localRotation = Quaternion.Slerp(strikeRotation, restRotation, recover);
            }

            transform.localScale = equippedScale;
            yield return null;
        }

        isAttacking = false;
        attackRoutine = null;
        ReturnToRest();
    }

    // ACCIÓN: iniciar el ataque especial sin interrumpir otro movimiento del arma.
    public bool ExecuteOrbitalAttack()
    {
        if (player == null || IsBusy)
            return false;

        isOrbiting = true;
        orbitalTimer = orbitalDuration;
        currentAngle = 0f;
        nextHitTime = 0f;
        SetVisible(true);
        return true;
    }

    private void UpdateOrbitasword()
    {
        orbitalTimer -= Time.deltaTime;
        currentAngle += orbitalSpeed * Time.deltaTime * GetFacingSign();

        float radians = currentAngle * Mathf.Deg2Rad;
        float x = Mathf.Cos(radians) * orbitalRadius;
        float z = Mathf.Sin(radians) * orbitalRadius;
        float y = orbitalHeight + Mathf.Sin(radians * 2f) * 0.1f;

        transform.localPosition = new Vector3(x, y, z);
        transform.localRotation = Quaternion.Euler(0f, 0f, 205f + Mathf.Cos(radians) * 24f);
        transform.localScale = equippedScale * (1.02f + Mathf.Sin(radians * 2f) * 0.05f);
        SetSwordSorting(z > 0f);

        if (orbitalTimer > 0f)
            return;

        isOrbiting = false;
        ReturnToRest();
        Debug.Log("Xunjuú v0.1: Orbitasword terminó correctamente.");
    }

    public bool IsOrbiting() => isOrbiting;
    public bool CanHit() => isOrbiting && Time.time >= nextHitTime;
    public void RegisterHit() => nextHitTime = Time.time + orbitalHitCooldown;
    public int GetOrbitalDamage() => orbitalDamage;
    public float GetAttackDuration() => attackDuration;
    public float GetOrbitalDuration() => orbitalDuration;
    public Vector3 GetWeaponPosition() => transform.position;

    // ACCIÓN: permitir que la validación compruebe que el arma regresó a la espalda.
    public bool IsAtRest(float tolerance = 0.06f)
    {
        if (player == null || IsBusy)
            return false;
        return Vector3.Distance(transform.localPosition, GetRestPosition(GetFacingSign())) <= tolerance;
    }

    private Vector3 GetClassicSlashPosition(float facing, float progress)
    {
        float halfArc = slashArcDegrees * 0.5f;
        float angle = Mathf.Lerp(90f + halfArc, 90f - halfArc, progress);
        float radians = angle * Mathf.Deg2Rad;
        return new Vector3(
            Mathf.Cos(radians) * slashForwardOffset * facing,
            slashHeight + Mathf.Sin(radians) * slashForwardOffset * 0.42f,
            -0.04f);
    }

    private Quaternion GetClassicSlashRotation(float facing, float progress)
    {
        float halfArc = slashArcDegrees * 0.5f;
        float angle = Mathf.Lerp(90f + halfArc, 90f - halfArc, progress);
        return Quaternion.Euler(0f, 0f, MirrorAngle(angle - 90f, facing));
    }

    private void ReturnToRest()
    {
        float facing = GetFacingSign();
        transform.localPosition = GetRestPosition(facing);
        transform.localRotation = GetRestRotation(facing);
        transform.localScale = equippedScale;
        SetVisible(true);
        SetSwordSorting(true);
    }

    private Vector3 GetRestPosition(float facing)
    {
        return new Vector3(restLocalPosition.x * facing, restLocalPosition.y, restLocalPosition.z);
    }

    private Quaternion GetRestRotation(float facing)
    {
        return Quaternion.Euler(0f, 0f, MirrorAngle(restLocalAngle, facing));
    }

    private float GetFacingSign()
    {
        if (playerRenderer == null && player != null)
            playerRenderer = FindPlayerRenderer();
        return playerRenderer != null && playerRenderer.flipX ? -1f : 1f;
    }

    private static float MirrorAngle(float angle, float facing)
    {
        return facing < 0f ? 180f - angle : angle;
    }

    private void SetVisible(bool visible)
    {
        if (swordRenderer == null)
            swordRenderer = GetComponentInChildren<SpriteRenderer>();
        if (swordRenderer != null)
            swordRenderer.enabled = visible;
    }

    private void SetSwordSorting(bool behindPlayer)
    {
        DisableCompetingDepthSorter();
        if (swordRenderer == null)
            swordRenderer = GetComponentInChildren<SpriteRenderer>();
        if (playerRenderer == null && player != null)
            playerRenderer = FindPlayerRenderer();
        if (swordRenderer == null)
            return;

        int playerOrder = playerRenderer != null ? playerRenderer.sortingOrder : 500;
        if (playerRenderer != null)
            swordRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        swordRenderer.sortingOrder = behindPlayer ? playerOrder - 2 : playerOrder + 4;
    }

    // ========================================================================
    // Xunjuu v0.1 - Capa estable del Macuahuitl
    // ACCION: excluir la espada del ordenamiento global para que no salte al
    // frente del protagonista cuando descansa en su espalda.
    // ========================================================================
    private void DisableCompetingDepthSorter()
    {
        if (swordRenderer == null)
            swordRenderer = GetComponentInChildren<SpriteRenderer>();
        if (swordRenderer == null)
            return;

        if (swordDepthSorter == null)
            swordDepthSorter = swordRenderer.GetComponent<DyanatroSpriteDepthSorter>();
        if (swordDepthSorter != null)
            swordDepthSorter.enabled = false;
    }

    private SpriteRenderer FindPlayerRenderer()
    {
        if (player == null)
            return null;

        foreach (SpriteRenderer candidate in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (candidate != null && !candidate.transform.IsChildOf(transform))
                return candidate;
        }
        return null;
    }

    public bool IsRenderedBehindPlayer()
    {
        return swordRenderer != null && playerRenderer != null
            && swordRenderer.sortingLayerID == playerRenderer.sortingLayerID
            && swordRenderer.sortingOrder < playerRenderer.sortingOrder;
    }
}
