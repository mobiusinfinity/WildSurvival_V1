//using UnityEngine;
//using UnityEditor;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.IO;

//// ==========================================================================================
//// PART 1: DATA STRUCTURES - These are the core data classes
//// Place these in Assets/_WildSurvival/Code/Runtime/Data/
//// ==========================================================================================

//[System.Serializable]
//public class SimpleItemData : ScriptableObject
//{
//    public string itemID = "";
//    public string displayName = "";
//    public string description = "";
//    public string icon = "📦"; // Unicode icon as placeholder
//    public ItemCategory category = ItemCategory.Misc;
//    public float weight = 1f;
//    public int maxStackSize = 1;
//    public Vector2Int gridSize = Vector2Int.one;
//    public int baseValue = 10;
//    public bool isUsable = false;
//    public string useEffect = ""; // What happens when used
//}

//[System.Serializable]
//public class SimpleRecipeData : ScriptableObject
//{
//    public string recipeID = "";
//    public string recipeName = "";
//    public string description = "";
//    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
//    public string outputItemID = "";
//    public int outputQuantity = 1;
//    public float craftTime = 3f;
//}

//[System.Serializable]
//public class RecipeIngredient
//{
//    public string itemID;
//    public int quantity;
//}

//[System.Serializable]
//public class SimpleItemDatabase : ScriptableObject
//{
//    public List<SimpleItemData> items = new List<SimpleItemData>();

//    public SimpleItemData GetItem(string itemID)
//    {
//        return items.FirstOrDefault(i => i.itemID == itemID);
//    }
//}

//[System.Serializable]
//public class SimpleRecipeDatabase : ScriptableObject
//{
//    public List<SimpleRecipeData> recipes = new List<SimpleRecipeData>();

//    public List<SimpleRecipeData> GetCraftableRecipes(Dictionary<string, int> inventory)
//    {
//        var craftable = new List<SimpleRecipeData>();
//        foreach (var recipe in recipes)
//        {
//            bool canCraft = true;
//            foreach (var ingredient in recipe.ingredients)
//            {
//                if (!inventory.ContainsKey(ingredient.itemID) ||
//                    inventory[ingredient.itemID] < ingredient.quantity)
//                {
//                    canCraft = false;
//                    break;
//                }
//            }
//            if (canCraft) craftable.Add(recipe);
//        }
//        return craftable;
//    }
//}

//public enum ItemCategory
//{
//    Misc, Resource, Tool, Weapon, Food, Material
//}

//// ==========================================================================================
//// PART 2: INVENTORY GRID SYSTEM - The core Tetris-style inventory
//// This is the heart of the inventory system
//// ==========================================================================================

//[System.Serializable]
//public class InventorySlot
//{
//    public SimpleItemData item;
//    public int quantity;
//    public Vector2Int position;
//    public string uniqueID;

//    public InventorySlot(SimpleItemData item, Vector2Int pos, int qty = 1)
//    {
//        this.item = item;
//        this.position = pos;
//        this.quantity = qty;
//        this.uniqueID = System.Guid.NewGuid().ToString();
//    }
//}

//public class InventoryGrid
//{
//    public int width = 8;
//    public int height = 10;
//    public bool[,] occupancy;
//    public List<InventorySlot> items = new List<InventorySlot>();

//    public InventoryGrid(int w, int h)
//    {
//        width = w;
//        height = h;
//        occupancy = new bool[width, height];
//    }

//    public bool CanPlaceItem(Vector2Int position, Vector2Int size, string excludeID = null)
//    {
//        // Check bounds
//        if (position.x < 0 || position.y < 0 ||
//            position.x + size.x > width ||
//            position.y + size.y > height)
//            return false;

//        // Check occupancy
//        for (int x = position.x; x < position.x + size.x; x++)
//        {
//            for (int y = position.y; y < position.y + size.y; y++)
//            {
//                if (occupancy[x, y])
//                {
//                    // Check if it's occupied by a different item
//                    var occupyingItem = GetItemAt(new Vector2Int(x, y));
//                    if (occupyingItem != null && occupyingItem.uniqueID != excludeID)
//                        return false;
//                }
//            }
//        }
//        return true;
//    }

//    public bool PlaceItem(InventorySlot slot)
//    {
//        if (!CanPlaceItem(slot.position, slot.item.gridSize, slot.uniqueID))
//            return false;

//        // Mark cells as occupied
//        for (int x = slot.position.x; x < slot.position.x + slot.item.gridSize.x; x++)
//        {
//            for (int y = slot.position.y; y < slot.position.y + slot.item.gridSize.y; y++)
//            {
//                occupancy[x, y] = true;
//            }
//        }

//        if (!items.Contains(slot))
//            items.Add(slot);

//        return true;
//    }

//    public void RemoveItem(InventorySlot slot)
//    {
//        // Clear occupancy
//        for (int x = slot.position.x; x < slot.position.x + slot.item.gridSize.x; x++)
//        {
//            for (int y = slot.position.y; y < slot.position.y + slot.item.gridSize.y; y++)
//            {
//                if (x >= 0 && x < width && y >= 0 && y < height)
//                    occupancy[x, y] = false;
//            }
//        }
//        items.Remove(slot);
//    }

