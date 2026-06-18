using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;
    public float sprintMultiplier = 1.35f;
    public float multiplicadorVelocidadAgachado = 0.55f;
    public float gravity = -9.81f;

    [Header("Agacharse — metros reales en el mundo")]
    [Tooltip("Altura total del jugador agachado en METROS (no en el Inspector del CC).")]
    public float alturaAgachadoMundo = 0.7f;
    [Tooltip("Radio del capsule agachado en METROS.")]
    public float radioAgachadoMundo = 0.28f;
    [Tooltip("Altura de los ojos desde el suelo al agacharse, en METROS.")]
    public float alturaOjosAgachadoMundo = 0.5f;
    [Tooltip("Ajuste fino extra de la camara en metros (negativo = mas bajo).")]
    public float ajusteCamaraAgachadoMundo = 0f;

    static bool colisionEnemigosConfigurada;

    CharacterController controller;
    Transform cameraTransform;
    MeshRenderer meshJugador;
    Vector3 velocity;

    float alturaDePie;
    float radioDePie;
    Vector3 centroDePie;
    float offsetBaseLocal;
    float alturaCamaraDePie;
    float stepOffsetDePie;
    float escalaY;
    float ultimaAlturaSegura;

    int esconditesActivos;
    bool agachado;

    public bool Agachado => agachado;
    public bool EstaEscondido => esconditesActivos > 0 && agachado;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>().transform;
        meshJugador = GetComponent<MeshRenderer>();

        escalaY = Mathf.Max(0.01f, transform.lossyScale.y);
        alturaDePie = controller.height;
        radioDePie = controller.radius;
        centroDePie = controller.center;
        offsetBaseLocal = centroDePie.y - alturaDePie * 0.5f;
        stepOffsetDePie = controller.stepOffset;
        alturaCamaraDePie = cameraTransform.localPosition.y;

        EliminarColliderDuplicado();
        ConfigurarColisionConEnemigos();
    }

    void Start()
    {
        EliminarColliderDuplicado();
        ultimaAlturaSegura = transform.position.y;
    }

    void EliminarColliderDuplicado()
    {
        foreach (CapsuleCollider col in GetComponents<CapsuleCollider>())
        {
            if (Application.isPlaying)
                Destroy(col);
            else
                DestroyImmediate(col);
        }
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
        if (GameState.UIAbierta)
            return;

        escalaY = Mathf.Max(0.01f, transform.lossyScale.y);

        if (TeclaAgacharsePresionada())
            AlternarAgacharse();

        float h = LeerEjeHorizontal();
        float v = LeerEjeVertical();
        bool sprint = !agachado && controller.isGrounded && SprintPresionado();

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        float velocidadBase = speed;
        if (agachado)
            velocidadBase *= multiplicadorVelocidadAgachado;
        else if (sprint)
            velocidadBase *= sprintMultiplier;

        Vector3 moveDir = camForward * v + camRight * h;
        controller.Move(moveDir * velocidadBase * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (controller.isGrounded)
            ultimaAlturaSegura = transform.position.y;
        else if (transform.position.y < ultimaAlturaSegura - 8f)
            RecuperarSiCayoDelMapa();
    }

    void AlternarAgacharse()
    {
        AplicarAgacharse(!agachado);
    }

    void AplicarAgacharse(bool activar)
    {
        escalaY = Mathf.Max(0.01f, transform.lossyScale.y);

        float radio = activar ? radioAgachadoMundo / escalaY : radioDePie;
        float altura = activar ? alturaAgachadoMundo / escalaY : alturaDePie;
        altura = Mathf.Max(altura, radio * 2f + 0.01f);

        controller.radius = radio;
        controller.height = altura;
        controller.center = new Vector3(
            centroDePie.x,
            offsetBaseLocal + altura * 0.5f,
            centroDePie.z);
        controller.stepOffset = Mathf.Min(stepOffsetDePie, altura * 0.4f);

        Vector3 camPos = cameraTransform.localPosition;
        camPos.y = activar ? CalcularCamaraYAgachado(altura) : alturaCamaraDePie;
        cameraTransform.localPosition = camPos;

        if (meshJugador != null)
            meshJugador.enabled = !activar;

        agachado = activar;
    }

    float CalcularCamaraYAgachado(float alturaCapsulaLocal)
    {
        float ojosDesdePiesLocal = (alturaOjosAgachadoMundo + ajusteCamaraAgachadoMundo) / escalaY;
        ojosDesdePiesLocal = Mathf.Clamp(ojosDesdePiesLocal, 0.05f, alturaCapsulaLocal - 0.02f);
        return offsetBaseLocal + ojosDesdePiesLocal;
    }

    static float LeerEjeHorizontal()
    {
        if (Keyboard.current != null)
        {
            float valor = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) valor -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) valor += 1f;
            if (valor != 0f)
                return valor;
        }

        return Input.GetAxisRaw("Horizontal");
    }

    static float LeerEjeVertical()
    {
        if (Keyboard.current != null)
        {
            float valor = 0f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) valor -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) valor += 1f;
            if (valor != 0f)
                return valor;
        }

        return Input.GetAxisRaw("Vertical");
    }

    static bool SprintPresionado()
    {
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            return true;

        return Input.GetKey(KeyCode.LeftShift);
    }

    static bool TeclaAgacharsePresionada()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(KeyCode.C);
    }

    public void EntrarEscondite()
    {
        esconditesActivos++;
    }

    public void SalirEscondite()
    {
        esconditesActivos = Mathf.Max(0, esconditesActivos - 1);
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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc == null)
            return;

        float sy = Mathf.Max(0.01f, transform.lossyScale.y);
        float sxz = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float baseLocal = cc.center.y - cc.height * 0.5f;
        float piesY = transform.position.y + baseLocal * sy;

        Gizmos.color = agachado ? Color.green : Color.cyan;
        float altoMundo = agachado ? alturaAgachadoMundo : cc.height * sy;
        float radioMundo = agachado ? radioAgachadoMundo : cc.radius * sxz;
        Gizmos.DrawWireSphere(
            new Vector3(transform.position.x, piesY + altoMundo * 0.5f, transform.position.z),
            radioMundo);
    }
#endif
}
