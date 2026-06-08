using UnityEngine;

public class Pickable : MonoBehaviour
{
    [HideInInspector] public bool isHeld = false;

    private Renderer[] renderers;
    private Color originalColor;
    public Color highlightColor = Color.yellow;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void SetHighlight(bool on)
    {
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                if (on)
                    mat.SetColor("_EmissionColor", highlightColor * 0.5f);
                else
                    mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}