//    public InventorySlot GetItemAt(Vector2Int pos)
//    {
//        foreach (var slot in items)
//        {
//            if (pos.x >= slot.position.x &&
//                pos.x < slot.position.x + slot.item.gridSize.x &&
//                pos.y >= slot.position.y &&
//                pos.y < slot.position.y + slot.item.gridSize.y)
//            {
//                return slot;
//            }
//        }
//        return null;
//    }
//}

//// ==========================================================================================
//// PART 3: MAIN INVENTORY TOOL WINDOW
//// This is the main editor window that ties everything together
//// Place in Assets/_WildSurvival/Code/Editor/Tools/
//// ==========================================================================================

//public class WorkingInventoryToolHub : EditorWindow
//{
//    // Grid settings
//    private const int CELL_SIZE = 50;
//    private InventoryGrid inventory;

//    // Databases
//    private SimpleItemDatabase itemDatabase;
//    private SimpleRecipeDatabase recipeDatabase;

//    // UI State
//    private Vector2 scrollPos;
//    private InventorySlot draggedItem;
//    private Vector2Int dragOffset;
//    private bool isDragging;
//    private InventorySlot selectedItem;
//    private SimpleRecipeData selectedRecipe;
//    private string statusMessage = "Ready";

//    // Tabs
//    private int currentTab = 0;
//    private string[] tabNames = { "Inventory", "Items", "Crafting", "Database Setup" };

//    [MenuItem("Tools/Wild Survival/Working Inventory System")]
//    public static void ShowWindow()
//    {
//        var window = GetWindow<WorkingInventoryToolHub>();
//        window.titleContent = new GUIContent("🎮 Inventory System");
//        window.minSize = new Vector2(800, 600);
//        window.Show();
//    }

//    private void OnEnable()
//    {
//        LoadDatabases();
//        InitializeInventory();
//    }

//    private void LoadDatabases()
//    {
//        // Try to load existing databases
//        string[] itemDbGuids = AssetDatabase.FindAssets("t:SimpleItemDatabase");
//        if (itemDbGuids.Length > 0)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(itemDbGuids[0]);
//            itemDatabase = AssetDatabase.LoadAssetAtPath<SimpleItemDatabase>(path);
//        }

//        string[] recipeDbGuids = AssetDatabase.FindAssets("t:SimpleRecipeDatabase");
//        if (recipeDbGuids.Length > 0)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(recipeDbGuids[0]);
//            recipeDatabase = AssetDatabase.LoadAssetAtPath<SimpleRecipeDatabase>(path);
//        }
//    }

//    private void InitializeInventory()
//    {
//        inventory = new InventoryGrid(8, 10);
//    }

//    private void OnGUI()
//    {
//        DrawHeader();
//        DrawTabs();

//        EditorGUILayout.Space(10);

//        switch (currentTab)
//        {
//            case 0: DrawInventoryTab(); break;
//            case 1: DrawItemsTab(); break;
//            case 2: DrawCraftingTab(); break;
//            case 3: DrawDatabaseSetupTab(); break;
//        }

//        DrawStatusBar();
//    }

//    private void DrawHeader()
//    {
//        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
//        EditorGUILayout.LabelField("Working Inventory System", EditorStyles.boldLabel);

//        if (GUILayout.Button("Clear Inventory", EditorStyles.toolbarButton))
//        {
//            InitializeInventory();
//            statusMessage = "Inventory cleared";
//        }

//        if (GUILayout.Button("Add Random Items", EditorStyles.toolbarButton))
//        {
//            AddRandomItems();
//        }

//        EditorGUILayout.EndHorizontal();
//    }

//    private void DrawTabs()
//    {
//        currentTab = GUILayout.Toolbar(currentTab, tabNames);
//    }

//    // ==========================================================================================
//    // INVENTORY TAB - The main grid view where items can be dragged
//    // ==========================================================================================

//    private void DrawInventoryTab()
//    {
//        EditorGUILayout.BeginHorizontal();

//        // Left side - Inventory Grid
//        DrawInventoryGrid();

//        // Right side - Item Details
//        DrawItemDetails();

//        EditorGUILayout.EndHorizontal();
//    }

//    private void DrawInventoryGrid()
//    {
//        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(inventory.width * CELL_SIZE + 20));

//        EditorGUILayout.LabelField("Inventory Grid", EditorStyles.boldLabel);

//        Rect gridRect = GUILayoutUtility.GetRect(
//            inventory.width * CELL_SIZE,
//            inventory.height * CELL_SIZE
//        );

//        // Draw grid background
//        EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

//        // Draw grid cells
//        for (int x = 0; x < inventory.width; x++)
//        {
//            for (int y = 0; y < inventory.height; y++)
//            {
//                Rect cellRect = new Rect(
//                    gridRect.x + x * CELL_SIZE,
//                    gridRect.y + y * CELL_SIZE,
//                    CELL_SIZE,
//                    CELL_SIZE
//                );

