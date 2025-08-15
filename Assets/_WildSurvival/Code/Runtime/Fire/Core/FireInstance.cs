// FireInstance.cs - Simplified version for testing
using System;
using System.Linq;  // ADD THIS for FirstOrDefault and Sum
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireInstance : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// States a fire can be in
    /// </summary>
    public enum FireState
    {
        Unlit,          // No fire
        Igniting,       // Starting to light
        Smoldering,     // Low heat, lots of smoke
        Burning,        // Normal fire
        Blazing,        // Hot fire
        Dying,          // Running out of fuel
        Extinguished    // Was lit but now out
    }

    /// <summary>
    /// Types of fuel that can be burned
    /// </summary>
    public enum FuelType
    {
        None,
        Tinder,         // Dry grass, paper
        Kindling,       // Small twigs
        Softwood,       // Pine, etc.
        Hardwood,       // Oak, etc.
        Logs,           // Large wood pieces
        Coal,           // Mined coal
        Charcoal,       // Processed wood
        Peat,           // Bog fuel
        Oil,            // Liquid fuel
        Gas,            // Gaseous fuel
        Special         // Magic/unique fuel
    }

    /// <summary>
    /// Different fire configurations
    /// </summary>
    public enum FireType
    {
        Campfire,       // Basic campfire
        Torch,          // Handheld or wall torch
        Forge,          // For smelting
        SignalFire,     // Large, visible fire
        CookingFire,    // Optimized for cooking
        Bonfire,        // Large celebration fire
        Brazier,        // Decorative fire container
        Furnace,        // Industrial fire
        Fireplace,      // Indoor fire
        WildfirePatch   // Spreading wildfire
    }

    #endregion

    [Header("Fire Configuration")]
    [SerializeField] private FireState currentState = FireState.Unlit;
    [SerializeField] private float currentTemperature = 0f;
    [SerializeField] private float maxTemperature = 800f;
    [SerializeField] private float currentFuel = 0f;
    [SerializeField] private float maxFuel = 100f;

    [Header("Visual")]
    [SerializeField] private Light fireLight;
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private AudioSource fireAudio;

    // ADD these missing fields at the top with other fields
    [Header("Fire Properties")]
    //private float currentTemperature = 0f;
    //private float maxTemperature = 800f;
    private float currentFuelAmount = 0f;
    private float maxFuelCapacity = 100f;
    private float burnDuration = 0f;
    //private FireState currentState = FireState.Unlit;
    private FireInstance.FireType fireType = FireInstance.FireType.Campfire;
    private float fireRadius = 5f;
    private AnimationCurve heatFalloffCurve;
    private List<FuelData> availableFuels = new List<FuelData>();

    // ADD this FuelData struct
    [System.Serializable]
    public struct FuelData
    {
        public string itemID;
        public int quantity;
        public float burnTime;
        public FuelType fuelType;

        // ADD these missing fields:
        public FuelType type;  // Duplicate of fuelType for compatibility
        public float amount;
        public float burnTemperature;
        public float burnDuration;
        public float heatOutput;
        public bool isWet;

        // Constructor
        public FuelData(string id, int qty, float time, FuelType fType)
        {
            itemID = id;
            quantity = qty;
            burnTime = time;
            fuelType = fType;
            type = fType;
            amount = qty;
            burnTemperature = 400f;
            burnDuration = time;
            heatOutput = 100f;
            isWet = false;
        }
    }

    private void Awake()
    {
        SetupComponents();
    }

    private void SetupComponents()
    {
        // Auto-setup light
        if (fireLight == null)
        {
            fireLight = GetComponentInChildren<Light>();
            if (fireLight == null)
            {
                GameObject lightObj = new GameObject("FireLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.up * 0.5f;
                fireLight = lightObj.AddComponent<Light>();
                fireLight.type = LightType.Point;
                fireLight.color = new Color(1f, 0.5f, 0.1f);
                fireLight.range = 5f;
                fireLight.intensity = 0f;
            }
        }

        // Setup particles if needed
        if (fireParticles == null)
        {
            fireParticles = GetComponentInChildren<ParticleSystem>();
        }

        // Setup audio
        if (fireAudio == null)
        {
            fireAudio = GetComponent<AudioSource>();
            if (fireAudio == null)
            {
                fireAudio = gameObject.AddComponent<AudioSource>();
                fireAudio.loop = true;
                fireAudio.spatialBlend = 1f;
                fireAudio.volume = 0f;
            }
        }
    }

    private void Update()
    {
        UpdateFire();
        UpdateVisuals();
    }

    private void UpdateFire()
    {
        switch (currentState)
        {
            case FireState.Burning:
                // Consume fuel
                currentFuel -= Time.deltaTime * 0.5f;

                // Update temperature
                currentTemperature = Mathf.Lerp(currentTemperature, maxTemperature, Time.deltaTime * 0.5f);

                // Check if running out of fuel
                if (currentFuel <= 0)
                {
                    currentState = FireState.Dying;
                }
                break;

            case FireState.Dying:
                currentTemperature = Mathf.Lerp(currentTemperature, 0f, Time.deltaTime * 0.3f);
                if (currentTemperature < 50f)
                {
                    currentState = FireState.Extinguished;
                }
                break;

            case FireState.Extinguished:
            case FireState.Unlit:
                currentTemperature = Mathf.Lerp(currentTemperature, 0f, Time.deltaTime);
                break;
        }
    }

    private void UpdateVisuals()
    {
        if (fireLight != null)
        {
            fireLight.intensity = Mathf.Lerp(0f, 2f, currentTemperature / maxTemperature);
        }

        if (fireParticles != null)
        {
            var emission = fireParticles.emission;
            emission.enabled = currentState == FireState.Burning;
        }

        if (fireAudio != null)
        {
            fireAudio.volume = Mathf.Lerp(0f, 0.5f, currentTemperature / maxTemperature);
        }
    }

    // Fix the TryAddFuel method signature to work with ItemData
    public bool TryAddFuel(ItemData fuelItem, int quantity)
    {
        if (fuelItem == null) return false;

        // Convert ItemData to ItemDefinition
        var itemDef = GetItemDefinition(fuelItem.itemID);
        if (itemDef != null)
        {
            return TryAddFuel(itemDef, quantity);
        }

        // Fallback - add as generic fuel
        var fuelType = GetFuelTypeFromItemData(fuelItem);
        var fuelData = availableFuels.FirstOrDefault(f => f.type == fuelType);

        if (string.IsNullOrEmpty(fuelData.itemID)) return true;
        //{
        //    fuelData = new FuelData
        //    {
        //        type = fuelType,
        //        amount = 0,
        //        burnTemperature = 400f,
        //        burnDuration = 10f,
        //        heatOutput = 1f,
        //        isWet = false
        //    };
        //    availableFuels.Add(fuelData);
        //}

        float addAmount = quantity * 10f; // Default fuel value
        fuelData.amount = Mathf.Min(fuelData.amount + addAmount, maxFuelCapacity);
        currentFuelAmount = availableFuels.Sum(f => f.amount);

        return true;
    }

    private ItemDefinition GetItemDefinition(string itemID)
    {
        // Try to load from Resources
        ItemDefinition item = Resources.Load<ItemDefinition>($"Items/{itemID}");

        // Or try from database if you have one
        if (item == null && ItemDatabase.Instance != null)
        {
            item = ItemDatabase.Instance.GetItem(itemID);
        }

        // Create a temporary one if not found
        if (item == null)
        {
            Debug.LogWarning($"Item {itemID} not found, creating temporary definition");
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.itemID = itemID;
            item.displayName = itemID;
        }

        return item;
    }

    private FuelType GetFuelTypeFromItemData(ItemData item)
    {
        string id = item.itemID.ToLower();

        if (id.Contains("tinder")) return FuelType.Tinder;
        if (id.Contains("kindling")) return FuelType.Kindling;
        if (id.Contains("log") || id.Contains("wood")) return FuelType.Logs;
        if (id.Contains("coal") || id.Contains("charcoal")) return FuelType.Coal;

        return FuelType.Kindling; // Default
    }

    public bool TryIgnite(IgnitionSource source)
    {
        if (currentState != FireState.Unlit || currentFuel <= 0)
            return false;

        // Check success rate
        if (UnityEngine.Random.value <= source.successRate)
        {
            currentState = FireState.Igniting;
            Invoke(nameof(StartBurning), 2f); // 2 second ignition time
            Debug.Log($"[Fire] Ignition successful with {source.name}");
            return true;
        }

        Debug.Log($"[Fire] Ignition failed with {source.name}");
        return false;
    }

    private void StartBurning()
    {
        currentState = FireState.Burning;
        currentTemperature = 200f; // Start temperature
    }

    private float GetFuelValue(ItemData item)
    {
        if (item == null) return 0f;

        string id = item.itemID.ToLower();

        if (id.Contains("tinder") || id.Contains("grass")) return 5f;
        if (id.Contains("kindling") || id.Contains("stick")) return 10f;
        if (id.Contains("log") || id.Contains("wood")) return 20f;
        if (id.Contains("coal")) return 40f;

        return 5f; // Default
    }

    public float GetTemperature()
    {
        return currentTemperature;
    }

    public FireState GetState()
    {
        return currentState;
    }

    /// <summary>
    /// Get the type of this fire instance
    /// </summary>
    public FireType GetFireType()
    {
        // Determine type based on configuration or name
        if (name.Contains("Torch")) return FireType.Torch;
        if (name.Contains("Forge")) return FireType.Forge;
        if (name.Contains("Signal")) return FireType.SignalFire;
        if (name.Contains("Cooking")) return FireType.CookingFire;
        return FireType.Campfire;
    }

    /// <summary>
    /// Get total burn time
    /// </summary>
    public float GetBurnTime()
    {
        return burnDuration;
    }

    /// <summary>
    /// Check if this fire was created by the player
    /// </summary>
    public bool IsPlayerMade()
    {
        return gameObject.name.Contains("Player") || gameObject.CompareTag("PlayerMade");
    }

    /// <summary>
    /// Get list of fuel items (for saving)
    /// </summary>
    public List<FireSaveData.FuelItemData> GetFuelItems()
    {
        // This would need to be implemented based on your fuel system
        return new List<FireSaveData.FuelItemData>();
    }

    /// <summary>
    /// Load from save data
    /// </summary>
    public void LoadFromSaveData(FireSaveData.FireInstanceData data)
    {
        if (data == null) return;

        // Restore state
        transform.position = data.position;
        transform.rotation = data.rotation;
        currentFuel = data.fuelAmount;
        maxFuelCapacity = data.maxFuel;
        burnDuration = data.burnTimeElapsed;

        // Restore fire state
        if (data.isLit)
        {
            StartFire(IgnitionSource.Lighter);
        }
    }

    /// <summary>
    /// Helper to reduce temperature
    /// </summary>
    public void ReduceTemperature(float amount)
    {
        float currentTemp = GetCookingTemperature();
        // Would need proper implementation
    }

    /// <summary>
    /// Helper to increase temperature
    /// </summary>
    public void IncreaseTemperature(float amount)
    {
        float currentTemp = GetCookingTemperature();
        // Would need proper implementation
    }


    // ADD all these missing methods:

    public float GetCookingTemperature()
    {
        return currentTemperature;
    }

    public float GetFuelPercentage()
    {
        return maxFuelCapacity > 0 ? (currentFuelAmount / maxFuelCapacity) * 100f : 0f;
    }

    public float GetFuelAmount()
    {
        return currentFuelAmount;
    }

    public float GetMaxFuelCapacity()
    {
        return maxFuelCapacity;
    }

    public void ExtinguishFire(string reason = "Manual")
    {
        currentState = FireState.Extinguished;
        currentTemperature = 0f;
        Debug.Log($"Fire extinguished: {reason}");
        // Add particle/sound effects here
    }

    public void StartFire(IgnitionSource source)
    {
        if (currentState == FireState.Unlit && source != null)
        {
            if (source.TryUse())
            {
                currentState = FireState.Igniting;
                StartCoroutine(IgniteSequence());
            }
        }
    }

    private IEnumerator IgniteSequence()
    {
        yield return new WaitForSeconds(2f);
        currentState = FireState.Burning;
        currentTemperature = 300f;
    }

    public float GetWarmthAtDistance(float distance)
    {
        if (currentState != FireState.Burning && currentState != FireState.Blazing)
            return 0f;

        if (distance > fireRadius)
            return 0f;

        float normalizedDistance = distance / fireRadius;
        if (heatFalloffCurve != null)
            return heatFalloffCurve.Evaluate(1f - normalizedDistance) * currentTemperature;
        else
            return (1f - normalizedDistance) * currentTemperature;
    }

    public bool CanLightTorch()
    {
        return currentState == FireState.Burning || currentState == FireState.Blazing;
    }

    public bool TryLightTorch()
    {
        if (CanLightTorch())
        {
            Debug.Log("Torch lit from fire!");
            return true;
        }
        return false;
    }

    public void SetFireType(FireType type)
    {
        fireType = type;
    }

    public void SetMaxTemperature(float temp)
    {
        maxTemperature = temp;
    }

    public void SetFuelCapacity(float capacity)
    {
        maxFuelCapacity = capacity;
    }
}

