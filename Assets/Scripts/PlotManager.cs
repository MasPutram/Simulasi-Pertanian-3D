using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlotManager : MonoBehaviour
{
    public enum PlotState { Empty, Growing, Ready }

    public PlotState state = PlotState.Empty;
    public PlantData currentPlant;

    List<GameObject> phaseModels = new List<GameObject>();
    int currentPhase = -1;
    float growthTimer;
    float growthDuration;
    bool isWatered;

    public float Progress => growthDuration > 0f ? Mathf.Clamp01(growthTimer / growthDuration) : 0f;
    public float TimeLeft => Mathf.Max(0f, growthDuration - growthTimer);

    // Admin: langsung matang (state berubah jadi Ready di Update berikutnya)
    public void GrowInstantly()
    {
        if (state == PlotState.Growing) growthTimer = growthDuration;
    }

    Canvas progressCanvas;
    RectTransform fillBar;
    ParticleSystem harvestParticle;

    void Start()
    {
        progressCanvas = GetComponentInChildren<Canvas>(true);
        if (progressCanvas != null)
        {
            Transform fill = progressCanvas.transform.Find("BarFill");
            if (fill != null) fillBar = fill.GetComponent<RectTransform>();
        }
        harvestParticle = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        if (state == PlotState.Empty || state == PlotState.Ready) return;
        if (phaseModels.Count == 0) return;

        growthTimer += Time.deltaTime;
        float pct = Mathf.Clamp01(growthTimer / growthDuration);

        int totalPhases = phaseModels.Count;
        int newPhase;

        if (pct >= 1f)
        {
            newPhase = totalPhases - 1;
            state = PlotState.Ready;
        }
        else
        {
            newPhase = Mathf.FloorToInt(pct * totalPhases);
            newPhase = Mathf.Clamp(newPhase, 0, totalPhases - 2);
            state = PlotState.Growing;
        }

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            UpdateVisual();
        }

        UpdateProgressBar();
    }

    void UpdateProgressBar()
    {
        if (fillBar == null || progressCanvas == null) return;

        float progress = Mathf.Clamp01(growthTimer / growthDuration);
        fillBar.anchorMax = new Vector2(progress, 1);
        fillBar.offsetMax = new Vector2(-2, -2);

        Image fillImg = fillBar.GetComponent<Image>();
        if (fillImg != null)
        {
            if (progress < 0.5f)
                fillImg.color = new Color(0.9f, 0.7f, 0.2f);
            else if (progress < 1f)
                fillImg.color = new Color(0.3f, 0.8f, 0.2f);
            else
                fillImg.color = new Color(0.2f, 1f, 0.3f);
        }
    }

    public bool Plant(PlantData plant)
    {
        if (!Application.isPlaying) return false; // jangan sampai tanaman tersimpan ke scene saat di Editor
        if (state != PlotState.Empty) return false;

        currentPlant = plant;
        growthDuration = plant.growthTime;
        growthTimer = 0f;
        isWatered = false;

        BuildPhasesFromDesign(plant.plantName);

        if (phaseModels.Count == 0)
        {
            Debug.LogWarning("Design_" + plant.plantName + " tidak ditemukan di scene! Pastikan ada GameObject bernama Design_" + plant.plantName);
            state = PlotState.Empty;
            return false;
        }

        currentPhase = 0;
        state = PlotState.Growing;
        UpdateVisual();
        return true;
    }

    void BuildPhasesFromDesign(string plantName)
    {
        ClearPhases();

        GameObject design = FindDesign(plantName);
        if (design == null)
        {
            Debug.LogError("[PlotManager] Design_" + plantName + " TIDAK DITEMUKAN di scene!");
            return;
        }

        Debug.Log("[PlotManager] Found: " + design.name + " (active=" + design.activeSelf + ", children=" + design.transform.childCount + ")");

        // Semua child = fase tumbuh (seed terpisah)
        foreach (Transform child in design.transform)
        {
            GameObject copy = Instantiate(child.gameObject);
            copy.name = "Phase_" + phaseModels.Count + "_" + child.name;

            // Taruh di world space dulu, tanpa parent
            copy.transform.SetParent(null);
            copy.transform.rotation = Quaternion.identity;
            Vector3 ls = child.lossyScale;
            copy.transform.localScale = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
            copy.SetActive(true);

            // Hitung bounds untuk centering
            Bounds bounds = GetCombinedBounds(copy);
            // Posisikan: center X/Z di spot, bagian bawah model di permukaan spot
            Vector3 spotPos = transform.position;
            copy.transform.position += new Vector3(
                spotPos.x - bounds.center.x,
                spotPos.y - bounds.min.y,
                spotPos.z - bounds.center.z
            );

            // Baru parent ke spot (pertahankan world position)
            copy.transform.SetParent(transform, true);
            copy.SetActive(false);

            phaseModels.Add(copy);
        }

        Debug.Log("[PlotManager] " + phaseModels.Count + " fase dibuat untuk " + plantName);
    }

    static GameObject FindDesign(string plantName)
    {
        string target = "Design_" + plantName;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.hideFlags != HideFlags.None) continue;
            if (!go.scene.isLoaded) continue;
            if (go.name == target) return go;
        }
        // Case-insensitive fallback
        string targetLower = target.ToLower();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.hideFlags != HideFlags.None) continue;
            if (!go.scene.isLoaded) continue;
            if (go.name.ToLower() == targetLower) return go;
        }
        return null;
    }

    static Bounds GetCombinedBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one * 0.1f);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    void ClearPhases()
    {
        foreach (var go in phaseModels)
        {
            if (go != null) Destroy(go);
        }
        phaseModels.Clear();
        currentPhase = -1;
    }

    public bool Water()
    {
        if (state != PlotState.Growing) return false;
        if (isWatered) return false;

        isWatered = true;
        growthTimer += 5f;
        return true;
    }

    public PlantData Harvest()
    {
        if (state != PlotState.Ready) return null;

        PlantData harvested = currentPlant;
        currentPlant = null;
        state = PlotState.Empty;
        growthTimer = 0f;
        isWatered = false;

        ClearPhases();

        if (harvestParticle != null)
            harvestParticle.Play();

        return harvested;
    }

    void UpdateVisual()
    {
        // Tiap fase ganti total — cuma 1 yang aktif
        for (int i = 0; i < phaseModels.Count; i++)
        {
            if (phaseModels[i] != null)
                phaseModels[i].SetActive(i == currentPhase);
        }

        if (progressCanvas != null)
            progressCanvas.gameObject.SetActive(state == PlotState.Growing);
    }
}
