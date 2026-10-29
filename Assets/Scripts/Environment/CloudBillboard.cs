using UnityEngine;

public class CloudBillboard : MonoBehaviour
{
    [SerializeField] private bool upright;
    public void SetUpright() => upright=true;
    void LateUpdate()
    {
        if (XunjuuCameraFrame.HasCamera)
        {
            if(upright)transform.rotation=XunjuuCameraFrame.Upright;
            else transform.LookAt(transform.position + XunjuuCameraFrame.Forward);
        }
    }
}
