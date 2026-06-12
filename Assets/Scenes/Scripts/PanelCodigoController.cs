using UnityEngine;
using TMPro;

public class PanelCodigoController : MonoBehaviour
{
    [Header("Código — editable desde Inspector")]
    public string codigoCorrecto = "1234";
    public int maxDigitos = 4;

    [Header("Display")]
    public GameObject displayObj;
    private TextMeshPro textoDisplay;

    [Header("Puerta")]
    public BisagraDoor puerta;

    private string codigoActual = "";

    void Start()
    {
        if (displayObj != null)
            textoDisplay = displayObj.GetComponentInChildren<TextMeshPro>();
        ActualizarDisplay();
    }

    public void AgregarDigito(string digito)
    {
        if (codigoActual.Length >= maxDigitos) return;
        codigoActual += digito;
        ActualizarDisplay();
    }

    public void Borrar()
    {
        if (codigoActual.Length == 0) return;
        codigoActual = codigoActual.Substring(0, codigoActual.Length - 1);
        ActualizarDisplay();
    }

    public void Aceptar()
    {
        if (codigoActual.Length == 0) return;

        if (codigoActual == codigoCorrecto)
        {
            ActualizarDisplay("OK");
            if (puerta != null) puerta.Abrir();

            // Salir del modo panel al abrir la puerta
            PanelInteraction panel = GetComponentInParent<PanelInteraction>();
            if (panel == null) panel = FindObjectOfType<PanelInteraction>();
            if (panel != null) panel.ForzarSalir();
        }
        else
        {
            ActualizarDisplay("ERR");
            Invoke("LimpiarDisplay", 1f);
        }
    }

    void LimpiarDisplay()
    {
        codigoActual = "";
        ActualizarDisplay();
    }

    void ActualizarDisplay(string texto = null)
    {
        if (textoDisplay == null) return;
        textoDisplay.text = texto ?? codigoActual;
    }
}