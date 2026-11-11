using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Legendary }

[CreateAssetMenu(fileName = "NewPlant", menuName = "Farm/Plant Data")]
public class PlantData : ScriptableObject
{
    public string plantName;
    public Rarity rarity = Rarity.Common;
    [Tooltip("Harga beli 1 benih (pakai skor)")]
    public int seedPrice = 5;
    [Tooltip("Harga jual 1 buah hasil panen")]
    public int scoreValue;
    public float growthTime;
    [TextArea] public string description;
    public Sprite icon;

    public static Color RarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.Uncommon: return new Color(0.3f, 0.75f, 0.3f);
            case Rarity.Rare: return new Color(0.25f, 0.55f, 0.95f);
            case Rarity.Legendary: return new Color(1f, 0.7f, 0.1f);
            default: return new Color(0.62f, 0.62f, 0.62f);
        }
    }
}
