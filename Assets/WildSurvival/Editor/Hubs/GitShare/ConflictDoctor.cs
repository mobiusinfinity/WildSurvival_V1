using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace WildSurvival.Editor.Collab
{
    public class ConflictDoctor : EditorWindow
    {
        [MenuItem("WildSurvival/Tools (V2)/Maintenance/Conflict Doctor")]
        public static void Open() => GetWindow<ConflictDoctor>("Conflict Doctor");

        class MetaInfo { public string metaPath; public string assetPath; public string guid; public bool isCode; public bool isNonCode; }
        Vector2 _scroll;
        List<MetaInfo> _dupeGuid = new List<MetaInfo>();
        List<string> _orphanMetas = new List<string>();
        List<string> _missingMetas = new List<string>();
        Dictionary<string, List<string>> _asmdefConflicts = new Dictionary<string, List<string>>();
        bool _dryRun = true;
        string _quarantine = "Assets/WildSurvival/Quarantine_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        bool _scanDone;

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Scans the project for GUID/.meta conflicts, orphaned metas, missing metas, and duplicate asmdef names. Offers safe, one-click fixes.\nTip: Run this before importing updates.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(_scanDone ? "Re-Scan Project" : "Scan Project", GUILayout.Height(26))) Scan();
                _dryRun = GUILayout.Toggle(_dryRun, "Dry Run", GUILayout.Width(120));
                if (GUILayout.Button("Open Quarantine", GUILayout.Width(140))) { Directory.CreateDirectory(_quarantine); EditorUtility.RevealInFinder(Path.GetFullPath(_quarantine)); }
            }

            GUILayout.Space(6);
            if (!_scanDone) return;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawDupeGuid();
            DrawOrphanMetas();
            DrawMissingMetas();
            DrawAsmdefConflicts();

            EditorGUILayout.EndScrollView();
        }

        void DrawDupeGuid()
        {
            GUILayout.Label("Duplicate .meta GUIDs", EditorStyles.boldLabel);
            if (_dupeGuid.Count == 0) { GUILayout.Label("✓ None"); return; }

            int nonCode = _dupeGuid.Count(mi => mi.isNonCode);
            int code = _dupeGuid.Count(mi => mi.isCode);

            if (nonCode > 0)
                EditorGUILayout.HelpBox($"Found {nonCode} non-code asset(s) sharing GUIDs. Fix will MOVE duplicate assets to Quarantine to avoid broken references.", MessageType.Warning);
            if (code > 0)
                EditorGUILayout.HelpBox($"Found {code} code file(s) sharing GUIDs. Fix will DELETE their .meta so Unity regenerates GUIDs safely.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_dryRun))
                {
                    if (GUILayout.Button("Fix All Duplicates (Safe)"))
                    {
                        if (EditorUtility.DisplayDialog("Fix Duplicates", "Proceed with safe fixes?\n- Non-code assets → moved to Quarantine\n- Code files → .meta deleted", "Proceed", "Cancel"))
                            FixDupeGuid();
                    }
                }
                if (GUILayout.Button("Select All")) SelectAssets(_dupeGuid.Select(d => d.assetPath).ToArray());
            }

            foreach (var g in _dupeGuid.GroupBy(m => m.guid))
            {
                GUILayout.Label($"GUID {g.Key}");
                foreach (var mi in g)
                    GUILayout.Label($"  {(mi.isCode?"[CODE]":"[ASSET]")} {mi.assetPath}  (meta: {mi.metaPath})");
            }
        }

        void DrawOrphanMetas()
        {
            GUILayout.Space(8);
            GUILayout.Label("Orphan .meta files", EditorStyles.boldLabel);
            if (_orphanMetas.Count == 0) { GUILayout.Label("✓ None"); return; }

            EditorGUILayout.HelpBox("Fix will move these orphan .meta files to Quarantine (or delete if you prefer).", MessageType.Info);
            using (new EditorGUI.DisabledScope(_dryRun))
            {
                if (GUILayout.Button("Move Orphan .meta to Quarantine"))
                {
                    foreach (var meta in _orphanMetas.ToArray())
                        MoveMetaToQuarantine(meta);
                    AssetDatabase.Refresh();
                    Scan();
                }
            }
            if (GUILayout.Button("Select All Orphan .meta")) SelectAssets(_orphanMetas.ToArray());
        }

        void DrawMissingMetas()
        {
            GUILayout.Space(8);
            GUILayout.Label("Assets missing .meta", EditorStyles.boldLabel);
            if (_missingMetas.Count == 0) { GUILayout.Label("✓ None"); return; }

            EditorGUILayout.HelpBox("Fix will force reimport on these assets so Unity generates .meta files.", MessageType.Info);
            using (new EditorGUI.DisabledScope(_dryRun))
            {
                if (GUILayout.Button("Force Reimport Missing .meta"))
                {
                    foreach (var asset in _missingMetas.ToArray())
                        AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.Refresh();
                    Scan();
                }
            }
            if (GUILayout.Button("Select All Missing")) SelectAssets(_missingMetas.ToArray());
        }

        void DrawAsmdefConflicts()
        {
            GUILayout.Space(8);
            GUILayout.Label("Assembly Definition name conflicts", EditorStyles.boldLabel);
            if (_asmdefConflicts.Count == 0) { GUILayout.Label("✓ None"); return; }

            EditorGUILayout.HelpBox("Two or more .asmdef files use the same 'name'. This can break compilation.\nFix will append a suffix to selected asmdef 'name' fields and update references in .asmref/.asmdef.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(_dryRun))
            {
                if (GUILayout.Button("Auto-Fix All (append _WS)"))
                {
                    foreach (var kv in _asmdefConflicts)
                    {
                        var name = kv.Key;
                        var newName = name + "_WS";
                        RenameAsmdefName(name, newName);
                    }
                    AssetDatabase.Refresh();
                    Scan();
                }
            }

            foreach (var kv in _asmdefConflicts)
            {
                GUILayout.Label($"Name: {kv.Key}");
                foreach (var file in kv.Value)
                    GUILayout.Label($"  {file}");
            }
        }

        void Scan()
        {
            _dupeGuid.Clear(); _orphanMetas.Clear(); _missingMetas.Clear(); _asmdefConflicts.Clear();
            var project = Directory.GetCurrentDirectory().Replace("\\","/");
            var assetsRoot = Path.Combine(project, "Assets").Replace("\\","/");

            var metas = new Dictionary<string, string>(); // meta path -> guid
            var guidToMetas = new Dictionary<string, List<string>>();

            foreach (var meta in Directory.GetFiles(assetsRoot, "*.meta", SearchOption.AllDirectories))
            {
                var rel = meta.Replace("\\","/").Replace(project + "/", "");
                if (rel.Contains("/Library/") || rel.Contains("/Temp/")) continue;
                var guid = ExtractGuidFromMeta(File.ReadAllText(meta));
                metas[rel] = guid ?? "";
                if (!string.IsNullOrEmpty(guid))
                {
                    if (!guidToMetas.ContainsKey(guid)) guidToMetas[guid] = new List<string>();
                    guidToMetas[guid].Add(rel);
                }
            }

            // Orphans
            foreach (var kv in metas)
            {
                var meta = kv.Key;
                var asset = meta.Substring(0, meta.Length - 5);
                if (!File.Exists(Path.Combine(project, asset)))
                    _orphanMetas.Add(meta);
            }

            // Missing metas
            foreach (var file in Directory.GetFiles(assetsRoot, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta")) continue;
                var rel = file.Replace("\\","/").Replace(project + "/", "");
                if (rel.Contains("/Library/") || rel.Contains("/Temp/") || rel.Contains("/Logs/")) continue;
                var meta = file + ".meta";
                if (!File.Exists(meta)) _missingMetas.Add(rel);
            }

            // Duplicate GUIDs
            foreach (var kv in guidToMetas.Where(kv => kv.Value.Count > 1))
            {
                var guid = kv.Key;
                foreach (var meta in kv.Value)
                {
                    var asset = meta.Substring(0, meta.Length - 5);
                    var code = IsCode(asset);
                    _dupeGuid.Add(new MetaInfo {
                        metaPath = meta, assetPath = asset, guid = guid,
                        isCode = code, isNonCode = !code
                    });
                }
            }

            // Asmdef conflicts
            var nameToFiles = new Dictionary<string, List<string>>();
            foreach (var asmdef in Directory.GetFiles(assetsRoot, "*.asmdef", SearchOption.AllDirectories))
            {
                var rel = asmdef.Replace("\\","/").Replace(project + "/", "");
                var json = File.ReadAllText(asmdef);
                var name = ExtractAsmdefName(json);
                if (string.IsNullOrEmpty(name)) continue;
                if (!nameToFiles.ContainsKey(name)) nameToFiles[name] = new List<string>();
                nameToFiles[name].Add(rel);
            }
            foreach (var kv in nameToFiles)
                if (kv.Value.Count > 1) _asmdefConflicts[kv.Key] = kv.Value;

            _scanDone = true;
        }

        void FixDupeGuid()
        {
            Directory.CreateDirectory(_quarantine);
            foreach (var group in _dupeGuid.GroupBy(d => d.guid))
            {
                // Keep the first encountered asset as the "winner"
                var first = group.FirstOrDefault();
                foreach (var item in group)
                {
                    if (item == first) continue;
                    if (item.isNonCode)
                    {
                        // Move the whole asset (not the meta) to quarantine
                        var res = AssetDatabase.MoveAsset(item.assetPath, ToQuarantine(item.assetPath));
                        if (!string.IsNullOrEmpty(res)) UnityEngine.Debug.LogWarning($"[ConflictDoctor] Move failed: {res}");
                    }
                    else if (item.isCode)
                    {
                        // Delete meta so Unity regenerates it
                        var metaAbs = Path.Combine(Directory.GetCurrentDirectory(), item.metaPath);
                        try { File.Delete(metaAbs); } catch {}
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        string ToQuarantine(string assetPath)
        {
            var fileName = Path.GetFileName(assetPath);
            var target = $"{_quarantine}/{fileName}";
            target = AssetDatabase.GenerateUniqueAssetPath(target);
            return target;
        }

        void MoveMetaToQuarantine(string metaRel)
        {
            var abs = Path.Combine(Directory.GetCurrentDirectory(), metaRel);
            var tgt = Path.Combine(Directory.GetCurrentDirectory(), _quarantine, Path.GetFileName(metaRel));
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), _quarantine));
            try { File.Move(abs, tgt); } catch {}
        }

        bool IsCode(string assetRel)
        {
            return assetRel.EndsWith(".cs") || assetRel.EndsWith(".asmdef") || assetRel.EndsWith(".asmref");
        }

        static string ExtractGuidFromMeta(string txt)
        {
            var m = Regex.Match(txt ?? "", @"guid:\s*([a-fA-F0-9]{32})");
            return m.Success ? m.Groups[1].Value.ToLower() : null;
        }

        static string ExtractAsmdefName(string json)
        {
            var m = Regex.Match(json ?? "", @"""name""\s*:\s*""([^""]+)""");
            return m.Success ? m.Groups[1].Value : null;
        }

        void RenameAsmdefName(string oldName, string newName)
        {
            var project = Directory.GetCurrentDirectory().Replace("\\","/");
            var assetsRoot = Path.Combine(project, "Assets").Replace("\\","/");

            // Update .asmdef name
            foreach (var asmdef in Directory.GetFiles(assetsRoot, "*.asmdef", SearchOption.AllDirectories))
            {
                var txt = File.ReadAllText(asmdef);
                var name = ExtractAsmdefName(txt);
                if (name == oldName)
                {
                    var txtNew = Regex.Replace(txt, @"""name""\s*:\s*""[^""]+""", "\"name\": \"" + newName + "\"");
                    File.WriteAllText(asmdef, txtNew);
                }
            }

            // Update references in .asmref and .asmdef "references" arrays
            foreach (var file in Directory.GetFiles(assetsRoot, "*.*", SearchOption.AllDirectories))
            {
                if (!file.EndsWith(".asmref") && !file.EndsWith(".asmdef")) continue;
                var txt = File.ReadAllText(file);
                txt = txt.Replace("\"" + oldName + "\"", "\"" + newName + "\"");
                File.WriteAllText(file, txt);
            }
        }

        void SelectAssets(string[] rels)
        {
            var list = new List<Object>();
            foreach (var rel in rels)
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(rel);
                if (obj) list.Add(obj);
            }
            Selection.objects = list.ToArray();
        }
    }
}
