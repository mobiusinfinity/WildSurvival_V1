using UnityEditor;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Run this from menu or create manually
    [MenuItem("Tools/Wild Survival/Setup/Create Fresh Databases")]
    public static void CreateFreshDatabases()
    {
        // Create ItemDatabase
        string itemDbPath = "Assets/_WildSurvival/Code/Runtime/Data/Databases/ItemDatabase.asset";

        if (!AssetDatabase.LoadAssetAtPath<ItemDatabase>(itemDbPath))
        {
            ItemDatabase itemDb = ScriptableObject.CreateInstance<ItemDatabase>();

            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(itemDbPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            AssetDatabase.CreateAsset(itemDb, itemDbPath);
            Debug.Log("✅ Created ItemDatabase at: " + itemDbPath);
        }

        // Create RecipeDatabase
        string recipeDbPath = "Assets/_WildSurvival/Code/Runtime/Data/Databases/RecipeDatabase.asset";

        if (!AssetDatabase.LoadAssetAtPath<RecipeDatabase>(recipeDbPath))
        {
            RecipeDatabase recipeDb = ScriptableObject.CreateInstance<RecipeDatabase>();
            AssetDatabase.CreateAsset(recipeDb, recipeDbPath);
            Debug.Log("✅ Created RecipeDatabase at: " + recipeDbPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✨ Databases ready!");
    }
}
