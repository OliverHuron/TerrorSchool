#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class MutantRestorer
{
    const string RutaPrefab = "Assets/MonsterMutant 7/Prefab/Base mesh MonsterMutant7 skin1.prefab";

    [MenuItem("TerrorSchool/Restaurar Mutant 7 en escena")]
    static void RestaurarMutant()
    {
        GameObject existente = GameObject.Find("MutantWrapper");
        if (existente != null)
        {
            Selection.activeGameObject = existente;
            EditorUtility.DisplayDialog("Mutant 7", "MutantWrapper ya existe en la escena.", "OK");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontro el prefab:\n" + RutaPrefab, "OK");
            return;
        }

        GameObject wrapper = new GameObject("MutantWrapper");
        Undo.RegisterCreatedObjectUndo(wrapper, "Restaurar Mutant 7");

        NavMeshAgent agent = wrapper.AddComponent<NavMeshAgent>();
        agent.speed = 2.5f;
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.acceleration = 10f;
        agent.angularSpeed = 360f;

        MonsterController monster = wrapper.AddComponent<MonsterController>();
        wrapper.AddComponent<AudioSource>();

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        modelo.name = "MutantModelo";
        modelo.transform.SetParent(wrapper.transform, false);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;

        monster.modeloVisual = modelo.transform;
        monster.animator = modelo.GetComponentInChildren<Animator>();

        JumpscareEffect jumpscare = Object.FindFirstObjectByType<JumpscareEffect>();
        if (jumpscare != null)
            monster.jumpscare = jumpscare;

        Transform[] puntosSpawn = BuscarOCrearPuntos("MutantSpawn", "Spawn_Mutant");
        Transform[] puntosRuta = BuscarOCrearPuntos("MutantRuta", "Ruta_");
        monster.puntosSpawn = puntosSpawn;
        monster.puntosRuta = puntosRuta;

        if (puntosSpawn.Length > 0)
            wrapper.transform.position = puntosSpawn[0].position;
        else if (NavMesh.SamplePosition(Vector3.zero, out NavMeshHit hit, 50f, NavMesh.AllAreas))
            wrapper.transform.position = hit.position;

        Selection.activeGameObject = wrapper;
        EditorUtility.DisplayDialog(
            "Mutant 7 restaurado",
            "Se creo MutantWrapper con patrulla, deteccion directa y NavMesh.\n\n" +
            "Asigna Puntos Spawn y Puntos Ruta (empties) si aun no existen.\n\nPulsa Play y espera ~5 segundos.",
            "OK");
    }

    static Transform[] BuscarOCrearPuntos(string contenedorNombre, string prefijoHijo)
    {
        GameObject contenedor = GameObject.Find(contenedorNombre);
        if (contenedor == null)
        {
            contenedor = new GameObject(contenedorNombre);
            Undo.RegisterCreatedObjectUndo(contenedor, "Crear puntos mutant");
        }

        Transform[] existentes = contenedor.GetComponentsInChildren<Transform>(true);
        System.Collections.Generic.List<Transform> puntos = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in existentes)
        {
            if (t == contenedor.transform)
                continue;
            if (t.name.StartsWith(prefijoHijo))
                puntos.Add(t);
        }

        if (puntos.Count == 0)
        {
            GameObject punto = new GameObject(prefijoHijo + "01");
            Undo.RegisterCreatedObjectUndo(punto, "Crear punto mutant");
            punto.transform.SetParent(contenedor.transform, false);
            puntos.Add(punto.transform);
        }

        return puntos.ToArray();
    }

    [MenuItem("TerrorSchool/Desactivar zombies")]
    static void DesactivarZombies()
    {
        foreach (string nombre in new[] { "SpawnManager", "ZombieManager" })
        {
            GameObject go = GameObject.Find(nombre);
            if (go != null)
            {
                go.SetActive(false);
                EditorUtility.SetDirty(go);
            }
        }

        EditorUtility.DisplayDialog("Zombies", "Spawner desactivado.", "OK");
    }
}
#endif
