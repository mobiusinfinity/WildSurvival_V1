using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Complete crafting UI system for Wild Survival
/// Fixed version with proper type handling for CraftingRecipe
/// </summary>
public class CraftingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeItemPrefab;
    [SerializeField] private GameObject ingredientSlotPrefab;

    [Header("Recipe Details")]
    [SerializeField] private TextMeshProUGUI recipeName;
    [SerializeField] private TextMeshProUGUI recipeDescription;
    [SerializeField] private Image outputIcon;
    [SerializeField] private TextMeshProUGUI outputQuantity;
    [SerializeField] private Transform ingredientsContainer;
    [SerializeField] private TextMeshProUGUI craftingTimeText;
    [SerializeField] private TextMeshProUGUI requiredLevelText;
    [SerializeField] private TextMeshProUGUI workbenchText;

    [Header("Crafting Controls")]
    [SerializeField] private Button craftButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Slider quantitySlider;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Filtering")]
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private Toggle showCraftableOnly;
    [SerializeField] private Toggle showUnlockedOnly;

    [Header("Category Tabs")]
    [SerializeField] private Transform tabContainer;
    [SerializeField] private GameObject tabPrefab;
    [SerializeField] private Color activeTabColor = new Color(0.2f, 0.6f, 0.2f);
    [SerializeField] private Color inactiveTabColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Visual Settings")]
    [SerializeField] private Color canCraftColor = Color.white;
    [SerializeField] private Color cannotCraftColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color missingIngredientColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color availableIngredientColor = new Color(0.3f, 1f, 0.3f);

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 0.3f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // References
    private CraftingManager craftingManager;
    private InventoryManager inventoryManager;
    private PlayerStats playerStats;
    private NotificationSystem notifications;

    // Current state
    private CraftingRecipe selectedRecipe;
    private List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
    private List<CraftingRecipe> filteredRecipes = new List<CraftingRecipe>();
    private Dictionary<string, RecipeUIItem> recipeUIItems = new Dictionary<string, RecipeUIItem>();
    private List<IngredientSlot> ingredientSlots = new List<IngredientSlot>();
    private string currentCategory = "All";
    private bool isOpen = false;
    private Coroutine animationCoroutine;

    // UI Components
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;

    private void Awake()
    {
        canvasGroup = craftingPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = craftingPanel.AddComponent<CanvasGroup>();
        }

        panelRect = craftingPanel.GetComponent<RectTransform>();

        SetupUI();
        craftingPanel.SetActive(false);
    }

    private void Start()
    {
        // Get references
        craftingManager = CraftingManager.Instance;
        inventoryManager = InventoryManager.Instance;
        playerStats = FindObjectOfType<PlayerStats>();
        notifications = NotificationSystem.Instance;

        if (craftingManager == null)
        {
            Debug.LogError("[CraftingUI] CraftingManager not found!");
            return;
        }

        // Subscribe to events - Fixed to use CraftingRecipe type
        if (craftingManager != null)
        {
            craftingManager.OnRecipeDiscovered.AddListener(OnRecipeDiscovered);
            craftingManager.OnCraftingStarted.AddListener(OnCraftingStarted);
            craftingManager.OnCraftingCompleted.AddListener(OnCraftingCompleted);
            craftingManager.OnCraftingCancelled.AddListener(OnCraftingCancelled);
            craftingManager.OnCraftingProgress.AddListener(UpdateProgressBar);
            craftingManager.OnWorkbenchChanged.AddListener(OnWorkbenchChanged);
        }

        // Setup UI callbacks
        craftButton.onClick.AddListener(OnCraftButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        quantitySlider.onValueChanged.AddListener(OnQuantityChanged);
        searchField.onValueChanged.AddListener(OnSearchChanged);
        categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        showCraftableOnly.onValueChanged.AddListener(OnFilterChanged);
        showUnlockedOnly.onValueChanged.AddListener(OnFilterChanged);

        // Initial setup
        PopulateCategories();
        RefreshRecipeList();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (craftingManager != null)
        {
            craftingManager.OnRecipeDiscovered.RemoveListener(OnRecipeDiscovered);
            craftingManager.OnCraftingStarted.RemoveListener(OnCraftingStarted);
            craftingManager.OnCraftingCompleted.RemoveListener(OnCraftingCompleted);
            craftingManager.OnCraftingCancelled.RemoveListener(OnCraftingCancelled);
            craftingManager.OnCraftingProgress.RemoveListener(UpdateProgressBar);
            craftingManager.OnWorkbenchChanged.RemoveListener(OnWorkbenchChanged);
        }
    }

    private void Update()
    {
        // Toggle UI with C key (configurable)
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleUI();
        }

        // Update crafting button state
        if (isOpen && selectedRecipe != null)
        {
            UpdateCraftButton();
        }
    }

    private void SetupUI()
    {
        // Ensure UI components are properly configured
        progressBar.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        quantitySlider.minValue = 1;
        quantitySlider.maxValue = 10;
        quantitySlider.wholeNumbers = true;
        quantitySlider.value = 1;
    }

    public void ToggleUI()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (isOpen) return;

        isOpen = true;
        craftingPanel.SetActive(true);

        // Animate opening
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateOpen());

        RefreshRecipeList();

        // Select first recipe if available
        if (filteredRecipes.Count > 0)
        {
            SelectRecipe(filteredRecipes[0]);
        }
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;

        // Cancel any ongoing crafting - Fixed: use property not method
        if (craftingManager.IsCrafting)
        {
            craftingManager.CancelCrafting();
        }

        // Animate closing
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateClose());
    }

    private IEnumerator AnimateOpen()
    {
        float elapsed = 0;
        canvasGroup.alpha = 0;
        panelRect.localScale = Vector3.one * 0.8f;

        while (elapsed < fadeSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeSpeed;
            float curveValue = openCurve.Evaluate(t);

            canvasGroup.alpha = curveValue;
            panelRect.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, curveValue);

            yield return null;
        }

        canvasGroup.alpha = 1;
        panelRect.localScale = Vector3.one;
    }

    private IEnumerator AnimateClose()
    {
        float elapsed = 0;

        while (elapsed < fadeSpeed)
        {
            elapsed += Time.deltaTime;
            float t = 1 - (elapsed / fadeSpeed);
            float curveValue = openCurve.Evaluate(t);

            canvasGroup.alpha = curveValue;
            panelRect.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, curveValue);

            yield return null;
        }

        canvasGroup.alpha = 0;
        craftingPanel.SetActive(false);
    }

    private void PopulateCategories()
    {
        // Get unique categories from all recipes
        HashSet<string> categories = new HashSet<string> { "All" };

        var allRecipes = craftingManager.GetAllRecipes();
        foreach (var recipe in allRecipes)
        {
            if (!string.IsNullOrEmpty(recipe.category))
            {
                categories.Add(recipe.category);
            }
        }

        // Populate dropdown
        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(categories.ToList());

        // Create category tabs if using tab system
        if (tabContainer != null && tabPrefab != null)
        {
            CreateCategoryTabs(categories.ToList());
        }
    }

    private void CreateCategoryTabs(List<string> categories)
    {
        // Clear existing tabs
        foreach (Transform child in tabContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new tabs
        foreach (string category in categories)
        {
            GameObject tab = Instantiate(tabPrefab, tabContainer);
            tab.name = $"Tab_{category}";

            var text = tab.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = category;

            var button = tab.GetComponent<Button>();
            if (button != null)
            {
                string cat = category; // Capture for closure
                button.onClick.AddListener(() => SelectCategory(cat));
            }

            // Set initial color
            var image = tab.GetComponent<Image>();
            if (image != null)
            {
                image.color = category == "All" ? activeTabColor : inactiveTabColor;
            }
        }
    }

    private void SelectCategory(string category)
    {
        currentCategory = category;

        // Update dropdown
        var options = categoryDropdown.options;
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].text == category)
            {
                categoryDropdown.value = i;
                break;
            }
        }

        // Update tab colors
        UpdateTabColors();

        // Refresh list
        RefreshRecipeList();
    }

    private void UpdateTabColors()
    {
        if (tabContainer == null) return;

        foreach (Transform tab in tabContainer)
        {
            var text = tab.GetComponentInChildren<TextMeshProUGUI>();
            var image = tab.GetComponent<Image>();

            if (text != null && image != null)
            {
                image.color = text.text == currentCategory ? activeTabColor : inactiveTabColor;
            }
        }
    }

    public void RefreshRecipeList()
    {
        if (craftingManager == null) return;

        // Get recipes from manager
        availableRecipes = craftingManager.GetAvailableRecipes();

        // Apply filters
        ApplyFilters();

        // Update UI
        UpdateRecipeListUI();
    }

    private void ApplyFilters()
    {
        filteredRecipes.Clear();

        string searchTerm = searchField != null ? searchField.text.ToLower() : "";
        bool craftableOnly = showCraftableOnly != null ? showCraftableOnly.isOn : false;
        bool unlockedOnly = showUnlockedOnly != null ? showUnlockedOnly.isOn : true;

        foreach (var recipe in availableRecipes)
        {
            // Category filter
            if (currentCategory != "All" && recipe.category != currentCategory)
                continue;

            // Search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                if (!recipe.displayName.ToLower().Contains(searchTerm) &&
                    !recipe.description.ToLower().Contains(searchTerm))
                    continue;
            }

            // Craftable filter
            if (craftableOnly && !craftingManager.CanCraftRecipe(recipe))
                continue;

            // Unlocked filter
            if (unlockedOnly && !craftingManager.IsRecipeKnown(recipe.recipeID))
                continue;

            filteredRecipes.Add(recipe);
        }

        // Sort by category, then name
        filteredRecipes.Sort((a, b) =>
        {
            int catCompare = string.Compare(a.category, b.category);
            return catCompare != 0 ? catCompare : string.Compare(a.displayName, b.displayName);
        });
    }

    private void UpdateRecipeListUI()
    {
        // Clear existing items
        foreach (var kvp in recipeUIItems)
        {
            Destroy(kvp.Value.gameObject);
        }
        recipeUIItems.Clear();

        // Create new items
        foreach (var recipe in filteredRecipes)
        {
            CreateRecipeUIItem(recipe);
        }
    }

    private void CreateRecipeUIItem(CraftingRecipe recipe)
    {
        GameObject itemGO = Instantiate(recipeItemPrefab, recipeListContainer);
        RecipeUIItem uiItem = itemGO.GetComponent<RecipeUIItem>();

        if (uiItem == null)
        {
            uiItem = itemGO.AddComponent<RecipeUIItem>();
        }

        // Setup the UI item
        uiItem.Setup(recipe, this);

        // Update visual state
        bool canCraft = craftingManager.CanCraftRecipe(recipe);
        uiItem.SetCraftableState(canCraft);

        // Store reference
        recipeUIItems[recipe.recipeID] = uiItem;
    }

    public void SelectRecipe(CraftingRecipe recipe)
    {
        if (recipe == null) return;

        selectedRecipe = recipe;

        // Update selection visual
        foreach (var kvp in recipeUIItems)
        {
            kvp.Value.SetSelected(kvp.Key == recipe.recipeID);
        }

        // Update details panel
        UpdateRecipeDetails();
    }

    private void UpdateRecipeDetails()
    {
        if (selectedRecipe == null)
        {
            ClearRecipeDetails();
            return;
        }

        // Basic info
        recipeName.text = selectedRecipe.displayName;
        recipeDescription.text = selectedRecipe.description;

        // Output info
        if (outputIcon != null && selectedRecipe.output != null)
        {
            // Load icon from item data
            var itemData = GetItemData(selectedRecipe.output.itemID);
            if (itemData != null && itemData.icon != null)
            {
                outputIcon.sprite = itemData.icon;
            }
        }

        outputQuantity.text = $"x{selectedRecipe.output.quantityMin}-{selectedRecipe.output.quantityMax}";

        // Crafting info
        craftingTimeText.text = $"Time: {selectedRecipe.craftingTime:F1}s";
        requiredLevelText.text = $"Level: {selectedRecipe.requiredPlayerLevel}";
        workbenchText.text = $"Station: {selectedRecipe.requiredWorkbench}";

        // Ingredients
        UpdateIngredientsList();

        // Quantity slider
        UpdateQuantitySlider();

        // Update craft button
        UpdateCraftButton();
    }

    private void UpdateIngredientsList()
    {
        // Clear existing slots
        foreach (var slot in ingredientSlots)
        {
            Destroy(slot.gameObject);
        }
        ingredientSlots.Clear();

        if (selectedRecipe == null) return;

        // Create ingredient slots
        foreach (var ingredient in selectedRecipe.ingredients)
        {
            GameObject slotGO = Instantiate(ingredientSlotPrefab, ingredientsContainer);
            IngredientSlot slot = slotGO.GetComponent<IngredientSlot>();

            if (slot == null)
            {
                slot = slotGO.AddComponent<IngredientSlot>();
            }

            // Setup slot - Fixed: use itemID not itemIDD
            var itemData = GetItemData(ingredient.itemID);
            int currentAmount = inventoryManager != null ?
                inventoryManager.GetItemCount(ingredient.itemID) : 0;

            slot.Setup(itemData, ingredient.quantity, currentAmount);

            // Update visual state
            bool hasEnough = currentAmount >= ingredient.quantity;
            slot.SetAvailableState(hasEnough);

            ingredientSlots.Add(slot);
        }
    }

    private void UpdateQuantitySlider()
    {
        if (selectedRecipe == null || inventoryManager == null)
        {
            quantitySlider.interactable = false;
            quantitySlider.value = 1;
            quantityText.text = "1";
            return;
        }

        // Calculate max craftable quantity
        int maxQuantity = int.MaxValue;

        foreach (var ingredient in selectedRecipe.ingredients)
        {
            int available = inventoryManager.GetItemCount(ingredient.itemID);
            int possibleCrafts = available / ingredient.quantity;
            maxQuantity = Mathf.Min(maxQuantity, possibleCrafts);
        }

        maxQuantity = Mathf.Clamp(maxQuantity, 0, 99);

        // Update slider
        quantitySlider.interactable = maxQuantity > 0;
        quantitySlider.maxValue = Mathf.Max(1, maxQuantity);
        quantitySlider.value = Mathf.Min(quantitySlider.value, maxQuantity);

        UpdateQuantityText();
    }

    private void OnQuantityChanged(float value)
    {
        UpdateQuantityText();
        UpdateIngredientsList(); // Update required amounts display
    }

    private void UpdateQuantityText()
    {
        int quantity = Mathf.RoundToInt(quantitySlider.value);
        quantityText.text = quantity.ToString();
    }

    private void UpdateCraftButton()
    {
        if (selectedRecipe == null || craftingManager == null)
        {
            craftButton.interactable = false;
            return;
        }

        bool canCraft = craftingManager.CanCraftRecipe(selectedRecipe);
        bool isCrafting = craftingManager.IsCrafting; // Fixed: property not method

        craftButton.interactable = canCraft && !isCrafting;

        // Update button text
        var buttonText = craftButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (isCrafting)
                buttonText.text = "Crafting...";
            else if (!canCraft)
                buttonText.text = "Cannot Craft";
            else
                buttonText.text = "Craft";
        }
    }

    private void OnCraftButtonClicked()
    {
        if (selectedRecipe == null || craftingManager == null) return;

        int quantity = Mathf.RoundToInt(quantitySlider.value);

        // Fixed: Pass recipe object, not just ID
        craftingManager.StartCrafting(selectedRecipe, quantity);
    }

    private void OnCancelButtonClicked()
    {
        if (craftingManager != null)
        {
            craftingManager.CancelCrafting();
        }
    }

    private void OnCraftingStarted(CraftingRecipe recipe)
    {
        // Show progress bar
        progressBar.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
        craftButton.gameObject.SetActive(false);

        // Disable quantity slider during crafting
        quantitySlider.interactable = false;

        // Update progress text - Fixed: use displayName
        progressText.text = $"Crafting {recipe.displayName}...";
    }

    private void OnCraftingCompleted(CraftingRecipe recipe)
    {
        // Hide progress bar
        progressBar.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        craftButton.gameObject.SetActive(true);

        // Re-enable quantity slider
        quantitySlider.interactable = true;

        // Update UI
        UpdateIngredientsList();
        UpdateQuantitySlider();
        UpdateCraftButton();

        // Show notification - Fixed: use displayName
        ShowNotification($"Crafted {recipe.displayName}!", NotificationType.Success);
    }

    private void OnCraftingCancelled(CraftingRecipe recipe)
    {
        // Hide progress bar
        progressBar.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        craftButton.gameObject.SetActive(true);

        // Re-enable quantity slider
        quantitySlider.interactable = true;

        // Update UI
        UpdateCraftButton();

        // Show notification
        ShowNotification("Crafting cancelled", NotificationType.Info);
    }

    private void UpdateProgressBar(float progress)
    {
        progressBar.value = progress;

        if (progressText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100);
            progressText.text = $"Crafting... {percentage}%";
        }
    }

    private void OnRecipeDiscovered(CraftingRecipe recipe)
    {
        RefreshRecipeList();
        ShowNotification($"New Recipe: {recipe.displayName}!", NotificationType.Success);
    }

    private void OnWorkbenchChanged(CraftingRecipe.WorkbenchType workbench)
    {
        RefreshRecipeList();
        UpdateRecipeDetails();
    }

    private void OnSearchChanged(string searchTerm)
    {
        RefreshRecipeList();
    }

    private void OnCategoryChanged(int index)
    {
        currentCategory = categoryDropdown.options[index].text;
        UpdateTabColors();
        RefreshRecipeList();
    }

    private void OnFilterChanged(bool value)
    {
        RefreshRecipeList();
    }

    private void ClearRecipeDetails()
    {
        recipeName.text = "Select a Recipe";
        recipeDescription.text = "";
        outputQuantity.text = "";
        craftingTimeText.text = "";
        requiredLevelText.text = "";
        workbenchText.text = "";

        // Clear ingredients
        foreach (var slot in ingredientSlots)
        {
            Destroy(slot.gameObject);
        }
        ingredientSlots.Clear();

        craftButton.interactable = false;
    }

    private ItemData GetItemData(string itemID)
    {
        // Try to get from inventory manager first
        if (inventoryManager != null)
        {
            return inventoryManager.GetItemData(itemID);
        }

        // Fallback to loading from resources
        return Resources.Load<ItemData>($"Items/{itemID}");
    }

    private void ShowNotification(string message, NotificationType type)
    {
        if (notifications != null)
        {
            notifications.ShowNotification(message, type);
        }
        else
        {
            Debug.Log($"[CraftingUI] {type}: {message}");
        }
    }
}

