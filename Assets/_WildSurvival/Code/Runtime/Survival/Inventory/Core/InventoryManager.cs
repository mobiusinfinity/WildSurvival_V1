using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Main inventory management system.
/// Handles all inventory operations, slot management, and item transactions.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    #region Singleton
    // Singleton instance (if not already present)
    private static InventoryManager instance;
    public static InventoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<InventoryManager>();
            }
            return instance;
        }
    }
    #endregion

    #region Configuration
    [Header("Inventory Configuration")]
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private int hotbarSize = 8;
    [SerializeField] private float maxWeight = 100f;
    [SerializeField] private bool useWeightLimit = true;
    [SerializeField] private bool autoSortOnPickup = false;

    [Header("Starting Items")]
    [SerializeField] private List<StartingItem> startingItems = new List<StartingItem>();

    [System.Serializable]
    public class StartingItem
    {
        public ItemData item;
        public int quantity = 1;
    }
    #endregion

    #region State
    private InventorySlot[] inventorySlots;
    //[SerializeField] private List<InventorySlot> inventorySlots = new List<InventorySlot>();


    private InventorySlot[] hotbarSlots;
    private InventorySlot[] equipmentSlots;

    private float currentWeight = 0f;
    private int selectedHotbarSlot = 0;
    private bool isInventoryOpen = false;

    // Drag & Drop
    private ItemStack draggedStack;
    private int draggedFromSlot = -1;
    private bool isDragging = false;
    #endregion

    #region Properties
    public int InventorySize => inventorySize;
    public int HotbarSize => hotbarSize;
    public float MaxWeight => maxWeight;
    public float CurrentWeight => currentWeight;
    public float WeightPercentage => maxWeight > 0 ? (currentWeight / maxWeight) * 100f : 0f;
    public bool IsOverWeight => useWeightLimit && currentWeight > maxWeight;
    public bool IsInventoryOpen => isInventoryOpen;
    public int SelectedHotbarSlot => selectedHotbarSlot;

    public InventorySlot[] InventorySlots => inventorySlots;
    public InventorySlot[] HotbarSlots => hotbarSlots;
    public InventorySlot[] EquipmentSlots => equipmentSlots;

    public bool HasSpace => GetFirstEmptySlot() != null;
    public int EmptySlotCount => inventorySlots.Count(s => s.IsEmpty);
    public int UsedSlotCount => inventorySlots.Count(s => !s.IsEmpty);
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeInventory();
    }

    private void Start()
    {
        AddStartingItems();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void Update()
    {
        HandleHotbarInput();
        HandleInventoryToggle();
        UpdateSpoilage();
    }
    #endregion

    #region Initialization
    private void InitializeInventory()
    {
        // Initialize main inventory
        inventorySlots = new InventorySlot[inventorySize];
        for (int i = 0; i < inventorySize; i++)
        {
            inventorySlots[i] = new InventorySlot(i, InventorySlot.SlotType.General);
            inventorySlots[i].OnSlotChanged += OnSlotChanged;
        }

        // Initialize hotbar
        hotbarSlots = new InventorySlot[hotbarSize];
        for (int i = 0; i < hotbarSize; i++)
        {
            hotbarSlots[i] = new InventorySlot(i, InventorySlot.SlotType.Hotbar);
            hotbarSlots[i].OnSlotChanged += OnSlotChanged;
        }

        // Initialize equipment slots (basic setup)
        equipmentSlots = new InventorySlot[6]; // Head, Chest, Legs, Feet, Hands, Back
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            equipmentSlots[i] = new InventorySlot(i, InventorySlot.SlotType.Equipment, ItemType.Equipment);
            equipmentSlots[i].OnSlotChanged += OnSlotChanged;
        }
    }

    private void AddStartingItems()
    {
        foreach (var startingItem in startingItems)
        {
            if (startingItem.item != null)
            {
                AddItem(startingItem.item, startingItem.quantity);
            }
        }
    }

    private void SubscribeToEvents()
    {
        // Subscribe to slot events
        foreach (var slot in inventorySlots)
        {
            slot.OnItemAdded += OnItemAddedToSlot;
            slot.OnItemRemoved += OnItemRemovedFromSlot;
        }
    }

    private void UnsubscribeFromEvents()
    {
        // Unsubscribe from slot events
        foreach (var slot in inventorySlots)
        {
            if (slot != null)
            {
                slot.OnItemAdded -= OnItemAddedToSlot;
                slot.OnItemRemoved -= OnItemRemovedFromSlot;
            }
        }
    }
    #endregion

    #region Public Methods - Adding Items

    public ItemData GetItemData(string itemID)
    {
        // First check current inventory
        foreach (var slot in inventorySlots)
        {
            if (slot != null && slot.Stack != null && slot.Stack.Item != null)
            {
                if (slot.Stack.Item.itemID == itemID)
                {
                    return slot.Stack.Item;
                }
            }
        }

        // Try to load from Resources if not in inventory
        ItemData data = Resources.Load<ItemData>($"Items/{itemID}");
        if (data != null)
            return data;

        // Try to find in project assets
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:ItemData {itemID}");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            data = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data != null && data.itemID == itemID)
                return data;
        }
