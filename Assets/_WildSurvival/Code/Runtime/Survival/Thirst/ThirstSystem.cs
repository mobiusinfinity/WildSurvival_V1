using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Thirst system managing hydration levels and dehydration effects
/// Works alongside HungerSystem and TemperatureSystem
/// </summary>
public class ThirstSystem : MonoBehaviour
{
    [System.Serializable]
    public class ThirstConfig
    {
        [Header("Base Values")]
        public float MaxHydration = 100f;
        public float DehydrationRate = 2f; // Units per hour
        public float CriticalThreshold = 20f;
        public float DangerThreshold = 10f;

        [Header("Activity Multipliers")]
        public float RestingMultiplier = 1f;
        public float WalkingMultiplier = 1.5f;
        public float RunningMultiplier = 2.5f;
        public float CombatMultiplier = 2f;

        [Header("Environmental Factors")]
        public float HotWeatherMultiplier = 1.8f;
        public float ColdWeatherMultiplier = 0.9f;
        public float RainBonus = 0.5f; // Slower dehydration in rain

        [Header("Consumption")]
        public float DrinkingSpeed = 20f; // Units per second when drinking
        public float StomachCapacity = 40f; // Max drink at once
    }

    [System.Serializable]
    public class DrinkItem
    {
        public string Name;
        public float HydrationValue;
        public float Quality; // 0-1, affects effectiveness
        public DrinkType Type;
        public List<StatusEffect> Effects;

        public enum DrinkType
        {
            Water,
            Juice,
            Tea,
            Coffee,
            Alcohol,
            Contaminated
        }
    }

    [System.Serializable]
    public class StatusEffect
    {
        public string Name;
        public float Duration;
        public float HydrationModifier = 1f;
        public float HealthModifier = 0f;
        public bool CausesNausea = false;
    }

    public enum ThirstLevel
    {
        Hydrated,      // > 80%
        Normal,        // 60-80%
        Thirsty,       // 40-60%
        Parched,       // 20-40%
        Dehydrated,    // 10-20%
        Critical       // < 10%
    }

    public enum ActivityLevel
    {
        Sleeping,
        Resting,
        Walking,
        Running,
        Working,
        Fighting
    }

    [Header("Configuration")]
    [SerializeField] private ThirstConfig config = new ThirstConfig();

    [Header("Current State")]
    [SerializeField] private float currentHydration;
    [SerializeField] private ThirstLevel currentLevel;
    [SerializeField] private ActivityLevel currentActivity = ActivityLevel.Resting;
    [SerializeField] private float stomachWater = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private float dehydrationRateDisplay;

    // Events
    public UnityEvent<float> OnHydrationChanged = new();
    public UnityEvent<ThirstLevel> OnThirstLevelChanged = new();
    public UnityEvent OnDehydrationStarted = new();
    public UnityEvent OnRehydrated = new();
    public UnityEvent<DrinkItem> OnDrink = new();
    public UnityEvent OnThirstDamage = new();

    // State
    private bool isDehydrated = false;
    private float timeSinceLastDrink = 0f;
    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    private Queue<float> hydrationQueue = new Queue<float>();

    // Dependencies
    private PlayerStats playerStats;
    private TemperatureSystem temperatureSystem;
    private HungerSystem hungerSystem;
    private NotificationSystem notifications;

    // Singleton
    private static ThirstSystem instance;
    public static ThirstSystem Instance => instance;

    // Properties
    public float CurrentHydration => currentHydration;
    public float HydrationPercentage => currentHydration / config.MaxHydration;
    public ThirstLevel Level => currentLevel;
    public bool IsDehydrated => isDehydrated;
    public bool IsCritical => currentHydration < config.DangerThreshold;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Initialize();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        currentHydration = config.MaxHydration * 0.8f; // Start at 80%
        UpdateThirstLevel();

