using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject defining a crafting recipe
/// Supports multiple ingredients, tools, and workbench requirements
/// FIXED VERSION - No namespaces, all enums accessible
/// </summary>
[CreateAssetMenu(fileName = "New Recipe", menuName = "Wild Survival/Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Basic Info")]
    public string recipeID;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public string category = "Misc"; // Changed from enum to string for flexibility

    [Header("Unlock Requirements")]
    public bool unlockedByDefault = true;
    public int requiredPlayerLevel = 1;
    public List<string> requiredRecipeIDs = new List<string>(); // Must know these recipes first
    public List<string> requiredSkills = new List<string>();

    [Header("Crafting Requirements")]
    public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
    public List<CraftingTool> requiredTools = new List<CraftingTool>();
    public WorkbenchType requiredWorkbench = WorkbenchType.None;
    public float craftingTime = 2f; // seconds
    public int craftingLevel = 1; // Skill level required

    [Header("Output")]
    public CraftingOutput output = new CraftingOutput();
    public List<CraftingOutput> additionalOutputs = new List<CraftingOutput>(); // Byproducts

    [Header("Advanced")]
    public bool allowBulkCrafting = true;
    public int maxBulkQuantity = 10;
    public bool consumeTools = false; // Whether tools are consumed
    public float toolDurabilityLoss = 10f; // If tools aren't consumed
    public int maxCraftableAtOnce = 99;
    public bool requiresNearbyFire = false;
    public bool requiresWaterSource = false;
    public bool requiresFuel = false;
    public float fuelCost = 1f;
    public string statusEffectOnCraft = ""; // Status effect to apply when crafting

    [Header("Experience")]
    public int experienceReward = 10;
    public string skillImproved = "Crafting"; // Changed from enum to string

    // Weather and time requirements (simplified to strings)
    public string weatherRequirement = "Any";
    public string timeRequirement = "Any";


    // ====== ENUMS MOVED TO TOP LEVEL ======
    public enum WorkbenchType
    {
        None,           // Can craft in inventory
        WorkBench,  // Basic workbench (renamed from Workbench)
        Forge,          // For metal working
        Campfire,       // For cooking (renamed from CookingFire)
        ChemistryLab,   // For advanced items
        TailoringBench, // For clothing
        ToolBench,      // For advanced tools
        Anvil,          // For weapons/armor
        CookingPot,     // For advanced cooking
        Loom,           // For fabric
        TanningRack,    // For leather
        AdvancedBench   // Master crafting station
    }

    public enum SkillType
    {
        Crafting,
        Cooking,
        Smithing,
        Tailoring,
        Alchemy,
        Engineering,
        Survival,
        Woodworking,
        Mining,
        Farming
    }

    [System.Serializable]
    public class CraftingOutput
    {
        [Header("Item")]
        public string itemID;
        public string displayName;

        [Header("Quantity")]
        public int quantityMin;
        public int quantityMax;

        [Header("Probability")]
        public float successChance = 1f;  // You already have this

        // ADD THIS FIELD - This is for additional outputs probability!
        public float chance = 1f;  // Probability from 0 to 1

        [Header("Quality")]
        public int quality = 100;

        // Constructor
        public CraftingOutput()
        {
            quantityMin = 1;
            quantityMax = 1;
            successChance = 1f;
            chance = 1f;  // Default to 100% chance
            quality = 100;
        }
    }

    // ====== NESTED CLASSES ======
    [System.Serializable]


    public class CraftingIngredient
    {
        public string itemID;
        public string displayName;
        public int quantity = 1;
        public bool consumeOnCraft = true;
        public bool allowSubstitutes = false;
        public List<string> substituteItemIDs = new List<string>();

        // For item quality systems
        public int minimumQuality = 0;
        public bool preserveQuality = false; // Output quality based on input
    }

    [System.Serializable]
    public class CraftingTool
    {
        public string toolID;
        public string displayName;
        public string toolType = "Generic"; // Changed from enum
        public int minimumTier = 1; // Tool tier required
        public bool mustBeEquipped = false;
        public bool consumeDurability = true;
        public float durabilityLoss = 5f;
    }

    //[System.Serializable]
    //public class CraftingOutput
    //{
    //    public string itemID;
    //    public string displayName;
    //    public int quantityMin = 1;
    //    public int quantityMax = 1;
    //    public float successChance = 1f; // 0-1
    //    public int quality = 100; // 0-100

    //    // ADD THIS FIELD:
    //    public float chance = 1f;  // Probability from 0 to 1 for additional outputs

    //    public int GetQuantity()
    //    {
    //        return UnityEngine.Random.Range(quantityMin, quantityMax + 1);
    //    }

    //    public bool RollSuccess()
    //    {
    //        return UnityEngine.Random.value <= successChance;
    //    }

    //    public CraftingOutput()
    //    {
    //        quantityMin = 1;
    //        quantityMax = 1;
    //        chance = 1f;  // Default to 100% chance
    //        successChance = 1f;
    //    }
    //}

    // ====== VALIDATION METHODS ======
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(recipeID))
            return false;

        if (ingredients.Count == 0)
            return false;

        if (output == null || string.IsNullOrEmpty(output.itemID))
            return false;

        return true;
    }

    public bool CanCraft(int playerLevel, List<string> knownRecipes, List<string> playerSkills)
    {
        // Check unlock status
        if (!unlockedByDefault)
        {
            if (!knownRecipes.Contains(recipeID))
                return false;
        }

        // Check level
        if (playerLevel < requiredPlayerLevel)
            return false;

        // Check prerequisite recipes
        foreach (var reqRecipe in requiredRecipeIDs)
        {
            if (!knownRecipes.Contains(reqRecipe))
                return false;
        }

        // Check skills
        foreach (var skill in requiredSkills)
        {
            if (!playerSkills.Contains(skill))
                return false;
        }

        return true;
    }

    public bool HasIngredients(Dictionary<string, int> inventory)
    {
        foreach (var ingredient in ingredients)
        {
            int required = ingredient.quantity;
            int available = 0;

            // Check main item
            if (inventory.TryGetValue(ingredient.itemID, out int count))
            {
                available += count;
            }

            // Check substitutes if allowed
            if (ingredient.allowSubstitutes)
            {
                foreach (var substitute in ingredient.substituteItemIDs)
                {
                    if (inventory.TryGetValue(substitute, out int subCount))
                    {
                        available += subCount;
                    }
                }
            }

            if (available < required)
                return false;
        }

        return true;


    }



    public float GetCraftingTime(float speedMultiplier = 1f)
    {
        return Mathf.Max(0.5f, craftingTime / speedMultiplier);
    }

    public int GetOutputQuality(int inputQualityAverage, float qualityBonus)
    {
        float baseQuality = output.quality;

        // If preserving quality from inputs
        bool preserveQuality = false;
        foreach (var ingredient in ingredients)
        {
            if (ingredient.preserveQuality)
            {
                preserveQuality = true;
                break;
            }
        }

        if (preserveQuality)
        {
            baseQuality = inputQualityAverage;
        }

        // Apply bonus
        float finalQuality = baseQuality + qualityBonus;

        // Add some randomness
        finalQuality += UnityEngine.Random.Range(-5f, 5f);

        return Mathf.Clamp(Mathf.RoundToInt(finalQuality), 0, 100);
    }
}