#endif

        return null;
    }

    public bool HasItem(string itemID, int quantity = 1)
    {
        int totalCount = GetItemCount(itemID);
        return totalCount >= quantity;
    }

    public int GetItemCount(string itemID)
    {
        int count = 0;
        foreach (var slot in inventorySlots)
        {
            if (slot != null && slot.Stack != null && slot.Stack.Item != null)
            {
                if (slot.Stack.Item.itemID == itemID)
                {
                    count += slot.Stack.Quantity;
                }
            }
        }
        return count;
    }

    //public bool AddItem(string itemID, int quantity)
    //{
    //    ItemData data = GetItemData(itemID);
    //    if (data == null)
    //    {
    //        Debug.LogError($"[InventoryManager] Cannot add item - ItemData not found for ID: {itemID}");
    //        return false;
    //    }

    //    return AddItem(data, quantity);
    //}

    public bool AddItem(ItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0)
            return false;

        int remaining = quantity;

        // First try to stack with existing items
        if (itemData.IsStackable())
        {
            foreach (var slot in inventorySlots)
            {
                if (slot != null && slot.Stack != null &&
                    slot.Stack.Item == itemData)
                {
                    int canAdd = itemData.maxStackSize - slot.Stack.Quantity;
                    if (canAdd > 0)
                    {
                        int toAdd = Mathf.Min(canAdd, remaining);
                        slot.Stack.Quantity += toAdd;
                        remaining -= toAdd;

                        if (remaining <= 0)
                            return true;
                    }
                }
            }
        }

        // Then try to find empty slots
        foreach (var slot in inventorySlots)
        {
            if (slot != null && slot.IsEmpty)
            {
                var newStack = new ItemStack
                {
                    Item = itemData,
                    Quantity = Mathf.Min(remaining, itemData.maxStackSize)
                };
                slot.SetStack(newStack);
                remaining -= newStack.Quantity;

                if (remaining <= 0)
                    return true;
            }
        }

        // Return false if we couldn't add all items
        return remaining <= 0;
    }

    #region String Overloads for Item Operations

    /// <summary>
    /// Add item by string ID - finds the ItemData and adds it
    /// </summary>
    public bool AddItem(string itemID, int quantity)
    {
        ItemData data = GetItemData(itemID);
        if (data == null)
        {
            Debug.LogError($"[InventoryManager] Cannot add item - ItemData not found for ID: {itemID}");
            return false;
        }

        return AddItem(data, quantity);
    }

    /// <summary>
    /// Remove item by string ID
    /// </summary>
    public bool RemoveItem(string itemID, int quantity)
    {
        if (quantity <= 0)
            return false;

        // First check if we have enough
        if (GetItemCount(itemID) < quantity)
        {
            Debug.LogWarning($"[InventoryManager] Cannot remove {quantity} of {itemID} - only have {GetItemCount(itemID)}");
            return false;
        }

        int remaining = quantity;

        // Remove from slots (iterate backwards to safely remove)
        for (int i = inventorySlots.Length - 1; i >= 0; i--)
        {
            var slot = inventorySlots[i];
            if (slot != null && slot.Stack != null &&
                slot.Stack.Item != null &&
                slot.Stack.Item.itemID == itemID)
            {
                if (slot.Stack.Quantity <= remaining)
                {
                    remaining -= slot.Stack.Quantity;
                    slot.Clear();
                }
                else
                {
                    slot.Stack.Quantity -= remaining;
                    remaining = 0;
                }

                if (remaining <= 0)
                {
                    InventoryEvents.ItemRemoved(GetItemData(itemID), quantity);
                    return true;
                }
            }
        }

        return remaining <= 0;
    }

    #endregion

    public bool AddItemToSlot(ItemData itemData, int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return false;

        var slot = inventorySlots[slotIndex];
        int added = slot.TryAddItem(itemData, quantity);

        if (added > 0)
        {
            InventoryEvents.ItemAdded(itemData, added);
            return true;
        }

        return false;
    }
    #endregion

    #region Public Methods - Removing Items
    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || quantity <= 0) return false;

        int remaining = quantity;

        // Remove from inventory slots
        for (int i = inventorySlots.Length - 1; i >= 0; i--)
        {
            var slot = inventorySlots[i];
            if (!slot.IsEmpty && slot.Item == itemData)
            {
                int removed = slot.RemoveItem(remaining);
                remaining -= removed;

                if (remaining <= 0) break;
            }
        }

        int actuallyRemoved = quantity - remaining;
        if (actuallyRemoved > 0)
        {
            InventoryEvents.ItemRemoved(itemData, actuallyRemoved);
            return true;
        }

        return false;
    }

    public void RemoveItemAtSlot(int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return;

        var slot = inventorySlots[slotIndex];
        if (!slot.IsEmpty)
        {
            var item = slot.Item;
            int removed = slot.RemoveItem(quantity);

            if (removed > 0)
            {
                InventoryEvents.ItemRemoved(item, removed);
            }
        }
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return;

        inventorySlots[slotIndex].Clear();
    }
    #endregion

    #region Public Methods - Queries
    public bool HasItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null) return false;

        int total = 0;
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.Item == itemData)
            {
                total += slot.Quantity;
                if (total >= quantity) return true;
            }
        }

        return false;
    }

    public int GetItemCount(ItemData itemData)
    {
        if (itemData == null) return 0;

        int total = 0;
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.Item == itemData)
            {
                total += slot.Quantity;
            }
        }

        return total;
    }

    public InventorySlot GetSlot(int index)
    {
        if (index >= 0 && index < inventorySlots.Length)
            return inventorySlots[index];
        return null;
    }

    public InventorySlot GetFirstEmptySlot()
    {
        return inventorySlots.FirstOrDefault(s => s.IsEmpty);
    }

    public List<InventorySlot> GetSlotsWithItem(ItemData itemData)
    {
        return inventorySlots.Where(s => !s.IsEmpty && s.Item == itemData).ToList();
    }
    #endregion

    #region Public Methods - Operations
    public void SwapSlots(int slotA, int slotB)
    {
        if (slotA < 0 || slotA >= inventorySlots.Length) return;
        if (slotB < 0 || slotB >= inventorySlots.Length) return;

        inventorySlots[slotA].SwapWith(inventorySlots[slotB]);
        InventoryEvents.SlotsSwapped(slotA, slotB);
    }

    public void SplitStack(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return;

        var slot = inventorySlots[slotIndex];
        if (slot.IsEmpty || slot.Quantity <= amount) return;

        var splitStack = slot.SplitStack(amount);
        if (splitStack != null)
        {
            var emptySlot = GetFirstEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.SetStack(splitStack);
            }
        }
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return;

        var slot = inventorySlots[slotIndex];
        if (!slot.IsEmpty && slot.Item.IsConsumable)
        {
            // Get player reference
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (slot.UseItem(player))
            {
                InventoryEvents.ItemUsed(slot.Item, 1);
            }
        }
    }

    public void DropItem(int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return;

        var slot = inventorySlots[slotIndex];
        if (!slot.IsEmpty)
        {
            var item = slot.Item;
            int removed = slot.RemoveItem(quantity);

            if (removed > 0)
            {
                // Spawn dropped item in world
                SpawnDroppedItem(item, removed);
                InventoryEvents.ItemRemoved(item, removed);
            }
        }
    }

    public void SortInventory()
    {
        // Collect all items
        List<ItemStack> allItems = new List<ItemStack>();
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty)
            {
                allItems.Add(slot.TakeStack());
            }
        }

        // Sort by type, then by name
        allItems = allItems.OrderBy(s => s.Item.Type.GetTabIndex())
                            .ThenBy(s => s.Item.SortOrder)
                            .ThenBy(s => s.Item.ItemName)
                            .ToList();

        // Redistribute items
        int slotIndex = 0;
        foreach (var stack in allItems)
        {
            if (slotIndex < inventorySlots.Length)
            {
                inventorySlots[slotIndex].SetStack(stack);
                slotIndex++;
            }
        }

        //InventoryEvents.OnInventoryChanged?.Invoke();
        //InventoryEvents.InventoryChanged();
    }
    #endregion

    #region Drag & Drop
    public void StartDragging(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return;

        var slot = inventorySlots[slotIndex];
        if (!slot.IsEmpty)
        {
            draggedStack = slot.TakeStack();
            draggedFromSlot = slotIndex;
            isDragging = true;

            InventoryEvents.DragStarted(slotIndex, draggedStack);
        }
    }

    public void EndDragging(int targetSlot)
    {
        if (!isDragging || draggedStack == null) return;

        if (targetSlot >= 0 && targetSlot < inventorySlots.Length)
        {
            var slot = inventorySlots[targetSlot];

            // Try to add to target slot
            if (slot.IsEmpty || slot.Item == draggedStack.Item)
            {
                int added = slot.TryAddItem(draggedStack.Item, draggedStack.Quantity);
                draggedStack.Quantity -= added;
            }

            // If anything remains, put it back
            if (draggedStack.Quantity > 0)
            {
                if (draggedFromSlot >= 0)
                {
                    inventorySlots[draggedFromSlot].SetStack(draggedStack);
                }
                else
                {
                    // Drop in world
                    SpawnDroppedItem(draggedStack.Item, draggedStack.Quantity);
                }
            }
        }
        else
        {
            // Dropped outside inventory - spawn in world
            SpawnDroppedItem(draggedStack.Item, draggedStack.Quantity);
        }

        isDragging = false;
        draggedStack = null;
        draggedFromSlot = -1;

        InventoryEvents.DragEnded(targetSlot, null);
    }

    public void CancelDragging()
    {
        if (!isDragging || draggedStack == null) return;

        // Return item to original slot
        if (draggedFromSlot >= 0 && draggedFromSlot < inventorySlots.Length)
        {
            inventorySlots[draggedFromSlot].SetStack(draggedStack);
        }

        isDragging = false;
        draggedStack = null;
        draggedFromSlot = -1;
    }
    #endregion

    #region Input Handling
    private void HandleHotbarInput()
    {
        // Number keys for hotbar
        for (int i = 0; i < hotbarSize && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectHotbarSlot(i);
            }
        }

        // Mouse wheel for hotbar
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int newSlot = selectedHotbarSlot;
            if (scroll > 0)
                newSlot = (newSlot - 1 + hotbarSize) % hotbarSize;
            else
                newSlot = (newSlot + 1) % hotbarSize;

            SelectHotbarSlot(newSlot);
        }

        // Use selected hotbar item
        if (Input.GetKeyDown(KeyCode.F))
        {
            UseHotbarItem(selectedHotbarSlot);
        }
    }

    private void HandleInventoryToggle()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (isInventoryOpen)
        {
            InventoryEvents.InventoryOpened();
            Time.timeScale = 0f; // Pause game
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            InventoryEvents.InventoryClosed();
            Time.timeScale = 1f; // Resume game
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SelectHotbarSlot(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
        {
            selectedHotbarSlot = slot;
            InventoryEvents.HotbarSlotSelected(slot);
        }
    }

    private void UseHotbarItem(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
        {
            var hotbarSlot = hotbarSlots[slot];
            if (!hotbarSlot.IsEmpty)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                hotbarSlot.UseItem(player);
                //InventoryEvents.HotbarSlotUsed(slot); // Just remove this line - the event is already fired in UseItem
            }
        }
    }
    #endregion

    #region Private Methods
    private void OnSlotChanged(InventorySlot slot)
    {
        RecalculateWeight();
        InventoryEvents.SlotChanged(slot.SlotIndex, slot.Stack);
    }

    private void OnItemAddedToSlot(InventorySlot slot)
    {
        RecalculateWeight();
    }

    private void OnItemRemovedFromSlot(InventorySlot slot)
    {
        RecalculateWeight();
    }

    private void RecalculateWeight()
    {
        float totalWeight = 0f;

        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty)
            {
                totalWeight += slot.TotalWeight;
            }
        }

        currentWeight = totalWeight;
        InventoryEvents.WeightChanged(currentWeight);
    }

    private void UpdateSpoilage()
    {
        float deltaTime = Time.deltaTime;

        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty)
            {
                slot.UpdateTimers(deltaTime);
            }
        }
    }

    private void SpawnDroppedItem(ItemData item, int quantity)
    {
        if (item == null || item.WorldPrefab == null) return;

        // Get drop position (in front of player)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 dropPos = player.transform.position + player.transform.forward * 2f;
            dropPos.y += 0.5f;

            GameObject droppedItem = Instantiate(item.WorldPrefab, dropPos, Quaternion.identity);

            // Add physics
            var rb = droppedItem.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = droppedItem.AddComponent<Rigidbody>();
            }

            // Add slight upward force
            rb.AddForce(Vector3.up * 3f + player.transform.forward * 2f, ForceMode.Impulse);

            // Store item data on the dropped object
            var pickup = droppedItem.GetComponent<ItemPickup>();
            if (pickup == null)
            {
                pickup = droppedItem.AddComponent<ItemPickup>();
            }
            pickup.SetItem(item, quantity);

            InventoryEvents.ItemDroppedInWorld(new ItemStack(item, quantity), dropPos);
        }
    }
    #endregion

    #region Save/Load
    public InventorySaveData GetSaveData()
    {
        var saveData = new InventorySaveData();

        // Save inventory slots
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (!inventorySlots[i].IsEmpty)
            {
                saveData.inventoryItems.Add(new ItemSaveData
                {
                    slotIndex = i,
                    itemID = inventorySlots[i].Item.ItemID,
                    quantity = inventorySlots[i].Quantity,
                    durability = inventorySlots[i].Stack.Durability
                });
            }
        }

        // Save hotbar
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (!hotbarSlots[i].IsEmpty)
            {
                saveData.hotbarItems.Add(new ItemSaveData
                {
                    slotIndex = i,
                    itemID = hotbarSlots[i].Item.ItemID,
                    quantity = hotbarSlots[i].Quantity,
                    durability = hotbarSlots[i].Stack.Durability
                });
            }
        }

        saveData.selectedHotbarSlot = selectedHotbarSlot;

        return saveData;
    }

    public void LoadSaveData(InventorySaveData saveData)
    {
        if (saveData == null) return;

        // Clear current inventory
        foreach (var slot in inventorySlots)
        {
            slot.Clear();
        }

        // Load items
        foreach (var itemData in saveData.inventoryItems)
        {
            // To (temporary fix):
            // var item = ItemDatabase.Instance.GetItem(itemData.itemID);
            // Comment it out for now - we'll implement ItemDatabase next
            //if (item != null && itemData.slotIndex < inventorySlots.Length)
            //{
            //    var stack = new ItemStack(item, itemData.quantity);
            //    stack.Durability = itemData.durability;
            //    inventorySlots[itemData.slotIndex].SetStack(stack);
            //}
        }

        selectedHotbarSlot = saveData.selectedHotbarSlot;
        RecalculateWeight();
    }
    #endregion
}

/// <summary>
/// Component for items that can be picked up in the world
/// </summary>
public class ItemPickup : MonoBehaviour
{
    private ItemData itemData;
    private int quantity = 1;

    public void SetItem(ItemData item, int qty)
    {
        itemData = item;
        quantity = qty;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (InventoryManager.Instance.AddItem(itemData, quantity))
            {
                //InventoryEvents.OnItemPickedUp?.Invoke(itemData);
                InventoryEvents.ItemAdded(itemData, quantity);
                //InventoryEvents.ItemPickedUp(itemData);
                Destroy(gameObject);
            }
        }
    }
}

/// <summary>
/// Save data structure for inventory
/// </summary>
[System.Serializable]
public class InventorySaveData
{
    public List<ItemSaveData> inventoryItems = new List<ItemSaveData>();
    public List<ItemSaveData> hotbarItems = new List<ItemSaveData>();
    public List<ItemSaveData> equipmentItems = new List<ItemSaveData>();
    public int selectedHotbarSlot = 0;
}

[System.Serializable]
public class ItemSaveData
{
    public int slotIndex;
    public string itemID;
    public int quantity;
    public float durability;
    public float spoilage;
}