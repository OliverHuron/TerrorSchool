using UnityEngine;
using TMPro;

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

    private bool abierta = false;
    private PlayerInventory inventario;

    void Start()
    {
        inventario = FindObjectOfType<PlayerInventory>();
        if (panelCodigo) panelCodigo.SetActive(false);
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
                else Debug.Log("Necesitas la llave: " + idLlaveNecesaria);
                break;

            case TipoPuerta.RequiereCodigo:
            case TipoPuerta.CajaFuerte:
                MostrarPanelCodigo();
                break;

            case TipoPuerta.LlaveDorada:
                if (inventario.TieneLlave("llave_dorada"))
                    EscaparJuego();
                else Debug.Log("Necesitas la llave dorada");
                break;
        }
    }

    // Llamado desde el botón OK del panel de código
    public void VerificarCodigo()
    {
        if (campoCodigo.text == codigoCorrecto)
        {
            panelCodigo.SetActive(false);
            Time.timeScale = 1f;

            if (tipo == TipoPuerta.CajaFuerte)
                SpawnLlaveDorada(); // La caja suelta la llave dorada
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
        Time.timeScale = 0f;
    }

    void Abrir()
    {
        abierta = true;
        // Animación simple: rotar la puerta 90°
        transform.Rotate(0, 90, 0);
        Debug.Log("Puerta abierta: " + gameObject.name);
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
}