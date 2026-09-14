using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Xunju2DDepthSorter : MonoBehaviour
{
    [SerializeField] private int baseOrder;
    [SerializeField] private int scale = 100;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sortingOrder = baseOrder + Mathf.RoundToInt(-transform.position.y * scale);
    }

    public void Configure(int newBaseOrder, int newScale)
    {
        baseOrder = newBaseOrder;
        scale = newScale;
    }
}
