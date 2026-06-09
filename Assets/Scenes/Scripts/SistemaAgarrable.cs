using UnityEngine;
using UnityEngine.InputSystem;

public class PickupSystem : MonoBehaviour
{
    public float pickupRange = 5f;
    public Transform holdPoint;
    public Light spotlight;
    public LayerMask pickupMask;

    private Agarrable heldObject = null;
    private Agarrable lastHighlighted = null;

    void Start()
    {
        if (spotlight != null)
            spotlight.enabled = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Resalta el objeto en la mira
        if (heldObject == null)
            CheckHighlight();

        if (keyboard.eKey.wasPressedThisFrame)
        {
            if (heldObject == null) TryPickup();
            else Drop();
        }

        if (keyboard.fKey.wasPressedThisFrame)
        {
            if (heldObject != null && spotlight != null)
                spotlight.enabled = !spotlight.enabled;
        }
    }

    void CheckHighlight()
    {
        Agarrable found = null;

        // SphereCast — radio de 0.3, más fácil apuntar
        if (Physics.SphereCast(transform.position, 0.3f, transform.forward,
            out RaycastHit hit, pickupRange, pickupMask))
        {
            Transform t = hit.collider.transform;
            while (t != null)
            {
                Agarrable p = t.GetComponent<Agarrable>();
                if (p != null) { found = p; break; }
                t = t.parent;
            }
        }

        if (lastHighlighted != null && lastHighlighted != found)
        {
            lastHighlighted.SetHighlight(false);
            lastHighlighted = null;
        }

        if (found != null && !found.isHeld)
        {
            found.SetHighlight(true);
            lastHighlighted = found;
        }
    }

    void TryPickup()
    {
        if (lastHighlighted != null)
        {
            lastHighlighted.SetHighlight(false);
            lastHighlighted = null;
        }

        if (Physics.SphereCast(transform.position, 0.3f, transform.forward,
            out RaycastHit hit, pickupRange, pickupMask))
        {
            Transform t = hit.collider.transform;
            Agarrable p = null;

            while (t != null)
            {
                p = t.GetComponent<Agarrable>();
                if (p != null) break;
                t = t.parent;
            }

            if (p != null)
            {
                heldObject = p;
                heldObject.isHeld = true;

                // Desactiva el Rigidbody completamente — sin gravedad, sin físicas
                Rigidbody rb = heldObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                }

                // Pega el teléfono al HoldPoint — posición y rotación fijas
                heldObject.transform.SetParent(holdPoint);
                heldObject.transform.localPosition = Vector3.zero;
                heldObject.transform.localRotation = Quaternion.identity;
                heldObject.transform.localScale = Vector3.one;
            }
        }
    }

    void Drop()
    {
        if (spotlight != null)
            spotlight.enabled = false;

        // Reactiva el Rigidbody al soltar
        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY
                           | RigidbodyConstraints.FreezeRotationZ;
        }

        heldObject.transform.SetParent(null);
        heldObject.isHeld = false;
        heldObject = null;
    }


    void FixedUpdate()
    {
        // SetParent maneja el seguimiento, FixedUpdate ya no necesario
    }
}