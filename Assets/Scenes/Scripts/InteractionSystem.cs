using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionSystem : MonoBehaviour
{
    public float rangoInteraccion = 3f;
    public LayerMask capaInteractuable;

    [Header("UI")]
    public GameObject textoInteraccion;

    private OutlineEffect outlineActual = null;

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
            IntentarInteractuar();
        ActualizarOutline();
    }

    void ActualizarOutline()
    {
        if (GameState.UIAbierta) // No mostrar outline si hay UI
        {
            if (outlineActual != null) outlineActual.OcultarOutline();
            outlineActual = null;
            if (textoInteraccion != null) textoInteraccion.SetActive(false);
            return;
        }
        Collider[] cercanos = Physics.OverlapSphere(
            transform.position, rangoInteraccion, capaInteractuable);

        OutlineEffect outlineMasCercano = null;
        float menorDist = float.MaxValue;

        foreach (Collider col in cercanos)
        {
            // Ignorar colliders del propio jugador
            if (col.transform.IsChildOf(transform) || col.transform == transform) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < menorDist)
            {
                OutlineEffect oe = col.GetComponentInParent<OutlineEffect>();
                if (oe != null)
                {
                    menorDist = dist;
                    outlineMasCercano = oe;
                }
            }
        }

        if (outlineMasCercano != outlineActual)
        {
            if (outlineActual != null) outlineActual.OcultarOutline();
            outlineActual = outlineMasCercano;
            if (outlineActual != null) outlineActual.MostrarOutline();
        }

        if (textoInteraccion != null)
            textoInteraccion.SetActive(outlineMasCercano != null);
    }

    void IntentarInteractuar()
    {
        if (GameState.UIAbierta) return;

        // 1. BUSCAR PUERTAS Y COMPROBAR SI ESTÁN CERRADAS
        Collider[] cercanos = Physics.OverlapSphere(transform.position, rangoInteraccion, capaInteractuable);
        foreach (Collider col in cercanos)
        {
            DoorController puerta = col.GetComponentInParent<DoorController>();
            if (puerta != null)
            {
                // Si la puerta existe y ESTÁ CERRADA, intentamos abrirla y salimos
                if (!puerta.EstaAbierta())
                {
                    puerta.IntentarAbrir();
                    return; // AQUÍ ES DONDE SE CORTA EL CÓDIGO. No busca llaves si la puerta sigue cerrada.
                }
            }
        }

        // 2. SI NO HUBO PUERTAS CERRADAS, BUSCAMOS LA LLAVE
        ItemPickup[] todosLosItems = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
        foreach (ItemPickup item in todosLosItems)
        {
            if (Vector3.Distance(transform.position, item.transform.position) <= rangoInteraccion)
            {
                item.Recoger();
                return;
            }
        }
    }
}