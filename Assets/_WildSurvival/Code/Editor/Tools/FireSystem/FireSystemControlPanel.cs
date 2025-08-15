using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Master control panel for fire system management
/// </summary>
public class FireSystemControlPanel : EditorWindow
{
    private Vector2 scrollPos;
    private int selectedTab = 0;
    private string[] tabs = { "🔥 Live Fires", "🧪 Testing", "📊 Stats", "⚙️ Config", "🛠️ Tools" };

    // Live monitoring
    private List<FireInstance> activeFires = new List<FireInstance>();
    private FireInstance selectedFire;
    private float refreshRate = 0.5f;
    private double lastRefreshTime;

    // Testing
    private GameObject testFirePrefab;
    private float testTemperature = 400f;
    private float testFuel = 50f;
    private FireInstance.FireState testState = FireInstance.FireState.Burning;

    // Configuration
    private FireSystemConfiguration config;
    private bool showAdvancedSettings = false;

    [MenuItem("Tools/Wild Survival/Fire System/Control Panel")]
    public static void ShowWindow()
    {
        var window = GetWindow<FireSystemControlPanel>("🔥 Fire Control");
        window.minSize = new Vector2(500, 600);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        DrawHeader();

        selectedTab = GUILayout.Toolbar(selectedTab, tabs);
        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (selectedTab)
        {
            case 0: DrawLiveFiresTab(); break;
            case 1: DrawTestingTab(); break;
            case 2: DrawStatsTab(); break;
            case 3: DrawConfigTab(); break;
            case 4: DrawToolsTab(); break;
        }

        EditorGUILayout.EndScrollView();

        DrawFooter();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Fire System Control Panel", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        // Play mode indicator
        GUI.color = Application.isPlaying ? Color.green : Color.yellow;
        GUILayout.Label(Application.isPlaying ? "▶ PLAYING" : "⏸ EDITOR", EditorStyles.miniLabel);
        GUI.color = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLiveFiresTab()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to monitor live fires", MessageType.Info);
            return;
        }

        // Refresh fires list
        if (EditorApplication.timeSinceStartup - lastRefreshTime > refreshRate)
        {
            RefreshFiresList();
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        EditorGUILayout.LabelField($"Active Fires: {activeFires.Count}", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Fire list
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200));
        EditorGUILayout.LabelField("Fire Instances", EditorStyles.boldLabel);