// ====== WORKBENCH TYPE EXTENSION METHODS ======
public static class WorkbenchTypeExtensions
{
    public static string GetDisplayName(this CraftingRecipe.WorkbenchType type)
    {
        return type switch
        {
            CraftingRecipe.WorkbenchType.None => "Inventory",
            CraftingRecipe.WorkbenchType.WorkBench => "Crafting Work Bench",
            CraftingRecipe.WorkbenchType.Forge => "Forge",
            CraftingRecipe.WorkbenchType.Campfire => "Campfire",
            CraftingRecipe.WorkbenchType.ChemistryLab => "Chemistry Lab",
            CraftingRecipe.WorkbenchType.TailoringBench => "Tailoring Bench",
            CraftingRecipe.WorkbenchType.ToolBench => "Tool Bench",
            CraftingRecipe.WorkbenchType.Anvil => "Anvil",
            CraftingRecipe.WorkbenchType.CookingPot => "Cooking Pot",
            CraftingRecipe.WorkbenchType.Loom => "Loom",
            CraftingRecipe.WorkbenchType.TanningRack => "Tanning Rack",
            CraftingRecipe.WorkbenchType.AdvancedBench => "Advanced Workbench",
            _ => type.ToString()
        };
    }

    public static Color GetColor(this CraftingRecipe.WorkbenchType type)
    {
        return type switch
        {
            CraftingRecipe.WorkbenchType.None => Color.white,
            CraftingRecipe.WorkbenchType.WorkBench => new Color(0.6f, 0.4f, 0.2f), // Brown
            CraftingRecipe.WorkbenchType.Forge => new Color(1f, 0.4f, 0f), // Orange
            CraftingRecipe.WorkbenchType.Campfire => new Color(1f, 0.6f, 0f), // Fire orange
            CraftingRecipe.WorkbenchType.ChemistryLab => new Color(0f, 1f, 0.5f), // Cyan
            CraftingRecipe.WorkbenchType.TailoringBench => new Color(0.8f, 0.5f, 0.8f), // Purple
            CraftingRecipe.WorkbenchType.ToolBench => new Color(0.5f, 0.5f, 0.5f), // Gray
            CraftingRecipe.WorkbenchType.Anvil => new Color(0.3f, 0.3f, 0.3f), // Dark gray
            _ => Color.white
        };
    }
}