using System.IO;
using UnityEngine;
using UnityEditor;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.ProjectSetup
{
    /// <summary>
    /// Simple tool to fix or remove assembly definition files
    /// </summary>
    public class SimpleAsmdefFix : EditorWindow
    {
        [MenuItem("Tools/Wild Survival/🔧 Simple Asmdef Fix", priority = -51)]
        public static void ShowWindow()
        {
            var window = GetWindow<SimpleAsmdefFix>("Simple Asmdef Fix");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            GUILayout.Label("Assembly Definition Quick Fix", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Choose an option to fix the assembly definition errors:\n\n" +
                "Option 1: Remove all assembly definitions (quickest)\n" +
                "Option 2: Create simple, working assembly definitions",
                MessageType.Info);

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Option 1: REMOVE All Assembly Definitions", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Confirm Removal",
                    "This will delete all .asmdef files in _WildSurvival folder.\n\n" +
                    "This is the quickest way to fix the errors.\n\n" +
                    "Continue?",
                    "Yes, Remove", "Cancel"))
                {
                    RemoveAllAsmdefs();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Option 2: CREATE Simple Assembly Definitions", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Create Simple Asmdefs",
                    "This will create minimal, working assembly definitions.\n\n" +
                    "Continue?",
                    "Yes, Create", "Cancel"))
                {
                    CreateSimpleAsmdefs();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void RemoveAllAsmdefs()
        {
            string[] searchPaths = {
                "Assets/_WildSurvival",
                "Assets/_Project",
                "Assets"
            };

            int removed = 0;

            foreach (string searchPath in searchPaths)
            {
                if (!AssetDatabase.IsValidFolder(searchPath))
                    continue;

                var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { searchPath });

                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("WildSurvival"))
                    {
                        AssetDatabase.DeleteAsset(path);
                        removed++;
                        Debug.Log($"Removed: {path}");
                    }
                }
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Complete",
                $"Removed {removed} assembly definition files.\n\n" +
                "The errors should now be gone.",
                "OK");
        }

        private void CreateSimpleAsmdefs()
        {
            // Remove old ones first
            RemoveAllAsmdefs();

            // Create only the essential ones with simple JSON
            CreateSimpleAsmdef(
                "Assets/_WildSurvival/Code/Runtime/Core",
                "WildSurvival.Core",
                null,
                false
            );

            CreateSimpleAsmdef(
                "Assets/_WildSurvival/Code/Runtime/Player",
                "WildSurvival.Player",
                new[] { "WildSurvival.Core" },
                false
            );

            CreateSimpleAsmdef(
                "Assets/_WildSurvival/Code/Editor",
                "WildSurvival.Editor",
                new[] { "WildSurvival.Core" },
                true
            );

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Complete",
                "Created minimal assembly definitions.\n\n" +
                "The project should now compile correctly.",
                "OK");
        }

        private void CreateSimpleAsmdef(string folderPath, string asmdefName, string[] references, bool editorOnly)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                // Create the folder if it doesn't exist
                string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
                string folderName = Path.GetFileName(folderPath);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    Directory.CreateDirectory(parent);
                }
                AssetDatabase.CreateFolder(parent, folderName);
            }

            string filePath = Path.Combine(folderPath, asmdefName + ".asmdef");

            // Create simple, minimal JSON
            string json = "{\n";
            json += "    \"name\": \"" + asmdefName + "\",\n";
            json += "    \"rootNamespace\": \"" + asmdefName + "\",\n";
            json += "    \"references\": [";

            if (references != null && references.Length > 0)
            {
                json += "\n";
                for (int i = 0; i < references.Length; i++)
                {
                    json += "        \"" + references[i] + "\"";
                    if (i < references.Length - 1)
                        json += ",";
                    json += "\n";
                }
                json += "    ";
            }

            json += "],\n";

            if (editorOnly)
            {
                json += "    \"includePlatforms\": [\n";
                json += "        \"Editor\"\n";
                json += "    ],\n";
            }
            else
            {
                json += "    \"includePlatforms\": [],\n";
            }

            json += "    \"excludePlatforms\": [],\n";
            json += "    \"allowUnsafeCode\": false,\n";
            json += "    \"overrideReferences\": false,\n";
            json += "    \"precompiledReferences\": [],\n";
            json += "    \"autoReferenced\": true,\n";
            json += "    \"defineConstraints\": [],\n";
            json += "    \"versionDefines\": [],\n";
            json += "    \"noEngineReferences\": false\n";
            json += "}";

            File.WriteAllText(filePath, json);
            Debug.Log($"Created: {filePath}");
        }
    }

    /// <summary>
    /// Alternative: Just delete all broken asmdefs quickly
    /// </summary>
    public static class QuickAsmdefDelete
    {
        [MenuItem("Tools/Wild Survival/❌ Quick Delete All Asmdefs", priority = -52)]
        public static void QuickDelete()
        {
            if (!EditorUtility.DisplayDialog("Quick Delete",
                "This will immediately delete ALL assembly definition files.\n\n" +
                "This is the fastest way to fix the errors.\n\n" +
                "Continue?",
                "Yes, Delete All", "Cancel"))
            {
                return;
            }

            var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            int count = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("WildSurvival") || path.Contains("_WildSurvival"))
                {
                    AssetDatabase.DeleteAsset(path);
                    count++;
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"Deleted {count} assembly definition files. Errors should be resolved.");
            EditorUtility.DisplayDialog("Complete",
                $"Deleted {count} assembly definition files.\n\n" +
                "The compilation errors should now be resolved.",
                "OK");
        }
    }
}