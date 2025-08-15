#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ItemDataMigration : MonoBehaviour
{
    [MenuItem("Tools/Wild Survival/Migrate/Convert ItemData to ItemDefinition")]
    public static void ConvertItemData()
    {
        // Find all ItemData assets
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        int converted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData oldItem = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (oldItem != null)
            {
                // Create new ItemDefinition
                ItemDefinition newItem = ScriptableObject.CreateInstance<ItemDefinition>();

                // Copy properties
                newItem.itemID = oldItem.itemID;
                newItem.displayName = oldItem.itemName;
                newItem.description = oldItem.description;
                newItem.icon = oldItem.icon;
                newItem.worldModel = oldItem.worldPrefab;
                newItem.weight = oldItem.weight;
                newItem.maxStackSize = oldItem.maxStackSize;
                newItem.baseValue = oldItem.value;

                // Map ItemType to ItemCategory
                newItem.primaryCategory = ConvertTypeToCategory(oldItem.itemType);

                // Use the existing gridSize from ItemData
                newItem.gridSize = oldItem.gridSize;

                // Map durability
                newItem.hasDurability = oldItem.hasDurability;
                newItem.maxDurability = oldItem.maxDurability;

                // Set rotation based on grid dimensions
                newItem.allowRotation = (oldItem.gridSize.x != oldItem.gridSize.y);

                // Handle special flags
                if (oldItem.itemType.HasFlag(ItemType.Stackable))
                {
                    newItem.maxStackSize = Mathf.Max(newItem.maxStackSize, 10);
                }

                // Save the new ItemDefinition
                string dir = System.IO.Path.GetDirectoryName(path);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                string newPath = $"{dir}/{fileName}_Definition.asset";

                AssetDatabase.CreateAsset(newItem, newPath);
                converted++;

                Debug.Log($"Converted {oldItem.itemName} -> {newPath}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Converted {converted} ItemData assets to ItemDefinition");
    }

    private static ItemCategory ConvertTypeToCategory(ItemType type)
    {
        // Handle combined flags for more precise categorization
        if (type.HasFlag(ItemType.Consumable))
        {
            // Check if it's medicine-like
            if (type.HasFlag(ItemType.Perishable) ||
                type.ToString().ToLower().Contains("medicine") ||
                type.ToString().ToLower().Contains("health"))
            {
                return ItemCategory.Medicine;
            }
            return ItemCategory.Food;
        }

        if (type.HasFlag(ItemType.Weapon)) return ItemCategory.Weapon;
        if (type.HasFlag(ItemType.Tool)) return ItemCategory.Tool;
        if (type.HasFlag(ItemType.Equipment)) return ItemCategory.Clothing;
        if (type.HasFlag(ItemType.Building)) return ItemCategory.Building;
        if (type.HasFlag(ItemType.Material) || type.HasFlag(ItemType.Resource))
            return ItemCategory.Resource;

        return ItemCategory.Misc;
    }
}
#endif