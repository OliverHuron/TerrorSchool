using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HidingSpot : MonoBehaviour
{
    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerMovement jugador = other.GetComponent<PlayerMovement>();
        if (jugador != null)
            jugador.EntrarEscondite();
    }

    void OnTriggerExit(Collider other)
    {
        PlayerMovement jugador = other.GetComponent<PlayerMovement>();
        if (jugador != null)
            jugador.SalirEscondite();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
            return;

        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        else if (col is SphereCollider sphere)
            Gizmos.DrawSphere(sphere.center, sphere.radius);
    }
#endif
}
