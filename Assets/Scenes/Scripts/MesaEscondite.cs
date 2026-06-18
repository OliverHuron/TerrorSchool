using UnityEngine;

/// <summary>
/// Añade zona de escondite bajo la mesa y deja solo la tapa con collider solido.
/// Add Component en la mesa → clic derecho → Configurar mesa escondite.
/// </summary>
public class MesaEscondite : MonoBehaviour
{
    [Header("Tapa de la mesa (collider solido)")]
    public float grosorTapa = 0.12f;

    [Header("Zona escondite (trigger)")]
    public Vector3 tamanoZona = new Vector3(1.1f, 0.65f, 1.6f);
    public Vector3 centroZona = new Vector3(0f, 0.32f, 0f);

    [ContextMenu("Configurar mesa escondite")]
    public void Configurar()
    {
        AjustarColliderTapa();
        CrearZonaEscondite();
    }

    void AjustarColliderTapa()
    {
        BoxCollider tapa = GetComponent<BoxCollider>();
        if (tapa == null)
            return;

        float baseLocalY = tapa.center.y - tapa.size.y * 0.5f;
        tapa.size = new Vector3(tapa.size.x, grosorTapa, tapa.size.z);
        tapa.center = new Vector3(
            tapa.center.x,
            baseLocalY + grosorTapa * 0.5f,
            tapa.center.z);
        tapa.isTrigger = false;
    }

    void CrearZonaEscondite()
    {
        Transform zona = transform.Find("ZonaEscondite");
        if (zona == null)
        {
            GameObject go = new GameObject("ZonaEscondite");
            go.transform.SetParent(transform, false);
            zona = go.transform;
        }

        BoxCollider trigger = zona.GetComponent<BoxCollider>();
        if (trigger == null)
            trigger = zona.gameObject.AddComponent<BoxCollider>();

        trigger.isTrigger = true;
        trigger.size = tamanoZona;
        trigger.center = centroZona;

        if (zona.GetComponent<HidingSpot>() == null)
            zona.gameObject.AddComponent<HidingSpot>();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.4f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(centroZona, tamanoZona);
    }
#endif
}
