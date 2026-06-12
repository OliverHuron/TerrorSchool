using UnityEngine;

public class Tecla : MonoBehaviour
{
    public string valor; // El número de esta tecla: "1", "2", etc.
    public PanelCodigoController panel;

    public void Presionar()
    {
        Debug.Log("Tecla presionada: " + valor);
        panel.AgregarDigito(valor);
    }
}