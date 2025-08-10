using UnityEditor;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System;
using System.Linq;

namespace WildSurvival.Editor.Collab
{
    public static class BugReportExporter
    {
        [MenuItem("WildSurvival/Tools (V2.1)/Export Bug Report Bundle")]
        public static void ExportBugBundle()
        {
            string outDir = "Assets/WildSurvival/Logs";
            Directory.CreateDirectory(outDir);

            // Ensure snapshot and log tail exist
            try { ProjectSnapshot.WriteSnapshot(); } catch {}

            string zipPath = Path.Combine(outDir, $"BugReport_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                void AddFile(string path, string entryName = null)
                {
                    if (!File.Exists(path)) return;
                    var name = entryName ?? Path.GetFileName(path);
                    var entry = zip.CreateEntry(name); // default compression
                    using (var from = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var to = entry.Open())
                        from.CopyTo(to);
                }

                // Include all non-zip files from Logs
                foreach (var f in Directory.GetFiles(outDir).Where(x => !x.EndsWith(".zip")))
                    AddFile(f, "Logs/" + Path.GetFileName(f));

                // Try to include WS_BuildInfo.json from the latest WindowsDev build
                var builds = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WindowsDev");
                if (Directory.Exists(builds))
                {
                    var latest = new DirectoryInfo(builds).GetDirectories()
                        .OrderByDescending(d => d.LastWriteTimeUtc).FirstOrDefault();
                    if (latest != null)
                    {
                        var buildInfo = Path.Combine(latest.FullName, "WS_BuildInfo.json");
                        AddFile(buildInfo, "Build/WS_BuildInfo.json");
                    }
                }

                // Attach full Editor.log if we can find it
                try
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#if UNITY_EDITOR_WIN
                    var p2 = Path.Combine(home, "AppData", "Local", "Unity", "Editor", "Editor.log");
#elif UNITY_EDITOR_OSX
                    var p2 = Path.Combine(home, "Library", "Logs", "Unity", "Editor.log");
#else
                    var p2 = Path.Combine(home, ".config", "unity3d", "Editor.log");
#endif
                    AddFile(p2, "Editor.log");
                }
                catch {}
            }

            var rel = zipPath.Replace("\\","/");
            if (rel.StartsWith("Assets/")) AssetDatabase.ImportAsset(rel); else AssetDatabase.Refresh();
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rel);
            if (obj) EditorGUIUtility.PingObject(obj);
            try { EditorUtility.RevealInFinder(Path.GetFullPath(zipPath)); } catch {}

            UnityEngine.Debug.Log($"[BugReportExporter] Bundle exported -> {zipPath}");
        }
    }
}
