using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

// Membuat data tanaman (rarity/harga) + model benih & fase tumbuh untuk tanaman baru.
// Model dibuat sebagai Design_[Nama] (child = fase) dan seed_[Nama], nonaktif seperti buatan manual.
public static class PlantDesigns
{
    const string DataDir = "Assets/Data";
    const string MatDir = "Assets/Materials/Plants";

    struct Spec
    {
        public string name; public Rarity rarity; public int price, sell; public float time; public string desc;
        public Spec(string n, Rarity r, float t, int p, int s, string d) { name = n; rarity = r; time = t; price = p; sell = s; desc = d; }
    }

    static readonly Spec[] Specs =
    {
        new Spec("Wortel",   Rarity.Common,    15, 5,   12,  "Sayur akar oranye, cepat tumbuh"),
        new Spec("Tomat",    Rarity.Common,    20, 8,   18,  "Buah merah segar, mudah ditanam"),
        new Spec("Stroberi", Rarity.Uncommon,  30, 20,  45,  "Buah merah manis, rendah dan rimbun"),
        new Spec("Jagung",   Rarity.Uncommon,  35, 30,  65,  "Tanaman tinggi berbiji emas"),
        new Spec("Labu",     Rarity.Rare,      50, 70,  150, "Labu oranye besar, harga mantap"),
        new Spec("Semangka", Rarity.Legendary, 80, 250, 600, "Semangka legendaris, untung paling besar!"),
    };

    static Material soil, stem, leaf, leafDark, white, yellow, berry, berryDark, pumpkin, pumpkinDark, brown, melon, melonStripe, gold;

    [MenuItem("Farm/Plants/Build Plant Data + New Plant Models")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[PlantDesigns] Hentikan Play mode dulu."); return; }

        CreateMaterials();
        List<PlantData> all = BuildData();

        Build("Stroberi", new Vector3(24, 0, 0), BuildStroberi, SeedStroberi);
        Build("Labu", new Vector3(24, 0, 2.5f), BuildLabu, SeedLabu);
        Build("Semangka", new Vector3(24, 0, 5f), BuildSemangka, SeedSemangka);

        // Desain ulang tanaman awal. Model buatan manual disimpan sebagai *_lama (nonaktif)
        BackupLegacy("Tomat"); BackupLegacy("Wortel"); BackupLegacy("Jagung");
        Build("Tomat", new Vector3(24, 0, 7.5f), BuildTomat, SeedTomat);
        Build("Wortel", new Vector3(24, 0, 10f), BuildWortel, SeedWortel);
        Build("Jagung", new Vector3(24, 0, 12.5f), BuildJagung, SeedJagung);
        BuildFruit("Tomat", new Vector3(21, 0, 7.5f), FruitTomat);
        BuildFruit("Wortel", new Vector3(21, 0, 10f), FruitWortel);
        BuildFruit("Jagung", new Vector3(21, 0, 12.5f), FruitJagung);
        BuildFruit("Stroberi", new Vector3(21, 0, 0), FruitStroberi);

        GameManager gm = Object.FindObjectOfType<GameManager>(true);
        if (gm != null)
        {
            SerializedObject so = new SerializedObject(gm);
            SerializedProperty arr = so.FindProperty("availablePlants");
            arr.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            so.FindProperty("startingScore").intValue = 20;
            so.ApplyModifiedProperties();
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[PlantDesigns] Selesai: " + all.Count + " tanaman, model Stroberi/Labu/Semangka dibuat. Scene disimpan.");
    }

    // ---------- Data ----------

    static List<PlantData> BuildData()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
        var list = new List<PlantData>();
        foreach (var s in Specs)
        {
            string path = DataDir + "/" + s.name + ".asset";
            PlantData pd = AssetDatabase.LoadAssetAtPath<PlantData>(path);
            if (pd == null)
            {
                pd = ScriptableObject.CreateInstance<PlantData>();
                AssetDatabase.CreateAsset(pd, path);
            }
            pd.plantName = s.name;
            pd.rarity = s.rarity;
            pd.seedPrice = s.price;
            pd.scoreValue = s.sell;
            pd.growthTime = s.time;
            pd.description = s.desc;
            EditorUtility.SetDirty(pd);
            list.Add(pd);
        }
        return list;
    }

