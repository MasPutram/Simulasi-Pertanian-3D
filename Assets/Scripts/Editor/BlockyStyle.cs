using UnityEngine;
using UnityEditor;

public class BlockyStyle : EditorWindow
{
    //[MenuItem("Farm/Apply Blocky Style")]
    static void ApplyBlockyStyle()
    {
        SetupMaterials();
        RedesignPlots();
        AddSoilRows();

        EditorUtility.DisplayDialog("Blocky Style!",
            "Style voxel/blocky diterapkan!\n\n" +
            "- Plot jadi raised bed dengan dinding\n" +
            "- Tanaman jadi bentuk kotak bertingkat\n" +
            "- Soil rows di atas plot\n\n" +
            "Ctrl+S untuk save!", "OK");
    }

    static Material CreateMat(Color color)
    {
        Shader shader = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
            ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.defaultMaterial.shader
            : Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        return mat;
    }

    static Material GetOrCreateMat(string path, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = CreateMat(color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void SetupMaterials()
    {
        string dir = "Assets/Materials";
        GetOrCreateMat(dir + "/PlotSide.mat", new Color(0.35f, 0.22f, 0.1f));
        GetOrCreateMat(dir + "/PlotTop.mat", new Color(0.45f, 0.3f, 0.15f));
        GetOrCreateMat(dir + "/SoilRow.mat", new Color(0.38f, 0.25f, 0.12f));
        GetOrCreateMat(dir + "/SeedBlock.mat", new Color(0.55f, 0.4f, 0.15f));
        GetOrCreateMat(dir + "/StemBlock.mat", new Color(0.3f, 0.6f, 0.2f));
        GetOrCreateMat(dir + "/LeafBlock.mat", new Color(0.2f, 0.7f, 0.15f));
        GetOrCreateMat(dir + "/TomatBlock.mat", new Color(0.9f, 0.2f, 0.15f));
        GetOrCreateMat(dir + "/WortelBlock.mat", new Color(1f, 0.5f, 0.1f));
        GetOrCreateMat(dir + "/JagungBlock.mat", new Color(0.95f, 0.85f, 0.2f));
    }

    static void RedesignPlots()
    {
        string dir = "Assets/Materials";
        Material sideMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/PlotSide.mat");
        Material topMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/PlotTop.mat");

        foreach (var plot in Object.FindObjectsOfType<PlotManager>())
        {
            // Remove old plant models
            Transform oldSeed = plot.transform.Find("Seed");
            Transform oldGrowing = plot.transform.Find("Growing");
            Transform oldReady = plot.transform.Find("Ready");
            if (oldSeed) Object.DestroyImmediate(oldSeed.gameObject);
            if (oldGrowing) Object.DestroyImmediate(oldGrowing.gameObject);
            if (oldReady) Object.DestroyImmediate(oldReady.gameObject);

            // Make the plot itself look like a raised bed
            plot.GetComponent<Renderer>().material = topMat;

            // Add side walls
            if (plot.transform.Find("SideWalls") == null)
            {
                GameObject walls = new GameObject("SideWalls");
                walls.transform.SetParent(plot.transform);
                walls.transform.localPosition = Vector3.zero;

                // 4 side panels
                CreateSideWall(walls.transform, sideMat, new Vector3(0, -0.3f, 0.5f), new Vector3(1f, 0.6f, 0.05f));
                CreateSideWall(walls.transform, sideMat, new Vector3(0, -0.3f, -0.5f), new Vector3(1f, 0.6f, 0.05f));
                CreateSideWall(walls.transform, sideMat, new Vector3(0.5f, -0.3f, 0), new Vector3(0.05f, 0.6f, 1f));
                CreateSideWall(walls.transform, sideMat, new Vector3(-0.5f, -0.3f, 0), new Vector3(0.05f, 0.6f, 1f));

                // Corner posts
                float postS = 0.08f;
                Vector3[] corners = {
                    new Vector3(0.5f, -0.1f, 0.5f), new Vector3(-0.5f, -0.1f, 0.5f),
                    new Vector3(0.5f, -0.1f, -0.5f), new Vector3(-0.5f, -0.1f, -0.5f)
                };
                foreach (var c in corners)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = "CornerPost";
                    post.transform.SetParent(walls.transform);
                    post.transform.localPosition = c;
                    post.transform.localScale = new Vector3(postS, 0.8f, postS);
                    post.GetComponent<Renderer>().material = sideMat;
                    Object.DestroyImmediate(post.GetComponent<Collider>());
                }
            }

            // Model tanaman sekarang otomatis dari Design_ saat runtime

            EditorUtility.SetDirty(plot);
        }
    }

    static void CreateSideWall(Transform parent, Material mat, Vector3 localPos, Vector3 localScale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = localScale;
        wall.GetComponent<Renderer>().material = mat;
        Object.DestroyImmediate(wall.GetComponent<Collider>());
    }

    static GameObject CreateBlockySeed(Transform parent)
    {
        string dir = "Assets/Materials";
        Material seedMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/SeedBlock.mat");

        GameObject root = new GameObject("Seed");
        root.transform.SetParent(parent);
        root.transform.localPosition = Vector3.zero;

        // 3 small seed cubes on the soil
        for (int i = 0; i < 3; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "SeedCube";
            cube.transform.SetParent(root.transform);
            float xOff = (i - 1) * 0.12f;
            cube.transform.localPosition = new Vector3(xOff, 0.6f, Random.Range(-0.05f, 0.05f));
            cube.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            cube.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 45), 0);
            cube.GetComponent<Renderer>().material = seedMat;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        return root;
    }