// Advanced Fire System Commented out for now, get the basic one working first then upgrade ////////// **************************

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.Events;

///// <summary>
///// Core fire system managing heat, fuel consumption, and fire propagation
///// Optimized for performance with spatial hashing and LOD systems
///// </summary>
//[System.Serializable]
//public class FireInstance : MonoBehaviour
//{
//    [Header("Fire Configuration")]
//    [SerializeField] private FireType fireType = FireType.Campfire;
//    [SerializeField] private float maxTemperature = 800f; // Celsius
//    [SerializeField] private float currentTemperature;
//    [SerializeField] private float fireRadius = 3f;
//    [SerializeField] private AnimationCurve heatFalloffCurve;

//    [Header("Fuel System")]
//    [SerializeField] private float currentFuelAmount;
//    [SerializeField] private float maxFuelCapacity = 100f;
//    [SerializeField] private float fuelBurnRate = 1f; // Units per minute
//    [SerializeField] private List<FuelData> availableFuels = new();

//    [Header("Visual Effects")]
//    [SerializeField] private ParticleSystem fireParticles;
//    [SerializeField] private ParticleSystem smokeParticles;
//    [SerializeField] private ParticleSystem embersParticles;
//    [SerializeField] private Light fireLight;
//    [SerializeField] private AudioSource fireAudioSource;

