using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;

public class JumpscareEffect : MonoBehaviour
{
    const string RutaVideo = "Assets/Videos/CamaraMuerte.mp4";

    [Header("Imagenes (fallback si no hay video)")]
    public Image imagenSadako;
    public Image imagenManos;

    [Header("Video muerte")]
    [Tooltip("Arrastra aqui tu .mp4 (CamaraMuerte, etc.)")]
    public VideoClip clipMuerte;
    public RawImage pantallaVideo;
    public bool preferirVideo = true;
    public bool audioDelVideo = true;

    [Header("Texto")]
    public TextMeshProUGUI textoMuerto;

    [Header("Tiempos imagen (si no hay video)")]
    public float duracionVisible = 0.8f;
    public float velocidadFade = 3f;

    [Header("Sonido imagen (si no hay video)")]
    public AudioClip sonidoJumpscare;
    public AudioSource audioSource;

    VideoPlayer reproductor;
    RenderTexture texturaVideo;
    bool activo;
    bool videoPreparado;

    void Awake()
    {
        ResolverReferencias();
        ConfigurarReproductor();
        OcultarTodoAlInicio();
    }

    public void ResolverReferenciasPublico()
    {
        ResolverReferencias();
        ConfigurarReproductor();
    }

    void Start()
    {
        StartCoroutine(PrepararVideoAlInicio());
    }

    void ResolverReferencias()
    {
        if (clipMuerte == null)
        {
#if UNITY_EDITOR
            clipMuerte = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(RutaVideo);
#endif
            if (clipMuerte == null)
                clipMuerte = Resources.Load<VideoClip>("CamaraMuerte");
        }

        if (imagenSadako == null)
            imagenSadako = BuscarComponentePorNombre<Image>("Jumpscareimage");

        if (textoMuerto == null)
            textoMuerto = BuscarComponentePorNombre<TextMeshProUGUI>("TextoMuerto");

        if (pantallaVideo == null)
            pantallaVideo = BuscarRawImagePantalla();

        if (pantallaVideo == null)
            CrearPantallaVideo();
    }

    static T BuscarComponentePorNombre<T>(string nombre) where T : Component
    {
        foreach (T comp in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (comp.gameObject.name == nombre)
                return comp;
        }

        return null;
    }

    RawImage BuscarRawImagePantalla()
    {
        foreach (RawImage raw in FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = raw.gameObject.name.ToLowerInvariant();
            if (n.Contains("pantalla") && n.Contains("video"))
                return raw;
            if (n.Contains("videomuerte"))
                return raw;
        }

        return null;
    }

    void CrearPantallaVideo()
    {
        Canvas canvas = null;
        if (imagenSadako != null)
            canvas = imagenSadako.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        GameObject go = new GameObject("PantallaVideoMuerte");
        go.transform.SetParent(canvas.transform, false);
        pantallaVideo = go.AddComponent<RawImage>();
        pantallaVideo.color = Color.white;
        pantallaVideo.raycastTarget = false;

        RectTransform rt = pantallaVideo.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.transform.SetAsLastSibling();
        go.SetActive(false);
    }

    IEnumerator PrepararVideoAlInicio()
    {
        if (clipMuerte == null)
            yield break;

        ConfigurarReproductor();
        if (reproductor == null)
            yield break;

        reproductor.Prepare();
        while (!reproductor.isPrepared)
            yield return null;

        videoPreparado = true;
    }

    void ConfigurarReproductor()
    {
        if (clipMuerte == null)
            return;

        reproductor = GetComponent<VideoPlayer>();
        if (reproductor == null)
            reproductor = gameObject.AddComponent<VideoPlayer>();

        reproductor.playOnAwake = false;
        reproductor.isLooping = false;
        reproductor.skipOnDrop = true;
        reproductor.waitForFirstFrame = true;
        reproductor.clip = clipMuerte;

        int ancho = clipMuerte.width > 0 ? (int)clipMuerte.width : 1920;
        int alto = clipMuerte.height > 0 ? (int)clipMuerte.height : 1080;

        if (pantallaVideo != null)
        {
            if (texturaVideo == null || texturaVideo.width != ancho || texturaVideo.height != alto)
            {
                if (texturaVideo != null)
                    texturaVideo.Release();

                texturaVideo = new RenderTexture(ancho, alto, 0);
                texturaVideo.Create();
            }

            reproductor.renderMode = VideoRenderMode.RenderTexture;
            reproductor.targetTexture = texturaVideo;
            pantallaVideo.texture = texturaVideo;
        }
        else if (Camera.main != null)
        {
            reproductor.renderMode = VideoRenderMode.CameraFarPlane;
            reproductor.targetCamera = Camera.main;
            reproductor.aspectRatio = VideoAspectRatio.FitHorizontally;
        }

        reproductor.audioOutputMode = audioDelVideo
            ? VideoAudioOutputMode.Direct
            : VideoAudioOutputMode.None;
    }