//                // Draw cell
//                Color cellColor = inventory.occupancy[x, y] ?
//                    new Color(0.3f, 0.3f, 0.5f) :
//                    new Color(0.25f, 0.25f, 0.25f);
//                EditorGUI.DrawRect(cellRect, cellColor);

//                // Draw border
//                GUI.Box(cellRect, "");
//            }
//        }

//        // Draw items
//        foreach (var slot in inventory.items)
//        {
//            DrawInventoryItem(slot, gridRect);
//        }

//        // Handle dragging
//        HandleDragAndDrop(gridRect);

//        EditorGUILayout.EndVertical();
//    }

//    private void DrawInventoryItem(InventorySlot slot, Rect gridRect)
//    {
//        if (slot == null || slot.item == null) return;

//        Rect itemRect = new Rect(
//            gridRect.x + slot.position.x * CELL_SIZE,
//            gridRect.y + slot.position.y * CELL_SIZE,
//            slot.item.gridSize.x * CELL_SIZE,
//            slot.item.gridSize.y * CELL_SIZE
//        );

//        // Don't draw if being dragged
//        if (isDragging && draggedItem == slot)
//            return;

//        // Draw item background
//        Color itemColor = GetCategoryColor(slot.item.category);
//        EditorGUI.DrawRect(itemRect, itemColor);

//        // Draw item icon and name
//        GUIStyle centeredStyle = new GUIStyle(GUI.skin.label);
//        centeredStyle.alignment = TextAnchor.MiddleCenter;
//        centeredStyle.fontSize = 24;

//        // Unicode icon
//        GUI.Label(new Rect(itemRect.x, itemRect.y, itemRect.width, itemRect.height - 20),
//            slot.item.icon, centeredStyle);

//        // Item name
//        centeredStyle.fontSize = 10;
//        GUI.Label(new Rect(itemRect.x, itemRect.y + itemRect.height - 20, itemRect.width, 20),
//            slot.item.displayName, centeredStyle);

//        // Quantity badge
//        if (slot.quantity > 1)
//        {
//            GUI.Label(new Rect(itemRect.xMax - 20, itemRect.yMax - 20, 20, 20),
//                slot.quantity.ToString(), EditorStyles.boldLabel);
//        }

//        // Handle click
//        if (Event.current.type == EventType.MouseDown &&
//            itemRect.Contains(Event.current.mousePosition))
//        {
//            selectedItem = slot;
//            StartDragging(slot, Event.current.mousePosition, gridRect);
//            Event.current.Use();
//        }
//    }

//    private void HandleDragAndDrop(Rect gridRect)
//    {
//        Event e = Event.current;

//        if (isDragging && draggedItem != null)
//        {
//            // Calculate grid position
//            Vector2 mousePos = e.mousePosition;
//            Vector2Int gridPos = ScreenToGridPos(mousePos, gridRect);
//            gridPos -= dragOffset;

//            // Draw preview
//            bool canPlace = inventory.CanPlaceItem(gridPos, draggedItem.item.gridSize, draggedItem.uniqueID);

//            Rect previewRect = new Rect(
//                gridRect.x + gridPos.x * CELL_SIZE,
//                gridRect.y + gridPos.y * CELL_SIZE,
//                draggedItem.item.gridSize.x * CELL_SIZE,
//                draggedItem.item.gridSize.y * CELL_SIZE
//            );

//            Color previewColor = canPlace ?
//                new Color(0, 1, 0, 0.3f) :
//                new Color(1, 0, 0, 0.3f);
//            EditorGUI.DrawRect(previewRect, previewColor);

//            // Draw dragged item at mouse
//            GUIStyle style = new GUIStyle(GUI.skin.label);
//            style.fontSize = 24;
//            style.alignment = TextAnchor.MiddleCenter;
//            GUI.Label(new Rect(mousePos.x - 20, mousePos.y - 20, 40, 40),
//                draggedItem.item.icon, style);

//            // Handle drop
//            if (e.type == EventType.MouseUp)
//            {
//                if (canPlace)
//                {
//                    inventory.RemoveItem(draggedItem);
//                    draggedItem.position = gridPos;
//                    inventory.PlaceItem(draggedItem);
//                    statusMessage = $"Moved {draggedItem.item.displayName}";
//                }

//                isDragging = false;
//                draggedItem = null;
//                e.Use();
//            }

//            Repaint();
//        }
//    }

//    private void StartDragging(InventorySlot slot, Vector2 mousePos, Rect gridRect)
//    {
//        isDragging = true;
//        draggedItem = slot;
//        Vector2Int gridPos = ScreenToGridPos(mousePos, gridRect);
//        dragOffset = gridPos - slot.position;
//    }

//    private Vector2Int ScreenToGridPos(Vector2 screenPos, Rect gridRect)
//    {
//        Vector2 localPos = screenPos - new Vector2(gridRect.x, gridRect.y);
//        int x = Mathf.FloorToInt(localPos.x / CELL_SIZE);
//        int y = Mathf.FloorToInt(localPos.y / CELL_SIZE);
//        return new Vector2Int(x, y);
//    }

