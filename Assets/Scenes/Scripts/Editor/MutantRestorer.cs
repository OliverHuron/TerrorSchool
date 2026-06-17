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
        monster.animRetirada = "walkback";
        monster.mirarAlJugadorAlHuir = true;

        JumpscareEffect jumpscare = Object.FindFirstObjectByType<JumpscareEffect>();
        if (jumpscare != null)
            monster.jumpscare = jumpscare;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 spawn = player.transform.position - player.transform.forward * 14f;
            if (NavMesh.SamplePosition(spawn, out NavMeshHit hit, 18f, NavMesh.AllAreas))
                wrapper.transform.position = hit.position;
        }

        Selection.activeGameObject = wrapper;
        EditorUtility.DisplayDialog(
            "Mutant 7 restaurado",
            "Se creo MutantWrapper con walkback, rage, ataques y NavMesh.\n\nPulsa Play y espera ~5 segundos.",
            "OK");
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
