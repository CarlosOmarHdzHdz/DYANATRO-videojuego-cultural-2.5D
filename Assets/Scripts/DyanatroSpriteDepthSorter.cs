using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DyanatroSpriteDepthSorter : MonoBehaviour
{
    [SerializeField] private int baseOrder;
    [SerializeField] private float orderScale = 10f;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        spriteRenderer.sortingOrder = baseOrder + Mathf.RoundToInt(-transform.position.z * orderScale);
    }

    public void Configure(int newBaseOrder, float newOrderScale)
    {
        baseOrder = newBaseOrder;
        orderScale = newOrderScale;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }
}
