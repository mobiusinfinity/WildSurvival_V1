using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Ultimate Batch Data Processor for Wild Survival
/// Handles mass import/export of items and recipes via JSON
/// Designed for AI agent integration and bulk operations
/// </summary>
public class UltimateBatchProcessor : EditorWindow
{
    // ========== CONFIGURATION ==========
    private const string TOOL_NAME = "Ultimate Batch Processor";
    private const string VERSION = "2.0";
    private const int MAX_BATCH_SIZE = 1000;

    // Database references
    private ItemDatabase itemDatabase;
    private RecipeDatabase recipeDatabase;

    // Import/Export settings
    private string importPath = "";
    private string exportPath = "";
    private bool validateOnImport = true;
    private bool overwriteExisting = false;
    private bool generateMissingAssets = true;

    // Batch operation state
    private List<ItemBatchData> pendingItems = new List<ItemBatchData>();
    private List<RecipeBatchData> pendingRecipes = new List<RecipeBatchData>();
    private int processedCount = 0;
    private List<string> importLog = new List<string>();

    // UI State
    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;
    private BatchMode currentMode = BatchMode.Import;

    private enum BatchMode
    {
        Import,
        Export,
        Generate,
        Validate,
        AIAssist
    }

    [MenuItem("Tools/Wild Survival/Ultimate Batch Processor")]
    public static void ShowWindow()
    {
        var window = GetWindow<UltimateBatchProcessor>(false, TOOL_NAME, true);
        window.minSize = new Vector2(600, 400);
    }

    private void OnEnable()
    {
        LoadDatabases();
    }

    private void LoadDatabases()
    {
        // Try to find databases
        string[] itemDbGuids = AssetDatabase.FindAssets("t:ItemDatabase");
        if (itemDbGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(itemDbGuids[0]);
            itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
        }

        string[] recipeDbGuids = AssetDatabase.FindAssets("t:RecipeDatabase");
        if (recipeDbGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(recipeDbGuids[0]);
            recipeDatabase = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
        }
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawDatabaseStatus();
        DrawModeSelector();

        EditorGUILayout.Space(10);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (currentMode)
        {
            case BatchMode.Import:
                DrawImportMode();
                break;
            case BatchMode.Export:
                DrawExportMode();
                break;
            case BatchMode.Generate:
                DrawGenerateMode();
                break;
            case BatchMode.Validate:
                DrawValidateMode();
                break;
            case BatchMode.AIAssist:
                DrawAIAssistMode();
                break;
        }

        EditorGUILayout.EndScrollView();

        DrawFooter();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"{TOOL_NAME} v{VERSION}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            LoadDatabases();
            importLog.Clear();
        }

        if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            showAdvancedOptions = !showAdvancedOptions;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawDatabaseStatus()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Item Database:", GUILayout.Width(100));