//    private void DrawItemDetails()
//    {
//        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250));

//        EditorGUILayout.LabelField("Item Details", EditorStyles.boldLabel);

//        if (selectedItem != null && selectedItem.item != null)
//        {
//            EditorGUILayout.Space(10);

//            // Icon
//            GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
//            iconStyle.fontSize = 48;
//            iconStyle.alignment = TextAnchor.MiddleCenter;
//            GUILayout.Label(selectedItem.item.icon, iconStyle, GUILayout.Height(60));

//            EditorGUILayout.LabelField("Name:", selectedItem.item.displayName);
//            EditorGUILayout.LabelField("Category:", selectedItem.item.category.ToString());
//            EditorGUILayout.LabelField("Weight:", $"{selectedItem.item.weight} kg");
//            EditorGUILayout.LabelField("Value:", $"{selectedItem.item.baseValue} gold");
//            EditorGUILayout.LabelField("Stack:", $"{selectedItem.quantity}/{selectedItem.item.maxStackSize}");

//            EditorGUILayout.Space(10);
//            EditorGUILayout.LabelField("Description:");
//            EditorGUILayout.TextArea(selectedItem.item.description, GUILayout.Height(60));

//            EditorGUILayout.Space(10);

//            // Actions
//            if (selectedItem.item.isUsable)
//            {
//                if (GUILayout.Button("Use Item", GUILayout.Height(30)))
//                {
//                    UseItem(selectedItem);
//                }
//            }

//            if (GUILayout.Button("Drop Item", GUILayout.Height(25)))
//            {
//                DropItem(selectedItem);
//            }

//            if (GUILayout.Button("Inspect", GUILayout.Height(25)))
//            {
//                InspectItem(selectedItem);
//            }
//        }
//        else
//        {
//            EditorGUILayout.HelpBox("Select an item to view details", MessageType.Info);
//        }

//        EditorGUILayout.EndVertical();
//    }

//    // ==========================================================================================
//    // ITEMS TAB - Browse available items
//    // ==========================================================================================

//    private void DrawItemsTab()
//    {
//        if (itemDatabase == null)
//        {
//            EditorGUILayout.HelpBox("No item database found. Go to Database Setup tab.", MessageType.Warning);
//            return;
//        }

//        EditorGUILayout.BeginHorizontal();

//        // Item list
//        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
//        EditorGUILayout.LabelField("Available Items", EditorStyles.boldLabel);

//        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

//        foreach (var item in itemDatabase.items)
//        {
//            EditorGUILayout.BeginHorizontal(GUI.skin.box);

//            GUILayout.Label(item.icon, GUILayout.Width(30));
//            EditorGUILayout.BeginVertical();
//            EditorGUILayout.LabelField(item.displayName, EditorStyles.boldLabel);
//            EditorGUILayout.LabelField($"Size: {item.gridSize.x}x{item.gridSize.y} | Weight: {item.weight}kg",
//                EditorStyles.miniLabel);
//            EditorGUILayout.EndVertical();

//            if (GUILayout.Button("Add", GUILayout.Width(40)))
//            {
//                AddItemToInventory(item);
//            }

//            EditorGUILayout.EndHorizontal();
//        }

//        EditorGUILayout.EndScrollView();
//        EditorGUILayout.EndVertical();

//        EditorGUILayout.EndHorizontal();
//    }

//    // ==========================================================================================
//    // CRAFTING TAB - Recipe system
//    // ==========================================================================================

//    private void DrawCraftingTab()
//    {
//        if (recipeDatabase == null)
//        {
//            EditorGUILayout.HelpBox("No recipe database found. Go to Database Setup tab.", MessageType.Warning);
//            return;
//        }

//        EditorGUILayout.BeginHorizontal();

//        // Recipe list
//        DrawRecipeList();

//        // Crafting interface
//        DrawCraftingInterface();

//        EditorGUILayout.EndHorizontal();
//    }

//    private void DrawRecipeList()
//    {
//        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
//        EditorGUILayout.LabelField("Recipes", EditorStyles.boldLabel);

//        var playerInventory = GetInventoryContents();
//        var craftableRecipes = recipeDatabase.GetCraftableRecipes(playerInventory);

//        EditorGUILayout.BeginScrollView(Vector2.zero);

//        foreach (var recipe in recipeDatabase.recipes)
//        {
//            bool canCraft = craftableRecipes.Contains(recipe);

//            GUI.enabled = canCraft;

//            EditorGUILayout.BeginVertical(GUI.skin.box);

//            EditorGUILayout.LabelField(recipe.recipeName,
//                canCraft ? EditorStyles.boldLabel : EditorStyles.label);

//            // Show ingredients
//            foreach (var ingredient in recipe.ingredients)
//            {
//                var item = itemDatabase.GetItem(ingredient.itemID);
//                if (item != null)
//                {
//                    int have = playerInventory.ContainsKey(ingredient.itemID) ?
//                        playerInventory[ingredient.itemID] : 0;

