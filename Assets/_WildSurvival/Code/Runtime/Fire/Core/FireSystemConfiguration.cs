using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
/// <summary>
/// Preset configuration for different fire types
/// </summary>
[System.Serializable]
public class FirePreset
{
    public string presetName = "Campfire";
    public FireInstance.FireType type; //= FireInstance.FireType.Campfire;

    [Header("Temperature Settings")]
    public float minTemperature = 100f;
    public float maxTemperature = 600f;
    public float optimalTemperature = 400f;
    public float ignitionTemperature = 200f;

    [Header("Fuel Settings")]
    public float maxFuelCapacity = 100f;
    public float fuelBurnRate = 1f;
    public FireInstance.FuelType[] acceptedFuelTypes;

    [Header("Visual Settings")]
    public float lightIntensity = 2f;
    public float lightRange = 10f;
    public Color lightColor = new Color(1f, 0.5f, 0.2f);
    public float smokeEmissionRate = 5f;

    [Header("Gameplay Settings")]
    public float cookingEfficiency = 1f;
    public float warmthRadius = 5f;
    public float spreadChance = 0.1f;
    public bool canSpread = false;
    public bool requiresIgnitionSource = true;

    [Header("Audio")]
    public AudioClip[] igniteSounds;
    public AudioClip[] burningSounds;
    public AudioClip[] extinguishSounds;

    [Header("Particles")]
    public GameObject flamePrefab;
    public GameObject smokePrefab;
    public GameObject embersPrefab;

    public FirePreset()
    {
        acceptedFuelTypes = new FireInstance.FuelType[]
        {
            FireInstance.FuelType.Tinder,
            FireInstance.FuelType.Kindling,
            FireInstance.FuelType.Softwood,
            FireInstance.FuelType.Hardwood
        };
    }

    /// <summary>
    /// Create a campfire preset
    /// </summary>
    public static FirePreset CreateCampfirePreset()
    {
        return new FirePreset
        {
            presetName = "Campfire",
            type = FireInstance.FireType.Campfire,
            maxTemperature = 600f,
            maxFuelCapacity = 100f,
            cookingEfficiency = 1f,
            warmthRadius = 5f
        };
    }

    /// <summary>
    /// Create a torch preset
    /// </summary>
    public static FirePreset CreateTorchPreset()
    {
        return new FirePreset
        {
            presetName = "Torch",
            type = FireInstance.FireType.Torch,
            maxTemperature = 400f,
            maxFuelCapacity = 30f,
            cookingEfficiency = 0.2f,
            warmthRadius = 2f,
            lightIntensity = 1.5f,
            lightRange = 8f
        };
    }

    /// <summary>
    /// Create a forge preset
    /// </summary>
    public static FirePreset CreateForgePreset()
    {
        return new FirePreset
        {
            presetName = "Forge",
            type = FireInstance.FireType.Forge,
            maxTemperature = 1200f,
            maxFuelCapacity = 200f,
            cookingEfficiency = 0f,
            warmthRadius = 3f,
            acceptedFuelTypes = new FireInstance.FuelType[]
            {
                FireInstance.FuelType.Coal,
                FireInstance.FuelType.Charcoal,
                FireInstance.FuelType.Hardwood
            }
        };
    }

    /// <summary>
    /// Create a signal fire preset
    /// </summary>
    public static FirePreset CreateSignalFirePreset()
    {
        return new FirePreset
        {
            presetName = "Signal Fire",
            type = FireInstance.FireType.SignalFire,
            maxTemperature = 800f,
            maxFuelCapacity = 150f,
            cookingEfficiency = 0.5f,
            warmthRadius = 8f,
            smokeEmissionRate = 20f,
            lightIntensity = 4f,
            lightRange = 20f
        };
    }
}




/// <summary>
/// Configuration assets for the fire system
/// </summary>
[CreateAssetMenu(fileName = "FireSystemConfig", menuName = "WildSurvival/Fire/Fire System Config")]
public class FireSystemConfiguration : ScriptableObject
{

    [Header("Fire Presets")]
    public List<FirePreset> firePresets = new List<FirePreset>();

