using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(PuertaNavMesh))]
public class DoorController : MonoBehaviour
{
    public enum TipoPuerta { RequiereLlave, RequiereCodigo, CajaFuerte, LlaveDorada }

    [Header("Tipo")]
    public TipoPuerta tipo;
    public string idLlaveNecesaria = "llave_sala1";
    public string codigoCorrecto = "4782";

    [Header("Puerta de escape (llave dorada)")]
    public bool esPuertaEscape = false;

    [Header("UI Código")]
    public GameObject panelCodigo;
    public TMP_InputField campoCodigo;

    [Header("Sonido")]
    public AudioClip sonidoBloqueada;
    public AudioClip sonidoAbrir;
    public AudioSource audioSource;

    [Header("Animación")]
    public float duracionApertura = 3f;

    private bool abierta = false;
    private PlayerInventory inventario;
    // Añade esto en tu DoorController.cs
    public bool EstaAbierta()
    {
        return abierta;
    }

    void Start()
    {
        inventario = FindObjectOfType<PlayerInventory>();
        if (panelCodigo) panelCodigo.SetActive(false);
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void IntentarAbrir()
    {
        if (abierta) return;

        switch (tipo)
        {
            case TipoPuerta.RequiereLlave:
                if (inventario.TieneLlave(idLlaveNecesaria))
                {
                    inventario.UsarLlave(idLlaveNecesaria);
                    Abrir();
                }
                else
                {
                    ReproducirSonido(sonidoBloqueada);
                    Debug.Log("Necesitas la llave: " + idLlaveNecesaria);
                }
                break;

            case TipoPuerta.RequiereCodigo:
            case TipoPuerta.CajaFuerte:
                MostrarPanelCodigo();
                break;

            case TipoPuerta.LlaveDorada:
                if (inventario.TieneLlave("llave_dorada"))
                    EscaparJuego();
                else
                {
                    ReproducirSonido(sonidoBloqueada);
                    Debug.Log("Necesitas la llave dorada");
                }
                break;
        }
    }

    // Llamado desde el botón OK del panel de código
    public void VerificarCodigo()
    {
        if (campoCodigo.text == codigoCorrecto)
        {
            panelCodigo.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            GameState.UIAbierta = false;
            if (tipo == TipoPuerta.CajaFuerte)
                SpawnLlaveDorada();
            else
                Abrir();
        }
        else
        {
            Debug.Log("Código incorrecto");
            campoCodigo.text = "";
        }
    }

    void MostrarPanelCodigo()
    {
        if (panelCodigo == null) return;
        panelCodigo.SetActive(true);
        GameState.UIAbierta = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Abrir()
    {
        abierta = true;
        ReproducirSonido(sonidoAbrir);

        PuertaNavMesh navPuerta = GetComponent<PuertaNavMesh>();
        if (navPuerta != null)
            navPuerta.NotificarApertura();

        // Usar BisagraDoor si existe para animación suave
        BisagraDoor bisagra = GetComponent<BisagraDoor>();
        if (bisagra != null)
        {
            bisagra.Abrir();
            if (esPuertaEscape)
                StartCoroutine(MostrarWinDespues(bisagra.duracionApertura));
        }
        else
        {
            StartCoroutine(AnimarApertura());
            if (esPuertaEscape)
                StartCoroutine(MostrarWinDespues(duracionApertura));
        }
    }

    IEnumerator MostrarWinDespues(float espera)
    {
        yield return new WaitForSeconds(espera);
        MostrarWin();
    }

    IEnumerator AnimarApertura()
    {
        float duracion = duracionApertura;
        float tiempo = 0f;
        Quaternion rotacionInicial = transform.rotation;
        Quaternion rotacionFinal = rotacionInicial * Quaternion.Euler(0, 90, 0);

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;
            t = t * t * (3f - 2f * t); // suavizado SmoothStep
            transform.rotation = Quaternion.Lerp(rotacionInicial, rotacionFinal, t);
            yield return null;
        }

        transform.rotation = rotacionFinal;
    }
    void SpawnLlaveDorada()
    {
        // La llave dorada se agrega directamente al inventario
        FindObjectOfType<PlayerInventory>().AgregarLlave("llave_dorada");
        Debug.Log("¡Llave dorada conseguida!");
        // Opcional: instanciar un prefab de llave que puedas ver
    }

    void EscaparJuego()
    {
        Debug.Log("¡Escapaste! — aquí cargas escena de victoria");
        // UnityEngine.SceneManagement.SceneManager.LoadScene("Victoria");
    }
    public void MarcarAbierta()
    {
        abierta = true;
        PuertaNavMesh navPuerta = GetComponent<PuertaNavMesh>();
        if (navPuerta != null)
            navPuerta.NotificarApertura();
    }

    void ReproducirSonido(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }
    void MostrarWin()
    {
        GameObject canvasObj = new GameObject("CanvasWin");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject textoObj = new GameObject("TextoWin");
        textoObj.transform.SetParent(canvasObj.transform, false);

        TMPro.TextMeshProUGUI texto = textoObj.AddComponent<TMPro.TextMeshProUGUI>();
        texto.text = "WIN";
        texto.fontSize = 120;
        texto.alignment = TMPro.TextAlignmentOptions.Center;
        texto.color = Color.yellow;

        RectTransform rect = textoObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Time.timeScale = 0f;
    }
}