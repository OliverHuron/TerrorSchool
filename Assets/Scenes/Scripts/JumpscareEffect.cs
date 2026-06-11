using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class JumpscareEffect : MonoBehaviour
{
    public Image imagenSadako;
    public float duracionVisible = 0.8f;   // Cuánto dura la imagen
    public float velocidadFade = 3f;        // Velocidad de fade out

    private bool activo = false;

    public void Activar()
    {
        if (!activo)
            StartCoroutine(MostrarJumpscare());
    }

    IEnumerator MostrarJumpscare()
    {
        activo = true;

        // Mostrar imagen instantáneamente
        imagenSadako.gameObject.SetActive(true);
        Color c = imagenSadako.color;
        c.a = 1f;
        imagenSadako.color = c;

        yield return new WaitForSeconds(duracionVisible);

        // Fade out gradual
        while (c.a > 0)
        {
            c.a -= Time.deltaTime * velocidadFade;
            imagenSadako.color = c;
            yield return null;
        }

        imagenSadako.gameObject.SetActive(false);
        activo = false;
    }
}