using UnityEngine;
using TMPro;

public class ItemPickup : MonoBehaviour
{
    public enum TipoItem { Bateria, Llave, Papel }

    [Header("Configuración")]
    public TipoItem tipo;
    public float recargaBateria = 30f;     // Solo si tipo == Bateria
    public string idLlave = "llave_sala1"; // ID único de esta llave
    public string textoPapel = "El código es 4782"; // Texto de la nota

    [Header("UI Nota")]
    public GameObject panelNota;           // Panel UI que muestra el texto
    public TextMeshProUGUI textoUI;

    private FlashlightController linterna;
    private PlayerInventory inventario;
    private bool mostrandoNota = false;

    void Start()
    {
        linterna = FindObjectOfType<FlashlightController>();
        inventario = FindObjectOfType<PlayerInventory>();
        if (panelNota) panelNota.SetActive(false);
    }

    void Update()
    {
        // Cerrar nota con E
        if (mostrandoNota && Input.GetKeyDown(KeyCode.E))
        {
            panelNota.SetActive(false);
            mostrandoNota = false;
            Time.timeScale = 1f; // Reanudar juego
        }
    }

    // Se llama cuando el jugador presiona E cerca del objeto
    public void Recoger()
    {
        switch (tipo)
        {
            case TipoItem.Bateria:
                linterna.AddBattery(recargaBateria);
                Destroy(gameObject); // Desaparece al recoger
                break;

            case TipoItem.Llave:
                inventario.AgregarLlave(idLlave);
                Destroy(gameObject);
                break;

            case TipoItem.Papel:
                MostrarNota();
                break; // El papel NO desaparece, solo se lee
        }
    }

    void MostrarNota()
    {
        if (panelNota == null) return;
        textoUI.text = textoPapel;
        panelNota.SetActive(true);
        mostrandoNota = true;
        Time.timeScale = 0f; // Pausar mientras lees
    }
}