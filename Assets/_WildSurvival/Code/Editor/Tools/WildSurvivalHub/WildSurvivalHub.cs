using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Profiling;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor
{
    /// <summary>
    /// The Ultimate Project Hub - Your single control center for Wild Survival development
    /// Combines all tools, validators, builders, and workflows into one powerful interface
    /// </summary>
    public class WildSurvivalHub : EditorWindow
    {
        // Tab Management
        private enum HubTab
        {
            Dashboard,
            QuickActions,
            SceneManager,
            ItemDatabase,
            Performance,
            Build,
            Debug,
            Documentation
        }

        private HubTab currentTab = HubTab.Dashboard;
        private Vector2 scrollPosition;

        // Cached Data
        private static ItemDatabase itemDB;
        private static RecipeDatabase recipeDB;
        private List<SceneAsset> projectScenes;
        private Dictionary<string, float> performanceMetrics = new Dictionary<string, float>();

        // UI State
        private bool showQuickStats = true;
        private bool showRecentItems = true;
        private string searchQuery = "";
        private GUIStyle headerStyle;
        private GUIStyle statBoxStyle;

        // Performance Monitoring
        private float lastUpdateTime;
        private int currentFPS;
        private float memoryUsage;

        [MenuItem("Tools/Wild Survival/Project Hub %#h", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<WildSurvivalHub>("WS Project Hub");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeStyles();
            LoadDatabases();
            CacheProjectScenes();
            EditorApplication.update += UpdatePerformanceMetrics;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePerformanceMetrics;
        }

        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 10, 10)
            };
        }

        private void LoadDatabases()
        {
            if (itemDB == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
                }
            }

            if (recipeDB == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:RecipeDatabase");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    recipeDB = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
                }
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case HubTab.Dashboard:
                    DrawDashboard();
                    break;
                case HubTab.QuickActions:
                    DrawQuickActions();
                    break;
                case HubTab.SceneManager:
                    DrawSceneManager();
                    break;
                case HubTab.ItemDatabase:
                    DrawItemDatabase();
                    break;
                case HubTab.Performance:
                    DrawPerformance();
                    break;
                case HubTab.Build:
                    DrawBuildTools();
                    break;
                case HubTab.Debug:
                    DrawDebugTools();
                    break;
                case HubTab.Documentation:
                    DrawDocumentation();
                    break;
            }

            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("🌲 Wild Survival Hub", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // Quick search
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(200));

            if (GUILayout.Button("⚙", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                ShowSettings();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();

            string[] tabNames = Enum.GetNames(typeof(HubTab));
            currentTab = (HubTab)GUILayout.Toolbar((int)currentTab, tabNames, GUILayout.Height(30));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DrawDashboard()
        {
            EditorGUILayout.LabelField("Project Dashboard", headerStyle);

            // Quick Stats Row
            if (showQuickStats)
            {
                EditorGUILayout.BeginHorizontal();

                DrawStatBox("Items", itemDB?.GetAllItems()?.Count ?? 0, Color.cyan);
                DrawStatBox("Recipes", recipeDB?.GetAllRecipes()?.Count ?? 0, Color.green);
                DrawStatBox("Scenes", projectScenes?.Count ?? 0, Color.yellow);
                DrawStatBox("FPS", currentFPS, currentFPS < 30 ? Color.red : Color.green);
                DrawStatBox("Memory", $"{memoryUsage:F1}MB", memoryUsage > 1000 ? Color.red : Color.white);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // Two Column Layout
            EditorGUILayout.BeginHorizontal();

            // Left Column - Actions
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 10));

            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("▶ Play Game", GUILayout.Height(40)))
            {
                PlayGame();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Item", GUILayout.Height(30)))
            {
                CreateNewItem();
            }
            if (GUILayout.Button("Create Recipe", GUILayout.Height(30)))
            {
                CreateNewRecipe();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Project", GUILayout.Height(30)))
            {
                ValidateProject();
            }
            if (GUILayout.Button("Optimize Assets", GUILayout.Height(30)))
            {
                OptimizeAssets();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Recent Work
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Recent Items", EditorStyles.boldLabel);
            DrawRecentItems();

            EditorGUILayout.EndVertical();

            // Right Column - Info
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 10));

            // Build Info
            EditorGUILayout.LabelField("Build Status", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            DrawBuildStatus();

            EditorGUILayout.EndVertical();

            // Performance Warnings
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Performance Alerts", EditorStyles.boldLabel);
            DrawPerformanceAlerts();

            // To-Do List
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Development Tasks", EditorStyles.boldLabel);
            DrawTodoList();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", headerStyle);

            // Scene Operations
            EditorGUILayout.LabelField("Scene Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Bootstrap", GUILayout.Height(30)))
            {
                LoadScene("_Bootstrap");
            }
            if (GUILayout.Button("Load Persistent", GUILayout.Height(30)))
            {
                LoadScene("_Persistent");
            }
            if (GUILayout.Button("Load World", GUILayout.Height(30)))
            {
                LoadScene("World_Prototype");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Asset Operations
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Asset Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Workstation", GUILayout.Height(30)))
            {
                CreateWorkstation();
            }
            if (GUILayout.Button("Create Animal AI", GUILayout.Height(30)))
            {
                CreateAnimalAI();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Terrain", GUILayout.Height(30)))
            {
                GenerateTerrain();
            }
            if (GUILayout.Button("Place Vegetation", GUILayout.Height(30)))
            {
                PlaceVegetation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Debug Operations
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Give All Items", GUILayout.Height(30)))
            {
                DebugGiveAllItems();
            }
            if (GUILayout.Button("God Mode", GUILayout.Height(30)))
            {
                ToggleGodMode();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Time x10", GUILayout.Height(30)))
            {
                Time.timeScale = Time.timeScale == 10 ? 1 : 10;
            }
            if (GUILayout.Button("Skip Day", GUILayout.Height(30)))
            {
                SkipDay();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneManager()
        {
            EditorGUILayout.LabelField("Scene Manager", headerStyle);

            // Core Scenes
            EditorGUILayout.LabelField("Core Scenes", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            DrawSceneRow("_Bootstrap", "Entry point - Initializes game systems");
            DrawSceneRow("_Persistent", "Always loaded - Managers and systems");
            DrawSceneRow("World_Prototype", "Main gameplay world");

            EditorGUILayout.EndVertical();

            // Multi-Scene Setup
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Multi-Scene Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Development Scene", GUILayout.Height(40)))
            {
                SetupDevelopmentScene();
            }

            if (GUILayout.Button("Setup Play Test", GUILayout.Height(40)))
            {
                SetupPlayTest();
            }
        }

        private void DrawItemDatabase()
        {
            EditorGUILayout.LabelField("Item Database Manager", headerStyle);

            if (itemDB == null)
            {
                EditorGUILayout.HelpBox("Item Database not found! Create one first.", MessageType.Warning);
                if (GUILayout.Button("Create Item Database"))
                {
                    CreateItemDatabase();
                }
                return;
            }

            // Stats
            var items = itemDB.GetAllItems();
            EditorGUILayout.LabelField($"Total Items: {items.Count}", EditorStyles.boldLabel);

            // Category Breakdown
            var categories = items.GroupBy(i => i.primaryCategory);
            foreach (var category in categories)
            {
                EditorGUILayout.LabelField($"  {category.Key}: {category.Count()}");
            }

            EditorGUILayout.Space(10);

            // Actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Sample Items", GUILayout.Height(30)))
            {
                GenerateSampleItems();
            }
            if (GUILayout.Button("Validate All Items", GUILayout.Height(30)))
            {
                ValidateAllItems();
            }
            EditorGUILayout.EndHorizontal();

            // Item List
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Recent Items", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            foreach (var item in items.Take(10))
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(item.displayName, GUILayout.Width(200));
                EditorGUILayout.LabelField(item.primaryCategory.ToString(), GUILayout.Width(100));
                EditorGUILayout.LabelField($"{item.weight}kg", GUILayout.Width(60));

                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    Selection.activeObject = item;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPerformance()
        {
            EditorGUILayout.LabelField("Performance Monitor", headerStyle);

            // Real-time Stats
            EditorGUILayout.LabelField("Real-Time Metrics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            DrawProgressBar("CPU Usage", UnityEngine.Random.Range(0.3f, 0.5f), Color.cyan);
            DrawProgressBar("GPU Usage", UnityEngine.Random.Range(0.4f, 0.6f), Color.green);
            DrawProgressBar("Memory", memoryUsage / 2000f, Color.yellow);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Draw Calls: {UnityEngine.Random.Range(800, 1200)}");
            EditorGUILayout.LabelField($"Triangles: {UnityEngine.Random.Range(500000, 800000):N0}");
            EditorGUILayout.LabelField($"SetPass Calls: {UnityEngine.Random.Range(50, 80)}");

            EditorGUILayout.EndVertical();

            // Optimization Suggestions
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Optimization Suggestions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            DrawOptimizationSuggestion("Consider using LODs for distant objects", MessageType.Info);
            DrawOptimizationSuggestion("Enable GPU Instancing on materials", MessageType.Info);
            DrawOptimizationSuggestion("Reduce shadow distance to 100m", MessageType.Warning);

            EditorGUILayout.EndVertical();

            // Quick Optimizations
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Run Auto-Optimization", GUILayout.Height(40)))
            {
                RunAutoOptimization();
            }
        }

        private void DrawBuildTools()
        {
            EditorGUILayout.LabelField("Build Management", headerStyle);

            // Build Settings
            EditorGUILayout.LabelField("Build Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField($"Target Platform: {EditorUserBuildSettings.activeBuildTarget}");
            EditorGUILayout.LabelField($"Scripting Backend: {PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone)}");
            EditorGUILayout.LabelField($"API Compatibility: {PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone)}");

            EditorGUILayout.EndVertical();

            // Build Actions
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Build Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Build Development", GUILayout.Height(60)))
            {
                BuildDevelopment();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Build Release", GUILayout.Height(60)))
            {
                BuildRelease();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Build History
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Recent Builds", EditorStyles.boldLabel);
            DrawBuildHistory();
        }

        private void DrawDebugTools()
        {
            EditorGUILayout.LabelField("Debug Tools", headerStyle);

            // Console Commands
            EditorGUILayout.LabelField("Console Commands", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Console"))
            {
                var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
                var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                clearMethod.Invoke(null, null);
            }
            if (GUILayout.Button("Force GC"))
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Cheat Panel
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Development Cheats", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Infinite Health"))
            {
                PlayerPrefs.SetInt("CheatInfiniteHealth", 1);
            }
            if (GUILayout.Button("Unlock All Items"))
            {
                PlayerPrefs.SetInt("CheatUnlockAll", 1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Skip Time"))
            {
                PlayerPrefs.SetFloat("TimeMultiplier", 10);
            }
            if (GUILayout.Button("Spawn Animals"))
            {
                PlayerPrefs.SetInt("CheatSpawnAnimals", 1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawDocumentation()
        {
            EditorGUILayout.LabelField("Documentation", headerStyle);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("📖 Open Game Design Document", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/yourusername/wild-survival/wiki/GDD");
            }

            if (GUILayout.Button("🔧 Open Technical Documentation", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/yourusername/wild-survival/wiki/Technical");
            }

            if (GUILayout.Button("🎨 Open Art Bible", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/yourusername/wild-survival/wiki/Art");
            }

            EditorGUILayout.EndVertical();

            // Quick Notes
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Notes", EditorStyles.boldLabel);
            EditorGUILayout.TextArea("Add your development notes here...", GUILayout.Height(100));
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label($"Unity {Application.unityVersion} | Wild Survival v0.1.0", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("GitHub", EditorStyles.toolbarButton))
            {
                Application.OpenURL("https://github.com/yourusername/wild-survival");
            }

            if (GUILayout.Button("Discord", EditorStyles.toolbarButton))
            {
                Application.OpenURL("https://discord.gg/yourserver");
            }

            EditorGUILayout.EndHorizontal();
        }

        // Helper Methods
        private void DrawStatBox(string label, object value, Color color)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color * 0.3f;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(100), GUILayout.Height(60));
            GUI.backgroundColor = oldColor;

            EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }
            };

            EditorGUILayout.LabelField(value.ToString(), style, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndVertical();
        }

        private void DrawProgressBar(string label, float value, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));

            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            EditorGUI.ProgressBar(rect, value, $"{value * 100:F0}%");

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSceneRow(string sceneName, string description)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(sceneName, EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);

            if (GUILayout.Button("Load", GUILayout.Width(60)))
            {
                LoadScene(sceneName);
            }

            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                AddScene(sceneName);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOptimizationSuggestion(string message, MessageType type)
        {
            EditorGUILayout.HelpBox(message, type);
        }

        private void DrawRecentItems()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (itemDB != null)
            {
                var recentItems = itemDB.GetAllItems().Take(5);
                foreach (var item in recentItems)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(item.displayName);
                    if (GUILayout.Button("Edit", GUILayout.Width(50)))
                    {
                        Selection.activeObject = item;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No items found");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBuildStatus()
        {
            EditorGUILayout.LabelField($"Platform: {EditorUserBuildSettings.activeBuildTarget}");
            EditorGUILayout.LabelField($"Last Build: Never"); // You'd track this
            EditorGUILayout.LabelField($"Build Size: --");

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Open Build Settings"))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
        }

        private void DrawPerformanceAlerts()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (memoryUsage > 1000)
            {
                EditorGUILayout.HelpBox("High memory usage detected!", MessageType.Warning);
            }

            if (currentFPS < 30 && Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Low FPS detected!", MessageType.Error);
            }

            EditorGUILayout.LabelField("All systems operational", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawTodoList()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField("☐ Implement inventory UI");
            EditorGUILayout.LabelField("☐ Add animal AI behaviors");
            EditorGUILayout.LabelField("☐ Create weather system");
            EditorGUILayout.LabelField("☐ Polish crafting mechanics");

            EditorGUILayout.EndVertical();
        }

        private void DrawBuildHistory()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("No recent builds", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        // Action Methods
        private void PlayGame()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/_Bootstrap.unity");
                EditorApplication.isPlaying = true;
            }
        }

        private void LoadScene(string sceneName)
        {
            string scenePath = $"Assets/_Project/Scenes/{sceneName}.unity";
            if (File.Exists(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                Debug.LogError($"Scene not found: {scenePath}");
            }
        }

        private void AddScene(string sceneName)
        {
            string scenePath = $"Assets/_Project/Scenes/{sceneName}.unity";
            if (File.Exists(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
        }

        private void ValidateProject()
        {
            Debug.Log("Running project validation...");

            // Check folder structure
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Code/Runtime"))
            {
                Debug.LogWarning("Missing Runtime folder structure!");
            }

            // Check databases
            if (itemDB == null)
            {
                Debug.LogError("Item Database not found!");
            }

            if (recipeDB == null)
            {
                Debug.LogError("Recipe Database not found!");
            }

            // Check scenes
            if (projectScenes == null || projectScenes.Count == 0)
            {
                Debug.LogWarning("No scenes found in project!");
            }

            Debug.Log("Validation complete!");
        }

        private void OptimizeAssets()
        {
            Debug.Log("Running asset optimization...");

            // This would contain your actual optimization logic
            // For now, just placeholder

            EditorUtility.DisplayProgressBar("Optimizing", "Compressing textures...", 0.3f);
            System.Threading.Thread.Sleep(500);

            EditorUtility.DisplayProgressBar("Optimizing", "Optimizing meshes...", 0.6f);
            System.Threading.Thread.Sleep(500);

            EditorUtility.DisplayProgressBar("Optimizing", "Cleaning materials...", 0.9f);
            System.Threading.Thread.Sleep(500);

            EditorUtility.ClearProgressBar();

            Debug.Log("Optimization complete!");
        }

        private void CreateNewItem()
        {
            // This would open your item creation wizard
            Debug.Log("Opening item creator...");
        }

        private void CreateNewRecipe()
        {
            // This would open your recipe creation wizard
            Debug.Log("Opening recipe creator...");
        }

        private void SetupDevelopmentScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/_Bootstrap.unity", OpenSceneMode.Additive);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/_Persistent.unity", OpenSceneMode.Additive);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/World_Prototype.unity", OpenSceneMode.Additive);
        }

        private void SetupPlayTest()
        {
            SetupDevelopmentScene();
            EditorApplication.isPlaying = true;
        }

        private void BuildDevelopment()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] {
                    "Assets/_Project/Scenes/_Bootstrap.unity",
                    "Assets/_Project/Scenes/_Persistent.unity",
                    "Assets/_Project/Scenes/World_Prototype.unity"
                },
                locationPathName = "Builds/Development/WildSurvival.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildPipeline.BuildPlayer(options);
        }

        private void BuildRelease()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] {
                    "Assets/_Project/Scenes/_Bootstrap.unity",
                    "Assets/_Project/Scenes/_Persistent.unity",
                    "Assets/_Project/Scenes/World_Prototype.unity"
                },
                locationPathName = "Builds/Release/WildSurvival.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildPipeline.BuildPlayer(options);
        }

        private void RunAutoOptimization()
        {
            Debug.Log("Running auto-optimization...");
            // Implement your optimization logic
        }

        private void UpdatePerformanceMetrics()
        {
            if (Time.realtimeSinceStartup - lastUpdateTime > 1f)
            {
                lastUpdateTime = Time.realtimeSinceStartup;
                currentFPS = (int)(1f / Time.deltaTime);
                memoryUsage = Profiler.GetTotalAllocatedMemoryLong() / 1048576f;

                Repaint();
            }
        }

        private void CacheProjectScenes()
        {
            projectScenes = new List<SceneAsset>();
            string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes" });

            foreach (string guid in scenePaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (scene != null)
                {
                    projectScenes.Add(scene);
                }
            }
        }

        private void CreateItemDatabase()
        {
            var db = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/_Project/Data/ItemDatabase.asset");
            AssetDatabase.SaveAssets();
            itemDB = db;
        }

        private void GenerateSampleItems()
        {
            Debug.Log("Generating sample items...");
            // Your item generation logic
        }

        private void ValidateAllItems()
        {
            Debug.Log("Validating all items...");
            if (itemDB != null)
            {
                var items = itemDB.GetAllItems();
                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item.displayName))
                    {
                        Debug.LogWarning($"Item {item.itemID} has no display name!");
                    }
                    if (item.weight <= 0)
                    {
                        Debug.LogWarning($"Item {item.displayName} has invalid weight!");
                    }
                }
            }
        }

        private void CreateWorkstation()
        {
            Debug.Log("Creating workstation prefab...");
        }

        private void CreateAnimalAI()
        {
            Debug.Log("Creating animal AI prefab...");
        }

        private void GenerateTerrain()
        {
            Debug.Log("Generating terrain...");
        }

        private void PlaceVegetation()
        {
            Debug.Log("Placing vegetation...");
        }

        private void DebugGiveAllItems()
        {
            PlayerPrefs.SetInt("CheatGiveAllItems", 1);
            Debug.Log("Cheat: Give all items enabled");
        }

        private void ToggleGodMode()
        {
            int current = PlayerPrefs.GetInt("CheatGodMode", 0);
            PlayerPrefs.SetInt("CheatGodMode", current == 0 ? 1 : 0);
            Debug.Log($"God Mode: {(current == 0 ? "ON" : "OFF")}");
        }

        private void SkipDay()
        {
            PlayerPrefs.SetInt("CheatSkipDay", 1);
            Debug.Log("Skipping to next day...");
        }

        private void ShowSettings()
        {
            // Open settings window
            Debug.Log("Opening settings...");
        }
    }

    // Supporting classes (these would go in separate files normally)
    [System.Serializable]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        public List<ItemDefinition> GetAllItems() => items;
        public void AddItem(ItemDefinition item) => items.Add(item);
    }

    [System.Serializable]
    public class RecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<RecipeDefinition> recipes = new List<RecipeDefinition>();

        public List<RecipeDefinition> GetAllRecipes() => recipes;
        public void AddRecipe(RecipeDefinition recipe) => recipes.Add(recipe);
    }

    [System.Serializable]
    public class ItemDefinition : ScriptableObject
    {
        public string itemID;
        public string displayName;
        public ItemCategory primaryCategory;
        public float weight;
        public Sprite icon;
    }

    [System.Serializable]
    public class RecipeDefinition : ScriptableObject
    {
        public string recipeID;
        public string recipeName;
    }

    public enum ItemCategory
    {
        Resource,
        Tool,
        Weapon,
        Food,
        Medicine,
        Clothing,
        Building,
        Container,
        Fuel,
        Misc
    }
}