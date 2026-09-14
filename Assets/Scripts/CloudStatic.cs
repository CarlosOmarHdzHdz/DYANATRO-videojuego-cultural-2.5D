using UnityEngine;

public class CloudStatic : MonoBehaviour
{
    void Start()
    {
        // Asegurar visibilidad
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            Debug.LogWarning("⚠️ Nube sin sprite");
        }

        gameObject.tag = "Cloud";
    }
}