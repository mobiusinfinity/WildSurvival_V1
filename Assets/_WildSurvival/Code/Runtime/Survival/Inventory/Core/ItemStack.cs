using UnityEngine;
using System;

/// <summary>
/// Represents an item with quantity and condition in the inventory.
/// Handles stacking logic and item state.
/// </summary>
[System.Serializable]
public class ItemStack : ICloneable
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int quantity = 1;
    [SerializeField] private float durability = 100f;
    [SerializeField] private float spoilageTimer = 0f;
    [SerializeField] private ItemMetadata metadata;

    // Properties
    public ItemData Item
    {
        get => itemData;
        set => itemData = value;
    }

    public int Quantity
    {
        get => quantity;
        set => quantity = Mathf.Clamp(value, 0, GetMaxStackSize());
    }

    public float Durability
    {
        get => durability;
        set => durability = Mathf.Clamp(value, 0f, GetMaxDurability());
    }

    public float SpoilageTimer
    {
        get => spoilageTimer;
        set => spoilageTimer = Mathf.Max(0f, value);
    }

    public ItemMetadata Metadata => metadata;

    // Computed properties
    public bool IsEmpty => itemData == null || quantity <= 0;
    public bool IsFull => quantity >= GetMaxStackSize();
    public bool IsDamaged => itemData != null && itemData.HasDurability && durability < itemData.MaxDurability;
    public bool IsSpoiled => itemData != null && itemData.CanSpoil && spoilageTimer >= itemData.SpoilTime;
    public float TotalWeight => itemData != null ? itemData.Weight * quantity : 0f;
    public int TotalValue => itemData != null ? itemData.Value * quantity : 0;

    // Constructors
    public ItemStack()
    {
        metadata = new ItemMetadata();
    }

    public ItemStack(ItemData item, int qty = 1) : this()
    {
        itemData = item;
        quantity = Mathf.Clamp(qty, 0, GetMaxStackSize());

        if (item != null)
        {
            durability = item.MaxDurability;
            spoilageTimer = 0f;
        }
    }

    public ItemStack(ItemStack other) : this()
    {
        if (other != null)
        {
            itemData = other.itemData;
            quantity = other.quantity;
            durability = other.durability;
            spoilageTimer = other.spoilageTimer;
            metadata = other.metadata?.Clone();
        }
    }

    /// <summary>
    /// Get maximum stack size for current item
    /// </summary>
    public int GetMaxStackSize()
    {
        return itemData != null ? itemData.MaxStackSize : 1;
    }

    /// <summary>
    /// Get maximum durability for current item
    /// </summary>
    public float GetMaxDurability()
    {
        return itemData != null ? itemData.MaxDurability : 100f;
    }

    /// <summary>
    /// Check if this stack can merge with another
    /// </summary>
    public bool CanStackWith(ItemStack other)
    {
        if (other == null || other.IsEmpty || IsEmpty) return false;
        if (itemData != other.itemData) return false;
        if (!itemData.IsStackable()) return false;
        if (IsFull) return false;

        // Don't stack items with different durability
        if (itemData.HasDurability && Math.Abs(durability - other.durability) > 0.01f)
            return false;

        // Don't stack items with different spoilage
        if (itemData.CanSpoil && Math.Abs(spoilageTimer - other.spoilageTimer) > 0.01f)
            return false;

        // Check metadata compatibility
        if (!metadata.IsCompatibleWith(other.metadata))
            return false;

        return true;
    }

    /// <summary>
    /// Try to add quantity from another stack
    /// </summary>
    public int AddFrom(ItemStack other)
    {
        if (!CanStackWith(other)) return 0;

        int spaceAvailable = GetMaxStackSize() - quantity;
        int amountToAdd = Mathf.Min(spaceAvailable, other.quantity);

        quantity += amountToAdd;
        other.quantity -= amountToAdd;

        return amountToAdd;
    }

    /// <summary>
    /// Split this stack into two
    /// </summary>
    public ItemStack Split(int amount)
    {
        if (amount <= 0 || amount >= quantity) return null;

        var newStack = new ItemStack(this);
        newStack.quantity = amount;
        quantity -= amount;

        return newStack;
    }

    /// <summary>
    /// Update timers (called each frame or game tick)
    /// </summary>
    public void UpdateTimers(float deltaTime)
    {
        if (itemData == null) return;

        // Update spoilage
        if (itemData.CanSpoil && spoilageTimer < itemData.SpoilTime)
        {
            spoilageTimer += deltaTime;

            if (IsSpoiled)
            {
                OnItemSpoiled();
            }
        }
    }

    /// <summary>
    /// Apply damage to item durability
    /// </summary>
    public void ApplyDamage(float damage)
    {
        if (itemData == null || !itemData.HasDurability) return;

        durability = Mathf.Max(0, durability - damage);

        if (durability <= 0)
        {
            OnItemBroken();
        }
    }

    /// <summary>
    /// Repair item durability
    /// </summary>
    public void Repair(float amount)
    {
        if (itemData == null || !itemData.HasDurability) return;

        durability = Mathf.Min(itemData.MaxDurability, durability + amount);
    }

    /// <summary>
    /// Called when item spoils
    /// </summary>
    protected virtual void OnItemSpoiled()
    {
        // Could transform into spoiled version or destroy
        Debug.Log($"{itemData.ItemName} has spoiled!");
    }

    /// <summary>
    /// Called when item breaks
    /// </summary>
    protected virtual void OnItemBroken()
    {
        // Could give back materials or destroy
        Debug.Log($"{itemData.ItemName} has broken!");
        quantity = 0; // Remove broken item
    }

    /// <summary>
    /// Clear this stack
    /// </summary>
    public void Clear()
    {
        itemData = null;
        quantity = 0;
        durability = 100f;
        spoilageTimer = 0f;
        metadata?.Clear();
    }

    /// <summary>
    /// Clone this stack
    /// </summary>
    public object Clone()
    {
        return new ItemStack(this);
    }

    /// <summary>
    /// Get display name with quantity
    /// </summary>
    public string GetDisplayName()
    {
        if (itemData == null) return "Empty";

        string name = itemData.ItemName;
        if (quantity > 1 && itemData.IsStackable())
        {
            name += $" x{quantity}";
        }

        if (IsDamaged)
        {
            name += $" ({(durability / itemData.MaxDurability * 100):F0}%)";
        }

        if (itemData.CanSpoil)
        {
            float spoilPercent = (1f - spoilageTimer / itemData.SpoilTime) * 100;
            if (spoilPercent < 50)
            {
                name += $" (Fresh: {spoilPercent:F0}%)";
            }
        }

        return name;
    }

    public override string ToString()
    {
        return GetDisplayName();
    }
}

