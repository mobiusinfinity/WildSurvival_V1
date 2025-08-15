using UnityEngine;
using System;

/// <summary>
/// Interface for objects that can catch fire and burn
/// </summary>
public interface IFlammable
{
    /// <summary>
    /// Current fire state of the object
    /// </summary>
    bool IsOnFire { get; }

    /// <summary>
    /// Temperature at which this object ignites
    /// </summary>
    float IgnitionTemperature { get; }

    /// <summary>
    /// How easily this burns (0-1, where 1 is very flammable)
    /// </summary>
    float Flammability { get; }

    /// <summary>
    /// Current burn percentage (0-1)
    /// </summary>
    float BurnProgress { get; }

    /// <summary>
    /// How long this object can burn in seconds
    /// </summary>
    float MaxBurnTime { get; }

    /// <summary>
    /// Attempt to ignite this object
    /// </summary>
    bool TryIgnite(float temperature, IgnitionSource source);

    /// <summary>
    /// Extinguish the fire on this object
    /// </summary>
    void Extinguish(string reason = "Manual");

    /// <summary>
    /// Apply burn damage over time
    /// </summary>
    void ApplyBurnDamage(float damage, float deltaTime);

    /// <summary>
    /// Get the heat output when burning
    /// </summary>
    float GetHeatOutput();

    /// <summary>
    /// Event fired when object catches fire
    /// </summary>
    event Action<IFlammable> OnIgnited;

    /// <summary>
    /// Event fired when fire is extinguished
    /// </summary>
    event Action<IFlammable> OnExtinguished;

    /// <summary>
    /// Event fired when object is destroyed by fire
    /// </summary>
    event Action<IFlammable> OnDestroyedByFire;
}

/// <summary>
/// Extended interface for objects that can spread fire
/// </summary>
public interface IFlammableSpreadable : IFlammable
{
    /// <summary>
    /// Radius in which fire can spread
    /// </summary>
    float FireSpreadRadius { get; }

    /// <summary>
    /// Chance per second to spread fire (0-1)
    /// </summary>
    float FireSpreadChance { get; }

    /// <summary>
    /// Check if fire can spread to target
    /// </summary>
    bool CanSpreadTo(IFlammable target);

    /// <summary>
    /// Attempt to spread fire to nearby objects
    /// </summary>
    void TrySpreadFire();
}

/// <summary>
/// Interface for objects that resist fire
/// </summary>
public interface IFireResistant
{
    /// <summary>
    /// Fire resistance level (0-1, where 1 is fireproof)
    /// </summary>
    float FireResistance { get; }

    /// <summary>
    /// Temperature threshold before taking damage
    /// </summary>
    float HeatResistanceThreshold { get; }

    /// <summary>
    /// Reduce incoming fire damage
    /// </summary>
    float CalculateReducedFireDamage(float incomingDamage);
}