using UnityEngine;
using TMPro;

public class ItemPickup : MonoBehaviour
{
    public enum TipoItem { Bateria, Llave, Papel }

    [Header("Configuración")]
    public TipoItem tipo;
    public float recargaBateria = 30f;
    public string idLlave = "llave_sala1";
    public string textoPapel = "El código es 2908";

    [Header("UI Nota - Canvas World Space")]
    public GameObject panelNota;
    public TextMeshProUGUI textoUI;

    PlayerInventory inventario;

    void Start()
    {
        inventario = FindObjectOfType<PlayerInventory>();
        if (panelNota)
            panelNota.SetActive(false);
    }

    public void Recoger()
    {
        switch (tipo)
        {
            case TipoItem.Bateria:
                RecargarBaterias();
                Destroy(gameObject);
                break;
            case TipoItem.Llave:
                if (inventario != null)
                    inventario.AgregarLlave(idLlave);
                Destroy(gameObject);
                break;
            case TipoItem.Papel:
                if (panelNota.activeSelf)
                    panelNota.SetActive(false);
                else
                    MostrarNota();
                break;
        }
    }

    void RecargarBaterias()
    {
        FlashlightController linterna = FindObjectOfType<FlashlightController>();
        if (linterna != null)
            linterna.AddBattery(recargaBateria);

        CamcorderController camara = FindObjectOfType<CamcorderController>();
        if (camara != null)
            camara.AddBattery(recargaBateria);
    }

    void MostrarNota()
    {
        if (panelNota == null)
            return;

        textoUI.text = textoPapel;
        panelNota.SetActive(true);
    }
}
