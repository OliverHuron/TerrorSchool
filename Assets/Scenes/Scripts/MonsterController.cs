using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterController : MonoBehaviour
{
    enum Estado { Inactivo, Patrullando, Atacando, GolpeLuz }

    [Header("Spawn")]
    public float tiempoEsperaInicial = 5f;
    public float cooldownMin = 10f;
    public float cooldownMax = 20f;
    [Tooltip("Obligatorio: Empty objects donde puede aparecer el mutante.")]
    public Transform[] puntosSpawn;

    [Header("Ruta de patrulla")]
    [Tooltip("Empty objects por los que patrulla aleatoriamente.")]
    public Transform[] puntosRuta;
    public float distanciaLlegadaRuta = 1.2f;

    [Header("Detección")]
    public float distanciaDeteccion = 12f;
    public bool requiereLineaVista = true;

    [Header("Movimiento")]
    public float velocidadCaminar = 2.5f;
    public float velocidadCorrer = 5.5f;

    [Header("Modelo")]
    public Transform modeloVisual;
    public Vector3 posicionModelo = Vector3.zero;
    public Vector3 rotacionModelo = Vector3.zero;
    public Vector3 escalaModelo = Vector3.one;
    public bool alinearPiesAlSuelo = true;

    [Header("Animación")]
    public Animator animator;
    public string[] caminatas = { "walk1", "walk2", "walk3", "walk4" };
    public string[] carreras = { "run1", "run2", "run3" };
    public string[] ataques =
    {
        "attack1", "attack2", "attack3", "attack4", "attack5",
        "attack1LSpike", "attack1RSpike", "attack2LSpike", "attack2RLSpike",
        "attack3RSpike", "attack4RSpike", "attack5LSpike"
    };
    public string[] golpesLuz = { "gethit1", "gethit2", "gethit3", "gethit4" };
    public float intervaloCambioWalk = 3.5f;

    [Header("Jumpscare / Muerte")]
    public JumpscareEffect jumpscare;
    public float distanciaMuerte = 2.5f;
    public float tiempoGolpeAntesMuerte = 0.15f;

    [Header("Luz")]
    public float tiempoLuzParaInterrumpir = 0.3f;
    public float duracionGolpeLuz = 0.5f;
    public float anguloDeteccionLuz = 60f;
    public float distanciaMaxLuz = 25f;
    public float margenAnguloLuz = 10f;

    [Header("Sonido")]
    public AudioClip sonidoAparicion;
    public AudioSource audioSource;
    public bool sonidoAparicionGlobal = true;
    [Range(0f, 1f)] public float volumenAparicion = 1f;

    Estado estado = Estado.Inactivo;
    Transform jugador;
    Transform camara;
    PlayerMovement movimientoJugador;
    FlashlightController linterna;
    CamcorderController camcorder;
    NavMeshAgent agent;

    float tiempoIluminado;
    float timerCambioWalk;
    float timerGolpeLuz;
    float timerAnimAtaque;
    bool ataqueAnimIniciado;
    bool muerteIniciada;
    string animActual = "";
    string ataqueElegido = "";
    string golpeLuzElegido = "";
    string locomotionActual = "";
    Transform puntoRutaActual;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("MonsterController: no se encontró el Player.");
            enabled = false;
            return;
        }

        jugador = playerObj.transform;
        movimientoJugador = playerObj.GetComponent<PlayerMovement>();
        camara = Camera.main != null ? Camera.main.transform : jugador;
        linterna = FindFirstObjectByType<FlashlightController>();
        camcorder = FindFirstObjectByType<CamcorderController>();
        agent = GetComponent<NavMeshAgent>();

        if (animator == null && modeloVisual != null)
            animator = modeloVisual.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (jumpscare == null)
            jumpscare = FindFirstObjectByType<JumpscareEffect>();

        if (modeloVisual != null)
            AplicarTransformModelo();

        ConfigurarAgente();
        SetVisible(false);
        StartCoroutine(CicloPrincipal());
    }

    void AplicarParadaMuerte()
    {
        if (agent == null)
            return;

        agent.stoppingDistance = Mathf.Max(0.6f, distanciaMuerte * 0.72f);
    }

    void ConfigurarAgente()
    {
        if (agent == null)
            return;

        agent.acceleration = 10f;
        agent.angularSpeed = 0f;
        agent.updateRotation = false;
        agent.updateUpAxis = true;
        agent.enabled = false;
    }

    void OnValidate()
    {
        if (modeloVisual == null)
            return;

        modeloVisual.localRotation = Quaternion.Euler(rotacionModelo);
        modeloVisual.localScale = escalaModelo;
        modeloVisual.localPosition = posicionModelo;

        if (animator == null)
            animator = modeloVisual.GetComponentInChildren<Animator>();
    }

    void AplicarTransformModelo()
    {
        OnValidate();
        if (!alinearPiesAlSuelo || !Application.isPlaying || modeloVisual == null)
            return;

        float offsetY = CalcularOffsetPiesLocal();
        Vector3 pos = posicionModelo;
        pos.y += offsetY;
        modeloVisual.localPosition = pos;
    }

    float CalcularOffsetPiesLocal()
    {
        Renderer[] renderers = modeloVisual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return 0f;

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
                if (localY < minY)
                    minY = localY;
            }
        }

        return -minY;
    }

    void Update()
    {
        if (estado == Estado.Inactivo || jugador == null)
            return;

        ActualizarRotacion();

        switch (estado)
        {
            case Estado.Patrullando:
                ActualizarPatrulla();
                break;
            case Estado.Atacando:
                ActualizarAtaque();
                break;
            case Estado.GolpeLuz:
                ActualizarGolpeLuz();
                break;
        }
    }

    void ActualizarPatrulla()
    {
        if (JugadorDetectable())
        {
            IniciarAtaque();
            return;
        }

        if (agent == null || !agent.enabled)
            return;

        agent.isStopped = false;
        agent.speed = velocidadCaminar;

        if (puntoRutaActual == null || LlegoAPuntoRuta())
            ElegirNuevoPuntoRuta();

        timerCambioWalk += Time.deltaTime;
        if (timerCambioWalk >= intervaloCambioWalk)
        {
            timerCambioWalk = 0f;
            EstablecerLocomotion(ElegirAleatoria(caminatas));
        }
        else if (string.IsNullOrEmpty(locomotionActual))
        {
            EstablecerLocomotion(ElegirAleatoria(caminatas, caminatas[0]));
        }
        else
        {
            MantenerAnimLoop(locomotionActual);
        }

        SincronizarVelocidadAnim(agent.velocity.magnitude, velocidadCaminar);
    }

    void ActualizarAtaque()
    {
        if (JugadorEscondido())
        {
            VolverAPatrulla();
            return;
        }

        if (EstaIluminada())
        {
            tiempoIluminado += Time.deltaTime;
            if (tiempoIluminado >= tiempoLuzParaInterrumpir)
                IniciarGolpeLuz();
            return;
        }

        tiempoIluminado = 0f;

        agent.isStopped = false;
        agent.speed = velocidadCorrer;
        AplicarParadaMuerte();
        agent.SetDestination(jugador.position);

        string animCarrera = locomotionActual;
        if (string.IsNullOrEmpty(animCarrera) || !EsAnimDePool(animCarrera, carreras))
            EstablecerLocomotion(ElegirAleatoria(carreras, carreras[0]));
        else
            MantenerAnimLoop(animCarrera);

        SincronizarVelocidadAnim(agent.velocity.magnitude, velocidadCorrer);

        timerAnimAtaque += Time.deltaTime;
        float dist = DistanciaHorizontal(transform.position, jugador.position);
        bool enRango = dist <= distanciaMuerte + 0.55f;

        if (!enRango)
            return;

        CorregirDistanciaMuerte();
        agent.isStopped = true;
        AlinearFrenteAlJugador();

        if (!ataqueAnimIniciado)
        {
            ataqueElegido = ElegirAleatoria(ataques, ataques[0]);
            ataqueAnimIniciado = true;
            timerAnimAtaque = 0f;
            ReproducirAnim(ataqueElegido);
        }
        else
        {
            MantenerAnimLoop(ataqueElegido);
        }

        if (timerAnimAtaque >= tiempoGolpeAntesMuerte && !muerteIniciada)
        {
            muerteIniciada = true;
            StartCoroutine(FinalizarAtaque());
        }
    }

    void ActualizarGolpeLuz()
    {
        agent.isStopped = true;
        timerGolpeLuz += Time.deltaTime;
        MantenerAnimLoop(golpeLuzElegido);

        if (timerGolpeLuz >= duracionGolpeLuz)
            VolverAPatrulla();
    }

    bool JugadorEscondido()
    {
        return movimientoJugador != null && movimientoJugador.EstaEscondido;
    }

    bool JugadorDetectable()
    {
        if (JugadorEscondido())
            return false;

        float dist = DistanciaHorizontal(transform.position, jugador.position);
        if (dist > distanciaDeteccion)
            return false;

        if (!requiereLineaVista)
            return true;

        return TieneLineaVistaJugador();
    }

    bool TieneLineaVistaJugador()
    {
        Vector3 origen = modeloVisual != null
            ? modeloVisual.position + Vector3.up * 1.4f
            : transform.position + Vector3.up * 1.6f;
        Vector3 destino = jugador.position + Vector3.up * 1f;
        Vector3 dir = destino - origen;
        float dist = dir.magnitude;
        if (dist < 0.01f)
            return true;

        dir.Normalize();
        RaycastHit[] hits = Physics.RaycastAll(origen, dir, dist + 0.5f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (EsParteDelJugador(hit.transform))
                return true;
            if (EsParteDelMonstruo(hit.transform))
                continue;
            return false;
        }

        return true;
    }

    void IniciarAtaque()
    {
        estado = Estado.Atacando;
        timerAnimAtaque = 0f;
        ataqueAnimIniciado = false;
        muerteIniciada = false;
        tiempoIluminado = 0f;
        locomotionActual = "";
        agent.isStopped = false;
        agent.speed = velocidadCorrer;
        AplicarParadaMuerte();
        agent.SetDestination(jugador.position);
    }

    void IniciarGolpeLuz()
    {
        if (estado == Estado.GolpeLuz)
            return;

        estado = Estado.GolpeLuz;
        timerGolpeLuz = 0f;
        agent.isStopped = true;
        agent.ResetPath();
        golpeLuzElegido = ElegirAleatoria(golpesLuz, "gethit1");
        ReproducirAnim(golpeLuzElegido);
    }

    void VolverAPatrulla()
    {
        estado = Estado.Patrullando;
        tiempoIluminado = 0f;
        timerAnimAtaque = 0f;
        ataqueAnimIniciado = false;
        muerteIniciada = false;
        locomotionActual = "";
        agent.isStopped = false;
        agent.speed = velocidadCaminar;
        ElegirNuevoPuntoRuta();
    }

    bool LlegoAPuntoRuta()
    {
        if (puntoRutaActual == null)
            return true;

        if (agent.pathPending)
            return false;

        if (agent.remainingDistance <= distanciaLlegadaRuta)
            return true;

        return !agent.hasPath || agent.remainingDistance == Mathf.Infinity;
    }

    void ElegirNuevoPuntoRuta()
    {
        if (puntosRuta == null || puntosRuta.Length == 0)
        {
            agent.ResetPath();
            puntoRutaActual = null;
            return;
        }

        Transform elegido = puntoRutaActual;
        for (int intento = 0; intento < 8; intento++)
        {
            Transform candidato = puntosRuta[Random.Range(0, puntosRuta.Length)];
            if (candidato == null)
                continue;
            if (puntosRuta.Length == 1 || candidato != puntoRutaActual)
            {
                elegido = candidato;
                break;
            }
        }

        puntoRutaActual = elegido;
        agent.isStopped = false;
        agent.SetDestination(puntoRutaActual.position);
    }

    IEnumerator CicloPrincipal()
    {
        yield return new WaitForSeconds(tiempoEsperaInicial);

        while (true)
        {
            yield return StartCoroutine(EncuentroActivo());
            float espera = Random.Range(cooldownMin, cooldownMax);
            yield return new WaitForSeconds(espera);
        }
    }

    IEnumerator EncuentroActivo()
    {
        if (agent == null)
        {
            Debug.LogWarning("MonsterController: falta NavMeshAgent.");
            yield break;
        }

        if (!ObtenerPuntoSpawn(out Vector3 spawn))
        {
            Debug.LogWarning("MonsterController: asigna al menos un punto en Puntos Spawn.");
            yield break;
        }

        agent.enabled = true;
        agent.Warp(spawn);
        agent.isStopped = false;
        agent.speed = velocidadCaminar;

        locomotionActual = "";
        muerteIniciada = false;
        ataqueAnimIniciado = false;
        SetVisible(true);
        ReproducirSonidoAparicion();

        estado = Estado.Patrullando;
        ElegirNuevoPuntoRuta();
        EstablecerLocomotion(ElegirAleatoria(caminatas, caminatas[0]));

        while (estado != Estado.Inactivo)
            yield return null;
    }

    IEnumerator FinalizarAtaque()
    {
        estado = Estado.Inactivo;
        agent.isStopped = true;
        AlinearFrenteAlJugador();
        CorregirDistanciaMuerte();
        agent.enabled = false;
        SetVisible(false);

        if (jumpscare != null)
        {
            jumpscare.Activar();
            yield return new WaitForSeconds(jumpscare.ObtenerDuracionMuerte());
        }
    }

    bool ObtenerPuntoSpawn(out Vector3 posicion)
    {
        posicion = transform.position;

        if (puntosSpawn == null || puntosSpawn.Length == 0)
            return false;

        for (int intento = 0; intento < 20; intento++)
        {
            Transform punto = puntosSpawn[Random.Range(0, puntosSpawn.Length)];
            if (punto == null)
                continue;

            if (NavMesh.SamplePosition(punto.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                posicion = hit.position;
                return true;
            }
        }

        return false;
    }

    void AlinearFrenteAlJugador()
    {
        Vector3 look = jugador.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(look.normalized);
    }

    void CorregirDistanciaMuerte()
    {
        if (agent == null || jugador == null)
            return;

        Vector3 objetivo = ObtenerPuntoFrenteJugador(distanciaMuerte);
        if (NavMesh.SamplePosition(objetivo, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
        {
            float dist = DistanciaHorizontal(transform.position, jugador.position);
            if (dist < distanciaMuerte - 0.1f || dist > distanciaMuerte + 0.75f)
            {
                if (agent.enabled)
                    agent.Warp(hit.position);
                else
                    transform.position = hit.position;
            }
        }

        AlinearFrenteAlJugador();
    }

    Vector3 ObtenerPuntoFrenteJugador(float distancia)
    {
        Vector3 haciaMonstruo = transform.position - jugador.position;
        haciaMonstruo.y = 0f;

        if (haciaMonstruo.sqrMagnitude > 0.04f)
        {
            haciaMonstruo.Normalize();
            return jugador.position + haciaMonstruo * distancia;
        }

        Vector3 forward = camara != null ? camara.forward : jugador.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = jugador.forward;

        forward.Normalize();
        return jugador.position + forward * distancia;
    }

    void ActualizarRotacion()
    {
        Vector3 dir = Vector3.zero;

        if (estado == Estado.Atacando || estado == Estado.GolpeLuz)
            dir = jugador.position - transform.position;
        else if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
            dir = agent.velocity;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    string ElegirAleatoria(string[] pool, string fallback = null)
    {
        if (pool == null || pool.Length == 0)
            return fallback ?? "";

        string elegida = pool[Random.Range(0, pool.Length)];
        if (pool.Length > 1 && elegida == animActual)
            elegida = pool[Random.Range(0, pool.Length)];

        return elegida;
    }

    bool EsAnimDePool(string nombre, string[] pool)
    {
        if (pool == null || string.IsNullOrEmpty(nombre))
            return false;

        foreach (string item in pool)
        {
            if (item == nombre)
                return true;
        }

        return false;
    }

    void EstablecerLocomotion(string nombre)
    {
        locomotionActual = nombre;
        ReproducirAnim(nombre);
    }

    void SincronizarVelocidadAnim(float velocidadReal, float velocidadReferencia)
    {
        if (animator == null || velocidadReferencia <= 0.01f)
            return;

        if (velocidadReal > 0.05f)
            animator.speed = Mathf.Clamp(velocidadReal / velocidadReferencia, 0.6f, 1.4f);
        else
            animator.speed = 1f;
    }

    void ReproducirAnim(string nombre)
    {
        if (animator == null || string.IsNullOrEmpty(nombre))
            return;
        if (animActual == nombre && EstaEnAnim(nombre))
            return;

        animActual = nombre;
        animator.Play(nombre, 0, 0f);
    }

    void MantenerAnimLoop(string nombre)
    {
        if (animator == null || string.IsNullOrEmpty(nombre))
            return;

        if (!EstaEnAnim(nombre))
        {
            animActual = "";
            ReproducirAnim(nombre);
            return;
        }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.normalizedTime >= 0.7f)
        {
            animActual = "";
            ReproducirAnim(nombre);
        }
    }

    bool EstaEnAnim(string nombre)
    {
        if (animator == null || string.IsNullOrEmpty(nombre))
            return false;

        int hash = Animator.StringToHash(nombre);
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.shortNameHash == hash || info.IsName(nombre);
    }

    void ReproducirSonidoAparicion()
    {
        if (sonidoAparicion == null)
            return;

        if (sonidoAparicionGlobal)
            ReproducirSonido2D(sonidoAparicion, volumenAparicion);
        else if (audioSource != null)
            audioSource.PlayOneShot(sonidoAparicion, volumenAparicion);
    }

    void ReproducirSonido2D(AudioClip clip, float volumen)
    {
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.PlayOneShot(clip, volumen);
            return;
        }

        AudioSource camaraAudio = camara.GetComponent<AudioSource>();
        if (camaraAudio == null)
            camaraAudio = camara.gameObject.AddComponent<AudioSource>();

        camaraAudio.spatialBlend = 0f;
        camaraAudio.PlayOneShot(clip, volumen);
    }

    void SetVisible(bool estadoVisible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = estadoVisible;
    }

    float DistanciaHorizontal(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    bool EstaIluminada()
    {
        if (estado == Estado.Inactivo || !LuzJugadorEncendida())
            return false;

        foreach (Vector3 punto in ObtenerPuntosDeteccion())
        {
            if (PuntoIluminado(punto))
                return true;
        }

        return false;
    }

    bool LuzJugadorEncendida()
    {
        if (linterna != null && linterna.IsLightOn())
            return true;

        return camcorder != null && camcorder.EstaEncendida();
    }

    Vector3[] ObtenerPuntosDeteccion()
    {
        Vector3 centro = modeloVisual != null ? modeloVisual.position : transform.position + Vector3.up;
        return new[]
        {
            centro,
            centro + Vector3.up * 0.8f,
            centro + Vector3.up * 1.6f
        };
    }

    bool PuntoIluminado(Vector3 punto)
    {
        if (EnCono(ObtenerOrigenLuz(), ObtenerDireccionLuz(), ObtenerAnguloLuz() + margenAnguloLuz, punto)
            && TieneLineaVision(punto))
            return true;

        return EnCono(camara.position, camara.forward, anguloDeteccionLuz + margenAnguloLuz, punto)
            && TieneLineaVision(punto);
    }

    bool EnCono(Vector3 origen, Vector3 direccion, float anguloMax, Vector3 punto)
    {
        Vector3 dirObjetivo = punto - origen;
        float distancia = dirObjetivo.magnitude;
        if (distancia > distanciaMaxLuz || distancia < 0.01f)
            return false;

        return Vector3.Angle(direccion, dirObjetivo) <= anguloMax;
    }

    bool TieneLineaVision(Vector3 punto)
    {
        Vector3 origen = camara.position;
        Vector3 dir = punto - origen;
        float distObjetivo = dir.magnitude;
        if (distObjetivo < 0.01f)
            return true;

        dir.Normalize();
        RaycastHit[] hits = Physics.RaycastAll(origen, dir, distObjetivo + 0.5f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (EsParteDelJugador(hit.transform))
                continue;
            if (EsParteDelMonstruo(hit.transform))
                return true;
            if (hit.distance < distObjetivo - 0.4f)
                return false;
        }

        return true;
    }

    bool EsParteDelJugador(Transform t)
    {
        return t.CompareTag("Player") || t.IsChildOf(jugador);
    }

    bool EsParteDelMonstruo(Transform t)
    {
        return t == transform || t.IsChildOf(transform);
    }

    Vector3 ObtenerOrigenLuz()
    {
        if (linterna != null && linterna.flashlight != null)
            return linterna.flashlight.transform.position;
        return camara.position;
    }

    Vector3 ObtenerDireccionLuz()
    {
        if (linterna != null && linterna.flashlight != null)
            return linterna.flashlight.transform.forward;
        return camara.forward;
    }

    float ObtenerAnguloLuz()
    {
        if (linterna != null && linterna.flashlight != null && linterna.flashlight.type == LightType.Spot)
            return linterna.flashlight.spotAngle * 0.5f;
        return anguloDeteccionLuz;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, distanciaDeteccion);

        if (puntosSpawn != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform punto in puntosSpawn)
            {
                if (punto != null)
                    Gizmos.DrawWireSphere(punto.position, 0.6f);
            }
        }

        if (puntosRuta != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform punto in puntosRuta)
            {
                if (punto != null)
                    Gizmos.DrawWireSphere(punto.position, 0.45f);
            }
        }
    }
#endif
}
