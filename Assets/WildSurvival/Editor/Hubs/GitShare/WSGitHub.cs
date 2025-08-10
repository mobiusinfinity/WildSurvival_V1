// Assets/_Project/Code/Editor/Git/WSGitHub.cs
// Ultimate Git Hub — single panel for Status, Stage, Commit, Push/Pull, Branches, Remotes, Stash, Log, Mirror/Share.
// Editor-only. Integrates with your Public Mirror + SharePackage tools if present.

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WildSurvival.EditorTools; // uses WSGitProcess helpers

namespace WildSurvival.EditorTools
{
    public class WSGitHub : EditorWindow
    {
        [MenuItem("Wild Survival/Collab/Git Hub", false, 0)]
        public static void Open() { GetWindow<WSGitHub>("WS — Git Hub").Show(); }

        // --- UI State ---
        Vector2 _scroll;
        int _tab = 0;
        string[] _tabs = new[] { "Status", "Commit", "Push/Pull", "Branches", "Remotes", "Stash", "Log", "Mirror" };

        // Status cache
        string _statusShort, _statusFull;
        string _branch = "";
        string _lastOutput = "";
        string _gitVersion = "";
        bool _gitOk = false;

        // Commit
        bool _stageAll = true;
        string _commitMsg = "";
        bool _amend = false;

        // Push/Pull
        string _remote = "origin";
        string _pushBranch = "";
        bool _setUpstream = false;

        // Branches
        List<string> _localBranches = new List<string>();
        string _newBranch = "";
        bool _checkoutNew = true;
        bool _deleteForce = false;

        // Remotes
        Dictionary<string, string> _remotes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string _newRemoteName = "public";
        string _newRemoteUrl = "https://github.com/your/mirror.git";

        // Stash
        string _stashMsg = "WIP";

        // Log
        int _logCount = 30;
        string _logText = "";

        // Mirror/Share
        bool _includeMeta = true;
        bool _includeYaml = true;

        void OnEnable()
        {
            RefreshGitAvailability();
            RefreshAllAsync();
        }

        void RefreshGitAvailability()
        {
            string v, e;
            _gitOk = WSGitEnv.VerifyGit(out v, out e);
            _gitVersion = _gitOk ? v : ("(not found) " + e);
        }

        void OnGUI()
        {
            DrawHeader();

            if (!_gitOk)
            {
                EditorGUILayout.HelpBox("Git not found. Set path to git executable and retry.\n" + _gitVersion, MessageType.Error);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("git path", GUILayout.Width(60));
                    WSGitEnv.GitPath = EditorGUILayout.TextField(WSGitEnv.GitPath);
                    if (GUILayout.Button("Verify", GUILayout.Width(80)))
                        RefreshGitAvailability();
                }
                return;
            }

            _tab = GUILayout.Toolbar(_tab, _tabs);
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            try
            {
                switch (_tab)
                {
                    case 0:
                        DrawStatusTab();
                        break;
                    case 1:
                        DrawCommitTab();
                        break;
                    case 2:
                        DrawPushPullTab();
                        break;
                    case 3:
                        DrawBranchesTab();
                        break;
                    case 4:
                        DrawRemotesTab();
                        break;
                    case 5:
                        DrawStashTab();
                        break;
                    case 6:
                        DrawLogTab();
                        break;
                    case 7:
                        DrawMirrorTab();
                        break;
                }
            }
            finally { EditorGUILayout.EndScrollView(); }

            DrawFooter();
        }

        void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Wild Survival — Git Hub", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(_gitVersion, EditorStyles.miniLabel);

                if (GUILayout.Button("Refresh", GUILayout.Width(90)))
                    RefreshAllAsync();

