using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

// ==================== DATA STRUCTURES ====================
#region Data Structures

// Enums
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

public enum CraftingCategory
{
    Tools,
    Weapons,
    Clothing,
    Building,
    Cooking,
    Medicine,
    Processing,
    Advanced
}

public enum WorkstationType
{
    None,
    Campfire,
    CookingPot,
    Workbench,
    Forge,
    Anvil,
    TanningRack,
    Loom,
    ChemistryStation,
    AdvancedWorkbench
}

public enum DiscoveryMethod
{
    Known,
    Experimentation,
    Book,
    NPC,
    Observation,
    Analysis,
    Milestone
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

// Item Definition
[Serializable]
[CreateAssetMenu(fileName = "NewItem", menuName = "Wild Survival/Item")]
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

// Recipe Definition
[Serializable]
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Wild Survival/Recipe")]
public class RecipeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string recipeID = "";
    public string recipeName = "New Recipe";
    [TextArea(3, 5)]
    public string description = "";
    public Sprite icon;

    [Header("Category")]
    public CraftingCategory category = CraftingCategory.Tools;
    public int tier = 0;

    [Header("Requirements")]
    public WorkstationType requiredWorkstation = WorkstationType.None;
    public RecipeIngredient[] ingredients = new RecipeIngredient[0];

    [Header("Process")]
    public float baseCraftTime = 3f;
    public float baseTemperature = 20f;
    public float failureChance = 0.1f;

    [Header("Output")]
    public RecipeOutput[] outputs = new RecipeOutput[0];
    public RecipeOutput[] failureOutputs = new RecipeOutput[0];

    [Header("Discovery")]
    public bool isKnownByDefault = true;
    public DiscoveryMethod discoveryMethod = DiscoveryMethod.Known;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(recipeID))
        {
            recipeID = "recipe_" + name.Replace(" ", "_").ToLower();
        }
    }
}

[Serializable]
public class RecipeIngredient
{
    public string name = "Ingredient";
    public ItemDefinition specificItem;
    public ItemCategory category = ItemCategory.Misc;
    public int quantity = 1;
    public bool consumed = true;
}

[Serializable]
public class RecipeOutput
{
    public ItemDefinition item;
    public int quantityMin = 1;
    public int quantityMax = 1;
    [Range(0f, 1f)]
    public float chance = 1f;
}

// Item Instance (for runtime/testing)
[Serializable]
public class ItemInstance
{
    public ItemDefinition definition;
    public int stackSize = 1;
    public ItemQuality quality = ItemQuality.Common;
    public float currentDurability = 100f;
    public float condition = 1f;

    public ItemInstance(ItemDefinition def)
    {
        definition = def;
        if (def != null)
        {
            currentDurability = def.maxDurability;
        }
    }
}

// Database classes
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Wild Survival/Databases/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

    public void AddItem(ItemDefinition item)
    {
        if (item != null && !items.Contains(item))
        {
            items.Add(item);
            EditorUtility.SetDirty(this);
        }
    }

    public void RemoveItem(ItemDefinition item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            EditorUtility.SetDirty(this);
        }
    }

    public List<ItemDefinition> GetAllItems()
    {
        return new List<ItemDefinition>(items);
    }

    public ItemDefinition GetItem(string itemID)
    {
        return items.FirstOrDefault(i => i.itemID == itemID);
    }
}

[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Wild Survival/Databases/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    [SerializeField] private List<RecipeDefinition> recipes = new List<RecipeDefinition>();

    public void AddRecipe(RecipeDefinition recipe)
    {
        if (recipe != null && !recipes.Contains(recipe))
        {
            recipes.Add(recipe);
            EditorUtility.SetDirty(this);
        }
    }

    public void RemoveRecipe(RecipeDefinition recipe)
    {
        if (recipes.Contains(recipe))
        {
            recipes.Remove(recipe);
            EditorUtility.SetDirty(this);
        }
    }

    public List<RecipeDefinition> GetAllRecipes()
    {
        return new List<RecipeDefinition>(recipes);
    }

    public RecipeDefinition GetRecipe(string recipeID)
    {
        return recipes.FirstOrDefault(r => r.recipeID == recipeID);
    }
}