    [Header("Global Fire Settings")]
    public float globalWindStrength = 5f;
    public float globalRainIntensity = 0f;
    public AnimationCurve temperatureFalloffCurve;
    public AnimationCurve windEffectCurve;

    [Header("Fuel Properties")]
    public List<FuelTypeProperties> fuelProperties = new();

    private FireInstance.FireType fireTypeToCreate = FireInstance.FireType.Campfire;

    [Header("Fire Types")]
    public List<FireTypeConfiguration> fireTypes = new();

    [Header("Performance")]
    public float updateInterval = 0.2f;
    public int maxActiveFires = 20;
    public float fireGridCellSize = 50f;

    [Header("Visual Effects")]
    public GameObject fireParticlesPrefab;
    public GameObject smokeParticlesPrefab;
    public GameObject embersParticlesPrefab;
    public Material fireMaterial;
    public AudioClip[] fireSounds;

    [System.Serializable]
    public class FuelTypeProperties
    {
        public FireInstance.FuelType type;
        public string displayName;
        public float burnTemperature = 400f;
        public float burnDuration = 10f; // Minutes
        public float heatOutput = 1f;
        public float ignitionTemperature = 200f;
        public float smokeAmount = 1f;
        public Color flameColor = Color.white;
        public bool spreadsFire = false;
        public float value = 1f; // Fuel units per item
    }

    [System.Serializable]
    public class FireTypeConfiguration
    {
        public FireInstance.FireType type;//fireType = FireInstance.FireType.Campfire;
        public string displayName;
        public float maxTemperature = 800f;
        public float maxFuelCapacity = 100f;
        public float baseBurnRate = 1f;
        public float heatRadius = 3f;
        public float lightRadius = 8f;
        public float predatorDeterrentRadius = 10f;
        public bool canCook = true;
        public bool canSmelt = false;
        public bool canSpread = false;
        public GameObject prefab;
    }

    public FirePreset GetPreset(FireInstance.FireType fireType)
    {
        foreach (var preset in firePresets)
        {
            if (preset.type == fireType)
                return preset;
        }

        // Return default campfire preset if not found
        return FirePreset.CreateCampfirePreset();
    }

    private void OnEnable()
    {
        // Initialize with default presets if empty
        if (firePresets == null || firePresets.Count == 0)
        {
            firePresets = new List<FirePreset>
            {
                FirePreset.CreateCampfirePreset(),
                FirePreset.CreateTorchPreset(),
                FirePreset.CreateForgePreset(),
                FirePreset.CreateSignalFirePreset()
            };
        }
    }
}

/// <summary>
/// Editor tool for setting up fire system
/// </summary>
#if UNITY_EDITOR
public class FireSystemSetupWizard : EditorWindow
{
    private FireSystemConfiguration config;
    private GameObject selectedPrefab;
    private FireInstance.FireType fireTypeToCreate;

