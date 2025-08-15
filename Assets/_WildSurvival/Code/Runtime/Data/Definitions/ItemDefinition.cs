using System;
using UnityEngine;


/// <summary>
/// Core item definition for Wild Survival inventory system
/// Part of the Ultimate Inventory Tool
/// </summary>
[Serializable]
[CreateAssetMenu(fileName = "NewItem", menuName = "Wild Survival/Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemID = "";
    public string displayName = "New Item";
    [TextArea(3, 5)]
    public string description = "";
    public Sprite icon;
    public GameObject worldModel;

    [Header("Category")]
    public ItemCategory primaryCategory = ItemCategory.Misc;
    public ItemSubcategory subcategory = ItemSubcategory.None;
    public ItemTag[] tags = new ItemTag[0];

    [Header("Properties")]
    public float weight = 1f;
    public int maxStackSize = 1;
    public Vector2Int gridSize = Vector2Int.one;
    public bool[,] shapeGrid;

    [Header("Durability")]
    public bool hasDurability = false;
    public float maxDurability = 100f;

    [Header("Value")]
    public int baseValue = 10;
    public CurrencyType currencyType = CurrencyType.Coins;

    [Header("Requirements")]
    public int requiredLevel = 0;
    public SkillRequirement[] skillRequirements = new SkillRequirement[0];

    [Header("Effects")]
    public ItemEffect[] useEffects = new ItemEffect[0];
    public ItemEffect[] equipEffects = new ItemEffect[0];

    [Header("Crafting")]
    public bool isCraftable = true;
    public bool isDeconstructable = true;
    public ItemOutput[] deconstructOutputs = new ItemOutput[0];

    [Header("Fire & Fuel Properties")]
    [SerializeField] private ItemFuelProperties fuelProperties; // RENAMED to avoid conflict

    public ItemFuelProperties FuelProperties => fuelProperties;
    public bool IsFuel => fuelProperties != null && fuelProperties.isFuel;

    [System.Serializable]
    public class ItemFuelProperties // RENAMED CLASS
    {
        public bool isFuel = false;
        public FireInstance.FuelType fuelType = FireInstance.FuelType.Kindling;
        public float burnDuration = 10f; // minutes
        public float burnTemperature = 400f;
        public float heatOutput = 1f;
        public float ignitionTemperature = 200f;
        public bool spreadsFire = false;
        public float smokeAmount = 1f;
        public float fuelValue = 10f; // units per item

        public float GetEffectiveBurnDuration(bool isWet)
        {
            return isWet ? burnDuration * 0.3f : burnDuration;
        }
    }

    //[SerializeField] private FuelProperties fuelProperties = new FuelProperties();

    //public FuelProperties FuelProperties => fuelProperties;
    //public bool IsFuel => fuelProperties != null && fuelProperties.isFuel;

    //[System.Serializable]
    //public class FuelProperties
    //{
    //    public bool isFuel = false;
    //    public FireInstance.FuelType fuelType = FireInstance.FuelType.Kindling;
    //    public float burnDuration = 10f; // minutes
    //    public float burnTemperature = 400f;
    //    public float heatOutput = 1f;
    //    public float ignitionTemperature = 200f;
    //    public bool spreadsFire = false;
    //    public float smokeAmount = 1f;
    //    public float fuelValue = 10f; // units per item

    //    public float GetEffectiveBurnDuration(bool isWet)
    //    {
    //        return isWet ? burnDuration * 0.3f : burnDuration;
    //    }
    //}

    public ItemType itemType = ItemType.Misc;      // For type compatibility
    public bool stackable => maxStackSize > 1;      // Computed from maxStackSize
    public int gridWidth => gridSize.x;             // Computed from gridSize
    public int gridHeight => gridSize.y;            // Computed from gridSize

    public bool allowRotation = true;
    public bool useCustomShape = false;



    // Update OnValidate method:
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemID))
        {
            itemID = "item_" + name.Replace(" ", "_").ToLower();
        }

        // Auto-set rotation based on grid size
        allowRotation = (gridSize.x != gridSize.y);

        // Initialize shape grid if needed
        if (gridSize.x > 0 && gridSize.y > 0)
        {
            if (shapeGrid == null || shapeGrid.GetLength(0) != gridSize.x || shapeGrid.GetLength(1) != gridSize.y)
            {
                shapeGrid = new bool[gridSize.x, gridSize.y];
                for (int x = 0; x < gridSize.x; x++)
                {
                    for (int y = 0; y < gridSize.y; y++)
                    {
                        shapeGrid[x, y] = true;
                    }
                }
            }
        }
    }
}

// Supporting structures (no namespaces)
[Serializable]
public class SkillRequirement
{
    public string skillName;
    public int requiredLevel;
}

[Serializable]
public class ItemEffect
{
    public EffectType type;
    public float value;
    public float duration;
}

[Serializable]
public class ItemOutput
{
    public ItemDefinition item;
    public int quantity;
    [Range(0f, 1f)]
    public float chance = 1f;
}

// Enums
public enum ItemCategory
{
    Misc,
    Resource,
    Tool,
    Weapon,
    Food,
    Medicine,
    Clothing,
    Building,
    Fuel,
    Container
}

public enum ItemSubcategory
{
    None,
    Raw,
    Processed,
    Consumable,
    Equipment,
    Material
}

[Flags]
public enum ItemTag
{
    None = 0,
    Wood = 1 << 0,
    Stone = 1 << 1,
    Metal = 1 << 2,
    Organic = 1 << 3,
    Fuel = 1 << 4,
    Sharp = 1 << 5,
    Heavy = 1 << 6,
    Fragile = 1 << 7,
    Valuable = 1 << 8,
    QuestItem = 1 << 9,
    Stackable = 1 << 10
}

public enum CurrencyType
{
    Coins,
    Gems,
    Barter
}

public enum EffectType
{
    Heal,
    Damage,
    Buff,
    Debuff,
    StatusEffect
}

public enum ItemQuality
{
    Poor,
    Common,
    Good,
    Excellent,
    Masterwork,
    Legendary
}