//    [Header("Gameplay Effects")]
//    [SerializeField] private float predatorDeterrentRadius = 10f;
//    [SerializeField] private float lightRadius = 8f;
//    [SerializeField] private float cookingEfficiency = 1f;

//    // Runtime state
//    private FireState currentState = FireState.Unlit;
//    private float stateTimer;
//    private float windStrength;
//    private float rainIntensity;
//    private List<Collider> nearbyColliders = new();
//    private Dictionary<GameObject, float> burnTargets = new();

//    // Performance optimization
//    private float lastUpdateTime;
//    private const float UPDATE_INTERVAL = 0.2f; // 5 updates per second
//    private static List<FireInstance> allFires = new();
//    private static SpatialHashGrid<FireInstance> fireGrid;

//    // Events
//    public UnityEvent<float> OnTemperatureChanged;
//    public UnityEvent<FireState> OnStateChanged;
//    public UnityEvent OnFireExtinguished;
//    public UnityEvent OnIgnited;

//    public enum FireType
//    {
//        Campfire,
//        Torch,
//        Forge,
//        SignalFire,
//        CookingFire,
//        Wildfire
//    }

//    public enum FireState
//    {
//        Unlit,
//        Igniting,      // 0-200°C
//        Smoldering,    // 200-400°C
//        Burning,       // 400-600°C
//        Blazing,       // 600-800°C
//        Dying,         // Fuel depleting
//        Extinguished
//    }

//    [System.Serializable]
//    public class FuelData
//    {
//        public FuelType type;
//        public float amount;
//        public float burnTemperature;
//        public float burnDuration;
//        public float heatOutput;
//        public bool isWet;

//        public float GetEffectiveBurnRate()
//        {
//            return isWet ? burnDuration * 0.3f : burnDuration;
//        }
//    }

