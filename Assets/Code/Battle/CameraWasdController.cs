using UnityEngine;
using UnityEngine.InputSystem;

public class CameraWasdController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 18f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Zoom / Height")]
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float minHeight = 8f;
    [SerializeField] private float maxHeight = 35f;

    [Header("Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 xBounds = new Vector2(-60f, 60f);
    [SerializeField] private Vector2 zBounds = new Vector2(-60f, 60f);

    [Header("Pitch")]
    [SerializeField] private float pitchSpeed = 60f;
    [SerializeField] private float minPitch = 25f;
    [SerializeField] private float maxPitch = 75f;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        HandleMove();
        HandlePitch();
        HandleRotation();
        HandleZoom();
        ClampPosition();
    }

    private void HandleMove()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;

        if (input.sqrMagnitude <= 0.001f)
            return;

        input.Normalize();

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        Vector3 moveDirection = forward * input.y + right * input.x;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        float rotationInput = 0f;

        if (Keyboard.current.qKey.isPressed) rotationInput -= 1f;
        if (Keyboard.current.eKey.isPressed) rotationInput += 1f;

        if (Mathf.Abs(rotationInput) <= 0.001f)
            return;

        transform.Rotate(Vector3.up, rotationInput * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) <= 0.001f)
            return;

        Vector3 position = transform.position;

        // scroll > 0 обычно означает "приблизить", значит уменьшаем Y.
        position.y -= scroll * zoomSpeed * Time.deltaTime;
        position.y = Mathf.Clamp(position.y, minHeight, maxHeight);

        transform.position = position;
    }
    private void HandlePitch()
    {
        float pitchInput = 0f;

        if (Keyboard.current.rKey.isPressed) pitchInput -= 1f;
        if (Keyboard.current.fKey.isPressed) pitchInput += 1f;

        if (Mathf.Abs(pitchInput) <= 0.001f)
            return;

        Vector3 euler = transform.eulerAngles;

        float currentPitch = euler.x;

        if (currentPitch > 180f)
            currentPitch -= 360f;

        currentPitch += pitchInput * pitchSpeed * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        transform.eulerAngles = new Vector3(currentPitch, euler.y, euler.z);
    }

    private void ClampPosition()
    {
        if (!useBounds)
            return;

        Vector3 position = transform.position;

        position.x = Mathf.Clamp(position.x, xBounds.x, xBounds.y);
        position.z = Mathf.Clamp(position.z, zBounds.x, zBounds.y);

        transform.position = position;
    }
}