    // ---------- Scene objects ----------

    static void Build(string plant, Vector3 pos, System.Action<Transform[]> phases, System.Action<Transform> seed)
    {
        foreach (string n in new[] { "Design_" + plant, "seed_" + plant })
        {
            GameObject old = HandItem.FindSceneObject(n);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }

        GameObject design = new GameObject("Design_" + plant);
        Undo.RegisterCreatedObjectUndo(design, "Build " + plant);
        design.transform.position = pos;
        string lower = plant.ToLower();
        Transform[] f = new Transform[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject phase = new GameObject("fase_" + (i + 1) + "_" + lower);
            phase.transform.SetParent(design.transform, false);
            phase.transform.localPosition = new Vector3(i * 1.2f, 0, 0); // dijajarkan supaya mudah dilihat saat diedit
            f[i] = phase.transform;
        }
        // fase 1-2 sama untuk semua tanaman
        Planted(f[0], plant);
        Sprout(f[1]);
        phases(f);
        design.SetActive(false);

        GameObject seedObj = new GameObject("seed_" + plant);
        Undo.RegisterCreatedObjectUndo(seedObj, "Build seed " + plant);
        seedObj.transform.position = pos + new Vector3(-1.5f, 0, 0);
        seed(seedObj.transform);
        seedObj.SetActive(false);
    }

    static void Planted(Transform p, string plant)
    {
        B(p, "Mound", new Vector3(0, 0.025f, 0), new Vector3(0.3f, 0.05f, 0.3f), soil);
        Material seedMat = plant == "Labu" || plant == "Wortel" ? pumpkin : plant == "Semangka" ? melon : plant == "Jagung" ? yellow : berry;
        B(p, "Seed", new Vector3(0, 0.06f, 0), new Vector3(0.06f, 0.04f, 0.06f), seedMat);
    }

    static void Sprout(Transform p)
    {
        B(p, "Mound", new Vector3(0, 0.015f, 0), new Vector3(0.22f, 0.03f, 0.22f), soil);
        B(p, "Stem", new Vector3(0, 0.08f, 0), new Vector3(0.025f, 0.14f, 0.025f), stem);
        B(p, "LeafL", new Vector3(-0.05f, 0.15f, 0), new Vector3(0.09f, 0.02f, 0.05f), leaf, new Vector3(0, 0, 20));
        B(p, "LeafR", new Vector3(0.05f, 0.15f, 0), new Vector3(0.09f, 0.02f, 0.05f), leaf, new Vector3(0, 0, -20));
    }

    // Daun melingkar (roset), sumbu panjang daun = X
    static void Rosette(Transform p, int count, float len, float y, Material m, float tilt, float offset = 0f)
    {
        for (int i = 0; i < count; i++)
        {
            float a = offset + i * 360f / count;
            Vector3 dir = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
            B(p, "Leaf" + i, dir * len * 0.5f + Vector3.up * y, new Vector3(len, 0.025f, len * 0.42f), m, new Vector3(0, -a, tilt));
        }
    }

    static void Flower(Transform p, string n, Vector3 pos, float h)
    {
        B(p, n + "Stem", pos + Vector3.up * h * 0.5f, new Vector3(0.018f, h, 0.018f), stem);
        B(p, n + "Petal", pos + Vector3.up * h, new Vector3(0.08f, 0.025f, 0.08f), white);
        B(p, n + "Center", pos + Vector3.up * (h + 0.018f), new Vector3(0.035f, 0.02f, 0.035f), yellow);
    }

    // ---------- Stroberi (Uncommon) ----------

