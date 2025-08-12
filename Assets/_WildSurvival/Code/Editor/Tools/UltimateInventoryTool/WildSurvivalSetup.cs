//using UnityEngine;
//using UnityEditor;
//using System.IO;

//namespace WildSurvival.Editor
//{
//    public class WildSurvivalSetup : EditorWindow
//    {
//        [MenuItem("Tools/Wild Survival/Initial Setup")]
//        public static void ShowWindow()
//        {
//            GetWindow<WildSurvivalSetup>("Wild Survival Setup");
//        }

//        private void OnGUI()
//        {
//            EditorGUILayout.LabelField("Wild Survival Setup", EditorStyles.boldLabel);
//            EditorGUILayout.Space(10);

//            if (GUILayout.Button("Create Folder Structure", GUILayout.Height(30)))
//            {
//                CreateFolderStructure();
//            }

//            if (GUILayout.Button("Create Databases", GUILayout.Height(30)))
//            {
//                CreateDatabases();
//            }

//            if (GUILayout.Button("Create Sample Items", GUILayout.Height(30)))
//            {
//                CreateSampleItems();
//            }
//        }

//        private void CreateFolderStructure()
//        {
//            CreateFolder("Assets", "_Project");
//            CreateFolder("Assets/_Project", "Data");
//            CreateFolder("Assets/_Project/Data", "Items");
//            CreateFolder("Assets/_Project/Data", "Recipes");
//            CreateFolder("Assets/_Project/Data", "Databases");

//            AssetDatabase.Refresh();
//            Debug.Log("Folder structure created!");
//        }

//        private void CreateFolder(string parent, string newFolder)
//        {
//            string path = $"{parent}/{newFolder}";
//            if (!AssetDatabase.IsValidFolder(path))
//            {
//                AssetDatabase.CreateFolder(parent, newFolder);
//            }
//        }

//        private void CreateDatabases()
//        {
//            // Create Item Database
//            if (!File.Exists("Assets/_Project/Data/Databases/ItemDatabase.asset"))
//            {
//                var itemDB = ScriptableObject.CreateInstance<WildSurvival.Data.ItemDatabase>();
//                AssetDatabase.CreateAsset(itemDB, "Assets/_Project/Data/Databases/ItemDatabase.asset");
//            }

//            // Create Recipe Database
//            if (!File.Exists("Assets/_Project/Data/Databases/RecipeDatabase.asset"))
//            {
//                var recipeDB = ScriptableObject.CreateInstance<WildSurvival.Data.RecipeDatabase>();
//                AssetDatabase.CreateAsset(recipeDB, "Assets/_Project/Data/Databases/RecipeDatabase.asset");
//            }

//            AssetDatabase.SaveAssets();
//            AssetDatabase.Refresh();
//            Debug.Log("Databases created!");
//        }

//        private void CreateSampleItems()
//        {
//            // Create a sample item
//            var wood = ScriptableObject.CreateInstance<WildSurvival.Data.ItemDefinition>();
//            wood.itemID = "wood_log";
//            wood.displayName = "Wood Log";
//            wood.description = "A sturdy log";
//            wood.primaryCategory = WildSurvival.Data.ItemCategory.Resource;
//            wood.weight = 2f;
//            wood.stackable = true;
//            wood.maxStackSize = 10;
//            wood.gridSize = new Vector2Int(1, 2);
//            wood.InitializeShape();

//            AssetDatabase.CreateAsset(wood, "Assets/_Project/Data/Items/wood_log.asset");

//            // Add to database
//            var itemDB = AssetDatabase.LoadAssetAtPath<WildSurvival.Data.ItemDatabase>(
//                "Assets/_Project/Data/Databases/ItemDatabase.asset");
//            if (itemDB != null)
//            {
//                itemDB.AddItem(wood);
//            }

//            AssetDatabase.SaveAssets();
//            Debug.Log("Sample items created!");
//        }
//    }
//}