using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WildSurvival.Editor.Collab
{
    public static class EditorActions
    {
        static string LogDir => "Assets/WildSurvival/Logs";
        static string JsonlPath => Path.Combine(LogDir, $"EditorActions_{DateTime.Now:yyyyMMdd}.jsonl");
        static string CsvIndexPath => Path.Combine(LogDir, "EditorActions_Index.csv");

        [Serializable]
        class Entry { public string time; public string action; public string status; public string details; public string user; public string project; }

        public static void Log(string action, string status = "OK", string details = null)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                var e = new Entry {
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action = action,
                    status = status,
                    details = details ?? "",
                    user = Environment.UserName,
                    project = Application.productName
                };
                var json = JsonUtility.ToJson(e);
                File.AppendAllText(JsonlPath, json + "\n");

                var header = "time,action,status,details,user,project\n";
                bool writeHeader = !File.Exists(CsvIndexPath);
                using (var w = new StreamWriter(CsvIndexPath, true))
                {
                    if (writeHeader) w.Write(header);
                    w.WriteLine($"{e.time},{Escape(e.action)},{Escape(e.status)},{Escape(e.details)},{Escape(e.user)},{Escape(e.project)}");
                }
                AssetDatabase.ImportAsset(JsonlPath);
                AssetDatabase.ImportAsset(CsvIndexPath);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[EditorActions] Failed to log action '{action}': {ex.Message}");
            }
        }

        static string Escape(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\"","'").Replace(",",";");
    }
}
