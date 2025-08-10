using UnityEditor;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;

namespace WildSurvival.Editor.Collab
{
    public static class FullProjectExporter
    {
        [MenuItem("WildSurvival/Tools (V2)/Export Full Code Bundle (Safe)")]
        public static void Export()
        {
            string projectRoot = Directory.GetCurrentDirectory().Replace("\\","/");
            string exportsDir = Path.Combine(projectRoot, "ProjectExports");
            Directory.CreateDirectory(exportsDir);

            var ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outZip = Path.Combine(exportsDir, $"FullCode_{ts}.zip");

            // Collect the files BEFORE creating the zip to avoid self-inclusion/sharing violations
            var includes = new List<string>
            {
                "Assets/WildSurvival",
                "Assets/Scenes",
                "Packages/manifest.json",
                "ProjectSettings/ProjectSettings.asset"
            };

            var files = new List<string>();
            foreach (var inc in includes)
            {
                if (File.Exists(inc)) files.Add(inc.Replace("\\","/"));
                else if (Directory.Exists(inc))
                {
                    files.AddRange(Directory.GetFiles(inc, "*", SearchOption.AllDirectories)
                        .Where(f => !f.Contains("/Library/") && !f.Contains("/Temp/") && !f.Contains("/Logs/"))
                        .Select(f => f.Replace("\\","/")));
                }
            }

            // Create zip and add files
            if (File.Exists(outZip)) File.Delete(outZip);
            using (var z = ZipFile.Open(outZip, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    // entry name always relative to project root
                    string rel = file.Replace(projectRoot + "/", "");
                    z.CreateEntryFromFile(file, rel);
                }
            }

            EditorUtility.RevealInFinder(outZip);
            UnityEngine.Debug.Log($"[FullProjectExporter] Wrote {outZip}");
        }

        [MenuItem("WildSurvival/Tools (V2)/Open/Exports Folder")]
        public static void OpenExports()
        {
            string exportsDir = Path.Combine(Directory.GetCurrentDirectory(), "ProjectExports");
            Directory.CreateDirectory(exportsDir);
            EditorUtility.RevealInFinder(exportsDir);
        }

        [MenuItem("WildSurvival/Tools (V2)/Open/Logs Folder")]
        public static void OpenLogs()
        {
            string logs = Path.Combine(Directory.GetCurrentDirectory(), "Assets/WildSurvival/Logs");
            Directory.CreateDirectory(logs);
            EditorUtility.RevealInFinder(logs);
        }
    }
}
