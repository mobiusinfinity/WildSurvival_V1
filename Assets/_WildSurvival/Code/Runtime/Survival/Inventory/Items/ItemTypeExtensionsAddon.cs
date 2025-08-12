using UnityEngine;

/// <summary>
/// Additional extension methods for ItemType that aren't already defined
/// </summary>
public static class ItemTypeExtensionsAddon
{
    /// <summary>
    /// Get the inventory tab index for this item type
    /// Used for organizing items in different tabs
    /// </summary>
    public static int GetTabIndex(this ItemType type)
    {
        // Determine primary category for tab placement
        if ((type & ItemType.Tool) != 0) return 0;
        if ((type & ItemType.Weapon) != 0) return 1;
        if ((type & ItemType.Equipment) != 0) return 2;
        if ((type & ItemType.Consumable) != 0) return 3;
        if ((type & ItemType.Resource) != 0) return 4;
        if ((type & ItemType.Material) != 0) return 5;
        if ((type & ItemType.Building) != 0) return 6;
        if ((type & ItemType.Quest) != 0) return 7;

        return 8; // Misc
    }

    /// <summary>
    /// Get color for item rarity/type in UI
    /// </summary>
    public static Color GetTypeColor(this ItemType type)
    {
        if ((type & ItemType.Quest) != 0) return Color.yellow;
        if ((type & ItemType.Unique) != 0) return Color.magenta;
        if ((type & ItemType.Equipment) != 0) return Color.cyan;
        if ((type & ItemType.Weapon) != 0) return new Color(1f, 0.5f, 0f); // Orange
        if ((type & ItemType.Tool) != 0) return Color.green;
        if ((type & ItemType.Consumable) != 0) return new Color(0.5f, 1f, 0.5f); // Light green

        return Color.white; // Default
    }

    /// <summary>
    /// Check if items of this type can be equipped
    /// </summary>
    public static bool CanBeEquipped(this ItemType type)
    {
        return (type & (ItemType.Tool | ItemType.Weapon | ItemType.Equipment)) != 0;
    }

    /// <summary>
    /// Check if items of this type can be consumed
    /// </summary>
    public static bool CanBeConsumed(this ItemType type)
    {
        return (type & ItemType.Consumable) != 0;
    }
}