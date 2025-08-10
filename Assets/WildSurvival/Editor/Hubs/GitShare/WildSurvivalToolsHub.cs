using UnityEngine;
using UnityEditor;
//using UnityEditor.AddressableAssets;
//using UnityEditor.AddressableAssets.Settings;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// Add HDRP reference only if package is installed
#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace WildernesssSurvival.Editor
{
    /// <summary>
    /// Master Project Hub for Wild Survival - Unified project management tool
    /// Fixed version with proper API references
    /// </summary>
    public class WildSurvivalToolsHub : EditorWindow
    {
        #region Constants & Configuration
        private const string WINDOW_TITLE = "Wild Survival Hub";
        private const string MENU_PATH = "Tools/C_0/Tools Hub %#h"; // Ctrl+Shift+H
        private const float MIN_WIDTH = 800f;
        private const float MIN_HEIGHT = 600f;
        private const string PROJECT_ROOT = "Assets/_Project";
        private const string THIRD_PARTY_ROOT = "Assets/_ThirdParty";
        private const string SAMPLES_ROOT = "Assets/_Samples";

        // Version info
        private const string PROJECT_VERSION = "0.1.0-alpha";
        private const string UNITY_MIN_VERSION = "6000.0.0f1";
        #endregion

        #region Private Fields
        private int selectedTab = 0;
        private Vector2 scrollPosition;
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle richTextStyle;
        private bool isInitialized = false;

        // Tab-specific data
        private ProjectValidation validation = new ProjectValidation();
        private BuildSettings buildSettings = new BuildSettings();
        private ContentCreators contentCreators = new ContentCreators();
        private PerformanceMetrics performanceMetrics = new PerformanceMetrics();

        // Git integration
        private GitInfo gitInfo = new GitInfo();
        private string lastGitOperation = "";

        // Style colors
        private readonly Color successColor = new Color(0.2f, 0.8f, 0.2f);
        private readonly Color warningColor = new Color(0.8f, 0.8f, 0.2f);
        private readonly Color errorColor = new Color(0.8f, 0.2f, 0.2f);
        private readonly Color headerColor = new Color(0.3f, 0.7f, 1f);
        #endregion

        #region Unity Lifecycle
        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            var window = GetWindow<WildSurvivalToolsHub>(false, WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeStyles();
            RefreshProjectState();
            EditorApplication.projectChanged += RefreshProjectState;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= RefreshProjectState;
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }

        private void OnGUI()
        {
            if (!isInitialized)
            {
                InitializeStyles();
                isInitialized = true;
            }

            DrawHeader();
            DrawTabs();
            DrawSelectedTabContent();
            DrawFooter();
        }
        #endregion

        #region UI Drawing
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🌲 WILD SURVIVAL PROJECT HUB", headerStyle);
            GUILayout.FlexibleSpace();

            // Quick status indicators
            DrawStatusIndicator("Unity", Application.unityVersion.StartsWith("6000"), "Unity 6 Required");
            DrawStatusIndicator("HDRP", IsHDRPConfigured(), "HDRP Active");
            DrawStatusIndicator("IL2CPP", GetScriptingBackend() == ScriptingImplementation.IL2CPP, "IL2CPP Ready");
            DrawStatusIndicator("Git", gitInfo.IsRepository, "Version Control");

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField($"Version: {PROJECT_VERSION} | Unity: {Application.unityVersion} | Platform: {EditorUserBuildSettings.activeBuildTarget}", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawTabs()
        {
            string[] tabNames = {
                "🏗️ Setup",
                "✓ Validate",
                "📦 Content",
                "🔨 Build",
                "📊 Performance",
                "🎮 Runtime",
                "📤 Export"
            };

            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
            EditorGUILayout.Space(5);
        }

        private void DrawSelectedTabContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0:
                    DrawSetupTab();
                    break;
                case 1:
                    DrawValidateTab();
                    break;
                case 2:
                    DrawContentTab();
                    break;
                case 3:
                    DrawBuildTab();
                    break;
                case 4:
                    DrawPerformanceTab();
                    break;
                case 5:
                    DrawRuntimeTab();
                    break;
                case 6:
                    DrawExportTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (!string.IsNullOrEmpty(lastGitOperation))
            {
                EditorGUILayout.LabelField($"Last: {lastGitOperation}", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("📚 Documentation", GUILayout.Width(120)))
            {
                Application.OpenURL("https://github.com/mobiusinfinity/WildSurvival/wiki");
            }

            if (GUILayout.Button("🔄 Refresh", GUILayout.Width(80)))
            {
                RefreshProjectState();
            }

            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Tab Content - Setup
        private void DrawSetupTab()
        {
            DrawSectionHeader("Project Structure Setup");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Initialize your project with the correct folder structure and settings.", EditorStyles.wordWrappedLabel);

            // Check for structure issues
            CheckAndWarnStructureIssues();

            if (GUILayout.Button("🏗️ Create Project Structure", GUILayout.Height(40)))
            {
                CreateProjectStructure();
            }

            EditorGUILayout.Space(10);

            // Quick settings
            EditorGUILayout.LabelField("Quick Settings", EditorStyles.boldLabel);

            if (GUILayout.Button("Configure for IL2CPP Release"))
            {
                ConfigureForRelease();
            }

            if (GUILayout.Button("Configure for Mono Development"))
            {
                ConfigureForDevelopment();
            }

            if (GUILayout.Button("Setup HDRP Quality Tiers"))
            {
                SetupHDRPQualityTiers();
            }

            if (GUILayout.Button("Configure Input System"))
            {
                ConfigureInputSystem();
            }

            EditorGUILayout.EndVertical();

            DrawSectionHeader("Assembly Definitions");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (GUILayout.Button("Create Assembly Definitions"))
            {
                CreateAssemblyDefinitions();
            }
            EditorGUILayout.EndVertical();

            DrawSectionHeader("Git Configuration");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (!gitInfo.IsRepository)
            {
                if (GUILayout.Button("Initialize Git Repository"))
                {
                    InitializeGitRepository();
                }
            }
            else
            {
                EditorGUILayout.LabelField($"Branch: {gitInfo.Branch}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Remote: {gitInfo.RemoteUrl}", EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Create .gitignore & .gitattributes"))
            {
                CreateGitFiles();
            }
            EditorGUILayout.EndVertical();
        }

        private void CheckAndWarnStructureIssues()
        {
            // Check for problematic folders
            if (AssetDatabase.IsValidFolder("Assets/Assets"))
            {
                EditorGUILayout.HelpBox("⚠️ CRITICAL: Duplicate 'Assets/Assets' folder detected! This must be fixed immediately.", MessageType.Error);
                if (GUILayout.Button("Fix Duplicate Assets Folder"))
                {
                    FixDuplicateAssetsFolder();
                }
            }

            if (AssetDatabase.IsValidFolder("Assets/WildSurvival/Imports"))
            {
                EditorGUILayout.HelpBox("⚠️ Imports folder should not be in version control", MessageType.Warning);
            }

            if (AssetDatabase.IsValidFolder("Assets/WildSurvival/Backups"))
            {
                EditorGUILayout.HelpBox("⚠️ Backup folders should be external to the project", MessageType.Warning);
            }

            // Check if using recommended structure
            if (!AssetDatabase.IsValidFolder("Assets/_Project") && AssetDatabase.IsValidFolder("Assets/WildSurvival"))
            {
                EditorGUILayout.HelpBox("Consider migrating to _Project folder structure for better organization", MessageType.Info);
                if (GUILayout.Button("Migrate to _Project Structure"))
                {
                    MigrateToProjectStructure();
                }
            }
        }

        private void FixDuplicateAssetsFolder()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Move contents out of duplicate folder
                    var assetsPath = "Assets/Assets";
                    if (AssetDatabase.IsValidFolder(assetsPath))
                    {
                        var subFolders = AssetDatabase.GetSubFolders(assetsPath);
                        foreach (var folder in subFolders)
                        {
                            var folderName = Path.GetFileName(folder);
                            var targetPath = $"Assets/{folderName}_temp";
                            AssetDatabase.MoveAsset(folder, targetPath);
                        }
                        AssetDatabase.DeleteAsset(assetsPath);
                        AssetDatabase.Refresh();
                        Debug.Log("✅ Fixed duplicate Assets folder structure");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to fix duplicate assets folder: {e.Message}");
                }
            };
        }

        private void MigrateToProjectStructure()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Create _Project structure if it doesn't exist
                    if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                    {
                        AssetDatabase.CreateFolder("Assets", "_Project");
                    }

                    // Migrate WildSurvival content
                    if (AssetDatabase.IsValidFolder("Assets/WildSurvival"))
                    {
                        // Move specific folders
                        MoveAssetSafe("Assets/WildSurvival/Editor", "Assets/_Project/Code/Editor");
                        MoveAssetSafe("Assets/WildSurvival/Runtime", "Assets/_Project/Code/Runtime");
                        MoveAssetSafe("Assets/WildSurvival/Gameplay", "Assets/_Project/Code/Gameplay");
                        MoveAssetSafe("Assets/WildSurvival/Scripts", "Assets/_Project/Code/Scripts");
                    }

                    // Move SurvivalCore
                    if (AssetDatabase.IsValidFolder("Assets/SurvivalCore"))
                    {
                        MoveAssetSafe("Assets/SurvivalCore", "Assets/_Project/Code/Core");
                    }

                    AssetDatabase.Refresh();
                    Debug.Log("✅ Migration to _Project structure complete");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Migration failed: {e.Message}");
                }
            };
        }

        private void MoveAssetSafe(string from, string to)
        {
            if (AssetDatabase.IsValidFolder(from))
            {
                // Create parent directory if needed
                var parent = Path.GetDirectoryName(to);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    CreateFolder(parent);
                }

                var result = AssetDatabase.MoveAsset(from, to);
                if (!string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning($"Could not move {from} to {to}: {result}");
                }
            }
        }
        #endregion

        #region Tab Content - Validate
        private void DrawValidateTab()
        {
            DrawSectionHeader("Project Validation");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("🔍 Run Full Validation", GUILayout.Height(35)))
            {
                validation.RunFullValidation();
            }

            EditorGUILayout.Space(10);

            // Critical Checks
            DrawValidationCategory("Critical Settings", validation.CriticalChecks);

            // Performance Settings
            DrawValidationCategory("Performance Settings", validation.PerformanceChecks);

            // HDRP Settings
            DrawValidationCategory("HDRP Configuration", validation.HDRPChecks);

            // Asset Validation
            DrawValidationCategory("Asset Configuration", validation.AssetChecks);

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationCategory(string title, List<ValidationCheck> checks)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            foreach (var check in checks)
            {
                EditorGUILayout.BeginHorizontal();

                var color = check.Status switch
                {
                    CheckStatus.Pass => successColor,
                    CheckStatus.Warning => warningColor,
                    CheckStatus.Fail => errorColor,
                    _ => Color.gray
                };

                GUI.color = color;
                EditorGUILayout.LabelField(check.StatusIcon, GUILayout.Width(20));
                GUI.color = Color.white;

                EditorGUILayout.LabelField(check.Name, GUILayout.Width(200));
                EditorGUILayout.LabelField(check.Message, EditorStyles.wordWrappedMiniLabel);

                if (!string.IsNullOrEmpty(check.FixAction) && check.Status != CheckStatus.Pass)
                {
                    if (GUILayout.Button("Fix", GUILayout.Width(50)))
                    {
                        check.ExecuteFix();
                        validation.RunFullValidation();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
        }
        #endregion

        #region Tab Content - Content
        private void DrawContentTab()
        {
            DrawSectionHeader("Content Creation Tools");

            // ScriptableObject Creators
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ScriptableObject Creators", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📦 Item Definition"))
            {
                CreateItemDefinition();
            }
            if (GUILayout.Button("🔧 Recipe"))
            {
                CreateRecipeDefinition();
            }
            if (GUILayout.Button("🏭 Workstation"))
            {
                CreateWorkstationDefinition();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🦌 Animal"))
            {
                CreateAnimalDefinition();
            }
            if (GUILayout.Button("🌲 Biome"))
            {
                CreateBiomeDefinition();
            }
            if (GUILayout.Button("💊 Status Effect"))
            {
                CreateStatusEffectDefinition();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Batch Operations
            DrawSectionHeader("Batch Operations");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Validate All Items"))
            {
                ValidateAllItems();
            }

            if (GUILayout.Button("Validate All Recipes"))
            {
                ValidateAllRecipes();
            }

            if (GUILayout.Button("Generate Item Icons"))
            {
                GenerateItemIcons();
            }

            if (GUILayout.Button("Update Localization Keys"))
            {
                UpdateLocalizationKeys();
            }

            EditorGUILayout.EndVertical();

            // Content Statistics
            DrawSectionHeader("Content Statistics");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            contentCreators.RefreshStatistics();

            EditorGUILayout.LabelField($"Items: {contentCreators.ItemCount}");
            EditorGUILayout.LabelField($"Recipes: {contentCreators.RecipeCount}");
            EditorGUILayout.LabelField($"Workstations: {contentCreators.WorkstationCount}");
            EditorGUILayout.LabelField($"Animals: {contentCreators.AnimalCount}");
            EditorGUILayout.LabelField($"Biomes: {contentCreators.BiomeCount}");
            EditorGUILayout.LabelField($"Status Effects: {contentCreators.StatusEffectCount}");

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Tab Content - Build
        private void DrawBuildTab()
        {
            DrawSectionHeader("Build Configuration");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Build Profile Selection
            EditorGUILayout.LabelField("Build Profile", EditorStyles.boldLabel);
            buildSettings.SelectedProfile = (BuildProfile)EditorGUILayout.EnumPopup(buildSettings.SelectedProfile);

            EditorGUILayout.Space(5);

            // Quick Build Buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🚀 Development Build", GUILayout.Height(40)))
            {
                BuildDevelopment();
            }

            if (GUILayout.Button("📦 Release Build", GUILayout.Height(40)))
            {
                BuildRelease();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Build Settings Display
            var profile = GetCurrentBuildProfile();
            EditorGUILayout.LabelField("Current Settings:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Backend: {profile.ScriptingBackend}");
            EditorGUILayout.LabelField($"  API Level: {profile.ApiLevel}");
            EditorGUILayout.LabelField($"  Target: {profile.BuildTarget}");
            EditorGUILayout.LabelField($"  IL2CPP Options: {profile.Il2CppCompilerConfiguration}");

            EditorGUILayout.EndVertical();

            // Addressables
            DrawSectionHeader("Addressables");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Build Addressables Content"))
            {
                BuildAddressablesContent();
            }

            if (GUILayout.Button("Clean Addressables Cache"))
            {
                CleanAddressablesCache();
            }

            EditorGUILayout.EndVertical();

            // Steam Integration
            DrawSectionHeader("Steam Integration");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            buildSettings.SteamAppId = EditorGUILayout.TextField("App ID", buildSettings.SteamAppId);
            buildSettings.SteamDepotId = EditorGUILayout.TextField("Depot ID", buildSettings.SteamDepotId);

            if (GUILayout.Button("Generate Steam Build Script"))
            {
                GenerateSteamBuildScript();
            }

            if (GUILayout.Button("Upload to Steam (SteamPipe)"))
            {
                UploadToSteam();
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Tab Content - Performance
        private void DrawPerformanceTab()
        {
            DrawSectionHeader("Performance Metrics");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("📊 Analyze Current Scene", GUILayout.Height(35)))
            {
                performanceMetrics.AnalyzeCurrentScene();
            }

            EditorGUILayout.Space(10);

            // Scene Metrics
            EditorGUILayout.LabelField("Scene Metrics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total GameObjects: {performanceMetrics.TotalGameObjects}");
            EditorGUILayout.LabelField($"Active GameObjects: {performanceMetrics.ActiveGameObjects}");
            EditorGUILayout.LabelField($"Total Triangles: {performanceMetrics.TotalTriangles:N0}");
            EditorGUILayout.LabelField($"Total Vertices: {performanceMetrics.TotalVertices:N0}");
            EditorGUILayout.LabelField($"Draw Calls (Est.): {performanceMetrics.EstimatedDrawCalls}");
            EditorGUILayout.LabelField($"SetPass Calls (Est.): {performanceMetrics.EstimatedSetPassCalls}");

            EditorGUILayout.Space(10);

            // Memory Usage
            EditorGUILayout.LabelField("Memory Usage", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Reserved: {performanceMetrics.TotalReservedMemoryMB:F1} MB");
            EditorGUILayout.LabelField($"Total Allocated: {performanceMetrics.TotalAllocatedMemoryMB:F1} MB");
            EditorGUILayout.LabelField($"Texture Memory: {performanceMetrics.TextureMemoryMB:F1} MB");
            EditorGUILayout.LabelField($"Mesh Memory: {performanceMetrics.MeshMemoryMB:F1} MB");

            EditorGUILayout.Space(10);

            // Performance Warnings
            if (performanceMetrics.Warnings.Count > 0)
            {
                EditorGUILayout.LabelField("⚠️ Performance Warnings", EditorStyles.boldLabel);
                foreach (var warning in performanceMetrics.Warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();

            // Optimization Tools
            DrawSectionHeader("Optimization Tools");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Generate LODs for Selected"))
            {
                GenerateLODs();
            }

            if (GUILayout.Button("Optimize Texture Settings"))
            {
                OptimizeTextureSettings();
            }

            if (GUILayout.Button("Batch Static Objects"))
            {
                BatchStaticObjects();
            }

            if (GUILayout.Button("Setup Occlusion Culling"))
            {
                SetupOcclusionCulling();
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Tab Content - Runtime
        private void DrawRuntimeTab()
        {
            DrawSectionHeader("Runtime Testing Tools");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Quick Play Modes", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🎮 Play from Boot"))
            {
                PlayFromBootScene();
            }

            if (GUILayout.Button("🏃 Play Current Scene"))
            {
                EditorApplication.EnterPlaymode();
            }

            if (GUILayout.Button("🛑 Stop"))
            {
                EditorApplication.ExitPlaymode();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Debug Options
            DrawSectionHeader("Debug Options");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.Toggle("God Mode", EditorPrefs.GetBool("WS_GodMode", false));
            EditorGUILayout.Toggle("Infinite Resources", EditorPrefs.GetBool("WS_InfiniteResources", false));
            EditorGUILayout.Toggle("Fast Time", EditorPrefs.GetBool("WS_FastTime", false));
            EditorGUILayout.Toggle("Debug UI", EditorPrefs.GetBool("WS_DebugUI", false));
            EditorGUILayout.Toggle("AI Debug Visualization", EditorPrefs.GetBool("WS_AIDebug", false));

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Spawn Test Items"))
            {
                SpawnTestItems();
            }

            if (GUILayout.Button("Reset Player Stats"))
            {
                ResetPlayerStats();
            }

            if (GUILayout.Button("Trigger Weather: Storm"))
            {
                TriggerWeatherStorm();
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Tab Content - Export
        private void DrawExportTab()
        {
            DrawSectionHeader("Project Export & Backup");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);

            if (GUILayout.Button("📤 Export Project (No Source Control)", GUILayout.Height(40)))
            {
                ExportProjectClean();
            }

            if (GUILayout.Button("📦 Create Asset Package"))
            {
                CreateAssetPackage();
            }

            if (GUILayout.Button("💾 Backup Save Files"))
            {
                BackupSaveFiles();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Documentation", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Project Report"))
            {
                GenerateProjectReport();
            }

            if (GUILayout.Button("Export Build Instructions"))
            {
                ExportBuildInstructions();
            }

            EditorGUILayout.EndVertical();

            // Marketing Assets
            DrawSectionHeader("Marketing Assets");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Capture Screenshot (4K)"))
            {
                CaptureScreenshot4K();
            }

            if (GUILayout.Button("Generate Steam Store Assets"))
            {
                GenerateSteamStoreAssets();
            }

            if (GUILayout.Button("Create GIF from Gameplay"))
            {
                CreateGameplayGIF();
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Helper Methods
        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = headerColor }
            };

            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            richTextStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true
            };
        }

        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, subHeaderStyle);
            DrawHorizontalLine();
            EditorGUILayout.Space(5);
        }

        private void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private void DrawStatusIndicator(string label, bool status, string tooltip)
        {
            var content = new GUIContent(
                status ? "✅" : "❌",
                tooltip
            );

            if (GUILayout.Button(content, GUILayout.Width(25)))
            {
                Debug.Log($"{label}: {(status ? "Active" : "Inactive")} - {tooltip}");
            }
        }

        private void RefreshProjectState()
        {
            validation.RunFullValidation();
            gitInfo.Refresh();
            contentCreators.RefreshStatistics();
            performanceMetrics.AnalyzeCurrentScene();
        }

        private bool IsHDRPConfigured()
        {
            var currentRP = GraphicsSettings.currentRenderPipeline;

            if (currentRP == null)
                return false;

#if UNITY_PIPELINE_HDRP
            return currentRP is HDRenderPipelineAsset;
#else
            return currentRP.GetType().Name.Contains("HDRenderPipelineAsset");
#endif
        }

        // Fixed: Use PlayerSettings API instead of EditorUserBuildSettings
        private ScriptingImplementation GetScriptingBackend()
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            return PlayerSettings.GetScriptingBackend(buildGroup);
        }

        private void SetScriptingBackend(ScriptingImplementation backend)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            PlayerSettings.SetScriptingBackend(buildGroup, backend);
        }
        #endregion

        #region Project Structure Creation
        private void CreateProjectStructure()
        {
            EditorApplication.delayCall += () =>
            {
                var folders = new[]
                {
                    "_Project/Code/Runtime/Core",
                    "_Project/Code/Runtime/Systems",
                    "_Project/Code/Runtime/Gameplay",
                    "_Project/Code/Runtime/Rendering",
                    "_Project/Code/Editor/Tools",
                    "_Project/Code/Editor/Validators",
                    "_Project/Code/Tests/Runtime",
                    "_Project/Code/Tests/Editor",
                    "_Project/Data/Items",
                    "_Project/Data/Recipes",
                    "_Project/Data/Workstations",
                    "_Project/Data/StatusEffects",
                    "_Project/Data/Biomes",
                    "_Project/Data/Animals",
                    "_Project/Art/Materials/Environment",
                    "_Project/Art/Materials/Characters",
                    "_Project/Art/Materials/Props",
                    "_Project/Art/Models/Environment",
                    "_Project/Art/Models/Characters",
                    "_Project/Art/Models/Props",
                    "_Project/Art/Textures/Environment",
                    "_Project/Art/Textures/Characters",
                    "_Project/Art/Textures/UI",
                    "_Project/Art/Animations",
                    "_Project/Art/VFX",
                    "_Project/Art/UI/Icons",
                    "_Project/Art/UI/HUD",
                    "_Project/Art/UI/Menus",
                    "_Project/Audio/Music",
                    "_Project/Audio/SFX/Environment",
                    "_Project/Audio/SFX/Character",
                    "_Project/Audio/SFX/UI",
                    "_Project/Audio/Voice",
                    "_Project/Audio/Mixer",
                    "_Project/Prefabs/Core",
                    "_Project/Prefabs/Environment",
                    "_Project/Prefabs/Characters",
                    "_Project/Prefabs/Items",
                    "_Project/Prefabs/UI",
                    "_Project/Prefabs/Systems",
                    "_Project/Scenes/Production",
                    "_Project/Scenes/Development",
                    "_Project/Scenes/Testing",
                    "_Project/Settings/HDRPSettings",
                    "_Project/Settings/InputSettings",
                    "_Project/Settings/QualitySettings",
                    "_Project/UI/Documents",
                    "_Project/UI/Styles",
                    "_ThirdParty",
                    "_Samples"
                };

                foreach (var folder in folders)
                {
                    var path = Path.Combine("Assets", folder);
                    if (!AssetDatabase.IsValidFolder(path))
                    {
                        CreateFolder(path);
                    }
                }

                AssetDatabase.Refresh();
                Debug.Log("✅ Project structure created successfully!");
            };
        }

        private void CreateFolder(string path)
        {
            var folders = path.Split('/');
            var parent = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                var child = folders[i];
                var fullPath = Path.Combine(parent, child);

                if (!AssetDatabase.IsValidFolder(fullPath))
                {
                    AssetDatabase.CreateFolder(parent, child);
                }

                parent = fullPath;
            }
        }
        #endregion

        #region Nested Classes
        [Serializable]
        private class ProjectValidation
        {
            public List<ValidationCheck> CriticalChecks = new List<ValidationCheck>();
            public List<ValidationCheck> PerformanceChecks = new List<ValidationCheck>();
            public List<ValidationCheck> HDRPChecks = new List<ValidationCheck>();
            public List<ValidationCheck> AssetChecks = new List<ValidationCheck>();

            public void RunFullValidation()
            {
                CriticalChecks.Clear();
                PerformanceChecks.Clear();
                HDRPChecks.Clear();
                AssetChecks.Clear();

                var window = GetWindow<WildSurvivalToolsHub>();

                // Critical Checks
                CriticalChecks.Add(new ValidationCheck(
                    "Unity Version",
                    Application.unityVersion.StartsWith("6000"),
                    "Unity 6 required for this project",
                    () => Debug.Log("Please upgrade to Unity 6")
                ));

                CriticalChecks.Add(new ValidationCheck(
                    "Scripting Backend",
                    window.GetScriptingBackend() == ScriptingImplementation.IL2CPP,
                    "IL2CPP required for production builds",
                    () => window.SetScriptingBackend(ScriptingImplementation.IL2CPP)
                ));

                var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                CriticalChecks.Add(new ValidationCheck(
                    "API Compatibility",
                    PlayerSettings.GetApiCompatibilityLevel(buildGroup) == ApiCompatibilityLevel.NET_Standard,
                    ".NET Standard 2.1 recommended",
                    () => PlayerSettings.SetApiCompatibilityLevel(buildGroup, ApiCompatibilityLevel.NET_Standard)
                ));

                // Performance Checks
                PerformanceChecks.Add(new ValidationCheck(
                    "Texture Streaming",
                    QualitySettings.streamingMipmapsActive,
                    "Texture streaming should be enabled",
                    () => QualitySettings.streamingMipmapsActive = true
                ));

                PerformanceChecks.Add(new ValidationCheck(
                    "VSync",
                    QualitySettings.vSyncCount <= 1,
                    "VSync should be set to Every V Blank or Off",
                    () => QualitySettings.vSyncCount = 1
                ));

                // HDRP Checks
#if UNITY_PIPELINE_HDRP
                var hdrpAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
                HDRPChecks.Add(new ValidationCheck(
                    "HDRP Active",
                    hdrpAsset != null,
                    "HDRP must be configured",
                    null
                ));
#else
                HDRPChecks.Add(new ValidationCheck(
                    "HDRP Package",
                    false,
                    "HDRP package not installed or not configured",
                    null
                ));
#endif

                // Asset Checks
                AssetChecks.Add(new ValidationCheck(
                    "Folder Structure",
                    AssetDatabase.IsValidFolder("Assets/_Project"),
                    "Recommended folder structure missing",
                    () => window.CreateProjectStructure()
                ));

                AssetChecks.Add(new ValidationCheck(
                    "No Duplicate Assets",
                    !AssetDatabase.IsValidFolder("Assets/Assets"),
                    "Duplicate Assets folder detected",
                    () => window.FixDuplicateAssetsFolder()
                ));
            }
        }

        [Serializable]
        private class ValidationCheck
        {
            public string Name;
            public CheckStatus Status;
            public string Message;
            public string StatusIcon => Status switch
            {
                CheckStatus.Pass => "✅",
                CheckStatus.Warning => "⚠️",
                CheckStatus.Fail => "❌",
                _ => "⭕"
            };
            public string FixAction;
            private Action fixCallback;

            public ValidationCheck(string name, bool condition, string message, Action fix)
            {
                Name = name;
                Status = condition ? CheckStatus.Pass : CheckStatus.Fail;
                Message = condition ? "OK" : message;
                FixAction = fix != null ? "Fix" : "";
                fixCallback = fix;
            }

            public void ExecuteFix()
            {
                fixCallback?.Invoke();
            }
        }

        private enum CheckStatus
        {
            NotRun,
            Pass,
            Warning,
            Fail
        }

        [Serializable]
        private class BuildSettings
        {
            public BuildProfile SelectedProfile = BuildProfile.Development;
            public string SteamAppId = "";
            public string SteamDepotId = "";
        }

        private enum BuildProfile
        {
            Development,
            Testing,
            Release,
            Steam
        }

        [Serializable]
        private class ContentCreators
        {
            public int ItemCount;
            public int RecipeCount;
            public int WorkstationCount;
            public int AnimalCount;
            public int BiomeCount;
            public int StatusEffectCount;

            public void RefreshStatistics()
            {
                // Count ScriptableObjects in project
                var itemPath = "Assets/_Project/Data/Items";
                if (AssetDatabase.IsValidFolder(itemPath))
                {
                    ItemCount = AssetDatabase.FindAssets("t:ScriptableObject", new[] { itemPath }).Length;
                }
                // Similar for other types...
            }
        }

        [Serializable]
        private class PerformanceMetrics
        {
            public int TotalGameObjects;
            public int ActiveGameObjects;
            public int TotalTriangles;
            public int TotalVertices;
            public int EstimatedDrawCalls;
            public int EstimatedSetPassCalls;
            public float TotalReservedMemoryMB;
            public float TotalAllocatedMemoryMB;
            public float TextureMemoryMB;
            public float MeshMemoryMB;
            public List<string> Warnings = new List<string>();

            public void AnalyzeCurrentScene()
            {
                Warnings.Clear();

                var scene = SceneManager.GetActiveScene();
                if (!scene.IsValid())
                    return;

                var allObjects = scene.GetRootGameObjects();
                TotalGameObjects = 0;
                ActiveGameObjects = 0;
                TotalTriangles = 0;
                TotalVertices = 0;

                foreach (var root in allObjects)
                {
                    AnalyzeGameObject(root);
                }

                // Memory stats
                TotalReservedMemoryMB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1024f / 1024f;
                TotalAllocatedMemoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f;

                // Warnings
                if (TotalTriangles > 1000000)
                {
                    Warnings.Add($"High triangle count: {TotalTriangles:N0}. Consider LODs.");
                }

                if (EstimatedDrawCalls > 3000)
                {
                    Warnings.Add($"High draw call count: {EstimatedDrawCalls}. Consider batching.");
                }
            }

            private void AnalyzeGameObject(GameObject obj)
            {
                TotalGameObjects++;
                if (obj.activeInHierarchy)
                    ActiveGameObjects++;

                var meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    TotalTriangles += meshFilter.sharedMesh.triangles.Length / 3;
                    TotalVertices += meshFilter.sharedMesh.vertexCount;
                }

                foreach (Transform child in obj.transform)
                {
                    AnalyzeGameObject(child.gameObject);
                }
            }
        }

        [Serializable]
        private class GitInfo
        {
            public bool IsRepository;
            public string Branch = "";
            public string RemoteUrl = "";

            public void Refresh()
            {
                var projectPath = Path.GetDirectoryName(Application.dataPath);
                var gitPath = Path.Combine(projectPath, ".git");
                IsRepository = Directory.Exists(gitPath);

                if (IsRepository)
                {
                    // Get branch and remote info via git commands
                    Branch = ExecuteGit("rev-parse --abbrev-ref HEAD").Trim();
                    RemoteUrl = ExecuteGit("config --get remote.origin.url").Trim();
                }
            }

            private string ExecuteGit(string arguments)
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        return process.StandardOutput.ReadToEnd();
                    }
                }
                catch
                {
                    return "";
                }
            }
        }

        private class BuildProfileSettings
        {
            public ScriptingImplementation ScriptingBackend;
            public ApiCompatibilityLevel ApiLevel;
            public BuildTarget BuildTarget;
            public Il2CppCompilerConfiguration Il2CppCompilerConfiguration;
        }
        #endregion

        #region Implementation Methods
        private void ConfigureForRelease()
        {
            SetScriptingBackend(ScriptingImplementation.IL2CPP);
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            PlayerSettings.SetIl2CppCompilerConfiguration(buildGroup, Il2CppCompilerConfiguration.Release);
            Debug.Log("✅ Configured for IL2CPP Release build");
        }

        private void ConfigureForDevelopment()
        {
            SetScriptingBackend(ScriptingImplementation.Mono2x);
            EditorUserBuildSettings.development = true;
            Debug.Log("✅ Configured for Mono Development build");
        }

        private void SetupHDRPQualityTiers()
        {
            Debug.Log("Setting up HDRP quality tiers...");
#if UNITY_PIPELINE_HDRP
            // Implementation would create quality settings assets for HDRP
            Debug.Log("HDRP quality tiers configuration would go here");
#else
            Debug.LogWarning("HDRP package not installed. Please install HDRP first.");
#endif
        }

        private void ConfigureInputSystem()
        {
            Debug.Log("Configuring Input System...");
            // Implementation would set up Input System package settings
        }

        private void CreateAssemblyDefinitions()
        {
            Debug.Log("Creating assembly definitions...");
            // Implementation would create .asmdef files
        }

        private void InitializeGitRepository()
        {
            Debug.Log("Initializing Git repository...");
            // Implementation would run git init
        }

        private void CreateGitFiles()
        {
            var gitignore = @"# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/

# Never track these
/Mirror/
/ProjectExports/
/ProjectTools/
*_PublicMirror_*/
*_Backup_*/

# Visual Studio cache
.vs/
*.csproj
*.unityproj
*.sln

# Builds
*.apk
*.aab
*.unitypackage
*.app

# OS generated
.DS_Store
Thumbs.db";

            var gitattributes = @"# Unity
*.cs diff=csharp text
*.shader text
*.mat merge=unityyamlmerge eol=lf
*.prefab merge=unityyamlmerge eol=lf
*.unity merge=unityyamlmerge eol=lf
*.asset merge=unityyamlmerge eol=lf
*.meta merge=unityyamlmerge eol=lf

# Git LFS
*.jpg filter=lfs diff=lfs merge=lfs -text
*.png filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text
*.fbx filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.mp3 filter=lfs diff=lfs merge=lfs -text";

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), ".gitignore"), gitignore);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), ".gitattributes"), gitattributes);

            Debug.Log("✅ Git files created");
        }

        private BuildProfileSettings GetCurrentBuildProfile()
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            return new BuildProfileSettings
            {
                ScriptingBackend = PlayerSettings.GetScriptingBackend(buildGroup),
                ApiLevel = PlayerSettings.GetApiCompatibilityLevel(buildGroup),
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                Il2CppCompilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(buildGroup)
            };
        }

        // Content creation stubs
        private void CreateItemDefinition() => Debug.Log("Creating Item Definition...");
        private void CreateRecipeDefinition() => Debug.Log("Creating Recipe Definition...");
        private void CreateWorkstationDefinition() => Debug.Log("Creating Workstation Definition...");
        private void CreateAnimalDefinition() => Debug.Log("Creating Animal Definition...");
        private void CreateBiomeDefinition() => Debug.Log("Creating Biome Definition...");
        private void CreateStatusEffectDefinition() => Debug.Log("Creating Status Effect Definition...");

        private void ValidateAllItems() => Debug.Log("Validating all items...");
        private void ValidateAllRecipes() => Debug.Log("Validating all recipes...");
        private void GenerateItemIcons() => Debug.Log("Generating item icons...");
        private void UpdateLocalizationKeys() => Debug.Log("Updating localization keys...");

        private void BuildDevelopment() => Debug.Log("Building development version...");
        private void BuildRelease() => Debug.Log("Building release version...");
        private void BuildAddressablesContent() => Debug.Log("Building Addressables content...");
        private void CleanAddressablesCache() => Debug.Log("Cleaning Addressables cache...");
        private void GenerateSteamBuildScript() => Debug.Log("Generating Steam build script...");
        private void UploadToSteam() => Debug.Log("Uploading to Steam...");

        private void GenerateLODs() => Debug.Log("Generating LODs...");
        private void OptimizeTextureSettings() => Debug.Log("Optimizing texture settings...");
        private void BatchStaticObjects() => Debug.Log("Batching static objects...");
        private void SetupOcclusionCulling() => Debug.Log("Setting up occlusion culling...");

        private void PlayFromBootScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/_Boot.unity");
            EditorApplication.EnterPlaymode();
        }

        private void SpawnTestItems() => Debug.Log("Spawning test items...");
        private void ResetPlayerStats() => Debug.Log("Resetting player stats...");
        private void TriggerWeatherStorm() => Debug.Log("Triggering weather storm...");

        private void ExportProjectClean() => Debug.Log("Exporting clean project...");
        private void CreateAssetPackage() => Debug.Log("Creating asset package...");
        private void BackupSaveFiles() => Debug.Log("Backing up save files...");
        private void GenerateProjectReport() => Debug.Log("Generating project report...");
        private void ExportBuildInstructions() => Debug.Log("Exporting build instructions...");

        private void CaptureScreenshot4K() => Debug.Log("Capturing 4K screenshot...");
        private void GenerateSteamStoreAssets() => Debug.Log("Generating Steam store assets...");
        private void CreateGameplayGIF() => Debug.Log("Creating gameplay GIF...");

        private void OnCompilationStarted(object obj) => Debug.Log("Compilation started...");
        private void OnCompilationFinished(object obj) => Debug.Log("Compilation finished.");
        #endregion
    }
}
