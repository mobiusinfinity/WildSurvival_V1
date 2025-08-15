using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Extension methods for FireInstance
/// </summary>
public static class FireInstanceExtensions
{
    /// <summary>
    /// Check if fire is hot enough for cooking
    /// </summary>
    public static bool CanCook(this FireInstance fire)
    {
        if (fire == null) return false;
        return fire.GetState() == FireInstance.FireState.Burning &&
               fire.GetCookingTemperature() >= 200f;
    }

    /// <summary>
    /// Check if fire is hot enough for smelting
    /// </summary>
    public static bool CanSmelt(this FireInstance fire)
    {
        if (fire == null) return false;
        return fire.GetState() == FireInstance.FireState.Blazing &&
               fire.GetCookingTemperature() >= 600f;
    }

    /// <summary>
    /// Get cooking efficiency based on temperature
    /// </summary>
    public static float GetCookingEfficiency(this FireInstance fire)
    {
        if (fire == null || !fire.CanCook()) return 0f;

        float temp = fire.GetCookingTemperature();
        if (temp < 200f) return 0f;
        if (temp > 400f) return 1f;

        return (temp - 200f) / 200f; // Linear scale from 200-400°C
    }

    /// <summary>
    /// Apply rain effects to the fire
    /// </summary>
    public static void ApplyRain(this FireInstance fire, float rainIntensity)
    {
        if (fire == null) return;

        // Rain reduces temperature and can extinguish fire
        float tempReduction = rainIntensity * 100f * Time.deltaTime;
        fire.ReduceTemperature(tempReduction);

        // Heavy rain can extinguish weak fires
        if (rainIntensity > 0.7f && fire.GetCookingTemperature() < 300f)
        {
            float extinguishChance = rainIntensity * Time.deltaTime * 0.1f;
            if (UnityEngine.Random.Range(0f, 1f) < extinguishChance)
            {
                fire.ExtinguishFire("Extinguished by rain");
            }
        }
    }

    /// <summary>
    /// Apply wind effects to the fire
    /// </summary>
    public static void ApplyWind(this FireInstance fire, float windStrength, Vector3 windDirection)
    {
        if (fire == null) return;

        // Wind can either feed or extinguish fire
        if (windStrength < 10f)
        {
            // Light wind feeds the fire
            fire.IncreaseTemperature(windStrength * 2f * Time.deltaTime);
        }
        else if (windStrength > 20f)
        {
            // Strong wind can blow out fire
            float extinguishChance = (windStrength - 20f) * Time.deltaTime * 0.01f;
            if (UnityEngine.Random.Range(0f, 1f) < extinguishChance)
            {
                fire.ExtinguishFire("Blown out by strong wind");
            }
        }
    }

    /// <summary>
    /// Apply snow effects to the fire
    /// </summary>
    public static void ApplySnow(this FireInstance fire, float snowIntensity)
    {
        if (fire == null) return;

        // Snow reduces temperature more slowly than rain
        float tempReduction = snowIntensity * 50f * Time.deltaTime;
        fire.ReduceTemperature(tempReduction);
    }

    /// <summary>
    /// Get the warmth radius of the fire
    /// </summary>
    public static float GetWarmthRadius(this FireInstance fire)
    {
        if (fire == null) return 0f;

        return fire.GetState() switch
        {
            FireInstance.FireState.Smoldering => 2f,
            FireInstance.FireState.Burning => 5f,
            FireInstance.FireState.Blazing => 8f,
            _ => 0f
        };
    }

    /// <summary>
    /// Get light intensity for the fire
    /// </summary>
    public static float GetLightIntensity(this FireInstance fire)
    {
        if (fire == null) return 0f;

        return fire.GetState() switch
        {
            FireInstance.FireState.Smoldering => 0.5f,
            FireInstance.FireState.Burning => 2f,
            FireInstance.FireState.Blazing => 4f,
            _ => 0f
        };
    }

    /// <summary>
    /// Check if the fire needs more fuel
    /// </summary>
    public static bool NeedsFuel(this FireInstance fire)
    {
        if (fire == null) return false;
        return fire.GetFuelPercentage() < 30f;
    }

    /// <summary>
    /// Get a description of the fire's current state
    /// </summary>
    public static string GetStateDescription(this FireInstance fire)
    {
        if (fire == null) return "No fire";

        return fire.GetState() switch
        {
            FireInstance.FireState.Unlit => "The fire is unlit",
            FireInstance.FireState.Igniting => "The fire is starting to catch",
            FireInstance.FireState.Smoldering => "The fire is smoldering with smoke",
            FireInstance.FireState.Burning => "The fire is burning steadily",
            FireInstance.FireState.Blazing => "The fire is blazing hot",
            FireInstance.FireState.Dying => "The fire is dying out",
            FireInstance.FireState.Extinguished => "The fire has gone out",
            _ => "Unknown state"
        };
    }

    // Helper methods for FireInstance that might be missing

    public static void ReduceTemperature(this FireInstance fire, float amount)
    {
        // This would need to be implemented in FireInstance
        // For now, we'll use reflection or a workaround
        if (fire != null)
        {
            var currentTemp = fire.GetCookingTemperature();
            // Would need a SetTemperature method in FireInstance
        }
    }

    public static void IncreaseTemperature(this FireInstance fire, float amount)
    {
        // This would need to be implemented in FireInstance
        if (fire != null)
        {
            var currentTemp = fire.GetCookingTemperature();
            // Would need a SetTemperature method in FireInstance
        }
    }

    /// <summary>
    /// Load fire state from save data
    /// </summary>
    public static void LoadFromSaveData(this FireInstance fire, FireSaveData.FireInstanceData data)
    {
        if (fire == null || data == null) return;

        // This would need implementation in FireInstance to expose setters
        // For now, this is a placeholder
        Debug.Log($"Loading fire data for {data.fireID}");
    }

    /// <summary>
    /// Get the fire type (for saving)
    /// </summary>
    public static FireInstance.FireType GetFireType(this FireInstance fire)
    {
        if (fire == null) return FireInstance.FireType.Campfire;

        // Determine type based on name or other properties
        if (fire.name.Contains("Torch")) return FireInstance.FireType.Torch;
        if (fire.name.Contains("Forge")) return FireInstance.FireType.Forge;
        if (fire.name.Contains("Signal")) return FireInstance.FireType.SignalFire;

        return FireInstance.FireType.Campfire;
    }

    /// <summary>
    /// Get total burn time
    /// </summary>
    public static float GetBurnTime(this FireInstance fire)
    {
        // Would need to track this in FireInstance
        return 0f;
    }

    /// <summary>
    /// Check if fire was made by player
    /// </summary>
    public static bool IsPlayerMade(this FireInstance fire)
    {
        if (fire == null) return false;
        return fire.gameObject.name.Contains("Player") ||
               fire.gameObject.CompareTag("PlayerMade");
    }
}