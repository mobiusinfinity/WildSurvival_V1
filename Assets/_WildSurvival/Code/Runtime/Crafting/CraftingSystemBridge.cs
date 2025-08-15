using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Bridge between Ultimate Inventory Tool and Crafting System
/// Fixed version that maps to actual ItemData properties
/// </summary>
public static class CraftingSystemBridge
{
    private static Dictionary<string, ItemData> itemDataCache = new Dictionary<string, ItemData>();
    private static Dictionary<string, CraftingRecipe> recipeCache = new Dictionary<string, CraftingRecipe>();

    // ====== ITEM CONVERSION ======

    /// <summary>
    /// Convert ItemDefinition (Ultimate Tool) to ItemData (Runtime)
    /// </summary>
    public static ItemData ConvertToItemData(ItemDefinition definition)
    {
        if (definition == null) return null;

        // Check cache first
        if (itemDataCache.TryGetValue(definition.itemID, out ItemData cached))
            return cached;

        var data = ScriptableObject.CreateInstance<ItemData>();

        // Map to ACTUAL ItemData properties
        data.itemID = definition.itemID;
        data.itemName = definition.displayName; // itemName instead of displayName
        data.description = definition.description;
        data.icon = definition.icon;
        data.worldPrefab = definition.worldModel; // worldPrefab instead of worldModel

        // Physical properties
        data.weight = definition.weight;
        data.value = definition.baseValue; // value instead of baseValue

        // Grid properties - ItemData uses Vector2Int gridSize
        data.gridSize = definition.gridSize;

        // Stacking
        data.maxStackSize = definition.maxStackSize;

        // ItemType conversion (map to your ItemType enum)
        data.itemType = ConvertToItemType(definition.primaryCategory);

        // Durability
        data.hasDurability = definition.hasDurability;
        data.maxDurability = definition.maxDurability;

        // Cache it
        itemDataCache[definition.itemID] = data;

        return data;
    }

    /// <summary>
    /// Convert ItemData (Runtime) to ItemDefinition (Ultimate Tool)
    /// </summary>
    //public static ItemDefinition ConvertToItemDefinition(ItemData data)
    //{
    //    if (data == null) return null;

    //    var definition = ScriptableObject.CreateInstance<ItemDefinition>();

    //    definition.itemID = data.itemID;
    //    definition.displayName = data.itemName; // Map from itemName
    //    definition.description = data.description;
    //    definition.icon = data.icon;
    //    definition.worldModel = data.worldPrefab; // Map from worldPrefab
    //    definition.weight = data.weight;
    //    definition.baseValue = data.value; // Map from value
    //    definition.gridSize = data.gridSize; // Both use Vector2Int
    //    definition.maxStackSize = data.maxStackSize;
    //    definition.primaryCategory = ConvertFromItemType(data.itemType);
    //    definition.hasDurability = data.hasDurability;
    //    definition.maxDurability = data.maxDurability;

    //    return definition;
    //}

    // ====== RECIPE CONVERSION ======

    /// <summary>
    /// Convert RecipeDefinition (Ultimate Tool) to CraftingRecipe (Runtime)
    /// </summary>
    public static CraftingRecipe ConvertToCraftingRecipe(RecipeDefinition definition)
    {
        if (definition == null) return null;

        // Check cache
        if (recipeCache.TryGetValue(definition.recipeID, out CraftingRecipe cached))
            return cached;

        var recipe = ScriptableObject.CreateInstance<CraftingRecipe>();

        // Basic info
        recipe.recipeID = definition.recipeID;
        recipe.displayName = definition.recipeName;
        recipe.description = definition.description;
        recipe.icon = definition.icon;
        recipe.category = definition.category.ToString();

        // Requirements
        recipe.requiredPlayerLevel = definition.tier;
        recipe.unlockedByDefault = definition.isKnownByDefault;
        recipe.craftingTime = definition.baseCraftTime;
        recipe.requiredWorkbench = ConvertWorkstation(definition.requiredWorkstation);

        // Ingredients - RecipeDefinition uses 'ingredients' not 'inputs'
        recipe.ingredients = new List<CraftingRecipe.CraftingIngredient>();
        if (definition.ingredients != null)
        {
            foreach (var input in definition.ingredients)
            {
                recipe.ingredients.Add(new CraftingRecipe.CraftingIngredient
                {
                    itemID = input.specificItem != null ? input.specificItem.itemID : input.name,
                    displayName = input.name,
                    quantity = input.quantity,
                    consumeOnCraft = input.consumed
                });
            }
        }

        // Output
        if (definition.outputs != null && definition.outputs.Length > 0)
        {
            var mainOutput = definition.outputs[0];
            recipe.output = new CraftingRecipe.CraftingOutput
            {
                itemID = mainOutput.item?.itemID ?? "",
                displayName = mainOutput.item?.displayName ?? "Unknown", // ← FIX: use displayName
                quantityMin = mainOutput.quantityMin,
                quantityMax = mainOutput.quantityMax,
                successChance = mainOutput.chance,
                quality = 100
            };

            // Additional outputs
            recipe.additionalOutputs = new List<CraftingRecipe.CraftingOutput>();
            for (int i = 1; i < definition.outputs.Length; i++)
            {
                var output = definition.outputs[i];
                recipe.additionalOutputs.Add(new CraftingRecipe.CraftingOutput
                {
                    itemID = output.item?.itemID ?? "",
                    displayName = output.item?.displayName ?? "Unknown", // ← FIX: use displayName
                    quantityMin = output.quantityMin,
                    quantityMax = output.quantityMax,
                    successChance = output.chance,
                    quality = 100
                });
            }
        }

        // Experience
        recipe.experienceReward = 10; // Default value

        // Cache it
        recipeCache[definition.recipeID] = recipe;

        return recipe;
    }

