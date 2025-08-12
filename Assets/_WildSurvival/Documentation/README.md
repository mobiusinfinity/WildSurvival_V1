# Wild Survival

## 🎮 Project Overview
A wilderness survival game built with Unity 6 and HDRP.

## 📁 Project Structure

```
Assets/
├── _WildSurvival/     # Main project folder
│   ├── Code/          # All scripts
│   ├── Content/       # Game assets
│   ├── Data/          # ScriptableObjects
│   ├── Prefabs/       # Prefab organization
│   ├── Scenes/        # Scene files
│   └── Settings/      # Project settings
├── _DevTools/         # Development utilities
└── _ThirdParty/       # External packages
```

## 🚀 Quick Start

1. Open `_WildSurvival/Scenes/Core/_Preload.unity`
2. Press Play
3. The game will automatically load necessary scenes

## 🏗️ Architecture

### Core Systems
- **GameManager**: Central game state management
- **ServiceRegistry**: Service locator pattern
- **EventBus**: Decoupled event system
- **PoolManager**: Object pooling for performance

### Assembly Definitions
The project uses assembly definitions for faster compilation:
- `WildSurvival.Core`: Core systems
- `WildSurvival.Player`: Player mechanics
- `WildSurvival.Survival`: Survival systems
- `WildSurvival.Environment`: World systems
- `WildSurvival.UI`: User interface

## 📝 Documentation

- [Architecture Guide](Documentation/ARCHITECTURE.md)
- [Coding Standards](Documentation/CODING_STANDARDS.md)
- [API Reference](Documentation/API/)

## 🤝 Team

Developed by Wild Forge Studios

---
*Generated: 2025-08-12*
