using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;

/// <summary>
/// Comprehensive weather system that affects fire and player temperature
/// </summary>
public class WeatherSystem : MonoBehaviour
{
    private static WeatherSystem instance;
    public static WeatherSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<WeatherSystem>();
                if (instance == null)
                {
                    GameObject go = new GameObject("WeatherSystem");
                    instance = go.AddComponent<WeatherSystem>();
                }
            }
            return instance;
        }
    }

    [Header("Weather Settings")]
    [SerializeField] private WeatherType currentWeather = WeatherType.Clear;
    [SerializeField] private WeatherType targetWeather = WeatherType.Clear;
    [SerializeField] private float transitionDuration = 30f;
    [SerializeField] private bool autoChangeWeather = true;
    [SerializeField] private float weatherChangeInterval = 300f; // 5 minutes

    [Header("Current Conditions")]
    [SerializeField] private float temperature = 15f;
    [SerializeField] private float humidity = 50f;
    [SerializeField] private float windStrength = 5f;
    [SerializeField] private Vector3 windDirection = Vector3.forward;
    [SerializeField] private float rainIntensity = 0f;
    [SerializeField] private float snowIntensity = 0f;
    [SerializeField] private float fogDensity = 0f;
    [SerializeField] private float cloudCoverage = 0.2f;

    [Header("Weather Probabilities")]
    [SerializeField] private AnimationCurve clearProbability;
    [SerializeField] private AnimationCurve cloudyProbability;
    [SerializeField] private AnimationCurve rainProbability;
    [SerializeField] private AnimationCurve stormProbability;
    [SerializeField] private AnimationCurve snowProbability;
    [SerializeField] private AnimationCurve fogProbability;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem rainParticles;
    [SerializeField] private ParticleSystem snowParticles;
    [SerializeField] private ParticleSystem fogParticles;
    [SerializeField] private ParticleSystem windParticles;

    [Header("Audio")]
    [SerializeField] private AudioSource rainAudioSource;
    [SerializeField] private AudioSource windAudioSource;
    [SerializeField] private AudioSource thunderAudioSource;
    [SerializeField] private AudioClip[] thunderClips;

    [Header("Lighting")]
    [SerializeField] private Light sunLight;
    [SerializeField] private Light moonLight;
    [SerializeField] private Gradient sunColor;
    [SerializeField] private AnimationCurve sunIntensity;
    [SerializeField] private AnimationCurve fogDensityCurve;

    [Header("Wind Zone")]
    [SerializeField] private WindZone windZone;

    [Header("Effects on Environment")]
    [SerializeField] private float fireExtinguishRainThreshold = 0.7f;
    [SerializeField] private float fireSpreadWindMultiplier = 1.5f;
    [SerializeField] private float wetnessBuildupRate = 0.1f;
    [SerializeField] private float wetnessDryRate = 0.05f;

    // Properties
    public WeatherType CurrentWeather => currentWeather;
    public float Temperature => temperature;
    public float Humidity => humidity;
    public float WindStrength => windStrength;
    public Vector3 WindDirection => windDirection;
    public float RainIntensity => rainIntensity;
    public float SnowIntensity => snowIntensity;
    public float FogDensity => fogDensity;
    public bool IsRaining => rainIntensity > 0.1f;
    public bool IsSnowing => snowIntensity > 0.1f;
    public bool IsStorming => currentWeather == WeatherType.Storm;
    public bool IsFoggy => fogDensity > 0.3f;

    // Events
    public UnityEvent<WeatherType> OnWeatherChanged = new UnityEvent<WeatherType>();
    public UnityEvent<float> OnRainStarted = new UnityEvent<float>();
    public UnityEvent OnRainStopped = new UnityEvent();
    public UnityEvent<float> OnSnowStarted = new UnityEvent<float>();
    public UnityEvent OnSnowStopped = new UnityEvent();
    public UnityEvent OnThunderStrike = new UnityEvent();
    public UnityEvent<float> OnWindChanged = new UnityEvent<float>();

    // Private
    private float nextWeatherChangeTime;
    private float transitionProgress;
    private bool isTransitioning;
    private Coroutine weatherTransitionCoroutine;
    private Coroutine thunderCoroutine;
    private float timeOfDay;
    private List<FireInstance> affectedFires = new List<FireInstance>();

    public enum WeatherType
    {
        Clear,
        Cloudy,
        LightRain,
        HeavyRain,
        Storm,
        Snow,
        Blizzard,
        Fog,
        Windy,
        Heatwave,
        Sandstorm
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeComponents();
    }

    private void Start()
    {
        SetupInitialWeather();

        if (autoChangeWeather)
        {
            nextWeatherChangeTime = Time.time + weatherChangeInterval;
        }
    }

    private void Update()
    {
        UpdateTimeOfDay();
        UpdateWeatherEffects();

        if (autoChangeWeather && Time.time >= nextWeatherChangeTime)
        {
            ChangeToRandomWeather();
            nextWeatherChangeTime = Time.time + weatherChangeInterval;
        }

        ApplyWeatherToFires();
        UpdateLighting();
        UpdateAudio();
    }

    private void InitializeComponents()
    {
        // Create wind zone if missing
        if (windZone == null)
        {
            GameObject windObj = new GameObject("Wind Zone");
            windObj.transform.SetParent(transform);
            windZone = windObj.AddComponent<WindZone>();
            windZone.mode = WindZoneMode.Directional;
        }

        // Setup particle systems if missing
        if (rainParticles == null)
        {
            CreateRainParticles();
        }

        if (snowParticles == null)
        {
            CreateSnowParticles();
        }

        // Find sun/moon lights if not assigned
        if (sunLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional && light.name.ToLower().Contains("sun"))
                {
                    sunLight = light;
                    break;
                }
            }
        }
    }

    private void CreateRainParticles()
    {
        GameObject rainObj = new GameObject("Rain Particles");
        rainObj.transform.SetParent(transform);
        rainObj.transform.position = Vector3.up * 25f;
        rainParticles = rainObj.AddComponent<ParticleSystem>();

        var main = rainParticles.main;
        main.loop = true;
        main.startLifetime = 2f;
        main.startSpeed = 20f;
        main.maxParticles = 10000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var shape = rainParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(100, 1, 100);

        var velocityOverLifetime = rainParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-10f);

        var collision = rainParticles.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.bounce = 0.1f;
        collision.lifetimeLoss = 1f;

        rainParticles.Stop();
    }

    private void CreateSnowParticles()
    {
        GameObject snowObj = new GameObject("Snow Particles");
        snowObj.transform.SetParent(transform);
        snowObj.transform.position = Vector3.up * 25f;
        snowParticles = snowObj.AddComponent<ParticleSystem>();

        var main = snowParticles.main;
        main.loop = true;
        main.startLifetime = 5f;
        main.startSpeed = 2f;
        main.maxParticles = 5000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.1f;

        var shape = snowParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(100, 1, 100);

        var velocityOverLifetime = snowParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-1f, 1f);

        snowParticles.Stop();
    }

    private void SetupInitialWeather()
    {
        ApplyWeatherPreset(currentWeather);
    }

    public void SetWeather(WeatherType weather, float transitionTime = 0f)
    {
        if (weatherTransitionCoroutine != null)
        {
            StopCoroutine(weatherTransitionCoroutine);
        }

        targetWeather = weather;

        if (transitionTime <= 0)
        {
            currentWeather = weather;
            ApplyWeatherPreset(weather);
            OnWeatherChanged?.Invoke(weather);
        }
        else
        {
            weatherTransitionCoroutine = StartCoroutine(TransitionToWeather(weather, transitionTime));
        }
    }

    private IEnumerator TransitionToWeather(WeatherType newWeather, float duration)
    {
        isTransitioning = true;

        // Store initial values
        WeatherData startData = GetCurrentWeatherData();
        WeatherData targetData = GetWeatherPresetData(newWeather);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transitionProgress = elapsed / duration;

            // Interpolate weather values
            temperature = Mathf.Lerp(startData.temperature, targetData.temperature, transitionProgress);
            humidity = Mathf.Lerp(startData.humidity, targetData.humidity, transitionProgress);
            windStrength = Mathf.Lerp(startData.windStrength, targetData.windStrength, transitionProgress);
            rainIntensity = Mathf.Lerp(startData.rainIntensity, targetData.rainIntensity, transitionProgress);
            snowIntensity = Mathf.Lerp(startData.snowIntensity, targetData.snowIntensity, transitionProgress);
            fogDensity = Mathf.Lerp(startData.fogDensity, targetData.fogDensity, transitionProgress);
            cloudCoverage = Mathf.Lerp(startData.cloudCoverage, targetData.cloudCoverage, transitionProgress);

            UpdateWeatherEffects();

            yield return null;
        }

        currentWeather = newWeather;
        ApplyWeatherPreset(newWeather);
        OnWeatherChanged?.Invoke(newWeather);

        isTransitioning = false;
        weatherTransitionCoroutine = null;
    }

    private void ApplyWeatherPreset(WeatherType weather)
    {
        WeatherData data = GetWeatherPresetData(weather);

        temperature = data.temperature;
        humidity = data.humidity;
        windStrength = data.windStrength;
        rainIntensity = data.rainIntensity;
        snowIntensity = data.snowIntensity;
        fogDensity = data.fogDensity;
        cloudCoverage = data.cloudCoverage;

        UpdateWeatherEffects();

        // Handle special weather effects
        switch (weather)
        {
            case WeatherType.Storm:
                if (thunderCoroutine == null)
                {
                    thunderCoroutine = StartCoroutine(ThunderEffect());
                }
                break;
            default:
                if (thunderCoroutine != null)
                {
                    StopCoroutine(thunderCoroutine);
                    thunderCoroutine = null;
                }
                break;
        }
    }

    private WeatherData GetWeatherPresetData(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Clear => new WeatherData { temperature = 22f, humidity = 40f, windStrength = 2f, cloudCoverage = 0.1f },
            WeatherType.Cloudy => new WeatherData { temperature = 18f, humidity = 60f, windStrength = 5f, cloudCoverage = 0.7f },
            WeatherType.LightRain => new WeatherData { temperature = 15f, humidity = 80f, windStrength = 8f, rainIntensity = 0.3f, cloudCoverage = 0.8f },
            WeatherType.HeavyRain => new WeatherData { temperature = 12f, humidity = 90f, windStrength = 12f, rainIntensity = 0.8f, cloudCoverage = 0.95f },
            WeatherType.Storm => new WeatherData { temperature = 10f, humidity = 95f, windStrength = 20f, rainIntensity = 1f, cloudCoverage = 1f },
            WeatherType.Snow => new WeatherData { temperature = -2f, humidity = 70f, windStrength = 6f, snowIntensity = 0.5f, cloudCoverage = 0.9f },
            WeatherType.Blizzard => new WeatherData { temperature = -10f, humidity = 80f, windStrength = 25f, snowIntensity = 1f, cloudCoverage = 1f },
            WeatherType.Fog => new WeatherData { temperature = 14f, humidity = 95f, windStrength = 1f, fogDensity = 0.8f, cloudCoverage = 0.8f },
            WeatherType.Windy => new WeatherData { temperature = 18f, humidity = 30f, windStrength = 18f, cloudCoverage = 0.3f },
            WeatherType.Heatwave => new WeatherData { temperature = 35f, humidity = 20f, windStrength = 3f, cloudCoverage = 0f },
            _ => new WeatherData { temperature = 15f, humidity = 50f, windStrength = 5f }
        };
    }

    private WeatherData GetCurrentWeatherData()
    {
        return new WeatherData
        {
            temperature = this.temperature,
            humidity = this.humidity,
            windStrength = this.windStrength,
            rainIntensity = this.rainIntensity,
            snowIntensity = this.snowIntensity,
            fogDensity = this.fogDensity,
            cloudCoverage = this.cloudCoverage
        };
    }

    private void UpdateWeatherEffects()
    {
        // Update particles
        if (rainParticles != null)
        {
            var emission = rainParticles.emission;
            emission.rateOverTime = rainIntensity * 2000f;

            if (rainIntensity > 0 && !rainParticles.isPlaying)
            {
                rainParticles.Play();
                OnRainStarted?.Invoke(rainIntensity);
            }
            else if (rainIntensity <= 0 && rainParticles.isPlaying)
            {
                rainParticles.Stop();
                OnRainStopped?.Invoke();
            }
        }

        if (snowParticles != null)
        {
            var emission = snowParticles.emission;
            emission.rateOverTime = snowIntensity * 1000f;

            if (snowIntensity > 0 && !snowParticles.isPlaying)
            {
                snowParticles.Play();
                OnSnowStarted?.Invoke(snowIntensity);
            }
            else if (snowIntensity <= 0 && snowParticles.isPlaying)
            {
                snowParticles.Stop();
                OnSnowStopped?.Invoke();
            }
        }

        // Update wind
        if (windZone != null)
        {
            windZone.windMain = windStrength * 0.5f;
            windZone.windTurbulence = windStrength * 0.3f;
            windZone.windPulseMagnitude = windStrength * 0.1f;
            windZone.windPulseFrequency = 0.5f;

            // Random wind direction changes
            if (UnityEngine.Random.Range(0f, 1f) < 0.01f)
            {
                windDirection = Quaternion.Euler(0, UnityEngine.Random.Range(-30f, 30f), 0) * windDirection;
                windDirection.Normalize();
            }

            windZone.transform.rotation = Quaternion.LookRotation(windDirection);
        }

        // Update fog
        RenderSettings.fog = fogDensity > 0.1f;
        RenderSettings.fogDensity = fogDensity * 0.1f;
        RenderSettings.fogMode = FogMode.Exponential;

        // Update skybox tint based on weather
        if (RenderSettings.skybox != null)
        {
            Color skyTint = Color.Lerp(Color.white, Color.gray, cloudCoverage);
            RenderSettings.skybox.SetColor("_Tint", skyTint);
        }
    }

    private void UpdateTimeOfDay()
    {
        // Simple day/night cycle (24 hour = 20 minutes real time)
        float dayDuration = 1200f; // 20 minutes
        timeOfDay = (Time.time % dayDuration) / dayDuration;
    }

    private void UpdateLighting()
    {
        if (sunLight != null)
        {
            // Rotate sun based on time
            float sunAngle = timeOfDay * 360f - 90f;
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 30f, 0);

            // Adjust intensity
            float intensity = sunIntensity != null ? sunIntensity.Evaluate(timeOfDay) : 1f;
            intensity *= (1f - cloudCoverage * 0.5f); // Clouds reduce light
            intensity *= (1f - rainIntensity * 0.3f); // Rain reduces light
            sunLight.intensity = intensity;

            // Adjust color
            if (sunColor != null)
            {
                sunLight.color = sunColor.Evaluate(timeOfDay);
            }
        }
    }

    private void UpdateAudio()
    {
        // Rain audio
        if (rainAudioSource != null)
        {
            rainAudioSource.volume = rainIntensity;
            if (rainIntensity > 0 && !rainAudioSource.isPlaying)
            {
                rainAudioSource.Play();
            }
            else if (rainIntensity <= 0 && rainAudioSource.isPlaying)
            {
                rainAudioSource.Stop();
            }
        }

        // Wind audio
        if (windAudioSource != null)
        {
            windAudioSource.volume = Mathf.Clamp01(windStrength / 20f);
            if (windStrength > 2f && !windAudioSource.isPlaying)
            {
                windAudioSource.Play();
            }
            else if (windStrength <= 2f && windAudioSource.isPlaying)
            {
                windAudioSource.Stop();
            }
        }
    }

    private IEnumerator ThunderEffect()
    {
        while (currentWeather == WeatherType.Storm)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(10f, 30f));

            // Lightning flash
            if (sunLight != null)
            {
                float originalIntensity = sunLight.intensity;
                sunLight.intensity = 10f;
                yield return new WaitForSeconds(0.1f);
                sunLight.intensity = originalIntensity;
            }

            // Thunder sound
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 2f));

            if (thunderAudioSource != null && thunderClips != null && thunderClips.Length > 0)
            {
                AudioClip clip = thunderClips[UnityEngine.Random.Range(0, thunderClips.Length)];
                thunderAudioSource.PlayOneShot(clip);
            }

            OnThunderStrike?.Invoke();
        }

        thunderCoroutine = null;
    }

    private void ApplyWeatherToFires()
    {
        // Find all active fires
        FireInstance[] fires = FindObjectsOfType<FireInstance>();

        foreach (var fire in fires)
        {
            if (fire == null) continue;

            // Apply rain effects
            if (rainIntensity > 0)
            {
                fire.ApplyRain(rainIntensity);

                // Heavy rain can extinguish weak fires
                if (rainIntensity > fireExtinguishRainThreshold &&
                    fire.GetCookingTemperature() < 300f)
                {
                    fire.ExtinguishFire("Extinguished by heavy rain");
                }
            }

            // Apply wind effects
            if (windStrength > 5f)
            {
                fire.ApplyWind(windStrength, windDirection);

                // Strong wind can spread fire
                if (windStrength > 15f)
                {
                    // Implement fire spread logic
                }
            }

            // Snow reduces fire temperature
            if (snowIntensity > 0)
            {
                fire.ApplySnow(snowIntensity);
            }
        }
    }

    private void ChangeToRandomWeather()
    {
        // Calculate probabilities based on current time and season
        float[] probabilities = new float[System.Enum.GetValues(typeof(WeatherType)).Length];

        // Simple probability calculation (can be enhanced with seasons, etc.)
        probabilities[(int)WeatherType.Clear] = 0.3f;
        probabilities[(int)WeatherType.Cloudy] = 0.25f;
        probabilities[(int)WeatherType.LightRain] = 0.2f;
        probabilities[(int)WeatherType.HeavyRain] = 0.1f;
        probabilities[(int)WeatherType.Storm] = 0.05f;
        probabilities[(int)WeatherType.Fog] = 0.05f;
        probabilities[(int)WeatherType.Windy] = 0.05f;

        // Adjust based on current weather (weather persistence)
        int currentIndex = (int)currentWeather;
        probabilities[currentIndex] *= 1.5f;

        // Normalize probabilities
        float total = 0f;
        foreach (float p in probabilities)
        {
            total += p;
        }

        for (int i = 0; i < probabilities.Length; i++)
        {
            probabilities[i] /= total;
        }

        // Select random weather
        float random = UnityEngine.Random.Range(0f, 1f);
        float cumulative = 0f;

        for (int i = 0; i < probabilities.Length; i++)
        {
            cumulative += probabilities[i];
            if (random <= cumulative)
            {
                SetWeather((WeatherType)i, transitionDuration);
                break;
            }
        }
    }

    // Struct for weather data
    [System.Serializable]
    private struct WeatherData
    {
        public float temperature;
        public float humidity;
        public float windStrength;
        public float rainIntensity;
        public float snowIntensity;
        public float fogDensity;
        public float cloudCoverage;
    }
}