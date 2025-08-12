using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Temperature system managing environmental and body temperature
/// Place this file at: Assets/_WildSurvival/Code/Runtime/Survival/Temperature/TemperatureSystem.cs
/// </summary>
public class TemperatureSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float _baseBodyTemp = 37f;
    [SerializeField] private float _comfortableMin = 18f;
    [SerializeField] private float _comfortableMax = 26f;
    [SerializeField] private float _hypothermiaThreshold = 35f;
    [SerializeField] private float _hyperthermiaThreshold = 39f;

    [Header("Current State")]
    [SerializeField] private float _currentEnvironmentTemp = 20f;
    [SerializeField] private float _currentBodyTemp = 37f;
    [SerializeField] private float _perceivedTemp = 20f;

    // Events
    public UnityEvent<float> OnBodyTempChanged = new();
    public UnityEvent<float> OnEnvironmentTempChanged = new();
    public UnityEvent<TemperatureState> OnStateChanged = new();

    public enum TemperatureState
    {
        Freezing,
        Cold,
        Cool,
        Comfortable,
        Warm,
        Hot,
        Overheating
    }

    // Properties
    public float CurrentTemperature => _currentEnvironmentTemp;
    public float BodyTemperature => _currentBodyTemp;
    public float PerceivedTemperature => _perceivedTemp;
    public TemperatureState CurrentState { get; private set; }

    // Modifiers
    private float _clothingInsulation = 1f;
    private float _wetness = 0f;
    private float _windChill = 0f;
    private float _nearFireBonus = 0f;

    // Singleton
    private static TemperatureSystem _instance;
    public static TemperatureSystem Instance => _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateTemperatureState();
    }

    private void Update()
    {
        // Simple body temp regulation
        float targetTemp = CalculateTargetBodyTemp();
        _currentBodyTemp = Mathf.Lerp(_currentBodyTemp, targetTemp, Time.deltaTime * 0.1f);

        // Update perceived temperature
        _perceivedTemp = CalculatePerceivedTemp();

        UpdateTemperatureState();
    }

    private float CalculateTargetBodyTemp()
    {
        float envEffect = (_currentEnvironmentTemp - 20f) * 0.05f;
        float wetnessEffect = _wetness * -2f;
        float fireEffect = _nearFireBonus;

        return Mathf.Clamp(
            _baseBodyTemp + envEffect + wetnessEffect + fireEffect,
            32f, // Minimum survivable
            42f  // Maximum survivable
        );
    }

    private float CalculatePerceivedTemp()
    {
        float perceived = _currentEnvironmentTemp;
        perceived -= _windChill;
        perceived += _nearFireBonus * 10f;
        perceived += (_clothingInsulation - 1f) * 5f;
        perceived -= _wetness * 5f;

        return perceived;
    }

    private void UpdateTemperatureState()
    {
        var oldState = CurrentState;

        CurrentState = _perceivedTemp switch
        {
            < 0f => TemperatureState.Freezing,
            < 10f => TemperatureState.Cold,
            < 15f => TemperatureState.Cool,
            < 26f => TemperatureState.Comfortable,
            < 30f => TemperatureState.Warm,
            < 35f => TemperatureState.Hot,
            _ => TemperatureState.Overheating
        };

        if (oldState != CurrentState)
        {
            OnStateChanged?.Invoke(CurrentState);
        }
    }

    public void SetEnvironmentTemperature(float temp)
    {
        _currentEnvironmentTemp = temp;
        OnEnvironmentTempChanged?.Invoke(temp);
    }

    public void SetClothingInsulation(float insulation)
    {
        _clothingInsulation = Mathf.Max(0f, insulation);
    }

    public void SetWetness(float wetness)
    {
        _wetness = Mathf.Clamp01(wetness);
    }

    public void SetWindChill(float chill)
    {
        _windChill = Mathf.Max(0f, chill);
    }

    public void SetNearFireBonus(float bonus)
    {
        _nearFireBonus = Mathf.Max(0f, bonus);
    }

    public bool IsComfortable()
    {
        return CurrentState == TemperatureState.Comfortable;
    }

    public bool IsDangerous()
    {
        return _currentBodyTemp < _hypothermiaThreshold ||
                _currentBodyTemp > _hyperthermiaThreshold;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}