using UnityEngine;

public class PhoneBattery : MonoBehaviour
{
    [Header("Materiales de batería (0=vacío, 6=lleno)")]
    public Material[] batteryMaterials;

    [Header("Referencia a la linterna")]
    public Light spotlight;

    [Header("Índice del submaterial de la pantalla")]
    public int screenMaterialIndex = 0;

    [Header("Batería")]
    public float maxBattery = 100f;
    public float drainRate = 1f;

    private float currentBattery;
    private MeshRenderer meshRenderer;

    [Header("Renderer de la pantalla del teléfono")]
    public MeshRenderer screenRenderer;

    void Start()
    {
        if (screenRenderer == null)
            screenRenderer = GetComponent<MeshRenderer>();
        currentBattery = maxBattery; // ¿maxBattery es 100?
        meshRenderer = GetComponent<MeshRenderer>();
        UpdateBatteryVisual();
    }

    void Update()
    {
        if (spotlight == null) return;

        if (spotlight.enabled && currentBattery > 0f)
        {
            currentBattery -= drainRate * Time.deltaTime;
            currentBattery = Mathf.Max(currentBattery, 0f);
            UpdateBatteryVisual();

            if (currentBattery <= 0f)
                spotlight.enabled = false;
        }
        Debug.Log($"Batería: {currentBattery} | Linterna: {spotlight.enabled}");

    }

    void UpdateBatteryVisual()
    {
        if (batteryMaterials == null || batteryMaterials.Length == 0) return;

        float pct = currentBattery / maxBattery;
        int index = Mathf.RoundToInt(pct * (batteryMaterials.Length - 1));
        index = Mathf.Clamp(index, 0, batteryMaterials.Length - 1);

        Debug.Log($"Batería: {currentBattery} | Índice: {index}");

        Material[] mats = screenRenderer.materials;
        mats[screenMaterialIndex] = batteryMaterials[index];
        screenRenderer.materials = mats;
    }
    public void CargarBateria(float cantidad)
    {
        currentBattery = Mathf.Min(currentBattery + cantidad, maxBattery);
        UpdateBatteryVisual();
    }
}