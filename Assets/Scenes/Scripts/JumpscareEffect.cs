using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class JumpscareEffect : MonoBehaviour
{
    [Header("Imùgenes")]
    public Image imagenSadako;
    public Image imagenManos;

    [Header("Texto")]
    public TextMeshProUGUI textoMuerto;

    [Header("Tiempos")]
    public float duracionVisible = 0.8f;
    public float velocidadFade = 3f;

    [Header("Sonido")]
    public AudioClip sonidoJumpscare;
    public AudioSource audioSource;

    private bool activo = false;

    public void Activar()
    {
        if (!activo)
            StartCoroutine(SecuenciaMuerte());
    }

    IEnumerator SecuenciaMuerte()
    {
        // 4 ù Mostrar "Has Muerto"
        if (textoMuerto != null)
        {
            Debug.Log("Activando texto Has Muerto");
            textoMuerto.gameObject.SetActive(true);
            Debug.Log("Texto activo: " + textoMuerto.gameObject.activeSelf);
            SetAlphaTexto(textoMuerto, 1f); // Cambiar a 1f directo sin fade para probar
            Debug.Log("Texto: " + textoMuerto.text + " | Alpha: " + textoMuerto.color.a);
        }
        activo = true;

        // 1 ù Mostrar Sadako instantùneo
        imagenSadako.gameObject.SetActive(true);
        SetAlpha(imagenSadako, 1f);

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

        // 2 ù Fade out Sadako
        yield return StartCoroutine(FadeOut(imagenSadako));

        // 3 ù Mostrar manos ensangrentadas
        if (imagenManos != null)
        {
            imagenManos.gameObject.SetActive(true);
            SetAlpha(imagenManos, 0f);
            yield return StartCoroutine(FadeIn(imagenManos, 1f));
        }

        // 4 ù Mostrar "Has Muerto"
        if (textoMuerto != null)
        {
            textoMuerto.gameObject.SetActive(true);
            SetAlphaTexto(textoMuerto, 0f);
            yield return StartCoroutine(FadeInTexto(textoMuerto, 1.5f));
        }

        // 5 ù Pausar el juego
        yield return new WaitForSeconds(0.5f);
        Time.timeScale = 0f;
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
}