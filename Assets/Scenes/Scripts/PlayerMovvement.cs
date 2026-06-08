using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Transform cameraTransform;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Busca la Main Camera que es hija del Player
        cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    void Update()
    {
        // Dirección relativa a donde apunta la cámara (ignorando el eje Y)
        float h = Input.GetAxis("Horizontal"); // A/D o flechas
        float v = Input.GetAxis("Vertical");   // W/S o flechas

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        // Aplana los vectores para que no vueles ni te hundas al mirar arriba/abajo
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * v + camRight * h);
        controller.Move(moveDir * speed * Time.deltaTime);

        // Gravedad simple
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}