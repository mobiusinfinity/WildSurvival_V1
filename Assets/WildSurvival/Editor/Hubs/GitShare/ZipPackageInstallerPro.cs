using UnityEditor;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WildSurvival.Editor.Collab
{
    public class ZipPackageInstallerPro : EditorWindow
    {
        [MenuItem("WildSurvival/Tools (V2)/Import/Importer Pro (Zip)…")]
        public static void Open() => GetWindow<ZipPackageInstallerPro>("Importer Pro (Zip)");

        const string DefaultImportsRoot = "Assets/WildSurvival/Imports";
        const string BackupRoot = "Assets/WildSurvival/Backups";

        string _zipPath;
        string _importsRoot = DefaultImportsRoot;
        string _packageFolder = ""; // auto from zip
        bool _analyzed;
        bool _allowInPlaceOverwrite = false;        // off by default
        bool _backupExisting = true;               // on by default
        bool _stripCodeMetasOnCollision = true;    // safer for code packs
        bool _abortOnNonCodeGuidCollision = true;  // never break prefabs/materials
        bool _dryRun = true;
        Vector2 _scroll;

        List<EntryPlan> _plan = new List<EntryPlan>();
        List<string> _warnings = new List<string>();
        List<string> _errors = new List<string>();
        HashSet<string> _existingAsmNames;

        class EntryPlan
        {
            public string entryPath;   // path inside zip
            public string outPath;     // final path in project
            public bool willOverwrite;
            public bool isMeta;
            public string guid;        // parsed from meta, if any
            public bool guidCollision;
            public string guidCollisionPath;
            public bool isCode;        // .cs / .asmdef / .asmref
            public bool isAsmdef;
            public string asmdefName;
        }

        void OnEnable()
        {
            Directory.CreateDirectory(_importsRoot);
            Directory.CreateDirectory(BackupRoot);
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Robust zip importer with conflict detection, backups, and drag & drop. Default mode imports into a unique folder to avoid overwrites.", MessageType.Info);

            DrawZipPicker();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_zipPath)))
            {
                DrawTargets();
                DrawOptions();

                if (GUILayout.Button(_analyzed ? "Re-Analyze" : "Analyze"))
                    Analyze();

                EditorGUILayout.Space();
                DrawReport();

                using (new EditorGUI.DisabledScope(!_analyzed))
                {
                    if (GUILayout.Button(_dryRun ? "Run (Dry)" : "Import Now"))
                    {
                        if (_dryRun) UnityEngine.Debug.Log("[ImporterPro] Dry run only; no files written.");
                        else Import();
                    }
                }
            }
        }

        void DrawZipPicker()
        {
            GUILayout.Label("Zip File", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField(_zipPath ?? "");
                if (GUILayout.Button("Browse…", GUILayout.Width(100)))
                {
                    var p = EditorUtility.OpenFilePanel("Select zip to import", "", "zip");
                    if (!string.IsNullOrEmpty(p)) { _zipPath = p; _analyzed = false; _plan.Clear(); }
                }
            }

            // Drag & drop support
            var e = Event.current;
            var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drag & Drop .zip here");
            if ((e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && dropRect.Contains(e.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var path in DragAndDrop.paths)
                    {
                        if (path.ToLower().EndsWith(".zip"))
                        {
                            _zipPath = path;
                            _analyzed = false; _plan.Clear();
                            break;
                        }
                    }
                }
                e.Use();
            }
        }

        void DrawTargets()
        {
            GUILayout.Label("Targets", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _importsRoot = EditorGUILayout.TextField("Imports Root", _importsRoot);
                if (GUILayout.Button("Default", GUILayout.Width(80))) _importsRoot = DefaultImportsRoot;
                if (GUILayout.Button("Open", GUILayout.Width(60))) EditorUtility.RevealInFinder(Path.GetFullPath(_importsRoot));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _packageFolder = EditorGUILayout.TextField("Package Subfolder", _packageFolder);
                if (GUILayout.Button("From Zip Name", GUILayout.Width(120)))
                {
                    _packageFolder = MakeSafeFolderName(Path.GetFileNameWithoutExtension(_zipPath)) + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }
            }
        }

        void DrawOptions()
        {
            GUILayout.Label("Options", EditorStyles.boldLabel);
            _dryRun = EditorGUILayout.ToggleLeft("Dry run (show plan only)", _dryRun);
            _allowInPlaceOverwrite = EditorGUILayout.ToggleLeft("Allow in-place overwrite (NOT recommended)", _allowInPlaceOverwrite);
            _backupExisting = EditorGUILayout.ToggleLeft("Backup existing files", _backupExisting);
            _stripCodeMetasOnCollision = EditorGUILayout.ToggleLeft("Strip .meta for code files on GUID collision (.cs/.asmdef/.asmref)", _stripCodeMetasOnCollision);
            _abortOnNonCodeGuidCollision = EditorGUILayout.ToggleLeft("Abort if non-code .meta GUID collides (prefab/material)", _abortOnNonCodeGuidCollision);
        }

        void DrawReport()
        {
            if (!_analyzed) return;

            if (_errors.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", _errors.ToArray()), MessageType.Error);

            if (_warnings.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", _warnings.ToArray()), MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(240));
            foreach (var p in _plan)
            {
                string s = $"{p.entryPath}  =>  {p.outPath}" + (p.willOverwrite ? "  (overwrite)" : "");
                if (p.guidCollision) s += $"  [GUID COLLISION with {p.guidCollisionPath}]";
                if (p.isAsmdef && !string.IsNullOrEmpty(p.asmdefName) && _existingAsmNames.Contains(p.asmdefName))
                    s += $"  [ASMDEF NAME CONFLICT: {p.asmdefName}]";
                GUILayout.Label(s);
            }
            EditorGUILayout.EndScrollView();
        }

        void Analyze()
        {
            _errors.Clear(); _warnings.Clear(); _plan.Clear();
            if (string.IsNullOrEmpty(_zipPath) || !File.Exists(_zipPath)) { _errors.Add("Zip not found."); return; }

            if (string.IsNullOrEmpty(_packageFolder))
                _packageFolder = MakeSafeFolderName(Path.GetFileNameWithoutExtension(_zipPath)) + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var outRoot = Path.Combine(_importsRoot, _packageFolder).Replace("\\","/");

            // Collect existing asmdef names
            _existingAsmNames = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:AssemblyDefinitionAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var text = File.ReadAllText(path);
                var name = ExtractAsmdefName(text);
                if (!string.IsNullOrEmpty(name)) _existingAsmNames.Add(name);
            }

            using (var zip = ZipFile.OpenRead(_zipPath))
            {
                foreach (var e in zip.Entries)
                {
                    if (string.IsNullOrEmpty(e.Name)) continue; // folder
                    string entryPath = e.FullName.Replace("\\","/");
                    // Strip leading root folder in zips like MyPack/Assets/...
                    if (entryPath.StartsWith("Assets/")) entryPath = entryPath.Substring("Assets/".Length);
                    if (entryPath.StartsWith("/")) entryPath = entryPath.Substring(1);

                    var outPath = Path.Combine(outRoot, entryPath).Replace("\\","/");
                    var isMeta = entryPath.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase);
                    var isCode = entryPath.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase) ||
                                 entryPath.EndsWith(".asmdef", System.StringComparison.OrdinalIgnoreCase) ||
                                 entryPath.EndsWith(".asmref", System.StringComparison.OrdinalIgnoreCase);
                    var isAsmdef = entryPath.EndsWith(".asmdef", System.StringComparison.OrdinalIgnoreCase);

                    var plan = new EntryPlan
                    {
                        entryPath = e.FullName,
                        outPath = outPath,
                        willOverwrite = File.Exists(outPath),
                        isMeta = isMeta,
                        isCode = isCode,
                        isAsmdef = isAsmdef
                    };

                    if (isAsmdef)
                    {
                        try
                        {
                            using (var sr = new StreamReader(e.Open()))
                            {
                                var txt = sr.ReadToEnd();
                                plan.asmdefName = ExtractAsmdefName(txt);
                            }
                        }
                        catch {}
                    }

                    if (isMeta)
                    {
                        string guid = null;
                        try
                        {
                            using (var sr = new StreamReader(e.Open()))
                            {
                                var txt = sr.ReadToEnd();
                                guid = ExtractGuidFromMeta(txt);
                            }
                        } catch {}
                        plan.guid = guid;
                        if (!string.IsNullOrEmpty(guid))
                        {
                            var existingPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (!string.IsNullOrEmpty(existingPath))
                            {
                                plan.guidCollision = true;
                                plan.guidCollisionPath = existingPath;
                            }
                        }
                    }

                    _plan.Add(plan);
                }
            }

            // Summaries
            int overwrites = _plan.Count(p => p.willOverwrite);
            if (!_allowInPlaceOverwrite && overwrites > 0)
            {
                _warnings.Add($"This import would overwrite {overwrites} file(s). Default mode imports to a unique folder under '{_importsRoot}'. To overwrite in place, enable 'Allow in-place overwrite' (not recommended).");
            }

            int metaCollisions = _plan.Count(p => p.isMeta && p.guidCollision);
            if (metaCollisions > 0)
            {
                _warnings.Add($"{metaCollisions} .meta GUID collision(s) detected.");
                if (_abortOnNonCodeGuidCollision)
                    _warnings.Add("Importer will ABORT if any non-code asset has a GUID collision.");
                if (_stripCodeMetasOnCollision)
                    _warnings.Add("Importer will strip .meta for code files that collide to let Unity generate new GUIDs safely.");
            }

            int asmConflicts = _plan.Count(p => p.isAsmdef && !string.IsNullOrEmpty(p.asmdefName) && _existingAsmNames.Contains(p.asmdefName));
            if (asmConflicts > 0)
            {
                _warnings.Add($"{asmConflicts} assembly definition(s) have the same 'name' as existing assemblies. This can break compilation. Consider renaming inside the zip or importing to a new folder and adjusting.");
            }

            _analyzed = true;
        }

        void Import()
        {
            if (!_analyzed) { Analyze(); if (!_analyzed) return; }

            var outRoot = Path.Combine(_importsRoot, _packageFolder).Replace("\\","/");
            Directory.CreateDirectory(outRoot);

            // Abort on non-code meta GUID collisions
            if (_abortOnNonCodeGuidCollision)
            {
                foreach (var p in _plan)
                {
                    if (p.isMeta && p.guidCollision && !p.isCode)
                    {
                        EditorUtility.DisplayDialog("Importer Pro", $"Aborting: non-code asset meta GUID collision for\n{p.entryPath}\nExisting: {p.guidCollisionPath}", "OK");
                        return;
                    }
                }
            }

            // Write files
            using (var zip = ZipFile.OpenRead(_zipPath))
            {
                foreach (var p in _plan)
                {
                    var entry = zip.GetEntry(p.entryPath);
                    if (entry == null) continue;

                    // Skip .meta for code files if collision or if we generally prefer fresh metas
                    if (p.isMeta && p.isCode && _stripCodeMetasOnCollision && p.guidCollision)
                        continue;

                    // Overwrite logic
                    if (File.Exists(p.outPath))
                    {
                        if (!_allowInPlaceOverwrite)
                        {
                            // rename duplicate inside the target folder
                            p.outPath = UniquePath(p.outPath);
                        }
                        else if (_backupExisting)
                        {
                            // Backup original file
                            var backupDir = Path.Combine(BackupRoot, System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                            var rel = p.outPath.Replace("\\","/");
                            var idx = rel.IndexOf("Assets/");
                            var sub = idx >= 0 ? rel.Substring(idx + "Assets/".Length) : Path.GetFileName(rel);
                            var bkPath = Path.Combine(backupDir, sub);
                            Directory.CreateDirectory(Path.GetDirectoryName(bkPath));
                            File.Copy(p.outPath, bkPath, true);
                        }
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(p.outPath));
                    entry.ExtractToFile(p.outPath, true);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            EditorUtility.RevealInFinder(Path.GetFullPath(outRoot));
            UnityEngine.Debug.Log($"[ImporterPro] Import complete -> {outRoot}");
        }

        static string MakeSafeFolderName(string s)
        {
            var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (var c in invalid) s = s.Replace(c.ToString(), "_");
            return Regex.Replace(s, @"\s+", "_");
        }

        static string ExtractGuidFromMeta(string txt)
        {
            // typical: guid: a1b2c3d4e5f6g7h8i9j0k...
            var m = Regex.Match(txt, @"guid:\s*([a-fA-F0-9]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }

        static string ExtractAsmdefName(string json)
        {
            var m = Regex.Match(json, @"""name""\s*:\s*""([^""]+)""");
            return m.Success ? m.Groups[1].Value : null;
        }

        static string UniquePath(string path)
        {
            if (!File.Exists(path)) return path;
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            int i = 1;
            string candidate;
            do {
                candidate = Path.Combine(dir, $"{name}_{i}{ext}");
                i++;
            } while (File.Exists(candidate));
            return candidate;
        }
    }
}
