using System;
using UnityEngine;

/// <summary>
/// Complete ItemData class with all properties the inventory system needs
/// Bridge class until full migration to ItemDefinition
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Wild Survival/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemID;
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Properties")]
    public ItemType itemType = ItemType.Misc;
    public int maxStackSize = 1;
    public float weight = 1f;
    public int value = 10;
    public int sortOrder = 0;  // For inventory sorting

    [Header("Grid")]
    public Vector2Int gridSize = Vector2Int.one;

    [Header("World")]
    public GameObject worldPrefab; // For dropping items in world

    [Header("Durability")]
    public bool hasDurability = false;
    public float maxDurability = 100f;
    public float durabilityLossOnUse = 1f;

    [Header("Spoilage")]
    public bool canSpoil = false;
    public float spoilTime = 3600f; // In seconds

    [Header("Usage")]
    public bool isConsumable = false;
    public bool isEquippable = false;
    public bool isQuestItem = false;
    public bool canUse = false;
    public ItemEffect[] useEffects = new ItemEffect[0];

    [Header("Crafting")]
    public bool isCraftable = true;
    public bool hasCraftingTag = false;
    public string[] craftingTags = new string[0];

    // Change these from { get; } to { get; set; }
    public string ItemName { get; set; }
    public string Description { get; set; }
    public float Weight { get; set; }
    public Sprite Icon { get; set; }
    public ItemType Type { get; set; }

    // Properties for compatibility (capitalized versions)
    public string ItemID => itemID;
    //public string ItemName => itemName;
    //public string Description => description;
    //public Sprite Icon => icon;
    //public ItemType Type => itemType;
    public int MaxStackSize => maxStackSize;
    //public float Weight => weight;
    public int Value => value;
    public int SortOrder => sortOrder;  // Added SortOrder property

    // Additional property accessors
    public bool HasDurability => hasDurability;
    public float MaxDurability => maxDurability;
    public bool CanSpoil => canSpoil;
    public float SpoilTime => spoilTime;
    public bool IsConsumable => isConsumable;
    public bool IsEquippable => isEquippable;
    public bool CanUse => canUse;
    public ItemEffect[] UseEffects => useEffects;
    public bool IsCraftable => isCraftable;
    public bool HasCraftingTag => hasCraftingTag;
    public GameObject WorldPrefab => worldPrefab;

    // Method for checking if stackable
    public bool IsStackable()
    {
        return maxStackSize > 1;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemID))
        {
            itemID = name.ToLower().Replace(" ", "_");
        }

        // Auto-set stackable based on max stack size
        if (maxStackSize <= 1)
        {
            maxStackSize = 1;
        }

        // Validate durability
        if (hasDurability)
        {
            maxDurability = Mathf.Max(1f, maxDurability);
        }

        // Validate spoil time
        if (canSpoil)
        {
            spoilTime = Mathf.Max(1f, spoilTime);
        }
    }

    // Constructor
    public ItemData()
    {
        ItemName = "Unknown Item";
        Description = "";
        Weight = 1f;
        Type = ItemType.Misc;
    }

    // Add conversion from ItemDefinition
    public static implicit operator ItemData(ItemDefinition definition)
    {
        if (definition == null) return null;

        return new ItemData
        {
            ItemName = definition.displayName,
            Description = definition.description,
            Weight = definition.weight,
            Icon = definition.icon,
            Type = definition.itemType
        };
    }
}

// Note: ItemEffect class already exists elsewhere in the project
// If you need to see where it's defined, search for "class ItemEffect" in the project