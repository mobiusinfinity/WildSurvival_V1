// Assets/_Project/Code/Editor/Share/WildSurvivalShareHub.cs
// Unity 6 – Editor-only: SharePackage++ (merged docs, settings, input, CM, HDRP, diff) + Mirror All Code with YAML option + privacy filter.
// Safe for older C# versions; uses reflection for optional packages; no Addressables dependency.

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO.Compression; // use fully qualified CompressionLevel when calling
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace WildSurvival.EditorTools
{
    public class WildSurvivalShareHub : EditorWindow
    {
        [MenuItem("Tools/G_5/Share & Mirror")]
        public static void Open() { GetWindow<WildSurvivalShareHub>("Wild Survival — Share & Mirror"); }

        Vector2 _scroll;
        // Share options
        string _label = "share";
        int _maxDocKB = 512;
        bool _includeCollabAndReports = true;
        bool _includeProjSettings = true;
        bool _includePackagesLock = true;
        bool _zipAfterShare = true;
        bool _generateExtras = true; // settings/input/CM/HDRP + diff
        bool _useFilter = true;

        // Mirror options
        bool _includeMetas = true;
        bool _includeYamlAssets = true; // .unity/.prefab/.mat/.asset/.anim/.controller/.overrideController

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            try
            {
                EditorGUILayout.LabelField("SharePackage (docs + snapshot + code index + extras)", EditorStyles.boldLabel);
                _label = EditorGUILayout.TextField("Label", _label);
                _maxDocKB = EditorGUILayout.IntSlider("Max per-doc size (KB)", _maxDocKB, 64, 4096);
                _includeCollabAndReports = EditorGUILayout.ToggleLeft("Include Collab & *Report* docs", _includeCollabAndReports);
                _includeProjSettings = EditorGUILayout.ToggleLeft("Include ProjectSettings snapshot", _includeProjSettings);
                _includePackagesLock = EditorGUILayout.ToggleLeft("Include packages-lock.json (if present)", _includePackagesLock);
                _generateExtras = EditorGUILayout.ToggleLeft("Generate extras (UnitySettings / Input / Cinemachine / HDRP / Diff)", _generateExtras);
                _useFilter = EditorGUILayout.ToggleLeft("Apply ShareFilter.txt (privacy / noise control)", _useFilter);
                _zipAfterShare = EditorGUILayout.ToggleLeft("Create SharePackage .zip", _zipAfterShare);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open/Create ShareFilter.txt"))
                        FilterActions.OpenOrCreateFilter();
                    if (GUILayout.Button("Build SharePackage"))
                    {
                        ShareOptions opts = new ShareOptions();
                        opts.Label = _label;
                        opts.MaxDocBytes = _maxDocKB * 1024;
                        opts.IncludeCollabAndReports = _includeCollabAndReports;
                        opts.IncludeProjectSettings = _includeProjSettings;
                        opts.IncludePackagesLock = _includePackagesLock;
                        opts.ZipAfterBuild = _zipAfterShare;
                        opts.GenerateExtras = _generateExtras;
                        opts.UseFilter = _useFilter;

                        EditorApplication.delayCall += () => ShareActions.BuildSharePackage(opts);
                    }
                }

                EditorGUILayout.Space(16);
                EditorGUILayout.LabelField("Improved Public Mirror (All Code)", EditorStyles.boldLabel);
                _includeMetas = EditorGUILayout.ToggleLeft("Include .meta files for copied code/assets", _includeMetas);
                _includeYamlAssets = EditorGUILayout.ToggleLeft("Include text YAML assets (.unity/.prefab/.mat/.asset/.anim/.controller…)", _includeYamlAssets);
                _useFilter = EditorGUILayout.ToggleLeft("Apply ShareFilter.txt", _useFilter);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Export Public Mirror — All Code (ZIP)"))
                    {
                        MirrorOptions m = new MirrorOptions();
                        m.IncludeMeta = _includeMetas;
                        m.IncludeYamlTextAssets = _includeYamlAssets;
                        m.UseFilter = _useFilter;
                        EditorApplication.delayCall += () => MirrorActions.ExportPublicMirrorAllCode(m);
                    }
                    if (GUILayout.Button("Open Builds Folder"))
                        EditorUtility.RevealInFinder(BuildPaths.BuildsRoot);
                }
            }
            finally { EditorGUILayout.EndScrollView(); }
        }
    }

    // ---------------- PATHS ----------------
    internal static class BuildPaths
    {
        public static string ProjectRoot { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
        public static string BuildsRoot { get { return Path.Combine(ProjectRoot, "Builds"); } }
        public static string ShareRoot { get { return Path.Combine(BuildsRoot, "SharePackage"); } }
        public static string MirrorRoot { get { return Path.Combine(BuildsRoot, "PublicMirror"); } }
    }

    // ---------------- FILTER ACTIONS ----------------
    internal static class FilterActions
    {
        // default filter locations (first existing wins)
        public static readonly string[] FilterCandidates = new[]
        {
            Path.Combine(BuildPaths.ProjectRoot, "ShareFilter.txt"),
            Path.Combine(BuildPaths.ProjectRoot, "Assets", "_Project", "ShareFilter.txt"),
            Path.Combine(BuildPaths.ShareRoot, "ShareFilter.txt"),
        };

        public static string GetFilterPathToUse()
        {
            foreach (var p in FilterCandidates)
                if (File.Exists(p))
                    return p;
            return FilterCandidates[0]; // default create at project root
        }

        public static void OpenOrCreateFilter()
        {
            string p = GetFilterPathToUse();
            if (!File.Exists(p))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(p));
                File.WriteAllText(p,
@"# ShareFilter.txt — glob patterns to EXCLUDE from SharePackage/Mirror (one per line)
# Lines starting with # are comments.
# Examples:
# Assets/**/Secrets/**
# Assets/**/AddressablesBuild/**
# Assets/**/Art/**    # (if you only want code)
# *.psd
");
            }
            EditorUtility.OpenWithDefaultApp(p);
        }

        public static GlobFilter Load()
        {
            string p = GetFilterPathToUse();
            return GlobFilter.FromFileOrEmpty(p);
        }
    }

    // Simple glob filter (supports *, ?, ** for path segments)
    internal class GlobFilter
    {
        readonly List<Regex> _rules = new List<Regex>(); // exclude rules

        public bool IsEmpty { get { return _rules.Count == 0; } }

        public static GlobFilter FromFileOrEmpty(string path)
        {
            GlobFilter f = new GlobFilter();
            try
            {
                if (!File.Exists(path))
                    return f;
                foreach (var raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                        continue;
                    f._rules.Add(new Regex("^" + GlobToRegex(line) + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
                }
            }
            catch { /* ignore */ }
            return f;
        }

        public bool Excludes(string fullPath)
        {
            string rel = MakeRelative(fullPath).Replace('\\', '/');
            for (int i = 0; i < _rules.Count; i++)
                if (_rules[i].IsMatch(rel))
                    return true;
            return false;
        }

        static string MakeRelative(string full)
        {
            string root = BuildPaths.ProjectRoot.TrimEnd(Path.DirectorySeparatorChar);
            full = Path.GetFullPath(full);
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return full.Substring(root.Length + 1);
            return full;
        }

        // very small glob -> regex
        static string GlobToRegex(string glob)
        {
            // support ** (any depth), * (any within segment), ? (single), and escape dots
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < glob.Length; i++)
            {
                char c = glob[i];
                if (c == '.')
                { sb.Append(@"\."); continue; }
                if (c == '?')
                { sb.Append('.'); continue; }
                if (c == '*')
                {
                    bool isDouble = (i + 1 < glob.Length && glob[i + 1] == '*');
                    if (isDouble)
                    { sb.Append(".*"); i++; }
                    else
                        sb.Append(@"[^/]*");
                    continue;
                }
                if ("+()^$|{}".IndexOf(c) >= 0)
                { sb.Append('\\').Append(c); continue; }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }

    // ---------------- SHARE OPTIONS ----------------
    internal struct ShareOptions
    {
        public string Label;
        public int MaxDocBytes;
        public bool IncludeCollabAndReports;
        public bool IncludeProjectSettings;
        public bool IncludePackagesLock;
        public bool ZipAfterBuild;
        public bool GenerateExtras;
        public bool UseFilter;
    }

    // ---------------- SHARE ACTIONS ----------------
    internal static class ShareActions
    {
        static readonly string[] RootDocNames = { "README", "CHANGELOG", "LICENSE", "CONTRIBUTING" };
        static readonly string[] TextExts = { ".md", ".txt", ".log", ".json", ".yml", ".yaml", ".xml", ".csv" };

        internal static readonly HashSet<string> CodeExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",".hlsl",".cginc",".compute",".shader",".uss",".uxml",".asmdef",".asmref",
            ".json",".yml",".yaml",".xml"
        };

        public static void BuildSharePackage(ShareOptions o)
        {
            GlobFilter filter = o.UseFilter ? FilterActions.Load() : new GlobFilter();

            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outDir = Path.Combine(BuildPaths.ShareRoot, ts);
            Directory.CreateDirectory(outDir);

            try
            {
                // 1) Mega snapshot doc
                string snapshotPath = Path.Combine(outDir, "SharePackage_Snapshot_" + Sanitize(o.Label) + ".md");
                StringBuilder sb = new StringBuilder(64 * 1024);

                sb.AppendLine("# Wild Survival — SharePackage Snapshot");
                sb.AppendLine();
                sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("Unity: " + Application.unityVersion + " | Target: " + EditorUserBuildSettings.activeBuildTarget + " | Color Space: " + PlayerSettings.colorSpace);
                sb.AppendLine();

                AppendProjectSummary(sb);
                AppendManifestSection(sb);

                AppendRootDocs(sb, o.MaxDocBytes, filter);
                if (o.IncludeCollabAndReports)
                    AppendCollabAndReports(sb, o.MaxDocBytes, filter);
                if (o.IncludeProjectSettings)
                    AppendProjectSettingsBits(sb, o.MaxDocBytes);

                File.WriteAllText(snapshotPath, sb.ToString(), new UTF8Encoding(false));
                UnityEngine.Debug.Log("[SharePackage] Wrote: " + snapshotPath);

                // 2) Code index CSV (respects filter)
                string codeIndexPath = Path.Combine(outDir, "CodeIndex.csv");
                BuildCodeIndexCsv(codeIndexPath, filter);
                UnityEngine.Debug.Log("[SharePackage] Wrote: " + codeIndexPath);

                // 3) Tree.txt (respects filter)
                string treePath = Path.Combine(outDir, "Tree.txt");
                WriteTree(treePath, "Assets", filter);
                UnityEngine.Debug.Log("[SharePackage] Wrote: " + treePath);

                // 4) Copy manifest/lock + ProjectVersion
                SafeCopy(Path.Combine(BuildPaths.ProjectRoot, "Packages", "manifest.json"), Path.Combine(outDir, "Packages", "manifest.json"));
                if (o.IncludePackagesLock)
                    SafeCopy(Path.Combine(BuildPaths.ProjectRoot, "Packages", "packages-lock.json"), Path.Combine(outDir, "Packages", "packages-lock.json"));
                if (o.IncludeProjectSettings)
                    SafeCopy(Path.Combine(BuildPaths.ProjectRoot, "ProjectSettings", "ProjectVersion.txt"), Path.Combine(outDir, "ProjectSettings", "ProjectVersion.txt"));

                // 5) Extras
                if (o.GenerateExtras)
                {
                    SettingsDump.WriteUnitySettingsJson(outDir);
                    ExtrasDump.TryWriteInputActionsJson(outDir);     // reflection; safe if not installed
                    ExtrasDump.TryWriteCinemachineSummary(outDir);    // reflection; safe if not installed
                    ExtrasDump.WriteVolumeProfilesSummary(outDir);    // uses VolumeProfile via reflection for components
                    DiffActions.WriteDiffAgainstPrevious(outDir);     // compares CodeIndex.csv to previous SharePackage
                }

                EditorUtility.RevealInFinder(outDir);

                // 6) Zip
                if (o.ZipAfterBuild)
                {
                    string zip = Path.Combine(BuildPaths.ShareRoot, "SharePackage_" + ts + "_" + Sanitize(o.Label) + ".zip");
                    ZipUtils.CreateZipFromFolder(outDir, zip);
                    UnityEngine.Debug.Log("[SharePackage] Zipped: " + zip);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[SharePackage] Failed: " + ex);
            }
        }

        static void AppendProjectSummary(StringBuilder sb)
        {
            sb.AppendLine("## Project Summary");
            sb.AppendLine();
            sb.AppendLine("- Company: " + PlayerSettings.companyName);
            sb.AppendLine("- Product: " + PlayerSettings.productName);
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            sb.AppendLine("- Scripting Backend (Standalone): " + PlayerSettings.GetScriptingBackend(group));
#if UNITY_2021_2_OR_NEWER
            sb.AppendLine("- API Compatibility (Standalone): " + PlayerSettings.GetApiCompatibilityLevel(group));
#endif
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            sb.AppendLine("- Scenes in Build: " + scenes.Length);
            for (int i = 0; i < scenes.Length; i++)
                sb.AppendLine("  - " + scenes[i]);
            sb.AppendLine();
        }

        static void AppendManifestSection(StringBuilder sb)
        {
            string manifestPath = Path.Combine(BuildPaths.ProjectRoot, "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
                return;

            sb.AppendLine("## Packages (manifest.json)");
            sb.AppendLine();
            string json = Truncate(File.ReadAllText(manifestPath), 64 * 1024);
            sb.AppendLine("```json");
            sb.AppendLine(json);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        static void AppendRootDocs(StringBuilder sb, int maxBytes, GlobFilter filter)
        {
            string root = BuildPaths.ProjectRoot;
            List<string> files = new List<string>();

            foreach (string name in RootDocNames)
            {
                var hits = Directory.GetFiles(root, name + ".*", SearchOption.TopDirectoryOnly);
                if (hits != null && hits.Length > 0)
                    files.AddRange(hits);
            }
            var md = Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly);
            if (md != null)
                files.AddRange(md);
            var txt = Directory.GetFiles(root, "*.txt", SearchOption.TopDirectoryOnly);
            if (txt != null)
                files.AddRange(txt);

            files = files.Distinct().Where(IsTextFile).Where(p => !filter.Excludes(p)).ToList();
            if (files.Count == 0)
                return;

            sb.AppendLine("## Root Documents");
            sb.AppendLine();
            foreach (string f in files)
                AppendDocSection(sb, f, maxBytes);
        }

        static void AppendCollabAndReports(StringBuilder sb, int maxBytes, GlobFilter filter)
        {
            string assets = Path.Combine(BuildPaths.ProjectRoot, "Assets");
            List<string> candidates = new List<string>();

            foreach (string p in Directory.GetFiles(assets, "*", SearchOption.AllDirectories))
            {
                if (filter.Excludes(p))
                    continue;
                string fn = Path.GetFileName(p);
                string dir = Path.GetDirectoryName(p) ?? "";
                bool looksText = IsTextFile(p);
                bool looksCollab = dir.IndexOf("collab", StringComparison.OrdinalIgnoreCase) >= 0;
                bool looksReport = fn.IndexOf("report", StringComparison.OrdinalIgnoreCase) >= 0 || fn.IndexOf("snapshot", StringComparison.OrdinalIgnoreCase) >= 0 || fn.IndexOf("log", StringComparison.OrdinalIgnoreCase) >= 0;
                if (looksText && (looksCollab || looksReport))
                    candidates.Add(p);
            }

            candidates = candidates.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            if (candidates.Count == 0)
                return;

            sb.AppendLine("## Collab & Reports (merged)");
            sb.AppendLine();
            foreach (string f in candidates)
                AppendDocSection(sb, f, maxBytes);
        }

        static void AppendProjectSettingsBits(StringBuilder sb, int maxBytes)
        {
            string p = Path.Combine(BuildPaths.ProjectRoot, "ProjectSettings", "ProjectVersion.txt");
            if (File.Exists(p))
            {
                sb.AppendLine("## ProjectSettings / ProjectVersion.txt");
                sb.AppendLine();
                AppendDocSection(sb, p, maxBytes);
            }
        }

        static void AppendDocSection(StringBuilder sb, string path, int maxBytes)
        {
            try
            {
                string rel = MakeRelative(path);
                sb.AppendLine("### " + rel);
                sb.AppendLine();
                bool truncated;
                string text = ReadTextSafe(path, maxBytes, out truncated);
                string fence = GuessFence(path);
                sb.AppendLine("```" + fence);
                sb.AppendLine(text);
                sb.AppendLine("```");
                if (truncated)
                    sb.AppendLine("_…truncated to " + (maxBytes / 1024) + " KB for brevity_");
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine("> (Could not read " + path + ": " + ex.Message + ")");
            }
        }

        static string GuessFence(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".json")
                return "json";
            if (ext == ".yml" || ext == ".yaml")
                return "yaml";
            if (ext == ".xml")
                return "xml";
            return "";
        }

        static string MakeRelative(string full)
        {
            string root = BuildPaths.ProjectRoot.TrimEnd(Path.DirectorySeparatorChar);
            full = Path.GetFullPath(full);
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return full.Substring(root.Length + 1);
            return full;
        }

        static string ReadTextSafe(string path, int maxBytes, out bool truncated)
        {
            truncated = false;
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length > maxBytes)
            { Array.Resize(ref bytes, maxBytes); truncated = true; }
            try
            { return new UTF8Encoding(false, true).GetString(bytes); }
            catch { return Encoding.Default.GetString(bytes); }
        }

        static bool IsTextFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            for (int i = 0; i < TextExts.Length; i++)
                if (TextExts[i] == ext)
                    return true;
            return false;
        }

        static string Truncate(string s, int max) { if (s.Length <= max) return s; return s.Substring(0, max) + "\n…(truncated)"; }
        static string Sanitize(string s) { if (string.IsNullOrWhiteSpace(s)) return "share"; foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s.Trim(); }

        static void BuildCodeIndexCsv(string outPath, GlobFilter filter)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            StringBuilder sb = new StringBuilder(1024 * 1024);
            sb.AppendLine("path,ext,bytes,lines,isEditor");

            string root = Path.Combine(BuildPaths.ProjectRoot, "Assets");
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                if (filter.Excludes(file))
                    continue;
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (!CodeExts.Contains(ext))
                    continue;
                try
                {
                    long size = new FileInfo(file).Length;
                    int lines = IsProbablyText(ext) ? SafeCountLines(file) : 0;
                    bool isEditor = file.Replace('\\', '/').IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0;
                    sb.AppendLine(MakeRelative(file) + "," + ext + "," + size + "," + lines + "," + (isEditor ? "1" : "0"));
                }
                catch { }
            }
            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
        }

        static int SafeCountLines(string path) { try { return File.ReadLines(path).Count(); } catch { return 0; } }
        static bool IsProbablyText(string ext)
        {
            if (ext == ".cs")
                return true;
            if (ext == ".hlsl" || ext == ".cginc" || ext == ".compute" || ext == ".shader")
                return true;
            if (ext == ".uxml" || ext == ".uss")
                return true;
            if (ext == ".asmdef" || ext == ".asmref")
                return true;
            if (ext == ".json" || ext == ".yml" || ext == ".yaml" || ext == ".xml" || ext == ".txt" || ext == ".md")
                return true;
            return false;
        }

        static void WriteTree(string outPath, string topFolderName, GlobFilter filter)
        {
            string root = Path.Combine(BuildPaths.ProjectRoot, topFolderName);
            List<string> lines = new List<string>();
            if (!Directory.Exists(root))
            { File.WriteAllText(outPath, "(No " + topFolderName + " folder)"); return; }

            Action<string, string, bool> Recurse = null;
            Recurse = (dir, indent, isLast) =>
            {
                string name = Path.GetFileName(dir);
                string[] subDirs = Directory.GetDirectories(dir).OrderBy(x => x).ToArray();

                string[] files = Directory.GetFiles(dir).OrderBy(x => x).Where(p => !filter.Excludes(p)).ToArray();
                Tuple<int, int, int, int> counts = CountCode(files);

                string branch = indent + (isLast ? "└─ " : "├─ ");
                lines.Add(branch + name + "  (cs:" + counts.Item1 + " sh:" + counts.Item2 + " ui:" + counts.Item3 + " defs:" + counts.Item4 + ")");

                string childIndent = indent + (isLast ? "   " : "│  ");
                for (int i = 0; i < subDirs.Length; i++)
                    Recurse(subDirs[i], childIndent, i == subDirs.Length - 1);
            };

            Recurse(root, "", true);
            File.WriteAllLines(outPath, lines.ToArray());
        }

        static Tuple<int, int, int, int> CountCode(string[] files)
        {
            int cs = 0, sh = 0, ui = 0, defs = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string e = Path.GetExtension(files[i]).ToLowerInvariant();
                if (e == ".cs")
                    cs++;
                else if (e == ".shader" || e == ".hlsl" || e == ".cginc" || e == ".compute")
                    sh++;
                else if (e == ".uxml" || e == ".uss")
                    ui++;
                else if (e == ".asmdef" || e == ".asmref")
                    defs++;
            }
            return new Tuple<int, int, int, int>(cs, sh, ui, defs);
        }

        static void SafeCopy(string src, string dst) { if (!File.Exists(src)) return; Directory.CreateDirectory(Path.GetDirectoryName(dst)); File.Copy(src, dst, true); }
    }

    // ---------------- EXTRAS (Settings / Input / CM / HDRP / Diff) ----------------
    internal static class SettingsDump
    {
        public static void WriteUnitySettingsJson(string outDir)
        {
            try
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                Dictionary<string, object> root = new Dictionary<string, object>();
                root["unityVersion"] = Application.unityVersion;
                root["target"] = EditorUserBuildSettings.activeBuildTarget.ToString();
                root["colorSpace"] = PlayerSettings.colorSpace.ToString();
                root["companyName"] = PlayerSettings.companyName;
                root["productName"] = PlayerSettings.productName;
                root["scriptingBackend"] = PlayerSettings.GetScriptingBackend(group).ToString();
#if UNITY_2021_2_OR_NEWER
                root["apiCompatibility"] = PlayerSettings.GetApiCompatibilityLevel(group).ToString();
#endif

                // Quality
                Dictionary<string, object> q = new Dictionary<string, object>();
                string[] names = QualitySettings.names;
                for (int i = 0; i < names.Length; i++)
                {
                    int idx = i;
                    Dictionary<string, object> qi = new Dictionary<string, object>();
                    qi["antiAliasing"] = QualitySettings.GetQualityLevel() == idx ? QualitySettings.antiAliasing : (object)"(open scene to read active)";
                    qi["vSyncCount"] = QualitySettings.vSyncCount;
                    qi["anisotropicFiltering"] = QualitySettings.anisotropicFiltering.ToString();
                    qi["shadowCascades"] = QualitySettings.shadowCascades;
                    qi["pixelLightCount"] = QualitySettings.pixelLightCount;
                    qi["lodBias"] = QualitySettings.lodBias;
                    q[names[i]] = qi;
                }
                root["quality"] = q;

                // Scenes in build
                List<string> scenes = new List<string>();
                foreach (var s in EditorBuildSettings.scenes.Where(s => s.enabled))
                    scenes.Add(s.path);
                root["buildScenes"] = scenes;

                string json = MiniJson.Serialize(root);
                File.WriteAllText(Path.Combine(outDir, "UnitySettings.json"), json, new UTF8Encoding(false));
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[SharePackage] UnitySettings.json failed: " + ex.Message); }
        }
    }

    internal static class ExtrasDump
    {
        public static void TryWriteInputActionsJson(string outDir)
        {
            try
            {
                // reflect InputActionAsset if present
                Type tAsset = Type.GetType("UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem");
                if (tAsset == null)
                    return;

                string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
                if (guids == null || guids.Length == 0)
                    return;

                Dictionary<string, string> outputs = new Dictionary<string, string>();
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, tAsset);
                    if (obj == null)
                        continue;

                    // call ToJson() via reflection
                    string json = null;
                    var m = tAsset.GetMethod("ToJson", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new Type[0], null);
                    if (m != null)
                        json = (string)m.Invoke(obj, null);
                    if (string.IsNullOrEmpty(json))
                        continue;

                    outputs[path] = json;
                }
                if (outputs.Count == 0)
                    return;

                StringBuilder sb = new StringBuilder(1024 * 1024);
                sb.AppendLine("{");
                int k = 0;
                foreach (var kv in outputs)
                {
                    if (k++ > 0)
                        sb.AppendLine(",");
                    sb.Append("\"").Append(kv.Key.Replace("\\", "/")).Append("\": ");
                    sb.Append(kv.Value);
                }
                sb.AppendLine();
                sb.AppendLine("}");
                File.WriteAllText(Path.Combine(outDir, "InputActions.json"), sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[SharePackage] InputActions.json skipped: " + ex.Message); }
        }

        public static void TryWriteCinemachineSummary(string outDir)
        {
            try
            {
                Type tCamBase = Type.GetType("Cinemachine.CinemachineVirtualCameraBase, Cinemachine");
                if (tCamBase == null)
                    return;

                // try to find prefabs that contain CM cameras
                List<string> lines = new List<string>();
                lines.Add("# Cinemachine Summary");
                string[] guids = AssetDatabase.FindAssets("t:GameObject");
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                        continue;
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go == null)
                        continue;
                    var comps = go.GetComponentsInChildren<Component>(true);
                    bool wroteHeader = false;
                    for (int c = 0; c < comps.Length; c++)
                    {
                        Component comp = comps[c];
                        if (comp == null)
                            continue;
                        Type ct = comp.GetType();
                        if (tCamBase.IsAssignableFrom(ct))
                        {
                            if (!wroteHeader)
                            { lines.Add("## " + path); wroteHeader = true; }
                            // reflect Lens props if present
                            var lensProp = ct.GetProperty("Lens");
                            string lens = lensProp != null ? lensProp.GetValue(comp, null).ToString() : "(lens n/a)";
                            lines.Add("- " + ct.Name + " on " + comp.gameObject.name + " | Lens: " + lens);
                        }
                    }
                }
                if (lines.Count > 1)
                    File.WriteAllLines(Path.Combine(outDir, "Cinemachine_Summary.md"), lines.ToArray());
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[SharePackage] Cinemachine summary skipped: " + ex.Message); }
        }

        public static void WriteVolumeProfilesSummary(string outDir)
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:VolumeProfile");
                if (guids == null || guids.Length == 0)
                    return;

                List<string> lines = new List<string>();
                lines.Add("# HDRP Volume Profiles");
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    VolumeProfile vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                    if (vp == null)
                        continue;
                    lines.Add("## " + path);
                    // list component names via reflection (works across HDRP versions)
                    var comps = vp.components;
                    for (int c = 0; c < comps.Count; c++)
                    {
                        var comp = comps[c];
                        if (comp == null)
                            continue;
                        lines.Add("- " + comp.GetType().Name);
                    }
                }
                if (lines.Count > 1)
                    File.WriteAllLines(Path.Combine(outDir, "HDRP_VolumeProfiles.md"), lines.ToArray());
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[SharePackage] HDRP VolumeProfiles summary skipped: " + ex.Message); }
        }
    }

    internal static class DiffActions
    {
        public static void WriteDiffAgainstPrevious(string currentOutDir)
        {
            try
            {
                string root = BuildPaths.ShareRoot;
                var dirs = Directory.GetDirectories(root).OrderBy(d => d).ToList();
                if (dirs.Count < 2)
                    return; // nothing to diff
                string prev = dirs[dirs.Count - 2];
                string curCsv = Path.Combine(currentOutDir, "CodeIndex.csv");
                string prevCsv = Path.Combine(prev, "CodeIndex.csv");
                if (!File.Exists(curCsv) || !File.Exists(prevCsv))
                    return;

                var cur = Load(curCsv);
                var old = Load(prevCsv);

                // by path
                var added = cur.Keys.Except(old.Keys).OrderBy(x => x).ToList();
                var removed = old.Keys.Except(cur.Keys).OrderBy(x => x).ToList();
                var changed = new List<string>();
                foreach (var p in cur.Keys.Intersect(old.Keys))
                {
                    if (cur[p] != old[p])
                        changed.Add(p);
                }
                changed.Sort(StringComparer.OrdinalIgnoreCase);

                List<string> lines = new List<string>();
                lines.Add("# Diff vs previous SharePackage");
                lines.Add("");
                lines.Add("Previous: " + Path.GetFileName(prev));
                lines.Add("Current:  " + Path.GetFileName(currentOutDir));
                lines.Add("");
                lines.Add("## Added (" + added.Count + ")");
                foreach (var a in added)
                    lines.Add("- " + a);
                lines.Add("");
                lines.Add("## Removed (" + removed.Count + ")");
                foreach (var r in removed)
                    lines.Add("- " + r);
                lines.Add("");
                lines.Add("## Changed (" + changed.Count + ")");
                foreach (var ch in changed)
                    lines.Add("- " + ch);

                File.WriteAllLines(Path.Combine(currentOutDir, "Diff.md"), lines.ToArray());
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[SharePackage] Diff generation skipped: " + ex.Message); }
        }

        static Dictionary<string, string> Load(string csv)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(csv))
            {
                if (line.StartsWith("path,"))
                    continue; // header
                var idx = line.IndexOf(',');
                if (idx <= 0)
                    continue;
                string path = line.Substring(0, idx);
                dict[path] = line; // entire record
            }
            return dict;
        }
    }

    // ---------------- MIRROR ----------------
    internal struct MirrorOptions
    {
        public bool IncludeMeta;
        public bool IncludeYamlTextAssets;
        public bool UseFilter;
    }

    internal static class MirrorActions
    {
        static readonly HashSet<string> YamlTextExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".unity",".prefab",".mat",".asset",".anim",".controller",".overrideController",".playable",".guiskin"
        };

        public static void ExportPublicMirrorAllCode(MirrorOptions m)
        {
            GlobFilter filter = m.UseFilter ? FilterActions.Load() : new GlobFilter();

            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string exportRoot = BuildPaths.MirrorRoot;
            Directory.CreateDirectory(exportRoot);

            string staging = Path.Combine(Path.GetTempPath(), "WS_Mirror_" + ts);
            if (Directory.Exists(staging))
                TryDeleteDir(staging);
            Directory.CreateDirectory(staging);

            try
            {
                string proj = BuildPaths.ProjectRoot;

                // Copy all code files under Assets (+ optional YAML text assets)
                string assets = Path.Combine(proj, "Assets");
                string dstAssets = Path.Combine(staging, "Assets");
                CopyCodeTree(assets, dstAssets, m.IncludeMeta, m.IncludeYamlTextAssets, filter);

                // Include ProjectSettings (text-based)
                string ps = Path.Combine(proj, "ProjectSettings");
                if (Directory.Exists(ps))
                    CopySelected(ps, Path.Combine(staging, "ProjectSettings"), filter,
                        path => IsTextSettings(Path.GetExtension(path)));

                // Include Packages manifest (+ lock optional)
                SafeCopy(Path.Combine(proj, "Packages", "manifest.json"), Path.Combine(staging, "Packages", "manifest.json"));
                SafeCopy(Path.Combine(proj, "Packages", "packages-lock.json"), Path.Combine(staging, "Packages", "packages-lock.json"));

                // Zip it (fully-qualify CompressionLevel)
                string zip = Path.Combine(exportRoot, "Mirror_" + ts + "_AllCode.zip");
                ZipUtils.CreateZipFromFolder(staging, zip);
                EditorUtility.RevealInFinder(exportRoot);
                UnityEngine.Debug.Log("[PublicMirror] Exported: " + zip);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[PublicMirror] Export failed: " + ex);
            }
            finally
            {
                TryDeleteDir(staging);
            }
        }

        static void CopyCodeTree(string srcDir, string dstDir, bool includeMeta, bool includeYaml, GlobFilter filter)
        {
            if (!Directory.Exists(srcDir))
                return;

            foreach (string dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
            {
                string rel = dir.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string abs = Path.Combine(srcDir, rel);
                if (filter.Excludes(abs))
                    continue;
                Directory.CreateDirectory(Path.Combine(dstDir, rel));
            }

            foreach (string file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
            {
                if (filter.Excludes(file))
                    continue;
                string ext = Path.GetExtension(file).ToLowerInvariant();
                bool isCode = ShareActions.CodeExts.Contains(ext);
                bool isYaml = includeYaml && YamlTextExts.Contains(ext);
                if (!isCode && !isYaml)
                    continue;

                string rel = file.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string dst = Path.Combine(dstDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(file, dst, true);

                if (includeMeta)
                {
                    string meta = file + ".meta";
                    if (File.Exists(meta))
                    {
                        string dstMeta = dst + ".meta";
                        File.Copy(meta, dstMeta, true);
                    }
                }
            }
        }

        static void CopySelected(string srcDir, string dstDir, GlobFilter filter, Func<string, bool> predicate)
        {
            foreach (string dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
            {
                string rel = dir.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(dstDir, rel));
            }

            foreach (string file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
            {
                if (filter.Excludes(file))
                    continue;
                if (!predicate(file))
                    continue;
                string rel = file.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string dst = Path.Combine(dstDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(file, dst, true);
            }
        }

        static bool IsTextSettings(string ext)
        {
            string e = ext.ToLowerInvariant();
            return (e == ".asset" || e == ".json" || e == ".txt" || e == ".yaml" || e == ".yml");
        }

        static void SafeCopy(string src, string dst) { if (!File.Exists(src)) return; Directory.CreateDirectory(Path.GetDirectoryName(dst)); File.Copy(src, dst, true); }
        static void TryDeleteDir(string path) { try { Directory.Delete(path, true); } catch { } }
    }

    // ---------------- ZIP UTILS ----------------
    internal static class ZipUtils
    {
        public static void CreateZipFromFolder(string sourceDir, string destZip)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destZip));

            string srcFull = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string zipFull = Path.GetFullPath(destZip);

            if (zipFull.StartsWith(srcFull, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Destination ZIP must be outside the source directory.\nsource: " + srcFull + "\ndest: " + zipFull);

            TryDeleteWithRetries(destZip, 6, 200);

            string tempZip = destZip + ".tmp";
            TryDeleteWithRetries(tempZip, 2, 100);

            System.IO.Compression.ZipFile.CreateFromDirectory(sourceDir, tempZip, System.IO.Compression.CompressionLevel.Optimal, false);
            TryMoveWithRetries(tempZip, destZip, 6, 250);
        }

        static void TryDeleteWithRetries(string path, int attempts, int delayMs)
        {
            for (int i = 0; i < attempts; i++)
            {
                try
                { if (File.Exists(path)) UnityEditor.FileUtil.DeleteFileOrDirectory(path); return; }
                catch (IOException) { Thread.Sleep(delayMs); }
                catch (UnauthorizedAccessException) { Thread.Sleep(delayMs); }
            }
        }

        static void TryMoveWithRetries(string src, string dst, int attempts, int delayMs)
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

    // ---------------- MINI JSON (simple serializer, no deps) ----------------
    internal static class MiniJson
    {
        public static string Serialize(object obj)
        {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, obj);
            return sb.ToString();
        }

        static void WriteValue(StringBuilder sb, object v)
        {
            if (v == null)
            { sb.Append("null"); return; }
            if (v is string)
            { sb.Append('\"').Append(Escape((string)v)).Append('\"'); return; }
            if (v is bool)
            { sb.Append(((bool)v) ? "true" : "false"); return; }
            if (v is IDictionary<string, object>)
            {
                sb.Append('{');
                bool first = true;
                foreach (var kv in (IDictionary<string, object>)v)
                {
                    if (!first)
                        sb.Append(',');
                    first = false;
                    sb.Append('\"').Append(Escape(kv.Key)).Append('\"').Append(':');
                    WriteValue(sb, kv.Value);
                }
                sb.Append('}');
                return;
            }
            if (v is IEnumerable<string>)
            {
                sb.Append('[');
                bool first = true;
                foreach (var s in (IEnumerable<string>)v)
                {
                    if (!first)
                        sb.Append(',');
                    first = false;
                    sb.Append('\"').Append(Escape(s)).Append('\"');
                }
                sb.Append(']');
                return;
            }
            sb.Append(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture));
        }

        static string Escape(string s) { return s.Replace("\\", "\\\\").Replace("\"", "\\\""); }
    }
}
#endif


//// Assets/_Project/Code/Editor/Share/WildSurvivalShareHub.cs
//// Unity 6 – Editor-only: SharePackage builder + Improved Public Mirror (All Code)
//// Safe for older C# versions, avoids UnityEngine.CompressionLevel ambiguity, always calls UnityEngine.Debug.

//#if UNITY_EDITOR
//using System;
//using System.IO;
//using System.Linq;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.IO.Compression; // we will fully qualify when using CompressionLevel to avoid ambiguity
//using UnityEditor;
//using UnityEditor.SceneManagement;
//using UnityEngine;

//namespace WildSurvival.EditorTools
//{
//    public class WildSurvivalShareHub : EditorWindow
//    {
//        [MenuItem("Tools/Wild Survival/Share & Mirror")]
//        public static void Open() { GetWindow<WildSurvivalShareHub>("Wild Survival — Share & Mirror"); }

//        Vector2 _scroll;
//        string _label = "share";
//        bool _includeMetas = true;
//        bool _zipAfterShare = true;
//        bool _includePackagesLock = true;
//        bool _includeProjSettings = true;
//        int _maxDocKB = 512; // cap per-doc merge size
//        bool _includeCollabAndReports = true;

//        void OnGUI()
//        {
//            _scroll = EditorGUILayout.BeginScrollView(_scroll);
//            try
//            {
//                EditorGUILayout.LabelField("SharePackage (docs + snapshot + code index)", EditorStyles.boldLabel);
//                _label = EditorGUILayout.TextField("Label", _label);
//                _maxDocKB = EditorGUILayout.IntSlider("Max per-doc size (KB)", _maxDocKB, 64, 4096);
//                _includeCollabAndReports = EditorGUILayout.ToggleLeft("Include Collab & *Report* docs", _includeCollabAndReports);
//                _includeProjSettings = EditorGUILayout.ToggleLeft("Include ProjectSettings snapshot", _includeProjSettings);
//                _includePackagesLock = EditorGUILayout.ToggleLeft("Include packages-lock.json (if present)", _includePackagesLock);
//                _zipAfterShare = EditorGUILayout.ToggleLeft("Create SharePackage .zip", _zipAfterShare);

//                if (GUILayout.Button("Build SharePackage"))
//                {
//                    ShareOptions opts = new ShareOptions();
//                    opts.Label = _label;
//                    opts.MaxDocBytes = _maxDocKB * 1024;
//                    opts.IncludeCollabAndReports = _includeCollabAndReports;
//                    opts.IncludeProjectSettings = _includeProjSettings;
//                    opts.IncludePackagesLock = _includePackagesLock;
//                    opts.ZipAfterBuild = _zipAfterShare;

//                    EditorApplication.delayCall += () => ShareActions.BuildSharePackage(opts);
//                }

//                EditorGUILayout.Space(16);
//                EditorGUILayout.LabelField("Improved Public Mirror (All Code)", EditorStyles.boldLabel);
//                _includeMetas = EditorGUILayout.ToggleLeft("Include .meta files for copied code", _includeMetas);

//                if (GUILayout.Button("Export Public Mirror — All Code (ZIP)"))
//                {
//                    bool withMetas = _includeMetas;
//                    EditorApplication.delayCall += () => MirrorActions.ExportPublicMirrorAllCode(withMetas);
//                }

//                if (GUILayout.Button("Open Builds Folder"))
//                    EditorUtility.RevealInFinder(BuildPaths.BuildsRoot);
//            }
//            finally { EditorGUILayout.EndScrollView(); }
//        }
//    }

//    // ---------------- PATHS ----------------
//    internal static class BuildPaths
//    {
//        public static string ProjectRoot { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
//        public static string BuildsRoot { get { return Path.Combine(ProjectRoot, "Builds"); } }
//        public static string ShareRoot { get { return Path.Combine(BuildsRoot, "SharePackage"); } }
//        public static string MirrorRoot { get { return Path.Combine(BuildsRoot, "PublicMirror"); } }
//    }

//    // ---------------- SHARE OPTIONS ----------------
//    internal struct ShareOptions
//    {
//        public string Label;
//        public int MaxDocBytes;
//        public bool IncludeCollabAndReports;
//        public bool IncludeProjectSettings;
//        public bool IncludePackagesLock;
//        public bool ZipAfterBuild;
//    }

//    // ---------------- SHARE ACTIONS ----------------
//    internal static class ShareActions
//    {
//        static readonly string[] RootDocNames = { "README", "CHANGELOG", "LICENSE", "CONTRIBUTING" };
//        static readonly string[] TextExts = { ".md", ".txt", ".log", ".json", ".yml", ".yaml", ".xml", ".csv" };

//        // Make this accessible to MirrorActions
//        internal static readonly HashSet<string> CodeExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
//        {
//            ".cs",".hlsl",".cginc",".compute",".shader",".uss",".uxml",".asmdef",".asmref",
//            ".json",".yml",".yaml",".xml"
//        };

//        public static void BuildSharePackage(ShareOptions o)
//        {
//            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
//            string outDir = Path.Combine(BuildPaths.ShareRoot, ts);
//            Directory.CreateDirectory(outDir);

//            try
//            {
//                // 1) Build mega snapshot doc
//                string snapshotPath = Path.Combine(outDir, "SharePackage_Snapshot_" + Sanitize(o.Label) + ".md");
//                StringBuilder sb = new StringBuilder(64 * 1024);

//                sb.AppendLine("# Wild Survival — SharePackage Snapshot");
//                sb.AppendLine();
//                sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
//                sb.AppendLine("Unity: " + Application.unityVersion + " | Target: " + EditorUserBuildSettings.activeBuildTarget + " | Color Space: " + PlayerSettings.colorSpace);
//                sb.AppendLine();

//                // Project summary
//                AppendProjectSummary(sb);

//                // Package manifest
//                AppendManifestSection(sb);

//                // Merge known root docs
//                AppendRootDocs(sb, o.MaxDocBytes);

//                // Collab + Reports (if requested)
//                if (o.IncludeCollabAndReports)
//                    AppendCollabAndReports(sb, o.MaxDocBytes);

//                // ProjectSettings/ProjectVersion
//                if (o.IncludeProjectSettings)
//                    AppendProjectSettingsBits(sb, o.MaxDocBytes);

//                File.WriteAllText(snapshotPath, sb.ToString(), new UTF8Encoding(false));
//                UnityEngine.Debug.Log("[SharePackage] Wrote: " + snapshotPath);

//                // 2) Code index CSV
//                string codeIndexPath = Path.Combine(outDir, "CodeIndex.csv");
//                BuildCodeIndexCsv(codeIndexPath);
//                UnityEngine.Debug.Log("[SharePackage] Wrote: " + codeIndexPath);

//                // 3) Tree.txt
//                string treePath = Path.Combine(outDir, "Tree.txt");
//                WriteTree(treePath, "Assets");
//                UnityEngine.Debug.Log("[SharePackage] Wrote: " + treePath);

//                // 4) Copy manifest & (optional) packages-lock, and ProjectVersion
//                SafeCopy(Path.Combine(BuildPaths.ProjectRoot, "Packages", "manifest.json"), Path.Combine(outDir, "Packages", "manifest.json"));
//                if (o.IncludePackagesLock)
//                    SafeCopy(Path.Combine(BuildPaths.ProjectRoot, "Packages", "packages-lock.json"), Path.Combine(outDir, "Packages", "packages-lock.json"));
//                if (o.IncludeProjectSettings)
//                    SafeCopy(Path.Combine(BuildPaths.ProjectRoot, "ProjectSettings", "ProjectVersion.txt"), Path.Combine(outDir, "ProjectSettings", "ProjectVersion.txt"));

//                EditorUtility.RevealInFinder(outDir);

//                // 5) Optional ZIP of the SharePackage
//                if (o.ZipAfterBuild)
//                {
//                    string zip = Path.Combine(BuildPaths.ShareRoot, "SharePackage_" + ts + "_" + Sanitize(o.Label) + ".zip");
//                    ZipUtils.CreateZipFromFolder(outDir, zip);
//                    UnityEngine.Debug.Log("[SharePackage] Zipped: " + zip);
//                }
//            }
//            catch (Exception ex)
//            {
//                UnityEngine.Debug.LogError("[SharePackage] Failed: " + ex);
//            }
//        }

//        static void AppendProjectSummary(StringBuilder sb)
//        {
//            sb.AppendLine("## Project Summary");
//            sb.AppendLine();
//            sb.AppendLine("- Company: " + PlayerSettings.companyName);
//            sb.AppendLine("- Product: " + PlayerSettings.productName);
//            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
//            sb.AppendLine("- Scripting Backend (Standalone): " + PlayerSettings.GetScriptingBackend(group));
//#if UNITY_2021_2_OR_NEWER
//            sb.AppendLine("- API Compatibility (Standalone): " + PlayerSettings.GetApiCompatibilityLevel(group));
//#endif
//            // Build scenes
//            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
//            sb.AppendLine("- Scenes in Build: " + scenes.Length);
//            for (int i = 0; i < scenes.Length; i++)
//                sb.AppendLine("  - " + scenes[i]);
//            sb.AppendLine();
//        }

//        static void AppendManifestSection(StringBuilder sb)
//        {
//            string manifestPath = Path.Combine(BuildPaths.ProjectRoot, "Packages", "manifest.json");
//            if (!File.Exists(manifestPath))
//                return;

//            sb.AppendLine("## Packages (manifest.json)");
//            sb.AppendLine();
//            string json = Truncate(File.ReadAllText(manifestPath), 64 * 1024);
//            sb.AppendLine("```json");
//            sb.AppendLine(json);
//            sb.AppendLine("```");
//            sb.AppendLine();
//        }

//        static void AppendRootDocs(StringBuilder sb, int maxBytes)
//        {
//            string root = BuildPaths.ProjectRoot;
//            List<string> files = new List<string>();

//            foreach (string name in RootDocNames)
//            {
//                string[] hits = Directory.GetFiles(root, name + ".*", SearchOption.TopDirectoryOnly);
//                if (hits != null && hits.Length > 0)
//                    files.AddRange(hits);
//            }

//            string[] md = Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly);
//            if (md != null)
//                files.AddRange(md);
//            string[] txt = Directory.GetFiles(root, "*.txt", SearchOption.TopDirectoryOnly);
//            if (txt != null)
//                files.AddRange(txt);

//            files = files.Distinct().Where(IsTextFile).ToList();
//            if (files.Count == 0)
//                return;

//            sb.AppendLine("## Root Documents");
//            sb.AppendLine();
//            foreach (string f in files)
//                AppendDocSection(sb, f, maxBytes);
//        }

//        static void AppendCollabAndReports(StringBuilder sb, int maxBytes)
//        {
//            string assets = Path.Combine(BuildPaths.ProjectRoot, "Assets");
//            List<string> candidates = new List<string>();

//            foreach (string p in Directory.GetFiles(assets, "*", SearchOption.AllDirectories))
//            {
//                string fn = Path.GetFileName(p);
//                string dir = Path.GetDirectoryName(p) ?? "";
//                bool looksText = IsTextFile(p);
//                bool looksCollab = dir.IndexOf("collab", StringComparison.OrdinalIgnoreCase) >= 0;
//                bool looksReport = fn.IndexOf("report", StringComparison.OrdinalIgnoreCase) >= 0
//                                   || fn.IndexOf("log", StringComparison.OrdinalIgnoreCase) >= 0;
//                if (looksText && (looksCollab || looksReport))
//                    candidates.Add(p);
//            }

//            candidates = candidates.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
//            if (candidates.Count == 0)
//                return;

//            sb.AppendLine("## Collab & Reports (merged)");
//            sb.AppendLine();
//            foreach (string f in candidates)
//                AppendDocSection(sb, f, maxBytes);
//        }

//        static void AppendProjectSettingsBits(StringBuilder sb, int maxBytes)
//        {
//            string p = Path.Combine(BuildPaths.ProjectRoot, "ProjectSettings", "ProjectVersion.txt");
//            if (File.Exists(p))
//            {
//                sb.AppendLine("## ProjectSettings / ProjectVersion.txt");
//                sb.AppendLine();
//                AppendDocSection(sb, p, maxBytes);
//            }
//        }

//        static void AppendDocSection(StringBuilder sb, string path, int maxBytes)
//        {
//            try
//            {
//                string rel = MakeRelative(path);
//                sb.AppendLine("### " + rel);
//                sb.AppendLine();
//                bool truncated;
//                string text = ReadTextSafe(path, maxBytes, out truncated);
//                string fence = GuessFence(path);
//                sb.AppendLine("```" + fence);
//                sb.AppendLine(text);
//                sb.AppendLine("```");
//                if (truncated)
//                    sb.AppendLine("_…truncated to " + (maxBytes / 1024) + " KB for brevity_");
//                sb.AppendLine();
//            }
//            catch (Exception ex)
//            {
//                sb.AppendLine("> (Could not read " + path + ": " + ex.Message + ")");
//            }
//        }

//        static string GuessFence(string path)
//        {
//            string ext = Path.GetExtension(path).ToLowerInvariant();
//            if (ext == ".json")
//                return "json";
//            if (ext == ".yml" || ext == ".yaml")
//                return "yaml";
//            if (ext == ".xml")
//                return "xml";
//            return "";
//        }

//        static string MakeRelative(string full)
//        {
//            string root = BuildPaths.ProjectRoot.TrimEnd(Path.DirectorySeparatorChar);
//            full = Path.GetFullPath(full);
//            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
//                return full.Substring(root.Length + 1);
//            return full;
//        }

//        static string ReadTextSafe(string path, int maxBytes, out bool truncated)
//        {
//            truncated = false;
//            byte[] bytes = File.ReadAllBytes(path);
//            if (bytes.Length > maxBytes)
//            {
//                Array.Resize(ref bytes, maxBytes);
//                truncated = true;
//            }
//            try
//            { return new UTF8Encoding(false, true).GetString(bytes); }
//            catch { return Encoding.Default.GetString(bytes); }
//        }

//        static bool IsTextFile(string path)
//        {
//            string ext = Path.GetExtension(path).ToLowerInvariant();
//            foreach (string e in TextExts)
//                if (e == ext)
//                    return true;
//            return false;
//        }

//        static string Truncate(string s, int max)
//        {
//            if (s.Length <= max)
//                return s;
//            return s.Substring(0, max) + "\n…(truncated)";
//        }

//        static string Sanitize(string s)
//        {
//            if (string.IsNullOrWhiteSpace(s))
//                return "share";
//            foreach (char c in Path.GetInvalidFileNameChars())
//                s = s.Replace(c, '_');
//            return s.Trim();
//        }

//        static void BuildCodeIndexCsv(string outPath)
//        {
//            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
//            StringBuilder sb = new StringBuilder(1024 * 1024);
//            sb.AppendLine("path,ext,bytes,lines,isEditor");

//            string root = Path.Combine(BuildPaths.ProjectRoot, "Assets");
//            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
//            {
//                string ext = Path.GetExtension(file).ToLowerInvariant();
//                if (!CodeExts.Contains(ext))
//                    continue;
//                try
//                {
//                    long size = new FileInfo(file).Length;
//                    int lines = IsProbablyText(ext) ? SafeCountLines(file) : 0;
//                    bool isEditor = file.Replace('\\', '/').IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0;
//                    sb.AppendLine(MakeRelative(file) + "," + ext + "," + size + "," + lines + "," + (isEditor ? "1" : "0"));
//                }
//                catch { /* ignore */ }
//            }
//            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
//        }

//        static int SafeCountLines(string path)
//        {
//            try
//            { return File.ReadLines(path).Count(); }
//            catch { return 0; }
//        }

//        static bool IsProbablyText(string ext)
//        {
//            // conservative: list the common textual code/config types
//            if (ext == ".cs")
//                return true;
//            if (ext == ".hlsl" || ext == ".cginc" || ext == ".compute" || ext == ".shader")
//                return true;
//            if (ext == ".uxml" || ext == ".uss")
//                return true;
//            if (ext == ".asmdef" || ext == ".asmref")
//                return true;
//            if (ext == ".json" || ext == ".yml" || ext == ".yaml" || ext == ".xml" || ext == ".txt" || ext == ".md")
//                return true;
//            return false;
//        }

//        static void WriteTree(string outPath, string topFolderName)
//        {
//            string root = Path.Combine(BuildPaths.ProjectRoot, topFolderName);
//            List<string> lines = new List<string>();
//            if (!Directory.Exists(root))
//            { File.WriteAllText(outPath, "(No " + topFolderName + " folder)"); return; }

//            Action<string, string, bool> Recurse = null;
//            Recurse = (dir, indent, isLast) =>
//            {
//                string name = Path.GetFileName(dir);
//                string[] subDirs = Directory.GetDirectories(dir).OrderBy(x => x).ToArray();
//                string[] files = Directory.GetFiles(dir).OrderBy(x => x).ToArray();
//                Tuple<int, int, int, int> counts = CountCode(files);

//                string branch = indent + (isLast ? "└─ " : "├─ ");
//                lines.Add(branch + name + "  (cs:" + counts.Item1 + " sh:" + counts.Item2 + " ui:" + counts.Item3 + " defs:" + counts.Item4 + ")");

//                string childIndent = indent + (isLast ? "   " : "│  ");
//                for (int i = 0; i < subDirs.Length; i++)
//                    Recurse(subDirs[i], childIndent, i == subDirs.Length - 1);
//            };

//            Recurse(root, "", true);
//            File.WriteAllLines(outPath, lines.ToArray());
//        }

//        static Tuple<int, int, int, int> CountCode(string[] files)
//        {
//            int cs = 0, sh = 0, ui = 0, defs = 0;
//            for (int i = 0; i < files.Length; i++)
//            {
//                string e = Path.GetExtension(files[i]).ToLowerInvariant();
//                if (e == ".cs")
//                    cs++;
//                else if (e == ".shader" || e == ".hlsl" || e == ".cginc" || e == ".compute")
//                    sh++;
//                else if (e == ".uxml" || e == ".uss")
//                    ui++;
//                else if (e == ".asmdef" || e == ".asmref")
//                    defs++;
//            }
//            return new Tuple<int, int, int, int>(cs, sh, ui, defs);
//        }

//        static void SafeCopy(string src, string dst)
//        {
//            if (!File.Exists(src))
//                return;
//            Directory.CreateDirectory(Path.GetDirectoryName(dst));
//            File.Copy(src, dst, true);
//        }
//    }

//    // ---------------- MIRROR ACTIONS ----------------
//    internal static class MirrorActions
//    {
//        public static void ExportPublicMirrorAllCode(bool includeMeta)
//        {
//            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
//            string exportRoot = BuildPaths.MirrorRoot;
//            Directory.CreateDirectory(exportRoot);

//            // Stage to temp folder OUTSIDE source to avoid self-zip
//            string staging = Path.Combine(Path.GetTempPath(), "WS_Mirror_" + ts);
//            if (Directory.Exists(staging))
//                TryDeleteDir(staging);
//            Directory.CreateDirectory(staging);

//            try
//            {
//                string proj = BuildPaths.ProjectRoot;

//                // Copy all code files under Assets
//                string assets = Path.Combine(proj, "Assets");
//                string dstAssets = Path.Combine(staging, "Assets");
//                CopyCodeTree(assets, dstAssets, includeMeta);

//                // Include ProjectSettings (text-based only)
//                string ps = Path.Combine(proj, "ProjectSettings");
//                if (Directory.Exists(ps))
//                    CopySelected(ps, Path.Combine(staging, "ProjectSettings"),
//                        path => IsTextSettings(Path.GetExtension(path)));

//                // Include Packages manifest (+ lock optional)
//                SafeCopy(Path.Combine(proj, "Packages", "manifest.json"), Path.Combine(staging, "Packages", "manifest.json"));
//                SafeCopy(Path.Combine(proj, "Packages", "packages-lock.json"), Path.Combine(staging, "Packages", "packages-lock.json"));

//                // Zip it (fully-qualify CompressionLevel to avoid ambiguity)
//                string zip = Path.Combine(exportRoot, "Mirror_" + ts + "_AllCode.zip");
//                ZipUtils.CreateZipFromFolder(staging, zip);
//                EditorUtility.RevealInFinder(exportRoot);
//                UnityEngine.Debug.Log("[PublicMirror] Exported: " + zip);
//            }
//            catch (Exception ex)
//            {
//                UnityEngine.Debug.LogError("[PublicMirror] Export failed: " + ex);
//            }
//            finally
//            {
//                TryDeleteDir(staging);
//            }
//        }

//        static void CopyCodeTree(string srcDir, string dstDir, bool includeMeta)
//        {
//            if (!Directory.Exists(srcDir))
//                return;

//            foreach (string dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
//            {
//                string rel = dir.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
//                Directory.CreateDirectory(Path.Combine(dstDir, rel));
//            }

//            foreach (string file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
//            {
//                string ext = Path.GetExtension(file).ToLowerInvariant();
//                if (!ShareActions.CodeExts.Contains(ext))
//                    continue;

//                string rel = file.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
//                string dst = Path.Combine(dstDir, rel);
//                Directory.CreateDirectory(Path.GetDirectoryName(dst));
//                File.Copy(file, dst, true);

//                if (includeMeta)
//                {
//                    string meta = file + ".meta";
//                    if (File.Exists(meta))
//                    {
//                        string dstMeta = dst + ".meta";
//                        File.Copy(meta, dstMeta, true);
//                    }
//                }
//            }
//        }

//        static void CopySelected(string srcDir, string dstDir, Func<string, bool> predicate)
//        {
//            foreach (string dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
//            {
//                string rel = dir.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
//                Directory.CreateDirectory(Path.Combine(dstDir, rel));
//            }

//            foreach (string file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
//            {
//                if (!predicate(file))
//                    continue;
//                string rel = file.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
//                string dst = Path.Combine(dstDir, rel);
//                Directory.CreateDirectory(Path.GetDirectoryName(dst));
//                File.Copy(file, dst, true);
//            }
//        }

//        static bool IsTextSettings(string ext)
//        {
//            string e = ext.ToLowerInvariant();
//            return (e == ".asset" || e == ".json" || e == ".txt" || e == ".yaml" || e == ".yml");
//        }

//        static void SafeCopy(string src, string dst)
//        {
//            if (!File.Exists(src))
//                return;
//            Directory.CreateDirectory(Path.GetDirectoryName(dst));
//            File.Copy(src, dst, true);
//        }

//        static void TryDeleteDir(string path)
//        {
//            try
//            { Directory.Delete(path, true); }
//            catch { /* ignore */ }
//        }
//    }

//    // ---------------- ZIP UTILS (robust) ----------------
//    internal static class ZipUtils
//    {
//        public static void CreateZipFromFolder(string sourceDir, string destZip)
//        {
//            Directory.CreateDirectory(Path.GetDirectoryName(destZip));

//            string srcFull = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
//            string zipFull = Path.GetFullPath(destZip);

//            if (zipFull.StartsWith(srcFull, StringComparison.OrdinalIgnoreCase))
//                throw new IOException("Destination ZIP must be outside the source directory.\nsource: " + srcFull + "\ndest: " + zipFull);

//            TryDeleteWithRetries(destZip, 6, 200);

//            string tempZip = destZip + ".tmp";
//            TryDeleteWithRetries(tempZip, 2, 100);

//            // Fully-qualify CompressionLevel to disambiguate from UnityEngine.CompressionLevel
//            System.IO.Compression.ZipFile.CreateFromDirectory(sourceDir, tempZip, System.IO.Compression.CompressionLevel.Optimal, false);
//            TryMoveWithRetries(tempZip, destZip, 6, 250);
//        }

//        static void TryDeleteWithRetries(string path, int attempts, int delayMs)
//        {
//            for (int i = 0; i < attempts; i++)
//            {
//                try
//                {
//                    if (File.Exists(path))
//                        UnityEditor.FileUtil.DeleteFileOrDirectory(path);
//                    return;
//                }
//                catch (IOException) { Thread.Sleep(delayMs); }
//                catch (UnauthorizedAccessException) { Thread.Sleep(delayMs); }
//            }
//        }

//        static void TryMoveWithRetries(string src, string dst, int attempts, int delayMs)
//        {
//            for (int i = 0; i < attempts; i++)
//            {
//                try
//                { File.Move(src, dst); return; }
//                catch (IOException) { Thread.Sleep(delayMs); }
//                catch (UnauthorizedAccessException) { Thread.Sleep(delayMs); }
//            }
//            File.Move(src, dst);
//        }
//    }
//}
//#endif
