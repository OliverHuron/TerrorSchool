using UnityEngine;

public class BotonPanel : MonoBehaviour
{
    public enum TipoBoton { Aceptar, Borrar }
    public TipoBoton tipo;
    public PanelCodigoController panel;

    public void Presionar()
    {
        if (tipo == TipoBoton.Aceptar)
            panel.Aceptar();
        else
            panel.Borrar();
    }
}