    [MenuItem("Tools/Wild Survival/Fire System Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<FireSystemSetupWizard>("Fire System Setup");
        window.minSize = new Vector2(400, 600);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fire System Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // Configuration
        config = (FireSystemConfiguration)EditorGUILayout.ObjectField(
            "Fire Config", config, typeof(FireSystemConfiguration), false);

        if (config == null)
        {
            if (GUILayout.Button("Create New Configuration"))
            {
                CreateConfiguration();
            }
            return;
        }

        EditorGUILayout.Space(10);

        // Quick Setup
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Fire Prefabs"))
        {
            CreateFirePrefabs();
        }

        if (GUILayout.Button("Setup Fire Particles"))
        {
            SetupParticles();
        }

        if (GUILayout.Button("Generate Fuel Items"))
        {
            GenerateFuelItems();
        }

        EditorGUILayout.Space(10);

        // Test Fire Creation
        EditorGUILayout.LabelField("Test Fire Creation", EditorStyles.boldLabel);

        fireTypeToCreate = (FireInstance.FireType)EditorGUILayout.EnumPopup(
            "Fire Type", fireTypeToCreate);

        if (GUILayout.Button("Create Test Fire in Scene"))
        {
            CreateTestFire();
        }

        EditorGUILayout.Space(10);

        // Validation
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate Setup"))
        {
            ValidateSetup();
        }
    }

    private void CreateConfiguration()
    {
        config = CreateInstance<FireSystemConfiguration>();

        // Set default values
        config.fuelProperties = new List<FireSystemConfiguration.FuelTypeProperties>
        {
            new() { type = FireInstance.FuelType.Tinder, displayName = "Tinder",
                    burnTemperature = 300f, burnDuration = 2f, heatOutput = 0.5f },
            new() { type = FireInstance.FuelType.Kindling, displayName = "Kindling",
                    burnTemperature = 400f, burnDuration = 5f, heatOutput = 0.8f },
            new() { type = FireInstance.FuelType.Logs, displayName = "Logs",
                    burnTemperature = 600f, burnDuration = 20f, heatOutput = 1f },
            new() { type = FireInstance.FuelType.Coal, displayName = "Coal",
                    burnTemperature = 800f, burnDuration = 60f, heatOutput = 1.5f }
        };

        config.fireTypes = new List<FireSystemConfiguration.FireTypeConfiguration>
        {
            new() { type = FireInstance.FireType.Campfire, displayName = "Campfire",
                    maxTemperature = 600f, maxFuelCapacity = 100f, canCook = true },
            new() { type = FireInstance.FireType.Torch, displayName = "Torch",
                    maxTemperature = 400f, maxFuelCapacity = 20f, canCook = false },
            new() { type = FireInstance.FireType.Forge, displayName = "Forge",
                    maxTemperature = 1000f, maxFuelCapacity = 200f, canSmelt = true }
        };

        // Create default curves
        config.temperatureFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        config.windEffectCurve = AnimationCurve.Linear(0, 0, 1, 1);

        string path = "Assets/_Project/Data/Config/FireSystemConfig.asset";
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", "Fire System Configuration created!", "OK");
    }

    private void CreateFirePrefabs()
    {
        string prefabPath = "Assets/_Project/Prefabs/Fire/";

        if (!AssetDatabase.IsValidFolder(prefabPath))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Fire");
        }

        foreach (var fireType in config.fireTypes)
        {
            if (fireType.prefab == null)
            {
                // Create prefab
                var fireObject = new GameObject($"Fire_{fireType.displayName}");

                // Add components
                var fireInstance = fireObject.AddComponent<FireInstance>();
                var audioSource = fireObject.AddComponent<AudioSource>();

                // Create light
                var lightObject = new GameObject("Fire Light");
                lightObject.transform.SetParent(fireObject.transform);
                lightObject.transform.localPosition = Vector3.up * 0.5f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.5f, 0.1f);
                light.range = fireType.lightRadius;

                // Create particle holder
                var particleHolder = new GameObject("Particles");
                particleHolder.transform.SetParent(fireObject.transform);

                // Save prefab
                string path = prefabPath + fireObject.name + ".prefab";
                fireType.prefab = PrefabUtility.SaveAsPrefabAsset(fireObject, path);

                DestroyImmediate(fireObject);
            }
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", "Fire prefabs created!", "OK");
    }