    /// <summary>
    /// Convert CraftingRecipe (Runtime) to RecipeDefinition (Ultimate Tool)
    /// </summary>
    public static RecipeDefinition ConvertToRecipeDefinition(CraftingRecipe recipe)
    {
        if (recipe == null) return null;

        var definition = ScriptableObject.CreateInstance<RecipeDefinition>();

        definition.recipeID = recipe.recipeID;
        definition.recipeName = recipe.displayName;
        definition.description = recipe.description;
        definition.icon = recipe.icon;
        definition.category = recipe.category;
        definition.tier = recipe.requiredPlayerLevel;
        definition.isKnownByDefault = recipe.unlockedByDefault;
        definition.baseCraftTime = recipe.craftingTime;
        definition.requiredWorkstation = ConvertToWorkstation(recipe.requiredWorkbench);

        // Convert ingredients
        var ingredients = new List<RecipeIngredient>();
        foreach (var ingredient in recipe.ingredients)
        {
            ingredients.Add(new RecipeIngredient
            {
                name = ingredient.displayName,
                quantity = ingredient.quantity,
                consumed = ingredient.consumeOnCraft,
                category = ItemCategory.Misc // Default
            });
        }
        definition.ingredients = ingredients.ToArray();

        // Convert outputs
        var outputs = new List<RecipeOutput>();
        if (recipe.output != null)
        {
            outputs.Add(new RecipeOutput
            {
                quantityMin = recipe.output.quantityMin,
                quantityMax = recipe.output.quantityMax,
                chance = recipe.output.successChance
            });
        }
        definition.outputs = outputs.ToArray();

        return definition;
    }

    // ====== ENUM CONVERSIONS ======

