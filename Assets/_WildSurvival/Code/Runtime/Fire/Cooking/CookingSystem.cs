using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles cooking mechanics with fire
/// </summary>
public class CookingSystem : MonoBehaviour
{
    [Header("Cooking Configuration")]
    [SerializeField] private float cookingCheckInterval = 1f;
    [SerializeField] private AnimationCurve cookingCurve;

    private List<CookingProcess> activeCookingProcesses = new List<CookingProcess>();
    private PlayerInventory playerInventory;

    [System.Serializable]
    public class CookingProcess
    {
        public FireInstance fire;
        public ItemDefinition rawItem;
        public ItemDefinition cookedItem;
        public float cookTime;
        public float currentTime;
        public bool isComplete;

        public float Progress => cookTime > 0 ? currentTime / cookTime : 0;
    }

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        UpdateCookingProcesses();
    }

    public void StartCooking(FireInstance fire, ItemDefinition foodItem)
    {
        if (fire == null || foodItem == null) return;

        if (!fire.CanCook())
        {
            NotificationSystem.Instance?.ShowNotification("Fire is not suitable for cooking",
                NotificationSystem.NotificationType.Warning);
            return;
        }

        // Get cooked version
        var cookedItem = GetCookedVersion(foodItem);
        if (cookedItem == null)
        {
            NotificationSystem.Instance?.ShowNotification($"Cannot cook {foodItem.displayName}",
                NotificationSystem.NotificationType.Warning);
            return;
        }

        // Start cooking process
        var process = new CookingProcess
        {
            fire = fire,
            rawItem = foodItem,
            cookedItem = cookedItem,
            cookTime = GetCookTime(foodItem),
            currentTime = 0,
            isComplete = false
        };

        activeCookingProcesses.Add(process);

        // Remove raw item from inventory
        playerInventory?.RemoveItem(foodItem, 1);

        NotificationSystem.Instance?.ShowNotification($"Started cooking {foodItem.displayName}",
            NotificationSystem.NotificationType.Info);
    }

    private void UpdateCookingProcesses()
    {
        for (int i = activeCookingProcesses.Count - 1; i >= 0; i--)
        {
            var process = activeCookingProcesses[i];

            if (process.fire == null || !process.fire.CanCook())
            {
                // Fire went out - food is ruined
                NotificationSystem.Instance?.ShowNotification($"{process.rawItem.displayName} burned!",
                    NotificationSystem.NotificationType.Error);
                activeCookingProcesses.RemoveAt(i);
                continue;
            }

            // Update cooking progress
            float efficiency = process.fire.GetCookingEfficiency();
            process.currentTime += Time.deltaTime * efficiency;

            if (process.currentTime >= process.cookTime && !process.isComplete)
            {
                CompleteCooking(process);
                process.isComplete = true;
                activeCookingProcesses.RemoveAt(i);
            }
        }
    }

    private void CompleteCooking(CookingProcess process)
    {
        // Add cooked item to inventory
        if (playerInventory != null)
        {
            playerInventory.AddItem(process.cookedItem, 1);
            NotificationSystem.Instance?.ShowNotification($"Finished cooking {process.cookedItem.displayName}!",
                NotificationSystem.NotificationType.Success);
        }
    }

    private ItemDefinition GetCookedVersion(ItemDefinition rawItem)
    {
        // Map raw items to cooked versions
        string cookedID = rawItem.itemID switch
        {
            "meat_raw" => "meat_cooked",
            "fish_raw" => "fish_cooked",
            "potato_raw" => "potato_baked",
            "corn_raw" => "corn_roasted",
            _ => null
        };

        if (cookedID == null) return null;

        // Get from database
        var database = Resources.Load<ItemDatabase>("ItemDatabase");
        return database?.GetItem(cookedID);
    }

    private float GetCookTime(ItemDefinition item)
    {
        // Define cook times for different items
        return item.itemID switch
        {
            "meat_raw" => 30f,
            "fish_raw" => 20f,
            "potato_raw" => 45f,
            "corn_raw" => 15f,
            _ => 25f
        };
    }
}