//// ========================================
//// Wild Survival - Ultimate Git Hub
//// Complete Git Management System for Unity
//// Place at: Assets/WildSurvival/Editor/Hubs/UltimateGitHub.cs
//// ========================================

//using UnityEngine;
//using UnityEditor;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.IO;
//using System.Text;
//using System.Diagnostics;
//using System.Net;
//using System.Text.RegularExpressions;
//using UnityEngine.Networking;
//using Debug = UnityEngine.Debug;

//namespace WildSurvival.Editor.Hubs
//{
//    public partial class UltimateGitHub : EditorWindow
//    {
//        // Tab system
//        private string[] tabNames = { "Overview", "Commits", "Branches", "Remote", "History", "Settings" };
//        private int currentTab = 0;

//        // Scroll positions
//        private Vector2 mainScrollPos;
//        private Vector2 commitScrollPos;
//        private Vector2 changesScrollPos;
//        private Vector2 historyScrollPos;

//        // Mirror settings
//        private bool enableMirrorExport = false;
//        private string mirrorExportPath = "";
//        private bool autoCreateMirror = false;
//        private DateTime lastMirrorExport = DateTime.MinValue;

//        // Improved branch operations
//        private string quickMergeBranch = "";
//        private bool showMergeConfirmation = true;

//        // Git status
//        private GitRepository repo = new GitRepository();
//        private bool isRefreshing = false;
//        private float lastRefreshTime = 0f;
//        private bool autoRefresh = true;
//        private float refreshInterval = 5f;

//        // Commit interface
//        private string commitMessage = "";
//        private string commitDescription = "";
//        private bool amendLastCommit = false;
//        private bool signOffCommit = false;
//        private List<ChangedFile> stagedFiles = new List<ChangedFile>();
//        private List<ChangedFile> unstagedFiles = new List<ChangedFile>();
//        private string selectedFilePath = "";

//        // Branch management
//        private List<GitBranch> localBranches = new List<GitBranch>();
//        private List<GitBranch> remoteBranches = new List<GitBranch>();
//        private string newBranchName = "";
//        private GitBranch selectedBranch;
//        private bool showRemoteBranches = true;
//        private string mergeSourceBranch = "";

//        // Remote operations
//        private List<GitRemote> remotes = new List<GitRemote>();
//        private GitRemote selectedRemote;
//        private string newRemoteUrl = "";
//        private string newRemoteName = "origin";
//        private bool forcePush = false;
//        private bool pushTags = false;
//        private string mirrorUrl = "";

//        // History and logs
//        private List<GitCommit> commitHistory = new List<GitCommit>();
//        private int historyLimit = 50;
//        private string historyFilter = "";
//        private GitCommit selectedCommit;

//        // Settings
//        private GitSettings settings = new GitSettings();
//        private List<GitAlias> aliases = new List<GitAlias>();
//        private List<string> ignoredPaths = new List<string>();

//        // UI State
//        private bool showAdvancedOptions = false;
//        private string quickActionSearch = "";
//        private List<QuickAction> quickActions = new List<QuickAction>();
//        private string terminalOutput = "";
//        private bool showTerminal = false;

//        private GitSafetySystem safety = new GitSafetySystem();
//        private string lastBackupPath = "";
//        private bool operationInProgress = false;
//        private List<string> operationLog = new List<string>();

//        //private GitignoreValidator gitignoreValidator = new GitignoreValidator();
//        //private bool hasGitignoreIssues = false;
//        //private DateTime lastGitignoreCheck = DateTime.MinValue;

//        [System.Serializable]
//        public class GitRepository
//        {
//            public bool isInitialized;
//            public string rootPath;
//            public string currentBranch = "main";
//            public string currentCommit = "";
//            public int ahead;
//            public int behind;
//            public bool hasUncommittedChanges;
//            public bool hasUntrackedFiles;
//            public bool hasConflicts;
//            public long repositorySize;
//            public DateTime lastFetch;
//            public string userName = "";
//            public string userEmail = "";
//        }

//        [System.Serializable]
//        public class ChangedFile
//        {
//            public enum ChangeType { Added, Modified, Deleted, Renamed, Untracked, Conflicted }

//            public string path;
//            public ChangeType changeType;
//            public bool isStaged;
//            public int additions;
//            public int deletions;
//            public string oldPath; // For renamed files
//            public bool isBinary;
//            public long fileSize;
//        }

//        [System.Serializable]
//        public class GitBranch
//        {
//            public string name;
//            public string remoteName;
//            public bool isLocal;
//            public bool isRemote;
//            public bool isCurrent;
//            public string lastCommit;
//            public DateTime lastActivity;
//            public int commitsAhead;
//            public int commitsBehind;
//            public string trackingBranch;
//        }

//        [System.Serializable]
//        public class GitRemote
//        {
//            public string name;
//            public string fetchUrl;
//            public string pushUrl;
//            public bool isConnected;
//            public DateTime lastSync;
//            public List<string> branches = new List<string>();
//        }

//        [System.Serializable]
//        public class GitCommit
//        {
//            public string hash;
//            public string shortHash;
//            public string author;
//            public string email;
//            public DateTime date;
//            public string message;
//            public string description;
//            public List<string> changedFiles = new List<string>();
//            public int additions;
//            public int deletions;
//            public List<string> tags = new List<string>();
//            public string branch;
//        }

//        [System.Serializable]
//        public class GitSettings
//        {
//            public bool autoFetch = true;
//            public int autoFetchInterval = 300; // seconds
//            public bool autoPushAfterCommit = false;
//            public bool showFileIcons = true;
//            public bool compactMode = false;
//            public string defaultBranch = "main";
//            public bool useLFS = false;
//            public bool gpgSign = false;
//            public string mergeStrategy = "recursive";
//            public bool verboseOutput = false;
//            public bool pruneOnFetch = true;
//        }

//        [System.Serializable]
//        public class GitAlias
//        {
//            public string name;
//            public string command;
//            public string description;
//            public KeyCode hotkey;
//        }

//        [System.Serializable]
//        public class QuickAction
//        {
//            public string name;
//            public string tooltip;
//            public System.Action action;
//            public Texture2D icon;
//            public bool requiresConfirmation;
//            public KeyCode shortcut;
//        }

//        [MenuItem("Wild Survival/Git/Ultimate Git Hub", false, 100)]
//        [MenuItem("Window/Wild Survival/Git Hub %#g", false, 100)]
//        public static void ShowWindow()
//        {
//            var window = GetWindow<UltimateGitHub>("Git Hub");
//            window.minSize = new Vector2(1000, 700);
//            window.titleContent = new GUIContent("Git Hub", EditorGUIUtility.FindTexture("CollabPush"));
//        }


//        private void OnDisable()
//        {
//            SaveSettings();
//            EditorApplication.update -= OnUpdate;
//            Undo.undoRedoPerformed -= OnUndoRedo;
//        }

//        private void OnUpdate()
//        {
//            //if (autoRefresh && Time.realtimeSinceStartup - lastRefreshTime > refreshInterval)
//            //{
//            //    RefreshStatus();
//            //    lastRefreshTime = Time.realtimeSinceStartup;
//            //}
//        }

//        private void OnUndoRedo()
//        {
//            RefreshStatus();
//        }

//        //private void OnGUI()
//        //{
//        //    DrawToolbar();
//        //    DrawQuickActions();

//        //    mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

//        //    // Tab selection
//        //    EditorGUILayout.BeginHorizontal();
//        //    GUILayout.Space(10);
//        //    currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(30));
//        //    GUILayout.Space(10);
//        //    EditorGUILayout.EndHorizontal();

//        //    EditorGUILayout.Space(10);

//        //    // Draw current tab content
//        //    switch (currentTab)
//        //    {
//        //        case 0:
//        //            DrawOverview();
//        //            break;
//        //        case 1:
//        //            DrawCommitInterface();
//        //            break;
//        //        case 2:
//        //            DrawBranchManagement();
//        //            break;
//        //        case 3:
//        //            DrawRemoteOperations();
//        //            break;
//        //        case 4:
//        //            DrawHistory();
//        //            break;
//        //        case 5:
//        //            DrawSettings();
//        //            break;
//        //    }

//        //    EditorGUILayout.EndScrollView();

//        //    // Draw terminal output if enabled
//        //    if (showTerminal)
//        //    {
//        //        DrawTerminal();
//        //    }
//        //}

//        private void DrawToolbar()
//        {
//            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

//            // Logo and title
//            GUILayout.Label(new GUIContent("🔀 Git Hub", "Ultimate Git Management"), EditorStyles.toolbarButton);

//            // Current branch indicator
//            GUI.color = repo.hasConflicts ? Color.red : (repo.hasUncommittedChanges ? Color.yellow : Color.green);
//            if (GUILayout.Button($"⎇ {repo.currentBranch}", EditorStyles.toolbarDropDown, GUILayout.Width(120)))
//            {
//                ShowBranchQuickSwitch();
//            }
//            GUI.color = Color.white;

