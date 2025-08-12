using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Migration Assistant for Wild Survival project
/// Tracks progress across all development phases
/// Updated: 2025-08-12 to reflect actual progress
/// </summary>
public class WildSurvivalMigrationAssistant : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showTestChecklist = false;
    private int selectedPhaseForTesting = -1;

    // Phase definitions with ACTUAL status
    private static readonly MigrationPhase[] PHASES = new MigrationPhase[]
    {
        // Phase 1 - Player System (COMPLETE)
        new MigrationPhase
        {
            Name = "Phase 1 - Player System",
            Description = "Core player movement and controls",
            Files = new[]
            {
                new FileInfo("PlayerMovementController.cs", "Runtime/Player/Controller", true),
                new FileInfo("PlayerAnimatorController.cs", "Runtime/Player/Controller", true),
                new FileInfo("ThirdPersonCameraController.cs", "Runtime/Player/Camera", true),
                new FileInfo("PlayerStats.cs", "Runtime/Player/Stats", true),
                new FileInfo("PlayerController_OLD.cs", "Runtime/Player/Controller", true)
            }
        },
        
        // Phase 2 - Inventory System (COMPLETE)
        new MigrationPhase
        {
            Name = "Phase 2 - Inventory System",
            Description = "Complete inventory management with UI",
            Files = new[]
            {
                new FileInfo("InventoryManager.cs", "Runtime/Survival/Inventory/Core", true),
                new FileInfo("InventorySlot.cs", "Runtime/Survival/Inventory/Core", true),
                new FileInfo("ItemStack.cs", "Runtime/Survival/Inventory/Core", true),
                new FileInfo("ItemData.cs", "Runtime/Survival/Inventory/Items", true),
                new FileInfo("ItemDatabase.cs", "Runtime/Survival/Inventory/Items", true),
                new FileInfo("ItemType.cs", "Runtime/Survival/Inventory/Items", true),
                new FileInfo("InventoryEvents.cs", "Runtime/Survival/Inventory/Events", true),
                new FileInfo("InventoryUI.cs", "Runtime/Survival/Inventory/UI", true),
                new FileInfo("InventorySlotUI.cs", "Runtime/Survival/Inventory/UI", true),
                new FileInfo("ItemDragHandler.cs", "Runtime/Survival/Inventory/UI", true),
                new FileInfo("SpatialInventoryGrid.cs", "Runtime/Survival/Inventory/UI", true)
            }
        },
        
        // Phase 3 - Journal UI System (COMPLETE)
        new MigrationPhase
        {
            Name = "Phase 3 - Journal UI System",
            Description = "Quest and journal tracking system",
            Files = new[]
            {
                new FileInfo("JournalManager.cs", "Runtime/UI/Journal/Core", true),
                new FileInfo("JournalEntry.cs", "Runtime/UI/Journal/Data", true),
                new FileInfo("JournalTab.cs", "Runtime/UI/Journal/Display", true),
                new FileInfo("JournalUI.cs", "Runtime/UI/Journal/Display", true)
            }
        },
        
        // Phase 4 - Survival Mechanics (COMPLETE)
        new MigrationPhase
        {
            Name = "Phase 4 - Survival Mechanics",
            Description = "Hunger, thirst, temperature, and status effects",
            Files = new[]
            {
                new FileInfo("HungerSystem.cs", "Runtime/Core/Managers", true),
                new FileInfo("ThirstSystem.cs", "Runtime/Survival/Thirst", true),
                new FileInfo("TemperatureSystem.cs", "Runtime/Survival/Temperature", true),
                new FileInfo("StatusEffectManager.cs", "Runtime/Survival/StatusEffects", true)
            }
        },
        
        // Phase 5 - Crafting System (IN PROGRESS)
        new MigrationPhase
        {
            Name = "Phase 5 - Crafting System",
            Description = "Item crafting and recipes (BLOCKED by inventory migration)",
            Files = new[]
            {
                new FileInfo("CraftingManager.cs", "Runtime/Crafting", true),
                new FileInfo("CraftingRecipe.cs", "Runtime/Crafting", true),
                new FileInfo("CraftingUI.cs", "Runtime/Crafting/UI", false),
                new FileInfo("RecipeDatabase.cs", "Editor/Tools/UltimateInventoryTool", true),
                new FileInfo("WorkbenchSystem.cs", "Runtime/Crafting", false)
            }
        },
        
        // Phase 6 - Building System (NOT STARTED)
        new MigrationPhase
        {
            Name = "Phase 6 - Building System",
            Description = "Structure placement and building",
            Files = new[]
            {
                new FileInfo("BuildingManager.cs", "Runtime/Building", false),
                new FileInfo("BuildingPlacement.cs", "Runtime/Building", false),
                new FileInfo("Structure.cs", "Runtime/Building", false),
                new FileInfo("BuildingUI.cs", "Runtime/Building/UI", false)
            }
        },
        
        // Phase 7 - Save/Load System (NOT STARTED)
        new MigrationPhase
        {
            Name = "Phase 7 - Save/Load System",
            Description = "Game persistence and save management",
            Files = new[]
            {
                new FileInfo("SaveManager.cs", "Runtime/SaveSystem", false),
                new FileInfo("SaveData.cs", "Runtime/SaveSystem", false),
                new FileInfo("SaveLoadUI.cs", "Runtime/SaveSystem/UI", false)
            }
        },
        
        // Phase 8 - Audio Integration (NOT STARTED)
        new MigrationPhase
        {
            Name = "Phase 8 - Audio Integration",
            Description = "Sound effects and music system",
            Files = new[]
            {
                new FileInfo("AudioManager.cs", "Runtime/Audio", false),
                new FileInfo("SoundEffect.cs", "Runtime/Audio", false),
                new FileInfo("MusicController.cs", "Runtime/Audio", false)
            }
        }
    };

    [MenuItem("Tools/Wild Survival/🚀 Migration Assistant")]
    public static void ShowWindow()
    {
        var window = GetWindow<WildSurvivalMigrationAssistant>();
        window.titleContent = new GUIContent("🚀 Migration Assistant");
        window.minSize = new Vector2(600, 400);
    }

    private void OnGUI()
    {
        DrawHeader();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawOverallProgress();
        DrawCurrentStatus();
        DrawPhaseDetails();
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Wild Survival Migration Tracker v2.0", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawOverallProgress()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Overall Progress", EditorStyles.boldLabel);

        float totalFiles = PHASES.Sum(p => p.Files.Length);
        float completedFiles = PHASES.Sum(p => p.Files.Count(f => f.IsImplemented));
        float progress = completedFiles / totalFiles;

        var rect = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.ProgressBar(rect, progress, $"{progress * 100:F0}% ({completedFiles}/{totalFiles} files)");

        EditorGUILayout.Space(5);

        // Show sub-migration status if active
        if (selectedPhaseForTesting == 4) // Crafting phase
        {
            EditorGUILayout.HelpBox(
                "⚠️ BLOCKED: Inventory System Sub-Migration in Progress\n" +
                "Unifying 3 conflicting inventory systems into Ultimate Inventory Tool",
                MessageType.Warning
            );
        }
    }

    private void DrawCurrentStatus()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Find current phase
        int currentPhaseIndex = -1;
        for (int i = 0; i < PHASES.Length; i++)
        {
            if (PHASES[i].GetProgress() < 1f)
            {
                currentPhaseIndex = i;
                break;
            }
        }

        if (currentPhaseIndex >= 0)
        {
            var phase = PHASES[currentPhaseIndex];
            EditorGUILayout.LabelField($"Working on: {phase.Name}");

            if (currentPhaseIndex == 4) // Crafting
            {
                EditorGUILayout.LabelField("Status: BLOCKED - Inventory migration needed", EditorStyles.miniLabel);
            }
            else
            {
                var nextFile = phase.Files.FirstOrDefault(f => !f.IsImplemented);
                if (nextFile != null)
                {
                    EditorGUILayout.LabelField($"Next file: {nextFile.Name}", EditorStyles.miniLabel);
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("All phases complete! 🎉");
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPhaseDetails()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Phase Details", EditorStyles.boldLabel);

        for (int i = 0; i < PHASES.Length; i++)
        {
            DrawPhase(PHASES[i], i);
        }
    }

    private void DrawPhase(MigrationPhase phase, int index)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        float progress = phase.GetProgress();
        string statusIcon = progress >= 1f ? "✅" : progress > 0 ? "🔄" : "⏳";

        phase.IsExpanded = EditorGUILayout.Foldout(phase.IsExpanded,
            $"{statusIcon} {phase.Name} ({progress * 100:F0}%)", true);

        GUILayout.FlexibleSpace();

        if (progress >= 1f && GUILayout.Button("Test", GUILayout.Width(50)))
        {
            selectedPhaseForTesting = index;
            showTestChecklist = true;
        }

        EditorGUILayout.EndHorizontal();

        if (phase.IsExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(phase.Description, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(5);

            foreach (var file in phase.Files)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    file.IsImplemented ? "✅" : "❌",
                    GUILayout.Width(20));
                EditorGUILayout.LabelField(file.Name,
                    file.IsImplemented ? EditorStyles.label : EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActions()
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Copy Status", GUILayout.Height(30)))
        {
            CopyMigrationStatus();
        }

        if (GUILayout.Button("Open Documentation", GUILayout.Height(30)))
        {
            ShowNotification(new GUIContent("Check project knowledge for docs"));
        }

        if (GUILayout.Button("Run Tests", GUILayout.Height(30)))
        {
            RunTests();
        }

        EditorGUILayout.EndHorizontal();

        if (showTestChecklist && selectedPhaseForTesting >= 0)
        {
            DrawTestChecklist();
        }
    }

    private void DrawTestChecklist()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Test Checklist - {PHASES[selectedPhaseForTesting].Name}", EditorStyles.boldLabel);

        var tests = GetTestChecklistForPhase(selectedPhaseForTesting);
        foreach (var test in tests)
        {
            EditorGUILayout.LabelField($"• {test}", EditorStyles.wordWrappedMiniLabel);
        }

        if (GUILayout.Button("Close"))
        {
            showTestChecklist = false;
            selectedPhaseForTesting = -1;
        }

        EditorGUILayout.EndVertical();
    }

    private string[] GetTestChecklistForPhase(int phaseIndex)
    {
        return phaseIndex switch
        {
            0 => new[] { "WASD movement", "Sprint (Shift)", "Jump (Space)", "Crouch (Ctrl)", "Camera rotation" },
            1 => new[] { "Open inventory (I)", "Drag items", "Stack items", "Close inventory", "Weight limits" },
            2 => new[] { "Open journal (J)", "Switch tabs", "View entries", "Search function" },
            3 => new[] { "Hunger depletes", "Thirst depletes", "Temperature changes", "Status effects apply" },
            4 => new[] { "Open crafting menu", "View recipes", "Check requirements", "Craft item (when fixed)" },
            _ => new[] { "Not yet implemented" }
        };
    }

    private void CopyMigrationStatus()
    {
        var status = new System.Text.StringBuilder();
        status.AppendLine("=== Wild Survival Migration Status ===");
        status.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        status.AppendLine();

        float totalFiles = PHASES.Sum(p => p.Files.Length);
        float completedFiles = PHASES.Sum(p => p.Files.Count(f => f.IsImplemented));
        status.AppendLine($"Overall: {completedFiles}/{totalFiles} files ({completedFiles / totalFiles * 100:F0}%)");
        status.AppendLine();

        foreach (var phase in PHASES)
        {
            float progress = phase.GetProgress();
            string status_icon = progress >= 1f ? "✅" : progress > 0 ? "🔄" : "⏳";
            status.AppendLine($"{status_icon} {phase.Name}: {progress * 100:F0}%");
        }

        status.AppendLine();
        status.AppendLine("Note: Phase 5 (Crafting) blocked by inventory system migration");

        EditorGUIUtility.systemCopyBuffer = status.ToString();
        ShowNotification(new GUIContent("Status copied to clipboard!"));
    }

    private void RunTests()
    {
        EditorApplication.EnterPlaymode();
    }

    // Helper classes
    [Serializable]
    private class MigrationPhase
    {
        public string Name;
        public string Description;
        public FileInfo[] Files;
        public bool IsExpanded;

        public float GetProgress()
        {
            if (Files == null || Files.Length == 0) return 0f;
            return (float)Files.Count(f => f.IsImplemented) / Files.Length;
        }
    }

    [Serializable]
    private class FileInfo
    {
        public string Name;
        public string Path;
        public bool IsImplemented;

        public FileInfo(string name, string path, bool implemented)
        {
            Name = name;
            Path = path;
            IsImplemented = implemented;
        }
    }
}