using UnityEngine;
using UnityEngine.UI;

public class FlashlightController : MonoBehaviour
{
    [Header("Batería")]
    public float maxBattery = 100f;       // Batería máxima
    public float drainRate = 5f;           // Cuánto drena por segundo
    [HideInInspector] public float currentBattery;

    [Header("Luz")]
    public Light flashlight;               // Arrastra tu Spotlight aquí
    public Slider batterySlider;           // UI barra de batería

    void Start()
    {
        currentBattery = maxBattery;
    }

    void Update()
    {
        // Drenar batería mientras la linterna esté encendida
        if (flashlight.enabled && currentBattery > 0)
        {
            currentBattery -= drainRate * Time.deltaTime;
            currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);
        }

        // Apagar si se agota
        if (currentBattery <= 0)
            flashlight.enabled = false;

        // Actualizar UI
        if (batterySlider != null)
            batterySlider.value = currentBattery / maxBattery;

        // Encender/apagar con F
        if (Input.GetKeyDown(KeyCode.F))
            ToggleFlashlight();
    }

    void ToggleFlashlight()
    {
        if (currentBattery > 0)
            flashlight.enabled = !flashlight.enabled;
    }

    // Llamado desde ItemPickup cuando recoges batería
    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Clamp(currentBattery + amount, 0, maxBattery);
    }

    // El GhostController usa esto para saber si la luz lo apunta
    public bool IsLightOn()
    {
        return flashlight.enabled && currentBattery > 0;
    }
}