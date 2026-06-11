using UnityEngine;
using System.Collections;

public class GhostController : MonoBehaviour
{
    [Header("Aparición")]
    public float distanciaAparicion = 3f;    // Qué tan lejos aparece frente al jugador
    public float tiempoVisible = 1.5f;        // Cuántos segundos es visible
    public float tiempoEntreApariciones = 12f; // Cada cuánto aparece

    [Header("Posición")]
    public Transform holdPoint;

    [Header("Luz")]
    public float anguloDeteccionLuz = 45f;
    public float tiempoParaDesaparecer = 1.5f;

    private Transform jugador;
    private FlashlightController linterna;
    private bool visible = false;
    private float tiempoIluminado = 0f;

    void Start()
    {
        Debug.Log("GhostController iniciado");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugador = playerObj.transform;
            Debug.Log("Jugador encontrado");
        }
        else
        {
            Debug.LogError("No se encontró el player");
            enabled = false;
            return;
        }

        linterna = FindObjectOfType<FlashlightController>();
        Debug.Log("Linterna: " + (linterna != null ? "encontrada" : "NO encontrada"));

        SetVisible(false);
        StartCoroutine(CicloApariciones());
        Debug.Log("Ciclo de apariciones iniciado");

        // Aparecer inmediatamente al iniciar para probar
        StartCoroutine(Aparecer());
    }

    void Update()
    {
        if (!visible || jugador == null || linterna == null) return;

        // Siempre mirar al jugador mientras es visible
        transform.LookAt(new Vector3(
            jugador.position.x,
            transform.position.y,
            jugador.position.z));

        // Detectar si la linterna la apunta
        if (EstaIluminada())
        {
            tiempoIluminado += Time.deltaTime;
            if (tiempoIluminado >= tiempoParaDesaparecer)
                StartCoroutine(Desaparecer());
        }
        else
        {
            tiempoIluminado = 0f;
        }
    }

    IEnumerator CicloApariciones()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoEntreApariciones);
            if (!visible)
                yield return StartCoroutine(Aparecer());
        }
    }

    IEnumerator Aparecer()
    {
        Debug.Log("Aparecer() llamado");

        Vector3 direccion = Camera.main.transform.forward;
        direccion.y = 0; // Ignorar si miras arriba o abajo
        direccion.Normalize();

        Vector3 posicion = jugador.position + direccion * distanciaAparicion;
        posicion.y = jugador.position.y; // Siempre a la altura del jugador
        transform.position = posicion;

        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        transform.LookAt(new Vector3(
            jugador.position.x,
            transform.position.y,
            jugador.position.z));

        SetVisible(true);
        tiempoIluminado = 0f;

        yield return new WaitForSeconds(tiempoVisible);

        if (visible)
            StartCoroutine(Desaparecer());
    }

    IEnumerator Desaparecer()
    {
        SetVisible(false);
        tiempoIluminado = 0f;
        yield return null;
    }

    void SetVisible(bool estado)
    {
        visible = estado;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = estado;
    }

    bool EstaIluminada()
    {
        if (!linterna.IsLightOn()) return false;

        Vector3 dir = (transform.position - jugador.position).normalized;
        float angulo = Vector3.Angle(jugador.forward, dir);
        if (angulo > anguloDeteccionLuz) return false;

        RaycastHit hit;
        if (Physics.Raycast(jugador.position + Vector3.up * 1.5f, dir, out hit, 20f))
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                return true;

        return false;
    }
}