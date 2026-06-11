// ── InteractionSystem.cs (va en el jugador) ─────────────
using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    public float rangoInteraccion = 2.5f;
    public LayerMask capaInteractuable; // Crea un Layer llamado "Interactuable"

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            IntentarInteractuar();
    }

    void IntentarInteractuar()
    {
        // Raycast desde el centro de la cámara hacia adelante
        Ray rayo = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit golpe;

        if (Physics.Raycast(rayo, out golpe, rangoInteraccion, capaInteractuable))
        {
            // Intentar recoger item
            ItemPickup item = golpe.collider.GetComponent<ItemPickup>();
            if (item != null) { item.Recoger(); return; }

            // Intentar abrir puerta
            DoorController puerta = golpe.collider.GetComponent<DoorController>();
            if (puerta != null) { puerta.IntentarAbrir(); return; }
        }
    }
}