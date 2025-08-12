using System;

/// <summary>
/// Item type enumeration for categorizing items
/// Used throughout the inventory system
/// </summary>
[Flags]
public enum ItemType
{
    None = 0,

    // Primary Categories
    Resource = 1 << 0,      // Wood, stone, fibers
    Tool = 1 << 1,          // Axes, pickaxes, knives
    Weapon = 1 << 2,        // Spears, bows, clubs
    Consumable = 1 << 3,    // Food, water, medicine
    Equipment = 1 << 4,     // Armor, clothing, accessories
    Building = 1 << 5,      // Structures, furniture
    Material = 1 << 6,      // Processed materials
    Quest = 1 << 7,         // Quest items
    Misc = 1 << 8,          // Everything else

    // Special flags
    Stackable = 1 << 16,    // Can stack in inventory
    Unique = 1 << 17,       // Only one allowed
    Essential = 1 << 18,    // Cannot be dropped
    Perishable = 1 << 19,   // Has durability/spoilage

    // Common combinations
    CraftingMaterial = Resource | Material,
    Survival = Tool | Equipment,
    Combat = Weapon | Equipment,

    // For UI filtering
    All = ~0,
    AllItems = Resource | Tool | Weapon | Consumable | Equipment | Building | Material | Quest | Misc
}

/// <summary>
/// Extension methods for ItemType
/// </summary>
public static class ItemTypeExtensions
{
    public static bool HasFlag(this ItemType type, ItemType flag)
    {
        return (type & flag) != 0;
    }

    public static bool IsStackable(this ItemType type)
    {
        return (type & ItemType.Stackable) != 0;
    }

    public static bool IsEssential(this ItemType type)
    {
        return (type & ItemType.Essential) != 0;
    }

    public static string GetDisplayName(this ItemType type)
    {
        // Remove flags for display
        var baseType = type & ItemType.AllItems;
        return baseType.ToString();
    }
}