//    public enum FuelType
//    {
//        Tinder,      // Quick ignition, burns fast
//        Kindling,    // Medium burn time
//        Logs,        // Long burn time
//        Coal,        // Very long burn, high heat
//        Oil,         // Liquid fuel, spreads fire
//        Paper,       // Quick ignition
//        Grass,       // Very quick burn
//        Peat,        // Slow burn, lots of smoke
//        Dung,        // Slow burn, smoky
//        Resin,       // Hot burn, sticky
//    }

//    #region Initialization

//    private void Awake()
//    {
//        InitializeComponents();
//        allFires.Add(this);

//        if (fireGrid == null)
//        {
//            fireGrid = new SpatialHashGrid<FireInstance>(50f); // 50m cell size
//        }
//        fireGrid.Add(this, transform.position);
//    }

//    private void InitializeComponents()
//    {
//        // Auto-setup components if missing
//        if (fireParticles == null)
//            fireParticles = GetComponentInChildren<ParticleSystem>();

//        if (fireLight == null)
//        {
//            fireLight = GetComponentInChildren<Light>();
//            if (fireLight == null)
//            {
//                var lightObj = new GameObject("Fire Light");
//                lightObj.transform.SetParent(transform);
//                lightObj.transform.localPosition = Vector3.up * 0.5f;
//                fireLight = lightObj.AddComponent<Light>();
//                fireLight.type = LightType.Point;
//                fireLight.color = new Color(1f, 0.5f, 0.1f);
//                fireLight.intensity = 0;
//                fireLight.range = lightRadius;
//            }
//        }

//        if (fireAudioSource == null)
//        {
//            fireAudioSource = gameObject.AddComponent<AudioSource>();
//            fireAudioSource.spatialBlend = 1f;
//            fireAudioSource.loop = true;
//            fireAudioSource.volume = 0;
//        }
//    }

//    private void OnDestroy()
//    {
//        allFires.Remove(this);
//        fireGrid?.Remove(this);
//    }

//    #endregion

//    #region Core Fire Logic

//    private void Update()
//    {
//        // Throttled update for performance
//        if (Time.time - lastUpdateTime < UPDATE_INTERVAL) return;
//        lastUpdateTime = Time.time;

//        if (currentState == FireState.Unlit || currentState == FireState.Extinguished)
//            return;

//        UpdateFirePhysics();
//        UpdateFuelConsumption();
//        UpdateVisualEffects();
//        UpdateGameplayEffects();
//        CheckFireSpread();
//    }

//    private void UpdateFirePhysics()
//    {
//        // Temperature decay based on environment
//        float targetTemp = CalculateTargetTemperature();
//        float tempDelta = (targetTemp - currentTemperature) * Time.deltaTime * 0.5f;

//        // Wind affects temperature
//        if (windStrength > 0)
//        {
//            tempDelta += windStrength * 10f * Time.deltaTime; // Wind feeds fire

//            // But too much wind can blow it out
//            if (windStrength > 20f && currentState == FireState.Igniting)
//            {
//                ExtinguishFire("Blown out by strong wind");
//                return;
//            }
//        }

//        // Rain dampens fire
//        if (rainIntensity > 0)
//        {
//            tempDelta -= rainIntensity * 50f * Time.deltaTime;

//            // Heavy rain extinguishes weak fires
//            if (rainIntensity > 0.7f && currentTemperature < 300f)
//            {
//                ExtinguishFire("Extinguished by rain");
//                return;
//            }
//        }

//        currentTemperature = Mathf.Clamp(currentTemperature + tempDelta, 0, maxTemperature);
//        UpdateFireState();
//        OnTemperatureChanged?.Invoke(currentTemperature);
//    }

//    private float CalculateTargetTemperature()
//    {
//        if (currentFuelAmount <= 0) return 0;

//        float fuelTemp = 0;
//        float totalWeight = 0;

//        foreach (var fuel in availableFuels)
//        {
//            float weight = fuel.amount / maxFuelCapacity;
//            fuelTemp += fuel.burnTemperature * weight * fuel.heatOutput;
//            totalWeight += weight;
//        }

//        if (totalWeight > 0)
//            fuelTemp /= totalWeight;

//        // Environmental factors
//        float environmentMod = 1f;
//        environmentMod -= rainIntensity * 0.5f;
//        environmentMod += windStrength * 0.1f; // Some wind helps

//        return fuelTemp * environmentMod;
//    }

//    private void UpdateFireState()
//    {
//        FireState newState = currentState;

//        if (currentTemperature <= 0)
//            newState = FireState.Extinguished;
//        else if (currentTemperature < 200)
//            newState = FireState.Igniting;
//        else if (currentTemperature < 400)
//            newState = FireState.Smoldering;
//        else if (currentTemperature < 600)
//            newState = FireState.Burning;
//        else
//            newState = FireState.Blazing;

//        if (currentFuelAmount < maxFuelCapacity * 0.1f && currentTemperature > 0)
//            newState = FireState.Dying;

//        if (newState != currentState)
//        {
//            currentState = newState;
//            OnStateChanged?.Invoke(currentState);

//            if (newState == FireState.Extinguished)
//                OnFireExtinguished?.Invoke();
//        }
//    }

//    #endregion

//    #region Fuel Management

//    private void UpdateFuelConsumption()
//    {
//        if (currentState == FireState.Unlit || currentState == FireState.Extinguished)
//            return;

//        float consumptionRate = fuelBurnRate * Time.deltaTime / 60f;

