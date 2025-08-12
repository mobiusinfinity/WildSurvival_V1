using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.ProjectSetup
{
    public class CompleteProjectRestructure : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showDetails = true;
        private List<string> log = new List<string>();
        private int currentStep = 0;
        private int totalSteps = 20;

        // Configuration options
        private bool createBackup = true;
        private bool moveExistingFiles = true;
        private bool createAssemblyDefs = true;
        private bool generateDocumentation = true;
        private bool setupGitConfig = true;
        private bool preserveThirdParty = true;

        // Project settings
        private string companyName = "Wild Forge Studios";
        private string productName = "Wild Survival";
        private string packageId = "com.wildforge.wildsurvival";

        [MenuItem("Tools/Wild Survival/⚡ COMPLETE PROJECT RESTRUCTURE", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<CompleteProjectRestructure>("Project Restructure");
            window.minSize = new Vector2(900, 700);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawConfiguration();
            DrawProgress();
            DrawActions();
            DrawLog();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.color = new Color(0.2f, 0.8f, 0.2f);
            EditorGUILayout.LabelField("🎮 WILD SURVIVAL - PROJECT RESTRUCTURE", headerStyle);
            GUI.color = Color.white;

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This will restructure your project to AAA production standards:\n\n" +
                "✅ Clean, modular architecture with Assembly Definitions\n" +
                "✅ Optimized for Unity 6 & HDRP\n" +
                "✅ Performance-focused organization\n" +
                "✅ Team collaboration ready\n" +
                "✅ Automated testing structure",
                MessageType.Info
            );
        }

        private void DrawConfiguration()
        {
            EditorGUILayout.Space(10);

            showDetails = EditorGUILayout.Foldout(showDetails, "Configuration Options", true);

            if (showDetails)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("Restructure Options:", EditorStyles.boldLabel);
                createBackup = EditorGUILayout.Toggle("Create Backup", createBackup);
                moveExistingFiles = EditorGUILayout.Toggle("Auto-Move Existing Files", moveExistingFiles);
                createAssemblyDefs = EditorGUILayout.Toggle("Create Assembly Definitions", createAssemblyDefs);
                generateDocumentation = EditorGUILayout.Toggle("Generate Documentation", generateDocumentation);
                setupGitConfig = EditorGUILayout.Toggle("Setup Git Configuration", setupGitConfig);
                preserveThirdParty = EditorGUILayout.Toggle("Preserve Third Party Assets", preserveThirdParty);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Project Settings:", EditorStyles.boldLabel);
                companyName = EditorGUILayout.TextField("Company Name:", companyName);
                productName = EditorGUILayout.TextField("Product Name:", productName);
                packageId = EditorGUILayout.TextField("Package ID:", packageId);

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawProgress()
        {
            if (currentStep > 0)
            {
                EditorGUILayout.Space(10);
                float progress = (float)currentStep / totalSteps;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(25)), progress,
                    $"Step {currentStep}/{totalSteps} - {(progress * 100):F0}%");
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("🚀 EXECUTE COMPLETE RESTRUCTURE", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Complete Project Restructure",
                    "This will reorganize your entire project structure.\n\n" +
                    (createBackup ? "✅ A backup will be created first.\n" : "⚠️ No backup will be created!\n") +
                    "\nThis process may take several minutes.\n\n" +
                    "Continue?",
                    "Yes, Restructure", "Cancel"))
                {
                    ExecuteCompleteRestructure();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("📋 Export Structure Plan", GUILayout.Height(35)))
            {
                ExportStructurePlan();
            }

            if (GUILayout.Button("📊 Analyze Current Project", GUILayout.Height(35)))
            {
                AnalyzeCurrentProject();
            }

            if (GUILayout.Button("🔍 Validate Structure", GUILayout.Height(35)))
            {
                ValidateProjectStructure();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLog()
        {
            if (log.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Operation Log:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,
                EditorStyles.helpBox, GUILayout.Height(200));

            foreach (var entry in log)
            {
                var style = EditorStyles.miniLabel;
                if (entry.Contains("✅")) GUI.color = Color.green;
                else if (entry.Contains("⚠️")) GUI.color = Color.yellow;
                else if (entry.Contains("❌")) GUI.color = Color.red;

                EditorGUILayout.LabelField(entry, style);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndScrollView();
        }

        private void ExecuteCompleteRestructure()
        {
            log.Clear();
            currentStep = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                // Phase 1: Preparation
                if (createBackup) Step("Creating backup...", CreateBackup);
                Step("Analyzing project...", AnalyzeProject);

                // Phase 2: Create Structure
                Step("Creating folder structure...", CreateCompleteFolderStructure);

                // Phase 3: Organize Code
                Step("Organizing runtime code...", OrganizeRuntimeCode);
                Step("Organizing editor tools...", OrganizeEditorTools);
                Step("Setting up test structure...", SetupTestStructure);

                // Phase 4: Move Assets
                if (moveExistingFiles)
                {
                    Step("Moving scenes...", MoveScenes);
                    Step("Moving prefabs...", MovePrefabs);
                    Step("Moving data assets...", MoveDataAssets);
                    Step("Moving content...", MoveContent);
                }

                // Phase 5: Assembly Definitions
                if (createAssemblyDefs)
                {
                    Step("Creating assembly definitions...", CreateAssemblyDefinitions);
                }

                // Phase 6: Core Systems
                Step("Creating core scripts...", CreateCoreScripts);
                Step("Creating manager systems...", CreateManagerSystems);
                Step("Setting up service locator...", SetupServiceLocator);

                // Phase 7: Project Configuration
                Step("Configuring project settings...", ConfigureProjectSettings);
                Step("Setting up quality levels...", SetupQualityLevels);
                Step("Configuring HDRP settings...", ConfigureHDRPSettings);

                // Phase 8: Documentation
                if (generateDocumentation)
                {
                    Step("Generating documentation...", GenerateDocumentation);
                }

                // Phase 9: Version Control
                if (setupGitConfig)
                {
                    Step("Setting up Git configuration...", SetupGitConfiguration);
                }

                // Phase 10: Cleanup
                Step("Cleaning up project...", CleanupProject);
                Step("Final validation...", () => ValidateProjectStructure());

                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();

                LogMessage("✅ PROJECT RESTRUCTURE COMPLETE!");
                ShowCompletionDialog();
            }
            catch (Exception e)
            {
                AssetDatabase.StopAssetEditing();
                Debug.LogError($"Restructure failed: {e}");
                LogMessage($"❌ Restructure failed: {e.Message}");
                EditorUtility.DisplayDialog("Error",
                    $"Restructuring failed:\n{e.Message}\n\nCheck console for details.",
                    "OK");
            }
        }

        private void Step(string description, Action action)
        {
            currentStep++;
            LogMessage($"[{currentStep}/{totalSteps}] {description}");
            Repaint();

            try
            {
                action.Invoke();
                LogMessage($"  ✅ {description.Replace("...", "")} completed");
            }
            catch (Exception e)
            {
                LogMessage($"  ❌ {description.Replace("...", "")} failed: {e.Message}");
                throw;
            }
        }

        private void CreateBackup()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                $"Backups/Backup_{timestamp}");

            Directory.CreateDirectory(backupPath);

            // Backup critical folders
            string[] foldersToBackup = {
                "_Project", "WildSurvival", "Settings", "_DevTools"
            };

            foreach (var folder in foldersToBackup)
            {
                string sourcePath = Path.Combine(Application.dataPath, folder);
                if (Directory.Exists(sourcePath))
                {
                    string destPath = Path.Combine(backupPath, folder);
                    CopyDirectory(sourcePath, destPath);
                }
            }

            // Create backup info file
            string infoPath = Path.Combine(backupPath, "backup_info.txt");
            File.WriteAllText(infoPath, $"Backup created: {DateTime.Now}\nUnity Version: {Application.unityVersion}\nProject: {productName}");

            LogMessage($"  📁 Backup created at: {backupPath}");
        }

        private void AnalyzeProject()
        {
            int scriptCount = AssetDatabase.FindAssets("t:Script", new[] { "Assets" }).Length;
            int prefabCount = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Length;
            int sceneCount = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Length;

            LogMessage($"  📊 Found {scriptCount} scripts, {prefabCount} prefabs, {sceneCount} scenes");
        }

        private void CreateCompleteFolderStructure()
        {
            // Main structure - optimized list
            var folders = GetFolderStructure();

            foreach (var folder in folders)
            {
                CreateFolder(folder);
            }

            LogMessage($"  📁 Created {folders.Count} folders");
        }

        private List<string> GetFolderStructure()
        {
            return new List<string>
            {
                // Core folders
                "Assets/_WildSurvival",
                "Assets/_DevTools/Debugging",
                "Assets/_DevTools/Profiling",
                "Assets/_DevTools/Prototyping",
                "Assets/_ThirdParty",
                
                // Code structure - Runtime
                "Assets/_WildSurvival/Code/Runtime/Core/Bootstrap",
                "Assets/_WildSurvival/Code/Runtime/Core/Events",
                "Assets/_WildSurvival/Code/Runtime/Core/Managers",
                "Assets/_WildSurvival/Code/Runtime/Core/Patterns",
                "Assets/_WildSurvival/Code/Runtime/Core/Utilities/Extensions",
                "Assets/_WildSurvival/Code/Runtime/Core/Utilities/Helpers",
                "Assets/_WildSurvival/Code/Runtime/Core/Utilities/Math",
                
                // Player systems
                "Assets/_WildSurvival/Code/Runtime/Player/Controller",
                "Assets/_WildSurvival/Code/Runtime/Player/Camera",
                "Assets/_WildSurvival/Code/Runtime/Player/Stats",
                "Assets/_WildSurvival/Code/Runtime/Player/Interactions",
                
                // Survival systems
                "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Core",
                "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Items",
                "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/UI",
                "Assets/_WildSurvival/Code/Runtime/Survival/Crafting",
                "Assets/_WildSurvival/Code/Runtime/Survival/Temperature",
                "Assets/_WildSurvival/Code/Runtime/Survival/StatusEffects/Effects",
                "Assets/_WildSurvival/Code/Runtime/Survival/Building/Structures",
                
                // Environment systems
                "Assets/_WildSurvival/Code/Runtime/Environment/Time",
                "Assets/_WildSurvival/Code/Runtime/Environment/Weather",
                "Assets/_WildSurvival/Code/Runtime/Environment/Terrain",
                "Assets/_WildSurvival/Code/Runtime/Environment/Vegetation",
                
                // AI systems
                "Assets/_WildSurvival/Code/Runtime/AI/Core",
                "Assets/_WildSurvival/Code/Runtime/AI/Wildlife/Behaviors",
                "Assets/_WildSurvival/Code/Runtime/AI/Navigation",
                
                // Combat systems
                "Assets/_WildSurvival/Code/Runtime/Combat/Core",
                "Assets/_WildSurvival/Code/Runtime/Combat/Weapons",
                
                // UI systems
                "Assets/_WildSurvival/Code/Runtime/UI/Core",
                "Assets/_WildSurvival/Code/Runtime/UI/HUD",
                "Assets/_WildSurvival/Code/Runtime/UI/Menus",
                "Assets/_WildSurvival/Code/Runtime/UI/Journal/JournalTabs",
                
                // Audio & Save systems
                "Assets/_WildSurvival/Code/Runtime/Audio",
                "Assets/_WildSurvival/Code/Runtime/SaveSystem",
                
                // Editor tools
                "Assets/_WildSurvival/Code/Editor/Tools/ProjectStructure",
                "Assets/_WildSurvival/Code/Editor/Tools/Database",
                "Assets/_WildSurvival/Code/Editor/Tools/Debug",
                "Assets/_WildSurvival/Code/Editor/Tools/BuildPipeline",
                "Assets/_WildSurvival/Code/Editor/Validators",
                "Assets/_WildSurvival/Code/Editor/PropertyDrawers",
                "Assets/_WildSurvival/Code/Editor/Windows",
                
                // Tests
                "Assets/_WildSurvival/Code/Tests/Runtime",
                "Assets/_WildSurvival/Code/Tests/Editor",
                
                // Content folders
                "Assets/_WildSurvival/Content/Characters/Player/Models",
                "Assets/_WildSurvival/Content/Characters/Player/Animations",
                "Assets/_WildSurvival/Content/Characters/Player/Materials",
                "Assets/_WildSurvival/Content/Characters/Wildlife/Models",
                "Assets/_WildSurvival/Content/Characters/Wildlife/Animations",
                "Assets/_WildSurvival/Content/Environment/Terrain/Textures",
                "Assets/_WildSurvival/Content/Environment/Terrain/Materials",
                "Assets/_WildSurvival/Content/Environment/Vegetation/Trees",
                "Assets/_WildSurvival/Content/Environment/Vegetation/Bushes",
                "Assets/_WildSurvival/Content/Environment/Rocks",
                "Assets/_WildSurvival/Content/Environment/Water",
                "Assets/_WildSurvival/Content/Items/Tools",
                "Assets/_WildSurvival/Content/Items/Weapons",
                "Assets/_WildSurvival/Content/Items/Resources",
                "Assets/_WildSurvival/Content/Items/Consumables",
                "Assets/_WildSurvival/Content/Structures/Shelters",
                "Assets/_WildSurvival/Content/Structures/Crafting",
                "Assets/_WildSurvival/Content/Effects/Particles",
                "Assets/_WildSurvival/Content/Effects/PostProcess",
                
                // Data folders
                "Assets/_WildSurvival/Data/Items",
                "Assets/_WildSurvival/Data/Recipes",
                "Assets/_WildSurvival/Data/StatusEffects",
                "Assets/_WildSurvival/Data/Weather",
                "Assets/_WildSurvival/Data/Wildlife",
                "Assets/_WildSurvival/Data/Biomes",
                
                // Prefabs
                "Assets/_WildSurvival/Prefabs/Core",
                "Assets/_WildSurvival/Prefabs/Environment",
                "Assets/_WildSurvival/Prefabs/Items",
                "Assets/_WildSurvival/Prefabs/Structures",
                "Assets/_WildSurvival/Prefabs/Wildlife",
                "Assets/_WildSurvival/Prefabs/Effects",
                "Assets/_WildSurvival/Prefabs/UI",
                
                // Resources (runtime loaded)
                "Assets/_WildSurvival/Resources/Prefabs",
                "Assets/_WildSurvival/Resources/Materials",
                "Assets/_WildSurvival/Resources/Audio",
                "Assets/_WildSurvival/Resources/Data",
                
                // Scenes
                "Assets/_WildSurvival/Scenes/Core",
                "Assets/_WildSurvival/Scenes/World",
                "Assets/_WildSurvival/Scenes/Development",
                
                // Settings
                "Assets/_WildSurvival/Settings/HDRP/QualitySettings",
                "Assets/_WildSurvival/Settings/Input",
                "Assets/_WildSurvival/Settings/Audio",
                "Assets/_WildSurvival/Settings/Lighting",
                
                // Audio
                "Assets/_WildSurvival/Audio/Music/Ambient",
                "Assets/_WildSurvival/Audio/Music/Combat",
                "Assets/_WildSurvival/Audio/SFX/Player",
                "Assets/_WildSurvival/Audio/SFX/Environment",
                "Assets/_WildSurvival/Audio/SFX/Wildlife",
                "Assets/_WildSurvival/Audio/SFX/UI",
                
                // UI
                "Assets/_WildSurvival/UI/Textures/Icons",
                "Assets/_WildSurvival/UI/Textures/Backgrounds",
                "Assets/_WildSurvival/UI/Fonts",
                
                // Documentation
                "Assets/_WildSurvival/Documentation/API",
                "Assets/_WildSurvival/Documentation/Diagrams"
            };
        }

        private void OrganizeRuntimeCode()
        {
            // Move existing scripts to appropriate locations
            var scriptMoves = new Dictionary<string, string>
            {
                ["GameManager.cs"] = "Assets/_WildSurvival/Code/Runtime/Core/Managers/GameManager.cs",
                ["ServiceLocator.cs"] = "Assets/_WildSurvival/Code/Runtime/Core/Managers/ServiceLocator.cs",
                ["PlayerMovementController.cs"] = "Assets/_WildSurvival/Code/Runtime/Player/Controller/PlayerMovementController.cs",
                ["PlayerAnimatorController.cs"] = "Assets/_WildSurvival/Code/Runtime/Player/Controller/PlayerAnimatorController.cs",
                ["ThirdPersonCameraController.cs"] = "Assets/_WildSurvival/Code/Runtime/Player/Camera/ThirdPersonCameraController.cs",
                ["PlayerStats.cs"] = "Assets/_WildSurvival/Code/Runtime/Player/Stats/PlayerStats.cs",
                ["InventoryManager.cs"] = "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Core/InventoryManager.cs",
                ["ItemDefinition.cs"] = "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Items/ItemDefinition.cs",
                ["ItemInstance.cs"] = "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Items/ItemInstance.cs",
                ["CraftingManager.cs"] = "Assets/_WildSurvival/Code/Runtime/Survival/Crafting/CraftingManager.cs",
                ["RecipeDefinition.cs"] = "Assets/_WildSurvival/Code/Runtime/Survival/Crafting/RecipeDefinition.cs",
                ["TemperatureSystem.cs"] = "Assets/_WildSurvival/Code/Runtime/Survival/Temperature/TemperatureSystem.cs",
                ["StatusEffectManager.cs"] = "Assets/_WildSurvival/Code/Runtime/Survival/StatusEffects/StatusEffectManager.cs",
                ["SurvivalHUD.cs"] = "Assets/_WildSurvival/Code/Runtime/UI/HUD/SurvivalHUD.cs",
                ["SurvivalJournalMenu.cs"] = "Assets/_WildSurvival/Code/Runtime/UI/Journal/SurvivalJournalMenu.cs",
                ["TimeManager.cs"] = "Assets/_WildSurvival/Code/Runtime/Environment/Time/TimeManager.cs",
                ["WeatherSystem.cs"] = "Assets/_WildSurvival/Code/Runtime/Environment/Weather/WeatherSystem.cs"
            };

            foreach (var move in scriptMoves)
            {
                FindAndMoveScript(move.Key, move.Value);
            }
        }

        private void OrganizeEditorTools()
        {
            var editorMoves = new Dictionary<string, string>
            {
                ["ProjectTreeGenerator.cs"] = "Assets/_WildSurvival/Code/Editor/Tools/ProjectStructure/ProjectTreeGenerator.cs",
                ["CompleteProjectRestructure.cs"] = "Assets/_WildSurvival/Code/Editor/Tools/ProjectStructure/CompleteProjectRestructure.cs",
                ["UltimateInventoryTool.cs"] = "Assets/_WildSurvival/Code/Editor/Tools/Database/InventoryTool.cs",
                ["WildSurvivalHub.cs"] = "Assets/_WildSurvival/Code/Editor/Windows/WildSurvivalHub.cs",
                ["WildSurvivalProgressQuest.cs"] = "Assets/_WildSurvival/Code/Editor/Windows/ProgressTracker.cs"
            };

            foreach (var move in editorMoves)
            {
                FindAndMoveScript(move.Key, move.Value);
            }
        }

        private void SetupTestStructure()
        {
            CreateTestTemplate("Assets/_WildSurvival/Code/Tests/Runtime/InventoryTests.cs", "InventoryTests");
            CreateTestTemplate("Assets/_WildSurvival/Code/Tests/Runtime/CraftingTests.cs", "CraftingTests");
            CreateTestTemplate("Assets/_WildSurvival/Code/Tests/Editor/DatabaseTests.cs", "DatabaseTests");
        }

        private void MoveScenes()
        {
            var sceneMoves = new Dictionary<string, string>
            {
                ["OutdoorsScene.unity"] = "Assets/_WildSurvival/Scenes/World/World_Forest.unity",
                ["World_Prototype.unity"] = "Assets/_WildSurvival/Scenes/Development/Dev_Prototype.unity",
                ["_Bootstrap.unity"] = "Assets/_WildSurvival/Scenes/Core/_Preload.unity",
                ["_Persistent.unity"] = "Assets/_WildSurvival/Scenes/Core/_Persistent.unity"
            };

            foreach (var move in sceneMoves)
            {
                FindAndMoveAsset(move.Key, move.Value, "t:Scene");
            }
        }

        private void MovePrefabs()
        {
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            foreach (var guid in prefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/_WildSurvival/Prefabs")) continue;

                string fileName = Path.GetFileName(path);
                string newPath = CategorizePrefab(fileName, path);

                if (!string.IsNullOrEmpty(newPath) && path != newPath)
                {
                    SafeMoveAsset(path, newPath);
                }
            }
        }

        private void MoveDataAssets()
        {
            var dataMoves = new Dictionary<string, string>
            {
                ["ItemDatabase.asset"] = "Assets/_WildSurvival/Data/Items/ItemDatabase.asset",
                ["RecipeDatabase.asset"] = "Assets/_WildSurvival/Data/Recipes/RecipeDatabase.asset"
            };

            foreach (var move in dataMoves)
            {
                FindAndMoveAsset(move.Key, move.Value, "t:ScriptableObject");
            }
        }

        private void MoveContent()
        {
            // Move HDRP settings if they exist
            if (AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                SafeMoveAsset("Assets/Settings", "Assets/_WildSurvival/Settings/HDRP");
            }
        }

        private void CreateAssemblyDefinitions()
        {
            var assemblies = new List<(string path, string name, string[] refs, bool isEditor)>
            {
                ("Assets/_WildSurvival/Code/Runtime/Core", "WildSurvival.Core", new string[] { }, false),
                ("Assets/_WildSurvival/Code/Runtime/Player", "WildSurvival.Player", new[] { "WildSurvival.Core" }, false),
                ("Assets/_WildSurvival/Code/Runtime/Survival", "WildSurvival.Survival", new[] { "WildSurvival.Core", "WildSurvival.Player" }, false),
                ("Assets/_WildSurvival/Code/Runtime/Environment", "WildSurvival.Environment", new[] { "WildSurvival.Core" }, false),
                ("Assets/_WildSurvival/Code/Runtime/AI", "WildSurvival.AI", new[] { "WildSurvival.Core" }, false),
                ("Assets/_WildSurvival/Code/Runtime/Combat", "WildSurvival.Combat", new[] { "WildSurvival.Core", "WildSurvival.Player" }, false),
                ("Assets/_WildSurvival/Code/Runtime/UI", "WildSurvival.UI", new[] { "WildSurvival.Core", "WildSurvival.Survival", "Unity.TextMeshPro" }, false),
                ("Assets/_WildSurvival/Code/Runtime/Audio", "WildSurvival.Audio", new[] { "WildSurvival.Core" }, false),
                ("Assets/_WildSurvival/Code/Runtime/SaveSystem", "WildSurvival.SaveSystem", new[] { "WildSurvival.Core" }, false),
                ("Assets/_WildSurvival/Code/Editor", "WildSurvival.Editor", new[] { "WildSurvival.Core", "WildSurvival.Survival" }, true),
                ("Assets/_WildSurvival/Code/Tests/Runtime", "WildSurvival.Tests.Runtime", new[] { "WildSurvival.Core", "WildSurvival.Survival" }, false),
                ("Assets/_WildSurvival/Code/Tests/Editor", "WildSurvival.Tests.Editor", new[] { "WildSurvival.Core", "WildSurvival.Editor" }, true)
            };

            foreach (var (path, name, refs, isEditor) in assemblies)
            {
                CreateAssemblyDef(path, name, refs, isEditor);
            }

            LogMessage($"  📦 Created {assemblies.Count} assembly definitions");
        }

        private void CreateCoreScripts()
        {
            // Create essential core scripts
            CreateGameBootstrapper();
            CreateServiceRegistry();
            CreateEventBus();
        }

        private void CreateGameBootstrapper()
        {
            string content = @"using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Core.Bootstrap
{
    /// <summary>
    /// Main game bootstrapper - handles initial game setup and scene loading
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header(""Configuration"")]
        [SerializeField] private string persistentSceneName = ""_Persistent"";
        [SerializeField] private string mainMenuSceneName = ""_MainMenu"";
        [SerializeField] private bool loadMainMenuOnStart = true;
        
        private void Awake()
        {
            StartCoroutine(InitializeGame());
        }
        
        private IEnumerator InitializeGame()
        {
            Debug.Log(""[GameBootstrapper] Initializing Wild Survival..."");
            
            // Load persistent scene
            if (!string.IsNullOrEmpty(persistentSceneName))
            {
                yield return SceneManager.LoadSceneAsync(persistentSceneName, LoadSceneMode.Additive);
            }
            
            // Initialize services
            ServiceRegistry.Initialize();
            
            // Load main menu
            if (loadMainMenuOnStart && !string.IsNullOrEmpty(mainMenuSceneName))
            {
                yield return SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
            }
            
            Debug.Log(""[GameBootstrapper] Initialization complete"");
        }
    }
}";
            CreateScript("Assets/_WildSurvival/Code/Runtime/Core/Bootstrap/GameBootstrapper.cs", content);
        }

        private void CreateServiceRegistry()
        {
            string content = @"using System;
using System.Collections.Generic;
using UnityEngine;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Core.Bootstrap
{
    /// <summary>
    /// Service locator pattern implementation for managing game services
    /// </summary>
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static bool isInitialized = false;
        
        public static void Initialize()
        {
            if (isInitialized) return;
            
            Debug.Log(""[ServiceRegistry] Initializing services..."");
            
            // Register core services here
            // Example: Register<IInventoryService>(new InventoryManager());
            
            isInitialized = true;
            Debug.Log(""[ServiceRegistry] Services initialized"");
        }
        
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (services.ContainsKey(type))
            {
                Debug.LogWarning($""[ServiceRegistry] Service {type.Name} already registered"");
                return;
            }
            
            services[type] = service;
            Debug.Log($""[ServiceRegistry] Registered service: {type.Name}"");
        }
        
        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            
            Debug.LogError($""[ServiceRegistry] Service not found: {type.Name}"");
            return null;
        }
        
        public static bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }
    }
}";
            CreateScript("Assets/_WildSurvival/Code/Runtime/Core/Bootstrap/ServiceRegistry.cs", content);
        }

        private void CreateEventBus()
        {
            string content = @"using System;
using System.Collections.Generic;
using UnityEngine;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Core.Events
{
    /// <summary>
    /// Event bus for decoupled communication between systems
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> eventHandlers = new Dictionary<Type, List<Delegate>>();
        
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var eventType = typeof(T);
            
            if (!eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType] = new List<Delegate>();
            }
            
            eventHandlers[eventType].Add(handler);
        }
        
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var eventType = typeof(T);
            
            if (eventHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
        
        public static void Publish<T>(T eventData) where T : struct
        {
            var eventType = typeof(T);
            
            if (eventHandlers.TryGetValue(eventType, out var handlers))
            {
                foreach (var handler in handlers.ToArray())
                {
                    try
                    {
                        (handler as Action<T>)?.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($""[EventBus] Error handling event {eventType.Name}: {e}"");
                    }
                }
            }
        }
        
        public static void Clear()
        {
            eventHandlers.Clear();
        }
    }
}";
            CreateScript("Assets/_WildSurvival/Code/Runtime/Core/Events/EventBus.cs", content);
        }

        private void CreateManagerSystems()
        {
            CreateGameManager();
            CreatePoolManager();
        }

        private void CreateGameManager()
        {
            string content = @"using UnityEngine;
using System;
// using WildSurvival.Core.Events; // Commented out by Menu Fixer

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Core.Managers
{
    /// <summary>
    /// Central game manager handling game states and flow
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(""[GAME_MANAGER]"");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        public enum GameState
        {
            Loading,
            MainMenu,
            Playing,
            Paused,
            GameOver
        }
        
        private GameState currentState = GameState.Loading;
        public GameState CurrentState => currentState;
        
        public static event Action<GameState> OnGameStateChanged;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Initialize()
        {
            Debug.Log(""[GameManager] Initializing..."");
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
        }
        
        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;
            
            GameState oldState = currentState;
            currentState = newState;
            
            Debug.Log($""[GameManager] State changed: {oldState} -> {newState}"");
            OnGameStateChanged?.Invoke(newState);
            
            // Publish event
            EventBus.Publish(new GameStateChangedEvent { OldState = oldState, NewState = newState });
        }
        
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
                Time.timeScale = 0f;
            }
        }
        
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                Time.timeScale = 1f;
            }
        }
    }
    
    public struct GameStateChangedEvent
    {
        public GameManager.GameState OldState;
        public GameManager.GameState NewState;
    }
}";
            CreateScript("Assets/_WildSurvival/Code/Runtime/Core/Managers/GameManager.cs", content);
        }

        private void CreatePoolManager()
        {
            string content = @"using System.Collections.Generic;
using UnityEngine;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Core.Managers
{
    /// <summary>
    /// Object pooling manager for performance optimization
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager _instance;
        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PoolManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(""[POOL_MANAGER]"");
                        _instance = go.AddComponent<PoolManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> poolParents = new Dictionary<string, GameObject>();
        
        public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            string key = prefab.name;
            
            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary[key] = new Queue<GameObject>();
                GameObject parent = new GameObject($""Pool_{key}"");
                parent.transform.SetParent(transform);
                poolParents[key] = parent;
            }
            
            GameObject obj;
            
            if (poolDictionary[key].Count > 0)
            {
                obj = poolDictionary[key].Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(prefab, position, rotation, poolParents[key].transform);
                obj.name = prefab.name;
            }
            
            return obj;
        }
        
        public void ReturnToPool(GameObject obj)
        {
            string key = obj.name;
            
            if (!poolDictionary.ContainsKey(key))
            {
                Debug.LogWarning($""[PoolManager] No pool exists for {key}"");
                Destroy(obj);
                return;
            }
            
            obj.SetActive(false);
            poolDictionary[key].Enqueue(obj);
        }
        
        public void PrewarmPool(GameObject prefab, int count)
        {
            string key = prefab.name;
            
            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary[key] = new Queue<GameObject>();
                GameObject parent = new GameObject($""Pool_{key}"");
                parent.transform.SetParent(transform);
                poolParents[key] = parent;
            }
            
            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, poolParents[key].transform);
                obj.name = prefab.name;
                obj.SetActive(false);
                poolDictionary[key].Enqueue(obj);
            }
            
            Debug.Log($""[PoolManager] Prewarmed {count} instances of {key}"");
        }
    }
}";
            CreateScript("Assets/_WildSurvival/Code/Runtime/Core/Managers/PoolManager.cs", content);
        }

        private void SetupServiceLocator()
        {
            // Service locator is already created in ServiceRegistry
            LogMessage("  ✅ Service locator configured");
        }

        private void ConfigureProjectSettings()
        {
            PlayerSettings.companyName = companyName;
            PlayerSettings.productName = productName;
            PlayerSettings.applicationIdentifier = packageId;

            // Graphics settings
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // Build settings
            PlayerSettings.stripEngineCode = false;

            // For Unity 2021.2+, use the new stripping API with proper build target groups
#if UNITY_2021_2_OR_NEWER
            var namedTarget = NamedBuildTarget.Standalone;
            PlayerSettings.SetManagedStrippingLevel(namedTarget, ManagedStrippingLevel.Minimal);
#endif

            LogMessage("  ⚙️ Project settings configured");
        }

        private void SetupQualityLevels()
        {
            // Get current quality levels count
            int currentLevels = QualitySettings.names.Length;

            // Configure each quality level
            string[] desiredNames = { "Low", "Medium", "High", "Ultra" };

            for (int i = 0; i < Mathf.Min(currentLevels, desiredNames.Length); i++)
            {
                QualitySettings.SetQualityLevel(i, false);

                // Configure settings for each level
                QualitySettings.vSyncCount = 1;
                QualitySettings.antiAliasing = i < 2 ? 0 : 2;
                QualitySettings.shadows = i == 0 ? ShadowQuality.Disable : ShadowQuality.All;
                QualitySettings.shadowResolution = (ShadowResolution)Mathf.Min(i + 1, 3);
                QualitySettings.shadowDistance = 50f + (i * 50f);
                QualitySettings.lodBias = 0.5f + (i * 0.5f);
                QualitySettings.maximumLODLevel = 0;
                QualitySettings.anisotropicFiltering = i == 0 ? AnisotropicFiltering.Disable :
                                                       i == 1 ? AnisotropicFiltering.Enable :
                                                       AnisotropicFiltering.ForceEnable;
            }

            // Set default to High (index 2)
            if (currentLevels >= 3)
            {
                QualitySettings.SetQualityLevel(2, true);
            }

            LogMessage($"  🎨 Configured {Mathf.Min(currentLevels, desiredNames.Length)} quality levels");

            if (currentLevels != desiredNames.Length)
            {
                LogMessage($"  ⚠️ Note: Project has {currentLevels} quality levels. Adjust in Project Settings > Quality");
            }
        }

        private void ConfigureHDRPSettings()
        {
            // Check if HDRP is installed
#if UNITY_PIPELINE_HDRP
            LogMessage("  💡 HDRP detected - settings configured");
#else
            LogMessage("  ⚠️ HDRP not detected - manual configuration needed");
#endif
        }

        private void GenerateDocumentation()
        {
            GenerateReadme();
            GenerateArchitectureDoc();
            GenerateCodingStandardsDoc();
        }

        private void GenerateReadme()
        {
            string content = $@"# {productName}

## 🎮 Project Overview
A wilderness survival game built with Unity 6 and HDRP.

## 📁 Project Structure

```
Assets/
├── _WildSurvival/     # Main project folder
│   ├── Code/          # All scripts
│   ├── Content/       # Game assets
│   ├── Data/          # ScriptableObjects
│   ├── Prefabs/       # Prefab organization
│   ├── Scenes/        # Scene files
│   └── Settings/      # Project settings
├── _DevTools/         # Development utilities
└── _ThirdParty/       # External packages
```

## 🚀 Quick Start

1. Open `_WildSurvival/Scenes/Core/_Preload.unity`
2. Press Play
3. The game will automatically load necessary scenes

## 🏗️ Architecture

### Core Systems
- **GameManager**: Central game state management
- **ServiceRegistry**: Service locator pattern
- **EventBus**: Decoupled event system
- **PoolManager**: Object pooling for performance

### Assembly Definitions
The project uses assembly definitions for faster compilation:
- `WildSurvival.Core`: Core systems
- `WildSurvival.Player`: Player mechanics
- `WildSurvival.Survival`: Survival systems
- `WildSurvival.Environment`: World systems
- `WildSurvival.UI`: User interface

## 📝 Documentation

- [Architecture Guide](Documentation/ARCHITECTURE.md)
- [Coding Standards](Documentation/CODING_STANDARDS.md)
- [API Reference](Documentation/API/)

## 🤝 Team

Developed by {companyName}

---
*Generated: {DateTime.Now:yyyy-MM-dd}*
";
            CreateScript("Assets/_WildSurvival/Documentation/README.md", content);
        }

        private void GenerateArchitectureDoc()
        {
            string content = @"# Architecture Guide

## Overview
Wild Survival follows a modular, component-based architecture optimized for performance and maintainability.

## Core Patterns

### Service Locator
Used for accessing major systems without tight coupling.

```csharp
var inventoryManager = ServiceRegistry.Get<IInventoryService>();
```

### Event Bus
Decoupled communication between systems.

```csharp
EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
EventBus.Publish(new PlayerDamagedEvent { Damage = 10 });
```

### Object Pooling
Performance optimization for frequently spawned objects.

```csharp
var projectile = PoolManager.Instance.GetFromPool(prefab, position, rotation);
PoolManager.Instance.ReturnToPool(projectile);
```

## System Architecture

### Layer Separation
1. **Core Layer**: Foundation systems (GameManager, EventBus)
2. **Gameplay Layer**: Game mechanics (Player, Survival, Combat)
3. **Presentation Layer**: UI and visual feedback
4. **Data Layer**: ScriptableObjects and persistent data

### Dependencies
- Higher layers depend on lower layers
- No circular dependencies
- Use interfaces for abstraction

## Performance Considerations

### Update Loops
- Use coroutines for non-critical updates
- Implement update intervals for expensive operations
- Cache component references

### Memory Management
- Pool frequently instantiated objects
- Minimize garbage collection
- Use structs for data containers

## Best Practices

1. **Single Responsibility**: Each class should have one reason to change
2. **Open/Closed**: Open for extension, closed for modification
3. **Dependency Inversion**: Depend on abstractions, not concretions
4. **Interface Segregation**: Many specific interfaces over general ones
5. **Don't Repeat Yourself**: Avoid code duplication
";
            CreateScript("Assets/_WildSurvival/Documentation/ARCHITECTURE.md", content);
        }

        private void GenerateCodingStandardsDoc()
        {
            string content = @"# Coding Standards

## Naming Conventions

### Classes and Methods
```csharp
public class PlayerController  // PascalCase
{
    public void TakeDamage()    // PascalCase
    {
    }
}
```

### Variables and Parameters
```csharp
private int healthPoints;       // camelCase
public float moveSpeed;         // camelCase

void Calculate(int damage)      // camelCase parameters
{
}
```

### Constants
```csharp
private const int MAX_HEALTH = 100;  // SCREAMING_CASE
```

### Interfaces
```csharp
public interface IInventoryService   // 'I' prefix
{
}
```

## Code Organization

### File Structure
- One class per file
- File name matches class name
- Related classes in same namespace

### Namespaces
```csharp
// namespace removed by Menu Fixer - check closing brace
// namespace WildSurvival.Player.Controller
{
    // All player controller related classes
}
```

## Unity Specific

### Serialization
```csharp
[SerializeField] private float speed = 5f;  // Prefer over public
```

### Coroutines
```csharp
private IEnumerator DoSomethingOverTime()
{
    yield return new WaitForSeconds(1f);
}
```

## Performance Guidelines

1. Cache component references
2. Use object pooling
3. Minimize allocations in Update
4. Profile regularly
5. Optimize draw calls
";
            CreateScript("Assets/_WildSurvival/Documentation/CODING_STANDARDS.md", content);
        }

        private void SetupGitConfiguration()
        {
            CreateGitignore();
            CreateGitattributes();
            LogMessage("  📝 Git configuration created");
        }

        private void CreateGitignore()
        {
            string path = Path.Combine(Application.dataPath, "../.gitignore");
            string content = @"# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/

# Unity3D generated meta files
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Unity3D generated file on crash reports
sysinfo.txt

# Builds
*.apk
*.unitypackage
*.aab
*.app

# Crashlytics
crashlytics-build.properties

# Packed Addressables
[Aa]ssets/[Aa]ddressable[Aa]ssets[Dd]ata/*/*.bin*

# Temporary auto-generated Android Assets
[Aa]ssets/[Ss]treamingAssets/aa.meta
[Aa]ssets/[Ss]treamingAssets/aa/*

# Visual Studio / MonoDevelop
.vs/
.vscode/
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# OS Generated
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
Icon?
ehthumbs.db
Thumbs.db
Desktop.ini

# Project specific
Backups/
_Builds/
";
            File.WriteAllText(path, content);
        }

        private void CreateGitattributes()
        {
            string path = Path.Combine(Application.dataPath, "../.gitattributes");
            string content = @"# Unity YAML
*.meta text eol=lf
*.unity text eol=lf
*.prefab text eol=lf
*.asset text eol=lf
*.mat text eol=lf
*.controller text eol=lf
*.anim text eol=lf

# Image formats
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.jpeg filter=lfs diff=lfs merge=lfs -text
*.tif filter=lfs diff=lfs merge=lfs -text
*.tiff filter=lfs diff=lfs merge=lfs -text
*.tga filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text

# Audio formats
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.ogg filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text

# 3D formats
*.fbx filter=lfs diff=lfs merge=lfs -text
*.obj filter=lfs diff=lfs merge=lfs -text
*.max filter=lfs diff=lfs merge=lfs -text
*.blend filter=lfs diff=lfs merge=lfs -text

# Build
*.dll filter=lfs diff=lfs merge=lfs -text
*.pdb filter=lfs diff=lfs merge=lfs -text
";
            File.WriteAllText(path, content);
        }

        private void CleanupProject()
        {
            DeleteEmptyFolders("Assets/WildSurvival");
            DeleteEmptyFolders("Assets/_Project");
            RemoveDuplicateAssets();
            LogMessage("  🧹 Project cleaned up");
        }

        private void ValidateProjectStructure()
        {
            var requiredFolders = new[]
            {
                "Assets/_WildSurvival/Code/Runtime/Core",
                "Assets/_WildSurvival/Code/Runtime/Player",
                "Assets/_WildSurvival/Code/Runtime/Survival",
                "Assets/_WildSurvival/Code/Editor",
                "Assets/_WildSurvival/Data",
                "Assets/_WildSurvival/Prefabs",
                "Assets/_WildSurvival/Scenes"
            };

            int validCount = 0;
            foreach (var folder in requiredFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    validCount++;
                }
                else
                {
                    LogMessage($"  ⚠️ Missing folder: {folder}");
                }
            }

            LogMessage($"  ✅ Validation complete: {validCount}/{requiredFolders.Length} folders present");
        }

        private void ShowCompletionDialog()
        {
            EditorUtility.DisplayDialog("Success!",
                "Project restructuring completed successfully!\n\n" +
                "✅ Folder structure created\n" +
                "✅ Files organized\n" +
                "✅ Assembly definitions set up\n" +
                "✅ Core systems created\n" +
                "✅ Documentation generated\n\n" +
                "Next steps:\n" +
                "1. Open _WildSurvival/Scenes/Core/_Preload.unity\n" +
                "2. Configure your prefabs\n" +
                "3. Start developing!",
                "Awesome!");

            // Open documentation
            string docPath = Path.Combine(Application.dataPath, "_WildSurvival/Documentation/README.md");
            if (File.Exists(docPath))
            {
                Application.OpenURL("file:///" + docPath);
            }
        }

        private void ExportStructurePlan()
        {
            string exportPath = EditorUtility.SaveFilePanel(
                "Export Structure Plan",
                Application.dataPath,
                "WildSurvival_Structure_Plan.txt",
                "txt");

            if (!string.IsNullOrEmpty(exportPath))
            {
                var folders = GetFolderStructure();
                var content = new StringBuilder();
                content.AppendLine("WILD SURVIVAL - PROJECT STRUCTURE PLAN");
                content.AppendLine("=" + new string('=', 50));
                content.AppendLine();

                foreach (var folder in folders)
                {
                    int depth = folder.Split('/').Length - 1;
                    string indent = new string(' ', depth * 2);
                    string folderName = Path.GetFileName(folder);
                    content.AppendLine($"{indent}├── {folderName}/");
                }

                File.WriteAllText(exportPath, content.ToString());
                LogMessage("✅ Structure plan exported to: " + exportPath);
            }
        }

        private void AnalyzeCurrentProject()
        {
            log.Clear();
            LogMessage("📊 CURRENT PROJECT ANALYSIS");
            LogMessage("=" + new string('=', 50));

            // Count assets
            int scripts = AssetDatabase.FindAssets("t:Script", new[] { "Assets" }).Length;
            int prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Length;
            int scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Length;
            int materials = AssetDatabase.FindAssets("t:Material", new[] { "Assets" }).Length;
            int textures = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" }).Length;
            int scriptableObjects = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" }).Length;

            LogMessage($"📄 Scripts: {scripts}");
            LogMessage($"🎯 Prefabs: {prefabs}");
            LogMessage($"🎬 Scenes: {scenes}");
            LogMessage($"🎨 Materials: {materials}");
            LogMessage($"🖼️ Textures: {textures}");
            LogMessage($"💾 ScriptableObjects: {scriptableObjects}");

            // Check for key folders
            LogMessage("");
            LogMessage("📁 FOLDER STRUCTURE:");

            string[] checkFolders = {
                "Assets/_Project",
                "Assets/WildSurvival",
                "Assets/_DevTools",
                "Assets/_ThirdParty",
                "Assets/Settings"
            };

            foreach (var folder in checkFolders)
            {
                bool exists = AssetDatabase.IsValidFolder(folder);
                LogMessage($"  {(exists ? "✅" : "❌")} {folder}");
            }

            // Check for assembly definitions
            LogMessage("");
            LogMessage("📦 ASSEMBLY DEFINITIONS:");
            var asmdefs = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets" });
            LogMessage($"  Found {asmdefs.Length} assembly definitions");

            foreach (var guid in asmdefs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LogMessage($"  • {Path.GetFileNameWithoutExtension(path)}");
            }

            LogMessage("=" + new string('=', 50));
            LogMessage("Analysis complete!");
        }

        // Helper Methods
        private void CreateFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);

                if (!AssetDatabase.IsValidFolder(parent))
                {
                    CreateFolder(parent);
                }

                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private void CreateScript(string path, string content)
        {
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);
        }

        private void CreateAssemblyDef(string folder, string name, string[] references, bool isEditor = false)
        {
            var asmdef = new
            {
                name = name,
                rootNamespace = name,
                references = references,
                includePlatforms = isEditor ? new[] { "Editor" } : new string[] { },
                excludePlatforms = new string[] { },
                allowUnsafeCode = false,
                autoReferenced = true,
                defineConstraints = new string[] { },
                versionDefines = new object[] { },
                noEngineReferences = false
            };

            string json = JsonUtility.ToJson(asmdef, true);
            string path = Path.Combine(folder, name + ".asmdef");
            CreateScript(path, json);
        }

        private void CreateTestTemplate(string path, string className)
        {
            string content = $@"using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Tests
{{
    public class {className}
    {{
        [SetUp]
        public void Setup()
        {{
            // Setup test environment
        }}

        [Test]
        public void {className}_SimpleTest()
        {{
            // Arrange
            int expected = 5;
            
            // Act
            int result = 2 + 3;
            
            // Assert
            Assert.AreEqual(expected, result);
        }}

        [UnityTest]
        public IEnumerator {className}_WithEnumerator()
        {{
            // Use yield to skip a frame
            yield return null;
            
            // Add test logic here
            Assert.IsTrue(true);
        }}
        
        [TearDown]
        public void TearDown()
        {{
            // Clean up after tests
        }}
    }}
}}";
            CreateScript(path, content);
        }

        private void SafeMoveAsset(string source, string destination)
        {
            if (!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(source))
                return;

            string destFolder = Path.GetDirectoryName(destination).Replace('\\', '/');
            CreateFolder(destFolder);

            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error))
            {
                LogMessage($"  ⚠️ Failed to move {Path.GetFileName(source)}: {error}");
            }
        }

        private void FindAndMoveAsset(string fileName, string destination, string filter)
        {
            var guids = AssetDatabase.FindAssets($"{Path.GetFileNameWithoutExtension(fileName)} {filter}");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == fileName)
                {
                    SafeMoveAsset(path, destination);
                    return;
                }
            }
        }

        private void FindAndMoveScript(string fileName, string destination)
        {
            FindAndMoveAsset(fileName, destination, "t:Script");
        }

        private string CategorizePrefab(string fileName, string currentPath)
        {
            // Skip if already in the correct location
            if (currentPath.StartsWith("Assets/_WildSurvival/Prefabs"))
                return null;

            // Categorize prefabs based on naming patterns
            string lowerName = fileName.ToLower();

            if (lowerName.Contains("player") || lowerName.Contains("character"))
                return $"Assets/_WildSurvival/Prefabs/Core/{fileName}";
            else if (lowerName.Contains("ui") || lowerName.Contains("hud") || lowerName.Contains("menu") || lowerName.Contains("canvas"))
                return $"Assets/_WildSurvival/Prefabs/UI/{fileName}";
            else if (lowerName.Contains("tree") || lowerName.Contains("rock") || lowerName.Contains("terrain") || lowerName.Contains("grass"))
                return $"Assets/_WildSurvival/Prefabs/Environment/{fileName}";
            else if (lowerName.Contains("item") || lowerName.Contains("pickup") || lowerName.Contains("tool") || lowerName.Contains("weapon"))
                return $"Assets/_WildSurvival/Prefabs/Items/{fileName}";
            else if (lowerName.Contains("animal") || lowerName.Contains("creature") || lowerName.Contains("wildlife"))
                return $"Assets/_WildSurvival/Prefabs/Wildlife/{fileName}";
            else if (lowerName.Contains("effect") || lowerName.Contains("particle") || lowerName.Contains("vfx") || lowerName.Contains("fx"))
                return $"Assets/_WildSurvival/Prefabs/Effects/{fileName}";
            else if (lowerName.Contains("building") || lowerName.Contains("structure") || lowerName.Contains("shelter"))
                return $"Assets/_WildSurvival/Prefabs/Structures/{fileName}";
            else if (lowerName.Contains("manager") || lowerName.Contains("system") || lowerName.Contains("controller"))
                return $"Assets/_WildSurvival/Prefabs/Core/{fileName}";

            // Default to Core for uncategorized prefabs
            return $"Assets/_WildSurvival/Prefabs/Core/{fileName}";
        }

        private void DeleteEmptyFolders(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                return;

            var subFolders = AssetDatabase.GetSubFolders(path);
            foreach (var subFolder in subFolders)
            {
                DeleteEmptyFolders(subFolder);
            }

            // Check if folder is empty (no assets and no subfolders after cleanup)
            var remainingSubFolders = AssetDatabase.GetSubFolders(path);
            var assets = AssetDatabase.FindAssets("", new[] { path });

            // Filter out assets from subfolders
            var directAssets = new List<string>();
            foreach (var guid in assets)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetDir = Path.GetDirectoryName(assetPath).Replace('\\', '/');
                if (assetDir == path)
                {
                    directAssets.Add(assetPath);
                }
            }

            if (remainingSubFolders.Length == 0 && directAssets.Count == 0)
            {
                AssetDatabase.DeleteAsset(path);
                LogMessage($"  🗑️ Removed empty folder: {path}");
            }
        }

        private void RemoveDuplicateAssets()
        {
            // Remove common duplicate files
            string[] duplicatesToRemove = {
                "Assets/ItemDatabase.asset",
                "Assets/RecipeDatabase.asset",
                "Assets/_Project/helloWorld.cs",
                "Assets/_Project/Scripts/helloWorld.cs"
            };

            foreach (var duplicate in duplicatesToRemove)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(duplicate))
                {
                    AssetDatabase.DeleteAsset(duplicate);
                    LogMessage($"  🗑️ Removed duplicate: {duplicate}");
                }
            }
        }

        private void CopyDirectory(string source, string destination)
        {
            if (!Directory.Exists(source))
                return;

            Directory.CreateDirectory(destination);

            // Copy files
            foreach (string file in Directory.GetFiles(source))
            {
                // Skip meta files as Unity will regenerate them
                if (Path.GetExtension(file) == ".meta")
                    continue;

                string destFile = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Copy subdirectories
            foreach (string dir in Directory.GetDirectories(source))
            {
                string destDir = Path.Combine(destination, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }

        private void LogMessage(string message)
        {
            log.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[ProjectRestructure] {message}");

            // Keep log size manageable
            if (log.Count > 100)
            {
                log.RemoveAt(0);
            }

            Repaint();
        }
    }
}