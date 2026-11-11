using UnityEngine;
using UnityEditor;

public class TextureGenerator : EditorWindow
{
    //[MenuItem("Farm/Generate Textures")]
    static void GenerateAllTextures()
    {
        string texDir = "Assets/Textures";
        if (!AssetDatabase.IsValidFolder(texDir))
            AssetDatabase.CreateFolder("Assets", "Textures");

        GenerateGrassTexture(texDir);
        GenerateSoilTexture(texDir);
        GenerateWoodTexture(texDir);
        ApplyTextures();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Textures",
            "Texture berhasil dibuat dan diterapkan!\n" +
            "- Rumput untuk tanah\n" +
            "- Tanah untuk plot\n" +
            "- Kayu untuk pagar", "OK");
    }

    static void GenerateGrassTexture(string dir)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        Color baseColor = new Color(0.35f, 0.65f, 0.25f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise1 = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                float noise2 = Mathf.PerlinNoise(x * 0.15f + 100, y * 0.15f + 100);
                float detail = Mathf.PerlinNoise(x * 0.4f + 50, y * 0.4f + 50);

                float r = baseColor.r + (noise1 - 0.5f) * 0.1f + (detail - 0.5f) * 0.05f;
                float g = baseColor.g + (noise1 - 0.5f) * 0.15f + (noise2 - 0.5f) * 0.08f;
                float b = baseColor.b + (noise1 - 0.5f) * 0.05f;

                // Grass blade hints
                if (detail > 0.6f && Random.value > 0.7f)
                    g += 0.08f;

                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b)));
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Textures/Grass.png", png);
        Object.DestroyImmediate(tex);
    }

    static void GenerateSoilTexture(string dir)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        Color baseColor = new Color(0.45f, 0.3f, 0.15f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise1 = Mathf.PerlinNoise(x * 0.06f + 200, y * 0.06f + 200);
                float noise2 = Mathf.PerlinNoise(x * 0.2f + 300, y * 0.2f + 300);
                float detail = Mathf.PerlinNoise(x * 0.5f + 400, y * 0.5f + 400);

                float r = baseColor.r + (noise1 - 0.5f) * 0.12f + (detail - 0.5f) * 0.06f;
                float g = baseColor.g + (noise1 - 0.5f) * 0.08f;
                float b = baseColor.b + (noise1 - 0.5f) * 0.04f;

                // Small pebble spots
                if (noise2 > 0.65f && detail > 0.55f)
                {
                    r += 0.1f; g += 0.08f; b += 0.06f;
                }

                // Dark dirt grooves
                if (detail < 0.35f)
                {
                    r -= 0.05f; g -= 0.03f; b -= 0.02f;
                }

                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b)));
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Textures/Soil.png", png);
        Object.DestroyImmediate(tex);
    }

    static void GenerateWoodTexture(string dir)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        Color baseColor = new Color(0.5f, 0.32f, 0.14f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Wood grain runs horizontally
                float grain = Mathf.PerlinNoise(x * 0.02f, y * 0.15f + 500);
                float detail = Mathf.PerlinNoise(x * 0.08f + 600, y * 0.4f + 600);
                float knot = Mathf.PerlinNoise(x * 0.03f + 700, y * 0.03f + 700);

                float r = baseColor.r + (grain - 0.5f) * 0.15f + (detail - 0.5f) * 0.05f;
                float g = baseColor.g + (grain - 0.5f) * 0.1f;
                float b = baseColor.b + (grain - 0.5f) * 0.05f;

                // Wood grain lines
                float line = Mathf.Sin(y * 0.8f + grain * 5f) * 0.5f + 0.5f;
                r += (line - 0.5f) * 0.06f;
                g += (line - 0.5f) * 0.04f;

                // Knots
                if (knot > 0.7f)
                {
                    r -= 0.08f; g -= 0.05f; b -= 0.03f;
                }

                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b)));
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Textures/Wood.png", png);
        Object.DestroyImmediate(tex);
    }

    static void ApplyTextures()
    {
        AssetDatabase.Refresh();

        Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Grass.png");
        Texture2D soilTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Soil.png");
        Texture2D woodTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Wood.png");

        // Apply grass to ground
        ApplyTextureToMaterial("Assets/Materials/Ground.mat", grassTex, 3f);
        ApplyTextureToMaterial("Assets/Materials/OuterGround.mat", grassTex, 8f);

        // Apply soil to plots
        ApplyTextureToMaterial("Assets/Materials/Plot.mat", soilTex, 2f);

        // Apply wood to fence
        ApplyTextureToMaterial("Assets/Materials/Fence.mat", woodTex, 2f);
        ApplyTextureToMaterial("Assets/Materials/Trunk.mat", woodTex, 1f);
    }

    static void ApplyTextureToMaterial(string matPath, Texture2D tex, float tiling)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null || tex == null) return;

        mat.SetTexture("_BaseMap", tex);
        mat.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
        mat.mainTexture = tex;
        mat.mainTextureScale = new Vector2(tiling, tiling);
        EditorUtility.SetDirty(mat);
    }
}