//        // Burn rate varies by state
//        switch (currentState)
//        {
//            case FireState.Igniting:
//                consumptionRate *= 0.5f;
//                break;
//            case FireState.Smoldering:
//                consumptionRate *= 0.7f;
//                break;
//            case FireState.Burning:
//                consumptionRate *= 1f;
//                break;
//            case FireState.Blazing:
//                consumptionRate *= 1.5f;
//                break;
//            case FireState.Dying:
//                consumptionRate *= 0.3f;
//                break;
//        }

//        // Consume fuel by priority (tinder -> kindling -> logs)
//        ConsumeFuelByPriority(consumptionRate);

//        currentFuelAmount = availableFuels.Sum(f => f.amount);

//        if (currentFuelAmount <= 0)
//        {
//            ExtinguishFire("Out of fuel");
//        }
//    }

//    private void ConsumeFuelByPriority(float amount)
//    {
//        // Sort by burn speed (tinder first)
//        var sortedFuels = availableFuels.OrderBy(f => f.burnDuration).ToList();

//        foreach (var fuel in sortedFuels)
//        {
//            if (fuel.amount > 0)
//            {
//                float toConsume = Mathf.Min(amount, fuel.amount);
//                fuel.amount -= toConsume;
//                amount -= toConsume;

//                if (amount <= 0) break;
//            }
//        }

//        // Remove depleted fuels
//        availableFuels.RemoveAll(f => f.amount <= 0);
//    }

//    public bool TryAddFuel(ItemDefinition fuelItem, int quantity)
//    {
//        if (!CanAcceptFuel(fuelItem)) return false;

//        var fuelType = GetFuelType(fuelItem);
//        var fuelData = availableFuels.FirstOrDefault(f => f.type == fuelType);

//        if (fuelData == null)
//        {
//            fuelData = new FuelData
//            {
//                type = fuelType,
//                amount = 0,
//                burnTemperature = GetFuelTemperature(fuelType),
//                burnDuration = GetFuelDuration(fuelType),
//                heatOutput = GetFuelHeatOutput(fuelType),
//                isWet = false // Check weather/storage
//            };
//            availableFuels.Add(fuelData);
//        }

//        float addAmount = quantity * GetFuelValue(fuelItem);
//        fuelData.amount = Mathf.Min(fuelData.amount + addAmount, maxFuelCapacity);
//        currentFuelAmount = availableFuels.Sum(f => f.amount);

//        // Auto-ignite if adding tinder to smoldering fire
//        if (fuelType == FuelType.Tinder && currentState == FireState.Smoldering)
//        {
//            currentTemperature += 100f;
//        }

//        return true;
//    }

//    private bool CanAcceptFuel(ItemDefinition item)
//    {
//        // Check if item has fuel properties
//        return item.tags.Contains(ItemTag.Fuel) || item.tags.Contains(ItemTag.Wood);
//    }

//    private FuelType GetFuelType(ItemDefinition item)
//    {
//        // Map item IDs to fuel types
//        return item.itemID switch
//        {
//            "fuel_tinder" => FuelType.Tinder,
//            "fuel_kindling" => FuelType.Kindling,
//            "wood_log" => FuelType.Logs,
//            "fuel_charcoal" => FuelType.Coal,
//            "fuel_coal" => FuelType.Coal,
//            "fuel_oil" => FuelType.Oil,
//            _ => FuelType.Kindling
//        };
//    }

//    private float GetFuelValue(ItemDefinition item)
//    {
//        // How much fuel value per item
//        return item.itemID switch
//        {
//            "fuel_tinder" => 5f,
//            "fuel_kindling" => 10f,
//            "wood_log" => 25f,
//            "fuel_charcoal" => 30f,
//            "fuel_coal" => 40f,
//            _ => 10f
//        };
//    }

//    private float GetFuelTemperature(FuelType type)
//    {
//        return type switch
//        {
//            FuelType.Tinder => 300f,
//            FuelType.Kindling => 400f,
//            FuelType.Logs => 600f,
//            FuelType.Coal => 800f,
//            FuelType.Oil => 700f,
//            _ => 400f
//        };
//    }

//    private float GetFuelDuration(FuelType type)
//    {
//        // Minutes of burn time per unit
//        return type switch
//        {
//            FuelType.Tinder => 2f,
//            FuelType.Kindling => 5f,
//            FuelType.Logs => 20f,
//            FuelType.Coal => 60f,
//            FuelType.Oil => 15f,
//            _ => 10f
//        };
//    }

//    private float GetFuelHeatOutput(FuelType type)
//    {
//        return type switch
//        {
//            FuelType.Tinder => 0.5f,
//            FuelType.Kindling => 0.8f,
//            FuelType.Logs => 1f,
//            FuelType.Coal => 1.5f,
//            FuelType.Oil => 1.2f,
//            _ => 1f
//        };
//    }

//    #endregion

//    #region Ignition System

//    public bool TryIgnite(IgnitionSource source)
//    {
//        if (currentState != FireState.Unlit && currentState != FireState.Extinguished)
//            return false;

//        // Check if we have proper fuel to start
//        bool hasTinder = availableFuels.Any(f => f.type == FuelType.Tinder && f.amount > 0);
//        bool hasKindling = availableFuels.Any(f => f.type == FuelType.Kindling && f.amount > 0);

//        float successChance = source.successRate;

//        // Modify success chance based on conditions
//        if (!hasTinder) successChance *= 0.3f;
//        if (!hasKindling) successChance *= 0.5f;
//        if (rainIntensity > 0) successChance *= (1f - rainIntensity);
//        if (windStrength > 10f) successChance *= 0.7f;

//        // Check for wet fuel
//        if (availableFuels.Any(f => f.isWet))
//            successChance *= 0.4f;

