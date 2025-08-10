using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WildSurvival.Crafting;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildSurvival.Database
{
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "WildSurvival/Databases/Recipe Database")]
    public class RecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<RecipeDefinition> recipes = new List<RecipeDefinition>();

        public void AddRecipe(RecipeDefinition recipe)
        {
            if (recipe != null && !recipes.Contains(recipe))
            {
                recipes.Add(recipe);
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
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

        public void Clear()
        {
            recipes.Clear();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public List<RecipeDefinition> GetRecipesByCategory(CraftingCategory category)
        {
            return recipes.Where(r => r.category == category).ToList();
        }

        public List<RecipeDefinition> GetRecipesByWorkstation(WorkstationType workstation)
        {
            return recipes.Where(r => r.requiredWorkstation == workstation).ToList();
        }
    }
}