    private void SetupParticles()
    {
        // Create fire particle system
        if (config.fireParticlesPrefab == null)
        {
            var fireParticles = CreateFireParticleSystem();
            string path = "Assets/_Project/Prefabs/Particles/FireParticles.prefab";
            config.fireParticlesPrefab = PrefabUtility.SaveAsPrefabAsset(fireParticles, path);
            DestroyImmediate(fireParticles);
        }

        // Create smoke particle system
        if (config.smokeParticlesPrefab == null)
        {
            var smokeParticles = CreateSmokeParticleSystem();
            string path = "Assets/_Project/Prefabs/Particles/SmokeParticles.prefab";
            config.smokeParticlesPrefab = PrefabUtility.SaveAsPrefabAsset(smokeParticles, path);
            DestroyImmediate(smokeParticles);
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", "Particle systems created!", "OK");
    }

    private GameObject CreateFireParticleSystem()
    {
        var obj = new GameObject("Fire Particles");
        var ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 1.5f;
        main.startSpeed = 2f;
        main.startSize = 0.5f;
        main.startColor = new Color(1f, 0.5f, 0.1f);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.5f;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f),
                new GradientColorKey(new Color(1f, 0f, 0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        return obj;
    }

    private GameObject CreateSmokeParticleSystem()
    {
        var obj = new GameObject("Smoke Particles");
        var ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 5f;
        main.startSpeed = 1f;
        main.startSize = 0.3f;
        main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.2f;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(2f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(1f, 2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0.0f),
                new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0.0f),
                new GradientAlphaKey(0.3f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        var emission = ps.emission;
        emission.rateOverTime = 5f;

        return obj;
    }

    private void GenerateFuelItems()
    {
        var itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabase>(
            "Assets/_Project/Data/ItemDatabase.asset");

        if (itemDB == null)
        {
            EditorUtility.DisplayDialog("Error", "Item Database not found!", "OK");
            return;
        }

        // Create fuel items
        CreateFuelItem(itemDB, "fuel_tinder", "Tinder", 0.01f, 100);
        CreateFuelItem(itemDB, "fuel_kindling", "Kindling", 0.1f, 50);
        CreateFuelItem(itemDB, "fuel_firewood", "Firewood", 2f, 10);
        CreateFuelItem(itemDB, "fuel_charcoal", "Charcoal", 0.5f, 20);
        CreateFuelItem(itemDB, "fuel_coal", "Coal", 1f, 20);
        CreateFuelItem(itemDB, "fuel_torch", "Torch", 0.5f, 5);

        EditorUtility.SetDirty(itemDB);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", "Fuel items created!", "OK");
    }

    private void CreateFuelItem(ItemDatabase db, string id, string name, float weight, int stackSize)
    {
        var item = CreateInstance<ItemDefinition>();
        item.itemID = id;
        item.displayName = name;
        item.description = $"Fuel for fires. {name} burns well.";
        item.primaryCategory = ItemCategory.Fuel;
        item.weight = weight;
        item.maxStackSize = stackSize;
        item.gridSize = Vector2Int.one;
        item.tags = new[] { ItemTag.Fuel };

        string path = $"Assets/_Project/Data/Items/{id}.asset";
        AssetDatabase.CreateAsset(item, path);

        db.AddItem(item);
    }

    private void CreateTestFire()
    {
        // TEMPORARY: Comment out until FirePrefabFactory is recognized
        /*
    var factory = FindObjectOfType<FirePrefabFactory>();
    if (factory == null)
    {
        var factoryObj = new GameObject("Fire Factory");
        factory = factoryObj.AddComponent<FirePrefabFactory>();
    }

    // Create at scene view camera position
    var sceneView = SceneView.lastActiveSceneView;
    Vector3 position = sceneView != null ?
        sceneView.camera.transform.position + sceneView.camera.transform.forward * 5f :
        Vector3.zero;

    position.y = 0; // Place on ground

    var fire = factory.CreateFire(fireTypeToCreate, position);

    if (fire != null)
    {
        Selection.activeGameObject = fire;
        EditorGUIUtility.PingObject(fire);
    }
        */
}

private void ValidateSetup()
{
    var issues = new List<string>();

    // Check configuration
    if (config == null)
    {
        issues.Add("No configuration assigned");
        return;
    }

    // Check prefabs
    foreach (var fireType in config.fireTypes)
    {
        if (fireType.prefab == null)
            issues.Add($"No prefab for fire type: {fireType.displayName}");
    }

    // Check particles
    if (config.fireParticlesPrefab == null)
        issues.Add("Fire particles prefab missing");
    if (config.smokeParticlesPrefab == null)
        issues.Add("Smoke particles prefab missing");

    // Check database
    var itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabase>(
        "Assets/_Project/Data/ItemDatabase.asset");
    if (itemDB == null)
        issues.Add("Item Database not found");

    // Display results
    if (issues.Count == 0)
    {
        EditorUtility.DisplayDialog("Validation Passed",
            "Fire system is properly configured!", "OK");
    }
    else
    {
        EditorUtility.DisplayDialog("Validation Failed",
            "Issues found:\n" + string.Join("\n", issues), "OK");
    }
}
}
#endif