//        if (UnityEngine.Random.value <= successChance)
//        {
//            StartIgnition(source.ignitionTemperature);
//            return true;
//        }

//        // Failed ignition consumes some tinder
//        var tinder = availableFuels.FirstOrDefault(f => f.type == FuelType.Tinder);
//        if (tinder != null)
//            tinder.amount = Mathf.Max(0, tinder.amount - 1f);

//        return false;
//    }

//    private void StartIgnition(float startTemp)
//    {
//        currentState = FireState.Igniting;
//        currentTemperature = startTemp;
//        OnIgnited?.Invoke();

//        // Start effects
//        if (fireParticles != null)
//            fireParticles.Play();

//        if (smokeParticles != null)
//        {
//            var emission = smokeParticles.emission;
//            emission.rateOverTime = 5f;
//            smokeParticles.Play();
//        }
//    }

//    public void ExtinguishFire(string reason = "Manually extinguished")
//    {
//        Debug.Log($"Fire extinguished: {reason}");

//        currentState = FireState.Extinguished;
//        currentTemperature = 0;

//        // Stop all effects
//        if (fireParticles != null) fireParticles.Stop();
//        if (smokeParticles != null) smokeParticles.Stop();
//        if (embersParticles != null) embersParticles.Stop();

//        fireLight.intensity = 0;
//        fireAudioSource.volume = 0;

//        OnFireExtinguished?.Invoke();

//        // Leave some charcoal if it was burning hot
//        if (currentTemperature > 400f)
//        {
//            // Convert some remaining wood to charcoal
//            ConvertToCharcoal();
//        }
//    }

//    private void ConvertToCharcoal()
//    {
//        var logs = availableFuels.FirstOrDefault(f => f.type == FuelType.Logs);
//        if (logs != null && logs.amount > 0)
//        {
//            float charcoalAmount = logs.amount * 0.3f; // 30% conversion rate

//            var charcoal = availableFuels.FirstOrDefault(f => f.type == FuelType.Coal);
//            if (charcoal == null)
//            {
//                charcoal = new FuelData
//                {
//                    type = FuelType.Coal,
//                    amount = charcoalAmount,
//                    burnTemperature = 800f,
//                    burnDuration = 60f,
//                    heatOutput = 1.5f
//                };
//                availableFuels.Add(charcoal);
//            }
//            else
//            {
//                charcoal.amount += charcoalAmount;
//            }

//            logs.amount = 0;
//        }
//    }

//    #endregion

//    #region Visual Effects

//    private void UpdateVisualEffects()
//    {
//        if (fireParticles == null || fireLight == null) return;

//        // Particle effects based on state
//        var main = fireParticles.main;
//        var emission = fireParticles.emission;

//        switch (currentState)
//        {
//            case FireState.Igniting:
//                main.startLifetime = 0.5f;
//                emission.rateOverTime = 5f;
//                fireLight.intensity = 0.5f;
//                fireLight.range = 2f;
//                break;

//            case FireState.Smoldering:
//                main.startLifetime = 1f;
//                emission.rateOverTime = 10f;
//                fireLight.intensity = 1f;
//                fireLight.range = 3f;
//                break;

//            case FireState.Burning:
//                main.startLifetime = 1.5f;
//                emission.rateOverTime = 30f;
//                fireLight.intensity = 3f;
//                fireLight.range = lightRadius * 0.7f;
//                break;

//            case FireState.Blazing:
//                main.startLifetime = 2f;
//                emission.rateOverTime = 50f;
//                fireLight.intensity = 5f;
//                fireLight.range = lightRadius;
//                break;

//            case FireState.Dying:
//                main.startLifetime = 0.8f;
//                emission.rateOverTime = 8f;
//                fireLight.intensity = 0.8f;
//                fireLight.range = 2f;
//                break;
//        }

//        // Update smoke based on fuel type
//        if (smokeParticles != null)
//        {
//            var smokeEmission = smokeParticles.emission;
//            float smokeRate = 10f;

//            // Wet fuel creates more smoke
//            if (availableFuels.Any(f => f.isWet))
//                smokeRate *= 3f;

//            // Certain fuels are smokier
//            if (availableFuels.Any(f => f.type == FuelType.Peat || f.type == FuelType.Dung))
//                smokeRate *= 2f;

//            smokeEmission.rateOverTime = smokeRate;
//        }

//        // Light color based on temperature
//        float tempNorm = currentTemperature / maxTemperature;
//        fireLight.color = Color.Lerp(
//            new Color(1f, 0.3f, 0f),    // Low temp - red/orange
//            new Color(1f, 0.9f, 0.7f),   // High temp - white/yellow
//            tempNorm
//        );

//        // Audio
//        if (fireAudioSource != null)
//        {
//            fireAudioSource.volume = Mathf.Lerp(0, 1f, tempNorm);
//            fireAudioSource.pitch = Mathf.Lerp(0.8f, 1.2f, tempNorm);
//        }
//    }

//    #endregion

//    #region Gameplay Effects

//    private void UpdateGameplayEffects()
//    {
//        // Update predator deterrent
//        if (currentState == FireState.Burning || currentState == FireState.Blazing)
//        {
//            DeterPredators();
//        }

//        // Warmth radius for players
//        ProvideWarmth();

//        // Check for burning damage
//        CheckBurnDamage();
//    }

//    private void DeterPredators()
//    {
//        var nearbyAnimals = Physics.OverlapSphere(transform.position, predatorDeterrentRadius, LayerMask.GetMask("Animals"));