                if (GUILayout.Button("Open Builds", GUILayout.Width(110)))
                    EditorUtility.RevealInFinder(WSGitEnv.BuildsRoot);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Repo:", GUILayout.Width(40));
                EditorGUILayout.SelectableLabel(GetOriginUrl(), EditorStyles.textField, GUILayout.Height(16));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Branch:", GUILayout.Width(50));
                EditorGUILayout.SelectableLabel(_branch, EditorStyles.textField, GUILayout.Height(16));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open in Terminal", GUILayout.Width(150)))
                    OpenTerminalHere();
            }
        }

        // ---------------- TABS ----------------

        void DrawStatusTab()
        {
            EditorGUILayout.LabelField("Status (short)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_statusShort ?? "", GUILayout.MinHeight(60));

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Stage All"))
                    RunGit("add -A");
                if (GUILayout.Button("Unstage All"))
                    RunGit("reset");
                if (GUILayout.Button("Discard Unstaged…"))
                    DiscardUnstagedWithPreview();

                void DiscardUnstagedWithPreview()
                {
                    WSGit.RunAsync("clean -fdn", r => {
                        string preview = string.IsNullOrEmpty(r.StdOut) ? "(no untracked files would be removed)" : r.StdOut.Trim();
                        bool go = EditorUtility.DisplayDialog(
                            "Discard changes",
                            "This will run:\n\n  git reset --hard\n  git clean -fd\n\nPreview of untracked removals:\n\n" +
                            (preview.Length > 2000 ? preview.Substring(0, 2000) + "\n…(truncated)" : preview),
                            "Yes, discard", "Cancel");
                        if (go)
                            RunGit("reset --hard", _ => RunGit("clean -fd"));
                    });
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                    RefreshAllAsync();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Status (full)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_statusFull ?? "", GUILayout.MinHeight(100));
        }

        void DrawCommitTab()
        {
            EditorGUILayout.LabelField("Commit", EditorStyles.boldLabel);
            _stageAll = EditorGUILayout.ToggleLeft("Stage all changes before commit (git add -A)", _stageAll);
            _amend = EditorGUILayout.ToggleLeft("Amend last commit (--amend)", _amend);
            _commitMsg = EditorGUILayout.TextField("Message", _commitMsg);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Commit"))
                    DoCommit(false);
                if (GUILayout.Button("Commit & Push"))
                    DoCommit(true);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                    RefreshAllAsync();
            }
        }

        void DoCommit(bool andPush)
        {
            if (string.IsNullOrWhiteSpace(_commitMsg))
            {
                EditorUtility.DisplayDialog("Commit", "Please enter a commit message.", "OK");
                return;
            }

            Action<GitResult> afterCommit = (r) =>
            {
                if (andPush)
                    Push();
                else
                    RefreshAllAsync();
            };

            if (_stageAll)
                RunGit("add -A", after: _ => RunGit("commit " + BuildCommitArgs(), after: afterCommit));
            else
                RunGit("commit " + BuildCommitArgs(), after: afterCommit);
        }

        string BuildCommitArgs()
        {
            var args = new StringBuilder("-m " + WSGit.Quote(_commitMsg));
            if (_amend)
                args.Append(" --amend");
            return args.ToString();
        }

        void DrawPushPullTab()
        {
            EditorGUILayout.LabelField("Remote", EditorStyles.boldLabel);
            var names = _remotes.Keys.ToArray();
            int idx = Math.Max(0, Array.IndexOf(names, _remote));
            int newIdx = EditorGUILayout.Popup("Push/Pull remote", idx, names);
            if (newIdx != idx && newIdx >= 0 && newIdx < names.Length)
                _remote = names[newIdx];

            _pushBranch = EditorGUILayout.TextField("Branch", string.IsNullOrEmpty(_pushBranch) ? _branch : _pushBranch);
            _setUpstream = EditorGUILayout.ToggleLeft("Set upstream on first push (--set-upstream)", _setUpstream);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pull"))
                    RunGit("pull " + WSGit.Quote(_remote) + " " + WSGit.Quote(_pushBranch));
                if (GUILayout.Button("Fetch"))
                    RunGit("fetch " + WSGit.Quote(_remote));
                if (GUILayout.Button("Push"))
                    Push();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                    RefreshAllAsync();
            }
        }

        void Push()
        {
            string branch = string.IsNullOrEmpty(_pushBranch) ? _branch : _pushBranch;
            string args = "push " + WSGit.Quote(_remote) + " " + WSGit.Quote(branch);
            if (_setUpstream)
                args += " --set-upstream";
            RunGit(args);
        }

        void DrawBranchesTab()
        {
            EditorGUILayout.LabelField("Local Branches", EditorStyles.boldLabel);
            foreach (var b in _localBranches)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(b == _branch ? "●" : "○", GUILayout.Width(18));
                    EditorGUILayout.SelectableLabel(b, GUILayout.Height(16));
                    if (b != _branch && GUILayout.Button("Checkout", GUILayout.Width(90)))
                        RunGit("checkout " + WSGit.Quote(b), after: _ => RefreshAllAsync());
                    if (b != _branch && GUILayout.Button(_deleteForce ? "Delete (!)" : "Delete", GUILayout.Width(80)))
                    {
                        string opt = _deleteForce ? "-D" : "-d";
                        RunGit("branch " + opt + " " + WSGit.Quote(b), after: _ => RefreshAllAsync());
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create Branch", EditorStyles.boldLabel);
            _newBranch = EditorGUILayout.TextField("Name", _newBranch);
            _checkoutNew = EditorGUILayout.ToggleLeft("Checkout after create", _checkoutNew);
            _deleteForce = EditorGUILayout.ToggleLeft("Force delete (unsafe)", _deleteForce);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create"))
                {
                    string args = "branch " + WSGit.Quote(_newBranch);
                    Action<GitResult> after = _ => RefreshAllAsync();
                    RunGit(args, after: _checkoutNew ? (Action<GitResult>)(_ => RunGit("checkout " + WSGit.Quote(_newBranch), after: after)) : after);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                    RefreshAllAsync();
            }
        }

        void DrawRemotesTab()
        {
            EditorGUILayout.LabelField("Remotes", EditorStyles.boldLabel);
            foreach (var kv in _remotes)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(kv.Key + "  →  " + kv.Value, GUILayout.Height(16));
                    if (GUILayout.Button("Set URL", GUILayout.Width(80)))
                    {
                        string current = kv.Value;
                        WSGitUrlPrompt.Show($"Set URL for {kv.Key}", current,
                            newUrl => { if (!string.IsNullOrWhiteSpace(newUrl)) RunGit("remote set-url " + WSGit.Quote(kv.Key) + " " + WSGit.Quote(newUrl), _ => RefreshRemotes()); });
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Remote", "Remove remote '" + kv.Key + "'?", "Remove", "Cancel"))
                            RunGit("remote remove " + WSGit.Quote(kv.Key), after: _ => RefreshRemotes());
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Add Remote", EditorStyles.boldLabel);
            _newRemoteName = EditorGUILayout.TextField("Name", _newRemoteName);
            _newRemoteUrl = EditorGUILayout.TextField("URL", _newRemoteUrl);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add"))
                    RunGit("remote add " + WSGit.Quote(_newRemoteName) + " " + WSGit.Quote(_newRemoteUrl), after: _ => RefreshRemotes());
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                    RefreshRemotes();
            }
        }

        void DrawStashTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _stashMsg = EditorGUILayout.TextField("Message", _stashMsg);
                if (GUILayout.Button("Stash Save"))
                    RunGit("stash push -m " + WSGit.Quote(_stashMsg));
                if (GUILayout.Button("Apply Latest"))
                    RunGit("stash apply");
                if (GUILayout.Button("Pop Latest"))
                    RunGit("stash pop");
                if (GUILayout.Button("Drop Latest"))
                    RunGit("stash drop");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("List", GUILayout.Width(100)))
                    RunGit("stash list");
            }
        }

        void DrawLogTab()
        {
            _logCount = EditorGUILayout.IntSlider("Count", _logCount, 5, 200);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Log"))
                    RefreshLog();
                if (GUILayout.Button("Show Graph"))
                    RunGit("--no-pager log --graph --oneline --decorate -n " + _logCount);
            }

            EditorGUILayout.TextArea(_logText ?? "", GUILayout.MinHeight(160));
        }

        void DrawMirrorTab()
        {
            EditorGUILayout.LabelField("Public Mirror & Share", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create a ZIP mirror of all code (and optional YAML assets) or build a SharePackage snapshot for AI/collab. These call the tools we added earlier if present.", MessageType.Info);

            _includeMeta = EditorGUILayout.ToggleLeft("Include .meta files", _includeMeta);
            _includeYaml = EditorGUILayout.ToggleLeft("Include YAML assets (.unity/.prefab/.mat/.asset/.anim…)", _includeYaml);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export Public Mirror — All Code (ZIP)"))
                    TryCallMirror(_includeMeta, _includeYaml);
                if (GUILayout.Button("Build SharePackage"))
                    TryCallShare();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Repo ZIP (snapshot)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Git Archive HEAD → ZIP"))
                    GitArchiveHeadZip();
                if (GUILayout.Button("Download GitHub ZIP (branch)"))
                    DownloadGithubZip();
                if (GUILayout.Button("Open Mirror Folder"))
                    EditorUtility.RevealInFinder(WSGitEnv.MirrorRoot);
            }
        }

        void DrawFooter()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_lastOutput ?? "", GUILayout.MinHeight(140));

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Copy Output to Clipboard", GUILayout.Width(200)))
                {
                    GUIUtility.systemCopyBuffer = _lastOutput ?? "";
                    EditorUtility.DisplayDialog("Copied", "Command output copied to clipboard.", "OK");
                }
            }

            // Cute AI postcard (if you added WSAiGifts.cs earlier)
            var giftType = Type.GetType("WildSurvival.EditorTools.WSAiGifts, Assembly-CSharp-Editor");
            if (giftType != null)
            {
                var m = giftType.GetMethod("GiftOneLinerComment", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (m != null)
                {
                    string line = (string)m.Invoke(null, null);
                    if (!string.IsNullOrEmpty(line))
                        EditorGUILayout.HelpBox(line.Replace("// AI gift: ", ""), MessageType.None);
                }
            }
        }

        // ---------------- Actions & Helpers ----------------

        void RefreshAllAsync()
        {
            RefreshStatus();
            RefreshRemotes();
            RefreshBranches();
            RefreshLog();
        }

        void RefreshStatus()
        {
            WSGit.RunAsync("status -sb", r =>
            {
                _statusShort = r.StdOut;
                _branch = WSGitParse.CurrentBranch(_statusShort);
                _pushBranch = _branch;
                AppendOutput(r);
            });

            WSGit.RunAsync("status", r =>
            {
                _statusFull = r.StdOut;
                AppendOutput(r);
            });
        }

        void RefreshRemotes()
        {
            WSGit.RunAsync("remote -v", r =>
            {
                _remotes = WSGitParse.Remotes(r.StdOut);
                if (_remotes.Count > 0 && !_remotes.ContainsKey(_remote))
                    _remote = _remotes.Keys.FirstOrDefault() ?? "origin";
                AppendOutput(r);
            });
        }

        void RefreshBranches()
        {
            WSGit.RunAsync("branch", r =>
            {
                _localBranches = WSGitParse.BranchesLocal(r.StdOut);
                AppendOutput(r);
            });
        }

        void RefreshLog()
        {
            WSGit.RunAsync("--no-pager log --oneline -n " + _logCount, r =>
            {
                _logText = r.StdOut;
                AppendOutput(r);
            });
        }

        void RunGit(string args, Action<GitResult> after = null)
        {
            AppendOutput("$ git " + args + "\n");
            WSGit.RunAsync(args, r =>
            {
                AppendOutput(r);
                if (after != null)
                    after(r);
            });
        }

        void AppendOutput(GitResult r) { AppendOutput(Format(r)); }
        void AppendOutput(string s)
        {
            if (string.IsNullOrEmpty(s))
                return;
            var stamp = DateTime.Now.ToString("HH:mm:ss");
            _lastOutput = "[" + stamp + "] " + s.TrimEnd() + "\n\n" + (_lastOutput ?? "");
            Repaint();
        }

        string Format(GitResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("$ git " + r.CommandLine + "   (" + (int)r.Duration.TotalMilliseconds + " ms, exit " + r.ExitCode + ")");
            if (!string.IsNullOrEmpty(r.StdOut))
                sb.AppendLine(r.StdOut.TrimEnd());
            if (!string.IsNullOrEmpty(r.StdErr))
                sb.AppendLine("[stderr]\n" + r.StdErr.TrimEnd());
            return sb.ToString();
        }

        string GetOriginUrl()
        {
            string url = "";
            if (_remotes != null && _remotes.TryGetValue("origin", out url))
                return url;
            return "(no origin)";
        }

        void OpenTerminalHere()
        {
#if UNITY_EDITOR_WIN
            var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/K \"cd /d " + WSGitEnv.ProjectRoot + "\"");
            System.Diagnostics.Process.Start(psi);