//            // Sync status
//            if (repo.ahead > 0 || repo.behind > 0)
//            {
//                string syncStatus = "";
//                if (repo.ahead > 0)
//                    syncStatus += $"↑{repo.ahead} ";
//                if (repo.behind > 0)
//                    syncStatus += $"↓{repo.behind}";
//                GUILayout.Label(syncStatus, EditorStyles.toolbarButton, GUILayout.Width(60));
//            }

//            GUILayout.FlexibleSpace();

//            // Quick search
//            quickActionSearch = EditorGUILayout.TextField(quickActionSearch, EditorStyles.toolbarSearchField, GUILayout.Width(150));

//            // Status indicators
//            if (repo.hasConflicts)
//            {
//                GUI.color = Color.red;
//                GUILayout.Label("⚠ Conflicts", EditorStyles.toolbarButton);
//                GUI.color = Color.white;
//            }

//            // Auto-refresh toggle
//            autoRefresh = GUILayout.Toggle(autoRefresh, new GUIContent("🔄", "Auto-refresh"), EditorStyles.toolbarButton, GUILayout.Width(30));

//            // Refresh button
//            if (GUILayout.Button(new GUIContent("↻", "Refresh"), EditorStyles.toolbarButton, GUILayout.Width(30)))
//            {
//                RefreshAll();
//            }

//            // Terminal toggle
//            showTerminal = GUILayout.Toggle(showTerminal, new GUIContent("▶", "Terminal"), EditorStyles.toolbarButton, GUILayout.Width(30));

//            // Settings
//            if (GUILayout.Button(new GUIContent("⚙", "Settings"), EditorStyles.toolbarButton, GUILayout.Width(30)))
//            {
//                currentTab = 5;
//            }

//            EditorGUILayout.EndHorizontal();
//        }

//        private void DrawQuickActions()
//        {
//            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

//            // Filter quick actions based on search
//            var filteredActions = string.IsNullOrEmpty(quickActionSearch) ?
//                quickActions.Take(8).ToList() :
//                quickActions.Where(a => a.name.ToLower().Contains(quickActionSearch.ToLower())).ToList();

//            foreach (var action in filteredActions)
//            {
//                if (GUILayout.Button(new GUIContent(action.name, action.icon, action.tooltip), EditorStyles.toolbarButton))
//                {
//                    if (action.requiresConfirmation)
//                    {
//                        if (EditorUtility.DisplayDialog("Confirm Action", $"Execute '{action.name}'?", "Yes", "Cancel"))
//                        {
//                            action.action?.Invoke();
//                        }
//                    }
//                    else
//                    {
//                        action.action?.Invoke();
//                    }
//                }
//            }

//            GUILayout.FlexibleSpace();

//            EditorGUILayout.EndHorizontal();
//        }

//        // ========================================
//        // OVERVIEW TAB
//        // ========================================
//        private void DrawOverview()
//        {
//            EditorGUILayout.LabelField("Repository Overview", EditorStyles.boldLabel);

//            // Repository status card
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("Repository:", repo.rootPath);
//            if (GUILayout.Button("Open in Explorer", GUILayout.Width(120)))
//            {
//                EditorUtility.RevealInFinder(repo.rootPath);
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.Space(5);

//            // Status grid
//            EditorGUILayout.BeginHorizontal();

//            // Left column
//            EditorGUILayout.BeginVertical();
//            DrawStatusItem("Branch:", repo.currentBranch, repo.hasUncommittedChanges ? Color.yellow : Color.green);
//            DrawStatusItem("Commit:", repo.currentCommit.Substring(0, Math.Min(7, repo.currentCommit.Length)), Color.cyan);
//            DrawStatusItem("User:", repo.userName, Color.white);
//            DrawStatusItem("Email:", repo.userEmail, Color.white);
//            EditorGUILayout.EndVertical();

//            // Right column
//            EditorGUILayout.BeginVertical();
//            DrawStatusItem("Modified:", unstagedFiles.Count(f => f.changeType == ChangedFile.ChangeType.Modified).ToString(), Color.yellow);
//            DrawStatusItem("Untracked:", unstagedFiles.Count(f => f.changeType == ChangedFile.ChangeType.Untracked).ToString(), Color.gray);
//            DrawStatusItem("Staged:", stagedFiles.Count.ToString(), Color.green);
//            DrawStatusItem("Size:", FormatBytes(repo.repositorySize), Color.white);
//            EditorGUILayout.EndVertical();

//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.Space(10);

//            // Changes overview
//            DrawChangesOverview();

//            EditorGUILayout.Space(10);

//            // Quick commit section
//            DrawQuickCommit();

//            EditorGUILayout.Space(10);

//            // Recent activity
//            DrawRecentActivity();

//            DrawGitignoreStatus();
//        }

//        private void DrawChangesOverview()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Working Directory Changes", EditorStyles.boldLabel);

//            if (unstagedFiles.Count == 0 && stagedFiles.Count == 0)
//            {
//                EditorGUILayout.HelpBox("✓ Working directory is clean", MessageType.Info);
//            }
//            else
//            {
//                // Unstaged changes
//                if (unstagedFiles.Count > 0)
//                {
//                    EditorGUILayout.LabelField($"Unstaged Changes ({unstagedFiles.Count})", EditorStyles.boldLabel);

//                    changesScrollPos = EditorGUILayout.BeginScrollView(changesScrollPos, GUILayout.MaxHeight(150));

//                    foreach (var file in unstagedFiles.Take(10))
//                    {
//                        EditorGUILayout.BeginHorizontal();

//                        // Change type icon
//                        GUI.color = GetChangeTypeColor(file.changeType);
//                        EditorGUILayout.LabelField(GetChangeTypeIcon(file.changeType), GUILayout.Width(20));
//                        GUI.color = Color.white;

//                        // File path
//                        if (GUILayout.Button(file.path, EditorStyles.label))
//                        {
//                            selectedFilePath = file.path;
//                            ShowFileDiff(file.path);
//                        }

//                        // Stage button
//                        if (GUILayout.Button("Stage", GUILayout.Width(50)))
//                        {
//                            StageFile(file.path);
//                        }

//                        // Discard button
//                        if (GUILayout.Button("Discard", GUILayout.Width(60)))
//                        {
//                            if (EditorUtility.DisplayDialog("Discard Changes",
//                                $"Discard changes to {file.path}?", "Discard", "Cancel"))
//                            {
//                                DiscardFileChanges(file.path);
//                            }
//                        }

//                        EditorGUILayout.EndHorizontal();
//                    }

//                    EditorGUILayout.EndScrollView();

//                    // Bulk actions
//                    EditorGUILayout.BeginHorizontal();
//                    if (GUILayout.Button("Stage All"))
//                    {
//                        StageAllFiles();
//                    }
//                    if (GUILayout.Button("Discard All"))
//                    {
//                        if (EditorUtility.DisplayDialog("Discard All Changes",
//                            "This will discard ALL unstaged changes. Are you sure?", "Discard All", "Cancel"))
//                        {
//                            DiscardAllChanges();
//                        }
//                    }
//                    EditorGUILayout.EndHorizontal();
//                }

//                EditorGUILayout.Space(5);

//                // Staged changes
//                if (stagedFiles.Count > 0)
//                {
//                    EditorGUILayout.LabelField($"Staged Changes ({stagedFiles.Count})", EditorStyles.boldLabel);

//                    foreach (var file in stagedFiles.Take(5))
//                    {
//                        EditorGUILayout.BeginHorizontal();

//                        GUI.color = GetChangeTypeColor(file.changeType);
//                        EditorGUILayout.LabelField(GetChangeTypeIcon(file.changeType), GUILayout.Width(20));
//                        GUI.color = Color.white;

//                        EditorGUILayout.LabelField(file.path);

//                        if (GUILayout.Button("Unstage", GUILayout.Width(60)))
//                        {
//                            UnstageFile(file.path);
//                        }

//                        EditorGUILayout.EndHorizontal();
//                    }

//                    if (GUILayout.Button("Unstage All"))
//                    {
//                        UnstageAllFiles();
//                    }
//                }
//            }

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawQuickCommit()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Quick Commit", EditorStyles.boldLabel);

//            commitMessage = EditorGUILayout.TextField("Message:", commitMessage);

//            EditorGUILayout.BeginHorizontal();

//            GUI.enabled = !string.IsNullOrEmpty(commitMessage) && stagedFiles.Count > 0;
//            if (GUILayout.Button("Commit", GUILayout.Height(30)))
//            {
//                PerformCommit();
//            }
//            GUI.enabled = true;

//            if (GUILayout.Button("Commit & Push", GUILayout.Height(30)))
//            {
//                PerformCommit();
//                PushToRemote();
//            }

//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawRecentActivity()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Recent Commits", EditorStyles.boldLabel);

//            if (commitHistory.Count > 0)
//            {
//                foreach (var commit in commitHistory.Take(5))
//                {
//                    EditorGUILayout.BeginHorizontal();

