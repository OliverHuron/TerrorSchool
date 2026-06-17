using UnityEngine;

[RequireComponent(typeof(Camera))]
public class NightVisionEffect : MonoBehaviour
{
    public float brillo = 9f;
    public float lift = 0.3f;
    public float ruido = 0.025f;

    Material material;

    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;

        Shader shader = Shader.Find("Hidden/NightVision");
        if (shader == null)
            return;

        material = new Material(shader);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        material.SetFloat("_Brightness", brillo);
        material.SetFloat("_Lift", lift);
        material.SetFloat("_Noise", ruido);
        Graphics.Blit(source, destination, material);
    }
}
