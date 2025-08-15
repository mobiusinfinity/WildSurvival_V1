using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

/// <summary>
/// Optimizes fire system performance using LOD and batching
/// </summary>
public class FirePerformanceOptimizer : MonoBehaviour
{
    [Header("LOD Settings")]
    [SerializeField] private float[] lodDistances = { 10f, 25f, 50f, 100f };
    [SerializeField] private bool useLOD = true;

    [Header("Update Rates")]
    [SerializeField] private float nearUpdateRate = 0.1f;
    [SerializeField] private float midUpdateRate = 0.5f;
    [SerializeField] private float farUpdateRate = 1f;

    [Header("Particle Settings")]
    [SerializeField] private int maxParticlesNear = 100;
    [SerializeField] private int maxParticlesMid = 50;
    [SerializeField] private int maxParticlesFar = 20;

    private Transform playerTransform;
    private Dictionary<FireInstance, FireLODState> fireLODStates = new Dictionary<FireInstance, FireLODState>();

    public enum LODLevel
    {
        Near = 0,   // Full quality
        Mid = 1,    // Reduced particles
        Far = 2,    // Minimal effects
        VeryFar = 3 // Just light
    }

    private class FireLODState
    {
        public LODLevel currentLOD;
        public float lastUpdateTime;
        public ParticleSystem[] particles;
        public Light fireLight;
    }

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        InvokeRepeating(nameof(UpdateAllFireLODs), 0f, 0.5f);
    }

    private void UpdateAllFireLODs()
    {
        if (playerTransform == null) return;

        var allFires = FindObjectsOfType<FireInstance>();

        foreach (var fire in allFires)
        {
            if (!fireLODStates.ContainsKey(fire))
            {
                RegisterFire(fire);
            }

            UpdateFireLOD(fire);
        }

        // Clean up destroyed fires
        var toRemove = new List<FireInstance>();
        foreach (var kvp in fireLODStates)
        {
            if (kvp.Key == null)
                toRemove.Add(kvp.Key);
        }

        foreach (var fire in toRemove)
        {
            fireLODStates.Remove(fire);
        }
    }

    private void RegisterFire(FireInstance fire)
    {
        var lodState = new FireLODState
        {
            currentLOD = LODLevel.Near,
            lastUpdateTime = Time.time,
            particles = fire.GetComponentsInChildren<ParticleSystem>(),
            fireLight = fire.GetComponentInChildren<Light>()
        };

        fireLODStates[fire] = lodState;
    }

    private void UpdateFireLOD(FireInstance fire)
    {
        if (!fireLODStates.TryGetValue(fire, out var lodState))
            return;

        float distance = Vector3.Distance(playerTransform.position, fire.transform.position);
        LODLevel newLOD = GetLODLevel(distance);

        if (newLOD != lodState.currentLOD)
        {
            ApplyLOD(fire, lodState, newLOD);
            lodState.currentLOD = newLOD;
        }

        // Update at appropriate rate
        float updateRate = GetUpdateRate(newLOD);
        if (Time.time - lodState.lastUpdateTime >= updateRate)
        {
            lodState.lastUpdateTime = Time.time;
            // Fire would update its logic here based on LOD
        }
    }

    private LODLevel GetLODLevel(float distance)
    {
        if (distance < lodDistances[0]) return LODLevel.Near;
        if (distance < lodDistances[1]) return LODLevel.Mid;
        if (distance < lodDistances[2]) return LODLevel.Far;
        return LODLevel.VeryFar;
    }

    private float GetUpdateRate(LODLevel lod)
    {
        return lod switch
        {
            LODLevel.Near => nearUpdateRate,
            LODLevel.Mid => midUpdateRate,
            LODLevel.Far => farUpdateRate,
            LODLevel.VeryFar => farUpdateRate * 2f,
            _ => midUpdateRate
        };
    }

    private void ApplyLOD(FireInstance fire, FireLODState state, LODLevel lod)
    {
        switch (lod)
        {
            case LODLevel.Near:
                ApplyNearLOD(state);
                break;
            case LODLevel.Mid:
                ApplyMidLOD(state);
                break;
            case LODLevel.Far:
                ApplyFarLOD(state);
                break;
            case LODLevel.VeryFar:
                ApplyVeryFarLOD(state);
                break;
        }
    }

    private void ApplyNearLOD(FireLODState state)
    {
        // Full quality
        foreach (var ps in state.particles)
        {
            if (ps != null)
            {
                ps.gameObject.SetActive(true);
                var main = ps.main;
                main.maxParticles = maxParticlesNear;
            }
        }

        if (state.fireLight != null)
        {
            state.fireLight.enabled = true;
            state.fireLight.shadows = LightShadows.Soft;
        }
    }

    private void ApplyMidLOD(FireLODState state)
    {
        // Reduced particles
        foreach (var ps in state.particles)
        {
            if (ps != null)
            {
                ps.gameObject.SetActive(true);
                var main = ps.main;
                main.maxParticles = maxParticlesMid;
            }
        }

        if (state.fireLight != null)
        {
            state.fireLight.enabled = true;
            state.fireLight.shadows = LightShadows.None;
        }
    }

    private void ApplyFarLOD(FireLODState state)
    {
        // Minimal particles
        foreach (var ps in state.particles)
        {
            if (ps != null)
            {
                if (ps.name.Contains("Smoke"))
                {
                    ps.gameObject.SetActive(false);
                }
                else
                {
                    var main = ps.main;
                    main.maxParticles = maxParticlesFar;
                }
            }
        }

        if (state.fireLight != null)
        {
            state.fireLight.enabled = true;
            state.fireLight.shadows = LightShadows.None;
        }
    }

    private void ApplyVeryFarLOD(FireLODState state)
    {
        // Just light, no particles
        foreach (var ps in state.particles)
        {
            if (ps != null)
            {
                ps.gameObject.SetActive(false);
            }
        }

        if (state.fireLight != null)
        {
            state.fireLight.enabled = true;
            state.fireLight.shadows = LightShadows.None;
        }
    }
}