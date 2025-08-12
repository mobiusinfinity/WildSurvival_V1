using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Complete CraftingManager for Wild Survival with all required methods
/// Manages crafting recipes, processes, and workbench integration
/// </summary>
public class CraftingManager : MonoBehaviour
{
    // Singleton
    private static CraftingManager instance;
    public static CraftingManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CraftingManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CraftingManager");
                    instance = go.AddComponent<CraftingManager>();
                }
            }
            return instance;
        }
    }

    [Header("Recipe Management")]
    [SerializeField] private List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();
    [SerializeField] private List<string> knownRecipeIDs = new List<string>();
    [SerializeField] private bool startWithAllRecipes = false;

    [Header("Crafting State")]
    [SerializeField] private bool isCrafting = false;
    [SerializeField] private CraftingRecipe currentRecipe;
    [SerializeField] private float craftingProgress = 0f;
    [SerializeField] private float craftingTimeRemaining = 0f;
    [SerializeField] private int craftingQuantity = 1;

    [Header("Workbench")]
    [SerializeField] private CraftingRecipe.WorkbenchType currentWorkbench = CraftingRecipe.WorkbenchType.None;
    [SerializeField] private float workbenchSpeedBonus = 1f;
    [SerializeField] private float workbenchQualityBonus = 0f;

    [Header("Settings")]
    [SerializeField] private bool instantCrafting = false;
    [SerializeField] private float globalCraftingSpeedMultiplier = 1f;
    [SerializeField] private bool allowCraftingWhileMoving = false;

    // Events - All using CraftingRecipe type
    public UnityEvent<CraftingRecipe> OnRecipeDiscovered = new UnityEvent<CraftingRecipe>();
    public UnityEvent<CraftingRecipe> OnCraftingStarted = new UnityEvent<CraftingRecipe>();
    public UnityEvent<CraftingRecipe> OnCraftingCompleted = new UnityEvent<CraftingRecipe>();
    public UnityEvent<CraftingRecipe> OnCraftingCancelled = new UnityEvent<CraftingRecipe>();
    public UnityEvent<string> OnCraftingFailed = new UnityEvent<string>();
    public UnityEvent<float> OnCraftingProgress = new UnityEvent<float>();
    public UnityEvent<CraftingRecipe.WorkbenchType> OnWorkbenchChanged = new UnityEvent<CraftingRecipe.WorkbenchType>();

    // References
    private InventoryManager inventoryManager;
    private PlayerStats playerStats;
    private NotificationSystem notifications;

    // Cache
    private Dictionary<string, CraftingRecipe> recipeDict = new Dictionary<string, CraftingRecipe>();
    private Coroutine craftingCoroutine;

    // Properties
    public bool IsCrafting => isCrafting; // Property, not method!
    public float CraftingProgress => craftingProgress;
    public CraftingRecipe CurrentRecipe => currentRecipe;
    public CraftingRecipe.WorkbenchType CurrentWorkbench => currentWorkbench;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeRecipes();
    }

    private void Start()
    {
        // Get references
        inventoryManager = InventoryManager.Instance;
        playerStats = FindObjectOfType<PlayerStats>();
        notifications = NotificationSystem.Instance;

        // Initialize known recipes
        if (startWithAllRecipes)
        {
            DiscoverAllRecipes();
        }
    }

    private void InitializeRecipes()
    {
        // Build recipe dictionary
        recipeDict.Clear();
        foreach (var recipe in allRecipes)
        {
            if (recipe != null && !string.IsNullOrEmpty(recipe.recipeID))
            {
                recipeDict[recipe.recipeID] = recipe;
            }
        }
    }

    // ========== RECIPE MANAGEMENT ==========

    public List<CraftingRecipe> GetAllRecipes()
    {
        return new List<CraftingRecipe>(allRecipes);
    }

    public List<CraftingRecipe> GetAvailableRecipes()
    {
        List<CraftingRecipe> available = new List<CraftingRecipe>();
        foreach (var recipe in allRecipes)
        {
            if (IsRecipeKnown(recipe.recipeID) &&
                (recipe.requiredWorkbench == currentWorkbench ||
                 recipe.requiredWorkbench == CraftingRecipe.WorkbenchType.None))
            {
                available.Add(recipe);
            }
        }
        return available;
    }

    public CraftingRecipe GetRecipe(string recipeID)
    {
        if (recipeDict.ContainsKey(recipeID))
            return recipeDict[recipeID];
        return null;
    }

    public bool IsRecipeKnown(string recipeID)
    {
        return knownRecipeIDs.Contains(recipeID) || startWithAllRecipes;
    }

    public void DiscoverRecipe(string recipeID)
    {
        if (!IsRecipeKnown(recipeID) && recipeDict.ContainsKey(recipeID))
        {
            knownRecipeIDs.Add(recipeID);
            OnRecipeDiscovered?.Invoke(recipeDict[recipeID]);
            ShowNotification($"New Recipe: {recipeDict[recipeID].displayName}!");
        }
    }

    public void DiscoverAllRecipes()
    {
        foreach (var recipe in allRecipes)
        {
            if (!IsRecipeKnown(recipe.recipeID))
            {
                knownRecipeIDs.Add(recipe.recipeID);
            }
        }
    }

    public void DiscoverRecipesNearItem(string itemID)
    {
        // Discover recipes that use this item as an ingredient
        foreach (var recipe in allRecipes)
        {
            if (!IsRecipeKnown(recipe.recipeID))
            {
                foreach (var ingredient in recipe.ingredients)
                {
                    if (ingredient.itemID == itemID)
                    {
                        DiscoverRecipe(recipe.recipeID);
                        break;
                    }
                }
            }
        }
    }

    // ========== CRAFTING VALIDATION ==========

    public bool CanCraftRecipe(string recipeID)
    {
        return CanCraftRecipe(GetRecipe(recipeID), 1);
    }

    public bool CanCraftRecipe(CraftingRecipe recipe)
    {
        return CanCraftRecipe(recipe, 1);
    }

    public bool CanCraftRecipe(CraftingRecipe recipe, int quantity)
    {


        if (recipe == null) return false;
        if (isCrafting) return false;
        if (!IsRecipeKnown(recipe.recipeID)) return false;



        // Check workbench requirement
        if (recipe.requiredWorkbench != CraftingRecipe.WorkbenchType.None &&
            recipe.requiredWorkbench != currentWorkbench)
        {
            return false;
        }

        // Check player level
        if (playerStats != null && playerStats.Level < recipe.requiredPlayerLevel)
        {
            return false;
        }

        // Check ingredients
        if (inventoryManager != null)
        {
            foreach (var ingredient in recipe.ingredients)
            {
                int required = ingredient.quantity * quantity;
                if (!inventoryManager.HasItem(ingredient.itemID, required))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // ========== WORKBENCH MANAGEMENT ==========

    public void SetWorkbench(CraftingRecipe.WorkbenchType type, float speedBonus = 1f, float qualityBonus = 0f)
    {
        currentWorkbench = type;
        workbenchSpeedBonus = speedBonus;
        workbenchQualityBonus = qualityBonus;
        OnWorkbenchChanged?.Invoke(type);
    }

    public void ClearWorkbench()
    {
        SetWorkbench(CraftingRecipe.WorkbenchType.None, 1f, 0f);
    }

    // ========== CRAFTING PROCESS ==========

    public void StartCrafting(CraftingRecipe recipe, int quantity = 1)
    {
        if (!CanCraftRecipe(recipe, quantity))
        {
            OnCraftingFailed?.Invoke("Cannot craft this recipe!");
            return;
        }

        // Stop any existing crafting
        if (craftingCoroutine != null)
        {
            StopCoroutine(craftingCoroutine);
        }

        // Set crafting state
        currentRecipe = recipe;
        craftingQuantity = quantity;
        isCrafting = true;
        craftingProgress = 0f;

        // Calculate crafting time
        float totalTime = recipe.craftingTime * quantity;
        totalTime *= globalCraftingSpeedMultiplier;
        totalTime /= workbenchSpeedBonus;
        craftingTimeRemaining = totalTime;

        // Consume ingredients
        ConsumeIngredients(recipe, quantity);

        // Start crafting coroutine
        if (instantCrafting || totalTime <= 0)
        {
            CompleteCrafting();
        }
        else
        {
            craftingCoroutine = StartCoroutine(CraftingProcess(totalTime));
        }

        OnCraftingStarted?.Invoke(recipe);
    }

    public bool StartCrafting(string recipeID, int quantity = 1)
    {
        var recipe = GetRecipe(recipeID);
        if (recipe != null)
        {
            StartCrafting(recipe, quantity);
            return true;
        }
        return false;
    }

    private IEnumerator CraftingProcess(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            craftingProgress = elapsed / duration;
            craftingTimeRemaining = duration - elapsed;

            OnCraftingProgress?.Invoke(craftingProgress);

            // Check if player can still craft (optional movement restriction)
            if (!allowCraftingWhileMoving && PlayerIsMoving())
            {
                CancelCrafting();
                yield break;
            }

            yield return null;
        }

        CompleteCrafting();
    }

    private void CompleteCrafting()
    {
        if (currentRecipe == null) return;

        // Give output items
        GiveOutputItems(currentRecipe, craftingQuantity);

        // Apply quality bonus if any
        if (workbenchQualityBonus > 0)
        {
            // Implement quality system if needed
        }

        OnCraftingCompleted?.Invoke(currentRecipe);
        ShowNotification($"Crafted {currentRecipe.displayName} x{craftingQuantity}!");

        // Reset state
        ResetCraftingState();
    }

    public void CancelCrafting()
    {
        if (!isCrafting) return;

        // Return partial ingredients (50% refund)
        if (currentRecipe != null && inventoryManager != null)
        {
            foreach (var ingredient in currentRecipe.ingredients)
            {
                int refund = Mathf.FloorToInt(ingredient.quantity * craftingQuantity * 0.5f);
                if (refund > 0)
                {
                    inventoryManager.AddItem(ingredient.itemID, refund);
                }
            }
        }

        if (craftingCoroutine != null)
        {
            StopCoroutine(craftingCoroutine);
        }

        OnCraftingCancelled?.Invoke(currentRecipe);
        ShowNotification("Crafting cancelled!");

        ResetCraftingState();
    }

    private void ResetCraftingState()
    {
        isCrafting = false;
        currentRecipe = null;
        craftingProgress = 0f;
        craftingTimeRemaining = 0f;
        craftingQuantity = 1;
        craftingCoroutine = null;
    }

    // ========== INGREDIENT & OUTPUT HANDLING ==========

    private void ConsumeIngredients(CraftingRecipe recipe, int quantity)
    {
        if (inventoryManager == null) return;

        foreach (var ingredient in recipe.ingredients)
        {
            int amount = ingredient.quantity * quantity;
            inventoryManager.RemoveItem(ingredient.itemID, amount);
        }
    }

    private void GiveOutputItems(CraftingRecipe recipe, int quantity)
    {
        if (inventoryManager == null) return;

        // Main output
        if (recipe.output != null)
        {
            int amount = UnityEngine.Random.Range(
                recipe.output.quantityMin * quantity,
                recipe.output.quantityMax * quantity + 1
            );
            inventoryManager.AddItem(recipe.output.itemID, amount);
        }

        // Additional outputs
        foreach (var output in recipe.additionalOutputs)
        {
            if (UnityEngine.Random.value <= output.chance)
            {
                int amount = UnityEngine.Random.Range(
                    output.quantityMin * quantity,
                    output.quantityMax * quantity + 1
                );
                inventoryManager.AddItem(output.itemID, amount);
            }
        }
    }

    // ========== HELPER METHODS ==========

    private bool PlayerIsMoving()
    {
        // Check if player is moving (implement based on your movement system)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                return rb.linearVelocity.magnitude > 0.1f;
            }
        }
        return false;
    }

    private void ShowNotification(string message)
    {
        if (notifications != null)
        {
            notifications.ShowNotification(message, NotificationType.Info);
        }
        else
        {
            Debug.Log($"[CraftingManager] {message}");
        }
    }

    // ========== SAVE/LOAD ==========

    public CraftingSaveData GetSaveData()
    {
        return new CraftingSaveData
        {
            knownRecipeIDs = new List<string>(knownRecipeIDs),
            currentWorkbench = currentWorkbench
        };
    }

    public void LoadSaveData(CraftingSaveData data)
    {
        if (data != null)
        {
            knownRecipeIDs = new List<string>(data.knownRecipeIDs);
            currentWorkbench = data.currentWorkbench;
        }
    }

    [System.Serializable]
    public class CraftingSaveData
    {
        public List<string> knownRecipeIDs;
        public CraftingRecipe.WorkbenchType currentWorkbench;
    }
}