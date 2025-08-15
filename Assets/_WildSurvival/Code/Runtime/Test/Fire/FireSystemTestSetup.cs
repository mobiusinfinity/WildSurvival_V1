using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Complete test scene setup for Fire System
/// </summary>
public class FireSystemTestSetup : MonoBehaviour
{
    [Header("Quick Test Controls")]
    [SerializeField] private bool autoSetupOnStart = true;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupCompleteTestScene();
        }
    }

    [ContextMenu("Setup Complete Test Scene")]
    public void SetupCompleteTestScene()
    {
        Debug.Log("🔥 Setting up Fire System Test Scene...");

        // 1. Setup Environment
        CreateTerrain();
        CreateLighting();

        // 2. Setup Player
        CreatePlayer();

        // 3. Setup Fire System
        CreateFireSystemManager();

        // 4. Setup Test Items
        CreateTestItems();

        // 5. Setup UI
        CreateUI();

        // 6. Create Test Fires
        CreateTestFires();

        // 7. Setup Debug Display
        CreateDebugDisplay();

        Debug.Log("✅ Test Scene Setup Complete!");
    }

    private void CreateTerrain()
    {
        // Create ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10, 1, 10);

        // Add terrain texture/material
        Renderer renderer = ground.GetComponent<Renderer>();
        renderer.material.color = new Color(0.3f, 0.5f, 0.2f); // Grass green

        // Add some rocks/obstacles
        for (int i = 0; i < 5; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = $"Rock_{i}";
            rock.transform.position = new Vector3(
                Random.Range(-20f, 20f),
                0.5f,
                Random.Range(-20f, 20f)
            );
            rock.transform.localScale = Vector3.one * Random.Range(0.5f, 2f);
            rock.GetComponent<Renderer>().material.color = Color.gray;
        }

        // Add trees
        for (int i = 0; i < 10; i++)
        {
            GameObject tree = new GameObject($"Tree_{i}");

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = Vector3.up;
            trunk.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
            trunk.GetComponent<Renderer>().material.color = new Color(0.4f, 0.2f, 0.1f);

            // Leaves
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.SetParent(tree.transform);
            leaves.transform.localPosition = Vector3.up * 3;
            leaves.transform.localScale = Vector3.one * 3;
            leaves.GetComponent<Renderer>().material.color = Color.green;

            tree.transform.position = new Vector3(
                Random.Range(-30f, 30f),
                0,
                Random.Range(-30f, 30f)
            );
        }
    }

    private void CreateLighting()
    {
        // Directional Light (Sun)
        GameObject sun = new GameObject("Sun");
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 1.2f;
        sunLight.color = new Color(1f, 0.95f, 0.8f);
        sun.transform.rotation = Quaternion.Euler(45f, -30f, 0);

        // Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.6f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.3f, 0.3f);

        // Fog for atmosphere
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 100f;
    }

    private GameObject CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1, -10);

        // Character Controller
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.5f;

        // Simple Player Controller
        PlayerController playerController = player.AddComponent<PlayerController>();

        // Camera
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        cameraObj.transform.SetParent(player.transform);
        cameraObj.transform.localPosition = new Vector3(0, 0.8f, 0);

        Camera cam = cameraObj.AddComponent<Camera>();
        cam.fieldOfView = 60;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        // Camera Look
         cameraLook = cameraObj.AddComponent<CameraLook>();
        cameraLook.playerBody = player.transform;

        // Audio Listener
        cameraObj.AddComponent<AudioListener>();

        // Player Stats
        PlayerStats stats = player.AddComponent<PlayerStats>();
        stats.maxHealth = 100f;
        stats.currentHealth = 100f;
        stats.maxStamina = 100f;
        stats.currentStamina = 100f;

        // Player Inventory
        PlayerInventory inventory = player.AddComponent<PlayerInventory>();

        // Fire Interaction Controller
        FireInteractionController fireController = player.AddComponent<FireInteractionController>();

        Debug.Log("✅ Player created with all components");

        return player;
    }

    private void CreateFireSystemManager()
    {
        GameObject manager = new GameObject("FireSystemManager");

        // Notification System
        NotificationSystem notifications = manager.AddComponent<NotificationSystem>();

        // Weather System (optional)
        WeatherSystem weather = manager.AddComponent<WeatherSystem>();
        weather.CurrentWeather = WeatherSystem.WeatherType.Clear;
        weather.WindStrength = 5f;

        // Temperature System
        TemperatureSystem tempSystem = manager.AddComponent<TemperatureSystem>();

        // Fire Prefab Factory
        FirePrefabFactory factory = manager.AddComponent<FirePrefabFactory>();

        Debug.Log("✅ Fire System Manager created");
    }

    private void CreateTestItems()
    {
        GameObject itemContainer = new GameObject("Test Items");

        // Create item spawners around the scene
        string[] itemsToSpawn = {
        "stone", "stick", "tinder", "kindling",
        "log", "matches", "raw_meat", "water"
    };

        foreach (string itemID in itemsToSpawn)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.name = $"Item_{itemID}_{i}";
                item.transform.SetParent(itemContainer.transform);
                item.transform.position = new Vector3(
                    Random.Range(-15f, 15f),
                    0.5f,
                    Random.Range(-15f, 15f)
                );
                item.transform.localScale = Vector3.one * 0.3f;

                // Add pickup component (now using the separate class)
                TestItemPickup pickup = item.AddComponent<TestItemPickup>();
                pickup.itemID = itemID;
                pickup.quantity = Random.Range(1, 5);
                pickup.destroyOnPickup = true;
                pickup.floatItem = true;
                pickup.rotateItem = true;

                // Visual distinction
                Renderer rend = item.GetComponent<Renderer>();
                switch (itemID)
                {
                    case "stone": rend.material.color = Color.gray; break;
                    case "stick": rend.material.color = new Color(0.4f, 0.2f, 0); break;
                    case "tinder": rend.material.color = Color.yellow; break;
                    case "matches": rend.material.color = Color.red; break;
                    case "raw_meat": rend.material.color = new Color(0.8f, 0.2f, 0.2f); break;
                    default: rend.material.color = Color.white; break;
                }
            }
        }

        Debug.Log("✅ Test items spawned");
    }

    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("UI Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Event System
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // HUD Panel
        GameObject hudPanel = new GameObject("HUD");
        hudPanel.transform.SetParent(canvasObj.transform);
        RectTransform hudRect = hudPanel.AddComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;

        // Instructions Text
        GameObject instructions = new GameObject("Instructions");
        instructions.transform.SetParent(hudPanel.transform);
        TextMeshProUGUI instructText = instructions.AddComponent<TextMeshProUGUI>();
        instructText.text = "FIRE SYSTEM TEST\n" +
                           "WASD - Move\n" +
                           "Mouse - Look\n" +
                           "B - Build Campfire\n" +
                           "F - Interact with Fire\n" +
                           "E - Pick up Items\n" +
                           "T - Light Torch\n" +
                           "Tab - Inventory";
        instructText.fontSize = 14;
        instructText.color = Color.white;
        RectTransform instructRect = instructions.GetComponent<RectTransform>();
        instructRect.anchorMin = new Vector2(0, 1);
        instructRect.anchorMax = new Vector2(0, 1);
        instructRect.pivot = new Vector2(0, 1);
        instructRect.anchoredPosition = new Vector2(10, -10);
        instructRect.sizeDelta = new Vector2(300, 200);

        // Stats Display
        GameObject statsDisplay = new GameObject("Stats");
        statsDisplay.transform.SetParent(hudPanel.transform);
        TextMeshProUGUI statsText = statsDisplay.AddComponent<TextMeshProUGUI>();
        statsText.text = "Health: 100/100\nStamina: 100/100\nTemp: 37°C";
        statsText.fontSize = 14;
        statsText.color = Color.green;
        RectTransform statsRect = statsDisplay.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(1, 1);
        statsRect.anchorMax = new Vector2(1, 1);
        statsRect.pivot = new Vector2(1, 1);
        statsRect.anchoredPosition = new Vector2(-10, -10);
        statsRect.sizeDelta = new Vector2(200, 100);

        // Inventory Display
        GameObject inventoryDisplay = new GameObject("Inventory");
        inventoryDisplay.transform.SetParent(hudPanel.transform);
        TextMeshProUGUI invText = inventoryDisplay.AddComponent<TextMeshProUGUI>();
        invText.text = "Inventory:\nEmpty";
        invText.fontSize = 12;
        invText.color = Color.white;
        RectTransform invRect = inventoryDisplay.GetComponent<RectTransform>();
        invRect.anchorMin = new Vector2(1, 0);
        invRect.anchorMax = new Vector2(1, 0);
        invRect.pivot = new Vector2(1, 0);
        invRect.anchoredPosition = new Vector2(-10, 10);
        invRect.sizeDelta = new Vector2(200, 150);

        // Fire Management UI Prefab
        CreateFireManagementUI(canvasObj);

        Debug.Log("✅ UI created");
    }

    private void CreateFireManagementUI(GameObject canvas)
    {
        GameObject fireUI = new GameObject("FireManagementUI");
        fireUI.transform.SetParent(canvas.transform);
        fireUI.SetActive(false);

        // Background panel
        Image bg = fireUI.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);
        RectTransform bgRect = fireUI.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(400, 300);

        // Title
        GameObject title = new GameObject("Title");
        title.transform.SetParent(fireUI.transform);
        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "Fire Management";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1);
        titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(380, 40);

        // Add FireManagementUI component
        FireManagementUI fireManagement = fireUI.AddComponent<FireManagementUI>();
    }

    private void CreateTestFires()
    {
        // Create different types of fires for testing
        FirePrefabFactory factory = FindObjectOfType<FirePrefabFactory>();
        if (factory == null)
        {
            GameObject factoryObj = new GameObject("FirePrefabFactory");
            factory = factoryObj.AddComponent<FirePrefabFactory>();
        }

        // Campfire
        GameObject campfire = factory.CreateFire(FireInstance.FireType.Campfire, new Vector3(5, 0, 0));
        campfire.name = "Test Campfire";

        // Torch
        GameObject torch = factory.CreateFire(FireInstance.FireType.Torch, new Vector3(-5, 0, 0));
        torch.name = "Test Torch";

        // Signal Fire
        GameObject signalFire = factory.CreateFire(FireInstance.FireType.SignalFire, new Vector3(0, 0, 5));
        signalFire.name = "Test Signal Fire";

        Debug.Log("✅ Test fires created");
    }

    private void CreateDebugDisplay()
    {
        GameObject debugObj = new GameObject("Debug Display");
        FireDebugDisplay debug = debugObj.AddComponent<FireDebugDisplay>();
        debug.showTemperatures = true;
        debug.showFuelLevels = true;
        debug.showPlayerStats = true;

        Debug.Log("✅ Debug display created");
    }
}