    static GameObject CreateBlockyGrowing(Transform parent)
    {
        string dir = "Assets/Materials";
        Material stemMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/StemBlock.mat");
        Material leafMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/LeafBlock.mat");

        GameObject root = new GameObject("Growing");
        root.transform.SetParent(parent);
        root.transform.localPosition = Vector3.zero;

        // 3 small sprouts
        for (int i = 0; i < 3; i++)
        {
            float xOff = (i - 1) * 0.2f;

            // Stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stem.name = "Stem";
            stem.transform.SetParent(root.transform);
            stem.transform.localPosition = new Vector3(xOff, 0.75f, 0);
            stem.transform.localScale = new Vector3(0.04f, 0.25f, 0.04f);
            stem.GetComponent<Renderer>().material = stemMat;
            Object.DestroyImmediate(stem.GetComponent<Collider>());

            // Small leaves
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Leaf";
            leaf.transform.SetParent(root.transform);
            leaf.transform.localPosition = new Vector3(xOff, 0.95f, 0);
            leaf.transform.localScale = new Vector3(0.12f, 0.06f, 0.12f);
            leaf.GetComponent<Renderer>().material = leafMat;
            Object.DestroyImmediate(leaf.GetComponent<Collider>());
        }

        return root;
    }

    static GameObject CreateBlockyReady(Transform parent, string plotName)
    {
        string dir = "Assets/Materials";
        Material stemMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/StemBlock.mat");
        Material leafMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/LeafBlock.mat");

        // Use different fruit colors per plot
        int plotIndex = 0;
        if (plotName.Contains("_")) int.TryParse(plotName.Split('_')[1], out plotIndex);
        string[] fruitMats = { "TomatBlock", "WortelBlock", "JagungBlock" };
        Material fruitMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/" + fruitMats[plotIndex % 3] + ".mat");

        GameObject root = new GameObject("Ready");
        root.transform.SetParent(parent);
        root.transform.localPosition = Vector3.zero;

        // 3 full grown plants with fruit
        for (int i = 0; i < 3; i++)
        {
            float xOff = (i - 1) * 0.25f;

            // Tall stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stem.name = "Stem";
            stem.transform.SetParent(root.transform);
            stem.transform.localPosition = new Vector3(xOff, 0.9f, 0);
            stem.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);
            stem.GetComponent<Renderer>().material = stemMat;
            Object.DestroyImmediate(stem.GetComponent<Collider>());

            // Leaves
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Leaf";
            leaf.transform.SetParent(root.transform);
            leaf.transform.localPosition = new Vector3(xOff, 1.15f, 0);
            leaf.transform.localScale = new Vector3(0.18f, 0.05f, 0.18f);
            leaf.GetComponent<Renderer>().material = leafMat;
            Object.DestroyImmediate(leaf.GetComponent<Collider>());

            // Fruit block
            GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fruit.name = "Fruit";
            fruit.transform.SetParent(root.transform);
            fruit.transform.localPosition = new Vector3(xOff + 0.06f, 1.05f, 0.06f);
            fruit.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            fruit.GetComponent<Renderer>().material = fruitMat;
            Object.DestroyImmediate(fruit.GetComponent<Collider>());
        }

        return root;
    }

    static void AddSoilRows()
    {
        string dir = "Assets/Materials";
        Material soilMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/SoilRow.mat");

        foreach (var plot in Object.FindObjectsOfType<PlotManager>())
        {
            if (plot.transform.Find("SoilRows") != null) continue;

            GameObject rows = new GameObject("SoilRows");
            rows.transform.SetParent(plot.transform);
            rows.transform.localPosition = Vector3.zero;

            // 4 soil ridges
            for (int i = 0; i < 4; i++)
            {
                float zOff = (i - 1.5f) * 0.2f;
                GameObject row = GameObject.CreatePrimitive(PrimitiveType.Cube);
                row.name = "Row";
                row.transform.SetParent(rows.transform);
                row.transform.localPosition = new Vector3(0, 0.55f, zOff);
                row.transform.localScale = new Vector3(0.85f, 0.03f, 0.06f);
                row.GetComponent<Renderer>().material = soilMat;
                Object.DestroyImmediate(row.GetComponent<Collider>());
            }
        }
    }
}
