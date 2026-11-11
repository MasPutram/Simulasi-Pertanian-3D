using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null)
            transform.forward = cam.transform.forward;
    }
}
