using UnityEngine;
using System;

/// <summary>
/// Represents different types of ignition sources for starting fires
/// </summary>
[System.Serializable]
public class IgnitionSource
{
    public string name;
    public float successRate;
    public float ignitionTemperature;
    public int usesRemaining;
    public bool requiresTinder;
    public bool consumeOnUse;

    // Predefined ignition sources
    public static readonly IgnitionSource Matches = new IgnitionSource
    {
        name = "Matches",
        successRate = 0.9f,
        ignitionTemperature = 250f,
        usesRemaining = 10,
        requiresTinder = false,
        consumeOnUse = true
    };

    public static readonly IgnitionSource Lighter = new IgnitionSource
    {
        name = "Lighter",
        successRate = 0.95f,
        ignitionTemperature = 300f,
        usesRemaining = 100,
        requiresTinder = false,
        consumeOnUse = false
    };

    public static readonly IgnitionSource FireStarter = new IgnitionSource
    {
        name = "Fire Starter",
        successRate = 0.8f,
        ignitionTemperature = 200f,
        usesRemaining = 50,
        requiresTinder = true,
        consumeOnUse = false
    };

    public static readonly IgnitionSource FlintAndSteel = new IgnitionSource
    {
        name = "Flint and Steel",
        successRate = 0.7f,
        ignitionTemperature = 180f,
        usesRemaining = 200,
        requiresTinder = true,
        consumeOnUse = false
    };

    public static readonly IgnitionSource BowDrill = new IgnitionSource
    {
        name = "Bow Drill",
        successRate = 0.5f,
        ignitionTemperature = 150f,
        usesRemaining = -1, // Unlimited
        requiresTinder = true,
        consumeOnUse = false
    };

    public static readonly IgnitionSource Magnifier = new IgnitionSource
    {
        name = "Magnifying Glass",
        successRate = 0.6f,
        ignitionTemperature = 200f,
        usesRemaining = -1, // Unlimited
        requiresTinder = true,
        consumeOnUse = false
    };

    // Constructor
    public IgnitionSource()
    {
        name = "Unknown";
        successRate = 0.5f;
        ignitionTemperature = 200f;
        usesRemaining = 1;
        requiresTinder = true;
        consumeOnUse = true;
    }

    public IgnitionSource(string name, float successRate, float ignitionTemp)
    {
        this.name = name;
        this.successRate = successRate;
        this.ignitionTemperature = ignitionTemp;
        this.usesRemaining = 1;
        this.requiresTinder = true;
        this.consumeOnUse = true;
    }

    public bool TryUse()
    {
        if (usesRemaining == 0)
            return false;

        bool success = UnityEngine.Random.Range(0f, 1f) <= successRate;

        if (consumeOnUse && usesRemaining > 0)
        {
            usesRemaining--;
        }

        return success;
    }

    public bool CanUse()
    {
        return usesRemaining != 0; // -1 means unlimited
    }

    public static IgnitionSource FromItemID(string itemID)
    {
        return itemID switch
        {
            "matches" => Matches,
            "lighter" => Lighter,
            "fire_starter" => FireStarter,
            "flint_steel" => FlintAndSteel,
            "bow_drill" => BowDrill,
            "magnifier" => Magnifier,
            _ => null
        };
    }
}