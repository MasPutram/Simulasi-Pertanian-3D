using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 720f;
    public float boundX = 14f;
    public float boundZ = 14f;

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        Vector2 input = Vector2.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;

        if (input.sqrMagnitude < 0.01f) return;

        Vector3 moveDir = new Vector3(input.x, 0, input.y).normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -boundX, boundX);
        pos.y = 1.1f;
        pos.z = Mathf.Clamp(pos.z, -boundZ, boundZ);
        transform.position = pos;
    }
}
