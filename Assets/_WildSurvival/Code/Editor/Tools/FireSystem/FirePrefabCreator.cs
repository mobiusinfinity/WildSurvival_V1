using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Creates all necessary fire prefabs for testing
/// </summary>
public class FirePrefabCreator : EditorWindow
{
    [MenuItem("Tools/Wild Survival/Fire System/Create All Fire Prefabs")]
    public static void CreateAllFirePrefabs()
    {
        CreateCampfirePrefab();
        CreateTorchPrefab();
        CreateForgePrefab();
        CreateSignalFirePrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ All fire prefabs created in Assets/_WildSurvival/Prefabs/Fire/");
    }

    [MenuItem("Tools/Wild Survival/Fire System/Create Test Scene")]
    public static void CreateTestScene()
    {
        FireTestSceneBuilder.CreateTestScene();
    }

    private static void CreateCampfirePrefab()
    {
        // Create root GameObject
        GameObject campfire = new GameObject("Campfire");

        // Add FireInstance component
        var fireInstance = campfire.AddComponent<FireInstance>();

        // Add Collider
        var collider = campfire.AddComponent<SphereCollider>();
        collider.radius = 2f;
        collider.isTrigger = true;

        // Set layer
        int fireLayer = LayerMask.NameToLayer("Fire");
        if (fireLayer == -1) fireLayer = 0;
        campfire.layer = fireLayer;

        // Create visual structure
        CreateCampfireVisuals(campfire);

        // Create particle systems
        CreateFireParticles(campfire);

        // Add light
        CreateFireLight(campfire);

        // Add audio source
        var audioSource = campfire.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 10f;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;

        // Save as prefab
        SavePrefab(campfire, "Campfire");
        DestroyImmediate(campfire);
    }

    private static void CreateCampfireVisuals(GameObject parent)
    {
        // Stone ring
        GameObject stoneRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stoneRing.name = "StoneRing";
        stoneRing.transform.SetParent(parent.transform);
        stoneRing.transform.localPosition = Vector3.zero;
        stoneRing.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);

