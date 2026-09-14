using UnityEngine;

public class Xunju2DCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.16f;

    private Vector3 velocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, -10f);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
