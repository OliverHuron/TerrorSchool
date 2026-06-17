using UnityEngine;

[RequireComponent(typeof(Camera))]
public class NightVisionRenderBoost : MonoBehaviour
{
    public Color ambienteDuranteRender = new Color(0.9f, 0.9f, 0.92f);
    public float intensidadLuzDireccional = 0.65f;

    Color ambienteGuardado;
    Light luzDireccional;
    float intensidadGuardada;
    bool teniaLuz;
    bool restaurar;

    void OnPreRender()
    {
        Camera cam = GetComponent<Camera>();
        if (Camera.current != cam || cam.targetTexture == null)
            return;

        ambienteGuardado = RenderSettings.ambientLight;
        RenderSettings.ambientLight = ambienteDuranteRender;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        luzDireccional = null;
        foreach (Light luz in FindObjectsOfType<Light>())
        {
            if (luz.type != LightType.Directional || !luz.enabled)
                continue;

            luzDireccional = luz;
            intensidadGuardada = luz.intensity;
            luz.intensity = intensidadLuzDireccional;
            teniaLuz = true;
            break;
        }

        restaurar = true;
    }

    void OnPostRender()
    {
        if (!restaurar)
            return;

        RenderSettings.ambientLight = ambienteGuardado;

        if (teniaLuz && luzDireccional != null)
            luzDireccional.intensity = intensidadGuardada;

        teniaLuz = false;
        restaurar = false;
    }
}
