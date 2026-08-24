using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Xunju2DEnemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float moveSpeed = 2.1f;
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float attackDistance = 1.2f;
    [SerializeField] private int damage = 8;
    [SerializeField] private float attackCooldown = 1f;

    private Rigidbody2D body;
    private Transform target;
    private int health;
    private float attackTimer;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        health = maxHealth;
    }

    void Start()
    {
        Xunju2DPlayerController player = FindFirstObjectByType<Xunju2DPlayerController>();
        if (player != null)
            target = player.transform;
    }

    void FixedUpdate()
    {
        if (target == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 delta = target.position - transform.position;
        float distance = delta.magnitude;
        if (distance <= detectionRadius && distance > attackDistance)
            body.linearVelocity = delta.normalized * moveSpeed;
        else
            body.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        if (target == null)
            return;

        attackTimer -= Time.deltaTime;
        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= attackDistance && attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            Xunju2DPlayerController player = target.GetComponent<Xunju2DPlayerController>();
            if (player != null)
                player.TakeDamage(damage);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        if (health <= 0)
            Destroy(gameObject);
    }
}
