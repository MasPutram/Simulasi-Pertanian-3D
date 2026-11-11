using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerInteraction : MonoBehaviour
{
    public Camera mainCamera;
    public float interactionRange = 3.5f;

    PlotManager nearestPlot = null;
    InteractionPrompt lastPrompt = null;

    Transform seedArea, sellArea;
    bool inSeedArea, inSellArea;

    void Start()
    {
        seedArea = FindSceneObject("area_seed")?.transform;
        sellArea = FindSceneObject("area_sell")?.transform;
        if (seedArea == null) Debug.LogWarning("[PlayerInteraction] area_seed tidak ditemukan di scene");
        if (sellArea == null) Debug.LogWarning("[PlayerInteraction] area_sell tidak ditemukan di scene");
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (GameManager.Instance == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        Vector3 playerPos = player.transform.position;

        CheckAreas(playerPos);
        FindNearestPlot(playerPos);

        if (kb.eKey.wasPressedThisFrame)
        {
            if (nearestPlot != null)
                InteractWithPlot(nearestPlot);
            else if (!inSeedArea && !inSellArea)
                UIManager.Instance?.ShowInfo("Tidak ada yang bisa diinteraksi!");
        }

        Mouse mouse = Mouse.current;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && mainCamera != null && !overUI)
        {
            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                PlotManager plot = hit.collider.GetComponent<PlotManager>();
                if (plot != null)
                    InteractWithPlot(plot);
            }
        }
    }

    void CheckAreas(Vector3 playerPos)
    {
        bool nowSeed = IsInsideArea(seedArea, playerPos);
        if (nowSeed && !inSeedArea) OnEnterSeedArea();
        else if (!nowSeed && inSeedArea) GetSeedUI()?.Close();
        inSeedArea = nowSeed;

        bool nowSell = IsInsideArea(sellArea, playerPos);
        if (nowSell && !inSellArea) InventoryUI.Instance?.OpenSell();
        else if (!nowSell && inSellArea) InventoryUI.Instance?.CloseSell();
        inSellArea = nowSell;
    }

    // Cek posisi player (sumbu X/Z) di dalam bounds model area
    static bool IsInsideArea(Transform area, Vector3 pos)
    {
        if (area == null) return false;
        var renderers = area.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(area.position.x, area.position.z)) < 1.5f;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return pos.x >= b.min.x && pos.x <= b.max.x && pos.z >= b.min.z && pos.z <= b.max.z;
    }

    void OnEnterSeedArea()
    {
        GetSeedUI()?.Open();
    }

    SeedSelectionUI GetSeedUI()
    {
        SeedSelectionUI seedUI = Object.FindObjectOfType<SeedSelectionUI>();
        if (seedUI == null)
        {
            GameObject canvas = GameObject.Find("UICanvas");
            if (canvas != null)
                seedUI = canvas.AddComponent<SeedSelectionUI>();
        }
        return seedUI;
    }

    void FindNearestPlot(Vector3 playerPos)
    {
        nearestPlot = null;
        float closestDist = interactionRange;
        foreach (var plot in Object.FindObjectsOfType<PlotManager>())
        {
            float dist = Vector3.Distance(playerPos, plot.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearestPlot = plot;
            }
        }
        UpdatePromptUI();
    }

    void UpdatePromptUI()
    {
        if (lastPrompt != null)
        {
            lastPrompt.Hide();
            lastPrompt = null;
        }

        if (nearestPlot == null) return;
        InteractionPrompt p = nearestPlot.GetComponent<InteractionPrompt>();
        if (p == null) return;

        string text = "";
        switch (nearestPlot.state)
        {
            case PlotManager.PlotState.Empty:
                text = GameManager.Instance.selectedPlant != null
                    ? "[E] Tanam " + GameManager.Instance.selectedPlant.plantName
                    : "Pegang benih dulu (1-9)";
                break;
            case PlotManager.PlotState.Growing:
                text = "[E] Siram";
                break;
            case PlotManager.PlotState.Ready:
                text = "[E] Panen!";
                break;
        }
        p.Show(text);
        lastPrompt = p;
    }

    void InteractWithPlot(PlotManager plot)
    {
        switch (plot.state)
        {
            case PlotManager.PlotState.Empty:
                TryPlant(plot);
                break;
            case PlotManager.PlotState.Growing:
                TryWater(plot);
                break;
            case PlotManager.PlotState.Ready:
                TryHarvest(plot);
                break;
        }
    }

    void TryPlant(PlotManager plot)
    {
        GameManager gm = GameManager.Instance;
        PlantData selected = gm.selectedPlant;
        if (selected == null)
        {
            UIManager.Instance?.ShowInfo("Pegang benih dulu! Tekan angka slot benih, atau beli di rak benih");
            return;
        }
        if (plot.Plant(selected))
        {
            AudioManager.Instance?.PlayPlant();
            gm.ConsumeHeldSeed();
            var left = gm.GetSlot(selected, true);
            UIManager.Instance?.ShowInfo(selected.plantName + " ditanam!" + (left != null ? " (sisa benih " + left.count + ")" : ""));
        }
    }

    void TryWater(PlotManager plot)
    {
        if (plot.Water())
        {
            WateringFx.Play(plot.transform.position);
            UIManager.Instance?.ShowInfo("Tanaman disiram! (+5 detik)");
        }
        else
            UIManager.Instance?.ShowInfo("Sudah disiram!");
    }

    void TryHarvest(PlotManager plot)
    {
        if (plot.currentPlant != null && !GameManager.Instance.CanAdd(plot.currentPlant, false))
        {
            UIManager.Instance?.ShowInfo("Tas penuh! Jual hasil panen dulu");
            return;
        }
        PlantData harvested = plot.Harvest();
        if (harvested != null)
        {
            AudioManager.Instance?.PlayHarvest();
            GameManager.Instance.AddToBag(harvested);
            UIManager.Instance?.ShowInfo(harvested.plantName + " masuk tas! Jual di rak SELL");
        }
    }

    static GameObject FindSceneObject(string name)
    {
        string lower = name.ToLower();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.hideFlags != HideFlags.None || !go.scene.isLoaded) continue;
            if (go.name.ToLower() == lower) return go;
        }
        return null;
    }
}
