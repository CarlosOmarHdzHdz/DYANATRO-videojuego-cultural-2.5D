using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Xunju2DPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4.4f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.75f;
    [SerializeField] private float swordAttackRadius = 1.25f;
    [SerializeField] private int handDamage = 16;
    [SerializeField] private int swordDamage = 34;
    [SerializeField] private LayerMask attackLayers;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Transform equippedSword;
    private Vector2 moveInput;
    private int currentHealth;
    private bool canMove = true;
    private float attackCooldown;
    private bool hasSword;

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public bool HasSword { get { return hasSword; } }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (!canMove)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (spriteRenderer != null && Mathf.Abs(moveInput.x) > 0.01f)
            spriteRenderer.flipX = moveInput.x < 0f;

        attackCooldown -= Time.deltaTime;
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && attackCooldown <= 0f)
            Attack();
    }

    void FixedUpdate()
    {
        body.linearVelocity = canMove ? moveInput * moveSpeed : Vector2.zero;
    }

    void Attack()
    {
        attackCooldown = 0.35f;
        Vector2 center = attackPoint != null ? attackPoint.position : transform.position;
        float radius = hasSword ? swordAttackRadius : attackRadius;
        int damage = hasSword ? swordDamage : handDamage;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, attackLayers);
        foreach (Collider2D hit in hits)
        {
            Xunju2DEnemy enemy = hit.GetComponentInParent<Xunju2DEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                continue;
            }

            Xunju2DTree tree = hit.GetComponentInParent<Xunju2DTree>();
            if (tree != null)
                tree.TakeDamage(1);
        }
    }

    public void EquipSword(Transform sword)
    {
        hasSword = true;
        equippedSword = sword;
        if (equippedSword == null)
            return;

        equippedSword.SetParent(transform, false);
        equippedSword.localPosition = new Vector3(0.55f, 0.18f, 0f);
        equippedSword.localRotation = Quaternion.Euler(0f, 0f, -36f);
        equippedSword.localScale = Vector3.one * 0.46f;
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
    }

    public void HealFull()
    {
        currentHealth = maxHealth;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!value && body != null)
            body.linearVelocity = Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, hasSword ? swordAttackRadius : attackRadius);
    }
}
