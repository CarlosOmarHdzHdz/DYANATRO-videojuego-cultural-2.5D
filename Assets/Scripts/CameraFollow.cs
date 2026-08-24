using UnityEngine;

// ============================================
// CÁMARA ORBITAL - SIGUE AL JUGADOR + ROTACIÓN MANUAL Y AUTOMÁTICA
// ============================================
// VERSIÓN: 2.0
// ============================================
// CARACTERÍSTICAS:
//   - Sigue al jugador suavemente
//   - Rotación manual con click derecho
//   - Rotación automática detrás del jugador al caminar hacia atrás
//   - Límites verticales para no ver desde abajo
// ============================================

public class CameraOrbit : MonoBehaviour
{
    [Header("=== SEGUIMIENTO ===")]
    [SerializeField] private Transform target;  // El jugador

    [Header("=== DISTANCIA ===")]
    [SerializeField] private float distance = 10f;
    [SerializeField] private float height = 3f;

    [Header("=== ROTACIÓN MANUAL ===")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float smoothSpeed = 5f;

    [Header("=== ROTACIÓN AUTOMÁTICA (al caminar atrás) ===")]
    [SerializeField] private float autoRotateSpeed = 180f;  // Velocidad para rotar detrás

    [Header("=== LÍMITES ===")]
    [SerializeField] private float minYAngle = -20f;
    [SerializeField] private float maxYAngle = 60f;

    public float CurrentX { get; private set; } = 0f;
    public float CurrentY { get; private set; } = 25f;

    private Vector3 desiredPosition;
    private PlayerController playerController;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                playerController = player.GetComponent<PlayerController>();
            }
        }
        else
        {
            playerController = target.GetComponent<PlayerController>();
        }
    }

    void Update()
    {
        if (target == null) return;

        // ============================================
        // ROTACIÓN MANUAL (click derecho)
        // ============================================
        if (Input.GetMouseButton(1))
        {
            CurrentX += Input.GetAxis("Mouse X") * mouseSensitivity;
            CurrentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            CurrentY = Mathf.Clamp(CurrentY, minYAngle, maxYAngle);
        }

        // ============================================
        // ROTACIÓN AUTOMÁTICA (cuando el jugador camina hacia atrás)
        // ============================================
        if (playerController != null && playerController.IsMovingBackward && !Input.GetMouseButton(1))
        {
            // Obtener la dirección hacia donde mira el jugador (Billboard)
            Vector3 playerForward = target.forward;
            float targetAngle = Mathf.Atan2(playerForward.x, playerForward.z) * Mathf.Rad2Deg;

            // Suavizar rotación hacia atrás
            CurrentX = Mathf.LerpAngle(CurrentX, targetAngle, autoRotateSpeed * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calcular posición de la cámara
        Quaternion rotation = Quaternion.Euler(CurrentY, CurrentX, 0);
        Vector3 offset = new Vector3(0, height, -distance);

        desiredPosition = target.position + rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Mirar al jugador
        transform.LookAt(target.position + Vector3.up * height);
    }

    // Método para cambiar el objetivo manualmente
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
            playerController = target.GetComponent<PlayerController>();
    }

    // Método para resetear la cámara detrás del jugador
    public void ResetCamera()
    {
        if (target == null) return;

        Vector3 playerForward = target.forward;
        CurrentX = Mathf.Atan2(playerForward.x, playerForward.z) * Mathf.Rad2Deg;
        CurrentY = 25f;
    }
}