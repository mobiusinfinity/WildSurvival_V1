using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FireSystemTestSceneMenu : EditorWindow
{
    [MenuItem("Tools/Wild Survival/Create Fire Test Scene")]
    public static void CreateTestScene()
    {
        // Create new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Add test setup
        GameObject setupObj = new GameObject("Fire System Test Setup");
        FireSystemTestSetup setup = setupObj.AddComponent<FireSystemTestSetup>();

        // Add test menu
        setupObj.AddComponent<FireTestMenu>();

        // Auto setup
        setup.SetupCompleteTestScene();

        // Save scene
        string scenePath = "Assets/_WildSurvival/Scenes/FireSystemTest.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log($"✅ Test scene created at: {scenePath}");
        EditorUtility.DisplayDialog("Success", "Fire System test scene created!", "OK");
    }

    [MenuItem("Tools/Wild Survival/Add Fire Debug Menu")]
    public static void AddDebugMenu()
    {
        GameObject debugObj = GameObject.Find("Fire System Test Setup");
        if (debugObj == null)
        {
            debugObj = new GameObject("Fire System Test Setup");
        }

        if (debugObj.GetComponent<FireTestMenu>() == null)
        {
            debugObj.AddComponent<FireTestMenu>();
            Debug.Log("✅ Fire debug menu added - Press F1 in play mode");
        }
    }
}