/// <summary>
/// Additional metadata for special items
/// </summary>
[System.Serializable]
public class ItemMetadata
{
    [SerializeField] private string customName;
    [SerializeField] private string customDescription;
    [SerializeField] private int enchantmentLevel;
    [SerializeField] private string craftedBy;
    [SerializeField] private float craftedTime;

    public string CustomName
    {
        get => customName;
        set => customName = value;
    }

    public string CustomDescription
    {
        get => customDescription;
        set => customDescription = value;
    }

    public int EnchantmentLevel
    {
        get => enchantmentLevel;
        set => enchantmentLevel = Mathf.Max(0, value);
    }

    public string CraftedBy
    {
        get => craftedBy;
        set => craftedBy = value;
    }

    public float CraftedTime
    {
        get => craftedTime;
        set => craftedTime = value;
    }

    public bool HasCustomData => !string.IsNullOrEmpty(customName) ||
                                    !string.IsNullOrEmpty(customDescription) ||
                                    enchantmentLevel > 0 ||
                                    !string.IsNullOrEmpty(craftedBy);

    public bool IsCompatibleWith(ItemMetadata other)
    {
        if (other == null) return !HasCustomData;

        // Items with custom names/enchantments don't stack
        if (HasCustomData || other.HasCustomData) return false;

        return true;
    }

    public void Clear()
    {
        customName = null;
        customDescription = null;
        enchantmentLevel = 0;
        craftedBy = null;
        craftedTime = 0;
    }

    public ItemMetadata Clone()
    {
        return new ItemMetadata
        {
            customName = customName,
            customDescription = customDescription,
            enchantmentLevel = enchantmentLevel,
            craftedBy = craftedBy,
            craftedTime = craftedTime
        };
    }
}