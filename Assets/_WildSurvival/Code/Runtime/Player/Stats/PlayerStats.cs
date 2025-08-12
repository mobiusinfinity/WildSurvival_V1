using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// Manages all player stats including health, stamina, hunger, and thirst
/// </summary>
public class PlayerStats : MonoBehaviour
{
    #region Nested Types
    [System.Serializable]
    public class Stat
    {
        public float currentValue;
        public float maxValue;
        public float minValue = 0f;
        public float regenRate = 0f;
        public float depleteRate = 0f;

        public float Percentage => maxValue > 0 ? currentValue / maxValue : 0f;

        public Stat(float max, float current = -1)
        {
            maxValue = max;
            currentValue = current < 0 ? max : current;
        }

        public void Add(float amount)
        {
            currentValue = Mathf.Clamp(currentValue + amount, minValue, maxValue);
        }

        public void SetMax(float newMax)
        {
            float percentage = Percentage;
            maxValue = newMax;
            currentValue = maxValue * percentage;
        }
    }
    #endregion

    #region Fields
    [Header("Core Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;

    [Header("Level System")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int xpToNextLevel = 100;

    [Header("Health")]
    [SerializeField] private Stat health = new Stat(100f);
    [SerializeField] private float healthRegenDelay = 5f;
    private float healthRegenTimer;
    private float healthRegenModifier = 1f;

    [Header("Stamina")]
    [SerializeField] private Stat stamina = new Stat(100f);
    [SerializeField] private float staminaRegenDelay = 1f;
    [SerializeField] private float sprintStaminaCost = 10f;
    private float staminaRegenTimer;
    private float staminaRegenModifier = 1f;

    [Header("Hunger")]
    [SerializeField] private Stat hunger = new Stat(100f);
    [SerializeField] private float hungerDepleteRate = 1f;
    private float hungerRateModifier = 1f;

    [Header("Thirst")]
    [SerializeField] private Stat thirst = new Stat(100f);
    [SerializeField] private float thirstDepleteRate = 2f;
    private float thirstRateModifier = 1f;

    [Header("Temperature")]
    [SerializeField] private float bodyTemperature = 37f;
    [SerializeField] private float minSafeTemp = 35f;
    [SerializeField] private float maxSafeTemp = 39f;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool enableNaturalDepletion = true;
    private float updateTimer;

    // Events
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent<float> OnStaminaChanged;
    public UnityEvent<float> OnHungerChanged;
    public UnityEvent<float> OnThirstChanged;
    public UnityEvent OnDeath;
    public UnityEvent OnExhausted;
    public UnityEvent OnStarving;
    public UnityEvent OnDehydrated;

    // Static events for other systems
    public static event Action<float> OnHealthChangedStatic;
    public static event Action<float> OnStaminaChangedStatic;
    public static event Action OnPlayerDeath;
    #endregion

    #region Properties
    // THIS IS THE IMPORTANT ONE FOR CRAFTING MANAGER
    public int Level => currentLevel;

    // Health properties
    public float CurrentHealth => health.currentValue;
    public float MaxHealth => health.maxValue;
    public float HealthPercentage => health.Percentage;

    // Stamina properties
    public float CurrentStamina => stamina.currentValue;
    public float MaxStamina => stamina.maxValue;
    public float StaminaPercentage => stamina.Percentage;

    // Other properties
    public float CurrentHunger => hunger.currentValue;
    public float CurrentThirst => thirst.currentValue;
    public bool IsAlive => health.currentValue > 0;
    public float BodyTemperature => bodyTemperature;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeStats();
    }

    private void Update()
    {
        if (!IsAlive) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateStats();
        }

        UpdateRegen();
    }
    #endregion

    #region Initialization
    private void InitializeStats()
    {
        health.regenRate = 5f;
        stamina.regenRate = 20f;
        hunger.depleteRate = hungerDepleteRate;
        thirst.depleteRate = thirstDepleteRate;
    }
    #endregion

    #region Stats Update
    private void UpdateStats()
    {
        if (enableNaturalDepletion)
        {
            // Deplete hunger and thirst over time
            ModifyHunger(-hunger.depleteRate * hungerRateModifier * updateInterval);
            ModifyThirst(-thirst.depleteRate * thirstRateModifier * updateInterval);

            // Health affected by hunger and thirst
            if (hunger.currentValue <= 0 || thirst.currentValue <= 0)
            {
                ModifyHealth(-2f * updateInterval);
            }

            // Temperature affects stats
            if (bodyTemperature < minSafeTemp || bodyTemperature > maxSafeTemp)
            {
                ModifyStamina(-5f * updateInterval);
            }
        }
    }

    private void UpdateRegen()
    {
        // Health regeneration
        if (health.currentValue < health.maxValue && hunger.currentValue > 30 && thirst.currentValue > 30)
        {
            if (healthRegenTimer <= 0)
            {
                ModifyHealth(health.regenRate * healthRegenModifier * Time.deltaTime);
            }
            else
            {
                healthRegenTimer -= Time.deltaTime;
            }
        }

        // Stamina regeneration
        if (stamina.currentValue < stamina.maxValue)
        {
            if (staminaRegenTimer <= 0)
            {
                ModifyStamina(stamina.regenRate * staminaRegenModifier * Time.deltaTime);
            }
            else
            {
                staminaRegenTimer -= Time.deltaTime;
            }
        }
    }
    #endregion

    #region Public Modification Methods
    public void ModifyHealth(float amount)
    {
        float previous = health.currentValue;
        health.Add(amount);

        if (amount < 0)
            healthRegenTimer = healthRegenDelay;

        OnHealthChanged?.Invoke(health.currentValue);
        OnHealthChangedStatic?.Invoke(health.currentValue);

        if (health.currentValue <= 0 && previous > 0)
        {
            Die();
        }
    }

    public void ModifyStamina(float amount)
    {
        stamina.Add(amount);

        if (amount < 0)
            staminaRegenTimer = staminaRegenDelay;

        OnStaminaChanged?.Invoke(stamina.currentValue);
        OnStaminaChangedStatic?.Invoke(stamina.currentValue);

        if (stamina.currentValue <= 0)
        {
            OnExhausted?.Invoke();
        }
    }

    public void ModifyHunger(float amount)
    {
        hunger.Add(amount);
        OnHungerChanged?.Invoke(hunger.currentValue);

        if (hunger.currentValue <= 0)
        {
            OnStarving?.Invoke();
        }
    }

    public void ModifyThirst(float amount)
    {
        thirst.Add(amount);
        OnThirstChanged?.Invoke(thirst.currentValue);

        if (thirst.currentValue <= 0)
        {
            OnDehydrated?.Invoke();
        }
    }

    public void ConsumeStamina(float amount)
    {
        ModifyStamina(-Mathf.Abs(amount));
    }

    public bool HasStamina(float amount)
    {
        return stamina.currentValue >= amount;
    }

    public void SetBodyTemperature(float temp)
    {
        bodyTemperature = Mathf.Clamp(temp, 30f, 45f);
    }
    #endregion

    #region Modifier Methods
    public void ResetAllModifiers()
    {
        healthRegenModifier = 1f;
        staminaRegenModifier = 1f;
        hungerRateModifier = 1f;
        thirstRateModifier = 1f;
    }

    public void SetHealthRegenModifier(float modifier)
    {
        healthRegenModifier = Mathf.Clamp(modifier, 0f, 3f);
    }

    public void SetStaminaRegenModifier(float modifier)
    {
        staminaRegenModifier = Mathf.Clamp(modifier, 0f, 3f);
    }

    public void SetHungerRateModifier(float modifier)
    {
        hungerRateModifier = Mathf.Clamp(modifier, 0f, 3f);
    }

    public void SetThirstRateModifier(float modifier)
    {
        thirstRateModifier = Mathf.Clamp(modifier, 0f, 3f);
    }
    #endregion

    #region Death Handling
    private void Die()
    {
        OnDeath?.Invoke();
        OnPlayerDeath?.Invoke();
        Debug.Log("[PlayerStats] Player has died!");
        // Additional death logic here
    }
    #endregion
}