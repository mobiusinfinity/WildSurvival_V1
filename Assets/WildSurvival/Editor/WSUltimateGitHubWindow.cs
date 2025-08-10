// Assets/WildSurvival/Editor/GitHubHub/WSUltimateGitHubWindow.cs
// Unity 6 (6000.0.xx)
// Ultimate Git Hub (single-file): Status, Commit, Push/Pull, Branches, Stash,
// First-Push Wizard, and a safe "All Code" Public Mirror ZIP.
// No external deps. Editor-only. UTF-8 without BOM for any file writes.
// Notes:
// - Uses System.IO.Compression (zip) with alias to avoid CompressionLevel ambiguity.
// - Push auto-detects upstream; first push sets "-u origin <branch>".
// - Log is verbose with clear error surfaces.
// - If "git" not in PATH, you can set a custom path in the UI.

#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using CompressionLevelAlias = System.IO.Compression.CompressionLevel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WildSurvival.EditorTools
{
    public class WSUltimateGitHubWindow : EditorWindow
    {
        [MenuItem("Wild Survival/Git & Share/Ultimate Git Hub", false, 10)]
        public static void Open()
        {
            var w = GetWindow<WSUltimateGitHubWindow>("Git & Share Hub");
            w.minSize = new Vector2(720, 560);
            w.Show();
        }

        // -------- prefs/keys --------
        const string K_GitPath    = "WS_UltGit_GitPath";
        const string K_RemoteUrl  = "WS_UltGit_RemoteUrl";
        const string K_CommitMsg  = "WS_UltGit_CommitMsg";
        const string K_IncludePS  = "WS_UltGit_MirrorIncludePS";
        const string K_IncludeMan = "WS_UltGit_MirrorIncludeManifest";
        const string K_IncIgnored = "WS_UltGit_MirrorIncludeIgnored";

        // -------- state --------
        string _projectRoot;
        string _gitPath;           // optional override path to git.exe
        string _repoUrl = "https://github.com/mobiusinfinity/WildSurvival_V1.git";
        string _currentBranch = "";
        string _statusShort = "";
        string _aheadBehind = "";
        string _commitMsg = "";
        Vector2 _scroll;
        string _log = "";
        bool _firstPushFoldout = true;
        bool _advancedFoldout = false;

        // Branch state
        string[] _localBranches = Array.Empty<string>();
        int _branchIndex = 0;
        string _newBranch = "";

        // Mirror options
        string _zipNameHint = "PublicMirror";
        bool _mirrorIncludeProjectSettings = true;
        bool _mirrorIncludeManifest = true;
        bool _includeGitIgnored = false;

        // File filters for mirror
        static readonly HashSet<string> CodeExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",".asmdef",".asmref",".shader",".cginc",".hlsl",".compute",".uxml",".uss",".json",".xml",
            ".txt",".md",".mmd",".yaml",".yml",".ps1",".bat",".sh"
        };

        static readonly string[] ExcludeDirs = {
            "Library","Temp","Obj","Logs","Build","Builds","UserSettings","MemoryCaptures",
            ".git",".vs",".idea",".vscode","Packages/PackageCache"
        };

        static readonly string[] ProjectMetaFiles = {
            "ProjectSettings/ProjectVersion.txt",
            "ProjectSettings/EditorSettings.asset",
            "ProjectSettings/QualitySettings.asset",
            "ProjectSettings/GraphicsSettings.asset"
        };

        void OnEnable()
        {
            _projectRoot = GetProjectRoot();
            _gitPath = EditorPrefs.GetString(K_GitPath, "").Trim();
            _repoUrl  = EditorPrefs.GetString(K_RemoteUrl, _repoUrl);
            _commitMsg = EditorPrefs.GetString(K_CommitMsg, "");
            _mirrorIncludeProjectSettings = EditorPrefs.GetBool(K_IncludePS, true);
            _mirrorIncludeManifest        = EditorPrefs.GetBool(K_IncludeMan, true);
            _includeGitIgnored            = EditorPrefs.GetBool(K_IncIgnored, false);

            TryLoadRepoInfo();
            RefreshBranchesAndStatus();
        }

        void OnDisable()
        {
            EditorPrefs.SetString(K_GitPath, _gitPath ?? "");
            EditorPrefs.SetString(K_RemoteUrl, _repoUrl ?? "");
            EditorPrefs.SetString(K_CommitMsg, _commitMsg ?? "");
            EditorPrefs.SetBool(K_IncludePS, _mirrorIncludeProjectSettings);
            EditorPrefs.SetBool(K_IncludeMan, _mirrorIncludeManifest);
            EditorPrefs.SetBool(K_IncIgnored, _includeGitIgnored);
        }

        void OnGUI()
        {
            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = sv.scrollPosition;

                HeaderBar();
                GUILayout.Space(6);

                RepoSetupBox();
                GUILayout.Space(4);

                CommitBox();
                GUILayout.Space(4);

                BranchBox();
                GUILayout.Space(4);

                MirrorBox();
                GUILayout.Space(6);

                OutputBox();
            }
        }

        // ---------------- UI: Header ----------------
        void HeaderBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Ultimate Git Hub", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                    RefreshBranchesAndStatus();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Project", GUILayout.Width(64));
                EditorGUILayout.SelectableLabel(_projectRoot ?? "-", GUILayout.Height(16));
                if (GUILayout.Button("Open Folder", GUILayout.Width(100)))
                    EditorUtility.RevealInFinder(_projectRoot);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Git Path", GUILayout.Width(64));
                _gitPath = EditorGUILayout.TextField(_gitPath ?? "");
                if (GUILayout.Button("Auto-Detect", GUILayout.Width(100)))
                {
                    _gitPath = AutoDetectGitPath();
                    AppendLog(string.IsNullOrEmpty(_gitPath)
                        ? "[git] Could not auto-detect. Set path manually or ensure 'git' is on PATH."
                        : $"[git] Using: {_gitPath}");
                }
                if (GUILayout.Button("Test Git", GUILayout.Width(80)))
                {
                    AppendLog(RunGit("--version", captureOnly:false, allowInteractive:false));
                }
            }
        }

        // ---------------- UI: Repo setup / First push ----------------
        void RepoSetupBox()
        {
            _firstPushFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_firstPushFoldout, "Repository / First Push Wizard");
            if (_firstPushFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Remote (origin):", GUILayout.Width(110));
                        _repoUrl = EditorGUILayout.TextField(_repoUrl ?? "");
                        if (GUILayout.Button("Set Remote", GUILayout.Width(100)))
                        {
                            if (!IsGitRepo()) AppendLog(RunGit("init -b main"));
                            RunGit("remote remove origin", true, ignoreErrors:true);
                            AppendLog(RunGit($"remote add origin \"{_repoUrl}\"", captureOnly:false, allowInteractive:false, ignoreErrors:true));
                            AppendLog(RunGit("remote -v"));
                        }
                        if (GUILayout.Button("Open Remote", GUILayout.Width(100)))
                        {
                            TryOpenUrl(_repoUrl);
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Init Repo (if needed)")) {
                            if (!IsGitRepo()) {
                                AppendLog(RunGit("init -b main"));
                                AppendLog("[git] Initialized new repository on 'main'.");
                            } else {
                                AppendLog("[git] Repo already initialized.");
                            }
                            RefreshBranchesAndStatus();
                        }

                        if (GUILayout.Button("Configure Name/Email"))
                        {
                            var name = SafeTrim(RunGit("config user.name", captureOnly:true));
                            var mail = SafeTrim(RunGit("config user.email", captureOnly:true));
                            if (string.IsNullOrEmpty(name)) AppendLog("[git] user.name not set. Use terminal: git config user.name \"Your Name\"");
                            else AppendLog($"[git] user.name = {name}");
                            if (string.IsNullOrEmpty(mail)) AppendLog("[git] user.email not set. Use terminal: git config user.email you@example.com");
                            else AppendLog($"[git] user.email = {mail}");
                        }

                        if (GUILayout.Button("Fetch"))
                        {
                            AppendLog(RunGit("fetch --all --prune"));
                            RefreshBranchesAndStatus();
                        }
                        if (GUILayout.Button("Pull"))
                        {
                            AppendLog(RunGit("pull --ff-only", captureOnly:false, allowInteractive:true, ignoreErrors:true));
                            RefreshBranchesAndStatus();
                        }
                        if (GUILayout.Button("Push"))
                        {
                            AppendLog(PushSmart());
                            RefreshBranchesAndStatus();
                        }
                    }

                    if (GUILayout.Button("First Push: Add All → Commit → Set Upstream → Push"))
                    {
                        EnsureGitIgnore();
                        AppendLog(RunGit("add -A"));
                        var msg = string.IsNullOrWhiteSpace(_commitMsg) ? "Initial commit" : EscapeQuotes(_commitMsg);
                        AppendLog(RunGit($"commit -m \"{msg}\"", ignoreErrors:true));
                        AppendLog(PushSmart(forceSetUpstream:true));
                        _commitMsg = "";
                        RefreshBranchesAndStatus();
                    }
                    EditorGUILayout.HelpBox(
                        "If push/auth fails, click 'Open Remote' to confirm URL, or open a terminal in project folder and run:\n" +
                        "  git config user.name \"Your Name\"\n" +
                        "  git config user.email you@example.com\n" +
                        "  git push -u origin <branch>\n" +
                        "On Windows, Git Credential Manager should prompt once you push.",
                        MessageType.Info);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ---------------- UI: Commit / Push ----------------
        void CommitBox()
        {
            GUILayout.Label("Commit & Push", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                GUILayout.Label("Commit Message");
                _commitMsg = EditorGUILayout.TextArea(_commitMsg ?? "", style, GUILayout.MinHeight(48));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add All", GUILayout.Width(100)))
                        AppendLog(RunGit("add -A"));

                    EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_commitMsg));
                    if (GUILayout.Button("Commit", GUILayout.Width(120)))
                    {
                        var msg = EscapeQuotes(_commitMsg);
                        AppendLog(RunGit($"commit -m \"{msg}\"", ignoreErrors:true));
                        _commitMsg = "";
                        RefreshBranchesAndStatus();
                    }
                    EditorGUI.EndDisabledGroup();

                    if (GUILayout.Button("Commit & Push", GUILayout.Width(150)))
                    {
                        EnsureGitIgnore();
                        if (!string.IsNullOrWhiteSpace(_commitMsg))
                        {
                            AppendLog(RunGit("add -A"));
                            var msg = EscapeQuotes(_commitMsg);
                            AppendLog(RunGit($"commit -m \"{msg}\"", ignoreErrors:true));
                            _commitMsg = "";
                        }
                        AppendLog(PushSmart());
                        RefreshBranchesAndStatus();
                    }

                    if (GUILayout.Button("Status", GUILayout.Width(100)))
                        RefreshBranchesAndStatus();
                }

                // Short status & ahead/behind
                if (!string.IsNullOrEmpty(_statusShort))
                {
                    EditorGUILayout.Space(2);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Branch:", GUILayout.Width(54));
                        GUILayout.Label(string.IsNullOrEmpty(_currentBranch) ? "-" : _currentBranch, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (!string.IsNullOrEmpty(_aheadBehind))
                            GUILayout.Label(_aheadBehind, EditorStyles.miniLabel);
                    }
                    EditorGUILayout.HelpBox(_statusShort, MessageType.None);
                }
            }
        }
        // Add inside the WSUltimateGitHubWindow class
        void AppendLog(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;

            // Append to the window log
            if (!string.IsNullOrEmpty(_log)) _log += "\n";
            _log += msg.TrimEnd();

            // Echo to Unity Console for convenience
            if (msg.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                msg.IndexOf("EXCEPTION", StringComparison.OrdinalIgnoreCase) >= 0)
                UnityEngine.Debug.LogError(msg);
            else
                UnityEngine.Debug.Log(msg);

            Repaint();
        }


        // ---------------- UI: Branches ----------------
        void BranchBox()
        {
            GUILayout.Label("Branches", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _branchIndex = Mathf.Clamp(_branchIndex, 0, Mathf.Max(0, _localBranches.Length - 1));
                    _branchIndex = EditorGUILayout.Popup("Local", _branchIndex, _localBranches);

                    if (GUILayout.Button("Checkout", GUILayout.Width(100)) && _localBranches.Length > 0)
                    {
                        var b = CleanBranchName(_localBranches[_branchIndex]);
                        AppendLog(RunGit($"checkout \"{b}\""));
                        RefreshBranchesAndStatus();
                    }

                    if (GUILayout.Button("Delete", GUILayout.Width(100)) && _localBranches.Length > 0)
                    {
                        var b = CleanBranchName(_localBranches[_branchIndex]);
                        if (EditorUtility.DisplayDialog("Delete Branch?",
                            $"Delete local branch '{b}'? (must be merged)", "Delete", "Cancel"))
                        {
                            AppendLog(RunGit($"branch -d \"{b}\"", ignoreErrors:true));
                            RefreshBranchesAndStatus();
                        }
                    }

                    if (GUILayout.Button("Merge (selected → current)", GUILayout.Width(200)) && _localBranches.Length > 0)
                    {
                        var b = CleanBranchName(_localBranches[_branchIndex]);
                        AppendLog(RunGit($"merge --no-ff \"{b}\"", ignoreErrors:true));
                        RefreshBranchesAndStatus();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _newBranch = EditorGUILayout.TextField("Create", _newBranch ?? "");
                    EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_newBranch));
                    if (GUILayout.Button("Create & Switch", GUILayout.Width(150)))
                    {
                        var b = _newBranch.Trim();
                        AppendLog(RunGit($"checkout -b \"{b}\"", ignoreErrors:true));
                        _newBranch = "";
                        RefreshBranchesAndStatus();
                    }
                    EditorGUI.EndDisabledGroup();

                    if (GUILayout.Button("Push Current → origin", GUILayout.Width(200)))
                    {
                        AppendLog(PushSmart());
                        RefreshBranchesAndStatus();
                    }
                }
            }
        }

        // ---------------- UI: Mirror ----------------
        void MirrorBox()
        {
            GUILayout.Label("Public Mirror (All Code) → ZIP", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "Creates a clean zip of code + optional minimal project metadata. Excludes Library/Temp/.git, etc.",
                    MessageType.None);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _zipNameHint = EditorGUILayout.TextField("Base name", _zipNameHint);
                    _includeGitIgnored = EditorGUILayout.ToggleLeft("Include gitignored", _includeGitIgnored, GUILayout.Width(160));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _mirrorIncludeProjectSettings = EditorGUILayout.ToggleLeft("Include minimal ProjectSettings", _mirrorIncludeProjectSettings, GUILayout.Width(220));
                    _mirrorIncludeManifest = EditorGUILayout.ToggleLeft("Include Packages/manifest.json", _mirrorIncludeManifest, GUILayout.Width(240));
                }

                if (GUILayout.Button("Create Code ZIP (safe)", GUILayout.Height(28)))
                {
                    try
                    {
                        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var baseName = string.IsNullOrWhiteSpace(_zipNameHint) ? "PublicMirror" : _zipNameHint.Trim();
                        var zipPath = Path.Combine(_projectRoot, $"{baseName}_{ts}_AllCode.zip");
                        var count = CreateCodeZip(zipPath, !_includeGitIgnored);
                        AppendLog($"[mirror] Wrote {zipPath} (files: {count})");
                        EditorUtility.RevealInFinder(zipPath);
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"[mirror][ERROR] {ex.Message}");
                    }
                }

                _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, "Advanced");
                if (_advancedFoldout)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Open project .gitignore"))
                            EnsureGitIgnore(openAfter:true);

                        if (GUILayout.Button("Open terminal here"))
                            OpenTerminalHere();
                    }
                }
            }
        }

        // ---------------- UI: Output ----------------
        void OutputBox()
        {
            GUILayout.Label("Output", EditorStyles.boldLabel);
            var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            _log = EditorGUILayout.TextArea(_log ?? "", style, GUILayout.MinHeight(160));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy Output"))
                    EditorGUIUtility.systemCopyBuffer = _log ?? "";
                if (GUILayout.Button("Clear"))
                    _log = "";
            }
        }

        // ---------------- Logic ----------------

        string GetProjectRoot()
        {
            var assets = Application.dataPath.Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(assets, ".."));
        }

        void TryLoadRepoInfo()
        {
            if (IsGitRepo())
            {
                _repoUrl = SafeTrim(RunGit("remote get-url origin", captureOnly:true, ignoreErrors:true)) ?? _repoUrl;
                _currentBranch = SafeTrim(RunGit("rev-parse --abbrev-ref HEAD", captureOnly:true, ignoreErrors:true));
            }
            else
            {
                _currentBranch = "(not initialized)";
            }
        }

        void RefreshBranchesAndStatus()
        {
            // branches
            var branches = RunGit("branch --list", captureOnly:true, ignoreErrors:true);
            if (!string.IsNullOrEmpty(branches))
            {
                _localBranches = branches.Split(new[] { '\r','\n' }, StringSplitOptions.RemoveEmptyEntries);
                var cur = _localBranches.Select((b,i) => (b,i)).FirstOrDefault(t => t.b.StartsWith("*"));
                _branchIndex = cur == default ? 0 : cur.i;
            }
            _currentBranch = SafeTrim(RunGit("rev-parse --abbrev-ref HEAD", captureOnly:true, ignoreErrors:true));

            // status short
            _statusShort = SafeTrim(RunGit("status -sb", captureOnly:true, ignoreErrors:true)) ?? "(no status)";
            _aheadBehind = DetectAheadBehind();
            AppendLog(RunGit("status", captureOnly:false, ignoreErrors:true));
            Repaint();
        }

        string DetectAheadBehind()
        {
            // returns something like "↑2 ↓1" or "" if no upstream
            var upstream = RunGit("rev-parse --abbrev-ref --symbolic-full-name @{u}", captureOnly:true, ignoreErrors:true);
            if (string.IsNullOrWhiteSpace(upstream) || upstream.Contains("fatal")) return "";
            var lr = RunGit("rev-list --left-right --count @{u}...HEAD", captureOnly:true, ignoreErrors:true);
            if (string.IsNullOrWhiteSpace(lr)) return "";
            var parts = lr.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[0], out var behind) && int.TryParse(parts[1], out var ahead))
            {
                string s = "";
                if (ahead > 0) s += $"↑{ahead} ";
                if (behind > 0) s += $"↓{behind}";
                return s.Trim();
            }
            return "";
        }

        string PushSmart(bool forceSetUpstream = false)
        {
            // Decide branch
            var branch = SafeTrim(RunGit("rev-parse --abbrev-ref HEAD", captureOnly:true, ignoreErrors:true));
            if (string.IsNullOrWhiteSpace(branch) || branch == "HEAD") branch = "main";

            // Ensure remote
            var remote = SafeTrim(RunGit("remote get-url origin", captureOnly:true, ignoreErrors:true));
            if (string.IsNullOrWhiteSpace(remote))
            {
                if (!string.IsNullOrWhiteSpace(_repoUrl))
                {
                    RunGit("remote remove origin", true, ignoreErrors:true);
                    AppendLog(RunGit($"remote add origin \"{_repoUrl}\"", ignoreErrors:true));
                }
                else
                {
                    return "[git] No 'origin' remote configured.";
                }
            }

            // Check upstream
            var upstream = RunGit("rev-parse --abbrev-ref --symbolic-full-name @{u}", captureOnly:true, ignoreErrors:true);
            var needUpstream = forceSetUpstream || string.IsNullOrWhiteSpace(upstream) || upstream.Contains("fatal");
            var cmd = needUpstream ? $"push -u origin {branch}" : "push";
            return RunGit(cmd, captureOnly:false, allowInteractive:true, ignoreErrors:true);
        }

        bool IsGitRepo()
        {
            var res = RunGit("rev-parse --is-inside-work-tree", captureOnly:true, ignoreErrors:true);
            return !string.IsNullOrWhiteSpace(res) && res.Trim().StartsWith("true", StringComparison.OrdinalIgnoreCase);
        }

        string CleanBranchName(string raw) => raw.Trim('*', ' ', '\t');

        void EnsureGitIgnore(bool openAfter = false)
        {
            var path = Path.Combine(_projectRoot, ".gitignore");
            if (!File.Exists(path))
            {
                var txt = string.Join("\n", new[]{
                    "# Unity",
                    "Library/",
                    "Temp/",
                    "Obj/",
                    "Build/",
                    "Builds/",
                    "Logs/",
                    "UserSettings/",
                    "MemoryCaptures/",
                    "*.csproj","*.sln","*.user","*.pidb","*.svd","*.pdb","*.mdb","*.opendb",".sysinfo",
                    "",
                    "# IDE",
                    ".idea/",".vs/",".vscode/",
                    "",
                    "# OS",
                    ".DS_Store","Thumbs.db"
                });
                WriteUtf8NoBom(path, txt);
                AppendLog("[git] Wrote .gitignore");
            }
            if (openAfter) EditorUtility.RevealInFinder(path);
        }

        int CreateCodeZip(string zipPath, bool respectGitIgnore)
        {
            var files = EnumerateProjectFiles(_projectRoot, respectGitIgnore)
                        .Where(f => CodeExts.Contains(Path.GetExtension(f)))
                        .ToList();

            if (_mirrorIncludeProjectSettings)
            {
                foreach (var rel in ProjectMetaFiles)
                {
                    var abs = Path.Combine(_projectRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(abs)) files.Add(rel.Replace('\\','/'));
                }
            }
            if (_mirrorIncludeManifest)
            {
                var rel = "Packages/manifest.json";
                var abs = Path.Combine(_projectRoot, rel);
                if (File.Exists(abs)) files.Add(rel.Replace('\\','/'));
            }

            files = files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (File.Exists(zipPath)) File.Delete(zipPath);
            using (var fs = new FileStream(zipPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen:false, entryNameEncoding: Encoding.UTF8))
            {
                foreach (var rel in files)
                {
                    var abs = Path.Combine(_projectRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(abs)) continue;
                    var entry = zip.CreateEntry(rel.Replace('\\','/'), CompressionLevelAlias.Optimal);
                    using (var zs = entry.Open())
                    using (var src = File.OpenRead(abs))
                        src.CopyTo(zs);
                }
            }
            return files.Count;
        }

        IEnumerable<string> EnumerateProjectFiles(string root, bool respectGitIgnore)
        {
            var all = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                               .Select(abs => MakeRelative(root, abs))
                               .Where(rel => !IsExcluded(rel));

            if (respectGitIgnore && Directory.Exists(Path.Combine(root, ".git")))
            {
                var listed = RunGit("ls-files --cached --modified --others --exclude-standard", captureOnly:true, ignoreErrors:true);
                var set = new HashSet<string>(
                    (listed ?? "").Split(new[] {'\r','\n'}, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(Norm),
                    StringComparer.OrdinalIgnoreCase);
                return all.Where(a => set.Contains(Norm(a)));
            }
            return all;

            bool IsExcluded(string rel)
            {
                rel = rel.Replace('\\','/');
                foreach (var ex in ExcludeDirs)
                    if (rel.StartsWith(ex + "/", StringComparison.OrdinalIgnoreCase)) return true;
                if (Path.GetFileName(rel).StartsWith(".")) return true;
                return false;
            }
        }

        string MakeRelative(string root, string abs)
        {
            var rel = abs.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return rel.Replace('\\','/');
        }

        string Norm(string rel) => rel.Replace('\\','/');

        // Main git runner
        string RunGit(string args, bool captureOnly = false, bool allowInteractive = false, bool ignoreErrors = false)
        {
            try
            {
                var exe = string.IsNullOrWhiteSpace(_gitPath) ? "git" : _gitPath;
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = _projectRoot,
                    UseShellExecute = allowInteractive,        // allow UI prompts for auth when pushing/pulling
                    RedirectStandardOutput = !allowInteractive,
                    RedirectStandardError  = !allowInteractive,
                    CreateNoWindow = !allowInteractive
                };
                if (!allowInteractive)
                {
                    psi.StandardOutputEncoding = Encoding.UTF8;
                    psi.StandardErrorEncoding  = Encoding.UTF8;
                }

                using var p = Process.Start(psi);
                string stdout = "", stderr = "";
                if (!allowInteractive)
                {
                    stdout = p.StandardOutput.ReadToEnd();
                    stderr = p.StandardError.ReadToEnd();
                }
                p.WaitForExit();

                if (!ignoreErrors && p.ExitCode != 0)
                    return $"[git ERROR] git {args}\n{stderr}";

                return captureOnly
                    ? (string.IsNullOrEmpty(stdout) ? stderr : stdout)
                    : $"$ git {args}\n{stdout}{stderr}";
            }
            catch (Exception e)
            {
                return $"[git EXCEPTION] git {args}\n{e.Message}";
            }
        }

        static void WriteUtf8NoBom(string path, string content)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var enc = new UTF8Encoding(false);
            File.WriteAllText(path, content ?? "", enc);
        }

        string AutoDetectGitPath()
        {
            // Common Windows installs
            var candidates = new[]
            {
                @"C:\Program Files\Git\bin\git.exe",
                @"C:\Program Files\Git\cmd\git.exe",
                @"C:\Program Files (x86)\Git\bin\git.exe",
                @"C:\Program Files (x86)\Git\cmd\git.exe"
            };
            foreach (var c in candidates)
                if (File.Exists(c)) return c;
            return "";
        }

        void OpenTerminalHere()
        {
#if UNITY_EDITOR_WIN
            // Open Windows Terminal or fallback to cmd.exe
            var wt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                  @"Microsoft\WindowsApps\wt.exe");
            if (File.Exists(wt))
                Process.Start(new ProcessStartInfo { FileName = wt, Arguments = $" -d \"{_projectRoot}\"", UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $"/K cd /d \"{_projectRoot}\"", UseShellExecute = true });
#else
            // macOS/Linux: open default terminal at folder
            Process.Start(new ProcessStartInfo { FileName = "open", Arguments = $"\"{_projectRoot}\"", UseShellExecute = true });
#endif
        }

        static void TryOpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try { Process.Start(new ProcessStartInfo{ FileName = url, UseShellExecute = true }); } catch { }
        }

        static string EscapeQuotes(string s) => (s ?? "").Replace("\"", "\\\"");
        static string SafeTrim(string s) => string.IsNullOrEmpty(s) ? s : s.Trim('\r','\n','\t',' ');
    }
}
#endif
