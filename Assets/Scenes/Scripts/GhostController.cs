using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class GhostController : MonoBehaviour
{
    public enum ModoComportamiento { AparicionFija, CaceriaNavMesh }

    [Header("Modo")]
    public ModoComportamiento modo = ModoComportamiento.CaceriaNavMesh;

    [Header("Aparición fija (modo popup)")]
    public float distanciaAparicion = 3f;
    public float tiempoVisible = 1.5f;

    [Header("Cacería NavMesh")]
    public float distanciaMinSpawn = 8f;
    public float distanciaMaxSpawn = 18f;
    public float velocidadCaminar = 1.4f;
    public float distanciaAtaque = 2f;
    public float intervaloActualizarDestino = 0.5f;

    [Header("Ciclo")]
    public float tiempoEntreApariciones = 12f;
    public float tiempoEsperaInicial = 5f;

    [Header("Modelo")]
    [Tooltip("Hijo con el mesh (Nina). El padre rota solo en Y; el hijo corrige posición y rotación.")]
    public Transform modeloVisual;
    public Vector3 posicionModelo = Vector3.zero;
    [Tooltip("Sadako viene acostada en el FBX. Ajusta aquí (no en el hijo nina). Prueba (-90,0,0).")]
    public Vector3 rotacionModelo = new Vector3(-90f, 0f, 0f);
    [Tooltip("Tamaño del mesh. El cilindro del agente mide ~2m de alto.")]
    public Vector3 escalaModelo = new Vector3(0.45f, 0.45f, 0.45f);
    [Tooltip("Solo al dar Play. Desactívalo mientras pruebas rotación en el Editor.")]
    public bool alinearPiesAlSuelo = true;

    [Header("Jumpscare")]
    public JumpscareEffect jumpscare;

    [Header("Luz")]
    public float anguloDeteccionLuz = 60f;
    public float tiempoParaDesaparecer = 1.5f;
    public float distanciaMaxLuz = 25f;
    [Tooltip("Margen extra del cono de la linterna para que sea más fácil apuntar.")]
    public float margenAnguloLuz = 8f;

    [Header("Sonido")]
    public AudioClip sonidoAparicion;
    public AudioSource audioSource;
    [Tooltip("Si está activo, se oye igual estés lejos o cerca.")]
    public bool sonidoAparicionGlobal = true;
    [Range(0f, 1f)] public float volumenSonidoAparicion = 1f;

    private Transform jugador;
    private Transform camara;
    private FlashlightController linterna;
    private NavMeshAgent agent;

    private bool visible = false;
    private bool caceriaActiva = false;
    private bool desapareciendo = false;
    private float tiempoIluminado = 0f;
    private float timerDestino = 0f;
    private Vector3 posicionFija;
    private Vector3 ultimaPosicionSpawn;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("GhostController: no se encontró el Player.");
            enabled = false;
            return;
        }

        jugador = playerObj.transform;
        camara = Camera.main != null ? Camera.main.transform : jugador;
        linterna = FindFirstObjectByType<FlashlightController>();
        agent = GetComponent<NavMeshAgent>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null && sonidoAparicionGlobal)
            audioSource.spatialBlend = 0f;

        if (modeloVisual != null)
            AplicarTransformModelo();

        ConfigurarAgente();
        SetVisible(false);
        StartCoroutine(CicloPrincipal());
    }

    void ConfigurarAgente()
    {
        if (agent == null) return;

        agent.speed = velocidadCaminar;
        agent.acceleration = 8f;
        agent.angularSpeed = 0f;
        agent.updateRotation = false;
        agent.updateUpAxis = true;
        agent.enabled = false;
    }

    void OnValidate()
    {
        if (modeloVisual == null) return;
        PrevisualizarModelo();
    }

    void PrevisualizarModelo()
    {
        modeloVisual.localRotation = Quaternion.Euler(rotacionModelo);
        modeloVisual.localScale = escalaModelo;
        modeloVisual.localPosition = posicionModelo;
    }

    void AplicarTransformModelo()
    {
        PrevisualizarModelo();
        if (!alinearPiesAlSuelo || !Application.isPlaying) return;

        float offsetY = CalcularOffsetPiesLocal();
        Vector3 pos = posicionModelo;
        pos.y += offsetY;
        modeloVisual.localPosition = pos;
    }

    float CalcularOffsetPiesLocal()
    {
        Renderer[] renderers = modeloVisual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0f;

        float minY = float.MaxValue;
        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;
            Vector3[] corners =
            {
                b.min, b.max,
                new Vector3(b.min.x, b.min.y, b.max.z),
                new Vector3(b.min.x, b.max.y, b.min.z),
                new Vector3(b.max.x, b.min.y, b.min.z),
                new Vector3(b.min.x, b.max.y, b.max.z),
                new Vector3(b.max.x, b.min.y, b.max.z),
                new Vector3(b.max.x, b.max.y, b.min.z)
            };

            foreach (Vector3 corner in corners)
            {
                float localY = transform.InverseTransformPoint(corner).y;
                if (localY < minY) minY = localY;
            }
        }

        return -minY;
    }

    void Update()
    {
        if (!visible || jugador == null || linterna == null) return;

        if (modo == ModoComportamiento.AparicionFija)
            transform.position = posicionFija;
        else if (caceriaActiva && agent != null && agent.enabled)
            ActualizarCaceria();

        ActualizarRotacion();

        if (EstaIluminada())
        {
            tiempoIluminado += Time.deltaTime;
            if (tiempoIluminado >= tiempoParaDesaparecer && !desapareciendo)
                StartCoroutine(Desaparecer(false));
        }
        else
        {
            tiempoIluminado = 0f;
        }
    }

    float DistanciaHorizontal(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void ActualizarCaceria()
    {
        timerDestino += Time.deltaTime;
        if (timerDestino >= intervaloActualizarDestino)
        {
            timerDestino = 0f;
            agent.SetDestination(jugador.position);
        }

        if (desapareciendo) return;

        float dist = DistanciaHorizontal(transform.position, jugador.position);
        bool cerca = dist <= distanciaAtaque;
        bool agenteCerca = agent.hasPath && !agent.pathPending && agent.remainingDistance <= distanciaAtaque;

        if (cerca || agenteCerca)
        {
            if (EstaIluminada())
                return;

            StartCoroutine(Desaparecer(true));
        }
    }

    void ActualizarRotacion()
    {
        Vector3 dir = Vector3.zero;

        if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
            dir = agent.velocity;
        else if (jugador != null)
            dir = jugador.position - transform.position;

        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    IEnumerator CicloPrincipal()
    {
        yield return new WaitForSeconds(tiempoEsperaInicial);

        while (true)
        {
            if (modo == ModoComportamiento.AparicionFija)
                yield return StartCoroutine(AparecerFijo());
            else
                yield return StartCoroutine(CaceriaNavMesh());

            yield return new WaitForSeconds(tiempoEntreApariciones);
        }
    }

    IEnumerator AparecerFijo()
    {
        Vector3 direccion = Camera.main.transform.forward;
        direccion.y = 0;
        direccion.Normalize();

        posicionFija = jugador.position + direccion * distanciaAparicion;
        posicionFija.y = jugador.position.y;
        transform.position = posicionFija;

        MostrarEnPosicion();

        yield return new WaitForSeconds(tiempoVisible);

        if (visible)
        {
            bool linternaApagada = !linterna.IsLightOn();
            yield return StartCoroutine(Desaparecer(linternaApagada));
        }
    }

    IEnumerator CaceriaNavMesh()
    {
        if (agent == null)
        {
            Debug.LogWarning("GhostController: modo CaceriaNavMesh requiere NavMeshAgent en el mismo objeto.");
            yield break;
        }

        if (!NavMesh.SamplePosition(jugador.position, out _, distanciaMaxSpawn, NavMesh.AllAreas))
        {
            Debug.LogWarning("GhostController: no hay NavMesh cerca del jugador. Hornea el NavMesh en Window > AI > Navigation.");
            yield break;
        }

        if (!ObtenerPuntoSpawn(out Vector3 spawn))
        {
            Debug.LogWarning("GhostController: no se encontró punto válido para aparecer.");
            yield break;
        }

        agent.enabled = true;
        agent.Warp(spawn);
        agent.isStopped = false;
        agent.SetDestination(jugador.position);
        caceriaActiva = true;
        timerDestino = 0f;

        MostrarEnPosicion();

        while (visible && caceriaActiva)
            yield return null;
    }

    bool ObtenerPuntoSpawn(out Vector3 posicion)
    {
        Vector3 mirada = Camera.main.transform.forward;
        mirada.y = 0;
        if (mirada.sqrMagnitude < 0.001f)
            mirada = -jugador.forward;
        mirada.Normalize();

        for (int i = 0; i < 30; i++)
        {
            float angulo = Random.Range(90f, 270f);
            Vector3 dir = Quaternion.Euler(0, angulo, 0) * mirada;
            float dist = Random.Range(distanciaMinSpawn, distanciaMaxSpawn);
            Vector3 candidato = jugador.position + dir * dist;

            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, distanciaMaxSpawn, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, jugador.position) < distanciaMinSpawn)
                    continue;
                if (Vector3.Distance(hit.position, ultimaPosicionSpawn) < 4f)
                    continue;

                posicion = hit.position;
                ultimaPosicionSpawn = posicion;
                return true;
            }
        }

        if (NavMesh.SamplePosition(jugador.position, out NavMeshHit fallback, distanciaMaxSpawn, NavMesh.AllAreas))
        {
            posicion = fallback.position;
            ultimaPosicionSpawn = posicion;
            return true;
        }

        posicion = jugador.position;
        return false;
    }

    void MostrarEnPosicion()
    {
        desapareciendo = false;
        SetVisible(true);
        tiempoIluminado = 0f;
        ReproducirSonidoAparicion();
    }

    void ReproducirSonidoAparicion()
    {
        if (sonidoAparicion == null) return;

        if (sonidoAparicionGlobal)
        {
            if (audioSource != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.PlayOneShot(sonidoAparicion, volumenSonidoAparicion);
                return;
            }

            AudioSource camaraAudio = camara.GetComponent<AudioSource>();
            if (camaraAudio == null)
                camaraAudio = camara.gameObject.AddComponent<AudioSource>();

            camaraAudio.spatialBlend = 0f;
            camaraAudio.PlayOneShot(sonidoAparicion, volumenSonidoAparicion);
            return;
        }

        if (audioSource != null)
            audioSource.PlayOneShot(sonidoAparicion, volumenSonidoAparicion);
        else
            AudioSource.PlayClipAtPoint(sonidoAparicion, transform.position, volumenSonidoAparicion);
    }

    IEnumerator Desaparecer(bool mostrarJumpscare = false)
    {
        if (desapareciendo) yield break;
        desapareciendo = true;
        caceriaActiva = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        SetVisible(false);
        tiempoIluminado = 0f;

        if (mostrarJumpscare && jumpscare != null)
        {
            yield return new WaitForSeconds(0.2f);
            jumpscare.Activar();
            yield return new WaitForSeconds(jumpscare.duracionVisible + 0.5f);
            Time.timeScale = 0f;
        }

        desapareciendo = false;
    }

    void SetVisible(bool estado)
    {
        visible = estado;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = estado;
    }

    bool EstaIluminada()
    {
        if (linterna == null || !linterna.IsLightOn() || !visible) return false;

        foreach (Vector3 punto in ObtenerPuntosDeteccion())
        {
            if (PuntoIluminado(punto))
                return true;
        }

        return false;
    }

    Vector3[] ObtenerPuntosDeteccion()
    {
        Vector3 centro = ObtenerPuntoIluminacion();
        return new[]
        {
            centro,
            centro + Vector3.up * 0.8f,
            centro + Vector3.up * 1.4f
        };
    }

    bool PuntoIluminado(Vector3 punto)
    {
        if (EnConoLuz(punto) && TieneLineaVision(punto))
            return true;

        return EnConoCamara(punto) && TieneLineaVision(punto);
    }

    bool EnConoLuz(Vector3 punto)
    {
        return EnCono(ObtenerOrigenLuz(), ObtenerDireccionLuz(), ObtenerAnguloLuz() + margenAnguloLuz, punto);
    }

    bool EnConoCamara(Vector3 punto)
    {
        return EnCono(camara.position, camara.forward, anguloDeteccionLuz + margenAnguloLuz, punto);
    }

    bool EnCono(Vector3 origen, Vector3 direccion, float anguloMax, Vector3 punto)
    {
        Vector3 dirObjetivo = punto - origen;
        float distancia = dirObjetivo.magnitude;
        if (distancia > distanciaMaxLuz || distancia < 0.01f) return false;

        return Vector3.Angle(direccion, dirObjetivo) <= anguloMax;
    }

    bool TieneLineaVision(Vector3 punto)
    {
        Vector3 origen = camara.position;
        Vector3 dir = punto - origen;
        float distObjetivo = dir.magnitude;
        if (distObjetivo < 0.01f) return true;

        dir.Normalize();
        RaycastHit[] hits = Physics.RaycastAll(origen, dir, distObjetivo + 0.5f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (EsParteDelJugador(hit.transform)) continue;
            if (EsParteDelFantasma(hit.transform)) return true;

            // Sin collider en el fantasma: si el obstáculo está detrás de él, cuenta como iluminado
            if (hit.distance < distObjetivo - 0.4f)
                return false;
        }

        return true;
    }

    bool EsParteDelJugador(Transform t)
    {
        if (jugador == null) return false;
        return t.CompareTag("Player") || t.IsChildOf(jugador);
    }

    Vector3 ObtenerOrigenLuz()
    {
        if (linterna.flashlight != null)
            return linterna.flashlight.transform.position;
        return camara.position;
    }

    Vector3 ObtenerDireccionLuz()
    {
        if (linterna.flashlight != null)
            return linterna.flashlight.transform.forward;
        return camara.forward;
    }

    float ObtenerAnguloLuz()
    {
        if (linterna.flashlight != null && linterna.flashlight.type == LightType.Spot)
            return linterna.flashlight.spotAngle * 0.5f;
        return anguloDeteccionLuz;
    }

    Vector3 ObtenerPuntoIluminacion()
    {
        if (modeloVisual != null)
            return modeloVisual.position;
        return transform.position + Vector3.up;
    }

    bool EsParteDelFantasma(Transform t)
    {
        return t == transform
            || t.IsChildOf(transform)
            || (modeloVisual != null && (t == modeloVisual || t.IsChildOf(modeloVisual)));
    }
}
