// PlayerInventory.cs - Updated to work with your existing systems
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("References")]
    private InventoryManager inventoryManager;
    private PlayerStats playerStats;
    private FireInteractionController fireController;

    [Header("Configuration")]
    [SerializeField] private float warmthUpdateInterval = 1f;
    private float warmthUpdateTimer;

    // Cache
    private Dictionary<FireInstance, float> nearbyFires = new Dictionary<FireInstance, float>();

    #region Initialization

    private void Awake()
    {
        inventoryManager = InventoryManager.Instance;
        playerStats = GetComponent<PlayerStats>();
        fireController = GetComponent<FireInteractionController>();

        // Add FireInteractionController if missing
        if (fireController == null)
        {
            fireController = gameObject.AddComponent<FireInteractionController>();
        }
    }

    #endregion

    #region Update

    private void Update()
    {
        // Update warmth from fires
        warmthUpdateTimer += Time.deltaTime;
        if (warmthUpdateTimer >= warmthUpdateInterval)
        {
            warmthUpdateTimer = 0f;
            UpdateWarmthFromFires();
        }
    }

    #endregion

    #region Inventory Bridge Methods

    /// <summary>
    /// Check if player has an item by ItemDefinition
    /// </summary>
    public bool HasItem(ItemDefinition item, int quantity = 1)
    {
        if (item == null || inventoryManager == null) return false;

        // Convert ItemDefinition to ItemData
        var itemData = GetItemData(item.itemID);
        if (itemData == null) return false;

        return inventoryManager.HasItem(itemData.itemID, quantity);
    }

    /// <summary>
    /// Remove item from inventory by ItemDefinition
    /// </summary>
    public void RemoveItem(ItemDefinition item, int quantity)
    {
        if (item == null || inventoryManager == null) return;

        inventoryManager.RemoveItem(item.itemID, quantity);
    }

    /// <summary>
    /// Add item to inventory by ItemDefinition
    /// </summary>
    public bool AddItem(ItemDefinition item, int quantity)
    {
        if (item == null || inventoryManager == null) return false;

        // Convert ItemDefinition to ItemData
        var itemData = GetItemData(item.itemID);
        if (itemData == null)
        {
            Debug.LogWarning($"[PlayerInventory] ItemData not found for {item.itemID}");
            return false;
        }

        return inventoryManager.AddItem(itemData, quantity);
    }

    /// <summary>
    /// Get count of an item by ItemDefinition
    /// </summary>
    public int GetItemCount(ItemDefinition item)
    {
        if (item == null || inventoryManager == null) return 0;
        return inventoryManager.GetItemCount(item.itemID);
    }

    /// <summary>
    /// Get all items in inventory as ItemStacks
    /// </summary>
    public List<ItemStack> GetAllItems()
    {
        var items = new List<ItemStack>();
        if (inventoryManager == null) return items;

        foreach (var slot in inventoryManager.InventorySlots)
        {
            if (slot != null && !slot.IsEmpty)
            {
                items.Add(new ItemStack
                {
                    item = ConvertToItemDefinition(slot.Item),
                    quantity = slot.Quantity
                });
            }
        }

        return items;
    }

    #endregion

    #region ItemData <-> ItemDefinition Conversion

    private ItemData GetItemData(string itemID)
    {
        // Try to get from inventory manager
        var data = inventoryManager?.GetItemData(itemID);
        if (data != null) return data;

        // Try to load from Resources
        data = Resources.Load<ItemData>($"Items/{itemID}");
        if (data != null) return data;

        // Create a temporary ItemData from ItemDefinition
        var itemDef = GetItemDefinition(itemID);
        if (itemDef != null)
        {
            return CreateItemDataFromDefinition(itemDef);
        }

        return null;
    }

    private ItemDefinition GetItemDefinition(string itemID)
    {
        // Try to load from Resources
        var def = Resources.Load<ItemDefinition>($"Items/{itemID}");
        if (def != null) return def;

        // Try from database
        var database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database != null)
        {
            return database.GetItem(itemID);
        }
        // Or load from resources
        return Resources.Load<ItemDefinition>($"Items/{itemID}");

        //return null;
    }

    private ItemDefinition ConvertToItemDefinition(ItemData data)
    {
        if (data == null) return null;

        // Try to find existing ItemDefinition
        var def = GetItemDefinition(data.itemID);
        if (def != null) return def;

        // Create temporary ItemDefinition
        var tempDef = ScriptableObject.CreateInstance<ItemDefinition>();
        tempDef.itemID = data.itemID;
        tempDef.displayName = data.ItemName;
        tempDef.description = data.Description;
        tempDef.weight = data.Weight;
        tempDef.maxStackSize = data.maxStackSize;
        tempDef.icon = data.Icon;

        return tempDef;
    }

    private ItemData CreateItemDataFromDefinition(ItemDefinition def)
    {
        var data = ScriptableObject.CreateInstance<ItemData>();
        data.itemID = def.itemID;
        data.ItemName = def.displayName;
        data.Description = def.description;
        data.Weight = def.weight;
        data.maxStackSize = def.maxStackSize;
        data.Icon = def.icon;
        data.Type = ConvertItemCategory(def.primaryCategory);

        return data;
    }

    private ItemType ConvertItemCategory(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Resource => ItemType.Resource,
            ItemCategory.Tool => ItemType.Tool,
            ItemCategory.Weapon => ItemType.Weapon,
            ItemCategory.Food => ItemType.Consumable,
            ItemCategory.Fuel => ItemType.Resource,
            _ => ItemType.Misc
        };
    }

    #endregion

    #region Fire Warmth System

    private void UpdateWarmthFromFires()
    {
        if (playerStats == null) return;

        // Find all nearby fires
        var fires = Physics.OverlapSphere(transform.position, 10f)
            .Select(c => c.GetComponent<FireInstance>())
            .Where(f => f != null)
            .ToList();

        float totalWarmth = 0f;

        foreach (var fire in fires)
        {
            float distance = Vector3.Distance(transform.position, fire.transform.position);
            float warmth = fire.GetWarmthAtDistance(distance);
            totalWarmth += warmth;
        }

        // Apply warmth to body temperature
        if (totalWarmth > 0)
        {
            float currentTemp = playerStats.BodyTemperature;
            float targetTemp = Mathf.Min(37f + totalWarmth * 0.1f, 39f);
            playerStats.SetBodyTemperature(Mathf.Lerp(currentTemp, targetTemp, Time.deltaTime * 0.5f));
        }
    }

    //public float GetWarmthAtDistance(float distance)
    //{
        //if (currentState == FireState.Unlit || currentState == FireState.Extinguished)
        //    return 0f;

        //if (distance > fireRadius) return 0f;

        //float normalizedDist = distance / fireRadius;
        //float falloff = heatFalloffCurve != null ?
        //    heatFalloffCurve.Evaluate(normalizedDist) :
        //    (1f - normalizedDist);

        //// Base warmth on temperature and distance
        //float baseWarmth = currentTemperature / 10f; // 0-80 warmth units
        //return baseWarmth * falloff;
    //}

    //public bool CanLightTorch()
    //{
    //    return currentState == FireState.Burning || currentState == FireState.Blazing;
    //}

    //public float GetFuelPercentage()
    //{
    //    return maxFuelCapacity > 0 ? currentFuelAmount / maxFuelCapacity : 0f;
    //}

    //public FireState GetState()
    //{
    //    return currentState;
    //}

    //public void SetFireType(FireInstance.FireType fireType)
    //{
    //    fireType = type;
    //}

    //public void SetMaxTemperature(float temp)
    //{
    //    maxTemperature = temp;
    //}

    //public void SetFuelCapacity(float capacity)
    //{
    //    maxFuelCapacity = capacity;
    //}

    #endregion

    #region Helper Classes

    [System.Serializable]
    public class ItemStack
    {
        public ItemDefinition item;
        public int quantity;
    }

    #endregion
}