        // Find dependencies
        playerStats = FindObjectOfType<PlayerStats>();
        temperatureSystem = TemperatureSystem.Instance;
        hungerSystem = FindObjectOfType<HungerSystem>();
        notifications = NotificationSystem.Instance;
    }

    private void Update()
    {
        // Process dehydration
        ProcessDehydration(Time.deltaTime);

        // Process stomach water absorption
        ProcessHydrationQueue(Time.deltaTime);

        // Update thirst level
        UpdateThirstLevel();

        // Apply dehydration effects
        if (isDehydrated)
        {
            ApplyDehydrationEffects(Time.deltaTime);
        }

        // Update timers
        timeSinceLastDrink += Time.deltaTime;

        // Update debug display
        if (showDebugInfo)
        {
            dehydrationRateDisplay = CalculateDehydrationRate();
        }
    }

    private void ProcessDehydration(float deltaTime)
    {
        float rate = CalculateDehydrationRate();

        // Convert from per hour to per second
        float dehydrationAmount = rate * (deltaTime / 3600f);

        currentHydration = Mathf.Max(0f, currentHydration - dehydrationAmount);
        OnHydrationChanged?.Invoke(currentHydration);
    }

    private float CalculateDehydrationRate()
    {
        float rate = config.DehydrationRate;

        // Apply activity modifier
        rate *= GetActivityMultiplier();

        // Apply temperature modifier
        if (temperatureSystem != null)
        {
            var tempState = temperatureSystem.CurrentState;
            if (tempState == TemperatureSystem.TemperatureState.Hot ||
                tempState == TemperatureSystem.TemperatureState.Overheating)
            {
                rate *= config.HotWeatherMultiplier;
            }
            else if (tempState == TemperatureSystem.TemperatureState.Cold ||
                        tempState == TemperatureSystem.TemperatureState.Freezing)
            {
                rate *= config.ColdWeatherMultiplier;
            }
        }

        // Apply status effects
        foreach (var effect in activeEffects)
        {
            rate *= effect.HydrationModifier;
        }

        // Hunger affects thirst
        if (hungerSystem != null && hungerSystem.IsStarving)
        {
            rate *= 1.2f; // 20% faster dehydration when starving
        }

        return rate;
    }

    private float GetActivityMultiplier()
    {
        return currentActivity switch
        {
            ActivityLevel.Sleeping => 0.8f,
            ActivityLevel.Resting => config.RestingMultiplier,
            ActivityLevel.Walking => config.WalkingMultiplier,
            ActivityLevel.Running => config.RunningMultiplier,
            ActivityLevel.Working => 1.8f,
            ActivityLevel.Fighting => config.CombatMultiplier,
            _ => 1f
        };
    }

    private void ProcessHydrationQueue(float deltaTime)
    {
        if (hydrationQueue.Count == 0) return;

        float absorptionRate = 10f; // Units per second
        float toAbsorb = absorptionRate * deltaTime;

        while (hydrationQueue.Count > 0 && toAbsorb > 0)
        {
            float queued = hydrationQueue.Peek();
            float absorbed = Mathf.Min(toAbsorb, queued);

            currentHydration = Mathf.Min(config.MaxHydration, currentHydration + absorbed);
            toAbsorb -= absorbed;

            if (absorbed >= queued)
            {
                hydrationQueue.Dequeue();
            }
            else
            {
                // Update remaining amount
                hydrationQueue.Dequeue();
                hydrationQueue.Enqueue(queued - absorbed);
                break;
            }
        }
    }

    private void UpdateThirstLevel()
    {
        var previousLevel = currentLevel;
        float percentage = HydrationPercentage;

        currentLevel = percentage switch
        {
            > 0.8f => ThirstLevel.Hydrated,
            > 0.6f => ThirstLevel.Normal,
            > 0.4f => ThirstLevel.Thirsty,
            > 0.2f => ThirstLevel.Parched,
            > 0.1f => ThirstLevel.Dehydrated,
            _ => ThirstLevel.Critical
        };

        if (currentLevel != previousLevel)
        {
            OnThirstLevelChanged?.Invoke(currentLevel);

            // Handle dehydration state
            bool wasDehydrated = isDehydrated;
            isDehydrated = currentHydration < config.CriticalThreshold;

            if (isDehydrated && !wasDehydrated)
            {
                OnDehydrationStarted?.Invoke();
                ShowNotification("You are becoming dehydrated!", NotificationSystem.NotificationType.Warning);
            }
            else if (!isDehydrated && wasDehydrated)
            {
                OnRehydrated?.Invoke();
                ShowNotification("You feel rehydrated.", NotificationSystem.NotificationType.Success);
            }

            // Level change notifications
            switch (currentLevel)
            {
                case ThirstLevel.Critical:
                    ShowNotification("CRITICAL: You desperately need water!", NotificationSystem.NotificationType.Critical);
                    break;
                case ThirstLevel.Dehydrated:
                    ShowNotification("You are severely dehydrated!", NotificationSystem.NotificationType.Warning);
                    break;
                case ThirstLevel.Parched:
                    ShowNotification("Your throat is parched...", NotificationSystem.NotificationType.Info);
                    break;
            }
        }
    }

    private void ApplyDehydrationEffects(float deltaTime)
    {
        if (playerStats == null) return;

        // Damage increases as hydration approaches zero
        if (currentHydration < config.DangerThreshold)
        {
            float damageMultiplier = 1f - (currentHydration / config.DangerThreshold);
            float damage = 3f * damageMultiplier * (deltaTime / 60f); // Up to 3 HP/minute

            playerStats.ModifyHealth(-damage);

            // Periodic damage notification
            if (UnityEngine.Random.value < 0.005f) // 0.5% chance per frame
            {
                OnThirstDamage?.Invoke();
                ShowNotification("You're dying of thirst!", NotificationSystem.NotificationType.Critical);
            }
        }

        // Stamina reduction when thirsty
        if (currentLevel >= ThirstLevel.Thirsty)
        {
            playerStats.ModifyStamina(-deltaTime * 2f); // Drain stamina
        }
    }

    public bool TryDrink(DrinkItem drink)
    {
        // Check stomach capacity
        if (stomachWater >= config.StomachCapacity)
        {
            ShowNotification("You can't drink any more right now!", NotificationSystem.NotificationType.Warning);
            return false;
        }

        // Add to hydration queue for gradual absorption
        float effectiveHydration = drink.HydrationValue * drink.Quality;
        hydrationQueue.Enqueue(effectiveHydration);
        stomachWater += drink.HydrationValue;

        // Apply immediate effects
        if (drink.Effects != null)
        {
            foreach (var effect in drink.Effects)
            {
                activeEffects.Add(effect);

                if (effect.HealthModifier != 0 && playerStats != null)
                {
                    playerStats.ModifyHealth(effect.HealthModifier);
                }

                if (effect.CausesNausea)
                {
                    ShowNotification("You feel nauseous...", NotificationSystem.NotificationType.Warning);
                }
            }
        }

        // Special handling for different drink types
        switch (drink.Type)
        {
            case DrinkItem.DrinkType.Contaminated:
                ShowNotification($"The {drink.Name} tastes awful!", NotificationSystem.NotificationType.Warning);
                break;
            case DrinkItem.DrinkType.Coffee:
                ShowNotification($"The {drink.Name} gives you energy!", NotificationSystem.NotificationType.Info);
                if (playerStats != null) playerStats.ModifyStamina(20f);
                break;
            case DrinkItem.DrinkType.Alcohol:
                ShowNotification($"The {drink.Name} makes you feel warm.", NotificationSystem.NotificationType.Info);
                break;
            default:
                ShowNotification($"You drink {drink.Name} (+{effectiveHydration:F0} hydration)", NotificationSystem.NotificationType.Info);
                break;
        }

        timeSinceLastDrink = 0f;
        OnDrink?.Invoke(drink);

        // Start stomach processing coroutine
        StartCoroutine(ProcessStomachWater());

        return true;
    }

    private System.Collections.IEnumerator ProcessStomachWater()
    {
        yield return new WaitForSeconds(30f); // 30 seconds to process
        stomachWater = Mathf.Max(0f, stomachWater - 10f);
    }

    public void DrinkFromSource(float amount, float quality = 1f)
    {
        var drink = new DrinkItem
        {
            Name = "water",
            HydrationValue = amount,
            Quality = quality,
            Type = quality < 0.5f ? DrinkItem.DrinkType.Contaminated : DrinkItem.DrinkType.Water
        };

        TryDrink(drink);
    }

    public void SetActivity(ActivityLevel activity)
    {
        currentActivity = activity;
    }

    private void ShowNotification(string message, NotificationSystem.NotificationType type)
    {
        if (notifications != null)
        {
            notifications.ShowNotification(message, type);
        }
        else
        {
            Debug.Log($"[Thirst] {type}: {message}");
        }
    }

    // Save/Load support
    [System.Serializable]
    public class ThirstSaveData
    {
        public float CurrentHydration;
        public float StomachWater;
        public float TimeSinceLastDrink;
        public ActivityLevel CurrentActivity;
    }

    public ThirstSaveData GetSaveData()
    {
        return new ThirstSaveData
        {
            CurrentHydration = currentHydration,
            StomachWater = stomachWater,
            TimeSinceLastDrink = timeSinceLastDrink,
            CurrentActivity = currentActivity
        };
    }

    public void LoadSaveData(ThirstSaveData data)
    {
        currentHydration = data.CurrentHydration;
        stomachWater = data.StomachWater;
        timeSinceLastDrink = data.TimeSinceLastDrink;
        currentActivity = data.CurrentActivity;
        UpdateThirstLevel();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUI.Box(new Rect(10, 200, 200, 100), "Thirst System");
        GUI.Label(new Rect(15, 220, 190, 20), $"Hydration: {currentHydration:F1}/{config.MaxHydration}");
        GUI.Label(new Rect(15, 240, 190, 20), $"Level: {currentLevel}");
        GUI.Label(new Rect(15, 260, 190, 20), $"Dehydration: {dehydrationRateDisplay:F2}/hr");
        GUI.Label(new Rect(15, 280, 190, 20), $"Activity: {currentActivity}");
    }
#endif
}