//                    EditorGUILayout.LabelField(commit.shortHash, GUILayout.Width(60));
//                    EditorGUILayout.LabelField(commit.message, EditorStyles.wordWrappedLabel);
//                    EditorGUILayout.LabelField(commit.author, GUILayout.Width(100));
//                    EditorGUILayout.LabelField(GetRelativeTime(commit.date), GUILayout.Width(80));

//                    EditorGUILayout.EndHorizontal();
//                }
//            }
//            else
//            {
//                EditorGUILayout.LabelField("No commits yet");
//            }

//            EditorGUILayout.EndVertical();
//        }

//        // ========================================
//        // COMMIT INTERFACE TAB
//        // ========================================
//        private void DrawCommitInterface()
//        {
//            EditorGUILayout.LabelField("Commit Changes", EditorStyles.boldLabel);

//            // File changes section
//            EditorGUILayout.BeginHorizontal();

//            // Unstaged files
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width * 0.45f));
//            EditorGUILayout.LabelField($"Unstaged Changes ({unstagedFiles.Count})", EditorStyles.boldLabel);

//            DrawFileList(unstagedFiles, false);

//            EditorGUILayout.Space(5);

//            EditorGUILayout.BeginHorizontal();
//            if (GUILayout.Button("Stage Selected"))
//            {
//                StageSelectedFiles();
//            }
//            if (GUILayout.Button("Stage All"))
//            {
//                StageAllFiles();
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();

//            // Transfer buttons
//            EditorGUILayout.BeginVertical(GUILayout.Width(40));
//            GUILayout.FlexibleSpace();
//            if (GUILayout.Button("→", GUILayout.Height(30)))
//            {
//                StageSelectedFiles();
//            }
//            if (GUILayout.Button("←", GUILayout.Height(30)))
//            {
//                UnstageSelectedFiles();
//            }
//            GUILayout.FlexibleSpace();
//            EditorGUILayout.EndVertical();

//            // Staged files
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width * 0.45f));
//            EditorGUILayout.LabelField($"Staged Changes ({stagedFiles.Count})", EditorStyles.boldLabel);

//            DrawFileList(stagedFiles, true);

//            EditorGUILayout.Space(5);

//            EditorGUILayout.BeginHorizontal();
//            if (GUILayout.Button("Unstage Selected"))
//            {
//                UnstageSelectedFiles();
//            }
//            if (GUILayout.Button("Unstage All"))
//            {
//                UnstageAllFiles();
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.Space(10);

//            // Commit message section
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Commit Message", EditorStyles.boldLabel);

//            commitMessage = EditorGUILayout.TextField("Summary:", commitMessage, GUILayout.Height(20));

//            EditorGUILayout.LabelField("Description:");
//            commitDescription = EditorGUILayout.TextArea(commitDescription, GUILayout.Height(60));

//            EditorGUILayout.Space(5);

//            // Commit options
//            EditorGUILayout.BeginHorizontal();
//            amendLastCommit = EditorGUILayout.Toggle("Amend last commit", amendLastCommit);
//            signOffCommit = EditorGUILayout.Toggle("Sign-off", signOffCommit);
//            EditorGUILayout.EndHorizontal();

//            // Commit templates
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("Template:", GUILayout.Width(60));
//            if (GUILayout.Button("feat:", GUILayout.Width(50)))
//                commitMessage = "feat: " + commitMessage;
//            if (GUILayout.Button("fix:", GUILayout.Width(50)))
//                commitMessage = "fix: " + commitMessage;
//            if (GUILayout.Button("docs:", GUILayout.Width(50)))
//                commitMessage = "docs: " + commitMessage;
//            if (GUILayout.Button("style:", GUILayout.Width(50)))
//                commitMessage = "style: " + commitMessage;
//            if (GUILayout.Button("refactor:", GUILayout.Width(70)))
//                commitMessage = "refactor: " + commitMessage;
//            if (GUILayout.Button("test:", GUILayout.Width(50)))
//                commitMessage = "test: " + commitMessage;
//            if (GUILayout.Button("chore:", GUILayout.Width(50)))
//                commitMessage = "chore: " + commitMessage;
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.Space(10);

//            // Commit actions
//            EditorGUILayout.BeginHorizontal();

//            GUI.enabled = !string.IsNullOrEmpty(commitMessage) && stagedFiles.Count > 0;
//            GUI.backgroundColor = Color.green;
//            if (GUILayout.Button("Commit", GUILayout.Height(35)))
//            {
//                PerformCommit();
//            }
//            GUI.backgroundColor = Color.white;

//            if (GUILayout.Button("Commit & Push", GUILayout.Height(35)))
//            {
//                PerformCommit();
//                PushToRemote();
//            }

//            if (GUILayout.Button("Commit & Sync", GUILayout.Height(35)))
//            {
//                PerformCommit();
//                SyncWithRemote();
//            }
//            GUI.enabled = true;

//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();

//            // Diff viewer
//            if (!string.IsNullOrEmpty(selectedFilePath))
//            {
//                EditorGUILayout.Space(10);
//                DrawDiffViewer();
//            }
//        }

//        private void DrawFileList(List<ChangedFile> files, bool staged)
//        {
//            var scrollPos = staged ? commitScrollPos : changesScrollPos;
//            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

//            foreach (var file in files)
//            {
//                EditorGUILayout.BeginHorizontal();

//                // Selection checkbox
//                bool selected = EditorGUILayout.Toggle(IsFileSelected(file.path), GUILayout.Width(20));
//                SetFileSelected(file.path, selected);

//                // File icon
//                GUI.color = GetChangeTypeColor(file.changeType);
//                EditorGUILayout.LabelField(GetChangeTypeIcon(file.changeType), GUILayout.Width(20));
//                GUI.color = Color.white;

//                // File path (clickable)
//                if (GUILayout.Button(file.path, EditorStyles.label))
//                {
//                    selectedFilePath = file.path;
//                    ShowFileDiff(file.path);
//                }

//                // Stats
//                if (!file.isBinary)
//                {
//                    GUI.color = Color.green;
//                    EditorGUILayout.LabelField($"+{file.additions}", GUILayout.Width(40));
//                    GUI.color = Color.red;
//                    EditorGUILayout.LabelField($"-{file.deletions}", GUILayout.Width(40));
//                    GUI.color = Color.white;
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("binary", GUILayout.Width(80));
//                }

//                EditorGUILayout.EndHorizontal();
//            }

//            EditorGUILayout.EndScrollView();

//            if (staged)
//                commitScrollPos = scrollPos;
//            else
//                changesScrollPos = scrollPos;
//        }

//        private void DrawDiffViewer()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField($"Diff: {selectedFilePath}", EditorStyles.boldLabel);

//            // Get diff content
//            string diff = ExecuteGitCommand($"diff {selectedFilePath}");

//            if (!string.IsNullOrEmpty(diff))
//            {
//                // Simple diff viewer (you could make this more sophisticated)
//                EditorGUILayout.TextArea(diff, GUILayout.Height(200));
//            }
//            else
//            {
//                EditorGUILayout.LabelField("No changes to display");
//            }

//            EditorGUILayout.EndVertical();
//        }

//        // ========================================
//        // BRANCH MANAGEMENT TAB
//        // ========================================
//        // ========================================
//        // FIX FOR NULL REFERENCE AND GUI LAYOUT ERRORS
//        // Replace the problematic DrawBranchManagement() method with this fixed version
//        // ========================================

//        private void DrawBranchManagement()
//        {
//            EditorGUILayout.LabelField("Branch Management", EditorStyles.boldLabel);

//            EditorGUILayout.BeginHorizontal();

//            // Branch list
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width * 0.35f));
//            EditorGUILayout.LabelField("Branches", EditorStyles.boldLabel);

//            showRemoteBranches = EditorGUILayout.Toggle("Show Remote", showRemoteBranches);

//            historyScrollPos = EditorGUILayout.BeginScrollView(historyScrollPos, GUILayout.Height(400));

//            // Local branches
//            EditorGUILayout.LabelField("Local:", EditorStyles.boldLabel);
//            foreach (var branch in localBranches)
//            {
//                EditorGUILayout.BeginHorizontal();

//                if (branch.isCurrent)
//                {
//                    GUI.color = Color.green;
//                    EditorGUILayout.LabelField("→", GUILayout.Width(15));
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("", GUILayout.Width(15));
//                }
//                GUI.color = Color.white;

//                bool isSelected = selectedBranch == branch;
//                if (GUILayout.Button(branch.name, isSelected ? EditorStyles.boldLabel : EditorStyles.label))
//                {
//                    selectedBranch = branch;
//                }

//                EditorGUILayout.EndHorizontal();
//            }

//            // Remote branches
//            if (showRemoteBranches)
//            {
//                EditorGUILayout.Space(10);
//                EditorGUILayout.LabelField("Remote:", EditorStyles.boldLabel);
//                foreach (var branch in remoteBranches)
//                {
//                    if (GUILayout.Button($"origin/{branch.name}", EditorStyles.label))
//                    {
//                        selectedBranch = branch;
//                    }
//                }
//            }

