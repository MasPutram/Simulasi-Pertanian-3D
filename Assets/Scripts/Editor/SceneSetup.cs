using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class SceneSetup : EditorWindow
{
    static Material CreateMat(Color color)
    {
        Shader shader = GraphicsSettings.defaultRenderPipeline != null
            ? GraphicsSettings.defaultRenderPipeline.defaultMaterial.shader
            : Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        return mat;
    }

    [MenuItem("Farm/Setup Scene")]
    static void SetupFarmScene()
    {
        if (!EditorUtility.DisplayDialog("Setup Scene",
            "Ini akan membuat semua GameObject untuk Simulasi Pertanian.\nLanjutkan?",
            "Ya", "Batal"))
            return;

        CreatePlantDataAssets();
        CreateGround();
        CreatePlots();
        SetupCamera();
        CreateGameManager();
        CreatePlayerInteraction();
        CreateUI();
        RelinkAll();

        FixInputSettings();

        EditorUtility.DisplayDialog("Selesai!",
            "Scene berhasil di-setup!\n\n" +
            "Yang sudah dibuat:\n" +
            "- Ground (tanah)\n" +
            "- 6 Plot lahan\n" +
            "- Camera Controller\n" +
            "- Game Manager\n" +
            "- Player Interaction\n" +
            "- 3 Plant Data (Tomat, Wortel, Jagung)\n" +
            "- UI Canvas (skor, rank, info, tombol bibit)\n\n" +
            "Tekan Play untuk mencoba!", "OK");
    }

    static void FixInputSettings()
    {
        var serializedSettings = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var prop = serializedSettings.FindProperty("activeInputHandler");
        if (prop != null && prop.intValue != 2)
        {
            prop.intValue = 2;
            serializedSettings.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("Input System",
                "Active Input Handling diubah ke 'Both'.\nUnity perlu RESTART.\n\nSetelah restart, tekan Play untuk test.", "OK");
        }
    }

    //[MenuItem("Farm/Fix Materials")]
    static void FixAllMaterials()
    {
        string[] matPaths = {
            "Assets/Materials/Ground.mat",
            "Assets/Materials/Plot.mat",
            "Assets/Materials/Seed.mat",
            "Assets/Materials/Growing.mat",
            "Assets/Materials/Ready.mat"
        };
        Color[] colors = {
            new Color(0.3f, 0.6f, 0.2f),
            new Color(0.4f, 0.26f, 0.13f),
            new Color(0.6f, 0.4f, 0.1f),
            new Color(0.2f, 0.7f, 0.1f),
            new Color(0.9f, 0.2f, 0.1f)
        };
        for (int i = 0; i < matPaths.Length; i++)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPaths[i]);
            if (mat != null)
            {
                mat.SetColor("_BaseColor", colors[i]);
                mat.color = colors[i];
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Fix Materials", "Warna material sudah diperbaiki!", "OK");
    }

    [MenuItem("Farm/Add Weather System")]
    static void AddWeatherSystem()
    {
        if (GameObject.Find("WeatherManager") != null)
        {
            EditorUtility.DisplayDialog("Info", "Weather System sudah ada!", "OK");
            return;
        }

        GameObject wmObj = new GameObject("WeatherManager");
        WeatherManager wm = wmObj.AddComponent<WeatherManager>();
        wmObj.AddComponent<WeatherUILinker>();

        // Find or create weather UI on the existing canvas
        GameObject canvas = GameObject.Find("UICanvas");
        if (canvas == null) return;

        // Right side panel for weather controls
        GameObject weatherPanel = new GameObject("WeatherPanel");
        weatherPanel.transform.SetParent(canvas.transform, false);
        Image panelImg = weatherPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.15f, 0.3f, 0.8f);
        RectTransform panelRt = weatherPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1, 0.5f);
        panelRt.anchorMax = new Vector2(1, 0.5f);
        panelRt.pivot = new Vector2(1, 0.5f);
        panelRt.anchoredPosition = new Vector2(-10, 0);
        panelRt.sizeDelta = new Vector2(120, 290);

        // Title
        CreateWeatherText(weatherPanel.transform, "WeatherTitle", "Waktu & Cuaca", 0, 125, 14, FontStyle.Bold);

        // Time text display
        GameObject timeTextObj = CreateWeatherText(weatherPanel.transform, "TimeDisplay", "o Siang", 0, 95, 13, FontStyle.Normal);
        wm.timeText = timeTextObj.GetComponent<UnityEngine.UI.Text>();

        // Time buttons
        string[] times = { "Pagi", "Siang", "Sore", "Malam" };
        Color[] timeColors = {
            new Color(0.9f, 0.7f, 0.3f),
            new Color(0.3f, 0.7f, 0.9f),
            new Color(0.9f, 0.5f, 0.2f),
            new Color(0.2f, 0.2f, 0.5f)
        };
        for (int i = 0; i < 4; i++)
        {
            float yPos = 60 - i * 35;
            CreateWeatherButton(weatherPanel.transform, times[i], new Vector2(0, yPos),
                new Vector2(100, 28), timeColors[i]);
        }

        // Auto cycle button
        CreateWeatherButton(weatherPanel.transform, "Auto", new Vector2(0, -85),
            new Vector2(100, 28), new Color(0.2f, 0.6f, 0.3f));

        // Weather text
        GameObject weatherTextObj = CreateWeatherText(weatherPanel.transform, "WeatherDisplay", "Cerah", 0, -115, 13, FontStyle.Normal);
        wm.weatherText = weatherTextObj.GetComponent<UnityEngine.UI.Text>();

        // Weather toggle button
        CreateWeatherButton(weatherPanel.transform, "Hujan/Cerah", new Vector2(0, -140),
            new Vector2(100, 28), new Color(0.3f, 0.5f, 0.8f));

        EditorUtility.DisplayDialog("Weather System",
            "Sistem cuaca ditambahkan!\n\n" +
            "Panel di kanan layar:\n" +
            "- Pagi / Siang / Sore / Malam\n" +
            "- Toggle Hujan/Cerah\n\n" +
            "Note: Tombol perlu di-link manual via WeatherUILinker script.", "OK");
    }

    static GameObject CreateWeatherText(Transform parent, string name, string content, float x, float y, int fontSize, FontStyle style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        UnityEngine.UI.Text text = obj.AddComponent<UnityEngine.UI.Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(110, 25);
        return obj;
    }

    static void CreateWeatherButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        btnObj.AddComponent<UnityEngine.UI.Button>();
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    [MenuItem("Farm/Add Main Menu")]
    static void AddMainMenu()
    {
        if (Object.FindObjectOfType<MainMenu>() != null)
        {
            EditorUtility.DisplayDialog("Info", "Main Menu sudah ada!", "OK");
            return;
        }
        GameObject mm = new GameObject("MainMenu");
        mm.AddComponent<MainMenu>();
        EditorUtility.SetDirty(mm);
        EditorUtility.DisplayDialog("Main Menu", "Main Menu ditambahkan!\nPlay untuk test.", "OK");
    }

    [MenuItem("Farm/Info Design System")]
    static void InfoDesignSystem()
    {
        EditorUtility.DisplayDialog("Design System - Auto Runtime",
            "Sistem design sekarang OTOMATIS!\n\n" +
            "Cara pakai:\n" +
            "1. Buat model tanaman di Scene\n" +
            "2. Namain: Design_Jagung / Design_Tomat / Design_Wortel\n" +
            "3. Setiap child = 1 fase pertumbuhan\n" +
            "4. Urutan child = urutan fase (atas → bawah)\n" +
            "5. Tekan Play, tanam → otomatis muncul!\n\n" +
            "Tidak perlu Apply manual lagi.", "OK");
    }


    [MenuItem("Farm/Apply Stud to Designs")]
    static void ApplyStudToDesigns()
    {
        AssetDatabase.Refresh();

        // Generate stud textures for plant parts
        GenerateStudTextureHere("LeafStud", new Color(0.25f, 0.65f, 0.18f), new Color(0.22f, 0.58f, 0.15f));
        GenerateStudTextureHere("StemStud", new Color(0.3f, 0.55f, 0.15f), new Color(0.26f, 0.48f, 0.12f));
        GenerateStudTextureHere("FruitRedStud", new Color(0.9f, 0.2f, 0.12f), new Color(0.82f, 0.16f, 0.1f));
        GenerateStudTextureHere("FruitOrangeStud", new Color(1f, 0.55f, 0.1f), new Color(0.9f, 0.48f, 0.08f));
        GenerateStudTextureHere("FruitYellowStud", new Color(0.95f, 0.85f, 0.15f), new Color(0.88f, 0.78f, 0.12f));
        GenerateStudTextureHere("SeedBrownStud", new Color(0.5f, 0.35f, 0.15f), new Color(0.44f, 0.3f, 0.12f));
        AssetDatabase.Refresh();

        int count = 0;

        // Find all Design_ objects and all Spot_ plant models
        var targets = new System.Collections.Generic.List<GameObject>();

        // Design objects in DesignArea
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.isLoaded) continue;
            if (go.name.StartsWith("Design_")) targets.Add(go);
        }

        // Spot plant models (Phase_ children of Spot_)
        foreach (var pm in Object.FindObjectsOfType<PlotManager>())
        {
            if (!pm.name.StartsWith("Spot_")) continue;
            foreach (Transform child in pm.transform)
            {
                if (child.name.StartsWith("Phase_"))
                    targets.Add(child.gameObject);
            }
        }

        Shader shader = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
            ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.defaultMaterial.shader
            : Shader.Find("Standard");

        foreach (var target in targets)
        {
            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            {
                Color origColor = Color.white;
                if (r.sharedMaterial != null)
                {
                    origColor = r.sharedMaterial.HasProperty("_BaseColor")
                        ? r.sharedMaterial.GetColor("_BaseColor")
                        : r.sharedMaterial.color;
                }

                string studName = PickStudTexture(r.gameObject.name, origColor);
                Material studMat = CreateStudMatForDesign(studName, shader);
                if (studMat != null)
                {
                    r.sharedMaterial = studMat;
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Stud Texture Applied!",
            count + " part diterapkan stud texture!\n\n" +
            "Semua model tanaman sekarang punya\n" +
            "Roblox stud texture.", "OK");
    }

    static string PickStudTexture(string objName, Color color)
    {
        string lower = objName.ToLower();

        if (lower.Contains("daun") || lower.Contains("leaf") || lower.Contains("husk"))
            return "LeafStud";
        if (lower.Contains("batang") || lower.Contains("stem") || lower.Contains("trunk"))
            return "StemStud";
        if (lower.Contains("benih") || lower.Contains("seed"))
            return "SeedBrownStud";

        // Match by color for fruit/buah
        if (lower.Contains("buah") || lower.Contains("fruit"))
        {
            if (color.r > 0.7f && color.g < 0.4f) return "FruitRedStud";
            if (color.r > 0.7f && color.g > 0.4f && color.g < 0.7f) return "FruitOrangeStud";
            if (color.g > 0.7f || color.b < 0.3f) return "FruitYellowStud";
            return "FruitRedStud";
        }

        // Fallback by color
        if (color.g > color.r && color.g > 0.4f) return "LeafStud";
        if (color.r > 0.7f && color.g < 0.4f) return "FruitRedStud";
        if (color.r > 0.7f && color.g > 0.4f) return "FruitOrangeStud";
        if (color.g > 0.7f) return "FruitYellowStud";

        return "StemStud";
    }

    static Material CreateStudMatForDesign(string texName, Shader shader)
    {
        string matPath = "Assets/Materials/Design_" + texName + ".mat";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/" + texName + ".png");
        if (tex == null) return null;

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat != null)
        {
            mat.shader = shader;
        }
        else
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        mat.SetTexture("_BaseMap", tex);
        mat.SetTextureScale("_BaseMap", new Vector2(2, 2));
        mat.mainTexture = tex;
        mat.mainTextureScale = new Vector2(2, 2);
        mat.SetFloat("_Smoothness", 0.2f);
        mat.SetColor("_BaseColor", Color.white);
        mat.color = Color.white;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void GenerateStudTextureHere(string name, Color baseColor, Color studShadow)
    {
        string dir = "Assets/Textures";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Textures");

        int size = 128;
        int studsPerSide = 4;
        float studRadius = size / (studsPerSide * 2f) * 0.55f;
        float cellSize = size / (float)studsPerSide;

        Texture2D tex = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color pixel = baseColor;
                float cellX = (x % cellSize) - cellSize / 2f;
                float cellY = (y % cellSize) - cellSize / 2f;
                float dist = Mathf.Sqrt(cellX * cellX + cellY * cellY);

                if (dist < studRadius)
                {
                    float edge = studRadius - dist;
                    if (edge < 2f)
                        pixel = studShadow;
                    else
                    {
                        float highlight = Mathf.Clamp01((studRadius - dist) / studRadius);
                        pixel = Color.Lerp(baseColor, baseColor * 1.15f, highlight * 0.5f);
                        if (cellX < 0 && cellY > 0 && dist < studRadius * 0.7f)
                            pixel = Color.Lerp(pixel, Color.white, 0.08f);
                        if (cellX > 0 && cellY < 0 && dist > studRadius * 0.3f)
                            pixel = Color.Lerp(pixel, studShadow, 0.15f);
                    }
                }
                else if (dist < studRadius + 1.5f)
                    pixel = Color.Lerp(baseColor, studShadow, 0.3f);

                tex.SetPixel(x, y, pixel);
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;
        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Textures/" + name + ".png", png);
        Object.DestroyImmediate(tex);
    }

    [MenuItem("Farm/Setup 3 Spots Per Plot")]
    static void SetupPlotSpots()
    {
        // Find all Plot_X objects
        GameObject plotsParent = GameObject.Find("Plots");
        if (plotsParent == null)
        {
            EditorUtility.DisplayDialog("Error", "Plots tidak ditemukan!", "OK");
            return;
        }

        int totalSpots = 0;
        foreach (Transform plotTransform in plotsParent.transform)
        {
            if (!plotTransform.name.StartsWith("Plot_")) continue;
            if (!plotTransform.gameObject.activeSelf) continue;

            // Remove existing PlotManager from main plot (it becomes visual only)
            PlotManager oldPm = plotTransform.GetComponent<PlotManager>();
            if (oldPm != null)
                Object.DestroyImmediate(oldPm);

            // Remove old PlantHolder
            Transform oldHolder = plotTransform.Find("PlantHolder");
            if (oldHolder != null) Object.DestroyImmediate(oldHolder.gameObject);

            // Remove old spots if re-running
            var oldSpots = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in plotTransform)
            {
                if (child.name.StartsWith("Spot_"))
                    oldSpots.Add(child.gameObject);
            }
            foreach (var s in oldSpots) Object.DestroyImmediate(s);

            // Find PlotWall children to determine section positions
            var walls = new System.Collections.Generic.List<float>();
            foreach (Transform child in plotTransform)
            {
                if (child.name.Contains("Wall") && !child.name.Contains("Walls"))
                    walls.Add(child.localPosition.x);
            }
            walls.Sort();

            // Calculate 3 spot X positions based on walls
            // If walls found, place spots between them
            // Otherwise evenly space 3 spots
            float plotHalfX = 0.4f; // local space (0.5 is edge)
            float[] spotXPositions;

            if (walls.Count >= 2)
            {
                // 3 sections: left edge to wall1, wall1 to wall2, wall2 to right edge
                spotXPositions = new float[] {
                    (-plotHalfX + walls[0]) / 2f,
                    (walls[0] + walls[1]) / 2f,
                    (walls[1] + plotHalfX) / 2f
                };
            }
            else
            {
                spotXPositions = new float[] { -0.27f, 0f, 0.27f };
            }

            Vector3 plotScale = plotTransform.localScale;

            for (int i = 0; i < 3; i++)
            {
                GameObject spot = new GameObject("Spot_" + i);
                spot.transform.SetParent(plotTransform);
                spot.transform.localPosition = new Vector3(spotXPositions[i], 0.5f, 0);
                // Inverse scale so children are in world-space proportions
                spot.transform.localScale = new Vector3(1f / plotScale.x, 1f / plotScale.y, 1f / plotScale.z);

                // Collider for interaction (world-space size)
                BoxCollider col = spot.AddComponent<BoxCollider>();
                col.size = new Vector3(plotScale.x / 3.5f, 0.3f, plotScale.z * 0.8f);
                col.center = new Vector3(0, 0.1f, 0);

                // PlotManager — model tanaman otomatis dari Design_ saat Plant()
                spot.AddComponent<PlotManager>();
                spot.AddComponent<InteractionPrompt>();

                EditorUtility.SetDirty(spot);
                totalSpots++;
            }
        }

        // Remove old Seed/Growing/Ready that are direct children of plots
        foreach (Transform plotTransform in plotsParent.transform)
        {
            var toRemove = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in plotTransform)
            {
                if (child.name == "Seed" || child.name == "Growing" || child.name == "Ready")
                    toRemove.Add(child.gameObject);
            }
            foreach (var obj in toRemove) Object.DestroyImmediate(obj);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Plot Spots",
            totalSpots + " spot tanaman dibuat!\n\n" +
            "Setiap plot punya 3 spot.\n" +
            "Jalan ke spot, tekan E untuk tanam.", "OK");
    }

    static Material GetOrCreateMat(string path, Color color, Shader shader)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
        {
            mat.shader = shader;
        }
        else
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        mat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    [MenuItem("Farm/Relink All")]
    static void RelinkAll()
    {
        string dataDir = "Assets/Data";
        PlantData tomat = AssetDatabase.LoadAssetAtPath<PlantData>(dataDir + "/Tomat.asset");
        PlantData wortel = AssetDatabase.LoadAssetAtPath<PlantData>(dataDir + "/Wortel.asset");
        PlantData jagung = AssetDatabase.LoadAssetAtPath<PlantData>(dataDir + "/Jagung.asset");

        GameManager gm = Object.FindObjectOfType<GameManager>();
        if (gm == null) gm = GameObject.Find("GameManager")?.GetComponent<GameManager>();
        if (gm != null && tomat != null && wortel != null && jagung != null)
        {
            gm.availablePlants = new PlantData[] { tomat, wortel, jagung };
            EditorUtility.SetDirty(gm);
        }

        PlayerInteraction pi = Object.FindObjectOfType<PlayerInteraction>();
        if (pi != null && pi.mainCamera == null)
        {
            pi.mainCamera = Camera.main;
            EditorUtility.SetDirty(pi);
        }

        // Add SeedSelectionUI to canvas if missing
        GameObject uiCanvas = GameObject.Find("UICanvas");
        if (uiCanvas != null && uiCanvas.GetComponentInChildren<SeedSelectionUI>() == null)
            uiCanvas.AddComponent<SeedSelectionUI>();

        UIManager uiMgr = Object.FindObjectOfType<UIManager>();
        if (uiMgr != null)
        {
            if (uiMgr.scoreText == null)
                uiMgr.scoreText = GameObject.Find("ScoreText")?.GetComponent<UnityEngine.UI.Text>();
            if (uiMgr.rankText == null)
                uiMgr.rankText = GameObject.Find("RankText")?.GetComponent<UnityEngine.UI.Text>();
            if (uiMgr.harvestText == null)
                uiMgr.harvestText = GameObject.Find("HarvestText")?.GetComponent<UnityEngine.UI.Text>();
            if (uiMgr.infoText == null)
                uiMgr.infoText = GameObject.Find("InfoText")?.GetComponent<UnityEngine.UI.Text>();

            if (uiMgr.plantButtons == null || uiMgr.plantButtons.Length == 0)
            {
                var btns = new System.Collections.Generic.List<UnityEngine.UI.Button>();
                foreach (var b in Object.FindObjectsOfType<UnityEngine.UI.Button>())
                    btns.Add(b);
                btns.Sort((a, b) => a.name.CompareTo(b.name));
                uiMgr.plantButtons = btns.ToArray();
            }
            EditorUtility.SetDirty(uiMgr);
        }
    }

    static void CreateGround()
    {
        if (GameObject.Find("Ground") != null) return;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3, 1, 3);

        Material groundMat = CreateMat(new Color(0.3f, 0.6f, 0.2f));

        string matDir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        AssetDatabase.CreateAsset(groundMat, matDir + "/Ground.mat");
        ground.GetComponent<Renderer>().material = groundMat;
    }

    static void CreatePlots()
    {
        if (GameObject.Find("Plots") != null) return;

        GameObject plotParent = new GameObject("Plots");
        plotParent.transform.position = Vector3.zero;

        Material plotMat = CreateMat(new Color(0.4f, 0.26f, 0.13f));
        AssetDatabase.CreateAsset(plotMat, "Assets/Materials/Plot.mat");

        // 4 rectangular plots in 2x2 grid
        Vector3[] positions = {
            new Vector3(-3.5f, 0.15f, 0.5f),
            new Vector3(3.5f, 0.15f, 0.5f),
            new Vector3(-3.5f, 0.15f, -3.5f),
            new Vector3(3.5f, 0.15f, -3.5f)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plot.name = "Plot_" + i;
            plot.transform.parent = plotParent.transform;
            plot.transform.position = positions[i];
            plot.transform.localScale = new Vector3(5f, 0.3f, 2.5f);
            plot.GetComponent<Renderer>().material = plotMat;
            plot.AddComponent<PlotManager>();

            // Model tanaman otomatis dari Design_ saat runtime
        }
    }

    static void CreatePlantModels(GameObject plot, int index)
    {
        GameObject seed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        seed.name = "Seed";
        seed.transform.parent = plot.transform;
        seed.transform.localPosition = new Vector3(0, 1f, 0);
        seed.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

        Material seedMat = CreateMat(new Color(0.6f, 0.4f, 0.1f));
        if (index == 0)
            AssetDatabase.CreateAsset(seedMat, "Assets/Materials/Seed.mat");
        else
            seedMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Seed.mat");
        seed.GetComponent<Renderer>().material = seedMat;
        seed.SetActive(false);

        GameObject growing = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        growing.name = "Growing";
        growing.transform.parent = plot.transform;
        growing.transform.localPosition = new Vector3(0, 1.2f, 0);
        growing.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);

        Material growMat = CreateMat(new Color(0.2f, 0.7f, 0.1f));
        if (index == 0)
            AssetDatabase.CreateAsset(growMat, "Assets/Materials/Growing.mat");
        else
            growMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Growing.mat");
        growing.GetComponent<Renderer>().material = growMat;
        growing.SetActive(false);

        GameObject ready = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ready.name = "Ready";
        ready.transform.parent = plot.transform;
        ready.transform.localPosition = new Vector3(0, 1.5f, 0);
        ready.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);

        Material readyMat = CreateMat(new Color(0.9f, 0.2f, 0.1f));
        if (index == 0)
            AssetDatabase.CreateAsset(readyMat, "Assets/Materials/Ready.mat");
        else
            readyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ready.mat");
        ready.GetComponent<Renderer>().material = readyMat;
        ready.SetActive(false);

        // PlotManager models are now auto-loaded from Design_ at runtime
    }

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.transform.position = new Vector3(0, 15, -10);
        cam.transform.rotation = Quaternion.Euler(55, 0, 0);

        if (cam.GetComponent<CameraController>() == null)
            cam.gameObject.AddComponent<CameraController>();
    }

    static void CreateGameManager()
    {
        if (GameObject.Find("GameManager") != null) return;

        GameObject gm = new GameObject("GameManager");
        GameManager gmComp = gm.AddComponent<GameManager>();

        string dataDir = "Assets/Data";
        if (!AssetDatabase.IsValidFolder(dataDir))
            AssetDatabase.CreateFolder("Assets", "Data");

        PlantData tomat = AssetDatabase.LoadAssetAtPath<PlantData>(dataDir + "/Tomat.asset");
        PlantData wortel = AssetDatabase.LoadAssetAtPath<PlantData>(dataDir + "/Wortel.asset");
        PlantData jagung = AssetDatabase.LoadAssetAtPath<PlantData>(dataDir + "/Jagung.asset");

        if (tomat != null && wortel != null && jagung != null)
            gmComp.availablePlants = new PlantData[] { tomat, wortel, jagung };
    }

    static void CreatePlantDataAssets()
    {
        string dataDir = "Assets/Data";
        if (!AssetDatabase.IsValidFolder(dataDir))
            AssetDatabase.CreateFolder("Assets", "Data");

        CreatePlantAsset(dataDir, "Tomat", 10, 15f);
        CreatePlantAsset(dataDir, "Wortel", 15, 20f);
        CreatePlantAsset(dataDir, "Jagung", 20, 30f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void CreatePlantAsset(string dir, string name, int score, float time)
    {
        string path = dir + "/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<PlantData>(path) != null) return;

        PlantData data = ScriptableObject.CreateInstance<PlantData>();
        data.plantName = name;
        data.scoreValue = score;
        data.growthTime = time;
        AssetDatabase.CreateAsset(data, path);
    }

    static void CreatePlayerInteraction()
    {
        if (GameObject.Find("PlayerInteraction") != null) return;

        GameObject pi = new GameObject("PlayerInteraction");
        PlayerInteraction piComp = pi.AddComponent<PlayerInteraction>();
        piComp.mainCamera = Camera.main;
    }

    static void CreateUI()
    {
        if (GameObject.Find("UICanvas") != null) return;

        GameObject canvasObj = new GameObject("UICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // Top panel
        GameObject topPanel = CreatePanel(canvasObj.transform, "TopPanel",
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(0, -60));

        CreateText(topPanel.transform, "ScoreText", "Skor: 0",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, -25), 24);

        CreateText(topPanel.transform, "RankText", "Petani Pemula",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -25), 22);

        CreateText(topPanel.transform, "HarvestText", "Panen: 0",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, -25), 24);

        // Info text
        CreateText(canvasObj.transform, "InfoText", "",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 80), 20);

        // Button panel
        GameObject btnPanel = CreatePanel(canvasObj.transform, "ButtonPanel",
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 10), new Vector2(0, 60));

        string[] plantNames = { "Tomat (+10)", "Wortel (+15)", "Jagung (+20)" };
        Color[] btnColors = {
            new Color(0.9f, 0.3f, 0.2f),
            new Color(1f, 0.6f, 0.2f),
            new Color(0.9f, 0.8f, 0.2f)
        };

        Button[] buttons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            float xPos = -200 + i * 200;
            GameObject btn = CreateButton(btnPanel.transform, "Btn_" + plantNames[i],
                plantNames[i], new Vector2(xPos, 0), btnColors[i]);
            buttons[i] = btn.GetComponent<Button>();
        }

        // Setup UIManager
        GameObject uiManagerObj = GameObject.Find("UIManager");
        if (uiManagerObj == null)
            uiManagerObj = new GameObject("UIManager");

        UIManager uiMgr = uiManagerObj.GetComponent<UIManager>();
        if (uiMgr == null)
            uiMgr = uiManagerObj.AddComponent<UIManager>();

        uiMgr.scoreText = GameObject.Find("ScoreText")?.GetComponent<Text>();
        uiMgr.rankText = GameObject.Find("RankText")?.GetComponent<Text>();
        uiMgr.harvestText = GameObject.Find("HarvestText")?.GetComponent<Text>();
        uiMgr.infoText = GameObject.Find("InfoText")?.GetComponent<Text>();
        uiMgr.plantButtons = buttons;

        // Link GameManager plants
        GameManager gm = GameObject.Find("GameManager")?.GetComponent<GameManager>();
        if (gm != null)
        {
            string dataDir = "Assets/Data";
            PlantData tomat = AssetDatabase.LoadAssetAtPath<PlantData>(dataDir + "/Tomat.asset");
            PlantData wortel = AssetDatabase.LoadAssetAtPath<PlantData>(dataDir + "/Wortel.asset");
            PlantData jagung = AssetDatabase.LoadAssetAtPath<PlantData>(dataDir + "/Jagung.asset");
            if (tomat && wortel && jagung)
                gm.availablePlants = new PlantData[] { tomat, wortel, jagung };
        }
    }

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.5f);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.sizeDelta = new Vector2(0, 50);

        return panel;
    }

    static void CreateText(Transform parent, string name, string content,
        Vector2 anchor, Vector2 pivot, Vector2 pos, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 40);
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 pos, Color color)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        btnObj.AddComponent<Button>();

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(170, 40);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return btnObj;
    }
}
