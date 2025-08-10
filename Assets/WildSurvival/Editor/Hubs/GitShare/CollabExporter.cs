using UnityEditor;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System;
using System.Linq;
using System.Collections.Generic;
using WildSurvival.Editor.Common;

namespace WildSurvival.Editor.Collab
{
    public static class CollabExporter
    {
        [MenuItem("WildSurvival/Tools (V3)/Logs/Export Collab Bundle (and Open)")]
        public static void Export()
        {
            string root = "Assets/WildSurvival/Logs";
            Directory.CreateDirectory(root);
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string zipPath = Path.Combine(root, $"CollabBundle_{ts}.zip");

            WriteEditorLogTail(root);

            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var za = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                void Add(string path, string entry)
                {
                    if (File.Exists(path))
                        za.CreateEntryFromFile(path, entry);
                }

                Add(Path.Combine(root, "EditorLogTail.txt"), "EditorLogTail.txt");

                foreach (var f in Directory.GetFiles(root, "Project_Snapshot_*.txt"))
                    Add(f, Path.GetFileName(f));
                foreach (var f in Directory.GetFiles(root, "Packages_*.json"))
                    Add(f, Path.GetFileName(f));
            }

            UnityEngine.Debug.Log($"[CollabExporter] Bundle exported: {zipPath.Replace("\\","/")}");
            EditorUtility.RevealInFinder(zipPath);
        }

        public static void WriteEditorLogTail(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var tailOut = Path.Combine(outDir, "EditorLogTail.txt");
            try
            {
                var editorLogPath = EditorLogUtil.GetEditorLogPath();
                if (string.IsNullOrEmpty(editorLogPath) || !File.Exists(editorLogPath))
                {
                    File.WriteAllText(tailOut, "Editor log not found.");
                    UnityEngine.Debug.Log("[CollabExporter] Wrote EditorLogTail.txt (not found)");
                    return;
                }

                var lines = File.ReadAllLines(editorLogPath);
                var tail = lines.Reverse()
                                .Take(1500)
                                .Reverse()
                                .Where(l => !l.Contains("~UnityDirMonSyncFile~") &&
                                            !l.Contains("BuiltInPackages/com.unity.modules.vr"))
                                .ToArray();
                File.WriteAllLines(tailOut, tail);
                UnityEngine.Debug.Log("[CollabExporter] Wrote EditorLogTail.txt");
            }
            catch (Exception ex)
            {
                File.WriteAllText(tailOut, "Failed to read editor log: " + ex.Message);
                UnityEngine.Debug.Log("[CollabExporter] Wrote EditorLogTail.txt (fallback)");
            }
        }
    }
}
