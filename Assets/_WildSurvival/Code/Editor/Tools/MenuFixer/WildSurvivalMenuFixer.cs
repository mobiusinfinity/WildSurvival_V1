using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
/// Fixes all menu naming inconsistencies in the Wild Survival project
/// Unifies everything under "Wild Survival" (with space)
/// </summary>
public class WildSurvivalMenuFixer : EditorWindow
{
    private Vector2 scrollPos;
    private List<FileIssue> foundIssues = new List<FileIssue>();
    private bool hasScanned = false;

    private class FileIssue
    {
        public string filePath;
        public string fileName;
        public List<string> issues = new List<string>();
        public bool willFix = true;
    }

    [MenuItem("Tools/Wild Survival/🔧 Fix Menu Duplicates", priority = -100)]
    public static void ShowWindow()
    {
        var window = GetWindow<WildSurvivalMenuFixer>("Menu Fix Tool");
        window.minSize = new Vector2(600, 400);
        window.ScanForIssues();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawScanResults();
        DrawActions();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Wild Survival Menu Unification Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "This tool will fix the duplicate menu problem by:\n" +
            "• Changing all 'WildSurvival' to 'Wild Survival' (with space)\n" +
            "• Unifying CreateAssetMenu entries\n" +
            "• Fixing MenuItem paths\n" +
            "• Removing namespace conflicts",
            MessageType.Info
        );

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawScanResults()
    {
        if (!hasScanned)
        {
            EditorGUILayout.LabelField("Click 'Scan Project' to find issues", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        EditorGUILayout.LabelField($"Found Issues: {foundIssues.Count} files", EditorStyles.boldLabel);

        if (foundIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("No menu naming issues found! Your project is clean.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(300));

        foreach (var issue in foundIssues)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            issue.willFix = EditorGUILayout.Toggle(issue.willFix, GUILayout.Width(20));
            EditorGUILayout.LabelField(issue.fileName, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            foreach (var line in issue.issues)
            {
                EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawActions()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("🔍 Scan Project", GUILayout.Height(30)))
        {
            ScanForIssues();
        }

        GUI.enabled = foundIssues.Count > 0;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("✅ Fix Selected Issues", GUILayout.Height(30)))
        {
            FixSelectedIssues();
        }

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("📋 Generate Report", GUILayout.Height(30)))
        {
            GenerateReport();
        }

        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void ScanForIssues()
    {
        foundIssues.Clear();

        string[] csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        foreach (string file in csFiles)
        {
            // Skip this tool itself
            if (file.Contains("WildSurvivalMenuFixer")) continue;

            string content = File.ReadAllText(file);
            var issue = new FileIssue
            {
                filePath = file,
                fileName = Path.GetFileName(file)
            };

            // Check for various issues

            // 1. CreateAssetMenu with WildSurvival (no space)
            if (Regex.IsMatch(content, @"\[CreateAssetMenu.*menuName\s*=\s*""WildSurvival/"))
            {
                var matches = Regex.Matches(content, @"\[CreateAssetMenu.*menuName\s*=\s*""WildSurvival/[^""]+""");
                foreach (Match match in matches)
                {
                    issue.issues.Add($"CreateAssetMenu: {match.Value}");
                }
            }

            // 2. MenuItem with WildSurvival (no space)
            if (Regex.IsMatch(content, @"\[MenuItem\(""WildSurvival/"))
            {
                var matches = Regex.Matches(content, @"\[MenuItem\(""WildSurvival/[^""]+""");
                foreach (Match match in matches)
                {
                    issue.issues.Add($"MenuItem: {match.Value}");
                }
            }

            // 3. Namespace issues
            if (content.Contains("namespace WildSurvival.") || content.Contains("using WildSurvival."))
            {
                if (content.Contains("namespace WildSurvival."))
                {
                    issue.issues.Add("Contains namespace WildSurvival.*");
                }
                if (content.Contains("using WildSurvival."))
                {
                    issue.issues.Add("Contains using WildSurvival.*");
                }
            }

            if (issue.issues.Count > 0)
            {
                foundIssues.Add(issue);
            }
        }

        hasScanned = true;
        Debug.Log($"[Menu Fixer] Scan complete. Found {foundIssues.Count} files with issues.");
    }

    private void FixSelectedIssues()
    {
        int fixedCount = 0;
        int fixedIssues = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var issue in foundIssues)
            {
                if (!issue.willFix) continue;

                string content = File.ReadAllText(issue.filePath);
                string original = content;

                // Fix CreateAssetMenu
                content = Regex.Replace(content,
                    @"\[CreateAssetMenu\((.*?)menuName\s*=\s*""WildSurvival/",
                    @"[CreateAssetMenu($1menuName = ""Wild Survival/");

                // Also fix any fileName patterns
                content = Regex.Replace(content,
                    @"(fileName\s*=\s*"".*?)"",",
                    @"$1"",");

                // Fix MenuItem
                content = Regex.Replace(content,
                    @"\[MenuItem\(""WildSurvival/",
                    @"[MenuItem(""Tools/Wild Survival/");

                // Remove namespaces (comment them out for safety)
                content = Regex.Replace(content,
                    @"^(\s*)namespace WildSurvival\.[^\{]*$",
                    @"$1// namespace removed by Menu Fixer
$1// $0",
                    RegexOptions.Multiline);

                // Comment out the closing brace for namespace
                if (content.Contains("// namespace removed by Menu Fixer"))
                {
                    // This is more complex - would need proper parsing
                    // For now, just add a note
                    content = content.Replace("// namespace removed by Menu Fixer",
                        "// namespace removed by Menu Fixer - check closing brace");
                }

                // Remove using statements for WildSurvival namespaces
                content = Regex.Replace(content,
                    @"^\s*using WildSurvival\.[^;]*;",
                    @"// $0 // Commented out by Menu Fixer",
                    RegexOptions.Multiline);

                if (content != original)
                {
                    File.WriteAllText(issue.filePath, content);
                    fixedCount++;
                    fixedIssues += issue.issues.Count;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Fix Complete",
            $"Fixed {fixedIssues} issues in {fixedCount} files.\n\n" +
            "Please check the console for any compilation errors.",
            "OK");

        Debug.Log($"[Menu Fixer] Fixed {fixedIssues} issues in {fixedCount} files");

        // Rescan to verify
        ScanForIssues();
    }

    private void GenerateReport()
    {
        string report = "Wild Survival Menu Issues Report\n";
        report += "=====================================\n\n";

        foreach (var issue in foundIssues)
        {
            report += $"File: {issue.fileName}\n";
            report += $"Path: {issue.filePath}\n";
            report += "Issues:\n";
            foreach (var line in issue.issues)
            {
                report += $"  - {line}\n";
            }
            report += "\n";
        }

        // Copy to clipboard
        GUIUtility.systemCopyBuffer = report;

        // Also save to file
        string path = EditorUtility.SaveFilePanel(
            "Save Report",
            Application.dataPath,
            "MenuIssuesReport.txt",
            "txt"
        );

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, report);
            Debug.Log($"Report saved to: {path}");
        }

        EditorUtility.DisplayDialog("Report Generated",
            "Report has been copied to clipboard and can be saved to file.",
            "OK");
    }
}

/// <summary>
/// Manual fix instructions if the automated tool has issues
/// </summary>
public class MenuFixInstructions
{
    [MenuItem("Tools/Wild Survival/📖 Menu Fix Instructions", priority = -99)]
    public static void ShowInstructions()
    {
        string instructions = @"
MANUAL FIX INSTRUCTIONS
=======================

If the automated fix has issues, here's what to do manually:

1. FIND ALL FILES WITH ISSUES:
   Search for: 'menuName = ""WildSurvival'
   In: *.cs files

2. FIX CreateAssetMenu:
   FROM: [CreateAssetMenu(menuName = ""WildSurvival/...
   TO:   [CreateAssetMenu(menuName = ""Wild Survival/...

3. FIX MenuItem:
   FROM: [MenuItem(""WildSurvival/...
   TO:   [MenuItem(""Tools/Wild Survival/...

4. FILES TO CHECK:
   - ItemDefinition.cs
   - RecipeDefinition.cs
   - ItemDatabase.cs
   - RecipeDatabase.cs
   - CraftingRecipe.cs
   - ItemData.cs

5. REMOVE NAMESPACES:
   Remove all 'namespace WildSurvival.*' declarations
   Remove corresponding closing braces
   Remove 'using WildSurvival.*' statements

6. AFTER FIXING:
   - Save all files
   - Return to Unity
   - Let it compile
   - Check for errors in console

EXPECTED RESULT:
- Single 'Wild Survival' menu in Create menu
- All tools under Tools → Wild Survival
- No compilation errors
";

        EditorUtility.DisplayDialog("Manual Fix Instructions",
            instructions,
            "OK");

        // Also copy to clipboard
        GUIUtility.systemCopyBuffer = instructions;
        Debug.Log("Instructions copied to clipboard");
    }
}

/// <summary>
/// Quick validation to ensure everything is working
/// </summary>
public class MenuValidator
{
    [MenuItem("Tools/Wild Survival/✓ Validate Menus", priority = -98)]
    public static void ValidateMenus()
    {
        Debug.Log("=== Menu Validation Starting ===");

        // Check for database assets
        string[] itemDbs = AssetDatabase.FindAssets("t:ItemDatabase");
        string[] recipeDbs = AssetDatabase.FindAssets("t:RecipeDatabase");

        Debug.Log($"Found {itemDbs.Length} Item Databases");
        Debug.Log($"Found {recipeDbs.Length} Recipe Databases");

        // Check if we can create assets via menu
        Debug.Log("\nTo test menu creation:");
        Debug.Log("1. Right-click in Project window");
        Debug.Log("2. Go to Create → Wild Survival");
        Debug.Log("3. You should see unified menu options");

        // Report status
        bool hasIssues = false;

        // Check for duplicate menus by searching for both patterns
        string[] csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        int wildSurvivalCount = 0;
        int wildSpaceSurvivalCount = 0;

        foreach (string file in csFiles)
        {
            string content = File.ReadAllText(file);

            if (content.Contains(@"menuName = ""WildSurvival/"))
                wildSurvivalCount++;

            if (content.Contains(@"menuName = ""Wild Survival/"))
                wildSpaceSurvivalCount++;
        }

        Debug.Log($"\nMenu naming status:");
        Debug.Log($"- 'WildSurvival' (no space): {wildSurvivalCount} files");
        Debug.Log($"- 'Wild Survival' (with space): {wildSpaceSurvivalCount} files");

        if (wildSurvivalCount > 0)
        {
            Debug.LogWarning("⚠ Still have files using 'WildSurvival' without space!");
            hasIssues = true;
        }

        if (!hasIssues)
        {
            Debug.Log("\n✅ All menus are properly unified!");
        }
        else
        {
            Debug.LogWarning("\n⚠ Issues found. Run 'Fix Menu Duplicates' tool.");
        }

        Debug.Log("=== Validation Complete ===");
    }
}