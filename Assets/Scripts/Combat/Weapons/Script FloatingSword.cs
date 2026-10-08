using UnityEngine;

// ============================================
// ESPADA FLOTANTE - RECOLECTABLE
// ============================================
// FUNCIONES:
//   - Flota en el mapa con animación
//   - Atrae al jugador cuando se acerca
//   - Al recolectar, se equipa detrás del jugador
//   - Crea el punto de anclaje (WeaponPivot) automáticamente
// ============================================

public class FloatingSword : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN DE ATRACCIÓN ===")]
    [SerializeField] private float attractRadius = 3f;      // Radio para detectar al jugador
    [SerializeField] private float attractSpeed = 5f;       // Velocidad al volar hacia el jugador
    [SerializeField] private LayerMask playerLayer;         // Capa del jugador

    [Header("=== ANIMACIÓN FLOTANTE ===")]
    [SerializeField] private Animator swordAnimator;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float rotationSpeed = 60f;

    [Header("=== EFECTOS ===")]
    [SerializeField] private ParticleSystem idleParticles;
    [SerializeField] private AudioClip pickupSound;

    private Transform player;
    private Vector3 startPosition;
    private bool isCollected = false;
    private bool isAttracted = false;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (swordAnimator == null)
            swordAnimator = GetComponent<Animator>();

        if (idleParticles != null)
            idleParticles.Play();
    }

    void Update()
    {
        if (isCollected) return;

        // ============================================
        // ANIMACIÓN FLOTANTE
        // ============================================
        if (!isAttracted)
        {
            // Movimiento flotante (sube y baja)
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Rotación suave
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // Brillo pulsante
            if (spriteRenderer != null)
            {
                float alpha = 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f;
                spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        // ============================================
        // DETECCIÓN DEL JUGADOR
        // ============================================
        if (player == null)
        {
            Collider[] players = Physics.OverlapSphere(transform.position, attractRadius, playerLayer);
            if (players.Length > 0)
            {
                player = players[0].transform;
                isAttracted = true;
                Debug.Log("⚔️ Jugador detectado - Espada volando hacia él");
            }
        }

        // ============================================
        // MOVIMIENTO HACIA EL JUGADOR
        // ============================================
        if (isAttracted && player != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                attractSpeed * Time.deltaTime
            );

            // Cuando está muy cerca, recolectar
            if (Vector3.Distance(transform.position, player.position) < 0.5f)
            {
                CollectSword();
            }
        }
    }

    // ============================================
    // RECOLECCIÓN DE LA ESPADA
    // ============================================
    void CollectSword()
    {
        if (isCollected) return;
        isCollected = true;

        Debug.Log("⚔️ Espada recolectada!");

        // Efectos...
        if (idleParticles != null)
        {
            idleParticles.transform.parent = null;
            idleParticles.Stop();
            Destroy(idleParticles.gameObject, 1f);
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // ============================================
        // REPARENTAR LA ESPADA AL JUGADOR
        // Posición LOCAL: Z negativa = detrás del sprite
        // ============================================
        transform.SetParent(player);
        transform.localPosition = new Vector3(0, 0.17f, -0.5f);  // Detrás del sprite
        transform.localRotation = Quaternion.identity;

        // Agregar script de espada orbital
        OrbitalWeapon orbital = gameObject.GetComponent<OrbitalWeapon>();
        if (orbital == null)
            orbital = gameObject.AddComponent<OrbitalWeapon>();

        orbital.Initialize(player);

        // Asignar al PlayerController
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetOrbitalWeapon(orbital);
            Debug.Log("✅ Espada asignada al PlayerController");
        }

        // Desactivar animación flotante
        if (swordAnimator != null)
        {
            swordAnimator.enabled = false;
        }

        enabled = false;

        Debug.Log("⚔️ Espada equipada detrás del sprite! Presiona F para atacar");
    }

    // ============================================
    // VISUALIZACIÓN EN EDITOR
    // ============================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attractRadius);
    }
}