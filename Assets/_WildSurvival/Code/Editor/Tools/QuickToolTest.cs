using UnityEngine;
using UnityEditor;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.Tools
{
    /// <summary>
    /// Quick test to verify all Wild Survival tools are accessible
    /// </summary>
    public class QuickToolTest : EditorWindow
    {
        [MenuItem("Tools/Wild Survival/🧪 Quick Tool Test")]
        public static void ShowWindow()
        {
            GetWindow<QuickToolTest>("Tool Test");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Wild Survival Tool Quick Test", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Click each button to test if the tool opens correctly.", MessageType.Info);
            EditorGUILayout.Space();

            // Test each tool
            if (GUILayout.Button("1. Test Error Reporter (ER)", GUILayout.Height(30)))
            {
                Debug.Log("Testing Error Reporter...");
                bool found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/📊 Error Reporter (Enhanced)");
                if (found)
                    Debug.Log("✅ Error Reporter opened!");
                else
                    Debug.LogError("Could not find Error Reporter menu item!");
            }

            if (GUILayout.Button("2. Test File Manager (FM)", GUILayout.Height(30)))
            {
                Debug.Log("Testing File Manager...");
                bool found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/🗂️ File Manager");
                if (found)
                    Debug.Log("✅ File Manager opened!");
                else
                    Debug.LogError("Could not find File Manager menu item!");
            }

            if (GUILayout.Button("3. Test Project Tree Generator (PTG)", GUILayout.Height(30)))
            {
                Debug.Log("Testing Project Tree Generator...");
                bool found = false;
                found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/Project Tree Generator");
                if (!found) found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/📁 Project Tree Generator");
                if (!found) found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/PTG");
                if (!found) Debug.LogError("Could not find Project Tree Generator menu item!");
                else Debug.Log("✅ Project Tree Generator opened!");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Other Tools:", EditorStyles.boldLabel);

            if (GUILayout.Button("Migration Assistant", GUILayout.Height(25)))
            {
                bool found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/Migration Assistant");
                if (!found) found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/🚀 Migration Assistant");
                if (found) Debug.Log("✅ Migration Assistant opened!");
                else Debug.LogError("Could not find Migration Assistant menu item!");
            }

            if (GUILayout.Button("Wild Survival Hub", GUILayout.Height(25)))
            {
                bool found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/Hub");
                if (!found) found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/🎮 Wild Survival Hub");
                if (!found) found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/Wild Survival Hub");
                if (found) Debug.Log("✅ Wild Survival Hub opened!");
                else Debug.LogError("Could not find Wild Survival Hub menu item!");
            }

            if (GUILayout.Button("Progress Quest", GUILayout.Height(25)))
            {
                bool found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/Progress Quest");
                if (!found) found = EditorApplication.ExecuteMenuItem("Tools/Wild Survival/📊 Progress Quest");
                if (found) Debug.Log("✅ Progress Quest opened!");
                else Debug.LogError("Could not find Progress Quest menu item!");
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Check the Console for any error messages.", MessageType.Info);

            if (GUILayout.Button("Clear Console"))
            {
                var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
                var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                clearMethod.Invoke(null, null);
            }
        }
    }
}