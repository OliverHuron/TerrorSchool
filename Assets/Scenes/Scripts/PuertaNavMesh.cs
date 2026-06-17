using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class PuertaNavMesh : MonoBehaviour
{
    NavMeshObstacle obstacle;
    DoorController doorController;
    BisagraDoor bisagraDoor;

    void Awake()
    {
        doorController = GetComponent<DoorController>();
        bisagraDoor = GetComponent<BisagraDoor>();
        ConfigurarObstaculo();
    }

    void Start()
    {
        ActualizarEstado();
    }

    public void NotificarApertura()
    {
        if (obstacle != null)
            obstacle.enabled = false;
    }

    public void ActualizarEstado()
    {
        if (obstacle == null)
            ConfigurarObstaculo();

        obstacle.enabled = !PuertaEstaAbierta();
    }

    void ConfigurarObstaculo()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
            obstacle = gameObject.AddComponent<NavMeshObstacle>();

        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.shape = NavMeshObstacleShape.Box;
        AjustarTamanoObstaculo();
    }

    bool PuertaEstaAbierta()
    {
        if (doorController != null && doorController.EstaAbierta())
            return true;
        if (bisagraDoor != null && bisagraDoor.EstaAbierta())
            return true;
        return false;
    }

    void AjustarTamanoObstaculo()
    {
        if (obstacle == null)
            return;

        Collider col = GetComponentInChildren<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            obstacle.center = transform.InverseTransformPoint(b.center);
            Vector3 local = transform.InverseTransformVector(b.size);
            obstacle.size = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            return;
        }

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            obstacle.size = new Vector3(1f, 2f, 0.2f);
            return;
        }

        Bounds bounds = renderer.bounds;
        obstacle.center = transform.InverseTransformPoint(bounds.center);
        Vector3 size = transform.InverseTransformVector(bounds.size);
        obstacle.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (obstacle == null)
            obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle != null)
            AjustarTamanoObstaculo();
    }
#endif
}
