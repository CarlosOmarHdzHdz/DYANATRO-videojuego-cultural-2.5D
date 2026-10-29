using UnityEngine;

// Reacción exclusivamente visual. No empuja el Rigidbody ni gira el collider.
[DisallowMultipleComponent]
[DefaultExecutionOrder(230)]
public sealed class XunjuuPlayerHitFeedback : MonoBehaviour
{
    static readonly int FlashId = Shader.PropertyToID("_HitAmount");
    static readonly int OffsetId = Shader.PropertyToID("_HitOffset");
    static readonly int SquashId = Shader.PropertyToID("_HitSquash");
    SpriteRenderer sprite;
    PlayerController player;
    MaterialPropertyBlock properties;
    float elapsed = .34f, strength;
    bool applied;
    const float Duration = .34f;
    public bool IsReacting => elapsed < Duration;
    public int HitCount { get; private set; }
    public float FlashAmount { get; private set; }
    public Vector2 VisualOffset { get; private set; }
    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>(); player = GetComponent<PlayerController>();
        properties = new MaterialPropertyBlock();
    }
    public void PlayHit(int damage, int maximum)
    {
        if (damage <= 0 || sprite == null) return;
        strength = Mathf.Lerp(.65f, 1, Mathf.Clamp01(damage / (float)Mathf.Max(1, maximum) * 3));
        elapsed = 0; HitCount++; Apply();
    }
    void LateUpdate()
    {
        if (player != null && player.IsDead()) { Clear(); elapsed = Duration; return; }
        if (!IsReacting) { if (applied) Clear(); return; }
        // La pausa congela también el gesto; el movimiento reducido evita el vaivén.
        elapsed = Mathf.Min(Duration, elapsed + (Time.timeScale > 0 ? Time.deltaTime : 0)); Apply();
    }
    void Apply()
    {
        if (sprite == null) return;
        float t = Mathf.Clamp01(elapsed / Duration), envelope = (1 - t) * (1 - t);
        FlashAmount = strength * envelope * (t < .22f ? 1 : .55f);
        float motion = XunjuuPresentationPreferences.ReducedMotion ? 0 : strength * envelope;
        VisualOffset = new Vector2(Mathf.Sin(t * Mathf.PI * 3) * .035f * motion, .014f * Mathf.Sin(t * Mathf.PI) * motion);
        sprite.GetPropertyBlock(properties);
        properties.SetFloat(FlashId, FlashAmount); properties.SetVector(OffsetId, new Vector4(VisualOffset.x, VisualOffset.y, 0, 0));
        properties.SetFloat(SquashId, Mathf.Sin(t * Mathf.PI) * .045f * motion);
        sprite.SetPropertyBlock(properties); applied = true;
    }
    void Clear()
    {
        if (sprite == null || properties == null) return;
        sprite.GetPropertyBlock(properties); properties.SetFloat(FlashId, 0); properties.SetVector(OffsetId, Vector4.zero); properties.SetFloat(SquashId, 0);
        sprite.SetPropertyBlock(properties); FlashAmount = 0; VisualOffset = Vector2.zero; applied = false;
    }
    void OnDisable() { Clear(); elapsed = Duration; }
}
