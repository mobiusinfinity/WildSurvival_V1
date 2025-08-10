#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace WildSurvival.EditorTools
{
    internal class WSGitUrlPrompt : EditorWindow
    {
        string _title, _url;
        Action<string> _onOk;

        public static void Show(string title, string currentUrl, Action<string> onOk)
        {
            var w = CreateInstance<WSGitUrlPrompt>();
            w._title = title;
            w._url = currentUrl;
            w._onOk = onOk;
            w.titleContent = new GUIContent(title);
            w.minSize = new Vector2(520, 90);
            w.ShowUtility();
        }

        void OnGUI()
        {
            GUILayout.Label(_title, EditorStyles.boldLabel);
            _url = EditorGUILayout.TextField("New URL", _url);
            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(90)))
                    Close();
                if (GUILayout.Button("OK", GUILayout.Width(90)))
                {
                    _onOk?.Invoke(_url?.Trim());
                    Close();
                }
            }
        }
    }
}
#endif
