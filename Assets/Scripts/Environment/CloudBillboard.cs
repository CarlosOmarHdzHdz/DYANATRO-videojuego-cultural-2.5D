using UnityEngine;

public class CloudBillboard : MonoBehaviour
{
    [SerializeField] private bool upright;
    public void SetUpright() => upright=true;
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            if(upright)transform.rotation=Quaternion.Euler(0,Camera.main.transform.eulerAngles.y,0);
            else transform.LookAt(transform.position + Camera.main.transform.forward);
        }
    }
}
