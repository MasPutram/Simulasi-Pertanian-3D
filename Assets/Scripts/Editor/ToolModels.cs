using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

// Model alat bergaya blocky, dibuat dari primitive supaya bisa diedit langsung di scene
public static class ToolModels
{
    const string MatDir = "Assets/Materials/Tools";

    [MenuItem("Farm/Tools/Build Watering Can")]
    public static void BuildWateringCan()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[ToolModels] Hentikan Play mode dulu."); return; }

        GameObject old = WateringFx.FindModel();
        if (old != null) Undo.DestroyObjectImmediate(old);

        Material body = Mat("WateringCan_Body", new Color(0.24f, 0.62f, 0.56f), 0.35f);
        Material dark = Mat("WateringCan_Dark", new Color(0.13f, 0.38f, 0.35f), 0.3f);
        Material metal = Mat("WateringCan_Metal", new Color(0.72f, 0.74f, 0.76f), 0.55f);

        GameObject root = new GameObject(WateringFx.ModelName);
        Undo.RegisterCreatedObjectUndo(root, "Build Watering Can");

        // Badan + detail (origin di dasar tengah, corong mengarah ke +X)
        Part(root, "Body", new Vector3(0, 0.23f, 0), new Vector3(0.5f, 0.42f, 0.36f), 0, body);
        Part(root, "Base", new Vector3(0, 0.02f, 0), new Vector3(0.54f, 0.04f, 0.4f), 0, dark);
        Part(root, "Rim", new Vector3(0, 0.45f, 0), new Vector3(0.54f, 0.05f, 0.4f), 0, dark);
        Part(root, "Lid", new Vector3(-0.06f, 0.49f, 0), new Vector3(0.3f, 0.04f, 0.26f), 0, metal);
        Part(root, "Stripe", new Vector3(0, 0.25f, 0), new Vector3(0.51f, 0.06f, 0.37f), 0, dark);

        // Pegangan melengkung di atas
        Part(root, "HandleBack", new Vector3(-0.2f, 0.58f, 0), new Vector3(0.06f, 0.22f, 0.06f), 0, dark);
        Part(root, "HandleFront", new Vector3(0.1f, 0.58f, 0), new Vector3(0.06f, 0.22f, 0.06f), 0, dark);
        Part(root, "HandleTop", new Vector3(-0.05f, 0.7f, 0), new Vector3(0.36f, 0.06f, 0.06f), 0, dark);

        // Corong + kepala penyiram
        Part(root, "Spout", new Vector3(0.4f, 0.33f, 0), new Vector3(0.46f, 0.07f, 0.07f), 38f, body);
        Part(root, "Rose", new Vector3(0.6f, 0.49f, 0), new Vector3(0.07f, 0.15f, 0.15f), 38f, metal);

        GameObject tip = new GameObject("SpoutTip");
        tip.transform.SetParent(root.transform, false);
        tip.transform.localPosition = new Vector3(0.65f, 0.52f, 0);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 pos = player != null ? player.transform.position + new Vector3(2.5f, -player.transform.position.y, 0) : Vector3.zero;
        root.transform.position = pos;
        root.SetActive(false); // seperti seed_*: model referensi, tidak tampil di game

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("[ToolModels] " + WateringFx.ModelName + " dibuat (nonaktif). Aktifkan untuk mengedit, nonaktifkan lagi setelahnya.");
    }

    static void Part(GameObject parent, string name, Vector3 pos, Vector3 size, float rotZ, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, 0, rotZ);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material Mat(string name, Color color, float gloss)
    {
        if (!Directory.Exists(MatDir)) Directory.CreateDirectory(MatDir);
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = color;
        m.SetFloat("_Glossiness", gloss);
        EditorUtility.SetDirty(m);
        return m;
    }
}
