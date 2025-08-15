using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interactive tutorial for fire system
/// </summary>
public class FireTutorialSystem : MonoBehaviour
{

    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        [TextArea(3, 5)]
        public string description;
        public string targetAction;
        public GameObject highlightTarget;
        public float waitTime = 0f;
        public bool requiresCompletion = true;
    }

    [Header("Tutorial Steps")]
    [SerializeField]
    private TutorialStep[] tutorialSteps = new TutorialStep[]
    {
        new TutorialStep
        {
            title = "Welcome to Fire System",
            description = "Learn how to create and manage fires for survival",
            waitTime = 3f,
            requiresCompletion = false
        },
        new TutorialStep
        {
            title = "Gather Materials",
            description = "Collect 5 stones, 3 sticks, and 2 tinder",
            targetAction = "gather_materials",
            requiresCompletion = true
        },
        new TutorialStep
        {
            title = "Build Campfire",
            description = "Press B to enter build mode, then click to place",
            targetAction = "build_campfire",
            requiresCompletion = true
        },
        new TutorialStep
        {
            title = "Add Fuel",
            description = "Approach the fire and press F, then add fuel",
            targetAction = "add_fuel",
            requiresCompletion = true
        },
        new TutorialStep
        {
            title = "Light Fire",
            description = "Use matches or fire starter to ignite",
            targetAction = "light_fire",
            requiresCompletion = true
        },
        new TutorialStep
        {
            title = "Stay Warm",
            description = "Stand near the fire to increase body temperature",
            targetAction = "get_warm",
            requiresCompletion = true
        }
    };

    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject highlightEffect;

    // ADD ALL THESE MISSING FIELDS:
    private Dictionary<FireInstance, float> nearbyFires = new Dictionary<FireInstance, float>();
    private bool isNearFire = false;
    private float fireDetectionRadius = 20f;
    private LayerMask fireLayerMask = -1;
    private PlayerStats playerStats;

    // Temperature fields (if keeping temperature code)
    private float currentBodyTemperature = 37f;
    private float ambientTemperature = 15f;
    private float minSafeTemp = 35f;
    private float maxSafeTemp = 39f;
    private float criticalLowTemp = 32f;
    private float criticalHighTemp = 41f;
    private float effectiveInsulation = 0.3f;
    private float rapidChangeRate = 1.5f;
    private float normalChangeRate = 0.5f;
    private float shiveringThreshold = 34f;
    private float sweatingThreshold = 38f;
    private float coldDamageRate = 2f;
    private float heatDamageRate = 1f;
    private float coldStaminaDrain = 3f;

    // Status fields
    private bool isShivering;
    private bool isSweating;
    private bool hasHypothermia;
    private bool hasHeatStroke;

    // Events
    public UnityEngine.Events.UnityEvent<FireInstance> OnNearFireStart = new UnityEngine.Events.UnityEvent<FireInstance>();
    public UnityEngine.Events.UnityEvent OnNearFireEnd;
    public UnityEngine.Events.UnityEvent OnStartShivering;
    public UnityEngine.Events.UnityEvent OnStopShivering;
    public UnityEngine.Events.UnityEvent OnStartSweating;
    public UnityEngine.Events.UnityEvent OnStopSweating;
    public UnityEngine.Events.UnityEvent OnHypothermiaStart;
    public UnityEngine.Events.UnityEvent OnHypothermiaEnd;
    public UnityEngine.Events.UnityEvent OnHeatStrokeStart;
    public UnityEngine.Events.UnityEvent OnHeatStrokeEnd;
    public UnityEngine.Events.UnityEvent<float> OnTemperatureChanged = new UnityEngine.Events.UnityEvent<float>();

    private int currentStep = 0;
    private bool tutorialActive = false;
    private bool stepCompleted = false;

    void Start()
    {
        if (PlayerPrefs.GetInt("FireTutorialCompleted", 0) == 0)
        {
            StartTutorial();
        }
    }

    public void StartTutorial()
    {
        tutorialActive = true;
        currentStep = 0;
        ShowStep(0);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
    }

    private void DetectNearbyFires()
    {
        // Clear old fires
        nearbyFires.Clear();

        // Find all fires in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, fireDetectionRadius, fireLayerMask);

        float totalWarmth = 0f;
        FireInstance closestFire = null;
        float closestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            FireInstance fire = collider.GetComponent<FireInstance>();
            if (fire != null && fire.GetState() == FireInstance.FireState.Burning)
            {
                float distance = Vector3.Distance(transform.position, fire.transform.position);
                float warmth = CalculateWarmthFromFire(fire, distance);

                if (warmth > 0)
                {
                    nearbyFires[fire] = warmth;
                    totalWarmth += warmth;

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestFire = fire;
                    }
                }
            }
        }

        // Update near fire status
        bool wasNearFire = isNearFire;
        isNearFire = nearbyFires.Count > 0;

        if (!wasNearFire && isNearFire && closestFire != null)
        {
            OnNearFireStart?.Invoke(closestFire);
}
        else if (wasNearFire && !isNearFire)
        {
            OnNearFireEnd?.Invoke();
        }
    }

    private float CalculateWarmthFromFire(FireInstance fire, float distance)
    {
        if (fire == null || distance > fireDetectionRadius)
            return 0f;

        // Get fire temperature and size
        float fireTemp = fire.GetCookingTemperature();
        float fireIntensity = fireTemp / 800f; // Normalize to 0-1

        // Calculate warmth based on inverse square law
        float warmth = fireIntensity * 20f / (1f + distance * distance * 0.1f);

        // Apply wind reduction if applicable
        if (WeatherSystem.Instance != null)
        {
            float windReduction = 1f - (WeatherSystem.Instance.WindStrength * 0.02f);
            warmth *= windReduction;
        }

        return Mathf.Clamp(warmth, 0f, 15f);
    }

    private void UpdateBodyTemperature()
    {
        // Calculate target temperature
        float targetTemp = ambientTemperature;

        // Add warmth from all nearby fires
        float totalWarmth = 0f;
        foreach (var kvp in nearbyFires)
        {
            if (kvp.Key != null)
            {
                totalWarmth += kvp.Value;
            }
        }

        targetTemp += totalWarmth;

        // Apply clothing insulation (reduces heat loss, not gain)
        if (targetTemp < currentBodyTemperature)
        {
            float heatLoss = currentBodyTemperature - targetTemp;
            heatLoss *= (1f - effectiveInsulation);
            targetTemp = currentBodyTemperature - heatLoss;
        }

        // Apply activity heat generation
        if (playerStats != null)
        {
            float activityHeat = CalculateActivityHeat();
            targetTemp += activityHeat;
        }

        // Clamp target temperature
        targetTemp = Mathf.Clamp(targetTemp, 25f, 45f);

        // Move current temperature toward target
        float changeRate = isNearFire ? rapidChangeRate : normalChangeRate;

        // Slower change when well-insulated
        if (!isNearFire)
        {
            changeRate *= (1f - effectiveInsulation * 0.5f);
        }

        float previousTemp = currentBodyTemperature;
        currentBodyTemperature = Mathf.MoveTowards(currentBodyTemperature, targetTemp, changeRate * Time.deltaTime);

        // Fire temperature changed event
        if (Mathf.Abs(previousTemp - currentBodyTemperature) > 0.01f)
        {
            OnTemperatureChanged?.Invoke(currentBodyTemperature);
        }
    }

    private float CalculateActivityHeat()
    {
        // Generate heat based on player activity
        float heat = 0f;

        // Check if sprinting
        var movement = GetComponent<PlayerMovementController>();
        if (movement != null && movement.IsSprinting)
        {
            heat += 2f;
        }
        else if (movement != null && movement.IsMoving)
        {
            heat += 0.5f;
        }

        // Check if in combat
        if (playerStats != null && playerStats.CurrentStamina < playerStats.MaxStamina * 0.5f)
        {
            heat += 1f;
        }

        return heat;
    }

    private void UpdateTemperatureEffects()
    {
        // Shivering
        bool shouldShiver = currentBodyTemperature < shiveringThreshold;
        if (shouldShiver != isShivering)
        {
            isShivering = shouldShiver;
            if (isShivering)
                OnStartShivering?.Invoke();
            else
                OnStopShivering?.Invoke();
        }

        // Sweating
        bool shouldSweat = currentBodyTemperature > sweatingThreshold;
        if (shouldSweat != isSweating)
        {
            isSweating = shouldSweat;
            if (isSweating)
                OnStartSweating?.Invoke();
            else
                OnStopSweating?.Invoke();
        }

        // Hypothermia
        bool shouldHaveHypothermia = currentBodyTemperature < criticalLowTemp;
        if (shouldHaveHypothermia != hasHypothermia)
        {
            hasHypothermia = shouldHaveHypothermia;
            if (hasHypothermia)
                OnHypothermiaStart?.Invoke();
            else
                OnHypothermiaEnd?.Invoke();
        }

        // Heat stroke
        bool shouldHaveHeatStroke = currentBodyTemperature > criticalHighTemp;
        if (shouldHaveHeatStroke != hasHeatStroke)
        {
            hasHeatStroke = shouldHaveHeatStroke;
            if (hasHeatStroke)
                OnHeatStrokeStart?.Invoke();
            else
                OnHeatStrokeEnd?.Invoke();
        }
    }

    private void ApplyTemperatureDamage()
    {
        if (playerStats == null) return;

        // Cold damage
        if (currentBodyTemperature < minSafeTemp)
        {
            float coldSeverity = (minSafeTemp - currentBodyTemperature) / 5f;
            coldSeverity = Mathf.Clamp01(coldSeverity);

            playerStats.ModifyHealth(-coldDamageRate * coldSeverity * Time.deltaTime);
            playerStats.ModifyStamina(-coldStaminaDrain * coldSeverity * Time.deltaTime);

            // Additional effects when very cold
            if (hasHypothermia)
            {
                playerStats.ModifyHealth(-coldDamageRate * 2f * Time.deltaTime);
                // Could add movement speed reduction, vision blur, etc.
            }
        }

        // Heat damage
        if (currentBodyTemperature > maxSafeTemp)
        {
            float heatSeverity = (currentBodyTemperature - maxSafeTemp) / 3f;
            heatSeverity = Mathf.Clamp01(heatSeverity);

            playerStats.ModifyHealth(-heatDamageRate * heatSeverity * Time.deltaTime);
            playerStats.ModifyThirst(-heatDamageRate * 2f * heatSeverity * Time.deltaTime);

            // Additional effects when very hot
            if (hasHeatStroke)
            {
                playerStats.ModifyHealth(-heatDamageRate * 3f * Time.deltaTime);
                playerStats.ModifyStamina(-heatDamageRate * 4f * Time.deltaTime);
            }
        }
    }

    private void UpdateAmbientTemperature()
    {
        // Base temperature from time of day
        float timeOfDay = (Time.time % 86400f) / 86400f; // 0-1 throughout the day
        float dailyTemp = Mathf.Sin(timeOfDay * Mathf.PI * 2f - Mathf.PI * 0.5f) * 10f + 15f;

        // Weather effects
        if (WeatherSystem.Instance != null)
        {
            switch (WeatherSystem.Instance.CurrentWeather)
            {
                case WeatherSystem.WeatherType.Clear:
                    dailyTemp += 5f;
                    break;
                case WeatherSystem.WeatherType.Cloudy:
                    dailyTemp -= 2f;
                    break;
                case WeatherSystem.WeatherType.LightRain:
                    dailyTemp -= 5f;
                    break;
                case WeatherSystem.WeatherType.HeavyRain:
                    dailyTemp -= 8f;
                    break;
                case WeatherSystem.WeatherType.Storm:
                    dailyTemp -= 10f;
                    break;
                case WeatherSystem.WeatherType.Snow:
                    dailyTemp = -5f;
                    break;
            }

            // Wind chill
            dailyTemp -= WeatherSystem.Instance.WindStrength * 0.5f;
        }

        // Season effects (if implemented)
        // dailyTemp += GetSeasonalModifier();

        // Indoor/outdoor check
        if (IsIndoors())
        {
            dailyTemp = Mathf.Max(dailyTemp, 18f); // Indoor minimum
        }

        ambientTemperature = dailyTemp;
    }

    private bool IsIndoors()
    {
        // Raycast up to check for roof
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.up, out hit, 10f))
        {
            // Check if hit object is a structure/building
            return hit.collider.CompareTag("Structure") || hit.collider.CompareTag("Building");
        }
        return false;
    }

    // Public methods
    public void SetClothingInsulation(float insulation)
    {
        effectiveInsulation = Mathf.Clamp01(insulation);
    }

    public void ApplyWarmth(float warmthAmount, float duration = 0f)
    {
        if (duration > 0)
        {
            StartCoroutine(ApplyWarmthOverTime(warmthAmount, duration));
        }
        else
        {
            currentBodyTemperature = Mathf.Min(currentBodyTemperature + warmthAmount, maxSafeTemp + 2f);
            OnTemperatureChanged?.Invoke(currentBodyTemperature);
        }
    }

    private System.Collections.IEnumerator ApplyWarmthOverTime(float warmthAmount, float duration)
    {
        float elapsed = 0f;
        float warmthPerSecond = warmthAmount / duration;

        while (elapsed < duration)
        {
            currentBodyTemperature = Mathf.Min(currentBodyTemperature + warmthPerSecond * Time.deltaTime, maxSafeTemp + 2f);
            OnTemperatureChanged?.Invoke(currentBodyTemperature);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void ApplyCold(float coldAmount, float duration = 0f)
    {
        if (duration > 0)
        {
            StartCoroutine(ApplyColdOverTime(coldAmount, duration));
        }
        else
        {
            currentBodyTemperature = Mathf.Max(currentBodyTemperature - coldAmount, criticalLowTemp - 2f);
            OnTemperatureChanged?.Invoke(currentBodyTemperature);
        }
    }

    private System.Collections.IEnumerator ApplyColdOverTime(float coldAmount, float duration)
    {
        float elapsed = 0f;
        float coldPerSecond = coldAmount / duration;

        while (elapsed < duration)
        {
            currentBodyTemperature = Mathf.Max(currentBodyTemperature - coldPerSecond * Time.deltaTime, criticalLowTemp - 2f);
            OnTemperatureChanged?.Invoke(currentBodyTemperature);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public float GetWarmthFromFires()
    {
        float total = 0f;
        foreach (var warmth in nearbyFires.Values)
        {
            total += warmth;
        }
        return total;
    }

    public FireInstance GetNearestFire()
    {
        FireInstance nearest = null;
        float minDistance = float.MaxValue;

        foreach (var fire in nearbyFires.Keys)
        {
            if (fire != null)
            {
                float dist = Vector3.Distance(transform.position, fire.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = fire;
                }
            }
        }

        return nearest;
    }

    // Debug
    private void OnDrawGizmosSelected()
    {
        // Draw fire detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fireDetectionRadius);

        // Draw lines to nearby fires
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            foreach (var fire in nearbyFires.Keys)
            {
                if (fire != null)
                {
                    Gizmos.DrawLine(transform.position, fire.transform.position);
                }
            }
        }
    }


private void ShowStep(int stepIndex)
    {
        if (stepIndex >= tutorialSteps.Length)
        {
            CompleteTutorial();
            return;
        }

        var step = tutorialSteps[stepIndex];

        if (titleText != null)
            titleText.text = step.title;

        if (descriptionText != null)
            descriptionText.text = step.description;

        // Handle highlight
        if (step.highlightTarget != null && highlightEffect != null)
        {
            highlightEffect.SetActive(true);
            highlightEffect.transform.position = step.highlightTarget.transform.position;
        }
        else if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }

        // Handle auto-advance
        if (step.waitTime > 0 && !step.requiresCompletion)
        {
            StartCoroutine(AutoAdvance(step.waitTime));
        }

        // Setup completion tracking
        if (step.requiresCompletion)
        {
            stepCompleted = false;
            StartCoroutine(WaitForCompletion(step.targetAction));
        }

        // Update button states
        if (nextButton != null)
        {
            nextButton.interactable = !step.requiresCompletion;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextStep);
        }
    }

    private IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextStep();
    }

    private IEnumerator WaitForCompletion(string action)
    {
        while (!stepCompleted)
        {
            stepCompleted = CheckActionCompleted(action);
            yield return new WaitForSeconds(0.5f);
        }

        // Auto advance when completed
        ShowCompletionMessage();
        yield return new WaitForSeconds(1f);
        NextStep();
    }

    private bool CheckActionCompleted(string action)
    {
        switch (action)
        {
            case "gather_materials":
                return CheckMaterialsGathered();

            case "build_campfire":
                return CheckCampfireBuilt();

            case "add_fuel":
                return CheckFuelAdded();

            case "light_fire":
                return CheckFireLit();

            case "get_warm":
                return CheckPlayerWarm();

            default:
                return true;
        }
    }

    private bool CheckMaterialsGathered()
    {
        var inventory = InventoryManager.Instance;
        if (inventory == null) return false;

        return inventory.HasItem("stone", 5) &&
               inventory.HasItem("stick", 3) &&
               inventory.HasItem("tinder", 2);
    }

    private bool CheckCampfireBuilt()
    {
        return FindObjectOfType<FireInstance>() != null;
    }

    private bool CheckFuelAdded()
    {
        var fire = FindObjectOfType<FireInstance>();
        return fire != null && fire.GetFuelPercentage() > 0;
    }

    private bool CheckFireLit()
    {
        var fire = FindObjectOfType<FireInstance>();
        return fire != null && fire.GetState() == FireInstance.FireState.Burning;
    }

    private bool CheckPlayerWarm()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var stats = player.GetComponent<PlayerStats>();
            return stats != null && stats.BodyTemperature > 36f;
        }
        return false;
    }

    private void ShowCompletionMessage()
    {
        if (descriptionText != null)
        {
            descriptionText.text = "✓ Step completed!";
            descriptionText.color = Color.green;
        }
    }

    public void NextStep()
    {
        currentStep++;
        ShowStep(currentStep);
    }

    public void SkipTutorial()
    {
        CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        tutorialActive = false;
        PlayerPrefs.SetInt("FireTutorialCompleted", 1);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        NotificationSystem.Instance?.ShowNotification(
            "Fire tutorial completed!",
            NotificationSystem.NotificationType.Achievement);
    }
}