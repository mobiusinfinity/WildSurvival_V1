using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.Tools
{
    public class MenuItemScanner : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> wildSurvivalMenuItems = new List<string>();
        private Dictionary<string, string> menuToClass = new Dictionary<string, string>();

        [MenuItem("Tools/Wild Survival/?? Scan All Menu Items")]
        public static void ShowWindow()
        {
            var window = GetWindow<MenuItemScanner>("Menu Scanner");
            window.minSize = new Vector2(600, 400);
            window.ScanMenuItems();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Wild Survival Menu Items Scanner", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("?? Scan for Menu Items", GUILayout.Height(30)))
            {
                ScanMenuItems();
            }

            EditorGUILayout.Space();

            if (wildSurvivalMenuItems.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Scan for Menu Items' to find all Wild Survival tools.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Found {wildSurvivalMenuItems.Count} Wild Survival menu items:", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var menuItem in wildSurvivalMenuItems.OrderBy(m => m))
            {
                EditorGUILayout.BeginHorizontal("box");

                // Menu path
                EditorGUILayout.LabelField(menuItem, GUILayout.MinWidth(300));

                // Class name if found
                if (menuToClass.ContainsKey(menuItem))
                {
                    EditorGUILayout.LabelField($"[{menuToClass[menuItem]}]", EditorStyles.miniLabel);
                }

                // Test button
                if (GUILayout.Button("Test", GUILayout.Width(50)))
                {
                    bool success = EditorApplication.ExecuteMenuItem(menuItem);
                    if (success)
                        Debug.Log($"? Successfully opened: {menuItem}");
                    else
                        Debug.LogError($"? Failed to open: {menuItem}");
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Missing tools check
            EditorGUILayout.LabelField("Expected Tools Status:", EditorStyles.boldLabel);

            string[] expectedTools = new[]
            {
                "Error Reporter",
                "File Manager",
                "Project Tree Generator",
                "Migration Assistant",
                "Wild Survival Hub",
                "Progress Quest"
            };

            foreach (var tool in expectedTools)
            {
                bool found = wildSurvivalMenuItems.Any(m => m.ToLower().Contains(tool.ToLower().Replace(" ", "")));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(tool, GUILayout.Width(200));

                if (found)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("? Found");
                }
                else
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("? Missing");
                }
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Copy All Menu Paths to Clipboard", GUILayout.Height(30)))
            {
                string allPaths = string.Join("\n", wildSurvivalMenuItems);
                GUIUtility.systemCopyBuffer = allPaths;
                Debug.Log("Menu paths copied to clipboard!");
            }
        }

        private void ScanMenuItems()
        {
            wildSurvivalMenuItems.Clear();
            menuToClass.Clear();

            // Scan all assemblies for MenuItem attributes
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            var menuItemAttr = method.GetCustomAttribute<MenuItem>();
                            if (menuItemAttr != null)
                            {
                                string menuPath = menuItemAttr.menuItem;

                                // Check if it's a Wild Survival menu item
                                if (menuPath.Contains("Wild Survival") ||
                                    menuPath.Contains("WildSurvival") ||
                                    (menuPath.StartsWith("Tools/") && type.Namespace != null && type.Namespace.Contains("WildSurvival")))
                                {
                                    wildSurvivalMenuItems.Add(menuPath);
                                    menuToClass[menuPath] = type.Name;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            Debug.Log($"Found {wildSurvivalMenuItems.Count} Wild Survival menu items");
        }
    }
}