using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple service registry without interface constraints
/// Place this in: Assets/_WildSurvival/Code/Runtime/Core/Bootstrap/ServiceRegistry.cs
/// </summary>
public static class ServiceRegistry
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    public static void Register<T>(T service) where T : class
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceRegistry] Service {type.Name} already registered");
            return;
        }

        services[type] = service;
        Debug.Log($"[ServiceRegistry] Registered service: {type.Name}");
    }

    public static T Get<T>() where T : class
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var service))
        {
            return service as T;
        }

        // Don't log error, just return null - let caller handle it
        return null;
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        service = Get<T>();
        return service != null;
    }

    public static void Clear()
    {
        services.Clear();
        Debug.Log("[ServiceRegistry] All services cleared");
    }

    public static bool Has<T>() where T : class
    {
        return services.ContainsKey(typeof(T));
    }
}