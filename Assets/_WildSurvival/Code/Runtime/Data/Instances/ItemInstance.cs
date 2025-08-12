using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime instance of an item in the inventory
/// Tracks stack size, durability, and other runtime data
/// </summary>
[Serializable]
public class ItemInstance
{
    public ItemDefinition definition;
    public int stackSize = 1;
    public ItemQuality quality = ItemQuality.Common;
    public float currentDurability = 100f;
    public float condition = 1f;

    // Runtime data
    public Vector2Int gridPosition;
    public int rotation; // 0, 90, 180, 270
    public Dictionary<string, object> customData = new Dictionary<string, object>();

    public ItemInstance(ItemDefinition def)
    {
        definition = def;
        if (def != null)
        {
            currentDurability = def.maxDurability;
            stackSize = 1;
        }
    }

    public float GetEffectiveWeight()
    {
        if (definition == null) return 0f;
        return definition.weight * stackSize;
    }

    public bool CanStackWith(ItemInstance other)
    {
        if (other == null || definition != other.definition)
            return false;

        if (definition.maxStackSize <= 1)
            return false;

        return stackSize + other.stackSize <= definition.maxStackSize;
    }

    public void RepairTo(float durability)
    {
        if (definition == null) return;
        currentDurability = Mathf.Clamp(durability, 0, definition.maxDurability);
        condition = currentDurability / definition.maxDurability;
    }

    public bool IsBroken()
    {
        return definition != null && definition.hasDurability && currentDurability <= 0;
    }

    public bool IsStackable()
    {
        return definition != null && definition.maxStackSize > 1;
    }

    public int GetRemainingStackSpace()
    {
        if (definition == null) return 0;
        return definition.maxStackSize - stackSize;
    }

    public ItemInstance Clone()
    {
        ItemInstance clone = new ItemInstance(definition)
        {
            stackSize = this.stackSize,
            quality = this.quality,
            currentDurability = this.currentDurability,
            condition = this.condition,
            gridPosition = this.gridPosition,
            rotation = this.rotation
        };

        // Clone custom data
        foreach (var kvp in customData)
        {
            clone.customData[kvp.Key] = kvp.Value;
        }

        return clone;
    }
}