#endregion

// ==================== MAIN TOOL ====================
public class UltimateInventoryTool : EditorWindow
{
    // Window Configuration
    private const string WINDOW_TITLE = "Ultimate Inventory Tool";
    private const float MIN_WINDOW_WIDTH = 1200f;
    private const float MIN_WINDOW_HEIGHT = 800f;

    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private bool stylesInitialized = false;

    // Tab System
    private string[] tabNames = new[]
    {
        "Dashboard",
        "Item Creator",
        "Recipe Builder",
        "Inventory Simulator",
        "Crafting Test",
        "Database Manager"
    };
    private int currentTab = 0;

    // Sub-systems
    private ItemCreatorTab itemCreator;
    private RecipeBuilderTab recipeBuilder;
    private InventorySimulatorTab inventorySimulator;
    private CraftingTestTab craftingTest;
    private DatabaseManagerTab databaseManager;
    private DashboardTab dashboard;

    // Shared Data
    private ItemDatabase itemDatabase;
    private RecipeDatabase recipeDatabase;
    private List<ItemDefinition> cachedItems;
    private List<RecipeDefinition> cachedRecipes;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private bool isDirty = false;

    // Styling
    //private GUIStyle headerStyle;
    //private GUIStyle boxStyle;

    [MenuItem("Tools/Wild Survival/Ultimate Inventory Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<UltimateInventoryTool>(WINDOW_TITLE);
        window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
        window.Show();
    }

    private void OnEnable()
    {
        LoadDatabases();
        InitializeTabs();
        RefreshCaches();
        // Remove SetupStyles() from here - it won't work properly
    }

    private void LoadDatabases()
    {
        // Try to find existing databases
        string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
        }

        // Create if doesn't exist
        if (itemDatabase == null)
        {
            // Create directories if they don't exist
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                AssetDatabase.CreateFolder("Assets", "_Project");
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Data");
            }

            itemDatabase = CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(itemDatabase, "Assets/_Project/Data/ItemDatabase.asset");
            AssetDatabase.SaveAssets();
        }

        // Same for recipes
        guids = AssetDatabase.FindAssets("t:RecipeDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            recipeDatabase = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
        }

