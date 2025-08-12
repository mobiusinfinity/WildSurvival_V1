using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.Tools
{
    public class ErrorReporter : EditorWindow
    {
        // Phase tracking data
        private static readonly Dictionary<string, PhaseInfo> PHASE_DATA = new Dictionary<string, PhaseInfo>
        {
            ["Phase1"] = new PhaseInfo("Player System", new[] {
                "PlayerMovementController.cs", "PlayerAnimatorController.cs",
                "ThirdPersonCameraController.cs", "PlayerStats.cs", "PlayerController_OLD.cs"
            }),
            ["Phase2"] = new PhaseInfo("Inventory System", new[] {
                "ItemType.cs", "ItemData.cs", "ItemStack.cs", "InventorySlot.cs",
                "InventoryEvents.cs", "InventoryManager.cs", "ItemDatabase.cs",
                "InventoryUI.cs", "InventorySlotUI.cs", "ItemDragHandler.cs"
            }),
            ["Phase3"] = new PhaseInfo("Journal UI", new[] {
                "JournalManager.cs", "JournalEntry.cs", "JournalUI.cs", "JournalTab.cs"
            }),
            ["Phase4"] = new PhaseInfo("Survival Mechanics", new[] {
                "HungerSystem.cs", "ThirstSystem.cs", "TemperatureSystem.cs", "StatusEffect.cs"
            }),
            ["Phase5"] = new PhaseInfo("Crafting System", new[] {
                "CraftingManager.cs", "Recipe.cs", "CraftingUI.cs", "CraftingStation.cs"
            })
        };

        private static DateTime lastCaptureTime = DateTime.Now;
        private static DateTime previousCaptureTime = DateTime.Now;
        private static List<string> captureHistory = new List<string>();
        private const int MAX_HISTORY = 5;

        private class PhaseInfo
        {
            public string Name { get; }
            public string[] RequiredFiles { get; }

            public PhaseInfo(string name, string[] files)
            {
                Name = name;
                RequiredFiles = files;
            }
        }

        [MenuItem("Tools/Wild Survival/📊 Error Reporter (Enhanced)")]
        public static void ShowWindow()
        {
            GetWindow<ErrorReporter>("Error Reporter").Show();
        }

        [MenuItem("Tools/Wild Survival/Quick Report %#e")]
        public static void QuickCapture()
        {
            string report = GenerateReport();
            EditorGUIUtility.systemCopyBuffer = report;
            Debug.Log("📊 Error Report copied to clipboard!");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("🔍 Unity Project Status Reporter", titleStyle);
            EditorGUILayout.Space(10);

            if (GUILayout.Button("📊 Capture Status Report", GUILayout.Height(30)))
            {
                string report = GenerateReport();
                EditorGUIUtility.systemCopyBuffer = report;
                ShowNotification(new GUIContent("Report copied to clipboard!"));
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("🔄 Refresh & Copy", GUILayout.Height(25)))
            {
                AssetDatabase.Refresh();
                string report = GenerateReport();
                EditorGUIUtility.systemCopyBuffer = report;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Shortcut: Ctrl+Shift+E for quick capture", MessageType.Info);

            // Show current phase progress
            DrawPhaseProgress();
        }

        private void DrawPhaseProgress()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Migration Progress", EditorStyles.boldLabel);

            foreach (var phase in PHASE_DATA)
            {
                var info = phase.Value;
                var implemented = CountImplementedFiles(info.RequiredFiles);
                var total = info.RequiredFiles.Length;
                var progress = (float)implemented / total;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{phase.Key}: {info.Name}", GUILayout.Width(150));

                // Progress bar
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(200));
                EditorGUI.ProgressBar(rect, progress, $"{implemented}/{total}");

                // Status icon
                string status = progress >= 1 ? "✅" : progress > 0 ? "🔄" : "⏳";
                EditorGUILayout.LabelField(status, GUILayout.Width(30));

                EditorGUILayout.EndHorizontal();
            }
        }

        private static int CountImplementedFiles(string[] files)
        {
            int count = 0;
            foreach (var file in files)
            {
                string[] paths = {
                    $"Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Core/{file}",
                    $"Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Items/{file}",
                    $"Assets/_WildSurvival/Code/Runtime/Survival/Inventory/UI/{file}",
                    $"Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Events/{file}",
                    $"Assets/_WildSurvival/Code/Runtime/Player/Controller/{file}",
                    $"Assets/_WildSurvival/Code/Runtime/Player/Camera/{file}",
                    $"Assets/_WildSurvival/Code/Runtime/Player/Stats/{file}"
                };

                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        var fileInfo = new FileInfo(path);
                        // Check if it's a real implementation (not placeholder)
                        if (fileInfo.Length > 500) // Placeholder files are ~235-241 bytes
                        {
                            count++;
                            break;
                        }
                    }
                }
            }
            return count;
        }

        private static string GenerateReport()
        {
            // Update capture times
            previousCaptureTime = lastCaptureTime;
            lastCaptureTime = DateTime.Now;

            // Add to history
            string captureEntry = $"Capture at {lastCaptureTime:HH:mm:ss} (prev: {previousCaptureTime:HH:mm:ss})";
            captureHistory.Add(captureEntry);
            if (captureHistory.Count > MAX_HISTORY)
                captureHistory.RemoveAt(0);

            StringBuilder report = new StringBuilder();

            // Header
            report.AppendLine("## 📊 Unity Project Status Report");
            report.AppendLine($"**Timestamp:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"**Unity Version:** {Application.unityVersion}");
            report.AppendLine("**Session:** Wild Survival Migration");

            // Build Status
            report.AppendLine("### 🔴 Build Status");
            report.AppendLine("```");

            var logs = GetConsoleLogs();
            report.AppendLine($"Console Entries: {logs.Count}");

            var errors = logs.Where(l => l.Contains("[Error]")).ToList();
            var warnings = logs.Where(l => l.Contains("[Warning]")).ToList();

            if (errors.Any())
            {
                report.AppendLine($"❌ Errors ({errors.Count}):");
                foreach (var error in errors.Take(3))
                {
                    report.AppendLine($"  {error}");
                }
            }
            else if (warnings.Any())
            {
                report.AppendLine($"⚠️ Warnings ({warnings.Count}):");
                foreach (var warning in warnings.Take(3))
                {
                    report.AppendLine($"  {warning}");
                }
            }
            else
            {
                report.AppendLine("✅ No compilation errors detected");
            }

            report.AppendLine("```");

            // Phase Progress Section
            report.AppendLine("### 📈 Migration Progress");
            report.AppendLine("```");

            string currentPhase = GetCurrentPhase();
            foreach (var phase in PHASE_DATA)
            {
                var info = phase.Value;
                var implemented = CountImplementedFiles(info.RequiredFiles);
                var total = info.RequiredFiles.Length;
                var progress = (float)implemented / total;

                // Create progress bar
                int barLength = 12;
                int filled = Mathf.RoundToInt(progress * barLength);
                string progressBar = new string('█', filled) + new string('░', barLength - filled);
                string status = progress >= 1 ? "✅ COMPLETE" : progress > 0 ? "🔄 IN PROGRESS" : "⏳ NOT STARTED";

                report.AppendLine($"{phase.Key}: {progressBar} {implemented}/{total} {status}");

                // Show next file to implement
                if (phase.Key == currentPhase && progress < 1)
                {
                    string nextFile = GetNextFileToImplement(info.RequiredFiles);
                    if (!string.IsNullOrEmpty(nextFile))
                    {
                        report.AppendLine($"  → Next: {nextFile}");
                    }
                }
            }

            report.AppendLine("```");

            // Project Structure
            report.AppendLine("### 📁 Project Structure");
            report.AppendLine("```");

            // Count scripts in each phase folder
            var phase1Scripts = Directory.GetFiles("Assets/_WildSurvival/Code/Runtime/Player", "*.cs", SearchOption.AllDirectories);
            var inventoryScripts = Directory.Exists("Assets/_WildSurvival/Code/Runtime/Survival/Inventory") ?
                Directory.GetFiles("Assets/_WildSurvival/Code/Runtime/Survival/Inventory", "*.cs", SearchOption.AllDirectories) : new string[0];

            report.AppendLine("Phase 1 - Player System:");
            report.AppendLine("├── Controller/");
            report.AppendLine("│   ├── PlayerAnimatorController.cs");
            report.AppendLine("│   ├── PlayerController_OLD.cs");
            report.AppendLine("│   ├── PlayerMovementController.cs");
            report.AppendLine("├── Camera/");
            report.AppendLine("│   ├── ThirdPersonCameraController.cs");
            report.AppendLine("├── Stats/");
            report.AppendLine("│   ├── PlayerStats.cs");

            if (inventoryScripts.Length > 0)
            {
                report.AppendLine("Phase 2 - Inventory System:");
                report.AppendLine($"├── {inventoryScripts.Length} scripts implemented");
            }

            // Scene list
            var scenes = Directory.GetFiles("Assets/_WildSurvival/Scenes", "*.unity", SearchOption.AllDirectories);
            report.AppendLine("Scenes:");
            foreach (var scene in scenes)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scene);
                string folder = Path.GetFileName(Path.GetDirectoryName(scene));
                report.AppendLine($"├── {folder}/{sceneName}.unity");
            }

            // Totals
            var allScripts = Directory.GetFiles("Assets/_WildSurvival/Code", "*.cs", SearchOption.AllDirectories);
            var prefabs = Directory.GetFiles("Assets/_WildSurvival/Prefabs", "*.prefab", SearchOption.AllDirectories);
            report.AppendLine($"Totals: Scripts: {allScripts.Length} | Prefabs: {prefabs.Length} | Scenes: {scenes.Length}");
            report.AppendLine("```");

            // Recent Activity
            report.AppendLine("### 🔄 Recent Activity");
            report.AppendLine("```");
            foreach (var entry in captureHistory)
            {
                report.AppendLine(entry);
            }
            report.AppendLine("```");

            // Current Scene
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            report.AppendLine("### 🎬 Current Scene");
            report.AppendLine("```");
            report.AppendLine($"Name: {activeScene.name}");
            report.AppendLine($"Path: {activeScene.path}");
            report.AppendLine($"GameObjects: {activeScene.rootCount}");

            // Check for player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                report.AppendLine($"✅ Player Found: {player.name}");
                report.AppendLine("Components:");
                foreach (var comp in player.GetComponents<Component>())
                {
                    if (comp != null && comp.GetType() != typeof(Transform))
                    {
                        report.AppendLine($"  - {comp.GetType().Name}");
                    }
                }
            }
            else
            {
                report.AppendLine("❌ No Player GameObject found");
            }
            report.AppendLine("```");

            // Quick Status
            report.AppendLine("### ✅ Quick Status");
            report.AppendLine($"- Errors: {errors.Count}");
            report.AppendLine($"- Warnings: {warnings.Count}");
            report.AppendLine($"- Current Phase: {currentPhase}");
            report.AppendLine($"- Last Capture: {lastCaptureTime:HH:mm:ss}");

            return report.ToString();
        }

        private static string GetCurrentPhase()
        {
            foreach (var phase in PHASE_DATA)
            {
                var info = phase.Value;
                var implemented = CountImplementedFiles(info.RequiredFiles);
                if (implemented < info.RequiredFiles.Length)
                {
                    return phase.Key;
                }
            }
            return "Phase1"; // Default
        }

        private static string GetNextFileToImplement(string[] files)
        {
            foreach (var file in files)
            {
                string[] paths = {
                    $"Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Core/{file}",
                    $"Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Items/{file}",
                    $"Assets/_WildSurvival/Code/Runtime/Survival/Inventory/UI/{file}",
                    $"Assets/_WildSurvival/Code/Runtime/Survival/Inventory/Events/{file}"
                };

                bool isImplemented = false;
                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        var fileInfo = new FileInfo(path);
                        if (fileInfo.Length > 500)
                        {
                            isImplemented = true;
                            break;
                        }
                    }
                }

                if (!isImplemented)
                {
                    return file;
                }
            }
            return null;
        }

        private static List<string> GetConsoleLogs()
        {
            var logs = new List<string>();

            // Access Unity's console logs
            var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            if (logEntriesType != null)
            {
                var getCountMethod = logEntriesType.GetMethod("GetCount",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                if (getCountMethod != null)
                {
                    int count = (int)getCountMethod.Invoke(null, null);
                    if (count > 0)
                    {
                        // Get the actual entries
                        var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal",
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                        if (getEntryMethod != null)
                        {
                            for (int i = 0; i < Math.Min(count, 10); i++) // Get last 10 entries
                            {
                                var entry = new object[] { i, null };
                                getEntryMethod.Invoke(null, entry);

                                if (entry[1] != null)
                                {
                                    var entryData = entry[1];
                                    var condition = entryData.GetType().GetField("condition")?.GetValue(entryData)?.ToString();

                                    if (!string.IsNullOrEmpty(condition))
                                    {
                                        if (condition.Contains("error"))
                                            logs.Add($"[Error] {condition}");
                                        else if (condition.Contains("warning"))
                                            logs.Add($"[Warning] {condition}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Fallback check
            if (EditorUtility.scriptCompilationFailed)
            {
                logs.Add("[Error] Script compilation failed");
            }

            return logs;
        }
    }
}


//using UnityEngine;
//using UnityEditor;
//using UnityEditor.Compilation;
//using System.Text;
//using System.Collections.Generic;
//using System.Linq;
//using System;

//namespace WildSurvival.Editor.Tools
//{
//    /// <summary>
//    /// Captures and formats Unity/VS errors for easy sharing with Claude AI
//    /// </summary>
//    public class ErrorReporter : EditorWindow
//    {
//        private Vector2 scrollPosition;
//        private string capturedErrors = "";
//        private bool includeWarnings = true;
//        private bool includeStackTrace = false;
//        private bool autoFormat = true;
//        private List<LogEntry> logEntries = new List<LogEntry>();

//        private string lastCaptureTime = "";
//        private List<string> recentChanges = new List<string>();

//        private string statusMessage = "";
//        private MessageType statusType = MessageType.Info;

//        private class LogEntry
//        {
//            public string condition;
//            public string stackTrace;
//            public LogType type;
//            public DateTime timestamp;

//            public LogEntry(string condition, string stackTrace, LogType type)
//            {
//                this.condition = condition;
//                this.stackTrace = stackTrace;
//                this.type = type;
//                this.timestamp = DateTime.Now;
//            }
//        }

//        [MenuItem("Tools/Wild Survival/🔧 Error Reporter (for Claude)", priority = 100)]
//        public static void ShowWindow()
//        {
//            var window = GetWindow<ErrorReporter>("Error Reporter");
//            window.minSize = new Vector2(500, 400);
//            window.Show();
//        }

//        private void OnEnable()
//        {
//            // Subscribe to Unity's log messages
//            Application.logMessageReceived += HandleLog;
//            CompilationPipeline.compilationStarted += OnCompilationStarted;
//            CompilationPipeline.compilationFinished += OnCompilationFinished;

//            CaptureCurrentErrors();
//        }

//        private void OnDisable()
//        {
//            Application.logMessageReceived -= HandleLog;
//            CompilationPipeline.compilationStarted -= OnCompilationStarted;
//            CompilationPipeline.compilationFinished -= OnCompilationFinished;
//        }

//        private void HandleLog(string condition, string stackTrace, LogType type)
//        {
//            if (type == LogType.Error || type == LogType.Exception ||
//                type == LogType.Assert || type == LogType.Warning)
//            {
//                logEntries.Add(new LogEntry(condition, stackTrace, type));

//                // Keep only last 100 entries
//                if (logEntries.Count > 100)
//                {
//                    logEntries.RemoveAt(0);
//                }
//            }
//        }

//        private void OnCompilationStarted(object obj)
//        {
//            logEntries.Clear();
//        }

//        private void OnCompilationFinished(object obj)
//        {
//            CaptureCurrentErrors();
//        }

//        private void OnGUI()
//        {
//            DrawHeader();
//            DrawOptions();
//            DrawErrorDisplay();
//            DrawActions();
//        }

//        private void DrawHeader()
//        {
//            EditorGUILayout.Space(5);

//            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
//            {
//                fontSize = 14,
//                alignment = TextAnchor.MiddleCenter
//            };

//            EditorGUILayout.LabelField("🔧 ERROR REPORTER FOR CLAUDE AI", headerStyle);
//            EditorGUILayout.Space(5);

//            EditorGUILayout.HelpBox(
//                "This tool captures Unity compilation errors and formats them for Claude AI.\n" +
//                "Click 'Capture' to get current errors, then 'Copy' to share with Claude.",
//                MessageType.Info
//            );

//            EditorGUILayout.Space(5);
//        }

//        private void DrawOptions()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

//            EditorGUILayout.BeginHorizontal();
//            includeWarnings = EditorGUILayout.Toggle("Include Warnings", includeWarnings);
//            includeStackTrace = EditorGUILayout.Toggle("Include Stack Trace", includeStackTrace);
//            autoFormat = EditorGUILayout.Toggle("Format for Claude", autoFormat);
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();
//            EditorGUILayout.Space(5);
//        }

//        private void DrawErrorDisplay()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField($"Captured Errors ({logEntries.Count})", EditorStyles.boldLabel);

//            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(500), GUILayout.MaxHeight(700));

//            if (string.IsNullOrEmpty(capturedErrors))
//            {
//                EditorGUILayout.LabelField("No errors captured. Click 'Capture Current Errors' below.");
//            }
//            else
//            {
//                // Display in a text area for easy viewing
//                EditorGUILayout.TextArea(capturedErrors, GUILayout.ExpandHeight(true));
//            }

//            EditorGUILayout.EndScrollView();
//            EditorGUILayout.EndVertical();
//        }

//        private void DrawActions()
//        {
//            EditorGUILayout.Space(10);
//            EditorGUILayout.BeginHorizontal();

//            // Capture button
//            GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f);
//            if (GUILayout.Button("📸 Capture Full Status Report", GUILayout.Height(30)))
//            {
//                CaptureCurrentErrors();

//                // Auto-copy to clipboard
//                if (!string.IsNullOrEmpty(capturedErrors))
//                {
//                    GUIUtility.systemCopyBuffer = capturedErrors;
//                    ShowStatus("✅ Status report captured and copied to clipboard!", MessageType.Info);
//                }
//            }

//            // Copy button
//            //GUI.backgroundColor = new Color(0.5f, 0.5f, 0.8f);
//            //if (GUILayout.Button("📋 Copy to Clipboard", GUILayout.Height(30)))
//            //{
//            //    if (!string.IsNullOrEmpty(capturedErrors))
//            //    {
//            //        GUIUtility.systemCopyBuffer = capturedErrors;
//            //        EditorUtility.DisplayDialog("Success",
//            //            "Errors copied to clipboard!\n\nPaste directly to Claude.",
//            //            "OK");
//            //    }
//            //    else
//            //    {
//            //        EditorUtility.DisplayDialog("No Errors",
//            //            "No errors to copy. Click 'Capture' first.",
//            //            "OK");
//            //    }
//            //}

//            // Clear button
//            GUI.backgroundColor = new Color(0.8f, 0.5f, 0.5f);
//            if (GUILayout.Button("🗑️ Clear", GUILayout.Height(30)))
//            {
//                logEntries.Clear();
//                capturedErrors = "";
//            }

//            //if (GUILayout.Button("🌳 Project Tree", GUILayout.Height(30)))
//            //{
//            //    var tree = GenerateProjectTree();
//            //    UnityEngine.Debug.Log(tree);
//            //    EditorUtility.DisplayDialog("Project Tree",
//            //        "Tree copied to clipboard and logged to console", "OK");
//            //    GUIUtility.systemCopyBuffer = tree;
//            //}

//            GUI.backgroundColor = Color.white;
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.Space(5);

//            // Quick actions
//            EditorGUILayout.BeginHorizontal();

//            if (GUILayout.Button("Recompile Scripts", GUILayout.Height(25)))
//            {
//                AssetDatabase.Refresh();
//                CompilationPipeline.RequestScriptCompilation();
//            }

//            if (GUILayout.Button("Clear Console", GUILayout.Height(25)))
//            {
//                var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
//                var clearMethod = logEntries.GetMethod("Clear",
//                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
//                clearMethod.Invoke(null, null);
//            }

//            EditorGUILayout.EndHorizontal();

//            if (!string.IsNullOrEmpty(statusMessage))
//            {
//                EditorGUILayout.Space(5);
//                EditorGUILayout.HelpBox(statusMessage, statusType);
//            }
//        }

//        private string GenerateProjectTree()
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("\n## Project Structure Summary");
//            sb.AppendLine("```");

//            // Count key folders
//            string[] keyPaths = {
//        "Assets/_WildSurvival/Code/Runtime/Player",
//        "Assets/_WildSurvival/Code/Runtime/Core",
//        "Assets/_WildSurvival/Code/Runtime/Survival",
//        "Assets/_WildSurvival/Scenes",
//        "Assets/_WildSurvival/Prefabs"
//    };

//            foreach (var path in keyPaths)
//            {
//                if (AssetDatabase.IsValidFolder(path))
//                {
//                    var files = AssetDatabase.FindAssets("t:Script", new[] { path });
//                    var scenes = AssetDatabase.FindAssets("t:Scene", new[] { path });
//                    var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { path });

//                    sb.AppendLine($"{path}:");
//                    sb.AppendLine($"  Scripts: {files.Length} | Scenes: {scenes.Length} | Prefabs: {prefabs.Length}");
//                }
//            }

//            // Add recent changes if any
//            if (recentChanges.Count > 0)
//            {
//                sb.AppendLine("\nRecent Changes Since Last Capture:");
//                foreach (var change in recentChanges.Take(10))
//                {
//                    sb.AppendLine($"  - {change}");
//                }
//            }

//            sb.AppendLine("```");
//            return sb.ToString();
//        }

//        private void TrackChanges()
//        {
//            // Track what changed since last capture
//            string currentTime = DateTime.Now.ToString("HH:mm:ss");
//            if (!string.IsNullOrEmpty(lastCaptureTime))
//            {
//                // Simple tracking - you could enhance this
//                recentChanges.Add($"Capture at {currentTime} (prev: {lastCaptureTime})");
//            }
//            lastCaptureTime = currentTime;
//        }


//        private void CaptureCurrentErrors()
//        {
//            var sb = new StringBuilder();
//            TrackChanges();

//            // Header
//            if (autoFormat)
//            {
//                sb.AppendLine("## 📊 Unity Project Status Report");
//                sb.AppendLine($"**Timestamp:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
//                sb.AppendLine($"**Unity Version:** {Application.unityVersion}");
//                sb.AppendLine($"**Session:** Wild Survival Migration");
//                sb.AppendLine();
//            }

//            // 1. ERRORS AND WARNINGS SECTION
//            sb.AppendLine("### 🔴 Build Status");
//            sb.AppendLine("```");

//            // Check for compilation errors
//            bool hasErrors = false;

//            // Get Console entries
//            var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
//            if (logEntriesType != null)
//            {
//                var getCount = logEntriesType.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
//                if (getCount != null)
//                {
//                    int errorCount = (int)getCount.Invoke(null, new object[] { });
//                    sb.AppendLine($"Console Entries: {errorCount}");
//                }
//            }

//            // Add log entries if any
//            if (logEntries.Count > 0)
//            {
//                var errors = logEntries.Where(e => e.type == LogType.Error || e.type == LogType.Exception).ToList();
//                var warnings = logEntries.Where(e => e.type == LogType.Warning).ToList();

//                if (errors.Count > 0)
//                {
//                    hasErrors = true;
//                    sb.AppendLine($"\n❌ Errors ({errors.Count}):");
//                    foreach (var entry in errors.Take(10)) // Limit to 10 most recent
//                    {
//                        sb.AppendLine($"  [{entry.timestamp:HH:mm:ss}] {entry.condition}");
//                        if (includeStackTrace && !string.IsNullOrEmpty(entry.stackTrace))
//                        {
//                            var lines = entry.stackTrace.Split('\n');
//                            if (lines.Length > 0 && lines[0].Contains(".cs"))
//                            {
//                                sb.AppendLine($"    at: {lines[0].Trim()}");
//                            }
//                        }
//                    }
//                }

//                if (includeWarnings && warnings.Count > 0)
//                {
//                    sb.AppendLine($"\n⚠️ Warnings ({warnings.Count}):");
//                    foreach (var entry in warnings.Take(5)) // Limit to 5 most recent
//                    {
//                        sb.AppendLine($"  [{entry.timestamp:HH:mm:ss}] {entry.condition}");
//                    }
//                }
//            }

//            if (!hasErrors && logEntries.Count == 0)
//            {
//                sb.AppendLine("✅ No compilation errors detected");
//            }

//            sb.AppendLine("```");

//            // 2. PROJECT STRUCTURE SECTION
//            sb.AppendLine("\n### 📁 Project Structure");
//            sb.AppendLine("```");

//            // Phase 1 Focus
//            string playerPath = "Assets/_WildSurvival/Code/Runtime/Player";
//            if (AssetDatabase.IsValidFolder(playerPath))
//            {
//                sb.AppendLine("Phase 1 - Player System:");

//                string[] folders = { "Controller", "Camera", "Stats", "Interactions" };
//                foreach (var folder in folders)
//                {
//                    string fullPath = $"{playerPath}/{folder}";
//                    if (AssetDatabase.IsValidFolder(fullPath))
//                    {
//                        var scripts = AssetDatabase.FindAssets("t:Script", new[] { fullPath });
//                        if (scripts.Length > 0)
//                        {
//                            sb.AppendLine($"├── {folder}/");
//                            foreach (var guid in scripts)
//                            {
//                                string path = AssetDatabase.GUIDToAssetPath(guid);
//                                string fileName = System.IO.Path.GetFileName(path);
//                                sb.AppendLine($"│   ├── {fileName}");
//                            }
//                        }
//                    }
//                }
//            }

//            // Scenes
//            string scenePath = "Assets/_WildSurvival/Scenes";
//            if (AssetDatabase.IsValidFolder(scenePath))
//            {
//                sb.AppendLine("\nScenes:");
//                var scenes = AssetDatabase.FindAssets("t:Scene", new[] { scenePath });
//                foreach (var guid in scenes.Take(10))
//                {
//                    string path = AssetDatabase.GUIDToAssetPath(guid);
//                    string fileName = System.IO.Path.GetFileName(path);
//                    string folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
//                    sb.AppendLine($"├── {folder}/{fileName}");
//                }
//            }

//            // Summary counts
//            var allScripts = AssetDatabase.FindAssets("t:Script", new[] { "Assets/_WildSurvival" });
//            var allPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_WildSurvival" });
//            var allScenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_WildSurvival" });

//            sb.AppendLine($"\nTotals: Scripts: {allScripts.Length} | Prefabs: {allPrefabs.Length} | Scenes: {allScenes.Length}");
//            sb.AppendLine("```");

//            // 3. RECENT CHANGES SECTION
//            if (recentChanges.Count > 0)
//            {
//                sb.AppendLine("\n### 🔄 Recent Activity");
//                sb.AppendLine("```");
//                foreach (var change in recentChanges.Take(5))
//                {
//                    sb.AppendLine(change);
//                }
//                sb.AppendLine("```");
//            }

//            // 4. CURRENT SCENE INFO
//            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
//            sb.AppendLine("\n### 🎬 Current Scene");
//            sb.AppendLine("```");
//            sb.AppendLine($"Name: {currentScene.name}");
//            sb.AppendLine($"Path: {currentScene.path}");
//            sb.AppendLine($"GameObjects: {currentScene.rootCount}");

//            // Check for Player
//            var player = GameObject.FindGameObjectWithTag("Player");
//            if (player != null)
//            {
//                sb.AppendLine($"\n✅ Player Found: {player.name}");
//                sb.AppendLine("Components:");
//                var components = player.GetComponents<Component>();
//                foreach (var comp in components)
//                {
//                    if (comp != null)
//                        sb.AppendLine($"  - {comp.GetType().Name}");
//                }
//            }
//            else
//            {
//                sb.AppendLine("\n❌ No Player GameObject found (check Tag)");
//            }
//            sb.AppendLine("```");

//            // 5. QUICK STATUS SUMMARY
//            sb.AppendLine("\n### ✅ Quick Status");
//            sb.AppendLine($"- Errors: {logEntries.Count(e => e.type == LogType.Error)}");
//            sb.AppendLine($"- Warnings: {logEntries.Count(e => e.type == LogType.Warning)}");
//            sb.AppendLine($"- Phase 1 Scripts: {(AssetDatabase.FindAssets("t:Script", new[] { playerPath }).Length)} files");
//            sb.AppendLine($"- Last Capture: {lastCaptureTime}");

//            capturedErrors = sb.ToString();
//            Repaint();
//        }
//        private string GenerateDetailedProjectTree()
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("\n## Project Structure");
//            sb.AppendLine("```");
//            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
//            sb.AppendLine("Key Locations:");

//            // Player System
//            string playerPath = "Assets/_WildSurvival/Code/Runtime/Player";
//            if (AssetDatabase.IsValidFolder(playerPath))
//            {
//                sb.AppendLine("\n[Player System]");
//                var controllers = AssetDatabase.FindAssets("t:Script", new[] { $"{playerPath}/Controller" });
//                var camera = AssetDatabase.FindAssets("t:Script", new[] { $"{playerPath}/Camera" });
//                var stats = AssetDatabase.FindAssets("t:Script", new[] { $"{playerPath}/Stats" });

//                sb.AppendLine($"├── Controller: {controllers.Length} scripts");
//                sb.AppendLine($"├── Camera: {camera.Length} scripts");
//                sb.AppendLine($"└── Stats: {stats.Length} scripts");
//            }

//            // Core System
//            string corePath = "Assets/_WildSurvival/Code/Runtime/Core";
//            if (AssetDatabase.IsValidFolder(corePath))
//            {
//                sb.AppendLine("\n[Core System]");
//                var managers = AssetDatabase.FindAssets("t:Script", new[] { $"{corePath}/Managers" });
//                sb.AppendLine($"└── Managers: {managers.Length} scripts");
//            }

//            // Count totals
//            var allScripts = AssetDatabase.FindAssets("t:Script", new[] { "Assets/_WildSurvival" });
//            var allPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_WildSurvival" });
//            var allScenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_WildSurvival" });

//            sb.AppendLine($"\n[Totals]");
//            sb.AppendLine($"Scripts: {allScripts.Length} | Prefabs: {allPrefabs.Length} | Scenes: {allScenes.Length}");

//            sb.AppendLine("```");
//            return sb.ToString();
//        }

//        private void ShowStatus(string message, MessageType type)
//        {
//            statusMessage = message;
//            statusType = type;
//            Repaint();
//        }
//    }


//}