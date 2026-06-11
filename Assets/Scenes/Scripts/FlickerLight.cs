using UnityEngine;
using System.Collections;

public class FlickerLight : MonoBehaviour
{
    public Light luz;
    public float intervaloMin = 0.05f;
    public float intervaloMax = 0.3f;

    void Start()
    {
        luz = GetComponent<Light>();
        StartCoroutine(Parpadear());
    }

    IEnumerator Parpadear()
    {
        while (true)
        {
            luz.enabled = !luz.enabled;
            yield return new WaitForSeconds(
                Random.Range(intervaloMin, intervaloMax));
        }
    }
}