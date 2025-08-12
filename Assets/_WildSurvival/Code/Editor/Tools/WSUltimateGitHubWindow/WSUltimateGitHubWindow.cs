// Assets/WildSurvival/Editor/WSUltimateGitHubWindow/WSUltimateGitHubWindow.cs
// Unity 6 Editor-only. Single-file "Git & Share Hub" window.
// v1.3: stage helpers, commit message field, quick commit+push,
//       branch create/checkout, VS Code diff/merge config,
//       origin+mirror remotes, .gitattributes writer, richer log.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.EditorTools
{
    public class WSUltimateGitHubWindow : EditorWindow
    {
        const string MenuPath = "Wild Survival/Git & Share/Git & Share Hub";
        const int DefaultTimeoutMs = 120000;

        [MenuItem(MenuPath, false, 20)]
        public static void Open()
        {
            var w = GetWindow<WSUltimateGitHubWindow>("Git & Share Hub");
            w.Show();
        }

        string _commitMsg = EditorPrefs.GetString("WSUGit.CommitMsg", "");
        string _defaultPushRemote = EditorPrefs.GetString("WSUGit.DefaultPushRemote", "origin");

        string _newRemoteName = "mirror";
        string _newRemoteUrl = "";
        string _newBranchName = "";

        bool _autoRefreshAfterActions = true;
        Vector2 _varCommitMsgScroll;

        // State
        string _repoRoot = "";
        string _branch = "";
        string _originUrl = "";
        string _mirrorUrl = "";
        //string _commitMsg = "";
        bool _busy = false;
        Vector2 _svStatus;
        Vector2 _svLog;
        readonly StringBuilder _log = new StringBuilder(4096);

        // Parsed status entries (from `git status --porcelain -z`)
        class StatusEntry
        {
            public string Code;   // e.g., "M ", "A ", " D", "??"
            public string Path;   // path (or old path for renames)
            public string Path2;  // new path if rename/copy
        }
        readonly List<StatusEntry> _status = new List<StatusEntry>();

        // --------- Unity lifecycle ---------
        void OnEnable()
        {
            // Load persisted prefs (unify on WSUGit.*)
            _originUrl = EditorPrefs.GetString("WSGitHub.originUrl", _originUrl);
            _mirrorUrl = EditorPrefs.GetString("WSGitHub.mirrorUrl", _mirrorUrl);
            _commitMsg = EditorPrefs.GetString("WSUGit.CommitMsg", _commitMsg);

            DetectRepoRoot();
            RefreshAll(silent: true);
            AppendLog($"[Init] v1.4. Repo: {(_repoRoot == "" ? "(not detected)" : _repoRoot)}");
        }

        void OnDisable()
        {
            // Persist small fields
            EditorPrefs.SetString("WSUGit.CommitMsg", _commitMsg ?? "");
            EditorPrefs.SetString("WSUGit.DefaultPushRemote", _defaultPushRemote ?? "origin");
        }


        string BuildTipsText(string repoRoot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Tips & helpers:");
            sb.AppendLine("• Configure Git LFS (for large binaries):");
            sb.AppendLine("    git lfs install");
            sb.AppendLine("    git lfs track \"*.psd\" \"*.fbx\" \"*.wav\"");
            sb.AppendLine();
            sb.AppendLine("• Set VS Code as diff/merge tool:");
            sb.AppendLine("    git config diff.tool vscode");
            sb.AppendLine("    git config difftool.vscode.cmd \"code --wait --diff \\\"$LOCAL\\\" \\\"$REMOTE\\\"\"");
            sb.AppendLine("    git config merge.tool vscode");
            sb.AppendLine("    git config mergetool.vscode.cmd \"code --wait \\\"$MERGED\\\"\"");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(repoRoot))
                sb.AppendLine("• Repo root: " + repoRoot);
            sb.AppendLine("• Emoji tip: Windows + . opens the emoji panel 🙂");
            return sb.ToString();
        }


        // --------- UI ---------
        void OnGUI()
        {
            using (new EditorGUI.DisabledScope(_busy))
            {
                DrawHeader();

                GUILayout.Label("Status", EditorStyles.boldLabel);

                if (GUILayout.Button("Refresh"))
                    RefreshStatus();

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Stage All")) { RunGit("add -A", out _, out _); RefreshStatus(); }
                    if (GUILayout.Button("Unstage All")) { RunGit("reset HEAD -- .", out _, out _); RefreshStatus(); }
                }


                if (string.IsNullOrEmpty(_repoRoot))
                {
                    EditorGUILayout.HelpBox(BuildTipsText(_repoRoot), MessageType.None);

                    if (GUILayout.Button("Initialize Git (git init)"))
                        SafeAction(GitInit);
                    GUILayout.Space(6);
                    DrawLog();
                    return;
                }


                DrawSummary();
                GUILayout.Space(4);
                DrawStatusArea();
                GUILayout.Space(6);
                DrawCommitArea();
                GUILayout.Space(6);
                DrawBranchArea();
                GUILayout.Space(6);
                DrawRemotesArea();
                GUILayout.Space(6);
                DrawToolsArea();
                GUILayout.Space(6);
                DrawLog();
            }
        }

        void DrawHeader()
        {
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("WS • Git & Share Hub", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                RefreshAll();
            EditorGUILayout.EndHorizontal();

            var hasGit = HasGitInPath(out var gitVersion);
            EditorGUILayout.LabelField("Git:", hasGit ? gitVersion : "Not found in PATH");
            if (!hasGit)
            {
                EditorGUILayout.HelpBox("Install Git (and restart Unity) OR ensure 'git' is in PATH.\nhttps://git-scm.com/downloads", MessageType.Error);
            }
        }

        void DrawSummary()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Repository", _repoRoot);
                EditorGUILayout.LabelField("Branch", string.IsNullOrEmpty(_branch) ? "(unknown)" : _branch);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Repo Folder")) EditorUtility.RevealInFinder(_repoRoot.Replace('\\', '/'));
                if (!string.IsNullOrEmpty(_originUrl) && Uri.IsWellFormedUriString(_originUrl, UriKind.Absolute))
                {
                    if (GUILayout.Button("Open Remote (origin)")) Application.OpenURL(_originUrl);
                }
                if (GUILayout.Button("Git Bash Here"))
                {
                    var bash = GuessGitBash();
                    if (!string.IsNullOrEmpty(bash))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = bash,
                            WorkingDirectory = _repoRoot,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Not Found", "Couldn't find Git Bash. Is Git for Windows installed?", "OK");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawStatusArea()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Working Tree", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Stage All (git add -A)", GUILayout.Width(180)))
                    SafeAction(StageAll);
                if (GUILayout.Button("Unstage All (git reset)", GUILayout.Width(180)))
                    SafeAction(UnstageAll);
                if (GUILayout.Button("Discard Local Changes (git restore .)", GUILayout.Width(230)))
                    SafeAction(DiscardAll);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(4);
                using (var sv = new EditorGUILayout.ScrollViewScope(_svStatus, GUILayout.MinHeight(100)))
                {
                    _svStatus = sv.scrollPosition;
                    if (_status.Count == 0)
                    {
                        GUILayout.Label("No changes. Working tree clean.");
                    }
                    else
                    {
                        foreach (var s in _status)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(s.Code, GUILayout.Width(40));
                            GUILayout.Label(s.Path);
                            if (!string.IsNullOrEmpty(s.Path2))
                                GUILayout.Label("→ " + s.Path2);
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Stage", GUILayout.Width(60)))
                                SafeAction(() => StagePath(s.Path2 ?? s.Path));
                            if (GUILayout.Button("Unstage", GUILayout.Width(70)))
                                SafeAction(() => UnstagePath(s.Path2 ?? s.Path));
                            if (GUILayout.Button("Discard", GUILayout.Width(70)))
                                SafeAction(() => DiscardPath(s.Path2 ?? s.Path));
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }



        void DrawCommitArea()
        {
            GUILayout.Space(6);
            GUILayout.Label("Commit", EditorStyles.boldLabel);

            // Commit message (persist live)
            EditorGUI.BeginChangeCheck();
            _varCommitMsgScroll = EditorGUILayout.BeginScrollView(_varCommitMsgScroll, GUILayout.MinHeight(60), GUILayout.MaxHeight(120));
            _commitMsg = EditorGUILayout.TextArea(_commitMsg ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString("WSUGit.CommitMsg", _commitMsg ?? "");

            using (new GUILayout.HorizontalScope())
            {
                // Commit (auto-stage if nothing staged)
                GUI.enabled = !string.IsNullOrWhiteSpace(_commitMsg);
                if (GUILayout.Button("Commit", GUILayout.Width(120)))
                {
                    RunGit("diff --cached --name-only", out var staged, out _);
                    if (string.IsNullOrWhiteSpace(staged))
                        RunGit("add -A", out _, out _);

                    var msgEsc = EscapeQuotes(_commitMsg.Trim());
                    if (!RunGit($"commit -m \"{msgEsc}\"", out var so, out var se))
                        AppendLog("Commit failed: " + se);
                    else
                    {
                        AppendLog("Commit OK:\n" + so);
                        _commitMsg = "";
                        EditorPrefs.SetString("WSUGit.CommitMsg", "");
                        if (_autoRefreshAfterActions) RefreshStatus();
                    }
                }
                GUI.enabled = true;

                // One-click combo
                if (GUILayout.Button($"Commit + Push → {(_defaultPushRemote ?? "origin")}", GUILayout.Width(220)))
                {
                    // do the same autostage-commit, then push default
                    RunGit("diff --cached --name-only", out var staged, out _);
                    if (string.IsNullOrWhiteSpace(staged))
                        RunGit("add -A", out _, out _);

                    var msg = string.IsNullOrWhiteSpace(_commitMsg) ? "update" : _commitMsg.Trim();
                    if (RunGit($"commit -m \"{EscapeQuotes(msg)}\"", out var so, out var se))
                    {
                        AppendLog(so);
                        _commitMsg = "";
                        EditorPrefs.SetString("WSUGit.CommitMsg", "");
                        PushDefault();
                    }
                    else AppendLog(se);

                    if (_autoRefreshAfterActions) RefreshStatus();
                }

                if (GUILayout.Button("Push", GUILayout.Width(80)))
                    PushDefault();

                if (GUILayout.Button("Pull", GUILayout.Width(80)))
                    SafeAction(Pull);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show Last 50 Commits"))
                    SafeAction(LogLast50);
                if (GUILayout.Button("Tag Current (v0.0.1)"))
                    SafeAction(() => CreateTag("v0.0.1"));
            }
        }


        void DrawBranchArea()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Branches", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("New branch:", GUILayout.Width(90));
                _newBranchName = EditorGUILayout.TextField(_newBranchName, GUILayout.MinWidth(120));
                if (GUILayout.Button("Create & Checkout", GUILayout.Width(160)) && !string.IsNullOrWhiteSpace(_newBranchName))
                    SafeAction(() => CreateAndCheckout(_newBranchName));
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Checkout… (popup)"))
                    SafeAction(ShowBranchPopup);
            }
        }


        void DrawRemotesArea()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Remotes", EditorStyles.boldLabel);

                // origin
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("origin:", GUILayout.Width(60));
                _originUrl = EditorGUILayout.TextField(_originUrl);
                if (GUILayout.Button("Save", GUILayout.Width(70)))
                    SafeAction(SetOriginRemote);
                if (GUILayout.Button("Fetch", GUILayout.Width(70)))
                    SafeAction(() => FetchRemote("origin"));
                if (!string.IsNullOrEmpty(_originUrl) && Uri.IsWellFormedUriString(_originUrl, UriKind.Absolute))
                    if (GUILayout.Button("Open", GUILayout.Width(60))) Application.OpenURL(_originUrl);
                EditorGUILayout.EndHorizontal();

                // mirror
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("mirror:", GUILayout.Width(60));
                _mirrorUrl = EditorGUILayout.TextField(_mirrorUrl);
                if (GUILayout.Button("Save", GUILayout.Width(70)))
                    SafeAction(SetMirrorRemote);
                if (GUILayout.Button("Push → mirror", GUILayout.Width(120)))
                    SafeAction(PushMirror);
                EditorGUILayout.EndHorizontal();

                // existing remotes list + default
                GUILayout.Space(6);
                if (TryGetRemotes(out var list))
                {
                    EditorGUILayout.LabelField("Configured remotes:");
                    foreach (var r in list)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label($"{r.name} — {r.url}", GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("Set Default", GUILayout.Width(100)))
                            SetDefaultPushRemote(r.name);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    GUILayout.Label("No remotes configured.");
                }

                // Add new remote
                GUILayout.Space(6);
                EditorGUILayout.LabelField("Add remote:");
                EditorGUILayout.BeginHorizontal();
                _newRemoteName = EditorGUILayout.TextField("Name", _newRemoteName);
                _newRemoteUrl = EditorGUILayout.TextField("URL", _newRemoteUrl);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Remote"))
                    SafeAction(() => AddRemote(_newRemoteName.Trim(), _newRemoteUrl.Trim()));
                if (GUILayout.Button("Add Mirror (old repo)"))
                    SafeAction(() => AddRemote("mirror", "https://github.com/mobiusinfinity/WildSurvival.git"));
                EditorGUILayout.EndHorizontal();
            }
        }


        void DrawToolsArea()
        {
            GUILayout.Space(10);
            GUILayout.Label("Diff/Merge Tools", EditorStyles.boldLabel);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Config VS Code diff/merge"))
                    ConfigVsCodeDiffMerge();
                if (GUILayout.Button("Verify"))
                    VerifyVsCodeDiffMerge();

            }
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(10);
                GUILayout.Label("Line Endings", EditorStyles.boldLabel);
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Windows style (CRLF)")) FixLineEndings(true);
                    if (GUILayout.Button("Mac/Linux style (LF)")) FixLineEndings(false);
                }

                EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Configure VS Code as diff/merge"))
                    SafeAction(ConfigVsCodeDiffMerge);
                if (GUILayout.Button("Write .gitattributes (line endings)"))
                    SafeAction(WriteGitattributes);
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawLog()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
                using (var sv = new EditorGUILayout.ScrollViewScope(_svLog, GUILayout.MinHeight(120)))
                {
                    _svLog = sv.scrollPosition;
                    GUILayout.TextArea(_log.ToString(), GUILayout.ExpandHeight(true));
                }
                if (GUILayout.Button("Clear Log")) { _log.Length = 0; }
            }
        }

        // --------- Actions ----------
        void SafeAction(Action a)
        {
            if (_busy) return;
            _busy = true;
            try { a(); }
            catch (Exception e) { AppendLog("[ERROR] " + e.Message); }
            finally { _busy = false; Repaint(); }
        }

        void DetectRepoRoot()
        {
            // Use `git rev-parse --show-toplevel` when available
            if (RunGit("rev-parse --show-toplevel", out var so, out var se))
            {
                _repoRoot = so.Trim().Replace('\\', '/');
            }
            else
            {
                // Fallback: walk up from project folder looking for .git
                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
                    dir = dir.Parent;
                _repoRoot = dir?.FullName?.Replace('\\', '/') ?? "";
            }
            if (string.IsNullOrEmpty(_repoRoot))
                AppendLog("[WARN] No .git found. Initialize to enable features.");
        }

        void RefreshAll(bool silent = false)
        {
            if (!_busy)
            {
                if (!silent) AppendLog("$ git rev-parse --abbrev-ref HEAD");
                if (RunGit("rev-parse --abbrev-ref HEAD", out var so, out var se))
                {
                    _branch = so.Trim();
                }
                else _branch = "(unknown)";

                RefreshRemotes();
                RefreshStatus();
            }
        }

        void RefreshStatus()
        {
            _status.Clear();
            // Newline-based porcelain avoids the -z/NUL parsing with line readers
            if (!RunGit("status --porcelain", out var so, out var se))
            {
                AppendLog(se);
                return;
            }

            var lines = so.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rec in lines)
            {
                if (rec.Length < 3) continue;

                var code = rec.Substring(0, 2);
                var rest = rec.Substring(3).Trim();

                string path = rest;
                string path2 = null;

                // Handle rename/copy: "R100 old -> new" (or "R  old -> new")
                const string arrow = " -> ";
                int idx = rest.IndexOf(arrow, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    path = rest.Substring(0, idx).Trim();
                    path2 = rest.Substring(idx + arrow.Length).Trim();
                }

                _status.Add(new StatusEntry { Code = code, Path = path, Path2 = path2 });
            }
        }


        void VerifyVsCodeDiffMerge()
        {
            RunGit("config --get difftool.vscode.cmd", out var dOut, out _);
            RunGit("config --get mergetool.vscode.cmd", out var mOut, out _);
            AppendLog("difftool.vscode.cmd = " + (string.IsNullOrWhiteSpace(dOut) ? "<not set>" : dOut.Trim()));
            AppendLog("mergetool.vscode.cmd = " + (string.IsNullOrWhiteSpace(mOut) ? "<not set>" : mOut.Trim()));
        }

        void ConfigVsCodeDiffMerge()
        {
            RunGit("config diff.tool vscode", out _, out _);
            RunGit("config difftool.vscode.cmd \"code --wait --diff \\\"$LOCAL\\\" \\\"$REMOTE\\\"\"", out _, out _);
            RunGit("config merge.tool vscode", out _, out _);
            RunGit("config mergetool.vscode.cmd \"code --wait \\\"$MERGED\\\"\"", out _, out _);
            AppendLog("Configured VS Code as diff/merge tool for this repo.");
        }


        bool TryGetRemotes(out List<(string name, string url)> remotes)
        {
            remotes = new List<(string, string)>();
            if (!RunGit("remote -v", out var so, out var se))
            {
                AppendLog("remote -v failed: " + se);
                return false;
            }

            // Lines look like: "origin https://... (fetch)" and "origin https://... (push)"
            var lines = so.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var ln in lines)
            {
                var parts = ln.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var name = parts[0];
                    var url = parts[1];
                    // keep the first URL we see per remote name
                    if (!remotes.Any(r => r.name == name))
                        remotes.Add((name, url));
                }
            }
            return remotes.Count > 0;
        }



        void AddRemote(string name, string url)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
            {
                AppendLog("Remote name/url cannot be empty.");
                return;
            }
            if (!RunGit($"remote add {name} \"{url}\"", out var so, out var se))
            {
                AppendLog($"Failed to add remote {name}: {se}");
            }
            else
            {
                AppendLog($"Added remote {name} → {url}");
                RefreshStatus();
            }
        }

        void SetDefaultPushRemote(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _defaultPushRemote = name;
            EditorPrefs.SetString("WSUGit.DefaultPushRemote", _defaultPushRemote);
            AppendLog("Default push remote set to: " + _defaultPushRemote);
        }

        void PushDefault()
        {
            var remote = string.IsNullOrWhiteSpace(_defaultPushRemote) ? "origin" : _defaultPushRemote;
            if (!RunGit($"push {remote} HEAD", out var so, out var se))
                AppendLog("Push failed: " + se);
            else
                AppendLog("Push OK:\n" + so);

            if (_autoRefreshAfterActions) RefreshStatus();
        }



        void RefreshRemotes()
        {
            if (RunGit("remote get-url origin", out var so, out var se))
                _originUrl = so.Trim();
            if (RunGit("remote get-url mirror", out so, out se))
                _mirrorUrl = so.Trim();
        }

        void GitInit()
        {
            var prj = Directory.GetCurrentDirectory().Replace('\\', '/');
            var root = new DirectoryInfo(prj); // Unity project root
            // We want to init at this folder (contains Assets/ Packages/ ProjectSettings/)
            if (RunGit("init", out var so, out var se))
            {
                AppendLog(so);
                DetectRepoRoot();
                WriteGitignoreIfMissing();
                AppendLog("Initialized repository.");
            }
            else AppendLog(se);
        }

        void StageAll()
        {
            AppendLog("$ git add -A");
            if (!RunGit("add -A", out var so, out var se)) AppendLog(se);
            else AppendLog(so);
            RefreshStatus();
        }

        void UnstageAll()
        {
            AppendLog("$ git reset");
            if (!RunGit("reset", out var so, out var se)) AppendLog(se);
            else AppendLog(so);
            RefreshStatus();
        }

        void DiscardAll()
        {
            if (!EditorUtility.DisplayDialog("Discard ALL changes?", "This will restore the working tree to last commit.\nUntracked files remain.", "Do it", "Cancel"))
                return;

            AppendLog("$ git restore --worktree -- .");
            if (!RunGit("restore --worktree -- .", out var so, out var se)) AppendLog(se);
            else AppendLog(so);
            RefreshStatus();
        }

        void StagePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!RunGit($"add -- \"{path.Replace("\\", "/")}\"", out var so, out var se)) AppendLog(se);
            RefreshStatus();
        }

        void UnstagePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!RunGit($"reset -- \"{path.Replace("\\", "/")}\"", out var so, out var se)) AppendLog(se);
            RefreshStatus();
        }

        void DiscardPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!EditorUtility.DisplayDialog("Discard file changes?", path, "Restore", "Cancel")) return;
            if (!RunGit($"restore --worktree -- \"{path.Replace("\\", "/")}\"", out var so, out var se)) AppendLog(se);
            RefreshStatus();
        }

        void CommitOnly()
        {
            var msg = string.IsNullOrWhiteSpace(_commitMsg) ? "update" : _commitMsg.Trim();
            AppendLog($"$ git commit -m \"{msg}\"");
            if (!RunGit($"commit -m \"{EscapeQuotes(msg)}\"", out var so, out var se))
            {
                AppendLog(se);
            }
            else
            {
                AppendLog(so);
                _commitMsg = ""; EditorPrefs.SetString("WSGitHub.commitMsg", _commitMsg);
                RefreshStatus();
            }
        }

        void CommitAndPush()
        {
            CommitOnly();
            Push();
        }

        void Push()
        {
            PushDefault();
        }

        void Pull()
        {
            var remote = "origin";
            var branch = string.IsNullOrEmpty(_branch) ? "main" : _branch;
            AppendLog($"$ git pull {remote} {branch}");
            if (!RunGit($"pull {remote} {branch}", out var so, out var se)) AppendLog(se);
            else AppendLog(so);
            RefreshStatus();
        }

        void LogLast50()
        {
            if (!RunGit("log --oneline -n 50", out var so, out var se)) AppendLog(se);
            else AppendLog(so);
        }

        void CreateTag(string tag)
        {
            if (!RunGit($"tag {tag}", out var so, out var se)) AppendLog(se);
            else AppendLog($"Created tag {tag}");
        }

        void CreateAndCheckout(string newBranch)
        {
            newBranch = newBranch.Trim();
            if (string.IsNullOrEmpty(newBranch)) return;
            if (!RunGit($"checkout -b {newBranch}", out var so, out var se)) AppendLog(se);
            else { AppendLog(so); _branch = newBranch; }
        }

        void ShowBranchPopup()
        {
            if (!RunGit(@"for-each-ref --format=""%(refname:short)"" refs/heads", out var so, out var se))
            {
                AppendLog(se); return;
            }
            var list = so.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var menu = new GenericMenu();
            foreach (var b in list)
            {
                var bb = b.Trim();
                menu.AddItem(new GUIContent(bb), bb == _branch, () => SafeAction(() => Checkout(bb)));
            }
            menu.DropDown(new Rect(Event.current.mousePosition, new Vector2(1, 1)));
        }

        void Checkout(string b)
        {
            if (!RunGit($"checkout {b}", out var so, out var se)) AppendLog(se);
            else { AppendLog(so); _branch = b; RefreshStatus(); }
        }

        void SetOriginRemote()
        {
            if (string.IsNullOrWhiteSpace(_originUrl))
            {
                AppendLog("[WARN] origin URL empty.");
                return;
            }

            if (!RunGit("remote", out var so, out var se)) { AppendLog(se); return; }
            if (so.Split('\n').Any(x => x.Trim() == "origin"))
            {
                RunGit($"remote set-url origin \"{_originUrl}\"", out var so2, out var se2);
                AppendLog($"Set origin → {_originUrl}");
            }
            else
            {
                RunGit($"remote add origin \"{_originUrl}\"", out var so3, out var se3);
                AppendLog($"Added origin → {_originUrl}");
            }
        }

        void SetMirrorRemote()
        {
            if (string.IsNullOrWhiteSpace(_mirrorUrl))
            {
                AppendLog("[WARN] mirror URL empty.");
                return;
            }
            // Use a simple "mirror" remote (not --mirror push by default to be safer)
            if (!RunGit("remote", out var so, out var se)) { AppendLog(se); return; }
            if (so.Split('\n').Any(x => x.Trim() == "mirror"))
                RunGit($"remote set-url mirror \"{_mirrorUrl}\"", out var so2, out var se2);
            else
                RunGit($"remote add mirror \"{_mirrorUrl}\"", out var so3, out var se3);

            AppendLog($"Mirror set → {_mirrorUrl}");
        }

        void PushMirror()
        {
            if (string.IsNullOrWhiteSpace(_mirrorUrl))
            {
                AppendLog("[WARN] mirror URL not set.");
                return;
            }
            // Safer default: normal push (not --mirror). Change to --mirror if you truly want a forced 1:1 mirror.
            var b = string.IsNullOrEmpty(_branch) ? "main" : _branch;
            if (!RunGit($"push mirror {b}", out var so, out var se)) AppendLog(se);
            else AppendLog(so);
        }

        void FetchRemote(string name)
        {
            if (!RunGit($"fetch {name}", out var so, out var se)) AppendLog(se);
            else AppendLog(so);
        }

        void FixLineEndings(bool windowsStyle = true)
        {
            // Prefer .gitattributes; this is a safety net.
            var cfg = windowsStyle ? "true" : "input";
            if (!RunGit($"config core.autocrlf {cfg}", out var so, out var se))
                AppendLog("Failed to set core.autocrlf: " + se);
            else
                AppendLog("Set core.autocrlf=" + cfg + (string.IsNullOrWhiteSpace(so) ? "" : " | " + so.Trim()));
        }

        //void ConfigVsCodeDiffMerge()
        //{
        //    // Scope to this repo
        //    RunGit("config --local diff.tool vscode", out _, out _);

        //    // difftool: code --wait --diff "$LOCAL" "$REMOTE"
        //    var diffCmd = "config --local difftool.vscode.cmd \"code --wait --diff \\\"$LOCAL\\\" \\\"$REMOTE\\\"\"";
        //    RunGit(diffCmd, out _, out _);

        //    // mergetool: code --wait "$MERGED"
        //    RunGit("config --local merge.tool vscode", out _, out _);
        //    var mergeCmd = "config --local mergetool.vscode.cmd \"code --wait \\\"$MERGED\\\"\"";
        //    RunGit(mergeCmd, out _, out _);

        //    AppendLog("Configured VS Code as diff/merge tool for this repo.");
        //}





        void WriteGitattributes()
        {
            var path = Path.Combine(_repoRoot, ".gitattributes");
            var content = string.Join("\n", new[]{
                "# Normalize text files for cross-platform dev",
                "* text=auto",
                "",
                "# Unity YAML / text assets should remain LF",
                "*.cs text eol=lf",
                "*.shader text eol=lf",
                "*.compute text eol=lf",
                "*.cginc text eol=lf",
                "*.hlsl text eol=lf",
                "*.json text eol=lf",
                "*.xml text eol=lf",
                "*.yaml text eol=lf",
                "*.yml text eol=lf",
                "*.asmdef text eol=lf",
                "*.asset text eol=lf",
                "*.meta text eol=lf",
                "*.prefab text eol=lf",
                "*.unity text eol=lf",
                "",
                "# Binary assets",
                "*.png binary",
                "*.jpg binary",
                "*.jpeg binary",
                "*.tga binary",
                "*.psd binary",
                "*.fbx binary",
                "*.wav binary",
                "*.mp3 binary",
                "*.ogg binary"
            });
            File.WriteAllText(path, content, new UTF8Encoding(false));
            AppendLog("Wrote .gitattributes");

            // Stage and commit quickly (optional convenience)
            RunGit("add .gitattributes", out var so, out var se);
            RunGit(@"commit -m ""chore: add .gitattributes""", out so, out se);
        }

        void WriteGitignoreIfMissing()
        {
            var p = Path.Combine(_repoRoot, ".gitignore");
            if (File.Exists(p)) return;
            var content = string.Join("\n", new[]{
                "Library/",
                "Temp/",
                "Obj/",
                "Build/",
                "Builds/",
                "Logs/",
                "UserSettings/",
                "MemoryCaptures/",
                "*.csproj",
                "*.sln",
                "*.user",
                ".idea/",
                ".vs/",
                ".vscode/",
                ".DS_Store",
                "Thumbs.db"
            });
            File.WriteAllText(p, content, new UTF8Encoding(false));
            AppendLog("Wrote .gitignore (Unity slim).");
            RunGit("add .gitignore", out var so, out var se);
            RunGit(@"commit -m ""chore: add .gitignore""", out so, out se);
        }

        // --------- Shell helpers ----------
        void AppendLog(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _log.AppendLine(msg);
            Repaint();
        }

        static string EscapeQuotes(string s) => s?.Replace("\"", "\\\"") ?? "";

        bool RunGit(string args, out string stdout, out string stderr, int timeoutMs = DefaultTimeoutMs)
        {
            stdout = ""; stderr = "";
            if (string.IsNullOrEmpty(_repoRoot)) { stderr = "Repo root not set."; return false; }

            try
            {
                var psi = new ProcessStartInfo
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
                using var p = new Process { StartInfo = psi };
                var so = new StringBuilder();
                var se = new StringBuilder();
                p.OutputDataReceived += (_, e) => { if (e.Data != null) so.AppendLine(e.Data); };
                p.ErrorDataReceived += (_, e) => { if (e.Data != null) se.AppendLine(e.Data); };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { }
                    se.AppendLine("timed out after " + timeoutMs + " ms");
                }

                stdout = so.ToString();
                stderr = se.ToString();

                // Echo command + result to UI log (shorten noisy outputs)
                var head = args.Length > 160 ? args.Substring(0, 160) + "…" : args;
                AppendLog("$ git " + head);
                if (!string.IsNullOrEmpty(stdout)) AppendLog(Clamp(stdout, 4000));
                if (!string.IsNullOrEmpty(stderr)) AppendLog(Clamp(stderr, 4000));

                return p.ExitCode == 0; // success even if stderr had warnings/progress
            }
            catch (Exception e)
            {
                stderr = e.Message;
                AppendLog("[EXC] " + e.Message);
                return false;
            }
        }


        static string Clamp(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "\n…(truncated)…";

        static bool HasGitInPath(out string version)
        {
            version = "";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var p = Process.Start(psi);
                version = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(2000);
                return p.ExitCode == 0 && version.StartsWith("git version");
            }
            catch { return false; }
        }

        static string GuessGitBash()
        {
            // Common default Git for Windows path. We avoid registry for simplicity.
            var p1 = @"C:\Program Files\Git\git-bash.exe";
            var p2 = @"C:\Program Files (x86)\Git\git-bash.exe";
            if (File.Exists(p1)) return p1;
            if (File.Exists(p2)) return p2;
            return null;
        }
    }
}
#endif
