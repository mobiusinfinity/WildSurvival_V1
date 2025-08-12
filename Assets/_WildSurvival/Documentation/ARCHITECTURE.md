# Architecture Guide

## Overview
Wild Survival follows a modular, component-based architecture optimized for performance and maintainability.

## Core Patterns

### Service Locator
Used for accessing major systems without tight coupling.

```csharp
var inventoryManager = ServiceRegistry.Get<IInventoryService>();
```

### Event Bus
Decoupled communication between systems.

```csharp
EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
EventBus.Publish(new PlayerDamagedEvent { Damage = 10 });
```

### Object Pooling
Performance optimization for frequently spawned objects.

```csharp
var projectile = PoolManager.Instance.GetFromPool(prefab, position, rotation);
PoolManager.Instance.ReturnToPool(projectile);
```

## System Architecture

### Layer Separation
1. **Core Layer**: Foundation systems (GameManager, EventBus)
2. **Gameplay Layer**: Game mechanics (Player, Survival, Combat)
3. **Presentation Layer**: UI and visual feedback
4. **Data Layer**: ScriptableObjects and persistent data

### Dependencies
- Higher layers depend on lower layers
- No circular dependencies
- Use interfaces for abstraction

## Performance Considerations

### Update Loops
- Use coroutines for non-critical updates
- Implement update intervals for expensive operations
- Cache component references

### Memory Management
- Pool frequently instantiated objects
- Minimize garbage collection
- Use structs for data containers

## Best Practices

1. **Single Responsibility**: Each class should have one reason to change
2. **Open/Closed**: Open for extension, closed for modification
3. **Dependency Inversion**: Depend on abstractions, not concretions
4. **Interface Segregation**: Many specific interfaces over general ones
5. **Don't Repeat Yourself**: Avoid code duplication
