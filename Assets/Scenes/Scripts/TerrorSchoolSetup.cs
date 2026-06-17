using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Asegura que el jugador tenga linterna y que el jumpscare tenga referencias al iniciar Play.
/// </summary>
public static class TerrorSchoolSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ConfigurarEscena()
    {
        ConfigurarLinterna();
        ConfigurarJumpscare();
    }

    static void ConfigurarLinterna()
    {
        FlashlightController linterna = Object.FindFirstObjectByType<FlashlightController>();
        if (linterna == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            linterna = player.AddComponent<FlashlightController>();
        }

        linterna.ResolverReferencias();
    }

    static void ConfigurarJumpscare()
    {
        JumpscareEffect jumpscare = Object.FindFirstObjectByType<JumpscareEffect>();
        if (jumpscare != null)
            jumpscare.ResolverReferenciasPublico();
    }
}
