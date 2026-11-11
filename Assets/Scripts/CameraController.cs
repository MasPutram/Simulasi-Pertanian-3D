using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 10f, -7f);
    public float smoothSpeed = 5f;
    public float scrollSpeed = 2f;
    public float minDistance = 4f;
    public float maxDistance = 18f;
    float currentZoom = 1f;

    void LateUpdate()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            else return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y / 120f;
            currentZoom -= scroll * 0.1f;
            currentZoom = Mathf.Clamp(currentZoom, 0.4f, 1.5f);
        }

        Vector3 desiredPos = target.position + offset * currentZoom;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}
