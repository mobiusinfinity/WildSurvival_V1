using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// Player interaction with fire systems
/// </summary>
public class FireInteractionController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask fireLayer;
    [SerializeField] private GameObject campfirePrefab;
    [SerializeField] private GameObject torchPrefab;

    [Header("UI References")]
    [SerializeField] private FireManagementUI fireUI;
    [SerializeField] private CookingUI cookingUI;

    [Header("Building Requirements")]
    [SerializeField]
    private BuildRequirement[] campfireRequirements = new BuildRequirement[]
    {
        new BuildRequirement { itemID = "AI_stone", quantity = 5 },
        new BuildRequirement { itemID = "AI_sticks", quantity = 3 },
        new BuildRequirement { itemID = "AI_tinder", quantity = 2 }
    };

    private FireInstance currentFire;
    private PlayerInventory playerInventory;
    private PlayerStats playerStats;
    private Camera playerCamera;

    private InventoryManager inventory;

    // Cached data
    private List<ItemDefinition> availableFuels = new();
    private IgnitionSource currentIgnitionSource;
    private float lastInteractionCheck;
    private GameObject buildingGhost;
    private bool isBuilding;

    [System.Serializable]
    public class BuildRequirement
    {
        public string itemID;
        public int quantity;
        public bool consumed = true;
    }

    private void Awake()
    {
        // Try to find components if not assigned
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        // Find UI if not assigned
        if (fireUI == null)
            fireUI = FindObjectOfType<FireManagementUI>();

        if (cookingUI == null)
            cookingUI = FindObjectOfType<CookingUI>();
    }

    private void Start()
    {
        // Get inventory reference
        inventory = InventoryManager.Instance;
    }

    private void Update()
    {
        // Throttle interaction checks for performance
        if (Time.time - lastInteractionCheck > 0.2f)
        {
            lastInteractionCheck = Time.time;
            CheckNearbyFires();
        }

        // Handle input
        if (Keyboard.current != null)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                InteractWithFire();
            }

            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                TryBuildCampfire();
            }

            if (Keyboard.current.tKey.wasPressedThisFrame && currentFire != null)
            {
                TryLightTorch();
            }
        }

        // Handle building mode
        if (isBuilding)
        {
            UpdateBuildingGhost();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlaceCampfire();
            }
            else if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelBuilding();
            }
        }
    }
    // Add this helper method
    private ItemDefinition GetItemDefinition(string itemID)
    {
        // Try to load from Resources
        ItemDefinition item = Resources.Load<ItemDefinition>($"Items/{itemID}");

        // Or try from database
        if (item == null && ItemDatabase.Instance != null)
        {
            item = ItemDatabase.Instance.GetItem(itemID);
        }

        // Create temporary if not found
        if (item == null)
        {
            Debug.LogWarning($"Item {itemID} not found, creating temporary");
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.itemID = itemID;
            item.displayName = itemID;
        }

        return item;
    }
    private void CheckNearbyFires()
    {
        var nearbyFires = Physics.OverlapSphere(transform.position, interactionRange, fireLayer)
            .Select(c => c.GetComponent<FireInstance>())
            .Where(f => f != null)
            .OrderBy(f => Vector3.Distance(transform.position, f.transform.position))
            .FirstOrDefault();

        if (nearbyFires != currentFire)
        {
            if (currentFire != null)
            {
                OnExitFireRange(currentFire);
            }

            currentFire = nearbyFires;

            if (currentFire != null)
            {
                OnEnterFireRange(currentFire);
            }
        }
    }

    private void OnEnterFireRange(FireInstance fire)
    {
        fireUI?.Show(fire);
        ShowInteractionPrompt("Press [F] to interact with fire");
    }

    private void OnExitFireRange(FireInstance fire)
    {
        fireUI?.Hide();
        HideInteractionPrompt();
    }

    private void InteractWithFire()
    {
        if (currentFire == null) return;

        // Open fire management UI
        fireUI?.OpenManagementPanel(currentFire, this);
    }

    #region Fire Building

    private void TryBuildCampfire()
    {
        // Check if we have required materials
        if (!HasMaterials(campfireRequirements))
        {
            string missing = GetMissingMaterialsText(campfireRequirements);
            ShowMessage($"Missing materials: {missing}");
            return;
        }

        // Enter building mode
        StartBuildingMode();
    }

    private void StartBuildingMode()
    {
        isBuilding = true;

        // Create ghost prefab
        if (campfirePrefab != null)
        {
            buildingGhost = Instantiate(campfirePrefab);
            buildingGhost.name = "CampfireGhost";

            // Disable all components except renderers
            DisableGhostComponents(buildingGhost);

            // Make it semi-transparent
            SetGhostTransparency(buildingGhost, 0.5f);
        }

        ShowMessage("Place campfire with Left Click, Cancel with Right Click");
    }

    private void UpdateBuildingGhost()
    {
        if (buildingGhost == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 10f, LayerMask.GetMask("Ground", "Terrain")))
        {
            buildingGhost.transform.position = hit.point;

            // Check if placement is valid
            bool canPlace = IsValidPlacement(hit.point);
            SetGhostColor(buildingGhost, canPlace ? Color.green : Color.red);
        }
    }

    private bool IsValidPlacement(Vector3 position)
    {
        // Check ground angle
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 2f))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle > 30f) return false; // Too steep
        }

        // Check for obstacles
        Collider[] obstacles = Physics.OverlapSphere(position, 1f, ~LayerMask.GetMask("Ground", "Terrain"));
        if (obstacles.Length > 0) return false;

        // Check for water
        if (Physics.CheckSphere(position, 0.5f, LayerMask.GetMask("Water")))
            return false;

        // Check proximity to other fires
        var nearbyFires = Physics.OverlapSphere(position, 3f, fireLayer);
        if (nearbyFires.Length > 0) return false;

        return true;
    }

    private void PlaceCampfire()
    {
        if (buildingGhost == null) return;

        Vector3 position = buildingGhost.transform.position;

        if (!IsValidPlacement(position))
        {
            ShowMessage("Cannot place here!");
            return;
        }

        // Consume materials
        ConsumeMaterials(campfireRequirements);

        // Create actual campfire
        var campfire = Instantiate(campfirePrefab, position, Quaternion.identity);
        var fireInstance = campfire.GetComponent<FireInstance>();

        if (fireInstance == null)
        {
            fireInstance = campfire.AddComponent<FireInstance>();
        }

        // Pre-load with initial fuel from consumed materials
        fireInstance.TryAddFuel(GetItem("AI_tinder"), 2);
        fireInstance.TryAddFuel(GetItem("AI_sticks"), 3);

        ShowMessage("Campfire built! Add fuel and light it.");

        CancelBuilding();
    }

    private void CancelBuilding()
    {
        isBuilding = false;

        if (buildingGhost != null)
        {
            Destroy(buildingGhost);
            buildingGhost = null;
        }
    }

    #endregion

    #region Fuel Management

    public void AddFuelToFire(FireInstance fire, string itemId, int quantity)
    {
        var item = GetItem(itemId);
        if (item == null)
        {
            Debug.LogError($"Item {itemId} not found!");
            return;
        }

        if (playerInventory.HasItem(item, quantity))
        {
            if (fire.TryAddFuel(item, quantity))
            {
                playerInventory.RemoveItem(item, quantity);
                ShowMessage($"Added {quantity}x {item.displayName} to fire");

                // Update UI if open
                fireUI?.RefreshDisplay();
            }
            else
            {
                ShowMessage("Fire is full of fuel");
            }
        }
        else
        {
            ShowMessage($"Not enough {item.displayName}");
        }
    }

    public List<ItemDefinition> GetAvailableFuels()
    {
        availableFuels.Clear();

        if (playerInventory == null) return availableFuels;

        var allItems = playerInventory.GetAllItems();
        foreach (var itemStack in allItems)
        {
            if (itemStack.item != null && itemStack.item.IsFuel)
            {
                if (!availableFuels.Contains(itemStack.item))
                {
                    availableFuels.Add(itemStack.item);
                }
            }
        }

        return availableFuels;
    }

    public int GetFuelCount(ItemDefinition fuel)
    {
        if (playerInventory == null) return 0;
        return playerInventory.GetItemCount(fuel);
    }

    #endregion

    #region Ignition

    public void TryIgniteFire(FireInstance fire)
    {
        if (fire == null) return;

        // Check for ignition sources in inventory
        var ignitionSources = GetIgnitionSources();

        if (ignitionSources.Count == 0)
        {
            ShowMessage("No ignition source available");
            return;
        }

        // Show ignition source selection if multiple
        if (ignitionSources.Count > 1)
        {
            ShowIgnitionSourceSelection(ignitionSources, fire);
        }
        else
        {
            // Use the only available source
            UseIgnitionSource(ignitionSources[0], fire);
        }
    }

    private void ShowIgnitionSourceSelection(List<IgnitionSource> sources, FireInstance fire)
    {
        // This would open a UI panel to select ignition source
        // For now, just use the best one
        var bestSource = sources.OrderByDescending(s => s.successRate).First();
        UseIgnitionSource(bestSource, fire);
    }

    private void UseIgnitionSource(IgnitionSource source, FireInstance fire)
    {
        ShowMessage($"Attempting to light fire with {source.name}...");

        if (fire.TryIgnite(source))
        {
            ShowMessage($"Fire lit successfully!");
            ConsumeIgnitionSource(source);

            // Play success sound
            PlaySound("fire_ignite");
        }
        else
        {
            ShowMessage($"Failed to light fire");
            ConsumeIgnitionSource(source);

            // Play failure sound
            PlaySound("fire_ignite_fail");
        }
    }

    private List<IgnitionSource> GetIgnitionSources()
    {
        var sources = new List<IgnitionSource>();

        if (inventory.HasItem(GetItemDefinition("tinder"), 3))
        {
            inventory.RemoveItem(GetItemDefinition("tinder"), 3);
        }

        if (inventory.HasItem(GetItemDefinition("kindling"), 2))
        {
            inventory.RemoveItem(GetItemDefinition("kindling"), 2);
        }

        return sources;
    }

    private void ConsumeIgnitionSource(IgnitionSource source)
    {
        if (source.usesRemaining > 0)
        {
            source.usesRemaining--;

            if (source.usesRemaining == 0)
            {
                // Remove depleted item
                var item = GetIgnitionItem(source);
                if (item != null)
                {
                    playerInventory.RemoveItem(item, 1);
                    ShowMessage($"{source.name} depleted");
                }
            }
        }
    }

    private ItemDefinition GetIgnitionItem(IgnitionSource source)
    {
        return source.name switch
        {
            "Matches" => GetItem("AI_matches"),
            "Lighter" => GetItem("tool_lighter"),
            "Flint and Steel" => GetItem("tool_flint_steel"),
            "Bow Drill" => GetItem("tool_bow_drill"),
            "Fire Starter" => GetItem("AI_fire_starter"),
            _ => null
        };
    }

    #endregion

    #region Torch Management

    private void TryLightTorch()
    {
        if (currentFire == null)
        {
            ShowMessage("Need a fire source to light torch");
            return;
        }

        if (!currentFire.CanLightTorch())
        {
            ShowMessage("Fire is not hot enough to light torch");
            return;
        }

        LightTorchFromFire(currentFire);
    }

    public void LightTorchFromFire(FireInstance fire)
    {
        if (!playerInventory.HasItem(GetItemDefinition("torch")))
        {
            ShowMessage("No torch in inventory");
            return;
        }

        // Create lit torch
        var torch = CreateLitTorch();
        fire.TryLightTorch();
    }

    private GameObject CreateLitTorch()
    {
        GameObject torch;

        if (torchPrefab != null)
        {
            torch = Instantiate(torchPrefab);
        }
        else
        {
            torch = new GameObject("Lit Torch");
        }

        var torchFire = torch.GetComponent<FireInstance>();
        if (torchFire == null)
        {
            torchFire = torch.AddComponent<FireInstance>();
        }

        // Configure as torch
        torchFire.SetFireType(FireInstance.FireType.Torch);
        torchFire.SetMaxTemperature(400f);
        torchFire.SetFuelCapacity(20f);

        return torch;
    }

    private ItemDefinition CreateLitTorchItem()
    {
        var litTorch = ScriptableObject.CreateInstance<ItemDefinition>();
        litTorch.itemID = "torch_lit";
        litTorch.displayName = "Lit Torch";
        litTorch.description = "A burning torch that provides light";
        litTorch.weight = 0.5f;
        litTorch.maxStackSize = 1;

        return litTorch;
    }

    private void EquipTorch(GameObject torch)
    {
        // Find hand transform
        Transform handTransform = transform.Find("RightHand");
        if (handTransform == null)
        {
            // Try to find it in children
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                if (child.name.Contains("Hand") || child.name.Contains("hand"))
                {
                    handTransform = child;
                    break;
                }
            }
        }

        if (handTransform != null)
        {
            torch.transform.SetParent(handTransform);
            torch.transform.localPosition = Vector3.zero;
            torch.transform.localRotation = Quaternion.identity;
        }
    }

    #endregion

    #region Cooking

    public void StartCooking(ItemDefinition foodItem)
    {
        if (currentFire == null)
        {
            ShowMessage("Need a fire to cook");
            return;
        }

        if (!currentFire.CanCook())
        {
            ShowMessage("Fire is not suitable for cooking");
            return;
        }

        if (cookingUI != null)
        {
            cookingUI.StartCooking(currentFire, foodItem);
        }
        else
        {
            // Direct cooking without UI
            var cookingSystem = GetComponent<CookingSystem>();
            if (cookingSystem != null)
            {
                cookingSystem.StartCooking(currentFire, foodItem);
            }
        }
    }

    #endregion

    #region Utilities

    private bool HasMaterials(BuildRequirement[] requirements)
    {
        if (playerInventory == null) return false;

        foreach (var req in requirements)
        {
            var item = GetItem(req.itemID);
            if (item == null || !playerInventory.HasItem(item, req.quantity))
                return false;
        }
        return true;
    }

    private string GetMissingMaterialsText(BuildRequirement[] requirements)
    {
        var missing = new List<string>();

        foreach (var req in requirements)
        {
            var item = GetItem(req.itemID);
            if (item != null)
            {
                int has = playerInventory != null ? playerInventory.GetItemCount(item) : 0;
                if (has < req.quantity)
                {
                    missing.Add($"{item.displayName} ({has}/{req.quantity})");
                }
            }
        }

        return string.Join(", ", missing);
    }

    private void ConsumeMaterials(BuildRequirement[] requirements)
    {
        if (playerInventory == null) return;

        foreach (var req in requirements)
        {
            if (req.consumed)
            {
                var item = GetItem(req.itemID);
                if (item != null)
                {
                    playerInventory.RemoveItem(item, req.quantity);
                }
            }
        }
    }

    private ItemDefinition GetItem(string itemId)
    {
        // Try to get from database
        var database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database != null)
        {
            return database.GetItem(itemId);
        }

        // Try to load directly
        return Resources.Load<ItemDefinition>($"Items/{itemId}");
    }

    private void ShowMessage(string message)
    {
        // Try UI notification system
        var notificationSystem = FindObjectOfType<NotificationSystem>();
        if (notificationSystem != null)
        {
            notificationSystem.ShowNotification(message);
            return;
        }

        // Fallback to console
        Debug.Log($"[Fire] {message}");
    }

    private void ShowInteractionPrompt(string prompt)
    {
        // This would show UI prompt
        // For now just log
        Debug.Log(prompt);
    }

    private void HideInteractionPrompt()
    {
        // Hide UI prompt
    }

    private void PlaySound(string soundName)
    {
        // Play sound effect
        // AudioManager.Instance?.PlaySFX(soundName);
    }

    private void DisableGhostComponents(GameObject ghost)
    {
        // Disable all colliders
        foreach (var collider in ghost.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // Disable scripts
        foreach (var script in ghost.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!(script is Transform))
            {
                script.enabled = false;
            }
        }
    }

    private void SetGhostTransparency(GameObject ghost, float alpha)
    {
        foreach (var renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            foreach (var material in renderer.materials)
            {
                var color = material.color;
                color.a = alpha;
                material.color = color;

                // Enable transparency
                material.SetFloat("_Mode", 3);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }
    }

    private void SetGhostColor(GameObject ghost, Color color)
    {
        foreach (var renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            foreach (var material in renderer.materials)
            {
                color.a = material.color.a; // Preserve alpha
                material.color = color;
            }
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // Draw line to current fire
        if (currentFire != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentFire.transform.position);
        }
    }
}