using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterController : MonoBehaviour
{
    enum Estado { Inactivo, Cazando, Observando, AdvertenciaAtaque, Atacando, Retirandose, GolpeLuz, Rage, SaltoSpawn }

    [Header("Spawn")]
    public float tiempoEsperaInicial = 5f;
    public float cooldownMin = 10f;
    public float cooldownMax = 20f;
    public float distanciaMinSpawn = 10f;
    public float distanciaMaxSpawn = 20f;
    [Tooltip("Opcional: crea Empty objects en el suelo permitido y arrástralos aquí.")]
    public Transform[] puntosSpawn;
    [Tooltip("No spawnea en plantas mucho más altas/bajas que el jugador.")]
    public float diferenciaAlturaMax = 2f;

    [Header("Cacería")]
    public float velocidadCaminar = 2.5f;
    public float velocidadCorrer = 5.5f;
    public float velocidadRetirada = 2.8f;
    public float distanciaObservacion = 10f;
    public float distanciaAdvertencia = 12f;
    public float distanciaAtaque = 3.5f;
    public float tiempoAntesAtaque = 2f;
    public float intervaloActualizarDestino = 0.3f;
    public float intervaloAtaqueAdvertencia = 7f;

    [Header("Linterna — retirada")]
    public float tiempoLuzParaRetroceder = 0.3f;
    public float duracionGolpeLuz = 0.5f;
    public string animRetirada = "walkback";

    [Header("Modelo")]
    public Transform modeloVisual;
    public Vector3 posicionModelo = Vector3.zero;
    public Vector3 rotacionModelo = Vector3.zero;
    public Vector3 escalaModelo = Vector3.one;
    public bool alinearPiesAlSuelo = true;

    [Header("Animación — pools")]
    [Tooltip("Opcional si Modelo Visual está asignado: se busca solo al iniciar.")]
    public Animator animator;
    public string[] idles = { "idle1", "idle2", "idle3", "idle4" };
    public string[] caminatas = { "walk1", "walk2", "walk3", "walk4" };
    public string[] carreras = { "run1", "run2", "run3" };
    public string[] huidas = { "walkback", "run2", "run3" };
    public string[] ataques =
    {
        "attack1", "attack2", "attack3", "attack4", "attack5",
        "attack1LSpike", "attack1RSpike", "attack2LSpike", "attack2RLSpike",
        "attack3RSpike", "attack4RSpike", "attack5LSpike"
    };
    public string[] golpesLuz = { "gethit1", "gethit2", "gethit3", "gethit4" };
    public string animRage = "rage";
    public string animSaltoSpawn = "jump";

    [Header("Animación — tiempos")]
    public float intervaloCambioIdle = 2.2f;
    public float intervaloCambioWalk = 3.5f;
    public float duracionRage = 1.1f;
    public float duracionAtaqueAdvertencia = 1.6f;
    public float duracionAnimAtaque = 0.35f;
    public float probabilidadSaltoSpawn = 0.25f;
    public bool usarStrafeAlObservar = true;
    public bool mirarAlJugadorAlHuir = true;

    [Header("Jumpscare / Muerte")]
    public JumpscareEffect jumpscare;
    [Tooltip("Distancia al jugador donde se detiene (frente a ti, no encima).")]
    public float distanciaMuerte = 2.5f;
    [Tooltip("Segundos de golpe antes del video de muerte.")]
    public float tiempoGolpeAntesMuerte = 0.15f;

    [Header("Luz")]
    public float anguloDeteccionLuz = 60f;
    public float distanciaMaxLuz = 25f;
    public float margenAnguloLuz = 10f;

    [Header("Sonido")]
    public AudioClip sonidoAparicion;
    public AudioClip sonidoObservacion;
    public AudioSource audioSource;
    public bool sonidoAparicionGlobal = true;
    public bool sonidoObservacionGlobal = true;
    public float intervaloRugido = 3f;
    [Range(0f, 1f)] public float volumenAparicion = 1f;
    [Range(0f, 1f)] public float volumenObservacion = 1f;

    private Estado estado = Estado.Inactivo;
    private Transform jugador;
    private Transform camara;
    private FlashlightController linterna;
    private CamcorderController camcorder;
    private NavMeshAgent agent;

    private float tiempoIluminado = 0f;
    private float timerDestino = 0f;
    private float timerObservacion = 0f;
    private float timerAnimAtaque = 0f;
    private float timerAdvertencia = 0f;
    private float timerEntreAdvertencias = 0f;
    private float timerCambioIdle = 0f;
    private float timerCambioWalk = 0f;
    private float timerRage = 0f;
    private float timerGolpeLuz = 0f;
    private float timerSaltoSpawn = 0f;
    private float timerRugido = 0f;
    private bool ataqueAnimIniciado = false;
    private bool advertenciaEnCurso = false;
    private bool muerteIniciada = false;
    private string animActual = "";
    private string ataqueElegido = "";
    private string golpeLuzElegido = "";
    private string locomotionActual = "";
    private Vector3 ultimaPosicionSpawn;

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
        if (agent == null) return;

        agent.acceleration = 10f;
        agent.angularSpeed = 0f;
        agent.updateRotation = false;
        agent.updateUpAxis = true;
        agent.enabled = false;
    }

    void OnValidate()
    {
        if (modeloVisual == null) return;
        modeloVisual.localRotation = Quaternion.Euler(rotacionModelo);
        modeloVisual.localScale = escalaModelo;
        modeloVisual.localPosition = posicionModelo;

        if (animator == null)
            animator = modeloVisual.GetComponentInChildren<Animator>();
    }

    void AplicarTransformModelo()
    {
        OnValidate();
        if (!alinearPiesAlSuelo || !Application.isPlaying || modeloVisual == null) return;

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
        if (estado == Estado.Inactivo || jugador == null) return;

        ActualizarRotacion();
        ActualizarLuz();

        switch (estado)
        {
            case Estado.SaltoSpawn: ActualizarSaltoSpawn(); break;
            case Estado.Cazando: ActualizarCazando(); break;
            case Estado.Observando: ActualizarObservando(); break;
            case Estado.Rage: ActualizarRage(); break;
            case Estado.AdvertenciaAtaque: ActualizarAdvertenciaAtaque(); break;
            case Estado.Atacando: ActualizarAtacando(); break;
            case Estado.GolpeLuz: ActualizarGolpeLuz(); break;
            case Estado.Retirandose: ActualizarRetirada(); break;
        }
    }

    void ActualizarSaltoSpawn()
    {
        agent.isStopped = true;
        timerSaltoSpawn += Time.deltaTime;
        if (timerSaltoSpawn >= 1.1f)
            ReanudarCaceriaTrasSpawn();
    }

    void ActualizarRage()
    {
        agent.isStopped = true;
        timerRage += Time.deltaTime;
        ReproducirAnim(animRage);

        if (timerRage >= duracionRage)
        {
            estado = Estado.Atacando;
            timerAnimAtaque = 0f;
            ataqueAnimIniciado = false;
            agent.isStopped = false;
            agent.speed = velocidadCorrer;
            AplicarParadaMuerte();
            agent.SetDestination(ObtenerPuntoFrenteJugador(distanciaMuerte));
        }
    }

    void ActualizarGolpeLuz()
    {
        agent.isStopped = true;
        timerGolpeLuz += Time.deltaTime;
        MantenerAnimLoop(golpeLuzElegido);
        if (timerGolpeLuz >= duracionGolpeLuz)
            IniciarRetirada();
    }

    void ActualizarLuz()
    {
        bool iluminado = EstaIluminada();

        if (!iluminado)
        {
            tiempoIluminado = 0f;
            return;
        }

        tiempoIluminado += Time.deltaTime;

        if (tiempoIluminado < tiempoLuzParaRetroceder) return;

        if (estado == Estado.Retirandose || estado == Estado.GolpeLuz || estado == Estado.Inactivo)
            return;

        if (golpesLuz != null && golpesLuz.Length > 0)
            IniciarGolpeLuz();
        else
            IniciarRetirada();
    }

    bool LinternaApagada()
    {
        if (linterna != null && linterna.IsLightOn())
            return false;

        if (camcorder != null && camcorder.EstaEncendida())
            return false;

        return true;
    }

    float VelocidadAcercamiento()
    {
        if (LinternaApagada())
            return velocidadCorrer * 0.85f;
        return velocidadCaminar;
    }

    void ActualizarCazando()
    {
        if (EstaIluminada())
            return;

        timerDestino += Time.deltaTime;
        if (timerDestino >= intervaloActualizarDestino)
        {
            timerDestino = 0f;
            agent.SetDestination(jugador.position);
        }

        agent.isStopped = false;
        agent.speed = VelocidadAcercamiento();
        timerCambioWalk += Time.deltaTime;
        timerEntreAdvertencias += Time.deltaTime;

        if (LinternaApagada())
        {
            if (string.IsNullOrEmpty(locomotionActual) || !EsAnimDePool(locomotionActual, carreras))
                EstablecerLocomotion(ElegirAleatoria(carreras, carreras[0]));
            else
                MantenerAnimLoop(locomotionActual);
        }
        else if (timerCambioWalk >= intervaloCambioWalk)
        {
            timerCambioWalk = 0f;
            EstablecerLocomotion(ElegirAleatoria(caminatas));
        }
        else if (string.IsNullOrEmpty(locomotionActual) || EsAnimDePool(locomotionActual, carreras))
            EstablecerLocomotion(ElegirAleatoria(caminatas, caminatas[0]));
        else
            MantenerAnimLoop(locomotionActual);

        SincronizarVelocidadAnim(agent.velocity.magnitude, LinternaApagada() ? velocidadCorrer : velocidadCaminar);

        float dist = DistanciaHorizontal(transform.position, jugador.position);

        if (dist <= distanciaAdvertencia)
            ActualizarRugido();

        if (LinternaApagada() && dist <= distanciaAdvertencia && timerEntreAdvertencias >= intervaloAtaqueAdvertencia)
            IniciarAdvertenciaAtaque();
        else if (dist <= distanciaObservacion)
            IniciarObservacion();
    }

    void ReproducirAnimActualOCambiar(string[] pool, string fallback)
    {
        if (string.IsNullOrEmpty(animActual))
            ReproducirAnim(ElegirAleatoria(pool, fallback));
    }

    void ActualizarObservando()
    {
        if (EstaIluminada())
            return;

        agent.isStopped = true;
        ActualizarRugido();

        timerObservacion -= Time.deltaTime;
        timerCambioIdle += Time.deltaTime;
        timerEntreAdvertencias += Time.deltaTime;

        float dist = DistanciaHorizontal(transform.position, jugador.position);

        if (LinternaApagada() && dist <= distanciaAdvertencia && timerEntreAdvertencias >= intervaloAtaqueAdvertencia)
        {
            IniciarAdvertenciaAtaque();
            return;
        }

        if (usarStrafeAlObservar && TryReproducirStrafe()) { }
        else if (timerCambioIdle >= intervaloCambioIdle)
        {
            timerCambioIdle = 0f;
            ReproducirAnim(ElegirAleatoria(idles));
        }
        else if (timerObservacion <= 1f && LinternaApagada() && !string.IsNullOrEmpty(animRage))
        {
            ReproducirAnim(animRage);
        }
        else
        {
            ReproducirAnimActualOCambiar(idles, idles[0]);
        }

        if (dist > distanciaObservacion * 1.3f)
        {
            ReanudarCaceria();
            return;
        }

        if (timerObservacion <= 0f && LinternaApagada())
            IniciarAtaque();
    }

    void ActualizarAdvertenciaAtaque()
    {
        agent.isStopped = true;
        timerAdvertencia += Time.deltaTime;

        if (!advertenciaEnCurso)
        {
            advertenciaEnCurso = true;
            ataqueElegido = ElegirAleatoria(ataques, ataques[0]);
            ReproducirAnim(ataqueElegido);
        }
        else
        {
            MantenerAnimLoop(ataqueElegido);
        }

        if (timerAdvertencia >= duracionAtaqueAdvertencia)
            FinalizarAdvertenciaAtaque();
    }

    void FinalizarAdvertenciaAtaque()
    {
        advertenciaEnCurso = false;
        timerAdvertencia = 0f;
        timerEntreAdvertencias = 0f;

        if (EstaIluminada())
        {
            IniciarRetirada();
            return;
        }

        float dist = DistanciaHorizontal(transform.position, jugador.position);
        if (LinternaApagada() && dist <= distanciaObservacion * 0.8f)
            IniciarAtaque();
        else
            ReanudarCaceria();
    }

    void IniciarAdvertenciaAtaque()
    {
        estado = Estado.AdvertenciaAtaque;
        timerAdvertencia = 0f;
        advertenciaEnCurso = false;
        agent.isStopped = true;
        agent.ResetPath();
    }

    bool TryReproducirStrafe()
    {
        Vector3 toPlayer = jugador.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) return false;

        Vector3 forward = transform.forward;
        float dotSide = Vector3.Dot(transform.right, toPlayer.normalized);

        if (dotSide > 0.55f)
        {
            ReproducirAnim("straferight");
            return true;
        }

        if (dotSide < -0.55f)
        {
            ReproducirAnim("strafeleft");
            return true;
        }

        return false;
    }

    void ActualizarAtacando()
    {
        if (EstaIluminada())
            return;

        agent.isStopped = false;
        agent.speed = velocidadCorrer;
        AplicarParadaMuerte();
        agent.SetDestination(ObtenerPuntoFrenteJugador(distanciaMuerte));

        string animCarrera = locomotionActual;
        if (string.IsNullOrEmpty(animCarrera) || !EsAnimDePool(animCarrera, carreras))
            EstablecerLocomotion(ElegirAleatoria(carreras, carreras[0]));
        else
            MantenerAnimLoop(animCarrera);

        SincronizarVelocidadAnim(agent.velocity.magnitude, velocidadCorrer);

        timerAnimAtaque += Time.deltaTime;
        float dist = DistanciaHorizontal(transform.position, jugador.position);
        bool enRango = dist <= distanciaMuerte + 0.55f;

        if (enRango)
        {
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
    }

    void ActualizarRetirada()
    {
        agent.isStopped = true;
        agent.ResetPath();

        string nombre = string.IsNullOrEmpty(animRetirada) ? "walkback" : animRetirada;
        EstablecerLocomotion(nombre);
        MantenerAnimLoop(nombre);
        SincronizarVelocidadAnim(velocidadRetirada, velocidadRetirada);

        if (!EstaIluminada())
        {
            FinalizarRetirada();
            return;
        }

        Vector3 retroceso = -transform.forward * velocidadRetirada * Time.deltaTime;
        if (agent != null && agent.enabled)
            agent.Move(retroceso);
    }

    void FinalizarRetirada()
    {
        tiempoIluminado = 0f;
        timerObservacion = tiempoAntesAtaque;
        timerRugido = intervaloRugido;
        estado = Estado.Observando;
        agent.isStopped = true;
        ReproducirAnim(ElegirAleatoria(idles));
    }

    void IniciarObservacion()
    {
        estado = Estado.Observando;
        timerObservacion = tiempoAntesAtaque;
        timerCambioIdle = 0f;
        timerRugido = intervaloRugido;
        agent.isStopped = true;
        agent.ResetPath();
        ReproducirAnim(ElegirAleatoria(idles));
    }

    void IniciarAtaque()
    {
        if (EstaIluminada()) return;

        if (!string.IsNullOrEmpty(animRage))
        {
            estado = Estado.Rage;
            timerRage = 0f;
            agent.isStopped = true;
            ReproducirAnim(animRage);
            return;
        }

        estado = Estado.Atacando;
        timerAnimAtaque = 0f;
        ataqueAnimIniciado = false;
        muerteIniciada = false;
        agent.isStopped = false;
        agent.speed = velocidadCorrer;
        AplicarParadaMuerte();
        agent.SetDestination(ObtenerPuntoFrenteJugador(distanciaMuerte));
    }

    void IniciarGolpeLuz()
    {
        if (estado == Estado.GolpeLuz || estado == Estado.Retirandose) return;

        estado = Estado.GolpeLuz;
        timerGolpeLuz = 0f;
        agent.isStopped = true;
        golpeLuzElegido = ElegirAleatoria(golpesLuz, "gethit1");
        ReproducirAnim(golpeLuzElegido);
    }

    void IniciarRetirada()
    {
        if (estado == Estado.Retirandose) return;

        estado = Estado.Retirandose;
        agent.isStopped = true;
        agent.ResetPath();
        animActual = "";
        ReproducirAnim(string.IsNullOrEmpty(animRetirada) ? "walkback" : animRetirada);
    }

    void ReanudarCaceria()
    {
        ReanudarCaceriaTrasSpawn();
    }

    void ReanudarCaceriaTrasSpawn()
    {
        estado = Estado.Cazando;
        agent.isStopped = false;
        agent.speed = velocidadCaminar;
        timerDestino = intervaloActualizarDestino;
        timerCambioWalk = 0f;
        locomotionActual = "";
        muerteIniciada = false;
        timerRugido = 0f;
        ataqueAnimIniciado = false;
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
            Debug.LogWarning("MonsterController: no hay punto de spawn válido.");
            yield break;
        }

        agent.enabled = true;
        agent.Warp(spawn);
        agent.isStopped = false;
        agent.speed = velocidadCaminar;
        agent.SetDestination(jugador.position);

        locomotionActual = "";
        muerteIniciada = false;
        timerRugido = 0f;
        SetVisible(true);
        ReproducirSonidoAparicion();

        if (Random.value <= probabilidadSaltoSpawn && !string.IsNullOrEmpty(animSaltoSpawn))
        {
            estado = Estado.SaltoSpawn;
            timerSaltoSpawn = 0f;
            agent.isStopped = true;
            ReproducirAnim(animSaltoSpawn);
        }
        else
        {
            estado = Estado.Cazando;
            timerDestino = 0f;
            ReproducirAnim(ElegirAleatoria(caminatas));
        }

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

    bool ObtenerPuntoSpawn(out Vector3 posicion)
    {
        if (puntosSpawn != null && puntosSpawn.Length > 0)
        {
            for (int intento = 0; intento < 20; intento++)
            {
                Transform punto = puntosSpawn[Random.Range(0, puntosSpawn.Length)];
                if (punto == null) continue;

                Vector3 candidato = punto.position + Random.insideUnitSphere * 3f;
                candidato.y = punto.position.y;

                if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, 6f, NavMesh.AllAreas)
                    && EsSpawnValido(hit.position))
                {
                    posicion = hit.position;
                    ultimaPosicionSpawn = posicion;
                    return true;
                }
            }
        }

        Vector3 mirada = camara.forward;
        mirada.y = 0f;
        if (mirada.sqrMagnitude < 0.001f)
            mirada = -jugador.forward;
        mirada.Normalize();

        for (int i = 0; i < 30; i++)
        {
            float angulo = Random.Range(90f, 270f);
            Vector3 dir = Quaternion.Euler(0f, angulo, 0f) * mirada;
            float dist = Random.Range(distanciaMinSpawn, distanciaMaxSpawn);
            Vector3 candidato = jugador.position + dir * dist;

            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, distanciaMaxSpawn, NavMesh.AllAreas)
                && EsSpawnValido(hit.position))
            {
                posicion = hit.position;
                ultimaPosicionSpawn = posicion;
                return true;
            }
        }

        if (NavMesh.SamplePosition(jugador.position, out NavMeshHit fallback, distanciaMaxSpawn, NavMesh.AllAreas)
            && EsSpawnValido(fallback.position))
        {
            posicion = fallback.position;
            ultimaPosicionSpawn = posicion;
            return true;
        }

        posicion = jugador.position;
        return false;
    }

    bool EsSpawnValido(Vector3 pos)
    {
        if (Mathf.Abs(pos.y - jugador.position.y) > diferenciaAlturaMax)
            return false;
        if (Vector3.Distance(pos, jugador.position) < distanciaMinSpawn)
            return false;
        if (Vector3.Distance(pos, ultimaPosicionSpawn) < 5f)
            return false;
        return true;
    }

    void ActualizarRotacion()
    {
        Vector3 dir = Vector3.zero;

        if ((estado == Estado.Retirandose && mirarAlJugadorAlHuir)
            || estado == Estado.Observando
            || estado == Estado.GolpeLuz
            || estado == Estado.AdvertenciaAtaque)
        {
            dir = jugador.position - transform.position;
        }
        else if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
        {
            dir = agent.velocity;
        }
        else if (jugador != null)
        {
            dir = jugador.position - transform.position;
        }

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

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
        if (animator == null || string.IsNullOrEmpty(nombre)) return;
        if (animActual == nombre && EstaEnAnim(nombre)) return;

        animActual = nombre;
        animator.Play(nombre, 0, 0f);
    }

    void MantenerAnimLoop(string nombre)
    {
        if (animator == null || string.IsNullOrEmpty(nombre)) return;

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
        if (sonidoAparicion == null) return;

        if (sonidoAparicionGlobal)
            ReproducirSonido2D(sonidoAparicion, volumenAparicion);
        else if (audioSource != null)
            audioSource.PlayOneShot(sonidoAparicion, volumenAparicion);
    }

    void ActualizarRugido()
    {
        if (sonidoObservacion == null)
            return;

        timerRugido += Time.deltaTime;
        if (timerRugido >= intervaloRugido)
        {
            timerRugido = 0f;
            ReproducirSonidoObservacion();
        }
    }

    void ReproducirSonidoObservacion()
    {
        if (sonidoObservacion == null) return;

        if (sonidoObservacionGlobal)
            ReproducirSonido2D(sonidoObservacion, volumenObservacion);
        else if (audioSource != null)
        {
            audioSource.spatialBlend = 1f;
            audioSource.PlayOneShot(sonidoObservacion, volumenObservacion);
        }
        else
            AudioSource.PlayClipAtPoint(sonidoObservacion, transform.position, volumenObservacion);
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
        if (estado == Estado.Inactivo || !LuzJugadorEncendida()) return false;

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
            if (EsParteDelMonstruo(hit.transform)) return true;
            if (hit.distance < distObjetivo - 0.4f) return false;
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
}