//            EditorGUILayout.EndScrollView();
//            EditorGUILayout.EndVertical();

//            // Branch operations
//            EditorGUILayout.BeginVertical();

//            // Branch info
//            if (selectedBranch != null)
//            {
//                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//                EditorGUILayout.LabelField("Branch Information", EditorStyles.boldLabel);

//                EditorGUILayout.LabelField($"Name: {selectedBranch.name}");
//                EditorGUILayout.LabelField($"Last Commit: {selectedBranch.lastCommit ?? "N/A"}");
//                EditorGUILayout.LabelField($"Last Activity: {GetRelativeTime(selectedBranch.lastActivity)}");

//                if (!string.IsNullOrEmpty(selectedBranch.trackingBranch))
//                {
//                    EditorGUILayout.LabelField($"Tracking: {selectedBranch.trackingBranch}");
//                    EditorGUILayout.LabelField($"Ahead: {selectedBranch.commitsAhead} | Behind: {selectedBranch.commitsBehind}");
//                }

//                EditorGUILayout.Space(10);

//                // Branch actions
//                EditorGUILayout.BeginHorizontal();

//                if (!selectedBranch.isCurrent)
//                {
//                    GUI.backgroundColor = Color.green;
//                    if (GUILayout.Button("Switch to This Branch", GUILayout.Height(30)))
//                    {
//                        if (repo.hasUncommittedChanges)
//                        {
//                            if (EditorUtility.DisplayDialog("Uncommitted Changes",
//                                $"You have uncommitted changes.\n\nSwitch to '{selectedBranch.name}' and:",
//                                "Stash & Switch", "Cancel"))
//                            {
//                                StashChanges();
//                                SwitchBranch(selectedBranch.name);

//                                if (EditorUtility.DisplayDialog("Apply Stash?",
//                                    "Would you like to apply your stashed changes to the new branch?",
//                                    "Yes", "No"))
//                                {
//                                    PopStash();
//                                }
//                            }
//                        }
//                        else
//                        {
//                            SwitchBranch(selectedBranch.name);
//                        }
//                    }
//                    GUI.backgroundColor = Color.white;

//                    // Quick merge button
//                    GUI.backgroundColor = Color.cyan;
//                    if (GUILayout.Button($"Merge into '{repo.currentBranch}'", GUILayout.Height(30)))
//                    {
//                        if (EditorUtility.DisplayDialog("Quick Merge",
//                            $"Merge '{selectedBranch.name}' into '{repo.currentBranch}'?",
//                            "Merge", "Cancel"))
//                        {
//                            MergeBranch(selectedBranch.name);
//                        }
//                    }
//                    GUI.backgroundColor = Color.white;
//                }

//                if (selectedBranch.isLocal && !selectedBranch.isCurrent)
//                {
//                    GUI.backgroundColor = Color.red;
//                    if (GUILayout.Button("Delete", GUILayout.Height(30)))
//                    {
//                        if (EditorUtility.DisplayDialog("Delete Branch",
//                            $"Delete branch '{selectedBranch.name}'?\n\n" +
//                            "Make sure all important changes are merged first!",
//                            "Delete", "Cancel"))
//                        {
//                            DeleteBranch(selectedBranch.name);
//                        }
//                    }
//                    GUI.backgroundColor = Color.white;
//                }

//                if (selectedBranch.isRemote)
//                {
//                    if (GUILayout.Button("Checkout", GUILayout.Height(30)))
//                    {
//                        CheckoutRemoteBranch(selectedBranch.name);
//                    }
//                }

//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.EndVertical();
//            }
//            else
//            {
//                // Show message when no branch is selected
//                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//                EditorGUILayout.LabelField("Select a branch to view details and actions", EditorStyles.wordWrappedLabel);
//                EditorGUILayout.EndVertical();
//            }

//            EditorGUILayout.Space(10);

//            // Create new branch
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Create New Branch", EditorStyles.boldLabel);

//            newBranchName = EditorGUILayout.TextField("Branch Name:", newBranchName);

//            EditorGUILayout.BeginHorizontal();

//            GUI.enabled = !string.IsNullOrEmpty(newBranchName);
//            if (GUILayout.Button("Create", GUILayout.Height(30)))
//            {
//                CreateBranch(newBranchName, false);
//            }

//            if (GUILayout.Button("Create & Switch", GUILayout.Height(30)))
//            {
//                CreateBranch(newBranchName, true);
//            }
//            GUI.enabled = true;

//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.Space(10);

//            // Merge operations
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Merge Operations", EditorStyles.boldLabel);

//            // Current branch info
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("Current Branch:", GUILayout.Width(100));
//            GUI.color = Color.green;
//            EditorGUILayout.LabelField(repo.currentBranch ?? "unknown", EditorStyles.boldLabel);
//            GUI.color = Color.white;
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.Space(5);

//            // Quick merge from selected branch - WITH NULL CHECK
//            if (selectedBranch != null && !selectedBranch.isCurrent)
//            {
//                EditorGUILayout.BeginHorizontal();
//                EditorGUILayout.LabelField($"Selected: {selectedBranch.name}", GUILayout.Width(200));

//                GUI.backgroundColor = Color.cyan;
//                if (GUILayout.Button($"Merge '{selectedBranch.name}' → '{repo.currentBranch}'", GUILayout.Height(30)))
//                {
//                    if (!showMergeConfirmation || EditorUtility.DisplayDialog("Confirm Merge",
//                        $"Merge branch '{selectedBranch.name}' into current branch '{repo.currentBranch}'?\n\n" +
//                        "This will bring all changes from the selected branch into your current branch.",
//                        "Merge", "Cancel"))
//                    {
//                        MergeBranch(selectedBranch.name);
//                    }
//                }
//                GUI.backgroundColor = Color.white;

//                EditorGUILayout.EndHorizontal();
//            }
//            else if (selectedBranch == null)
//            {
//                EditorGUILayout.HelpBox("Select a branch from the list to merge it into the current branch.", MessageType.Info);
//            }

//            EditorGUILayout.Space(5);

//            // Manual merge
//            EditorGUILayout.LabelField("Or enter branch name manually:");
//            EditorGUILayout.BeginHorizontal();
//            mergeSourceBranch = EditorGUILayout.TextField(mergeSourceBranch);

//            GUI.enabled = !string.IsNullOrEmpty(mergeSourceBranch);
//            if (GUILayout.Button("Merge", GUILayout.Width(80)))
//            {
//                if (EditorUtility.DisplayDialog("Merge Branch",
//                    $"Merge '{mergeSourceBranch}' into '{repo.currentBranch}'?", "Merge", "Cancel"))
//                {
//                    MergeBranch(mergeSourceBranch);
//                }
//            }
//            GUI.enabled = true;

//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.Space(5);

//            // Merge options
//            showMergeConfirmation = EditorGUILayout.Toggle("Show Confirmation", showMergeConfirmation);

//            // Conflict resolution
//            if (repo != null && repo.hasConflicts)
//            {
//                GUI.backgroundColor = Color.red;
//                EditorGUILayout.HelpBox("⚠ Merge conflicts detected! Resolve them in your IDE, then stage and commit.", MessageType.Error);

//                if (GUILayout.Button("Abort Merge", GUILayout.Height(25)))
//                {
//                    AbortMerge();
//                }
//                GUI.backgroundColor = Color.white;
//            }

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.EndVertical(); // End branch operations vertical

//            EditorGUILayout.EndHorizontal(); // End main horizontal
//        }

//        // ========================================
//        // ADDITIONAL SAFETY FIXES
//        // Add these null checks to other methods that might have issues
//        // ========================================

//        // Fix for RefreshAll() - add null checks
//        private void RefreshAll()
//        {
//            if (!IsGitRepository())
//                return;

//            isRefreshing = true;

//            try
//            {
//                RefreshStatus();
//                RefreshBranches();
//                RefreshRemotes();
//                RefreshHistory();
//                RefreshFileChanges();
//            }
//            catch (Exception e)
//            {
//                UnityEngine.Debug.LogError($"Error during refresh: {e.Message}");
//            }
//            finally
//            {
//                isRefreshing = false;
//                Repaint();
//            }
//        }

//        // Fix for OnGUI() - add error handling
//        private void OnGUI()
//        {
//            try
//            {
//                DrawToolbar();
//                DrawQuickActions();

//                mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

//                // Tab selection
//                EditorGUILayout.BeginHorizontal();
//                GUILayout.Space(10);
//                currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(30));
//                GUILayout.Space(10);
//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.Space(10);

