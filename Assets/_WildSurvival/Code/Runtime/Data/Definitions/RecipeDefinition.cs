using System;
using UnityEngine;

/// <summary>
/// Defines a crafting recipe for the Ultimate Inventory Tool
/// </summary>
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

// Enums for RecipeDefinition
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