// Helper component for recipe list items
public class RecipeUIItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private Image background;
    [SerializeField] private GameObject craftableIndicator;
    [SerializeField] private GameObject selectedIndicator;

    private CraftingRecipe recipe;
    private CraftingUI craftingUI;
    private bool isSelected;

    public void Setup(CraftingRecipe recipe, CraftingUI ui)
    {
        this.recipe = recipe;
        this.craftingUI = ui;

        // Set display info - Fixed: use displayName
        if (nameText != null) nameText.text = recipe.displayName;
        if (categoryText != null) categoryText.text = recipe.category;

        // Load icon
        if (icon != null && recipe.output != null)
        {
            var itemData = Resources.Load<ItemData>($"Items/{recipe.output.itemID}");
            if (itemData != null && itemData.icon != null)
            {
                icon.sprite = itemData.icon;
            }
        }
    }

    public void SetCraftableState(bool canCraft)
    {
        if (craftableIndicator != null)
            craftableIndicator.SetActive(canCraft);

        if (background != null)
        {
            var color = canCraft ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            background.color = color;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);

        if (background != null)
        {
            var color = selected ? new Color(0.2f, 0.5f, 0.2f) : Color.white;
            background.color = color;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (craftingUI != null && recipe != null)
        {
            craftingUI.SelectRecipe(recipe);
        }
    }
}

// Helper component for ingredient slots
public class IngredientSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Image background;

    private ItemData itemData;
    private int requiredAmount;
    private int currentAmount;

    public void Setup(ItemData data, int required, int current)
    {
        itemData = data;
        requiredAmount = required;
        currentAmount = current;

        // Update display
        if (data != null)
        {
            if (icon != null && data.icon != null)
                icon.sprite = data.icon;

            if (nameText != null)
                nameText.text = data.name; // Use GetDisplayName for compatibility
        }

        if (amountText != null)
            amountText.text = $"{current}/{required}";
    }

    public void SetAvailableState(bool hasEnough)
    {
        var color = hasEnough ? new Color(0.3f, 1f, 0.3f, 0.3f) : new Color(1f, 0.3f, 0.3f, 0.3f);
        if (background != null)
            background.color = color;

        if (amountText != null)
            amountText.color = hasEnough ? Color.green : Color.red;
    }
}