    static void BuildStroberi(Transform[] f)
    {
        B(f[2], "Stem", new Vector3(0, 0.05f, 0), new Vector3(0.03f, 0.1f, 0.03f), stem);
        Rosette(f[2], 4, 0.18f, 0.08f, leaf, 15);

        B(f[3], "Stem", new Vector3(0, 0.06f, 0), new Vector3(0.035f, 0.12f, 0.035f), stem);
        Rosette(f[3], 6, 0.24f, 0.09f, leaf, 14);
        Flower(f[3], "F1", new Vector3(0.1f, 0, 0.02f), 0.2f);
        Flower(f[3], "F2", new Vector3(-0.06f, 0, 0.09f), 0.17f);
        Flower(f[3], "F3", new Vector3(-0.04f, 0, -0.1f), 0.22f);

        B(f[4], "Stem", new Vector3(0, 0.06f, 0), new Vector3(0.035f, 0.12f, 0.035f), stem);
        Rosette(f[4], 6, 0.27f, 0.11f, leafDark, 16, 15);
        Rosette(f[4], 4, 0.18f, 0.14f, leaf, 22, 45);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90 + 30) * Mathf.Deg2Rad;
            Vector3 c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.2f;
            Strawberry(f[4], "Berry" + i, c, 0.42f);
        }
        Flower(f[4], "F1", new Vector3(0, 0, 0.04f), 0.24f);
    }

    // Stroberi blocky seperti referensi: 2 tingkat balok merah, bintik biji krem, mahkota daun tebal.
    // basePos = titik dasar, s = skala (1 = ukuran buah yang dipegang)
    static void Strawberry(Transform p, string n, Vector3 basePos, float s)
    {
        float bw = 0.22f * s, bh = 0.13f * s;   // tingkat bawah
        float tw = 0.32f * s, th = 0.2f * s;    // tingkat atas
        Vector3 bc = basePos + Vector3.up * (bh * 0.5f);
        Vector3 tc = basePos + Vector3.up * (bh + th * 0.5f);
        B(p, n + "Bottom", bc, new Vector3(bw, bh, bw), berryDark);
        B(p, n + "Top", tc, new Vector3(tw, th, tw), berry);

        // bintik biji di 4 sisi
        float d = 0.026f * s;
        Vector2[] topDots = { new Vector2(-0.1f, 0.05f), new Vector2(0.02f, 0.06f), new Vector2(0.11f, 0.03f), new Vector2(-0.05f, -0.04f), new Vector2(0.07f, -0.05f) };
        Vector2[] botDots = { new Vector2(-0.05f, 0.02f), new Vector2(0.06f, -0.02f) };
        for (int side = 0; side < 4; side++)
        {
            Quaternion r = Quaternion.Euler(0, side * 90f, 0);
            foreach (var (dots, center, half, tag) in new[] { (topDots, tc, tw * 0.5f, "T"), (botDots, bc, bw * 0.5f, "B") })
                for (int i = 0; i < dots.Length; i++)
                {
                    Vector2 o = dots[i] * s;
                    if (Mathf.Abs(o.x) > half - d) continue;
                    Vector3 local = new Vector3(o.x, o.y, -half - d * 0.2f);
                    B(p, n + "Dot" + tag + side + i, center + r * local, new Vector3(d, d, d * 0.4f), white, new Vector3(0, side * 90f, 0));
                }
        }

        // mahkota daun: inti + 4 balok miring keluar
        float capY = basePos.y + bh + th;
        B(p, n + "CapCore", new Vector3(basePos.x, capY + 0.04f * s, basePos.z), new Vector3(0.22f * s, 0.08f * s, 0.22f * s), leafDark);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f + 45f;
            Vector3 dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
            B(p, n + "Leaf" + i, new Vector3(basePos.x, capY + 0.05f * s, basePos.z) + dir * 0.09f * s,
                new Vector3(0.15f * s, 0.07f * s, 0.12f * s), i % 2 == 0 ? leaf : leafDark, new Vector3(-18, a, 0));
        }
        B(p, n + "Stem", new Vector3(basePos.x, capY + 0.11f * s, basePos.z), new Vector3(0.04f * s, 0.06f * s, 0.04f * s), stem);
    }

    static void FruitStroberi(Transform t, float s) => Strawberry(t, "Strawberry", Vector3.zero, s);
    static void SeedStroberi(Transform t) => Strawberry(t, "Strawberry", Vector3.zero, 0.45f);

    // ---------- Labu (Rare) ----------

    static void Vine(Transform p, Material leafMat, float length)
    {
        B(p, "Vine", new Vector3(0, 0.02f, 0), new Vector3(length, 0.035f, 0.035f), stem);
        B(p, "VineB", new Vector3(0.05f, 0.02f, 0.08f), new Vector3(0.035f, 0.035f, 0.2f), stem, new Vector3(0, 30, 0));
        B(p, "LeafA", new Vector3(-length * 0.35f, 0.07f, 0.07f), new Vector3(0.18f, 0.025f, 0.18f), leafMat, new Vector3(8, 20, 0));
        B(p, "LeafB", new Vector3(0.02f, 0.08f, -0.08f), new Vector3(0.2f, 0.025f, 0.2f), leafMat, new Vector3(-8, -15, 0));
        B(p, "LeafC", new Vector3(length * 0.38f, 0.06f, 0.06f), new Vector3(0.16f, 0.025f, 0.16f), leafMat, new Vector3(6, 40, 0));
    }

    static void BuildLabu(Transform[] f)
    {
        Vine(f[2], leaf, 0.4f);

        Vine(f[3], leaf, 0.55f);
        B(f[3], "Flower", new Vector3(-0.1f, 0.12f, -0.02f), new Vector3(0.1f, 0.06f, 0.1f), yellow);
        B(f[3], "BabyPumpkin", new Vector3(0.12f, 0.05f, -0.06f), new Vector3(0.13f, 0.1f, 0.13f), leafDark);

        Vine(f[4], leafDark, 0.6f);
        Vector3 c = new Vector3(0.02f, 0, -0.02f);
        B(f[4], "Pumpkin", c + new Vector3(0, 0.15f, 0), new Vector3(0.38f, 0.28f, 0.38f), pumpkin);
        B(f[4], "RibX", c + new Vector3(0, 0.145f, 0), new Vector3(0.42f, 0.25f, 0.26f), pumpkinDark);
        B(f[4], "RibZ", c + new Vector3(0, 0.145f, 0), new Vector3(0.26f, 0.25f, 0.42f), pumpkinDark);
        B(f[4], "Top", c + new Vector3(0, 0.3f, 0), new Vector3(0.24f, 0.03f, 0.24f), pumpkin);
        B(f[4], "Stem", c + new Vector3(0, 0.36f, 0), new Vector3(0.05f, 0.1f, 0.05f), brown, new Vector3(0, 0, 10));
        B(f[4], "Curl", c + new Vector3(0.05f, 0.4f, 0), new Vector3(0.07f, 0.02f, 0.02f), stem);
    }

    static void SeedLabu(Transform s)
    {
        B(s, "Body", new Vector3(0, 0.055f, 0), new Vector3(0.15f, 0.11f, 0.15f), pumpkin);
        B(s, "Rib", new Vector3(0, 0.055f, 0), new Vector3(0.16f, 0.1f, 0.09f), pumpkinDark);
        B(s, "Stem", new Vector3(0, 0.125f, 0), new Vector3(0.025f, 0.04f, 0.025f), brown);
    }

    // ---------- Semangka (Legendary) ----------

    static void BuildSemangka(Transform[] f)
    {
        Vine(f[2], leafDark, 0.45f);

        Vine(f[3], leafDark, 0.6f);
        B(f[3], "Flower", new Vector3(-0.12f, 0.12f, 0.02f), new Vector3(0.1f, 0.06f, 0.1f), yellow);
        B(f[3], "BabyMelon", new Vector3(0.12f, 0.06f, -0.07f), new Vector3(0.17f, 0.11f, 0.11f), melon);
        B(f[3], "BabyStripe", new Vector3(0.12f, 0.06f, -0.07f), new Vector3(0.175f, 0.115f, 0.03f), melonStripe);

        Vine(f[4], leafDark, 0.7f);
        Vector3 c = new Vector3(0, 0, -0.03f);
        B(f[4], "Melon", c + new Vector3(0, 0.19f, 0), new Vector3(0.6f, 0.38f, 0.4f), melon);
        B(f[4], "MelonTop", c + new Vector3(0, 0.39f, 0), new Vector3(0.5f, 0.03f, 0.3f), melon);
        B(f[4], "Stripe1", c + new Vector3(0, 0.19f, -0.12f), new Vector3(0.61f, 0.39f, 0.05f), melonStripe);
        B(f[4], "Stripe2", c + new Vector3(0, 0.19f, 0f), new Vector3(0.61f, 0.4f, 0.05f), melonStripe);
        B(f[4], "Stripe3", c + new Vector3(0, 0.19f, 0.12f), new Vector3(0.61f, 0.39f, 0.05f), melonStripe);
        B(f[4], "Stem", c + new Vector3(0.31f, 0.3f, 0), new Vector3(0.06f, 0.04f, 0.04f), stem);

        // Penanda legendary: kilau emas kecil (tanpa lampu, supaya tidak menyala terang saat malam)
        Vector3[] sparkles = { new Vector3(-0.28f, 0.55f, 0.1f), new Vector3(0.25f, 0.62f, -0.12f), new Vector3(0.05f, 0.72f, 0.18f), new Vector3(-0.1f, 0.5f, -0.22f) };
        for (int i = 0; i < sparkles.Length; i++)
            B(f[4], "Sparkle" + i, c + sparkles[i], Vector3.one * 0.04f, gold, new Vector3(45, 45, 0));
    }

    static void SeedSemangka(Transform s)
    {
        B(s, "Body", new Vector3(0, 0.055f, 0), new Vector3(0.17f, 0.11f, 0.12f), melon);
        B(s, "Stripe1", new Vector3(0, 0.055f, -0.03f), new Vector3(0.175f, 0.115f, 0.02f), melonStripe);
        B(s, "Stripe2", new Vector3(0, 0.055f, 0.03f), new Vector3(0.175f, 0.115f, 0.02f), melonStripe);
        B(s, "Sparkle", new Vector3(0.06f, 0.14f, 0), Vector3.one * 0.025f, gold, new Vector3(45, 45, 0));
    }

    // ---------- Tanaman awal (redesign, gaya referensi Grow a Garden) ----------

    // Model lama buatan manual disimpan sebagai *_lama (nonaktif), hanya sekali
    static void BackupLegacy(string plant)
    {
        foreach (string n in new[] { "Design_" + plant, "seed_" + plant })
        {
            if (HandItem.FindSceneObject(n + "_lama") != null) continue;
            GameObject old = HandItem.FindSceneObject(n);
            if (old == null) continue;
            Undo.RecordObject(old, "Backup " + n);
            old.name = old.name + "_lama";
            old.SetActive(false);
        }
    }

    // Model buah untuk dipegang di atas kepala (fruit_[Nama])
    static void BuildFruit(string plant, Vector3 pos, System.Action<Transform, float> build)
    {
        GameObject old = HandItem.FindSceneObject("fruit_" + plant);
        if (old != null) Undo.DestroyObjectImmediate(old);
        GameObject go = new GameObject("fruit_" + plant);
        Undo.RegisterCreatedObjectUndo(go, "Build fruit " + plant);
        go.transform.position = pos;
        build(go.transform, 1f);
        go.SetActive(false);
    }

    // Bola blocky: 3 balok bersilang
    static void Ball(Transform p, string n, Vector3 c, float s, Material m)
    {
        B(p, n + "X", c, new Vector3(s, s * 0.78f, s * 0.78f), m);
        B(p, n + "Y", c, new Vector3(s * 0.78f, s, s * 0.78f), m);
        B(p, n + "Z", c, new Vector3(s * 0.78f, s * 0.78f, s), m);
    }

    // Tomat bulat + tangkai bintang; c = pusat buah
    static void Tomato(Transform p, string n, Vector3 c, float s)
    {
        Ball(p, n, c, s, berry);
        float top = c.y + s * 0.5f;
        B(p, n + "StarA", new Vector3(c.x, top, c.z), new Vector3(s * 0.62f, s * 0.07f, s * 0.16f), leaf);
        B(p, n + "StarB", new Vector3(c.x, top, c.z), new Vector3(s * 0.16f, s * 0.07f, s * 0.62f), leaf);
        B(p, n + "StarC", new Vector3(c.x, top, c.z), new Vector3(s * 0.45f, s * 0.07f, s * 0.12f), leafDark, new Vector3(0, 45, 0));
        B(p, n + "Stem", new Vector3(c.x, top + s * 0.1f, c.z), new Vector3(s * 0.1f, s * 0.2f, s * 0.1f), leafDark);
    }

    // Cabang datar dari batang: sumbu panjang X, dimiringkan ke atas
    static void Branch(Transform p, string n, float y, float len, float yaw, float up, bool pad)
    {
        Quaternion r = Quaternion.Euler(0, -yaw, up);
        Vector3 dir = r * Vector3.right;
        B(p, n, dir * len * 0.5f + Vector3.up * y, new Vector3(len, 0.035f, 0.035f), stem, new Vector3(0, -yaw, up));
        if (pad)
            B(p, n + "Pad", dir * len + Vector3.up * y, new Vector3(0.11f, 0.025f, 0.08f), leaf, new Vector3(0, -yaw, 0));
    }

    static void BuildTomat(Transform[] f)
    {
        B(f[2], "Stalk", new Vector3(0, 0.22f, 0), new Vector3(0.045f, 0.44f, 0.045f), stem);
        Branch(f[2], "B1", 0.06f, 0.1f, 200, 0, true);
        Branch(f[2], "B2", 0.24f, 0.14f, 20, 35, false);

        B(f[3], "Stalk", new Vector3(0, 0.42f, 0), new Vector3(0.05f, 0.84f, 0.05f), stem);
        Branch(f[3], "B1", 0.07f, 0.12f, 200, 0, true);
        Branch(f[3], "B2", 0.34f, 0.24f, 15, 32, false);
        Branch(f[3], "B3", 0.7f, 0.16f, 160, 20, true);
        B(f[3], "Flower1", new Vector3(0.2f, 0.47f, -0.05f), Vector3.one * 0.05f, yellow);
        B(f[3], "Flower2", new Vector3(-0.15f, 0.76f, -0.05f), Vector3.one * 0.05f, yellow);

        // matang: batang tinggi, cabang, 2 tomat besar tergantung (seperti referensi)
        B(f[4], "Stalk", new Vector3(0, 0.55f, 0), new Vector3(0.055f, 1.1f, 0.055f), stem);
        Branch(f[4], "B1", 0.08f, 0.13f, 200, 0, true);
        Branch(f[4], "B2", 0.42f, 0.3f, 15, 32, false);
        Branch(f[4], "B3", 0.95f, 0.18f, 0, 0, true);
        Branch(f[4], "B4", 0.98f, 0.2f, 160, 28, false);
        Tomato(f[4], "TomatoBig", new Vector3(-0.2f, 0.92f, -0.03f), 0.26f);
        Tomato(f[4], "TomatoSmall", new Vector3(0.06f, 0.72f, -0.06f), 0.18f);
    }

    static void FruitTomat(Transform t, float s)
    {
        Tomato(t, "Tomato", new Vector3(0, 0.13f * s, 0), 0.26f * s);
    }

    static void SeedTomat(Transform t) => FruitTomat(t, 0.5f);

    // Wortel gemuk dari balok bertumpuk + kotak timbul, daun tebal di atas
    static void Carrot(Transform p, string n, Vector3 basePos, float s, bool withLeaves)
    {
        float[] w = { 0.3f, 0.26f, 0.2f, 0.12f };
        float[] h = { 0.26f, 0.24f, 0.22f, 0.16f };
        float y = basePos.y;
        // dari ujung bawah ke atas
        for (int i = w.Length - 1; i >= 0; i--)
        {
            float hh = h[i] * s, ww = w[i] * s;
            Vector3 c = new Vector3(basePos.x, y + hh * 0.5f, basePos.z);
            B(p, n + "Seg" + i, c, new Vector3(ww, hh, ww), pumpkin);
            if (i < 3)
            {
                B(p, n + "StudF" + i, c + new Vector3(0, 0, -ww * 0.5f), new Vector3(ww * 0.4f, hh * 0.4f, 0.012f * s), pumpkinDark);
                B(p, n + "StudR" + i, c + new Vector3(ww * 0.5f, 0, 0), new Vector3(0.012f * s, hh * 0.4f, ww * 0.4f), pumpkinDark);
            }
            y += hh;
        }
        if (!withLeaves) return;
        float top = y;
        B(p, n + "LeafC", new Vector3(basePos.x, top + 0.2f * s, basePos.z), new Vector3(0.12f * s, 0.4f * s, 0.14f * s), leaf);
        B(p, n + "LeafL", new Vector3(basePos.x - 0.09f * s, top + 0.16f * s, basePos.z), new Vector3(0.11f * s, 0.34f * s, 0.13f * s), leafDark, new Vector3(0, 0, 16));
        B(p, n + "LeafR", new Vector3(basePos.x + 0.09f * s, top + 0.16f * s, basePos.z + 0.02f * s), new Vector3(0.11f * s, 0.34f * s, 0.13f * s), leafDark, new Vector3(0, 0, -16));
        B(p, n + "LeafB", new Vector3(basePos.x, top + 0.14f * s, basePos.z + 0.08f * s), new Vector3(0.11f * s, 0.3f * s, 0.11f * s), leaf, new Vector3(-16, 0, 0));
    }

    static void ChunkyLeaves(Transform p, int count, float height, float y0)
    {
        for (int i = 0; i < count; i++)
        {
            float a = i * 360f / count + 20;
            Vector3 dir = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
            B(p, "Leaf" + i, dir * 0.05f + Vector3.up * (y0 + height * 0.5f), new Vector3(0.1f, height, 0.12f), i % 2 == 0 ? leaf : leafDark,
                new Vector3(0, -a, -14));
        }
    }

    static void BuildWortel(Transform[] f)
    {
        B(f[2], "Mound", new Vector3(0, 0.015f, 0), new Vector3(0.24f, 0.03f, 0.24f), soil);
        ChunkyLeaves(f[2], 3, 0.18f, 0.02f);

        B(f[3], "Mound", new Vector3(0, 0.015f, 0), new Vector3(0.28f, 0.03f, 0.28f), soil);
        B(f[3], "CarrotTop", new Vector3(0, 0.05f, 0), new Vector3(0.14f, 0.07f, 0.14f), pumpkin);
        ChunkyLeaves(f[3], 4, 0.28f, 0.08f);

        // matang: pangkal wortel besar menyembul dari tanah + daun tebal
        B(f[4], "Mound", new Vector3(0, 0.02f, 0), new Vector3(0.36f, 0.04f, 0.36f), soil);
        B(f[4], "CarrotTop", new Vector3(0, 0.1f, 0), new Vector3(0.22f, 0.16f, 0.22f), pumpkin);
        B(f[4], "StudF", new Vector3(0, 0.1f, -0.111f), new Vector3(0.08f, 0.06f, 0.01f), pumpkinDark);
        B(f[4], "StudR", new Vector3(0.111f, 0.1f, 0), new Vector3(0.01f, 0.06f, 0.08f), pumpkinDark);
        ChunkyLeaves(f[4], 5, 0.38f, 0.17f);
    }

    static void FruitWortel(Transform t, float s) => Carrot(t, "Carrot", Vector3.zero, s, true);
    static void SeedWortel(Transform t) => FruitWortel(t, 0.35f);

    // Tongkol jagung dari banyak kubus kecil (bergelombang seperti referensi)
    static void Cob(Transform p, string n, Vector3 basePos, float s)
    {
        int layers = 7;
        float cube = 0.065f * s;
        for (int L = 0; L < layers; L++)
        {
            float t = (float)L / (layers - 1);
            float radius = Mathf.Lerp(0.075f, 0.035f, t * t) * s;
            int count = L == layers - 1 ? 1 : 6;
            float y = basePos.y + cube * 0.5f + L * cube * 0.9f;
            for (int i = 0; i < count; i++)
            {
                float a = (i * 60f + L * 30f) * Mathf.Deg2Rad;
                Vector3 off = count == 1 ? Vector3.zero : new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                B(p, n + "K" + L + "_" + i, new Vector3(basePos.x, y, basePos.z) + off, Vector3.one * cube,
                    (i + L) % 3 == 0 ? pumpkinDark : yellow, new Vector3(0, i * 60f + L * 30f, 0));
            }
            if (count > 1) // inti supaya tidak berlubang
                B(p, n + "Core" + L, new Vector3(basePos.x, y, basePos.z), Vector3.one * radius * 1.6f, yellow);
        }
    }

    // Daun lebar datar yang menyebar di tanah dengan kotak timbul
    static void FlatLeaves(Transform p, int count, float len, float width, float lift)
    {
        for (int i = 0; i < count; i++)
        {
            float a = i * 360f / count + 45;
            Quaternion r = Quaternion.Euler(0, -a, lift);
            Vector3 dir = r * Vector3.right;
            B(p, "Leaf" + i, dir * len * 0.5f + Vector3.up * 0.03f, new Vector3(len, 0.035f, width), i % 2 == 0 ? leaf : leafDark, new Vector3(0, -a, lift));
            B(p, "Stud" + i, dir * len * 0.6f + Vector3.up * 0.05f, new Vector3(width * 0.35f, 0.012f, width * 0.35f), leafDark, new Vector3(0, -a + 45, lift));
        }
    }

    static void BuildJagung(Transform[] f)
    {
        FlatLeaves(f[2], 3, 0.24f, 0.12f, 12);
        B(f[2], "Shoot", new Vector3(0, 0.08f, 0), new Vector3(0.05f, 0.12f, 0.05f), stem);

        FlatLeaves(f[3], 4, 0.32f, 0.15f, 10);
        B(f[3], "YoungCob", new Vector3(0, 0.12f, 0), new Vector3(0.09f, 0.18f, 0.09f), leaf);

        FlatLeaves(f[4], 4, 0.42f, 0.18f, 9);
        Cob(f[4], "Cob", new Vector3(0, 0.04f, 0), 1.2f);
    }

    static void FruitJagung(Transform t, float s)
    {
        Cob(t, "Cob", new Vector3(0, 0.03f * s, 0), s);
        B(t, "HuskA", new Vector3(0.07f * s, 0.06f * s, 0), new Vector3(0.03f * s, 0.14f * s, 0.1f * s), leaf, new Vector3(0, 0, -20));
        B(t, "HuskB", new Vector3(-0.07f * s, 0.06f * s, 0), new Vector3(0.03f * s, 0.14f * s, 0.1f * s), leafDark, new Vector3(0, 0, 20));
        B(t, "HuskC", new Vector3(0, 0.06f * s, 0.07f * s), new Vector3(0.1f * s, 0.14f * s, 0.03f * s), leaf, new Vector3(20, 0, 0));
    }

    static void SeedJagung(Transform t) => FruitJagung(t, 0.45f);

    // ---------- Helper ----------

    static void B(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void CreateMaterials()
    {
        soil = Mat("Plant_Soil", new Color(0.4f, 0.26f, 0.15f));
        stem = Mat("Plant_Stem", new Color(0.32f, 0.6f, 0.2f));
        leaf = Mat("Plant_Leaf", new Color(0.27f, 0.66f, 0.22f));
        leafDark = Mat("Plant_LeafDark", new Color(0.15f, 0.45f, 0.15f));
        white = Mat("Plant_FlowerWhite", new Color(0.97f, 0.96f, 0.92f));
        yellow = Mat("Plant_FlowerYellow", new Color(1f, 0.85f, 0.15f));
        berry = Mat("Plant_Berry", new Color(0.88f, 0.1f, 0.15f));
        berryDark = Mat("Plant_BerryDark", new Color(0.72f, 0.06f, 0.12f));
        pumpkin = Mat("Plant_Pumpkin", new Color(0.97f, 0.52f, 0.1f));
        pumpkinDark = Mat("Plant_PumpkinDark", new Color(0.82f, 0.38f, 0.06f));
        brown = Mat("Plant_StemBrown", new Color(0.42f, 0.32f, 0.14f));
        melon = Mat("Plant_Melon", new Color(0.1f, 0.38f, 0.14f));
        melonStripe = Mat("Plant_MelonStripe", new Color(0.42f, 0.74f, 0.32f));
        gold = Mat("Plant_Gold", new Color(1f, 0.82f, 0.25f), true);
    }

    static Material Mat(string name, Color color, bool emissive = false)
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
        m.SetFloat("_Glossiness", 0.12f);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 0.3f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
