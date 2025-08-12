# Coding Standards

## Naming Conventions

### Classes and Methods
```csharp
public class PlayerController  // PascalCase
{
    public void TakeDamage()    // PascalCase
    {
    }
}
```

### Variables and Parameters
```csharp
private int healthPoints;       // camelCase
public float moveSpeed;         // camelCase

void Calculate(int damage)      // camelCase parameters
{
}
```

### Constants
```csharp
private const int MAX_HEALTH = 100;  // SCREAMING_CASE
```

### Interfaces
```csharp
public interface IInventoryService   // 'I' prefix
{
}
```

## Code Organization

### File Structure
- One class per file
- File name matches class name
- Related classes in same namespace

### Namespaces
```csharp
namespace WildSurvival.Player.Controller
{
    // All player controller related classes
}
```

## Unity Specific

### Serialization
```csharp
[SerializeField] private float speed = 5f;  // Prefer over public
```

### Coroutines
```csharp
private IEnumerator DoSomethingOverTime()
{
    yield return new WaitForSeconds(1f);
}
```

## Performance Guidelines

1. Cache component references
2. Use object pooling
3. Minimize allocations in Update
4. Profile regularly
5. Optimize draw calls
