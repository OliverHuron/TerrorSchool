using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Sensibilidad")]
    public float mouseSensitivity = 1000f;

    [Header("Límite vertical (grados)")]
    public float clampAngle = 80f;

    private float xRotation = 0f;
    private Transform playerBody; // referencia al Player padre

    void Start()
    {
        // El padre de la cámara es el Player
        playerBody = transform.parent;

        // Oculta y bloquea el cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotación vertical (cámara sola, con clamp para no dar la vuelta)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -clampAngle, clampAngle);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotación horizontal (rota TODO el cuerpo del jugador)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}