    private static ItemType ConvertToItemType(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Resource => ItemType.Resource,
            ItemCategory.Tool => ItemType.Tool,
            ItemCategory.Weapon => ItemType.Weapon,
            ItemCategory.Food => ItemType.Consumable,
            ItemCategory.Medicine => ItemType.Consumable,
            ItemCategory.Clothing => ItemType.Equipment,
            ItemCategory.Building => ItemType.Building,
            _ => ItemType.Misc
        };
    }

    private static ItemCategory ConvertFromItemType(ItemType type)
    {
        return type switch
        {
            ItemType.Resource => ItemCategory.Resource,
            ItemType.Tool => ItemCategory.Tool,
            ItemType.Weapon => ItemCategory.Weapon,
            ItemType.Consumable => ItemCategory.Food,
            ItemType.Equipment => ItemCategory.Clothing,
            ItemType.Building => ItemCategory.Building,
            _ => ItemCategory.Misc
        };
    }

    private static CraftingRecipe.WorkbenchType ConvertWorkstation(WorkstationType station)
    {
        return station switch
        {
            WorkstationType.None => CraftingRecipe.WorkbenchType.None,
            WorkstationType.WorkBench => CraftingRecipe.WorkbenchType.WorkBench,
            WorkstationType.Forge => CraftingRecipe.WorkbenchType.Forge,
            WorkstationType.Campfire => CraftingRecipe.WorkbenchType.Campfire,
            WorkstationType.Anvil => CraftingRecipe.WorkbenchType.Anvil,
            WorkstationType.CookingPot => CraftingRecipe.WorkbenchType.CookingPot,
            WorkstationType.Loom => CraftingRecipe.WorkbenchType.Loom,
            WorkstationType.TanningRack => CraftingRecipe.WorkbenchType.TanningRack,
            WorkstationType.ChemistryStation => CraftingRecipe.WorkbenchType.ChemistryLab,
            WorkstationType.AdvancedWorkbench => CraftingRecipe.WorkbenchType.AdvancedBench,
            _ => CraftingRecipe.WorkbenchType.None
        };
    }

    private static WorkstationType ConvertToWorkstation(CraftingRecipe.WorkbenchType type)
    {
        return type switch
        {
            CraftingRecipe.WorkbenchType.None => WorkstationType.None,
            CraftingRecipe.WorkbenchType.WorkBench => WorkstationType.WorkBench,
            CraftingRecipe.WorkbenchType.Forge => WorkstationType.Forge,
            CraftingRecipe.WorkbenchType.Campfire => WorkstationType.Campfire,
            CraftingRecipe.WorkbenchType.Anvil => WorkstationType.Anvil,
            CraftingRecipe.WorkbenchType.CookingPot => WorkstationType.CookingPot,
            CraftingRecipe.WorkbenchType.Loom => WorkstationType.Loom,
            CraftingRecipe.WorkbenchType.TanningRack => WorkstationType.TanningRack,
            CraftingRecipe.WorkbenchType.ChemistryLab => WorkstationType.ChemistryStation,
            CraftingRecipe.WorkbenchType.AdvancedBench => WorkstationType.AdvancedWorkbench,
            _ => WorkstationType.None
        };
    }

    private static CraftingCategory ParseCraftingCategory(string category)
    {
        return category?.ToLower() switch
        {
            "tools" => CraftingCategory.Tools,
            "weapons" => CraftingCategory.Weapons,
            "clothing" => CraftingCategory.Clothing,
            "building" => CraftingCategory.Building,
            "cooking" => CraftingCategory.Cooking,
            "medicine" => CraftingCategory.Medicine,
            "processing" => CraftingCategory.Processing,
            "advanced" => CraftingCategory.Advanced,
            _ => CraftingCategory.Tools
        };
    }

    // ====== UTILITY METHODS ======

    /// <summary>
    /// Clear all cached conversions
    /// </summary>
    public static void ClearCache()
    {
        itemDataCache.Clear();
        recipeCache.Clear();
    }

    /// <summary>
    /// Get or convert an item from either system
    /// </summary>
    public static ItemData GetItemData(string itemID)
    {
        // Try cache first
        if (itemDataCache.TryGetValue(itemID, out ItemData cached))
            return cached;

        // Try loading ItemData directly
        var itemData = Resources.Load<ItemData>($"Items/{itemID}");
        if (itemData != null)
            return itemData;

        // Try loading ItemDefinition and converting
        var itemDef = Resources.Load<ItemDefinition>($"Definitions/{itemID}");
        if (itemDef != null)
            return ConvertToItemData(itemDef);

        // Try loading from database
        var database = Resources.Load<ItemDatabase>("Databases/MasterItemDatabase");
        if (database != null)
        {
            var definition = database.GetItem(itemID);
            if (definition != null)
                return ConvertToItemData(itemDef);
        }

        Debug.LogWarning($"[CraftingSystemBridge] Item not found: {itemID}");
        return null;
    }

    public static ItemDefinition ConvertToItemDefinition(ItemData data)
    {
        if (data == null) return null;

        // Create a new ItemDefinition from ItemData
        var definition = ScriptableObject.CreateInstance<ItemDefinition>();

        // Map all the properties
        definition.itemID = data.itemID;
        definition.displayName = data.itemName ?? data.name ?? "Unknown";
        definition.description = data.description;
        definition.icon = data.icon;
        definition.worldModel = data.worldPrefab;
        definition.itemType = data.itemType;
        definition.weight = data.weight;
        definition.baseValue = data.value;
        //definition.stackable = data.IsStackable();
        definition.maxStackSize = data.maxStackSize;

        definition.gridSize = data.gridSize; // Both use Vector2Int
 
        definition.primaryCategory = ConvertFromItemType(data.itemType);
        definition.hasDurability = data.hasDurability;
        definition.maxDurability = data.maxDurability;

        // Handle grid size - check if it's Vector2Int or separate values
        if (definition.gridSize.x > 0 && definition.gridSize.y > 0)
        {
            definition.shapeGrid = new bool[definition.gridSize.x, definition.gridSize.y];
            // Default to full shape
            for (int x = 0; x < definition.gridSize.x; x++)
            {
                for (int y = 0; y < definition.gridSize.y; y++)
                {
                    definition.shapeGrid[x, y] = true;
                }
            }
        }

        return definition;
    }

    /// <summary>
    /// Get or convert a recipe from either system
    /// </summary>
    public static CraftingRecipe GetRecipe(string recipeID)
    {
        // Try cache first
        if (recipeCache.TryGetValue(recipeID, out CraftingRecipe cached))
            return cached;

        // Try loading CraftingRecipe directly
        var recipe = Resources.Load<CraftingRecipe>($"Recipes/{recipeID}");
        if (recipe != null)
            return recipe;

        // Try loading RecipeDefinition and converting
        var recipeDef = Resources.Load<RecipeDefinition>($"RecipeDefinitions/{recipeID}");
        if (recipeDef != null)
            return ConvertToCraftingRecipe(recipeDef);

        // Try loading from database
        var database = Resources.Load<RecipeDatabase>("Databases/MasterRecipeDatabase");
        if (database != null)
        {
            var definition = database.GetRecipe(recipeID);
            if (definition != null)
                return ConvertToCraftingRecipe(definition);
        }

        Debug.LogWarning($"[CraftingSystemBridge] Recipe not found: {recipeID}");
        return null;
    }
}