//                    Color color = have >= ingredient.quantity ? Color.green : Color.red;
//                    GUI.color = color;
//                    EditorGUILayout.LabelField($"  {item.icon} {item.displayName}: {have}/{ingredient.quantity}",
//                        EditorStyles.miniLabel);
//                    GUI.color = Color.white;
//                }
//            }

//            if (GUILayout.Button("Select Recipe"))
//            {
//                selectedRecipe = recipe;
//            }

//            EditorGUILayout.EndVertical();

//            GUI.enabled = true;
//        }

//        EditorGUILayout.EndScrollView();
//        EditorGUILayout.EndVertical();
//    }

//    private void DrawCraftingInterface()
//    {
//        EditorGUILayout.BeginVertical(GUI.skin.box);
//        EditorGUILayout.LabelField("Crafting", EditorStyles.boldLabel);

//        if (selectedRecipe != null)
//        {
//            EditorGUILayout.LabelField($"Recipe: {selectedRecipe.recipeName}");
//            EditorGUILayout.TextArea(selectedRecipe.description, GUILayout.Height(40));

//            var outputItem = itemDatabase.GetItem(selectedRecipe.outputItemID);
//            if (outputItem != null)
//            {
//                EditorGUILayout.LabelField($"Output: {outputItem.icon} {outputItem.displayName} x{selectedRecipe.outputQuantity}");
//            }

//            EditorGUILayout.Space(10);

//            if (GUILayout.Button("Craft Item", GUILayout.Height(40)))
//            {
//                CraftItem(selectedRecipe);
//            }
//        }
//        else
//        {
//            EditorGUILayout.HelpBox("Select a recipe to craft", MessageType.Info);
//        }

//        EditorGUILayout.EndVertical();
//    }

//    // ==========================================================================================
//    // DATABASE SETUP TAB - Create and manage test data
//    // ==========================================================================================

//    private void DrawDatabaseSetupTab()
//    {
//        EditorGUILayout.LabelField("Database Setup", EditorStyles.boldLabel);

//        EditorGUILayout.Space(10);

//        // Item Database
//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.LabelField("Item Database:", GUILayout.Width(100));
//        itemDatabase = (SimpleItemDatabase)EditorGUILayout.ObjectField(itemDatabase, typeof(SimpleItemDatabase), false);

//        if (itemDatabase == null)
//        {
//            if (GUILayout.Button("Create", GUILayout.Width(60)))
//            {
//                CreateItemDatabase();
//            }
//        }
//        else
//        {
//            if (GUILayout.Button("Generate Test Items", GUILayout.Width(120)))
//            {
//                GenerateTestItems();
//            }
//        }
//        EditorGUILayout.EndHorizontal();

//        // Recipe Database
//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.LabelField("Recipe Database:", GUILayout.Width(100));
//        recipeDatabase = (SimpleRecipeDatabase)EditorGUILayout.ObjectField(recipeDatabase, typeof(SimpleRecipeDatabase), false);

//        if (recipeDatabase == null)
//        {
//            if (GUILayout.Button("Create", GUILayout.Width(60)))
//            {
//                CreateRecipeDatabase();
//            }
//        }
//        else
//        {
//            if (GUILayout.Button("Generate Test Recipes", GUILayout.Width(120)))
//            {
//                GenerateTestRecipes();
//            }
//        }
//        EditorGUILayout.EndHorizontal();

//        EditorGUILayout.Space(20);

//        // Quick setup
//        if (GUILayout.Button("Quick Setup Everything", GUILayout.Height(40)))
//        {
//            QuickSetup();
//        }

//        EditorGUILayout.Space(10);

//        // Display current items
//        if (itemDatabase != null && itemDatabase.items.Count > 0)
//        {
//            EditorGUILayout.LabelField($"Items in database: {itemDatabase.items.Count}", EditorStyles.boldLabel);

//            EditorGUILayout.BeginVertical(GUI.skin.box);
//            foreach (var item in itemDatabase.items.Take(5))
//            {
//                EditorGUILayout.LabelField($"{item.icon} {item.displayName} ({item.itemID})");
//            }
//            if (itemDatabase.items.Count > 5)
//            {
//                EditorGUILayout.LabelField($"... and {itemDatabase.items.Count - 5} more");
//            }
//            EditorGUILayout.EndVertical();
//        }
//    }

//    // ==========================================================================================
//    // HELPER METHODS
//    // ==========================================================================================

//    private void CreateItemDatabase()
//    {
//        var db = ScriptableObject.CreateInstance<SimpleItemDatabase>();
//        string path = "Assets/_WildSurvival/Data/TestItemDatabase.asset";

//        // Ensure directory exists
//        string dir = Path.GetDirectoryName(path);
//        if (!Directory.Exists(dir))
//        {
//            Directory.CreateDirectory(dir);
//        }

//        AssetDatabase.CreateAsset(db, path);
//        AssetDatabase.SaveAssets();
//        itemDatabase = db;

