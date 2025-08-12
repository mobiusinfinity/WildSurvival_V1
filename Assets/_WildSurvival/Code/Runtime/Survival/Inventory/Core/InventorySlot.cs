using System;
using UnityEngine;

/// <summary>
/// Represents a single slot in the inventory.
/// Manages item storage, stacking, and slot-specific rules.
/// </summary>
[System.Serializable]
public class InventorySlot
{
    [SerializeField] private int slotIndex;
    [SerializeField] private ItemStack itemStack;
    [SerializeField] private SlotType slotType;
    [SerializeField] private ItemType allowedTypes;
    [SerializeField] private bool isLocked;
    [SerializeField] private string slotTag;

    // Events
    public event Action<InventorySlot> OnSlotChanged;
    public event Action<InventorySlot> OnItemAdded;
    public event Action<InventorySlot> OnItemRemoved;

    // Properties
    public int SlotIndex => slotIndex;
    public ItemStack Stack => itemStack;
    public SlotType Type => slotType;
    public ItemType AllowedTypes => allowedTypes;
    public bool IsLocked
    {
        get => isLocked;
        set => isLocked = value;
    }
    public string SlotTag => slotTag;

    // State properties
    public bool IsEmpty => itemStack == null || itemStack.IsEmpty;
    public bool HasItem => !IsEmpty;
    public bool IsFull => itemStack != null && itemStack.IsFull;
    public bool CanAcceptMore => !IsEmpty && !IsFull && itemStack.Item.IsStackable();

    // Quick access to item data
    public ItemData Item => itemStack?.Item;
    public int Quantity => itemStack?.Quantity ?? 0;
    public float TotalWeight => itemStack?.TotalWeight ?? 0f;

    /// <summary>
    /// Slot types for different inventory sections
    /// </summary>
    public enum SlotType
    {
        General,        // Normal inventory slot
        Equipment,      // Equipment slots (armor, weapons)
        Hotbar,         // Quick access bar
        Storage,        // External storage (chests, etc)
        Crafting,       // Crafting grid slots
        Output,         // Crafting output slot
        Fuel,           // Fuel slot for fires/forges
        Special         // Special purpose slots
    }

    // Constructors
    public InventorySlot(int index)
    {
        slotIndex = index;
        slotType = SlotType.General;
        allowedTypes = ItemType.All;
        itemStack = null;
        isLocked = false;
        slotTag = "";
    }

    public InventorySlot(int index, SlotType type, ItemType allowed = ItemType.All)
    {
        slotIndex = index;
        slotType = type;
        allowedTypes = allowed;
        itemStack = null;
        isLocked = false;
        slotTag = "";
    }

    /// <summary>
    /// Check if this slot can accept a specific item
    /// </summary>
    public bool CanAcceptItem(ItemData item)
    {
        if (item == null) return false;
        if (isLocked) return false;

        // Check type restrictions
        if (allowedTypes != ItemType.All)
        {
            if ((item.Type & allowedTypes) == 0)
                return false;
        }

        // Check slot type specific rules
        switch (slotType)
        {
            case SlotType.Equipment:
                return item.IsEquippable;

            case SlotType.Fuel:
                return item.HasCraftingTag;

            case SlotType.Output:
                return false; // Output slots are read-only

            case SlotType.Crafting:
                return item.IsCraftable || item.Type.HasFlag(ItemType.Material);
        }

        return true;
    }

    /// <summary>
    /// Check if this slot can stack with an item
    /// </summary>
    public bool CanStackWith(ItemData item, int quantity = 1)
    {
        if (!CanAcceptItem(item)) return false;

        if (IsEmpty)
        {
            return true; // Can always add to empty slot
        }

        if (itemStack.Item != item)
        {
            return false; // Different items can't stack
        }

        return itemStack.Quantity + quantity <= itemStack.GetMaxStackSize();
    }

    /// <summary>
    /// Try to add an item to this slot
    /// </summary>
    public int TryAddItem(ItemData item, int quantity = 1)
    {
        if (!CanAcceptItem(item)) return 0;

        if (IsEmpty)
        {
            // Create new stack
            itemStack = new ItemStack(item, quantity);
            OnItemAdded?.Invoke(this);
            OnSlotChanged?.Invoke(this);
            return quantity;
        }

        if (itemStack.Item == item && item.IsStackable())
        {
            // Add to existing stack
            int spaceAvailable = itemStack.GetMaxStackSize() - itemStack.Quantity;
            int amountToAdd = Mathf.Min(spaceAvailable, quantity);

            if (amountToAdd > 0)
            {
                itemStack.Quantity += amountToAdd;
                OnSlotChanged?.Invoke(this);
                return amountToAdd;
            }
        }

        return 0;
    }

    /// <summary>
    /// Set the item stack directly
    /// </summary>
    public void SetStack(ItemStack stack)
    {
        bool wasEmpty = IsEmpty;
        itemStack = stack;

        if (wasEmpty && !IsEmpty)
        {
            OnItemAdded?.Invoke(this);
        }
        else if (!wasEmpty && IsEmpty)
        {
            OnItemRemoved?.Invoke(this);
        }

        OnSlotChanged?.Invoke(this);
    }

    /// <summary>
    /// Remove a specific quantity from the slot
    /// </summary>
    public int RemoveItem(int quantity = 1)
    {
        if (IsEmpty) return 0;

        int removed = Mathf.Min(quantity, itemStack.Quantity);
        itemStack.Quantity -= removed;

        if (itemStack.Quantity <= 0)
        {
            Clear();
        }
        else
        {
            OnSlotChanged?.Invoke(this);
        }

        return removed;
    }

