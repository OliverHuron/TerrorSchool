using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Sensibilidad")]
    public float mouseSensitivity = 0.15f;

    [Header("Límite vertical (grados)")]
    public float clampAngle = 80f;

    [Header("Arrastra el Capsule aquí")]
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue();

        float mouseX = delta.x * mouseSensitivity;
        float mouseY = delta.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -clampAngle, clampAngle);

        // Solo la cámara rota verticalmente
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // El Capsule rota horizontalmente
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }
}