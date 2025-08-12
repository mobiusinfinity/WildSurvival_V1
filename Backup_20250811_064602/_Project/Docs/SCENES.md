# Scene roles
- **_Bootstrap**: entry, config, then loads _Persistent additively
- **_Persistent**: systems (audio, save/load, time, input, managers)
- **World_Prototype**: the actual play space (terrain, POIs, fauna)

Load order: `_Bootstrap` → add `_Persistent` → load `World_Prototype`.