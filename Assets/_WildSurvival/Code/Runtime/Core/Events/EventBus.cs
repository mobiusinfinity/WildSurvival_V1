using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Event bus for decoupled communication between systems
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> eventHandlers = new Dictionary<Type, List<Delegate>>();
        
    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        var eventType = typeof(T);
            
        if (!eventHandlers.ContainsKey(eventType))
        {
            eventHandlers[eventType] = new List<Delegate>();
        }
            
        eventHandlers[eventType].Add(handler);
    }
        
    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        var eventType = typeof(T);
            
        if (eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
        }
    }
        
    public static void Publish<T>(T eventData) where T : struct
    {
        var eventType = typeof(T);
            
        if (eventHandlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers.ToArray())
            {
                try
                {
                    (handler as Action<T>)?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Error handling event {eventType.Name}: {e}");
                }
            }
        }
    }
        
    public static void Clear()
    {
        eventHandlers.Clear();
    }
}