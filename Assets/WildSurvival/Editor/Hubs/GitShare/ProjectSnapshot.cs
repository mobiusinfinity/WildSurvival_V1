
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Text;
using System;
using System.Linq;

#if UNITY_2019_1_OR_NEWER
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

namespace WildSurvival.Editor.Collab
{
    public static class ProjectSnapshot
    {
        const string OutDir = "Assets/WildSurvival/Logs";

        [MenuItem("WildSurvival/Tools (V2.1)/Write Project Snapshot")]
        public static void WriteSnapshot()
        {
            Directory.CreateDirectory(OutDir);

            // Basic info
            var sb = new StringBuilder();
            sb.AppendLine($"WildSurvival Project Snapshot - {DateTime.Now}");
            sb.AppendLine($"Unity: {Application.unityVersion}");
            var rp = GraphicsSettings.currentRenderPipeline;
            var rpName = rp ? rp.name : "(no SRP)";
            var rpType = rp ? rp.GetType().FullName : "Built-in";
            sb.AppendLine($"SRP Asset: {rpName} ({rpType})");

            // Build scenes
            sb.AppendLine("\nBuild Scenes:");
            foreach (var s in EditorBuildSettings.scenes)
                sb.AppendLine($"- {(s.enabled ? "[x]" : "[ ]")} {s.path}");

            // Player prefab path (if stored by Player Prefab Doctor)
            var playerPath = EditorPrefs.GetString("WS_PlayerPrefabDoctorV2_Path", "(unset)");
            sb.AppendLine($"\nPlayer Prefab Path: {playerPath}");

            // GameConfig presence
            var cfg = AssetDatabase.FindAssets("t:SurvivalCore.Config.GameConfig").Length > 0 ? "YES" : "NO";
            sb.AppendLine($"GameConfig present: {cfg}");

            // Input Actions presence
#if ENABLE_INPUT_SYSTEM
            var iaa = AssetDatabase.FindAssets("t:UnityEngine.InputSystem.InputActionAsset PlayerControls").Length > 0 ? "YES" : "NO";
            sb.AppendLine($"PlayerControls.inputactions present: {iaa}");
#else
            sb.AppendLine("Input System: NOT INSTALLED");
#endif

            // Write snapshot
            var snapPath = Path.Combine(OutDir, $"Project_Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(snapPath, sb.ToString());
            AssetDatabase.ImportAsset(snapPath.Replace("\\","/"));
            UnityEngine.Debug.Log($"[ProjectSnapshot] Wrote {snapPath}");

            // Packages snapshot (best-effort)
#if UNITY_2019_1_OR_NEWER
            try
            {
                var pkgPath = Path.Combine(OutDir, $"Packages_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                var list = Client.List(true, true);
                double t0 = EditorApplication.timeSinceStartup;
                EditorApplication.update += Poll;
                void Poll()
                {
                    if (!list.IsCompleted)
                    {
                        if (EditorApplication.timeSinceStartup - t0 > 10) { EditorApplication.update -= Poll; }
                        return;
                    }
                    EditorApplication.update -= Poll;
                    try
                    {
                        var arr = list.Result.Select(p => new Item { name = p.name, version = p.version, source = p.source.ToString() }).ToArray();
                        var wrap = new Wrapper { items = arr };
                        var json = JsonUtility.ToJson(wrap, true);
                        File.WriteAllText(pkgPath, json);
                        AssetDatabase.ImportAsset(pkgPath.Replace("\\","/"));
                        UnityEngine.Debug.Log($"[ProjectSnapshot] Wrote packages -> {pkgPath}");
                    }
                    catch (Exception ex) { UnityEngine.Debug.LogWarning("[ProjectSnapshot] Package snapshot failed: " + ex.Message); }
                }
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[ProjectSnapshot] Package snapshot init failed: " + ex.Message); }
#endif
        }

        [Serializable] class Wrapper { public Item[] items; }
        [Serializable] class Item { public string name; public string version; public string source; }
    }
}
