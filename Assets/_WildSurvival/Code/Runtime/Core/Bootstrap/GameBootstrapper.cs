using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Simple GameBootstrapper without ServiceRegistry dependencies
/// Place this file at: Assets/_WildSurvival/Code/Runtime/Core/Bootstrap/GameBootstrapper.cs
/// </summary>
public class GameBootstrapper : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string persistentSceneName = "_Persistent";
    [SerializeField] private string mainMenuSceneName = "";
    [SerializeField] private bool loadMainMenuOnStart = false;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private void Awake()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        if (debugMode)
            Debug.Log("[GameBootstrapper] Initializing Wild Survival...");

        // Load persistent scene if specified and not already loaded
        if (!string.IsNullOrEmpty(persistentSceneName))
        {
            var scene = SceneManager.GetSceneByName(persistentSceneName);
            if (!scene.isLoaded)
            {
                if (debugMode)
                    Debug.Log($"[GameBootstrapper] Loading persistent scene: {persistentSceneName}");

                var asyncLoad = SceneManager.LoadSceneAsync(persistentSceneName, LoadSceneMode.Additive);
                if (asyncLoad != null)
                {
                    yield return asyncLoad;
                }
            }
        }

        // Small delay to ensure all objects are initialized
        yield return null;

        // Log found systems if in debug mode
        if (debugMode)
        {
            LogFoundSystems();
        }

        // Load main menu if configured
        if (loadMainMenuOnStart && !string.IsNullOrEmpty(mainMenuSceneName))
        {
            if (debugMode)
                Debug.Log($"[GameBootstrapper] Loading main menu: {mainMenuSceneName}");

            yield return SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
        }

        if (debugMode)
            Debug.Log("[GameBootstrapper] Initialization complete");
    }

    private void LogFoundSystems()
    {
        // Check for GameManager
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            Debug.Log("[GameBootstrapper] ✓ Found GameManager");
        else
            Debug.Log("[GameBootstrapper] ✗ GameManager not found");

        // Check for PlayerStats  
        var playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats != null)
            Debug.Log("[GameBootstrapper] ✓ Found PlayerStats");
        else
            Debug.Log("[GameBootstrapper] ✗ PlayerStats not found");

        // Check for HungerSystem
        var hungerSystem = FindObjectOfType<HungerSystem>();
        if (hungerSystem != null)
            Debug.Log("[GameBootstrapper] ✓ Found HungerSystem");
        else
            Debug.Log("[GameBootstrapper] ✗ HungerSystem not found");

        // Check for TemperatureSystem
        var temperatureSystem = FindObjectOfType<TemperatureSystem>();
        if (temperatureSystem != null)
            Debug.Log("[GameBootstrapper] ✓ Found TemperatureSystem");
        else
            Debug.Log("[GameBootstrapper] ✗ TemperatureSystem not found");

        // Check for NotificationSystem
        var notificationSystem = FindObjectOfType<NotificationSystem>();
        if (notificationSystem != null)
            Debug.Log("[GameBootstrapper] ✓ Found NotificationSystem");
        else
            Debug.Log("[GameBootstrapper] ✗ NotificationSystem not found");
    }
}