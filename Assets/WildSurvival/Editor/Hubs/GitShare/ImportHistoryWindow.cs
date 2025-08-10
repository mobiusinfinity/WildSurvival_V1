using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace WildSurvival.Editor.Collab
{
    public class ImportHistoryWindow : EditorWindow
    {
        [MenuItem("WildSurvival/Tools (V2)/Import/Import History & Revert")]
        public static void Open() => GetWindow<ImportHistoryWindow>("Import History");

        string _importsDir = "Assets/WildSurvival/Imports";
        string _backupsDir = "Assets/WildSurvival/Backups";
        Vector2 _scroll;

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Quick access to recent Imports and Backups. You can open, ping, or remove an import folder.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Imports")) EditorUtility.RevealInFinder(Path.GetFullPath(_importsDir));
                if (GUILayout.Button("Open Backups")) EditorUtility.RevealInFinder(Path.GetFullPath(_backupsDir));
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawSection("Recent Imports", _importsDir);
            GUILayout.Space(8);
            DrawSection("Backups", _backupsDir, allowDelete:false);
            EditorGUILayout.EndScrollView();
        }

        void DrawSection(string title, string root, bool allowDelete = true)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
            if (!Directory.Exists(root)) { GUILayout.Label("(none)"); return; }
            var dirs = Directory.GetDirectories(root).OrderByDescending(Directory.GetLastWriteTimeUtc).Take(20).ToList();
            if (dirs.Count == 0) { GUILayout.Label("(none)"); return; }
            foreach (var d in dirs)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(Path.GetFileName(d));
                    if (GUILayout.Button("Open", GUILayout.Width(60))) EditorUtility.RevealInFinder(Path.GetFullPath(d));
                    if (allowDelete && GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Import", $"Delete folder?\n{d}", "Delete", "Cancel"))
                        {
                            FileUtil.DeleteFileOrDirectory(d);
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }
        }
    }
}