    void OcultarTodoAlInicio()
    {
        if (imagenSadako != null)
            imagenSadako.gameObject.SetActive(false);
        if (imagenManos != null)
            imagenManos.gameObject.SetActive(false);
        if (pantallaVideo != null)
            pantallaVideo.gameObject.SetActive(false);
        if (textoMuerto != null)
            textoMuerto.gameObject.SetActive(false);
    }

    public void Activar()
    {
        if (!activo)
            StartCoroutine(SecuenciaMuerte());
    }

    public float ObtenerDuracionMuerte()
    {
        if (UsarVideo())
            return (float)clipMuerte.length + 2f;

        return duracionVisible + 0.5f + 1.5f + 0.5f;
    }

    bool UsarVideo()
    {
        return preferirVideo && clipMuerte != null;
    }

    void EstirarPantallaVideo()
    {
        if (pantallaVideo == null)
            return;

        RectTransform rt = pantallaVideo.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        Canvas canvas = pantallaVideo.GetComponentInParent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = 999;
    }

    IEnumerator SecuenciaMuerte()
    {
        activo = true;
        ResolverReferencias();
        OcultarTodoAlInicio();

        if (UsarVideo())
            yield return ReproducirVideoMuerte();
        else
            yield return ReproducirImagenMuerte();

        if (textoMuerto != null)
        {
            textoMuerto.gameObject.SetActive(true);
            SetAlphaTexto(textoMuerto, 0f);
            yield return StartCoroutine(FadeInTexto(textoMuerto, 1.5f));
        }

        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 0f;
    }

    IEnumerator ReproducirVideoMuerte()
    {
        ConfigurarReproductor();

        if (reproductor == null)
        {
            yield return ReproducirImagenMuerte();
            yield break;
        }

        if (pantallaVideo != null)
        {
            pantallaVideo.gameObject.SetActive(true);
            EstirarPantallaVideo();
        }

        reproductor.Stop();
        reproductor.Prepare();

        while (!reproductor.isPrepared)
            yield return null;

        reproductor.time = 0;
        reproductor.Play();

        float duracion = (float)clipMuerte.length + 0.25f;
        float t = 0f;
        while (t < duracion && (reproductor.isPlaying || t < clipMuerte.length))
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        reproductor.Stop();

        if (pantallaVideo != null)
            pantallaVideo.gameObject.SetActive(false);
    }

    IEnumerator ReproducirImagenMuerte()
    {
        if (imagenSadako != null)
        {
            imagenSadako.gameObject.SetActive(true);
            SetAlpha(imagenSadako, 1f);
        }

        if (sonidoJumpscare != null)
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
                audioSource.PlayOneShot(sonidoJumpscare);
            else
                AudioSource.PlayClipAtPoint(sonidoJumpscare, Camera.main.transform.position);
        }

        yield return new WaitForSeconds(duracionVisible);

        if (imagenSadako != null)
            yield return StartCoroutine(FadeOut(imagenSadako));

        if (imagenManos != null)
        {
            imagenManos.gameObject.SetActive(true);
            SetAlpha(imagenManos, 0f);
            yield return StartCoroutine(FadeIn(imagenManos, 1f));
        }
    }

    IEnumerator FadeOut(Image img)
    {
        Color c = img.color;
        while (c.a > 0)
        {
            c.a -= Time.deltaTime * velocidadFade;
            img.color = c;
            yield return null;
        }
        img.gameObject.SetActive(false);
    }

    IEnumerator FadeIn(Image img, float duracion)
    {
        Color c = img.color;
        c.a = 0f;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / duracion);
            img.color = c;
            yield return null;
        }
    }

    IEnumerator FadeInTexto(TextMeshProUGUI txt, float duracion)
    {
        Color c = txt.color;
        c.a = 0f;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duracion);
            txt.color = c;
            yield return null;
        }
    }

    void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    void SetAlphaTexto(TextMeshProUGUI txt, float a)
    {
        Color c = txt.color;
        c.a = a;
        txt.color = c;
    }

    void OnDestroy()
    {
        if (texturaVideo != null)
            texturaVideo.Release();
    }
}
