using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Toko benih: beli benih pakai skor, benih masuk ke tas (hotbar)
public class SeedSelectionUI : MonoBehaviour
{
    GameObject panel;
    bool isOpen = false;
    Text balanceText;

    class Card
    {
        public PlantData plant;
        public Button buy1, buy5;
        public Image buy1Img, buy5Img;
        public Text owned;
    }
    readonly List<Card> cards = new List<Card>();

    static readonly Color frameBrown = new Color(0.45f, 0.28f, 0.12f);
    static readonly Color darkBrown = new Color(0.25f, 0.15f, 0.07f);
    static readonly Color headerGreen = new Color(0.18f, 0.55f, 0.25f);
    static readonly Color cardBrown = new Color(0.35f, 0.22f, 0.1f);
    static readonly Color buyGreen = new Color(0.22f, 0.62f, 0.25f);
    static readonly Color disabledGrey = new Color(0.32f, 0.3f, 0.28f);
    static readonly Color leafGreen = new Color(0.25f, 0.65f, 0.18f);

    void Start()
    {
        if (panel == null) CreateSeedPanel();
    }

    void Update()
    {
        if (isOpen) RefreshCards();
    }

    void CreateSeedPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || GameManager.Instance == null) return;

        // digeser ke atas supaya tidak menutupi hotbar
        panel = MakeRect(canvas.transform, "SeedPanel", new Vector2(0, 50), new Vector2(560, 520));
        panel.AddComponent<Image>().color = frameBrown;

        GameObject inner = MakeRect(panel.transform, "Inner", Vector2.zero, new Vector2(540, 500));
        inner.AddComponent<Image>().color = darkBrown;

        GameObject header = MakeRect(inner.transform, "Header", new Vector2(0, 222), new Vector2(520, 44));
        header.AddComponent<Image>().color = headerGreen;
        Text title = MakeLabel(header.transform, "TOKO BENIH", 22, FontStyle.Bold, Color.white);
        title.alignment = TextAnchor.MiddleLeft;
        title.rectTransform.offsetMin = new Vector2(16, 0);

        balanceText = MakeLabel(header.transform, "", 17, FontStyle.Bold, new Color(1f, 0.92f, 0.55f));
        balanceText.alignment = TextAnchor.MiddleRight;
        balanceText.rectTransform.offsetMax = new Vector2(-56, 0);

        GameObject closeBtn = MakeRect(header.transform, "Close", new Vector2(238, 0), new Vector2(34, 34));
        closeBtn.AddComponent<Image>().color = new Color(0.85f, 0.2f, 0.15f);
        closeBtn.AddComponent<Button>().onClick.AddListener(Close);
        MakeLabel(closeBtn.transform, "X", 18, FontStyle.Bold, Color.white);

        // Daftar kartu yang bisa di-scroll
        GameObject view = MakeRect(inner.transform, "Viewport", new Vector2(0, -16), new Vector2(520, 408));
        view.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        view.AddComponent<RectMask2D>();

        GameObject content = MakeRect(view.transform, "Content", Vector2.zero, new Vector2(520, 0));
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
        crt.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 8;
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = view.AddComponent<ScrollRect>();
        sr.content = crt;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        var plants = new List<PlantData>(GameManager.Instance.availablePlants);
        plants.RemoveAll(p => p == null);
        plants.Sort((a, b) => a.rarity != b.rarity ? a.rarity.CompareTo(b.rarity) : a.seedPrice.CompareTo(b.seedPrice));
        foreach (var p in plants) cards.Add(CreateCard(content.transform, p));

        GameObject hint = MakeRect(inner.transform, "Hint", new Vector2(0, -234), new Vector2(520, 24));
        MakeLabel(hint.transform, "Benih masuk ke tas. Tekan angka slot (1-9) untuk memegang, lalu [E] di plot", 12,
            FontStyle.Normal, new Color(0.75f, 0.68f, 0.55f));

        panel.SetActive(false);
    }

    Card CreateCard(Transform parent, PlantData plant)
    {
        Card c = new Card { plant = plant };
        Color rc = PlantData.RarityColor(plant.rarity);

        GameObject card = new GameObject("Card_" + plant.plantName, typeof(RectTransform));
        card.transform.SetParent(parent, false);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 92);
        card.AddComponent<Image>().color = cardBrown;

        // Garis warna rarity di kiri
        GameObject stripe = MakeRect(card.transform, "RarityStripe", Vector2.zero, Vector2.zero);
        RectTransform srt = stripe.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(0, 1); srt.pivot = new Vector2(0, 0.5f);
        srt.sizeDelta = new Vector2(6, 0); srt.anchoredPosition = Vector2.zero;
        stripe.AddComponent<Image>().color = rc;

        GameObject iconArea = MakeRect(card.transform, "IconArea", Vector2.zero, new Vector2(72, 72));
        Anchor(iconArea, new Vector2(0, 0.5f), new Vector2(50, 0));
        iconArea.AddComponent<Image>().color = new Color(0.18f, 0.1f, 0.05f);
        GameObject icon = MakeRect(iconArea.transform, "Icon", Vector2.zero, new Vector2(75, 75));
        icon.transform.localScale = Vector3.one * 0.85f;
        BuildSeedIcon(icon.transform, plant.plantName, HandItem.PlantColor(plant.plantName), leafGreen);

        Text name = TextAt(card.transform, plant.plantName, 19, FontStyle.Bold, Color.white, new Vector2(100, 20), new Vector2(190, 26));

        // Badge rarity
        GameObject badge = MakeRect(card.transform, "Rarity", Vector2.zero, new Vector2(92, 20));
        Anchor(badge, new Vector2(0, 0.5f), new Vector2(146, -4));
        badge.AddComponent<Image>().color = rc;
        MakeLabel(badge.transform, plant.rarity.ToString().ToUpper(), 11, FontStyle.Bold, Color.white);

        TextAt(card.transform, plant.growthTime + " dtk  |  Jual " + plant.scoreValue, 12, FontStyle.Normal,
            new Color(0.8f, 0.72f, 0.58f), new Vector2(100, -28), new Vector2(190, 20));

        c.owned = TextAt(card.transform, "", 11, FontStyle.Italic, new Color(0.65f, 0.85f, 0.6f),
            new Vector2(300, -28), new Vector2(85, 20));

        // Harga + tombol beli
        TextAt(card.transform, "Harga " + plant.seedPrice, 14, FontStyle.Bold, new Color(1f, 0.85f, 0.4f),
            new Vector2(300, 20), new Vector2(85, 22));

        c.buy1 = BuyButton(card.transform, "+1", new Vector2(-90, 0), plant, 1, out c.buy1Img);
        c.buy5 = BuyButton(card.transform, "+5", new Vector2(-34, 0), plant, 5, out c.buy5Img);
        return c;
    }

    Button BuyButton(Transform parent, string label, Vector2 pos, PlantData plant, int amount, out Image img)
    {
        GameObject b = MakeRect(parent, "Buy" + amount, Vector2.zero, new Vector2(50, 40));
        Anchor(b, new Vector2(1, 0.5f), pos);
        img = b.AddComponent<Image>();
        img.color = buyGreen;
        Button btn = b.AddComponent<Button>();
        btn.onClick.AddListener(() => Buy(plant, amount));
        MakeLabel(b.transform, label, 16, FontStyle.Bold, Color.white);
        return btn;
    }

    void Buy(PlantData plant, int amount)
    {
        if (GameManager.Instance.BuySeeds(plant, amount, out string reason))
        {
            AudioManager.Instance?.PlayBuy();
            UIManager.Instance?.ShowInfo("Beli " + amount + " benih " + plant.plantName + " (-" + plant.seedPrice * amount + ")");
        }
        else
        {
            UIManager.Instance?.ShowInfo(reason);
        }
    }

    void RefreshCards()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        balanceText.text = "Skor: " + gm.score;
        foreach (var c in cards)
        {
            bool full = !gm.CanAdd(c.plant, true);
            SetBuyState(c.buy1, c.buy1Img, !full && gm.score >= c.plant.seedPrice);
            SetBuyState(c.buy5, c.buy5Img, !full && gm.score >= c.plant.seedPrice * 5);
            var slot = gm.GetSlot(c.plant, true);
            c.owned.text = slot != null ? "Punya " + slot.count : "";
        }
    }

    static void SetBuyState(Button b, Image img, bool on)
    {
        b.interactable = on;
        img.color = on ? buyGreen : disabledGrey;
    }

    public bool IsOpen => isOpen;
    public void Toggle() => SetOpen(!isOpen);
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    void SetOpen(bool open)
    {
        if (panel == null) CreateSeedPanel();
        if (panel == null) return;
        isOpen = open;
        panel.SetActive(isOpen);
        if (open) RefreshCards();
    }

    // ---------- Helper UI (dipakai juga oleh InventoryUI) ----------

    static void Anchor(GameObject go, Vector2 anchor, Vector2 pos)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
    }

    static Text TextAt(Transform parent, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 sz)
    {
        GameObject holder = MakeRect(parent, "Text", Vector2.zero, sz);
        RectTransform rt = holder.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        Text t = MakeLabel(holder.transform, content, size, style, color);
        t.alignment = TextAnchor.MiddleLeft;
        return t;
    }

    public static void BuildSeedIcon(Transform parent, string plantName, Color mainColor, Color leafColor)
    {
        Color darkMain = mainColor * 0.75f; darkMain.a = 1f;
        Color darkLeaf = leafColor * 0.7f; darkLeaf.a = 1f;

        void Px(string n, float x, float y, float w, float h, Color col) =>
            MakeRect(parent, n, new Vector2(x, y), new Vector2(w, h)).AddComponent<Image>().color = col;

        switch (plantName)
        {
            case "Tomat":
                Px("BodyH", 0, -3, 40, 32, mainColor);
                Px("BodyV", 0, -3, 32, 40, mainColor);
                Px("ShadeL", -18, -3, 6, 28, darkMain);
                Px("ShadeR", 18, -3, 6, 28, darkMain);
                Px("LeafH", 0, 18, 28, 8, leafColor);
                Px("LeafV", 0, 22, 10, 14, leafColor);
                Px("LeafBL", -10, 14, 8, 8, darkLeaf);
                Px("LeafBR", 10, 14, 8, 8, darkLeaf);
                Px("Stem", 0, 30, 6, 8, darkLeaf);
                break;
            case "Wortel":
                Px("Top", 0, 2, 28, 16, mainColor);
                Px("Mid", 0, -14, 22, 14, mainColor);
                Px("Bot", 0, -26, 14, 12, mainColor);
                Px("Tip", 0, -34, 8, 8, darkMain);
                Px("Stripe1", 0, 4, 20, 3, darkMain);
                Px("Stripe2", 0, -10, 16, 3, darkMain);
                Px("LeafL", -8, 18, 8, 22, leafColor);
                Px("LeafR", 8, 18, 8, 22, leafColor);
                Px("LeafLi", -5, 20, 4, 18, darkLeaf);
                Px("LeafRi", 5, 20, 4, 18, darkLeaf);
                Px("LeafC", 0, 14, 6, 8, leafColor);
                break;
            case "Jagung":
                Px("Base", 0, -28, 22, 16, leafColor);
                Px("Bod1", 0, -16, 24, 12, mainColor);
                Px("Bod2", 0, -5, 28, 12, mainColor);
                Px("Bod3", 0, 7, 30, 12, mainColor);
                Px("Bod4", 0, 18, 26, 10, mainColor);
                Px("Bod5", 0, 27, 20, 8, mainColor);
                Px("K1", 0, -4, 22, 2, darkMain);
                Px("K2", 0, 8, 24, 2, darkMain);
                Px("K3", 0, 19, 20, 2, darkMain);
                Px("HuskL", -16, -10, 8, 28, darkLeaf);
                Px("HuskR", 16, -10, 8, 28, darkLeaf);
                break;
            case "Stroberi":
                Px("Top", 0, 4, 36, 18, mainColor);
                Px("Mid", 0, -10, 28, 14, mainColor);
                Px("Bot", 0, -22, 16, 12, mainColor);
                Px("ShadeR", 14, 0, 6, 16, darkMain);
                Px("Seed1", -8, 6, 3, 3, new Color(1f, 0.9f, 0.5f));
                Px("Seed2", 6, 2, 3, 3, new Color(1f, 0.9f, 0.5f));
                Px("Seed3", -2, -12, 3, 3, new Color(1f, 0.9f, 0.5f));
                Px("Seed4", 8, -14, 3, 3, new Color(1f, 0.9f, 0.5f));
                Px("Cap", 0, 16, 30, 6, leafColor);
                Px("CapL", -12, 12, 8, 6, darkLeaf);
                Px("CapR", 12, 12, 8, 6, darkLeaf);
                Px("Stem", 0, 23, 5, 9, darkLeaf);
                break;
            case "Labu":
                Px("Body", 0, -6, 48, 30, mainColor);
                Px("Top", 0, 10, 38, 6, mainColor);
                Px("RibL", -12, -6, 3, 28, darkMain);
                Px("RibR", 12, -6, 3, 28, darkMain);
                Px("ShadeB", 0, -20, 44, 4, darkMain);
                Px("Stem", 0, 18, 7, 12, new Color(0.45f, 0.35f, 0.15f));
                Px("Leaf", 10, 20, 12, 7, leafColor);
                break;
            case "Semangka":
                Px("Body", 0, -2, 50, 32, mainColor);
                Px("BodyTop", 0, 15, 40, 4, mainColor);
                Px("BodyBot", 0, -19, 40, 4, mainColor);
                Color stripe = new Color(0.45f, 0.78f, 0.35f);
                Px("S1", -14, -2, 5, 36, stripe);
                Px("S2", 0, -2, 5, 38, stripe);
                Px("S3", 14, -2, 5, 36, stripe);
                Px("Shine", -16, 10, 6, 4, new Color(0.7f, 0.95f, 0.6f));
                Px("Stem", 22, 16, 6, 6, darkLeaf);
                break;
            default:
                Px("Body", 0, 0, 40, 40, mainColor);
                break;
        }
    }

    public static GameObject MakeRect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return obj;
    }

    public static void FillParent(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static Text MakeLabel(Transform parent, string text, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        FillParent(obj);
        return t;
    }
}
