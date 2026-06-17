using UnityEngine;
using UnityEngine.UI;

public class CamcorderController : MonoBehaviour
{
    [Header("Bateria")]
    public float maxBattery = 100f;
    public float drainRate = 4f;
    [HideInInspector] public float currentBattery;
    public Slider batterySlider;

    [Header("Referencias")]
    public Camera camaraPrincipal;
    public Camera camaraLenteManual;
    public Renderer pantallaManual;
    public KeyCode teclaEncender = KeyCode.F;

    [Header("Escala")]
    public Vector3 escalaCamara = new Vector3(8f, 8f, 8f);
    public bool usarEscalaManual = true;

    [Header("Quad en la pantalla")]
    public Vector3 pantallaLocalPos = new Vector3(0f, 0.038f, -0.072f);
    public Vector3 pantallaLocalRot = new Vector3(0f, 180f, 0f);
    public Vector2 pantallaTamano = new Vector2(0.055f, 0.04f);

    [Header("Vision nocturna LCD")]
    public int resolucionPantalla = 512;
    public float brilloLCD = 10f;
    public float liftLCD = 0.12f;

    [Header("Ambiente")]
    public bool oscurecerEscenaAlIniciar = true;

    static readonly string[] NombresCamaraLente = { "CamaraLente", "CameraLens", "Camara View", "Camera View" };
    static readonly string[] NombresPantalla = { "PantallaLCD", "Pantalla", "LCD", "Screen" };

    Camera camaraLente;
    Renderer rendererPantalla;
    RenderTexture texturaCamara;
    Material materialPantalla;
    bool encendida;

    void OnValidate()
    {
        if (usarEscalaManual)
            transform.localScale = escalaCamara;
    }

    [ContextMenu("Reconfigurar camcorder")]
    public void ReconfigurarManualmente()
    {
        camaraLente = null;
        rendererPantalla = null;
        CrearSistemaPantalla();
    }

    void Awake()
    {
        if (camaraPrincipal == null)
            camaraPrincipal = GetComponentInParent<Camera>();

        if (usarEscalaManual)
            transform.localScale = escalaCamara;

        LimpiarComponentesIncorrectosEnRaiz();
        EliminarColliderDelModelo();
        CrearSistemaPantalla();

        if (Application.isPlaying && oscurecerEscenaAlIniciar)
            HorrorAmbience.AplicarOscuridadSuave();
    }

    void Start()
    {
        currentBattery = maxBattery;

        if (batterySlider == null)
        {
            GameObject slider = GameObject.Find("SliderBateria");
            if (slider != null)
                batterySlider = slider.GetComponent<Slider>();
        }

        ActualizarPantalla(false);
    }

    void Update()
    {
        if (usarEscalaManual)
            transform.localScale = escalaCamara;

        if (!Application.isPlaying)
            return;

        if (GameState.UIAbierta)
            return;

        if (Input.GetKeyDown(teclaEncender))
            ToggleCamcorder();

        if (encendida && currentBattery > 0f)
        {
            currentBattery -= drainRate * Time.deltaTime;
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
        }

        if (currentBattery <= 0f && encendida)
            ActualizarPantalla(false);

        if (batterySlider != null)
            batterySlider.value = currentBattery / maxBattery;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || !encendida)
            return;