        if (recipeDatabase == null)
        {
            recipeDatabase = CreateInstance<RecipeDatabase>();
            AssetDatabase.CreateAsset(recipeDatabase, "Assets/_Project/Data/RecipeDatabase.asset");
            AssetDatabase.SaveAssets();
        }
    }

    private void InitializeTabs()
    {
        dashboard = new DashboardTab(this);
        itemCreator = new ItemCreatorTab(this);
        recipeBuilder = new RecipeBuilderTab(this);
        inventorySimulator = new InventorySimulatorTab(this);
        craftingTest = new CraftingTestTab(this);
        databaseManager = new DatabaseManagerTab(this);
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        try
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 5, 5)
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            stylesInitialized = true;
        }
        catch
        {
            // Fallback to defaults if initialization fails
            headerStyle = EditorStyles.boldLabel;
            boxStyle = GUI.skin.box;
        }
    }

    private void OnGUI()
    {
        // Initialize styles at the start of OnGUI
        InitializeStyles();

        DrawHeader();
        DrawToolbar();
        DrawTabContent();
        DrawFooter();

        if (isDirty)
        {
            SaveChangesInternal();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Ultimate Inventory Tool", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        // Quick Actions
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshCaches();
        }

        if (GUILayout.Button("Save All", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            SaveChangesInternal();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        int newTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(30));
        if (newTab != currentTab)
        {
            currentTab = newTab;
            GUI.FocusControl(null); // Clear focus when switching tabs
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
    }

    private void DrawTabContent()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        try
        {
            switch (currentTab)
            {
                case 0:
                    dashboard?.Draw();
                    break;
                case 1:
                    itemCreator?.Draw();
                    break;
                case 2:
                    recipeBuilder?.Draw();
                    break;
                case 3:
                    inventorySimulator?.Draw();
                    break;
                case 4:
                    craftingTest?.Draw();
                    break;
                case 5:
                    databaseManager?.Draw();
                    break;
            }
        }
        catch (Exception e)
        {
            EditorGUILayout.HelpBox($"Error in tab: {e.Message}", MessageType.Error);
            Debug.LogError($"Ultimate Inventory Tool Error: {e}");
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Status
        GUILayout.Label($"Items: {cachedItems?.Count ?? 0} | Recipes: {cachedRecipes?.Count ?? 0}",
            EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        // Save indicator
        if (isDirty)
        {
            GUILayout.Label("• Unsaved Changes", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.yellow }
            });
        }

        EditorGUILayout.EndHorizontal();
    }

    public void RefreshCaches()
    {
        if (itemDatabase != null)
            cachedItems = itemDatabase.GetAllItems();
        else
            cachedItems = new List<ItemDefinition>();

        if (recipeDatabase != null)
            cachedRecipes = recipeDatabase.GetAllRecipes();
        else
            cachedRecipes = new List<RecipeDefinition>();

        // Notify all tabs
        dashboard?.Refresh();
        itemCreator?.Refresh();
        recipeBuilder?.Refresh();
        databaseManager?.Refresh();
    }

    public void MarkDirty()
    {
        isDirty = true;
        if (itemDatabase != null)
            EditorUtility.SetDirty(itemDatabase);
        if (recipeDatabase != null)
            EditorUtility.SetDirty(recipeDatabase);
    }

    private void SaveChangesInternal()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        isDirty = false;
    }

    // Shared utility methods for tabs
    public ItemDefinition GetItem(string itemID)
    {
        return cachedItems?.FirstOrDefault(i => i.itemID == itemID);
    }

    public RecipeDefinition GetRecipe(string recipeID)
    {
        return cachedRecipes?.FirstOrDefault(r => r.recipeID == recipeID);
    }

    public List<ItemDefinition> GetItemsByCategory(ItemCategory category)
    {
        return cachedItems?.Where(i => i.primaryCategory == category).ToList() ?? new List<ItemDefinition>();
    }

    public void AddItem(ItemDefinition item)
    {
        if (itemDatabase != null && item != null)
        {
            itemDatabase.AddItem(item);
            if (cachedItems == null)
                cachedItems = new List<ItemDefinition>();
            if (!cachedItems.Contains(item))
                cachedItems.Add(item);
            MarkDirty();
        }
    }

    public void AddRecipe(RecipeDefinition recipe)
    {
        if (recipeDatabase != null && recipe != null)
        {
            recipeDatabase.AddRecipe(recipe);
            if (cachedRecipes == null)
                cachedRecipes = new List<RecipeDefinition>();
            if (!cachedRecipes.Contains(recipe))
                cachedRecipes.Add(recipe);
            MarkDirty();
        }
    }

    // ==================== TAB CLASSES ====================

    // Dashboard Tab
    public class DashboardTab
    {
        private UltimateInventoryTool tool;

        public DashboardTab(UltimateInventoryTool tool)
        {
            this.tool = tool;
        }

        public void Draw()
        {

            var safeHeaderStyle = tool.headerStyle ?? EditorStyles.boldLabel;
            var safeBoxStyle = tool.boxStyle ?? GUI.skin.box;

            EditorGUILayout.LabelField("Dashboard", safeHeaderStyle);

            EditorGUILayout.LabelField("Dashboard", tool.headerStyle);
            EditorGUILayout.Space(10);

            // Quick Stats
            EditorGUILayout.BeginHorizontal();

            DrawStatBox("Total Items", tool.cachedItems?.Count ?? 0, Color.cyan);
            DrawStatBox("Total Recipes", tool.cachedRecipes?.Count ?? 0, Color.green);
            DrawStatBox("Categories", CountCategories(), Color.yellow);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Create Item", GUILayout.Height(40)))
            {
                tool.currentTab = 1;
                tool.itemCreator?.InitializeNewItem();
            }

            if (GUILayout.Button("Create Recipe", GUILayout.Height(40)))
            {
                tool.currentTab = 2;
                tool.recipeBuilder?.InitializeNewRecipe();
            }

            if (GUILayout.Button("Test Inventory", GUILayout.Height(40)))
            {
                tool.currentTab = 3;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Recent Items
            DrawRecentItems();
        }

        private void DrawStatBox(string label, int value, Color color)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color * 0.3f;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(120), GUILayout.Height(60));
            GUI.backgroundColor = oldColor;

            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }
            };

            EditorGUILayout.LabelField(value.ToString(), style);

            EditorGUILayout.EndVertical();
        }

        private int CountCategories()
        {
            if (tool.cachedItems == null || tool.cachedItems.Count == 0)
                return 0;

            return tool.cachedItems.Select(i => i.primaryCategory).Distinct().Count();
        }

        private void DrawRecentItems()
        {
            EditorGUILayout.LabelField("Recent Items", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(tool.boxStyle);

            var recentItems = tool.cachedItems?.Take(5).ToList() ?? new List<ItemDefinition>();

            if (recentItems.Count == 0)
            {
                EditorGUILayout.HelpBox("No items found. Create your first item!", MessageType.Info);
            }
            else
            {
                foreach (var item in recentItems)
                {
                    EditorGUILayout.BeginHorizontal();

                    if (item.icon != null)
                    {
                        GUILayout.Label(item.icon.texture, GUILayout.Width(32), GUILayout.Height(32));
                    }
                    else
                    {
                        GUILayout.Box("", GUILayout.Width(32), GUILayout.Height(32));
                    }

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(item.displayName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{item.primaryCategory} | {item.weight}kg",
                        EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Edit", GUILayout.Width(50)))
                    {
                        tool.currentTab = 1;
                        tool.itemCreator?.LoadItem(item);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        public void Refresh()
        {
            // Refresh dashboard
        }
    }

    // Item Creator Tab - Simplified version
    public class ItemCreatorTab
    {
        private UltimateInventoryTool tool;
        private ItemDefinition currentItem;
        private bool isCreatingNew = true;

        // Editor fields
        private string itemID = "";
        private string displayName = "";
        private string description = "";
        private Sprite icon;
        private GameObject worldModel;

        private ItemCategory category = ItemCategory.Misc;
        private ItemSubcategory subcategory;
        private List<ItemTag> tags = new List<ItemTag>();

        private float weight = 1f;
        private int maxStackSize = 1;
        private Vector2Int gridSize = Vector2Int.one;

        private bool hasDurability = false;
        private float maxDurability = 100f;

        private int baseValue = 1;

        public ItemCreatorTab(UltimateInventoryTool tool)
        {
            this.tool = tool;
            InitializeNewItem();
        }

        public void Draw()
        {
            var safeHeaderStyle = tool.headerStyle ?? EditorStyles.boldLabel;
            var safeBoxStyle = tool.boxStyle ?? GUI.skin.box;
                
            EditorGUILayout.BeginHorizontal();

            // Left Panel - Item List
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            DrawItemList();
            EditorGUILayout.EndVertical();

            // Right Panel - Item Editor
            EditorGUILayout.BeginVertical();
            DrawItemEditor();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawItemList()
        {
            EditorGUILayout.LabelField("Items", tool.headerStyle);

            if (GUILayout.Button("+ Create New Item", GUILayout.Height(30)))
            {
                InitializeNewItem();
            }

            EditorGUILayout.Space(5);

            var items = tool.cachedItems ?? new List<ItemDefinition>();

            foreach (var item in items.Take(20)) // Limit display for performance
            {
                if (GUILayout.Button(item.displayName, EditorStyles.toolbarButton))
                {
                    LoadItem(item);
                }
            }
        }

        private void DrawItemEditor()
        {
            EditorGUILayout.LabelField(isCreatingNew ? "Create New Item" : $"Edit: {displayName}",
                tool.headerStyle);

            EditorGUILayout.BeginVertical(tool.boxStyle);

            // Basic Info
            itemID = EditorGUILayout.TextField("Item ID", itemID);
            displayName = EditorGUILayout.TextField("Display Name", displayName);

            EditorGUILayout.LabelField("Description");
            description = EditorGUILayout.TextArea(description, GUILayout.Height(60));

            icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);
            worldModel = (GameObject)EditorGUILayout.ObjectField("World Model", worldModel, typeof(GameObject), false);

            EditorGUILayout.Space(10);

            // Properties
            category = (ItemCategory)EditorGUILayout.EnumPopup("Category", category);
            subcategory = (ItemSubcategory)EditorGUILayout.EnumPopup("Subcategory", subcategory);

            weight = EditorGUILayout.FloatField("Weight (kg)", weight);
            maxStackSize = EditorGUILayout.IntField("Max Stack Size", maxStackSize);
            gridSize = EditorGUILayout.Vector2IntField("Grid Size", gridSize);

            EditorGUILayout.Space(10);

            // Durability
            hasDurability = EditorGUILayout.Toggle("Has Durability", hasDurability);
            if (hasDurability)
            {
                EditorGUI.indentLevel++;
                maxDurability = EditorGUILayout.FloatField("Max Durability", maxDurability);
                EditorGUI.indentLevel--;
            }

            baseValue = EditorGUILayout.IntField("Base Value", baseValue);

            EditorGUILayout.Space(10);

            // Actions
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(isCreatingNew ? "Create Item" : "Save Changes", GUILayout.Height(30)))
            {
                SaveItem();
            }
            GUI.backgroundColor = Color.white;

            if (!isCreatingNew && GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                LoadItem(currentItem);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        public void InitializeNewItem()
        {
            isCreatingNew = true;
            currentItem = null;

            itemID = "item_" + System.Guid.NewGuid().ToString().Substring(0, 8);
            displayName = "New Item";
            description = "";
            icon = null;
            worldModel = null;

            category = ItemCategory.Misc;
            subcategory = ItemSubcategory.None;
            tags.Clear();

            weight = 1f;
            maxStackSize = 1;
            gridSize = Vector2Int.one;

            hasDurability = false;
            maxDurability = 100f;

            baseValue = 1;
        }

        public void LoadItem(ItemDefinition item)
        {
            if (item == null) return;

            isCreatingNew = false;
            currentItem = item;

            itemID = item.itemID;
            displayName = item.displayName;
            description = item.description;
            icon = item.icon;
            worldModel = item.worldModel;

            category = item.primaryCategory;
            subcategory = item.subcategory;
            tags = item.tags?.ToList() ?? new List<ItemTag>();

            weight = item.weight;
            maxStackSize = item.maxStackSize;
            gridSize = item.gridSize;

            hasDurability = item.hasDurability;
            maxDurability = item.maxDurability;

            baseValue = item.baseValue;
        }

        private void SaveItem()
        {
            if (string.IsNullOrEmpty(itemID) || string.IsNullOrEmpty(displayName))
            {
                EditorUtility.DisplayDialog("Error", "Item ID and Display Name are required", "OK");
                return;
            }

            if (isCreatingNew)
            {
                // Create directories if needed
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Items"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
                    {
                        AssetDatabase.CreateFolder("Assets/_Project", "Data");
                    }
                    AssetDatabase.CreateFolder("Assets/_Project/Data", "Items");
                }

                currentItem = ScriptableObject.CreateInstance<ItemDefinition>();
                string path = $"Assets/_Project/Data/Items/{itemID}.asset";
                AssetDatabase.CreateAsset(currentItem, path);
            }

            // Update properties
            currentItem.itemID = itemID;
            currentItem.displayName = displayName;
            currentItem.description = description;
            currentItem.icon = icon;
            currentItem.worldModel = worldModel;

            currentItem.primaryCategory = category;
            currentItem.subcategory = subcategory;
            currentItem.tags = tags.ToArray();

            currentItem.weight = weight;
            currentItem.maxStackSize = maxStackSize;
            currentItem.gridSize = gridSize;

            currentItem.hasDurability = hasDurability;
            currentItem.maxDurability = maxDurability;

            currentItem.baseValue = baseValue;

            EditorUtility.SetDirty(currentItem);

            if (isCreatingNew)
            {
                tool.AddItem(currentItem);
                isCreatingNew = false;
            }
            else
            {
                tool.MarkDirty();
            }

            AssetDatabase.SaveAssets();
            tool.RefreshCaches();
        }

        public void Refresh()
        {
            // Refresh item creator
        }
    }

    // Recipe Builder Tab - Simplified version
    public class RecipeBuilderTab
    {
        private UltimateInventoryTool tool;
        private RecipeDefinition currentRecipe;
        private bool isCreatingNew = true;

        private string recipeID = "";
        private string recipeName = "";
        private string description = "";

        private CraftingCategory category = CraftingCategory.Tools;
        private WorkstationType workstation = WorkstationType.None;

        private List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        private List<RecipeOutput> outputs = new List<RecipeOutput>();

        private float craftTime = 3f;

        public RecipeBuilderTab(UltimateInventoryTool tool)
        {
            this.tool = tool;
            InitializeNewRecipe();
        }

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal();

            // Left Panel
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            DrawRecipeList();
            EditorGUILayout.EndVertical();

            // Right Panel
            EditorGUILayout.BeginVertical();
            DrawRecipeEditor();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRecipeList()
        {
            EditorGUILayout.LabelField("Recipes", tool.headerStyle);

            if (GUILayout.Button("+ Create New Recipe", GUILayout.Height(30)))
            {
                InitializeNewRecipe();
            }

            EditorGUILayout.Space(5);

            var recipes = tool.cachedRecipes ?? new List<RecipeDefinition>();

            foreach (var recipe in recipes.Take(20))
            {
                if (GUILayout.Button(recipe.recipeName, EditorStyles.toolbarButton))
                {
                    LoadRecipe(recipe);
                }
            }
        }

        private void DrawRecipeEditor()
        {
            EditorGUILayout.LabelField(isCreatingNew ? "Create New Recipe" : $"Edit: {recipeName}",
                tool.headerStyle);

            EditorGUILayout.BeginVertical(tool.boxStyle);

            // Basic Info
            recipeID = EditorGUILayout.TextField("Recipe ID", recipeID);
            recipeName = EditorGUILayout.TextField("Recipe Name", recipeName);
            description = EditorGUILayout.TextArea(description, GUILayout.Height(60));

            category = (CraftingCategory)EditorGUILayout.EnumPopup("Category", category);
            workstation = (WorkstationType)EditorGUILayout.EnumPopup("Workstation", workstation);
            craftTime = EditorGUILayout.FloatField("Craft Time (seconds)", craftTime);

            EditorGUILayout.Space(10);

            // Ingredients
            EditorGUILayout.LabelField("Ingredients", EditorStyles.boldLabel);
            for (int i = 0; i < ingredients.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                ingredients[i].specificItem = (ItemDefinition)EditorGUILayout.ObjectField(
                    ingredients[i].specificItem, typeof(ItemDefinition), false);
                ingredients[i].quantity = EditorGUILayout.IntField(ingredients[i].quantity, GUILayout.Width(50));

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    ingredients.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Ingredient"))
            {
                ingredients.Add(new RecipeIngredient());
            }

            EditorGUILayout.Space(10);

            // Outputs
            EditorGUILayout.LabelField("Outputs", EditorStyles.boldLabel);
            for (int i = 0; i < outputs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                outputs[i].item = (ItemDefinition)EditorGUILayout.ObjectField(
                    outputs[i].item, typeof(ItemDefinition), false);
                outputs[i].quantityMin = EditorGUILayout.IntField(outputs[i].quantityMin, GUILayout.Width(50));

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    outputs.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Output"))
            {
                outputs.Add(new RecipeOutput());
            }

            EditorGUILayout.Space(10);

            // Actions
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(isCreatingNew ? "Create Recipe" : "Save Changes", GUILayout.Height(30)))
            {
                SaveRecipe();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        public void InitializeNewRecipe()
        {
            isCreatingNew = true;
            currentRecipe = null;

            recipeID = "recipe_" + System.Guid.NewGuid().ToString().Substring(0, 8);
            recipeName = "New Recipe";
            description = "";

            category = CraftingCategory.Tools;
            workstation = WorkstationType.None;

            ingredients.Clear();
            outputs.Clear();

            craftTime = 3f;
        }

        private void LoadRecipe(RecipeDefinition recipe)
        {
            if (recipe == null) return;

            isCreatingNew = false;
            currentRecipe = recipe;

            recipeID = recipe.recipeID;
            recipeName = recipe.recipeName;
            description = recipe.description;

            category = recipe.category;
            workstation = recipe.requiredWorkstation;

            ingredients = recipe.ingredients?.ToList() ?? new List<RecipeIngredient>();
            outputs = recipe.outputs?.ToList() ?? new List<RecipeOutput>();

            craftTime = recipe.baseCraftTime;
        }

        private void SaveRecipe()
        {
            if (string.IsNullOrEmpty(recipeID) || string.IsNullOrEmpty(recipeName))
            {
                EditorUtility.DisplayDialog("Error", "Recipe ID and Name are required", "OK");
                return;
            }

            if (isCreatingNew)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Recipes"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Data", "Recipes");
                }

                currentRecipe = ScriptableObject.CreateInstance<RecipeDefinition>();
                string path = $"Assets/_Project/Data/Recipes/{recipeID}.asset";
                AssetDatabase.CreateAsset(currentRecipe, path);
            }

            currentRecipe.recipeID = recipeID;
            currentRecipe.recipeName = recipeName;
            currentRecipe.description = description;

            currentRecipe.category = category;
            currentRecipe.requiredWorkstation = workstation;

            currentRecipe.ingredients = ingredients.ToArray();
            currentRecipe.outputs = outputs.ToArray();

            currentRecipe.baseCraftTime = craftTime;

            EditorUtility.SetDirty(currentRecipe);

            if (isCreatingNew)
            {
                tool.AddRecipe(currentRecipe);
                isCreatingNew = false;
            }
            else
            {
                tool.MarkDirty();
            }

            AssetDatabase.SaveAssets();
            tool.RefreshCaches();
        }

        public void Refresh()
        {
            // Refresh recipe builder
        }
    }

    // Simplified placeholder tabs
    public class InventorySimulatorTab
    {
        private UltimateInventoryTool tool;

        public InventorySimulatorTab(UltimateInventoryTool tool)
        {
            this.tool = tool;
        }

        public void Draw()
        {
            EditorGUILayout.LabelField("Inventory Simulator", tool.headerStyle);
            EditorGUILayout.HelpBox("Inventory simulation coming soon!", MessageType.Info);
        }

        public void Refresh() { }
    }

    public class CraftingTestTab
    {
        private UltimateInventoryTool tool;

        public CraftingTestTab(UltimateInventoryTool tool)
        {
            this.tool = tool;
        }

        public void Draw()
        {
            EditorGUILayout.LabelField("Crafting Test", tool.headerStyle);
            EditorGUILayout.HelpBox("Crafting test system coming soon!", MessageType.Info);
        }

        public void Refresh() { }
    }

    public class DatabaseManagerTab
    {
        private UltimateInventoryTool tool;

        public DatabaseManagerTab(UltimateInventoryTool tool)
        {
            this.tool = tool;
        }

        public void Draw()
        {
            // Use safe style access
            var safeHeaderStyle = tool.headerStyle ?? EditorStyles.boldLabel;
            var safeBoxStyle = tool.boxStyle ?? GUI.skin.box;

            EditorGUILayout.LabelField("Database Manager", safeHeaderStyle);

            EditorGUILayout.BeginVertical(safeBoxStyle);

            EditorGUILayout.LabelField("Database Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Items: {tool.cachedItems?.Count ?? 0}");
            EditorGUILayout.LabelField($"Recipes: {tool.cachedRecipes?.Count ?? 0}");

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Refresh All", GUILayout.Height(30)))
            {
                tool.RefreshCaches();
            }

            if (GUILayout.Button("Save All", GUILayout.Height(30)))
            {
                tool.SaveChangesInternal();
            }

            EditorGUILayout.Space(10);

            // Add more database operations
            EditorGUILayout.LabelField("Database Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate All Items", GUILayout.Height(25)))
            {
                ValidateItems();
            }

            if (GUILayout.Button("Validate All Recipes", GUILayout.Height(25)))
            {
                ValidateRecipes();
            }

            if (GUILayout.Button("Export Database to JSON", GUILayout.Height(25)))
            {
                ExportToJSON();
            }

            EditorGUILayout.EndVertical();
        }

        private void ValidateItems()
        {
            int issues = 0;
            foreach (var item in tool.cachedItems ?? new List<ItemDefinition>())
            {
                if (string.IsNullOrEmpty(item.itemID))
                {
                    Debug.LogWarning($"Item '{item.name}' has no ID");
                    issues++;
                }
                if (item.weight <= 0)
                {
                    Debug.LogWarning($"Item '{item.displayName}' has invalid weight: {item.weight}");
                    issues++;
                }
                if (item.gridSize.x <= 0 || item.gridSize.y <= 0)
                {
                    Debug.LogWarning($"Item '{item.displayName}' has invalid grid size: {item.gridSize}");
                    issues++;
                }
            }

            if (issues == 0)
            {
                Debug.Log("✓ All items validated successfully!");
                EditorUtility.DisplayDialog("Validation Complete", "All items are valid!", "Great!");
            }
            else
            {
                Debug.LogError($"✗ Found {issues} validation issues");
                EditorUtility.DisplayDialog("Validation Failed", $"Found {issues} issues. Check console for details.", "OK");
            }
        }

        private void ValidateRecipes()
        {
            int issues = 0;
            foreach (var recipe in tool.cachedRecipes ?? new List<RecipeDefinition>())
            {
                if (string.IsNullOrEmpty(recipe.recipeID))
                {
                    Debug.LogWarning($"Recipe '{recipe.name}' has no ID");
                    issues++;
                }
                if (recipe.ingredients == null || recipe.ingredients.Length == 0)
                {
                    Debug.LogWarning($"Recipe '{recipe.recipeName}' has no ingredients");
                    issues++;
                }
                if (recipe.outputs == null || recipe.outputs.Length == 0)
                {
                    Debug.LogWarning($"Recipe '{recipe.recipeName}' has no outputs");
                    issues++;
                }
            }

            if (issues == 0)
            {
                Debug.Log("✓ All recipes validated successfully!");
                EditorUtility.DisplayDialog("Validation Complete", "All recipes are valid!", "Great!");
            }
            else
            {
                Debug.LogError($"✗ Found {issues} validation issues");
                EditorUtility.DisplayDialog("Validation Failed", $"Found {issues} issues. Check console for details.", "OK");
            }
        }

        private void ExportToJSON()
        {
            string path = EditorUtility.SaveFilePanel("Export Database", "", "inventory_database.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                var data = new
                {
                    items = tool.cachedItems,
                    recipes = tool.cachedRecipes,
                    exportDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(path, json);

                Debug.Log($"Database exported to: {path}");
                EditorUtility.DisplayDialog("Export Complete", "Database exported successfully!", "OK");
            }
        }

        public void Refresh()
        {
            // Refresh database manager if needed
        }
    }
}