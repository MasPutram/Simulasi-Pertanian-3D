using UnityEngine;
using UnityEditor;

public class RobloxStyle : EditorWindow
{
    [MenuItem("Farm/Apply Roblox Style")]
    static void ApplyRobloxStyle()
    {
        GenerateStudTextures();
        ApplyStudMaterials();
        StyleTrees();
        MakeRaisedPlots();
        CreatePlayerCharacter();
        CreateSeedShelf();

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Roblox Style!",
            "Classic Roblox/LEGO style diterapkan!\n" +
            "- Stud texture di semua permukaan\n" +
            "- Plot raised bed tebal\n" +
            "- Pohon blocky style", "OK");
    }

    static void GenerateStudTextures()
    {
        string dir = "Assets/Textures";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Textures");

        // Green stud for grass
        GenerateStudTexture("GrassStud", new Color(0.3f, 0.7f, 0.2f), new Color(0.28f, 0.65f, 0.18f));
        // Dark green for outer
        GenerateStudTexture("DarkGrassStud", new Color(0.25f, 0.6f, 0.18f), new Color(0.22f, 0.55f, 0.15f));
        // Brown stud for soil/plots
        GenerateStudTexture("SoilStud", new Color(0.52f, 0.36f, 0.2f), new Color(0.48f, 0.32f, 0.17f));
        // Wood stud for fences
        GenerateStudTexture("WoodStud", new Color(0.6f, 0.4f, 0.18f), new Color(0.55f, 0.36f, 0.15f));
        // Grey stud for rocks/path
        GenerateStudTexture("GreyStud", new Color(0.65f, 0.65f, 0.63f), new Color(0.6f, 0.6f, 0.58f));
        // Mountain green
        GenerateStudTexture("MtStud", new Color(0.35f, 0.6f, 0.28f), new Color(0.32f, 0.55f, 0.25f));
        // Dark brown for plot sides
        GenerateStudTexture("PlotSideStud", new Color(0.38f, 0.24f, 0.12f), new Color(0.34f, 0.2f, 0.09f));

        AssetDatabase.Refresh();
    }

    static void GenerateStudTexture(string name, Color baseColor, Color studShadow)
    {
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

                // Check if this pixel is near a stud center
                float cellX = (x % cellSize) - cellSize / 2f;
                float cellY = (y % cellSize) - cellSize / 2f;
                float dist = Mathf.Sqrt(cellX * cellX + cellY * cellY);

                if (dist < studRadius)
                {
                    // Inside stud circle
                    float edge = studRadius - dist;

                    if (edge < 2f)
                    {
                        // Stud rim - darker
                        pixel = studShadow;
                    }
                    else
                    {
                        // Stud top - slightly brighter
                        float highlight = Mathf.Clamp01((studRadius - dist) / studRadius);
                        pixel = Color.Lerp(baseColor, baseColor * 1.15f, highlight * 0.5f);

                        // Top-left highlight
                        if (cellX < 0 && cellY > 0 && dist < studRadius * 0.7f)
                            pixel = Color.Lerp(pixel, Color.white, 0.08f);

                        // Bottom-right shadow
                        if (cellX > 0 && cellY < 0 && dist > studRadius * 0.3f)
                            pixel = Color.Lerp(pixel, studShadow, 0.15f);
                    }
                }
                else if (dist < studRadius + 1.5f)
                {
                    // Just outside stud - subtle shadow
                    pixel = Color.Lerp(baseColor, studShadow, 0.3f);
                }

                tex.SetPixel(x, y, pixel);
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point; // Crisp pixels!

        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Textures/" + name + ".png", png);
        Object.DestroyImmediate(tex);
    }

    static Material CreateStudMaterial(string texName, string matName, float tiling)
    {
        string matPath = "Assets/Materials/" + matName + ".mat";

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/" + texName + ".png");
        if (tex == null) return null;

        Shader shader = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
            ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.defaultMaterial.shader
            : Shader.Find("Standard");

        // Always recreate to ensure correct shader and texture
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
        mat.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
        mat.mainTexture = tex;
        mat.mainTextureScale = new Vector2(tiling, tiling);
        mat.SetFloat("_Smoothness", 0.2f);
        mat.SetColor("_BaseColor", Color.white);
        mat.color = Color.white;
        EditorUtility.SetDirty(mat);

        return mat;
    }

    static void ApplyStudMaterials()
    {
        AssetDatabase.Refresh();

        // Ground
        Material grassMat = CreateStudMaterial("GrassStud", "Ground", 12);
        GameObject ground = GameObject.Find("Ground");
        if (ground) ground.GetComponent<Renderer>().sharedMaterial = grassMat;

        // Outer ground
        Material darkGrassMat = CreateStudMaterial("DarkGrassStud", "OuterGround", 40);
        GameObject outerGround = GameObject.Find("OuterGround");
        if (outerGround) outerGround.GetComponent<Renderer>().sharedMaterial = darkGrassMat;

        // Plots
        Material soilMat = CreateStudMaterial("SoilStud", "Plot", 4);
        Material soilTopMat = CreateStudMaterial("SoilStud", "PlotTop", 4);
        foreach (var plot in Object.FindObjectsOfType<PlotManager>())
        {
            Renderer r = plot.GetComponent<Renderer>();
            if (r) r.sharedMaterial = soilTopMat;
        }

        // Fences
        Material woodMat = CreateStudMaterial("WoodStud", "Fence", 3);
        GameObject fences = GameObject.Find("Fences");
        if (fences)
        {
            foreach (var r in fences.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = woodMat;
        }

        // Mountains
        Material mtMat = CreateStudMaterial("MtStud", "Mountain1", 8);
        Material mtMat2 = CreateStudMaterial("DarkGrassStud", "Mountain2", 8);
        GameObject mountains = GameObject.Find("Mountains");
        if (mountains)
        {
            foreach (var r in mountains.GetComponentsInChildren<Renderer>())
            {
                if (r.gameObject.name == "SnowCap")
                {
                    Material snowMat = CreateStudMaterial("GreyStud", "MountainSnow", 4);
                    r.sharedMaterial = snowMat;
                }
                else
                {
                    r.sharedMaterial = Random.value > 0.5f ? mtMat : mtMat2;
                }
            }
        }

        // Trees - trunk
        Material trunkStudMat = CreateStudMaterial("WoodStud", "Trunk", 2);
        Material leavesStudMat = CreateStudMaterial("GrassStud", "Leaves", 2);
        GameObject env = GameObject.Find("Environment");
        if (env)
        {
            foreach (var r in env.GetComponentsInChildren<Renderer>())
            {
                if (r.gameObject.name == "Trunk")
                    r.sharedMaterial = trunkStudMat;
                else if (r.gameObject.name == "Leaves")
                    r.sharedMaterial = leavesStudMat;
                else if (r.gameObject.name == "Rock")
                {
                    Material rockMat = CreateStudMaterial("GreyStud", "Rock", 2);
                    r.sharedMaterial = rockMat;
                }
            }
        }
    }

    static void MakeRaisedPlots()
    {
        AssetDatabase.Refresh();
        Material sideMat = CreateStudMaterial("PlotSideStud", "PlotSide", 3);
        Material topMat = CreateStudMaterial("SoilStud", "PlotTop", 6);

        // 4 rectangular plots - 2x2 grid
        Vector3[] plotPositions = {
            new Vector3(-3.5f, 0.3f, 0.5f),
            new Vector3(3.5f, 0.3f, 0.5f),
            new Vector3(-3.5f, 0.3f, -3.5f),
            new Vector3(3.5f, 0.3f, -3.5f)
        };

        var allPlots = Object.FindObjectsOfType<PlotManager>();
        // Sort by name for consistent ordering
        System.Array.Sort(allPlots, (a, b) => a.name.CompareTo(b.name));

        int idx = 0;
        foreach (var plot in allPlots)
        {
            // Hide extra plots beyond 4
            if (idx >= 4)
            {
                plot.gameObject.SetActive(false);
                idx++;
                continue;
            }
            // Clean up old remnants
            var toClean = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in plot.transform)
            {
                if (child.name == "SideWalls" || child.name == "SoilRows" ||
                    child.name == "CornerPost" || child.name == "PlotWalls")
                    toClean.Add(child.gameObject);
            }
            foreach (var obj in toClean)
                Object.DestroyImmediate(obj);

            // Position and size - rectangular raised bed
            if (idx < plotPositions.Length)
                plot.transform.position = plotPositions[idx];
            plot.transform.localScale = new Vector3(5f, 0.6f, 2.5f);

            Renderer r = plot.GetComponent<Renderer>();
            if (r) r.sharedMaterial = topMat;

            // Side walls
            GameObject walls = new GameObject("PlotWalls");
            walls.transform.SetParent(plot.transform);
            walls.transform.localPosition = Vector3.zero;

            // Front/back walls
            CreateWall(walls.transform, sideMat, new Vector3(0, 0, 0.5f), new Vector3(1.01f, 1f, 0.02f));
            CreateWall(walls.transform, sideMat, new Vector3(0, 0, -0.5f), new Vector3(1.01f, 1f, 0.02f));
            // Left/right walls
            CreateWall(walls.transform, sideMat, new Vector3(0.5f, 0, 0), new Vector3(0.02f, 1f, 1.01f));
            CreateWall(walls.transform, sideMat, new Vector3(-0.5f, 0, 0), new Vector3(0.02f, 1f, 1.01f));

            // Top rim
            CreateWall(walls.transform, sideMat, new Vector3(0, 0.52f, 0.5f), new Vector3(1.02f, 0.05f, 0.03f));
            CreateWall(walls.transform, sideMat, new Vector3(0, 0.52f, -0.5f), new Vector3(1.02f, 0.05f, 0.03f));
            CreateWall(walls.transform, sideMat, new Vector3(0.5f, 0.52f, 0), new Vector3(0.03f, 0.05f, 1.02f));
            CreateWall(walls.transform, sideMat, new Vector3(-0.5f, 0.52f, 0), new Vector3(0.03f, 0.05f, 1.02f));

            // Create a PlantHolder with inverse scale so plants aren't distorted
            Transform oldHolder = plot.transform.Find("PlantHolder");
            if (oldHolder != null)
                Object.DestroyImmediate(oldHolder.gameObject);

            GameObject holder = new GameObject("PlantHolder");
            holder.transform.SetParent(plot.transform);
            holder.transform.localPosition = new Vector3(0, 0.5f, 0);
            // Compensate parent's non-uniform scale
            Vector3 ps = plot.transform.localScale;
            holder.transform.localScale = new Vector3(1f / ps.x, 1f / ps.y, 1f / ps.z);

            // Model tanaman sekarang otomatis dari Design_ saat runtime

            // Move harvest effect particle if exists
            ParticleSystem ps2 = plot.GetComponentInChildren<ParticleSystem>();
            if (ps2 != null)
            {
                ps2.transform.SetParent(holder.transform);
                ps2.transform.localPosition = new Vector3(0, 0.2f, 0);
            }

            // Move progress bar to holder too
            Canvas progCanvas = plot.GetComponentInChildren<Canvas>();
            if (progCanvas != null)
            {
                progCanvas.transform.SetParent(holder.transform);
                progCanvas.transform.localPosition = new Vector3(0, 1.5f, 0);
                progCanvas.transform.localScale = Vector3.one * 0.01f;
            }

            // Interaction prompt
            if (plot.GetComponent<InteractionPrompt>() == null)
                plot.gameObject.AddComponent<InteractionPrompt>();

            EditorUtility.SetDirty(plot);
            idx++;
        }
    }

    static void CreateSeedShelf()
    {
        // Remove old shelf
        GameObject oldShelf = GameObject.Find("SeedShelf");
        if (oldShelf != null)
            Object.DestroyImmediate(oldShelf);

        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Fence.mat");
        Material tomatMat = GetOrCreateColorMat("ShelfTomat", new Color(0.9f, 0.2f, 0.15f));
        Material wortelMat = GetOrCreateColorMat("ShelfWortel", new Color(1f, 0.5f, 0.1f));
        Material jagungMat = GetOrCreateColorMat("ShelfJagung", new Color(0.95f, 0.85f, 0.2f));
        Material signMat = GetOrCreateColorMat("ShelfSign", new Color(0.3f, 0.55f, 0.2f));

        GameObject shelf = new GameObject("SeedShelf");
        shelf.transform.position = new Vector3(0, 0, 4f);

        // Table top
        var table = MakePart(shelf.transform, "Table", new Vector3(0, 0.7f, 0), new Vector3(2.5f, 0.15f, 1f), woodMat);

        // Table legs
        MakePart(shelf.transform, "Leg", new Vector3(-1.1f, 0.35f, -0.35f), new Vector3(0.15f, 0.7f, 0.15f), woodMat);
        MakePart(shelf.transform, "Leg", new Vector3(1.1f, 0.35f, -0.35f), new Vector3(0.15f, 0.7f, 0.15f), woodMat);
        MakePart(shelf.transform, "Leg", new Vector3(-1.1f, 0.35f, 0.35f), new Vector3(0.15f, 0.7f, 0.15f), woodMat);
        MakePart(shelf.transform, "Leg", new Vector3(1.1f, 0.35f, 0.35f), new Vector3(0.15f, 0.7f, 0.15f), woodMat);

        // Back board / sign
        MakePart(shelf.transform, "BackBoard", new Vector3(0, 1.3f, -0.4f), new Vector3(2.5f, 1f, 0.1f), signMat);

        // Seed boxes on table (table top at 0.775)
        MakePart(shelf.transform, "TomatBox", new Vector3(-0.7f, 0.9f, 0.05f), new Vector3(0.45f, 0.25f, 0.45f), tomatMat);
        MakePart(shelf.transform, "WortelBox", new Vector3(0, 0.9f, 0.05f), new Vector3(0.45f, 0.25f, 0.45f), wortelMat);
        MakePart(shelf.transform, "JagungBox", new Vector3(0.7f, 0.9f, 0.05f), new Vector3(0.45f, 0.25f, 0.45f), jagungMat);

        // Sign text via world space canvas
        GameObject canvasObj = new GameObject("ShelfLabel");
        canvasObj.transform.SetParent(shelf.transform);
        canvasObj.transform.localPosition = new Vector3(0, 1.3f, -0.46f);
        canvasObj.transform.localScale = Vector3.one * 0.01f;
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform crt = canvasObj.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(250, 60);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform, false);
        var text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "BENIH";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 40;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        // Add collider for interaction detection
        BoxCollider col = shelf.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.7f, 0);
        col.size = new Vector3(3f, 1.5f, 1.5f);
        col.isTrigger = true;

        shelf.AddComponent<SeedShelfMarker>();
        shelf.AddComponent<InteractionPrompt>();
    }

    static GameObject MakePart(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }

    static void CreateWall(Transform parent, Material mat, Vector3 localPos, Vector3 localScale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = localScale;
        wall.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(wall.GetComponent<Collider>());
    }

    static Material GetOrCreateColorMat(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
            ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.defaultMaterial.shader
            : Shader.Find("Standard");
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

    static void CreatePlayerCharacter()
    {
        // Remove old player if re-applying
        GameObject oldPlayer = GameObject.FindGameObjectWithTag("Player");
        if (oldPlayer != null)
            Object.DestroyImmediate(oldPlayer);

        Material torsoMat = GetOrCreateColorMat("PlayerTorso", new Color(0.2f, 0.5f, 0.85f));
        Material headMat = GetOrCreateColorMat("PlayerHead", new Color(0.95f, 0.8f, 0.6f));
        Material eyeMat = GetOrCreateColorMat("PlayerEye", new Color(0.1f, 0.1f, 0.1f));
        Material armMat = GetOrCreateColorMat("PlayerArm", new Color(0.95f, 0.8f, 0.6f));
        Material legMat = GetOrCreateColorMat("PlayerLeg", new Color(0.15f, 0.35f, 0.15f));

        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1.1f, -4f);

        // Torso
        var torso = MakePart(player.transform, "Torso", new Vector3(0, 0.5f, 0), new Vector3(0.8f, 1f, 0.5f), torsoMat);

        // Head
        var head = MakePart(player.transform, "Head", new Vector3(0, 1.3f, 0), new Vector3(0.65f, 0.65f, 0.65f), headMat);

        // Eyes & smile on head
        MakePart(head.transform, "EyeL", new Vector3(-0.2f, 0.1f, 0.5f), new Vector3(0.15f, 0.15f, 0.08f), eyeMat);
        MakePart(head.transform, "EyeR", new Vector3(0.2f, 0.1f, 0.5f), new Vector3(0.15f, 0.15f, 0.08f), eyeMat);
        MakePart(head.transform, "Smile", new Vector3(0, -0.15f, 0.5f), new Vector3(0.3f, 0.06f, 0.05f), eyeMat);

        // Arms
        MakePart(player.transform, "ArmL", new Vector3(-0.55f, 0.5f, 0), new Vector3(0.3f, 1f, 0.5f), armMat);
        MakePart(player.transform, "ArmR", new Vector3(0.55f, 0.5f, 0), new Vector3(0.3f, 1f, 0.5f), armMat);

        // Legs
        MakePart(player.transform, "LegL", new Vector3(-0.2f, -0.5f, 0), new Vector3(0.4f, 1f, 0.5f), legMat);
        MakePart(player.transform, "LegR", new Vector3(0.2f, -0.5f, 0), new Vector3(0.4f, 1f, 0.5f), legMat);

        // Add capsule collider for physics
        CapsuleCollider col = player.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.5f, 0);
        col.radius = 0.4f;
        col.height = 2f;

        // Add rigidbody (kinematic - no physics falling)
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        // Add player controller
        player.AddComponent<PlayerController>();

        // Link camera to player
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraController cc = cam.GetComponent<CameraController>();
            if (cc != null)
                cc.target = player.transform;
        }

        EditorUtility.SetDirty(player);
    }

    static void StyleTrees()
    {
        GameObject env = GameObject.Find("Environment");
        if (env == null) return;

        Material leavesMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Leaves.mat");

        foreach (Transform tree in env.transform)
        {
            if (!tree.name.StartsWith("Tree")) continue;

            var toReplace = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in tree)
            {
                if (child.name != "Leaves") continue;
                var mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.Contains("Sphere"))
                    toReplace.Add(child.gameObject);
            }

            if (toReplace.Count == 0) continue;

            var positions = new System.Collections.Generic.List<Vector3>();
            var scales = new System.Collections.Generic.List<Vector3>();
            foreach (var obj in toReplace)
            {
                positions.Add(obj.transform.position);
                scales.Add(obj.transform.localScale);
            }

            foreach (var obj in toReplace)
                Object.DestroyImmediate(obj);

            for (int i = 0; i < positions.Count; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Leaves";
                cube.transform.SetParent(tree);
                cube.transform.position = positions[i];
                Vector3 s = scales[i];
                cube.transform.localScale = new Vector3(s.x * 0.85f, s.y * 0.85f, s.z * 0.85f);
                cube.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 45), 0);
                if (leavesMat) cube.GetComponent<Renderer>().sharedMaterial = leavesMat;
                Object.DestroyImmediate(cube.GetComponent<Collider>());
            }
        }
    }
}
