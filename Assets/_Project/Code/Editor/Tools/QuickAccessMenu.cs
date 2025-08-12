using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor
{
    public static class QuickAccessMenu
    {
        [MenuItem("Wild Survival/Play Game %&p")]
        public static void PlayGame()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Core/_Bootstrap.unity");
            EditorApplication.isPlaying = true;
        }
    }
}