//        foreach (var animal in nearbyAnimals)
//        {
//            var ai = animal.GetComponent<AnimalAI>();
//            if (ai != null && ai.IsPredator)
//            {
//                // Make predator flee or avoid
//                ai.SetFearTarget(transform.position, currentTemperature / 100f);
//            }
//        }
//    }

//    private void ProvideWarmth()
//    {
//        var nearbyPlayers = Physics.OverlapSphere(transform.position, fireRadius, LayerMask.GetMask("Player"));

//        foreach (var player in nearbyPlayers)
//        {
//            var vitals = player.GetComponent<PlayerStats>();
//            if (vitals != null)
//            {
//                float distance = Vector3.Distance(transform.position, player.transform.position);
//                float warmthAmount = CalculateWarmthAtDistance(distance);

//                vitals.ApplyWarmth(warmthAmount, transform.position);
//            }
//        }
//    }

//    private float CalculateWarmthAtDistance(float distance)
//    {
//        if (distance > fireRadius) return 0;

//        float normalizedDist = distance / fireRadius;
//        float falloff = heatFalloffCurve?.Evaluate(normalizedDist) ?? (1f - normalizedDist);

//        // Base warmth on temperature and distance
//        float baseWarmth = currentTemperature / 10f; // 0-80 warmth units
//        return baseWarmth * falloff;
//    }

//    private void CheckBurnDamage()
//    {
//        // Very close = burn damage
//        var burnRadius = fireRadius * 0.3f; // 30% of heat radius
//        var nearbyObjects = Physics.OverlapSphere(transform.position, burnRadius);

//        foreach (var obj in nearbyObjects)
//        {
//            // Skip the fire itself
//            if (obj.gameObject == gameObject) continue;

//            var damageable = obj.GetComponent<IDamageable>();
//            if (damageable != null)
//            {
//                float distance = Vector3.Distance(transform.position, obj.transform.position);

//                if (distance < burnRadius * 0.5f) // Very close
//                {
//                    float damage = (currentTemperature / 100f) * Time.deltaTime; // 0-8 DPS
//                    damageable.TakeDamage(damage, DamageType.Fire);
//                }
//            }

//            // Check if item can catch fire
//            var flammable = obj.GetComponent<IFlammable>();
//            if (flammable != null && currentState == FireState.Blazing)
//            {
//                flammable.TryIgnite(currentTemperature);
//            }
//        }
//    }

//    private void CheckFireSpread()
//    {
//        if (fireType != FireType.Wildfire && currentState != FireState.Blazing)
//            return;

//        // Only spread if very hot and has excess fuel
//        if (currentTemperature < 700f || currentFuelAmount < maxFuelCapacity * 0.8f)
//            return;

//        // Look for flammable objects nearby
//        var spreadRadius = fireRadius * 1.5f;
//        var nearbyFlammables = Physics.OverlapSphere(transform.position, spreadRadius)
//            .Select(c => c.GetComponent<IFlammable>())
//            .Where(f => f != null && !f.IsOnFire);

//        foreach (var flammable in nearbyFlammables)
//        {
//            float distance = Vector3.Distance(transform.position, ((Component)flammable).transform.position);
//            float spreadChance = (1f - distance / spreadRadius) * 0.1f * Time.deltaTime;

//            if (UnityEngine.Random.value < spreadChance)
//            {
//                flammable.TryIgnite(currentTemperature * 0.8f);
//            }
//        }
//    }

//    #endregion

//    #region Cooking & Crafting

//    public bool CanCook()
//    {
//        return currentState == FireState.Burning || currentState == FireState.Blazing;
//    }

//    public float GetCookingTemperature()
//    {
//        return currentTemperature;
//    }

//    public float GetCookingEfficiency()
//    {
//        // Optimal cooking at 400-600°C
//        if (currentTemperature < 300f) return 0.5f;
//        if (currentTemperature < 400f) return 0.8f;
//        if (currentTemperature <= 600f) return 1f;
//        return 0.9f; // Too hot, might burn food
//    }

//    public bool CanBoilWater()
//    {
//        return currentTemperature >= 100f; // Water boils at 100°C
//    }

//    public float GetBoilingSpeed()
//    {
//        // Faster boiling at higher temps
//        if (currentTemperature < 100f) return 0;
//        if (currentTemperature < 200f) return 0.5f;
//        if (currentTemperature < 400f) return 1f;
//        return 1.5f;
//    }

//    public bool CanSmelt()
//    {
//        return fireType == FireType.Forge && currentTemperature >= 600f;
//    }

//    public bool CanForge()
//    {
//        return fireType == FireType.Forge && currentTemperature >= 700f;
//    }

//    #endregion

//    #region Torch Lighting

//    public bool TryLightTorch(GameObject torch)
//    {
//        if (currentState != FireState.Burning && currentState != FireState.Blazing)
//            return false;

//        var torchFire = torch.GetComponent<FireInstance>();
//        if (torchFire == null)
//        {
//            torchFire = torch.AddComponent<FireInstance>();
//            torchFire.fireType = FireType.Torch;
//            torchFire.maxFuelCapacity = 20f;
//        }

//        // Transfer some fire
//        torchFire.availableFuels.Clear();
//        torchFire.availableFuels.Add(new FuelData
//        {
//            type = FuelType.Oil, // Torch fuel
//            amount = 15f,
//            burnTemperature = 500f,
//            burnDuration = 30f, // 30 minutes
//            heatOutput = 0.8f
//        });

//        torchFire.StartIgnition(400f);

//        return true;
//    }

//    #endregion

//    #region Save/Load

