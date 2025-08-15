using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a complete test scene for the fire system
/// </summary>
public class FireTestSceneBuilder
{
    [MenuItem("Tools/Wild Survival/Fire System/Build Complete Test Scene")]
    public static void CreateTestScene()
    {
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        newScene.name = "FireSystemTestScene";

        // Setup environment
        CreateTerrain();
        CreatePlayer();
        CreateTestItems();
        CreatePrebuiltFires();
        CreateUI();
        CreateTestController();
        SetupLighting();

        // Save scene
        string scenePath = "Assets/_WildSurvival/Scenes/FireSystemTestScene.unity";

        // Ensure directory exists
        string dir = System.IO.Path.GetDirectoryName(scenePath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log($"✅ Test scene created: {scenePath}");
        Debug.Log("Press Play and use:\n" +
                  "• WASD to move\n" +
                  "• Mouse to look\n" +
                  "• B to build campfire\n" +
                  "• F to interact with fire\n" +
                  "• I to open inventory\n" +
                  "• 1-5 for quick test actions");
    }

    private static void CreateTerrain()
    {
        // Create ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * 10f;

        // Add grass-like material
        var renderer = ground.GetComponent<Renderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Standard"));
        renderer.sharedMaterial.color = new Color(0.2f, 0.5f, 0.2f);

        // Add terrain layer
        ground.layer = LayerMask.NameToLayer("Default");

        // Add some rocks and trees for atmosphere
        for (int i = 0; i < 10; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = $"Rock_{i}";
            rock.transform.position = new Vector3(
                Random.Range(-20f, 20f),
                0.3f,
                Random.Range(-20f, 20f)
            );
            rock.transform.localScale = Vector3.one * Random.Range(0.5f, 2f);

            var rockRenderer = rock.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Standard"));
            renderer.sharedMaterial.color = new Color(0.4f, 0.4f, 0.4f);
        }

        // Simple trees
        for (int i = 0; i < 15; i++)
        {
            GameObject tree = new GameObject($"Tree_{i}");
            tree.transform.position = new Vector3(
                Random.Range(-30f, 30f),
                0,
                Random.Range(-30f, 30f)
            );

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = Vector3.up * 1f;
            trunk.transform.localScale = new Vector3(0.5f, 2f, 0.5f);

            var trunkRenderer = trunk.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Standard"));
            renderer.sharedMaterial.color = new Color(0.4f, 0.25f, 0.1f);

            // Leaves
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Leaves";
            leaves.transform.SetParent(tree.transform);
            leaves.transform.localPosition = Vector3.up * 3f;
            leaves.transform.localScale = Vector3.one * 3f;

            var leavesRenderer = leaves.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Standard"));
            renderer.sharedMaterial.color = new Color(0.1f, 0.6f, 0.1f);
        }
    }

    private static void CreatePlayer()
    {
        // Create player GameObject
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0, 1f, -5f);
        player.tag = "Player";

        // Add components
        player.AddComponent<Rigidbody>();
        var col = player.GetComponent<CapsuleCollider>();
        col.height = 2f;

        // Add player scripts
        player.AddComponent<PlayerStats>();
        //player.AddComponent<PlayerVitals>();
        player.AddComponent<InventoryManager>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<FireInteractionController>();
        player.AddComponent<CookingSystem>();

        // Add simple player controller
        //player.AddComponent<SimplePlayerController>();

        // Add camera
        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraObj.transform.SetParent(player.transform);
        cameraObj.transform.localPosition = new Vector3(0, 0.5f, 0);

        var camera = cameraObj.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        cameraObj.AddComponent<AudioListener>();
        //cameraObj.AddComponent<SimpleCameraLook>();

        // Player hands (for holding torch)
        GameObject rightHand = new GameObject("RightHand");
        rightHand.transform.SetParent(player.transform);
        rightHand.transform.localPosition = new Vector3(0.3f, 0, 0.5f);
    }

    private static void CreateTestItems()
    {
        // Create item spawners around the map
        CreateItemSpawner("Stone", new Vector3(2, 0.5f, 0), "stone", 10);
        CreateItemSpawner("Sticks", new Vector3(-2, 0.5f, 0), "stick", 10);
        CreateItemSpawner("Tinder", new Vector3(0, 0.5f, 2), "tinder", 5);
        CreateItemSpawner("Matches", new Vector3(0, 0.5f, -2), "matches", 3);
        CreateItemSpawner("Wood Logs", new Vector3(4, 0.5f, 0), "wood_log", 5);
        CreateItemSpawner("Raw Meat", new Vector3(-4, 0.5f, 0), "meat_raw", 3);
    }