    /// <summary>
    /// Take the entire stack from this slot
    /// </summary>
    public ItemStack TakeStack()
    {
        if (IsEmpty) return null;

        var stack = itemStack;
        itemStack = null;

        OnItemRemoved?.Invoke(this);
        OnSlotChanged?.Invoke(this);

        return stack;
    }

    /// <summary>
    /// Split the stack in this slot
    /// </summary>
    public ItemStack SplitStack(int amount)
    {
        if (IsEmpty || amount <= 0) return null;

        var splitStack = itemStack.Split(amount);

        if (itemStack.IsEmpty)
        {
            Clear();
        }
        else
        {
            OnSlotChanged?.Invoke(this);
        }

        return splitStack;
    }

    /// <summary>
    /// Swap contents with another slot
    /// </summary>
    public void SwapWith(InventorySlot other)
    {
        if (other == null || other == this) return;

        // Check if swap is allowed
        if (isLocked || other.isLocked) return;

        // Check type compatibility
        bool thisCanAcceptOther = IsEmpty || CanAcceptItem(other.Item);
        bool otherCanAcceptThis = other.IsEmpty || other.CanAcceptItem(Item);

        if (!thisCanAcceptOther || !otherCanAcceptThis) return;

        // Perform swap
        var temp = itemStack;
        itemStack = other.itemStack;
        other.itemStack = temp;

        OnSlotChanged?.Invoke(this);
        other.OnSlotChanged?.Invoke(other);
    }

    /// <summary>
    /// Try to merge with another slot
    /// </summary>
    public int MergeWith(InventorySlot other)
    {
        if (other == null || other == this) return 0;
        if (IsEmpty || other.IsEmpty) return 0;
        if (itemStack.Item != other.itemStack.Item) return 0;

        return itemStack.AddFrom(other.itemStack);
    }

    /// <summary>
    /// Update slot timers (for spoilage, etc)
    /// </summary>
    public void UpdateTimers(float deltaTime)
    {
        if (!IsEmpty)
        {
            itemStack.UpdateTimers(deltaTime);

            // Remove spoiled items
            if (itemStack.Item.CanSpoil && itemStack.IsSpoiled)
            {
                Clear();
            }
        }
    }

    /// <summary>
    /// Clear this slot
    /// </summary>
    public void Clear()
    {
        if (IsEmpty) return;

        itemStack = null;
        OnItemRemoved?.Invoke(this);
        OnSlotChanged?.Invoke(this);
    }

    /// <summary>
    /// Use the item in this slot
    /// </summary>
    public bool UseItem(GameObject target = null)
    {
        if (IsEmpty || !itemStack.Item.CanUse) return false;

        // Apply item effects
        foreach (var effect in itemStack.Item.UseEffects)
        {
            ApplyItemEffect(effect, target);
        }

        // Consume if consumable
        if (itemStack.Item.IsConsumable)
        {
            RemoveItem(1);
        }

        return true;
    }

    private void ApplyItemEffect(ItemEffect effect, GameObject target)
    {
        // This would connect to player stats, etc
        //Debug.Log($"Applying effect: {effect.Type} with value {effect.Value}");

        // Example implementation:
        //if (target != null)
        //{
        //    var stats = target.GetComponent<PlayerStats>();
        //    if (stats != null)
        //    {
        //        switch (effect.Type)
        //        {
        //            case ItemEffect.EffectType.RestoreHealth:
        //                // Use the actual PlayerStats methods
        //                stats.Health = Mathf.Min(stats.Health + effect.Value, stats.MaxHealth);
        //                break;
        //            case ItemEffect.EffectType.RestoreStamina:
        //                stats.Stamina = Mathf.Min(stats.Stamina + effect.Value, stats.MaxStamina);
        //                break;
        //            case ItemEffect.EffectType.RestoreHunger:
        //                stats.Hunger = Mathf.Min(stats.Hunger + effect.Value, stats.MaxHunger);
        //                break;
        //            case ItemEffect.EffectType.RestoreThirst:
        //                stats.Thirst = Mathf.Min(stats.Thirst + effect.Value, stats.MaxThirst);
        //                break;
        //            case ItemEffect.EffectType.DamageHealth:
        //                stats.TakeDamage(effect.Value);
        //                break;
        //                // Add more cases as needed
        //        }
              
        //    }
        //}

        // TODO: Connect to PlayerStats when methods are available
        // This will be implemented when PlayerStats has the necessary methods
        /*
        Example future implementation:
        var stats = target?.GetComponent<PlayerStats>();
        if (stats != null)
        {
            switch (effect.Type)
            {
                case ItemEffect.EffectType.RestoreHealth:
                    stats.ModifyHealth(effect.Value);
                    break;
                // etc...
            }
        }
        */
    }

    /// <summary>
    /// Get display information for UI
    /// </summary>
    public string GetDisplayText()
    {
        if (IsEmpty) return "Empty";
        return itemStack.GetDisplayName();
    }

    /// <summary>
    /// Clone this slot
    /// </summary>
    public InventorySlot Clone()
    {
        var clone = new InventorySlot(slotIndex, slotType, allowedTypes);
        if (!IsEmpty)
        {
            clone.itemStack = new ItemStack(itemStack);
        }
        clone.isLocked = isLocked;
        clone.slotTag = slotTag;
        return clone;
    }
}