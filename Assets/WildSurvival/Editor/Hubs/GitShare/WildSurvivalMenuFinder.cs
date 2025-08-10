using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace WildSurvival.Editor.Collab
{
    public class WildSurvivalMenuFinder : EditorWindow
    {
        [MenuItem("WildSurvival/Tools (V2)/Menu Finder")]
        public static void Open() => GetWindow<WildSurvivalMenuFinder>("Menu Finder");

        string[] _menus;
        Vector2 _scroll;

        void OnEnable()
        {
            Refresh();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Refresh")) Refresh();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_menus != null)
            {
                foreach (var m in _menus)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(m);
                    if (GUILayout.Button("Open", GUILayout.Width(80)))
                        EditorApplication.ExecuteMenuItem(m);
                    GUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void Refresh()
        {
            _menus = GetMenusStartingWith("WildSurvival/") ?? new string[0];
        }

        static string[] GetMenusStartingWith(string prefix)
        {
            var t = typeof(EditorApplication).Assembly.GetType("UnityEditor.Unsupported");
            var mi = t?.GetMethod("GetSubmenus", BindingFlags.NonPublic | BindingFlags.Static);
            if (mi == null) return null;
            var all = mi.Invoke(null, new object[] { "" }) as string[];
            return all?.Where(s => s.StartsWith(prefix)).OrderBy(s => s).ToArray();
        }
    }
}
