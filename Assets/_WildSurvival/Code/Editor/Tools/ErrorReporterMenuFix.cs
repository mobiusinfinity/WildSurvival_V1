using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.Tools
{
    /// <summary>
    /// Temporary fix to add Error Reporter menu item if missing
    /// This creates a wrapper that finds and opens the Error Reporter
    /// </summary>
    public class ErrorReporterMenuFix
    {
        [MenuItem("Tools/Wild Survival/?? Error Reporter (Fixed) %#e", priority = 1)]
        public static void OpenErrorReporter()
        {
            // Try to find the ErrorReporter class
            Type errorReporterType = null;

            // Search all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                errorReporterType = assembly.GetTypes().FirstOrDefault(t => t.Name == "ErrorReporter");
                if (errorReporterType != null) break;
            }

            if (errorReporterType != null)
            {
                // Try to find ShowWindow method
                var showWindowMethod = errorReporterType.GetMethod("ShowWindow",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (showWindowMethod != null)
                {
                    showWindowMethod.Invoke(null, null);
                    Debug.Log("? Error Reporter opened successfully!");
                }
                else
                {
                    // Try to find CaptureCurrentState or similar method
                    var captureMethod = errorReporterType.GetMethod("CaptureCurrentState",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (captureMethod != null)
                    {
                        captureMethod.Invoke(null, null);
                        Debug.Log("? Error Reporter capture triggered!");
                    }
                    else
                    {
                        // Try to create instance as EditorWindow
                        if (typeof(EditorWindow).IsAssignableFrom(errorReporterType))
                        {
                            var window = EditorWindow.GetWindow(errorReporterType);
                            window.Show();
                            Debug.Log("? Error Reporter window created!");
                        }
                        else
                        {
                            Debug.LogError("Could not find a way to open Error Reporter!");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Error Reporter class not found! It might not be compiled correctly.");
                Debug.Log("Attempting to open ErrorReporter.cs file for inspection...");

                // Try to open the file directly
                var guid = AssetDatabase.FindAssets("ErrorReporter t:Script").FirstOrDefault();
                if (!string.IsNullOrEmpty(guid))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
                    Debug.Log($"Opened ErrorReporter.cs at: {path}");
                }
            }
        }

        [MenuItem("Tools/Wild Survival/?? Hub (Fixed)", priority = 50)]
        public static void OpenHub()
        {
            // Try to find WildSurvivalHub class
            Type hubType = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                hubType = assembly.GetTypes().FirstOrDefault(t => t.Name == "WildSurvivalHub");
                if (hubType != null) break;
            }

            if (hubType != null)
            {
                var showWindowMethod = hubType.GetMethod("ShowWindow",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (showWindowMethod != null)
                {
                    showWindowMethod.Invoke(null, null);
                    Debug.Log("? Wild Survival Hub opened successfully!");
                }
                else if (typeof(EditorWindow).IsAssignableFrom(hubType))
                {
                    var window = EditorWindow.GetWindow(hubType);
                    window.Show();
                    Debug.Log("? Wild Survival Hub window created!");
                }
            }
            else
            {
                Debug.LogError("WildSurvivalHub class not found!");
            }
        }
    }

    /// <summary>
    /// Quick access to all working tools
    /// </summary>
    public class QuickToolAccess : EditorWindow
    {
        [MenuItem("Tools/Wild Survival/?? Quick Tool Access")]
        public static void ShowWindow()
        {
            GetWindow<QuickToolAccess>("Quick Tools");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Quick Tool Access", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Direct access to all Wild Survival tools", MessageType.Info);
            EditorGUILayout.Space();

            // Working tools
            GUI.backgroundColor = Color.green;
            EditorGUILayout.LabelField("Working Tools:", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("??? File Manager", GUILayout.Height(30)))
            {
                EditorApplication.ExecuteMenuItem("Tools/Wild Survival/??? File Manager (Enhanced)");
            }

            if (GUILayout.Button("?? Project Tree Generator", GUILayout.Height(30)))
            {
                EditorApplication.ExecuteMenuItem("Tools/Wild Survival/Project Tree Generator");
            }

            if (GUILayout.Button("?? Migration Assistant", GUILayout.Height(30)))
            {
                EditorApplication.ExecuteMenuItem("Tools/Wild Survival/?? Migration Assistant");
            }

            if (GUILayout.Button("?? Progress Quest", GUILayout.Height(30)))
            {
                EditorApplication.ExecuteMenuItem("Tools/Wild Survival/?? Progress Quest");
            }

            EditorGUILayout.Space();

            // Fixed tools
            GUI.backgroundColor = Color.yellow;
            EditorGUILayout.LabelField("Fixed Tools:", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("?? Error Reporter (Ctrl+Shift+E)", GUILayout.Height(30)))
            {
                ErrorReporterMenuFix.OpenErrorReporter();
            }

            if (GUILayout.Button("?? Wild Survival Hub", GUILayout.Height(30)))
            {
                ErrorReporterMenuFix.OpenHub();
            }
        }
    }
}