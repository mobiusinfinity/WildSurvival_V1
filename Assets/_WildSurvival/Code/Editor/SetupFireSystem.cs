// 1. Create Fire System Manager
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WildSurvival.Systems.Fire;

public class FireSystemSetup : Editor
{

    [MenuItem("Tools/Wild Survival/Fire System/Setup Fire System")]
    public static void SetupFireSystem()
    {
        // Create FireSystemConfiguration
        var config = CreateFireConfig();

        // Update existing fuel items
        UpdateFuelItems();

        // Create fire prefabs
        CreateFirePrefabs(config);

        Debug.Log("Fire System Setup Complete!");
    }

    private static FireSystemConfiguration CreateFireConfig()
    {
        string path = "Assets/_WildSurvival/Data/Config/FireSystemConfig.asset";

        var config = ScriptableObject.CreateInstance<FireSystemConfiguration>();

        // Configure fuel types
        config.fuelProperties = new List<FireSystemConfiguration.FuelTypeProperties>
    {
        new() { type = FireInstance.FuelType.Tinder, displayName = "Tinder",
               burnDuration = 2f, burnTemperature = 300f, heatOutput = 0.5f },
        new() { type = FireInstance.FuelType.Kindling, displayName = "Kindling",
               burnDuration = 5f, burnTemperature = 400f, heatOutput = 0.8f },
        new() { type = FireInstance.FuelType.Logs, displayName = "Logs",
               burnDuration = 20f, burnTemperature = 600f, heatOutput = 1f },
        new() { type = FireInstance.FuelType.Coal, displayName = "Coal",
               burnDuration = 60f, burnTemperature = 800f, heatOutput = 1.5f }
    };

        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        return config;
    }
}