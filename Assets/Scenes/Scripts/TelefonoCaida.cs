using UnityEngine;

public class TelefonoCaida : MonoBehaviour
{
    private Rigidbody rb;
    private bool enElSuelo = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Si el teléfono está cayendo o quieto, forzamos sutilmente su rotación
        if (!enElSuelo && rb.linearVelocity.y < 0.1f)
        {
            // Rotación ideal: acostado plano (X: 0, Y: rotación actual, Z: 0)
            Quaternion rotacionObjetivo = Quaternion.Euler(0, transform.eulerAngles.y, 0);

            // Sopesa la rotación física hacia la posición plana de forma fluida
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 5f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Al tocar el suelo, congelamos los ejes X y Z para que no se voltee de lado por un rebote
        if (collision.gameObject.CompareTag("Suelo") || collision.gameObject.layer == 0)
        {
            enElSuelo = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Si alguien levanta el teléfono, vuelve a tener libertad física total
        enElSuelo = false;
        rb.constraints = RigidbodyConstraints.None;
    }
}
