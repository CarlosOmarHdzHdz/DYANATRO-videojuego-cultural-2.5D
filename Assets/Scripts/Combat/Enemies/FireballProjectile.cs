using UnityEngine;

// ============================================
// BOLA DE FUEGO - PROYECTIL DEL ENEMIGO
// ============================================
// AUTOR: Sistema de juego 2.5D
// FECHA: 2026
// ============================================
// DESCRIPCIÓN:
//   Este script controla la bola de fuego que lanza el enemigo.
//   La bola viaja hacia el jugador, causa daño al impactar
//   y se destruye automáticamente después de un tiempo.
// ============================================
// REQUISITOS:
//   - El objeto debe tener un Collider (Is Trigger = true)
//   - El jugador debe tener el Tag "Player"
//   - El enemigo debe llamar al método Initialize()
// ============================================

public class FireballProjectile : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: VARIABLES PRIVADAS
    // ============================================
    // Estos valores se asignan desde el enemigo cuando se crea la bola
    // ============================================

    private int damage;          // Cantidad de daño que causa al jugador
    private Vector3 direction;   // Dirección hacia donde viaja la bola
    private float speed;         // Velocidad de movimiento (unidades por segundo)

    // ============================================
    // SECCIÓN 2: MÉTODO DE INICIALIZACIÓN
    // ============================================
    // Este método es llamado por el enemigo (EnemyFireBreath)
    // cuando instancia la bola de fuego.
    // ============================================

    /// <summary>
    /// Configura la bola de fuego con dirección, velocidad y daño
    /// </summary>
    /// <param name="dir">Dirección normalizada hacia el jugador</param>
    /// <param name="spd">Velocidad de la bola (ej: 8)</param>
    /// <param name="dmg">Cantidad de daño al jugador (ej: 15)</param>
    public void Initialize(Vector3 dir, float spd, int dmg)
    {
        // Guardar valores
        direction = dir.normalized;  // Normalizar para velocidad constante
        speed = spd;
        damage = dmg;

        // Rotar el sprite para que mire hacia la dirección de viaje
        // Esto hace que la bola "apunte" hacia donde va
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Destruir la bola automáticamente después de 3 segundos
        // Esto evita que se acumulen bolas perdidas en la escena
        Destroy(gameObject, 3f);

        // Mensaje de depuración (se ve en la consola)
        Debug.Log($"🔥 Bola de fuego creada. Daño: {damage}, Velocidad: {speed}");
    }

    // ============================================
    // SECCIÓN 3: MOVIMIENTO
    // ============================================
    // Se ejecuta cada frame y mueve la bola en línea recta
    // ============================================

    void Update()
    {
        // Movimiento en línea recta según dirección y velocidad
        // Time.deltaTime hace que el movimiento sea independiente de los FPS
        transform.position += direction * speed * Time.deltaTime;
    }

    // ============================================
    // SECCIÓN 4: DETECCIÓN DE COLISIÓN
    // ============================================
    // Se ejecuta cuando la bola entra en contacto con otro collider
    // El collider debe tener "Is Trigger = true"
    // ============================================

    void OnTriggerEnter(Collider other)
    {
        // ============================================
        // CASO 1: IMPACTO CONTRA EL JUGADOR
        // ============================================
        if (other.CompareTag("Player"))
        {
            // Buscar el script PlayerController en el jugador
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                // Calcular la nueva vida después del daño
                int currentHealth = player.GetCurrentHealth();
                int newHealth = currentHealth - damage;

                // Aplicar el daño al jugador
                player.SetHealth(newHealth);

                // Mensaje de depuración
                Debug.Log($"💥 ¡BOLA DE FUEGO IMPACTA! {damage} de daño. " +
                          $"Vida: {currentHealth} → {newHealth}");
            }
            else
            {
                // Error: el jugador no tiene el script PlayerController
                Debug.LogError("❌ No se encontró PlayerController en el jugador");
            }

            // Destruir la bola al impactar (desaparece)
            Destroy(gameObject);
        }

        // ============================================
        // CASO 2: IMPACTO CONTRA OTROS OBJETOS
        // ============================================
        // Si no es el jugador y no es el enemigo, también se destruye
        // Esto incluye: suelo, paredes, objetos, etc.
        else if (other.GetComponentInParent<EnemyFireBreath>() == null
            && other.GetComponentInParent<FireballProjectile>() == null
            && !other.CompareTag("Enemy"))
        {
            Debug.Log($"🔥 Bola de fuego impactó contra: {other.gameObject.name}");
            Destroy(gameObject);
        }

        // NOTA: Si la bola toca a otro enemigo, NO se destruye
        // Esto evita que las bolas se anulen entre sí
    }
}
