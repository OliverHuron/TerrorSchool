using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class GhostController : MonoBehaviour
{
    [Header("Persecución")]
    public float velocidadPersecucion = 3.5f;
    public float distanciaDeteccion = 12f;   // A qué distancia detecta al jugador

    [Header("Luz")]
    public float anguloDeteccionLuz = 30f;   // Cono del spotlight
    public float tiempoParaDesaparecer = 1.5f; // Segundos iluminado antes de desaparecer

    [Header("Spawn")]
    public Transform[] puntosSpawn;          // Arrastra tus SpawnPoints aquí
    public float tiempoEntreSpawns = 15f;    // Cada cuánto reaparece

    private NavMeshAgent agente;
    private Transform jugador;
    private FlashlightController linterna;
    private float tiempoIluminado = 0f;
    private bool desapareciendo = false;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        agente.speed = velocidadPersecucion;
        jugador = GameObject.FindGameObjectWithTag("Player").transform;
        linterna = FindObjectOfType<FlashlightController>();

        // Empezar en un spawn aleatorio
        SpawnEnPuntoAleatorio();

        // Ciclo de reaparición
        StartCoroutine(CicloSpawn());
    }

    void Update()
    {
        if (desapareciendo) return;

        // Perseguir al jugador si está cerca
        float distancia = Vector3.Distance(transform.position, jugador.position);
        if (distancia < distanciaDeteccion)
        {
            agente.SetDestination(jugador.position);
        }

        // Revisar si la linterna la apunta
        if (EstaIluminadaPorLinterna())
        {
            tiempoIluminado += Time.deltaTime;
            if (tiempoIluminado >= tiempoParaDesaparecer)
                StartCoroutine(Desaparecer());
        }
        else
        {
            tiempoIluminado = 0f; // Reiniciar si dejas de apuntarle
        }
    }

    bool EstaIluminadaPorLinterna()
    {
        if (!linterna.IsLightOn()) return false;

        // Vector del jugador hacia la niña
        Vector3 dir = (transform.position - jugador.position).normalized;
        float angulo = Vector3.Angle(jugador.forward, dir);

        // ¿Está dentro del cono de la linterna?
        if (angulo > anguloDeteccionLuz) return false;

        // ¿Hay algo bloqueando (pared, etc.)?
        RaycastHit hit;
        if (Physics.Raycast(jugador.position, dir, out hit, 20f))
        {
            if (hit.transform == transform)
                return true; // La niña es lo primero que golpea el rayo
        }
        return false;
    }

    IEnumerator Desaparecer()
    {
        desapareciendo = true;
        agente.isStopped = true;

        // Aquí puedes agregar animación de desvanecimiento
        // Por ahora simplemente desactiva el renderer
        GetComponent<Renderer>().enabled = false;

        yield return new WaitForSeconds(2f);

        // Mover a un nuevo spawn antes de reactivar
        SpawnEnPuntoAleatorio();
        GetComponent<Renderer>().enabled = true;
        agente.isStopped = false;
        desapareciendo = false;
        tiempoIluminado = 0f;
    }

    IEnumerator CicloSpawn()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoEntreSpawns);
            if (!desapareciendo)
            {
                // Cambiar de posición aunque el jugador no la haya visto
                SpawnEnPuntoAleatorio();
            }
        }
    }

    void SpawnEnPuntoAleatorio()
    {
        if (puntosSpawn.Length == 0) return;
        int i = Random.Range(0, puntosSpawn.Length);
        // NavMesh.SamplePosition garantiza que aterrice en el NavMesh
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(puntosSpawn[i].position, out navHit, 2f, NavMesh.AllAreas))
            agente.Warp(navHit.position);
    }
}