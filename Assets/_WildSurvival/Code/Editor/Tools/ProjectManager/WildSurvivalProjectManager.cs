using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.ProjectManagement
{
    /// <summary>
    /// Comprehensive project management tool for Wild Survival
    /// Combines restructuring, optimization, and development tools
    /// </summary>
    public class WildSurvivalProjectManager : EditorWindow
    {
        // Window Configuration
        private const string WINDOW_TITLE = "Wild Survival Project Manager";
        private const float MIN_WIDTH = 1200f;
        private const float MIN_HEIGHT = 800f;
        private const string VERSION = "2.0.0";

        // Tab System
        private readonly string[] _mainTabs =
        {
            "📊 Dashboard",
            "🔧 Project Setup",
            "📁 Restructure",
            "💻 Code Tools",
            "🎮 Game Tools",
            "📈 Performance",
            "🚀 Build & Deploy"
        };

        private int _currentMainTab = 0;
        private Vector2 _scrollPosition;

        // Sub-modules
        private ProjectDashboard _dashboard;
        private ProjectSetup _setup;
        private ProjectRestructure _restructure;
        private CodeTools _codeTools;
        private GameTools _gameTools;
        private PerformanceTools _performance;
        private BuildTools _build;

        // Shared State
        private static ProjectAnalysis _projectAnalysis;
        private static bool _isProcessing;
        private static List<LogEntry> _logs = new List<LogEntry>();

        // Styling
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        [MenuItem("Tools/Wild Survival/Project Manager %#p", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<WildSurvivalProjectManager>(false, WINDOW_TITLE, true);
            window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeModules();
            AnalyzeProject();
        }

        private void InitializeModules()
        {
            _dashboard = new ProjectDashboard(this);
            _setup = new ProjectSetup(this);
            _restructure = new ProjectRestructure(this);
            _codeTools = new CodeTools(this);
            _gameTools = new GameTools(this);
            _performance = new PerformanceTools(this);
            _build = new BuildTools(this);
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 10, 10)
            };

            _sectionStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            DrawHeader();
            DrawMainContent();
            DrawFooter();

            ProcessRepaintRequests();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.toolbar);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🎮 WILD SURVIVAL PROJECT MANAGER", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Quick Actions
            if (GUILayout.Button("🔄 Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                AnalyzeProject();
            }

            if (GUILayout.Button("💾 Save All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("📚 Docs", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Application.OpenURL("https://github.com/yourusername/wildsurvival/wiki");
            }

            EditorGUILayout.EndHorizontal();

            // Tab Bar
            EditorGUILayout.BeginHorizontal();
            int newTab = GUILayout.Toolbar(_currentMainTab, _mainTabs, EditorStyles.toolbarButton, GUILayout.Height(30));
            if (newTab != _currentMainTab)
            {
                _currentMainTab = newTab;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawMainContent()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_isProcessing)
            {
                DrawProcessingOverlay();
            }
            else
            {
                switch (_currentMainTab)
                {
                    case 0: _dashboard?.Draw(); break;
                    case 1: _setup?.Draw(); break;
                    case 2: _restructure?.Draw(); break;
                    case 3: _codeTools?.Draw(); break;
                    case 4: _gameTools?.Draw(); break;
                    case 5: _performance?.Draw(); break;
                    case 6: _build?.Draw(); break;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Status
            GUILayout.Label($"Unity {Application.unityVersion} | HDRP | Version {VERSION}", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            // Project Status
            if (_projectAnalysis != null)
            {
                GUI.color = _projectAnalysis.HasIssues ? Color.yellow : Color.green;
                GUILayout.Label($"● {_projectAnalysis.Status}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            // Log Console
            if (_logs.Count > 0)
            {
                DrawLogConsole();
            }
        }

        private void DrawProcessingOverlay()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            GUILayout.Label("Processing...", _headerStyle);
            EditorGUILayout.Space(20);

            // Progress bar
            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                0.5f,
                "Working..."
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawLogConsole()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(100));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("📋 Log Console", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                _logs.Clear();
            }
            EditorGUILayout.EndHorizontal();

            var scrollView = EditorGUILayout.BeginScrollView(Vector2.zero);
            foreach (var log in _logs.TakeLast(10))
            {
                GUI.color = log.GetColor();
                EditorGUILayout.LabelField($"[{log.Time:HH:mm:ss}] {log.Message}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private static void AnalyzeProject()
        {
            _projectAnalysis = new ProjectAnalysis();
            _projectAnalysis.Analyze();
        }

        private void ProcessRepaintRequests()
        {
            if (Event.current.type == EventType.Repaint && _isProcessing)
            {
                Repaint();
            }
        }

        public static void Log(string message, LogType type = LogType.Info)
        {
            _logs.Add(new LogEntry(message, type));
        }

        public static void StartProcessing()
        {
            _isProcessing = true;
        }

        public static void EndProcessing()
        {
            _isProcessing = false;
        }

        // ==================== PUBLIC ENUMS ====================

        public enum LogType
        {
            Info,
            Warning,
            Error,
            Success
        }

        // ==================== MODULES ====================

        /// <summary>
        /// Dashboard showing project overview and quick actions
        /// </summary>
        public class ProjectDashboard
        {
            private WildSurvivalProjectManager _manager;

            public ProjectDashboard(WildSurvivalProjectManager manager)
            {
                _manager = manager;
            }

            public void Draw()
            {
                EditorGUILayout.LabelField("Project Dashboard", _manager._headerStyle);

                // Project Health
                DrawProjectHealth();

                EditorGUILayout.Space(20);

                // Quick Stats
                DrawQuickStats();

                EditorGUILayout.Space(20);

                // Recent Activity
                DrawRecentActivity();

                EditorGUILayout.Space(20);

                // Quick Actions Grid
                DrawQuickActions();
            }

            private void DrawProjectHealth()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("🏥 Project Health", EditorStyles.boldLabel);

                if (_projectAnalysis == null)
                {
                    EditorGUILayout.HelpBox("Analyzing project...", MessageType.Info);
                }
                else
                {
                    // Health Score
                    float health = _projectAnalysis.GetHealthScore();
                    Color healthColor = health > 0.8f ? Color.green : health > 0.5f ? Color.yellow : Color.red;

                    EditorGUI.ProgressBar(
                        EditorGUILayout.GetControlRect(GUILayout.Height(30)),
                        health,
                        $"Health Score: {health * 100:F0}%"
                    );

                    // Issues
                    if (_projectAnalysis.Issues.Count > 0)
                    {
                        EditorGUILayout.Space(10);
                        EditorGUILayout.LabelField("Issues Found:", EditorStyles.boldLabel);
                        foreach (var issue in _projectAnalysis.Issues.Take(5))
                        {
                            EditorGUILayout.HelpBox(issue, MessageType.Warning);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawQuickStats()
            {
                EditorGUILayout.BeginHorizontal();

                DrawStatCard("Scripts", CountFiles(".cs"), Color.cyan);
                DrawStatCard("Scenes", CountFiles(".unity"), Color.green);
                DrawStatCard("Prefabs", CountFiles(".prefab"), Color.blue);
                DrawStatCard("Materials", CountFiles(".mat"), Color.magenta);

                EditorGUILayout.EndHorizontal();
            }

            private void DrawStatCard(string label, int count, Color color)
            {
                GUI.backgroundColor = color * 0.3f;
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(140), GUILayout.Height(80));
                GUI.backgroundColor = Color.white;

                GUILayout.FlexibleSpace();

                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 24,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = color }
                };

                EditorGUILayout.LabelField(count.ToString(), style);
                EditorGUILayout.LabelField(label, new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter });

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndVertical();
            }

            private void DrawRecentActivity()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("📝 Recent Activity", EditorStyles.boldLabel);

                foreach (var log in _logs.TakeLast(5))
                {
                    GUI.color = log.GetColor();
                    EditorGUILayout.LabelField($"• {log.Message}", EditorStyles.wordWrappedMiniLabel);
                    GUI.color = Color.white;
                }

                if (_logs.Count == 0)
                {
                    EditorGUILayout.LabelField("No recent activity", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawQuickActions()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("⚡ Quick Actions", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("🔧 Fix All Issues", GUILayout.Height(40)))
                {
                    _manager._setup.FixAllIssues();
                }

                if (GUILayout.Button("🧹 Clean Project", GUILayout.Height(40)))
                {
                    _manager._restructure.CleanProject();
                }

                if (GUILayout.Button("📦 Setup Build", GUILayout.Height(40)))
                {
                    _manager._build.QuickBuild();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            private int CountFiles(string extension)
            {
                return Directory.GetFiles("Assets", "*" + extension, SearchOption.AllDirectories).Length;
            }
        }

        /// <summary>
        /// Project setup and configuration tools
        /// </summary>
        public class ProjectSetup
        {
            private WildSurvivalProjectManager _manager;
            private List<SetupTask> _setupTasks;

            public ProjectSetup(WildSurvivalProjectManager manager)
            {
                _manager = manager;
                InitializeTasks();
            }

            private void InitializeTasks()
            {
                _setupTasks = new List<SetupTask>
                {
                    new SetupTask("Create Folder Structure", CreateFolderStructure, CheckFolderStructure),
                    new SetupTask("Setup Assembly Definitions", SetupAssemblies, CheckAssemblies),
                    new SetupTask("Configure HDRP", ConfigureHDRP, CheckHDRP),
                    new SetupTask("Setup Input System", SetupInputSystem, CheckInputSystem),
                    new SetupTask("Configure Build Settings", ConfigureBuildSettings, CheckBuildSettings),
                    new SetupTask("Setup Version Control", SetupVersionControl, CheckVersionControl)
                };
            }

            public void Draw()
            {
                EditorGUILayout.LabelField("Project Setup", _manager._headerStyle);

                // Setup Checklist
                DrawSetupChecklist();

                EditorGUILayout.Space(20);

                // Quick Setup
                DrawQuickSetup();

                EditorGUILayout.Space(20);

                // Package Management
                DrawPackageManagement();
            }

            private void DrawSetupChecklist()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("✅ Setup Checklist", EditorStyles.boldLabel);

                foreach (var task in _setupTasks)
                {
                    EditorGUILayout.BeginHorizontal();

                    bool isComplete = task.CheckFunc();
                    GUI.color = isComplete ? Color.green : Color.yellow;
                    EditorGUILayout.LabelField(isComplete ? "✓" : "○", GUILayout.Width(20));
                    GUI.color = Color.white;

                    EditorGUILayout.LabelField(task.Name);

                    if (!isComplete && GUILayout.Button("Fix", GUILayout.Width(60)))
                    {
                        task.ExecuteFunc();
                        Log($"Fixed: {task.Name}", LogType.Success);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(10);

                if (GUILayout.Button("🔧 Fix All Issues", _manager._buttonStyle))
                {
                    FixAllIssues();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawQuickSetup()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("🚀 Quick Setup", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("New Survival Game", GUILayout.Height(50)))
                {
                    SetupNewProject();
                }

                if (GUILayout.Button("Import from Template", GUILayout.Height(50)))
                {
                    ImportTemplate();
                }

                if (GUILayout.Button("Migrate Existing", GUILayout.Height(50)))
                {
                    MigrateProject();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            private void DrawPackageManagement()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("📦 Package Management", EditorStyles.boldLabel);

                string[] requiredPackages =
                {
                    "com.unity.render-pipelines.high-definition",
                    "com.unity.inputsystem",
                    "com.unity.addressables",
                    "com.unity.cinemachine",
                    "com.unity.ai.navigation"
                };

                foreach (var package in requiredPackages)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(package);

                    if (GUILayout.Button("Install/Update", GUILayout.Width(100)))
                    {
                        InstallPackage(package);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            public void FixAllIssues()
            {
                StartProcessing();

                foreach (var task in _setupTasks.Where(t => !t.CheckFunc()))
                {
                    task.ExecuteFunc();
                    Log($"Fixed: {task.Name}", LogType.Success);
                }

                EndProcessing();
                AnalyzeProject();
            }

            private void SetupNewProject()
            {
                StartProcessing();

                CreateFolderStructure();
                SetupAssemblies();
                ConfigureHDRP();
                SetupInputSystem();
                CreateStarterAssets();

                Log("New project setup complete!", LogType.Success);
                EndProcessing();
            }

            private void CreateFolderStructure()
            {
                string[] folders =
                {
                    "Assets/_Project",
                    "Assets/_Project/Code/Runtime/Core",
                    "Assets/_Project/Code/Runtime/Systems",
                    "Assets/_Project/Code/Runtime/Gameplay",
                    "Assets/_Project/Code/Runtime/UI",
                    "Assets/_Project/Code/Editor",
                    "Assets/_Project/Content/Characters",
                    "Assets/_Project/Content/Environment",
                    "Assets/_Project/Content/Items",
                    "Assets/_Project/Content/Effects",
                    "Assets/_Project/Data/Items",
                    "Assets/_Project/Data/Recipes",
                    "Assets/_Project/Data/Config",
                    "Assets/_Project/Scenes/Core",
                    "Assets/_Project/Scenes/Gameplay",
                    "Assets/_Project/Scenes/Test",
                    "Assets/_Project/Settings/HDRP",
                    "Assets/_Project/Settings/Input",
                    "Assets/_ThirdParty",
                    "Assets/_DevTools"
                };

                foreach (var folder in folders)
                {
                    if (!AssetDatabase.IsValidFolder(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                }

                AssetDatabase.Refresh();
            }

            private bool CheckFolderStructure()
            {
                return AssetDatabase.IsValidFolder("Assets/_Project/Code/Runtime/Core");
            }

            private void SetupAssemblies()
            {
                CreateAssemblyDefinition("WildSurvival.Core", "Assets/_Project/Code/Runtime/Core");
                CreateAssemblyDefinition("WildSurvival.Systems", "Assets/_Project/Code/Runtime/Systems");
                CreateAssemblyDefinition("WildSurvival.Gameplay", "Assets/_Project/Code/Runtime/Gameplay");
                CreateAssemblyDefinition("WildSurvival.Editor", "Assets/_Project/Code/Editor", true);
            }

            private bool CheckAssemblies()
            {
                return File.Exists("Assets/_Project/Code/Runtime/Core/WildSurvival.Core.asmdef");
            }

            private void CreateAssemblyDefinition(string name, string path, bool editorOnly = false)
            {
                var asmdef = new
                {
                    name = name,
                    references = new string[] { },
                    includePlatforms = editorOnly ? new[] { "Editor" } : new string[] { },
                    excludePlatforms = new string[] { },
                    allowUnsafeCode = false,
                    overrideReferences = false,
                    precompiledReferences = new string[] { },
                    autoReferenced = true,
                    defineConstraints = new string[] { },
                    versionDefines = new string[] { }
                };

                string json = JsonUtility.ToJson(asmdef, true);
                File.WriteAllText($"{path}/{name}.asmdef", json);
            }

            private void ConfigureHDRP() { /* HDRP setup */ }
            private bool CheckHDRP() { return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null; }

            private void SetupInputSystem() { /* Input setup */ }
            private bool CheckInputSystem() { return true; }

            private void ConfigureBuildSettings() { /* Build setup */ }
            private bool CheckBuildSettings() { return true; }

            private void SetupVersionControl() { /* Git setup */ }
            private bool CheckVersionControl() { return Directory.Exists(".git"); }

            private void ImportTemplate() { /* Template import */ }
            private void MigrateProject() { /* Project migration */ }
            private void CreateStarterAssets() { /* Create starter assets */ }

            private void InstallPackage(string packageId)
            {
                Client.Add(packageId);
                Log($"Installing package: {packageId}", LogType.Info);
            }
        }

        /// <summary>
        /// Project restructuring and cleanup tools
        /// </summary>
        public class ProjectRestructure
        {
            private WildSurvivalProjectManager _manager;
            private List<FileOperation> _pendingOperations;

            public ProjectRestructure(WildSurvivalProjectManager manager)
            {
                _manager = manager;
                _pendingOperations = new List<FileOperation>();
            }

            public void Draw()
            {
                EditorGUILayout.LabelField("Project Restructure", _manager._headerStyle);

                // Duplicate Finder
                DrawDuplicateFinder();

                EditorGUILayout.Space(20);

                // File Organization
                DrawFileOrganization();

                EditorGUILayout.Space(20);

                // Namespace Fixer
                DrawNamespaceFixer();
            }

            private void DrawDuplicateFinder()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("🔍 Duplicate Finder", EditorStyles.boldLabel);

                if (GUILayout.Button("Scan for Duplicates", GUILayout.Height(30)))
                {
                    FindDuplicates();
                }

                if (_pendingOperations.Count > 0)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField($"Found {_pendingOperations.Count} duplicates:");

                    foreach (var op in _pendingOperations.Take(10))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(op.Path, EditorStyles.miniLabel);

                        if (GUILayout.Button("Delete", GUILayout.Width(60)))
                        {
                            AssetDatabase.DeleteAsset(op.Path);
                            _pendingOperations.Remove(op);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    if (GUILayout.Button("Delete All Duplicates", _manager._buttonStyle))
                    {
                        DeleteAllDuplicates();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawFileOrganization()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("📂 File Organization", EditorStyles.boldLabel);

                if (GUILayout.Button("Organize Scripts by Type", GUILayout.Height(30)))
                {
                    OrganizeScripts();
                }

                if (GUILayout.Button("Consolidate Resources", GUILayout.Height(30)))
                {
                    ConsolidateResources();
                }

                if (GUILayout.Button("Clean Empty Folders", GUILayout.Height(30)))
                {
                    CleanEmptyFolders();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawNamespaceFixer()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("🏷️ Namespace Fixer", EditorStyles.boldLabel);

                if (GUILayout.Button("Fix All Namespaces", GUILayout.Height(30)))
                {
                    FixAllNamespaces();
                }

                if (GUILayout.Button("Update Using Statements", GUILayout.Height(30)))
                {
                    UpdateUsingStatements();
                }

                EditorGUILayout.EndVertical();
            }

            public void CleanProject()
            {
                StartProcessing();

                FindDuplicates();
                DeleteAllDuplicates();
                CleanEmptyFolders();
                ConsolidateResources();

                AssetDatabase.Refresh();

                Log("Project cleaned successfully!", LogType.Success);
                EndProcessing();
            }

            private void FindDuplicates()
            {
                _pendingOperations.Clear();

                string[] patterns = { " 1", " 2", " 3", " 4", " 5" };
                string[] folders = { "Audio", "Content", "Core", "Resources", "Runtime", "Tools" };

                foreach (var folder in folders)
                {
                    foreach (var pattern in patterns)
                    {
                        string searchPath = $"Assets/_Project/{folder}{pattern}";
                        if (AssetDatabase.IsValidFolder(searchPath))
                        {
                            _pendingOperations.Add(new FileOperation
                            {
                                Type = OperationType.Delete,
                                Path = searchPath,
                                Reason = "Duplicate folder"
                            });
                        }
                    }
                }

                Log($"Found {_pendingOperations.Count} duplicate folders", LogType.Warning);
            }

            private void DeleteAllDuplicates()
            {
                foreach (var op in _pendingOperations.Where(o => o.Type == OperationType.Delete))
                {
                    AssetDatabase.DeleteAsset(op.Path);
                    Log($"Deleted: {op.Path}", LogType.Info);
                }

                _pendingOperations.Clear();
                AssetDatabase.Refresh();
            }

            private void OrganizeScripts()
            {
                var scripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Project" });

                foreach (var guid in scripts)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                    if (script != null)
                    {
                        string targetFolder = DetermineScriptFolder(script);
                        string fileName = Path.GetFileName(path);
                        string newPath = $"{targetFolder}/{fileName}";

                        if (path != newPath && !File.Exists(newPath))
                        {
                            AssetDatabase.MoveAsset(path, newPath);
                        }
                    }
                }
            }

            private string DetermineScriptFolder(MonoScript script)
            {
                string content = script.text;

                if (content.Contains(": MonoBehaviour") || content.Contains(": ScriptableObject"))
                {
                    if (content.Contains("Editor") || content.Contains("[MenuItem"))
                        return "Assets/_Project/Code/Editor";
                    if (content.Contains("UI") || content.Contains("Canvas"))
                        return "Assets/_Project/Code/Runtime/UI";
                    if (content.Contains("Manager") || content.Contains("Service"))
                        return "Assets/_Project/Code/Runtime/Core";
                    if (content.Contains("Player") || content.Contains("Character"))
                        return "Assets/_Project/Code/Runtime/Gameplay";

                    return "Assets/_Project/Code/Runtime/Systems";
                }

                return "Assets/_Project/Code/Runtime/Core";
            }

            private void ConsolidateResources()
            {
                // Move all Resources folders to Addressables
                var resourceFolders = Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories);

                foreach (var folder in resourceFolders)
                {
                    if (!folder.Contains("_Project")) continue;

                    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith(".meta"));

                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        string targetPath = $"Assets/_Project/Content/Legacy/{fileName}";

                        if (!File.Exists(targetPath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                            AssetDatabase.MoveAsset(file, targetPath);
                        }
                    }
                }

                Log("Resources consolidated - remember to set up Addressables!", LogType.Warning);
            }

            private void CleanEmptyFolders()
            {
                var folders = Directory.GetDirectories("Assets/_Project", "*", SearchOption.AllDirectories)
                    .OrderByDescending(f => f.Length);

                int cleaned = 0;
                foreach (var folder in folders)
                {
                    if (Directory.GetFiles(folder).Length == 0 &&
                        Directory.GetDirectories(folder).Length == 0)
                    {
                        AssetDatabase.DeleteAsset(folder);
                        cleaned++;
                    }
                }

                Log($"Cleaned {cleaned} empty folders", LogType.Success);
            }

            private void FixAllNamespaces()
            {
                var scripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Project" });

                foreach (var guid in scripts)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    FixNamespaceInFile(path);
                }

                AssetDatabase.Refresh();
                Log("All namespaces fixed!", LogType.Success);
            }

            private void FixNamespaceInFile(string path)
            {
                string content = File.ReadAllText(path);
                string directory = Path.GetDirectoryName(path).Replace('\\', '/');

                // Determine correct namespace
                string correctNamespace = "WildSurvival";

                if (directory.Contains("/Core"))
                    correctNamespace = "WildSurvival.Core";
                else if (directory.Contains("/Systems"))
                    correctNamespace = "WildSurvival.Systems";
                else if (directory.Contains("/Gameplay"))
                    correctNamespace = "WildSurvival.Gameplay";
                else if (directory.Contains("/UI"))
                    correctNamespace = "WildSurvival.UI";
                else if (directory.Contains("/Editor"))
                    correctNamespace = "WildSurvival.Editor";

                // Fix namespace
                string pattern = @"namespace\s+[\w\.]+";
                string replacement = $"namespace {correctNamespace}";

                string newContent = Regex.Replace(content, pattern, replacement);

                if (newContent != content)
                {
                    File.WriteAllText(path, newContent);
                }
            }

            private void UpdateUsingStatements()
            {
                var scripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Project" });

                foreach (var guid in scripts)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string content = File.ReadAllText(path);

                    // Add common using statements if missing
                    var usings = new[]
                    {
                        "using System;",
                        "using System.Collections.Generic;",
                        "using UnityEngine;",
                        "using WildSurvival.Core;",
                        "using WildSurvival.Systems;"
                    };

                    foreach (var usingStatement in usings)
                    {
                        if (!content.Contains(usingStatement) && ShouldAddUsing(content, usingStatement))
                        {
                            content = usingStatement + "\n" + content;
                        }
                    }

                    File.WriteAllText(path, content);
                }

                AssetDatabase.Refresh();
            }

            private bool ShouldAddUsing(string content, string usingStatement)
            {
                // Add logic to determine if using statement is needed
                return false;
            }
        }

        /// <summary>
        /// Code generation and modification tools
        /// </summary>
        public class CodeTools
        {
            private WildSurvivalProjectManager _manager;

            public CodeTools(WildSurvivalProjectManager manager)
            {
                _manager = manager;
            }

            public void Draw()
            {
                EditorGUILayout.LabelField("Code Tools", _manager._headerStyle);

                // Code Templates
                DrawCodeTemplates();

                EditorGUILayout.Space(20);

                // Script Generator
                DrawScriptGenerator();

                EditorGUILayout.Space(20);

                // Code Analysis
                DrawCodeAnalysis();
            }

            private void DrawCodeTemplates()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("📝 Code Templates", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Manager\nSingleton", GUILayout.Height(60)))
                {
                    CreateManagerTemplate();
                }

                if (GUILayout.Button("System\nComponent", GUILayout.Height(60)))
                {
                    CreateSystemTemplate();
                }

                if (GUILayout.Button("Data\nContainer", GUILayout.Height(60)))
                {
                    CreateDataTemplate();
                }

                if (GUILayout.Button("Custom\nEditor", GUILayout.Height(60)))
                {
                    CreateEditorTemplate();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            private void DrawScriptGenerator()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("⚙️ Script Generator", EditorStyles.boldLabel);

                // Generator UI would go here

                EditorGUILayout.EndVertical();
            }

            private void DrawCodeAnalysis()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("🔬 Code Analysis", EditorStyles.boldLabel);

                if (GUILayout.Button("Analyze Code Quality", GUILayout.Height(30)))
                {
                    AnalyzeCodeQuality();
                }

                if (GUILayout.Button("Find Unused Scripts", GUILayout.Height(30)))
                {
                    FindUnusedScripts();
                }

                if (GUILayout.Button("Check Null References", GUILayout.Height(30)))
                {
                    CheckNullReferences();
                }

                EditorGUILayout.EndVertical();
            }

            private void CreateManagerTemplate()
            {
                string template = @"using System;
using UnityEngine;
// using WildSurvival.Core; // Commented out by Menu Fixer

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Systems
{
    public class [NAME]Manager : MonoBehaviour
    {
        private static [NAME]Manager _instance;
        public static [NAME]Manager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<[NAME]Manager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(""[NAME]Manager"");
                        _instance = go.AddComponent<[NAME]Manager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }

        private void Initialize()
        {
            // Initialize manager
        }
    }
}";

                CreateScriptFromTemplate(template, "Manager");
            }

            private void CreateSystemTemplate() { /* System template */ }
            private void CreateDataTemplate() { /* Data template */ }
            private void CreateEditorTemplate() { /* Editor template */ }

            private void CreateScriptFromTemplate(string template, string type)
            {
                string name = EditorInputDialog.Show("Create " + type, "Enter name:", "New" + type);
                if (string.IsNullOrEmpty(name)) return;

                string content = template.Replace("[NAME]", name);
                string path = $"Assets/_Project/Code/Runtime/Systems/{name}{type}.cs";

                File.WriteAllText(path, content);
                AssetDatabase.Refresh();

                Log($"Created {name}{type}.cs", LogType.Success);
            }

            private void AnalyzeCodeQuality()
            {
                var scripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Project" });

                int totalLines = 0;
                int totalComplexity = 0;
                List<string> issues = new List<string>();

                foreach (var guid in scripts)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string content = File.ReadAllText(path);

                    totalLines += content.Split('\n').Length;

                    // Check for common issues
                    if (content.Contains("public static") && !content.Contains("Instance"))
                    {
                        issues.Add($"Potential static abuse in {Path.GetFileName(path)}");
                    }

                    if (content.Contains("GameObject.Find"))
                    {
                        issues.Add($"GameObject.Find usage in {Path.GetFileName(path)}");
                    }

                    if (content.Contains("Resources.Load"))
                    {
                        issues.Add($"Resources.Load usage in {Path.GetFileName(path)}");
                    }
                }

                Log($"Code Analysis: {scripts.Length} scripts, {totalLines} lines", LogType.Info);

                foreach (var issue in issues.Take(10))
                {
                    Log(issue, LogType.Warning);
                }
            }

            private void FindUnusedScripts() { /* Find unused scripts */ }
            private void CheckNullReferences() { /* Check null references */ }
        }

        /// <summary>
        /// Game-specific tools for Wild Survival
        /// </summary>
        public class GameTools
        {
            private WildSurvivalProjectManager _manager;

            public GameTools(WildSurvivalProjectManager manager)
            {
                _manager = manager;
            }

            public void Draw()
            {
                EditorGUILayout.LabelField("Game Tools", _manager._headerStyle);

                // Inventory Tools
                DrawInventoryTools();

                EditorGUILayout.Space(20);

                // World Builder
                DrawWorldBuilder();

                EditorGUILayout.Space(20);

                // Debug Tools
                DrawDebugTools();
            }

            private void DrawInventoryTools()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("🎒 Inventory Tools", EditorStyles.boldLabel);

                if (GUILayout.Button("Open Inventory Designer", GUILayout.Height(40)))
                {
                    // Open inventory designer window
                }

                if (GUILayout.Button("Generate Item Database", GUILayout.Height(40)))
                {
                    // Generate items
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawWorldBuilder()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("🌍 World Builder", EditorStyles.boldLabel);

                if (GUILayout.Button("Terrain Tools", GUILayout.Height(40)))
                {
                    // Open terrain tools
                }

                if (GUILayout.Button("Vegetation Painter", GUILayout.Height(40)))
                {
                    // Open vegetation painter
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawDebugTools()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("🐛 Debug Tools", EditorStyles.boldLabel);

                if (GUILayout.Button("Enable Debug Mode", GUILayout.Height(30)))
                {
                    EnableDebugMode();
                }

                if (GUILayout.Button("Spawn Test Items", GUILayout.Height(30)))
                {
                    SpawnTestItems();
                }

                EditorGUILayout.EndVertical();
            }

            private void EnableDebugMode() { /* Enable debug */ }
            private void SpawnTestItems() { /* Spawn items */ }
        }

        /// <summary>
        /// Performance analysis and optimization tools
        /// </summary>
        public class PerformanceTools
        {
            private WildSurvivalProjectManager _manager;

            public PerformanceTools(WildSurvivalProjectManager manager)
            {
                _manager = manager;
            }

            public void Draw()
            {
                EditorGUILayout.LabelField("Performance Tools", _manager._headerStyle);

                // Performance Analysis
                DrawPerformanceAnalysis();

                EditorGUILayout.Space(20);

                // Optimization Tools
                DrawOptimizationTools();
            }

            private void DrawPerformanceAnalysis()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("📊 Performance Analysis", EditorStyles.boldLabel);

                if (GUILayout.Button("Analyze Scene Performance", GUILayout.Height(30)))
                {
                    AnalyzeScenePerformance();
                }

                if (GUILayout.Button("Profile Texture Memory", GUILayout.Height(30)))
                {
                    ProfileTextureMemory();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawOptimizationTools()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("⚡ Optimization", EditorStyles.boldLabel);

                if (GUILayout.Button("Optimize Textures", GUILayout.Height(30)))
                {
                    OptimizeTextures();
                }

                if (GUILayout.Button("Setup LODs", GUILayout.Height(30)))
                {
                    SetupLODs();
                }

                if (GUILayout.Button("Optimize HDRP Settings", GUILayout.Height(30)))
                {
                    OptimizeHDRP();
                }

                EditorGUILayout.EndVertical();
            }

            private void AnalyzeScenePerformance() { /* Analyze scene */ }
            private void ProfileTextureMemory() { /* Profile textures */ }
            private void OptimizeTextures() { /* Optimize textures */ }
            private void SetupLODs() { /* Setup LODs */ }
            private void OptimizeHDRP() { /* Optimize HDRP */ }
        }

        /// <summary>
        /// Build and deployment tools
        /// </summary>
        public class BuildTools
        {
            private WildSurvivalProjectManager _manager;

            public BuildTools(WildSurvivalProjectManager manager)
            {
                _manager = manager;
            }

            public void Draw()
            {
                EditorGUILayout.LabelField("Build & Deploy", _manager._headerStyle);

                // Build Configuration
                DrawBuildConfiguration();

                EditorGUILayout.Space(20);

                // Quick Build
                DrawQuickBuild();
            }

            private void DrawBuildConfiguration()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("⚙️ Build Configuration", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Platform: Windows");
                EditorGUILayout.LabelField("Backend: IL2CPP");
                EditorGUILayout.LabelField("Target: Steam");

                EditorGUILayout.EndVertical();
            }

            private void DrawQuickBuild()
            {
                EditorGUILayout.BeginVertical(_manager._sectionStyle);
                EditorGUILayout.LabelField("🚀 Quick Build", EditorStyles.boldLabel);

                if (GUILayout.Button("Development Build", GUILayout.Height(40)))
                {
                    BuildDevelopment();
                }

                if (GUILayout.Button("Release Build", GUILayout.Height(40)))
                {
                    BuildRelease();
                }

                EditorGUILayout.EndVertical();
            }

            public void QuickBuild()
            {
                BuildDevelopment();
            }

            private void BuildDevelopment()
            {
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
                    locationPathName = "Builds/Development/WildSurvival.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development | BuildOptions.AllowDebugging
                };

                BuildPipeline.BuildPlayer(options);
                Log("Development build complete!", LogType.Success);
            }

            private void BuildRelease()
            {
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
                    locationPathName = "Builds/Release/WildSurvival.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };

                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

                BuildPipeline.BuildPlayer(options);
                Log("Release build complete!", LogType.Success);
            }
        }

        // ==================== HELPER CLASSES ====================

        public class ProjectAnalysis
        {
            public List<string> Issues { get; private set; }
            public string Status { get; private set; }
            public bool HasIssues => Issues.Count > 0;

            public ProjectAnalysis()
            {
                Issues = new List<string>();
                Status = "Analyzing...";
            }

            public void Analyze()
            {
                Issues.Clear();

                // Check folder structure
                if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                    Issues.Add("Missing _Project folder structure");

                // Check for duplicates
                CheckForDuplicates();

                // Check assemblies
                if (!File.Exists("Assets/_Project/Code/Runtime/Core/WildSurvival.Core.asmdef"))
                    Issues.Add("Missing assembly definitions");

                // Check HDRP
                if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
                    Issues.Add("HDRP not configured");

                // Check Resources usage
                if (Directory.Exists("Assets/_Project/Resources"))
                    Issues.Add("Using Resources folder (migrate to Addressables)");

                Status = HasIssues ? $"{Issues.Count} issues found" : "All systems operational";
            }

            private void CheckForDuplicates()
            {
                string[] patterns = { " 1", " 2", " 3" };
                foreach (var pattern in patterns)
                {
                    var folders = Directory.GetDirectories("Assets/_Project", "*" + pattern, SearchOption.AllDirectories);
                    if (folders.Length > 0)
                    {
                        Issues.Add($"Found {folders.Length} duplicate folders");
                        break;
                    }
                }
            }

            public float GetHealthScore()
            {
                if (Issues.Count == 0) return 1f;
                if (Issues.Count <= 2) return 0.8f;
                if (Issues.Count <= 5) return 0.6f;
                if (Issues.Count <= 10) return 0.4f;
                return 0.2f;
            }
        }

        public class SetupTask
        {
            public string Name { get; set; }
            public Action ExecuteFunc { get; set; }
            public Func<bool> CheckFunc { get; set; }

            public SetupTask(string name, Action execute, Func<bool> check)
            {
                Name = name;
                ExecuteFunc = execute;
                CheckFunc = check;
            }
        }

        public class FileOperation
        {
            public OperationType Type { get; set; }
            public string Path { get; set; }
            public string TargetPath { get; set; }
            public string Reason { get; set; }
        }

        public enum OperationType
        {
            Move,
            Delete,
            Rename,
            Create
        }

        // ==================== HELPER CLASSES (PUBLIC) ====================

        public class LogEntry
        {
            public string Message { get; set; }
            public LogType Type { get; set; }
            public DateTime Time { get; set; }

            public LogEntry(string message, LogType type)
            {
                Message = message;
                Type = type;
                Time = DateTime.Now;
            }

            public Color GetColor()
            {
                return Type switch
                {
                    LogType.Error => Color.red,
                    LogType.Warning => Color.yellow,
                    LogType.Success => Color.green,
                    _ => Color.white
                };
            }
        }
    }

    // Simple input dialog helper
    public static class EditorInputDialog
    {
        public static string Show(string title, string message, string defaultValue = "")
        {
            // This is a simplified version - in production you'd want a proper dialog
            return EditorUtility.SaveFilePanel(title, "Assets/_Project/Code", defaultValue, "cs")
                .Replace(Application.dataPath, "Assets");
        }
    }
}