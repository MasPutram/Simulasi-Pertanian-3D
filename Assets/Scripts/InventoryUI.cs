using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Hotbar tas (bawah layar) + panel jual
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    const int SlotCount = GameManager.MaxSlots;
    const float SlotSize = 72f;
    const float SlotGap = 8f;

    static readonly Color frameBrown = new Color(0.45f, 0.28f, 0.12f);
    static readonly Color darkBrown = new Color(0.25f, 0.15f, 0.07f);
    static readonly Color headerGreen = new Color(0.18f, 0.55f, 0.25f);
    static readonly Color slotBg = new Color(0.12f, 0.08f, 0.04f, 0.85f);
    static readonly Color slotBorder = new Color(0.45f, 0.28f, 0.12f, 0.95f);
    static readonly Color slotSelected = new Color(1f, 0.85f, 0.2f);
    static readonly Color leafGreen = new Color(0.25f, 0.65f, 0.18f);
    static readonly Color disabledGrey = new Color(0.35f, 0.35f, 0.35f);

    class Slot
    {
        public Image border;
        public Transform iconRoot;
        public Text count;
        public Image inner;
        public Image rarityBar;
        public GameObject seedTag;
        public GameManager.BagSlot shown;
        public bool shownIsSeed;
    }

    Slot[] slots;
    GameObject sellPanel;
    Text sellListText;
    Button sellAllBtn, sellHeldBtn;
    Text sellAllLabel, sellHeldLabel;
    Image sellAllImg, sellHeldImg;

    // Tetap muncul walaupun UIManager tidak ada di scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindObjectOfType<InventoryUI>() == null)
            new GameObject("InventoryUI").AddComponent<InventoryUI>();
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance == null) { enabled = false; return; }
        Canvas canvas = CreateCanvas();
        CreateHotbar(canvas.transform);
        CreateSellPanel(canvas.transform);
        GameManager.Instance.OnBagChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnBagChanged -= Refresh;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || slots == null) return;
        for (int i = 0; i < SlotCount; i++)
            if (kb[Key.Digit1 + i].wasPressedThisFrame) OnSlotClicked(i);
    }

    Canvas CreateCanvas()
    {
        GameObject obj = new GameObject("InventoryCanvas");
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        obj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // ---------- Hotbar ----------

    void CreateHotbar(Transform canvas)
    {
        float width = SlotCount * SlotSize + (SlotCount - 1) * SlotGap + 16;
        GameObject bar = SeedSelectionUI.MakeRect(canvas, "Hotbar", Vector2.zero, new Vector2(width, SlotSize + 16));
        RectTransform brt = bar.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0, 12);
        bar.AddComponent<Image>().color = new Color(0, 0, 0, 0.35f);

        slots = new Slot[SlotCount];
        float startX = -(SlotCount - 1) * (SlotSize + SlotGap) / 2f;
        for (int i = 0; i < SlotCount; i++)
        {
            int idx = i;
            Slot s = new Slot();

            GameObject border = SeedSelectionUI.MakeRect(bar.transform, "Slot" + (i + 1),
                new Vector2(startX + i * (SlotSize + SlotGap), 0), new Vector2(SlotSize, SlotSize));
            s.border = border.AddComponent<Image>();
            s.border.color = slotBorder;
            border.AddComponent<Button>().onClick.AddListener(() => OnSlotClicked(idx));

            GameObject inner = SeedSelectionUI.MakeRect(border.transform, "Inner", Vector2.zero, new Vector2(SlotSize - 8, SlotSize - 8));
            s.inner = inner.AddComponent<Image>();
            s.inner.color = slotBg;

            GameObject bar2 = SeedSelectionUI.MakeRect(inner.transform, "Rarity", Vector2.zero, Vector2.zero);
            RectTransform rrt = bar2.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 0); rrt.anchorMax = new Vector2(1, 0);
            rrt.pivot = new Vector2(0.5f, 0); rrt.sizeDelta = new Vector2(0, 4); rrt.anchoredPosition = Vector2.zero;
            s.rarityBar = bar2.AddComponent<Image>();
            s.rarityBar.enabled = false;

            GameObject iconRoot = SeedSelectionUI.MakeRect(inner.transform, "Icon", Vector2.zero, new Vector2(75, 75));
            iconRoot.transform.localScale = Vector3.one * 0.75f;
            s.iconRoot = iconRoot.transform;

            Text num = SeedSelectionUI.MakeLabel(inner.transform, (i + 1).ToString(), 12, FontStyle.Bold, new Color(1, 1, 1, 0.6f));
            num.alignment = TextAnchor.UpperLeft;
            num.rectTransform.offsetMin = new Vector2(4, 0);
            num.rectTransform.offsetMax = new Vector2(0, -2);

            s.count = SeedSelectionUI.MakeLabel(inner.transform, "", 15, FontStyle.Bold, Color.white);
            s.count.alignment = TextAnchor.LowerRight;
            s.count.rectTransform.offsetMax = new Vector2(-4, 0);
            s.count.rectTransform.offsetMin = new Vector2(0, 2);
            s.count.gameObject.AddComponent<Outline>().effectColor = Color.black;

            Text tag = SeedSelectionUI.MakeLabel(inner.transform, "BENIH", 9, FontStyle.Bold, new Color(1f, 0.9f, 0.6f));
            tag.alignment = TextAnchor.LowerLeft;
            tag.rectTransform.offsetMin = new Vector2(4, 5);
            s.seedTag = tag.gameObject;
            s.seedTag.SetActive(false);

            slots[i] = s;
        }
    }

    void OnSlotClicked(int index)
    {
        GameManager gm = GameManager.Instance;
        if (index >= gm.bag.Count) return;
        GameManager.BagSlot slot = gm.bag[index];
        if (gm.heldSlot == slot)
        {
            gm.HoldSlot(null);
        }
        else
        {
            gm.HoldSlot(slot);
            UIManager.Instance?.ShowInfo("Memegang " + slot.Label);
        }
    }

    void Refresh()
    {
        GameManager gm = GameManager.Instance;
        for (int i = 0; i < SlotCount; i++)
        {
            Slot s = slots[i];
            GameManager.BagSlot data = i < gm.bag.Count ? gm.bag[i] : null;
            PlantData plant = data?.plant;
            bool isSeed = data != null && data.isSeed;

            if (s.shown != data || s.shownIsSeed != isSeed)
            {
                foreach (Transform c in s.iconRoot) Destroy(c.gameObject);
                if (plant != null)
                    SeedSelectionUI.BuildSeedIcon(s.iconRoot, plant.plantName, HandItem.PlantColor(plant.plantName), leafGreen);
                // benih: ikon lebih kecil di atas latar kantong
                s.iconRoot.localScale = Vector3.one * (isSeed ? 0.5f : 0.75f);
                s.iconRoot.GetComponent<RectTransform>().anchoredPosition = isSeed ? new Vector2(4, 4) : Vector2.zero;
                s.inner.color = isSeed ? new Color(0.42f, 0.3f, 0.16f, 0.95f) : slotBg;
                s.seedTag.SetActive(isSeed);
                s.rarityBar.enabled = plant != null;
                if (plant != null) s.rarityBar.color = PlantData.RarityColor(plant.rarity);
                s.shown = data;
                s.shownIsSeed = isSeed;
            }

            s.count.text = data != null ? "x" + data.count : "";
            bool held = data != null && gm.heldSlot == data;
            s.border.color = held ? slotSelected : slotBorder;
            s.border.transform.localScale = held ? Vector3.one * 1.08f : Vector3.one;
        }

        if (sellPanel != null && sellPanel.activeSelf) RefreshSellPanel();
    }

    // ---------- Panel Jual ----------

    void CreateSellPanel(Transform canvas)
    {
        sellPanel = SeedSelectionUI.MakeRect(canvas, "SellPanel", new Vector2(0, 30), new Vector2(420, 320));
        sellPanel.AddComponent<Image>().color = frameBrown;

        GameObject inner = SeedSelectionUI.MakeRect(sellPanel.transform, "Inner", Vector2.zero, new Vector2(400, 300));
        inner.AddComponent<Image>().color = darkBrown;

        GameObject header = SeedSelectionUI.MakeRect(inner.transform, "Header", new Vector2(0, 125), new Vector2(380, 42));
        header.AddComponent<Image>().color = headerGreen;
        SeedSelectionUI.MakeLabel(header.transform, "JUAL HASIL PANEN", 20, FontStyle.Bold, Color.white);

        GameObject close = SeedSelectionUI.MakeRect(header.transform, "Close", new Vector2(170, 0), new Vector2(32, 32));
        close.AddComponent<Image>().color = new Color(0.85f, 0.2f, 0.15f);
        close.AddComponent<Button>().onClick.AddListener(CloseSell);
        SeedSelectionUI.MakeLabel(close.transform, "X", 16, FontStyle.Bold, Color.white);

        GameObject list = SeedSelectionUI.MakeRect(inner.transform, "List", new Vector2(0, 40), new Vector2(360, 110));
        sellListText = SeedSelectionUI.MakeLabel(list.transform, "", 16, FontStyle.Normal, new Color(0.95f, 0.9f, 0.8f));

        CreateSellButton(inner.transform, "SellHeld", new Vector2(0, -50), new Color(0.9f, 0.55f, 0.15f),
            out sellHeldBtn, out sellHeldImg, out sellHeldLabel, OnSellHeld);
        CreateSellButton(inner.transform, "SellAll", new Vector2(0, -108), new Color(0.2f, 0.7f, 0.3f),
            out sellAllBtn, out sellAllImg, out sellAllLabel, OnSellAll);

        sellPanel.SetActive(false);
    }

    void CreateSellButton(Transform parent, string name, Vector2 pos, Color color,
        out Button btn, out Image img, out Text label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = SeedSelectionUI.MakeRect(parent, name, pos, new Vector2(340, 48));
        img = obj.AddComponent<Image>();
        img.color = color;
        btn = obj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        label = SeedSelectionUI.MakeLabel(obj.transform, "", 17, FontStyle.Bold, Color.white);
    }

    public void OpenSell()
    {
        if (sellPanel == null) return;
        sellPanel.SetActive(true);
        RefreshSellPanel();
    }

    public void CloseSell()
    {
        if (sellPanel == null) return;
        sellPanel.SetActive(false);
    }

    void RefreshSellPanel()
    {
        GameManager gm = GameManager.Instance;

        var sb = new System.Text.StringBuilder();
        foreach (var s in gm.bag)
            if (!s.isSeed)
                sb.AppendLine(s.plant.plantName + " x" + s.count + "   (" + (s.plant.scoreValue * s.count) + " poin)");
        sellListText.text = sb.Length > 0 ? sb.ToString().TrimEnd() : "Belum ada hasil panen.\nBenih tidak bisa dijual.";

        int allValue = gm.FruitValue();
        bool canAll = allValue > 0;
        sellAllBtn.interactable = canAll;
        sellAllImg.color = canAll ? new Color(0.2f, 0.7f, 0.3f) : disabledGrey;
        sellAllLabel.text = "JUAL SEMUA BUAH  (+" + allValue + ")";

        GameManager.BagSlot held = gm.heldFruit != null ? gm.heldSlot : null;
        sellHeldBtn.interactable = held != null;
        sellHeldImg.color = held != null ? new Color(0.9f, 0.55f, 0.15f) : disabledGrey;
        sellHeldLabel.text = held != null
            ? "JUAL YANG DIPEGANG: " + held.plant.plantName + " x" + held.count + "  (+" + (held.plant.scoreValue * held.count) + ")"
            : "PEGANG BUAH DULU (1-" + SlotCount + ")";
    }

    void OnSellAll()
    {
        int earned = GameManager.Instance.SellAll(out string summary);
        if (earned <= 0 && summary == "") return;
        AudioManager.Instance?.PlayBuy();
        UIManager.Instance?.ShowInfo("Terjual: " + summary + "  (+" + earned + " poin)");
    }

    void OnSellHeld()
    {
        int earned = GameManager.Instance.SellHeld(out string summary);
        if (summary == "") return;
        AudioManager.Instance?.PlayBuy();
        UIManager.Instance?.ShowInfo("Terjual: " + summary + "  (+" + earned + " poin)");
    }
}
