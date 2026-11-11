using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Ikon di tengah kiri layar -> panel daftar tanaman yang sedang tumbuh + sisa waktu panen (bisa di-scroll).
// [Tab] juga membuka/menutup panel.
public class GrowingPanelUI : MonoBehaviour
{
    public static GrowingPanelUI Instance { get; private set; }

    const float PanelWidth = 290f;
    const float HeaderHeight = 34f;
    const float RowHeight = 50f;
    const float RowSpacing = 5f;
    const float MaxListHeight = 330f; // lebih dari ini -> scroll

    static readonly Color frameBrown = new Color(0.45f, 0.28f, 0.12f, 0.95f);
    static readonly Color darkBrown = new Color(0.22f, 0.13f, 0.06f, 0.92f);
    static readonly Color headerGreen = new Color(0.18f, 0.55f, 0.25f);
    static readonly Color rowBrown = new Color(0.33f, 0.21f, 0.1f);
    static readonly Color barBg = new Color(0.12f, 0.08f, 0.04f);
    static readonly Color barGrow = new Color(0.95f, 0.72f, 0.2f);
    static readonly Color barReady = new Color(0.3f, 0.85f, 0.3f);
    static readonly Color leafGreen = new Color(0.25f, 0.65f, 0.18f);

    class Row
    {
        public PlotManager plot;
        public PlantData plant;
        public GameObject go;
        public RectTransform fill;
        public Image fillImg;
        public Text time;
    }

    GameObject panel, content, icon, badge, emptyLabel;
    RectTransform panelRt, viewportRt;
    Text countText, badgeText;
    readonly List<Row> rows = new List<Row>();
    bool open = false;

    public bool IsOpen => open;
    public void SetOpen(bool value)
    {
        open = value;
        if (open) AdminPanel.Instance?.SetOpen(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindObjectOfType<GrowingPanelUI>() == null)
            new GameObject("GrowingPanelUI").AddComponent<GrowingPanelUI>();
    }

    void Awake() { Instance = this; }

    void Start()
    {
        GameObject canvasObj = new GameObject("GrowingCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Ikon di tengah kiri
        icon = MakeIconButton(canvasObj.transform, "GrowIcon", new Vector2(0, 0.5f), new Vector2(16, 0), () => SetOpen(!open));
        DrawSproutIcon(icon.transform.Find("Inner"));
        badge = SeedSelectionUI.MakeRect(icon.transform, "Badge", Vector2.zero, new Vector2(24, 24));
        RectTransform brt = badge.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(1, 1);
        brt.anchoredPosition = new Vector2(-2, -2);
        badge.AddComponent<Image>().color = new Color(0.85f, 0.18f, 0.15f);
        badgeText = SeedSelectionUI.MakeLabel(badge.transform, "", 13, FontStyle.Bold, Color.white);

        // Panel di kanan ikon, tinggi menyesuaikan isi (maks MaxListHeight lalu scroll)
        panel = SeedSelectionUI.MakeRect(canvasObj.transform, "GrowingPanel", Vector2.zero, new Vector2(PanelWidth, 100));
        panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0, 0.5f);
        panelRt.pivot = new Vector2(0, 0.5f);
        panelRt.anchoredPosition = new Vector2(84, 0);
        panel.AddComponent<Image>().color = frameBrown;

        GameObject header = SeedSelectionUI.MakeRect(panel.transform, "Header", Vector2.zero, Vector2.zero);
        RectTransform hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1); hrt.pivot = new Vector2(0.5f, 1);
        hrt.offsetMin = new Vector2(6, -6 - HeaderHeight); hrt.offsetMax = new Vector2(-6, -6);
        header.AddComponent<Image>().color = headerGreen;
        Text title = SeedSelectionUI.MakeLabel(header.transform, "TANAMAN TUMBUH", 16, FontStyle.Bold, Color.white);
        title.alignment = TextAnchor.MiddleLeft;
        title.rectTransform.offsetMin = new Vector2(10, 0);
        countText = SeedSelectionUI.MakeLabel(header.transform, "", 12, FontStyle.Bold, new Color(1f, 0.92f, 0.6f));
        countText.alignment = TextAnchor.MiddleRight;
        countText.rectTransform.offsetMax = new Vector2(-10, 0);

