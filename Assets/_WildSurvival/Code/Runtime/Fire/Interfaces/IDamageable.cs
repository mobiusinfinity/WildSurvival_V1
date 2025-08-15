using UnityEngine;
using System;

/// <summary>
/// Interface for objects that can take damage
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Current health
    /// </summary>
    float Health { get; }

    /// <summary>
    /// Maximum health
    /// </summary>
    float MaxHealth { get; }

    /// <summary>
    /// Is this object still alive/functional
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Can this object currently take damage
    /// </summary>
    bool CanTakeDamage { get; }

    /// <summary>
    /// Apply damage to this object
    /// </summary>
    void TakeDamage(float amount, DamageType type, GameObject source = null);

    /// <summary>
    /// Heal this object
    /// </summary>
    void Heal(float amount);

    /// <summary>
    /// Kill/destroy this object immediately
    /// </summary>
    void Kill();

    /// <summary>
    /// Event fired when damage is taken
    /// </summary>
    event Action<float, DamageType> OnDamageTaken;

    /// <summary>
    /// Event fired when healed
    /// </summary>
    event Action<float> OnHealed;

    /// <summary>
    /// Event fired when killed/destroyed
    /// </summary>
    event Action<IDamageable> OnDeath;
}

/// <summary>
/// Types of damage in the game
/// </summary>
[System.Serializable]
public enum DamageType
{
    Physical,
    Fire,
    Cold,
    Poison,
    Electric,
    Explosive,
    Fall,
    Drowning,
    Starvation,
    Dehydration,
    Suffocation,
    Radiation,
    Bleeding,
    Disease,
    Psychic
}

/// <summary>
/// Detailed damage information
/// </summary>
[System.Serializable]
public struct DamageInfo
{
    public float amount;
    public DamageType type;
    public Vector3 point;
    public Vector3 direction;
    public GameObject source;
    public GameObject attacker;
    public bool isCritical;
    public float criticalMultiplier;

    public DamageInfo(float dmg, DamageType dmgType)
    {
        amount = dmg;
        type = dmgType;
        point = Vector3.zero;
        direction = Vector3.forward;
        source = null;
        attacker = null;
        isCritical = false;
        criticalMultiplier = 1f;
    }

    public static DamageInfo Fire(float amount, Vector3 point)
    {
        return new DamageInfo
        {
            amount = amount,
            type = DamageType.Fire,
            point = point,
            direction = Vector3.up,
            isCritical = false,
            criticalMultiplier = 1f
        };
    }
}