using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace WildSurvival.Editor.Collab
{
    public static class LogsShortcuts
    {
        const string LogsRel = "Assets/WildSurvival/Logs";

        [MenuItem("WildSurvival/Tools (V2.1)/Open Logs Folder")]
        public static void OpenLogsFolder()
        {
            var abs = Path.GetFullPath(LogsRel);
            Directory.CreateDirectory(abs);
            EditorUtility.RevealInFinder(abs);
        }

        [MenuItem("WildSurvival/Tools (V2.1)/Open Latest Collaboration Bundle")]
        public static void OpenLatestBundle()
        {
            var abs = Path.GetFullPath(LogsRel);
            Directory.CreateDirectory(abs);
            var latest = new DirectoryInfo(abs).GetFiles("CollabBundle_*.zip")
                                               .OrderByDescending(f => f.LastWriteTimeUtc)
                                               .FirstOrDefault();
            if (latest == null)
            {
                EditorUtility.DisplayDialog("Open Latest Bundle", "No collaboration bundles found yet.", "OK");
                return;
            }
            EditorUtility.RevealInFinder(latest.FullName);
        }

        [MenuItem("WildSurvival/Tools (V2.1)/Quick Export & Open Bundle")]
        public static void QuickExportAndOpen()
        {
            CollabExporter.Export();
            OpenLatestBundle();
        }
    }
}
