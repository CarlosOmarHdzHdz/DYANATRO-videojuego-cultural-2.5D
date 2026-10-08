using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(140)]
public class DyanatroSpriteDepthSorter : MonoBehaviour
{
    [SerializeField] private int baseOrder;
    [SerializeField] private float orderScale = 10f;

    private SpriteRenderer spriteRenderer;
    private int category;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        category=XunjuuSceneVisualPolicy.IsActor(transform)?12000:XunjuuSceneVisualPolicy.IsSoftDecoration(transform)?-12000:0;
    }

    void LateUpdate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        Vector3 forward=Camera.main!=null?Camera.main.transform.forward:Vector3.forward;forward.y=0;forward.Normalize();
        int depth=Mathf.Clamp(Mathf.RoundToInt(-Vector3.Dot(transform.position,forward)*orderScale),-4000,4000);
        spriteRenderer.sortingOrder = category!=0?category+depth:baseOrder+depth;
    }

    public void Configure(int newBaseOrder, float newOrderScale)
    {
        baseOrder = newBaseOrder;
        orderScale = newOrderScale;
        category=XunjuuSceneVisualPolicy.IsActor(transform)?12000:XunjuuSceneVisualPolicy.IsSoftDecoration(transform)?-12000:0;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }
}
