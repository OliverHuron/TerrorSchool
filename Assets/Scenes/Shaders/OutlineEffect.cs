using UnityEngine;

public class OutlineEffect : MonoBehaviour
{
    public Material outlineMaterial;
    private Renderer[] renderers;
    private bool outlineActivo = false;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void MostrarOutline()
    {
        if (outlineActivo) return;
        outlineActivo = true;

        foreach (Renderer r in renderers)
        {
            // Agregar el material de outline sin quitar el original
            Material[] mats = r.materials;
            Material[] nuevosMats = new Material[mats.Length + 1];
            mats.CopyTo(nuevosMats, 0);
            nuevosMats[nuevosMats.Length - 1] = outlineMaterial;
            r.materials = nuevosMats;
        }
    }

    public void OcultarOutline()
    {
        if (!outlineActivo) return;
        outlineActivo = false;

        foreach (Renderer r in renderers)
        {
            // Quitar el último material (el outline)
            Material[] mats = r.materials;
            Material[] nuevosMats = new Material[mats.Length - 1];
            for (int i = 0; i < nuevosMats.Length; i++)
                nuevosMats[i] = mats[i];
            r.materials = nuevosMats;
        }
    }
}