//        statusMessage = "Item database created";
//    }

//    private void CreateRecipeDatabase()
//    {
//        var db = ScriptableObject.CreateInstance<SimpleRecipeDatabase>();
//        string path = "Assets/_WildSurvival/Data/TestRecipeDatabase.asset";

//        // Ensure directory exists
//        string dir = Path.GetDirectoryName(path);
//        if (!Directory.Exists(dir))
//        {
//            Directory.CreateDirectory(dir);
//        }

//        AssetDatabase.CreateAsset(db, path);
//        AssetDatabase.SaveAssets();
//        recipeDatabase = db;

//        statusMessage = "Recipe database created";
//    }

//    private void GenerateTestItems()
//    {
//        if (itemDatabase == null) return;

//        itemDatabase.items.Clear();

//        // Resources
//        AddTestItem("wood", "Wood", "🪵", ItemCategory.Resource, 0.5f, 20, 1, 1, 5);
//        AddTestItem("stone", "Stone", "🪨", ItemCategory.Resource, 1f, 20, 1, 1, 3);
//        AddTestItem("iron_ore", "Iron Ore", "⛏️", ItemCategory.Resource, 2f, 10, 1, 1, 10);
//        AddTestItem("fiber", "Fiber", "🧵", ItemCategory.Resource, 0.1f, 50, 1, 1, 2);
//        AddTestItem("leather", "Leather", "🟫", ItemCategory.Material, 0.3f, 20, 1, 1, 8);

//        // Tools
//        AddTestItem("axe_stone", "Stone Axe", "🪓", ItemCategory.Tool, 2f, 1, 1, 2, 25, true, "Chop wood faster");
//        AddTestItem("pickaxe_stone", "Stone Pickaxe", "⛏️", ItemCategory.Tool, 2.5f, 1, 1, 2, 30, true, "Mine stone faster");
//        AddTestItem("knife", "Knife", "🔪", ItemCategory.Tool, 0.5f, 1, 1, 1, 20, true, "Cut and skin");
//        AddTestItem("hammer", "Hammer", "🔨", ItemCategory.Tool, 1.5f, 1, 1, 2, 35);

//        // Weapons
//        AddTestItem("sword_iron", "Iron Sword", "⚔️", ItemCategory.Weapon, 3f, 1, 1, 3, 100, true, "Deal damage");
//        AddTestItem("bow", "Bow", "🏹", ItemCategory.Weapon, 1f, 1, 1, 3, 50, true, "Ranged attacks");
//        AddTestItem("spear", "Spear", "🔱", ItemCategory.Weapon, 2f, 1, 1, 4, 40, true, "Long reach");

//        // Food
//        AddTestItem("apple", "Apple", "🍎", ItemCategory.Food, 0.2f, 10, 1, 1, 5, true, "Restore 10 health");
//        AddTestItem("bread", "Bread", "🍞", ItemCategory.Food, 0.3f, 5, 1, 1, 10, true, "Restore 20 health");
//        AddTestItem("meat_raw", "Raw Meat", "🥩", ItemCategory.Food, 0.5f, 10, 1, 1, 8);
//        AddTestItem("meat_cooked", "Cooked Meat", "🍖", ItemCategory.Food, 0.4f, 10, 1, 1, 15, true, "Restore 30 health");
//        AddTestItem("water", "Water", "💧", ItemCategory.Food, 1f, 5, 1, 1, 3, true, "Restore thirst");

//        // Materials
//        AddTestItem("rope", "Rope", "🪢", ItemCategory.Material, 0.5f, 10, 2, 1, 15);
//        AddTestItem("cloth", "Cloth", "🧻", ItemCategory.Material, 0.2f, 20, 2, 2, 12);
//        AddTestItem("iron_ingot", "Iron Ingot", "🔧", ItemCategory.Material, 1f, 20, 1, 1, 20);

//        // Misc
//        AddTestItem("torch", "Torch", "🔦", ItemCategory.Misc, 0.5f, 10, 1, 2, 5, true, "Provide light");
//        AddTestItem("bandage", "Bandage", "🩹", ItemCategory.Misc, 0.1f, 20, 1, 1, 10, true, "Heal wounds");
//        AddTestItem("map", "Map", "🗺️", ItemCategory.Misc, 0.1f, 1, 2, 2, 25, true, "Show location");
//        AddTestItem("compass", "Compass", "🧭", ItemCategory.Misc, 0.2f, 1, 1, 1, 30, true, "Show direction");
//        AddTestItem("backpack", "Backpack", "🎒", ItemCategory.Misc, 1f, 1, 2, 3, 50, true, "Increase inventory");

//        EditorUtility.SetDirty(itemDatabase);
//        AssetDatabase.SaveAssets();

//        statusMessage = $"Generated {itemDatabase.items.Count} test items";
//    }

