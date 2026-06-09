using UnityEngine;
using UnityEngine.InputSystem;

public class DetectorCargador : MonoBehaviour
{
    [Header("Configuración")]
    public float rangoDeteccion = 5f;
    public float radioDeteccion = 1.5f;
    public GameObject textoUI;

    [Header("Layers a ignorar (selecciona Agarrable)")]
    public LayerMask ignorarLayers;

    private Agarrable contactoMiradoActual;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        int maskFinal = ~ignorarLayers;
        bool encontrado = false;
        RaycastHit hit;

        // Intento 1 — SphereCast para distancia normal
        if (Physics.SphereCast(transform.position, radioDeteccion,
            transform.forward, out hit, rangoDeteccion, maskFinal))
        {
            encontrado = ProcesarHit(hit.collider.transform, keyboard);
        }

        // Intento 2 — OverlapSphere para cuando está muy cerca
        if (!encontrado)
        {
            Collider[] cercanos = Physics.OverlapSphere(
                transform.position + transform.forward * 0.5f,
                1.5f, maskFinal);

            foreach (var col in cercanos)
            {
                if (ProcesarHit(col.transform, keyboard))
                {
                    encontrado = true;
                    break;
                }
            }
        }

        if (!encontrado)
            LimpiarInteraccion();
    }

    bool ProcesarHit(Transform hitTransform, Keyboard keyboard)
    {
        Transform t = hitTransform;
        Agarrable agarrable = null;

        while (t != null)
        {
            if (t.CompareTag("Cargador"))
            {
                agarrable = t.GetComponent<Agarrable>();
                break;
            }
            t = t.parent;
        }

        if (agarrable == null) return false;

        if (contactoMiradoActual != agarrable)
        {
            LimpiarInteraccion();
            contactoMiradoActual = agarrable;
            contactoMiradoActual.SetHighlight(true);
        }

        if (textoUI != null) textoUI.SetActive(true);

        if (keyboard.rKey.wasPressedThisFrame)
            CargarBateria();

        return true;
    }

    void LimpiarInteraccion()
    {
        if (textoUI != null) textoUI.SetActive(false);

        if (contactoMiradoActual != null)
        {
            contactoMiradoActual.SetHighlight(false);
            contactoMiradoActual = null;
        }
    }

    void CargarBateria()
    {
        Debug.Log("Cargando batería...");
        PhoneBattery bateria = FindObjectOfType<PhoneBattery>();
        if (bateria != null)
            bateria.CargarBateria(30f);
    }
}