using UnityEngine;
using UnityEditor;
using System.IO;

public class FireSystemFixer : Editor
{
    [MenuItem("Tools/Wild Survival/Fire System/Fix Duplicates")]
    public static void FixFireSystemDuplicates()
    {
        // Delete duplicate UI files
        string[] duplicatePaths = {
            "Assets/_WildSurvival/Code/Runtime/UI/Fire/FireManagementUI.cs",
            "Assets/_WildSurvival/Code/Runtime/UI/Fire/CookingUI.cs"
        };

        foreach (string path in duplicatePaths)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"✅ Deleted duplicate: {path}");
            }
        }

        // Delete the UI/Fire folder if empty
        string uiFireFolder = "Assets/_WildSurvival/Code/Runtime/UI/Fire";
        if (Directory.Exists(uiFireFolder) && Directory.GetFiles(uiFireFolder).Length == 0)
        {
            AssetDatabase.DeleteAsset(uiFireFolder);
            Debug.Log($"✅ Deleted empty folder: {uiFireFolder}");
        }

        AssetDatabase.Refresh();
        Debug.Log("✅ Fire System duplicates fixed!");
    }

    [MenuItem("Tools/Wild Survival/Fire System/Verify Structure")]
    public static void VerifyFireSystemStructure()
    {
        string[] requiredFiles = {
            "Assets/_WildSurvival/Code/Runtime/Fire/Core/FireInstance.cs",
            "Assets/_WildSurvival/Code/Runtime/Fire/Core/FireSystemConfiguration.cs",
            "Assets/_WildSurvival/Code/Runtime/Fire/Interaction/FireInteractionController.cs",
            "Assets/_WildSurvival/Code/Runtime/Fire/Interaction/FireManagementUI.cs",
            "Assets/_WildSurvival/Code/Runtime/Fire/Interaction/CampfireBuilder.cs"
        };

        int missing = 0;
        int empty = 0;

        foreach (string path in requiredFiles)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"❌ Missing: {path}");
                missing++;
            }
            else
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Length < 500) // Less than 500 bytes means probably empty template
                {
                    Debug.LogWarning($"⚠️ Empty/Template: {path} ({fi.Length} bytes)");
                    empty++;
                }
                else
                {
                    Debug.Log($"✅ Found: {path} ({fi.Length} bytes)");
                }
            }
        }

        if (missing == 0 && empty == 0)
        {
            Debug.Log("✅ Fire System structure verified successfully!");
        }
        else
        {
            Debug.LogError($"❌ Issues found - Missing: {missing}, Empty: {empty}");
        }
    }
}