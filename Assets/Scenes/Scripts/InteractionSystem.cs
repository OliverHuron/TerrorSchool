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
        // Nuevo Input System
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("E presionado");
            IntentarInteractuar();
        }

        ActualizarOutline();
    }

    void ActualizarOutline()
    {
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
        Collider[] cercanos = Physics.OverlapSphere(
            transform.position, rangoInteraccion, capaInteractuable);

        if (cercanos.Length == 0) return;

        Collider masCercano = null;
        float menorDist = float.MaxValue;

        foreach (Collider col in cercanos)
        {
            // Ignorar colliders del propio jugador
            if (col.transform.IsChildOf(transform) || col.transform == transform) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < menorDist)
            {
                menorDist = dist;
                masCercano = col;
            }
        }

        if (masCercano == null) return;

        Debug.Log("Interactuando con: " + masCercano.gameObject.name);

        ItemPickup item = masCercano.GetComponentInParent<ItemPickup>();
        if (item != null) { item.Recoger(); return; }

        DoorController puerta = masCercano.GetComponentInParent<DoorController>();
        if (puerta != null) { puerta.IntentarAbrir(); return; }
    }
}