using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

// UI farming-sim versi modern: bingkai kayu halus (rounded, gradasi) + isi krem, digambar lewat kode
public static class StardewUI
{
    public const string Dir = "Assets/UI/Stardew";
    public const string FramePath = Dir + "/frame_wood.png";
    public const string InsetPath = Dir + "/inset_cream.png";
    public const string ButtonPath = Dir + "/button_round.png";
    public const string VignettePath = Dir + "/vignette.png";
    public const string FontDir = "Assets/UI/Fonts";

    const int ButtonSize = 48, ButtonRadius = 14, ButtonBorder = 17;

    const int FrameSize = 64, FrameRadius = 18, FrameBorder = 24;
    const int InsetSize = 32, InsetRadius = 9, InsetBorder = 11;
    const float FrameScale = 1.35f; // pixelsPerUnitMultiplier: bingkai sedikit lebih ramping di canvas

    public static readonly Color Outline = Hex("3b1d0a");
    public static readonly Color WoodLight = Hex("e39a52");
    public static readonly Color WoodDark = Hex("a85a22");
    public static readonly Color InnerLine = Hex("5c2e0e");
    public static readonly Color CreamShadow = Hex("e8c283");
    public static readonly Color Cream = Hex("fbe6b4");
    public static readonly Color InsetFill = Hex("fff4da");
    public static readonly Color InsetLine = Hex("8a5426");
    public static readonly Color TextBrown = Hex("5c2e0e");
    public static readonly Color TextOrange = Hex("c0501c");

    [MenuItem("Farm/Stardew UI/1. Build HUD")]
    public static void BuildHUD()
    {
        if (BlockedInPlayMode()) return;
        EnsureTextures();
        Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
        Sprite inset = AssetDatabase.LoadAssetAtPath<Sprite>(InsetPath);
        Font font = LoadFont();

        GameObject canvasObj = GameObject.Find("UICanvas");
        if (canvasObj == null) { Debug.LogError("[StardewUI] UICanvas tidak ditemukan"); return; }

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Stardew HUD");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform old = canvasObj.transform.Find("HUD_Stardew");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        Transform topPanel = canvasObj.transform.Find("TopPanel");
        if (topPanel != null)
        {
            Undo.RecordObject(topPanel.gameObject, "Stardew HUD");
            topPanel.gameObject.SetActive(false);
        }

        GameObject hud = Rect(canvasObj.transform, "HUD_Stardew", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(1, 1), new Vector2(-16, -16), new Vector2(270, 150));
        Undo.RegisterCreatedObjectUndo(hud, "Stardew HUD");
        Sliced(hud, frame, FrameScale);
        hud.AddComponent<Shadow>().effectDistance = new Vector2(0, -4);
        hud.GetComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.35f);

        Text rank = Label(hud.transform, "RankText", "Petani Pemula", font, 22, TextBrown, TextAnchor.MiddleCenter);
        rank.fontStyle = FontStyle.Bold;
        Place(rank.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(-40, 30));

        GameObject divider = Rect(hud.transform, "Divider", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(-52, 2));
        divider.AddComponent<Image>().color = CreamShadow;

