// Assets/WildSurvival/Editor/WSUltimateGitHubWindow.cs
// Unity 6+ (Editor only). Single-file "Ultimate Git Hub" panel.
// v1.2 - branches, stash, per-file staging, VSCode difftool, commit UX.

#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace WildSurvival.EditorTools
{
    public class WSUltimateGitHubWindow : EditorWindow
    {
        const string Version = "1.2";
        const string MenuPath = "Wild Survival/Git & Share/Git & Share Hub";
        [MenuItem(MenuPath, false, 20)]
        public static void Open() => GetWindow<WSUltimateGitHubWindow>("WS • Git Hub").Show();

        // Persisted UI
        const string PREF_REPO = "WSGitHub.RepoRoot";
        const string PREF_COMMIT = "WSGitHub.CommitMsg";
        const string PREF_BRANCH = "WSGitHub.Branch";
        const string PREF_REMOTE = "WSGitHub.Remote";

        string _repoRoot;
        string _commitMessage;
        string _currentBranch;
        string _remoteName = "origin";
        string _remoteUrl;

        // status cache
        class FileEntry
        {
            public string code;   // e.g. "M ", "??", " D", "A "
            public string path;   // relative path
        }
        readonly List<FileEntry> _changed = new();
        readonly List<FileEntry> _untracked = new();
        readonly HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);

        // misc ui
        string _newBranch = "";
        string _stashMessage = "";
        string _userName = "";
        string _userEmail = "";
        Vector2 _scroll;
        StringBuilder _log = new StringBuilder(2048);
        DateTime _lastRefresh;

        void OnEnable()
        {
            _repoRoot = EditorPrefs.GetString(PREF_REPO, TryFindRepoRoot(Application.dataPath));
            _commitMessage = EditorPrefs.GetString(PREF_COMMIT, "");
            _remoteName = EditorPrefs.GetString(PREF_REMOTE, "origin");
            _currentBranch = EditorPrefs.GetString(PREF_BRANCH, "main");
            AppendLog($"[Init] v{Version}. Repo: {_repoRoot}");
            SafeRefresh();
        }

        void OnDisable()
        {
            EditorPrefs.SetString(PREF_REPO, _repoRoot ?? "");
            EditorPrefs.SetString(PREF_COMMIT, _commitMessage ?? "");
            EditorPrefs.SetString(PREF_REMOTE, _remoteName ?? "origin");
            EditorPrefs.SetString(PREF_BRANCH, _currentBranch ?? "");
        }

        static string TryFindRepoRoot(string hintPath)
        {
            try
            {
                var d = new DirectoryInfo(hintPath);
                while (d != null && d.Exists)
                {
                    var gitDir = Path.Combine(d.FullName, ".git");
                    if (Directory.Exists(gitDir) || File.Exists(gitDir)) return d.FullName.Replace('\\', '/');
                    d = d.Parent;
                }
            }
            catch { }
            return "";
        }

        void OnGUI()
        {
            using var scope = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scope.scrollPosition;

            GUILayout.Space(6);
            Header();

            GUILayout.Space(6);
            RepoBox();

            GUILayout.Space(4);
            RowButtons();

            GUILayout.Space(10);
            StatusBox();

            GUILayout.Space(10);
            BranchingBox();

            GUILayout.Space(10);
            StashBox();

            GUILayout.Space(10);
            ToolsBox();

            GUILayout.Space(10);
            LogBox();
        }

        void Header()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Ultimate Git Hub", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"v{Version}", EditorStyles.miniLabel);
                if (GUILayout.Button("Refresh", GUILayout.Width(80))) SafeRefresh();
            }
            EditorGUILayout.HelpBox("One-stop Git panel for staging, committing, pulling, pushing, branching and more.\nTip: Ctrl/Cmd + Enter commits.", MessageType.Info);
        }

        void RepoBox()
        {
            EditorGUILayout.LabelField("Repository", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Repo Root", GUILayout.Width(80));
                    var next = EditorGUILayout.TextField(_repoRoot);
                    if (next != _repoRoot) { _repoRoot = next; EditorPrefs.SetString(PREF_REPO, _repoRoot); }
                    if (GUILayout.Button("Browse", GUILayout.Width(80)))
                    {
                        var pick = EditorUtility.OpenFolderPanel("Select Repo Root (contains .git)", _repoRoot ?? "", "");
                        if (!string.IsNullOrEmpty(pick)) { _repoRoot = pick.Replace('\\', '/'); SafeRefresh(); }
                    }
                    if (GUILayout.Button("Open Folder", GUILayout.Width(100)) && Directory.Exists(_repoRoot))
                        EditorUtility.RevealInFinder(_repoRoot);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Branch", GUILayout.Width(80));
                    EditorGUILayout.SelectableLabel(_currentBranch ?? "(unknown)", GUILayout.Height(18));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Remote", GUILayout.Width(60));
                    _remoteName = EditorGUILayout.TextField(_remoteName, GUILayout.Width(100));
                    EditorGUILayout.SelectableLabel(_remoteUrl ?? "", GUILayout.Height(18));
                }

                GUILayout.Space(2);
                EditorGUILayout.LabelField("Commit Message", EditorStyles.boldLabel);
                var nextMsg = EditorGUILayout.TextArea(_commitMessage, GUILayout.MinHeight(48));
                if (nextMsg != _commitMessage) { _commitMessage = nextMsg; EditorPrefs.SetString(PREF_COMMIT, _commitMessage); }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !string.IsNullOrWhiteSpace(_commitMessage);
                    if (GUILayout.Button("Commit (Ctrl/Cmd+Enter)", GUILayout.Height(28)))
                        Commit(_commitMessage);
                    GUI.enabled = true;

                    if (GUILayout.Button("Push", GUILayout.Width(100), GUILayout.Height(28)))
                        Push();

                    if (GUILayout.Button("Pull --rebase", GUILayout.Width(120), GUILayout.Height(28)))
                        PullRebase();

                    if (GUILayout.Button("Fetch", GUILayout.Width(80), GUILayout.Height(28)))
                        GitSafe($"fetch {_remoteName}");
                }

                HandleCommitShortcut();
            }
        }

        void HandleCommitShortcut()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