        foreach (var fire in activeFires)
        {
            if (fire == null) continue;

            EditorGUILayout.BeginHorizontal();

            // State indicator
            GUI.color = GetFireStateColor(fire.GetState());
            GUILayout.Label("●", GUILayout.Width(20));
            GUI.color = Color.white;

            if (GUILayout.Button(fire.name, EditorStyles.miniButton))
            {
                selectedFire = fire;
                Selection.activeGameObject = fire.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        // Fire details
        EditorGUILayout.BeginVertical(GUI.skin.box);

        if (selectedFire != null)
        {
            DrawFireDetails(selectedFire);
        }
        else
        {
            EditorGUILayout.HelpBox("Select a fire to view details", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFireDetails(FireInstance fire)
    {
        EditorGUILayout.LabelField($"Fire: {fire.name}", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // Temperature gauge
        EditorGUILayout.LabelField("Temperature");
        float temp = fire.GetCookingTemperature();
        var rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        EditorGUI.ProgressBar(rect, temp / 800f, $"{temp:F0}°C");

        // Fuel gauge
        EditorGUILayout.LabelField("Fuel Level");
        float fuel = fire.GetFuelPercentage();
        rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        EditorGUI.ProgressBar(rect, fuel / 100f, $"{fuel:F0}%");

        // State
        EditorGUILayout.LabelField("State", fire.GetState().ToString());

        EditorGUILayout.Space(10);

        // Control buttons
        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Ignite"))
        {
            fire.TryIgnite(IgnitionSource.Matches);
        }

        if (GUILayout.Button("Add Fuel"))
        {
            AddTestFuel(fire);
        }

        if (GUILayout.Button("Extinguish"))
        {
            fire.ExtinguishFire("Manual extinguish from editor");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Set Blazing"))
        {
            SetFireState(fire, FireInstance.FireState.Blazing);
        }

        if (GUILayout.Button("Set Dying"))
        {
            SetFireState(fire, FireInstance.FireState.Dying);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTestingTab()
    {
        EditorGUILayout.LabelField("Fire Testing Tools", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Spawn test fire
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Spawn Test Fire", EditorStyles.boldLabel);

        testFirePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Fire Prefab", testFirePrefab, typeof(GameObject), false);

        testTemperature = EditorGUILayout.Slider("Initial Temperature", testTemperature, 0, 800);
        testFuel = EditorGUILayout.Slider("Initial Fuel", testFuel, 0, 100);
        testState = (FireInstance.FireState)EditorGUILayout.EnumPopup("Initial State", testState);

        if (GUILayout.Button("Spawn at Scene View", GUILayout.Height(30)))
        {
            SpawnTestFire();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Batch operations
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Ignite All"))
        {
            BatchOperation(f => f.TryIgnite(IgnitionSource.Matches));
        }

        if (GUILayout.Button("Extinguish All"))
        {
            BatchOperation(f => f.ExtinguishFire("Batch extinguish"));
        }

        if (GUILayout.Button("Refuel All"))
        {
            BatchOperation(f => AddTestFuel(f));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Environmental testing
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Environmental Effects", EditorStyles.boldLabel);

        if (GUILayout.Button("Simulate Rain"))
        {
            SimulateRain();
        }

        if (GUILayout.Button("Simulate Wind"))
        {
            SimulateWind();
        }

        if (GUILayout.Button("Test Fire Spread"))
        {
            TestFireSpread();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStatsTab()
    {
        EditorGUILayout.LabelField("Fire System Statistics", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Statistics available during play mode", MessageType.Info);
            return;
        }

        RefreshFiresList();

        // Overview stats
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Total Fires: {activeFires.Count}");
        EditorGUILayout.LabelField($"Burning: {activeFires.Count(f => f.GetState() == FireInstance.FireState.Burning)}");
        EditorGUILayout.LabelField($"Extinguished: {activeFires.Count(f => f.GetState() == FireInstance.FireState.Extinguished)}");

        float totalTemp = activeFires.Sum(f => f.GetCookingTemperature());
        EditorGUILayout.LabelField($"Average Temperature: {(activeFires.Count > 0 ? totalTemp / activeFires.Count : 0):F0}°C");

        EditorGUILayout.EndVertical();

        // Performance stats
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Update Interval: {1f / 5f:F2}s");
        EditorGUILayout.LabelField($"Active Particles: {CountActiveParticles()}");
        EditorGUILayout.LabelField($"Active Lights: {CountActiveLights()}");

        EditorGUILayout.EndVertical();

        // Fuel consumption
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Fuel Statistics", EditorStyles.boldLabel);

        float totalFuel = activeFires.Sum(f => f.GetFuelPercentage());
        EditorGUILayout.LabelField($"Total Fuel: {totalFuel:F0}%");
        EditorGUILayout.LabelField($"Average Fuel: {(activeFires.Count > 0 ? totalFuel / activeFires.Count : 0):F0}%");

        EditorGUILayout.EndVertical();
    }

    private void DrawConfigTab()
    {
        EditorGUILayout.LabelField("Fire System Configuration", EditorStyles.boldLabel);

        config = (FireSystemConfiguration)EditorGUILayout.ObjectField(
            "Config Asset", config, typeof(FireSystemConfiguration), false);

        if (config == null)
        {
            EditorGUILayout.HelpBox("No configuration loaded", MessageType.Warning);

            if (GUILayout.Button("Create New Configuration"))
            {
                CreateNewConfig();
            }

            if (GUILayout.Button("Find Existing Configuration"))
            {
                FindConfig();
            }

            return;
        }

        EditorGUILayout.Space(10);

        // Edit configuration
        var editor = Editor.CreateEditor(config);
        editor.OnInspectorGUI();

        EditorGUILayout.Space(10);

        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");

        if (showAdvancedSettings)
        {
            DrawAdvancedSettings();
        }
    }

    private void DrawToolsTab()
    {
        EditorGUILayout.LabelField("Fire System Tools", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Setup tools
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Setup Fire Layer", GUILayout.Height(25)))
        {
            SetupFireLayer();
        }

        if (GUILayout.Button("Create All Prefabs", GUILayout.Height(25)))
        {
            FirePrefabCreator.CreateAllFirePrefabs();
        }

        if (GUILayout.Button("Build Test Scene", GUILayout.Height(25)))
        {
            FireTestSceneBuilder.CreateTestScene();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Validation tools
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate Setup", GUILayout.Height(25)))
        {
            ValidateSetup();
        }

        if (GUILayout.Button("Check Dependencies", GUILayout.Height(25)))
        {
            CheckDependencies();
        }

        if (GUILayout.Button("Fix Common Issues", GUILayout.Height(25)))
        {
            FixCommonIssues();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Debug tools
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        if (GUILayout.Button("Enable Debug Mode", GUILayout.Height(25)))
        {
            EnableDebugMode();
        }

        if (GUILayout.Button("Export Fire Data", GUILayout.Height(25)))
        {
            ExportFireData();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label($"Last Update: {System.DateTime.Now:HH:mm:ss}", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
        {
            RefreshFiresList();
        }

        if (GUILayout.Button("Help", EditorStyles.toolbarButton))
        {
            ShowHelp();
        }

        EditorGUILayout.EndHorizontal();
    }

    // Helper methods
    private void RefreshFiresList()
    {
        activeFires = FindObjectsOfType<FireInstance>().ToList();
    }

    private Color GetFireStateColor(FireInstance.FireState state)
    {
        return state switch
        {
            FireInstance.FireState.Unlit => Color.gray,
            FireInstance.FireState.Igniting => Color.yellow,
            FireInstance.FireState.Smoldering => new Color(1f, 0.5f, 0f),
            FireInstance.FireState.Burning => Color.red,
            FireInstance.FireState.Blazing => new Color(1f, 0.2f, 0f),
            FireInstance.FireState.Dying => new Color(0.5f, 0.3f, 0.1f),
            FireInstance.FireState.Extinguished => Color.black,
            _ => Color.white
        };
    }

    private void SetFireState(FireInstance fire, FireInstance.FireState state)
    {
        // This would require adding a public method to FireInstance
        Debug.Log($"Setting {fire.name} to {state}");
    }

    private void AddTestFuel(FireInstance fire)
    {
        // Create temporary fuel item
        var fuelItem = ScriptableObject.CreateInstance<ItemDefinition>();
        fuelItem.itemID = "test_fuel";
        fuelItem.displayName = "Test Fuel";
        fuelItem.primaryCategory = ItemCategory.Fuel;
        fuelItem.tags = new[] { ItemTag.Fuel };

        fire.TryAddFuel(fuelItem, 10);
    }

    private void SpawnTestFire()
    {
        GameObject prefab = testFirePrefab;

        if (prefab == null)
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_WildSurvival/Prefabs/Fire/Campfire.prefab");
        }

        if (prefab != null)
        {
            var sceneView = SceneView.lastActiveSceneView;
            Vector3 position = sceneView != null ?
                sceneView.camera.transform.position + sceneView.camera.transform.forward * 5f :
                Vector3.zero;

            GameObject fire = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            fire.transform.position = position;

            var fireInstance = fire.GetComponent<FireInstance>();
            if (fireInstance != null)
            {
                // Set initial values
                AddTestFuel(fireInstance);
            }

            Selection.activeGameObject = fire;
        }
    }

    private void BatchOperation(System.Action<FireInstance> operation)
    {
        RefreshFiresList();
        foreach (var fire in activeFires)
        {
            if (fire != null)
                operation(fire);
        }
    }

    private void SimulateRain()
    {
        Debug.Log("Simulating rain on all fires");
        // Would need to add weather system integration
    }

    private void SimulateWind()
    {
        Debug.Log("Simulating wind on all fires");
    }

    private void TestFireSpread()
    {
        Debug.Log("Testing fire spread mechanics");
    }

    private int CountActiveParticles()
    {
        return activeFires.Sum(f =>
            f.GetComponentsInChildren<ParticleSystem>()
                .Count(ps => ps.isPlaying));
    }

    private int CountActiveLights()
    {
        return activeFires.Sum(f =>
            f.GetComponentsInChildren<Light>()
                .Count(l => l.enabled && l.intensity > 0));
    }

    private void CreateNewConfig()
    {
        var config = ScriptableObject.CreateInstance<FireSystemConfiguration>();
        string path = "Assets/_WildSurvival/Data/Config/FireSystemConfig.asset";
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        this.config = config;
    }

    private void FindConfig()
    {
        string[] guids = AssetDatabase.FindAssets("t:FireSystemConfiguration");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            config = AssetDatabase.LoadAssetAtPath<FireSystemConfiguration>(path);
        }
    }

    private void DrawAdvancedSettings()
    {
        EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
        config.updateInterval = EditorGUILayout.Slider("Update Interval", config.updateInterval, 0.1f, 1f);
        config.maxActiveFires = EditorGUILayout.IntSlider("Max Active Fires", config.maxActiveFires, 1, 100);
        config.fireGridCellSize = EditorGUILayout.Slider("Grid Cell Size", config.fireGridCellSize, 10f, 100f);
    }

    private void SetupFireLayer()
    {
        // Add Fire layer to Tags and Layers
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));

        SerializedProperty layers = tagManager.FindProperty("layers");

        bool found = false;
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (layer.stringValue == "Fire")
            {
                found = true;
                break;
            }
            else if (string.IsNullOrEmpty(layer.stringValue) && !found)
            {
                layer.stringValue = "Fire";
                found = true;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"Added 'Fire' layer at index {i}");
                break;
            }
        }

        if (found)
            EditorUtility.DisplayDialog("Success", "Fire layer setup complete", "OK");
    }

    private void ValidateSetup()
    {
        var issues = new List<string>();

        // Check for FireSystemConfiguration
        if (config == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:FireSystemConfiguration");
            if (guids.Length == 0)
                issues.Add("No FireSystemConfiguration found");
        }

        // Check for fire prefabs
        string[] firePrefabs = AssetDatabase.FindAssets("t:Prefab Fire");
        if (firePrefabs.Length == 0)
            issues.Add("No fire prefabs found");

        // Check for required components in scene
        if (Application.isPlaying)
        {
            if (FindObjectOfType<NotificationSystem>() == null)
                issues.Add("NotificationSystem not found in scene");

            if (FindObjectOfType<PlayerInventory>() == null)
                issues.Add("PlayerInventory not found in scene");
        }

        // Display results
        if (issues.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Passed",
                "Fire system setup is valid!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Validation Issues",
                string.Join("\n", issues), "OK");
        }
    }

    private void CheckDependencies()
    {
        Debug.Log("Checking fire system dependencies...");

        var dependencies = new Dictionary<string, bool>
        {
            { "FireInstance.cs", AssetDatabase.FindAssets("FireInstance t:Script").Length > 0 },
            { "PlayerVitals.cs", AssetDatabase.FindAssets("PlayerVitals t:Script").Length > 0 },
            { "ItemDatabase", AssetDatabase.FindAssets("t:ItemDatabase").Length > 0 },
            { "NotificationSystem.cs", AssetDatabase.FindAssets("NotificationSystem t:Script").Length > 0 },
            { "Fire Layer", LayerMask.NameToLayer("Fire") != -1 }
        };

        foreach (var dep in dependencies)
        {
            Debug.Log($"{(dep.Value ? "✓" : "✗")} {dep.Key}");
        }
    }

    private void FixCommonIssues()
    {
        int fixes = 0;

        // Add Fire layer if missing
        if (LayerMask.NameToLayer("Fire") == -1)
        {
            SetupFireLayer();
            fixes++;
        }

        // Create config if missing
        if (config == null)
        {
            FindConfig();
            if (config == null)
            {
                CreateNewConfig();
                fixes++;
            }
        }

        EditorUtility.DisplayDialog("Fixes Applied",
            $"Applied {fixes} fixes", "OK");
    }

    private void EnableDebugMode()
    {
        PlayerPrefs.SetInt("FireSystemDebug", 1);
        Debug.Log("Fire system debug mode enabled");
    }

    private void ExportFireData()
    {
        RefreshFiresList();

        var data = new System.Text.StringBuilder();
        data.AppendLine("Fire System Data Export");
        data.AppendLine($"Time: {System.DateTime.Now}");
        data.AppendLine($"Active Fires: {activeFires.Count}");
        data.AppendLine();

        foreach (var fire in activeFires)
        {
            data.AppendLine($"Fire: {fire.name}");
            data.AppendLine($"  Position: {fire.transform.position}");
            data.AppendLine($"  State: {fire.GetState()}");
            data.AppendLine($"  Temperature: {fire.GetCookingTemperature():F0}°C");
            data.AppendLine($"  Fuel: {fire.GetFuelPercentage():F0}%");
            data.AppendLine();
        }

        string path = EditorUtility.SaveFilePanel("Export Fire Data", "", "fire_data.txt", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, data.ToString());
            Debug.Log($"Fire data exported to: {path}");
        }
    }

    private void ShowHelp()
    {
        Application.OpenURL("https://docs.unity3d.com/Manual/index.html");
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            RefreshFiresList();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!Application.isPlaying) return;

        // Draw fire indicators in scene view
        foreach (var fire in activeFires)
        {
            if (fire == null) continue;

            Vector3 pos = fire.transform.position;

            // Draw temperature label
            Handles.Label(pos + Vector3.up * 2f,
                $"{fire.GetCookingTemperature():F0}°C",
                EditorStyles.boldLabel);

            // Draw radius
            Handles.color = new Color(1f, 0.5f, 0f, 0.2f);
            Handles.DrawSolidDisc(pos, Vector3.up, 3f);
        }
    }
}