using UnityEngine;

// Menampilkan barang yang dipegang player: benih di depan dada, buah di atas kepala.
// Pose lengan diatur PlayerAnimator.
public static class HandItem
{
    const float SeedSize = 0.32f;
    static readonly Vector3 SeedPos = new Vector3(0f, 1.0f, 1.0f); // di antara kedua tangan (lokal player)
    const float OverheadSize = 1.2f;
    static readonly Vector3 OverheadPos = new Vector3(0f, 1.76f, 0f); // di atas tangan yang terangkat

    // overhead = buah diangkat di atas kepala; selain itu benih dipegang ke depan
    public static void Hold(GameObject model, Color fallbackColor, bool overhead = false)
    {
        if (!Application.isPlaying) return; // jangan sampai tersimpan ke scene saat di Editor
        Clear();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float size = overhead ? OverheadSize : SeedSize;
        Vector3 basePos = overhead ? OverheadPos : SeedPos;

        // Susun dulu di ruang dunia tanpa rotasi supaya bounds akurat
        GameObject held = new GameObject("HeldItem");
        GameObject copy = MakeCopy(model, fallbackColor);
        copy.transform.SetParent(held.transform, false);
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.identity;

        Bounds b = CombinedBounds(copy);
        float maxSize = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (maxSize > 0) copy.transform.localScale *= size / maxSize;
        b = CombinedBounds(copy);
        // overhead: dasar model di titik pegang; depan: model di tengah titik pegang
        float yRef = overhead ? b.min.y : b.center.y;
        copy.transform.localPosition -= new Vector3(b.center.x, yRef, b.center.z);

        held.transform.SetParent(player.transform, false);
        held.transform.localPosition = basePos;
        held.transform.localRotation = Quaternion.identity;
        PlayerAnimator.heldBasePos = basePos;
    }

    static GameObject MakeCopy(GameObject model, Color fallbackColor)
    {
        GameObject copy;
        if (model != null)
        {
            copy = Object.Instantiate(model);
            copy.SetActive(true);
            foreach (var col in copy.GetComponentsInChildren<Collider>())
                Object.Destroy(col);
            foreach (var l in copy.GetComponentsInChildren<Light>())
                Object.Destroy(l);
        }
        else
        {
            copy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(copy.GetComponent<Collider>());
            copy.GetComponent<Renderer>().material.color = fallbackColor;
        }
        copy.name = "Model";
        return copy;
    }

    static Bounds CombinedBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    public static void Clear()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        Transform held = FindChildRecursive(player.transform, "HeldItem");
        if (held != null)
        {
            held.name = "HeldItem_old";
            Object.Destroy(held.gameObject);
        }
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    public static GameObject FindSceneObject(string name)
    {
        string lower = name.ToLower();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.hideFlags != HideFlags.None || !go.scene.isLoaded) continue;
            if (go.name.ToLower() == lower) return go;
        }
        return null;
    }

    // Model buah: fruit_[nama] -> buah_[nama] -> seed_[nama]
    public static GameObject FindFruitModel(string plantName)
    {
        return FindSceneObject("fruit_" + plantName)
            ?? FindSceneObject("buah_" + plantName)
            ?? FindSceneObject("seed_" + plantName);
    }

    public static Color PlantColor(string plantName)
    {
        switch (plantName)
        {
            case "Tomat": return new Color(0.95f, 0.15f, 0.1f);
            case "Wortel": return new Color(1f, 0.55f, 0.1f);
            case "Jagung": return new Color(0.95f, 0.85f, 0.15f);
            case "Stroberi": return new Color(0.9f, 0.12f, 0.18f);
            case "Labu": return new Color(0.95f, 0.5f, 0.1f);
            case "Semangka": return new Color(0.12f, 0.42f, 0.15f);
            default: return Color.gray;
        }
    }
}
