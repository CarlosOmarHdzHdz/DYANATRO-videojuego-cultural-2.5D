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
    private XunjuuOrbitalBloom bloom;
    private XunjuuCompleteSpriteAnimator poseAnimator;
    private Sprite equipmentSprite;
    private Material equipmentMaterial;
    private float directionalAttackProgress;

    public bool IsBusy => isAttacking || isOrbiting;
    public bool UsesOriginalPlayerAttackAnimation => useOriginalPlayerAttackAnimation;
    public bool IsWeaponVisible => swordRenderer != null && swordRenderer.enabled;

    // ACCIÓN: equipar una sola vez y apagar el Animator antiguo que competía con el código.
    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
        swordRenderer = GetComponentInChildren<SpriteRenderer>();
        playerRenderer = FindPlayerRenderer();
        poseAnimator = player != null ? player.GetComponent<XunjuuCompleteSpriteAnimator>() : null;
        if (poseAnimator != null && poseAnimator.HasRefreshedArt) useOriginalPlayerAttackAnimation = false;
        bloom = GetComponent<XunjuuOrbitalBloom>() ?? gameObject.AddComponent<XunjuuOrbitalBloom>();
        DisableCompetingDepthSorter();

        if (!scaleInitialized)
        {
            originalPrefabScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            equippedScale = originalPrefabScale * equippedScaleMultiplier;
            scaleInitialized = true;
        }
        ConfigureDirectionalEquipment();

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

    private void LateUpdate()
    {
        if (player == null)
            return;

        if (isOrbiting)
            UpdateOrbitasword();
        else if (!isAttacking)
            ReturnToRest();
        // The rendered hand is authoritative. Never interpolate the grip through
        // empty space while a four-frame arm changes pose.
        if(!isOrbiting && poseAnimator!=null && poseAnimator.HasDirectionalArt)
            ApplyHandGrip();
        if (bloom != null) bloom.Refresh(isOrbiting, player, currentAngle);
        if (isOrbiting && swordRenderer != null) swordRenderer.enabled = false;
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
            directionalAttackProgress=t;
            if(poseAnimator!=null && poseAnimator.HasDirectionalArt){yield return null;continue;}

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
        currentAngle = Mathf.Repeat(currentAngle + orbitalSpeed * Time.deltaTime, 360f);

        float radians = currentAngle * Mathf.Deg2Rad;
        float x = Mathf.Cos(radians) * orbitalRadius;
        float z = Mathf.Sin(radians) * orbitalRadius;
        float y = orbitalHeight;

        // World coordinates keep the radius independent of the player's rotation and scale.
        transform.position = player.position + new Vector3(x, y, z);
        Quaternion billboard = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            billboard * Quaternion.Euler(0f, 0f, 205f), 1f - Mathf.Exp(-20f * Time.deltaTime));
        transform.localScale = equippedScale;
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
        if(poseAnimator!=null && poseAnimator.HasDirectionalArt)return Vector3.Distance(transform.position,poseAnimator.HandGripWorld)<=tolerance;
        return Vector3.Distance(transform.localPosition, GetRestPosition(GetFacingSign())) <= tolerance;
    }

    private Vector3 GetClassicSlashPosition(float facing, float progress)
    {
        float halfArc = slashArcDegrees * 0.5f;
        float angle = Mathf.Lerp(90f + halfArc, 90f - halfArc, progress);
        float radians = angle * Mathf.Deg2Rad;
        if(poseAnimator != null && poseAnimator.HasDirectionalArt)
        {
            bool vertical=poseAnimator.FacingIndex==0 || poseAnimator.FacingIndex==4;
            float reach=vertical?.30f:.55f;
            SetSwordSorting(poseAnimator.FacingAway);
            return new Vector3((reach + Mathf.Cos(radians)*.32f)*facing,
                .22f+Mathf.Sin(radians)*.20f,poseAnimator.FacingAway?.04f:-.04f);
        }
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
        if(poseAnimator!=null && poseAnimator.HasDirectionalArt){ApplyHandGrip();SetVisible(true);return;}
        float facing = GetFacingSign();
        transform.localPosition = GetRestPosition(facing);
        transform.localRotation = GetRestRotation(facing);
        transform.localScale = equippedScale;
        SetVisible(true);
        SetSwordSorting(poseAnimator == null || !poseAnimator.HasDirectionalArt || !poseAnimator.FacingAway);
    }

    private void ApplyHandGrip()
    {
        transform.position=poseAnimator.HandGripWorld;
        float angle=isAttacking?Mathf.Lerp(25f,-65f,Mathf.Clamp01(directionalAttackProgress)):-18f;
        transform.localRotation=Quaternion.Euler(0,0,angle*poseAnimator.FacingSign);
        transform.localScale=equippedScale;
        SetSwordSorting(poseAnimator.FacingAway);
    }

    private void OnDisable()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null; isAttacking = false; isOrbiting = false;
        if (bloom != null) bloom.Refresh(false, player, currentAngle);
    }

    private Vector3 GetRestPosition(float facing)
    {
        if(poseAnimator != null && poseAnimator.HasDirectionalArt)
            return new Vector3(-.32f*facing,-.10f,poseAnimator.FacingAway?-.02f:.02f);
        return new Vector3(restLocalPosition.x * facing, restLocalPosition.y, restLocalPosition.z);
    }

    private Quaternion GetRestRotation(float facing)
    {
        if(poseAnimator != null && poseAnimator.HasDirectionalArt)
            return Quaternion.Euler(0,0,28f*facing);
        return Quaternion.Euler(0f, 0f, MirrorAngle(restLocalAngle, facing));
    }

    private float GetFacingSign()
    {
        if(poseAnimator != null && poseAnimator.HasDirectionalArt) return poseAnimator.FacingSign;
        if (playerRenderer == null && player != null)
            playerRenderer = FindPlayerRenderer();
        return playerRenderer != null && playerRenderer.flipX ? -1f : 1f;
    }

    private void ConfigureDirectionalEquipment()
    {
        if(poseAnimator==null || !poseAnimator.HasDirectionalArt || swordRenderer==null) return;
        Texture2D texture=Resources.Load<Texture2D>("Sprites/Player/MacuahuitlEquipment");
        if(texture==null || !texture.isReadable) return;
        if(equipmentSprite==null)
        {
            Color32[] pixels=texture.GetPixels32();
            int minX=texture.width,minY=texture.height,maxX=0,maxY=0;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
                if(pixels[y*texture.width+x].a>32)
                {minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            if(minX>maxX || minY>maxY) return;
            Rect rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
            equipmentSprite=Sprite.Create(texture,rect,new Vector2(.5f,.10f),rect.height/1.32f,0,SpriteMeshType.FullRect);
            equipmentSprite.name="Macuahuitl_Equipo_Completo";
            equipmentMaterial=new Material(Shader.Find("Xunjuu/AtlasSprite")){name="Macuahuitl_Pixel_Alpha"};
            equipmentMaterial.SetFloat("_KeyMode",2);
        }
        swordRenderer.sprite=equipmentSprite;
        swordRenderer.sharedMaterial=equipmentMaterial;
        swordRenderer.color=Color.white;
        equippedScale=Vector3.one;
    }

    private void OnDestroy()
    {
        if(equipmentSprite!=null)Destroy(equipmentSprite);
        if(equipmentMaterial!=null)Destroy(equipmentMaterial);
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
