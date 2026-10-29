using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(140)]
public class DyanatroSpriteDepthSorter : MonoBehaviour
{
    [SerializeField] private int baseOrder;
    [SerializeField] private float orderScale = 10f;

    private SpriteRenderer spriteRenderer;
    private int category;
    private Vector3 lastPosition,lastForward;
    private int lastOrder;
    private bool sorted;

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

        Vector3 forward=XunjuuCameraFrame.GroundForward;
        Vector3 position=transform.position;
        if(sorted&&position==lastPosition&&forward==lastForward&&spriteRenderer.sortingOrder==lastOrder)return;
        int depth=Mathf.Clamp(Mathf.RoundToInt(-Vector3.Dot(transform.position,forward)*orderScale),-4000,4000);
        int order = category!=0?category+depth:baseOrder+depth;
        if (spriteRenderer.sortingOrder != order) spriteRenderer.sortingOrder = order;
        lastPosition=position;lastForward=forward;lastOrder=order;sorted=true;
    }

    public void Configure(int newBaseOrder, float newOrderScale)
    {
        baseOrder = newBaseOrder;
        orderScale = newOrderScale;
        sorted=false;
        category=XunjuuSceneVisualPolicy.IsActor(transform)?12000:XunjuuSceneVisualPolicy.IsSoftDecoration(transform)?-12000:0;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }
}