//    [System.Serializable]
//    public class FireSaveData
//    {
//        public float temperature;
//        public float fuelAmount;
//        public List<FuelData> fuels;
//        public FireState state;
//        public Vector3 position;
//        public FireType type;
//    }

//    public FireSaveData GetSaveData()
//    {
//        return new FireSaveData
//        {
//            temperature = currentTemperature,
//            fuelAmount = currentFuelAmount,
//            fuels = new List<FuelData>(availableFuels),
//            state = currentState,
//            position = transform.position,
//            type = fireType
//        };
//    }

//    public void LoadSaveData(FireSaveData data)
//    {
//        currentTemperature = data.temperature;
//        currentFuelAmount = data.fuelAmount;
//        availableFuels = new List<FuelData>(data.fuels);
//        currentState = data.state;
//        fireType = data.type;

//        UpdateFireState();
//        UpdateVisualEffects();
//    }

//    #endregion

//    #region Debug

//    private void OnDrawGizmosSelected()
//    {
//        // Heat radius
//        Gizmos.color = Color.red;
//        Gizmos.DrawWireSphere(transform.position, fireRadius);

//        // Light radius
//        Gizmos.color = Color.yellow;
//        Gizmos.DrawWireSphere(transform.position, lightRadius);

//        // Predator deterrent radius
//        Gizmos.color = Color.blue;
//        Gizmos.DrawWireSphere(transform.position, predatorDeterrentRadius);

//        // Temperature indicator
//        float tempNorm = currentTemperature / maxTemperature;
//        Gizmos.color = Color.Lerp(Color.black, Color.red, tempNorm);
//        Gizmos.DrawCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
//    }

//    #endregion
//}

///// <summary>
///// Ignition sources for starting fires
///// </summary>
//[System.Serializable]
//public class IgnitionSource
//{
//    public string name;
//    public float successRate; // 0-1
//    public float ignitionTemperature;
//    public int usesRemaining;
//    public bool requiresTinder;

//    public static readonly IgnitionSource Matches = new()
//    {
//        name = "Matches",
//        successRate = 0.9f,
//        ignitionTemperature = 200f,
//        usesRemaining = 20,
//        requiresTinder = true
//    };

//    public static readonly IgnitionSource Lighter = new()
//    {
//        name = "Lighter",
//        successRate = 0.95f,
//        ignitionTemperature = 250f,
//        usesRemaining = 100,
//        requiresTinder = false
//    };

//    public static readonly IgnitionSource FlintAndSteel = new()
//    {
//        name = "Flint and Steel",
//        successRate = 0.6f,
//        ignitionTemperature = 150f,
//        usesRemaining = -1, // Infinite
//        requiresTinder = true
//    };

//    public static readonly IgnitionSource BowDrill = new()
//    {
//        name = "Bow Drill",
//        successRate = 0.3f,
//        ignitionTemperature = 100f,
//        usesRemaining = -1,
//        requiresTinder = true
//    };

//    public static readonly IgnitionSource FirePlough = new()
//    {
//        name = "Fire Plough",
//        successRate = 0.2f,
//        ignitionTemperature = 80f,
//        usesRemaining = -1,
//        requiresTinder = true
//    };
//}

///// <summary>
///// Interface for flammable objects
///// </summary>
//public interface IFlammable
//{
//    bool IsOnFire { get; }
//    float FlammabilityRating { get; } // 0-1
//    bool TryIgnite(float temperature);
//    void Extinguish();
//}

///// <summary>
///// Interface for objects that can take damage
///// </summary>
//public interface IDamageable
//{
//    void TakeDamage(float amount, DamageType type);
//    float Health { get; }
//    float MaxHealth { get; }
//}

//public enum DamageType
//{
//    Physical,
//    Fire,
//    Cold,
//    Poison,
//    Electric
//}

///// <summary>
///// Spatial hash grid for efficient proximity queries
///// </summary>
//public class SpatialHashGrid<T> where T : Component
//{
//    private Dictionary<int, List<T>> grid = new();
//    private float cellSize;

//    public SpatialHashGrid(float cellSize)
//    {
//        this.cellSize = cellSize;
//    }

//    public void Add(T item, Vector3 position)
//    {
//        int hash = GetHash(position);
//        if (!grid.ContainsKey(hash))
//            grid[hash] = new List<T>();

//        if (!grid[hash].Contains(item))
//            grid[hash].Add(item);
//    }

//    public void Remove(T item)
//    {
//        foreach (var cell in grid.Values)
//        {
//            cell.Remove(item);
//        }
//    }

//    public List<T> GetNearby(Vector3 position, float radius)
//    {
//        var result = new HashSet<T>();
//        int cellRadius = Mathf.CeilToInt(radius / cellSize);

//        for (int x = -cellRadius; x <= cellRadius; x++)
//        {
//            for (int z = -cellRadius; z <= cellRadius; z++)
//            {
//                Vector3 checkPos = position + new Vector3(x * cellSize, 0, z * cellSize);
//                int hash = GetHash(checkPos);

//                if (grid.TryGetValue(hash, out var items))
//                {
//                    foreach (var item in items)
//                    {
//                        if (item != null && Vector3.Distance(item.transform.position, position) <= radius)
//                            result.Add(item);
//                    }
//                }
//            }
//        }

//        return result.ToList();
//    }

//    private int GetHash(Vector3 position)
//    {
//        int x = Mathf.FloorToInt(position.x / cellSize);
//        int z = Mathf.FloorToInt(position.z / cellSize);
//        return x * 73856093 ^ z * 19349663; // Large primes for better distribution
//    }
//}