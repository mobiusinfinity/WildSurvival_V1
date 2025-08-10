using System;
using UnityEngine;

namespace WildSurvival.Data
{
    [Serializable]
    [CreateAssetMenu(fileName = "NewItem", menuName = "WildSurvival/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string itemID = "";
        public string displayName = "New Item";
        [TextArea(3, 5)]
        public string description = "";
        public Sprite icon;
        public GameObject worldModel;

        [Header("Categories")]
        public ItemCategory primaryCategory = ItemCategory.Misc;
        public ItemSubcategory subcategory = ItemSubcategory.None;
        public ItemTag[] tags = new ItemTag[0];

        [Header("Inventory")]
        public Vector2Int gridSize = Vector2Int.one;
        [HideInInspector]
        public bool[,] shapeGrid;
        public float weight = 1f;
        public int maxStackSize = 1;
        public bool canRotateInInventory = true;

        [Header("Durability")]
        public bool hasDurability = false;
        public float maxDurability = 100f;
        public float durabilityLossRate = 0.1f;

        [Header("Value")]
        public int baseValue = 1;
        public float rarityMultiplier = 1f;

        [Header("Usage")]
        public bool isConsumable = false;
        public bool isEquippable = false;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(itemID))
            {
                itemID = name.Replace(" ", "_").ToLower();
            }

            // Initialize shape grid if needed
            if (shapeGrid == null || shapeGrid.GetLength(0) != gridSize.x || shapeGrid.GetLength(1) != gridSize.y)
            {
                shapeGrid = new bool[gridSize.x, gridSize.y];
                // Default to full shape
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

    // Enums for ItemDefinition
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

    public enum ItemTag
    {
        None,
        Wood,
        Stone,
        Metal,
        Organic,
        Fuel,
        Sharp,
        Heavy,
        Fragile,
        Valuable,
        QuestItem,
        Stackable,
        Consumable,
        Tool,
        Weapon,
        CraftingMaterial
    }

    public enum ItemQuality
    {
        Ruined = -1,
        Poor = 0,
        Common = 1,
        Good = 2,
        Excellent = 3,
        Masterwork = 4,
        Legendary = 5
    }
}