//                // Draw current tab content with error handling
//                try
//                {
//                    switch (currentTab)
//                    {
//                        case 0:
//                            DrawOverview();
//                            break;
//                        case 1:
//                            DrawCommitInterface();
//                            break;
//                        case 2:
//                            DrawBranchManagement();
//                            break;
//                        case 3:
//                            DrawRemoteOperations();
//                            break;
//                        case 4:
//                            DrawHistory();
//                            break;
//                        case 5:
//                            DrawSettings();
//                            break;
//                    }
//                }
//                catch (Exception e)
//                {
//                    EditorGUILayout.HelpBox($"Error in tab {tabNames[currentTab]}: {e.Message}", MessageType.Error);
//                    UnityEngine.Debug.LogError($"Git Hub tab error: {e}");
//                }

//                EditorGUILayout.EndScrollView();

//                // Draw terminal output if enabled
//                if (showTerminal)
//                {
//                    DrawTerminal();
//                }
//            }
//            catch (Exception e)
//            {
//                // Emergency recovery - ensure GUI state is clean
//                UnityEngine.Debug.LogError($"Git Hub GUI Error: {e}");
//                GUILayout.Label($"Error: {e.Message}");
//            }
//        }

//        // Initialize lists in OnEnable to prevent null references
//        private void OnEnable()
//        {
//            // Initialize all lists to prevent null references
//            if (stagedFiles == null)
//                stagedFiles = new List<ChangedFile>();
//            if (unstagedFiles == null)
//                unstagedFiles = new List<ChangedFile>();
//            if (localBranches == null)
//                localBranches = new List<GitBranch>();
//            if (remoteBranches == null)
//                remoteBranches = new List<GitBranch>();
//            if (remotes == null)
//                remotes = new List<GitRemote>();
//            if (commitHistory == null)
//                commitHistory = new List<GitCommit>();
//            if (aliases == null)
//                aliases = new List<GitAlias>();
//            if (quickActions == null)
//                quickActions = new List<QuickAction>();
//            if (repo == null)
//                repo = new GitRepository();
//            if (settings == null)
//                settings = new GitSettings();

//            LoadSettings();
//            InitializeRepository();
//            InitializeQuickActions();
//            RefreshAll();

//            EditorApplication.update += OnUpdate;
//            Undo.undoRedoPerformed += OnUndoRedo;
//        }

//        // ========================================
//        // REMOTE OPERATIONS TAB - PART 1
//        // ========================================
//        private void DrawRemoteOperations()
//        {
//            EditorGUILayout.LabelField("Remote Repository Management", EditorStyles.boldLabel);

//            EditorGUILayout.BeginHorizontal();

//            // Remote list
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width * 0.4f));
//            EditorGUILayout.LabelField("Configured Remotes", EditorStyles.boldLabel);

//            foreach (var remote in remotes)
//            {
//                bool isSelected = selectedRemote == remote;
//                GUI.backgroundColor = isSelected ? Color.cyan : Color.white;

//                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

//                EditorGUILayout.BeginHorizontal();
//                if (GUILayout.Button(remote.name, EditorStyles.boldLabel))
//                {
//                    selectedRemote = remote;
//                }

//                GUI.color = remote.isConnected ? Color.green : Color.red;
//                EditorGUILayout.LabelField(remote.isConnected ? "●" : "○", GUILayout.Width(20));
//                GUI.color = Color.white;

//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.LabelField(remote.fetchUrl, EditorStyles.miniLabel);

//                EditorGUILayout.EndVertical();
//            }

//            GUI.backgroundColor = Color.white;

//            EditorGUILayout.Space(10);

//            // Add remote
//            EditorGUILayout.LabelField("Add Remote:", EditorStyles.boldLabel);
//            newRemoteName = EditorGUILayout.TextField("Name:", newRemoteName);
//            newRemoteUrl = EditorGUILayout.TextField("URL:", newRemoteUrl);

//            if (GUILayout.Button("Add Remote"))
//            {
//                AddRemote(newRemoteName, newRemoteUrl);
//            }

//            EditorGUILayout.EndVertical();

//            // Remote operations
//            EditorGUILayout.BeginVertical();

//            if (selectedRemote != null)
//            {
//                // Remote info
//                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//                EditorGUILayout.LabelField("Remote Details", EditorStyles.boldLabel);

//                EditorGUILayout.LabelField($"Name: {selectedRemote.name}");
//                EditorGUILayout.LabelField($"Fetch URL: {selectedRemote.fetchUrl}");
//                EditorGUILayout.LabelField($"Push URL: {selectedRemote.pushUrl}");
//                EditorGUILayout.LabelField($"Last Sync: {GetRelativeTime(selectedRemote.lastSync)}");
//                EditorGUILayout.LabelField($"Branches: {selectedRemote.branches.Count}");

//                EditorGUILayout.Space(10);

//                // Remote actions
//                EditorGUILayout.BeginHorizontal();

//                if (GUILayout.Button("Fetch", GUILayout.Height(30)))
//                {
//                    FetchFromRemote(selectedRemote.name);
//                }

//                if (GUILayout.Button("Pull", GUILayout.Height(30)))
//                {
//                    PullFromRemote(selectedRemote.name);
//                }

//                GUI.backgroundColor = Color.green;
//                if (GUILayout.Button("Push", GUILayout.Height(30)))
//                {
//                    PushToRemote(selectedRemote.name);
//                }
//                GUI.backgroundColor = Color.white;

//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.Space(5);

//                EditorGUILayout.BeginHorizontal();

//                if (GUILayout.Button("Prune", GUILayout.Height(25)))
//                {
//                    PruneRemote(selectedRemote.name);
//                }

//                GUI.backgroundColor = Color.red;
//                if (GUILayout.Button("Remove", GUILayout.Height(25)))
//                {
//                    if (EditorUtility.DisplayDialog("Remove Remote",
//                        $"Remove remote '{selectedRemote.name}'?", "Remove", "Cancel"))
//                    {
//                        RemoveRemote(selectedRemote.name);
//                    }
//                }
//                GUI.backgroundColor = Color.white;

//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.EndVertical();
//            }

//            EditorGUILayout.Space(10);

//            // Push options
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Push Options", EditorStyles.boldLabel);

//            forcePush = EditorGUILayout.Toggle("Force Push", forcePush);
//            pushTags = EditorGUILayout.Toggle("Push Tags", pushTags);

//            EditorGUILayout.BeginHorizontal();

//            GUI.backgroundColor = forcePush ? Color.red : Color.green;
//            if (GUILayout.Button(forcePush ? "Force Push" : "Push", GUILayout.Height(35)))
//            {
//                if (!forcePush || EditorUtility.DisplayDialog("Force Push",
//                    "Force push will overwrite remote changes. Are you sure?", "Force Push", "Cancel"))
//                {
//                    PushToRemote();
//                }
//            }
//            GUI.backgroundColor = Color.white;

//            if (GUILayout.Button("Push to All", GUILayout.Height(35)))
//            {
//                PushToAllRemotes();
//            }

//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.Space(10);

//            // Mirror operations
//            DrawMirrorOperations();

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.EndHorizontal();
//        }

//        private void DrawMirrorOperations()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Mirror Repository (Safe Export)", EditorStyles.boldLabel);

//            // Mirror safety settings
//            EditorGUILayout.BeginHorizontal();
//            enableMirrorExport = EditorGUILayout.Toggle("Enable Mirror Export", enableMirrorExport);
//            if (GUILayout.Button("?", GUILayout.Width(20)))
//            {
//                EditorUtility.DisplayDialog("Mirror Export",
//                    "Mirror export creates a copy of your repository OUTSIDE the Unity project to prevent file conflicts.\n\n" +
//                    "Exports to: [Parent Directory]/Mirrored/[ProjectName]_[DateTime]/",
//                    "OK");
//            }
//            EditorGUILayout.EndHorizontal();

//            if (enableMirrorExport)
//            {
//                EditorGUILayout.Space(5);

//                // Show current export path
//                string projectName = Application.productName.Replace(" ", "_");
//                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
//                string defaultExportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Mirrored", $"{projectName}_{timestamp}"));

//                EditorGUILayout.BeginHorizontal();
//                EditorGUILayout.LabelField("Export Path:", GUILayout.Width(80));
//                EditorGUILayout.LabelField(defaultExportPath, EditorStyles.miniLabel);
//                EditorGUILayout.EndHorizontal();

//                if (lastMirrorExport != DateTime.MinValue)
//                {
//                    EditorGUILayout.LabelField($"Last Export: {GetRelativeTime(lastMirrorExport)}", EditorStyles.miniLabel);
//                }

//                EditorGUILayout.Space(5);

//                // Mirror URL for remote mirror
//                mirrorUrl = EditorGUILayout.TextField("Remote Mirror URL:", mirrorUrl);

//                EditorGUILayout.BeginHorizontal();

//                // Local mirror export (SAFE)
//                GUI.backgroundColor = Color.green;
//                if (GUILayout.Button("Export Local Mirror", GUILayout.Height(30)))
//                {
//                    if (EditorUtility.DisplayDialog("Export Mirror",
//                        $"This will export a mirror copy to:\n{defaultExportPath}\n\nThis is OUTSIDE your Unity project (safe).\n\nContinue?",
//                        "Export", "Cancel"))
//                    {
//                        ExportLocalMirror(defaultExportPath);
//                    }
//                }
//                GUI.backgroundColor = Color.white;

