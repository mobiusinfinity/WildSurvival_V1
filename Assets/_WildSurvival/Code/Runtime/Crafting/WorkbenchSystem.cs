using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Workbench system for Wild Survival
/// Handles crafting stations, upgrades, and special crafting bonuses
/// </summary>
public class WorkbenchSystem : MonoBehaviour, IInteractable
{
    [Header("Workbench Configuration")]
    [SerializeField] private string workbenchID = "basic_workbench";
    [SerializeField] private string displayName = "Crafting Bench";
    [SerializeField] private CraftingRecipe.WorkbenchType workbenchType = CraftingRecipe.WorkbenchType.WorkBench;
    [SerializeField] private int tier = 1;

    [Header("Crafting Bonuses")]
    [SerializeField] private float craftingSpeedMultiplier = 1.0f;
    [SerializeField] private float qualityBonus = 0f;
    [SerializeField] private float successRateBonus = 0f;
    [SerializeField] private bool allowBulkCrafting = true;
    [SerializeField] private int maxQueueSize = 5;

    [Header("Fuel System")]
    [SerializeField] private bool requiresFuel = false;
    [SerializeField] private string fuelItemID = "item_wood";
    [SerializeField] private float fuelPerCraft = 1f;
    [SerializeField] private float currentFuel = 0f;
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float fuelBurnRate = 0.1f; // Per second while crafting

    [Header("Upgrade System")]
    [SerializeField] private bool isUpgradeable = true;
    [SerializeField] private List<UpgradeRequirement> upgradeRequirements = new List<UpgradeRequirement>();
    [SerializeField] private WorkbenchSystem upgradedVersionPrefab;

    [Header("Special Features")]
    [SerializeField] private List<string> exclusiveRecipeIDs = new List<string>();
    [SerializeField] private List<string> bonusRecipeIDs = new List<string>();
    [SerializeField] private bool autoDiscoverRecipes = false;
    [SerializeField] private float discoveryRadius = 5f;

    [Header("Visual & Audio")]
    [SerializeField] private GameObject craftingEffect;
    [SerializeField] private Transform craftingEffectPoint;
    [SerializeField] private Light workLight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip craftingSound;
    [SerializeField] private AudioClip completeSound;
    [SerializeField] private AudioClip upgradeSound;

    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private TMPro.TextMeshProUGUI promptText;
    [SerializeField] private TMPro.TextMeshProUGUI fuelText;
    [SerializeField] private UnityEngine.UI.Slider fuelBar;

    // Events
    public UnityEvent<WorkbenchSystem> OnWorkbenchOpened = new UnityEvent<WorkbenchSystem>();
    public UnityEvent<WorkbenchSystem> OnWorkbenchClosed = new UnityEvent<WorkbenchSystem>();
    public UnityEvent<WorkbenchSystem> OnWorkbenchUpgraded = new UnityEvent<WorkbenchSystem>();
    public UnityEvent<float> OnFuelChanged = new UnityEvent<float>();

    // State
    private bool isInUse = false;
    private bool isPlayerNearby = false;
    private GameObject currentUser;
    private CraftingManager craftingManager;
    private InventoryManager inventoryManager;
    private CraftingUI craftingUI;
    private NotificationSystem notifications;

    // Cache
    private Collider workbenchCollider;
    private List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
    private Coroutine fuelCoroutine;
    private ParticleSystem craftingParticles;

    [System.Serializable]
    public class UpgradeRequirement
    {
        public string itemID;
        public int quantity;
        public string displayName;
        public Sprite icon;
    }

    private void Awake()
    {
        workbenchCollider = GetComponent<Collider>();
        if (workbenchCollider == null)
        {
            workbenchCollider = gameObject.AddComponent<BoxCollider>();
            ((BoxCollider)workbenchCollider).isTrigger = true;
            ((BoxCollider)workbenchCollider).size = new Vector3(2, 2, 2);
        }

        if (craftingEffect != null && craftingEffectPoint == null)
        {
            craftingEffectPoint = transform;
        }

        if (craftingEffect != null)
        {
            craftingParticles = craftingEffect.GetComponent<ParticleSystem>();
        }

        SetupUI();
    }

