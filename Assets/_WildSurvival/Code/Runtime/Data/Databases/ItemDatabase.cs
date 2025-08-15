using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Database that supports both ItemData (legacy) and ItemDefinition (new)
/// This allows gradual migration without breaking existing assets
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Wild Survival/Databases/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("Legacy Items (ItemData)")]
    [SerializeField] private List<ItemData> legacyItems = new List<ItemData>();

    // ADD this singleton instance
    private static ItemDatabase instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (instance == null)
                {
                    instance = FindObjectOfType<ItemDatabase>();
                }
            }
            return instance;
        }
    }

    [Header("New Items (ItemDefinition)")]
    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

    public bool AddItem(ItemDefinition item)
    {
        if (item == null)
        {
            Debug.LogError("Cannot add null item to database!");
            return false;
        }

        // Check for duplicate ID
        if (items.Any(i => i != null && i.itemID == item.itemID))
        {
            Debug.LogError($"Item with ID '{item.itemID}' already exists in database!");
            return false;
        }

        // Check for duplicate asset (same object)
        if (items.Contains(item))
        {
            Debug.LogWarning($"Item '{item.itemID}' is already in the database!");
            return false;
        }

        items.Add(item);
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
        Debug.Log($"✅ Added item '{item.displayName}' (ID: {item.itemID}) to database");
        return true;
    }

    public bool HasItemWithID(string itemID)
    {
        return items.Any(i => i != null && i.itemID == itemID);
    }

    public bool HasItemWithName(string displayName)
    {
        return items.Any(i => i != null && i.displayName == displayName);
    }

    // Clean up any null or duplicate entries
    public void CleanDatabase()
    {
        // Remove nulls
        items.RemoveAll(i => i == null);

        // Remove duplicates (keep first occurrence)
        var seen = new HashSet<string>();
        var uniqueItems = new List<ItemDefinition>();

        foreach (var item in items)
        {
            if (!seen.Contains(item.itemID))
            {
                seen.Add(item.itemID);
                uniqueItems.Add(item);
            }
            else
            {
                Debug.LogWarning($"Removed duplicate item: {item.itemID}");
            }
        }

        items = uniqueItems;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    // Add legacy ItemData
    public void AddLegacyItem(ItemData item)
    {
        if (item != null && !legacyItems.Contains(item))
        {
            legacyItems.Add(item);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }

    public void RemoveItem(ItemDefinition item)
    {
        if (items.Remove(item))
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }

    // Get all items as ItemDefinition (converts legacy on the fly)
    public List<ItemDefinition> GetAllItems()
    {
        List<ItemDefinition> allItems = new List<ItemDefinition>(items);

        // Convert legacy items to ItemDefinition temporarily
        foreach (var legacy in legacyItems)
        {
            if (legacy != null)
            {
                var converted = ConvertToDefinition(legacy);
                if (converted != null)
                    allItems.Add(converted);
            }
        }

        return allItems;
    }

    public ItemDefinition GetItem(string itemID)
    {
        // First check new items
        var item = items.FirstOrDefault(i => i.itemID == itemID);
        if (item != null) return item;

        // Then check legacy items and convert
        var legacy = legacyItems.FirstOrDefault(i => i.itemID == itemID);
        if (legacy != null)
            return ConvertToDefinition(legacy);

        return null;
    }

    public List<ItemDefinition> GetItemsByCategory(ItemCategory category)
    {
        return GetAllItems().Where(i => i.primaryCategory == category).ToList();
    }

    public void Clear()
    {
        items.Clear();
        legacyItems.Clear();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    // Convert ItemData to ItemDefinition on the fly
    private ItemDefinition ConvertToDefinition(ItemData data)
    {
        if (data == null) return null;

        // Create temporary ItemDefinition (not saved as asset)
        var def = ScriptableObject.CreateInstance<ItemDefinition>();

        // Map properties
        def.itemID = data.itemID;
        def.displayName = data.itemName;
        def.description = data.description;
        def.icon = data.icon;
        def.worldModel = data.worldPrefab;

        // Map category
        def.primaryCategory = ConvertTypeToCategory(data.itemType);

        // Map properties
        def.weight = data.weight;
        def.maxStackSize = data.maxStackSize;
        def.gridSize = data.gridSize;
        def.baseValue = data.value;

        // Map durability
        def.hasDurability = data.hasDurability;
        def.maxDurability = data.maxDurability;

        // Set grid properties
        def.allowRotation = data.gridSize.x != data.gridSize.y;

        // Handle stackable flag
        if (data.itemType.HasFlag(ItemType.Stackable))
        {
            def.maxStackSize = Mathf.Max(def.maxStackSize, 10); // Ensure stackable items have stack size > 1
        }

        def.name = data.name + "_Converted";

        return def;
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

#if UNITY_EDITOR
    [ContextMenu("Migrate All Legacy Items")]
    public void MigrateAllLegacyItems()
    {
        if (EditorUtility.DisplayDialog("Migrate Items",
            $"Convert {legacyItems.Count} ItemData assets to ItemDefinition?",
            "Convert", "Cancel"))
        {
            int converted = 0;
            foreach (var legacy in legacyItems.ToList())
            {
                if (ConvertAndSaveLegacyItem(legacy))
                {
                    converted++;
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"Migrated {converted} items successfully!");
        }
    }

    private bool ConvertAndSaveLegacyItem(ItemData data)
    {
        if (data == null) return false;

        // Create new ItemDefinition asset
        var def = ConvertToDefinition(data);

        // Save as asset
        string path = AssetDatabase.GetAssetPath(data);
        string newPath = path.Replace(".asset", "_Definition.asset");

        AssetDatabase.CreateAsset(def, newPath);

        // Add to new items list
        items.Add(def);

        // Remove from legacy list
        legacyItems.Remove(data);

        return true;
    }

    [ContextMenu("Scan for ItemData Assets")]
    public void ScanForLegacyItems()
    {
        legacyItems.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ItemData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
            {
                legacyItems.Add(item);
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"Found {legacyItems.Count} ItemData assets");
    }

    [ContextMenu("Scan for ItemDefinition Assets")]
    public void RefreshDatabase()
    {
        items.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null)
            {
                items.Add(item);
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"Found {items.Count} ItemDefinition assets");
    }
#endif
}