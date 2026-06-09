using UnityEngine;

public class Pickable : MonoBehaviour
{
    [HideInInspector] public bool isHeld = false;

    private Renderer[] renderers;
    public Color highlightColor = Color.yellow;

    // Guardaremos el color original de cada material para restaurarlo al apagar el brillo
    private System.Collections.Generic.List<Color> coloresOriginales = new System.Collections.Generic.List<Color>();

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();

        // Guardamos los colores originales antes de hacer cualquier cambio
        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var mat in r.materials)
            {
                coloresOriginales.Add(mat.color);
            }
        }
    }

    public void SetHighlight(bool on)
    {
        if (renderers == null) return;

        int index = 0;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var mat in r.materials)
            {
                if (on)
                {
                    // Cambia el color base directamente a amarillo
                    mat.color = highlightColor;
                }
                else
                {
                    // Restaura exactamente el color original que tenía el objeto
                    if (index < coloresOriginales.Count)
                    {
                        mat.color = coloresOriginales[index];
                    }
                }
                index++;
            }
        }
    }
}