    private void Start()
    {
        // Get references
        craftingManager = CraftingManager.Instance;
        inventoryManager = InventoryManager.Instance;
        craftingUI = FindObjectOfType<CraftingUI>();
        notifications = NotificationSystem.Instance;

        // Subscribe to crafting events
        if (craftingManager != null)
        {
            craftingManager.OnCraftingStarted.AddListener(OnCraftingStarted);
            craftingManager.OnCraftingCompleted.AddListener(OnCraftingCompleted);
            craftingManager.OnCraftingCancelled.AddListener(OnCraftingCancelled);
        }

        // Initial setup
        UpdateFuelDisplay();
        LoadAvailableRecipes();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void OnDestroy()
    {
        if (craftingManager != null)
        {
            craftingManager.OnCraftingStarted.RemoveListener(OnCraftingStarted);
            craftingManager.OnCraftingCompleted.RemoveListener(OnCraftingCompleted);
            craftingManager.OnCraftingCancelled.RemoveListener(OnCraftingCancelled);
        }
    }

    private void SetupUI()
    {
        if (worldCanvas == null)
        {
            GameObject canvasGO = new GameObject("WorkbenchCanvas");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = Vector3.up * 2;

            worldCanvas = canvasGO.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.transform.localScale = Vector3.one * 0.01f;
        }

        if (promptText == null && interactionPrompt != null)
        {
            promptText = interactionPrompt.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            ShowInteractionPrompt(true);

            // Auto-discover recipes if enabled
            if (autoDiscoverRecipes && craftingManager != null)
            {
                DiscoverNearbyRecipes();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            ShowInteractionPrompt(false);

            if (isInUse && currentUser == other.gameObject)
            {
                CloseWorkbench();
            }
        }
    }

    private void Update()
    {
        // Handle interaction input
        if (isPlayerNearby && !isInUse && Input.GetKeyDown(KeyCode.E))
        {
            OpenWorkbench();
        }
        else if (isInUse && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
        {
            CloseWorkbench();
        }

        // Update fuel consumption if crafting
        if (requiresFuel && isInUse && craftingManager != null && craftingManager.IsCrafting)
        {
            ConsumeFuel(fuelBurnRate * Time.deltaTime);
        }

        // Update visual effects
        UpdateVisualEffects();
    }

    public void Interact(GameObject interactor)
    {
        if (!isInUse)
        {
            OpenWorkbench(interactor);
        }
        else if (currentUser == interactor)
        {
            CloseWorkbench();
        }
    }

    public void OpenWorkbench(GameObject user = null)
    {
        if (isInUse) return;

        isInUse = true;
        currentUser = user ?? GameObject.FindGameObjectWithTag("Player");

        // Set workbench type in crafting manager
        if (craftingManager != null)
        {
            craftingManager.SetWorkbench(workbenchType, craftingSpeedMultiplier, qualityBonus);
        }

        // Open crafting UI
        if (craftingUI != null)
        {
            craftingUI.Open();
        }

        // Start effects
        StartCraftingEffects();

        // Fire event
        OnWorkbenchOpened?.Invoke(this);

        ShowNotification($"Opened {displayName}", NotificationSystem.NotificationType.Info);
    }

    public void CloseWorkbench()
    {
        if (!isInUse) return;

        isInUse = false;

        // Clear workbench in crafting manager
        if (craftingManager != null)
        {
            craftingManager.SetWorkbench(CraftingRecipe.WorkbenchType.None, 1f, 0f);
        }

        // Close crafting UI
        if (craftingUI != null)
        {
            craftingUI.Close();
        }

        // Stop effects
        StopCraftingEffects();

        // Fire event
        OnWorkbenchClosed?.Invoke(this);

        currentUser = null;
    }

    public bool CanCraft(CraftingRecipe recipe)
    {
        // Check workbench type
        if (recipe.requiredWorkbench != workbenchType &&
            recipe.requiredWorkbench != CraftingRecipe.WorkbenchType.None)
        {
            return false;
        }

        // Check fuel
        if (requiresFuel && currentFuel < fuelPerCraft)
        {
            ShowNotification("Not enough fuel!", NotificationSystem.NotificationType.Error);
            return false;
        }

        // Check if recipe is exclusive to other workbenches
        // Implementation depends on your recipe system

        return true;
    }

    public void AddFuel(int amount)
    {
        if (!requiresFuel) return;

        // Check if player has fuel
        if (inventoryManager != null)
        {
            int available = inventoryManager.GetItemCount(fuelItemID);
            int toAdd = Mathf.Min(amount, available);
            toAdd = Mathf.Min(toAdd, Mathf.FloorToInt(maxFuel - currentFuel));

            if (toAdd > 0)
            {
                inventoryManager.RemoveItem(fuelItemID, toAdd);
                currentFuel = Mathf.Min(currentFuel + toAdd, maxFuel);
                UpdateFuelDisplay();
                OnFuelChanged?.Invoke(currentFuel);

                ShowNotification($"Added {toAdd} fuel", NotificationSystem.NotificationType.Success);
            }
        }
    }

    private void ConsumeFuel(float amount)
    {
        if (!requiresFuel) return;

        currentFuel = Mathf.Max(0, currentFuel - amount);
        UpdateFuelDisplay();

        // Cancel crafting if out of fuel
        if (currentFuel <= 0 && craftingManager != null && craftingManager.IsCrafting)
        {
            craftingManager.CancelCrafting();
            ShowNotification("Out of fuel! Crafting cancelled.", NotificationSystem.NotificationType.Error);
        }

        OnFuelChanged?.Invoke(currentFuel);
    }

    public void UpgradeWorkbench()
    {
        if (!isUpgradeable || upgradedVersionPrefab == null) return;

        // Check requirements
        if (!HasUpgradeRequirements())
        {
            ShowNotification("Missing upgrade materials!", NotificationSystem.NotificationType.Error);
            return;
        }

        // Consume materials
        ConsumeUpgradeMaterials();

        // Spawn upgraded version
        GameObject upgraded = Instantiate(upgradedVersionPrefab.gameObject, transform.position, transform.rotation);
        WorkbenchSystem newWorkbench = upgraded.GetComponent<WorkbenchSystem>();

        // Transfer fuel if applicable
        if (newWorkbench.requiresFuel && requiresFuel)
        {
            newWorkbench.currentFuel = currentFuel;
        }

        // Play effects
        if (upgradeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(upgradeSound);
        }

        // Fire event
        OnWorkbenchUpgraded?.Invoke(newWorkbench);

        ShowNotification($"Upgraded to {newWorkbench.displayName}!", NotificationSystem.NotificationType.Success);

        // Destroy old workbench
        Destroy(gameObject, 0.5f);
    }

    private bool HasUpgradeRequirements()
    {
        if (inventoryManager == null) return false;

        foreach (var req in upgradeRequirements)
        {
            if (inventoryManager.GetItemCount(req.itemID) < req.quantity)
            {
                return false;
            }
        }

        return true;
    }

    private void ConsumeUpgradeMaterials()
    {
        if (inventoryManager == null) return;

        foreach (var req in upgradeRequirements)
        {
            inventoryManager.RemoveItem(req.itemID, req.quantity);
        }
    }

    private void LoadAvailableRecipes()
    {
        if (craftingManager == null) return;

        availableRecipes.Clear();
        var allRecipes = craftingManager.GetAllRecipes();

        foreach (var recipe in allRecipes)
        {
            // Check if recipe can be crafted at this workbench
            if (recipe.requiredWorkbench == workbenchType ||
                recipe.requiredWorkbench == CraftingRecipe.WorkbenchType.None)
            {
                availableRecipes.Add(recipe);
            }
        }

        // Add exclusive recipes
        foreach (string recipeID in exclusiveRecipeIDs)
        {
            var recipe = craftingManager.GetRecipe(recipeID);
            if (recipe != null && !availableRecipes.Contains(recipe))
            {
                availableRecipes.Add(recipe);
            }
        }
    }

    private void DiscoverNearbyRecipes()
    {
        // Find items in radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, discoveryRadius);

        foreach (var col in colliders)
        {
            // Check if it's an item
            var item = col.GetComponent<IItem>();
            if (item != null)
            {
                craftingManager.DiscoverRecipesNearItem(item.GetItemID());
            }
        }
    }

    private void ShowInteractionPrompt(bool show)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show);

            if (show && promptText != null)
            {
                string fuelInfo = requiresFuel ? $" (Fuel: {currentFuel:F0}/{maxFuel:F0})" : "";
                promptText.text = $"[E] Use {displayName}{fuelInfo}";
            }
        }
    }

