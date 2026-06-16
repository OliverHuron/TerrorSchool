using UnityEngine;
using System.Collections;

public class BisagraDoor : MonoBehaviour
{
    public float anguloApertura = 90f;
    public float duracionApertura = 3f;
    private bool abierta = false;

    public void Abrir()
    {
        if (!abierta)
            StartCoroutine(AnimarApertura());
    }

    IEnumerator AnimarApertura()
    {
        abierta = true;
        float duracion = duracionApertura;
        float tiempo = 0f;
        Quaternion rotacionInicial = transform.rotation;
        Quaternion rotacionFinal = rotacionInicial * Quaternion.Euler(0, anguloApertura, 0);

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;
            t = t * t * (3f - 2f * t);
            transform.rotation = Quaternion.Lerp(rotacionInicial, rotacionFinal, t);
            yield return null;
        }

        transform.rotation = rotacionFinal;

        // Desactivar teclas del panel
        PanelCodigoController panel = FindAnyObjectByType<PanelCodigoController>();
        if (panel != null)
            foreach (Tecla t in panel.GetComponentsInChildren<Tecla>())
                t.gameObject.layer = 0;

        // Sincronizar con DoorController
        DoorController dc = GetComponent<DoorController>();
        if (dc != null) dc.MarcarAbierta();
    }

    public bool EstaAbierta()
    {
        return abierta;
    }
}