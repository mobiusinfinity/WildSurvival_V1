// Assets/_WildSurvival/Code/Editor/Tools/WSUltimateGitHubWindow/WSUltimateGitHubWindow.cs
// Wild Survival Git Hub Tool v2.1 - FIXED Collection Modified Error
// Complete Git integration with diff capture for AI collaboration

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class WSUltimateGitHubWindow : EditorWindow
{
    const string TOOL_NAME = "Wild Survival Git Hub";
    const string VERSION = "2.1"; // Updated version
    const string MENU_PATH = "Tools/Wild Survival/Git Hub";
    const int DEFAULT_TIMEOUT = 30000;

    [MenuItem(MENU_PATH, false, 100)]
    [MenuItem("Wild Survival/Git & Share/Git Hub", false, 20)]
    public static void ShowWindow()
    {
        var window = GetWindow<WSUltimateGitHubWindow>();
        window.titleContent = new GUIContent("🚀 Git Hub", EditorGUIUtility.IconContent("d_UnityEditor.VersionControl").image);
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    // Core fields
    string _repoRoot = "";
    string _branch = "";
    string _originUrl = "";
    string _mirrorUrl = "";
    string _commitMsg = "";
    string _defaultPushRemote = "origin";
    string _newBranchName = "";
    string _newRemoteName = "";
    string _newRemoteUrl = "";

    // State
    bool _busy = false;
    bool _autoRefresh = true;
    bool _hasUncommittedChanges = false;
    bool _needsRefresh = false; // Flag for deferred refresh
    Vector2 _scrollStatus;
    Vector2 _scrollLog;
    Vector2 _scrollDiff;

    // Collections
    StringBuilder _log;
    List<StatusEntry> _status;
    List<string> _recentCommits;
    string _lastDiffExport = "";

    class StatusEntry
    {
        public string Code;
        public string Path;
        public string Path2;
    }

    void OnEnable()
    {
        // Initialize
        _log = new StringBuilder(4096);
        _status = new List<StatusEntry>();
        _recentCommits = new List<string>();

        // Load preferences
        _commitMsg = EditorPrefs.GetString("WSGH.CommitMsg", "");
        _defaultPushRemote = EditorPrefs.GetString("WSGH.DefaultRemote", "origin");
        _originUrl = EditorPrefs.GetString("WSGH.OriginUrl", "");
        _mirrorUrl = EditorPrefs.GetString("WSGH.MirrorUrl", "");
        _autoRefresh = EditorPrefs.GetBool("WSGH.AutoRefresh", true);

        // Load recent commits
        for (int i = 0; i < 5; i++)
        {
            var msg = EditorPrefs.GetString($"WSGH.Recent{i}", "");
            if (!string.IsNullOrEmpty(msg))
                _recentCommits.Add(msg);
        }

        DetectRepository();
        RefreshAll(true);

        AppendLog($"[{DateTime.Now:HH:mm:ss}] Git Hub v{VERSION} initialized");
    }

    void OnDisable()
    {
        // Save preferences
        EditorPrefs.SetString("WSGH.CommitMsg", _commitMsg);
        EditorPrefs.SetString("WSGH.DefaultRemote", _defaultPushRemote);
        EditorPrefs.SetString("WSGH.OriginUrl", _originUrl);
        EditorPrefs.SetString("WSGH.MirrorUrl", _mirrorUrl);
        EditorPrefs.SetBool("WSGH.AutoRefresh", _autoRefresh);

        // Save recent commits
        for (int i = 0; i < Math.Min(5, _recentCommits.Count); i++)
        {
            EditorPrefs.SetString($"WSGH.Recent{i}", _recentCommits[i]);
        }
    }

    void OnGUI()
    {
        // Handle deferred refresh at the start of OnGUI
        if (_needsRefresh && Event.current.type == EventType.Layout)
        {
            _needsRefresh = false;
            RefreshStatus();
        }

        // Safety checks
        if (_log == null) _log = new StringBuilder(4096);
        if (_status == null) _status = new List<StatusEntry>();

        using (new EditorGUI.DisabledScope(_busy))
        {
            DrawHeader();

            if (string.IsNullOrEmpty(_repoRoot))
            {
                DrawNoRepoView();
                return;
            }

            DrawQuickActions();
            DrawStatusSection();
            DrawCommitSection();
            DrawDiffSection();
            DrawBranchSection();
            DrawRemoteSection();
            DrawToolsSection();
            DrawLogSection();
        }
    }

    void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"🚀 {TOOL_NAME} v{VERSION}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        // Status indicators
        if (_hasUncommittedChanges)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("● Changes", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.green;
            GUILayout.Label("✓ Clean", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }

        GUILayout.Space(10);

        _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(45));

        if (GUILayout.Button("🔄", EditorStyles.toolbarButton, GUILayout.Width(25)))
        {
            RefreshAll();
        }

        EditorGUILayout.EndHorizontal();

        // Repository info
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Repository:", _repoRoot);
            EditorGUILayout.LabelField("Branch:", string.IsNullOrEmpty(_branch) ? "(unknown)" : _branch);

            var hasGit = HasGitInPath(out var gitVersion);
            EditorGUILayout.LabelField("Git:", hasGit ? gitVersion : "Not found in PATH");

            if (!hasGit)
            {
                EditorGUILayout.HelpBox("Git is not installed or not in PATH!", MessageType.Error);
            }
        }
    }

    void DrawQuickActions()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button("📁 Open Folder", GUILayout.Height(25)))
            {
                EditorUtility.RevealInFinder(_repoRoot);
            }

            if (GUILayout.Button("🌐 Open Remote", GUILayout.Height(25)))
            {
                if (!string.IsNullOrEmpty(_originUrl))
                    Application.OpenURL(_originUrl);
            }

            if (GUILayout.Button("📋 Export Status", GUILayout.Height(25)))
            {
                ExportCurrentStatus();
            }

            if (GUILayout.Button("📊 Export Diff", GUILayout.Height(25)))
            {
                ExportFullDiff();
            }

            if (GUILayout.Button("🔧 Git Bash", GUILayout.Height(25)))
            {
                OpenGitBash();
            }
        }
    }

    void DrawStatusSection()
    {
        GUILayout.Label("Working Tree Status", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Stage All", GUILayout.Width(80)))
                    SafeExecute(StageAll);

                if (GUILayout.Button("Unstage All", GUILayout.Width(90)))
                    SafeExecute(UnstageAll);

                if (GUILayout.Button("Discard All", GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Discard Changes",
                        "This will permanently discard ALL local changes!",
                        "Discard", "Cancel"))
                    {
                        SafeExecute(DiscardAll);
                    }
                }

                GUILayout.FlexibleSpace();

                if (_status.Count > 0)
                {
                    GUILayout.Label($"{_status.Count} changes", EditorStyles.miniLabel);
                }
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollStatus, GUILayout.Height(150)))
            {
                _scrollStatus = scroll.scrollPosition;

                if (_status.Count == 0)
                {
                    EditorGUILayout.HelpBox("No changes. Working tree is clean.", MessageType.Info);
                }
                else
                {
                    // CRITICAL FIX: Create a copy of the list to iterate
                    var statusCopy = new List<StatusEntry>(_status);

                    foreach (var entry in statusCopy)
                    {
                        DrawStatusEntry(entry);
                    }
                }
            }
        }
    }

    void DrawStatusEntry(StatusEntry entry)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            // Status code with color
            var color = GetStatusColor(entry.Code);
            GUI.color = color;
            GUILayout.Label(entry.Code, GUILayout.Width(30));
            GUI.color = Color.white;

            // File path
            GUILayout.Label(entry.Path);
            if (!string.IsNullOrEmpty(entry.Path2))
                GUILayout.Label($"→ {entry.Path2}");

            GUILayout.FlexibleSpace();

            // Actions - use deferred refresh
            if (GUILayout.Button("Stage", GUILayout.Width(50)))
            {
                StageFileDeferred(entry.Path);
            }

            if (GUILayout.Button("Unstage", GUILayout.Width(60)))
            {
                UnstageFileDeferred(entry.Path);
            }

            if (GUILayout.Button("Discard", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Discard Changes",
                    $"Discard changes to {entry.Path}?",
                    "Discard", "Cancel"))
                {
                    DiscardFileDeferred(entry.Path);
                }
            }
        }
    }

    void DrawCommitSection()
    {
        GUILayout.Label("Commit", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // Recent commits dropdown
            if (_recentCommits.Count > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Recent:", GUILayout.Width(50));
                    if (GUILayout.Button("Select ▼", GUILayout.Width(80)))
                    {
                        var menu = new GenericMenu();
                        foreach (var msg in _recentCommits)
                        {
                            var m = msg; // Capture for closure
                            menu.AddItem(new GUIContent(msg), false, () => _commitMsg = m);
                        }
                        menu.ShowAsContext();
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            // Commit message
            GUILayout.Label("Message:");
            _commitMsg = EditorGUILayout.TextArea(_commitMsg, GUILayout.Height(60));

            // Commit buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                bool canCommit = !string.IsNullOrWhiteSpace(_commitMsg);

                GUI.enabled = canCommit;
                if (GUILayout.Button("💾 Commit", GUILayout.Height(30)))
                {
                    SafeExecute(Commit);
                }

                if (GUILayout.Button("💾📤 Commit + Push", GUILayout.Height(30)))
                {
                    SafeExecute(CommitAndPush);
                }
                GUI.enabled = true;

                if (GUILayout.Button("📤 Push", GUILayout.Width(60), GUILayout.Height(30)))
                {
                    SafeExecute(Push);
                }

                if (GUILayout.Button("📥 Pull", GUILayout.Width(60), GUILayout.Height(30)))
                {
                    SafeExecute(Pull);
                }
            }

            // Git flow helpers
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show Log"))
                    SafeExecute(ShowLog);

                if (GUILayout.Button("Stash Changes"))
                    SafeExecute(StashChanges);

                if (GUILayout.Button("Pop Stash"))
                    SafeExecute(PopStash);
            }
        }
    }

    void DrawDiffSection()
    {
        GUILayout.Label("Diff Export (for Claude/AI)", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("📊 Generate Full Diff", GUILayout.Height(25)))
                {
                    GenerateFullDiff();
                }

                if (GUILayout.Button("📋 Copy to Clipboard", GUILayout.Height(25)))
                {
                    if (!string.IsNullOrEmpty(_lastDiffExport))
                    {
                        GUIUtility.systemCopyBuffer = _lastDiffExport;
                        AppendLog("Diff copied to clipboard!");
                    }
                }

                if (GUILayout.Button("💾 Save to File", GUILayout.Height(25)))
                {
                    SaveDiffToFile();
                }
            }

            if (!string.IsNullOrEmpty(_lastDiffExport))
            {
                GUILayout.Label($"Last export: {_lastDiffExport.Length} characters", EditorStyles.miniLabel);

                using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollDiff, GUILayout.Height(100)))
                {
                    _scrollDiff = scroll.scrollPosition;
                    EditorGUILayout.TextArea(_lastDiffExport, GUILayout.ExpandHeight(true));
                }
            }
        }
    }

    void DrawBranchSection()
    {
        GUILayout.Label("Branches", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("New branch:", GUILayout.Width(80));
                _newBranchName = EditorGUILayout.TextField(_newBranchName);

                if (GUILayout.Button("Create", GUILayout.Width(60)))
                {
                    if (!string.IsNullOrWhiteSpace(_newBranchName))
                    {
                        SafeExecute(() => CreateBranch(_newBranchName));
                    }
                }
            }

            if (GUILayout.Button("Switch Branch..."))
            {
                ShowBranchMenu();
            }
        }
    }

    void DrawRemoteSection()
    {
        GUILayout.Label("Remotes", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // Origin
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("origin:", GUILayout.Width(50));
                _originUrl = EditorGUILayout.TextField(_originUrl);

                if (GUILayout.Button("Set", GUILayout.Width(40)))
                    SafeExecute(SetOrigin);

                if (GUILayout.Button("Fetch", GUILayout.Width(50)))
                    SafeExecute(() => Fetch("origin"));
            }

            // Mirror
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("mirror:", GUILayout.Width(50));
                _mirrorUrl = EditorGUILayout.TextField(_mirrorUrl);

                if (GUILayout.Button("Set", GUILayout.Width(40)))
                    SafeExecute(SetMirror);

                if (GUILayout.Button("Push", GUILayout.Width(50)))
                    SafeExecute(PushToMirror);
            }

            // Add new
            using (new EditorGUILayout.HorizontalScope())
            {
                _newRemoteName = EditorGUILayout.TextField("Name", _newRemoteName, GUILayout.Width(100));
                _newRemoteUrl = EditorGUILayout.TextField("URL", _newRemoteUrl);

                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    if (!string.IsNullOrWhiteSpace(_newRemoteName) && !string.IsNullOrWhiteSpace(_newRemoteUrl))
                    {
                        SafeExecute(() => AddRemote(_newRemoteName, _newRemoteUrl));
                    }
                }
            }
        }
    }

    void DrawToolsSection()
    {
        GUILayout.Label("Tools & Configuration", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Configure VS Code"))
                    SafeExecute(ConfigureVSCode);

                if (GUILayout.Button("Write .gitignore"))
                    SafeExecute(WriteGitIgnore);

                if (GUILayout.Button("Write .gitattributes"))
                    SafeExecute(WriteGitAttributes);
            }
        }
    }

    void DrawLogSection()
    {
        GUILayout.Label("Log", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollLog, GUILayout.Height(100)))
            {
                _scrollLog = scroll.scrollPosition;
                EditorGUILayout.TextArea(_log.ToString(), GUILayout.ExpandHeight(true));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear"))
                    _log.Clear();

                if (GUILayout.Button("Export"))
                {
                    var path = Path.Combine(Application.dataPath, "..", "Logs", $"GitLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, _log.ToString());
                    EditorUtility.RevealInFinder(path);
                }
            }
        }
    }

    void DrawNoRepoView()
    {
        EditorGUILayout.HelpBox("No Git repository detected!", MessageType.Warning);

        if (GUILayout.Button("Initialize Repository", GUILayout.Height(40)))
        {
            SafeExecute(InitRepository);
        }
    }

    // ========== DEFERRED OPERATIONS (FIX) ==========

    void StageFileDeferred(string path)
    {
        RunGit($"add \"{path}\"");
        _needsRefresh = true;
        Repaint();
    }

    void UnstageFileDeferred(string path)
    {
        RunGit($"reset HEAD \"{path}\"");
        _needsRefresh = true;
        Repaint();
    }

    void DiscardFileDeferred(string path)
    {
        RunGit($"checkout -- \"{path}\"");
        _needsRefresh = true;
        Repaint();
    }

    // ========== CORE OPERATIONS ==========

    void DetectRepository()
    {
        if (RunGit("rev-parse --show-toplevel", out var output, 2000))
        {
            _repoRoot = output.Trim().Replace('\\', '/');
        }
        else
        {
            var dir = new DirectoryInfo(Application.dataPath);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
                dir = dir.Parent;
            _repoRoot = dir?.FullName?.Replace('\\', '/') ?? "";
        }
    }

    void RefreshAll(bool silent = false)
    {
        if (_busy) return;

        if (!silent) AppendLog("Refreshing...");

        // Get branch
        if (RunGit("branch --show-current", out var branch))
            _branch = branch.Trim();

        // Get remotes
        if (RunGit("remote get-url origin", out var origin))
            _originUrl = origin.Trim();

        if (RunGit("remote get-url mirror", out var mirror))
            _mirrorUrl = mirror.Trim();

        RefreshStatus();
    }

    void RefreshStatus()
    {
        _status.Clear();

        if (RunGit("status --porcelain", out var output))
        {
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Length < 3) continue;

                var code = line.Substring(0, 2);
                var path = line.Substring(3);

                // Handle renames
                string path2 = null;
                if (path.Contains(" -> "))
                {
                    var parts = path.Split(new[] { " -> " }, StringSplitOptions.None);
                    path = parts[0];
                    path2 = parts[1];
                }

                _status.Add(new StatusEntry { Code = code, Path = path, Path2 = path2 });
            }

            _hasUncommittedChanges = _status.Count > 0;
        }

        Repaint();
    }

    // ========== GIT COMMANDS ==========

    void InitRepository()
    {
        if (RunGit("init"))
        {
            AppendLog("Repository initialized!");
            WriteGitIgnore();
            WriteGitAttributes();
            RefreshAll();
        }
    }

    void StageAll()
    {
        RunGit("add -A");
        if (_autoRefresh) RefreshStatus();
    }

    void UnstageAll()
    {
        RunGit("reset");
        if (_autoRefresh) RefreshStatus();
    }

    void DiscardAll()
    {
        RunGit("checkout -- .");
        RunGit("clean -fd");
        if (_autoRefresh) RefreshStatus();
    }

    void StageFile(string path)
    {
        RunGit($"add \"{path}\"");
        if (_autoRefresh) RefreshStatus();
    }

    void UnstageFile(string path)
    {
        RunGit($"reset HEAD \"{path}\"");
        if (_autoRefresh) RefreshStatus();
    }

    void DiscardFile(string path)
    {
        RunGit($"checkout -- \"{path}\"");
        if (_autoRefresh) RefreshStatus();
    }

    void Commit()
    {
        if (string.IsNullOrWhiteSpace(_commitMsg))
        {
            AppendLog("ERROR: Commit message is empty!");
            return;
        }

        // Auto-stage if nothing staged
        RunGit("diff --cached --name-only", out var staged);
        if (string.IsNullOrWhiteSpace(staged))
        {
            AppendLog("Auto-staging all changes...");
            StageAll();
        }

        var msg = _commitMsg.Replace("\"", "\\\"");
        if (RunGit($"commit -m \"{msg}\""))
        {
            // Add to recent
            _recentCommits.Remove(_commitMsg);
            _recentCommits.Insert(0, _commitMsg);
            if (_recentCommits.Count > 5)
                _recentCommits.RemoveAt(5);

            AppendLog($"Committed: {_commitMsg}");
            _commitMsg = "";

            if (_autoRefresh) RefreshAll();
        }
    }

    void CommitAndPush()
    {
        Commit();
        if (!_hasUncommittedChanges)
            Push();
    }

    void Push()
    {
        AppendLog($"Pushing to {_defaultPushRemote}...");
        if (RunGit($"push {_defaultPushRemote} {_branch}"))
        {
            AppendLog($"✅ Pushed to {_defaultPushRemote}");
        }
        else
        {
            AppendLog($"❌ Push failed. Try manual push in Git Bash.");
        }
    }

    void Pull()
    {
        RunGit($"pull {_defaultPushRemote} {_branch}");
        if (_autoRefresh) RefreshAll();
    }

    void Fetch(string remote)
    {
        RunGit($"fetch {remote}");
        AppendLog($"Fetched from {remote}");
    }

    void StashChanges()
    {
        RunGit("stash");
        if (_autoRefresh) RefreshStatus();
    }

    void PopStash()
    {
        RunGit("stash pop");
        if (_autoRefresh) RefreshStatus();
    }

    void ShowLog()
    {
        if (RunGit("log --oneline -20", out var output))
        {
            AppendLog("=== Recent Commits ===");
            AppendLog(output);
        }
    }

    void CreateBranch(string name)
    {
        RunGit($"checkout -b {name}");
        _newBranchName = "";
        RefreshAll();
    }

    void ShowBranchMenu()
    {
        if (RunGit("branch", out var output))
        {
            var menu = new GenericMenu();
            var lines = output.Split('\n');

            foreach (var line in lines)
            {
                var branch = line.Trim().TrimStart('*').Trim();
                if (!string.IsNullOrEmpty(branch))
                {
                    var b = branch; // Capture
                    menu.AddItem(new GUIContent(branch), branch == _branch, () =>
                    {
                        SafeExecute(() => SwitchBranch(b));
                    });
                }
            }

            menu.ShowAsContext();
        }
    }

    void SwitchBranch(string branch)
    {
        RunGit($"checkout {branch}");
        RefreshAll();
    }

    void SetOrigin()
    {
        if (string.IsNullOrWhiteSpace(_originUrl)) return;

        RunGit("remote", out var remotes);
        if (remotes.Contains("origin"))
            RunGit($"remote set-url origin \"{_originUrl}\"");
        else
            RunGit($"remote add origin \"{_originUrl}\"");

        AppendLog($"Origin set to: {_originUrl}");
    }

    void SetMirror()
    {
        if (string.IsNullOrWhiteSpace(_mirrorUrl)) return;

        RunGit("remote", out var remotes);
        if (remotes.Contains("mirror"))
            RunGit($"remote set-url mirror \"{_mirrorUrl}\"");
        else
            RunGit($"remote add mirror \"{_mirrorUrl}\"");

        AppendLog($"Mirror set to: {_mirrorUrl}");
    }

    void PushToMirror()
    {
        RunGit($"push mirror {_branch}");
        AppendLog("Pushed to mirror");
    }

    void AddRemote(string name, string url)
    {
        RunGit($"remote add {name} \"{url}\"");
        AppendLog($"Added remote: {name}");
        _newRemoteName = "";
        _newRemoteUrl = "";
    }

    // ========== DIFF EXPORT ==========

    void GenerateFullDiff()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Git Diff Export - {DateTime.Now}");
        sb.AppendLine($"Repository: {_repoRoot}");
        sb.AppendLine($"Branch: {_branch}");
        sb.AppendLine();

        // Get status
        if (RunGit("status", out var status))
        {
            sb.AppendLine("## Status");
            sb.AppendLine("```");
            sb.AppendLine(status);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Get diff
        if (RunGit("diff", out var diff))
        {
            sb.AppendLine("## Changes (Unstaged)");
            sb.AppendLine("```diff");
            sb.AppendLine(diff);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Get staged diff
        if (RunGit("diff --cached", out var staged))
        {
            sb.AppendLine("## Changes (Staged)");
            sb.AppendLine("```diff");
            sb.AppendLine(staged);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Get untracked files
        if (RunGit("ls-files --others --exclude-standard", out var untracked))
        {
            sb.AppendLine("## Untracked Files");
            sb.AppendLine("```");
            sb.AppendLine(untracked);
            sb.AppendLine("```");
        }

        _lastDiffExport = sb.ToString();
        AppendLog($"Diff generated: {_lastDiffExport.Length} characters");
    }

    void ExportCurrentStatus()
    {
        GenerateFullDiff();
        GUIUtility.systemCopyBuffer = _lastDiffExport;
        AppendLog("Status copied to clipboard!");
    }

    void ExportFullDiff()
    {
        GenerateFullDiff();
        GUIUtility.systemCopyBuffer = _lastDiffExport;
        AppendLog("Full diff copied to clipboard!");
    }

    void SaveDiffToFile()
    {
        if (string.IsNullOrEmpty(_lastDiffExport))
        {
            GenerateFullDiff();
        }

        var path = Path.Combine(Application.dataPath, "..", "GitExports");
        Directory.CreateDirectory(path);

        var filename = $"diff_{DateTime.Now:yyyyMMdd_HHmmss}.md";
        var fullPath = Path.Combine(path, filename);

        File.WriteAllText(fullPath, _lastDiffExport);
        EditorUtility.RevealInFinder(fullPath);
        AppendLog($"Diff saved to: {filename}");
    }

    // ========== UTILITIES ==========

    void ConfigureVSCode()
    {
        RunGit("config diff.tool vscode");
        RunGit("config difftool.vscode.cmd \"code --wait --diff \\\"$LOCAL\\\" \\\"$REMOTE\\\"\"");
        RunGit("config merge.tool vscode");
        RunGit("config mergetool.vscode.cmd \"code --wait \\\"$MERGED\\\"\"");
        AppendLog("VS Code configured as diff/merge tool");
    }

    void WriteGitIgnore()
    {
        var path = Path.Combine(_repoRoot, ".gitignore");
        var content = @"# Unity
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emory[Cc]aptures/
GitExports/

# Unity files
*.csproj
*.sln
*.pidb.meta
*.pdb.meta

# Visual Studio
.vs/
*.vsconfig

# OS
.DS_Store
Thumbs.db";

        File.WriteAllText(path, content);
        RunGit("add .gitignore");
        AppendLog("Created .gitignore");
    }

    void WriteGitAttributes()
    {
        var path = Path.Combine(_repoRoot, ".gitattributes");
        var content = @"# Unity
*.cs text eol=lf
*.shader text eol=lf
*.meta text eol=lf
*.prefab text eol=lf
*.unity text eol=lf
*.asset text eol=lf

# Binary
*.png binary
*.jpg binary
*.fbx binary";

        File.WriteAllText(path, content);
        RunGit("add .gitattributes");
        AppendLog("Created .gitattributes");
    }

    void OpenGitBash()
    {
        var paths = new[]
        {
            @"C:\Program Files\Git\git-bash.exe",
            @"C:\Program Files (x86)\Git\git-bash.exe"
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = _repoRoot,
                    UseShellExecute = true
                };
                Process.Start(psi);
                return;
            }
        }

        AppendLog("Git Bash not found");
    }

    // ========== HELPERS ==========

    void SafeExecute(Action action)
    {
        if (_busy) return;

        _busy = true;
        try
        {
            action();
        }
        catch (Exception e)
        {
            AppendLog($"ERROR: {e.Message}");
            Debug.LogError(e);
        }
        finally
        {
            _busy = false;
            Repaint();
        }
    }

    bool RunGit(string args, out string output, int timeout = DEFAULT_TIMEOUT)
    {
        output = "";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = string.IsNullOrEmpty(_repoRoot) ? Application.dataPath : _repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit(timeout);

                if (process.ExitCode != 0)
                {
                    if (!string.IsNullOrEmpty(error) && !error.Contains("No such remote"))
                        AppendLog($"Git error: {error}");
                    return false;
                }

                return true;
            }
        }
        catch (Exception e)
        {
            AppendLog($"Failed to run git: {e.Message}");
            return false;
        }
    }

    bool RunGit(string args)
    {
        return RunGit(args, out _);
    }

    void AppendLog(string message)
    {
        if (_log == null) _log = new StringBuilder(4096);

        _log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");

        // Limit log size
        if (_log.Length > 10000)
        {
            var text = _log.ToString();
            _log.Clear();
            _log.Append(text.Substring(text.Length - 8000));
        }

        Repaint();
    }

    bool HasGitInPath(out string version)
    {
        version = "";

        if (RunGit("--version", out var output, 2000))
        {
            version = output.Trim();
            return true;
        }

        return false;
    }

    Color GetStatusColor(string code)
    {
        if (code.StartsWith("M")) return Color.yellow;
        if (code.StartsWith("A")) return Color.green;
        if (code.StartsWith("D")) return Color.red;
        if (code.StartsWith("R")) return Color.cyan;
        if (code == "??") return Color.gray;
        return Color.white;
    }
}
#endif