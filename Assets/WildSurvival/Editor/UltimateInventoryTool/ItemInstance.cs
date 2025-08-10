using System;
using System.Collections.Generic;
using UnityEngine;
using WildSurvival.Data;

namespace WildSurvival.Inventory
{
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
            currentDurability = Mathf.Clamp(durability, 0, definition.maxDurability);
            condition = currentDurability / definition.maxDurability;
        }
    }
}