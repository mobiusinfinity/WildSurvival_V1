using UnityEngine;
using System;

/// <summary>
/// Central event system for all inventory operations.
/// Allows UI and other systems to react to inventory changes.
/// </summary>
public static class InventoryEvents
{
    // Item Events
    public static event Action<ItemData, int> OnItemAdded;
    public static event Action<ItemData, int> OnItemRemoved;
    public static event Action<ItemData, int> OnItemUsed;
    public static event Action<ItemData, int> OnItemEquipped;
    public static event Action<ItemData, int> OnItemUnequipped;
    public static event Action<ItemData> OnItemDropped;
    public static event Action<ItemData> OnItemPickedUp;
    public static event Action<ItemData> OnItemCrafted;
    public static event Action<ItemData> OnItemBroken;
    public static event Action<ItemData> OnItemSpoiled;

    // Slot Events
    public static event Action<int, ItemStack> OnSlotChanged;
    public static event Action<int, int> OnSlotsSwapped;
    public static event Action<int> OnSlotCleared;
    public static event Action<int, ItemStack> OnSlotSplit;
    public static event Action<int, int> OnSlotsMerged;

    // Inventory Events
    public static event Action OnInventoryOpened;
    public static event Action OnInventoryClosed;
    public static event Action OnInventoryFull;
    public static event Action OnInventoryChanged;
    public static event Action<float> OnWeightChanged;
    public static event Action<int> OnSlotsUnlocked;

    // Container Events
    public static event Action<GameObject> OnContainerOpened;
    public static event Action<GameObject> OnContainerClosed;
    public static event Action<GameObject, ItemData, int> OnContainerLooted;

    // Crafting Events
    public static event Action<Recipe> OnCraftingStarted;
    public static event Action<Recipe> OnCraftingCompleted;
    public static event Action<Recipe> OnCraftingFailed;
    public static event Action<Recipe> OnRecipeUnlocked;

    // Trade Events
    public static event Action<ItemData, int, int> OnItemSold;
    public static event Action<ItemData, int, int> OnItemBought;
    public static event Action<TradeOffer> OnTradeOffered;
    public static event Action<TradeOffer> OnTradeAccepted;
    public static event Action<TradeOffer> OnTradeDeclined;

    // Hotbar Events
    public static event Action<int, ItemData> OnHotbarSlotAssigned;
    public static event Action<int> OnHotbarSlotCleared;
    public static event Action<int> OnHotbarSlotSelected;
    public static event Action<int> OnHotbarSlotUsed;

    // UI Events
    public static event Action<ItemData> OnItemHovered;
    public static event Action<ItemData> OnItemSelected;
    public static event Action<ItemData> OnItemInspected;
    public static event Action<ItemData, Vector3> OnItemTooltipRequested;
    public static event Action OnTooltipHidden;

    // Drag & Drop Events
    public static event Action<int, ItemStack> OnDragStarted;
    public static event Action<int, ItemStack> OnDragEnded;
    public static event Action<int> OnDragCancelled;
    public static event Action<ItemStack, Vector3> OnItemDroppedInWorld;

    // Special Events
    public static event Action<Quest, ItemData> OnQuestItemReceived;
    public static event Action<Achievement> OnInventoryAchievement;
    public static event Action<ItemData, int> OnStackLimitReached;
    public static event Action<ItemData> OnUniqueItemObtained;

    // Event Triggers
    public static void ItemAdded(ItemData item, int quantity)
    {
        OnItemAdded?.Invoke(item, quantity);
        OnInventoryChanged?.Invoke();

        if (IsDebugMode)
            Debug.Log($"[Inventory] Added {quantity}x {item.ItemName}");
    }

    public static void ItemRemoved(ItemData item, int quantity)
    {
        OnItemRemoved?.Invoke(item, quantity);
        OnInventoryChanged?.Invoke();

        if (IsDebugMode)
            Debug.Log($"[Inventory] Removed {quantity}x {item.ItemName}");
    }

    public static void ItemUsed(ItemData item, int quantity = 1)
    {
        OnItemUsed?.Invoke(item, quantity);

        if (IsDebugMode)
            Debug.Log($"[Inventory] Used {quantity}x {item.ItemName}");
    }

    public static void ItemEquipped(ItemData item, int slotIndex = -1)
    {
        OnItemEquipped?.Invoke(item, slotIndex);

        if (IsDebugMode)
            Debug.Log($"[Inventory] Equipped {item.ItemName}");
    }

    public static void ItemUnequipped(ItemData item, int slotIndex = -1)
    {
        OnItemUnequipped?.Invoke(item, slotIndex);

        if (IsDebugMode)
            Debug.Log($"[Inventory] Unequipped {item.ItemName}");
    }

    public static void SlotChanged(int slotIndex, ItemStack newStack)
    {
        OnSlotChanged?.Invoke(slotIndex, newStack);
        OnInventoryChanged?.Invoke();
    }

    public static void SlotsSwapped(int slotA, int slotB)
    {
        OnSlotsSwapped?.Invoke(slotA, slotB);
        OnInventoryChanged?.Invoke();
    }

    public static void InventoryOpened()
    {
        OnInventoryOpened?.Invoke();

        if (IsDebugMode)
            Debug.Log("[Inventory] Opened");
    }

    public static void InventoryClosed()
    {
        OnInventoryClosed?.Invoke();

        if (IsDebugMode)
            Debug.Log("[Inventory] Closed");
    }

