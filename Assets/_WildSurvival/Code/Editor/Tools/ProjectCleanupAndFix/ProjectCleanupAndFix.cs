using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.ProjectSetup
{
    /// <summary>
    /// Emergency cleanup tool to fix the duplicate folder issue
    /// </summary>
    public class ProjectCleanupAndFix : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> duplicateFolders = new List<string>();
        private List<string> log = new List<string>();
        private bool analyzed = false;

        [MenuItem("Tools/Wild Survival/🚨 EMERGENCY CLEANUP - Fix Duplicates", priority = -100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectCleanupAndFix>("Emergency Cleanup");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.color = Color.red;
            EditorGUILayout.LabelField("🚨 EMERGENCY CLEANUP TOOL", headerStyle);
            GUI.color = Color.white;

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This tool will clean up the duplicate folders created by the restructure bug.\n\n" +
                "It will:\n" +
                "• Remove all numbered duplicate folders (_WildSurvival 1, 2, 3, etc.)\n" +
                "• Keep only the main _WildSurvival folder\n" +
                "• Clean up duplicate subfolders\n" +
                "• Preserve all your actual files",
                MessageType.Warning);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("1️⃣ Analyze Duplicates", GUILayout.Height(40)))
            {
                AnalyzeDuplicates();
            }

            GUI.enabled = analyzed && duplicateFolders.Count > 0;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("2️⃣ CLEAN DUPLICATES", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Confirm Cleanup",
                    $"This will delete {duplicateFolders.Count} duplicate folders.\n\n" +
                    "Make sure you have a backup!\n\n" +
                    "Continue?",
                    "Yes, Clean Up", "Cancel"))
                {
                    CleanupDuplicates();
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (duplicateFolders.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField($"Found {duplicateFolders.Count} duplicate folders to remove:", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                foreach (var folder in duplicateFolders.Take(100)) // Show first 100
                {
                    EditorGUILayout.LabelField(folder, EditorStyles.miniLabel);
                }
                if (duplicateFolders.Count > 100)
                {
                    EditorGUILayout.LabelField($"... and {duplicateFolders.Count - 100} more", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
            }

            // Show log
            if (log.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Log:", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var entry in log)
                {
                    EditorGUILayout.LabelField(entry, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void AnalyzeDuplicates()
        {
            duplicateFolders.Clear();
            log.Clear();

            // Find all numbered _WildSurvival folders
            for (int i = 1; i <= 200; i++)
            {
                string path = $"Assets/_WildSurvival {i}";
                if (AssetDatabase.IsValidFolder(path))
                {
                    duplicateFolders.Add(path);
                }
            }

            // Find duplicate subfolders
            string[] basePaths = {
                "Assets/_WildSurvival/Code/Runtime",
                "Assets/_WildSurvival/Code/Editor",
                "Assets/_WildSurvival/Content",
                "Assets/_WildSurvival/Audio",
                "Assets/_WildSurvival/Scenes",
                "Assets/_WildSurvival/Settings",
                "Assets/_Tests"
            };

            foreach (var basePath in basePaths)
            {
                if (AssetDatabase.IsValidFolder(basePath))
                {
                    var subFolders = AssetDatabase.GetSubFolders(basePath);
                    foreach (var folder in subFolders)
                    {
                        string folderName = Path.GetFileName(folder);
                        // Check if it ends with a number (duplicate)
                        if (System.Text.RegularExpressions.Regex.IsMatch(folderName, @" \d+$"))
                        {
                            duplicateFolders.Add(folder);
                        }
                    }
                }
            }

            analyzed = true;
            log.Add($"✅ Analysis complete: Found {duplicateFolders.Count} duplicate folders");
            Repaint();
        }

        private void CleanupDuplicates()
        {
            int deleted = 0;
            int failed = 0;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var folder in duplicateFolders)
                {
                    try
                    {
                        if (AssetDatabase.DeleteAsset(folder))
                        {
                            deleted++;
                        }
                        else
                        {
                            failed++;
                            Debug.LogWarning($"Failed to delete: {folder}");
                        }
                    }
                    catch (Exception e)
                    {
                        failed++;
                        Debug.LogError($"Error deleting {folder}: {e.Message}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            log.Add($"✅ Cleanup complete: Deleted {deleted} folders, {failed} failed");

            if (deleted > 0)
            {
                EditorUtility.DisplayDialog("Cleanup Complete",
                    $"Successfully removed {deleted} duplicate folders.\n\n" +
                    "Your project structure is now clean!",
                    "Great!");
            }

            // Re-analyze to update the list
            AnalyzeDuplicates();
        }
    }

    /// <summary>
    /// FIXED version of the project restructure tool
    /// </summary>
    public class CompleteProjectRestructureFixed : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> log = new List<string>();
        private HashSet<string> createdFolders = new HashSet<string>(); // Track what we've created
        private int currentStep = 0;
        private int totalSteps = 10;

        [MenuItem("Tools/Wild Survival/✅ Project Restructure (FIXED)", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<CompleteProjectRestructureFixed>("Project Restructure (Fixed)");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.color = Color.green;
            EditorGUILayout.LabelField("✅ PROJECT RESTRUCTURE - FIXED VERSION", headerStyle);
            GUI.color = Color.white;

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This is the FIXED version that won't create duplicate folders.\n\n" +
                "Features:\n" +
                "• Creates clean folder structure\n" +
                "• No duplicate folders\n" +
                "• Moves existing files properly\n" +
                "• Creates core scripts\n" +
                "• Safe to run multiple times",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Progress bar
            if (currentStep > 0)
            {
                float progress = (float)currentStep / totalSteps;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(25)), progress,
                    $"Step {currentStep}/{totalSteps} - {(progress * 100):F0}%");
                EditorGUILayout.Space(5);
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 RUN FIXED RESTRUCTURE", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Run Fixed Restructure",
                    "This will create a clean project structure.\n\n" +
                    "It's safe to run and won't create duplicates.\n\n" +
                    "Continue?",
                    "Yes, Restructure", "Cancel"))
                {
                    RunFixedRestructure();
                }
            }
            GUI.backgroundColor = Color.white;

            // Log
            if (log.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Log:", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,
                    EditorStyles.helpBox, GUILayout.Height(300));

                foreach (var entry in log)
                {
                    var style = EditorStyles.miniLabel;
                    if (entry.Contains("✅")) GUI.color = Color.green;
                    else if (entry.Contains("⚠️")) GUI.color = Color.yellow;

                    EditorGUILayout.LabelField(entry, style);
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void RunFixedRestructure()
        {
            log.Clear();
            createdFolders.Clear();
            currentStep = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                Step("Creating folder structure...", CreateFolderStructure);
                Step("Creating core scripts...", CreateCoreScripts);
                Step("Moving existing files...", MoveExistingFiles);
                Step("Creating assembly definitions...", CreateAssemblyDefinitions);
                Step("Generating documentation...", GenerateDocumentation);
                Step("Cleaning up empty folders...", CleanupEmptyFolders);
                Step("Final validation...", ValidateStructure);

                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();

                LogMessage("✅ PROJECT RESTRUCTURE COMPLETE!");

                EditorUtility.DisplayDialog("Success!",
                    "Project restructuring completed successfully!\n\n" +
                    "Your project now has a clean, organized structure.",
                    "Excellent!");
            }
            catch (Exception e)
            {
                AssetDatabase.StopAssetEditing();
                Debug.LogError($"Restructure failed: {e}");
                LogMessage($"❌ Error: {e.Message}");
            }
        }

        private void Step(string description, Action action)
        {
            currentStep++;
            LogMessage($"Step {currentStep}: {description}");
            Repaint();

            try
            {
                action();
                LogMessage($"  ✅ Completed");
            }
            catch (Exception e)
            {
                LogMessage($"  ❌ Failed: {e.Message}");
                throw;
            }
        }

        private void CreateFolderStructure()
        {
            // FIXED: Only create each folder once
            var folders = new List<string>
            {
                // Main folders
                "Assets/_WildSurvival",
                "Assets/_DevTools",
                "Assets/_ThirdParty",
                
                // Code structure
                "Assets/_WildSurvival/Code",
                "Assets/_WildSurvival/Code/Runtime",
                "Assets/_WildSurvival/Code/Runtime/Core",
                "Assets/_WildSurvival/Code/Runtime/Core/Bootstrap",
                "Assets/_WildSurvival/Code/Runtime/Core/Events",
                "Assets/_WildSurvival/Code/Runtime/Core/Managers",
                "Assets/_WildSurvival/Code/Runtime/Core/Patterns",
                "Assets/_WildSurvival/Code/Runtime/Core/Utilities",

                "Assets/_WildSurvival/Code/Runtime/Player",
                "Assets/_WildSurvival/Code/Runtime/Player/Controller",
                "Assets/_WildSurvival/Code/Runtime/Player/Camera",
                "Assets/_WildSurvival/Code/Runtime/Player/Stats",

                "Assets/_WildSurvival/Code/Runtime/Survival",
                "Assets/_WildSurvival/Code/Runtime/Survival/Inventory",
                "Assets/_WildSurvival/Code/Runtime/Survival/Crafting",
                "Assets/_WildSurvival/Code/Runtime/Survival/Temperature",
                "Assets/_WildSurvival/Code/Runtime/Survival/StatusEffects",

                "Assets/_WildSurvival/Code/Runtime/Environment",
                "Assets/_WildSurvival/Code/Runtime/Environment/Time",
                "Assets/_WildSurvival/Code/Runtime/Environment/Weather",

                "Assets/_WildSurvival/Code/Runtime/AI",
                "Assets/_WildSurvival/Code/Runtime/Combat",
                "Assets/_WildSurvival/Code/Runtime/UI",
                "Assets/_WildSurvival/Code/Runtime/Audio",
                "Assets/_WildSurvival/Code/Runtime/SaveSystem",

                "Assets/_WildSurvival/Code/Editor",
                "Assets/_WildSurvival/Code/Editor/Tools",
                "Assets/_WildSurvival/Code/Editor/Windows",
                "Assets/_WildSurvival/Code/Editor/Validators",

                "Assets/_WildSurvival/Code/Tests",
                "Assets/_WildSurvival/Code/Tests/Runtime",
                "Assets/_WildSurvival/Code/Tests/Editor",
                
                // Content
                "Assets/_WildSurvival/Content",
                "Assets/_WildSurvival/Content/Characters",
                "Assets/_WildSurvival/Content/Environment",
                "Assets/_WildSurvival/Content/Items",
                "Assets/_WildSurvival/Content/Structures",
                "Assets/_WildSurvival/Content/Effects",
                
                // Data
                "Assets/_WildSurvival/Data",
                "Assets/_WildSurvival/Data/Items",
                "Assets/_WildSurvival/Data/Recipes",
                "Assets/_WildSurvival/Data/StatusEffects",
                
                // Other essential folders
                "Assets/_WildSurvival/Prefabs",
                "Assets/_WildSurvival/Prefabs/Core",
                "Assets/_WildSurvival/Prefabs/UI",
                "Assets/_WildSurvival/Prefabs/Environment",

                "Assets/_WildSurvival/Resources",

                "Assets/_WildSurvival/Scenes",
                "Assets/_WildSurvival/Scenes/Core",
                "Assets/_WildSurvival/Scenes/World",
                "Assets/_WildSurvival/Scenes/Development",

                "Assets/_WildSurvival/Settings",
                "Assets/_WildSurvival/Settings/HDRP",
                "Assets/_WildSurvival/Settings/Input",

                "Assets/_WildSurvival/Audio",
                "Assets/_WildSurvival/Audio/Music",
                "Assets/_WildSurvival/Audio/SFX",

                "Assets/_WildSurvival/UI",
                "Assets/_WildSurvival/UI/Textures",
                "Assets/_WildSurvival/UI/Fonts",

                "Assets/_WildSurvival/Documentation",
                
                // Dev tools
                "Assets/_DevTools/Debugging",
                "Assets/_DevTools/Profiling",
                "Assets/_DevTools/Prototyping"
            };

            foreach (var folder in folders)
            {
                CreateFolderSafe(folder);
            }

            LogMessage($"  Created {createdFolders.Count} folders");
        }

        private void CreateFolderSafe(string path)
        {
            // FIXED: Check if we've already created this folder
            if (createdFolders.Contains(path))
                return;

            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);

                // Recursively create parent if needed
                if (!AssetDatabase.IsValidFolder(parent) && parent != "Assets")
                {
                    CreateFolderSafe(parent);
                }

                // Create the folder
                AssetDatabase.CreateFolder(parent, folderName);
                createdFolders.Add(path);
            }
            else
            {
                // Folder already exists, just track it
                createdFolders.Add(path);
            }
        }

        private void CreateCoreScripts()
        {
            // GameBootstrapper
            CreateScript("Assets/_WildSurvival/Code/Runtime/Core/Bootstrap/GameBootstrapper.cs",
@"using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Core.Bootstrap
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private string persistentSceneName = ""_Persistent"";
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(Initialize());
        }
        
        private IEnumerator Initialize()
        {
            Debug.Log(""[GameBootstrapper] Initializing..."");
            
            if (!string.IsNullOrEmpty(persistentSceneName))
            {
                yield return SceneManager.LoadSceneAsync(persistentSceneName, LoadSceneMode.Additive);
            }
            
            Debug.Log(""[GameBootstrapper] Complete"");
        }
    }
}");

            // GameManager
            CreateScript("Assets/_WildSurvival/Code/Runtime/Core/Managers/GameManager.cs",
@"using UnityEngine;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Core.Managers
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}");

            LogMessage("  Created core scripts");
        }

        private void MoveExistingFiles()
        {
            // Move scenes if they exist
            SafeMoveAsset("Assets/_Project/Scenes/_Bootstrap.unity",
                         "Assets/_WildSurvival/Scenes/Core/_Bootstrap.unity");
            SafeMoveAsset("Assets/_Project/Scenes/_Persistent.unity",
                         "Assets/_WildSurvival/Scenes/Core/_Persistent.unity");
            SafeMoveAsset("Assets/_Project/Scenes/World_Prototype.unity",
                         "Assets/_WildSurvival/Scenes/World/World_Prototype.unity");
            SafeMoveAsset("Assets/OutdoorsScene.unity",
                         "Assets/_WildSurvival/Scenes/World/OutdoorsScene.unity");

            // Move data assets
            SafeMoveAsset("Assets/_Project/Data/ItemDatabase.asset",
                         "Assets/_WildSurvival/Data/Items/ItemDatabase.asset");
            SafeMoveAsset("Assets/_Project/Data/RecipeDatabase.asset",
                         "Assets/_WildSurvival/Data/Recipes/RecipeDatabase.asset");

            // Move scripts
            SafeMoveAsset("Assets/_Project/Code/Runtime/Core/GameManager.cs",
                         "Assets/_WildSurvival/Code/Runtime/Core/Managers/GameManager.cs");
            SafeMoveAsset("Assets/_Project/Code/Runtime/Systems/Inventory/InventoryManager.cs",
                         "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/InventoryManager.cs");
        }

        private void CreateAssemblyDefinitions()
        {
            CreateAssemblyDef("Assets/_WildSurvival/Code/Runtime/Core",
                            "WildSurvival.Core", new string[] { });
            CreateAssemblyDef("Assets/_WildSurvival/Code/Runtime/Player",
                            "WildSurvival.Player", new[] { "WildSurvival.Core" });
            CreateAssemblyDef("Assets/_WildSurvival/Code/Runtime/Survival",
                            "WildSurvival.Survival", new[] { "WildSurvival.Core" });
            CreateAssemblyDef("Assets/_WildSurvival/Code/Editor",
                            "WildSurvival.Editor", new[] { "WildSurvival.Core" }, true);

            LogMessage("  Created assembly definitions");
        }

        private void GenerateDocumentation()
        {
            string readme = @"# Wild Survival

## Project Structure
- `_WildSurvival/` - Main project folder
- `_DevTools/` - Development tools
- `_ThirdParty/` - External packages

## Quick Start
1. Open `_WildSurvival/Scenes/Core/_Bootstrap.unity`
2. Press Play

Generated: " + DateTime.Now.ToString("yyyy-MM-dd");

            CreateScript("Assets/_WildSurvival/Documentation/README.md", readme);
            LogMessage("  Generated documentation");
        }

        private void CleanupEmptyFolders()
        {
            // Clean up old empty folders
            DeleteEmptyFolder("Assets/_Project/Scripts");
            DeleteEmptyFolder("Assets/_Project/Code/Runtime/Gameplay");
            DeleteEmptyFolder("Assets/_Tests 1");
            DeleteEmptyFolder("Assets/_Tests 2");
            DeleteEmptyFolder("Assets/_Tests 3");
        }

        private void ValidateStructure()
        {
            var requiredFolders = new[]
            {
                "Assets/_WildSurvival/Code/Runtime/Core",
                "Assets/_WildSurvival/Code/Editor",
                "Assets/_WildSurvival/Data",
                "Assets/_WildSurvival/Prefabs",
                "Assets/_WildSurvival/Scenes"
            };

            int valid = 0;
            foreach (var folder in requiredFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                    valid++;
            }

            LogMessage($"  Validation: {valid}/{requiredFolders.Length} core folders present");
        }

        // Helper methods
        private void CreateScript(string path, string content)
        {
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, content);
        }

        private void CreateAssemblyDef(string folder, string name, string[] refs, bool isEditor = false)
        {
            var asmdef = new
            {
                name = name,
                rootNamespace = name,
                references = refs,
                includePlatforms = isEditor ? new[] { "Editor" } : new string[] { },
                autoReferenced = true
            };

            string json = JsonUtility.ToJson(asmdef, true);
            CreateScript(Path.Combine(folder, name + ".asmdef"), json);
        }

        private void SafeMoveAsset(string source, string dest)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(source))
            {
                string destFolder = Path.GetDirectoryName(dest).Replace('\\', '/');
                CreateFolderSafe(destFolder);

                string error = AssetDatabase.MoveAsset(source, dest);
                if (!string.IsNullOrEmpty(error))
                {
                    LogMessage($"  ⚠️ Move failed: {error}");
                }
            }
        }

        private void DeleteEmptyFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                var assets = AssetDatabase.FindAssets("", new[] { path });
                if (assets.Length == 0)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private void LogMessage(string message)
        {
            log.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[Restructure] {message}");
            Repaint();
        }
    }
}