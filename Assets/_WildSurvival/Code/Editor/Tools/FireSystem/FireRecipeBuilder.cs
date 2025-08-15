using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Tool for creating and managing fire-related recipes
/// </summary>
public class FireRecipeBuilder : EditorWindow
{
    private Vector2 scrollPos;
    private RecipeDefinition currentRecipe;
    private List<RecipeDefinition> fireRecipes = new List<RecipeDefinition>();

    // Recipe creation
    private string newRecipeID = "";
    private string newRecipeName = "";
    private CraftingCategory category = CraftingCategory.Cooking;
    private List<FireRecipeIngredient> ingredients = new List<FireRecipeIngredient>();
    private string outputItemID = "";
    private int outputQuantity = 1;
    private float craftTime = 30f;
    private float requiredTemperature = 200f;

    [MenuItem("Tools/Wild Survival/Fire System/Recipe Builder")]
    public static void ShowWindow()
    {
        var window = GetWindow<FireRecipeBuilder>("🍳 Fire Recipes");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        RefreshRecipeList();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fire Recipe Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawRecipeCreation();
        EditorGUILayout.Space(20);
        DrawExistingRecipes();

        EditorGUILayout.EndScrollView();
    }

    private void DrawRecipeCreation()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Create New Recipe", EditorStyles.boldLabel);

        newRecipeID = EditorGUILayout.TextField("Recipe ID", newRecipeID);
        newRecipeName = EditorGUILayout.TextField("Recipe Name", newRecipeName);
        category = (CraftingCategory)EditorGUILayout.EnumPopup("Category", category);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Ingredients", EditorStyles.boldLabel);

        for (int i = 0; i < ingredients.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            ingredients[i].specificItem = (ItemDefinition)EditorGUILayout.ObjectField(
                ingredients[i].specificItem,
                typeof(ItemDefinition),
                false,
                GUILayout.Width(200)
            );
            ingredients[i].quantity = EditorGUILayout.IntField(ingredients[i].quantity, GUILayout.Width(50));

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                ingredients.RemoveAt(i);
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Ingredient"))
        {
            ingredients.Add(new FireRecipeIngredient());
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

        outputItemID = EditorGUILayout.TextField("Output Item", outputItemID);
        outputQuantity = EditorGUILayout.IntField("Quantity", outputQuantity);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Fire Requirements", EditorStyles.boldLabel);

        requiredTemperature = EditorGUILayout.Slider("Min Temperature", requiredTemperature, 100f, 800f);
        craftTime = EditorGUILayout.Slider("Cook Time (sec)", craftTime, 5f, 300f);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create Recipe", GUILayout.Height(30)))
        {
            CreateRecipe();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawExistingRecipes()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField($"Fire Recipes ({fireRecipes.Count})", EditorStyles.boldLabel);

        foreach (var recipe in fireRecipes)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(recipe.recipeName);

            if (GUILayout.Button("Edit", GUILayout.Width(50)))
            {
                Selection.activeObject = recipe;
            }

            if (GUILayout.Button("Delete", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("Delete Recipe",
                    $"Delete {recipe.recipeName}?", "Yes", "No"))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(recipe));
                    RefreshRecipeList();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void CreateRecipe()
    {
        var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
        recipe.recipeID = newRecipeID;
        recipe.recipeName = newRecipeName;
        recipe.category = category.ToString();
        recipe.ingredients = ConvertToRecipeIngredients(ingredients);
        recipe.baseCraftTime = craftTime;
        recipe.baseTemperature = requiredTemperature;

        // Add output
        //recipe.outputs = new List<RecipeOutput>
        //{
        //    new FireRecipeOutput
        //    {
        //        item = outputItemID,
        //        quantity = outputQuantity,
        //        chance = 1f
        //    }
        //};

        // Replace the outputs creation with:
        var output = new RecipeOutput();
        output.item = GetItemDefinition(outputItemID);
        //output.quantity = outputQuantity;
        // If RecipeOutput has quantityMin/Max instead:
        output.quantityMin = outputQuantity;
        output.quantityMax = outputQuantity;

        recipe.outputs = new RecipeOutput[] { output };

        string path = $"Assets/_WildSurvival/Data/Recipes/Fire_{newRecipeID}.asset";
        AssetDatabase.CreateAsset(recipe, path);
        AssetDatabase.SaveAssets();

        RefreshRecipeList();
        ClearForm();

        EditorUtility.DisplayDialog("Success", $"Recipe '{newRecipeName}' created!", "OK");
    }

    private void RefreshRecipeList()
    {
        fireRecipes.Clear();
        string[] guids = AssetDatabase.FindAssets("t:RecipeDefinition Fire");

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
            if (recipe != null && (recipe.category == "Cooking" || recipe.category == "Smelting"))
            {
                fireRecipes.Add(recipe);
            }
        }
    }

    private void ClearForm()
    {
        newRecipeID = "";
        newRecipeName = "";
        ingredients.Clear();
        outputItemID = "";
        outputQuantity = 1;
    }

    private RecipeIngredient[] ConvertToRecipeIngredients(List<FireRecipeIngredient> fireIngredients)
    {
        var result = new List<RecipeIngredient>();
        foreach (var fireIng in fireIngredients)
        {
            var ingredient = new RecipeIngredient();
            ingredient.specificItem = fireIng.specificItem;
            ingredient.quantity = fireIng.quantity;
            ingredient.consumed = fireIng.consumed;
            result.Add(ingredient);
        }
        return result.ToArray();
    }

    private ItemDefinition GetItemDefinition(string itemID)
    {
        // Try to load from Resources
        ItemDefinition item = Resources.Load<ItemDefinition>($"Items/{itemID}");

        // Try from database
        if (item == null)
        {
            var itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            if (itemDB != null)
            {
                item = itemDB.GetItem(itemID);
            }
        }

        // Find in project
        if (item == null && !string.IsNullOrEmpty(itemID))
        {
            string[] guids = AssetDatabase.FindAssets($"t:ItemDefinition {itemID}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            }
        }

        return item;
    }

}

// Supporting classes
[System.Serializable]
public class FireRecipeIngredient  // Changed from RecipeIngredient
{
    public ItemDefinition specificItem;
    public string name;
    public int quantity = 1;
    public bool consumed = true;
    public ItemCategory category;
}

[System.Serializable]
public class FireRecipeOutput  // Changed from RecipeOutput
{
    public ItemDefinition item;
    public int quantityMin = 1;
    public int quantityMax = 1;
}

public enum CraftingCategory
{
    Cooking,
    Smelting,
    Tools,
    Weapons,
    Building,
    Survival
}