    public static void InventoryFull()
    {
        OnInventoryFull?.Invoke();

        if (IsDebugMode)
            Debug.Log("[Inventory] Full - Cannot add more items");
    }

    public static void WeightChanged(float newWeight)
    {
        OnWeightChanged?.Invoke(newWeight);
    }

    public static void ContainerOpened(GameObject container)
    {
        OnContainerOpened?.Invoke(container);

        if (IsDebugMode)
            Debug.Log($"[Inventory] Container opened: {container.name}");
    }

    public static void ContainerClosed(GameObject container)
    {
        OnContainerClosed?.Invoke(container);

        if (IsDebugMode)
            Debug.Log($"[Inventory] Container closed: {container.name}");
    }

    public static void CraftingCompleted(Recipe recipe)
    {
        OnCraftingCompleted?.Invoke(recipe);
        OnInventoryChanged?.Invoke();

        if (IsDebugMode && recipe != null)
            Debug.Log($"[Inventory] Crafted: {recipe.OutputItem.ItemName}");
    }

    public static void HotbarSlotSelected(int slot)
    {
        OnHotbarSlotSelected?.Invoke(slot);

        if (IsDebugMode)
            Debug.Log($"[Inventory] Hotbar slot {slot} selected");
    }

    public static void ItemTooltipRequested(ItemData item, Vector3 position)
    {
        OnItemTooltipRequested?.Invoke(item, position);
    }

    public static void TooltipHidden()
    {
        OnTooltipHidden?.Invoke();
    }

    public static void DragStarted(int slotIndex, ItemStack stack)
    {
        OnDragStarted?.Invoke(slotIndex, stack);

        if (IsDebugMode)
            Debug.Log($"[Inventory] Started dragging from slot {slotIndex}");
    }

    public static void DragEnded(int targetSlot, ItemStack stack)
    {
        OnDragEnded?.Invoke(targetSlot, stack);

        if (IsDebugMode)
            Debug.Log($"[Inventory] Dropped on slot {targetSlot}");
    }

    public static void ItemDroppedInWorld(ItemStack stack, Vector3 position)
    {
        OnItemDroppedInWorld?.Invoke(stack, position);
        OnInventoryChanged?.Invoke();

        if (IsDebugMode)
            Debug.Log($"[Inventory] Dropped {stack.Item.ItemName} in world at {position}");
    }

    // Clear all event subscriptions (useful for scene changes)
    public static void ClearAllSubscriptions()
    {
        // Item Events
        OnItemAdded = null;
        OnItemRemoved = null;
        OnItemUsed = null;
        OnItemEquipped = null;
        OnItemUnequipped = null;
        OnItemDropped = null;
        OnItemPickedUp = null;
        OnItemCrafted = null;
        OnItemBroken = null;
        OnItemSpoiled = null;

        // Slot Events
        OnSlotChanged = null;
        OnSlotsSwapped = null;
        OnSlotCleared = null;
        OnSlotSplit = null;
        OnSlotsMerged = null;

        // Inventory Events
        OnInventoryOpened = null;
        OnInventoryClosed = null;
        OnInventoryFull = null;
        OnInventoryChanged = null;
        OnWeightChanged = null;
        OnSlotsUnlocked = null;

        // Container Events
        OnContainerOpened = null;
        OnContainerClosed = null;
        OnContainerLooted = null;

        // Crafting Events
        OnCraftingStarted = null;
        OnCraftingCompleted = null;
        OnCraftingFailed = null;
        OnRecipeUnlocked = null;

        // Trade Events
        OnItemSold = null;
        OnItemBought = null;
        OnTradeOffered = null;
        OnTradeAccepted = null;
        OnTradeDeclined = null;

        // Hotbar Events
        OnHotbarSlotAssigned = null;
        OnHotbarSlotCleared = null;
        OnHotbarSlotSelected = null;
        OnHotbarSlotUsed = null;

        // UI Events
        OnItemHovered = null;
        OnItemSelected = null;
        OnItemInspected = null;
        OnItemTooltipRequested = null;
        OnTooltipHidden = null;

        // Drag & Drop Events
        OnDragStarted = null;
        OnDragEnded = null;
        OnDragCancelled = null;
        OnItemDroppedInWorld = null;

        // Special Events
        OnQuestItemReceived = null;
        OnInventoryAchievement = null;
        OnStackLimitReached = null;
        OnUniqueItemObtained = null;

        if (IsDebugMode)
            Debug.Log("[Inventory] All event subscriptions cleared");
    }

    // Debug mode for logging
    public static bool IsDebugMode { get; set; } = false;
}

/// <summary>
/// Data structure for crafting recipes (placeholder for now)
/// </summary>
[System.Serializable]
public class Recipe
{
    public ItemData OutputItem;
    public int OutputQuantity;
    // Will be expanded in Phase 5 (Crafting System)
}

/// <summary>
/// Data structure for trade offers (placeholder for now)
/// </summary>
[System.Serializable]
public class TradeOffer
{
    public ItemData OfferedItem;
    public int OfferedQuantity;
    public ItemData RequestedItem;
    public int RequestedQuantity;
    // Will be expanded later
}

/// <summary>
/// Data structure for quests (placeholder for now)
/// </summary>
[System.Serializable]
public class Quest
{
    public string QuestName;
    public string QuestID;
    // Will be expanded later
}

/// <summary>
/// Data structure for achievements (placeholder for now)
/// </summary>
[System.Serializable]
public class Achievement
{
    public string AchievementName;
    public string AchievementID;
    // Will be expanded later
}