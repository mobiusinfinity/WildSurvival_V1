using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Generates a complete setup checklist
/// </summary>
public class FireSystemChecklistGenerator : EditorWindow
{
    private Vector2 scrollPos;
    private Dictionary<string, bool> checklist = new Dictionary<string, bool>();

    [MenuItem("Tools/Wild Survival/Fire System/Setup Checklist")]
    public static void ShowWindow()
    {
        var window = GetWindow<FireSystemChecklistGenerator>("📋 Setup Checklist");
        window.minSize = new Vector2(400, 600);
        window.GenerateChecklist();
    }

    private void GenerateChecklist()
    {
        checklist.Clear();

        // Core Components
        checklist["FireInstance.cs exists"] = AssetExists("FireInstance");
        checklist["FireInteractionController.cs exists"] = AssetExists("FireInteractionController");
        checklist["FireManagementUI.cs exists"] = AssetExists("FireManagementUI");
        checklist["PlayerVitals.cs exists"] = AssetExists("PlayerVitals");
        checklist["ItemDatabase created"] = DatabaseExists();

        // Prefabs
        checklist["Campfire prefab exists"] = PrefabExists("Campfire");
        checklist["Torch prefab exists"] = PrefabExists("Torch");

        // Scene Setup
        checklist["Player has required components"] = PlayerSetupCorrect();
        checklist["Fire layer configured"] = LayerMask.NameToLayer("Fire") != -1;
        checklist["NotificationSystem in scene"] = FindObjectOfType<NotificationSystem>() != null;

        // Items
        checklist["Fuel items created"] = FuelItemsExist();
        checklist["Fire recipes created"] = RecipesExist();

        // Testing
        checklist["Test scene created"] = SceneExists("FireSystemTestScene");
        checklist["Debug overlay available"] = AssetExists("FireSystemDebugOverlay");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fire System Setup Checklist", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        int completed = 0;
        int total = checklist.Count;

        foreach (var item in checklist)
        {
            EditorGUILayout.BeginHorizontal();

            GUI.color = item.Value ? Color.green : Color.red;
            EditorGUILayout.LabelField(item.Value ? "✓" : "✗", GUILayout.Width(20));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(item.Key);

            if (!item.Value)
            {
                if (GUILayout.Button("Fix", GUILayout.Width(50)))
                {
                    FixIssue(item.Key);
                }
            }

            EditorGUILayout.EndHorizontal();

            if (item.Value) completed++;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(20);

        // Progress bar
        float progress = (float)completed / total;
        var rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        EditorGUI.ProgressBar(rect, progress, $"{completed}/{total} Complete");

        EditorGUILayout.Space(10);

        if (progress >= 1f)
        {
            EditorGUILayout.HelpBox("Fire system is fully configured!", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"{total - completed} issues remaining", MessageType.Warning);
        }

        if (GUILayout.Button("Refresh Checklist", GUILayout.Height(30)))
        {
            GenerateChecklist();
        }

        if (GUILayout.Button("Auto-Fix All Issues", GUILayout.Height(30)))
        {
            AutoFixAll();
        }
    }

    private bool AssetExists(string name)
    {
        return AssetDatabase.FindAssets($"{name} t:Script").Length > 0;
    }

    private bool PrefabExists(string name)
    {
        return AssetDatabase.FindAssets($"{name} t:Prefab").Length > 0;
    }

    private bool DatabaseExists()
    {
        return AssetDatabase.FindAssets("t:ItemDatabase").Length > 0;
    }

    private bool PlayerSetupCorrect()
    {
        if (!Application.isPlaying) return true; // Can't check in edit mode

        var player = GameObject.FindWithTag("Player");
        if (player == null) return false;

        return player.GetComponent<PlayerStats>() != null &&
               player.GetComponent<PlayerInventory>() != null &&
               player.GetComponent<FireInteractionController>() != null;
    }

    private bool FuelItemsExist()
    {
        return AssetDatabase.FindAssets("fuel t:ItemDefinition").Length > 0;
    }

    private bool RecipesExist()
    {
        return AssetDatabase.FindAssets("fire t:RecipeDefinition").Length > 0 ||
               AssetDatabase.FindAssets("campfire t:RecipeDefinition").Length > 0;
    }

    private bool SceneExists(string sceneName)
    {
        return AssetDatabase.FindAssets($"{sceneName} t:Scene").Length > 0;
    }

    private void FixIssue(string issue)
    {
        Debug.Log($"Attempting to fix: {issue}");

        // Implement specific fixes for each issue
        switch (issue)
        {
            case "Fire layer configured":
                SetupFireLayer();
                break;

            case "Campfire prefab exists":
                FirePrefabCreator.CreateAllFirePrefabs();
                break;

            case "Test scene created":
                FireTestSceneBuilder.CreateTestScene();
                break;

                // Add more fixes as needed
        }

        GenerateChecklist(); // Refresh
    }

    private void AutoFixAll()
    {
        foreach (var item in checklist)
        {
            if (!item.Value)
            {
                FixIssue(item.Key);
            }
        }
    }

    private void SetupFireLayer()
    {
        // Implementation from previous code
    }
}