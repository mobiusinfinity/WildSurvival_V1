using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildSurvival.Editor.Git
{
    public class PublicMirrorSettingsV1 : ScriptableObject
    {
        [Header("Destination (outside the Unity project)")]
        public string mirrorFolderAbs = "";

        [Header("Optional: public repo to push mirror to")]
        public string remoteUrl = "";

        [Header("Zip options")]
        public bool zipMirror = true;

        [Header("What to include from Project Root")]
        public string[] includeTop = new[] { "Assets", "Packages", "ProjectSettings" };

        [Header("Directories to exclude anywhere")]
        public string[] excludeDirs = new[]
        {
            "Library","Logs","Temp","Obj",".git",".vs","Build","Builds",".idea","UserSettings","MemoryCaptures"
        };

        [Header("Extra project-relative excludes")]
        public string[] extraProjectExcludes = new[]
        {
            "Assets/WildSurvival/Logs",
            "Assets/WildSurvival/Imports"
        };

#if UNITY_EDITOR
        public static PublicMirrorSettingsV1 LoadOrCreate()
        {
            var settings = Resources.Load<PublicMirrorSettingsV1>("PublicMirrorSettingsV1");
            if (settings == null)
            {
                settings = CreateInstance<PublicMirrorSettingsV1>();
                System.IO.Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.CreateAsset(settings, "Assets/Resources/PublicMirrorSettingsV1.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("[PublicMirror] Created default settings at Assets/Resources/PublicMirrorSettingsV1.asset");
            }
            return settings;
        }
#endif
    }
}