//    private void AddTestItem(string id, string name, string icon, ItemCategory category,
//        float weight, int maxStack, int gridWidth, int gridHeight, int value,
//        bool usable = false, string effect = "")
//    {
//        var item = ScriptableObject.CreateInstance<SimpleItemData>();
//        item.itemID = id;
//        item.displayName = name;
//        item.icon = icon;
//        item.category = category;
//        item.weight = weight;
//        item.maxStackSize = maxStack;
//        item.gridSize = new Vector2Int(gridWidth, gridHeight);
//        item.baseValue = value;
//        item.isUsable = usable;
//        item.useEffect = effect;
//        item.description = $"A {name.ToLower()} item. {effect}";

//        itemDatabase.items.Add(item);

//        // Save as asset
//        string path = $"Assets/_WildSurvival/Data/Items/{id}.asset";
//        string dir = Path.GetDirectoryName(path);
//        if (!Directory.Exists(dir))
//        {
//            Directory.CreateDirectory(dir);
//        }
//        AssetDatabase.CreateAsset(item, path);
//    }

//    private void GenerateTestRecipes()
//    {
//        if (recipeDatabase == null || itemDatabase == null) return;

//        recipeDatabase.recipes.Clear();

//        // Basic recipes
//        AddTestRecipe("recipe_torch", "Craft Torch", "wood", 1, "fiber", 1, "torch", 2, 2f);
//        AddTestRecipe("recipe_rope", "Craft Rope", "fiber", 3, null, 0, "rope", 1, 3f);
//        AddTestRecipe("recipe_bandage", "Craft Bandage", "cloth", 1, "fiber", 1, "bandage", 2, 2f);

//        // Tool recipes
//        AddTestRecipe("recipe_stone_axe", "Craft Stone Axe", "wood", 2, "stone", 3, "axe_stone", 1, 5f);
//        AddTestRecipe("recipe_stone_pickaxe", "Craft Stone Pickaxe", "wood", 2, "stone", 3, "pickaxe_stone", 1, 5f);
//        AddTestRecipe("recipe_knife", "Craft Knife", "stone", 1, "fiber", 1, "knife", 1, 3f);

//        // Weapon recipes
//        AddTestRecipe("recipe_bow", "Craft Bow", "wood", 3, "rope", 1, "bow", 1, 8f);
//        AddTestRecipe("recipe_spear", "Craft Spear", "wood", 2, "stone", 1, "spear", 1, 4f);

//        // Food recipes
//        AddTestRecipe("recipe_bread", "Bake Bread", "fiber", 3, null, 0, "bread", 1, 5f);
//        AddTestRecipe("recipe_cooked_meat", "Cook Meat", "meat_raw", 1, null, 0, "meat_cooked", 1, 3f);

//        EditorUtility.SetDirty(recipeDatabase);
//        AssetDatabase.SaveAssets();

//        statusMessage = $"Generated {recipeDatabase.recipes.Count} test recipes";
//    }

//    private void AddTestRecipe(string id, string name, string input1, int qty1,
//        string input2, int qty2, string output, int outputQty, float craftTime)
//    {
//        var recipe = ScriptableObject.CreateInstance<SimpleRecipeData>();
//        recipe.recipeID = id;
//        recipe.recipeName = name;
//        recipe.description = $"Recipe to craft {output}";
//        recipe.outputItemID = output;
//        recipe.outputQuantity = outputQty;
//        recipe.craftTime = craftTime;

//        recipe.ingredients.Add(new RecipeIngredient { itemID = input1, quantity = qty1 });
//        if (!string.IsNullOrEmpty(input2))
//        {
//            recipe.ingredients.Add(new RecipeIngredient { itemID = input2, quantity = qty2 });
//        }

//        recipeDatabase.recipes.Add(recipe);

//        // Save as asset
//        string path = $"Assets/_WildSurvival/Data/Recipes/{id}.asset";
//        string dir = Path.GetDirectoryName(path);
//        if (!Directory.Exists(dir))
//        {
//            Directory.CreateDirectory(dir);
//        }
//        AssetDatabase.CreateAsset(recipe, path);
//    }

//    private void QuickSetup()
//    {
//        // Create databases if needed
//        if (itemDatabase == null)
//        {
//            CreateItemDatabase();
//        }

//        if (recipeDatabase == null)
//        {
//            CreateRecipeDatabase();
//        }

//        // Generate test data
//        GenerateTestItems();
//        GenerateTestRecipes();

//        // Add some items to inventory
//        AddRandomItems();

//        statusMessage = "Quick setup complete!";
//    }

//    private void AddRandomItems()
//    {
//        if (itemDatabase == null || itemDatabase.items.Count == 0) return;

//        // Add 5-10 random items
//        int count = UnityEngine.Random.Range(5, 10);

//        for (int i = 0; i < count; i++)
//        {
//            var item = itemDatabase.items[UnityEngine.Random.Range(0, itemDatabase.items.Count)];
//            AddItemToInventory(item);
//        }

//        statusMessage = $"Added {count} random items";
//    }