//                // Remote mirror push
//                if (!string.IsNullOrEmpty(mirrorUrl))
//                {
//                    if (GUILayout.Button("Push to Remote Mirror", GUILayout.Height(30)))
//                    {
//                        PushToMirror();
//                    }
//                }

//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.Space(5);

//                // Download ZIP (safe)
//                if (GUILayout.Button("Download as ZIP", GUILayout.Height(25)))
//                {
//                    DownloadRepositoryZip();
//                }

//                // Auto-mirror option (advanced)
//                EditorGUILayout.Space(5);
//                autoCreateMirror = EditorGUILayout.Toggle("Auto-Mirror on Push", autoCreateMirror);
//                if (autoCreateMirror)
//                {
//                    EditorGUILayout.HelpBox("Auto-mirror will create a backup outside the project after each push.", MessageType.Info);
//                }
//            }
//            else
//            {
//                EditorGUILayout.HelpBox("Mirror export is disabled. Enable it to create safe backups outside your Unity project.", MessageType.Info);

//                if (GUILayout.Button("Quick Backup (ZIP)", GUILayout.Height(25)))
//                {
//                    CreateQuickBackup();
//                }
//            }

//            EditorGUILayout.EndVertical();
//        }

//        //private void DrawMirrorOperations()
//        //{
//        //    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//        //    EditorGUILayout.LabelField("Mirror Repository", EditorStyles.boldLabel);

//        //    mirrorUrl = EditorGUILayout.TextField("Mirror URL:", mirrorUrl);

//        //    EditorGUILayout.BeginHorizontal();

//        //    if (GUILayout.Button("Push to Mirror", GUILayout.Height(30)))
//        //    {
//        //        PushToMirror();
//        //    }

//        //    if (GUILayout.Button("Download ZIP", GUILayout.Height(30)))
//        //    {
//        //        DownloadRepositoryZip();
//        //    }

//        //    if (GUILayout.Button("Create Backup", GUILayout.Height(30)))
//        //    {
//        //        CreateBackup();
//        //    }

//        //    EditorGUILayout.EndHorizontal();

//        //    EditorGUILayout.EndVertical();
//        //}

//        // ========================================
//        // HISTORY TAB
//        // ========================================
//        private void DrawHistory()
//        {
//            EditorGUILayout.LabelField("Commit History", EditorStyles.boldLabel);

//            // Filter controls
//            EditorGUILayout.BeginHorizontal();
//            historyFilter = EditorGUILayout.TextField("Search:", historyFilter);
//            historyLimit = EditorGUILayout.IntField("Limit:", historyLimit, GUILayout.Width(100));

//            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
//            {
//                RefreshHistory();
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.Space(10);

//            // Commit list
//            EditorGUILayout.BeginHorizontal();

//            // Left panel - commit list
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width * 0.5f));

//            historyScrollPos = EditorGUILayout.BeginScrollView(historyScrollPos, GUILayout.Height(500));

//            var filteredCommits = string.IsNullOrEmpty(historyFilter) ?
//                commitHistory :
//                commitHistory.Where(c =>
//                    c.message.ToLower().Contains(historyFilter.ToLower()) ||
//                    c.author.ToLower().Contains(historyFilter.ToLower())).ToList();

//            foreach (var commit in filteredCommits.Take(historyLimit))
//            {
//                bool isSelected = selectedCommit == commit;
//                GUI.backgroundColor = isSelected ? Color.cyan : Color.white;

//                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

//                EditorGUILayout.BeginHorizontal();

//                // Commit hash
//                GUI.color = Color.cyan;
//                if (GUILayout.Button(commit.shortHash, EditorStyles.label, GUILayout.Width(60)))
//                {
//                    selectedCommit = commit;
//                }
//                GUI.color = Color.white;

//                // Commit message
//                EditorGUILayout.LabelField(commit.message, EditorStyles.wordWrappedLabel);

//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.BeginHorizontal();

//                // Author
//                EditorGUILayout.LabelField(commit.author, EditorStyles.miniLabel, GUILayout.Width(150));

//                // Date
//                EditorGUILayout.LabelField(GetRelativeTime(commit.date), EditorStyles.miniLabel, GUILayout.Width(100));

//                // Stats
//                GUI.color = Color.green;
//                EditorGUILayout.LabelField($"+{commit.additions}", EditorStyles.miniLabel, GUILayout.Width(40));
//                GUI.color = Color.red;
//                EditorGUILayout.LabelField($"-{commit.deletions}", EditorStyles.miniLabel, GUILayout.Width(40));
//                GUI.color = Color.white;

//                EditorGUILayout.EndHorizontal();

//                // Tags
//                if (commit.tags.Count > 0)
//                {
//                    EditorGUILayout.BeginHorizontal();
//                    foreach (var tag in commit.tags)
//                    {
//                        GUI.color = Color.yellow;
//                        EditorGUILayout.LabelField($"[{tag}]", EditorStyles.miniLabel, GUILayout.Width(60));
//                        GUI.color = Color.white;
//                    }
//                    EditorGUILayout.EndHorizontal();
//                }

//                EditorGUILayout.EndVertical();

//                GUI.backgroundColor = Color.white;
//            }

//            EditorGUILayout.EndScrollView();
//            EditorGUILayout.EndVertical();

//            // Right panel - commit details
//            EditorGUILayout.BeginVertical();

//            if (selectedCommit != null)
//            {
//                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//                EditorGUILayout.LabelField("Commit Details", EditorStyles.boldLabel);

//                EditorGUILayout.LabelField($"Hash: {selectedCommit.hash}");
//                EditorGUILayout.LabelField($"Author: {selectedCommit.author} <{selectedCommit.email}>");
//                EditorGUILayout.LabelField($"Date: {selectedCommit.date}");
//                EditorGUILayout.LabelField($"Branch: {selectedCommit.branch}");

//                EditorGUILayout.Space(10);

//                EditorGUILayout.LabelField("Message:", EditorStyles.boldLabel);
//                EditorGUILayout.TextArea(selectedCommit.message + "\n\n" + selectedCommit.description,
//                    EditorStyles.wordWrappedLabel, GUILayout.Height(80));

//                EditorGUILayout.Space(10);

//                EditorGUILayout.LabelField($"Changed Files ({selectedCommit.changedFiles.Count}):", EditorStyles.boldLabel);

//                var fileScrollPos = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(150));
//                foreach (var file in selectedCommit.changedFiles)
//                {
//                    EditorGUILayout.LabelField(file, EditorStyles.miniLabel);
//                }
//                EditorGUILayout.EndScrollView();

//                EditorGUILayout.Space(10);

//                // Commit actions
//                EditorGUILayout.BeginHorizontal();

//                if (GUILayout.Button("Checkout", GUILayout.Height(30)))
//                {
//                    if (EditorUtility.DisplayDialog("Checkout Commit",
//                        $"Checkout commit {selectedCommit.shortHash}?", "Checkout", "Cancel"))
//                    {
//                        CheckoutCommit(selectedCommit.hash);
//                    }
//                }

//                if (GUILayout.Button("Cherry-pick", GUILayout.Height(30)))
//                {
//                    CherryPickCommit(selectedCommit.hash);
//                }

//                if (GUILayout.Button("Revert", GUILayout.Height(30)))
//                {
//                    if (EditorUtility.DisplayDialog("Revert Commit",
//                        $"Create a revert commit for {selectedCommit.shortHash}?", "Revert", "Cancel"))
//                    {
//                        RevertCommit(selectedCommit.hash);
//                    }
//                }

//                EditorGUILayout.EndHorizontal();

//                EditorGUILayout.EndVertical();
//            }

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.EndHorizontal();
//        }

//        // ========================================
//        // SETTINGS TAB
//        // ========================================
//        private void DrawSettings()
//        {
//            EditorGUILayout.LabelField("Git Settings", EditorStyles.boldLabel);

//            // General settings
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);

//            settings.autoFetch = EditorGUILayout.Toggle("Auto Fetch", settings.autoFetch);
//            if (settings.autoFetch)
//            {
//                settings.autoFetchInterval = EditorGUILayout.IntSlider("Fetch Interval (seconds)",
//                    settings.autoFetchInterval, 60, 3600);
//            }

//            settings.autoPushAfterCommit = EditorGUILayout.Toggle("Auto Push After Commit", settings.autoPushAfterCommit);
//            settings.showFileIcons = EditorGUILayout.Toggle("Show File Icons", settings.showFileIcons);
//            settings.compactMode = EditorGUILayout.Toggle("Compact Mode", settings.compactMode);
//            settings.verboseOutput = EditorGUILayout.Toggle("Verbose Output", settings.verboseOutput);

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.Space(10);

