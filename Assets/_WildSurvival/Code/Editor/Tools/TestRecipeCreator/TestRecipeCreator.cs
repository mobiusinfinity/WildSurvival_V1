using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to quickly create test recipes for Wild Survival
/// Place in Editor folder
/// </summary>
public class TestRecipeCreator : EditorWindow
{
    [MenuItem("Tools/Wild Survival/Create Test Recipes")]
    public static void ShowWindow()
    {
        GetWindow<TestRecipeCreator>("Recipe Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Test Recipe Creator", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Basic Recipes"))
        {
            CreateBasicRecipes();
        }

        if (GUILayout.Button("Create Tool Recipes"))
        {
            CreateToolRecipes();
        }

        if (GUILayout.Button("Create Food Recipes"))
        {
            CreateFoodRecipes();
        }

        if (GUILayout.Button("Create ALL Test Recipes"))
        {
            CreateBasicRecipes();
            CreateToolRecipes();
            CreateFoodRecipes();
        }
    }

    private void CreateBasicRecipes()
    {
        // Recipe: Wood → Planks
        CreateRecipe(
            "recipe_planks",
            "Wooden Planks",
            "Process wood into usable planks",
            "Processing",
            new List<IngredientData>
            {
                new IngredientData("item_wood", 2)
            },
            new OutputData("item_planks", 4, 6),
            CraftingRecipe.WorkbenchType.None,
            2f
        );

        // Recipe: Fiber → Rope
        CreateRecipe(
            "recipe_rope",
            "Rope",
            "Twist fibers into strong rope",
            "Processing",
            new List<IngredientData>
            {
                new IngredientData("item_fiber", 3)
            },
            new OutputData("item_rope", 1, 1),
            CraftingRecipe.WorkbenchType.None,
            3f
        );

        // Recipe: Stone + Wood → Campfire
        CreateRecipe(
            "recipe_campfire",
            "Campfire",
            "A basic fire for cooking and warmth",
            "Building",
            new List<IngredientData>
            {
                new IngredientData("item_stone", 5),
                new IngredientData("item_wood", 3)
            },
            new OutputData("item_campfire", 1, 1),
            CraftingRecipe.WorkbenchType.None,
            5f
        );

        Debug.Log("Created basic recipes!");
    }

    private void CreateToolRecipes()
    {
        // Recipe: Stone Axe
        CreateRecipe(
            "recipe_stone_axe",
            "Stone Axe",
            "Basic tool for chopping wood",
            "Tools",
            new List<IngredientData>
            {
                new IngredientData("item_stone", 2),
                new IngredientData("item_wood", 1),
                new IngredientData("item_rope", 1)
            },
            new OutputData("item_stone_axe", 1, 1),
            CraftingRecipe.WorkbenchType.WorkBench,
            10f
        );

        // Recipe: Stone Pickaxe
        CreateRecipe(
            "recipe_stone_pickaxe",
            "Stone Pickaxe",
            "Tool for mining stone and ore",
            "Tools",
            new List<IngredientData>
            {
                new IngredientData("item_stone", 3),
                new IngredientData("item_wood", 2),
                new IngredientData("item_rope", 1)
            },
            new OutputData("item_stone_pickaxe", 1, 1),
            CraftingRecipe.WorkbenchType.WorkBench,
            12f
        );

        // Recipe: Crafting Bench
        CreateRecipe(
            "recipe_crafting_bench",
            "Crafting Bench",
            "Workstation for advanced crafting",
            "Building",
            new List<IngredientData>
            {
                new IngredientData("item_planks", 4),
                new IngredientData("item_stone", 2)
            },
            new OutputData("item_crafting_bench", 1, 1),
            CraftingRecipe.WorkbenchType.None,
            15f
        );

        Debug.Log("Created tool recipes!");
    }

    private void CreateFoodRecipes()
    {
        // Recipe: Cooked Meat
        CreateRecipe(
            "recipe_cooked_meat",
            "Cooked Meat",
            "Grilled meat, safe to eat",
            "Cooking",
            new List<IngredientData>
            {
                new IngredientData("item_raw_meat", 1)
            },
            new OutputData("item_cooked_meat", 1, 1),
            CraftingRecipe.WorkbenchType.Campfire,
            5f
        );

        // Recipe: Berry Juice
        CreateRecipe(
            "recipe_berry_juice",
            "Berry Juice",
            "Refreshing drink from berries",
            "Cooking",
            new List<IngredientData>
            {
                new IngredientData("item_berries", 3),
                new IngredientData("item_water", 1)
            },
            new OutputData("item_berry_juice", 1, 2),
            CraftingRecipe.WorkbenchType.CookingPot,
            4f
        );

        Debug.Log("Created food recipes!");
    }

    private void CreateRecipe(string id, string name, string desc, string category,
        List<IngredientData> ingredients, OutputData output,
        CraftingRecipe.WorkbenchType workbench, float craftTime)
    {
        // Create the recipe ScriptableObject
        CraftingRecipe recipe = ScriptableObject.CreateInstance<CraftingRecipe>();

        recipe.recipeID = id;
        recipe.displayName = name;
        recipe.description = desc;
        recipe.category = category;
        recipe.requiredWorkbench = workbench;
        recipe.craftingTime = craftTime;
        recipe.unlockedByDefault = true;
        recipe.requiredPlayerLevel = 0;

        // Set ingredients
        recipe.ingredients = new List<CraftingRecipe.CraftingIngredient>();
        foreach (var ing in ingredients)
        {
            recipe.ingredients.Add(new CraftingRecipe.CraftingIngredient
            {
                itemID = ing.itemID,
                displayName = ing.itemID.Replace("item_", "").Replace("_", " "),
                quantity = ing.quantity,
                consumeOnCraft = true
            });
        }

        // Set output
        recipe.output = new CraftingRecipe.CraftingOutput
        {
            itemID = output.itemID,
            displayName = output.itemID.Replace("item_", "").Replace("_", " "),
            quantityMin = output.minQty,
            quantityMax = output.maxQty,
            successChance = 1f,
            quality = 100
        };

        // Save the asset
        string path = $"Assets/_WildSurvival/Data/Recipes/{id}.asset";

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/_WildSurvival/Data/Recipes"))
        {
            AssetDatabase.CreateFolder("Assets/_WildSurvival/Data", "Recipes");
        }

        AssetDatabase.CreateAsset(recipe, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Created recipe: {name} at {path}");
    }

    // Helper classes
    private class IngredientData
    {
        public string itemID;
        public int quantity;

        public IngredientData(string id, int qty)
        {
            itemID = id;
            quantity = qty;
        }
    }

    private class OutputData
    {
        public string itemID;
        public int minQty;
        public int maxQty;

        public OutputData(string id, int min, int max)
        {
            itemID = id;
            minQty = min;
            maxQty = max;
        }
    }
}

// ========== MANUAL RECIPE EXAMPLES ==========
// If you prefer to create recipes manually, here are the key fields:

/*
Example Recipe Structure:

CraftingRecipe recipe = ScriptableObject.CreateInstance<CraftingRecipe>();

// Basic Info
recipe.recipeID = "recipe_wooden_sword";
recipe.displayName = "Wooden Sword";
recipe.description = "A basic sword made from wood";
recipe.category = "Weapons";
recipe.icon = null; // Assign sprite if available

// Requirements
recipe.requiredWorkbench = CraftingRecipe.WorkbenchType.CraftingBench;
recipe.requiredPlayerLevel = 0;
recipe.unlockedByDefault = true;
recipe.craftingTime = 5f;

// Ingredients
recipe.ingredients = new List<CraftingRecipe.CraftingIngredient>
{
    new CraftingRecipe.CraftingIngredient
    {
        itemID = "item_wood",
        displayName = "Wood",
        quantity = 3,
        consumeOnCraft = true
    },
    new CraftingRecipe.CraftingIngredient
    {
        itemID = "item_rope",
        displayName = "Rope",
        quantity = 1,
        consumeOnCraft = true
    }
};

// Output
recipe.output = new CraftingRecipe.CraftingOutput
{
    itemID = "item_wooden_sword",
    displayName = "Wooden Sword",
    quantityMin = 1,
    quantityMax = 1,
    successChance = 1f,
    quality = 100
};

// Experience reward
recipe.experienceReward = 10;
*/