#if UNITY_EDITOR_OSX
                if (e.command) { Commit(_commitMessage); e.Use(); }
#else
                if (e.control) { Commit(_commitMessage); e.Use(); }
#endif
            }
        }

        void RowButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Stage All", GUILayout.Height(24))) StageAll();
                if (GUILayout.Button("Unstage All", GUILayout.Height(24))) UnstageAll();
                if (GUILayout.Button("Discard Working Changes (careful)", GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("Discard ALL changes?",
                        "This will revert modified files and remove untracked files.\nThis cannot be undone from here.",
                        "Yes, discard", "Cancel"))
                    {
                        DiscardAll();
                    }
                }
                if (GUILayout.Button("Open in External Git GUI", GUILayout.Height(24)))
                    OpenExternalGitGUI();
            }
        }

        void StatusBox()
        {
            EditorGUILayout.LabelField("Changes", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (string.IsNullOrEmpty(_repoRoot) || !HasGit())
                {
                    EditorGUILayout.HelpBox("Set Repo Root and ensure Git is installed.", MessageType.Warning);
                    return;
                }

                EditorGUILayout.LabelField("Modified / Deleted / Added (tracked)", EditorStyles.miniBoldLabel);
                if (_changed.Count == 0) EditorGUILayout.LabelField("• None");
                foreach (var f in _changed)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool picked = _selected.Contains(f.path);
                        bool next = EditorGUILayout.ToggleLeft($"{f.code}  {f.path}", picked);
                        if (next && !picked) _selected.Add(f.path);
                        else if (!next && picked) _selected.Remove(f.path);

                        if (GUILayout.Button("Diff", GUILayout.Width(60)))
                            DiffFile(f.path);
                    }
                }

                GUILayout.Space(6);
                EditorGUILayout.LabelField("Untracked", EditorStyles.miniBoldLabel);
                if (_untracked.Count == 0) EditorGUILayout.LabelField("• None");
                foreach (var f in _untracked)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool picked = _selected.Contains(f.path);
                        bool next = EditorGUILayout.ToggleLeft($"{f.code}  {f.path}", picked);
                        if (next && !picked) _selected.Add(f.path);
                        else if (!next && picked) _selected.Remove(f.path);

                        if (GUILayout.Button("Open", GUILayout.Width(60)))
                            OpenPath(f.path);
                    }
                }

                GUILayout.Space(6);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Stage Selected")) StageSelected();
                    if (GUILayout.Button("Unstage Selected")) UnstageSelected();
                    if (GUILayout.Button("Discard Selected"))
                    {
                        if (EditorUtility.DisplayDialog("Discard selected?", "Revert changes and/or remove untracked for selected items?", "Yes", "Cancel"))
                            DiscardSelected();
                    }
                    if (GUILayout.Button("Clear Selection")) _selected.Clear();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"Last refresh: {_lastRefresh:HH:mm:ss}", GUILayout.Width(180));
                }
            }
        }

        void BranchingBox()
        {
            EditorGUILayout.LabelField("Branching", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newBranch = EditorGUILayout.TextField("New Branch", _newBranch);
                    GUI.enabled = !string.IsNullOrWhiteSpace(_newBranch);
                    if (GUILayout.Button("Create & Switch", GUILayout.Width(150)))
                    {
                        GitSafe($"switch -c \"{_newBranch}\"");
                        _newBranch = "";
                        SafeRefresh();
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button("Switch to…", GUILayout.Width(110))) PromptSwitchBranch();
                }
            }
        }

        void StashBox()
        {
            EditorGUILayout.LabelField("Stash", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _stashMessage = EditorGUILayout.TextField("Message", _stashMessage);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Stash (incl. untracked)"))
                        GitSafe($"stash push -u -m \"{_stashMessage}\"");
                    if (GUILayout.Button("List")) GitSafe("stash list");
                    if (GUILayout.Button("Pop")) GitSafe("stash pop");
                    if (GUILayout.Button("Drop")) GitSafe("stash drop");
                }
            }
        }

        void ToolsBox()
        {
            EditorGUILayout.LabelField("Tools & Config", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Configure VS Code as difftool/mergetool"))
                        ConfigureVSCodeDiffMerge();

                    if (GUILayout.Button("Open .gitignore"))
                        OpenPath(".gitignore");
                }

                // user config (local)
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Local user (this repo only)", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _userName = EditorGUILayout.TextField("user.name", _userName);
                    if (GUILayout.Button("Read", GUILayout.Width(60))) _userName = GitRead("config user.name");
                    if (GUILayout.Button("Write", GUILayout.Width(60))) GitSafe($"config user.name \"{_userName}\"");
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _userEmail = EditorGUILayout.TextField("user.email", _userEmail);
                    if (GUILayout.Button("Read", GUILayout.Width(60))) _userEmail = GitRead("config user.email");
                    if (GUILayout.Button("Write", GUILayout.Width(60))) GitSafe($"config user.email \"{_userEmail}\"");
                }
            }
        }

        void LogBox()
        {
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var txt = _log.ToString();
                EditorGUILayout.TextArea(txt, GUILayout.MinHeight(120));
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Copy Log"))
                    {
                        EditorGUIUtility.systemCopyBuffer = txt + "\n\nPs: Why did the commit go to therapy? Too many unresolved conflicts.";
                        AppendLog("Log copied to clipboard. 🧠");
                    }
                    if (GUILayout.Button("Clear")) { _log.Clear(); }
                }
            }
        }

        // --- Actions ---

        void Commit(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                AppendLog("Commit message is empty.");
                return;
            }
            // Stage nothing? Git will error. We try anyway so user sees message.
            GitSafe("commit -m " + Quote(msg));
            SafeRefresh();
        }

        void Push()
        {
            // Set upstream if needed
            var hasUpstream = !string.IsNullOrEmpty(GitRead("rev-parse --abbrev-ref --symbolic-full-name @{u}", out var ok, quiet: true)) && ok;
            if (!hasUpstream)
                GitSafe($"push -u {_remoteName} {_currentBranch}");
            else
                GitSafe("push");
            SafeRefresh();
        }

        void PullRebase()
        {
            GitSafe($"pull --rebase {_remoteName} {_currentBranch}");
            SafeRefresh();
        }

        void StageAll() { GitSafe("add -A"); SafeRefresh(); }
        void UnstageAll() { GitSafe("reset"); SafeRefresh(); }

        void StageSelected()
        {
            foreach (var p in _selected.ToArray())
                GitSafe($"add -- {Quote(p)}");
            SafeRefresh();
        }

        void UnstageSelected()
        {
            foreach (var p in _selected.ToArray())
                GitSafe($"restore --staged -- {Quote(p)}");
            SafeRefresh();
        }

        void DiscardSelected()
        {
            foreach (var p in _selected.ToArray())
            {
                // If untracked -> remove
                if (_untracked.Any(u => u.path.Equals(p, StringComparison.OrdinalIgnoreCase)))
                    SafeDeleteFile(p);
                else
                    GitSafe($"restore -- {Quote(p)}");
            }
            SafeRefresh();
        }

        void DiscardAll()
        {
            GitSafe("restore .");
            GitSafe("clean -fd");
            SafeRefresh();
        }

        void DiffFile(string rel)
        {
            // Try difftool, fallback to 'git diff'
            var configured = GitRead("config diff.tool");
            if (!string.IsNullOrEmpty(configured))
                GitSafe($"difftool --no-prompt -- {Quote(rel)}");
            else
                GitSafe($"diff -- {Quote(rel)}");
        }

        void OpenExternalGitGUI()
        {
            // Try GitHub Desktop or fallback to 'git gui'
            try
            {
                // GitHub Desktop uses URL protocol 'x-github-client://'
                if (!string.IsNullOrEmpty(_repoRoot))
                {
                    // Try to open repo folder; Desktop usually hooks the protocol from shell.
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _repoRoot,
                        UseShellExecute = true
                    });
                }
                else
                {
                    GitSafe("gui");
                }
            }
            catch (Exception e) { AppendLog("GUI open failed: " + e.Message); }
        }

        void PromptSwitchBranch()
        {
            var list = GitRead("branch --format=\"%(refname:short)\"").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var picked = EditorUtility.DisplayDialogComplex("Switch Branch", "Choose a branch in the console list below.", "Show in Log", "Cancel", "Manual Enter");
            if (picked == 0)
            {
                AppendLog("Branches:\n" + string.Join("\n", list));
            }
            else if (picked == 2)
            {
                var name = EditorUtility.DisplayDialogComplex("Manual", "Enter exact branch name in the Console (use 'SwitchBranch: <name>').", "OK", "Cancel", "Help");
                AppendLog("SwitchBranch: (type in Console) e.g. SwitchBranch: feature/my-awesome-work");
            }
        }

        // --- Refresh & parsing ---
        void SafeRefresh()
        {
            if (string.IsNullOrEmpty(_repoRoot))
            {
                _repoRoot = TryFindRepoRoot(Application.dataPath);
                if (string.IsNullOrEmpty(_repoRoot))
                {
                    AppendLog("Repo root not set. Select a folder that contains '.git'.");
                    return;
                }
            }
            if (!HasGit()) { AppendLog("Git not found on PATH."); return; }

            _currentBranch = GitRead("rev-parse --abbrev-ref HEAD", out var ok1)?.Trim();
            if (!ok1) _currentBranch = "(unknown)";
            _remoteUrl = GitRead($"remote get-url {_remoteName}", out _, quiet: true);

            ReadUserConfig();
            ParseStatus();
            _lastRefresh = DateTime.Now;
        }

        void ReadUserConfig()
        {
            _userName = GitRead("config user.name",  out _, quiet: true);
            _userEmail = GitRead("config user.email", out _, quiet: true);
        }

        void ParseStatus()
        {
            _changed.Clear(); _untracked.Clear();

            var raw = GitRead("status --porcelain");
            var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var ln in lines)
            {
                // Format: XY<space>path
                if (ln.Length < 4) continue;
                var code = ln.Substring(0, 2);
                var path = ln.Substring(3).Trim();
                var entry = new FileEntry { code = code, path = path };

                if (code == "??") _untracked.Add(entry);
                else _changed.Add(entry);
            }
        }

        // --- Utilities ---

        void ConfigureVSCodeDiffMerge()
        {
            // Configure for the current repo (local). VS Code must be installed and on PATH as 'code'.
            // This is a friendly default that avoids the "No supported VCS diff tools" warning path.
            GitSafe("config diff.tool vscode");
            GitSafe("config difftool.vscode.cmd \"code --wait --diff \\\"$LOCAL\\\" \\\"$REMOTE\\\"\"");
            GitSafe("config merge.tool vscode");
            GitSafe("config mergetool.vscode.cmd \"code --wait \\\"$MERGED\\\"\"");
            AppendLog("Configured VS Code as diff/merge tool for this repo.");
        }

        static void SafeDeleteFile(string relPath)
        {
            try
            {
                var abs = Path.GetFullPath(relPath);
                if (File.Exists(abs)) File.Delete(abs);
                else if (Directory.Exists(abs)) Directory.Delete(abs, true);
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("Failed delete: " + relPath + " → " + e.Message); }
        }

        void OpenPath(string rel)
        {
            try
            {
                var abs = Path.Combine(_repoRoot, rel).Replace('\\', '/');
                if (File.Exists(abs) || Directory.Exists(abs))
                    Process.Start(new ProcessStartInfo { FileName = abs, UseShellExecute = true });
                else
                    AppendLog("Path not found: " + rel);
            }
            catch (Exception e) { AppendLog("Open failed: " + e.Message); }
        }

        bool HasGit()
        {
            try
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                p.Start();
                p.WaitForExit(4000);
                return p.ExitCode == 0;
            }
            catch { return false; }
        }

        string Quote(string s) => "\"" + s.Replace("\"", "\\\"") + "\"";

        void GitSafe(string args)
        {
            var ok = RunGit(args, out var so, out var se);
            AppendLog($"$ git {args}\n{so}{(string.IsNullOrEmpty(se) ? "" : "\n" + se)}");
            if (!ok) AppendLog("Command failed.");
        }

        string GitRead(string args,  out bool ok, bool quiet = false)
        {
            ok = RunGit(args, out var so, out var se);
            if (!quiet) AppendLog($"$ git {args}\n{so}{(string.IsNullOrEmpty(se) ? "" : "\n" + se)}");
            return ok ? so.Trim() : "";
        }
        string GitRead(string args) => GitRead(args,  out _, quiet: false);

        // Drop-in replacement: put both into WSUltimateGitHubWindow.cs
        // Overload without timeout -> uses the default 120s
        bool RunGit(string args, out string stdout, out string stderr)
        {
            return RunGit(args, out stdout, out stderr, 120000);
        }

        // Overload with explicit timeout
        bool RunGit(string args, out string stdout, out string stderr, int timeoutMs)
        {
            stdout = "";
            stderr = "";
            if (string.IsNullOrEmpty(_repoRoot)) { stderr = "Repo root not set."; return false; }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = args,
                    WorkingDirectory = _repoRoot,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (var p = new System.Diagnostics.Process { StartInfo = psi })
                {
                    var so = new StringBuilder();
                    var se = new StringBuilder();

                    p.OutputDataReceived += (_, e) => { if (e.Data != null) so.AppendLine(e.Data); };
                    p.ErrorDataReceived += (_, e) => { if (e.Data != null) se.AppendLine(e.Data); };

                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();

                    if (!p.WaitForExit(timeoutMs))
                    {
                        try { p.Kill(); } catch { /* ignore */ }
                    }

                    stdout = so.ToString();
                    stderr = se.ToString();

                    return p.ExitCode == 0;
                }
            }
            catch (Exception e)
            {
                stderr = e.Message;
                return false;
            }
        }


        void AppendLog(string line)
        {
            _log.AppendLine(line);
            Repaint();
        }
    }
}
#endif