//    private void AddItemToInventory(SimpleItemData item)
//    {
//        // Find a valid position
//        for (int y = 0; y < inventory.height - item.gridSize.y + 1; y++)
//        {
//            for (int x = 0; x < inventory.width - item.gridSize.x + 1; x++)
//            {
//                Vector2Int pos = new Vector2Int(x, y);
//                if (inventory.CanPlaceItem(pos, item.gridSize))
//                {
//                    var slot = new InventorySlot(item, pos, 1);
//                    inventory.PlaceItem(slot);
//                    statusMessage = $"Added {item.displayName}";
//                    return;
//                }
//            }
//        }

//        statusMessage = "No space in inventory!";
//    }

//    private Dictionary<string, int> GetInventoryContents()
//    {
//        var contents = new Dictionary<string, int>();

//        foreach (var slot in inventory.items)
//        {
//            if (contents.ContainsKey(slot.item.itemID))
//            {
//                contents[slot.item.itemID] += slot.quantity;
//            }
//            else
//            {
//                contents[slot.item.itemID] = slot.quantity;
//            }
//        }

//        return contents;
//    }

//    private void CraftItem(SimpleRecipeData recipe)
//    {
//        // Check materials
//        var playerInventory = GetInventoryContents();

//        foreach (var ingredient in recipe.ingredients)
//        {
//            if (!playerInventory.ContainsKey(ingredient.itemID) ||
//                playerInventory[ingredient.itemID] < ingredient.quantity)
//            {
//                statusMessage = "Not enough materials!";
//                return;
//            }
//        }

//        // Remove ingredients
//        foreach (var ingredient in recipe.ingredients)
//        {
//            int toRemove = ingredient.quantity;

//            for (int i = inventory.items.Count - 1; i >= 0 && toRemove > 0; i--)
//            {
//                var slot = inventory.items[i];
//                if (slot.item.itemID == ingredient.itemID)
//                {
//                    if (slot.quantity <= toRemove)
//                    {
//                        toRemove -= slot.quantity;
//                        inventory.RemoveItem(slot);
//                    }
//                    else
//                    {
//                        slot.quantity -= toRemove;
//                        toRemove = 0;
//                    }
//                }
//            }
//        }

//        // Add output
//        var outputItem = itemDatabase.GetItem(recipe.outputItemID);
//        if (outputItem != null)
//        {
//            for (int i = 0; i < recipe.outputQuantity; i++)
//            {
//                AddItemToInventory(outputItem);
//            }
//        }

//        statusMessage = $"Crafted {recipe.outputQuantity}x {outputItem.displayName}!";
//    }

//    private void UseItem(InventorySlot slot)
//    {
//        statusMessage = $"Used {slot.item.displayName}: {slot.item.useEffect}";

//        slot.quantity--;
//        if (slot.quantity <= 0)
//        {
//            inventory.RemoveItem(slot);
//            selectedItem = null;
//        }
//    }

//    private void DropItem(InventorySlot slot)
//    {
//        inventory.RemoveItem(slot);
//        selectedItem = null;
//        statusMessage = $"Dropped {slot.item.displayName}";
//    }

//    private void InspectItem(InventorySlot slot)
//    {
//        statusMessage = $"Inspecting {slot.item.displayName}: {slot.item.description}";
//    }

//    private Color GetCategoryColor(ItemCategory category)
//    {
//        switch (category)
//        {
//            case ItemCategory.Resource: return new Color(0.6f, 0.4f, 0.2f, 0.8f);
//            case ItemCategory.Tool: return new Color(0.4f, 0.4f, 0.6f, 0.8f);
//            case ItemCategory.Weapon: return new Color(0.6f, 0.2f, 0.2f, 0.8f);
//            case ItemCategory.Food: return new Color(0.2f, 0.6f, 0.2f, 0.8f);
//            case ItemCategory.Material: return new Color(0.5f, 0.5f, 0.3f, 0.8f);
//            default: return new Color(0.4f, 0.4f, 0.4f, 0.8f);
//        }
//    }

//    private void DrawStatusBar()
//    {
//        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
//        EditorGUILayout.LabelField(statusMessage);
//        EditorGUILayout.EndHorizontal();
//    }
//}

//// ==========================================================================================
//// INSTALLATION INSTRUCTIONS:
//// 1. Create folder structure: Assets/_WildSurvival/Code/Editor/Tools/
//// 2. Create folder: Assets/_WildSurvival/Data/
//// 3. Copy this entire script into a file called WorkingInventorySystem.cs
//// 4. Place it in the Editor/Tools folder
//// 5. Open Unity and wait for compilation
//// 6. Access via: Tools → Wild Survival → Working Inventory System
//// 7. Click "Quick Setup Everything" button to create test data
//// 8. Start testing the inventory system!
////
//// FEATURES INCLUDED:
//// - Drag and drop items in grid
//// - Visual feedback (green/red preview)
//// - Item details panel
//// - Use/Drop/Inspect items
//// - Crafting system with recipes
//// - Test data generation (25 items, 10 recipes)
//// - Unicode icons as placeholders
//// - Category-based coloring
//// - Stack support
//// - Database management
//// ==========================================================================================