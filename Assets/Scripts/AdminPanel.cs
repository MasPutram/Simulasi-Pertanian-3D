using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Panel admin untuk tes/demo: beri semua benih, tumbuhkan semua tanaman, tambah skor. [F1] atau tombol ADMIN.
public class AdminPanel : MonoBehaviour
{
    public int seedsPerType = 5;
    public int bonusScore = 1000;

    static readonly Color frameBrown = new Color(0.45f, 0.28f, 0.12f);
    static readonly Color darkBrown = new Color(0.22f, 0.13f, 0.06f);
    static readonly Color headerRed = new Color(0.7f, 0.2f, 0.18f);

    public static AdminPanel Instance { get; private set; }

    GameObject panel;

    void Awake() { Instance = this; }

    public void SetOpen(bool value)
    {
        if (panel == null) return;
        panel.SetActive(value);
        if (value) GrowingPanelUI.Instance?.SetOpen(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindObjectOfType<AdminPanel>() == null)
            new GameObject("AdminPanel").AddComponent<AdminPanel>();
    }

    void Start()
    {
        GameObject canvasObj = new GameObject("AdminCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        panel = SeedSelectionUI.MakeRect(canvasObj.transform, "AdminPanel", Vector2.zero, new Vector2(300, 352));
        // hanya dibuka lewat F1; posisi di tengah kiri (sama seperti panel Tanaman Tumbuh)
        Anchor(panel, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(84, 0));
        panel.AddComponent<Image>().color = frameBrown;

        GameObject inner = SeedSelectionUI.MakeRect(panel.transform, "Inner", Vector2.zero, new Vector2(288, 340));
        inner.AddComponent<Image>().color = darkBrown;

        GameObject header = SeedSelectionUI.MakeRect(inner.transform, "Header", new Vector2(0, 150), new Vector2(276, 34));
        header.AddComponent<Image>().color = headerRed;
        SeedSelectionUI.MakeLabel(header.transform, "PANEL ADMIN  [F1]", 17, FontStyle.Bold, Color.white);

        MakeButton(inner.transform, "Beli Semua Benih (x" + seedsPerType + ")", new Vector2(0, 104), new Vector2(260, 38), new Color(0.22f, 0.62f, 0.25f), GiveAllSeeds);
        MakeButton(inner.transform, "Tumbuhkan Semua Tanaman", new Vector2(0, 60), new Vector2(260, 38), new Color(0.2f, 0.5f, 0.85f), GrowAll);
        MakeButton(inner.transform, "+" + bonusScore + " Skor", new Vector2(0, 16), new Vector2(260, 38), new Color(0.85f, 0.6f, 0.15f), AddScore);

        // Waktu & cuaca (dulu panel di kanan layar)
        GameObject sep = SeedSelectionUI.MakeRect(inner.transform, "WeatherTitle", new Vector2(0, -18), new Vector2(260, 24));
        SeedSelectionUI.MakeLabel(sep.transform, "WAKTU & CUACA", 13, FontStyle.Bold, new Color(1f, 0.88f, 0.55f));
        string[] times = { "Pagi", "Siang", "Sore", "Malam" };
        Color[] colors = { new Color(0.9f, 0.7f, 0.3f), new Color(0.3f, 0.65f, 0.9f), new Color(0.9f, 0.5f, 0.2f), new Color(0.2f, 0.22f, 0.5f) };
        for (int i = 0; i < 4; i++)
        {
            var t = (WeatherManager.TimeOfDay)i;
            MakeButton(inner.transform, times[i], new Vector2(-97.5f + i * 65f, -50), new Vector2(60, 34), colors[i], () => SetTime(t));
        }
        MakeButton(inner.transform, "Hujan / Cerah", new Vector2(0, -90), new Vector2(260, 34), new Color(0.3f, 0.45f, 0.6f), ToggleRain);

        MakeButton(inner.transform, "Tutup", new Vector2(0, -136), new Vector2(260, 32), new Color(0.4f, 0.35f, 0.3f), Toggle);

        panel.SetActive(false);
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame) Toggle();
    }

    void Toggle()
    {
        if (panel != null) SetOpen(!panel.activeSelf);
    }

    void GiveAllSeeds()
    {
        GameManager gm = GameManager.Instance;
        int given = 0, skipped = 0;
        foreach (var plant in gm.availablePlants)
        {
            if (plant == null) continue;
            if (gm.GiveSeeds(plant, seedsPerType)) given++;
            else skipped++;
        }
        AudioManager.Instance?.PlayBuy();
        UIManager.Instance?.ShowInfo("Admin: " + given + " jenis benih x" + seedsPerType + " masuk tas" + (skipped > 0 ? " (" + skipped + " gagal, tas penuh)" : ""));
    }

    void GrowAll()
    {
        int n = 0;
        foreach (var plot in FindObjectsOfType<PlotManager>())
            if (plot.state == PlotManager.PlotState.Growing) { plot.GrowInstantly(); n++; }
        AudioManager.Instance?.PlayHarvest();
        UIManager.Instance?.ShowInfo(n > 0 ? "Admin: " + n + " tanaman langsung matang!" : "Admin: tidak ada tanaman yang sedang tumbuh");
    }

    void AddScore()
    {
        GameManager.Instance.AddScore(bonusScore);
        AudioManager.Instance?.PlayBuy();
        UIManager.Instance?.ShowInfo("Admin: +" + bonusScore + " skor");
    }

    void SetTime(WeatherManager.TimeOfDay t)
    {
        if (WeatherManager.Instance == null) return;
        WeatherManager.Instance.SetTime(t);
        UIManager.Instance?.ShowInfo("Admin: waktu diubah ke " + t);
    }

    void ToggleRain()
    {
        if (WeatherManager.Instance == null) return;
        WeatherManager.Instance.ToggleWeather();
        UIManager.Instance?.ShowInfo("Admin: cuaca " + WeatherManager.Instance.currentWeather);
    }

    static void MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject b = SeedSelectionUI.MakeRect(parent, label, pos, size);
        b.AddComponent<Image>().color = color;
        b.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(onClick);
        SeedSelectionUI.MakeLabel(b.transform, label, 15, FontStyle.Bold, Color.white);
    }

    static void Anchor(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
    }
}
