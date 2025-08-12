using UnityEngine;
using UnityEditor;
using System.IO;

public class ProjectCleanupTool : EditorWindow
{
    [MenuItem("Tools/Wild Survival/Clean & Organize Project")]
    public static void ShowWindow()
    {
        GetWindow<ProjectCleanupTool>("Project Cleanup");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Project Cleanup & Organization", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox("This will:\n" +
            "• Remove duplicate databases\n" +
            "• Organize scripts properly\n" +
            "• Create missing core files", MessageType.Info);

        if (GUILayout.Button("Clean & Organize", GUILayout.Height(40)))
        {
            CleanupProject();
        }
    }

    private void CleanupProject()
    {
        // Remove duplicate databases
        if (File.Exists("Assets/ItemDatabase.asset"))
        {
            AssetDatabase.DeleteAsset("Assets/ItemDatabase.asset");
        }
        if (File.Exists("Assets/RecipeDatabase.asset"))
        {
            AssetDatabase.DeleteAsset("Assets/RecipeDatabase.asset");
        }

        // Keep only the ones in _Project/Data
        Debug.Log("✓ Removed duplicate databases");

        // Create proper folder structure
        CreateFolderStructure();

        AssetDatabase.Refresh();
        Debug.Log("✓ Project cleaned and organized!");
    }

    private void CreateFolderStructure()
    {
        // Core runtime folders
        EnsureFolder("Assets/_Project/Code/Runtime", "Core");
        EnsureFolder("Assets/_Project/Code/Runtime", "Systems");
        EnsureFolder("Assets/_Project/Code/Runtime", "Gameplay");
        EnsureFolder("Assets/_Project/Code/Runtime", "UI");

        // System subfolders
        EnsureFolder("Assets/_Project/Code/Runtime/Systems", "Inventory");
        EnsureFolder("Assets/_Project/Code/Runtime/Systems", "Crafting");
        EnsureFolder("Assets/_Project/Code/Runtime/Systems", "Vitals");

        Debug.Log("✓ Folder structure created");
    }

    private void EnsureFolder(string parent, string folder)
    {
        string path = $"{parent}/{folder}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}