        if (itemDatabase != null)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"✓ Loaded ({itemDatabase.GetAllItems().Count} items)");
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("✗ Not Found");
            GUI.color = Color.white;

            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                CreateItemDatabase();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Recipe Database:", GUILayout.Width(100));

        if (recipeDatabase != null)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"✓ Loaded ({recipeDatabase.GetAllRecipes().Count} recipes)");
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("✗ Not Found");
            GUI.color = Color.white;

            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                CreateRecipeDatabase();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawModeSelector()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Toggle(currentMode == BatchMode.Import, "Import", EditorStyles.toolbarButton))
            currentMode = BatchMode.Import;

        if (GUILayout.Toggle(currentMode == BatchMode.Export, "Export", EditorStyles.toolbarButton))
            currentMode = BatchMode.Export;

        if (GUILayout.Toggle(currentMode == BatchMode.Generate, "Generate", EditorStyles.toolbarButton))
            currentMode = BatchMode.Generate;

        if (GUILayout.Toggle(currentMode == BatchMode.Validate, "Validate", EditorStyles.toolbarButton))
            currentMode = BatchMode.Validate;

        if (GUILayout.Toggle(currentMode == BatchMode.AIAssist, "AI Assist", EditorStyles.toolbarButton))
            currentMode = BatchMode.AIAssist;

        EditorGUILayout.EndHorizontal();
    }

    // ========== IMPORT MODE ==========
    private void DrawImportMode()
    {
        EditorGUILayout.LabelField("JSON Import", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Import items and recipes from JSON files. Supports batch operations from AI-generated data.", MessageType.Info);

        EditorGUILayout.Space();

        // File selection
        EditorGUILayout.BeginHorizontal();
        importPath = EditorGUILayout.TextField("Import File:", importPath);

        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select JSON file", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                importPath = path;
                AnalyzeImportFile();
            }
        }
        EditorGUILayout.EndHorizontal();

        // Import options
        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Import Options", EditorStyles.boldLabel);

            validateOnImport = EditorGUILayout.Toggle("Validate Data", validateOnImport);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
            generateMissingAssets = EditorGUILayout.Toggle("Generate Missing Assets", generateMissingAssets);

            EditorGUILayout.EndVertical();
        }

        // Preview
        if (pendingItems.Count > 0 || pendingRecipes.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Import Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Items to import: {pendingItems.Count}");
            EditorGUILayout.LabelField($"Recipes to import: {pendingRecipes.Count}");

            if (GUILayout.Button("Import All", GUILayout.Height(30)))
            {
                ExecuteBatchImport();
            }
        }

        // Import from clipboard
        EditorGUILayout.Space();
        if (GUILayout.Button("Import from Clipboard", GUILayout.Height(25)))
        {
            ImportFromClipboard();
        }

        // Log display
        DrawImportLog();
    }

    // ========== EXPORT MODE ==========
    private void DrawExportMode()
    {
        EditorGUILayout.LabelField("JSON Export", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Export database to JSON format for backup or external processing.", MessageType.Info);

        EditorGUILayout.Space();

        // Export options
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        bool exportItems = EditorGUILayout.Toggle("Export Items", true);
        bool exportRecipes = EditorGUILayout.Toggle("Export Recipes", true);
        bool includeMetadata = EditorGUILayout.Toggle("Include Metadata", true);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        if (GUILayout.Button("Export to File", GUILayout.Height(30)))
        {
            ExportToFile(exportItems, exportRecipes, includeMetadata);
        }

        if (GUILayout.Button("Export to Clipboard", GUILayout.Height(25)))
        {
            ExportToClipboard(exportItems, exportRecipes, includeMetadata);
        }

        // Export templates for AI
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AI Templates", EditorStyles.boldLabel);

        if (GUILayout.Button("Export Item Template", GUILayout.Height(25)))
        {
            ExportItemTemplate();
        }

        if (GUILayout.Button("Export Recipe Template", GUILayout.Height(25)))
        {
            ExportRecipeTemplate();
        }

        if (GUILayout.Button("Export Schema Documentation", GUILayout.Height(25)))
        {
            ExportSchemaDocumentation();
        }
    }

    // ========== GENERATE MODE ==========
    private void DrawGenerateMode()
    {
        EditorGUILayout.LabelField("Batch Generation", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Generate complete item and recipe sets based on templates.", MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Survival Essentials (50 items)", GUILayout.Height(30)))
        {
            GenerateSurvivalEssentials();
        }

        if (GUILayout.Button("Generate Crafting Progression (100 recipes)", GUILayout.Height(30)))
        {
            GenerateCraftingProgression();
        }

        if (GUILayout.Button("Generate Complete Database (250+ items)", GUILayout.Height(30)))
        {
            GenerateCompleteDatabase();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Custom Generation", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate from Template", GUILayout.Height(25)))
        {
            GenerateFromTemplate();
        }
    }

    // ========== AI ASSIST MODE ==========
    private void DrawAIAssistMode()
    {
        EditorGUILayout.LabelField("AI Assistant Integration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This mode helps you integrate with AI agents for content generation.\n" +
            "1. Export templates and schemas\n" +
            "2. Send to AI with your requirements\n" +
            "3. Import the generated JSON data",
            MessageType.Info
        );

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Step 1: Prepare for AI", EditorStyles.boldLabel);

        if (GUILayout.Button("Copy Item Generation Prompt", GUILayout.Height(25)))
        {
            CopyItemGenerationPrompt();
        }

        if (GUILayout.Button("Copy Recipe Generation Prompt", GUILayout.Height(25)))
        {
            CopyRecipeGenerationPrompt();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Step 2: Process AI Output", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate AI JSON from Clipboard", GUILayout.Height(25)))
        {
            ValidateAIJSON();
        }

        if (GUILayout.Button("Import AI Data from Clipboard", GUILayout.Height(30)))
        {
            ImportFromClipboard();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Batch Templates", EditorStyles.boldLabel);

        if (GUILayout.Button("Export 10-Item Template", GUILayout.Height(25)))
        {
            ExportBatchTemplate(10, 0);
        }

        if (GUILayout.Button("Export 50-Item Template", GUILayout.Height(25)))
        {
            ExportBatchTemplate(50, 0);
        }

        if (GUILayout.Button("Export 10-Recipe Template", GUILayout.Height(25)))
        {
            ExportBatchTemplate(0, 10);
        }
    }

    // ========== VALIDATE MODE ==========
    private void DrawValidateMode()
    {
        EditorGUILayout.LabelField("Database Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate All Items", GUILayout.Height(30)))
        {
            ValidateAllItems();
        }

        if (GUILayout.Button("Validate All Recipes", GUILayout.Height(30)))
        {
            ValidateAllRecipes();
        }

        if (GUILayout.Button("Check Missing Dependencies", GUILayout.Height(30)))
        {
            CheckMissingDependencies();
        }

        DrawImportLog();
    }

    // ========== IMPORT FUNCTIONS ==========

    private void AnalyzeImportFile()
    {
        if (!File.Exists(importPath))
        {
            Debug.LogError($"File not found: {importPath}");
            return;
        }

        try
        {
            string json = File.ReadAllText(importPath);
            var data = JObject.Parse(json);

            pendingItems.Clear();
            pendingRecipes.Clear();

            // Parse items
            if (data["items"] != null)
            {
                var items = data["items"].ToObject<List<ItemBatchData>>();
                pendingItems = items ?? new List<ItemBatchData>();
            }

            // Parse recipes
            if (data["recipes"] != null)
            {
                var recipes = data["recipes"].ToObject<List<RecipeBatchData>>();
                pendingRecipes = recipes ?? new List<RecipeBatchData>();
            }

            importLog.Add($"File analyzed: {pendingItems.Count} items, {pendingRecipes.Count} recipes found");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse JSON: {e.Message}");
            importLog.Add($"ERROR: {e.Message}");
        }
    }

    private void ImportFromClipboard()
    {
        string json = GUIUtility.systemCopyBuffer;

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("Clipboard is empty");
            return;
        }

        try
        {
            var data = JObject.Parse(json);

            pendingItems.Clear();
            pendingRecipes.Clear();

            // Parse items
            if (data["items"] != null)
            {
                var items = data["items"].ToObject<List<ItemBatchData>>();
                pendingItems = items ?? new List<ItemBatchData>();
            }

            // Parse recipes
            if (data["recipes"] != null)
            {
                var recipes = data["recipes"].ToObject<List<RecipeBatchData>>();
                pendingRecipes = recipes ?? new List<RecipeBatchData>();
            }

            importLog.Add($"Clipboard parsed: {pendingItems.Count} items, {pendingRecipes.Count} recipes");

            if (pendingItems.Count > 0 || pendingRecipes.Count > 0)
            {
                ExecuteBatchImport();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse clipboard JSON: {e.Message}");
            importLog.Add($"ERROR: {e.Message}");
        }
    }

    private void ExecuteBatchImport()
    {
        processedCount = 0;
        int errors = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            // Import items
            foreach (var itemData in pendingItems)
            {
                if (ImportItem(itemData))
                    processedCount++;
                else
                    errors++;
            }

            // Import recipes
            foreach (var recipeData in pendingRecipes)
            {
                if (ImportRecipe(recipeData))
                    processedCount++;
                else
                    errors++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        importLog.Add($"Import complete: {processedCount} successful, {errors} errors");

        // Clear pending
        pendingItems.Clear();
        pendingRecipes.Clear();

        ShowNotification(new GUIContent($"Imported {processedCount} items/recipes"));
    }

    private bool ImportItem(ItemBatchData data)
    {
        try
        {
            // Check if exists
            if (!overwriteExisting)
            {
                var existing = itemDatabase?.GetItem(data.itemID);
                if (existing != null)
                {
                    importLog.Add($"Skipped {data.itemID} - already exists");
                    return false;
                }
            }

            // Create ItemDefinition
            var item = ScriptableObject.CreateInstance<ItemDefinition>();

            // Map basic properties
            item.itemID = data.itemID;
            item.displayName = data.displayName;
            item.description = data.description;
            item.baseValue = data.value;
            item.weight = data.weight;
            item.maxStackSize = data.maxStackSize;

            // Map grid size
            item.gridSize = new Vector2Int(data.gridWidth, data.gridHeight);

            // Map category
            item.primaryCategory = ParseItemCategory(data.category);

            // Map tags
            if (data.tags != null)
            {
                item.tags = data.tags.Select(t => ParseItemTag(t)).ToArray();
            }

            // Durability
            item.hasDurability = data.hasDurability;
            item.maxDurability = data.maxDurability;

            // Generate missing assets if needed
            if (generateMissingAssets && item.icon == null)
            {
                // Could generate placeholder icon here
            }

            // Save asset
            string path = $"Assets/_WildSurvival/Data/Items/{data.itemID}.asset";
            EnsureDirectoryExists(Path.GetDirectoryName(path));

            AssetDatabase.CreateAsset(item, path);

            // Add to database
            if (itemDatabase != null)
            {
                itemDatabase.AddItem(item);
            }

            importLog.Add($"✓ Imported: {data.itemID}");
            return true;
        }
        catch (Exception e)
        {
            importLog.Add($"✗ Failed {data.itemID}: {e.Message}");
            return false;
        }
    }

    private bool ImportRecipe(RecipeBatchData data)
    {
        try
        {
            // Create RecipeDefinition
            var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();

            // Map properties
            recipe.recipeID = data.recipeID;
            recipe.recipeName = data.recipeName;
            recipe.description = data.description;
            recipe.category = data.category;
            recipe.baseCraftTime = data.craftTime;
            recipe.tier = data.tier;
            recipe.isKnownByDefault = data.unlockedByDefault;

            // Map workstation
            recipe.requiredWorkstation = ParseWorkstation(data.workstation);

            // Map ingredients
            var ingredients = new List<RecipeIngredient>();
            foreach (var ing in data.ingredients)
            {
                ingredients.Add(new RecipeIngredient
                {
                    name = ing.name,
                    quantity = ing.quantity,
                    consumed = ing.consumed,
                    category = ItemCategory.Misc
                });
            }
            recipe.ingredients = ingredients.ToArray();

            // Map outputs
            var outputs = new List<RecipeOutput>();
            foreach (var output in data.outputs)
            {
                outputs.Add(new RecipeOutput
                {
                    quantityMin = output.quantityMin,
                    quantityMax = output.quantityMax,
                    chance = output.chance
                });
            }
            recipe.outputs = outputs.ToArray();

            // Save asset
            string path = $"Assets/_WildSurvival/Data/Recipes/{data.recipeID}.asset";
            EnsureDirectoryExists(Path.GetDirectoryName(path));

            AssetDatabase.CreateAsset(recipe, path);

            // Add to database
            if (recipeDatabase != null)
            {
                recipeDatabase.AddRecipe(recipe);
            }

            importLog.Add($"✓ Imported recipe: {data.recipeID}");
            return true;
        }
        catch (Exception e)
        {
            importLog.Add($"✗ Failed recipe {data.recipeID}: {e.Message}");
            return false;
        }
    }

    // ========== EXPORT FUNCTIONS ==========

    private void ExportToFile(bool items, bool recipes, bool metadata)
    {
        string path = EditorUtility.SaveFilePanel(
            "Export Database",
            Application.dataPath,
            $"WildSurvival_Database_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            "json"
        );

        if (string.IsNullOrEmpty(path)) return;

        var exportData = CreateExportData(items, recipes, metadata);
        string json = JsonConvert.SerializeObject(exportData, Formatting.Indented);

        File.WriteAllText(path, json);

        Debug.Log($"Database exported to: {path}");
        ShowNotification(new GUIContent("Export complete!"));
    }

    private void ExportToClipboard(bool items, bool recipes, bool metadata)
    {
        var exportData = CreateExportData(items, recipes, metadata);
        string json = JsonConvert.SerializeObject(exportData, Formatting.Indented);

        GUIUtility.systemCopyBuffer = json;

        ShowNotification(new GUIContent("Exported to clipboard!"));
    }

    private object CreateExportData(bool includeItems, bool includeRecipes, bool includeMetadata)
    {
        var data = new Dictionary<string, object>();

        if (includeMetadata)
        {
            data["metadata"] = new
            {
                version = VERSION,
                exportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                itemCount = itemDatabase?.GetAllItems().Count ?? 0,
                recipeCount = recipeDatabase?.GetAllRecipes().Count ?? 0
            };
        }

        if (includeItems && itemDatabase != null)
        {
            var items = itemDatabase.GetAllItems().Select(item => new ItemBatchData
            {
                itemID = item.itemID,
                displayName = item.displayName,
                description = item.description,
                category = item.primaryCategory.ToString(),
                value = item.baseValue,
                weight = item.weight,
                maxStackSize = item.maxStackSize,
                gridWidth = item.gridSize.x,
                gridHeight = item.gridSize.y,
                hasDurability = item.hasDurability,
                maxDurability = item.maxDurability,
                tags = item.tags?.Select(t => t.ToString()).ToArray()
            }).ToList();

            data["items"] = items;
        }

        if (includeRecipes && recipeDatabase != null)
        {
            var recipes = recipeDatabase.GetAllRecipes().Select(recipe => new RecipeBatchData
            {
                recipeID = recipe.recipeID,
                recipeName = recipe.recipeName,
                description = recipe.description,
                category = recipe.category.ToString(),
                workstation = recipe.requiredWorkstation.ToString(),
                craftTime = recipe.baseCraftTime,
                tier = recipe.tier,
                unlockedByDefault = recipe.isKnownByDefault,
                ingredients = recipe.ingredients?.Select(ing => new IngredientData
                {
                    name = ing.name,
                    quantity = ing.quantity,
                    consumed = ing.consumed
                }).ToList(),
                outputs = recipe.outputs?.Select(output => new OutputData
                {
                    itemID = "",  // Would need to be mapped
                    quantityMin = output.quantityMin,
                    quantityMax = output.quantityMax,
                    chance = output.chance
                }).ToList()
            }).ToList();

            data["recipes"] = recipes;
        }

        return data;
    }

    // ========== TEMPLATE GENERATION ==========

    private void ExportItemTemplate()
    {
        var template = new ItemBatchData
        {
            itemID = "item_example",
            displayName = "Example Item",
            description = "Description of the item",
            category = "Resource",
            value = 10,
            weight = 1.0f,
            maxStackSize = 20,
            gridWidth = 1,
            gridHeight = 1,
            hasDurability = false,
            maxDurability = 100,
            tags = new[] { "Wood", "Craftable" }
        };

        string json = JsonConvert.SerializeObject(new { items = new[] { template } }, Formatting.Indented);
        GUIUtility.systemCopyBuffer = json;

        ShowNotification(new GUIContent("Item template copied to clipboard!"));
    }

    private void ExportRecipeTemplate()
    {
        var template = new RecipeBatchData
        {
            recipeID = "recipe_example",
            recipeName = "Example Recipe",
            description = "Crafts an example item",
            category = "Tools",
            workstation = "CraftingBench",
            craftTime = 5.0f,
            tier = 1,
            unlockedByDefault = true,
            ingredients = new List<IngredientData>
            {
                new IngredientData { name = "Wood", quantity = 2, consumed = true },
                new IngredientData { name = "Stone", quantity = 1, consumed = true }
            },
            outputs = new List<OutputData>
            {
                new OutputData { itemID = "item_example", quantityMin = 1, quantityMax = 1, chance = 1.0f }
            }
        };

        string json = JsonConvert.SerializeObject(new { recipes = new[] { template } }, Formatting.Indented);
        GUIUtility.systemCopyBuffer = json;

        ShowNotification(new GUIContent("Recipe template copied to clipboard!"));
    }

    private void ExportSchemaDocumentation()
    {
        var schema = @"
# Wild Survival Database Schema

## Item Structure
{
  'itemID': 'unique_identifier',
  'displayName': 'Human Readable Name',
  'description': 'Item description text',
  'category': 'Resource|Tool|Weapon|Food|Medicine|Clothing|Building|Misc',
  'value': 10,
  'weight': 1.0,
  'maxStackSize': 20,
  'gridWidth': 1,
  'gridHeight': 1,
  'hasDurability': false,
  'maxDurability': 100,
  'tags': ['Tag1', 'Tag2']
}

## Recipe Structure
{
  'recipeID': 'unique_identifier',
  'recipeName': 'Recipe Name',
  'description': 'Recipe description',
  'category': 'Tools|Weapons|Clothing|Building|Cooking|Medicine|Processing|Advanced',
  'workstation': 'None|CraftingBench|Forge|Campfire|Anvil|CookingPot|Loom|TanningRack|ChemistryLab|AdvancedBench',
  'craftTime': 5.0,
  'tier': 1,
  'unlockedByDefault': true,
  'ingredients': [
    { 'name': 'ItemName', 'quantity': 2, 'consumed': true }
  ],
  'outputs': [
    { 'itemID': 'item_id', 'quantityMin': 1, 'quantityMax': 1, 'chance': 1.0 }
  ]
}
";

        GUIUtility.systemCopyBuffer = schema;
        ShowNotification(new GUIContent("Schema documentation copied to clipboard!"));
    }

    // ========== AI INTEGRATION ==========

    private void CopyItemGenerationPrompt()
    {
        string prompt = @"Generate a JSON array of items for a wilderness survival game using this exact structure:

{
  'items': [
    {
      'itemID': 'item_wood',  // Unique identifier, lowercase with underscores
      'displayName': 'Wood',   // Human-readable name
      'description': 'Basic building material',
      'category': 'Resource',  // Resource|Tool|Weapon|Food|Medicine|Clothing|Building|Misc
      'value': 5,              // Base monetary value
      'weight': 2.0,           // Weight in kg
      'maxStackSize': 20,      // Maximum stack size (1 for non-stackable)
      'gridWidth': 1,          // Width in inventory grid
      'gridHeight': 2,         // Height in inventory grid
      'hasDurability': false,  // Whether item degrades
      'maxDurability': 100,    // Maximum durability if applicable
      'tags': ['Wood', 'Fuel', 'Craftable']  // Item tags for filtering
    }
  ]
}

Generate 20 diverse items including:
- Basic resources (wood, stone, fiber, metal)
- Tools (axes, pickaxes, knives)
- Food items (berries, meat, vegetables)
- Crafting materials (rope, cloth, leather)
- Special items (gems, rare materials)

Ensure logical values for weight, stack sizes, and grid dimensions based on item type.";

        GUIUtility.systemCopyBuffer = prompt;
        ShowNotification(new GUIContent("Item generation prompt copied!"));
    }

    private void CopyRecipeGenerationPrompt()
    {
        string prompt = @"Generate a JSON array of crafting recipes for a wilderness survival game using this exact structure:

{
  'recipes': [
    {
      'recipeID': 'recipe_wooden_axe',
      'recipeName': 'Wooden Axe',
      'description': 'Basic tool for chopping wood',
      'category': 'Tools',  // Tools|Weapons|Clothing|Building|Cooking|Medicine|Processing|Advanced
      'workstation': 'CraftingBench',  // None|CraftingBench|Forge|Campfire|etc
      'craftTime': 10.0,  // Time in seconds
      'tier': 1,  // Progression tier (1-5)
      'unlockedByDefault': true,
      'ingredients': [
        { 'name': 'Wood', 'quantity': 3, 'consumed': true },
        { 'name': 'Stone', 'quantity': 2, 'consumed': true },
        { 'name': 'Rope', 'quantity': 1, 'consumed': true }
      ],
      'outputs': [
        { 'itemID': 'item_wooden_axe', 'quantityMin': 1, 'quantityMax': 1, 'chance': 1.0 }
      ]
    }
  ]
}

Generate 30 recipes with logical progression:
- Tier 1: Basic tools and processing (no workstation required)
- Tier 2: Improved tools and basic weapons (requires CraftingBench)
- Tier 3: Advanced tools and armor (requires Forge)
- Tier 4: Specialized equipment (requires specific workstations)
- Tier 5: Endgame items (requires multiple workstations)

Ensure ingredients exist in the item database and outputs are balanced.";

        GUIUtility.systemCopyBuffer = prompt;
        ShowNotification(new GUIContent("Recipe generation prompt copied!"));
    }

    // ========== UTILITY FUNCTIONS ==========

    private void DrawImportLog()
    {
        if (importLog.Count == 0) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Import Log", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Show last 10 entries
        int startIndex = Mathf.Max(0, importLog.Count - 10);
        for (int i = startIndex; i < importLog.Count; i++)
        {
            EditorGUILayout.LabelField(importLog[i], EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Clear Log", GUILayout.Width(80)))
        {
            importLog.Clear();
        }
    }

    private void DrawFooter()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Documentation", GUILayout.Width(100)))
        {
            Application.OpenURL("https://github.com/yourgame/docs");
        }

        if (GUILayout.Button("Clear All", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Clear All", "This will clear all pending operations. Continue?", "Yes", "No"))
            {
                pendingItems.Clear();
                pendingRecipes.Clear();
                importLog.Clear();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ValidateAllItems()
    {
        // Implementation for validating items
        int issues = 0;
        importLog.Clear();

        foreach (var item in itemDatabase.GetAllItems())
        {
            if (string.IsNullOrEmpty(item.itemID))
            {
                importLog.Add($"✗ Missing ID on item {item.name}");
                issues++;
            }

            if (item.weight <= 0)
            {
                importLog.Add($"⚠ Invalid weight on {item.itemID}");
                issues++;
            }

            if (item.gridSize.x <= 0 || item.gridSize.y <= 0)
            {
                importLog.Add($"⚠ Invalid grid size on {item.itemID}");
                issues++;
            }
        }

        importLog.Add($"Validation complete: {issues} issues found");
    }

    private void ValidateAllRecipes()
    {
        // Implementation for validating recipes
        int issues = 0;
        importLog.Clear();

        foreach (var recipe in recipeDatabase.GetAllRecipes())
        {
            if (string.IsNullOrEmpty(recipe.recipeID))
            {
                importLog.Add($"✗ Missing ID on recipe {recipe.name}");
                issues++;
            }

            if (recipe.ingredients == null || recipe.ingredients.Length == 0)
            {
                importLog.Add($"✗ No ingredients in {recipe.recipeID}");
                issues++;
            }

            if (recipe.outputs == null || recipe.outputs.Length == 0)
            {
                importLog.Add($"✗ No outputs in {recipe.recipeID}");
                issues++;
            }
        }

        importLog.Add($"Validation complete: {issues} issues found");
    }

    private void CheckMissingDependencies()
    {
        // Check for missing item references in recipes
        importLog.Clear();

        foreach (var recipe in recipeDatabase.GetAllRecipes())
        {
            foreach (var ingredient in recipe.ingredients)
            {
                // Check if item exists
                // This would need actual implementation based on your item lookup
                importLog.Add($"Checking {ingredient.name} in {recipe.recipeID}");
            }
        }
    }

    private void CreateItemDatabase()
    {
        var db = ScriptableObject.CreateInstance<ItemDatabase>();
        string path = "Assets/_WildSurvival/Data/Databases/MasterItemDatabase.asset";
        EnsureDirectoryExists(Path.GetDirectoryName(path));
        AssetDatabase.CreateAsset(db, path);
        AssetDatabase.SaveAssets();
        itemDatabase = db;
    }

    private void CreateRecipeDatabase()
    {
        var db = ScriptableObject.CreateInstance<RecipeDatabase>();
        string path = "Assets/_WildSurvival/Data/Databases/MasterRecipeDatabase.asset";
        EnsureDirectoryExists(Path.GetDirectoryName(path));
        AssetDatabase.CreateAsset(db, path);
        AssetDatabase.SaveAssets();
        recipeDatabase = db;
    }

    private void EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    // Parsing helpers
    private ItemCategory ParseItemCategory(string category)
    {
        if (Enum.TryParse<ItemCategory>(category, out var result))
            return result;
        return ItemCategory.Misc;
    }

    private ItemTag ParseItemTag(string tag)
    {
        if (Enum.TryParse<ItemTag>(tag, out var result))
            return result;
        return ItemTag.None;
    }

    private CraftingCategory ParseCraftingCategory(string category)
    {
        if (Enum.TryParse<CraftingCategory>(category, out var result))
            return result;
        return CraftingCategory.Tools;
    }

    private WorkstationType ParseWorkstation(string workstation)
    {
        if (Enum.TryParse<WorkstationType>(workstation, out var result))
            return result;
        return WorkstationType.None;
    }

    // ========== DATA STRUCTURES ==========

    [Serializable]
    public class ItemBatchData
    {
        public string itemID;
        public string displayName;
        public string description;
        public string category;
        public int value;
        public float weight;
        public int maxStackSize;
        public int gridWidth;
        public int gridHeight;
        public bool hasDurability;
        public float maxDurability;
        public string[] tags;
    }

    [Serializable]
    public class RecipeBatchData
    {
        public string recipeID;
        public string recipeName;
        public string description;
        public string category;
        public string workstation;
        public float craftTime;
        public int tier;
        public bool unlockedByDefault;
        public List<IngredientData> ingredients;
        public List<OutputData> outputs;
    }

    [Serializable]
    public class IngredientData
    {
        public string name;
        public int quantity;
        public bool consumed;
    }

    [Serializable]
    public class OutputData
    {
        public string itemID;
        public int quantityMin;
        public int quantityMax;
        public float chance;
    }

    // Generation methods would go here...
    private void GenerateSurvivalEssentials() { /* Implementation */ }
    private void GenerateCraftingProgression() { /* Implementation */ }
    private void GenerateCompleteDatabase() { /* Implementation */ }
    private void GenerateFromTemplate() { /* Implementation */ }
    private void ValidateAIJSON() { /* Implementation */ }
    private void ExportBatchTemplate(int items, int recipes) { /* Implementation */ }
}