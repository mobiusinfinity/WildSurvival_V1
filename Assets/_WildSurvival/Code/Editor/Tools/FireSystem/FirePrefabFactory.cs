using UnityEngine;

/// <summary>
/// Factory for creating fire prefabs at runtime and in editor
/// </summary>
public class FirePrefabFactory : MonoBehaviour
{
    private static FirePrefabFactory instance;

    public static FirePrefabFactory Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("FirePrefabFactory");
                instance = go.AddComponent<FirePrefabFactory>();
            }
            return instance;
        }
    }

    /// <summary>
    /// Create a fire at specified position
    /// </summary>
    public GameObject CreateFire(FireInstance.FireType fireType, Vector3 position)
    {
        GameObject fireObj = CreateFirePrefab(fireType);
        fireObj.transform.position = position;
        return fireObj;
    }

    /// <summary>
    /// Create a fire prefab of specified type
    /// </summary>
    public static GameObject CreateFirePrefab(FireInstance.FireType fireType)
    {
        // Create base fire object
        GameObject fireObj = new GameObject($"Fire_{fireType}");

        // Add FireInstance component
        FireInstance fire = fireObj.AddComponent<FireInstance>();
        fire.SetFireType(fireType);

        // Configure based on type
        switch (fireType)
        {
            case FireInstance.FireType.Campfire:
                ConfigureCampfire(fireObj, fire);
                break;
            case FireInstance.FireType.Torch:
                ConfigureTorch(fireObj, fire);
                break;
            case FireInstance.FireType.Forge:
                ConfigureForge(fireObj, fire);
                break;
            case FireInstance.FireType.SignalFire:
                ConfigureSignalFire(fireObj, fire);
                break;
            default:
                ConfigureCampfire(fireObj, fire);
                break;
        }

        return fireObj;
    }

    private static void ConfigureCampfire(GameObject obj, FireInstance fire)
    {
        // Add collider
        SphereCollider collider = obj.AddComponent<SphereCollider>();
        collider.radius = 2f;
        collider.isTrigger = true;

        // Add light
        GameObject lightObj = new GameObject("Fire Light");
        lightObj.transform.SetParent(obj.transform);
        lightObj.transform.localPosition = Vector3.up * 0.5f;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.2f);
        light.intensity = 2f;
        light.range = 10f;

        // Add audio source
        obj.AddComponent<AudioSource>();

        // Set fire properties
        fire.SetMaxTemperature(600f);
        fire.SetFuelCapacity(100f);
    }

    private static void ConfigureTorch(GameObject obj, FireInstance fire)
    {
        // Smaller collider for torch
        SphereCollider collider = obj.AddComponent<SphereCollider>();
        collider.radius = 0.5f;
        collider.isTrigger = true;

        // Smaller light for torch
        GameObject lightObj = new GameObject("Torch Light");
        lightObj.transform.SetParent(obj.transform);
        lightObj.transform.localPosition = Vector3.up * 1f;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.3f);
        light.intensity = 1.5f;
        light.range = 5f;

        // Set torch properties
        fire.SetMaxTemperature(400f);
        fire.SetFuelCapacity(30f);
    }

    private static void ConfigureForge(GameObject obj, FireInstance fire)
    {
        // Larger collider for forge
        BoxCollider collider = obj.AddComponent<BoxCollider>();
        collider.size = new Vector3(3f, 2f, 3f);
        collider.isTrigger = true;

        // Brighter light for forge
        GameObject lightObj = new GameObject("Forge Light");
        lightObj.transform.SetParent(obj.transform);
        lightObj.transform.localPosition = Vector3.up * 0.5f;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.4f);
        light.intensity = 3f;
        light.range = 12f;

        // Set forge properties
        fire.SetMaxTemperature(1200f);
        fire.SetFuelCapacity(200f);
    }

    private static void ConfigureSignalFire(GameObject obj, FireInstance fire)
    {
        // Large collider for signal fire
        SphereCollider collider = obj.AddComponent<SphereCollider>();
        collider.radius = 3f;
        collider.isTrigger = true;

        // Bright light for signal fire
        GameObject lightObj = new GameObject("Signal Fire Light");
        lightObj.transform.SetParent(obj.transform);
        lightObj.transform.localPosition = Vector3.up * 2f;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.7f, 0.3f);
        light.intensity = 4f;
        light.range = 20f;

        // Set signal fire properties
        fire.SetMaxTemperature(800f);
        fire.SetFuelCapacity(150f);
    }
}