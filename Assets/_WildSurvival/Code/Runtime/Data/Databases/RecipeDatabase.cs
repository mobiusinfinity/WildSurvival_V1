using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ScriptableObject database for all crafting recipes
/// Part of the Ultimate Inventory Tool
/// </summary>
[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Wild Survival/Databases/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    [SerializeField] private List<RecipeDefinition> recipes = new List<RecipeDefinition>();

    public bool AddRecipe(RecipeDefinition recipe)
    {
        if (recipe == null)
        {
            Debug.LogError("Cannot add null recipe to database!");
            return false;
        }

        // Check for duplicate ID
        if (recipes.Any(r => r != null && r.recipeID == recipe.recipeID))
        {
            Debug.LogError($"Recipe with ID '{recipe.recipeID}' already exists in database!");
            return false;
        }

        // Check for duplicate asset
        if (recipes.Contains(recipe))
        {
            Debug.LogWarning($"Recipe '{recipe.recipeID}' is already in the database!");
            return false;
        }

        recipes.Add(recipe);
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
        Debug.Log($"✅ Added recipe '{recipe.recipeName}' (ID: {recipe.recipeID}) to database");
        return true;
    }

    public bool HasRecipeWithID(string recipeID)
    {
        return recipes.Any(r => r != null && r.recipeID == recipeID);
    }

    // Clean up method
    public void CleanDatabase()
    {
        recipes.RemoveAll(r => r == null);

        var seen = new HashSet<string>();
        var uniqueRecipes = new List<RecipeDefinition>();

        foreach (var recipe in recipes)
        {
            if (!seen.Contains(recipe.recipeID))
            {
                seen.Add(recipe.recipeID);
                uniqueRecipes.Add(recipe);
            }
            else
            {
                Debug.LogWarning($"Removed duplicate recipe: {recipe.recipeID}");
            }
        }

        recipes = uniqueRecipes;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void RemoveRecipe(RecipeDefinition recipe)
    {
        if (recipes.Contains(recipe))
        {
            recipes.Remove(recipe);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
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

    public RecipeDefinition GetRecipeByName(string recipeName)
    {
        return recipes.FirstOrDefault(r => r.recipeName == recipeName);
    }

    //public List<RecipeDefinition> GetRecipesByCategory(CraftingCategory category)
    //{
    //    return recipes.Where(r => r.category == category).ToList();
    //}

    public List<RecipeDefinition> GetRecipesByCategory(CraftingCategory category)
    {
        // Convert enum to string for comparison
        string categoryString = category.ToString();
        return recipes.Where(r => r.category == categoryString).ToList();
    }

    public List<RecipeDefinition> GetRecipesByWorkstation(WorkstationType workstation)
    {
        if (workstation == WorkstationType.None)
        {
            // Return recipes that don't require a workstation
            return recipes.Where(r => r.requiredWorkstation == WorkstationType.None).ToList();
        }
        else
        {
            // Return recipes that can be made at this workstation
            return recipes.Where(r => r.requiredWorkstation == workstation).ToList();
        }
    }

    public List<RecipeDefinition> GetKnownRecipes()
    {
        return recipes.Where(r => r.isKnownByDefault).ToList();
    }

    public void Clear()
    {
        recipes.Clear();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    // In RecipeDatabase.cs, add this method:
    public void ClearAllRecipes()
    {
        recipes.Clear();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    // Validation
    private void OnValidate()
    {
        // Remove null entries
        recipes.RemoveAll(r => r == null);

        // Check for duplicates
        var duplicates = recipes.GroupBy(r => r?.recipeID)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var dup in duplicates)
        {
            if (!string.IsNullOrEmpty(dup))
            {
                Debug.LogWarning($"[RecipeDatabase] Duplicate recipe ID found: {dup}");
            }
        }
    }

    // Editor helpers
#if UNITY_EDITOR
    public void RefreshDatabase()
    {
        // Find all RecipeDefinition assets in project
        string[] guids = AssetDatabase.FindAssets("t:RecipeDefinition");

        recipes.Clear();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
            if (recipe != null)
            {
                recipes.Add(recipe);
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"[RecipeDatabase] Refreshed with {recipes.Count} recipes");
    }
#endif
}

