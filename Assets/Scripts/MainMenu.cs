using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public static bool gameStarted = false;

    [Range(1, 5)] public int blurLevels = 4;

    [Header("Style (diisi lewat Farm > Stardew UI > 2. Style Main Menu)")]
    public Sprite panelSprite;
    public Sprite buttonSprite;
    public Sprite vignetteSprite;
    public Font font;

    static readonly Color Cream = new Color(1f, 0.96f, 0.85f);
    static readonly Color Brown = new Color(0.36f, 0.18f, 0.05f);
    static readonly Color GreenBtn = new Color(0.36f, 0.72f, 0.3f);
    static readonly Color WoodBtn = new Color(0.86f, 0.55f, 0.24f);
    static readonly Color RedBtn = new Color(0.78f, 0.33f, 0.24f);

    GameObject menuCanvas;
    RawImage blurImage;
    Image vignette;
    CanvasGroup menuGroup;
    RectTransform title, panel, playBtn;
    RenderTexture blurRT;
    bool liveBlur = true;
    bool leaving = false;

    void Start()
    {
        gameStarted = false;
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateMenu();
        FreezeGame();
        StartCoroutine(PopIn());
    }

    void LateUpdate()
    {
        if (liveBlur) RenderBlur();
    }

    void Update()
    {
        if (leaving) return;
        float t = Time.unscaledTime;
        if (title) title.anchoredPosition = new Vector2(0, Mathf.Sin(t * 1.6f) * 6f);
        if (playBtn && !playBtn.GetComponent<MenuButtonFx>().Hovered)
            playBtn.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.025f);
    }

    // Built-in pipeline: render kamera ke texture kecil, lalu downsample/upsample bilinear = blur
    void RenderBlur()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        int w = Mathf.Max(1, Screen.width / 4);
        int h = Mathf.Max(1, Screen.height / 4);

        if (blurRT == null || blurRT.width != w || blurRT.height != h)
        {
            if (blurRT != null) blurRT.Release();
            blurRT = new RenderTexture(w, h, 0);
            blurRT.filterMode = FilterMode.Bilinear;
            blurImage.texture = blurRT;
        }

        RenderTexture src = RenderTexture.GetTemporary(w, h, 24);
        src.filterMode = FilterMode.Bilinear;
        RenderTexture prevTarget = cam.targetTexture;
        cam.targetTexture = src;
        cam.Render();
        cam.targetTexture = prevTarget;

        RenderTexture[] levels = new RenderTexture[blurLevels];
        RenderTexture current = src;
        for (int i = 0; i < blurLevels; i++)
        {
            levels[i] = RenderTexture.GetTemporary(Mathf.Max(1, current.width / 2), Mathf.Max(1, current.height / 2), 0);
            levels[i].filterMode = FilterMode.Bilinear;
            Graphics.Blit(current, levels[i]);
            current = levels[i];
        }
        for (int i = blurLevels - 2; i >= 0; i--)
        {
            Graphics.Blit(current, levels[i]);
            current = levels[i];
        }
        Graphics.Blit(current, blurRT);

        foreach (var rt in levels) RenderTexture.ReleaseTemporary(rt);
        RenderTexture.ReleaseTemporary(src);
    }

    void FreezeGame() => SetGameplay(false);
    void UnfreezeGame() => SetGameplay(true);

    void SetGameplay(bool on)
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc != null) pc.enabled = on;
        var pi = FindObjectOfType<PlayerInteraction>();
        if (pi != null) pi.enabled = on;
    }

    void CreateMenu()
    {
        menuCanvas = new GameObject("MainMenuCanvas");
        Canvas canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = menuCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        menuCanvas.AddComponent<GraphicRaycaster>();

        GameObject blurObj = Child(menuCanvas.transform, "BlurBackground");
        blurImage = blurObj.AddComponent<RawImage>();
        Stretch(blurObj);

        GameObject vigObj = Child(menuCanvas.transform, "Vignette");
        vignette = vigObj.AddComponent<Image>();
        if (vignetteSprite != null) vignette.sprite = vignetteSprite;
        else vignette.color = new Color(0, 0, 0, 0.4f);
        vignette.raycastTarget = false;
        Stretch(vigObj);

        GameObject root = Child(menuCanvas.transform, "MenuRoot");
        menuGroup = root.AddComponent<CanvasGroup>();
        Stretch(root);

        // Judul
        GameObject titleHolder = Child(root.transform, "TitleHolder");
        Anchor(titleHolder, new Vector2(0.5f, 0.76f), new Vector2(900, 110));
        Text titleText = MakeText(titleHolder.transform, "Title", "Simulasi Pertanian", 66, Cream);
        title = titleText.rectTransform;
        Outline ol = titleText.gameObject.AddComponent<Outline>();
        ol.effectColor = Brown;
        ol.effectDistance = new Vector2(3, -3);
        Shadow sh = titleText.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.45f);
        sh.effectDistance = new Vector2(0, -7);

        // Panel kayu
        GameObject panelObj = Child(root.transform, "Panel");
        Anchor(panelObj, new Vector2(0.5f, 0.38f), new Vector2(380, 290));
        panel = panelObj.GetComponent<RectTransform>();
        Image pImg = panelObj.AddComponent<Image>();
        if (panelSprite != null)
        {
            pImg.sprite = panelSprite;
            pImg.type = Image.Type.Sliced;
            pImg.pixelsPerUnitMultiplier = 1.35f;
        }
        else pImg.color = new Color(0.98f, 0.9f, 0.7f);
        Shadow ps = panelObj.AddComponent<Shadow>();
        ps.effectColor = new Color(0, 0, 0, 0.4f);
        ps.effectDistance = new Vector2(0, -6);

        playBtn = MakeButton(panelObj.transform, "Btn_Play", "PLAY", new Vector2(0, 72), new Vector2(290, 74), GreenBtn, 32, OnPlayClicked);
        MakeButton(panelObj.transform, "Btn_Settings", "PENGATURAN", new Vector2(0, -16), new Vector2(290, 60), WoodBtn, 22, OnSettingsClicked);
        MakeButton(panelObj.transform, "Btn_Quit", "KELUAR", new Vector2(0, -90), new Vector2(290, 60), RedBtn, 22, OnQuitClicked);

        GameObject toastHolder = Child(root.transform, "ToastHolder");
        Anchor(toastHolder, new Vector2(0.5f, 0.1f), new Vector2(600, 40));
        toast = MakeText(toastHolder.transform, "Toast", "", 20, Cream);
        toast.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.6f);
    }

    Text toast;

    void ShowToast(string msg)
    {
        toast.text = msg;
        CancelInvoke(nameof(ClearToast));
        Invoke(nameof(ClearToast), 2f);
    }

    void ClearToast() => toast.text = "";

    RectTransform MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size,
        Color color, int fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = Child(parent, name);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        if (buttonSprite != null)
        {
            img.sprite = buttonSprite;
            img.type = Image.Type.Sliced;
        }

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.onClick.AddListener(() => AudioManager.Instance?.PlayClick());
        btn.onClick.AddListener(onClick);
        btnObj.AddComponent<MenuButtonFx>();

        Text text = MakeText(btnObj.transform, "Text", label, fontSize, Color.white);
        text.rectTransform.offsetMin = new Vector2(0, 4); // naik sedikit, di atas "bibir" tombol
        Shadow ts = text.gameObject.AddComponent<Shadow>();
        ts.effectColor = new Color(0, 0, 0, 0.35f);
        ts.effectDistance = new Vector2(0, -2);
        return rt;
    }

    IEnumerator PopIn()
    {
        menuGroup.alpha = 0f;
        float d = 0.45f, e = 0f;
        while (e < d)
        {
            e += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(e / d);
            menuGroup.alpha = t;
            panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, EaseOutBack(t));
            yield return null;
        }
        panel.localScale = Vector3.one;
        menuGroup.alpha = 1f;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
    }

    void OnPlayClicked()
    {
        if (leaving) return;
        leaving = true;
        menuGroup.interactable = false;
        StartCoroutine(FadeAndStart());
    }

    void OnSettingsClicked()
    {
        ShowToast("Pengaturan segera hadir!");
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator FadeAndStart()
    {
        float duration = 1.2f, elapsed = 0f;
        Color vigStart = vignette.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = t * t * (3f - 2f * t);

            menuGroup.alpha = 1f - Mathf.Clamp01(t * 2f);
            panel.localScale = Vector3.one * (1f + ease * 0.08f);
            vignette.color = new Color(vigStart.r, vigStart.g, vigStart.b, vigStart.a * (1f - ease));
            blurImage.color = new Color(1, 1, 1, 1f - ease);
            yield return null;
        }

        gameStarted = true;
        liveBlur = false;
        UnfreezeGame();
        Destroy(menuCanvas);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (blurRT != null) blurRT.Release();
    }

    // ---------- Helper ----------

    static GameObject Child(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Anchor(GameObject go, Vector2 anchor, Vector2 size)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    Text MakeText(Transform parent, string name, string content, int fontSize, Color color)
    {
        GameObject obj = Child(parent, name);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;
        Stretch(obj);
        return text;
    }
}

// Efek tombol: membesar saat hover, mengecil saat ditekan (halus, tidak terpengaruh timeScale)
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool Hovered { get; private set; }
    bool pressed;

    public void OnPointerEnter(PointerEventData e) => Hovered = true;
    public void OnPointerExit(PointerEventData e) { Hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData e) => pressed = true;
    public void OnPointerUp(PointerEventData e) => pressed = false;

    void Update()
    {
        if (!Hovered && !pressed && name == "Btn_Play") return; // pulse PLAY diatur MainMenu
        float target = pressed ? 0.94f : (Hovered ? 1.07f : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
    }
}
