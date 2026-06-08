using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightToggle : MonoBehaviour
{
    public Light spotlight;
    private bool isOn = true;

    void Start()
    {
        spotlight.enabled = false;
    }
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.fKey.wasPressedThisFrame)
        {
            isOn = !isOn;
            spotlight.enabled = isOn;
        }
    }
}