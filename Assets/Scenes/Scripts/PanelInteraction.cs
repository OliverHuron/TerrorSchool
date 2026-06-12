using UnityEngine;
using UnityEngine.InputSystem;

public class PanelInteraction : MonoBehaviour
{
    public MouseLook mouseLook;
    public float rangoActivacion = 2.5f;
    public LayerMask capaTeclas;
    public BisagraDoor puerta;

    private bool modoPanel = false;
    private Transform jugador;

    void Start()
    {
        jugador = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        // Si la puerta ya está abierta, salir del modo panel y no volver a entrar
        if (puerta != null && puerta.EstaAbierta())
        {
            if (modoPanel) ForzarSalir();
            return;
        }
        if (puerta != null)
            Debug.Log("Puerta abierta: " + puerta.EstaAbierta());

        float dist = Vector3.Distance(jugador.position, transform.position);

        if (!modoPanel && dist < rangoActivacion)
            EntrarPanel();
        else if (modoPanel && dist > rangoActivacion + 0.5f)
            SalirPanel();

        if (modoPanel && Keyboard.current.escapeKey.wasPressedThisFrame)
            SalirPanel();

        if (modoPanel && Mouse.current.leftButton.wasPressedThisFrame)
            HacerClic();
    }

    void HacerClic()
    {
        Ray rayo = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(rayo, out hit, 10f, capaTeclas))
        {
            Tecla tecla = hit.collider.GetComponentInParent<Tecla>();
            if (tecla != null) { tecla.Presionar(); return; }

            BotonPanel boton = hit.collider.GetComponentInParent<BotonPanel>();
            if (boton != null) { boton.Presionar(); return; }
        }
    }

    void EntrarPanel()
    {
        modoPanel = true;
        mouseLook.EntrarModoPanel();
    }

    void SalirPanel()
    {
        modoPanel = false;
        mouseLook.SalirModoPanel();
    }

    public void ForzarSalir()
    {
        modoPanel = false;
        mouseLook.SalirModoPanel();
        enabled = false;
    }
}