        RenderizarVistaLCD();
    }

    void RenderizarVistaLCD()
    {
        if (camaraLente == null || camaraPrincipal == null || texturaCamara == null)
            return;

        camaraLente.CopyFrom(camaraPrincipal);
        camaraLente.targetTexture = texturaCamara;
        camaraLente.enabled = false;
        camaraLente.clearFlags = CameraClearFlags.Skybox;
        camaraLente.nearClipPlane = camaraPrincipal.nearClipPlane;
        camaraLente.farClipPlane = camaraPrincipal.farClipPlane;

        int capaIgnorada = LayerMask.NameToLayer("IgnorarInteraccion");
        if (capaIgnorada >= 0)
            camaraLente.cullingMask = camaraPrincipal.cullingMask & ~(1 << capaIgnorada);

        camaraLente.Render();

        if (materialPantalla != null)
            AsignarTexturaAlMaterial(materialPantalla, texturaCamara);

        if (rendererPantalla != null)
            rendererPantalla.material = materialPantalla;
    }

    void LimpiarComponentesIncorrectosEnRaiz()
    {
        Camera camRaiz = GetComponent<Camera>();
        if (camRaiz != null)
            Liberar(camRaiz);

        NightVisionEffect fx = GetComponent<NightVisionEffect>();
        if (fx != null)
            Liberar(fx);

        NightVisionRenderBoost boost = GetComponent<NightVisionRenderBoost>();
        if (boost != null)
            Liberar(boost);
    }

    void CrearSistemaPantalla()
    {
        CrearTextura();
        CrearMaterialPantalla();
        CrearCamaraLente();
        CrearQuadPantalla();
    }

    void CrearTextura()
    {
        if (texturaCamara != null && texturaCamara.width == resolucionPantalla)
            return;

        if (texturaCamara != null)
            Liberar(texturaCamara);

        texturaCamara = new RenderTexture(resolucionPantalla, resolucionPantalla, 24);
        texturaCamara.antiAliasing = 1;
        texturaCamara.Create();
    }

    void CrearMaterialPantalla()
    {
        if (materialPantalla == null)
        {
            Shader shader = Shader.Find("NightVision/LCD");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            materialPantalla = new Material(shader);
        }

        AsignarTexturaAlMaterial(materialPantalla, texturaCamara);
        if (materialPantalla.HasProperty("_Brightness"))
            materialPantalla.SetFloat("_Brightness", brilloLCD);
        if (materialPantalla.HasProperty("_Lift"))
            materialPantalla.SetFloat("_Lift", liftLCD);
    }

    static void AsignarTexturaAlMaterial(Material mat, Texture tex)
    {
        if (mat == null || tex == null)
            return;

        mat.mainTexture = tex;
        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", tex);
    }

    void CrearCamaraLente()
    {
        if (camaraLenteManual != null)
            camaraLente = camaraLenteManual;
        else
            camaraLente = BuscarCamaraLente();

        if (camaraLente == null)
        {
            GameObject go = new GameObject("CamaraLente");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            camaraLente = go.AddComponent<Camera>();
        }

        if (camaraLente.GetComponent<NightVisionRenderBoost>() == null)
            camaraLente.gameObject.AddComponent<NightVisionRenderBoost>();

        camaraLente.enabled = false;
        camaraLente.targetTexture = texturaCamara;
        camaraLente.gameObject.layer = 0;

        AsignarCapaAlModelo();
    }

    Camera BuscarCamaraLente()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            foreach (string nombre in NombresCamaraLente)
            {
                if (t.name != nombre)
                    continue;

                Camera c = t.GetComponent<Camera>();
                if (c != null)
                    return c;
            }
        }

        foreach (Camera c in GetComponentsInChildren<Camera>(true))
        {
            if (c == camaraPrincipal)
                continue;

            return c;
        }

        return null;
    }

    void CrearQuadPantalla()
    {
        if (pantallaManual != null)
            rendererPantalla = pantallaManual;
        else
        {
            Transform existente = BuscarQuadPantalla();
            if (existente != null)
                rendererPantalla = existente.GetComponent<Renderer>();
        }

        if (rendererPantalla == null)
        {
            GameObject quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadGo.name = "PantallaLCD";
            quadGo.transform.SetParent(transform, false);

            Collider col = quadGo.GetComponent<Collider>();
            if (col != null)
                Liberar(col);

            rendererPantalla = quadGo.GetComponent<Renderer>();
        }

        Transform pantallaTransform = rendererPantalla.transform;
        pantallaTransform.localPosition = pantallaLocalPos;
        pantallaTransform.localRotation = Quaternion.Euler(pantallaLocalRot);
        pantallaTransform.localScale = new Vector3(pantallaTamano.x, pantallaTamano.y, 1f);

        rendererPantalla.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendererPantalla.receiveShadows = false;
        rendererPantalla.material = materialPantalla;
        rendererPantalla.gameObject.SetActive(false);
    }

    Transform BuscarQuadPantalla()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            foreach (string nombre in NombresPantalla)
            {
                if (t.name == nombre)
                    return t;
            }
        }

        return null;
    }

    void AsignarCapaAlModelo()
    {
        int capa = LayerMask.NameToLayer("IgnorarInteraccion");
        if (capa < 0)
            return;

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (camaraLente != null && t.gameObject == camaraLente.gameObject)
                continue;

            t.gameObject.layer = capa;
        }
    }

    void ToggleCamcorder()
    {
        if (!encendida && currentBattery <= 0f)
            return;

        ActualizarPantalla(!encendida);
    }

    void ActualizarPantalla(bool activa)
    {
        encendida = activa;

        if (camaraLente == null || rendererPantalla == null)
            CrearSistemaPantalla();

        if (rendererPantalla != null)
        {
            rendererPantalla.material = materialPantalla;
            rendererPantalla.gameObject.SetActive(activa);
        }

        if (activa)
            RenderizarVistaLCD();
    }

    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Clamp(currentBattery + amount, 0f, maxBattery);

        if (batterySlider != null)
            batterySlider.value = currentBattery / maxBattery;
    }

    public bool EstaEncendida()
    {
        return encendida && currentBattery > 0f;
    }

    void EliminarColliderDelModelo()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            Liberar(col);
    }

    void Liberar(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    void OnDestroy()
    {
        if (texturaCamara != null)
            Liberar(texturaCamara);

        if (materialPantalla != null)
            Liberar(materialPantalla);
    }
}
