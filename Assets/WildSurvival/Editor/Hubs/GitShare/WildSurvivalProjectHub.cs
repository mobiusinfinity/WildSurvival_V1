// Assets/_Project/Code/Editor/ProjectHub/WildSurvivalProjectHub.cs
// Unity 6 (6000.x) – Editor-only tools for Wild Survival
// One-window project bootstrap: setup, validate, build, export.

#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace WildSurvival.EditorTools
{
    public class WildSurvivalProjectHub : EditorWindow
    {
        private readonly string _projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private int _tab;
        private Vector2 _scroll;
        private string _buildVersion = DateTime.Now.ToString("yyyy.MM.dd-HH.mm");
        private string _exportNote = "public-mirror";
        private bool _devEnterPlayModeOptions = true; // toggle for Setup
        private bool _openFolderAfterBuild = true;

        [MenuItem("Tools/G_5/Project Hub")]
        public static void Open() => GetWindow<WildSurvivalProjectHub>("Wild Survival — Project Hub");

        private void OnGUI()
        {
            _tab = GUILayout.Toolbar(_tab, new[] { "Setup", "Validate", "Builds", "Export", "Utilities" });
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            try
            {
                switch (_tab)
                {
                    case 0:
                        DrawSetup();
                        break;
                    case 1:
                        DrawValidate();
                        break;
                    case 2:
                        DrawBuilds();
                        break;
                    case 3:
                        DrawExport();
                        break;
                    case 4:
                        DrawUtilities();
                        break;
                }
            }
            finally { EditorGUILayout.EndScrollView(); }
        }

        #region Setup
        private void DrawSetup()
        {
            EditorGUILayout.LabelField("Project bootstrap", EditorStyles.boldLabel);
            if (GUILayout.Button("1) Create Standard Folders"))
                EditorApplication.delayCall += () => SetupActions.CreateFolders();
            if (GUILayout.Button("2) Create Assembly Definitions"))
                EditorApplication.delayCall += () => SetupActions.CreateAsmdefs();
            if (GUILayout.Button("3) Create Boot/Persistent Scenes + Add to Build"))
                EditorApplication.delayCall += () => SetupActions.CreateScenes();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Editor & Version Control", EditorStyles.boldLabel);
            _devEnterPlayModeOptions = EditorGUILayout.ToggleLeft("Enable Enter Play Mode Options (fast iteration)", _devEnterPlayModeOptions);
            if (GUILayout.Button("Apply: Force Text + Visible Meta Files (+ Enter Play Mode Options)"))
            {
                bool enterPlay = _devEnterPlayModeOptions;
                EditorApplication.delayCall += () => SetupActions.ApplyEditorDefaults(enterPlay);
            }
            EditorGUILayout.HelpBox("If Visible Meta Files can’t be set programmatically on your Unity version, the tool will open Project Settings so you can set it manually.", MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("Run HDRP Wizard"))
                EditorApplication.delayCall += SetupActions.OpenHDRPWizard;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Addressables (optional, recommended)", EditorStyles.boldLabel);
            if (GUILayout.Button("Detect Addressables + Open Groups Window"))
                EditorApplication.delayCall += SetupActions.OpenAddressablesOrWarn;
        }
        #endregion

        #region Validate
        private void DrawValidate()
        {
            if (GUILayout.Button("Run All Validators"))
                EditorApplication.delayCall += Validators.RunAll;

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Show Current Snapshot (quick)"))
                EditorApplication.delayCall += Validators.DumpQuickSnapshot;
        }
        #endregion

        #region Builds
        private void DrawBuilds()
        {
            EditorGUILayout.LabelField("Build Version", EditorStyles.boldLabel);
            _buildVersion = EditorGUILayout.TextField("Version Stamp", _buildVersion);

            _openFolderAfterBuild = EditorGUILayout.ToggleLeft("Open build folder when done", _openFolderAfterBuild);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Windows – Development (Mono)", EditorStyles.boldLabel);
            if (GUILayout.Button("Build Windows (Development, Mono)"))
            {
                string v = _buildVersion;
                bool open = _openFolderAfterBuild;
                EditorApplication.delayCall += () => BuildActions.BuildWindowsDev(v, open);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Windows – Release (IL2CPP, x64)", EditorStyles.boldLabel);
            if (GUILayout.Button("Build Windows (Release, IL2CPP)"))
            {
                string v = _buildVersion;
                bool open = _openFolderAfterBuild;
                EditorApplication.delayCall += () => BuildActions.BuildWindowsRelease(v, open);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Open Build Settings"))
                EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem("File/Build Settings...");
        }
        #endregion

        #region Export
        private void DrawExport()
        {
            EditorGUILayout.LabelField("Public Mirror Export", EditorStyles.boldLabel);
            _exportNote = EditorGUILayout.TextField("Label/Note", _exportNote);
            EditorGUILayout.HelpBox("Creates a zip with selected public content (Assets/_Project, ProjectSettings, Packages/manifest.json) to Builds/PublicMirror/. The ZIP is written OUTSIDE the source folder (no self-zip).", MessageType.None);

            if (GUILayout.Button("Export Public Mirror (.zip)"))
            {
                string label = _exportNote;
                EditorApplication.delayCall += () => ExportActions.ExportPublicMirror(label);
            }
            if (GUILayout.Button("Open Export Folder"))
            {
                var path = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Builds", "PublicMirror");
                Directory.CreateDirectory(path);
                EditorUtility.RevealInFinder(path);
            }
        }
        #endregion

        #region Utilities
        private void DrawUtilities()
        {
            EditorGUILayout.LabelField("Shortcuts", EditorStyles.boldLabel);
            if (GUILayout.Button("Project Settings"))
                EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
            if (GUILayout.Button("Quality Settings"))
                EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem("Edit/Project Settings/Quality");
            if (GUILayout.Button("Graphics Settings"))
                EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem("Edit/Project Settings/Graphics");

            EditorGUILayout.Space();
            if (GUILayout.Button("Open _Project folder"))
            {
                var path = Path.Combine(Application.dataPath, "_Project");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                EditorUtility.RevealInFinder(path);
            }

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Show Log: Unity/Platform/ColorSpace/ScriptingBackend snapshot"))
                EditorApplication.delayCall += Validators.DumpQuickSnapshot;
        }
        #endregion
    }

    // ----------------------------- SETUP ACTIONS -----------------------------
    internal static class SetupActions
    {
        public static void CreateFolders()
        {
            string[] paths = {
                "Assets/_Project",
                "Assets/_Project/Art",
                "Assets/_Project/Audio",
                "Assets/_Project/Code/Runtime/Core",
                "Assets/_Project/Code/Runtime/Systems",
                "Assets/_Project/Code/Runtime/Gameplay",
                "Assets/_Project/Code/Runtime/Rendering",
                "Assets/_Project/Code/Editor/ProjectHub",
                "Assets/_Project/Code/Editor/Validators",
                "Assets/_Project/Data/Items",
                "Assets/_Project/Data/Recipes",
                "Assets/_Project/Data/Workstations",
                "Assets/_Project/Data/StatusEffects",
                "Assets/_Project/Data/Biomes",
                "Assets/_Project/Scenes/Levels",
                "Assets/_Project/Scenes/Sandbox",
                "Assets/_Project/UI/Documents",
                "Assets/_ThirdParty",
                "Assets/_Samples"
            };
            foreach (var p in paths)
                if (!AssetDatabase.IsValidFolder(p))
                    Directory.CreateDirectory(p);
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[ProjectHub] Folders created/verified.");
        }

        public static void CreateAsmdefs()
        {
            // Editor-only asmdef
            AsmdefWriter.Create("Assets/_Project/Code/Editor/ProjectHub", "WildSurvival.Project.Editor",
                references: new[] { "UnityEditor", "UnityEngine.CoreModule" },
                includeEditorOnly: true);
            AsmdefWriter.Create("Assets/_Project/Code/Runtime/Core", "WildSurvival.Project.Core");
            AsmdefWriter.Create("Assets/_Project/Code/Runtime/Systems", "WildSurvival.Project.Systems",
                references: new[] { "WildSurvival.Project.Core", "Unity.InputSystem" });
            AsmdefWriter.Create("Assets/_Project/Code/Runtime/Gameplay", "WildSurvival.Project.Gameplay",
                references: new[] { "WildSurvival.Project.Core", "WildSurvival.Project.Systems" });
            AsmdefWriter.Create("Assets/_Project/Code/Runtime/Rendering", "WildSurvival.Project.Rendering",
                references: new[] { "WildSurvival.Project.Core" });
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[ProjectHub] Assembly Definitions created/verified.");
        }

        public static void CreateScenes()
        {
            string scenesRoot = "Assets/_Project/Scenes";
            Directory.CreateDirectory(scenesRoot);
            CreateSceneIfMissing(Path.Combine(scenesRoot, "_Boot.unity"));
            CreateSceneIfMissing(Path.Combine(scenesRoot, "_Persistent.unity"));

            var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(Path.Combine(scenesRoot, "_Boot.unity"));
            var persist = AssetDatabase.LoadAssetAtPath<SceneAsset>(Path.Combine(scenesRoot, "_Persistent.unity"));

            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            void Add(SceneAsset s)
            {
                string sp = AssetDatabase.GetAssetPath(s);
                if (!list.Any(x => x.path == sp))
                    list.Insert(0, new EditorBuildSettingsScene(sp, true));
            }
            Add(persist);
            Add(boot);
            EditorBuildSettings.scenes = list.ToArray();
            UnityEngine.Debug.Log("[ProjectHub] Boot/Persistent scenes created and added to Build Settings.");
        }

        private static void CreateSceneIfMissing(string path)
        {
            if (File.Exists(path))
                return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
        }

        public static void ApplyEditorDefaults(bool enableEnterPlayModeOptions)
        {
            // Serialization: Force Text
            EditorSettings.serializationMode = SerializationMode.ForceText;
            // Enter Play Mode options (fast iteration)
            EditorSettings.enterPlayModeOptionsEnabled = enableEnterPlayModeOptions;

            // Try to set Visible Meta Files via reflection (property name differs across versions)
            try
            {
                var t = typeof(EditorSettings);
                var prop = t.GetProperty("externalVersionControl", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(null, "Visible Meta Files", null);
                    UnityEngine.Debug.Log("[ProjectHub] Version Control set to Visible Meta Files.");
                }
                else
                {
                    // Open Project Settings for manual switch
                    EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                    UnityEngine.Debug.LogWarning("[ProjectHub] Could not set Visible Meta Files via API; opened Project Settings for manual change.");
                }
            }
            catch (Exception ex)
            {
                EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                UnityEngine.Debug.LogWarning("[ProjectHub] Could not set Visible Meta Files via API; opened Project Settings. Details: " + ex);
            }

            UnityEngine.Debug.Log("[ProjectHub] Editor defaults applied: Force Text, Enter Play Mode Options=" + enableEnterPlayModeOptions);
        }

        public static void OpenHDRPWizard()
            => EditorApplication.ExecuteMenuItem("Window/Rendering/HDRP Wizard");

        public static void OpenAddressablesOrWarn()
        {
            var type = Type.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
            if (type == null)
            {
                EditorUtility.DisplayDialog("Addressables not found",
                    "The Addressables package isn't installed. Open Window → Package Manager and install 'Addressables'.",
                    "OK");
                return;
            }
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        }
    }

    // ----------------------------- VALIDATORS -----------------------------
    internal static class Validators
    {
        public static void RunAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Wild Survival — Validation Report ===");

            // Unity / Platform snapshot
            sb.AppendLine($"Unity: {Application.unityVersion}");
            sb.AppendLine($"Platform: {EditorUserBuildSettings.activeBuildTarget}");
            sb.AppendLine($"Color Space: {PlayerSettings.colorSpace}");

            // API Compatibility
            try
            {
#if UNITY_2021_2_OR_NEWER
                var api = PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone);
                sb.AppendLine($"API Compatibility (Standalone): {api}");
                if (api.ToString().Contains("2_0"))
                    sb.AppendLine("⚠️ Recommend upgrading API Compatibility to .NET Standard 2.1 (Player Settings → Other Settings).");
#endif
            }
            catch { }

            // Scripting Backend
            try
            {
                var back = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone);
                sb.AppendLine($"Scripting Backend (Standalone): {back}");
                if (back == ScriptingImplementation.Mono2x)
                    sb.AppendLine("ℹ️ Dev builds can use Mono; switch to IL2CPP for Steam release.");
            }
            catch { }

            // Anisotropic Filtering (prefer PerTexture)
            sb.AppendLine($"Anisotropic Filtering: {QualitySettings.anisotropicFiltering}");
            if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.ForceEnable)
                sb.AppendLine("ℹ️ Consider 'PerTexture' for better control and perf balance.");

            // Scenes sanity
            var boot = AssetDatabase.FindAssets("t:Scene _Boot").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.EndsWith("_Boot.unity"));
            var persist = AssetDatabase.FindAssets("t:Scene _Persistent").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.EndsWith("_Persistent.unity"));
            if (string.IsNullOrEmpty(boot) || string.IsNullOrEmpty(persist))
                sb.AppendLine("❌ Missing _Boot/_Persistent scenes. Use Setup → Create Boot/Persistent Scenes.");

            // Check build scenes count (from your report was 2) and flag if still default.  :contentReference[oaicite:2]{index=2}
            if (EditorBuildSettings.scenes.Length <= 2)
                sb.AppendLine("ℹ️ Only a couple scenes in Build Settings — ensure your additive Level_* scenes are included as you grow.");

            // Volume presence
            bool anyVolume = UnityEngine.Object.FindObjectsOfType<LightProbeProxyVolume>().Any();
            sb.AppendLine("Volumes in open scenes: " + (anyVolume ? "yes" : "not detected (open a level scene to validate)"));

            // Input System presence (package check)
            bool inputInstalled = File.ReadAllText(Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Packages", "manifest.json"))
                .Contains("com.unity.inputsystem");
            sb.AppendLine("Input System package: " + (inputInstalled ? "installed" : "not found (install com.unity.inputsystem)"));

            // Addressables presence (type reflection)
            var addr = Type.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
            sb.AppendLine("Addressables: " + (addr != null ? "installed" : "not installed"));

            UnityEngine.Debug.Log(sb.ToString());
        }

        public static void DumpQuickSnapshot()
        {
            UnityEngine.Debug.Log($"[Snapshot] Unity {Application.unityVersion} | Target {EditorUserBuildSettings.activeBuildTarget} | ColorSpace {PlayerSettings.colorSpace} | ScriptingBackend {PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone)}");
        }
    }

    // ----------------------------- BUILD ACTIONS -----------------------------
    internal static class BuildActions
    {
        private static string BuildsRoot => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Builds");

        public static void BuildWindowsDev(string version, bool reveal)
        {
            var target = BuildTarget.StandaloneWindows64;
            var group = BuildTargetGroup.Standalone;

            // Dev configuration: Mono, fast iteration
            PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.Mono2x);
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetApiCompatibilityLevel(group, ApiCompatibilityLevel.NET_Standard_2_0);
#endif
            PlayerSettings.graphicsJobs = false; // safer default during iteration
            PlayerSettings.gcIncremental = true;

            var outDir = Path.Combine(BuildsRoot, "WindowsDev", version);
            Directory.CreateDirectory(outDir);
            var exePath = Path.Combine(outDir, "WildSurvival.exe");

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                locationPathName = exePath,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(opts);
            LogBuildReport(report);

            if (reveal && Directory.Exists(outDir))
                EditorUtility.RevealInFinder(outDir);
        }

        public static void BuildWindowsRelease(string version, bool reveal)
        {
            var target = BuildTarget.StandaloneWindows64;
            var group = BuildTargetGroup.Standalone;

            // Release configuration: IL2CPP, x64, Incremental GC, Medium stripping
            PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.IL2CPP);
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetApiCompatibilityLevel(group, ApiCompatibilityLevel.NET_Standard_2_0);
#endif
            PlayerSettings.gcIncremental = true;
            PlayerSettings.SetManagedStrippingLevel(group, ManagedStrippingLevel.Medium);

            var outDir = Path.Combine(BuildsRoot, "WindowsRelease", version);
            Directory.CreateDirectory(outDir);
            var exePath = Path.Combine(outDir, "WildSurvival.exe");

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                locationPathName = exePath,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            LogBuildReport(report);

            if (reveal && Directory.Exists(outDir))
                EditorUtility.RevealInFinder(outDir);
        }

        private static void LogBuildReport(UnityEditor.Build.Reporting.BuildReport report)
        {
            var result = report.summary.result;
            var sizeMb = (report.summary.totalSize / (1024f * 1024f)).ToString("0.0");
            UnityEngine.Debug.Log($"[Build] {result} | {report.summary.outputPath} | {sizeMb} MB | duration {report.summary.totalTime}");
            if (result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                UnityEngine.Debug.LogError("[Build] Build failed. Check console for details.");
        }
    }

    // ----------------------------- EXPORT ACTIONS -----------------------------
    internal static class ExportActions
    {
        public static void ExportPublicMirror(string note)
        {
            // Stage a curated subset (Assets/_Project, ProjectSettings, Packages/manifest.json) into Temp, then zip OUTSIDE source.
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var staging = Path.Combine(Path.GetTempPath(), $"WS_PublicMirror_{ts}");
            var exportRoot = Path.Combine(projectRoot, "Builds", "PublicMirror");
            Directory.CreateDirectory(exportRoot);

            try
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, true);
                Directory.CreateDirectory(staging);

                // Copy folders/files
                CopyTree(Path.Combine(projectRoot, "Assets", "_Project"), Path.Combine(staging, "Assets", "_Project"));
                CopyTree(Path.Combine(projectRoot, "ProjectSettings"), Path.Combine(staging, "ProjectSettings"));
                SafeCopy(Path.Combine(projectRoot, "Packages", "manifest.json"), Path.Combine(staging, "Packages", "manifest.json"));

                // Exclusions typical for mirrors
                // (Nothing from Library/Temp/Logs/Obj is copied)

                // Create zip
                var zipName = $"PublicMirror_{ts}_{Sanitize(note)}.zip";
                var zipPath = Path.Combine(exportRoot, zipName);
                ZipUtils.CreateZipFromFolder(staging, zipPath);

                EditorUtility.RevealInFinder(exportRoot);
                UnityEngine.Debug.Log($"[PublicMirror] Exported: {zipPath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[PublicMirror] Export failed: " + ex);
            }
            finally
            {
                // Best-effort cleanup
                try
                { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
                catch { }
            }
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "export";
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim();
        }

        private static void SafeCopy(string src, string dst)
        {
            if (!File.Exists(src))
                return;
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(src, dst, overwrite: true);
        }

        private static void CopyTree(string srcDir, string dstDir)
        {
            if (!Directory.Exists(srcDir))
                return;
            foreach (var dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
            {
                var rel = dir.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(dstDir, rel));
            }

            foreach (var file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
            {
                // Skip .meta? Keep .meta for Assets; safe to include for ProjectSettings
                var rel = file.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var dst = Path.Combine(dstDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                File.Copy(file, dst, overwrite: true);
            }
        }
    }

    // ----------------------------- ZIP UTILS (robust) -----------------------------
    internal static class ZipUtils
    {
        public static void CreateZipFromFolder(string sourceDir, string destZip)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destZip)!);

            var srcFull = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var zipFull = Path.GetFullPath(destZip);

            if (zipFull.StartsWith(srcFull, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"Destination ZIP must be outside the source directory.\nsource: {srcFull}\ndest: {zipFull}");

            TryDeleteWithRetries(destZip, 6, 200);

            var tempZip = destZip + ".tmp";
            TryDeleteWithRetries(tempZip, 2, 100);

            ZipFile.CreateFromDirectory(sourceDir, tempZip, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);
            TryMoveWithRetries(tempZip, destZip, 6, 250);
        }

        private static void TryDeleteWithRetries(string path, int attempts, int delayMs)
        {
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    if (File.Exists(path))
                        UnityEditor.FileUtil.DeleteFileOrDirectory(path);
                    return;
                }
                catch (IOException) { Thread.Sleep(delayMs); }
                catch (UnauthorizedAccessException) { Thread.Sleep(delayMs); }
            }
        }

        private static void TryMoveWithRetries(string src, string dst, int attempts, int delayMs)
        {
            for (int i = 0; i < attempts; i++)
            {
                try
                { File.Move(src, dst); return; }
                catch (IOException) { Thread.Sleep(delayMs); }
                catch (UnauthorizedAccessException) { Thread.Sleep(delayMs); }
            }
            File.Move(src, dst);
        }
    }

    // ----------------------------- ASMDEF WRITER -----------------------------
    internal static class AsmdefWriter
    {
        [Serializable]
        private class Asmdef
        {
            public string name;
            public string[] references = Array.Empty<string>();
            public string[] includePlatforms = Array.Empty<string>();
            public string[] excludePlatforms = Array.Empty<string>();
            public bool allowUnsafeCode = false;
            public bool overrideReferences = false;
            public string[] precompiledReferences = Array.Empty<string>();
            public bool autoReferenced = true;
            public string[] defineConstraints = Array.Empty<string>();
            public VersionDefine[] versionDefines = Array.Empty<VersionDefine>();
            public bool noEngineReferences = false;
        }

        [Serializable]
        private class VersionDefine { public string name; public string expression; public string define; }

        public static void Create(string folder, string name, string[] references = null, bool includeEditorOnly = false)
        {
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, name + ".asmdef");
            if (File.Exists(path))
                return;

            var a = new Asmdef { name = name };
            if (references != null)
                a.references = references;
            if (includeEditorOnly)
                a.includePlatforms = new[] { "Editor" };

            File.WriteAllText(path, EditorJsonUtility.ToJson(a, true));
        }
    }
}
#endif