// Helper Components

//[System.Serializable]
//public class TestItemPickup : MonoBehaviour
//{
//    public string itemID;
//    public int quantity = 1;

//    private void OnTriggerEnter(Collider other)
//    {
//        if (other.CompareTag("Player"))
//        {
//            PlayerInventory inv = other.GetComponent<PlayerInventory>();
//            if (inv != null)
//            {
//                Debug.Log($"Picked up {quantity}x {itemID}");
//                // Add to inventory logic here
//                Destroy(gameObject);
//            }
//        }
//    }
//}

//[System.Serializable]
//public class FloatingAnimation : MonoBehaviour
//{
//    public float amplitude = 0.5f;
//    public float frequency = 1f;
//    private float startY;

//    void Start()
//    {
//        startY = transform.position.y;
//    }

//    void Update()
//    {
//        Vector3 pos = transform.position;
//        pos.y = startY + Mathf.Sin(Time.time * frequency) * amplitude;
//        transform.position = pos;
//        transform.Rotate(Vector3.up * 30 * Time.deltaTime);
//    }
//}

public class FireDebugDisplay : MonoBehaviour
{
    public bool showTemperatures = true;
    public bool showFuelLevels = true;
    public bool showPlayerStats = true;

    private void OnGUI()
    {
        if (!showTemperatures && !showFuelLevels && !showPlayerStats)
            return;

        GUI.Box(new Rect(10, 10, 250, 200), "Fire System Debug");

        int y = 30;

        if (showTemperatures)
        {
            GUI.Label(new Rect(15, y, 240, 20), "=== TEMPERATURES ===");
            y += 20;

            FireInstance[] fires = FindObjectsOfType<FireInstance>();
            foreach (var fire in fires)
            {
                GUI.Label(new Rect(15, y, 240, 20),
                    $"{fire.name}: {fire.GetCurrentTemperature():F0}°C");
                y += 20;
            }
        }

        if (showFuelLevels)
        {
            y += 10;
            GUI.Label(new Rect(15, y, 240, 20), "=== FUEL LEVELS ===");
            y += 20;

            FireInstance[] fires = FindObjectsOfType<FireInstance>();
            foreach (var fire in fires)
            {
                GUI.Label(new Rect(15, y, 240, 20),
                    $"{fire.name}: {fire.GetFuelPercentage() * 100:F0}%");
                y += 20;
            }
        }
    }
}