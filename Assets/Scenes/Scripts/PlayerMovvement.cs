using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;
    public float gravity = -9.81f;

    static bool colisionEnemigosConfigurada;

    CharacterController controller;
    Transform cameraTransform;
    Vector3 velocity;
    float ultimaAlturaSegura;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>().transform;

        EliminarColliderDuplicado();
        ValidarCharacterController();
        ConfigurarColisionConEnemigos();
    }

    void Start()
    {
        ultimaAlturaSegura = transform.position.y;
    }

    void EliminarColliderDuplicado()
    {
        CapsuleCollider colliderExtra = GetComponent<CapsuleCollider>();
        if (colliderExtra == null)
            return;

        colliderExtra.enabled = false;
        Destroy(colliderExtra);
    }

    void ValidarCharacterController()
    {
        Vector3 escala = transform.localScale;
        if (!Mathf.Approximately(escala.x, escala.y) || !Mathf.Approximately(escala.y, escala.z))
        {
            Debug.LogWarning(
                "PlayerMovement: la escala del jugador no es uniforme. En el Inspector pon Scale en (1,1,1) y escala solo el mesh en un hijo.",
                this);
        }

        controller.stepOffset = Mathf.Min(controller.stepOffset, 0.45f);
    }

    void ConfigurarColisionConEnemigos()
    {
        if (colisionEnemigosConfigurada)
            return;

        int capaEnemigo = LayerMask.NameToLayer("Enemy");
        if (capaEnemigo < 0)
            return;

        Physics.IgnoreLayerCollision(gameObject.layer, capaEnemigo, true);
        colisionEnemigosConfigurada = true;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * v + camRight * h;
        controller.Move(moveDir * speed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (controller.isGrounded)
            ultimaAlturaSegura = transform.position.y;
        else if (transform.position.y < ultimaAlturaSegura - 8f)
            RecuperarSiCayoDelMapa();
    }

    void RecuperarSiCayoDelMapa()
    {
        Vector3 pos = transform.position;
        pos.y = ultimaAlturaSegura + 0.1f;
        controller.enabled = false;
        transform.position = pos;
        controller.enabled = true;
        velocity.y = -2f;
    }
}
