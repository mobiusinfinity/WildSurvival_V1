using UnityEditor;
using UnityEngine;
using System.IO;

namespace WildSurvival.Editor.Collab
{
    public static class SessionNotes
    {
        [MenuItem("WildSurvival/Tools (V2.1)/Create Session Notes")]
        public static void CreateNotes()
        {
            var dir = "Assets/WildSurvival/Logs";
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"SessionNotes_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var template = 
@"WildSurvival – Session Notes

What I changed:
- 

What I ran (tools):
- 

Issues / Errors I saw:
- 

Next steps / Tasks:
- 
";
            File.WriteAllText(path, template);
            AssetDatabase.ImportAsset(path.Replace("\\","/"));
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path.Replace("\\","/"));
            UnityEngine.Debug.Log($"[SessionNotes] Created {path}");
        }

        [MenuItem("WildSurvival/Tools (V2)/Set Player Prefab from Selection")]
        public static void SetPlayerPrefabFromSelection()
        {
            var go = Selection.activeObject as GameObject;
            if (!go)
            {
                EditorUtility.DisplayDialog("Set Player Prefab", "Select a prefab in the Project window first.", "OK");
                return;
            }
            var path = AssetDatabase.GetAssetPath(go);
            if (PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.NotAPrefab || string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Set Player Prefab", "Selection is not a prefab asset.", "OK");
                return;
            }
            EditorPrefs.SetString("WS_PlayerPrefabDoctorV2_Path", path);
            UnityEngine.Debug.Log($"[SessionNotes] Stored Player Prefab path: {path}");
        }
    }
}
