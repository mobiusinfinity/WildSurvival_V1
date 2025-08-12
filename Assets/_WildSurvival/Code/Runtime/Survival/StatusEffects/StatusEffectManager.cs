using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages all status effects, buffs, debuffs, and conditions
/// Central system for temporary and permanent modifiers
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    [System.Serializable]
    public class StatusEffect
    {
        public string id;
        public string displayName;
        public string description;
        public Sprite icon;
        public EffectType type;
        public float duration; // -1 for permanent
        public float tickInterval = 1f; // How often to apply effect
        public bool isStackable = false;
        public int maxStacks = 1;
        public int currentStacks = 1;
        public float timeRemaining;
        public float nextTickTime;

        // Modifiers
        public StatModifiers modifiers = new StatModifiers();

        // Damage/Healing over time
        public float healthPerTick = 0f;
        public float staminaPerTick = 0f;
        public float hungerPerTick = 0f;
        public float thirstPerTick = 0f;

        // Special effects
        public bool causesBleeding = false;
        public bool causesPoisoning = false;
        public bool causesInfection = false;
        public bool preventsSprinting = false;
        public bool preventsCrafting = false;
        public bool blursVision = false;

        // Visual/Audio
        public Color screenTintColor = Color.clear;
        public float screenTintIntensity = 0f;
        public AudioClip applySound;
        public AudioClip removeSound;
        public GameObject visualEffectPrefab;

        public enum EffectType
        {
            Buff,
            Debuff,
            Neutral,
            Disease,
            Injury,
            Environmental
        }
    }

    [System.Serializable]
    public class StatModifiers
    {
        [Range(-1f, 2f)] public float healthMultiplier = 1f;
        [Range(-1f, 2f)] public float staminaMultiplier = 1f;
        [Range(-1f, 2f)] public float speedMultiplier = 1f;
        [Range(-1f, 2f)] public float damageMultiplier = 1f;
        [Range(-1f, 2f)] public float defenseMultiplier = 1f;
        [Range(-1f, 2f)] public float hungerRateMultiplier = 1f;
        [Range(-1f, 2f)] public float thirstRateMultiplier = 1f;
        [Range(-1f, 2f)] public float temperatureResistance = 1f;

        public float healthFlat = 0f;
        public float staminaFlat = 0f;
        public float speedFlat = 0f;
        public float damageFlat = 0f;
        public float defenseFlat = 0f;
    }

    [Header("Active Effects")]
    [SerializeField] private List<StatusEffect> activeEffects = new List<StatusEffect>();

    [Header("Effect Database")]
    [SerializeField] private List<StatusEffect> effectTemplates = new List<StatusEffect>();

    [Header("UI")]
    [SerializeField] private Transform effectIconContainer;
    [SerializeField] private GameObject effectIconPrefab;

    [Header("Settings")]
    [SerializeField] private int maxActiveEffects = 20;
    [SerializeField] private bool allowDuplicateEffects = false;
    [SerializeField] private bool showDebugInfo = false;

    // Events
    public UnityEvent<StatusEffect> OnEffectAdded = new UnityEvent<StatusEffect>();
    public UnityEvent<StatusEffect> OnEffectRemoved = new UnityEvent<StatusEffect>();
    public UnityEvent<StatusEffect> OnEffectExpired = new UnityEvent<StatusEffect>();
    public UnityEvent<StatusEffect> OnEffectStacked = new UnityEvent<StatusEffect>();
    public UnityEvent<StatModifiers> OnModifiersChanged = new UnityEvent<StatModifiers>();

    // Calculated modifiers
    private StatModifiers combinedModifiers = new StatModifiers();

    // Dependencies
    private PlayerStats playerStats;
    private HungerSystem hungerSystem;
    private ThirstSystem thirstSystem;
    private TemperatureSystem temperatureSystem;
    private NotificationSystem notifications;

    // Effect icons
    private Dictionary<StatusEffect, GameObject> effectIcons = new Dictionary<StatusEffect, GameObject>();

    // Singleton
    private static StatusEffectManager instance;
    public static StatusEffectManager Instance => instance;

    // Properties
    public List<StatusEffect> ActiveEffects => new List<StatusEffect>(activeEffects);
    public StatModifiers CombinedModifiers => combinedModifiers;
    public bool HasDebuff => activeEffects.Any(e => e.type == StatusEffect.EffectType.Debuff);
    public bool HasBuff => activeEffects.Any(e => e.type == StatusEffect.EffectType.Buff);
    public bool IsIncapacitated => activeEffects.Any(e => e.preventsSprinting && e.preventsCrafting);

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
        // Find dependencies
        playerStats = FindObjectOfType<PlayerStats>();
        hungerSystem = FindObjectOfType<HungerSystem>();
        thirstSystem = ThirstSystem.Instance;
        temperatureSystem = TemperatureSystem.Instance;
        notifications = NotificationSystem.Instance;

        // Initialize default effects if needed
        if (effectTemplates.Count == 0)
        {
            CreateDefaultEffects();
        }
    }

    private void Update()
    {
        // Update all active effects
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];

            // Update duration
            if (effect.duration > 0)
            {
                effect.timeRemaining -= Time.deltaTime;

                if (effect.timeRemaining <= 0)
                {
                    RemoveEffect(effect);
                    continue;
                }
            }

            // Apply tick effects
            if (Time.time >= effect.nextTickTime)
            {
                ApplyTickEffect(effect);
                effect.nextTickTime = Time.time + effect.tickInterval;
            }
        }

        // Update combined modifiers
        RecalculateModifiers();
    }

    public bool AddEffect(string effectId, int stacks = 1)
    {
        // Find template
        var template = effectTemplates.FirstOrDefault(e => e.id == effectId);
        if (template == null)
        {
            Debug.LogError($"StatusEffect {effectId} not found in templates!");
            return false;
        }

        // Check if already active
        var existing = activeEffects.FirstOrDefault(e => e.id == effectId);
        if (existing != null)
        {
            if (template.isStackable && existing.currentStacks < existing.maxStacks)
            {
                existing.currentStacks = Mathf.Min(existing.currentStacks + stacks, existing.maxStacks);
                existing.timeRemaining = template.duration; // Refresh duration
                OnEffectStacked?.Invoke(existing);
                ShowNotification($"{existing.displayName} stacked ({existing.currentStacks}x)", NotificationType.Info);
                return true;
            }
            else if (!allowDuplicateEffects)
            {
                // Refresh duration only
                existing.timeRemaining = template.duration;
                return true;
            }
        }

        // Check max effects
        if (activeEffects.Count >= maxActiveEffects)
        {
            ShowNotification("Too many active effects!", NotificationType.Warning);
            return false;
        }

        // Create new instance
        var newEffect = CreateEffectInstance(template);
        newEffect.currentStacks = Mathf.Min(stacks, newEffect.maxStacks);
        newEffect.timeRemaining = newEffect.duration;
        newEffect.nextTickTime = Time.time + newEffect.tickInterval;

        // Add to active
        activeEffects.Add(newEffect);

        // Create UI icon
        if (effectIconContainer != null && effectIconPrefab != null)
        {
            CreateEffectIcon(newEffect);
        }

        // Apply immediate effects
        ApplyImmediateEffect(newEffect);

        // Fire event
        OnEffectAdded?.Invoke(newEffect);

        // Notification
        string message = newEffect.type == StatusEffect.EffectType.Buff ?
            $"+ {newEffect.displayName}" :
            $"- {newEffect.displayName}";
        var notifType = newEffect.type == StatusEffect.EffectType.Buff ?
            NotificationType.Success :
            NotificationType.Warning;
        ShowNotification(message, notifType);

        // Play sound
        if (newEffect.applySound != null)
        {
            AudioSource.PlayClipAtPoint(newEffect.applySound, transform.position);
        }

        return true;
    }

    public void RemoveEffect(StatusEffect effect)
    {
        if (!activeEffects.Contains(effect)) return;

        // Remove from active
        activeEffects.Remove(effect);

        // Remove UI icon
        if (effectIcons.ContainsKey(effect))
        {
            Destroy(effectIcons[effect]);
            effectIcons.Remove(effect);
        }

        // Apply removal effects
        ApplyRemovalEffect(effect);

        // Fire event
        OnEffectRemoved?.Invoke(effect);

        // Play sound
        if (effect.removeSound != null)
        {
            AudioSource.PlayClipAtPoint(effect.removeSound, transform.position);
        }

        ShowNotification($"{effect.displayName} removed", NotificationType.Info);
    }

    public void RemoveEffectById(string effectId)
    {
        var effect = activeEffects.FirstOrDefault(e => e.id == effectId);
        if (effect != null)
        {
            RemoveEffect(effect);
        }
    }

    public void RemoveAllDebuffs()
    {
        var debuffs = activeEffects.Where(e => e.type == StatusEffect.EffectType.Debuff).ToList();
        foreach (var debuff in debuffs)
        {
            RemoveEffect(debuff);
        }
    }

    public void RemoveAllEffects()
    {
        while (activeEffects.Count > 0)
        {
            RemoveEffect(activeEffects[0]);
        }
    }

    private void ApplyTickEffect(StatusEffect effect)
    {
        // Apply health/stamina changes
        if (effect.healthPerTick != 0 && playerStats != null)
        {
            playerStats.ModifyHealth(effect.healthPerTick * effect.currentStacks);
        }

        if (effect.staminaPerTick != 0 && playerStats != null)
        {
            playerStats.ModifyStamina(effect.staminaPerTick * effect.currentStacks);
        }

        if (effect.hungerPerTick != 0 && hungerSystem != null)
        {
            // Negative values increase hunger (drain calories)
            if (effect.hungerPerTick < 0)
            {
                hungerSystem.SetActivity(HungerSystem.ActivityLevel.Working);
            }
        }

        if (effect.thirstPerTick != 0 && thirstSystem != null)
        {
            if (effect.thirstPerTick > 0)
            {
                thirstSystem.DrinkFromSource(effect.thirstPerTick, 1f);
            }
            else
            {
                thirstSystem.SetActivity(ThirstSystem.ActivityLevel.Working);
            }
        }
    }

    private void ApplyImmediateEffect(StatusEffect effect)
    {
        // Apply any immediate changes
        if (effect.causesBleeding)
        {
            AddEffect("bleeding", 1);
        }

        if (effect.causesPoisoning)
        {
            AddEffect("poisoned", 1);
        }

        if (effect.causesInfection)
        {
            AddEffect("infected", 1);
        }
    }

    private void ApplyRemovalEffect(StatusEffect effect)
    {
        // Clean up any special effects
        if (effect.causesBleeding)
        {
            RemoveEffectById("bleeding");
        }
    }

    private void RecalculateModifiers()
    {
        // Reset modifiers
        combinedModifiers = new StatModifiers();

        // Combine all active effects
        foreach (var effect in activeEffects)
        {
            var mod = effect.modifiers;
            int stacks = effect.currentStacks;

            // Multiplicative modifiers (compound)
            combinedModifiers.healthMultiplier *= Mathf.Pow(mod.healthMultiplier, stacks);
            combinedModifiers.staminaMultiplier *= Mathf.Pow(mod.staminaMultiplier, stacks);
            combinedModifiers.speedMultiplier *= Mathf.Pow(mod.speedMultiplier, stacks);
            combinedModifiers.damageMultiplier *= Mathf.Pow(mod.damageMultiplier, stacks);
            combinedModifiers.defenseMultiplier *= Mathf.Pow(mod.defenseMultiplier, stacks);
            combinedModifiers.hungerRateMultiplier *= Mathf.Pow(mod.hungerRateMultiplier, stacks);
            combinedModifiers.thirstRateMultiplier *= Mathf.Pow(mod.thirstRateMultiplier, stacks);
            combinedModifiers.temperatureResistance *= Mathf.Pow(mod.temperatureResistance, stacks);

            // Flat modifiers (additive)
            combinedModifiers.healthFlat += mod.healthFlat * stacks;
            combinedModifiers.staminaFlat += mod.staminaFlat * stacks;
            combinedModifiers.speedFlat += mod.speedFlat * stacks;
            combinedModifiers.damageFlat += mod.damageFlat * stacks;
            combinedModifiers.defenseFlat += mod.defenseFlat * stacks;
        }

        OnModifiersChanged?.Invoke(combinedModifiers);
    }

    private StatusEffect CreateEffectInstance(StatusEffect template)
    {
        // Deep copy the template
        var json = JsonUtility.ToJson(template);
        var instance = JsonUtility.FromJson<StatusEffect>(json);
        return instance;
    }

    private void CreateEffectIcon(StatusEffect effect)
    {
        if (effect.icon == null) return;

        var iconObj = Instantiate(effectIconPrefab, effectIconContainer);
        var image = iconObj.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.sprite = effect.icon;
        }

        effectIcons[effect] = iconObj;
    }

    private void CreateDefaultEffects()
    {
        // Well Fed
        effectTemplates.Add(new StatusEffect
        {
            id = "well_fed",
            displayName = "Well Fed",
            description = "You feel satisfied and energized",
            type = StatusEffect.EffectType.Buff,
            duration = 600f, // 10 minutes
            modifiers = new StatModifiers
            {
                healthMultiplier = 1.1f,
                staminaMultiplier = 1.2f
            }
        });

        // Bleeding
        effectTemplates.Add(new StatusEffect
        {
            id = "bleeding",
            displayName = "Bleeding",
            description = "You're losing blood",
            type = StatusEffect.EffectType.Injury,
            duration = 120f,
            tickInterval = 2f,
            healthPerTick = -1f,
            isStackable = true,
            maxStacks = 5
        });

        // Poisoned
        effectTemplates.Add(new StatusEffect
        {
            id = "poisoned",
            displayName = "Poisoned",
            description = "You feel sick",
            type = StatusEffect.EffectType.Debuff,
            duration = 300f,
            tickInterval = 5f,
            healthPerTick = -0.5f,
            staminaPerTick = -1f,
            blursVision = true
        });

        // Infected
        effectTemplates.Add(new StatusEffect
        {
            id = "infected",
            displayName = "Infected",
            description = "A wound has become infected",
            type = StatusEffect.EffectType.Disease,
            duration = -1f, // Permanent until cured
            tickInterval = 10f,
            healthPerTick = -0.25f,
            modifiers = new StatModifiers
            {
                staminaMultiplier = 0.8f,
                speedMultiplier = 0.9f
            }
        });

        // Adrenaline Rush
        effectTemplates.Add(new StatusEffect
        {
            id = "adrenaline",
            displayName = "Adrenaline Rush",
            description = "Your heart is racing!",
            type = StatusEffect.EffectType.Buff,
            duration = 30f,
            modifiers = new StatModifiers
            {
                speedMultiplier = 1.5f,
                damageMultiplier = 1.3f,
                staminaMultiplier = 2f
            }
        });

        // Hypothermia
        effectTemplates.Add(new StatusEffect
        {
            id = "hypothermia",
            displayName = "Hypothermia",
            description = "You're freezing!",
            type = StatusEffect.EffectType.Environmental,
            duration = -1f,
            tickInterval = 3f,
            healthPerTick = -0.5f,
            staminaPerTick = -2f,
            modifiers = new StatModifiers
            {
                speedMultiplier = 0.5f
            },
            preventsSprinting = true
        });
    }

    private void ShowNotification(string message, NotificationType type)
    {
        if (notifications != null)
        {
            notifications.ShowNotification(message, type);
        }
        else
        {
            Debug.Log($"[StatusEffects] {type}: {message}");
        }
    }

    // Public helper methods
    public bool HasEffect(string effectId)
    {
        return activeEffects.Any(e => e.id == effectId);
    }

    public StatusEffect GetEffect(string effectId)
    {
        return activeEffects.FirstOrDefault(e => e.id == effectId);
    }

    public float GetModifier(string statName)
    {
        switch (statName.ToLower())
        {
            case "health": return combinedModifiers.healthMultiplier;
            case "stamina": return combinedModifiers.staminaMultiplier;
            case "speed": return combinedModifiers.speedMultiplier;
            case "damage": return combinedModifiers.damageMultiplier;
            case "defense": return combinedModifiers.defenseMultiplier;
            default: return 1f;
        }
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

        GUI.Box(new Rect(10, 310, 250, 150), "Status Effects");
        int y = 330;

        foreach (var effect in activeEffects)
        {
            string text = $"{effect.displayName}";
            if (effect.currentStacks > 1)
                text += $" x{effect.currentStacks}";
            if (effect.duration > 0)
                text += $" ({effect.timeRemaining:F1}s)";

            GUI.Label(new Rect(15, y, 230, 20), text);
            y += 20;
        }
    }
#endif
}