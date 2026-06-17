#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public static class SceneSetupFixer
{
    const string RutaVideo = "Assets/Videos/CamaraMuerte.mp4";

    [MenuItem("TerrorSchool/Arreglar jumpscare y linterna")]
    static void ArreglarJumpscareYLinterna()
    {
        int cambios = 0;

        FlashlightController linterna = Object.FindFirstObjectByType<FlashlightController>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (linterna == null && player != null)
        {
            Transform cam = player.transform.Find("Main Camera");
            Transform luzGo = cam != null ? cam.Find("Linterna") : null;

            if (luzGo != null)
            {
                linterna = Undo.AddComponent<FlashlightController>(luzGo.gameObject);
                cambios++;
            }
            else
            {
                linterna = Undo.AddComponent<FlashlightController>(player);
                cambios++;
            }
        }

        if (linterna != null)
        {
            linterna.ResolverReferencias();

            if (linterna.currentBattery <= 0f)
            {
                Undo.RecordObject(linterna, "Restaurar bateria linterna");
                linterna.currentBattery = linterna.maxBattery;
                cambios++;
            }

            if (player != null)
            {
                FlashlightController duplicado = player.GetComponent<FlashlightController>();
                if (duplicado != null && duplicado != linterna)
                {
                    Undo.DestroyObjectImmediate(duplicado);
                    cambios++;
                }
            }

            EditorUtility.SetDirty(linterna);
        }

        if (player != null)
        {
            Transform cam = player.transform.Find("Main Camera");
            if (cam != null)
            {
                Transform luz = cam.Find("Linterna");
                if (luz != null && !luz.gameObject.activeSelf)
                {
                    Undo.RecordObject(luz.gameObject, "Activar Linterna");
                    luz.gameObject.SetActive(true);
                    cambios++;
                }
            }
        }

        JumpscareEffect jumpscare = Object.FindFirstObjectByType<JumpscareEffect>();
        if (jumpscare != null)
        {
            Undo.RecordObject(jumpscare, "Arreglar Jumpscare");

            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(RutaVideo);
            if (clip != null)
            {
                jumpscare.clipMuerte = clip;
                jumpscare.preferirVideo = true;
                cambios++;
            }

            if (jumpscare.imagenSadako == null)
            {
                Image img = BuscarPorNombre<Image>("Jumpscareimage");
                if (img != null)
                    jumpscare.imagenSadako = img;
            }

            if (jumpscare.textoMuerto == null)
            {
                TMPro.TextMeshProUGUI txt = BuscarPorNombre<TMPro.TextMeshProUGUI>("TextoMuerto");
                if (txt != null)
                    jumpscare.textoMuerto = txt;
            }

            jumpscare.ResolverReferenciasPublico();
            EditorUtility.SetDirty(jumpscare);
        }

        MonsterController monstruo = Object.FindFirstObjectByType<MonsterController>();
        if (monstruo != null && jumpscare != null && monstruo.jumpscare == null)
        {
            Undo.RecordObject(monstruo, "Conectar jumpscare");
            monstruo.jumpscare = jumpscare;
            EditorUtility.SetDirty(monstruo);
            cambios++;
        }

        GameObject spawn = GameObject.Find("SpawnManager");
        if (spawn != null && spawn.activeSelf)
        {
            Undo.RecordObject(spawn, "Desactivar SpawnManager");
            spawn.SetActive(false);
            cambios++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "TerrorSchool",
            cambios > 0
                ? "Escena arreglada:\n- Linterna en el jugador\n- Video de muerte conectado\n- Referencias del jumpscare\n\nGuarda la escena (Ctrl+S)."
                : "Todo ya estaba bien configurado.",
            "OK");
    }

    static T BuscarPorNombre<T>(string nombre) where T : Component
    {
        foreach (T comp in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (comp.gameObject.name == nombre)
                return comp;
        }

        return null;
    }
}
#endif
