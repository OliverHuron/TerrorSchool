using UnityEngine;

public class HorrorAmbience : MonoBehaviour
{
    [Header("Oscuridad")]
    public bool aplicarAlIniciar = true;
    public Color luzAmbiente = new Color(0.065f, 0.065f, 0.075f);
    [Range(0f, 0.15f)] public float intensidadLuzDireccional = 0.04f;
    public bool apagarLucesPuntuales = true;

    static bool yaAplicado;

    void Awake()
    {
        if (aplicarAlIniciar)
            AplicarOscuridadSuave();
    }

    public static void AplicarOscuridad()
    {
        if (yaAplicado)
            return;

        HorrorAmbience instancia = FindObjectOfType<HorrorAmbience>();
        if (instancia != null)
            instancia.Aplicar();
        else
            AplicarValores(new Color(0.065f, 0.065f, 0.075f), 0.04f, true);

        yaAplicado = true;
    }

    public static void AplicarOscuridadSuave()
    {
        AplicarOscuridad();
    }

    public void Aplicar()
    {
        AplicarValores(luzAmbiente, intensidadLuzDireccional, apagarLucesPuntuales);
        yaAplicado = true;
    }

    static void AplicarValores(Color ambiente, float direccional, bool apagarPuntuales)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambiente;
        RenderSettings.reflectionIntensity = 0.05f;

        Light[] luces = FindObjectsOfType<Light>();
        foreach (Light luz in luces)
        {
            if (luz.type == LightType.Directional)
                luz.intensity = direccional;
            else if (apagarPuntuales && luz.type != LightType.Directional)
                luz.enabled = false;
        }
    }
}
