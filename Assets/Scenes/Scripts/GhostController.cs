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

    [Header("Jumpscare")]
    public JumpscareEffect jumpscare;

    [Header("Luz")]
    public float anguloDeteccionLuz = 45f;
    public float tiempoParaDesaparecer = 1.5f;

    private Transform jugador;
    private FlashlightController linterna;
    private bool visible = false;
    private float tiempoIluminado = 0f;

    private Vector3 posicionFija;

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
       
    }

    void Update()
    {
        if (!visible || jugador == null || linterna == null) return;

        transform.position = posicionFija;
        // Sin LookAt, posición y rotación completamente fijas

        if (EstaIluminada())
        {
            tiempoIluminado += Time.deltaTime;
            if (tiempoIluminado >= tiempoParaDesaparecer)
                StartCoroutine(Desaparecer(false));
        }
        else
        {
            tiempoIluminado = 0f;
        }
    }

    IEnumerator CicloApariciones()
    {
        // Esperar 5 segundos al inicio antes de la primera aparición
        yield return new WaitForSeconds(5f);

        while (true)
        {
            yield return StartCoroutine(Aparecer());
            yield return new WaitForSeconds(tiempoEntreApariciones);
        }
    }

    IEnumerator Aparecer()
    {
        Vector3 direccion = Camera.main.transform.forward;
        direccion.y = 0;
        direccion.Normalize();

        posicionFija = jugador.position + direccion * distanciaAparicion;
        posicionFija.y = jugador.position.y;
        transform.position = posicionFija;

        transform.LookAt(new Vector3(jugador.position.x, transform.position.y, jugador.position.z));
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        SetVisible(true);
        tiempoIluminado = 0f;

        yield return new WaitForSeconds(tiempoVisible);

        if (visible)
        {
            bool linternaApagada = !linterna.IsLightOn();
            yield return StartCoroutine(Desaparecer(linternaApagada));
        }
    }

    IEnumerator Desaparecer(bool mostrarJumpscare = false)
    {
        SetVisible(false);
        tiempoIluminado = 0f;

        if (mostrarJumpscare && jumpscare != null)
        {
            yield return new WaitForSeconds(0.2f); // pequeña pausa dramática
            jumpscare.Activar();

            // Esperar que termine el jumpscare antes de pausar
            yield return new WaitForSeconds(jumpscare.duracionVisible + 0.5f);

            // Pausar el juego — pantalla de Game Over
            Time.timeScale = 0f;
            Debug.Log("GAME OVER");
        }
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