#elif UNITY_EDITOR_OSX
            System.Diagnostics.Process.Start("open", "-a Terminal \"" + WSGitEnv.ProjectRoot + "\"");
#else
            // Linux: try x-terminal-emulator
            System.Diagnostics.Process.Start("x-terminal-emulator", "--working-directory=\"" + WSGitEnv.ProjectRoot + "\"");
#endif
        }

        // ---------------- Mirror / Share integration ----------------

        void TryCallMirror(bool includeMeta, bool includeYaml)
        {
            // Prefer our improved Share & Mirror (MirrorActions)
            var t = Type.GetType("WildSurvival.EditorTools.MirrorActions, Assembly-CSharp-Editor");
            if (t != null)
            {
                // MirrorOptions struct?
                var mo = Type.GetType("WildSurvival.EditorTools.MirrorOptions, Assembly-CSharp-Editor");
                if (mo != null)
                {
                    object opts = Activator.CreateInstance(mo);
                    // set fields via reflection
                    var fMeta = mo.GetField("IncludeMeta");
                    if (fMeta != null)
                        fMeta.SetValue(opts, includeMeta);
                    var fYaml = mo.GetField("IncludeYamlTextAssets");
                    if (fYaml != null)
                        fYaml.SetValue(opts, includeYaml);
                    var fFilt = mo.GetField("UseFilter");
                    if (fFilt != null)
                        fFilt.SetValue(opts, true);

                    var m = t.GetMethod("ExportPublicMirrorAllCode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if (m != null)
                    { m.Invoke(null, new object[] { opts }); return; }
                }
            }

            // Fallback: open your legacy Public Mirror exporter UI if present
            var legacy = Type.GetType("WildSurvival.Editor.Git.PublicMirrorExporterV2, Assembly-CSharp-Editor");
            if (legacy != null)
            {
                EditorWindow.GetWindow(legacy).Show();
                return;
            }

            EditorUtility.DisplayDialog("Mirror", "No Mirror tool found.\nAdd the Share & Mirror tool I provided earlier to enable this button.", "OK");
        }

        void TryCallShare()
        {
            var t = Type.GetType("WildSurvival.EditorTools.ShareActions, Assembly-CSharp-Editor");
            var to = Type.GetType("WildSurvival.EditorTools.ShareOptions, Assembly-CSharp-Editor");
            if (t != null && to != null)
            {
                object opts = Activator.CreateInstance(to);
                // set defaults
                SetField(to, opts, "Label", "git-hub");
                SetField(to, opts, "MaxDocBytes", 1024 * 1024);
                SetField(to, opts, "IncludeCollabAndReports", true);
                SetField(to, opts, "IncludeProjectSettings", true);
                SetField(to, opts, "IncludePackagesLock", true);
                SetField(to, opts, "ZipAfterBuild", true);
                SetField(to, opts, "GenerateExtras", true);
                SetField(to, opts, "UseFilter", true);

                var m = t.GetMethod("BuildSharePackage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (m != null)
                { m.Invoke(null, new object[] { opts }); return; }
            }
            EditorUtility.DisplayDialog("SharePackage", "No Share tool found.\nAdd the Share & Mirror tool I provided earlier to enable this button.", "OK");
        }

        void SetField(Type t, object o, string name, object val)
        {
            var f = t.GetField(name);
            if (f != null)
                f.SetValue(o, val);
        }

        // ---------------- Repo ZIP helpers ----------------

        void GitArchiveHeadZip()
        {
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outDir = WSGitEnv.MirrorRoot;
            Directory.CreateDirectory(outDir);
            var zip = Path.Combine(outDir, "RepoArchive_HEAD_" + ts + ".zip");
            RunGit("archive -o " + WSGit.Quote(zip) + " HEAD");
        }

        void DownloadGithubZip()
        {
            string origin = GetOriginUrl();
            if (string.IsNullOrEmpty(origin) || origin.IndexOf("github.com", StringComparison.OrdinalIgnoreCase) < 0)
            {
                EditorUtility.DisplayDialog("Download ZIP", "Origin does not look like GitHub. This helper only supports GitHub’s branch zip URLs.", "OK");
                return;
            }

            string userRepo = ExtractGithubUserRepo(origin);
            if (string.IsNullOrEmpty(userRepo))
            {
                EditorUtility.DisplayDialog("Download ZIP", "Could not parse GitHub URL.", "OK");
                return;
            }

            string branch = string.IsNullOrEmpty(_pushBranch) ? _branch : _pushBranch;
            string url = "https://github.com/" + userRepo + "/archive/refs/heads/" + branch + ".zip";

            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outDir = WSGitEnv.MirrorRoot;
            Directory.CreateDirectory(outDir);
            var dst = Path.Combine(outDir, "RepoGithub_" + branch + "_" + ts + ".zip");

            // Simple blocking download in a background thread
            AppendOutput("Downloading: " + url + " → " + dst);
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                string err = null;
                try
                {
                    using (var wc = new System.Net.WebClient())
                    {
                        wc.DownloadFile(new Uri(url), dst);
                    }
                }
                catch (Exception ex) { err = ex.Message; }
                EditorApplication.delayCall += () =>
                {
                    if (string.IsNullOrEmpty(err))
                    { AppendOutput("Downloaded ZIP → " + dst); EditorUtility.RevealInFinder(dst); }
                    else
                    { AppendOutput("Download failed: " + err); }
                };
            });
        }

        string ExtractGithubUserRepo(string url)
        {
            // supports https://github.com/user/repo(.git) and git@github.com:user/repo.git
            url = url.Trim();
            if (url.StartsWith("git@github.com:"))
            {
                string tail = url.Substring("git@github.com:".Length);
                if (tail.EndsWith(".git"))
                    tail = tail.Substring(0, tail.Length - 4);
                return tail;
            }
            // https URLs
            int idx = url.IndexOf("github.com/");
            if (idx >= 0)
            {
                string tail = url.Substring(idx + "github.com/".Length);
                if (tail.StartsWith("/"))
                    tail = tail.Substring(1);
                if (tail.EndsWith(".git"))
                    tail = tail.Substring(0, tail.Length - 4);
                var parts = tail.Split('/');
                if (parts.Length >= 2)
                    return parts[0] + "/" + parts[1];
            }
            return null;
        }

        // ---------------- Small UX helpers ----------------

        void AskAndRun(string title, string current, Action<string> onConfirm)
        {
            string input = current;
            input = EditorGUILayout.TextField("URL", input); // (note: quick UI-less helper kept simple)
            // The simple flow above is mostly placeholder; to keep this self-contained, use clipboard flow instead:
            string clip = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clip) && EditorUtility.DisplayDialog(title, "Use clipboard value?\n" + clip, "Use", "Cancel"))
            {
                onConfirm(clip);
            }
        }
    }
}
#endif
