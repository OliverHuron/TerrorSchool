using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("Bateria")]
    public float maxBattery = 100f;
    public float drainRate = 5f;
    public float currentBattery;

    [Header("Luz")]
    public Light flashlight;
    public Slider batterySlider;

    void Awake()
    {
        ResolverReferencias();
    }

    void Start()
    {
        if (currentBattery <= 0f)
            currentBattery = maxBattery;

        if (flashlight != null && currentBattery > 0f)
            flashlight.enabled = true;
    }

    public void ResolverReferencias()
    {
        if (flashlight == null)
        {
            flashlight = GetComponent<Light>();

            Transform cam = transform;
            if (transform.CompareTag("Player"))
            {
                Transform mainCam = transform.Find("Main Camera");
                if (mainCam != null)
                    cam = mainCam;
            }

            Transform linterna = cam.Find("Linterna");
            if (linterna != null)
            {
                if (!linterna.gameObject.activeSelf)
                    linterna.gameObject.SetActive(true);

                flashlight = linterna.GetComponent<Light>();
            }
        }

        if (batterySlider == null)
        {
            GameObject slider = GameObject.Find("SliderBateria");
            if (slider != null)
                batterySlider = slider.GetComponent<Slider>();
        }
    }

    void Update()
    {
        if (flashlight == null)
            return;

        if (flashlight.enabled && currentBattery > 0f)
        {
            currentBattery -= drainRate * Time.deltaTime;
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
        }

        if (currentBattery <= 0f)
            flashlight.enabled = false;

        if (batterySlider != null)
            batterySlider.value = currentBattery / maxBattery;

        if (TeclaEncendidoPresionada())
            ToggleFlashlight();
    }

    static bool TeclaEncendidoPresionada()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(KeyCode.F);
    }

    void ToggleFlashlight()
    {
        if (flashlight == null)
            return;

        if (flashlight.enabled)
        {
            flashlight.enabled = false;
            return;
        }

        if (currentBattery > 0f)
            flashlight.enabled = true;
    }

    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Clamp(currentBattery + amount, 0f, maxBattery);

        if (batterySlider != null)
            batterySlider.value = currentBattery / maxBattery;

        if (currentBattery > 0f && flashlight != null && !flashlight.enabled)
            flashlight.enabled = true;
    }

    public bool IsLightOn()
    {
        return flashlight != null && flashlight.enabled && currentBattery > 0f;
    }
}
