using UnityEngine;
using UnityEngine.UI;

public class InteractionPrompt : MonoBehaviour
{
    public string defaultText = "";
    Canvas canvas;
    Text label;
    Camera cam;

    void Start()
    {
        cam = Camera.main;
        CreatePrompt();
        Hide();
    }

    void CreatePrompt()
    {
        GameObject canvasObj = new GameObject("PromptCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0);
        canvasObj.transform.localScale = Vector3.one * 0.005f;

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform crt = canvasObj.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(250, 35);

        // Background
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.7f);
        RectTransform bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform, false);
        label = textObj.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 18;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.fontStyle = FontStyle.Bold;
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    void LateUpdate()
    {
        if (canvas == null || !canvas.gameObject.activeSelf) return;
        if (cam == null) cam = Camera.main;
        if (cam != null)
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cam.transform.position);
    }

    public void Show(string text)
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            if (label != null) label.text = text;
        }
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }
}
