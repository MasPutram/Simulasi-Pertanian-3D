using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class ScenePolish : EditorWindow
{
    //[MenuItem("Farm/Polish Scene")]
    static void PolishScene()
    {
        PolishLighting();
        AddFences();
        AddEnvironment();
        AddMountains();
        PolishUI();
        AddProgressBars();

        EditorUtility.DisplayDialog("Polish Selesai!",
            "Visual sudah ditingkatkan:\n" +
            "- Lighting lebih hangat\n" +
            "- Pagar sekeliling lahan\n" +
            "- UI lebih rapi\n" +
            "- Progress bar di setiap plot\n\n" +
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

    static void PolishLighting()
    {
        Light sun = Object.FindObjectOfType<Light>();
        if (sun != null)
        {
            sun.color = new Color(1f, 0.98f, 0.9f);
            sun.intensity = 3f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            sun.shadows = LightShadows.Soft;
        }

        RenderSettings.ambientLight = new Color(0.7f, 0.75f, 0.8f);
        RenderSettings.ambientIntensity = 1.5f;
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
        }
    }

    static void AddEnvironment()
    {
        if (GameObject.Find("Environment") != null) return;

        GameObject envParent = new GameObject("Environment");

        // Outer ground (larger, darker green)
        GameObject outerGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
        outerGround.name = "OuterGround";
        outerGround.transform.parent = envParent.transform;
        outerGround.transform.position = new Vector3(0, -0.01f, 0);
        outerGround.transform.localScale = new Vector3(10, 1, 10);
        Material outerMat = CreateMat(new Color(0.2f, 0.45f, 0.15f));
        AssetDatabase.CreateAsset(outerMat, "Assets/Materials/OuterGround.mat");
        outerGround.GetComponent<Renderer>().material = outerMat;
        Object.DestroyImmediate(outerGround.GetComponent<Collider>());

        // Simple trees around the farm
        Material trunkMat = CreateMat(new Color(0.45f, 0.3f, 0.12f));
        AssetDatabase.CreateAsset(trunkMat, "Assets/Materials/Trunk.mat");
        Material leavesMat = CreateMat(new Color(0.15f, 0.5f, 0.1f));
        AssetDatabase.CreateAsset(leavesMat, "Assets/Materials/Leaves.mat");

        Vector3[] treePositions = {
            new Vector3(-20, 0, 20), new Vector3(-10, 0, 22), new Vector3(5, 0, 21),
            new Vector3(15, 0, 19), new Vector3(22, 0, 10), new Vector3(21, 0, -5),
            new Vector3(20, 0, -18), new Vector3(8, 0, -22), new Vector3(-8, 0, -20),
            new Vector3(-22, 0, -8), new Vector3(-21, 0, 8), new Vector3(-18, 0, 18),
            new Vector3(25, 0, 0), new Vector3(-25, 0, 5), new Vector3(0, 0, 25),
        };

        foreach (var treePos in treePositions)
        {
            float scale = Random.Range(0.8f, 1.3f);
            CreateTree(envParent.transform, treePos, scale, trunkMat, leavesMat);
        }

        // Decorative rocks
        Material rockMat = CreateMat(new Color(0.5f, 0.5f, 0.45f));
        AssetDatabase.CreateAsset(rockMat, "Assets/Materials/Rock.mat");

        Vector3[] rockPositions = {
            new Vector3(-17, 0.2f, -15), new Vector3(18, 0.15f, 14),
            new Vector3(-14, 0.25f, 16), new Vector3(16, 0.2f, -16)
        };
        foreach (var rockPos in rockPositions)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Rock";
            rock.transform.parent = envParent.transform;
            rock.transform.position = rockPos;
            float s = Random.Range(0.5f, 1.2f);
            rock.transform.localScale = new Vector3(s * 1.5f, s * 0.6f, s);
            rock.GetComponent<Renderer>().material = rockMat;
            Object.DestroyImmediate(rock.GetComponent<Collider>());
        }
    }

    static void CreateTree(Transform parent, Vector3 pos, float scale, Material trunkMat, Material leavesMat)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.parent = parent;
        tree.transform.position = pos;

        // Trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.parent = tree.transform;
        trunk.transform.localPosition = new Vector3(0, 1.5f * scale, 0);
        trunk.transform.localScale = new Vector3(0.4f * scale, 1.5f * scale, 0.4f * scale);
        trunk.GetComponent<Renderer>().material = trunkMat;
        Object.DestroyImmediate(trunk.GetComponent<Collider>());

        // Leaves (3 spheres stacked)
        float[] leafY = { 3f, 3.8f, 4.4f };
        float[] leafS = { 2.5f, 2f, 1.3f };
        for (int i = 0; i < 3; i++)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "Leaves";
            leaf.transform.parent = tree.transform;
            leaf.transform.localPosition = new Vector3(0, leafY[i] * scale, 0);
            float ls = leafS[i] * scale;
            leaf.transform.localScale = new Vector3(ls, ls * 0.7f, ls);
            leaf.GetComponent<Renderer>().material = leavesMat;
            Object.DestroyImmediate(leaf.GetComponent<Collider>());
        }
    }

    static void AddMountains()
    {
        if (GameObject.Find("Mountains") != null) return;

        GameObject mtParent = new GameObject("Mountains");

        Material mtMat1 = CreateMat(new Color(0.35f, 0.5f, 0.3f));
        AssetDatabase.CreateAsset(mtMat1, "Assets/Materials/Mountain1.mat");
        Material mtMat2 = CreateMat(new Color(0.3f, 0.42f, 0.28f));
        AssetDatabase.CreateAsset(mtMat2, "Assets/Materials/Mountain2.mat");
        Material mtSnow = CreateMat(new Color(0.9f, 0.92f, 0.95f));
        AssetDatabase.CreateAsset(mtSnow, "Assets/Materials/MountainSnow.mat");

        // Back mountains (behind the farm)
        CreateMountain(mtParent.transform, new Vector3(-30, 0, 50), 18f, 25f, mtMat1);
        CreateMountain(mtParent.transform, new Vector3(-10, 0, 55), 25f, 35f, mtMat2);
        CreateMountain(mtParent.transform, new Vector3(15, 0, 52), 20f, 30f, mtMat1);
        CreateMountain(mtParent.transform, new Vector3(35, 0, 48), 15f, 22f, mtMat2);
        CreateMountain(mtParent.transform, new Vector3(-5, 0, 60), 30f, 40f, mtMat2);

        // Snow caps on tall mountains
        CreateSnowCap(mtParent.transform, new Vector3(-10, 30f, 55), 8f, mtSnow);
        CreateSnowCap(mtParent.transform, new Vector3(-5, 35f, 60), 10f, mtSnow);
        CreateSnowCap(mtParent.transform, new Vector3(15, 25f, 52), 7f, mtSnow);

        // Left mountains
        CreateMountain(mtParent.transform, new Vector3(-50, 0, 20), 15f, 20f, mtMat1);
        CreateMountain(mtParent.transform, new Vector3(-55, 0, 0), 18f, 25f, mtMat2);
        CreateMountain(mtParent.transform, new Vector3(-48, 0, -15), 12f, 18f, mtMat1);

        // Right mountains
        CreateMountain(mtParent.transform, new Vector3(50, 0, 15), 16f, 22f, mtMat2);
        CreateMountain(mtParent.transform, new Vector3(55, 0, -5), 14f, 20f, mtMat1);
        CreateMountain(mtParent.transform, new Vector3(48, 0, -20), 13f, 18f, mtMat2);

        // Small hills closer to farm
        CreateMountain(mtParent.transform, new Vector3(-30, 0, 30), 8f, 10f, mtMat1);
        CreateMountain(mtParent.transform, new Vector3(30, 0, 32), 10f, 12f, mtMat1);
        CreateMountain(mtParent.transform, new Vector3(0, 0, -40), 12f, 15f, mtMat2);
        CreateMountain(mtParent.transform, new Vector3(-25, 0, -35), 10f, 12f, mtMat1);
        CreateMountain(mtParent.transform, new Vector3(28, 0, -38), 9f, 11f, mtMat2);
    }

    static void CreateMountain(Transform parent, Vector3 pos, float radius, float height, Material mat)
    {
        GameObject mt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mt.name = "Mountain";
        mt.transform.parent = parent;
        mt.transform.position = pos;
        mt.transform.localScale = new Vector3(radius * 2, height, radius * 2);
        mt.GetComponent<Renderer>().material = mat;
        Object.DestroyImmediate(mt.GetComponent<Collider>());
    }

    static void CreateSnowCap(Transform parent, Vector3 pos, float size, Material mat)
    {
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = "SnowCap";
        cap.transform.parent = parent;
        cap.transform.position = pos;
        cap.transform.localScale = new Vector3(size, size * 0.5f, size);
        cap.GetComponent<Renderer>().material = mat;
        Object.DestroyImmediate(cap.GetComponent<Collider>());
    }

    static void AddFences()
    {
        if (GameObject.Find("Fences") != null) return;

        GameObject fenceParent = new GameObject("Fences");

        string matDir = "Assets/Materials";
        Material fenceMat;
        string fencePath = matDir + "/Fence.mat";
        fenceMat = AssetDatabase.LoadAssetAtPath<Material>(fencePath);
        if (fenceMat == null)
        {
            fenceMat = CreateMat(new Color(0.55f, 0.35f, 0.15f));
            AssetDatabase.CreateAsset(fenceMat, fencePath);
        }

        float groundSize = 15f;
        float fenceHeight = 0.8f;
        float postSize = 0.2f;

        // 4 fence walls
        CreateFenceWall(fenceParent.transform, fenceMat, new Vector3(0, fenceHeight / 2, groundSize),
            new Vector3(groundSize * 2, fenceHeight, 0.15f));
        CreateFenceWall(fenceParent.transform, fenceMat, new Vector3(0, fenceHeight / 2, -groundSize),
            new Vector3(groundSize * 2, fenceHeight, 0.15f));
        CreateFenceWall(fenceParent.transform, fenceMat, new Vector3(groundSize, fenceHeight / 2, 0),
            new Vector3(0.15f, fenceHeight, groundSize * 2));
        CreateFenceWall(fenceParent.transform, fenceMat, new Vector3(-groundSize, fenceHeight / 2, 0),
            new Vector3(0.15f, fenceHeight, groundSize * 2));

        // Corner posts
        Vector3[] corners = {
            new Vector3(groundSize, 0, groundSize),
            new Vector3(-groundSize, 0, groundSize),
            new Vector3(groundSize, 0, -groundSize),
            new Vector3(-groundSize, 0, -groundSize)
        };
        foreach (var c in corners)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.parent = fenceParent.transform;
            post.transform.position = new Vector3(c.x, fenceHeight / 2, c.z);
            post.transform.localScale = new Vector3(postSize, fenceHeight / 2, postSize);
            post.GetComponent<Renderer>().material = fenceMat;
        }

        // Fence posts along edges
        for (float x = -groundSize + 3; x < groundSize; x += 3f)
        {
            CreatePost(fenceParent.transform, fenceMat, new Vector3(x, fenceHeight / 2, groundSize), postSize, fenceHeight);
            CreatePost(fenceParent.transform, fenceMat, new Vector3(x, fenceHeight / 2, -groundSize), postSize, fenceHeight);
        }
        for (float z = -groundSize + 3; z < groundSize; z += 3f)
        {
            CreatePost(fenceParent.transform, fenceMat, new Vector3(groundSize, fenceHeight / 2, z), postSize, fenceHeight);
            CreatePost(fenceParent.transform, fenceMat, new Vector3(-groundSize, fenceHeight / 2, z), postSize, fenceHeight);
        }
    }

    static void CreateFenceWall(Transform parent, Material mat, Vector3 pos, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "FenceWall";
        wall.transform.parent = parent;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = mat;
    }

    static void CreatePost(Transform parent, Material mat, Vector3 pos, float size, float height)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Post";
        post.transform.parent = parent;
        post.transform.position = pos;
        post.transform.localScale = new Vector3(size, height / 2, size);
        post.GetComponent<Renderer>().material = mat;
    }

    static void PolishUI()
    {
        // Make top panel nicer
        GameObject topPanel = GameObject.Find("TopPanel");
        if (topPanel != null)
        {
            Image img = topPanel.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.1f, 0.25f, 0.1f, 0.85f);

            RectTransform rt = topPanel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 55);
        }

        // Make button panel nicer
        GameObject btnPanel = GameObject.Find("ButtonPanel");
        if (btnPanel != null)
        {
            Image img = btnPanel.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.1f, 0.25f, 0.1f, 0.85f);

            RectTransform rt = btnPanel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 55);
        }

        // Add outline to text elements
        string[] textNames = { "ScoreText", "RankText", "HarvestText", "InfoText" };
        foreach (string name in textNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null && obj.GetComponent<Outline>() == null)
            {
                Outline outline = obj.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1, -1);
            }
        }

        // Style buttons with rounded feel
        foreach (var btn in Object.FindObjectsOfType<Button>())
        {
            Outline outline = btn.GetComponent<Outline>();
            if (outline == null)
            {
                outline = btn.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.5f);
                outline.effectDistance = new Vector2(2, -2);
            }
        }
    }

    static void AddProgressBars()
    {
        foreach (var plot in Object.FindObjectsOfType<PlotManager>())
        {
            if (plot.GetComponentInChildren<Canvas>() != null) continue;

            GameObject canvasObj = new GameObject("ProgressCanvas");
            canvasObj.transform.SetParent(plot.transform);
            canvasObj.transform.localPosition = new Vector3(0, 3f, 0);
            canvasObj.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(200, 30);

            canvasObj.AddComponent<BillboardCanvas>();

            // Background bar
            GameObject bg = new GameObject("BarBG");
            bg.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Fill bar
            GameObject fill = new GameObject("BarFill");
            fill.transform.SetParent(canvasObj.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.8f, 0.2f);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            fillRt.pivot = new Vector2(0, 0.5f);

            canvasObj.SetActive(false);
        }
    }

    //[MenuItem("Farm/Add Harvest Effects")]
    static void AddHarvestEffects()
    {
        Material particleMat = null;
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
            particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null)
            particleShader = Shader.Find("Default-Particle");

        if (particleShader != null)
        {
            particleMat = new Material(particleShader);
            particleMat.color = new Color(1f, 0.85f, 0.2f);
            string matPath = "Assets/Materials/HarvestParticle.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing == null)
                AssetDatabase.CreateAsset(particleMat, matPath);
            else
                particleMat = existing;
        }

        foreach (var plot in Object.FindObjectsOfType<PlotManager>())
        {
            if (plot.GetComponentInChildren<ParticleSystem>() != null) continue;

            GameObject fx = new GameObject("HarvestFX");
            fx.transform.SetParent(plot.transform);
            fx.transform.localPosition = new Vector3(0, 2f, 0);

            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.8f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.startColor = new Color(1f, 0.85f, 0.2f);
            main.maxParticles = 20;
            main.loop = false;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 15)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            if (particleMat != null)
                fx.GetComponent<ParticleSystemRenderer>().material = particleMat;
        }

        EditorUtility.DisplayDialog("Done", "Harvest effects added!", "OK");
    }
}
