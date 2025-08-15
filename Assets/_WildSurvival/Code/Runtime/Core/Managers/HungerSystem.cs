using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Add these temporary interfaces if your project doesn't have them yet
public interface ITickable
{
    void Tick(float deltaTime);
}

/// <summary>
/// Hunger system with realistic metabolism modeling - FIXED VERSION
/// Now handles missing dependencies gracefully
/// </summary>
public class HungerSystem : MonoBehaviour, ITickable
{
    [System.Serializable]
    public class HungerConfig
    {
        public float MaxCalories = 2000f;
        public float BaseMetabolicRate = 1.2f;
        public float StarvationThreshold = 200f;
        public float CriticalThreshold = 0f;

        public float WalkingMultiplier = 1.5f;
        public float RunningMultiplier = 3.0f;
        public float ChoppingMultiplier = 2.5f;
        public float SwimmingMultiplier = 4.0f;
        public float SleepingMultiplier = 0.8f;

        public float ColdMultiplier = 1.3f;
        public float HotMultiplier = 1.1f;

        public float DigestionRate = 10f;
        public float MaxStomachCapacity = 500f;
    }

    public UnityEvent<float> OnHungerChanged = new();
    public UnityEvent<HungerLevel> OnHungerLevelChanged = new();
    public UnityEvent OnStarvationStarted = new();
    public UnityEvent OnStarvationEnded = new();
    public UnityEvent<FoodItem> OnFoodConsumed = new();
    public UnityEvent OnStomachFull = new();

    public enum HungerLevel
    {
        Stuffed, Full, Satisfied, Peckish, Hungry, Starving
    }

    public enum ActivityLevel
    {
        Sleeping, Resting, Walking, Working, Running, Swimming
    }

    [System.Serializable]
    public class FoodItem
    {
        public string Name;
        public float Calories;
        public float Volume;
        public float DigestionTime;
        public FoodType Type;
        public List<StatusEffect> Effects;

        public enum FoodType
        {
            Raw, Cooked, Preserved, Liquid, Spoiled
        }
    }

    [System.Serializable]
    public class StatusEffect
    {
        public string Name;
        public float Duration;
        public float MetabolismModifier = 1f;
        public float HealthModifier = 0f;
    }

    [Header("Configuration")]
    [SerializeField] private HungerConfig _config = new HungerConfig();

    [Header("Debug View")]
    [SerializeField] private float _currentCalories;
    [SerializeField] private float _stomachContents;
    [SerializeField] private HungerLevel _currentLevel;

    private ActivityLevel _currentActivity = ActivityLevel.Resting;
    private bool _isStarving;
    private float _timeSinceLastMeal;

    private readonly Queue<DigestingFood> _digestionQueue = new();
    private readonly List<StatusEffect> _activeEffects = new();

    private struct DigestingFood
    {
        public FoodItem Food;
        public float RemainingCalories;
        public float DigestProgress;
    }

    private float _updateTimer;
    private const float UPDATE_INTERVAL = 1f;

    // Optional dependencies - will work without them
    private PlayerStats _playerStats;
    private TemperatureSystem _temperature;
    private NotificationSystem _notifications;