        // Area scroll
        GameObject viewport = SeedSelectionUI.MakeRect(panel.transform, "Viewport", Vector2.zero, Vector2.zero);
        viewportRt = viewport.GetComponent<RectTransform>();
        viewportRt.anchorMin = new Vector2(0, 1); viewportRt.anchorMax = new Vector2(1, 1); viewportRt.pivot = new Vector2(0.5f, 1);
        viewportRt.offsetMin = new Vector2(6, 0); viewportRt.offsetMax = new Vector2(-6, -12 - HeaderHeight);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.12f);
        viewport.AddComponent<RectMask2D>();

        content = SeedSelectionUI.MakeRect(viewport.transform, "Content", Vector2.zero, Vector2.zero);
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
        crt.sizeDelta = Vector2.zero;
        VerticalLayoutGroup lv = content.AddComponent<VerticalLayoutGroup>();
        lv.spacing = RowSpacing;
        lv.childControlWidth = true; lv.childControlHeight = false;
        lv.childForceExpandWidth = true; lv.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = viewport.AddComponent<ScrollRect>();
        sr.content = crt;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 25f;

        emptyLabel = SeedSelectionUI.MakeRect(viewport.transform, "Empty", Vector2.zero, Vector2.zero);
        SeedSelectionUI.FillParent(emptyLabel);
        SeedSelectionUI.MakeLabel(emptyLabel.transform, "Belum ada tanaman.\nTanam benih di plot dulu!", 13, FontStyle.Italic, new Color(0.85f, 0.78f, 0.62f));

        Rebuild(new List<PlotManager>());
        panel.SetActive(false);
    }

    // Tombol ikon kotak kayu 58x58 (dipakai juga oleh AdminPanel)
    public static GameObject MakeIconButton(Transform parent, string name, Vector2 anchor, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject b = SeedSelectionUI.MakeRect(parent, name, Vector2.zero, new Vector2(58, 58));
        RectTransform rt = b.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0, anchor.y);
        rt.anchoredPosition = pos;
        b.AddComponent<Image>().color = new Color(0.45f, 0.28f, 0.12f);
        b.AddComponent<Button>().onClick.AddListener(onClick);
        b.AddComponent<Outline>().effectColor = new Color(0.2f, 0.11f, 0.04f);
        GameObject inner = SeedSelectionUI.MakeRect(b.transform, "Inner", Vector2.zero, new Vector2(48, 48));
        inner.AddComponent<Image>().color = new Color(0.25f, 0.15f, 0.07f);
        return b;
    }

    static void DrawSproutIcon(Transform p)
    {
        void Px(float x, float y, float w, float h, Color c) => SeedSelectionUI.MakeRect(p, "px", new Vector2(x, y), new Vector2(w, h)).AddComponent<Image>().color = c;
        Color pot = new Color(0.78f, 0.42f, 0.2f), potDark = new Color(0.6f, 0.3f, 0.14f);
        Color leaf = new Color(0.35f, 0.78f, 0.25f), leafDark = new Color(0.2f, 0.55f, 0.15f);
        Px(0, -12, 26, 14, pot);
        Px(0, -4, 30, 5, potDark);
        Px(0, 4, 4, 14, leafDark);
        Px(-7, 10, 12, 7, leaf);
        Px(7, 14, 12, 7, leaf);
        Px(-10, 12, 5, 4, leafDark);
        Px(10, 16, 5, 4, leafDark);
    }

    void Update()
    {
        if (panel == null) return;
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame) SetOpen(!open);
        icon.SetActive(MainMenu.gameStarted);

        var plots = new List<PlotManager>();
        foreach (var p in FindObjectsOfType<PlotManager>())
            if (p.state != PlotManager.PlotState.Empty && p.currentPlant != null) plots.Add(p);
        // yang paling cepat panen di atas (urutan stabil untuk yang sama)
        plots.Sort((a, b) =>
        {
            int c = a.TimeLeft.CompareTo(b.TimeLeft);
            if (c != 0) return c;
            c = a.transform.position.x.CompareTo(b.transform.position.x);
            return c != 0 ? c : a.transform.position.z.CompareTo(b.transform.position.z);
        });

        int readyCount = 0;
        foreach (var p in plots) if (p.state == PlotManager.PlotState.Ready) readyCount++;
        badge.SetActive(readyCount > 0);
        badgeText.text = readyCount.ToString();

        bool show = open && MainMenu.gameStarted;
        if (panel.activeSelf != show) panel.SetActive(show);
        if (!show) return;

        emptyLabel.SetActive(plots.Count == 0);
        if (!SameRows(plots)) Rebuild(plots);

        foreach (var r in rows)
        {
            bool isReady = r.plot.state == PlotManager.PlotState.Ready;
            r.fill.anchorMax = new Vector2(isReady ? 1f : r.plot.Progress, 1f);
            if (isReady)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 6f);
                r.fillImg.color = new Color(barReady.r * pulse, barReady.g * pulse, barReady.b * pulse, 1f);
                r.time.text = "SIAP PANEN!";
            }
            else
            {
                r.fillImg.color = barGrow;
                int secs = Mathf.CeilToInt(r.plot.TimeLeft);
                r.time.text = (secs / 60) + ":" + (secs % 60).ToString("00");
            }
        }
        countText.text = readyCount > 0 ? readyCount + " siap panen" : rows.Count + " tanaman";
    }

    bool SameRows(List<PlotManager> plots)
    {
        if (plots.Count != rows.Count) return false;
        for (int i = 0; i < plots.Count; i++)
            if (rows[i].plot != plots[i] || rows[i].plant != plots[i].currentPlant) return false;
        return true;
    }

    void Rebuild(List<PlotManager> plots)
    {
        foreach (var r in rows) Destroy(r.go);
        rows.Clear();
        foreach (var plot in plots) rows.Add(CreateRow(plot));

        // tinggi panel ikut isi, dibatasi MaxListHeight (sisanya di-scroll)
        float listH = plots.Count == 0 ? 50f : plots.Count * RowHeight + (plots.Count - 1) * RowSpacing;
        float viewH = Mathf.Min(listH, MaxListHeight);
        float top = 12f + HeaderHeight;
        viewportRt.offsetMin = new Vector2(6f, -top - viewH);
        viewportRt.offsetMax = new Vector2(-6f, -top);
        panelRt.sizeDelta = new Vector2(PanelWidth, viewH + HeaderHeight + 18f);
    }

    Row CreateRow(PlotManager plot)
    {
        PlantData plant = plot.currentPlant;
        Row r = new Row { plot = plot, plant = plant };

        r.go = new GameObject("Row_" + plant.plantName, typeof(RectTransform));
        r.go.transform.SetParent(content.transform, false);
        r.go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, RowHeight);
        r.go.AddComponent<Image>().color = rowBrown;

        GameObject stripe = SeedSelectionUI.MakeRect(r.go.transform, "Rarity", Vector2.zero, Vector2.zero);
        RectTransform srt = stripe.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(0, 1); srt.pivot = new Vector2(0, 0.5f);
        srt.sizeDelta = new Vector2(4, 0);
        stripe.AddComponent<Image>().color = PlantData.RarityColor(plant.rarity);

        GameObject iconBg = SeedSelectionUI.MakeRect(r.go.transform, "IconBg", Vector2.zero, new Vector2(42, 42));
        Place(iconBg, new Vector2(0, 0.5f), new Vector2(29, 0));
        iconBg.AddComponent<Image>().color = darkBrown;
        GameObject ic = SeedSelectionUI.MakeRect(iconBg.transform, "Icon", Vector2.zero, new Vector2(75, 75));
        ic.transform.localScale = Vector3.one * 0.5f;
        SeedSelectionUI.BuildSeedIcon(ic.transform, plant.plantName, HandItem.PlantColor(plant.plantName), leafGreen);

        GameObject nameObj = SeedSelectionUI.MakeRect(r.go.transform, "Name", Vector2.zero, new Vector2(200, 20));
        Place(nameObj, new Vector2(0, 0.5f), new Vector2(158, 12));
        Text name = SeedSelectionUI.MakeLabel(nameObj.transform, plant.plantName, 14, FontStyle.Bold, Color.white);
        name.alignment = TextAnchor.MiddleLeft;

        GameObject bar = SeedSelectionUI.MakeRect(r.go.transform, "Bar", Vector2.zero, new Vector2(200, 18));
        Place(bar, new Vector2(0, 0.5f), new Vector2(158, -10));
        bar.AddComponent<Image>().color = barBg;

        GameObject fill = SeedSelectionUI.MakeRect(bar.transform, "Fill", Vector2.zero, Vector2.zero);
        r.fill = fill.GetComponent<RectTransform>();
        r.fill.anchorMin = Vector2.zero; r.fill.anchorMax = new Vector2(0, 1);
        r.fill.pivot = new Vector2(0, 0.5f);
        r.fill.offsetMin = new Vector2(2, 2); r.fill.offsetMax = new Vector2(-2, -2);
        r.fillImg = fill.AddComponent<Image>();

        r.time = SeedSelectionUI.MakeLabel(bar.transform, "", 12, FontStyle.Bold, Color.white);
        r.time.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
        return r;
    }

    static void Place(GameObject go, Vector2 anchor, Vector2 pos)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
    }
}
