
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace WildSurvival.Editor.Collab
{
    public static class LogsHousekeeping
    {
        [MenuItem("WildSurvival/Tools (V2.1)/Logs: Prune Old Bundles (keep 10)")]
        public static void Prune()
        {
            var dir = "Assets/WildSurvival/Logs";
            if (!Directory.Exists(dir)) { UnityEngine.Debug.Log("[LogsHousekeeping] No logs directory."); return; }
            var zips = new DirectoryInfo(dir).GetFiles("*.zip").OrderByDescending(f => f.LastWriteTimeUtc).ToList();
            for (int i = 10; i < zips.Count; i++)
            {
                try { zips[i].Delete(); } catch {}
            }
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[LogsHousekeeping] Pruned old bundles, kept 10 most recent.");
        }
    }
}
