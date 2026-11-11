using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Text scoreText;
    public Text rankText;
    public Text harvestText;
    public Text infoText;
    public string scoreFormat = "Skor: {0}";
    public string harvestFormat = "Panen: {0}";

    public Button[] plantButtons;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (!scoreText) scoreText = FindText("ScoreText");
        if (!rankText) rankText = FindText("RankText");
        if (!harvestText) harvestText = FindText("HarvestText");
        if (!infoText) infoText = FindText("InfoText");
        if (FindObjectOfType<InventoryUI>() == null)
            gameObject.AddComponent<InventoryUI>();

        // Hide bottom plant buttons (seed selection is now via shelf + E key)
        GameObject btnPanel = GameObject.Find("ButtonPanel");
        if (btnPanel != null)
            btnPanel.SetActive(false);

        UpdateUI();
    }

    static Text FindText(string objName)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (t.hideFlags != HideFlags.None || !t.gameObject.scene.isLoaded) continue;
            if (t.name == objName) return t;
        }
        Debug.LogWarning("[UIManager] Text '" + objName + "' tidak ditemukan di scene");
        return null;
    }

    public void UpdateUI()
    {
        GameManager gm = GameManager.Instance;
        if (scoreText) scoreText.text = string.Format(scoreFormat, gm.score);
        if (rankText) rankText.text = gm.CurrentRank;
        if (harvestText) harvestText.text = string.Format(harvestFormat, gm.totalHarvested);
    }

    public void ShowInfo(string message)
    {
        if (infoText)
        {
            infoText.text = message;
            CancelInvoke(nameof(ClearInfo));
            Invoke(nameof(ClearInfo), 3f);
        }
    }

    void ClearInfo()
    {
        if (infoText) infoText.text = "";
    }
}