//            // User settings
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("User Configuration", EditorStyles.boldLabel);

//            repo.userName = EditorGUILayout.TextField("Name:", repo.userName);
//            repo.userEmail = EditorGUILayout.TextField("Email:", repo.userEmail);

//            if (GUILayout.Button("Apply User Config"))
//            {
//                ApplyUserConfig();
//            }

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.Space(10);

//            // Advanced settings
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);

//            settings.defaultBranch = EditorGUILayout.TextField("Default Branch:", settings.defaultBranch);
//            settings.useLFS = EditorGUILayout.Toggle("Use Git LFS", settings.useLFS);
//            settings.gpgSign = EditorGUILayout.Toggle("GPG Sign Commits", settings.gpgSign);
//            settings.pruneOnFetch = EditorGUILayout.Toggle("Prune on Fetch", settings.pruneOnFetch);

//            EditorGUILayout.LabelField("Merge Strategy:");
//            settings.mergeStrategy = EditorGUILayout.TextField(settings.mergeStrategy);

//            EditorGUILayout.EndVertical();

//            EditorGUILayout.Space(10);

//            // Git aliases
//            DrawGitAliases();

//            EditorGUILayout.Space(10);

//            // Maintenance
//            DrawMaintenance();

//            DrawSafetySettings();
//        }

//        private void DrawGitAliases()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Git Aliases", EditorStyles.boldLabel);

//            if (GUILayout.Button("Add Alias"))
//            {
//                aliases.Add(new GitAlias { name = "new-alias" });
//            }

//            var aliasScrollPos = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(150));

//            for (int i = 0; i < aliases.Count; i++)
//            {
//                EditorGUILayout.BeginHorizontal();

//                aliases[i].name = EditorGUILayout.TextField(aliases[i].name, GUILayout.Width(100));
//                aliases[i].command = EditorGUILayout.TextField(aliases[i].command);
//                aliases[i].hotkey = (KeyCode)EditorGUILayout.EnumPopup(aliases[i].hotkey, GUILayout.Width(80));

//                if (GUILayout.Button("X", GUILayout.Width(25)))
//                {
//                    aliases.RemoveAt(i);
//                    break;
//                }

//                EditorGUILayout.EndHorizontal();
//            }

//            EditorGUILayout.EndScrollView();

//            if (GUILayout.Button("Apply Aliases"))
//            {
//                ApplyGitAliases();
//            }

//            EditorGUILayout.EndVertical();
//        }

//        private void DrawMaintenance()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//            EditorGUILayout.LabelField("Repository Maintenance", EditorStyles.boldLabel);

//            EditorGUILayout.LabelField($"Repository Size: {FormatBytes(repo.repositorySize)}");

//            EditorGUILayout.Space(10);

//            EditorGUILayout.BeginHorizontal();

//            if (GUILayout.Button("Garbage Collection", GUILayout.Height(30)))
//            {
//                RunGarbageCollection();
//            }

//            if (GUILayout.Button("Verify Integrity", GUILayout.Height(30)))
//            {
//                VerifyRepositoryIntegrity();
//            }

//            if (GUILayout.Button("Clean", GUILayout.Height(30)))
//            {
//                CleanRepository();
//            }

//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.Space(5);

//            if (GUILayout.Button("Reset to HEAD", GUILayout.Height(25)))
//            {
//                if (EditorUtility.DisplayDialog("Reset Repository",
//                    "This will discard ALL local changes. Are you sure?", "Reset", "Cancel"))
//                {
//                    ResetToHead();
//                }
//            }

//            EditorGUILayout.EndVertical();
//        }

//        //private void DrawSafetySettings()
//        //{
//        //    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//        //    EditorGUILayout.LabelField("Safety Settings", EditorStyles.boldLabel);

//        //    safety.enableSafetyChecks = EditorGUILayout.Toggle("Enable Safety Checks", safety.enableSafetyChecks);
//        //    safety.autoBackupBeforeMerge = EditorGUILayout.Toggle("Auto-Backup Before Merge", safety.autoBackupBeforeMerge);
//        //    safety.autoBackupBeforePull = EditorGUILayout.Toggle("Auto-Backup Before Pull", safety.autoBackupBeforePull);
//        //    safety.requireConfirmationForDestructive = EditorGUILayout.Toggle("Require Confirmation", safety.requireConfirmationForDestructive);
//        //    safety.preventMirrorInProject = EditorGUILayout.Toggle("Prevent In-Project Mirrors", safety.preventMirrorInProject);
//        //    safety.maxBackupsToKeep = EditorGUILayout.IntSlider("Max Backups to Keep", safety.maxBackupsToKeep, 1, 20);

//        //    EditorGUILayout.Space(5);

//        //    if (safety.backupHistory.Count > 0)
//        //    {
//        //        EditorGUILayout.LabelField($"Recent Backups ({safety.backupHistory.Count}):", EditorStyles.boldLabel);

//        //        foreach (var backup in safety.backupHistory.TakeLast(3))
//        //        {
//        //            EditorGUILayout.BeginHorizontal();
//        //            EditorGUILayout.LabelField(Path.GetFileName(backup), EditorStyles.miniLabel);

//        //            if (GUILayout.Button("Restore", GUILayout.Width(60)))
//        //            {
//        //                if (EditorUtility.DisplayDialog("Restore Backup",
//        //                    $"Restore from this backup?\n{backup}", "Restore", "Cancel"))
//        //                {
//        //                    RestoreFromBackup(backup);
//        //                }
//        //            }

//        //            EditorGUILayout.EndHorizontal();
//        //        }
//        //    }

//        //    EditorGUILayout.Space(5);

//        //    if (GUILayout.Button("Open Backup Folder"))
//        //    {
//        //        string backupDir = Path.GetFullPath(Path.Combine(
//        //            Application.dataPath,
//        //            "../../GitSafetyBackups",
//        //            Application.productName));

//        //        if (Directory.Exists(backupDir))
//        //            EditorUtility.RevealInFinder(backupDir);
//        //    }

//        //    EditorGUILayout.EndVertical();
//        //}

//        // ========================================
//        // TERMINAL OUTPUT
//        // ========================================
//        private void DrawTerminal()
//        {
//            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(150));
//            EditorGUILayout.LabelField("Terminal Output", EditorStyles.boldLabel);

//            var terminalScrollPos = EditorGUILayout.BeginScrollView(Vector2.zero);
//            EditorGUILayout.TextArea(terminalOutput, EditorStyles.wordWrappedLabel);
//            EditorGUILayout.EndScrollView();

//            EditorGUILayout.BeginHorizontal();
//            if (GUILayout.Button("Clear"))
//            {
//                terminalOutput = "";
//            }
//            if (GUILayout.Button("Copy"))
//            {
//                GUIUtility.systemCopyBuffer = terminalOutput;
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();
//        }

//        // ========================================
//        // GIT OPERATIONS - CORE IMPLEMENTATION
//        // Continue in next file from here...
//        // ========================================

//        private void InitializeRepository()
//        {
//            repo.rootPath = Application.dataPath.Replace("/Assets", "");

//            if (!IsGitRepository())
//            {
//                if (EditorUtility.DisplayDialog("Initialize Git",
//                    "No Git repository found. Initialize one?", "Initialize", "Cancel"))
//                {
//                    ExecuteGitCommand("init");
//                    ExecuteGitCommand($"branch -M {settings.defaultBranch}");
//                }
//            }

//            RefreshAll();
//        }

//        private void InitializeQuickActions()
//        {
//            quickActions = new List<QuickAction>
//            {
//                new QuickAction {
//                    name = "Pull",
//                    tooltip = "Pull from remote",
//                    action = () => PullFromRemote(),
//                    shortcut = KeyCode.P
//                },
//                new QuickAction {
//                    name = "Push",
//                    tooltip = "Push to remote",
//                    action = () => PushToRemote(),
//                    shortcut = KeyCode.U
//                },
//                new QuickAction {
//                    name = "Fetch",
//                    tooltip = "Fetch from remote",
//                    action = () => FetchFromRemote(),
//                    shortcut = KeyCode.F
//                },
//                new QuickAction {
//                    name = "Stash",
//                    tooltip = "Stash changes",
//                    action = () => StashChanges(),
//                    shortcut = KeyCode.S
//                },
//                new QuickAction {
//                    name = "Pop Stash",
//                    tooltip = "Apply stashed changes",
//                    action = () => PopStash(),
//                    shortcut = KeyCode.O
//                },
//                new QuickAction {
//                    name = "Sync",
//                    tooltip = "Sync with remote",
//                    action = () => SyncWithRemote(),
//                    shortcut = KeyCode.Y
//                },
//                new QuickAction {
//                    name = "Discard All",
//                    tooltip = "Discard all changes",
//                    action = () => DiscardAllChanges(),
//                    requiresConfirmation = true
//                },
//                new QuickAction {
//                    name = "Clean",
//                    tooltip = "Clean untracked files",
//                    action = () => CleanRepository(),
//                    requiresConfirmation = true
//                }
//            };
//        }