    private void UpdateFuelDisplay()
    {
        if (!requiresFuel) return;

        if (fuelBar != null)
        {
            fuelBar.value = currentFuel / maxFuel;
        }

        if (fuelText != null)
        {
            fuelText.text = $"Fuel: {currentFuel:F0}/{maxFuel:F0}";
        }
    }

    private void StartCraftingEffects()
    {
        // Visual effects
        if (craftingEffect != null)
        {
            craftingEffect.SetActive(true);
            if (craftingParticles != null)
            {
                craftingParticles.Play();
            }
        }

        // Light
        if (workLight != null)
        {
            workLight.enabled = true;
            StartCoroutine(AnimateLight(true));
        }

        // Audio
        if (audioSource != null && craftingSound != null)
        {
            audioSource.clip = craftingSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void StopCraftingEffects()
    {
        // Visual effects
        if (craftingEffect != null)
        {
            if (craftingParticles != null)
            {
                craftingParticles.Stop();
            }
            StartCoroutine(DelayedDisable(craftingEffect, 2f));
        }

        // Light
        if (workLight != null)
        {
            StartCoroutine(AnimateLight(false));
        }

        // Audio
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void UpdateVisualEffects()
    {
        // Update based on crafting state
        if (isInUse && craftingManager != null && craftingManager.IsCrafting)
        {
            // Pulsing light effect
            if (workLight != null)
            {
                float pulse = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
                workLight.intensity = 1f + pulse;
            }
        }
    }

    private IEnumerator AnimateLight(bool turnOn)
    {
        if (workLight == null) yield break;

        float startIntensity = workLight.intensity;
        float targetIntensity = turnOn ? 2f : 0f;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            workLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            yield return null;
        }

        workLight.intensity = targetIntensity;
        if (!turnOn)
        {
            workLight.enabled = false;
        }
    }

    private IEnumerator DelayedDisable(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }

    private void OnCraftingStarted(CraftingRecipe recipe)
    {
        if (!isInUse) return;

        // Consume fuel for this craft
        if (requiresFuel)
        {
            ConsumeFuel(fuelPerCraft);
        }

        // Enhanced effects for special recipes
        if (bonusRecipeIDs.Contains(recipe.recipeID))
        {
            // Add bonus effects
            if (craftingParticles != null)
            {
                var main = craftingParticles.main;
                main.startColor = Color.yellow;
            }
        }
    }

    private void OnCraftingCompleted(CraftingRecipe recipe)
    {
        if (!isInUse) return;

        // Play completion sound
        if (audioSource != null && completeSound != null)
        {
            audioSource.PlayOneShot(completeSound);
        }

        // Visual feedback
        if (craftingParticles != null)
        {
            craftingParticles.Emit(50);
        }
    }

    private void OnCraftingCancelled(CraftingRecipe recipe)
    {
        if (!isInUse) return;

        // Return partial fuel
        if (requiresFuel)
        {
            float refund = fuelPerCraft * 0.5f; // 50% refund
            currentFuel = Mathf.Min(currentFuel + refund, maxFuel);
            UpdateFuelDisplay();
        }
    }

    private void ShowNotification(string message, NotificationSystem.NotificationType type)
    {
        if (notifications != null)
        {
            notifications.ShowNotification(message, type);
        }
        else
        {
            Debug.Log($"[WorkbenchSystem] {type}: {message}");
        }
    }

    // Interface implementation
    public string GetInteractionPrompt()
    {
        return $"Use {displayName}";
    }

    public bool CanInteract(GameObject interactor)
    {
        return !isInUse || currentUser == interactor;
    }

    // Getters
    public CraftingRecipe.WorkbenchType GetWorkbenchType() => workbenchType;
    public float GetSpeedMultiplier() => craftingSpeedMultiplier;
    public float GetQualityBonus() => qualityBonus;
    public int GetTier() => tier;
    public bool IsInUse() => isInUse;
    public float GetFuelPercentage() => requiresFuel ? currentFuel / maxFuel : 1f;
    public List<CraftingRecipe> GetAvailableRecipes() => availableRecipes;
}

// Interface for interactable objects
public interface IInteractable
{
    void Interact(GameObject interactor);
    string GetInteractionPrompt();
    bool CanInteract(GameObject interactor);
}

// Interface for items (for recipe discovery)
public interface IItem
{
    string GetItemID();
}