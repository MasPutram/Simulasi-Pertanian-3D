using UnityEngine;
using UnityEngine.UI;

// Bar hitung mundur di tengah atas: kuning + matahari saat siang, biru tua + bulan saat malam
public class DayNightBarUI : MonoBehaviour
{
    const float Width = 380f, Height = 28f;

    static readonly Color frame = new Color(0.16f, 0.1f, 0.05f);
    static readonly Color trough = new Color(0.1f, 0.1f, 0.14f, 0.9f);
    static readonly Color dayFill = new Color(0.95f, 0.8f, 0.3f);
    static readonly Color nightFill = new Color(0.17f, 0.25f, 0.6f);

    GameObject root, sunIcon, moonIcon;
    RectTransform fill;
    Image fillImg;
    Text label;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindObjectOfType<DayNightBarUI>() == null)
            new GameObject("DayNightBarUI").AddComponent<DayNightBarUI>();
    }

    void Start()
    {
        GameObject canvasObj = new GameObject("DayNightCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        root = SeedSelectionUI.MakeRect(canvasObj.transform, "DayNightBar", Vector2.zero, new Vector2(Width + 6, Height + 6));
        RectTransform rrt = root.GetComponent<RectTransform>();
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.anchoredPosition = new Vector2(0, -18);
        root.AddComponent<Image>().color = frame;
        root.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.4f);

        GameObject bg = SeedSelectionUI.MakeRect(root.transform, "Trough", Vector2.zero, new Vector2(Width, Height));
        bg.AddComponent<Image>().color = trough;

        GameObject f = SeedSelectionUI.MakeRect(bg.transform, "Fill", Vector2.zero, Vector2.zero);
        fill = f.GetComponent<RectTransform>();
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.pivot = new Vector2(0, 0.5f);
        fill.offsetMin = Vector2.zero; fill.offsetMax = Vector2.zero;
        fillImg = f.AddComponent<Image>();

        // kilap tipis di atas bar
        GameObject shine = SeedSelectionUI.MakeRect(f.transform, "Shine", Vector2.zero, Vector2.zero);
        RectTransform srt = shine.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1); srt.pivot = new Vector2(0.5f, 1);
        srt.offsetMin = new Vector2(0, -6); srt.offsetMax = Vector2.zero;
        shine.AddComponent<Image>().color = new Color(1, 1, 1, 0.22f);

        label = SeedSelectionUI.MakeLabel(bg.transform, "", 18, FontStyle.Bold, Color.white);
        label.gameObject.AddComponent<Outline>().effectColor = Color.black;

        sunIcon = BuildSun(root.transform, new Vector2(Width * 0.5f + 10, -10));
        moonIcon = BuildMoon(root.transform, new Vector2(-Width * 0.5f - 12, -10));
        sunIcon.transform.localScale = Vector3.one * 0.8f;
        moonIcon.transform.localScale = Vector3.one * 0.9f;
    }

    void Update()
    {
        WeatherManager wm = WeatherManager.Instance;
        bool show = wm != null && MainMenu.gameStarted;
        if (root.activeSelf != show) root.SetActive(show);
        if (!show) return;

        bool night = wm.IsNight;
        float left = wm.TimeUntilSwitch;
        float frac = wm.SwitchTotal > 0 ? Mathf.Clamp01(left / wm.SwitchTotal) : 0f;

        fill.anchorMax = new Vector2(frac, 1f);
        fillImg.color = night ? nightFill : dayFill;
        sunIcon.SetActive(!night);
        moonIcon.SetActive(night);

        int secs = Mathf.CeilToInt(left);
        label.text = secs >= 60 ? (secs / 60) + "m " + (secs % 60) + "s" : secs + "s";
    }

    // ---------- ikon blocky ----------

    static GameObject Holder(Transform parent, string name, Vector2 pos)
    {
        GameObject h = SeedSelectionUI.MakeRect(parent, name, pos, new Vector2(60, 60));
        return h;
    }

    static void Px(Transform p, float x, float y, float w, float h, Color c, float rot = 0f)
    {
        GameObject g = SeedSelectionUI.MakeRect(p, "px", new Vector2(x, y), new Vector2(w, h));
        g.AddComponent<Image>().color = c;
        g.transform.localRotation = Quaternion.Euler(0, 0, rot);
    }

    static void Cloud(Transform p, float x, float y)
    {
        Color c = new Color(0.95f, 0.96f, 1f), shade = new Color(0.78f, 0.82f, 0.9f);
        Px(p, x, y - 3, 22, 8, shade);
        Px(p, x, y - 1, 20, 8, c);
        Px(p, x - 4, y + 5, 9, 8, c);
        Px(p, x + 4, y + 6, 10, 9, c);
    }

    static GameObject BuildSun(Transform parent, Vector2 pos)
    {
        GameObject h = Holder(parent, "Sun", pos);
        Transform t = h.transform;
        Color ray = new Color(1f, 0.82f, 0.15f), body = new Color(1f, 0.9f, 0.3f);
        Px(t, 0, 8, 6, 40, ray);
        Px(t, 0, 8, 40, 6, ray);
        Px(t, 0, 8, 6, 40, ray, 45);
        Px(t, 0, 8, 6, 40, ray, -45);
        Px(t, 0, 8, 26, 26, body);
        Px(t, 0, 8, 26, 26, body, 45);
        Cloud(t, 16, -10);
        return h;
    }

    static GameObject BuildMoon(Transform parent, Vector2 pos)
    {
        GameObject h = Holder(parent, "Moon", pos);
        Transform t = h.transform;
        Color moon = new Color(0.95f, 0.95f, 0.85f);
        // bulan sabit berbentuk "C" dari balok
        Color moonShade = new Color(0.8f, 0.8f, 0.72f);
        Px(t, -7, 8, 8, 22, moon);       // punggung
        Px(t, -1, 19, 10, 6, moon);      // lengkung atas
        Px(t, -1, -3, 10, 6, moon);      // lengkung bawah
        Px(t, 6, 23, 6, 5, moon);        // ujung atas
        Px(t, 6, -7, 6, 5, moon);        // ujung bawah
        Px(t, -9, 8, 4, 16, moonShade);  // bayangan punggung
        Cloud(t, 12, -12);
        return h;
    }
}