        Text harvest = Label(hud.transform, "HarvestText", "Panen: 0", font, 17, TextBrown, TextAnchor.MiddleLeft);
        Place(harvest.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -62), new Vector2(-52, 26));

        GameObject box = Rect(hud.transform, "ScoreBox", new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(-40, 40));
        Sliced(box, inset, 1f);

        Text scoreLabel = Label(box.transform, "ScoreLabel", "SKOR", font, 15, TextBrown, TextAnchor.MiddleLeft);
        scoreLabel.fontStyle = FontStyle.Bold;
        Place(scoreLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(6, 0), new Vector2(-28, 0));

        Text score = Label(box.transform, "ScoreText", "0", font, 24, TextOrange, TextAnchor.MiddleRight);
        score.fontStyle = FontStyle.Bold;
        Place(score.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-6, 0), new Vector2(-28, 0));

        UIManager ui = Object.FindObjectOfType<UIManager>();
        if (ui != null)
        {
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("scoreText").objectReferenceValue = score;
            so.FindProperty("rankText").objectReferenceValue = rank;
            so.FindProperty("harvestText").objectReferenceValue = harvest;
            so.FindProperty("scoreFormat").stringValue = "{0}";
            so.FindProperty("harvestFormat").stringValue = "Panen: {0}";
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(canvasObj.scene);
        Selection.activeGameObject = hud;
        Debug.Log("[StardewUI] HUD dibuat. Tekan Ctrl+S untuk menyimpan scene.");
    }

    // Perubahan scene saat Play mode hilang begitu Play dihentikan
    static bool BlockedInPlayMode()
    {
        if (!EditorApplication.isPlaying) return false;
        Debug.LogError("[StardewUI] Hentikan Play mode dulu, perubahan saat Play tidak tersimpan.");
        return true;
    }

    // ---------- Tekstur ----------

    public static void EnsureTextures()
    {
        if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
        WriteFrame(FramePath);
        WriteInset(InsetPath);
        WriteButton(ButtonPath);
        WriteVignette(VignettePath);
        AssetDatabase.Refresh();
        ConfigureSprite(FramePath, FrameBorder);
        ConfigureSprite(InsetPath, InsetBorder);
        ConfigureSprite(ButtonPath, ButtonBorder);
        ConfigureSprite(VignettePath, 0);
    }

    [MenuItem("Farm/Stardew UI/2. Style Main Menu")]
    public static void StyleMainMenu()
    {
        if (BlockedInPlayMode()) return;
        EnsureTextures();
        MainMenu menu = Object.FindObjectOfType<MainMenu>(true);
        if (menu == null) { Debug.LogError("[StardewUI] MainMenu tidak ada di scene (Farm/Add Main Menu dulu)"); return; }

        SerializedObject so = new SerializedObject(menu);
        so.FindProperty("panelSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
        so.FindProperty("buttonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
        so.FindProperty("vignetteSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
        so.FindProperty("font").objectReferenceValue = LoadFont();
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        Selection.activeGameObject = menu.gameObject;
        Debug.Log("[StardewUI] Main menu di-style. Tekan Play untuk melihat, Ctrl+S untuk menyimpan.");
    }

    // Tombol putih (di-tint lewat Image.color): tepi bawah lebih gelap = efek 3D
    static void WriteButton(string path)
    {
        int s = ButtonSize;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color edge = new Color(0.32f, 0.32f, 0.32f);
        Color lip = new Color(0.72f, 0.72f, 0.72f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float d = Inside(x, y, s, ButtonRadius);
            // permukaan atas digeser naik 5px -> sisanya jadi "bibir" bawah
            float top = Inside(x, y - 5, s, ButtonRadius) ;
            float t = (float)y / (s - 1);
            Color face = Color.Lerp(new Color(0.9f, 0.9f, 0.9f), Color.white, t);
            if (y > s - 9) face = Color.Lerp(face, Color.white, 0.5f);

            Color c = edge;
            c = Color.Lerp(c, lip, Step(d - 2f));
            c = Color.Lerp(c, face, Step(Mathf.Min(top, d) - 2f) * (y >= 5 ? 1f : 0f));
            c.a = Step(d);
            tex.SetPixel(x, y, c);
        }
        Save(tex, path);
    }

    static void WriteVignette(string path)
    {
        int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) / 0.7071f;
            float a = Mathf.Lerp(0.18f, 0.75f, Mathf.SmoothStep(0.25f, 1f, r));
            tex.SetPixel(x, y, new Color(0.08f, 0.05f, 0.02f, a));
        }
        Save(tex, path);
    }

    static void WriteFrame(string path)
    {
        int s = FrameSize;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float d = Inside(x, y, s, FrameRadius);
            float t = (float)y / (s - 1);

            Color wood = Color.Lerp(WoodDark, WoodLight, t);
            float grain = Mathf.Sin(x * 0.55f + Mathf.Sin(y * 0.35f) * 1.6f) * 0.035f;
            wood = Shade(wood, grain);
            if (d < 6f) wood = Shade(wood, 0.08f * (1f - (d - 3f) / 3f));   // bevel terang di luar
            if (d > 11f) wood = Shade(wood, -0.10f * ((d - 11f) / 3f));      // bevel gelap ke dalam

            Color cream = Color.Lerp(CreamShadow, Cream, Mathf.SmoothStep(0f, 1f, (d - 16f) / 6f));

            Color c = Outline;
            c = Color.Lerp(c, wood, Step(d - 3f));
            c = Color.Lerp(c, InnerLine, Step(d - 14f));
            c = Color.Lerp(c, cream, Step(d - 16f));
            c.a = Step(d);
            tex.SetPixel(x, y, c);
        }
        Save(tex, path);
    }

    static void WriteInset(string path)
    {
        int s = InsetSize;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float d = Inside(x, y, s, InsetRadius);
            float fromTop = (s - 1 - y);
            Color fill = Color.Lerp(CreamShadow, InsetFill, Mathf.SmoothStep(0f, 1f, (fromTop - 2f) / 6f));
            Color c = Color.Lerp(InsetLine, fill, Step(d - 2f));
            c.a = Step(d);
            tex.SetPixel(x, y, c);
        }
        Save(tex, path);
    }

    // Jarak (px) dari tepi rounded-rect ke dalam; negatif = di luar
    static float Inside(int x, int y, int size, float r)
    {
        float half = size * 0.5f;
        float px = Mathf.Abs(x + 0.5f - half) - (half - r);
        float py = Mathf.Abs(y + 0.5f - half) - (half - r);
        float outside = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - r;
        return -outside;
    }

    static float Step(float v) => Mathf.Clamp01(v + 0.5f);

    static Color Shade(Color c, float amount)
    {
        return amount >= 0 ? Color.Lerp(c, Color.white, amount) : Color.Lerp(c, Color.black, -amount);
    }

    static void Save(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void ConfigureSprite(string path, int border)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.spriteBorder = new Vector4(border, border, border, border);
        imp.SaveAndReimport();
    }

    // ---------- Helper UI ----------

    // Pakai font .ttf pertama di Assets/UI/Fonts yang bukan font pixel, kalau tidak ada pakai font bawaan
    public static Font LoadFont()
    {
        if (Directory.Exists(FontDir))
        {
            foreach (string f in Directory.GetFiles(FontDir, "*.ttf"))
            {
                if (Path.GetFileName(f).ToLower().Contains("pixel")) continue;
                Font font = AssetDatabase.LoadAssetAtPath<Font>(f.Replace('\\', '/'));
                if (font != null) return font;
            }
        }
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public static GameObject Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), aMin, aMax, pivot, pos, size);
        return go;
    }

    public static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static void Sliced(GameObject go, Sprite sprite, float ppuMultiplier)
    {
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = ppuMultiplier;
        img.raycastTarget = false;
    }

    public static Text Label(Transform parent, string name, string content, Font font, int size, Color color, TextAnchor align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.text = content;
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
