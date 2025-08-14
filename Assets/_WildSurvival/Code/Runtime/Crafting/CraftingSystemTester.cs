using UnityEngine;

/// <summary>
/// Quick tester to ensure CraftingUI works in scene
/// Handles C key input and manages CraftingManager/UI instances
/// </summary>
public class CraftingSystemTester : MonoBehaviour
{
    private CraftingUI craftingUI;
    private CraftingManager craftingManager;
    private bool debugMode = true;

    void Start()
    {
        Debug.Log("[CraftingTester] Initializing crafting system...");

        // Ensure CraftingManager exists
        craftingManager = CraftingManager.Instance;
        if (craftingManager == null)
        {
            Debug.Log("[CraftingTester] Creating CraftingManager...");
            GameObject managerGO = new GameObject("CraftingManager");
            craftingManager = managerGO.AddComponent<CraftingManager>();
            DontDestroyOnLoad(managerGO);
        }

        // Find CraftingUI
        craftingUI = FindObjectOfType<CraftingUI>();

        if (craftingUI == null)
        {
            Debug.LogWarning("[CraftingTester] No CraftingUI found in scene! Add CraftingUI component to a UI Canvas.");
        }
        else
        {
            Debug.Log("[CraftingTester] CraftingUI found and ready!");
        }

        // Quick check for inventory
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[CraftingTester] No InventoryManager found - crafting may not work properly!");
        }
    }

    void Update()
    {
        // Handle C key for crafting
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[CraftingTester] C key pressed!");

            if (craftingUI != null)
            {
                craftingUI.ToggleUI();
                //Debug.Log($"[CraftingTester] Crafting UI toggled - IsOpen: {craftingUI.isOpen}");
                Debug.Log($"[CraftingTester] Crafting UI toggled!");
            }
            else
            {
                Debug.LogError("[CraftingTester] CraftingUI is null! Please add CraftingUI to the scene.");

                // Try to find it again
                craftingUI = FindObjectOfType<CraftingUI>();
                if (craftingUI != null)
                {
                    Debug.Log("[CraftingTester] Found CraftingUI on retry!");
                    craftingUI.ToggleUI();
                }
            }
        }

        // Debug keys
        if (debugMode)
        {
            // F1 - System status
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugSystemStatus();
            }

            // F2 - Force refresh recipes
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (craftingUI != null)
                {
                    craftingUI.RefreshRecipeList();
                    Debug.Log("[CraftingTester] Forced recipe list refresh");
                }
            }

            // F3 - Add test items to inventory
            if (Input.GetKeyDown(KeyCode.F3))
            {
                AddTestItems();
            }
        }
    }

    private void DebugSystemStatus()
    {
        Debug.Log("=== SYSTEM STATUS ===");
        Debug.Log($"CraftingManager: {(craftingManager != null ? "EXISTS" : "NULL")}");
        Debug.Log($"CraftingUI: {(craftingUI != null ? "EXISTS" : "NULL")}");
        Debug.Log($"InventoryManager: {(InventoryManager.Instance != null ? "EXISTS" : "NULL")}");

        if (craftingManager != null)
        {
            var recipes = craftingManager.GetAvailableRecipes();
            Debug.Log($"Available Recipes: {recipes.Count}");
            foreach (var recipe in recipes)
            {
                Debug.Log($"  - {recipe.displayName}");
            }
        }
    }

    private void AddTestItems()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[CraftingTester] No InventoryManager to add items to!");
            return;
        }

        Debug.Log("[CraftingTester] Adding test crafting materials...");

        // Add basic crafting materials
        var inventory = InventoryManager.Instance;

        // Basic resources
        inventory.AddItem("wood", 20);
        inventory.AddItem("stone", 15);
        inventory.AddItem("fiber", 10);
        inventory.AddItem("cloth", 5);
        inventory.AddItem("raw_meat", 5);
        inventory.AddItem("planks", 10);
        inventory.AddItem("nails", 50);

        Debug.Log("[CraftingTester] Test items added! Check inventory (Tab/I)");
    }

    private void OnGUI()
    {
        if (!debugMode) return;

        // Display help text
        GUI.Label(new Rect(10, 10, 300, 100),
            "CRAFTING SYSTEM CONTROLS:\n" +
            "C - Toggle Crafting UI\n" +
            "F1 - System Status\n" +
            "F2 - Refresh Recipes\n" +
            "F3 - Add Test Items\n" +
            "Tab/I - Inventory");
    }
}