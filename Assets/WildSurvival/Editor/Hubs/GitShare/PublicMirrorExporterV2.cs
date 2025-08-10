using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using WildSurvival.Editor.Git;

namespace WildSurvival.Editor.Git
{
    public class PublicMirrorExporterV2 : EditorWindow
    {
        private PublicMirrorSettingsV1 settings;

        [MenuItem("WildSurvival/Tools (V3)/Git/Mirror Exporter (V2)")]
        public static void Open()
        {
            var wnd = GetWindow<PublicMirrorExporterV2>("Public Mirror (V2)");
            wnd.minSize = new Vector2(480, 280);
            wnd.Show();
        }

        void OnEnable()
        {
            settings = PublicMirrorSettingsV1.LoadOrCreate();
        }

        void OnGUI()
        {
            if (settings == null) settings = PublicMirrorSettingsV1.LoadOrCreate();

            EditorGUILayout.LabelField("Public Git Mirror Exporter (V2)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Choose a mirror folder OUTSIDE your Unity project. We'll copy Assets/Packages/ProjectSettings (excluding Library/Temp/etc), optionally zip, and (optionally) init & push to a remote.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Mirror Folder (abs)");
            settings.mirrorFolderAbs = EditorGUILayout.TextField(settings.mirrorFolderAbs);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var sel = EditorUtility.OpenFolderPanel("Select Mirror Root Folder", settings.mirrorFolderAbs, "");
                if (!string.IsNullOrEmpty(sel)) settings.mirrorFolderAbs = sel;
            }
            EditorGUILayout.EndHorizontal();

            settings.remoteUrl = EditorGUILayout.TextField("Remote URL (optional)", settings.remoteUrl);
            settings.zipMirror = EditorGUILayout.Toggle("Also zip the mirror", settings.zipMirror);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(settings.mirrorFolderAbs)))
            {
                if (GUILayout.Button("Build Mirror Snapshot"))
                {
                    //BuildMirror(false);
                    EditorApplication.delayCall += () => BuildMirror(false);
                }
                if (GUILayout.Button("Build + Init Git + Push"))
                {
                    BuildMirror(true);
                }
            }

            if (string.IsNullOrEmpty(settings.mirrorFolderAbs))
                EditorGUILayout.HelpBox("Mirror Folder is not set. Choose a folder first.", MessageType.Warning);
        }

        private void BuildMirror(bool initAndPush)
        {
            try
            {
                if (string.IsNullOrEmpty(settings.mirrorFolderAbs))
                {
                    UnityEngine.Debug.LogWarning("[PublicMirror] Mirror folder is empty.");
                    return;
                }

                var projRoot = Path.GetFullPath(Application.dataPath + "/..");
                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var mirrorBatchRoot = Path.Combine(settings.mirrorFolderAbs, $"WildSurvival_PublicMirror_{ts}");

                if (Directory.Exists(mirrorBatchRoot))
                    Directory.Delete(mirrorBatchRoot, true);
                Directory.CreateDirectory(mirrorBatchRoot);

                // Copy includes
                foreach (var top in settings.includeTop)
                {
                    var src = Path.Combine(projRoot, top);
                    if (!Directory.Exists(src)) continue;
                    var dst = Path.Combine(mirrorBatchRoot, top);
                    CopyTree(src, dst, projRoot);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string zipPath = null;
                if (settings.zipMirror)
                {
                    //var zipName = $"PublicMirror_{ts}.zip";
                    //zipPath = Path.Combine(mirrorBatchRoot, zipName);
                    var zipName = $"PublicMirror_{DateTime.Now:yyyyMMdd_HHmmss}";
                    var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    var exportRoot = Path.Combine(projectRoot, "Builds", "PublicMirror");     // e.g., <project>/Builds/PublicMirror
                    Directory.CreateDirectory(exportRoot);
                    var stagingDir = Path.Combine(Path.GetTempPath(), $"WildSurvival_{zipName}"); // disposable staging
                    zipPath = Path.Combine(exportRoot, $"{zipName}.zip");
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    ZipFile.CreateFromDirectory(mirrorBatchRoot, zipPath, System.IO.Compression.CompressionLevel.Fastest, false);
                    UnityEngine.Debug.Log($"[PublicMirror] Wrote {zipPath}");
                }

                if (initAndPush)
                {
                    var res = GitUtilsV2.Run("init", mirrorBatchRoot);
                    if (res.code != 0) UnityEngine.Debug.LogWarning("[PublicMirror] git init: " + res.stderr);
                    GitUtilsV2.Run("add -A", mirrorBatchRoot);
                    GitUtilsV2.Run("commit -m \"Mirror snapshot " + ts + "\"", mirrorBatchRoot);
                    if (!string.IsNullOrEmpty(settings.remoteUrl))
                    {
                        GitUtilsV2.Run("remote remove origin", mirrorBatchRoot); // ignore failure
                        var add = GitUtilsV2.Run($"remote add origin \"{settings.remoteUrl}\"", mirrorBatchRoot);
                        if (add.code != 0) UnityEngine.Debug.LogWarning("[PublicMirror] git remote add: " + add.stderr);
                        var push = GitUtilsV2.Run("push -u origin HEAD:main", mirrorBatchRoot);
                        if (push.code != 0)
                        {
                            // Try master fallback
                            push = GitUtilsV2.Run("push -u origin HEAD:master", mirrorBatchRoot);
                            if (push.code != 0) UnityEngine.Debug.LogWarning("[PublicMirror] git push: " + push.stderr);
                        }
                    }
                }

                UnityEngine.Debug.Log($"[PublicMirror] Mirror built at: {mirrorBatchRoot}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("[PublicMirror] Build failed: " + ex.Message);
            }
        }

        private void CopyTree(string src, string dst, string projRoot)
        {
            var normalizedEx = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in (PublicMirrorSettingsV1.LoadOrCreate().extraProjectExcludes ?? new string[0]))
            {
                var full = Path.GetFullPath(Path.Combine(projRoot, e.Replace("/", Path.DirectorySeparatorChar.ToString())));
                normalizedEx.Add(full);
            }

            foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(dir);
                if (IsExcludedDir(name)) continue;
                var full = Path.GetFullPath(dir);
                // Exclude explicit project-relative dirs
                bool skip = false;
                foreach (var exFull in normalizedEx)
                {
                    if (full.StartsWith(exFull, StringComparison.OrdinalIgnoreCase)) { skip = true; break; }
                }
                if (skip) continue;

                var rel = full.Substring(src.Length).TrimStart(Path.DirectorySeparatorChar);
                var target = Path.Combine(dst, rel);
                Directory.CreateDirectory(target);
            }

            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                var dirName = Path.GetFileName(Path.GetDirectoryName(file));
                if (IsExcludedDir(dirName)) continue;
                var full = Path.GetFullPath(file);
                bool skip = false;
                foreach (var exFull in normalizedEx)
                {
                    if (full.StartsWith(exFull, StringComparison.OrdinalIgnoreCase)) { skip = true; break; }
                }
                if (skip) continue;

                var rel = full.Substring(src.Length).TrimStart(Path.DirectorySeparatorChar);
                var target = Path.Combine(dst, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target) ?? dst);
                File.Copy(full, target, true);
            }
        }

        private bool IsExcludedDir(string name)
        {
            foreach (var ex in (settings.excludeDirs ?? new string[0]))
                if (string.Equals(ex, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
