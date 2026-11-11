using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Tooltip("Skor = uang. Nilai awal saat game dimulai")]
    public int startingScore = 20;
    public int score;
    [Tooltip("Total skor yang pernah didapat dari menjual (untuk gelar)")]
    public int totalEarned;
    public int totalHarvested;
    public PlantData[] availablePlants;

    public const int MaxSlots = 9;

    public string CurrentRank
    {
        get
        {
            if (totalEarned >= 5000) return "Petani Legenda";
            if (totalEarned >= 1500) return "Petani Ahli";
            if (totalEarned >= 300) return "Petani Terampil";
            return "Petani Pemula";
        }
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        score = startingScore;
        if (availablePlants == null || availablePlants.Length == 0)
            availablePlants = Resources.LoadAll<PlantData>("");
    }

    void Start()
    {
        UIManager.Instance?.UpdateUI();
    }

    // ---------- Tas: benih & buah ----------

    public class BagSlot
    {
        public PlantData plant;
        public int count;
        public bool isSeed;
        public string Label => (isSeed ? "Benih " : "") + plant.plantName;
    }

    public readonly List<BagSlot> bag = new List<BagSlot>();
    public BagSlot heldSlot;
    public event System.Action OnBagChanged;

    // Benih yang sedang dipegang (null kalau tidak pegang benih)
    public PlantData selectedPlant => heldSlot != null && heldSlot.isSeed ? heldSlot.plant : null;
    public PlantData heldFruit => heldSlot != null && !heldSlot.isSeed ? heldSlot.plant : null;

    public BagSlot GetSlot(PlantData plant, bool isSeed)
    {
        if (plant == null) return null;
        foreach (var s in bag) if (s.plant == plant && s.isSeed == isSeed) return s;
        return null;
    }

    public bool CanAdd(PlantData plant, bool isSeed) => GetSlot(plant, isSeed) != null || bag.Count < MaxSlots;

    bool Add(PlantData plant, bool isSeed, int amount)
    {
        BagSlot slot = GetSlot(plant, isSeed);
        if (slot == null)
        {
            if (bag.Count >= MaxSlots) return false;
            slot = new BagSlot { plant = plant, isSeed = isSeed };
            bag.Add(slot);
        }
        slot.count += amount;
        NotifyBag();
        return true;
    }

    public bool AddToBag(PlantData plant)
    {
        if (!Add(plant, false, 1)) return false;
        totalHarvested++;
        UIManager.Instance?.UpdateUI();
        return true;
    }

    // Beli benih: kurangi skor, masukkan ke tas. Return false kalau skor kurang / tas penuh
    public bool BuySeeds(PlantData plant, int amount, out string reason)
    {
        int cost = plant.seedPrice * amount;
        reason = "";
        if (score < cost) { reason = "Skor tidak cukup! Butuh " + cost; return false; }
        if (!CanAdd(plant, true)) { reason = "Tas penuh!"; return false; }
        score -= cost;
        Add(plant, true, amount);
        UIManager.Instance?.UpdateUI();
        if (heldSlot == null) HoldSlot(GetSlot(plant, true));
        return true;
    }

    // Admin: beri benih gratis
    public bool GiveSeeds(PlantData plant, int amount)
    {
        bool ok = Add(plant, true, amount);
        if (ok && heldSlot == null) HoldSlot(GetSlot(plant, true));
        return ok;
    }

    public void AddScore(int amount)
    {
        score += amount;
        UIManager.Instance?.UpdateUI();
    }

    // Pakai 1 benih yang dipegang (setelah menanam)
    public void ConsumeHeldSeed()
    {
        if (heldSlot == null || !heldSlot.isSeed) return;
        heldSlot.count--;
        if (heldSlot.count <= 0)
        {
            bag.Remove(heldSlot);
            HoldSlot(null);
        }
        NotifyBag();
    }

    // null = tangan kosong
    public void HoldSlot(BagSlot slot)
    {
        if (slot != null && !bag.Contains(slot)) slot = null;
        heldSlot = slot;
        if (slot == null)
            HandItem.Clear();
        else
        {
            GameObject model = slot.isSeed
                ? HandItem.FindSceneObject("seed_" + slot.plant.plantName)
                : HandItem.FindFruitModel(slot.plant.plantName);
            HandItem.Hold(model, HandItem.PlantColor(slot.plant.plantName), overhead: !slot.isSeed);
        }
        OnBagChanged?.Invoke();
    }

    public int SellAll(out string summary)
    {
        int earned = 0;
        var parts = new List<string>();
        for (int i = bag.Count - 1; i >= 0; i--)
        {
            BagSlot s = bag[i];
            if (s.isSeed) continue;
            earned += s.plant.scoreValue * s.count;
            parts.Insert(0, s.count + " " + s.plant.plantName);
            bag.RemoveAt(i);
        }
        summary = string.Join(", ", parts);
        AddEarnings(earned);
        if (heldSlot != null && !bag.Contains(heldSlot)) HoldSlot(null);
        NotifyBag();
        return earned;
    }

    // Jual semua buah sejenis yang sedang dipegang
    public int SellHeld(out string summary)
    {
        summary = "";
        if (heldSlot == null || heldSlot.isSeed) return 0;
        BagSlot slot = heldSlot;
        int earned = slot.plant.scoreValue * slot.count;
        summary = slot.count + " " + slot.plant.plantName;
        bag.Remove(slot);
        AddEarnings(earned);
        HoldSlot(null);
        NotifyBag();
        return earned;
    }

    public int FruitValue()
    {
        int v = 0;
        foreach (var s in bag) if (!s.isSeed) v += s.plant.scoreValue * s.count;
        return v;
    }

    void AddEarnings(int amount)
    {
        score += amount;
        totalEarned += amount;
        UIManager.Instance?.UpdateUI();
    }

    void NotifyBag()
    {
        UIManager.Instance?.UpdateUI();
        OnBagChanged?.Invoke();
    }
}
