using UnityEngine;

public class Agarrable : MonoBehaviour
{
    [HideInInspector] public bool isHeld = false;

    private Renderer[] renderers;
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
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", highlightColor * 0.5f);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
}