using UnityEngine;

public class Xunju2DCloudDrift : MonoBehaviour
{
    [SerializeField] private float speed = 0.12f;
    [SerializeField] private float wrapX = 24f;

    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
        if (transform.position.x > wrapX)
            transform.position = new Vector3(-wrapX, transform.position.y, transform.position.z);
    }
}
