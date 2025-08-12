using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.Tools
{
    public class WildSurvivalFileManager : EditorWindow
    {
        // ────────────────────────────── State ──────────────────────────────
        // Primary paths
        private string sourcePath = "";
        private string destinationPath = "";

        // UI toggles
        private bool showQuickPaths = true;
        private bool showOperations = true;
        private bool showClipboard = true;
        private bool showHistory = true;
        private bool showBatchCreate = false;

        // Options
        private bool autoRefresh = true;

        // Layout
        private Vector2 scrollPosition;
        private Vector2 historyScrollPosition;
        private Vector2 quickPathsScrollPosition;

        // Status
        private string statusMessage = "";
        private MessageType statusType = MessageType.Info;

        // Clipboard
        private string clipboardPath = "";

        // History
        private List<string> pathHistory = new List<string>();
        private const int MAX_HISTORY = 20;

        // Quick paths
        [Serializable]
        private class QuickPath
        {
            public string name;
            public string path;
            public Color color = Color.white;
            public QuickPath(string n, string p) { name = n; path = p; }
        }
        private List<QuickPath> quickPaths = new List<QuickPath>();

        // Search
        private string searchFilter = "";
        private readonly List<string> searchResults = new List<string>();

        // Batch creation
        private string batchCreatePath = "";
        private string batchScriptNames = "";
        private string batchFolderNames = "";

        // Template system
        private enum ScriptTemplate { EmptyClass, MonoBehaviour, ScriptableObject, Manager, System, UI }
        private ScriptTemplate selectedTemplate = ScriptTemplate.EmptyClass;
        private bool useNamespace = true;
        private string defaultNamespace = "WildSurvival";

        // File status tracking
        private readonly Dictionary<string, FileStatus> fileStatuses = new Dictionary<string, FileStatus>();
        private class FileStatus
        {
            public bool IsPlaceholder { get; set; }
            public long FileSize { get; set; }
            public string Path { get; set; }
            public DateTime LastModified { get; set; }
        }

        // ───────────────────────────── Menu ────────────────────────────────
        [MenuItem("Tools/Wild Survival/🗂️ File Manager", priority = 50)]
        public static void ShowWindow()
        {
            var window = GetWindow<WildSurvivalFileManager>("WS File Manager");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadQuickPaths();
            LoadHistory();
        }

        private void OnGUI()
        {
            DrawHeader();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawClipboardSection();
            DrawPathInputSection();
            DrawQuickPathSection();
            DrawOperationsSection();
            DrawBatchCreationSection();
            DrawTemplateSystem();
            DrawFileStatusChecker();
            DrawHistorySection();
            DrawStatusSection();

            EditorGUILayout.EndScrollView();
        }

        // ───────────────────────────── UI ──────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🗂️ WILD SURVIVAL FILE MANAGER", headerStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Paste paths directly to quickly manage files and folders.\n" +
                "Example: Assets/_WildSurvival/Code/Runtime/Player\n" +
                "Tip: Press Ctrl+Shift+C in Project view to copy asset path!",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Quick phase jump buttons → set Source path
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Phase 1: Player", GUILayout.Height(25)))
            {
                SetSourcePath("Assets/_WildSurvival/Code/Runtime/Player/");
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("Phase 2: Inventory", GUILayout.Height(25)))
            {
                SetSourcePath("Assets/_WildSurvival/Code/Runtime/Survival/Inventory/");
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("Phase 3: Journal", GUILayout.Height(25)))
            {
                SetSourcePath("Assets/_WildSurvival/Code/Runtime/UI/Journal/");
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        private void DrawClipboardSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showClipboard = EditorGUILayout.Foldout(showClipboard, "📋 Clipboard Helper", true);
            if (showClipboard)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("📥 Paste as Source", GUILayout.Height(25)))
                {
                    clipboardPath = CleanPath(EditorGUIUtility.systemCopyBuffer);
                    if (IsValidPath(clipboardPath))
                    {
                        sourcePath = clipboardPath;
                        AddToHistory(clipboardPath);
                        ShowStatus($"Source set from clipboard: {clipboardPath}", MessageType.Info);
                    }
                    else
                    {
                        ShowStatus($"Invalid path in clipboard: {clipboardPath}", MessageType.Warning);
                    }
                }

                if (GUILayout.Button("📥 Paste as Destination", GUILayout.Height(25)))
                {
                    clipboardPath = CleanPath(EditorGUIUtility.systemCopyBuffer);
                    if (IsValidPath(clipboardPath))
                    {
                        destinationPath = clipboardPath;
                        ShowStatus($"Destination set from clipboard: {clipboardPath}", MessageType.Info);
                    }
                    else
                    {
                        ShowStatus($"Invalid path in clipboard: {clipboardPath}", MessageType.Warning);
                    }
                }

                if (GUILayout.Button("📤 Copy Source", GUILayout.Height(25)))
                {
                    if (!string.IsNullOrEmpty(sourcePath))
                    {
                        EditorGUIUtility.systemCopyBuffer = sourcePath;
                        ShowStatus($"Copied to clipboard: {sourcePath}", MessageType.Info);
                    }
                }

                if (GUILayout.Button("📤 Copy Dest", GUILayout.Height(25)))
                {
                    if (!string.IsNullOrEmpty(destinationPath))
                    {
                        EditorGUIUtility.systemCopyBuffer = destinationPath;
                        ShowStatus($"Copied to clipboard: {destinationPath}", MessageType.Info);
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(clipboardPath))
                {
                    EditorGUILayout.LabelField("Last clipboard:", clipboardPath, EditorStyles.miniLabel);
                }

                // Smart actions
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Smart Actions:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("📋→📁 Paste & Open"))
                {
                    clipboardPath = CleanPath(EditorGUIUtility.systemCopyBuffer);
                    if (IsValidPath(clipboardPath)) { sourcePath = clipboardPath; OpenInExplorer(sourcePath); }
                }
                if (GUILayout.Button("📋→🎯 Paste & Select"))
                {
                    clipboardPath = CleanPath(EditorGUIUtility.systemCopyBuffer);
                    if (IsValidPath(clipboardPath)) { sourcePath = clipboardPath; SelectInProject(sourcePath); }
                }
                if (GUILayout.Button("📋→💻 Paste & Code"))
                {
                    clipboardPath = CleanPath(EditorGUIUtility.systemCopyBuffer);
                    if (IsValidPath(clipboardPath)) { sourcePath = clipboardPath; OpenInCode(sourcePath); }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPathInputSection()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📁 Path Management", EditorStyles.boldLabel);

            // Source
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source:", GUILayout.Width(80));
            var prevSource = sourcePath;
            sourcePath = EditorGUILayout.TextField(sourcePath);
            if (sourcePath != prevSource && !string.IsNullOrEmpty(sourcePath)) AddToHistory(sourcePath);

            bool sourceExists = AssetExists(sourcePath);
            GUI.color = sourceExists ? Color.green : Color.red;
            EditorGUILayout.LabelField(sourceExists ? "✓" : "✗", GUILayout.Width(20));
            GUI.color = Color.white;

            if (GUILayout.Button("📂", GUILayout.Width(25)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Source Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    sourcePath = ConvertToAssetPath(selected);
                    AddToHistory(sourcePath);
                }
            }
            if (GUILayout.Button("🎯", GUILayout.Width(25))) SelectInProject(sourcePath);
            if (GUILayout.Button("🔍", GUILayout.Width(25))) ShowPathInfo(sourcePath);
            EditorGUILayout.EndHorizontal();

            // Destination
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Destination:", GUILayout.Width(80));
            destinationPath = EditorGUILayout.TextField(destinationPath);

            bool destExists = AssetExists(destinationPath);
            GUI.color = destExists ? Color.green : Color.red;
            EditorGUILayout.LabelField(destExists ? "✓" : "✗", GUILayout.Width(20));
            GUI.color = Color.white;

            if (GUILayout.Button("📂", GUILayout.Width(25)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Destination Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected)) destinationPath = ConvertToAssetPath(selected);
            }
            if (GUILayout.Button("🎯", GUILayout.Width(25))) SelectInProject(destinationPath);
            if (GUILayout.Button("🔍", GUILayout.Width(25))) ShowPathInfo(destinationPath);
            EditorGUILayout.EndHorizontal();

            // Quick ops
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("⇅ Swap", GUILayout.Width(60))) (sourcePath, destinationPath) = (destinationPath, sourcePath);
            if (GUILayout.Button("Clear", GUILayout.Width(60))) { sourcePath = ""; destinationPath = ""; }
            if (GUILayout.Button("→ Parent", GUILayout.Width(80)))
            {
                if (!string.IsNullOrEmpty(sourcePath))
                {
                    var p = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
                    sourcePath = string.IsNullOrEmpty(p) ? "Assets" : p;
                    AddToHistory(sourcePath);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawQuickPathSection()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showQuickPaths = EditorGUILayout.Foldout(showQuickPaths, "⚡ Quick Paths", true);
            if (showQuickPaths)
            {
                // Phase buttons
                EditorGUILayout.LabelField("Migration Phases:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f);
                if (GUILayout.Button("Phase 1: Player")) SetSourcePath("Assets/_WildSurvival/Code/Runtime/Player");

                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.8f);
                if (GUILayout.Button("Phase 2: Inventory")) SetSourcePath("Assets/_WildSurvival/Code/Runtime/Survival/Inventory");

                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.5f);
                if (GUILayout.Button("Phase 3: UI")) SetSourcePath("Assets/_WildSurvival/Code/Runtime/UI");

                GUI.backgroundColor = new Color(0.8f, 0.5f, 0.8f);
                if (GUILayout.Button("Phase 4: Survival")) SetSourcePath("Assets/_WildSurvival/Code/Runtime/Survival");

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.8f);
                if (GUILayout.Button("Phase 5: Crafting")) SetSourcePath("Assets/_WildSurvival/Code/Runtime/Survival/Crafting");

                GUI.backgroundColor = new Color(0.9f, 0.6f, 0.4f);
                if (GUILayout.Button("Phase 6: Environment")) SetSourcePath("Assets/_WildSurvival/Code/Runtime/Environment");

                GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
                if (GUILayout.Button("Phase 7: Save")) SetSourcePath("Assets/_WildSurvival/Code/Runtime/SaveSystem");

                GUI.backgroundColor = new Color(0.6f, 0.9f, 0.4f);
                if (GUILayout.Button("Phase 8: Audio")) SetSourcePath("Assets/_WildSurvival/Code/Runtime/Audio");
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Common locations
                EditorGUILayout.LabelField("Common Locations:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Core")) SetSourcePath("Assets/_WildSurvival/Code/Runtime/Core");
                if (GUILayout.Button("Prefabs")) SetSourcePath("Assets/_WildSurvival/Prefabs");
                if (GUILayout.Button("Scenes")) SetSourcePath("Assets/_WildSurvival/Scenes");
                if (GUILayout.Button("Data")) SetSourcePath("Assets/_WildSurvival/Data");
                if (GUILayout.Button("Editor")) SetSourcePath("Assets/WildSurvival/Editor");
                EditorGUILayout.EndHorizontal();

                // Saved paths
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Saved Paths:", EditorStyles.miniLabel);

                quickPathsScrollPosition = EditorGUILayout.BeginScrollView(quickPathsScrollPosition, GUILayout.MaxHeight(100));
                for (int i = 0; i < quickPaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUI.backgroundColor = quickPaths[i].color;
                    if (GUILayout.Button(quickPaths[i].name, GUILayout.Width(120))) SetSourcePath(quickPaths[i].path);
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.LabelField(quickPaths[i].path, EditorStyles.miniLabel);
                    quickPaths[i].color = EditorGUILayout.ColorField(quickPaths[i].color, GUILayout.Width(50));

                    if (GUILayout.Button("❌", GUILayout.Width(25)))
                    {
                        quickPaths.RemoveAt(i);
                        SaveQuickPaths();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("💾 Save Current Source as Quick Path"))
                {
                    if (!string.IsNullOrEmpty(sourcePath) && AssetExists(sourcePath))
                    {
                        string name = Path.GetFileName(sourcePath);
                        if (string.IsNullOrEmpty(name)) name = "Root";
                        var newPath = new QuickPath(name, sourcePath)
                        {
                            color = UnityEngine.Random.ColorHSV(0.3f, 0.8f, 0.5f, 0.9f, 0.8f, 1f)
                        };
                        quickPaths.Add(newPath);
                        SaveQuickPaths();
                        ShowStatus($"Saved quick path: {name}", MessageType.Info);
                    }
                    else
                    {
                        ShowStatus("Invalid or empty source path", MessageType.Warning);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawOperationsSection()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showOperations = EditorGUILayout.Foldout(showOperations, "🔧 Operations", true);
            if (showOperations)
            {
                EditorGUILayout.LabelField("File Operations:", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f);
                if (GUILayout.Button("📁 Open Explorer", GUILayout.Height(30))) OpenInExplorer(sourcePath);
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.8f);
                if (GUILayout.Button("💻 Open Code", GUILayout.Height(30))) OpenInCode(sourcePath);
                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.5f);
                if (GUILayout.Button("🎯 Select", GUILayout.Height(30))) SelectInProject(sourcePath);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.8f);
                if (GUILayout.Button("✂️ Move to Dest", GUILayout.Height(30))) MoveAsset(sourcePath, destinationPath);
                GUI.backgroundColor = new Color(0.8f, 0.5f, 0.8f);
                if (GUILayout.Button("📋 Copy to Dest", GUILayout.Height(30))) CopyAsset(sourcePath, destinationPath);
                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.5f);
                if (GUILayout.Button("✏️ Rename", GUILayout.Height(30))) RenameAsset(sourcePath);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.7f, 0.9f, 0.7f);
                if (GUILayout.Button("📁 Create Subfolder", GUILayout.Height(30))) CreateFolder(sourcePath);
                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.7f);
                if (GUILayout.Button("📄 Duplicate", GUILayout.Height(30))) DuplicateAsset(sourcePath);
                GUI.backgroundColor = new Color(0.7f, 0.7f, 0.9f);
                if (GUILayout.Button("🔄 Refresh", GUILayout.Height(30))) { AssetDatabase.Refresh(); ShowStatus("Asset database refreshed", MessageType.Info); }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                // Dangerous operations
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
                if (GUILayout.Button("🗑️ Delete Source", GUILayout.Height(30))) DeleteAsset(sourcePath);
                GUI.backgroundColor = new Color(0.9f, 0.7f, 0.5f);
                if (GUILayout.Button("🧹 Clean Empty Folders", GUILayout.Height(30))) CleanEmptyFolders();
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                // Search
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Search & Find:", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
                if (GUILayout.Button("🔍", GUILayout.Width(30))) SearchInPath(searchFilter, sourcePath);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Find Scripts", GUILayout.Height(25))) FindAllOfType("t:Script", sourcePath);
                if (GUILayout.Button("Find Prefabs", GUILayout.Height(25))) FindAllOfType("t:Prefab", sourcePath);
                if (GUILayout.Button("Find Scenes", GUILayout.Height(25))) FindAllOfType("t:Scene", sourcePath);
                if (GUILayout.Button("Find ScriptableObjects", GUILayout.Height(25))) FindAllOfType("t:ScriptableObject", sourcePath);
                EditorGUILayout.EndHorizontal();

                // Placeholder check for selected script
                if (!string.IsNullOrEmpty(sourcePath) && AssetDatabase.GetMainAssetTypeAtPath(sourcePath) == typeof(MonoScript))
                {
                    var full = ToFullPath(sourcePath);
                    if (File.Exists(full))
                    {
                        var fi = new FileInfo(full);
                        if (fi.Length < 500)
                        {
                            EditorGUILayout.HelpBox($"⚠️ The selected script looks like a placeholder ({fi.Length} bytes).", MessageType.Warning);
                            if (GUILayout.Button("Replace with Template")) ReplaceWithTemplate(full);
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawBatchCreationSection()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showBatchCreate = EditorGUILayout.Foldout(showBatchCreate, "🚀 Batch Creation", true);
            if (showBatchCreate)
            {
                EditorGUILayout.LabelField("Batch Create Scripts & Folders:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Base Path:", GUILayout.Width(80));
                batchCreatePath = EditorGUILayout.TextField(batchCreatePath);
                if (GUILayout.Button("Use Source", GUILayout.Width(80))) batchCreatePath = sourcePath;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Folders (one per line):", EditorStyles.miniLabel);
                batchFolderNames = EditorGUILayout.TextArea(batchFolderNames, GUILayout.Height(60));

                EditorGUILayout.LabelField("Scripts (one per line, .cs optional):", EditorStyles.miniLabel);
                batchScriptNames = EditorGUILayout.TextArea(batchScriptNames, GUILayout.Height(60));

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f);
                if (GUILayout.Button("📁 Create All Folders", GUILayout.Height(30))) CreateBatchFoldersUI();
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.8f);
                if (GUILayout.Button("📄 Create All Scripts", GUILayout.Height(30))) CreateBatchScriptsUI();
                GUI.backgroundColor = new Color(0.8f, 0.5f, 0.8f);
                if (GUILayout.Button("⚡ Create Both", GUILayout.Height(30))) { CreateBatchFoldersUI(); CreateBatchScriptsUI(); }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Quick Templates:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Phase 2 Structure", GUILayout.Height(25)))
                {
                    batchFolderNames = "Core\nUI\nItems\nData\nEvents";
                    batchScriptNames = "InventoryManager\nInventorySlot\nItemStack\nInventoryUI\nInventorySlotUI\nItemDragHandler\nItemData\nItemDatabase\nItemType\nInventoryEvents";
                }

                if (GUILayout.Button("Clear", GUILayout.Height(25)))
                {
                    batchFolderNames = "";
                    batchScriptNames = "";
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTemplateSystem()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📝 Template System", EditorStyles.boldLabel);

            selectedTemplate = (ScriptTemplate)EditorGUILayout.EnumPopup("Template:", selectedTemplate);
            useNamespace = EditorGUILayout.Toggle("Use Namespace:", useNamespace);
            if (useNamespace) defaultNamespace = EditorGUILayout.TextField("Namespace:", defaultNamespace);

            EditorGUILayout.HelpBox(GetTemplateDescription(selectedTemplate), MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawFileStatusChecker()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📊 File Status Checker", EditorStyles.boldLabel);

            if (GUILayout.Button("Check Phase 2 Files", GUILayout.Height(25))) CheckPhase2Status();

            if (fileStatuses.Count > 0)
            {
                EditorGUILayout.Space(5);
                foreach (var kv in fileStatuses.OrderBy(k => k.Key))
                {
                    var status = kv.Value;
                    EditorGUILayout.BeginHorizontal();
                    string icon = status.IsPlaceholder ? "⏳" : "✅";
                    EditorGUILayout.LabelField(icon, GUILayout.Width(20));

                    string fileName = Path.GetFileName(kv.Key);
                    EditorGUILayout.LabelField(fileName, GUILayout.Width(200));

                    string sizeText = status.IsPlaceholder ? $"Placeholder ({status.FileSize}B)" : $"{status.FileSize / 1024f:F1} KB";
                    EditorGUILayout.LabelField(sizeText, GUILayout.Width(140));

                    EditorGUILayout.LabelField(status.LastModified.ToString("yyyy-MM-dd HH:mm"), GUILayout.Width(160));

                    if (status.IsPlaceholder)
                    {
                        if (GUILayout.Button("Implement", GUILayout.Width(90)))
                        {
                            sourcePath = status.Path;
                            OpenInCode(sourcePath);
                        }
                    }
                    if (GUILayout.Button("Reveal", GUILayout.Width(70)))
                    {
                        OpenInExplorer(status.Path);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(5);
                int implemented = fileStatuses.Count(s => !s.Value.IsPlaceholder);
                int total = fileStatuses.Count;
                float progress = total > 0 ? (float)implemented / total : 0f;

                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                EditorGUI.ProgressBar(rect, progress, $"Progress: {implemented}/{total} files implemented");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHistorySection()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showHistory = EditorGUILayout.Foldout(showHistory, "📜 Path History", true);
            if (showHistory && pathHistory.Count > 0)
            {
                historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition, GUILayout.MaxHeight(120));
                for (int i = pathHistory.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button($"{pathHistory.Count - i}.", GUILayout.Width(25)))
                    {
                        sourcePath = pathHistory[i];
                        SelectInProject(sourcePath);
                    }

                    EditorGUILayout.LabelField(pathHistory[i], EditorStyles.miniLabel);

                    if (GUILayout.Button("→S", GUILayout.Width(28))) sourcePath = pathHistory[i];
                    if (GUILayout.Button("→D", GUILayout.Width(28))) destinationPath = pathHistory[i];

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Clear History"))
                {
                    pathHistory.Clear();
                    SaveHistory();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusSection()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }

        // ────────────────────────── Helpers & Ops ──────────────────────────
        private string CleanPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            path = path.Trim('"', ' ', '\n', '\r', '\t');
            path = path.Replace('\\', '/');

            if (path.StartsWith("file:///")) path = path.Substring(8);

            if (Path.IsPathRooted(path))
            {
                string dataPath = Application.dataPath.Replace('\\', '/');
                if (path.Contains(dataPath))
                    path = "Assets" + path.Substring(dataPath.Length);
                else if (path.Contains("/Assets/"))
                    path = path.Substring(path.IndexOf("Assets", StringComparison.Ordinal));
            }

            if (!path.StartsWith("Assets") && !path.StartsWith("Packages") && !Path.IsPathRooted(path))
                path = "Assets/" + path;

            return path;
        }

        private bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.StartsWith("Assets") || path.StartsWith("Packages") || Path.IsPathRooted(path);
        }

        private bool AssetExists(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null || AssetDatabase.IsValidFolder(path);
        }

        private string ConvertToAssetPath(string fullPath)
        {
            fullPath = fullPath.Replace('\\', '/');
            if (fullPath.Contains("/Assets/") || fullPath.EndsWith("/Assets"))
            {
                int index = fullPath.IndexOf("Assets", StringComparison.Ordinal);
                return fullPath.Substring(index).Replace('\\', '/');
            }
            return fullPath;
        }

        private string ToFullPath(string assetOrFullPath)
        {
            if (string.IsNullOrEmpty(assetOrFullPath)) return assetOrFullPath ?? "";
            if (Path.IsPathRooted(assetOrFullPath)) return Path.GetFullPath(assetOrFullPath);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            return Path.GetFullPath(Path.Combine(projectRoot, assetOrFullPath)).Replace('\\', '/');
        }

        private void SetSourcePath(string path)
        {
            sourcePath = path;
            AddToHistory(path);
            SelectInProject(path);
        }

        private void AddToHistory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            pathHistory.Remove(path);
            pathHistory.Add(path);
            while (pathHistory.Count > MAX_HISTORY) pathHistory.RemoveAt(0);
            SaveHistory();
        }

        private void OpenInExplorer(string path)
        {
            if (string.IsNullOrEmpty(path)) { ShowStatus("No path specified", MessageType.Warning); return; }
            string fullPath = ToFullPath(path);
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
                ShowStatus($"Opened in Explorer: {path}", MessageType.Info);
            }
            else ShowStatus($"Path not found: {path}", MessageType.Error);
        }

        private void OpenInCode(string path)
        {
            if (string.IsNullOrEmpty(path)) { ShowStatus("No path specified", MessageType.Warning); return; }
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null) { AssetDatabase.OpenAsset(asset); ShowStatus($"Opened in editor: {path}", MessageType.Info); }
            else ShowStatus($"Could not open: {path}", MessageType.Error);
        }

        private void SelectInProject(string path)
        {
            if (string.IsNullOrEmpty(path)) { ShowStatus("No path specified", MessageType.Warning); return; }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                ShowStatus($"Selected: {path}", MessageType.Info);
            }
            else if (AssetDatabase.IsValidFolder(path))
            {
                var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                if (folderAsset != null)
                {
                    Selection.activeObject = folderAsset;
                    EditorGUIUtility.PingObject(folderAsset);
                    ShowStatus($"Selected folder: {path}", MessageType.Info);
                }
            }
            else ShowStatus($"Asset not found: {path}", MessageType.Error);
        }

        private void ShowPathInfo(string path)
        {
            if (string.IsNullOrEmpty(path)) { ShowStatus("No path specified", MessageType.Warning); return; }

            string full = ToFullPath(path);
            string info = $"Path Info for: {path}\n";

            if (File.Exists(full))
            {
                FileInfo fi = new FileInfo(full);
                info += $"Type: File\nSize: {FormatFileSize(fi.Length)}\nCreated: {fi.CreationTime}\nModified: {fi.LastWriteTime}";
            }
            else if (Directory.Exists(full))
            {
                DirectoryInfo di = new DirectoryInfo(full);
                var files = di.GetFiles("*", SearchOption.AllDirectories);
                var dirs = di.GetDirectories("*", SearchOption.AllDirectories);
                long totalSize = files.Sum(f => f.Length);
                info += $"Type: Folder\nFiles: {files.Length}\nSubfolders: {dirs.Length}\nTotal Size: {FormatFileSize(totalSize)}";
            }
            else info += "Path does not exist";

            EditorUtility.DisplayDialog("Path Information", info, "OK");
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:0.##} {sizes[order]}";
        }

        private void MoveAsset(string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) { ShowStatus("Source and destination paths required", MessageType.Warning); return; }
            if (!AssetExists(from)) { ShowStatus($"Source not found: {from}", MessageType.Error); return; }

            if (AssetDatabase.IsValidFolder(to))
            {
                string fileName = Path.GetFileName(from);
                to = Path.Combine(to, fileName).Replace('\\', '/');
            }

            string error = AssetDatabase.MoveAsset(from, to);
            if (string.IsNullOrEmpty(error))
            {
                if (autoRefresh) AssetDatabase.Refresh();
                ShowStatus($"Moved: {from} → {to}", MessageType.Info);
                sourcePath = to;
                AddToHistory(to);
            }
            else ShowStatus($"Move failed: {error}", MessageType.Error);
        }

        private void CopyAsset(string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) { ShowStatus("Source and destination paths required", MessageType.Warning); return; }

            if (AssetDatabase.IsValidFolder(to))
            {
                string fileName = Path.GetFileName(from);
                to = Path.Combine(to, fileName).Replace('\\', '/');
            }

            if (AssetDatabase.CopyAsset(from, to))
            {
                if (autoRefresh) AssetDatabase.Refresh();
                ShowStatus($"Copied: {from} → {to}", MessageType.Info);
                AddToHistory(to);
            }
            else ShowStatus($"Copy failed: {from} → {to}", MessageType.Error);
        }

        private void DuplicateAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) { ShowStatus("No path specified", MessageType.Warning); return; }

            string newPath = AssetDatabase.GenerateUniqueAssetPath(path);
            if (AssetDatabase.CopyAsset(path, newPath))
            {
                if (autoRefresh) AssetDatabase.Refresh();
                ShowStatus($"Duplicated: {path} → {newPath}", MessageType.Info);
                sourcePath = newPath;
                AddToHistory(newPath);
                SelectInProject(newPath);
            }
            else ShowStatus($"Duplication failed: {path}", MessageType.Error);
        }

        private void RenameAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) { ShowStatus("No path specified", MessageType.Warning); return; }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorApplication.ExecuteMenuItem("Assets/Rename");
                ShowStatus($"Rename initiated for: {path}", MessageType.Info);
            }
            else ShowStatus($"Asset not found: {path}", MessageType.Error);
        }

        private void DeleteAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) { ShowStatus("No path specified", MessageType.Warning); return; }

            if (EditorUtility.DisplayDialog("Confirm Delete", $"Are you sure you want to delete:\n{path}?\n\nThis cannot be undone!", "Delete", "Cancel"))
            {
                if (AssetDatabase.DeleteAsset(path))
                {
                    if (autoRefresh) AssetDatabase.Refresh();
                    ShowStatus($"Deleted: {path}", MessageType.Info);
                    sourcePath = "";
                }
                else ShowStatus($"Delete failed: {path}", MessageType.Error);
            }
        }

        private void CreateFolder(string basePathForFolder)
        {
            string defaultName = "NewFolder";
            string input = EditorInputDialog.Show("Create Folder", "Enter folder name:", defaultName);
            if (string.IsNullOrEmpty(input)) return;

            string parentPath = string.IsNullOrEmpty(basePathForFolder) ? "Assets" : basePathForFolder;
            if (!AssetDatabase.IsValidFolder(parentPath))
                parentPath = Path.GetDirectoryName(parentPath)?.Replace('\\', '/') ?? "Assets";

            string guid = AssetDatabase.CreateFolder(parentPath, input);
            if (!string.IsNullOrEmpty(guid))
            {
                string newFolderPath = AssetDatabase.GUIDToAssetPath(guid);
                if (autoRefresh) AssetDatabase.Refresh();
                ShowStatus($"Created folder: {newFolderPath}", MessageType.Info);
                sourcePath = newFolderPath;
                AddToHistory(newFolderPath);
                SelectInProject(newFolderPath);
            }
            else ShowStatus("Failed to create folder", MessageType.Error);
        }

        private void CleanEmptyFolders()
        {
            if (string.IsNullOrEmpty(sourcePath)) { ShowStatus("No source path specified", MessageType.Warning); return; }
            if (!AssetDatabase.IsValidFolder(sourcePath)) { ShowStatus("Source must be a folder", MessageType.Warning); return; }

            int deleted = 0;
            string fullRoot = ToFullPath(sourcePath);
            var dirs = Directory.GetDirectories(fullRoot, "*", SearchOption.AllDirectories);
            Array.Sort(dirs, (a, b) => b.Length.CompareTo(a.Length)); // deepest first

            foreach (var dir in dirs)
            {
                var files = Directory.GetFiles(dir);
                bool hasNonMetaFiles = files.Any(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
                bool hasSubDirs = Directory.GetDirectories(dir).Length > 0;

                if (!hasNonMetaFiles && !hasSubDirs)
                {
                    string assetPath = ConvertToAssetPath(dir);
                    if (AssetDatabase.DeleteAsset(assetPath)) deleted++;
                }
            }

            if (deleted > 0) { AssetDatabase.Refresh(); ShowStatus($"Deleted {deleted} empty folders", MessageType.Info); }
            else ShowStatus("No empty folders found", MessageType.Info);
        }

        private void SearchInPath(string searchTerm, string inPath)
        {
            if (string.IsNullOrEmpty(searchTerm)) { ShowStatus("Enter a search term", MessageType.Warning); return; }
            string searchPath = string.IsNullOrEmpty(inPath) ? "Assets" : inPath;
            var guids = AssetDatabase.FindAssets(searchTerm, new[] { searchPath });

            searchResults.Clear();
            foreach (var guid in guids) searchResults.Add(AssetDatabase.GUIDToAssetPath(guid));

            if (searchResults.Count > 0)
            {
                Debug.Log($"Search results for '{searchTerm}' in {searchPath}:");
                foreach (var result in searchResults) Debug.Log($"  → {result}");
                ShowStatus($"Found {searchResults.Count} results (see Console)", MessageType.Info);
            }
            else ShowStatus($"No results for '{searchTerm}'", MessageType.Warning);
        }

        private void FindAllOfType(string filter, string inPath)
        {
            string searchPath = string.IsNullOrEmpty(inPath) ? "Assets" : inPath;
            var guids = AssetDatabase.FindAssets(filter, new[] { searchPath });

            if (guids.Length == 0) { ShowStatus($"No items found with filter: {filter}", MessageType.Warning); return; }

            Debug.Log($"Found {guids.Length} items in {searchPath}:");
            foreach (var guid in guids) Debug.Log($"  → {AssetDatabase.GUIDToAssetPath(guid)}");
            ShowStatus($"Found {guids.Length} items (see Console)", MessageType.Info);
        }

        private void ShowStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
            Debug.Log($"[WS File Manager] {message}");
            Repaint();
        }

        // ─────────────── Batch Creation (UI) ───────────────
        private void CreateBatchFoldersUI()
        {
            if (string.IsNullOrEmpty(batchCreatePath)) { ShowStatus("Please specify a base path", MessageType.Warning); return; }

            string[] folders = batchFolderNames.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            int created = 0;

            foreach (string folderName in folders)
            {
                string trimmed = folderName.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string currentParent = batchCreatePath.TrimEnd('/');
                string[] parts = trimmed.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string part in parts)
                {
                    string newPath = $"{currentParent}/{part}";
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentParent, part);
                        created++;
                    }
                    currentParent = newPath;
                }
            }

            if (created > 0) { AssetDatabase.Refresh(); ShowStatus($"Created {created} folders", MessageType.Info); }
            else ShowStatus("No folders created (they may already exist)", MessageType.Info);
        }

        private void CreateBatchScriptsUI()
        {
            if (string.IsNullOrEmpty(batchCreatePath)) { ShowStatus("Please specify a base path", MessageType.Warning); return; }

            string[] scripts = batchScriptNames.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            int created = 0;

            foreach (string scriptLine in scripts)
            {
                string trimmed = scriptLine.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (!trimmed.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) trimmed += ".cs";

                string scriptPath = $"{batchCreatePath}/{trimmed}".Replace('\\', '/');
                string folder = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/') ?? batchCreatePath;

                // Make sure folders exist (nested)
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = "";
                    foreach (var part in folder.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (part == "Assets" && string.IsNullOrEmpty(parent)) { parent = "Assets"; continue; }
                        parent = string.IsNullOrEmpty(parent) ? part : $"{parent}/{part}";
                        if (!AssetDatabase.IsValidFolder(parent))
                        {
                            string parentDir = Path.GetDirectoryName(parent)?.Replace('\\', '/') ?? "Assets";
                            string leaf = Path.GetFileName(parent);
                            if (AssetDatabase.IsValidFolder(parentDir))
                                AssetDatabase.CreateFolder(parentDir, leaf);
                        }
                    }
                }

                if (!File.Exists(ToFullPath(scriptPath)))
                {
                    string className = Path.GetFileNameWithoutExtension(trimmed);
                    string content = GenerateScriptContent(className, selectedTemplate);
                    File.WriteAllText(ToFullPath(scriptPath), content);
                    created++;
                }
            }

            if (created > 0) { AssetDatabase.Refresh(); ShowStatus($"Created {created} scripts", MessageType.Info); }
            else ShowStatus("No scripts created (they may already exist)", MessageType.Info);
        }

        // ─────────────── Templates ───────────────
        private void ReplaceWithTemplate(string filePathOrAsset)
        {
            string full = ToFullPath(filePathOrAsset);
            if (!File.Exists(full)) return;

            string className = Path.GetFileNameWithoutExtension(full);
            string content = GenerateScriptContent(className, selectedTemplate);
            File.WriteAllText(full, content);
            AssetDatabase.Refresh();

            ShowNotification(new GUIContent($"Replaced {className} with template"));
            OpenInCode(ConvertToAssetPath(full));
        }

        private string GenerateScriptContent(string className, ScriptTemplate template)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            if (template == ScriptTemplate.UI) sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            if (useNamespace)
            {
                sb.AppendLine($"namespace {defaultNamespace}");
                sb.AppendLine("{");
            }

            string indent = useNamespace ? "    " : "";

            switch (template)
            {
                case ScriptTemplate.MonoBehaviour:
                    sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
                    break;
                case ScriptTemplate.ScriptableObject:
                    sb.AppendLine($"{indent}[CreateAssetMenu(fileName = \"{className}\", menuName = \"Wild Survival/{className}\")]");
                    sb.AppendLine($"{indent}public class {className} : ScriptableObject");
                    break;
                default:
                    sb.AppendLine($"{indent}public class {className}");
                    break;
            }

            sb.AppendLine($"{indent}{{");

            switch (template)
            {
                case ScriptTemplate.Manager:
                    sb.AppendLine($"{indent}    private static {className} instance;");
                    sb.AppendLine($"{indent}    public static {className} Instance => instance;");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    private void Awake()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        if (instance != null && instance != this)");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            Destroy(gameObject);");
                    sb.AppendLine($"{indent}            return;");
                    sb.AppendLine($"{indent}        }}");
                    sb.AppendLine($"{indent}        instance = this;");
                    sb.AppendLine($"{indent}    }}");
                    break;

                case ScriptTemplate.MonoBehaviour:
                    sb.AppendLine($"{indent}    private void Awake()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        ");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    private void Start()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        ");
                    sb.AppendLine($"{indent}    }}");
                    break;

                default:
                    sb.AppendLine($"{indent}    // TODO: Implement {className}");
                    break;
            }

            sb.AppendLine($"{indent}}}");
            if (useNamespace) sb.AppendLine("}");

            return sb.ToString();
        }

        private string GetTemplateDescription(ScriptTemplate template)
        {
            switch (template)
            {
                case ScriptTemplate.EmptyClass: return "Basic C# class with minimal structure";
                case ScriptTemplate.MonoBehaviour: return "Unity MonoBehaviour with Awake/Start methods";
                case ScriptTemplate.ScriptableObject: return "ScriptableObject with CreateAssetMenu attribute";
                case ScriptTemplate.Manager: return "Singleton manager pattern";
                case ScriptTemplate.System: return "System class for game logic";
                case ScriptTemplate.UI: return "UI controller with Unity UI imports";
                default: return "";
            }
        }

        private void CheckPhase2Status()
        {
            fileStatuses.Clear();

            string[] phase2Files = {
                "Core/InventoryManager.cs",
                "Core/InventorySlot.cs",
                "Core/ItemStack.cs",
                "Items/ItemData.cs",
                "Items/ItemDatabase.cs",
                "Items/ItemType.cs",
                "UI/InventoryUI.cs",
                "UI/InventorySlotUI.cs",
                "UI/ItemDragHandler.cs",
                "Events/InventoryEvents.cs"
            };

            string inventoryPath = "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/";

            foreach (var file in phase2Files)
            {
                string assetPath = (inventoryPath + file).Replace('\\', '/');
                string fullPath = ToFullPath(assetPath);
                if (File.Exists(fullPath))
                {
                    var fi = new FileInfo(fullPath);
                    fileStatuses[file] = new FileStatus
                    {
                        IsPlaceholder = fi.Length < 500,
                        FileSize = fi.Length,
                        Path = assetPath,
                        LastModified = fi.LastWriteTime
                    };
                }
            }
        }

        // ─────────────── Persistence ───────────────
        private void SaveQuickPaths()
        {
            string json = JsonUtility.ToJson(new SerializableList<QuickPath>(quickPaths));
            EditorPrefs.SetString("WildSurvival_FileManager_QuickPaths", json);
        }

        private void LoadQuickPaths()
        {
            string json = EditorPrefs.GetString("WildSurvival_FileManager_QuickPaths", "");
            if (!string.IsNullOrEmpty(json))
            {
                var loaded = JsonUtility.FromJson<SerializableList<QuickPath>>(json);
                if (loaded != null && loaded.items != null) quickPaths = loaded.items;
            }
        }

        private void SaveHistory()
        {
            string json = JsonUtility.ToJson(new SerializableList<string>(pathHistory));
            EditorPrefs.SetString("WildSurvival_FileManager_History", json);
        }

        private void LoadHistory()
        {
            string json = EditorPrefs.GetString("WildSurvival_FileManager_History", "");
            if (!string.IsNullOrEmpty(json))
            {
                var loaded = JsonUtility.FromJson<SerializableList<string>>(json);
                if (loaded != null && loaded.items != null) pathHistory = loaded.items;
            }
        }

        [Serializable]
        private class SerializableList<T>
        {
            public List<T> items;
            public SerializableList(List<T> list) { items = list; }
        }
    }

    // ───────────────────────────── Input Dialog ───────────────────────────
    public class EditorInputDialog : EditorWindow
    {
        private static string inputValue = "";
        private static string promptText = "";
        private static bool confirmed = false;

        public static string Show(string title, string prompt, string defaultValue)
        {
            inputValue = defaultValue;
            promptText = prompt;
            confirmed = false;

            var window = GetWindow<EditorInputDialog>(true, title, true);
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(300, 100);
            window.ShowModalUtility(); // safer across Unity versions

            return confirmed ? inputValue : null;
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(promptText);

            GUI.SetNextControlName("InputField");
            inputValue = EditorGUILayout.TextField(inputValue);

            if (Event.current.type == EventType.Repaint)
                EditorGUI.FocusTextInControl("InputField");

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("OK") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                confirmed = true;
                Close();
            }

            if (GUILayout.Button("Cancel") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
            {
                confirmed = false;
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}


//using UnityEngine;
//using UnityEditor;
//using System.IO;
//using System.Collections.Generic;
//using System.Text;
//using System.Linq;
//using System;

//namespace WildSurvival.Editor.Tools
//{
//    public class WildSurvivalFileManager : EditorWindow
//    {
//        // Existing fields
//        private string sourcePath = "";
//        private string destinationPath = "";
//        private string renamePattern = "";
//        private bool showAdvanced = false;
//        private bool copyMetaFiles = true;
//        private Vector2 scrollPosition;

//        // Batch operation fields
//        private string basePath = "Assets/_WildSurvival/Code/Runtime/";
//        private string foldersToCreate = "";
//        private string scriptsToCreate = "";

//        // Template system
//        private enum ScriptTemplate
//        {
//            EmptyClass,
//            MonoBehaviour,
//            ScriptableObject,
//            Manager,
//            System,
//            UI
//        }

//        private ScriptTemplate selectedTemplate = ScriptTemplate.EmptyClass;
//        private bool useNamespace = true;
//        private string defaultNamespace = "WildSurvival";

//        // File status tracking
//        private Dictionary<string, FileStatus> fileStatuses = new Dictionary<string, FileStatus>();

//        private class FileStatus
//        {
//            public bool IsPlaceholder { get; set; }
//            public long FileSize { get; set; }
//            public string Path { get; set; }
//            public DateTime LastModified { get; set; }
//        }

//        [MenuItem("Tools/Wild Survival/🗂️ File Manager (Enhanced)")]
//        public static void ShowWindow()
//        {
//            var window = GetWindow<WildSurvivalFileManager>("WS File Manager");
//            window.minSize = new Vector2(500, 600);
//            window.Show();
//        }

//        // Path inputs
//        private string sourcePath = "";
//        private string destinationPath = "";

//        // Quick path buttons
//        private List<QuickPath> quickPaths = new List<QuickPath>();

//        // Operation options
//        private bool showQuickPaths = true;
//        private bool showOperations = true;
//        private bool showClipboard = true;
//        private bool showHistory = true;
//        private bool autoRefresh = true;

//        // Display
//        private Vector2 scrollPosition;
//        private Vector2 historyScrollPosition;
//        private string statusMessage = "";
//        private MessageType statusType = MessageType.Info;

//        // Batch creation
//        private bool showBatchCreate = false;
//        private string batchCreatePath = "";
//        private string batchScriptNames = "";
//        private string batchFolderNames = "";
//        private Vector2 batchScrollPosition;

//        // Clipboard
//        private string clipboardPath = "";

//        // History tracking
//        private List<string> pathHistory = new List<string>();
//        private const int MAX_HISTORY = 20;

//        // Search
//        private string searchFilter = "";
//        private List<string> searchResults = new List<string>();

//        [System.Serializable]
//        private class QuickPath
//        {
//            public string name;
//            public string path;
//            public Color color = Color.white;
//            public QuickPath(string n, string p) { name = n; path = p; }
//        }

//        [MenuItem("Tools/Wild Survival/🗂️ File Manager", priority = 50)]
//        public static void ShowWindow()
//        {
//            var window = GetWindow<WildSurvivalFileManager>("WS File Manager");
//            window.minSize = new Vector2(600, 500);
//            window.Show();
//        }

//        private void OnEnable()
//        {
//            LoadQuickPaths();
//            LoadHistory();
//        }



//        private void OnGUI()
//        {
//            DrawHeader();
//            DrawClipboardSection();
//            DrawPathInputSection();
//            DrawQuickPathSection();
//            DrawBatchCreationSection();
//            DrawOperationsSection();
//            DrawHistorySection();
//            DrawStatusSection();

//            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

//            DrawPathOperations();
//            EditorGUILayout.Space(10);

//            DrawBatchOperations();
//            EditorGUILayout.Space(10);

//            DrawTemplateSystem();
//            EditorGUILayout.Space(10);

//            DrawFileStatusChecker();

//            EditorGUILayout.EndScrollView();
//        }

//        private void DrawHeader()
//        {
//            EditorGUILayout.Space(5);
//            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
//            {
//                fontSize = 16,
//                fontStyle = FontStyle.Bold,
//                alignment = TextAnchor.MiddleCenter
//            };

//            EditorGUILayout.LabelField("🗂️ Wild Survival File Manager", titleStyle);
//            EditorGUILayout.Space(5);

//            // Quick phase jump buttons
//            EditorGUILayout.BeginHorizontal();
//            if (GUILayout.Button("Phase 1: Player", GUILayout.Height(25)))
//            {
//                basePath = "Assets/_WildSurvival/Code/Runtime/Player/";
//                GUI.FocusControl(null);
//            }
//            if (GUILayout.Button("Phase 2: Inventory", GUILayout.Height(25)))
//            {
//                basePath = "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/";
//                GUI.FocusControl(null);
//            }
//            if (GUILayout.Button("Phase 3: Journal", GUILayout.Height(25)))
//            {
//                basePath = "Assets/_WildSurvival/Code/Runtime/UI/Journal/";
//                GUI.FocusControl(null);
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.Space(5);
//        }

//        private void DrawPathOperations()
//        {
//            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };

//            EditorGUILayout.BeginVertical(boxStyle);
//            EditorGUILayout.LabelField("📁 Path Operations", EditorStyles.boldLabel);

//            // Source path
//            EditorGUILayout.BeginHorizontal();
//            sourcePath = EditorGUILayout.TextField("Source:", sourcePath);
//            if (GUILayout.Button("📋 Paste", GUILayout.Width(60)))
//            {
//                sourcePath = EditorGUIUtility.systemCopyBuffer;
//            }
//            EditorGUILayout.EndHorizontal();

//            // Quick operation buttons
//            EditorGUILayout.BeginHorizontal();
//            if (GUILayout.Button("📋→🎯 Paste & Select"))
//            {
//                sourcePath = EditorGUIUtility.systemCopyBuffer;
//                SelectInProject(sourcePath);
//            }
//            if (GUILayout.Button("📋→💻 Paste & Code"))
//            {
//                sourcePath = EditorGUIUtility.systemCopyBuffer;
//                OpenInCode(sourcePath);
//            }
//            if (GUILayout.Button("📋→📁 Paste & Open"))
//            {
//                sourcePath = EditorGUIUtility.systemCopyBuffer;
//                OpenInExplorer(sourcePath);
//            }
//            EditorGUILayout.EndHorizontal();

//            // Individual operations
//            EditorGUILayout.BeginHorizontal();
//            GUI.enabled = !string.IsNullOrEmpty(sourcePath);
//            if (GUILayout.Button("🎯 Select"))
//            {
//                SelectInProject(sourcePath);
//            }
//            if (GUILayout.Button("💻 Open Code"))
//            {
//                OpenInCode(sourcePath);
//            }
//            if (GUILayout.Button("📁 Open Folder"))
//            {
//                OpenInExplorer(sourcePath);
//            }
//            GUI.enabled = true;
//            EditorGUILayout.EndHorizontal();

//            // Check if file is placeholder
//            if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
//            {
//                var fileInfo = new FileInfo(sourcePath);
//                if (fileInfo.Length < 500)
//                {
//                    EditorGUILayout.HelpBox($"⚠️ This appears to be a placeholder file ({fileInfo.Length} bytes)", MessageType.Warning);
//                    if (GUILayout.Button("Replace with Template"))
//                    {
//                        ReplaceWithTemplate(sourcePath);
//                    }
//                }
//            }

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawBatchOperations()
//        {
//            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };

//            EditorGUILayout.BeginVertical(boxStyle);
//            EditorGUILayout.LabelField("⚡ Batch Operations", EditorStyles.boldLabel);

//            basePath = EditorGUILayout.TextField("Base Path:", basePath);

//            EditorGUILayout.LabelField("Folders (one per line):");
//            foldersToCreate = EditorGUILayout.TextArea(foldersToCreate, GUILayout.Height(60));

//            EditorGUILayout.LabelField("Scripts (one per line, include folder path):");
//            scriptsToCreate = EditorGUILayout.TextArea(scriptsToCreate, GUILayout.Height(80));

//            EditorGUILayout.BeginHorizontal();
//            if (GUILayout.Button("📁 Create Folders", GUILayout.Height(25)))
//            {
//                CreateBatchFolders();
//            }
//            if (GUILayout.Button("📄 Create Scripts", GUILayout.Height(25)))
//            {
//                CreateBatchScripts();
//            }
//            if (GUILayout.Button("⚡ Create Both", GUILayout.Height(25)))
//            {
//                CreateBatchFolders();
//                CreateBatchScripts();
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawTemplateSystem()
//        {
//            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };

//            EditorGUILayout.BeginVertical(boxStyle);
//            EditorGUILayout.LabelField("📝 Template System", EditorStyles.boldLabel);

//            selectedTemplate = (ScriptTemplate)EditorGUILayout.EnumPopup("Template:", selectedTemplate);
//            useNamespace = EditorGUILayout.Toggle("Use Namespace:", useNamespace);
//            if (useNamespace)
//            {
//                defaultNamespace = EditorGUILayout.TextField("Namespace:", defaultNamespace);
//            }

//            EditorGUILayout.HelpBox(GetTemplateDescription(selectedTemplate), MessageType.Info);

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawFileStatusChecker()
//        {
//            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };

//            EditorGUILayout.BeginVertical(boxStyle);
//            EditorGUILayout.LabelField("📊 File Status Checker", EditorStyles.boldLabel);

//            if (GUILayout.Button("Check Phase 2 Files", GUILayout.Height(25)))
//            {
//                CheckPhase2Status();
//            }

//            if (fileStatuses.Count > 0)
//            {
//                EditorGUILayout.Space(5);
//                foreach (var status in fileStatuses.OrderBy(s => s.Key))
//                {
//                    EditorGUILayout.BeginHorizontal();

//                    // Icon based on status
//                    string icon = status.Value.IsPlaceholder ? "⏳" : "✅";
//                    EditorGUILayout.LabelField(icon, GUILayout.Width(20));

//                    // File name
//                    string fileName = Path.GetFileName(status.Key);
//                    EditorGUILayout.LabelField(fileName, GUILayout.Width(150));

//                    // Size
//                    string sizeText = status.Value.IsPlaceholder ?
//                        $"Placeholder ({status.Value.FileSize}B)" :
//                        $"{status.Value.FileSize / 1024f:F1} KB";
//                    EditorGUILayout.LabelField(sizeText, GUILayout.Width(100));

//                    // Action button
//                    if (status.Value.IsPlaceholder)
//                    {
//                        if (GUILayout.Button("Implement", GUILayout.Width(80)))
//                        {
//                            sourcePath = status.Value.Path;
//                            OpenInCode(sourcePath);
//                        }
//                    }

//                    EditorGUILayout.EndHorizontal();
//                }

//                // Summary
//                EditorGUILayout.Space(5);
//                int implemented = fileStatuses.Count(s => !s.Value.IsPlaceholder);
//                int total = fileStatuses.Count;
//                float progress = (float)implemented / total;

//                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
//                EditorGUI.ProgressBar(rect, progress, $"Progress: {implemented}/{total} files implemented");
//            }

//            EditorGUILayout.EndVertical();
//        }

//        private void CheckPhase2Status()
//        {
//            fileStatuses.Clear();

//            string[] phase2Files = {
//                "Core/InventoryManager.cs",
//                "Core/InventorySlot.cs",
//                "Core/ItemStack.cs",
//                "Items/ItemData.cs",
//                "Items/ItemDatabase.cs",
//                "Items/ItemType.cs",
//                "UI/InventoryUI.cs",
//                "UI/InventorySlotUI.cs",
//                "UI/ItemDragHandler.cs",
//                "Events/InventoryEvents.cs"
//            };

//            string inventoryPath = "Assets/_WildSurvival/Code/Runtime/Survival/Inventory/";

//            foreach (var file in phase2Files)
//            {
//                string fullPath = Path.Combine(inventoryPath, file);
//                if (File.Exists(fullPath))
//                {
//                    var fileInfo = new FileInfo(fullPath);
//                    fileStatuses[file] = new FileStatus
//                    {
//                        IsPlaceholder = fileInfo.Length < 500,
//                        FileSize = fileInfo.Length,
//                        Path = fullPath,
//                        LastModified = fileInfo.LastWriteTime
//                    };
//                }
//            }
//        }

//        private void ReplaceWithTemplate(string filePath)
//        {
//            if (!File.Exists(filePath)) return;

//            string fileName = Path.GetFileNameWithoutExtension(filePath);
//            string content = GenerateScriptContent(fileName, selectedTemplate);

//            File.WriteAllText(filePath, content);
//            AssetDatabase.Refresh();

//            ShowNotification(new GUIContent($"Replaced {fileName} with template"));
//            OpenInCode(filePath);
//        }

//        private string GenerateScriptContent(string className, ScriptTemplate template)
//        {
//            StringBuilder sb = new StringBuilder();

//            // Usings
//            sb.AppendLine("using UnityEngine;");
//            if (template == ScriptTemplate.UI)
//                sb.AppendLine("using UnityEngine.UI;");
//            sb.AppendLine("using System;");
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine();

//            // Namespace
//            if (useNamespace)
//            {
//                sb.AppendLine($"namespace {defaultNamespace}");
//                sb.AppendLine("{");
//            }

//            string indent = useNamespace ? "    " : "";

//            // Class declaration
//            switch (template)
//            {
//                case ScriptTemplate.MonoBehaviour:
//                    sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
//                    break;
//                case ScriptTemplate.ScriptableObject:
//                    sb.AppendLine($"{indent}[CreateAssetMenu(fileName = \"{className}\", menuName = \"Wild Survival/{className}\")]");
//                    sb.AppendLine($"{indent}public class {className} : ScriptableObject");
//                    break;
//                default:
//                    sb.AppendLine($"{indent}public class {className}");
//                    break;
//            }

//            sb.AppendLine($"{indent}{{");

//            // Add template-specific content
//            switch (template)
//            {
//                case ScriptTemplate.Manager:
//                    sb.AppendLine($"{indent}    private static {className} instance;");
//                    sb.AppendLine($"{indent}    public static {className} Instance => instance;");
//                    sb.AppendLine();
//                    sb.AppendLine($"{indent}    private void Awake()");
//                    sb.AppendLine($"{indent}    {{");
//                    sb.AppendLine($"{indent}        if (instance != null && instance != this)");
//                    sb.AppendLine($"{indent}        {{");
//                    sb.AppendLine($"{indent}            Destroy(gameObject);");
//                    sb.AppendLine($"{indent}            return;");
//                    sb.AppendLine($"{indent}        }}");
//                    sb.AppendLine($"{indent}        instance = this;");
//                    sb.AppendLine($"{indent}    }}");
//                    break;

//                case ScriptTemplate.MonoBehaviour:
//                    sb.AppendLine($"{indent}    private void Awake()");
//                    sb.AppendLine($"{indent}    {{");
//                    sb.AppendLine($"{indent}        ");
//                    sb.AppendLine($"{indent}    }}");
//                    sb.AppendLine();
//                    sb.AppendLine($"{indent}    private void Start()");
//                    sb.AppendLine($"{indent}    {{");
//                    sb.AppendLine($"{indent}        ");
//                    sb.AppendLine($"{indent}    }}");
//                    break;

//                default:
//                    sb.AppendLine($"{indent}    // TODO: Implement {className}");
//                    break;
//            }

//            sb.AppendLine($"{indent}}}");

//            if (useNamespace)
//            {
//                sb.AppendLine("}");
//            }

//            return sb.ToString();
//        }

//        private string GetTemplateDescription(ScriptTemplate template)
//        {
//            return template switch
//            {
//                ScriptTemplate.EmptyClass => "Basic C# class with minimal structure",
//                ScriptTemplate.MonoBehaviour => "Unity MonoBehaviour with Awake/Start methods",
//                ScriptTemplate.ScriptableObject => "ScriptableObject with CreateAssetMenu attribute",
//                ScriptTemplate.Manager => "Singleton manager pattern",
//                ScriptTemplate.System => "System class for game logic",
//                ScriptTemplate.UI => "UI controller with Unity UI imports",
//                _ => ""
//            };
//        }

//        private void SelectInProject(string path)
//        {
//            if (string.IsNullOrEmpty(path)) return;

//            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
//            if (asset != null)
//            {
//                Selection.activeObject = asset;
//                EditorGUIUtility.PingObject(asset);
//            }
//        }

//        private void OpenInCode(string path)
//        {
//            if (string.IsNullOrEmpty(path)) return;

//            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
//            if (asset != null)
//            {
//                AssetDatabase.OpenAsset(asset);
//            }
//        }

//        private void OpenInExplorer(string path)
//        {
//            if (string.IsNullOrEmpty(path)) return;

//            string fullPath = Path.GetFullPath(path);
//            if (Directory.Exists(fullPath))
//            {
//                EditorUtility.RevealInFinder(fullPath);
//            }
//            else if (File.Exists(fullPath))
//            {
//                EditorUtility.RevealInFinder(fullPath);
//            }
//        }

//        private void CreateBatchFolders()
//        {
//            string[] folders = foldersToCreate.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var folder in folders)
//            {
//                string fullPath = Path.Combine(basePath, folder.Trim());
//                if (!Directory.Exists(fullPath))
//                {
//                    Directory.CreateDirectory(fullPath);
//                }
//            }
//            AssetDatabase.Refresh();
//            ShowNotification(new GUIContent($"Created {folders.Length} folders"));
//        }

//        private void CreateBatchScripts()
//        {
//            string[] scripts = scriptsToCreate.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var script in scripts)
//            {
//                string scriptName = script.Trim();
//                if (!scriptName.EndsWith(".cs"))
//                {
//                    scriptName += ".cs";
//                }

//                string fullPath = Path.Combine(basePath, scriptName);
//                string directory = Path.GetDirectoryName(fullPath);

//                if (!Directory.Exists(directory))
//                {
//                    Directory.CreateDirectory(directory);
//                }

//                if (!File.Exists(fullPath))
//                {
//                    string className = Path.GetFileNameWithoutExtension(scriptName);
//                    string content = GenerateScriptContent(className, selectedTemplate);
//                    File.WriteAllText(fullPath, content);
//                }
//            }
//            AssetDatabase.Refresh();
//            ShowNotification(new GUIContent($"Created {scripts.Length} scripts"));
//        }




//        private void DrawHeader()
//        {
//            EditorGUILayout.Space(5);
//            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
//            {
//                fontSize = 16,
//                alignment = TextAnchor.MiddleCenter
//            };

//            EditorGUILayout.LabelField("🗂️ WILD SURVIVAL FILE MANAGER", headerStyle);
//            EditorGUILayout.Space(5);

//            EditorGUILayout.HelpBox(
//                "Paste paths directly to quickly manage files and folders.\n" +
//                "Example: Assets/_WildSurvival/Code/Runtime/Player\n" +
//                "Tip: Press Ctrl+Shift+C in Project view to copy asset path!",
//                MessageType.Info);

//            EditorGUILayout.Space(5);
//        }

//        private void DrawClipboardSection()
//        {
//            if (!showClipboard) return;

//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            showClipboard = EditorGUILayout.Foldout(showClipboard, "📋 Clipboard Helper", true);

//            if (showClipboard)
//            {
//                EditorGUILayout.BeginHorizontal();

//                if (GUILayout.Button("📥 Paste as Source", GUILayout.Height(25)))
//                {
//                    clipboardPath = GUIUtility.systemCopyBuffer;
//                    clipboardPath = CleanPath(clipboardPath);

//                    if (IsValidPath(clipboardPath))
//                    {
//                        sourcePath = clipboardPath;
//                        AddToHistory(clipboardPath);
//                        ShowStatus($"Source set from clipboard: {clipboardPath}", MessageType.Info);
//                    }
//                    else
//                    {
//                        ShowStatus($"Invalid path in clipboard: {clipboardPath}", MessageType.Warning);
//                    }
//                }

//                if (GUILayout.Button("📥 Paste as Destination", GUILayout.Height(25)))
//                {
//                    clipboardPath = GUIUtility.systemCopyBuffer;
//                    clipboardPath = CleanPath(clipboardPath);

//                    if (IsValidPath(clipboardPath))
//                    {
//                        destinationPath = clipboardPath;
//                        ShowStatus($"Destination set from clipboard: {clipboardPath}", MessageType.Info);
//                    }
//                }

//                if (GUILayout.Button("📤 Copy Source", GUILayout.Height(25)))
//                {
//                    if (!string.IsNullOrEmpty(sourcePath))
//                    {
//                        GUIUtility.systemCopyBuffer = sourcePath;
//                        ShowStatus($"Copied to clipboard: {sourcePath}", MessageType.Info);
//                    }
//                }

//                if (GUILayout.Button("📤 Copy Dest", GUILayout.Height(25)))
//                {
//                    if (!string.IsNullOrEmpty(destinationPath))
//                    {
//                        GUIUtility.systemCopyBuffer = destinationPath;
//                        ShowStatus($"Copied to clipboard: {destinationPath}", MessageType.Info);
//                    }
//                }

//                EditorGUILayout.EndHorizontal();

//                if (!string.IsNullOrEmpty(clipboardPath))
//                {
//                    EditorGUILayout.LabelField("Last clipboard:", clipboardPath, EditorStyles.miniLabel);
//                }

//                // Smart paste buttons for common operations
//                EditorGUILayout.Space(3);
//                EditorGUILayout.LabelField("Smart Actions:", EditorStyles.miniLabel);
//                EditorGUILayout.BeginHorizontal();

//                if (GUILayout.Button("📋→📁 Paste & Open"))
//                {
//                    clipboardPath = CleanPath(GUIUtility.systemCopyBuffer);
//                    if (IsValidPath(clipboardPath))
//                    {
//                        sourcePath = clipboardPath;
//                        OpenInExplorer(sourcePath);
//                    }
//                }

//                if (GUILayout.Button("📋→🎯 Paste & Select"))
//                {
//                    clipboardPath = CleanPath(GUIUtility.systemCopyBuffer);
//                    if (IsValidPath(clipboardPath))
//                    {
//                        sourcePath = clipboardPath;
//                        SelectInProject(sourcePath);
//                    }
//                }

//                if (GUILayout.Button("📋→💻 Paste & Code"))
//                {
//                    clipboardPath = CleanPath(GUIUtility.systemCopyBuffer);
//                    if (IsValidPath(clipboardPath))
//                    {
//                        sourcePath = clipboardPath;
//                        OpenInVSCode(sourcePath);
//                    }
//                }

//                EditorGUILayout.EndHorizontal();
//            }

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawPathInputSection()
//        {
//            EditorGUILayout.Space(5);
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("📁 Path Management", EditorStyles.boldLabel);

//            // Source Path
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("Source:", GUILayout.Width(80));

//            var prevSource = sourcePath;
//            sourcePath = EditorGUILayout.TextField(sourcePath);
//            if (sourcePath != prevSource && !string.IsNullOrEmpty(sourcePath))
//            {
//                AddToHistory(sourcePath);
//            }

//            // Visual indicator if path exists
//            bool sourceExists = AssetExists(sourcePath);
//            GUI.color = sourceExists ? Color.green : Color.red;
//            EditorGUILayout.LabelField(sourceExists ? "✓" : "✗", GUILayout.Width(20));
//            GUI.color = Color.white;

//            if (GUILayout.Button("📂", GUILayout.Width(25)))
//            {
//                string selected = EditorUtility.OpenFolderPanel("Select Source Folder", "Assets", "");
//                if (!string.IsNullOrEmpty(selected))
//                {
//                    sourcePath = ConvertToAssetPath(selected);
//                    AddToHistory(sourcePath);
//                }
//            }

//            if (GUILayout.Button("🎯", GUILayout.Width(25)))
//            {
//                SelectInProject(sourcePath);
//            }

//            if (GUILayout.Button("🔍", GUILayout.Width(25)))
//            {
//                ShowPathInfo(sourcePath);
//            }

//            EditorGUILayout.EndHorizontal();

//            // Destination Path
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("Destination:", GUILayout.Width(80));
//            destinationPath = EditorGUILayout.TextField(destinationPath);

//            // Visual indicator if path exists
//            bool destExists = AssetExists(destinationPath);
//            GUI.color = destExists ? Color.green : Color.red;
//            EditorGUILayout.LabelField(destExists ? "✓" : "✗", GUILayout.Width(20));
//            GUI.color = Color.white;

//            if (GUILayout.Button("📂", GUILayout.Width(25)))
//            {
//                string selected = EditorUtility.OpenFolderPanel("Select Destination Folder", "Assets", "");
//                if (!string.IsNullOrEmpty(selected))
//                {
//                    destinationPath = ConvertToAssetPath(selected);
//                }
//            }

//            if (GUILayout.Button("🎯", GUILayout.Width(25)))
//            {
//                SelectInProject(destinationPath);
//            }

//            if (GUILayout.Button("🔍", GUILayout.Width(25)))
//            {
//                ShowPathInfo(destinationPath);
//            }

//            EditorGUILayout.EndHorizontal();

//            // Quick operations
//            EditorGUILayout.BeginHorizontal();
//            GUILayout.FlexibleSpace();

//            if (GUILayout.Button("⇅ Swap", GUILayout.Width(60)))
//            {
//                (sourcePath, destinationPath) = (destinationPath, sourcePath);
//            }

//            if (GUILayout.Button("Clear", GUILayout.Width(60)))
//            {
//                sourcePath = "";
//                destinationPath = "";
//            }

//            if (GUILayout.Button("→ Parent", GUILayout.Width(60)))
//            {
//                if (!string.IsNullOrEmpty(sourcePath))
//                {
//                    sourcePath = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? "Assets";
//                    AddToHistory(sourcePath);
//                }
//            }

//            GUILayout.FlexibleSpace();
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawQuickPathSection()
//        {
//            EditorGUILayout.Space(5);
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

//            showQuickPaths = EditorGUILayout.Foldout(showQuickPaths, "⚡ Quick Paths", true);

//            if (showQuickPaths)
//            {
//                // Phase-based quick paths
//                EditorGUILayout.LabelField("Migration Phases:", EditorStyles.miniLabel);
//                EditorGUILayout.BeginHorizontal();

//                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f);
//                if (GUILayout.Button("Phase 1: Player"))
//                    SetSourcePath("Assets/_WildSurvival/Code/Runtime/Player");

//                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.8f);
//                if (GUILayout.Button("Phase 2: Inventory"))
//                    SetSourcePath("Assets/_WildSurvival/Code/Runtime/Survival/Inventory");

//                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.5f);
//                if (GUILayout.Button("Phase 3: UI"))
//                    SetSourcePath("Assets/_WildSurvival/Code/Runtime/UI");

//                GUI.backgroundColor = new Color(0.8f, 0.5f, 0.8f);
//                if (GUILayout.Button("Phase 4: Survival"))
//                    SetSourcePath("Assets/_WildSurvival/Code/Runtime/Survival");

//                GUI.backgroundColor = Color.white;
//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.BeginHorizontal();

//                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.8f);
//                if (GUILayout.Button("Phase 5: Crafting"))
//                    SetSourcePath("Assets/_WildSurvival/Code/Runtime/Survival/Crafting");

//                GUI.backgroundColor = new Color(0.9f, 0.6f, 0.4f);
//                if (GUILayout.Button("Phase 6: Environment"))
//                    SetSourcePath("Assets/_WildSurvival/Code/Runtime/Environment");

//                GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
//                if (GUILayout.Button("Phase 7: Save"))
//                    SetSourcePath("Assets/_WildSurvival/Code/Runtime/SaveSystem");

//                GUI.backgroundColor = new Color(0.6f, 0.9f, 0.4f);
//                if (GUILayout.Button("Phase 8: Audio"))
//                    SetSourcePath("Assets/_WildSurvival/Code/Runtime/Audio");

//                GUI.backgroundColor = Color.white;
//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.Space(5);

//                // Common paths
//                EditorGUILayout.LabelField("Common Locations:", EditorStyles.miniLabel);
//                EditorGUILayout.BeginHorizontal();
//                if (GUILayout.Button("Core"))
//                    SetSourcePath("Assets/_WildSurvival/Code/Runtime/Core");
//                if (GUILayout.Button("Prefabs"))
//                    SetSourcePath("Assets/_WildSurvival/Prefabs");
//                if (GUILayout.Button("Scenes"))
//                    SetSourcePath("Assets/_WildSurvival/Scenes");
//                if (GUILayout.Button("Data"))
//                    SetSourcePath("Assets/_WildSurvival/Data");
//                if (GUILayout.Button("Editor"))
//                    SetSourcePath("Assets/WildSurvival/Editor");
//                EditorGUILayout.EndHorizontal();

//                // Custom saved paths
//                EditorGUILayout.Space(3);
//                EditorGUILayout.LabelField("Saved Paths:", EditorStyles.miniLabel);

//                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(80));

//                for (int i = 0; i < quickPaths.Count; i++)
//                {
//                    EditorGUILayout.BeginHorizontal();

//                    GUI.backgroundColor = quickPaths[i].color;
//                    if (GUILayout.Button(quickPaths[i].name, GUILayout.Width(100)))
//                    {
//                        SetSourcePath(quickPaths[i].path);
//                    }
//                    GUI.backgroundColor = Color.white;

//                    EditorGUILayout.LabelField(quickPaths[i].path, EditorStyles.miniLabel);

//                    // Color picker
//                    quickPaths[i].color = EditorGUILayout.ColorField(quickPaths[i].color, GUILayout.Width(40));

//                    if (GUILayout.Button("❌", GUILayout.Width(20)))
//                    {
//                        quickPaths.RemoveAt(i);
//                        SaveQuickPaths();
//                        break;
//                    }

//                    EditorGUILayout.EndHorizontal();
//                }

//                EditorGUILayout.EndScrollView();

//                // Add current as quick path
//                EditorGUILayout.BeginHorizontal();
//                if (GUILayout.Button("💾 Save Current Source as Quick Path"))
//                {
//                    if (!string.IsNullOrEmpty(sourcePath) && AssetExists(sourcePath))
//                    {
//                        string name = Path.GetFileName(sourcePath);
//                        if (string.IsNullOrEmpty(name)) name = "Root";

//                        var newPath = new QuickPath(name, sourcePath);
//                        newPath.color = UnityEngine.Random.ColorHSV(0.3f, 0.8f, 0.5f, 0.9f, 0.8f, 1f);
//                        quickPaths.Add(newPath);
//                        SaveQuickPaths();
//                        ShowStatus($"Saved quick path: {name}", MessageType.Info);
//                    }
//                    else
//                    {
//                        ShowStatus("Invalid or empty source path", MessageType.Warning);
//                    }
//                }
//                EditorGUILayout.EndHorizontal();
//            }

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawOperationsSection()
//        {
//            EditorGUILayout.Space(5);
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

//            showOperations = EditorGUILayout.Foldout(showOperations, "🔧 Operations", true);

//            if (showOperations)
//            {
//                // File Operations Row 1
//                EditorGUILayout.LabelField("File Operations:", EditorStyles.boldLabel);
//                EditorGUILayout.BeginHorizontal();

//                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f);
//                if (GUILayout.Button("📁 Open Explorer", GUILayout.Height(30)))
//                {
//                    OpenInExplorer(sourcePath);
//                }

//                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.8f);
//                if (GUILayout.Button("💻 Open Code", GUILayout.Height(30)))
//                {
//                    OpenInVSCode(sourcePath);
//                }

//                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.5f);
//                if (GUILayout.Button("🎯 Select", GUILayout.Height(30)))
//                {
//                    SelectInProject(sourcePath);
//                }

//                GUI.backgroundColor = Color.white;
//                EditorGUILayout.EndHorizontal();

//                // File Operations Row 2
//                EditorGUILayout.BeginHorizontal();

//                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.8f);
//                if (GUILayout.Button("✂️ Move to Dest", GUILayout.Height(30)))
//                {
//                    MoveAsset(sourcePath, destinationPath);
//                }

//                GUI.backgroundColor = new Color(0.8f, 0.5f, 0.8f);
//                if (GUILayout.Button("📋 Copy to Dest", GUILayout.Height(30)))
//                {
//                    CopyAsset(sourcePath, destinationPath);
//                }

//                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.5f);
//                if (GUILayout.Button("✏️ Rename", GUILayout.Height(30)))
//                {
//                    RenameAsset(sourcePath);
//                }

//                GUI.backgroundColor = Color.white;
//                EditorGUILayout.EndHorizontal();

//                // Batch operations
//                EditorGUILayout.BeginHorizontal();

//                GUI.backgroundColor = new Color(0.7f, 0.9f, 0.7f);
//                if (GUILayout.Button("📁 Create Subfolder", GUILayout.Height(30)))
//                {
//                    CreateFolder(sourcePath);
//                }

//                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.7f);
//                if (GUILayout.Button("📄 Duplicate", GUILayout.Height(30)))
//                {
//                    DuplicateAsset(sourcePath);
//                }

//                GUI.backgroundColor = new Color(0.7f, 0.7f, 0.9f);
//                if (GUILayout.Button("🔄 Refresh", GUILayout.Height(30)))
//                {
//                    AssetDatabase.Refresh();
//                    ShowStatus("Asset database refreshed", MessageType.Info);
//                }

//                GUI.backgroundColor = Color.white;
//                EditorGUILayout.EndHorizontal();

//                // Dangerous operations
//                EditorGUILayout.Space(5);
//                EditorGUILayout.BeginHorizontal();

//                GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
//                if (GUILayout.Button("🗑️ Delete Source", GUILayout.Height(30)))
//                {
//                    DeleteAsset(sourcePath);
//                }

//                GUI.backgroundColor = new Color(0.9f, 0.7f, 0.5f);
//                if (GUILayout.Button("🧹 Clean Empty Folders", GUILayout.Height(30)))
//                {
//                    CleanEmptyFolders();
//                }

//                GUI.backgroundColor = Color.white;
//                EditorGUILayout.EndHorizontal();

//                // Search operations
//                EditorGUILayout.Space(5);
//                EditorGUILayout.LabelField("Search & Find:", EditorStyles.boldLabel);

//                EditorGUILayout.BeginHorizontal();
//                searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
//                if (GUILayout.Button("🔍", GUILayout.Width(30)))
//                {
//                    SearchInPath(searchFilter, sourcePath);
//                }
//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.BeginHorizontal();
//                if (GUILayout.Button("Find Scripts", GUILayout.Height(25)))
//                {
//                    FindAllOfType("t:Script", sourcePath);
//                }

//                if (GUILayout.Button("Find Prefabs", GUILayout.Height(25)))
//                {
//                    FindAllOfType("t:Prefab", sourcePath);
//                }

//                if (GUILayout.Button("Find Scenes", GUILayout.Height(25)))
//                {
//                    FindAllOfType("t:Scene", sourcePath);
//                }

//                if (GUILayout.Button("Find ScriptableObjects", GUILayout.Height(25)))
//                {
//                    FindAllOfType("t:ScriptableObject", sourcePath);
//                }
//                EditorGUILayout.EndHorizontal();
//            }

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawBatchCreationSection()
//        {
//            EditorGUILayout.Space(5);
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

//            showBatchCreate = EditorGUILayout.Foldout(showBatchCreate, "🚀 Batch Creation", true);

//            if (showBatchCreate)
//            {
//                EditorGUILayout.LabelField("Batch Create Scripts & Folders:", EditorStyles.boldLabel);

//                // Base path
//                EditorGUILayout.BeginHorizontal();
//                EditorGUILayout.LabelField("Base Path:", GUILayout.Width(80));
//                batchCreatePath = EditorGUILayout.TextField(batchCreatePath);
//                if (GUILayout.Button("Use Source", GUILayout.Width(80)))
//                {
//                    batchCreatePath = sourcePath;
//                }
//                EditorGUILayout.EndHorizontal();

//                // Folders to create
//                EditorGUILayout.LabelField("Folders (one per line):", EditorStyles.miniLabel);
//                batchFolderNames = EditorGUILayout.TextArea(batchFolderNames, GUILayout.Height(60));

//                // Scripts to create
//                EditorGUILayout.LabelField("Scripts (one per line, .cs optional):", EditorStyles.miniLabel);
//                batchScriptNames = EditorGUILayout.TextArea(batchScriptNames, GUILayout.Height(60));

//                EditorGUILayout.Space(5);
//                EditorGUILayout.BeginHorizontal();

//                // Create folders button
//                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f);
//                if (GUILayout.Button("📁 Create All Folders", GUILayout.Height(30)))
//                {
//                    CreateBatchFolders();
//                }

//                // Create scripts button
//                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.8f);
//                if (GUILayout.Button("📄 Create All Scripts", GUILayout.Height(30)))
//                {
//                    CreateBatchScripts();
//                }

//                // Create both button
//                GUI.backgroundColor = new Color(0.8f, 0.5f, 0.8f);
//                if (GUILayout.Button("⚡ Create Both", GUILayout.Height(30)))
//                {
//                    CreateBatchFolders();
//                    CreateBatchScripts();
//                }

//                GUI.backgroundColor = Color.white;
//                EditorGUILayout.EndHorizontal();

//                // Quick templates
//                EditorGUILayout.Space(5);
//                EditorGUILayout.LabelField("Quick Templates:", EditorStyles.miniLabel);
//                EditorGUILayout.BeginHorizontal();

//                if (GUILayout.Button("Phase 2 Structure", GUILayout.Height(25)))
//                {
//                    batchFolderNames = "Core\nUI\nItems\nData";
//                    batchScriptNames = "InventoryManager\nInventorySlot\nInventoryUI\nItemDefinition\nItemInstance";
//                }

//                if (GUILayout.Button("Clear", GUILayout.Height(25)))
//                {
//                    batchFolderNames = "";
//                    batchScriptNames = "";
//                }

//                EditorGUILayout.EndHorizontal();
//            }

//            EditorGUILayout.EndVertical();
//        }

//        private void CreateBatchFolders()
//        {
//            if (string.IsNullOrEmpty(batchCreatePath))
//            {
//                ShowStatus("Please specify a base path", MessageType.Warning);
//                return;
//            }

//            string[] folders = batchFolderNames.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
//            int created = 0;

//            foreach (string folderName in folders)
//            {
//                string trimmed = folderName.Trim();
//                if (!string.IsNullOrEmpty(trimmed))
//                {
//                    string fullPath = $"{batchCreatePath}/{trimmed}";
//                    if (!AssetDatabase.IsValidFolder(fullPath))
//                    {
//                        string parentPath = batchCreatePath;
//                        string[] parts = trimmed.Split('/');

//                        foreach (string part in parts)
//                        {
//                            string newPath = $"{parentPath}/{part}";
//                            if (!AssetDatabase.IsValidFolder(newPath))
//                            {
//                                AssetDatabase.CreateFolder(parentPath, part);
//                            }
//                            parentPath = newPath;
//                        }
//                        created++;
//                    }
//                }
//            }

//            if (created > 0)
//            {
//                AssetDatabase.Refresh();
//                ShowStatus($"Created {created} folders", MessageType.Info);
//            }
//        }

//        private void CreateBatchScripts()
//        {
//            if (string.IsNullOrEmpty(batchCreatePath))
//            {
//                ShowStatus("Please specify a base path", MessageType.Warning);
//                return;
//            }

//            string[] scripts = batchScriptNames.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
//            int created = 0;

//            // Script template
//            string template = @"using UnityEngine;

//namespace WildSurvival
//{
//    public class #SCRIPTNAME# : MonoBehaviour
//    {
//        void Start()
//        {

//        }

//        void Update()
//        {

//        }
//    }
//}";

//            foreach (string scriptName in scripts)
//            {
//                string trimmed = scriptName.Trim();
//                if (!string.IsNullOrEmpty(trimmed))
//                {
//                    // Add .cs if not present
//                    if (!trimmed.EndsWith(".cs"))
//                        trimmed += ".cs";

//                    string scriptPath = $"{batchCreatePath}/{trimmed}";

//                    if (!System.IO.File.Exists(scriptPath))
//                    {
//                        // Create script with template
//                        string className = System.IO.Path.GetFileNameWithoutExtension(trimmed);
//                        string content = template.Replace("#SCRIPTNAME#", className);

//                        System.IO.File.WriteAllText(scriptPath, content);
//                        created++;
//                    }
//                }
//            }

//            if (created > 0)
//            {
//                AssetDatabase.Refresh();
//                ShowStatus($"Created {created} scripts", MessageType.Info);
//            }
//        }

//        private void DrawHistorySection()
//        {
//            if (!showHistory) return;

//            EditorGUILayout.Space(5);
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

//            showHistory = EditorGUILayout.Foldout(showHistory, "📜 Path History", true);

//            if (showHistory && pathHistory.Count > 0)
//            {
//                historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition, GUILayout.MaxHeight(100));

//                for (int i = pathHistory.Count - 1; i >= 0; i--)
//                {
//                    EditorGUILayout.BeginHorizontal();

//                    if (GUILayout.Button($"{pathHistory.Count - i}.", GUILayout.Width(25)))
//                    {
//                        sourcePath = pathHistory[i];
//                        SelectInProject(sourcePath);
//                    }

//                    EditorGUILayout.LabelField(pathHistory[i], EditorStyles.miniLabel);

//                    if (GUILayout.Button("→S", GUILayout.Width(25)))
//                    {
//                        sourcePath = pathHistory[i];
//                    }

//                    if (GUILayout.Button("→D", GUILayout.Width(25)))
//                    {
//                        destinationPath = pathHistory[i];
//                    }

//                    EditorGUILayout.EndHorizontal();
//                }

//                EditorGUILayout.EndScrollView();

//                if (GUILayout.Button("Clear History"))
//                {
//                    pathHistory.Clear();
//                    SaveHistory();
//                }
//            }

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawStatusSection()
//        {
//            if (!string.IsNullOrEmpty(statusMessage))
//            {
//                EditorGUILayout.Space(5);
//                EditorGUILayout.HelpBox(statusMessage, statusType);
//            }
//        }

//        // Helper Methods
//        private string CleanPath(string path)
//        {
//            if (string.IsNullOrEmpty(path)) return "";

//            // Remove quotes and trim
//            path = path.Trim('"', ' ', '\n', '\r', '\t');

//            // Convert backslashes to forward slashes
//            path = path.Replace('\\', '/');

//            // Remove file:/// prefix if present
//            if (path.StartsWith("file:///"))
//                path = path.Substring(8);

//            // Handle full paths
//            if (Path.IsPathRooted(path))
//            {
//                // Try to convert to relative asset path
//                string dataPath = Application.dataPath;
//                if (path.Contains(dataPath))
//                {
//                    path = "Assets" + path.Substring(dataPath.Length);
//                }
//                else if (path.Contains("Assets"))
//                {
//                    int index = path.IndexOf("Assets");
//                    path = path.Substring(index);
//                }
//            }

//            // Ensure it starts with Assets if it's a relative path
//            if (!path.StartsWith("Assets") && !path.StartsWith("Packages") && !Path.IsPathRooted(path))
//            {
//                path = "Assets/" + path;
//            }

//            return path;
//        }

//        private bool IsValidPath(string path)
//        {
//            if (string.IsNullOrEmpty(path)) return false;
//            return path.StartsWith("Assets") || path.StartsWith("Packages");
//        }

//        private bool AssetExists(string path)
//        {
//            if (string.IsNullOrEmpty(path)) return false;
//            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null ||
//                   AssetDatabase.IsValidFolder(path);
//        }

//        private string ConvertToAssetPath(string fullPath)
//        {
//            if (fullPath.Contains("Assets"))
//            {
//                int index = fullPath.IndexOf("Assets");
//                return fullPath.Substring(index).Replace('\\', '/');
//            }
//            return fullPath;
//        }

//        private void SetSourcePath(string path)
//        {
//            sourcePath = path;
//            AddToHistory(path);
//            SelectInProject(path);
//        }

//        private void AddToHistory(string path)
//        {
//            if (string.IsNullOrEmpty(path)) return;

//            // Remove if already exists
//            pathHistory.Remove(path);

//            // Add to end
//            pathHistory.Add(path);

//            // Limit history size
//            while (pathHistory.Count > MAX_HISTORY)
//            {
//                pathHistory.RemoveAt(0);
//            }

//            SaveHistory();
//        }

//        // Operation Methods
//        private void OpenInExplorer(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//            {
//                ShowStatus("No path specified", MessageType.Warning);
//                return;
//            }

//            string fullPath = Path.GetFullPath(path);

//            if (File.Exists(fullPath) || Directory.Exists(fullPath))
//            {
//                EditorUtility.RevealInFinder(fullPath);
//                ShowStatus($"Opened in Explorer: {path}", MessageType.Info);
//            }
//            else
//            {
//                ShowStatus($"Path not found: {path}", MessageType.Error);
//            }
//        }

//        private void OpenInVSCode(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//            {
//                ShowStatus("No path specified", MessageType.Warning);
//                return;
//            }

//            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
//            if (asset != null)
//            {
//                AssetDatabase.OpenAsset(asset);
//                ShowStatus($"Opened in editor: {path}", MessageType.Info);
//            }
//            else
//            {
//                ShowStatus($"Could not open: {path}", MessageType.Error);
//            }
//        }

//        private void SelectInProject(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//            {
//                ShowStatus("No path specified", MessageType.Warning);
//                return;
//            }

//            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
//            if (asset != null)
//            {
//                Selection.activeObject = asset;
//                EditorGUIUtility.PingObject(asset);
//                ShowStatus($"Selected: {path}", MessageType.Info);
//            }
//            else if (AssetDatabase.IsValidFolder(path))
//            {
//                // For folders, load the folder asset
//                var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
//                if (folderAsset != null)
//                {
//                    Selection.activeObject = folderAsset;
//                    EditorGUIUtility.PingObject(folderAsset);
//                    ShowStatus($"Selected folder: {path}", MessageType.Info);
//                }
//            }
//            else
//            {
//                ShowStatus($"Asset not found: {path}", MessageType.Error);
//            }
//        }

//        private void ShowPathInfo(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//            {
//                ShowStatus("No path specified", MessageType.Warning);
//                return;
//            }

//            string info = $"Path Info for: {path}\n";

//            if (File.Exists(path))
//            {
//                FileInfo fi = new FileInfo(path);
//                info += $"Type: File\n";
//                info += $"Size: {FormatFileSize(fi.Length)}\n";
//                info += $"Created: {fi.CreationTime}\n";
//                info += $"Modified: {fi.LastWriteTime}";
//            }
//            else if (Directory.Exists(path))
//            {
//                DirectoryInfo di = new DirectoryInfo(path);
//                var files = di.GetFiles("*", SearchOption.AllDirectories);
//                var dirs = di.GetDirectories("*", SearchOption.AllDirectories);
//                long totalSize = files.Sum(f => f.Length);

//                info += $"Type: Folder\n";
//                info += $"Files: {files.Length}\n";
//                info += $"Subfolders: {dirs.Length}\n";
//                info += $"Total Size: {FormatFileSize(totalSize)}";
//            }
//            else
//            {
//                info += "Path does not exist";
//            }

//            EditorUtility.DisplayDialog("Path Information", info, "OK");
//        }

//        private string FormatFileSize(long bytes)
//        {
//            string[] sizes = { "B", "KB", "MB", "GB" };
//            double len = bytes;
//            int order = 0;
//            while (len >= 1024 && order < sizes.Length - 1)
//            {
//                order++;
//                len = len / 1024;
//            }
//            return $"{len:0.##} {sizes[order]}";
//        }

//        private void MoveAsset(string from, string to)
//        {
//            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
//            {
//                ShowStatus("Source and destination paths required", MessageType.Warning);
//                return;
//            }

//            if (!AssetExists(from))
//            {
//                ShowStatus($"Source not found: {from}", MessageType.Error);
//                return;
//            }

//            // If destination is a folder, append the source filename
//            if (AssetDatabase.IsValidFolder(to))
//            {
//                string fileName = Path.GetFileName(from);
//                to = Path.Combine(to, fileName).Replace('\\', '/');
//            }

//            string error = AssetDatabase.MoveAsset(from, to);
//            if (string.IsNullOrEmpty(error))
//            {
//                if (autoRefresh) AssetDatabase.Refresh();
//                ShowStatus($"Moved: {from} → {to}", MessageType.Info);
//                sourcePath = to; // Update source to new location
//                AddToHistory(to);
//            }
//            else
//            {
//                ShowStatus($"Move failed: {error}", MessageType.Error);
//            }
//        }

//        private void CopyAsset(string from, string to)
//        {
//            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
//            {
//                ShowStatus("Source and destination paths required", MessageType.Warning);
//                return;
//            }

//            // If destination is a folder, append the source filename
//            if (AssetDatabase.IsValidFolder(to))
//            {
//                string fileName = Path.GetFileName(from);
//                to = Path.Combine(to, fileName).Replace('\\', '/');
//            }

//            if (AssetDatabase.CopyAsset(from, to))
//            {
//                if (autoRefresh) AssetDatabase.Refresh();
//                ShowStatus($"Copied: {from} → {to}", MessageType.Info);
//                AddToHistory(to);
//            }
//            else
//            {
//                ShowStatus($"Copy failed: {from} → {to}", MessageType.Error);
//            }
//        }

//        private void DuplicateAsset(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//            {
//                ShowStatus("No path specified", MessageType.Warning);
//                return;
//            }

//            string newPath = AssetDatabase.GenerateUniqueAssetPath(path);
//            if (AssetDatabase.CopyAsset(path, newPath))
//            {
//                if (autoRefresh) AssetDatabase.Refresh();
//                ShowStatus($"Duplicated: {path} → {newPath}", MessageType.Info);
//                sourcePath = newPath;
//                AddToHistory(newPath);
//                SelectInProject(newPath);
//            }
//            else
//            {
//                ShowStatus($"Duplication failed: {path}", MessageType.Error);
//            }
//        }

//        private void RenameAsset(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//            {
//                ShowStatus("No path specified", MessageType.Warning);
//                return;
//            }

//            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
//            if (asset != null)
//            {
//                Selection.activeObject = asset;
//                EditorApplication.ExecuteMenuItem("Assets/Rename");
//                ShowStatus($"Rename initiated for: {path}", MessageType.Info);
//            }
//            else
//            {
//                ShowStatus($"Asset not found: {path}", MessageType.Error);
//            }
//        }

//        private void DeleteAsset(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//            {
//                ShowStatus("No path specified", MessageType.Warning);
//                return;
//            }

//            if (EditorUtility.DisplayDialog("Confirm Delete",
//                $"Are you sure you want to delete:\n{path}?\n\nThis cannot be undone!",
//                "Delete", "Cancel"))
//            {
//                if (AssetDatabase.DeleteAsset(path))
//                {
//                    if (autoRefresh) AssetDatabase.Refresh();
//                    ShowStatus($"Deleted: {path}", MessageType.Info);
//                    sourcePath = ""; // Clear source
//                }
//                else
//                {
//                    ShowStatus($"Delete failed: {path}", MessageType.Error);
//                }
//            }
//        }

//        private void CreateFolder(string basePath)
//        {
//            string folderName = "NewFolder";

//            // Show input dialog
//            string input = EditorInputDialog.Show("Create Folder", "Enter folder name:", folderName);
//            if (string.IsNullOrEmpty(input)) return;

//            string parentPath = string.IsNullOrEmpty(basePath) ? "Assets" : basePath;

//            if (!AssetDatabase.IsValidFolder(parentPath))
//            {
//                // If it's a file, use its directory
//                parentPath = Path.GetDirectoryName(parentPath)?.Replace('\\', '/') ?? "Assets";
//            }

//            string guid = AssetDatabase.CreateFolder(parentPath, input);
//            if (!string.IsNullOrEmpty(guid))
//            {
//                string newFolderPath = AssetDatabase.GUIDToAssetPath(guid);
//                if (autoRefresh) AssetDatabase.Refresh();
//                ShowStatus($"Created folder: {newFolderPath}", MessageType.Info);
//                sourcePath = newFolderPath;
//                AddToHistory(newFolderPath);
//                SelectInProject(newFolderPath);
//            }
//            else
//            {
//                ShowStatus($"Failed to create folder", MessageType.Error);
//            }
//        }

//        private void CleanEmptyFolders()
//        {
//            if (string.IsNullOrEmpty(sourcePath))
//            {
//                ShowStatus("No source path specified", MessageType.Warning);
//                return;
//            }

//            if (!AssetDatabase.IsValidFolder(sourcePath))
//            {
//                ShowStatus("Source must be a folder", MessageType.Warning);
//                return;
//            }

//            int deleted = 0;
//            var dirs = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories);

//            // Process from deepest to shallowest
//            Array.Sort(dirs, (a, b) => b.Length.CompareTo(a.Length));

//            foreach (var dir in dirs)
//            {
//                if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
//                {
//                    string assetPath = ConvertToAssetPath(dir);
//                    if (AssetDatabase.DeleteAsset(assetPath))
//                    {
//                        deleted++;
//                    }
//                }
//            }

//            if (deleted > 0)
//            {
//                AssetDatabase.Refresh();
//                ShowStatus($"Deleted {deleted} empty folders", MessageType.Info);
//            }
//            else
//            {
//                ShowStatus("No empty folders found", MessageType.Info);
//            }
//        }

//        private void SearchInPath(string searchTerm, string inPath)
//        {
//            if (string.IsNullOrEmpty(searchTerm))
//            {
//                ShowStatus("Enter a search term", MessageType.Warning);
//                return;
//            }

//            string searchPath = string.IsNullOrEmpty(inPath) ? "Assets" : inPath;
//            var guids = AssetDatabase.FindAssets(searchTerm, new[] { searchPath });

//            searchResults.Clear();
//            foreach (var guid in guids)
//            {
//                searchResults.Add(AssetDatabase.GUIDToAssetPath(guid));
//            }

//            if (searchResults.Count > 0)
//            {
//                Debug.Log($"Search results for '{searchTerm}' in {searchPath}:");
//                foreach (var result in searchResults)
//                {
//                    Debug.Log($"  → {result}");
//                }
//                ShowStatus($"Found {searchResults.Count} results (see Console)", MessageType.Info);
//            }
//            else
//            {
//                ShowStatus($"No results for '{searchTerm}'", MessageType.Warning);
//            }
//        }

//        private void FindAllOfType(string filter, string inPath)
//        {
//            string searchPath = string.IsNullOrEmpty(inPath) ? "Assets" : inPath;
//            var guids = AssetDatabase.FindAssets(filter, new[] { searchPath });

//            if (guids.Length == 0)
//            {
//                ShowStatus($"No items found with filter: {filter}", MessageType.Warning);
//                return;
//            }

//            Debug.Log($"Found {guids.Length} items in {searchPath}:");
//            foreach (var guid in guids)
//            {
//                string path = AssetDatabase.GUIDToAssetPath(guid);
//                Debug.Log($"  → {path}");
//            }

//            ShowStatus($"Found {guids.Length} items (see Console)", MessageType.Info);
//        }

//        private void ShowStatus(string message, MessageType type)
//        {
//            statusMessage = message;
//            statusType = type;
//            Debug.Log($"[WS File Manager] {message}");
//            Repaint();
//        }

//        // Persistence
//        private void SaveQuickPaths()
//        {
//            string json = JsonUtility.ToJson(new SerializableList<QuickPath>(quickPaths));
//            EditorPrefs.SetString("WildSurvival_FileManager_QuickPaths", json);
//        }

//        private void LoadQuickPaths()
//        {
//            string json = EditorPrefs.GetString("WildSurvival_FileManager_QuickPaths", "");
//            if (!string.IsNullOrEmpty(json))
//            {
//                var loaded = JsonUtility.FromJson<SerializableList<QuickPath>>(json);
//                if (loaded != null && loaded.items != null)
//                {
//                    quickPaths = loaded.items;
//                }
//            }
//        }

//        private void SaveHistory()
//        {
//            string json = JsonUtility.ToJson(new SerializableList<string>(pathHistory));
//            EditorPrefs.SetString("WildSurvival_FileManager_History", json);
//        }

//        private void LoadHistory()
//        {
//            string json = EditorPrefs.GetString("WildSurvival_FileManager_History", "");
//            if (!string.IsNullOrEmpty(json))
//            {
//                var loaded = JsonUtility.FromJson<SerializableList<string>>(json);
//                if (loaded != null && loaded.items != null)
//                {
//                    pathHistory = loaded.items;
//                }
//            }
//        }

//        [System.Serializable]
//        private class SerializableList<T>
//        {
//            public List<T> items;
//            public SerializableList(List<T> list) { items = list; }
//        }
//    }

//    // Simple Input Dialog
//    public class EditorInputDialog : EditorWindow
//    {
//        private static string inputValue = "";
//        private static string promptText = "";
//        private static bool confirmed = false;

//        public static string Show(string title, string prompt, string defaultValue)
//        {
//            inputValue = defaultValue;
//            promptText = prompt;
//            confirmed = false;

//            var window = GetWindow<EditorInputDialog>(true, title, true);
//            window.minSize = new Vector2(300, 100);
//            window.maxSize = new Vector2(300, 100);
//            window.ShowModal();

//            return confirmed ? inputValue : null;
//        }

//        void OnGUI()
//        {
//            EditorGUILayout.Space(10);
//            EditorGUILayout.LabelField(promptText);

//            GUI.SetNextControlName("InputField");
//            inputValue = EditorGUILayout.TextField(inputValue);

//            // Focus the text field on first frame
//            if (Event.current.type == EventType.Repaint)
//            {
//                EditorGUI.FocusTextInControl("InputField");
//            }

//            EditorGUILayout.Space(10);
//            EditorGUILayout.BeginHorizontal();

//            if (GUILayout.Button("OK") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
//            {
//                confirmed = true;
//                Close();
//            }

//            if (GUILayout.Button("Cancel") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
//            {
//                confirmed = false;
//                Close();
//            }

//            EditorGUILayout.EndHorizontal();
//        }
//    }
//}