        // Make it dark gray like stones
        var stoneRenderer = stoneRing.GetComponent<Renderer>();
        stoneRenderer.material = new Material(Shader.Find("Standard"));
        stoneRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);

        // Individual stones for detail
        for (int i = 0; i < 8; i++)
        {
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.name = $"Stone_{i}";
            stone.transform.SetParent(stoneRing.transform);

            float angle = i * (360f / 8f) * Mathf.Deg2Rad;
            float radius = 0.6f;
            stone.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * radius,
                0.5f,
                Mathf.Sin(angle) * radius
            );
            stone.transform.localScale = Vector3.one * Random.Range(0.3f, 0.5f);

            var renderer = stone.GetComponent<Renderer>();
            renderer.material = stoneRenderer.material;
        }

        // Wood pile (starts visible, burns away)
        GameObject woodPile = new GameObject("WoodPile");
        woodPile.transform.SetParent(parent.transform);
        woodPile.transform.localPosition = Vector3.up * 0.1f;

        // Create some logs
        for (int i = 0; i < 3; i++)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = $"Log_{i}";
            log.transform.SetParent(woodPile.transform);
            log.transform.localPosition = new Vector3(
                Random.Range(-0.2f, 0.2f),
                i * 0.1f,
                Random.Range(-0.2f, 0.2f)
            );
            log.transform.localRotation = Quaternion.Euler(90, Random.Range(0, 360), 0);
            log.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);

            var renderer = log.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(0.4f, 0.25f, 0.1f); // Brown wood color
        }

        // Ash pile (for when fire is out)
        GameObject ashPile = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ashPile.name = "AshPile";
        ashPile.transform.SetParent(parent.transform);
        ashPile.transform.localPosition = Vector3.up * 0.01f;
        ashPile.transform.localScale = Vector3.one * 0.1f;
        ashPile.SetActive(false); // Hidden by default

        var ashRenderer = ashPile.GetComponent<Renderer>();
        ashRenderer.material = new Material(Shader.Find("Standard"));
        ashRenderer.material.color = new Color(0.2f, 0.2f, 0.2f); // Dark gray ash
    }

    private static void CreateFireParticles(GameObject parent)
    {
        // Main fire particles
        GameObject fireObj = new GameObject("FireParticles");
        fireObj.transform.SetParent(parent.transform);
        fireObj.transform.localPosition = Vector3.up * 0.2f;

        var firePS = fireObj.AddComponent<ParticleSystem>();
        var main = firePS.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor = new Color(1f, 0.5f, 0.1f);
        main.maxParticles = 50;

        var shape = firePS.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.3f;

        var velocityOverLifetime = firePS.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, 3f);

        var colorOverLifetime = firePS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0.3f),
                new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.6f),
                new GradientColorKey(new Color(0.5f, 0.1f, 0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.5f),
                new GradientAlphaKey(0.3f, 0.8f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = firePS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var emission = firePS.emission;
        emission.rateOverTime = 30f;

        // Smoke particles
        GameObject smokeObj = new GameObject("SmokeParticles");
        smokeObj.transform.SetParent(parent.transform);
        smokeObj.transform.localPosition = Vector3.up * 0.5f;

        var smokePS = smokeObj.AddComponent<ParticleSystem>();
        var smokeMain = smokePS.main;
        smokeMain.duration = 1f;
        smokeMain.loop = true;
        smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        smokeMain.startColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        smokeMain.maxParticles = 30;

        var smokeShape = smokePS.shape;
        smokeShape.shapeType = ParticleSystemShapeType.Cone;
        smokeShape.angle = 10f;
        smokeShape.radius = 0.2f;

        var smokeVelocity = smokePS.velocityOverLifetime;
        smokeVelocity.enabled = true;
        smokeVelocity.space = ParticleSystemSimulationSpace.World;
        smokeVelocity.y = new ParticleSystem.MinMaxCurve(1f, 2f);

        var smokeSizeOverLifetime = smokePS.sizeOverLifetime;
        smokeSizeOverLifetime.enabled = true;
        AnimationCurve smokeSizeCurve = new AnimationCurve();
        smokeSizeCurve.AddKey(0f, 0.3f);
        smokeSizeCurve.AddKey(0.5f, 1f);
        smokeSizeCurve.AddKey(1f, 2f);
        smokeSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, smokeSizeCurve);

        var smokeColorOverLifetime = smokePS.colorOverLifetime;
        smokeColorOverLifetime.enabled = true;
        Gradient smokeGradient = new Gradient();
        smokeGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 0.0f),
                new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0.0f),
                new GradientAlphaKey(0.3f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        smokeColorOverLifetime.color = smokeGradient;

        var smokeEmission = smokePS.emission;
        smokeEmission.rateOverTime = 5f;

        // Ember particles
        GameObject emberObj = new GameObject("EmberParticles");
        emberObj.transform.SetParent(parent.transform);
        emberObj.transform.localPosition = Vector3.up * 0.3f;

        var emberPS = emberObj.AddComponent<ParticleSystem>();
        var emberMain = emberPS.main;
        emberMain.duration = 1f;
        emberMain.loop = true;
        emberMain.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        emberMain.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        emberMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        emberMain.startColor = new Color(1f, 0.3f, 0f);
        emberMain.maxParticles = 20;
        emberMain.gravityModifier = -0.1f; // Floats up

        var emberShape = emberPS.shape;
        emberShape.shapeType = ParticleSystemShapeType.Circle;
        emberShape.radius = 0.3f;

        var emberEmission = emberPS.emission;
        emberEmission.rateOverTime = 3f;

        var emberVelocity = emberPS.velocityOverLifetime;
        emberVelocity.enabled = true;
        emberVelocity.space = ParticleSystemSimulationSpace.World;
        emberVelocity.radial = new ParticleSystem.MinMaxCurve(0.5f);

        // Configure renderer for emissive effect
        var emberRenderer = emberPS.GetComponent<ParticleSystemRenderer>();
        emberRenderer.material = new Material(Shader.Find("Sprites/Default"));
        emberRenderer.material.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f) * 2f);
    }

    private static void CreateFireLight(GameObject parent)
    {
        GameObject lightObj = new GameObject("FireLight");
        lightObj.transform.SetParent(parent.transform);
        lightObj.transform.localPosition = Vector3.up * 0.5f;

        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.2f);
        light.intensity = 0f; // Starts off
        light.range = 8f;
        light.shadows = LightShadows.Soft;

        // Add light flicker script
        lightObj.AddComponent<FireLightFlicker>();
    }

    private static void CreateTorchPrefab()
    {
        GameObject torch = new GameObject("Torch");

        var fireInstance = torch.AddComponent<FireInstance>();

        // Torch handle
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Handle";
        handle.transform.SetParent(torch.transform);
        handle.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);

        var handleRenderer = handle.GetComponent<Renderer>();
        handleRenderer.material = new Material(Shader.Find("Standard"));
        handleRenderer.material.color = new Color(0.4f, 0.25f, 0.1f);

        // Torch head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(torch.transform);
        head.transform.localPosition = Vector3.up * 0.5f;
        head.transform.localScale = Vector3.one * 0.2f;

        var headRenderer = head.GetComponent<Renderer>();
        headRenderer.material = new Material(Shader.Find("Standard"));
        headRenderer.material.color = new Color(0.1f, 0.1f, 0.1f);

        // Add smaller particles for torch
        CreateTorchParticles(torch);

        // Add light
        GameObject lightObj = new GameObject("TorchLight");
        lightObj.transform.SetParent(torch.transform);
        lightObj.transform.localPosition = Vector3.up * 0.6f;

        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.3f);
        light.intensity = 0f;
        light.range = 5f;

        SavePrefab(torch, "Torch");
        DestroyImmediate(torch);
    }

    private static void CreateTorchParticles(GameObject parent)
    {
        GameObject fireObj = new GameObject("TorchFire");
        fireObj.transform.SetParent(parent.transform);
        fireObj.transform.localPosition = Vector3.up * 0.6f;

        var ps = fireObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = 0.5f;
        main.startSpeed = 1f;
        main.startSize = 0.2f;
        main.startColor = new Color(1f, 0.7f, 0.3f);
        main.maxParticles = 20;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var emission = ps.emission;
        emission.rateOverTime = 15f;
    }

    private static void CreateForgePrefab()
    {
        GameObject forge = new GameObject("Forge");

        var fireInstance = forge.AddComponent<FireInstance>();

        // Forge structure
        GameObject structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
        structure.name = "ForgeStructure";
        structure.transform.SetParent(forge.transform);
        structure.transform.localScale = new Vector3(2f, 1f, 1.5f);

        var renderer = structure.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = new Color(0.2f, 0.2f, 0.2f);

        SavePrefab(forge, "Forge");
        DestroyImmediate(forge);
    }

    private static void CreateSignalFirePrefab()
    {
        GameObject signalFire = new GameObject("SignalFire");

        var fireInstance = signalFire.AddComponent<FireInstance>();

        // Larger base
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "Platform";
        platform.transform.SetParent(signalFire.transform);
        platform.transform.localScale = new Vector3(3f, 0.2f, 3f);

        var renderer = platform.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = new Color(0.4f, 0.3f, 0.2f);

        SavePrefab(signalFire, "SignalFire");
        DestroyImmediate(signalFire);
    }

    private static void SavePrefab(GameObject obj, string name)
    {
        string path = $"Assets/_WildSurvival/Prefabs/Fire/{name}.prefab";

        // Ensure directory exists
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Debug.Log($"Created prefab: {path}");
    }
}

/// <summary>
/// Simple light flicker effect for fire
/// </summary>
public class FireLightFlicker : MonoBehaviour
{
    private Light fireLight;
    private float baseIntensity;
    private float flickerSpeed = 5f;

    void Start()
    {
        fireLight = GetComponent<Light>();
        baseIntensity = fireLight.intensity;
    }

    void Update()
    {
        if (fireLight != null && baseIntensity > 0)
        {
            fireLight.intensity = baseIntensity +
                Mathf.Sin(Time.time * flickerSpeed) * 0.2f +
                Mathf.Sin(Time.time * flickerSpeed * 1.5f) * 0.1f;
        }
    }
}