    private static void CreateItemSpawner(string name, Vector3 position, string itemID, int quantity)
    {
        GameObject spawner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spawner.name = $"ItemSpawner_{name}";
        spawner.transform.position = position;
        spawner.transform.localScale = Vector3.one * 0.5f;

        // Visual indicator
        var renderer = spawner.GetComponent<Renderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Standard"));
        renderer.sharedMaterial.color = Color.yellow;

        // Add pickup script
        var pickup = spawner.AddComponent<TestItemPickup>();
        pickup.itemID = itemID;
        pickup.quantity = quantity;
        pickup.name = name;
    }

    private static void CreatePrebuiltFires()
    {
        // Load campfire prefab
        GameObject campfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_WildSurvival/Prefabs/Fire/Campfire.prefab");

        if (campfirePrefab != null)
        {
            // Unlit campfire
            GameObject campfire1 = PrefabUtility.InstantiatePrefab(campfirePrefab) as GameObject;
            campfire1.name = "Campfire_Unlit";
            campfire1.transform.position = new Vector3(5, 0, 5);

            // Pre-fueled campfire
            GameObject campfire2 = PrefabUtility.InstantiatePrefab(campfirePrefab) as GameObject;
            campfire2.name = "Campfire_Fueled";
            campfire2.transform.position = new Vector3(-5, 0, 5);

            // Lit campfire
            GameObject campfire3 = PrefabUtility.InstantiatePrefab(campfirePrefab) as GameObject;
            campfire3.name = "Campfire_Burning";
            campfire3.transform.position = new Vector3(0, 0, 10);
        }
        else
        {
            Debug.LogWarning("Campfire prefab not found. Run 'Create All Fire Prefabs' first.");
        }
    }

    private static void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Add EventSystem
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Notification System
        GameObject notificationSystem = new GameObject("NotificationSystem");
        notificationSystem.AddComponent<NotificationSystem>();

        // Fire UI Manager
        GameObject fireUI = new GameObject("FireManagementUI");
        fireUI.transform.SetParent(canvasObj.transform);
        fireUI.AddComponent<FireManagementUI>();

        // Inventory UI
        GameObject inventoryUI = new GameObject("InventoryUI");
        inventoryUI.transform.SetParent(canvasObj.transform);
        inventoryUI.AddComponent<InventoryUI>();

        // Debug info panel
        CreateDebugPanel(canvasObj);
    }

    private static void CreateDebugPanel(GameObject canvas)
    {
        GameObject panel = new GameObject("DebugPanel");
        panel.transform.SetParent(canvas.transform, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10);
        rect.sizeDelta = new Vector2(300, 200);

        var image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0, 0, 0, 0.7f);

        GameObject text = new GameObject("DebugText");
        text.transform.SetParent(panel.transform, false);

        var textRect = text.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        var textComp = text.AddComponent<TMPro.TextMeshProUGUI>();
        textComp.text = "Fire System Debug\n" +
                        "Press H for help\n" +
                        "Temperature: --\n" +
                        "Nearby Fire: None";
        textComp.fontSize = 12;
        textComp.color = Color.white;

        text.AddComponent<FireDebugDisplay>();
    }

    private static void CreateTestController()
    {
        GameObject controller = new GameObject("FireTestController");
        controller.AddComponent<FireTestController>();
    }

    private static void SetupLighting()
    {
        // Directional light (sun)
        GameObject lightObj = GameObject.Find("Directional Light");
        if (lightObj == null)
        {
            lightObj = new GameObject("Directional Light");
        }

        var light = lightObj.GetComponent<Light>();
        if (light == null)
        {
            light = lightObj.AddComponent<Light>();
        }

        light.type = LightType.Directional;
        light.intensity = 0.5f; // Dimmer for fire atmosphere
        light.color = new Color(0.9f, 0.8f, 0.7f);
        light.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0);

        // Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.2f, 0.3f, 0.4f);
        RenderSettings.ambientEquatorColor = new Color(0.15f, 0.2f, 0.25f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.1f);

        // Fog for atmosphere
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.3f, 0.3f, 0.4f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 60f;
    }
}