    public float CurrentCalories => _currentCalories;
    public float CaloriePercentage => _currentCalories / _config.MaxCalories;
    public HungerLevel Level => _currentLevel;
    public bool IsStarving => _isStarving;
    public float StomachFullness => _stomachContents / _config.MaxStomachCapacity;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_config == null)
            _config = new HungerConfig();

        _currentCalories = _config.MaxCalories * 0.75f;
        UpdateHungerLevel();

        // Try to get dependencies, but don't fail if they're missing
        TryGetDependencies();
    }

    private void TryGetDependencies()
    {
        // Try to find PlayerStats
        _playerStats = FindObjectOfType<PlayerStats>();
        if (_playerStats == null)
        {
            Debug.LogWarning("[HungerSystem] PlayerStats not found - health effects disabled");
        }

        // Try to find TemperatureSystem
        _temperature = FindObjectOfType<TemperatureSystem>();
        if (_temperature == null)
        {
            Debug.LogWarning("[HungerSystem] TemperatureSystem not found - temperature effects disabled");
        }

        // Try to find NotificationSystem
        _notifications = FindObjectOfType<NotificationSystem>();
        if (_notifications == null)
        {
            Debug.LogWarning("[HungerSystem] NotificationSystem not found - notifications disabled");
        }
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        _updateTimer += deltaTime;
        if (_updateTimer < UPDATE_INTERVAL)
            return;

        _updateTimer = 0f;

        ProcessMetabolism(UPDATE_INTERVAL);
        ProcessDigestion(UPDATE_INTERVAL);
        UpdateHungerLevel();

        if (_isStarving)
            ApplyStarvationDamage(UPDATE_INTERVAL);

        _timeSinceLastMeal += UPDATE_INTERVAL;
    }

    private void ProcessMetabolism(float deltaTime)
    {
        float metabolicRate = _config.BaseMetabolicRate;

        metabolicRate *= GetActivityMultiplier();

        // Apply temperature modifier if system exists
        if (_temperature != null)
        {
            var temp = _temperature.CurrentTemperature;
            if (temp < 10f)
                metabolicRate *= _config.ColdMultiplier;
            else if (temp > 30f)
                metabolicRate *= _config.HotMultiplier;
        }

        foreach (var effect in _activeEffects)
            metabolicRate *= effect.MetabolismModifier;

        float caloriesBurned = metabolicRate * (deltaTime / 60f);
        _currentCalories = Mathf.Max(0, _currentCalories - caloriesBurned);

        OnHungerChanged?.Invoke(_currentCalories);
    }

    private void ProcessDigestion(float deltaTime)
    {
        if (_digestionQueue.Count == 0)
            return;

        float digestionAmount = _config.DigestionRate * (deltaTime / 60f);

        while (_digestionQueue.Count > 0 && digestionAmount > 0)
        {
            var food = _digestionQueue.Peek();

            float toDigest = Mathf.Min(digestionAmount, food.RemainingCalories);
            food.RemainingCalories -= toDigest;
            food.DigestProgress += toDigest;

            _currentCalories = Mathf.Min(_config.MaxCalories, _currentCalories + toDigest);
            digestionAmount -= toDigest;

            float volumeReduction = (toDigest / food.Food.Calories) * food.Food.Volume;
            _stomachContents = Mathf.Max(0, _stomachContents - volumeReduction);

            if (food.RemainingCalories <= 0)
            {
                _digestionQueue.Dequeue();
                _activeEffects.RemoveAll(e => e.Duration <= 0);
            }
            else
            {
                _digestionQueue.Dequeue();
                _digestionQueue.Enqueue(food);
                break;
            }
        }
    }

    public bool TryEatFood(FoodItem food)
    {
        if (_stomachContents + food.Volume > _config.MaxStomachCapacity)
        {
            ShowNotification("Too full to eat!", NotificationSystem.NotificationType.Warning);
            OnStomachFull?.Invoke();
            return false;
        }

        if (food.Type == FoodItem.FoodType.Spoiled)
        {
            ShowNotification("Food is spoiled!", NotificationSystem.NotificationType.Warning);
        }

        _digestionQueue.Enqueue(new DigestingFood
        {
            Food = food,
            RemainingCalories = food.Calories,
            DigestProgress = 0f
        });

        _stomachContents += food.Volume;

        if (food.Effects != null)
        {
            foreach (var effect in food.Effects)
            {
                _activeEffects.Add(effect);

                if (effect.HealthModifier != 0 && _playerStats != null)
                {
                    _playerStats.ModifyHealth(effect.HealthModifier);
                }
            }
        }

        _timeSinceLastMeal = 0f;
        OnFoodConsumed?.Invoke(food);

        string message = food.Type == FoodItem.FoodType.Cooked
            ? $"Enjoyed {food.Name} (+{food.Calories} cal)"
            : $"Ate {food.Name} (+{food.Calories} cal)";
        ShowNotification(message, NotificationSystem.NotificationType.Info);

        return true;
    }

    private void UpdateHungerLevel()
    {
        var previousLevel = _currentLevel;
        float percentage = CaloriePercentage;

        _currentLevel = percentage switch
        {
            > 0.9f => HungerLevel.Stuffed,
            > 0.7f => HungerLevel.Full,
            > 0.5f => HungerLevel.Satisfied,
            > 0.3f => HungerLevel.Peckish,
            > 0.15f => HungerLevel.Hungry,
            _ => HungerLevel.Starving
        };

        if (_currentLevel != previousLevel)
        {
            OnHungerLevelChanged?.Invoke(_currentLevel);

            bool wasStarving = _isStarving;
            _isStarving = _currentCalories < _config.StarvationThreshold;

            if (_isStarving && !wasStarving)
            {
                OnStarvationStarted?.Invoke();
                ShowNotification("You are starving!", NotificationSystem.NotificationType.Critical);
            }
            else if (!_isStarving && wasStarving)
            {
                OnStarvationEnded?.Invoke();
            }
        }
    }

    private void ApplyStarvationDamage(float deltaTime)
    {
        if (_playerStats == null)
        {
            // Can't apply damage without PlayerStats
            return;
        }

        float damageMultiplier = 1f - (_currentCalories / _config.StarvationThreshold);
        float damage = 2f * damageMultiplier * (deltaTime / 60f);

        _playerStats.ModifyHealth(-damage);

        if (UnityEngine.Random.value < 0.01f)
        {
            string[] messages =
            {
                "Your stomach growls painfully...",
                "You feel weak from hunger...",
                "Your vision blurs from lack of food..."
            };
            ShowNotification(
                messages[UnityEngine.Random.Range(0, messages.Length)],
                NotificationSystem.NotificationType.Warning
            );
        }
    }

    private void ShowNotification(string message, NotificationSystem.NotificationType fireType)
    {
        if (_notifications != null)
        {
            _notifications.ShowNotification(message, fireType);
        }
        else
        {
            // Fallback to console if no notification system
            Debug.Log($"[Hunger] {fireType}: {message}");
        }
    }

    public void SetActivity(ActivityLevel activity)
    {
        _currentActivity = activity;
    }

    private float GetActivityMultiplier()
    {
        return _currentActivity switch
        {
            ActivityLevel.Sleeping => _config.SleepingMultiplier,
            ActivityLevel.Walking => _config.WalkingMultiplier,
            ActivityLevel.Working => _config.ChoppingMultiplier,
            ActivityLevel.Running => _config.RunningMultiplier,
            ActivityLevel.Swimming => _config.SwimmingMultiplier,
            _ => 1f
        };
    }

    [System.Serializable]
    public class HungerSaveData
    {
        public float CurrentCalories;
        public float StomachContents;
        public float TimeSinceLastMeal;
        public ActivityLevel CurrentActivity;
    }

    public HungerSaveData GetSaveData()
    {
        return new HungerSaveData
        {
            CurrentCalories = _currentCalories,
            StomachContents = _stomachContents,
            TimeSinceLastMeal = _timeSinceLastMeal,
            CurrentActivity = _currentActivity
        };
    }

    public void LoadSaveData(HungerSaveData data)
    {
        _currentCalories = data.CurrentCalories;
        _stomachContents = data.StomachContents;
        _timeSinceLastMeal = data.TimeSinceLastMeal;
        _currentActivity = data.CurrentActivity;
        UpdateHungerLevel();
    }
}