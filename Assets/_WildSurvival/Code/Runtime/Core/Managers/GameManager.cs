using UnityEngine;
using System;

/// <summary>
/// Central game manager handling game states and flow
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[GAME_MANAGER]");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
        
    public enum GameState
    {
        Loading,
        MainMenu,
        Playing,
        Paused,
        GameOver
    }
        
    private GameState currentState = GameState.Loading;
    public GameState CurrentState => currentState;
        
    public static event Action<GameState> OnGameStateChanged;
        
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
        
    private void Initialize()
    {
        Debug.Log("[GameManager] Initializing...");
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
    }
        
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
            
        GameState oldState = currentState;
        currentState = newState;
            
        Debug.Log($"[GameManager] State changed: {oldState} -> {newState}");
        OnGameStateChanged?.Invoke(newState);
            
        // Publish event
        EventBus.Publish(new GameStateChangedEvent { OldState = oldState, NewState = newState });
    }
        
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            Time.timeScale = 0f;
        }
    }
        
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
        }
    }
}
    
public struct GameStateChangedEvent
{
    public GameManager.GameState OldState;
    public GameManager.GameState NewState;
}