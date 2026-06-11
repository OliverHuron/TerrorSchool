
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    // Lista de IDs de llaves que lleva el jugador
    private List<string> llaves = new List<string>();

    public void AgregarLlave(string id)
    {
        if (!llaves.Contains(id))
        {
            llaves.Add(id);
            Debug.Log("Llave recogida: " + id);
        }
    }

    public bool TieneLlave(string id)
    {
        return llaves.Contains(id);
    }

    public void UsarLlave(string id)
    {
        llaves.Remove(id); // La llave se consume al usarla
    }
}