//        // NOTE: Continue implementation in UltimateGitHub_Operations.cs
//        // All git command execution methods go there

//        // ========================================
//        // HELPER METHODS
//        // ========================================

//        private void DrawStatusItem(string label, string value, Color color)
//        {
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField(label, GUILayout.Width(60));
//            GUI.color = color;
//            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
//            GUI.color = Color.white;
//            EditorGUILayout.EndHorizontal();
//        }

//        private Color GetChangeTypeColor(ChangedFile.ChangeType type)
//        {
//            switch (type)
//            {
//                case ChangedFile.ChangeType.Added:
//                    return Color.green;
//                case ChangedFile.ChangeType.Modified:
//                    return Color.yellow;
//                case ChangedFile.ChangeType.Deleted:
//                    return Color.red;
//                case ChangedFile.ChangeType.Renamed:
//                    return Color.cyan;
//                case ChangedFile.ChangeType.Untracked:
//                    return Color.gray;
//                case ChangedFile.ChangeType.Conflicted:
//                    return Color.magenta;
//                default:
//                    return Color.white;
//            }
//        }

//        private string GetChangeTypeIcon(ChangedFile.ChangeType type)
//        {
//            switch (type)
//            {
//                case ChangedFile.ChangeType.Added:
//                    return "+";
//                case ChangedFile.ChangeType.Modified:
//                    return "M";
//                case ChangedFile.ChangeType.Deleted:
//                    return "-";
//                case ChangedFile.ChangeType.Renamed:
//                    return "R";
//                case ChangedFile.ChangeType.Untracked:
//                    return "?";
//                case ChangedFile.ChangeType.Conflicted:
//                    return "!";
//                default:
//                    return " ";
//            }
//        }

//        private string GetRelativeTime(DateTime date)
//        {
//            var span = DateTime.Now - date;

//            if (span.TotalMinutes < 1)
//                return "just now";
//            if (span.TotalMinutes < 60)
//                return $"{(int)span.TotalMinutes}m ago";
//            if (span.TotalHours < 24)
//                return $"{(int)span.TotalHours}h ago";
//            if (span.TotalDays < 30)
//                return $"{(int)span.TotalDays}d ago";
//            if (span.TotalDays < 365)
//                return $"{(int)(span.TotalDays / 30)}mo ago";
//            return $"{(int)(span.TotalDays / 365)}y ago";
//        }

//        private string FormatBytes(long bytes)
//        {
//            string[] sizes = { "B", "KB", "MB", "GB" };
//            int order = 0;
//            double size = bytes;

//            while (size >= 1024 && order < sizes.Length - 1)
//            {
//                order++;
//                size /= 1024;
//            }

//            return $"{size:F2} {sizes[order]}";
//        }

//        // File selection tracking
//        private HashSet<string> selectedFiles = new HashSet<string>();

//        private bool IsFileSelected(string path)
//        {
//            return selectedFiles.Contains(path);
//        }

//        private void SetFileSelected(string path, bool selected)
//        {
//            if (selected)
//                selectedFiles.Add(path);
//            else
//                selectedFiles.Remove(path);
//        }

//        private void ShowBranchQuickSwitch()
//        {
//            GenericMenu menu = new GenericMenu();

//            foreach (var branch in localBranches)
//            {
//                string branchName = branch.name;
//                menu.AddItem(new GUIContent(branchName), branch.isCurrent, () => SwitchBranch(branchName));
//            }

//            menu.ShowAsContext();
//        }

//        private void ShowFileDiff(string filePath)
//        {
//            selectedFilePath = filePath;
//            // The diff will be shown in the UI
//        }


//        // Replace the incomplete SaveSettings() method with this:
//        private void SaveSettings()
//        {
//            // Save settings to EditorPrefs
//            string settingsJson = JsonUtility.ToJson(settings);
//            EditorPrefs.SetString("UltimateGitHub_Settings", settingsJson);

//            // Save aliases
//            string aliasesJson = JsonUtility.ToJson(new SerializableList<GitAlias> { items = aliases });
//            EditorPrefs.SetString("UltimateGitHub_Aliases", aliasesJson);
//        }

//        private void LoadSettings()
//        {
//            // Load settings from EditorPrefs
//            string settingsJson = EditorPrefs.GetString("UltimateGitHub_Settings", "");
//            if (!string.IsNullOrEmpty(settingsJson))
//            {
//                JsonUtility.FromJsonOverwrite(settingsJson, settings);
//            }

//            // Load aliases
//            string aliasesJson = EditorPrefs.GetString("UltimateGitHub_Aliases", "");
//            if (!string.IsNullOrEmpty(aliasesJson))
//            {
//                var aliasesList = JsonUtility.FromJson<SerializableList<GitAlias>>(aliasesJson);
//                if (aliasesList != null && aliasesList.items != null)
//                {
//                    aliases = aliasesList.items;
//                }
//            }
//        }

//        // Serializable wrapper for lists (needed for JsonUtility)
//        [System.Serializable]
//        private class SerializableList<T>
//        {
//            public List<T> items;
//        }

//        // ========================================
//        // PLACEHOLDER METHODS FOR GIT OPERATIONS
//        // These are stub methods that will be implemented in UltimateGitHub_Operations.cs
//        // They are here to prevent compilation errors in the UI code
//        // The real implementations are in the Operations file
//        // ========================================

//        // These methods are implemented in UltimateGitHub_Operations.cs (partial class)
//        // Adding empty stubs here only if you get compilation errors
//        // If you don't get errors, you can remove this section

//        /*
//        // Uncomment these ONLY if you get compilation errors:
        
//        private void RefreshAll() { }
//        private void RefreshStatus() { }
//        private void RefreshFileChanges() { }
//        private void RefreshBranches() { }
//        private void RefreshRemotes() { }
//        private void RefreshHistory() { }
//        private void InitializeRepository() { }
//        private void InitializeQuickActions() { }
//        private bool IsGitRepository() { return false; }
//        private void StageFile(string path) { }
//        private void StageSelectedFiles() { }
//        private void StageAllFiles() { }
//        private void UnstageFile(string path) { }
//        private void UnstageSelectedFiles() { }
//        private void UnstageAllFiles() { }
//        private void DiscardFileChanges(string path) { }
//        private void DiscardAllChanges() { }
//        private void PerformCommit() { }
//        private void PushToRemote(string remoteName = "origin") { }
//        private void PullFromRemote(string remoteName = "origin") { }
//        private void FetchFromRemote(string remoteName = "origin") { }
//        private void SyncWithRemote() { }
//        private void SwitchBranch(string branchName) { }
//        private void CreateBranch(string branchName, bool switchTo) { }
//        private void DeleteBranch(string branchName) { }
//        private void CheckoutRemoteBranch(string branchName) { }
//        private void MergeBranch(string sourceBranch) { }
//        private void AbortMerge() { }
//        private void AddRemote(string name, string url) { }
//        private void RemoveRemote(string name) { }
//        private void PruneRemote(string remoteName) { }
//        private void PushToAllRemotes() { }
//        private void PushToMirror() { }
//        private void CheckoutCommit(string hash) { }
//        private void CherryPickCommit(string hash) { }
//        private void RevertCommit(string hash) { }
//        private void StashChanges() { }
//        private void PopStash() { }
//        private void RunGarbageCollection() { }
//        private void VerifyRepositoryIntegrity() { }
//        private void CleanRepository() { }
//        private void ResetToHead() { }
//        private void ApplyUserConfig() { }
//        private void ApplyGitAliases() { }
//        private void DownloadRepositoryZip() { }
//        private void CreateBackup() { }
//        private void ShowFileDiff(string filePath) { }
//        private string ExecuteGitCommand(string arguments, bool showOutput = true) { return ""; }
//        private void LogToTerminal(string message) { }
//        */

//        // ========================================
//        // END OF UltimateGitHub.cs (Main File)
//        // Continue with UltimateGitHub_Operations.cs for all Git operations
//        // Both files work together as a partial class
//        // ========================================
//    }
//    public class GitSafetySystem
//    {
//        public bool enableSafetyChecks = true;
//        public bool autoBackupBeforeMerge = true;
//        public bool autoBackupBeforePull = true;
//        public bool requireConfirmationForDestructive = true;
//        public bool preventMirrorInProject = true;
//        public int maxBackupsToKeep = 5;
//        public List<string> backupHistory = new List<string>();
//        public DateTime lastSafetyCheck = DateTime.Now;

//        // Critical paths that should NEVER be touched
//        public readonly string[] protectedPaths = new string[]
//        {
//            "Assets",
//            "ProjectSettings",
//            "Packages",
//            "UserSettings"
//        };

//        public bool ValidateOperation(string operation, string details)
//        {
//            UnityEngine.Debug.Log($"[Git Safety] Validating: {operation} - {details}");
//            lastSafetyCheck = DateTime.Now;
//            return true;
//        }
//    }
//}
